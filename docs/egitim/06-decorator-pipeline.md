# Ortak işler, Decorator ve Pipeline:

## 1. Uygulamada gerçek sorun neydi?

Doğrulama çağrıları farklı use case'lerde tekrarlanıyordu. İşlem başlangıcı, sonucu ve
süresi için ortak loglama yoktu; 19 use case DI'a tek tek ekleniyordu. Bu ortak işleri
tek bir akışa bağladım.

## 2. Eklediğim parçalar

Dosyalar `todo.Application/Common/Execution` içinde:

- `IUseCase<TRequest,TResponse>`: bütün işlemlerin ortak sözleşmesi.
- `UseCase<TRequest,TResponse>`: public ExecuteAsync'i decorator'lara bağlar.
  İş mantığındaki HandleAsync protected olduğundan controller doğrudan onu çağırmaz.
- `UseCasePipeline`: iki decorator'ı sıraya dizer.
- `ValidationDecorator`: bu isteğin kayıtlı validator'larını bulup çalıştırır.
- `LoggingDecorator`: başlangıç, süre, sonuç, hata ve iptal bilgilerini loglar.
- `UseCaseRequests`: çok parametreli işlemler için küçük, tipli istek nesneleri.
- `UseCaseRequestValidators`: okuma/silme/tamamlama isteklerinde pozitif ID ve
  sayfa kontrolleri. Sayfa boyutu önceki değişikliğin ortak sınırını kullanır.

Gerçek akış:

```text
ExecuteAsync
  → LoggingDecorator: başlangıcı kaydet
    → ValidationDecorator: bilgileri doğrula
      → HandleAsync: asıl işi yap
  → LoggingDecorator: sonucu ve geçen süreyi kaydet
```

Doğrulama başarısızsa HandleAsync çalışmaz. Log decorator dışta olduğu için
doğrulama reddini de görür. Async validator'lar ValidateAsync ile ve sıralı
çalışır; aynı scoped DbContext'i kullanan validator'ları paralel koşturmuyorum.
Birden fazla validator'ın hataları ortak RequestValidationException içinde
toplanır; mevcut API hata eşlemesi bunları 400 olarak döndürür.

Liste ID'si ve görev ID'si alan eski ExecuteAsync overload'ları controller
çağrılarını küçük istek nesnelerine dönüştürüp ortak girişe gönderir. Parametresiz
işlemler EmptyRequest kullanır. Logout'taki Task sonucu, ortak sözleşme için
veri taşımayan Unit tipine bağlandı; HTTP yanıtı aynı kaldı.

## 3. Kaldırdığım parçalar

Yedi use case'teki validator field'ları, validator constructor bağımlılıkları
ve ValidateOrThrowAsync çağrıları çıkarıldı. Eski ValidatorExtensions.cs
dosyası artık gerekmediği için kaldırıldı. Doğrulama ortak decorator'da.

19 elle yazılmış DI use case kaydı kaldırıldı. AddApplication assembly'de
ortak sözleşmeyi uygulayan sınıfları tarar. Somut sınıf ve ortak arayüz aynı
scoped nesneye çözülür. Controller'lar mevcut somut bağımlılıklarıyla da
pipeline'dan geçer. Kayıtların tekrar eklenmemesi ve bütün işlemlerin ortak
giriş kapısını kullanması DI kayıtlarıyla sağlanır.

Application projesine .NET 8 ile uyumlu Microsoft.Extensions.Logging 8.0.1
eklendi. Yeni mediator veya interceptor framework'ü gerekmedi.

## 4. Kodun adım adım akışı

### Önce / sonra

Önceden CreateTodoUseCase içinde şu çağrı vardı:

```csharp
await _validator.ValidateOrThrowAsync(command, cancellationToken);
```

Aynı çağrı yedi farklı use case'te vardı. Validator field'ı, constructor
parametresi ve null kontrolü de tekrarlanıyordu. Application işlemlerinin
başlangıcı, sonucu ve süresi için ortak loglama yoktu. 19 use case DI'a tek
tek ekleniyordu.

Şimdi asıl iş `HandleAsync` içinde:

```csharp
public class CreateTodoUseCase : UseCase<CreateTodoCommand, TodoItemDto?>
{
    // Constructor: repository, current user, Unit of Work ve ortak pipeline alır.
    protected override async Task<TodoItemDto?> HandleAsync(
        CreateTodoCommand command, CancellationToken cancellationToken)
    {
        // Listeyi bul, erişimi kontrol et, görevi ekle, kaydet, yanıtı oluştur.
    }
}
```

Bu parça sınıfın yapısını gösterir; tam çalışan metot ilgili kaynak dosyada.
`CreateTodoCommand` gelen bilginin tipi; `TodoItemDto?` dönecek yanıtın tipi.
`UseCase<...>` ortak giriş kapısıdır.

## 5. Nerelerde uygulandı?

### Loglarda ne var?

İşlem adı, varsa Activity TraceId, geçen milisaniye ve sonuç/hata türü.
İstek ve yanıt içeriği, parola, token ve exception mesajı decorator loguna
yazılmaz. Hatalar aynı exception olarak üst katmana iletilir; iptal ayrı
loglanır. Null veya false sonuçlar NoResult olarak görünür. Bu nedenlerin ayrılması için
tipli sonuç sözleşmesi kullanılır.

### Kaydetme ve yetki sınırları

Kaydetme ilgili iş akışında kalır. Auth işlemleri hazırlama → SaveChanges →
oturum oluşturma sırasını korur. Refresh token tekrar kullanımında token
iptali kaydedildikten sonra hata dönülür. Hatırlatmalarda her gönderim
sonucu ayrı kaydedilir. Genel akışın sonunda otomatik SaveChanges yapmak bu
farklı zamanlamalara uygun olmazdı.

Liste sahipliği için mevcut veriyle yapılan erişim kontrolü HandleAsync veya
okuma sorgusunda, Admin endpoint yetkisi API'de devam eder. Validator girdi
şeklini kontrol eder; kullanıcının o kayda erişebileceğini tek başına kanıtlamaz.
UpdateTodo'nun tamamlanmış görevin mevcut tarihiyle ilgili bağlama bağlı
kontrolü de iş mantığında kalır.


