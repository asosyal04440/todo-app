# CQRS, projection, AsNoTracking ve sayfalama:

## 1. Uygulamada gerçek sorun neydi?

### Önce ne oluyordu?

`GetTodoListByIdUseCase`, sadece liste bilgisi göstermek için listeyle beraber
bütün görevlerini getiriyordu. `GetTodoByIdUseCase`, tek görev göstermek için
bütün görevleri getirip bellekte arıyordu. Görevler, listeler ve yönetici
kullanıcı listesi sınırsız diziler olarak dönüyordu.

`AsNoTracking` liste ve kullanıcı listeleme sorgularında zaten vardı; listeyi
görevleriyle yükleyen okuma yolunda yoktu. Review'deki genel ifadeyi bu gerçek
durumu dikkate alarak düzelttim.

## 2. Eklediğim parçalar

### Ne ekledim, nerede kullanıyorum?

#### Application: okuma ve değiştirme için ayrı yollar

`ITodoReadQueries` ve `IUserReadQueries` arayüzleri eklendi. Beş okuma use case'i
bu arayüzlere bağlandı:

- GetTodoListsUseCase
- GetTodoListByIdUseCase
- GetTodosInListUseCase
- GetTodoByIdUseCase
- GetUsersUseCase

Örneğin tek görev okuma use case'i artık şunu yapıyor:

```csharp
queries.GetTodoAsync(todoListId, todoId, currentUser.UserId, currentUser.IsAdmin, cancellationToken);
```

Bu hafif CQRS ayrımıdır. Aynı veritabanı kullanılır; veri değiştirme use
case'leri Domain nesnelerini ve Unit of Work'ü kullanmaya devam eder.

Eski repository arayüzleri ve gerçek repository'lerden sınırsız
`GetAllByOwnerAsync` / `GetAllAsync` metotları çıkarıldı. Yazma yolundaki
`GetByIdWithTodosAsync` kaldı; örneğin aynı isimli görev eklenmesini önlemek
için Domain'in mevcut görevleri bilmesi hâlâ gerekli.

#### Infrastructure: gereken alanları seçmek

`TodoReadQueries` ve `UserReadQueries` eklendi. Domain nesnesinin tamamını
yükleyip sonra DTO oluşturmak yerine SQL sorgusu doğrudan DTO alanlarını seçer:

```csharp
list => new TodoListDto(list.Id, list.Name, list.CreatedAtUtc)
```

Bu projection'dır. DTO'lara, bu alanları ayrı ayrı alan constructor'lar
eklendi. Domain nesnesi alan mevcut constructor'lar yazma yanıtlarında kullanılır.

Görev kartı açıklama, tarih ve önceliği de gösterdiği için görev DTO'su bu
alanları içerir. Normalizasyon ve hatırlatma takip alanları okunmaz. Kullanıcı
listesinde şifre hash'i veya Google subject değeri dönmez; SQL'de sadece bu
değerlerin var olup olmadığından boolean giriş yöntemi bilgisi üretilir.

#### Infrastructure: AsNoTracking

Yeni okuma sorguları `AsNoTracking` kullanır. Okumada değişiklik takibi tutulmaz.
Sadece scalar alanlardan DTO üretmek zaten entity takibi oluşturmaz;
AsNoTracking burada okuma niyetini de açıkça gösterir. Yazmada takip açık kalır.

#### API'den sorguya: sayfalama

Application'da `PageRequest` ve `PagedResult<T>`, API'de `PageQuery` eklendi.
Varsayılan sayfa 1, boyut 20; sayfa boyutu 1–100 olabilir. Sıfır, negatif,
100'den büyük boyutlar ve offset hesabını taşıran sayfalar reddedilir.

Ortak `ReadQueryPaging` sorguyu SQL'de sınırlar:

```csharp
int total = await orderedQuery.CountAsync(cancellationToken);
List<T> items = await orderedQuery
    .Skip(page.Offset)
    .Take(page.PageSize)
    .ToListAsync(cancellationToken);
```

`Skip` önceki sayfaları geçer; `Take` bu sayfanın sınırıdır. Öncesindeki
`OrderBy(...Id)` benzersiz, belirli bir sıralama sağlar. Verinin tamamını
belleğe alıp sonra sayfalama yapılmaz.

İstek örnekleri:

```text
GET /api/todo-lists?pageNumber=2&pageSize=20
GET /api/todo-lists/7/todos?pageNumber=2&pageSize=20
GET /api/admin/users?pageNumber=2&pageSize=20
```

Listeleme yanıtı artık doğrudan dizi yerine şu yapıdır:

```json
{
  "items": [],
  "totalCount": 21,
  "pageNumber": 2,
  "pageSize": 20,
  "totalPages": 2
}
```

Buradaki boş items yalnızca yapıyı gösterir; 21 kaydın ikinci sayfasında
normalde bir kayıt olur. Tek kayıt okuma ve yazma yanıtları aynı DTO yapısında
kalır. API'yi başka bir istemci tüketiyorsa liste dizisini `items` içinden
okuması gerekir; projedeki Angular istemcisi bu sözleşmeye uyarlandı.

Mevcut yetkiler korundu: kullanıcının liste koleksiyonu yalnızca kendi
listeleridir. Tek liste/görev ve listedeki görevler için sahibi veya Admin
erişebilir. Başkasının listesi normal kullanıcıya 404 olarak gizlenir;
Admin kullanıcı listesi hâlâ Admin yetkisi ister. Erişilebilen boş liste boş
sayfa döner; gizli veya bulunamayan liste null/404 döner.

## 3. Kaldırdığım parçalar

Okuma yolundaki sınırsız GetAllByOwnerAsync / GetAllAsync metotlarını çıkardım. Tek
kayıt okumak için aggregate'in bütün görevlerini yükleyen yolu DTO projection
sorgularıyla değiştirdim. Yazma yolundaki GetByIdWithTodosAsync korundu.

## 4. Kodun adım adım akışı

API PageQuery'yi alır; Application PageRequest ile sınırları doğrular. Okuma sorgusu
erişim şartını ve sıralamayı uygular, CountAsync ile toplamı hesaplar, Skip / Take ile
yalnızca istenen sayfayı getirir. PagedResult içindeki items ve toplam bilgileri
frontend'e döner.

## 5. Nerelerde uygulandı?

### Frontend'de ne değişti?

API servisleri `pageNumber` / `pageSize` gönderir ve `PagedResult` okur.
Liste kenar çubuğu, görevler ve yönetici kullanıcı ekranında Önceki/Sonraki
düğmeleri eklendi. Toplam kayıt sayısı API'den gelir. Tamamlanan görev sayısı
sadece görünen sayfaya ait olduğundan etikette “Bu sayfada” yazar.

Liste değişince görevlerin ilk sayfasına dönülür. Yeni kayıt oluşturulunca
sona eklenen kayıt için son sayfa yüklenir. Son sayfadaki kayıt silinip sayfa
boşalırsa önceki geçerli sayfa yüklenir. Görevlerde eski bir isteğin geç gelen
yanıtı yeni sayfanın üstüne yazamaz. Sayfa düğmeleri yükleme/yazma sırasında
devre dışı kalır. Frontend tüm sayfaları arka arkaya getirerek tekrar sınırsız
bir liste oluşturmuyor.


