# Result, tipli hata ve HTTP eşlemesi:

## 1. Uygulamada gerçek sorun neydi?

19 use case artık `Result<T>` dönüyor. İşlem başarılıysa sonuç verisini;
reddedildiyse nedenini taşıyor. Önceden aynı `null` veya `false`, farklı
sorunları saklıyordu. Şimdi kod ve loglar bunları ayırabiliyor.

`Result` API'nin JSON yanıtına eklenen bir kutu değil. Application içindeki
sözleşme. API, başarıda mevcut DTO'yu, hatada ProblemDetails yanıtını gönderiyor.

## 2. Eklediğim parçalar

- `Common/Results/Result.cs`: `IsSuccess`, `Value`, `Error`; yalnızca Success
  veya Failure ile oluşturuluyor. Başarıya null veri konulamıyor. Hatalı
  sonuçtan Value okumak programlama hatası olarak exception oluşturuyor.
- `Common/Results/AppError.cs`: ErrorKind genel kategoriyi, ErrorCode somut
  nedeni belirliyor. Örneğin NotFound / ListNotFound. Validation alan hatalarını
  da taşıyor; girişteki sözlük ve dizilerin kopyası alınıyor.
- `Common/Results/AppErrors.cs`: ortak hata tanımları ve kullanıcı mesajları.
- `Common/Execution/ExpectedErrorDecorator.cs`: Domain kuralları ve kayıt
  çakışmalarından gelen belirli exception türlerini Result'a dönüştürüyor.
- `todo.API/Models/Responses/ResultHttpMapping.cs`: HTTP kararının ortak noktası.

## 3. Kaldırdığım parçalar

### Çıkardığım kod ve kazanç

Use case'lerden belirsiz null/false dönüşleri; controller'lardan bunları tek tek
kontrol eden dallar kaldırıldı. Auth controller'ın beklenen login hatalarını
exception yakalayarak yönetmesi yerine Result kullanılıyor. Cookie yalnızca
başarıda yazılıyor; geçersiz refresh oturumunda temizleniyor. Eski exception
handler'ları gerektiğinde aynı ortak HTTP eşlemesini kullanıyor.

Hazırla → kaydet → yanıt üret sırası korunuyor. Refresh token yeniden kullanımında
oturum iptalleri hata dönmeden önce kaydedilmeye devam ediyor. DTO projection,
AsNoTracking ve sayfalama korunuyor. Başarılı tek kayıt okumaları bir SQL sorgusu;
bulunamayan okumada nedeni ayırmak için sadece liste sahibinin ID'si sorgulanıyor.

Kazanç: hata nedenleri logda görünür; bütün controller'lar ortak
eşleme kullanır; mesaj değişse bile HTTP davranışı mesaj metnine bağlı değildir.

## 4. Kodun adım adım akışı

### Gerçek kod üzerinden önce / sonra

`todo.Application/Features/Todos/DeleteTodo/DeleteTodoUseCase.cs` içinde:

```csharp
// Önce: üç durumda da aynı cevap.
return false;

// Şimdi: hangi adımda reddedildiği belli.
return Result<Unit>.Failure(AppErrors.ListNotFound);
return Result<Unit>.Failure(AppErrors.AccessDenied);
return Result<Unit>.Failure(AppErrors.TodoNotFound);
```

Bu üç satır farklı if bloklarında. Liste yoksa ilk satır; liste başkasınınsa
ikinci; liste erişilebilir ama görev yoksa üçüncü çalışıyor. Hiçbirinde
SaveChanges çağrılmıyor.

Başarı yolu:

```csharp
await _unitOfWork.SaveChangesAsync(cancellationToken);
return Result<Unit>.Success(default);
```

`Unit`, “işlem tamamlandı, ayrıca döndürecek veri yok” anlamında küçük bir tip.
Silme endpoint'i başarılı olunca yine HTTP 204 dönüyor.

### Hata içeride nasıl ilerliyor?

```text
Logging → ExpectedError → Validation → HandleAsync
                                        ↓
                       Result.Success veya Result.Failure
                                        ↓
                    Log: sonuç + ErrorKind + ErrorCode + süre
                                        ↓
                              API: ToActionResult
```

ValidationDecorator alan hatalarını doğrudan Result ile döndürüyor. Use case'ler
liste/görev bulunamaması, erişim reddi ve geçersiz oturum gibi durumları açıkça
Failure ile ifade ediyor. Domain ve persistence'ın mevcut koruyucu exception'ları
Application sınırında ExpectedErrorDecorator tarafından uyarlanıyor. Örneğin
eşzamanlı unique index çakışması Conflict; sürüm çakışması ConcurrentWrite oluyor.

Veritabanına ulaşılamaması, beklenmeyen kod hatası ve iptal bu decorator tarafından
iş hatasına çevrilmiyor. API'nin mevcut 503/500 yönetimi ve iptal akışı devam ediyor.
Güncellemenin kayıtlı görevle birlikte değerlendirilen tarih kontrolü de aynı
validation Result'una uyarlanıyor.

## 5. Nerelerde uygulandı?

### API ne görüyor?

| İçerideki durum | HTTP yanıtı |
|---|---:|
| Liste yok / görev yok / kaynak erişimi reddedildi | 404 |
| Geçersiz alan veya Domain kuralı | 400 |
| Yanlış parola / geçersiz oturum | 401 |
| Aynı isim / kayıt çakışması / eşzamanlı değişiklik | 409 |

Kaynak erişimi reddedildiğinde dışarıya 404 maskesi bilinçli olarak korunuyor.
Başkasının listesinin varlığını açıklamamak için title/detail/type aynı;
ListNotFound veya AccessDenied kodu dışarıya yazılmıyor. İç loglar gerçek kodu
görüyor. Admin endpoint'inin rol yetkilendirmesi ASP.NET tarafından yönetiliyor
ve rol yetersizse mevcut 403 davranışı sürüyor.

ConcurrentWrite için frontend'in tanıdığı
`urn:todo:problem:concurrency-conflict` korunuyor. Eşleme hata mesajının içinde
kelime aramıyor; enum alanlarına bakıyor.


