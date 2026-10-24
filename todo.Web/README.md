# Todo Web

Bu klasör, Angular 21, TypeScript ve NG-ZORRO ile hazırlanmış web arayüzüdür. Verileri doğrudan SQL Server'dan okumaz; Todo API'ye HTTP/JSON istekleri gönderir.

## Çalıştırma

Projenin ana klasöründeki `HerSeyiBaslat.bat` dosyasını aç. Bu dosya SQL Server Express'i kontrol eder, API'yi ve Angular geliştirme sunucusunu başlatır; hazır olunca tarayıcıda `http://localhost:4200` adresini açar.

## Testler

.NET katman testlerini ana proje klasöründe çalıştır:

```powershell
dotnet test --solution .\TodoApi.sln --configuration E2E --no-restore
```

Angular bileşen ve HTTP servis testlerini `todo.Web` klasöründe çalıştır:

```powershell
npm test -- --watch=false
```

Tam uçtan uca testler Chromium tarayıcısında web arayüzü, gerçek .NET API ve SQL Server Express ile çalışır. İlk kullanımdan önce tarayıcıyı bir kez kur:

```powershell
npm run test:e2e:install
```

Ardından `todo.Web` klasöründe testi başlat:

```powershell
npm run test:e2e
```

Uçtan uca test komutu kendi geçici SQL veritabanını oluşturur, test API'sini `17070` portunda ve web arayüzünü `4201` portunda başlatır; çalışma bitince geçici veritabanını siler. Normal uygulama portlarındaki `7070` ve `4200` sunucularına veya onların veritabanına dokunmaz. SQL Server Express servisi açık olmalıdır.

## Klasör düzeni ve önemli dosyalar

Kod, ekranların yaptığı işe göre üç feature altında toplanır: `features/auth`, `features/todos`, `features/admin`. Her feature kendi sayfasını, HTTP servislerini, modellerini ve testlerini birlikte tutar.

- `src/app/features/todos/todo-page.html` ve `todo-page.ts`: görev ekranındaki component'leri ve olayları bağlayan container.
- `src/app/features/todos/todo.facade.ts`: liste/görev işlemleri, API çağrıları, sayfalama ve ekranın ortak state'i.
- `src/app/features/todos/components`: liste menüsü, liste başlığı, görev listesi, görev satırı ve oluşturma/düzenlemenin ortak formu.
- `src/app/features/todos/todo-date-time.ts`: görev formunun yerel tarih/UTC dönüşümleri.
- `src/app/features/todos/todo-api.service.ts`: görev/liste HTTP istekleri.
- `src/app/features/todos/todo.models.ts`: görev/liste JSON veri şekilleri.
- `src/app/features/auth/login-page.ts`: giriş ve kayıt ekranı.
- `src/app/features/admin/admin-users-page.ts`: kullanıcı yönetimi ekranı.
- `src/app/core/auth`: uygulama genelinde oturum, kullanıcı sözleşmeleri ve route guard'ları.
- `src/app/core/http/auth.interceptor.ts`: HTTP isteklerine oturum bilgisi ekleme.
- `src/app/core/layout`: workspace yerleşimi ve hesap menüsünü içeren üst bar.
- `src/app/shared/pagination/paged-result.ts`: feature'dan bağımsız sayfalama sözleşmesi.
- `src/app/app.routes.ts`: ekranları URL'lerle birleştiren kök route tanımları.
- `src/app/app.config.ts`: Angular servisleri, HTTP istemcisi ve Türkçe yerel ayarlar.
- `src/app/app.ts`: router'ın seçtiği ekranı gösteren kök component.

Feature'lar birbirini import etmez; kendi dosyaları, `core` ve `shared` üzerinden çalışır. `core`, `shared` kullanabilir; `shared` yalnızca kendi dosyalarına bağımlı olabilir. Kök `app.routes.ts` gibi birleştirme dosyaları feature'ları bağlayabilir.

`todo.Web` klasöründe mimari sınırlarını ve kontrol aracının testlerini çalıştır:

```powershell
npm run check:architecture
npm run test:architecture
```

Sınır kontrolü, `npm test` ve `npm run build` öncesinde de otomatik çalışır. Yeni bağımlılık eklemeden TypeScript'in import çözümlemesini kullanır.

Örnek bir görev ekleme akışı: `components/todo-form.ts`, `submitted` olayıyla TodoRequest verir; `todo-page.html` bu olayı `facade.createTodo($event)` çağrısına bağlar; `todo.facade.ts`, `TodoApiService.createTodo` üzerinden `POST /api/todo-lists/{todoListId}/todos` isteğini gönderir. İsteğin devamı `todo.API` controller'ı, Application kullanım senaryosu, repository ve SQL Server üzerinden ilerler.

TodoFacade, TodoPage'in `providers` listesinde tanımlıdır. Her açılan görev sayfası kendi state örneğini alır; form taslakları typed Reactive Forms ile ilgili component'te bulunur. Ortak state signal, türetilmiş değerler computed kullanır. switchMap yeni seçimde eski görev okumasını iptal eder. Hatalar `core/errors` üzerinden normalize edilir; `shared/errors` ve `shared/forms` component'leriyle gösterilir. Üç ekranın route/component kodu lazy yüklenir.

## Ortam ayarları ve deployment

API adresi geliştirmede `src/environments/environment.ts`, production'da
`src/environments/environment.prod.ts` içindedir. `angular.json` içindeki production
`fileReplacements`, temel environment dosyasını production sürümüyle değiştirir.
`main.ts` seçilen ayarı doğrular ve `API_URL` DI token'ı ile servislere verir.
Artık `runtime-config.json` okunmaz; adres değişince yeniden build gerekir.

İki ortamın varsayılanı `/api`'dir. Geliştirmede `proxy.conf.json` bu yolu
`localhost:7070`'ye iletir. Tarayıcı adresi `localhost:4200` olarak kalır.
E2E'nin ayrı proxy'si `17070`'yi kullanır. Production'da `/api` için hosting
sunucusunda backend reverse proxy gerekir. Gerçek hosting adresi bilinmediğinden
production dosyasına örnek veya yanlış bir sunucu adresi gömülmez.

Node 24 (veya TypeScript çalıştırabilen Node 22.18+) ile:

```powershell
npm start
npm run build:development
npm run build:production
npm run test:deployment
```

`npm run build` varsayılan olarak production build alır. Build komutu Angular CLI'yi
çalıştırır, ardından seçilen environment'ın API adresine göre çıktının CSP
`connect-src` iznini günceller. Ayrı API kullanılacaksa önce ilgili environment'ın
`apiUrl` değerini değiştirip tekrar `npm run build:production` çalıştır.
Backend `Cors:AllowedOrigins` ve varsa hosting CSP header'ı da aynı ortama uygun
olmalıdır. Doğrudan `ng build` CSP düzenleme adımını çalıştırmaz.
Ayar dosyalarına parola veya imzalama anahtarı konmaz; içerikleri kullanıcıya ulaşır.
Ayrıntılar: `../docs/egitim/17-runtime-config-di-csp.md`.

## Lazy loading'i F12 ile doğrulama

1. Admin hesabıyla görev ekranına (`/`) gir. F12 → Network'ü aç, **Disable cache** seç.
2. F12 açıkken görev ekranını tam yenile. Network'te **JS** filtresini seç.
   `admin-users-page-...js` henüz gelmemeli.
3. Kaydı temizleyip **Kullanıcılar** bağlantısına tıkla. İlk geçişte
   `admin.routes-...js` ve `admin-users-page-...js` isteklerini görmelisin.
4. Network'teki `/api/admin/users` isteği veriyi, `admin-users-page-...js` ise ekranın
   kodunu taşır. Lazy loading kanıtı JavaScript isteğidir; yeni HTML belgesi beklenmez.
5. Görevlere dönüp tekrar Kullanıcılar'a girersen JS modülü zaten yüklüdür;
   tekrar istek yapılmaması normaldir. Disable cache, çalışan uygulamanın modül
   belleğini temizlemez; ilk yüklemeyi yeniden görmek için tam yenileme gerekir.

Geliştirme ve production çıktısında bu davranışı Chromium ile test etmek için:

```powershell
npm run test:lazy
```

Bu test 4310/4311 portlarında iki ayrı frontend açar; API yanıtlarını yalnızca
testte taklit eder ve gerçek veritabanını kullanmaz. İlk açılışta admin JS'inin
gelmediğini, ilk Kullanıcılar tıklamasında geldiğini, ikinci geçişte yeniden
istenmediğini doğrular. Network kayıtları ve ekran görüntüsü `test-results/lazy-loading`
altına yazılır. Bu test gerçek API/oturum testlerinin yerine geçmez.

Derlenmiş production dosyalarını gerçek API/ayrı SQL veritabanıyla tarayıcıda kontrol etmek için:

```powershell
npm run test:e2e:production
```

Bu komut build, yerel production preview ve Playwright testlerini çalıştırır; test veritabanını sonunda siler. Preview gerçek hosting sunucusunun yerine geçmez.
