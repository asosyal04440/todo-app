# Feature-based Angular mimarisi:

## 1. Uygulamada gerçek sorun neydi?

Frontend dosyaları yaptıkları işe göre değil, pages/services/models gibi dosya türlerine
göre ayrılmıştı. Görev ekranıyla ilgili bir değişiklikte birden fazla klasör arasında
dolaşıyordum.

### Feature ne demek?

Feature, uygulamanın bir işi gerçekleştiren bölümüdür. Görev ekleme, silme,
listeleme ve bunların HTTP istekleri birlikte `todos` feature'ını oluşturur.

Bir tamir çekmecesi düşün: tornavida başka odada, vida başka odada, kullanım
kılavuzu başka odadaysa tek iş için üç yere gidersin. Aynı işe ait parçaları
bir çekmecede toplamak, kodda feature düzenidir.

### Önce / sonra

Önceden dosyanın türüne göre üç ayrı yer vardı:

```text
pages/todos/todo-page.ts
services/todo-api.service.ts
models/todo.models.ts
```

Görev ekranıyla ilgili bir değişiklikte bu klasörler arasında dolaşıyordum.
Şimdi hepsi aynı işin yanında:

```text
todo.Web/src/app/
├── app.ts, app.routes.ts, app.config.ts
├── core/
│   ├── api-url.ts
│   ├── auth/   → oturum servisi, modeller, guard'lar
│   └── http/   → auth interceptor
├── shared/
│   └── pagination/paged-result.ts
└── features/
    ├── auth/
    │   ├── login-page.ts, login-page.html, login-page.scss
    │   ├── google-identity.service.ts
    │   └── registration-validation.ts
    ├── todos/
    │   ├── todo-page.ts, todo-page.html, todo-page.scss
    │   ├── todo-api.service.ts
    │   ├── todo.models.ts
    │   └── todo-validation.ts
    └── admin/
        ├── admin-users-page.ts, .html, .scss
        └── admin-api.service.ts
```

Dosya sayısı az olduğundan her feature içinde tekrar pages/services/models
klasörleri açmadım. Sayfa ve servis yan yana bulunuyor.

## 2. Eklediğim parçalar

Auth, todos ve admin feature klasörlerini oluşturdum. Ortak oturum kodunu core, genel
sayfalama sözleşmesini shared altında topladım. TypeScript bağımlılık sınırlarını
kontrol eden aracı ekledim.

### Hangi dosyayı nereye taşıdım, neden?

Bu turda 26 mevcut dosya taşındı. Eski yolları kullanan import'lar güncellendi;
eski klasörlerde kopya dosya veya yönlendiren ara dosya bırakılmadı.

Tablodaki yollar `todo.Web/src/app` altındadır. Sayfaların HTML/SCSS dosyaları
da ilgili sayfayla birlikte taşındı.

| Eski yer | Yeni yer | Gerekçe |
|---|---|---|
| pages/login/login-page.* | features/auth/login-page.* | Giriş/kayıt ekranı bu işe ait |
| pages/todos/todo-page.* | features/todos/todo-page.* | Görev ekranı kendi feature'ında |
| pages/admin/admin-users-page.* | features/admin/admin-users-page.* | Kullanıcı yönetimi kendi feature'ında |
| services/todo-api.service.ts | features/todos/todo-api.service.ts | Görev HTTP çağrılarının sahibi todos |
| models/todo.models.ts | features/todos/todo.models.ts | Görev verilerinin şekli todos'a ait |
| services/admin-api.service.ts | features/admin/admin-api.service.ts | Admin HTTP çağrıları admin'e ait |
| services/google-identity.service.ts | features/auth/google-identity.service.ts | Google giriş arayüzü yalnızca login ekranında kullanılıyor |
| services/auth.service.ts | core/auth/auth.service.ts | Oturum bilgisi bütün uygulamada kullanılıyor |
| models/auth.models.ts | core/auth/auth.models.ts | Ortak oturum/kullanıcı sözleşmeleri |
| guards/auth.guards.ts | core/auth/auth.guards.ts | Ekranlara erişimi ortak oturuma göre kontrol ediyor |
| interceptors/auth.interceptor.ts | core/http/auth.interceptor.ts | HTTP istekleri için ortak oturum davranışı |
| models/paged-result.ts | shared/pagination/paged-result.ts | Hem görev hem admin listesinin kullandığı genel sözleşme |

Önceki notlardaki eski yolların yeni karşılıkları bu tabloda bulunabilir.

## 3. Kaldırdığım parçalar

Eski pages/services/models/guards/interceptors klasör yapısını kaldırdım. Taşınan
dosyalar için eski yerde kopya veya yönlendiren ara dosya bırakmadım; import yollarını
yeni konuma göre değiştirdim.

## 4. Kodun adım adım akışı

### Kodda ne değişti?

TodoPage daha önce farklı tür klasörlerine uzanıyordu:

```typescript
import { TodoApiService } from '../../services/todo-api.service';
import { TodoItem, TodoList, TodoPriority, TodoRequest } from '../../models/todo.models';
```

Şimdi görev parçaları kendi yanında, ortak oturum core'da:

```typescript
import { TodoApiService } from './todo-api.service';
import { TodoItem, TodoList, TodoPriority, TodoRequest } from './todo.models';
import { AuthService } from '../../core/auth/auth.service';
```

Metotların görevi ve API endpoint'leri aynı. Dosyanın yeni yerinden bağımlılığına
nasıl ulaşacağı değişti. Sayfaların template/style bağlantıları yanlarındaki
dosyaları kullanmaya devam ediyor.

### Domain kurallarıyla ilgili sabitlere ne oldu?

Eski `shared/validation-rules.ts`, giriş ve görev kurallarını birlikte tutuyordu.
İki ayrı feature'a ait kuralları ayırdım:

- `features/auth/registration-validation.ts`: AUTH_VALIDATION_LIMITS ve
  isRegistrationPasswordValid; email 256, görünen isim 100, şifre 8–128.
- `features/todos/todo-validation.ts`: TODO_VALIDATION_LIMITS; liste adı 80,
  görev başlığı 120, açıklama 1000.

Değerler ve şifre kontrolünün davranışı aynı. Frontend hâlâ backend Domain
kurallarının kullanıcıya erken geri bildirim veren kopyasını kullanıyor.
Gerçek iş kuralının sahibi backend Domain olmaya devam ediyor.

Sayfalama PagedResult ve DEFAULT_PAGE_SIZE ise feature'dan bağımsız olduğundan
`shared/pagination` içinde. Birden fazla ekranda kullanılması tek başına yeterli
değil; parçanın belirli bir işe bağımlı olmaması da gerekiyor.

## 5. Nerelerde uygulandı?

### AuthService neden auth feature'ında değil?

Giriş ekranı ile oturum yönetimi ayrı sorumluluklardır. Giriş ekranı kullanıcıdan
email/şifre alır. AuthService ise giriş sonucunu saklar; görev ve admin ekranı,
guard ve interceptor da bu oturumu kullanır.

AuthService'i giriş feature'ına koyarsam todos/admin, başka bir feature'a
bağımlı olur. Ortak oturum sözleşmesini `core/auth` içinde tutuyorum.
Google giriş arayüzü ise `features/auth` içinde kalıyor.

`providedIn: 'root'` servisin uygulama genelinde tek örnekle sunulmasını
sağlar. Dosyanın hangi işe ait olduğunu belirlemez. TodoApiService bu ayarı
taşısa da görev işine ait olduğundan `features/todos` altındadır.

### Klasör sınırlarını nasıl koruyorum?

| Kodun bulunduğu yer | Import edebileceği uygulama kodu |
|---|---|
| features/todos | Kendi feature'ı, core, shared |
| features/auth | Kendi feature'ı, core, shared |
| features/admin | Kendi feature'ı, core, shared |
| core | core, shared |
| shared | shared |
| Kök app dosyaları | Feature'ları, core ve shared'ı birleştirebilir |

Örneğin TodoPage'in AdminApiService'i import etmesi yasak. `app.routes.ts`
ise uygulamanın birleştirme noktasıdır: URL'leri üç feature'ın ekranlarına
bağlaması normaldir. Feature'lar bu kök dosyaları da import edemez.

`todo.Web/tools/check-feature-boundaries.mjs` kontrolünü ekledim. TypeScript'in
kod ağacını ve gerçek tsconfig import çözümlemesini kullanıyor. Şunları kontrol eder:

- Normal import'lar, type import'lar ve başka dosyadan yeniden dışa aktarmalar.
- Sabit yolu olan dinamik import'lar; hesaplanan dinamik import yollarını reddeder.
- Alias ile yazılmış yolların gerçek hedefleri.
- Feature'lar arası, core → feature ve shared → core gibi yasak bağımlılıklar.
- Uygulama dışındaki yerel dosyalara erişim, çözülemeyen yollar ve eski
  services gibi tanımlanmamış üst klasörlere yerleştirilen kod.

Angular/RxJS gibi harici paketlerin kullanılması serbest. Kontrol TypeScript
bağımlılıkları içindir. Yeni npm bağımlılığı eklemedim; mevcut TypeScript'i kullandım.

Bağımlılık kontrolünü derleme öncesine bağladım. Kural ihlalinde derleme
durur; klasör düzeninin zamanla bozulmasını böyle engelliyorum.

## 6. Ne kazandım?

Görev ekranında çalışırken ilgili sayfa, model ve HTTP servisi aynı yerde.
Bir feature'ın başka hangi bölümlere bağımlı olduğunu import'lardan ve otomatik
kontrolden görebiliyorum. Ortak alanlar da kendi sorumluluklarıyla sınırlı kalıyor.

Bu değişiklikte dosya sahipliğini ve bağımlılıkları düzenledim. Sayfa URL'leri,
erişim guard'ları, HTML/SCSS ve kullanıcı akışları korundu. Mevcut eager route
yüklemesi devam ediyor. Büyük TodoPage'in küçük component'lere ayrılması ve lazy loading
ayrı değişikliklerde ele alındı.

Resmî Angular rehberi de uygulama kodunu feature/tema etrafında toplama ve
ilgili dosyaları yan yana tutma yaklaşımını önerir:
[Angular style guide](https://angular.dev/style-guide).
