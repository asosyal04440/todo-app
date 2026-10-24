# Ortak frontend hata yönetimi:

## 1. Uygulamada gerçek sorun neydi?

Giriş, admin ve görev ekranları ProblemDetails JSON'unu ayrı ayrı yorumluyordu. Genel
hata mesajları tekrarlanıyor, API'nin alan hataları form kontrollerine ortak bir
yöntemle bağlanmıyordu.

### ProblemDetails ne?

API'nin hatayı JSON ile anlatma biçimi: status, title, detail, type ve bazen
errors sözlüğü. errors içindeki Title/Email gibi anahtarlar alan isimleridir.
Üç ekran ayrı ayrı bu JSON'u cast edip yorumluyordu. Bozuk/eksik içerik de
gelebileceğinden artık unknown veriyi tür kontrolüyle ortak yerde okuyorum.

`core/errors/api-error.service.ts` normalize metodu:

```typescript
const normalized = this.errors.normalize(error);
// normalized.status, message, problemType, fieldErrors
```

ApiError zaten normalize edilmişse aynı nesne döner. Böylece interceptor'da
yapılan işlem sayfada tekrar yapılınca bildirim bilgisi kaybolmaz.

## 2. Eklediğim parçalar

ApiError modeli, ApiErrorService, genel API hata interceptor'ı, silent context ve ortak
ApiErrors görünümünü ekledim. Form hatalarını doğru işlem/hedef kayda göre ilgili
kontrole bağlıyorum.

## 3. Kaldırdığım parçalar

### Ortak component ve kod temizliği

`shared/errors/api-errors.ts`, giriş, admin ve görev ekranının ortak hata
görünümüdür; core'a bağımlı olmadan message/fieldErrors sözleşmesini gösterir.
Metin interpolasyonla çizilir; HTML olarak işlenmez.

LoginPage.describeError, adminin ayrı HttpErrorResponse yorumlaması ve
TodoFacade.showError içindeki tekrar eden genel status mesajları kaldırıldı.
Facade'da concurrency uyarısı/yenileme ve isim çakışması mesajı kaldı.
Checkbox da sunucunun onayladığı durumu gösterir; başarısız kayıtta tıklanmış
ama kaydedilmemiş bir tamamlandı işareti bırakmaz.

## 4. Kodun adım adım akışı

### Interceptor sırası neden önemli?

```typescript
withInterceptors([apiErrorInterceptor, authInterceptor])
```

İstek dıştan içe, hata içten dışa döner. Auth interceptor ham 401'i görüp
refresh/retry yapabilmeli. Hata modeli 401'den önce değiştirilirse auth
interceptor'ın HttpErrorResponse kontrolü kaçabilir. Normalizasyon bu yüzden
auth akışının dışındadır.

Oturumu otomatik geri yükleme, logout ve isteğe bağlı Google config sorgusu
SILENT_API_ERROR context'i taşır. Hata yine normalize edilir; bu arka plan
istekleri genel toast yağmuru üretmez. Gerçek giriş isteğinin hatası görünürdür.

### Alan hatası forma nasıl gidiyor?

API şunu döndürmüş olsun:

```json
{ "errors": { "Title": ["Başlık kabul edilmedi."] } }
```

```typescript
this.errors.applyFieldErrors(this.form, normalized);
```

Alan ismi büyük/küçük harf farkından bağımsız eşleştirilir. Kontrole server
hatası eklenir ve touched işaretlenir. FormFieldError bunu alan yanında gösterir.
DueAtUtc, görev formundaki dueAtLocal'e alias ile bağlanır. Kullanıcı değeri
değiştirince Angular validator'ları çalışır, eski server hatası kaldırılır.
Taslak reset edilmez. Bilinmeyen alanlar ortak hata özetinde de görülebilir.

TodoFacade errorTarget ve errorTodoId ile hatanın hangi işleme ait olduğunu
tutar. Liste oluşturma hatası yeniden adlandırma formuna; görev oluşturma
hatası başka görevin edit formuna gönderilmez. Sadece ilgili component apiError
input'unu alır. LoginPage kendi giriş/kayıt formuna eşler.

## 5. Nerelerde uygulandı?

### Nerede hangi hata ele alınıyor?

`core/http/api-error.interceptor.ts`: yalnızca yapılandırılmış API'nin
istekleri. 0 bağlantı hatası, 429 çok fazla deneme, 5xx sunucu hatalarında ortak
mesaj bildirir. notified işareti aynı hata için ikinci toast'ı engeller.

401 giriş/oturum; 409 aynı isim/e-posta; concurrency 409 yeniden yükleme gibi
işe özgü davranışlar ilgili ekranda/facade'da kalır. Yerel Todo hatası ortak
banner'da gösterilir; aynı mesajı bir de toast ile tekrarlamıyorum.
Teknik 5xx detail içeriği yerine kullanıcıya ortak anlaşılır mesaj veriliyor.

## 6. Ne kazandım?

Genel hata mesajlarını tek yerde yönetiyorum. Aynı hata için iki toast oluşmuyor; 401
refresh/retry akışı korunuyor. Kullanıcı API alan hatasını formda görüp taslağını
kaybetmeden düzeltebiliyor.
