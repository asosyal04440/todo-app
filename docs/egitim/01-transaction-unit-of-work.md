# Transaction ve Unit of Work:

## 1. Bizim uygulamada gerçek sorun neydi?

Bu projedeki gerçek hata özellikle **kullanıcı + oturum** akışındaydı.

Şifreyle girişte eski ayarlarla üretilmiş bir şifre hash'i, daha güçlü güncel
ayarlarla yenilenebiliyor. Hash, şifrenin veritabanında tutulan dönüşmüş halidir.
Ardından `RefreshTokens` tablosuna oturumu yenilemek için bir kayıt ekleniyor.

Eski sıralama:

1. Kullanıcıdaki hash değişikliğini kaydet.
2. Yeni oturum kaydını hazırla.
3. Oturum kaydını ayrıca kaydet.

Üçüncü adım hata verirse ilk değişiklik çoktan kaydedilmiş oluyordu.
Yeni kullanıcı kaydında da kullanıcı oluşturulup oturum oluşturulamama ihtimali vardı.

Google hesabı bağlanırken kullanıcının eski oturumları iptal ediliyordu.
Bu iptaller, kullanıcı repository'sinin aynı `DbContext` üzerinde yaptığı
`SaveChangesAsync` çağrısıyla kaydediliyordu. Kodun adı yalnızca kullanıcıyı
kaydediyormuş gibi görünüyordu ama bekleyen oturum değişikliklerini de kaydediyordu.

## 2. Eklediğim parçalar

| Parça | Görevi | Neden eklendi? |
|---|---|---|
| `IUnitOfWork` | Application katmanının toplu kaydetme sözleşmesi | Kaydetme kararının sahibi açık olsun. |
| `EfUnitOfWork` | Ortak `TodoDbContext.SaveChangesAsync` metodunu çağırır | Gerçek veritabanı yazımı Infrastructure katmanında kalsın. |
| Scoped DI kaydı | Repository'ler ve Unit of Work aynı istek kapsamındaki context'i kullanır | Hazırlanan değişiklikler aynı çalışma alanında toplansın. |
| `PendingAuthSession` | Henüz kaydedilmeyi bekleyen oturum verisini taşır | Kaydetmeden önce hazırlık, kaydettikten sonra giriş cevabı oluşturulabilsin. |
| `RefreshToken.User` ilişkisi ve kullanıcı alan constructor | Oturumu doğrudan kullanıcı nesnesine bağlar | Yeni kullanıcı numarası veritabanında üretilse bile kullanıcı ve token tek seferde kaydedilebilsin. |

## 3. Kaldırdığım parçalar

`IUserRepository`, `ITodoListRepository`, `IRefreshTokenRepository` ve
`ITodoReminderRepository` arayüzlerindeki `SaveChangesAsync` metodunu kaldırdım.
Infrastructure'daki dört repository'nin aynı işi yapan kaydetme metotlarını da kaldırdım.

Repository'ler veriyi getiriyor, eklenecek kaydı hazırlıyor veya silinecek kaydı
işaretliyor. İş akışını yöneten **use case**, Unit of Work üzerinden kaydetme
kararını veriyor. Use case, örneğin “şifreyle giriş yap” işini yöneten sınıftır.

`SessionIssuer` içindeki kaydetme çağrısını da kaldırdık. Oturum hazırlayan
yardımcı, kullanıcı değişikliklerini beklenmedik bir anda kaydetmiyor.

## 4. Giriş kodunu satır satır akışı

Eski `LoginWithPasswordUseCase` akışının ilgili kısmı:

```csharp
await _userRepository.SaveChangesAsync(cancellationToken);

AuthSession session = await _sessionIssuer.IssueAsync(user, cancellationToken);

return session;
```

İlk satır kullanıcı değişikliğini kaydediyordu. İkinci satırdaki `IssueAsync`
oturum kaydını hazırlıyor ve **kendi içinde bir kez daha kaydediyordu**.

Yeni kod:

```csharp
PendingAuthSession session = await _sessionIssuer.PrepareAsync(user, cancellationToken);

await _unitOfWork.SaveChangesAsync(cancellationToken);

return _sessionIssuer.CreateSession(session);
```

1. **PrepareAsync:** Oturum için token hazırlar, veritabanına eklenecek token
   kaydını işaretler. Bu aşamada kullanıcıdaki hash değişikliği de bekliyor.
2. **SaveChangesAsync:** Ortak context üzerindeki kullanıcı ve token
   değişikliklerini birlikte kaydeder. SQL Server sağlayıcısı, gerekli tüm
   yazımları EF Core'un varsayılan transaction davranışıyla atomik yapar.
3. **CreateSession:** Başarılı kayıttan sonra giriş cevabını ve access token'ı
   oluşturur. Kullanıcının gerçek veritabanı numarası artık bellidir.

Yeni kullanıcıda numarayı önceden almak için ayrıca kaydetmeye gerek kalmadı.
EF Core, kullanıcı nesnesiyle token arasındaki ilişkiyi izleyip üretilen numarayı
token'ın `UserId` alanına aynı kaydetme işlemi sırasında aktarır.
Bu yüzden önce kullanıcıyı, sonra token'ı kaydeden iki adımlı bir transaction'a
ve ona özel retry düzenlemesine ihtiyaç duymadık.

## 5. Nerelerde uygulandı?

- Şifreli kayıt, şifreli giriş ve Google girişi.
- Oturum yenileme ve çıkış.
- Liste oluşturma, ad değiştirme ve silme.
- Görev oluşturma, güncelleme, tamamlama ve silme.
- Hatırlatma sonuçlarının kaydedilmesi.

Oturum yenilemede eski token'ın iptaliyle yenisinin eklenmesi aynı kaydetme
işleminde kalıyor. Eski token'ın tekrar kullanılması tespit edildiğinde ise
aktif oturumların iptali **istek reddedilmeden önce** Unit of Work ile kaydediliyor.
Böylece güvenlik önlemi, hata cevabı verildi diye kaybolmuyor.

Hatırlatma işi, her e-posta denemesinin başarı veya başarısızlık sonucunu
kaydeden mevcut sınırlarını koruyor. Bütün e-postaları tek transaction'a almadık:
veritabanının geri alması gönderilmiş e-postayı geri getiremez.

## 6. Ne kazandık?

- Oturum kaydı başarısızsa yeni kullanıcı kaydı yarım kalmıyor.
- Token ekleme başarısızsa şifre hash'i güncellemesi geri alınıyor.
- Google bağlantısı, eski oturumların iptali ve yeni oturum kaydı birlikte başarılı oluyor.
- Kullanıcı repository'sinin token değişikliklerini de kaydettiği gizli bağımlılık kalkıyor.
- Kaydetme noktası kodu okuyan kişinin görebildiği tek bir yerde bulunuyor.
- Başarılı giriş/kayıt akışında iki `SaveChanges` yerine bir çağrı yapılıyor.

Bu garanti **veritabanına yapılan yazımları** kapsar. 


