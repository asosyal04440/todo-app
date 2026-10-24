# Projeyi akışa göre dosya dosya okuma sırası

**İlk açacağım dosya: [todo.Web/src/main.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/main.ts).** Tarayıcı önce HTML'i yükler; Angular uygulamasının başlangıç kodunu anlamak için main.ts üzerinden ilerliyorum. Listedeki sıra bir okuma rotasıdır: bütün dosyaların tek işlemde sırayla çalıştığı anlamına gelmez.

Bina sırası gibi önce giriş ve hazırlık, ardından ekrandan veritabanına gidip geri gelen bir işlem, sonra diğer özellikler ve ortak tesisat var. Aynı dosya birkaç işlemde kullanıldığı için listede bir kez yer alır; başka akışa geldiğimde ilgili metoduna geri dönerim.

- **Ana:** İşlemi başlatan, adımlarını yöneten veya doğrudan işi yapan dosya.
- **Yan:** Model, arayüz, kural, ayar, görünüm, hata, araç veya kontrol dosyası. Yan olması kullanılmadığı anlamına gelmez; validator gibi destek kodları da gerçek akışta çalışır.
- **Okurken:** Önce belirtilen metodu bulurum, sonra dosyanın kurucusunu, alanlarını ve kalan davranışlarını okurum. Girdinin kimden geldiğini, yapılan işin nedenini, sonucu kime verdiğini ve hata kolunu not ederim.

Bu liste mevcut projedeki **383 dosyayı** kapsar: uygulama kaynakları, ayarlar, migration ve otomatik üretilmiş EF model dosyaları, yardımcı araçlar, testler, belgeler ve statik varlıklar. node_modules, bin, obj, dist, .angular ve Git iç dosyaları bağımlılık/derleme çıktısı olduğu için bu okuma envanterinde yoktur. package-lock.json ise bağımlılık kaydı olarak listede bulunur.

## Klasör tabelalarının Türkçesi

| Ad | Buradaki anlamı |
|---|---|
| features | Kullanıcının yapabildiği işler/özellikler |
| components | Ekranı oluşturan bileşenler |
| core | Frontend'in uygulama genelindeki altyapısı |
| shared | Birden fazla frontend özelliğinin ortak kullandığı parçalar |
| abstractions | Application'ın ihtiyaç duyduğu sözleşmeler |
| contracts | Parçalar arasında taşınan veri biçimleri/DTO'lar |
| common | Kullanım senaryolarının ortak yardımcıları ve çalışma düzeni |
| persistence | Veritabanı erişimi ve kalıcı kayıt altyapısı |
| configuration | Tipli ayarlar ve doğrulamaları |
| services | Token/e-posta gibi hizmetlerin teknik uygulamaları |
| specifications | Seçim/uygunluk kurallarının toplandığı yer |
| migrations | Veritabanı yapısının değişim geçmişi |
| TestDoubles | Gerçek bağımlılık yerine testte kullanılan fake nesneler |

## İlk bütün tur nerede tamamlanıyor?

**1–10. aşamalar**, Web başlangıcından görev ekleme ve sonrasındaki liste okumasına kadar gidiş-dönüşü tamamlar. Başlangıçta görülen restoreSession, kullanıcı veya Hangfire dalları için ilgili sonraki aşamaya dönebilirim. Oturum yoksa görev ekranından önce giriş ekranı açılabilir; bu dosyalar 13–17. aşamalarda ayrıntılıdır.

Görev eklemenin kısa izi: **TodoForm → TodoFacade → TodoApiService → interceptor → TodosController → pipeline → CreateTodoUseCase → repository/Domain → Unit of Work/DbContext → SQL Server → DTO/HTTP → facade → sayfalı GET → ekran.** Application, Domain'i ve Infrastructure sözleşmelerini kullanır; Domain kendi başına SQL çağırmaz.

## Aşamalar

1. [Web açılışı: ilk dosyam main.ts](#asama-1) — dosya 1–12
2. [API'nin başlangıç hazırlığı](#asama-2) — dosya 13–21
3. [Görev ekranı: sayfa, yerleşim ve olay bağlantıları](#asama-3) — dosya 22–42
4. [Görev formundan API servisine çıkış](#asama-4) — dosya 43–52
5. [HTTP isteğinin controller ve kullanım senaryosuna ulaşması](#asama-5) — dosya 53–58
6. [Pipeline: asıl işten önce ve sonra çalışan zincir](#asama-6) — dosya 59–71
7. [Mevcut kullanıcı, erişim ve Domain kuralları](#asama-7) — dosya 72–81
8. [Repository'den gerçek kayda: ortak context ve EF Core](#asama-8) — dosya 82–90
9. [Başarı veya hatanın API'den ekrana dönmesi](#asama-9) — dosya 91–108
10. [Liste ve görevleri sayfalı okuma](#asama-10) — dosya 109–119
11. [Görev listesi oluşturma, ad değiştirme, silme ve tek liste okuma](#asama-11) — dosya 120–129
12. [Görevi düzenleme, tamamlama, silme ve tek görev okuma](#asama-12) — dosya 130–137
13. [Giriş ekranı ve Web oturum yönetimi](#asama-13) — dosya 138–144
14. [Auth API: istekler, cookie ve giriş kontrolleri](#asama-14) — dosya 145–153
15. [Parola ile kayıt ve giriş: kullanıcı nereden geliyor?](#asama-15) — dosya 154–167
16. [Oturum hazırlama, access/refresh token, yenileme ve çıkış](#asama-16) — dosya 168–185
17. [Google kimliği ve rol politikası](#asama-17) — dosya 186–194
18. [Admin kullanıcı listesinin gidiş ve dönüşü](#asama-18) — dosya 195–202
19. [Reminder: Hangfire'dan SMTP'ye ve kayıt sonucuna](#asama-19) — dosya 203–217
20. [Ayarların gerçek sınıfları ve başlangıç doğrulaması](#asama-20) — dosya 218–224
21. [Başlatıcı ve veritabanı hazırlama aracı](#asama-21) — dosya 225–230
22. [Migration geçmişi: tablo yapısı nasıl bu hâle geldi?](#asama-22) — dosya 231–251
23. [Derleme, paketler, proje bağlantıları ve yayın ayarı](#asama-23) — dosya 252–267
24. [Domain testleri: iş kuralının hangi durumlarda korunması bekleniyor?](#asama-24) — dosya 268–275
25. [Application testleri ve sahte bağımlılıklar](#asama-25) — dosya 276–296
26. [Infrastructure testleri](#asama-26) — dosya 297–310
27. [API testleri ve test sunucusu](#asama-27) — dosya 311–325
28. [Frontend birim testleri ve yardımcı araç testleri](#asama-28) — dosya 326–342
29. [Tarayıcı üzerinden uçtan uca kontroller](#asama-29) — dosya 343–349
30. [Editör, biçimlendirme ve Git yardımcı ayarları](#asama-30) — dosya 350–358
31. [Proje belgeleri ve statik kaynaklar](#asama-31) — dosya 359–383

<a id="asama-1"></a>

## 1. Web açılışı: ilk dosyam main.ts

API adresinin okunmasını, Angular'ın başlamasını ve hangi sayfanın seçildiğini izle. app.config.ts içindeki restoreSession dalının ayrıntısı auth bölümlerinde açılacak.

- [ ] **001.** [todo.Web/src/main.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/main.ts) — **Ana:** start(): runtime ayarını okuma, parseRuntimeConfig(), bootstrapApplication() ve başlangıç hatası.
- [ ] **002.** [todo.Web/src/index.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/index.html) — **Yan:** app-root: Angular'ın tarayıcıda yerleştiği kök nokta.
- [ ] **003.** [todo.Web/public/runtime-config.json](C:/Users/Bahadir/Desktop/todo/todo.Web/public/runtime-config.json) — **Yan:** apiUrl: çalışma ortamında hangi API adresi seçiliyor?
- [ ] **004.** [todo.Web/src/app/core/config/runtime-config.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/config/runtime-config.ts) — **Yan:** parseRuntimeConfig(), apiConnectSource(): adres doğrulaması ve bağlantı kaynağı.
- [ ] **005.** [todo.Web/src/app/core/config/api-url.token.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/config/api-url.token.ts) — **Yan:** API_URL ve isApiRequest(): adresin DI ile paylaşılması, isteğin bizim API'ye aitliğinin kontrolü.
- [ ] **006.** [todo.Web/src/app/app.config.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/app.config.ts) — **Ana:** HTTP interceptor sırası, router, restoreSession başlangıç işi ve dil ayarları.
- [ ] **007.** [todo.Web/src/app/app.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/app.ts) — **Ana:** app-root ve router-outlet: router'ın seçtiği sayfa nereye yerleşiyor?
- [ ] **008.** [todo.Web/src/app/app.routes.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/app.routes.ts) — **Ana:** Guard'lar, loadChildren ve URL → özellik bağlantıları.
- [ ] **009.** [todo.Web/src/app/core/auth/auth.guards.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/auth/auth.guards.ts) — **Yan:** authGuard(), guestGuard(), adminGuard(): yönlendirme kararları.
- [ ] **010.** [todo.Web/src/app/features/todos/todos.routes.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todos.routes.ts) — **Yan:** Görev özelliğinin TodoPage'e bağlanması.
- [ ] **011.** [todo.Web/src/app/features/auth/auth.routes.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/auth/auth.routes.ts) — **Yan:** Giriş özelliğinin LoginPage'e bağlanması.
- [ ] **012.** [todo.Web/src/app/features/admin/admin.routes.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/admin/admin.routes.ts) — **Yan:** Admin özelliğinin sayfaya bağlanması.

<a id="asama-2"></a>

## 2. API'nin başlangıç hazırlığı

Web isteği gelmeden API'nin hazırlanması gerekir. Add... hizmet kaydını, Use... istek işleme düzenini anlatır. Her HTTP isteğinde bütün Program.cs yeniden çalışmaz.

- [ ] **013.** [todo.API/Program.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Program.cs) — **Ana:** CreateBuilder → Add... kayıtları → Build → Use... zinciri → MapControllers → Run.
- [ ] **014.** [todo.API/Properties/launchSettings.json](C:/Users/Bahadir/Desktop/todo/todo.API/Properties/launchSettings.json) — **Yan:** Dosyanın tamamını oku: hangi değeri/sözleşmeyi sağlıyor, kim kullanıyor ve hangi akışa destek oluyor?
- [ ] **015.** [todo.API/appsettings.json](C:/Users/Bahadir/Desktop/todo/todo.API/appsettings.json) — **Yan:** Dosyanın tamamını oku: hangi değeri/sözleşmeyi sağlıyor, kim kullanıyor ve hangi akışa destek oluyor?
- [ ] **016.** [todo.API/appsettings.Development.json](C:/Users/Bahadir/Desktop/todo/todo.API/appsettings.Development.json) — **Yan:** Dosyanın tamamını oku: hangi değeri/sözleşmeyi sağlıyor, kim kullanıyor ve hangi akışa destek oluyor?
- [ ] **017.** [todo.Application/DependencyInjection.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/DependencyInjection.cs) — **Ana:** AddApplication(): use case, validator ve pipeline nesneleri DI'ye nasıl kaydediliyor?
- [ ] **018.** [todo.Infrastructure/DependencyInjection.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/DependencyInjection.cs) — **Ana:** AddInfrastructure(): arayüz → gerçek sınıf eşlemeleri; DbContext ve servis ömürleri.
- [ ] **019.** [todo.Infrastructure/Configuration/OptionsSetup.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Configuration/OptionsSetup.cs) — **Yan:** AddTodoOptions: Bind, ValidateOnStart, doğrulayıcı kayıtları.
- [ ] **020.** [todo.API/Authentication/AuthenticationSetup.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/AuthenticationSetup.cs) — **Yan:** AddTodoAuthentication: JWT kontrolü, OnTokenValidated, AdminOnly, rate limiter ve current-user kaydı.
- [ ] **021.** [todo.API/Security/SecurityHeadersMiddleware.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Security/SecurityHeadersMiddleware.cs) — **Yan:** InvokeAsync/UseSecurityHeaders: cevap güvenlik başlıkları.

<a id="asama-3"></a>

## 3. Görev ekranı: sayfa, yerleşim ve olay bağlantıları

Sayfanın hangi parçaları kullandığını oku. .ts davranışı, .html olay bağlantısını, .scss görünümü gösterir. Backend kaydını bu görsel dosyalarda arama.

- [ ] **022.** [todo.Web/src/app/features/todos/todo-page.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-page.ts) — **Ana:** ngOnInit(), inject(TodoFacade): ilk liste okuması nasıl başlıyor?
- [ ] **023.** [todo.Web/src/app/features/todos/todo-page.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-page.html) — **Ana:** Olaylar: create/select/rename/update/delete/page çağrıları hangi facade metoduna gidiyor?
- [ ] **024.** [todo.Web/src/app/features/todos/todo-page.scss](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-page.scss) — **Yan:** Bileşenin görünümü, yerleşimi ve ekran boyutu kuralları; kayıt/iş kuralı çalıştırmaz.
- [ ] **025.** [todo.Web/src/app/core/layout/workspace-layout.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/layout/workspace-layout.ts) — **Yan:** Yan alan ve ana alan yerleşimi; sayfa içeriği bu kabuğa nasıl veriliyor?
- [ ] **026.** [todo.Web/src/app/core/layout/workspace-layout.scss](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/layout/workspace-layout.scss) — **Yan:** Bileşenin görünümü, yerleşimi ve ekran boyutu kuralları; kayıt/iş kuralı çalıştırmaz.
- [ ] **027.** [todo.Web/src/app/core/layout/workspace-header.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/layout/workspace-header.ts) — **Yan:** Kullanıcı bilgisi, oluşturma/çıkış olayları ve input/output alanları.
- [ ] **028.** [todo.Web/src/app/core/layout/workspace-header.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/layout/workspace-header.html) — **Yan:** Gösterilen değerler, input/output bağları ve olayın çağırdığı metot. Yanındaki .ts dosyasıyla birlikte oku.
- [ ] **029.** [todo.Web/src/app/core/layout/workspace-header.scss](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/layout/workspace-header.scss) — **Yan:** Bileşenin görünümü, yerleşimi ve ekran boyutu kuralları; kayıt/iş kuralı çalıştırmaz.
- [ ] **030.** [todo.Web/src/app/features/todos/components/todo-list-sidebar.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-list-sidebar.ts) — **Yan:** Liste oluşturma, seçme ve sayfa değiştirme olayları.
- [ ] **031.** [todo.Web/src/app/features/todos/components/todo-list-sidebar.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-list-sidebar.html) — **Yan:** Gösterilen değerler, input/output bağları ve olayın çağırdığı metot. Yanındaki .ts dosyasıyla birlikte oku.
- [ ] **032.** [todo.Web/src/app/features/todos/components/todo-list-sidebar.scss](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-list-sidebar.scss) — **Yan:** Bileşenin görünümü, yerleşimi ve ekran boyutu kuralları; kayıt/iş kuralı çalıştırmaz.
- [ ] **033.** [todo.Web/src/app/features/todos/components/todo-list-header.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-list-header.ts) — **Yan:** Liste adını değiştirme ve listeyi silme olayları.
- [ ] **034.** [todo.Web/src/app/features/todos/components/todo-list-header.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-list-header.html) — **Yan:** Gösterilen değerler, input/output bağları ve olayın çağırdığı metot. Yanındaki .ts dosyasıyla birlikte oku.
- [ ] **035.** [todo.Web/src/app/features/todos/components/todo-list-header.scss](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-list-header.scss) — **Yan:** Bileşenin görünümü, yerleşimi ve ekran boyutu kuralları; kayıt/iş kuralı çalıştırmaz.
- [ ] **036.** [todo.Web/src/app/features/todos/components/todo-list-view.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-list-view.ts) — **Yan:** Görevleri satırlara aktarma; güncelleme/tamamlama/silme olaylarını üst sayfaya taşıma.
- [ ] **037.** [todo.Web/src/app/features/todos/components/todo-list-view.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-list-view.html) — **Yan:** Gösterilen değerler, input/output bağları ve olayın çağırdığı metot. Yanındaki .ts dosyasıyla birlikte oku.
- [ ] **038.** [todo.Web/src/app/features/todos/components/todo-list-view.scss](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-list-view.scss) — **Yan:** Bileşenin görünümü, yerleşimi ve ekran boyutu kuralları; kayıt/iş kuralı çalıştırmaz.
- [ ] **039.** [todo.Web/src/app/features/todos/components/todo-row.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-row.ts) — **Yan:** changeCompletion() ve satırdaki düzenleme/silme olayları; form ile bağlantı.
- [ ] **040.** [todo.Web/src/app/features/todos/components/todo-row.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-row.html) — **Yan:** Gösterilen değerler, input/output bağları ve olayın çağırdığı metot. Yanındaki .ts dosyasıyla birlikte oku.
- [ ] **041.** [todo.Web/src/app/features/todos/components/todo-row.scss](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-row.scss) — **Yan:** Bileşenin görünümü, yerleşimi ve ekran boyutu kuralları; kayıt/iş kuralı çalıştırmaz.
- [ ] **042.** [todo.Web/src/styles.scss](C:/Users/Bahadir/Desktop/todo/todo.Web/src/styles.scss) — **Yan:** Bileşenin görünümü, yerleşimi ve ekran boyutu kuralları; kayıt/iş kuralı çalıştırmaz.

<a id="asama-4"></a>

## 4. Görev formundan API servisine çıkış

submit → submitted olayı → facade.createTodo → API servisi. Facade'in diğer metotlarını okuduktan sonra ilgili kullanım senaryolarında tekrar bağlayacaksın.

- [ ] **043.** [todo.Web/src/app/features/todos/components/todo-form.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-form.ts) — **Ana:** Form tanımı → ngOnChanges() → submit() → submitted.emit(...).
- [ ] **044.** [todo.Web/src/app/features/todos/components/todo-form.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-form.html) — **Yan:** Gösterilen değerler, input/output bağları ve olayın çağırdığı metot. Yanındaki .ts dosyasıyla birlikte oku.
- [ ] **045.** [todo.Web/src/app/features/todos/components/todo-form.scss](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-form.scss) — **Yan:** Bileşenin görünümü, yerleşimi ve ekran boyutu kuralları; kayıt/iş kuralı çalıştırmaz.
- [ ] **046.** [todo.Web/src/app/features/todos/todo.models.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo.models.ts) — **Yan:** TodoItem/TodoList/TodoRequest/TodoPriority: ekran ile HTTP arasında hangi alanlar taşınıyor?
- [ ] **047.** [todo.Web/src/app/features/todos/todo-validation.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-validation.ts) — **Yan:** Frontend alan sınırları; Domain/backend sınırlarıyla ilişkisi.
- [ ] **048.** [todo.Web/src/app/features/todos/todo-date-time.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-date-time.ts) — **Yan:** Yerel form tarihi ile UTC arasında dönüşüm.
- [ ] **049.** [todo.Web/src/app/shared/forms/form-validators.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/shared/forms/form-validators.ts) — **Yan:** requiredTrimmed ve tarih kontrolü: kullanıcıya erken geri bildirim.
- [ ] **050.** [todo.Web/src/app/shared/forms/form-field-error.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/shared/forms/form-field-error.ts) — **Yan:** Alan hatası hangi öncelikle seçilip gösteriliyor?
- [ ] **051.** [todo.Web/src/app/features/todos/todo.facade.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo.facade.ts) — **Ana:** İlk durak createTodo(); sonra constructor/loadTodos/fetchTodoPage; kalan CRUD metotlarına ileride yeniden dön.
- [ ] **052.** [todo.Web/src/app/features/todos/todo-api.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-api.service.ts) — **Ana:** Önce createTodo(): HttpClient.post ve URL. Sonra get/put/patch/delete yolları.

**Bağlantı:** Formdaki submitted olayının todo-page.html üzerinden facade.createTodo() metoduna gittiğini gözünle bul.

<a id="asama-5"></a>

## 5. HTTP isteğinin controller ve kullanım senaryosuna ulaşması

İstek çıkarken interceptor; API'ye varınca güvenlik düzeni ve controller. JSON istek modeliyle Application command'ini ayrı gör.

- [ ] **053.** [todo.Web/src/app/core/http/auth.interceptor.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/http/auth.interceptor.ts) — **Ana:** authInterceptor/sendWithToken: Bearer başlığı, auth cookie ayarı ve 401 sonrası refresh.
- [ ] **054.** [todo.Web/src/app/core/http/api-error.interceptor.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/http/api-error.interceptor.ts) — **Ana:** apiErrorInterceptor: geri gelen HTTP hatası nerede normalize ediliyor?
- [ ] **055.** [todo.API/Controllers/TodosController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/TodosController.cs) — **Ana:** Önce Create(); sonra GetForList/GetById/Update/SetCompletionStatus/Delete.
- [ ] **056.** [todo.API/Models/Requests/CreateTodoRequest.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Requests/CreateTodoRequest.cs) — **Yan:** HTTP isteğinde kabul edilen alanlar; varsa zorunluluk/validation işaretleri.
- [ ] **057.** [todo.Application/Features/Todos/CreateTodo/CreateTodoCommand.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/CreateTodo/CreateTodoCommand.cs) — **Yan:** Bu kullanım senaryosuna taşınan alanları controller'daki oluşturma koduyla eşleştir.
- [ ] **058.** [todo.Application/Features/Todos/CreateTodo/CreateTodoUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/CreateTodo/CreateTodoUseCase.cs) — **Ana:** HandleAsync(): listeyi getir → CanAccess → AddTodo → SaveChangesAsync → DTO/Result.

<a id="asama-6"></a>

## 6. Pipeline: asıl işten önce ve sonra çalışan zincir

Logging → ExpectedError → Validation → Handler → HandleAsync sırasını ve ters yöndeki sonucu izle. ExecuteAsync üst sınıftan gelir.

- [ ] **059.** [todo.Application/Common/Execution/IUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/IUseCase.cs) — **Yan:** ExecuteAsync sözleşmesi: zincirdeki nesnelerin ortak giriş biçimi.
- [ ] **060.** [todo.Application/Common/Execution/UseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/UseCase.cs) — **Ana:** Kurucu, ExecuteAsync ve iç Handler: çağrı HandleAsync'e nasıl ulaşıyor?
- [ ] **061.** [todo.Application/Common/Execution/UseCasePipeline.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/UseCasePipeline.cs) — **Ana:** Wrap(): Validation, ExpectedError, Logging nesneleri nasıl iç içe kuruluyor?
- [ ] **062.** [todo.Application/Common/Execution/LoggingDecorator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/LoggingDecorator.cs) — **Ana:** ExecuteAsync(): başlangıç, süre, sonuç, iptal ve exception logları.
- [ ] **063.** [todo.Application/Common/Execution/ExpectedErrorDecorator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/ExpectedErrorDecorator.cs) — **Ana:** ExecuteAsync(): hangi bilinen exception hangi Result hatasına çevriliyor?
- [ ] **064.** [todo.Application/Common/Execution/ValidationDecorator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/ValidationDecorator.cs) — **Ana:** ExecuteAsync(): ValidateAsync, erken hata dönüşü ve sonraki adıma geçiş.
- [ ] **065.** [todo.Application/Features/Todos/CreateTodo/CreateTodoCommandValidator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/CreateTodo/CreateTodoCommandValidator.cs) — **Yan:** Kurucudaki RuleFor kuralları: hangi girdi hangi nedenle reddediliyor?
- [ ] **066.** [todo.Application/Common/Validation/SharedValidationRules.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Validation/SharedValidationRules.cs) — **Yan:** Validator'ların paylaştığı alan kuralları ve Domain sınırlarına başvuru.
- [ ] **067.** [todo.Application/Common/Execution/UseCaseRequests.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/UseCaseRequests.cs) — **Yan:** EmptyRequest, Unit, ListIdRequest, TodoIdRequest ve sayfa/tamamlanma request'leri.
- [ ] **068.** [todo.Application/Common/Execution/UseCaseRequestValidators.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/UseCaseRequestValidators.cs) — **Yan:** Ortak request'lerde kimlik ve sayfa denetimleri.
- [ ] **069.** [todo.Application/Common/Results/Result.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Results/Result.cs) — **Yan:** Success/Failure/Value/Error: başarı veya hata nasıl taşınıyor?
- [ ] **070.** [todo.Application/Common/Results/AppError.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Results/AppError.cs) — **Yan:** Hata türü, hata kodu ve hata bilgisinin biçimi.
- [ ] **071.** [todo.Application/Common/Results/AppErrors.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Results/AppErrors.cs) — **Yan:** Kullanım senaryolarının seçtiği ortak hata değerleri/fabrikaları.

**Bağlantı:** Zinciri kuran Wrap() ile asıl işin çalıştığı HandleAsync() aynı şey değil. Kurulum ve çalıştırmayı ayır.

<a id="asama-7"></a>

## 7. Mevcut kullanıcı, erişim ve Domain kuralları

CanAccess ve AddTodo çağrılarını tamamla. Sadece alan atamasını değil, sahiplik ve tutarlılık davranışını oku.

- [ ] **072.** [todo.Application/Abstractions/Authentication/ICurrentUser.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Authentication/ICurrentUser.cs) — **Yan:** Application mevcut kullanıcıdan hangi bilgileri istiyor?
- [ ] **073.** [todo.API/Authentication/HttpContextCurrentUser.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/HttpContextCurrentUser.cs) — **Yan:** UserId/IsAdmin/UserIdOrNull: doğrulanmış HTTP kimliğinden okuma.
- [ ] **074.** [todo.Application/Authorization/TodoListAccessPolicy.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Authorization/TodoListAccessPolicy.cs) — **Ana:** CanAccess(): Admin veya sahip olma kontrolü.
- [ ] **075.** [todo.Domain/Entities/TodoList.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Entities/TodoList.cs) — **Ana:** AddTodo/EnsureTitleIsUnique/IsOwnedBy; sonra Rename/RenameTodo/RemoveTodo.
- [ ] **076.** [todo.Domain/Entities/TodoItem.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Entities/TodoItem.cs) — **Ana:** Kurucu ve alan kuralları; durum, bitiş zamanı ve reminder davranışları.
- [ ] **077.** [todo.Domain/Common/AuditableEntity.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Common/AuditableEntity.cs) — **Yan:** Oluşturma, güncelleme ve soft delete alanlarının ortak tanımı.
- [ ] **078.** [todo.Domain/Common/NameComparison.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Common/NameComparison.cs) — **Yan:** NormalizeForLookup ve ad karşılaştırmasının ortak kaynağı.
- [ ] **079.** [todo.Domain/Enums/TodoPriority.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Enums/TodoPriority.cs) — **Yan:** Öncelik değerleri; JSON metni ve veritabanı karşılığını ayır.
- [ ] **080.** [todo.Domain/Exceptions/DomainRuleViolationException.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Exceptions/DomainRuleViolationException.cs) — **Yan:** Bu hata hangi koşulu anlatıyor, nerede üretiliyor ve decorator/handler'da nasıl karşılanıyor?
- [ ] **081.** [todo.Domain/Exceptions/DuplicateNameException.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Exceptions/DuplicateNameException.cs) — **Yan:** Bu hata hangi koşulu anlatıyor, nerede üretiliyor ve decorator/handler'da nasıl karşılanıyor?

<a id="asama-8"></a>

## 8. Repository'den gerçek kayda: ortak context ve EF Core

Arayüzde durma: DI eşlemesiyle gerçek sınıfa geç. Okuma, EF takibine ekleme ve kalıcı kaydetme farklı adımlardır.

- [ ] **082.** [todo.Application/Abstractions/Persistence/ITodoListRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Persistence/ITodoListRepository.cs) — **Yan:** Application'ın liste okuma/hazırlama/silme için istediği sözleşme.
- [ ] **083.** [todo.Infrastructure/Persistence/TodoListRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoListRepository.cs) — **Ana:** GetByIdWithTodosAsync; sonra ExistsWithNameForOwnerAsync/AddAsync/Delete.
- [ ] **084.** [todo.Application/Abstractions/Persistence/IUnitOfWork.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Persistence/IUnitOfWork.cs) — **Yan:** Application'ın ortak kaydetme sözleşmesi.
- [ ] **085.** [todo.Infrastructure/Persistence/EfUnitOfWork.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/EfUnitOfWork.cs) — **Ana:** SaveChangesAsync(): çağrının aynı TodoDbContext'e aktarılması.
- [ ] **086.** [todo.Infrastructure/Persistence/TodoDbContextConfiguration.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContextConfiguration.cs) — **Yan:** UseTodoSqlServer(): sağlayıcı ve geçici SQL hatası retry ayarları.
- [ ] **087.** [todo.Infrastructure/Persistence/TodoDbContext.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContext.cs) — **Ana:** Önce DbSet/SaveChangesAsync/PrepareAudit; sonra OnModelCreating ve Configure... eşlemeleri.
- [ ] **088.** [todo.Infrastructure/Persistence/TodoDbContext.SoftDeletion.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContext.SoftDeletion.cs) — **Yan:** ExpandSoftDeletionAsync(): bağlı kayıtların silme/iptal durumlarının hazırlanması.
- [ ] **089.** [todo.Infrastructure/Persistence/Auditing/AuditRecord.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Auditing/AuditRecord.cs) — **Yan:** İşlem geçmişinde hangi metadata saklanıyor, hangi veri saklanmıyor?
- [ ] **090.** [todo.Infrastructure/Persistence/DatabaseErrorTranslator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/DatabaseErrorTranslator.cs) — **Yan:** Translate(): SQL/EF hatası uygulama hatasına nasıl çevriliyor?

**Bağlantı:** IUnitOfWork metodunun gövdesi sözleşmede değil; DI'nin seçtiği EfUnitOfWork ve onun kullandığı context'tedir.

<a id="asama-9"></a>

## 9. Başarı veya hatanın API'den ekrana dönmesi

Görev eklemenin dönüş yolunu burada tamamla. Result/DTO HTTP'ye, HTTP sonucu frontend durumuna dönüşür. Exception sınıfları da bu davranışın yan parçalarıdır.

- [ ] **091.** [todo.Application/Contracts/TodoItemDto.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Contracts/TodoItemDto.cs) — **Yan:** Kurucular ve dışarı açılan alanlar: Domain nesnesinden hangi bilgiler alınıp istemciye veriliyor?
- [ ] **092.** [todo.API/Models/Responses/ResultHttpMapping.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Responses/ResultHttpMapping.cs) — **Ana:** ToActionResult/CreateProblem: Result → HTTP durum kodu ve ProblemDetails.
- [ ] **093.** [todo.API/ExceptionHandling/ProblemDetailsWriter.cs](C:/Users/Bahadir/Desktop/todo/todo.API/ExceptionHandling/ProblemDetailsWriter.cs) — **Yan:** WriteAsync/GetTraceId: hata cevabı ve takip bilgisinin yazılması.
- [ ] **094.** [todo.API/ExceptionHandling/ValidationExceptionHandler.cs](C:/Users/Bahadir/Desktop/todo/todo.API/ExceptionHandling/ValidationExceptionHandler.cs) — **Yan:** Dosyanın tamamını oku: hangi değeri/sözleşmeyi sağlıyor, kim kullanıyor ve hangi akışa destek oluyor?
- [ ] **095.** [todo.API/ExceptionHandling/DomainExceptionHandler.cs](C:/Users/Bahadir/Desktop/todo/todo.API/ExceptionHandling/DomainExceptionHandler.cs) — **Yan:** Dosyanın tamamını oku: hangi değeri/sözleşmeyi sağlıyor, kim kullanıyor ve hangi akışa destek oluyor?
- [ ] **096.** [todo.API/ExceptionHandling/ApplicationExceptionHandler.cs](C:/Users/Bahadir/Desktop/todo/todo.API/ExceptionHandling/ApplicationExceptionHandler.cs) — **Yan:** Dosyanın tamamını oku: hangi değeri/sözleşmeyi sağlıyor, kim kullanıyor ve hangi akışa destek oluyor?
- [ ] **097.** [todo.API/ExceptionHandling/DatabaseExceptionHandler.cs](C:/Users/Bahadir/Desktop/todo/todo.API/ExceptionHandling/DatabaseExceptionHandler.cs) — **Yan:** Dosyanın tamamını oku: hangi değeri/sözleşmeyi sağlıyor, kim kullanıyor ve hangi akışa destek oluyor?
- [ ] **098.** [todo.API/ExceptionHandling/UnhandledExceptionHandler.cs](C:/Users/Bahadir/Desktop/todo/todo.API/ExceptionHandling/UnhandledExceptionHandler.cs) — **Yan:** Dosyanın tamamını oku: hangi değeri/sözleşmeyi sağlıyor, kim kullanıyor ve hangi akışa destek oluyor?
- [ ] **099.** [todo.Web/src/app/core/errors/api-error.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/errors/api-error.ts) — **Yan:** Frontend ortak hata nesnesi ve alan bilgileri.
- [ ] **100.** [todo.Web/src/app/core/errors/api-error.context.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/errors/api-error.context.ts) — **Yan:** Sessiz hata işleme gibi HttpContext işaretleri.
- [ ] **101.** [todo.Web/src/app/core/errors/api-error.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/errors/api-error.service.ts) — **Yan:** normalize/report/applyFieldErrors: hata mesajı, bildirim ve form alanı bağlantısı.
- [ ] **102.** [todo.Web/src/app/shared/errors/api-errors.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/shared/errors/api-errors.ts) — **Yan:** Ortak hata bilgisinin Angular'da gösterilmesi.
- [ ] **103.** [todo.Application/Exceptions/RequestValidationException.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Exceptions/RequestValidationException.cs) — **Yan:** Bu hata hangi koşulu anlatıyor, nerede üretiliyor ve decorator/handler'da nasıl karşılanıyor?
- [ ] **104.** [todo.Application/Exceptions/AuthenticationFailedException.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Exceptions/AuthenticationFailedException.cs) — **Yan:** Bu hata hangi koşulu anlatıyor, nerede üretiliyor ve decorator/handler'da nasıl karşılanıyor?
- [ ] **105.** [todo.Application/Exceptions/EmailAlreadyRegisteredException.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Exceptions/EmailAlreadyRegisteredException.cs) — **Yan:** Bu hata hangi koşulu anlatıyor, nerede üretiliyor ve decorator/handler'da nasıl karşılanıyor?
- [ ] **106.** [todo.Application/Exceptions/ConcurrencyConflictException.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Exceptions/ConcurrencyConflictException.cs) — **Yan:** Bu hata hangi koşulu anlatıyor, nerede üretiliyor ve decorator/handler'da nasıl karşılanıyor?
- [ ] **107.** [todo.Application/Exceptions/ConflictException.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Exceptions/ConflictException.cs) — **Yan:** Bu hata hangi koşulu anlatıyor, nerede üretiliyor ve decorator/handler'da nasıl karşılanıyor?
- [ ] **108.** [todo.Application/Exceptions/DataAccessException.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Exceptions/DataAccessException.cs) — **Yan:** Bu hata hangi koşulu anlatıyor, nerede üretiliyor ve decorator/handler'da nasıl karşılanıyor?

<a id="asama-10"></a>

## 10. Liste ve görevleri sayfalı okuma

Oluşturma cevabından sonra loadTodos yeniden GET gönderir. SQL'deki sayfa aralığından Web'deki sayfa düğmesine kadar okuma yolunu tamamla.

- [ ] **109.** [todo.API/Controllers/TodoListsController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/TodoListsController.cs) — **Ana:** GetAll/Create/GetById/Rename/Delete: her HTTP çağrısını ilgili use case'e bağla.
- [ ] **110.** [todo.API/Models/Requests/PageQuery.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Requests/PageQuery.cs) — **Yan:** HTTP sayfa parametreleri ve ToPageRequest().
- [ ] **111.** [todo.Application/Contracts/PageRequest.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Contracts/PageRequest.cs) — **Yan:** Sayfa numarası/boyutu sınırları ve Offset hesaplaması.
- [ ] **112.** [todo.Application/Contracts/PagedResult.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Contracts/PagedResult.cs) — **Yan:** Items/TotalCount/PageNumber/PageSize/TotalPages cevabı.
- [ ] **113.** [todo.Application/Features/TodoLists/GetTodoLists/GetTodoListsUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/GetTodoLists/GetTodoListsUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.
- [ ] **114.** [todo.Application/Features/Todos/GetTodosInList/GetTodosInListUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/GetTodosInList/GetTodosInListUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.
- [ ] **115.** [todo.Application/Abstractions/Persistence/ITodoReadQueries.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Persistence/ITodoReadQueries.cs) — **Yan:** Dosyanın tamamını oku: hangi değeri/sözleşmeyi sağlıyor, kim kullanıyor ve hangi akışa destek oluyor?
- [ ] **116.** [todo.Infrastructure/Persistence/TodoReadQueries.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoReadQueries.cs) — **Ana:** GetListsAsync/GetTodosAsync/GetListAsync/GetTodoAsync; erişim ve DTO projection.
- [ ] **117.** [todo.Infrastructure/Persistence/ReadQueryPaging.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/ReadQueryPaging.cs) — **Ana:** ReadPageAsync(): CountAsync → Skip/Take → ToListAsync.
- [ ] **118.** [todo.Application/Contracts/TodoListDto.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Contracts/TodoListDto.cs) — **Yan:** Kurucular ve dışarı açılan alanlar: Domain nesnesinden hangi bilgiler alınıp istemciye veriliyor?
- [ ] **119.** [todo.Web/src/app/shared/pagination/paged-result.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/shared/pagination/paged-result.ts) — **Yan:** Backend sayfalı sonucunun frontend karşılığı ve varsayılan sayfa boyutu.

**Bu turun sonunda:** Görev eklenirken hangi kontrolün nerede yapıldığını, kaydın ne zaman gerçekleştiğini ve ekrana neden ikinci bir GET ile dönüldüğünü anlatabilmeliyim.

<a id="asama-11"></a>

## 11. Görev listesi oluşturma, ad değiştirme, silme ve tek liste okuma

Facade'deki createList/saveListName/deleteList metotlarına geri dön. Sonra her controller çağrısını ilgili kullanım senaryosuna bağla.

- [ ] **120.** [todo.API/Models/Requests/CreateTodoListRequest.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Requests/CreateTodoListRequest.cs) — **Yan:** HTTP isteğinde kabul edilen alanlar; varsa zorunluluk/validation işaretleri.
- [ ] **121.** [todo.Application/Features/TodoLists/CreateTodoList/CreateTodoListCommand.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/CreateTodoList/CreateTodoListCommand.cs) — **Yan:** Bu kullanım senaryosuna taşınan alanları controller'daki oluşturma koduyla eşleştir.
- [ ] **122.** [todo.Application/Features/TodoLists/CreateTodoList/CreateTodoListCommandValidator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/CreateTodoList/CreateTodoListCommandValidator.cs) — **Yan:** Kurucudaki RuleFor kuralları: hangi girdi hangi nedenle reddediliyor?
- [ ] **123.** [todo.Application/Features/TodoLists/CreateTodoList/CreateTodoListUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/CreateTodoList/CreateTodoListUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.
- [ ] **124.** [todo.API/Models/Requests/RenameTodoListRequest.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Requests/RenameTodoListRequest.cs) — **Yan:** HTTP isteğinde kabul edilen alanlar; varsa zorunluluk/validation işaretleri.
- [ ] **125.** [todo.Application/Features/TodoLists/RenameTodoList/RenameTodoListCommand.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/RenameTodoList/RenameTodoListCommand.cs) — **Yan:** Bu kullanım senaryosuna taşınan alanları controller'daki oluşturma koduyla eşleştir.
- [ ] **126.** [todo.Application/Features/TodoLists/RenameTodoList/RenameTodoListCommandValidator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/RenameTodoList/RenameTodoListCommandValidator.cs) — **Yan:** Kurucudaki RuleFor kuralları: hangi girdi hangi nedenle reddediliyor?
- [ ] **127.** [todo.Application/Features/TodoLists/RenameTodoList/RenameTodoListUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/RenameTodoList/RenameTodoListUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.
- [ ] **128.** [todo.Application/Features/TodoLists/DeleteTodoList/DeleteTodoListUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/DeleteTodoList/DeleteTodoListUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.
- [ ] **129.** [todo.Application/Features/TodoLists/GetTodoListById/GetTodoListByIdUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/GetTodoListById/GetTodoListByIdUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.

<a id="asama-12"></a>

## 12. Görevi düzenleme, tamamlama, silme ve tek görev okuma

TodosController'ın kalan metotları ile facade.saveTodo/setTodoCompletion/deleteTodo yollarını eşleştir. Tarih değişince reminder durumunun sıfırlanmasını da TodoItem'da tekrar gör.

- [ ] **130.** [todo.API/Models/Requests/UpdateTodoRequest.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Requests/UpdateTodoRequest.cs) — **Yan:** HTTP isteğinde kabul edilen alanlar; varsa zorunluluk/validation işaretleri.
- [ ] **131.** [todo.Application/Features/Todos/UpdateTodo/UpdateTodoCommand.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/UpdateTodo/UpdateTodoCommand.cs) — **Yan:** Bu kullanım senaryosuna taşınan alanları controller'daki oluşturma koduyla eşleştir.
- [ ] **132.** [todo.Application/Features/Todos/UpdateTodo/UpdateTodoCommandValidator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/UpdateTodo/UpdateTodoCommandValidator.cs) — **Yan:** Kurucudaki RuleFor kuralları: hangi girdi hangi nedenle reddediliyor?
- [ ] **133.** [todo.Application/Features/Todos/UpdateTodo/UpdateTodoUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/UpdateTodo/UpdateTodoUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.
- [ ] **134.** [todo.API/Models/Requests/SetTodoCompletionStatusRequest.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Requests/SetTodoCompletionStatusRequest.cs) — **Yan:** HTTP isteğinde kabul edilen alanlar; varsa zorunluluk/validation işaretleri.
- [ ] **135.** [todo.Application/Features/Todos/SetTodoCompletionStatus/SetTodoCompletionStatusUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/SetTodoCompletionStatus/SetTodoCompletionStatusUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.
- [ ] **136.** [todo.Application/Features/Todos/DeleteTodo/DeleteTodoUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/DeleteTodo/DeleteTodoUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.
- [ ] **137.** [todo.Application/Features/Todos/GetTodoById/GetTodoByIdUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/GetTodoById/GetTodoByIdUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.

<a id="asama-13"></a>

## 13. Giriş ekranı ve Web oturum yönetimi

Formdan parola veya Google giriş isteğinin nasıl çıktığını; cevabın signal'a nasıl yazıldığını oku. restoreSession ile bu bölüm Web başlangıcına da geri bağlanır.

- [ ] **138.** [todo.Web/src/app/features/auth/login-page.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/auth/login-page.ts) — **Ana:** submit/signIn/setUpGoogleSignIn/renderGoogleButton: form ve Google giriş yolları.
- [ ] **139.** [todo.Web/src/app/features/auth/login-page.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/auth/login-page.html) — **Yan:** Gösterilen değerler, input/output bağları ve olayın çağırdığı metot. Yanındaki .ts dosyasıyla birlikte oku.
- [ ] **140.** [todo.Web/src/app/features/auth/login-page.scss](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/auth/login-page.scss) — **Yan:** Bileşenin görünümü, yerleşimi ve ekran boyutu kuralları; kayıt/iş kuralı çalıştırmaz.
- [ ] **141.** [todo.Web/src/app/core/auth/auth.models.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/auth/auth.models.ts) — **Yan:** Giriş/kayıt isteği, AuthResult ve CurrentUser alanları.
- [ ] **142.** [todo.Web/src/app/core/auth/auth.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/auth/auth.service.ts) — **Ana:** login/register/loginWithGoogle → startSession; restore/refresh/logout/endSession.
- [ ] **143.** [todo.Web/src/app/features/auth/google-identity.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/auth/google-identity.service.ts) — **Ana:** renderButton/loadScript: Google callback'inden ID token'ın alınması.
- [ ] **144.** [todo.Web/src/app/features/auth/registration-validation.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/auth/registration-validation.ts) — **Yan:** Kayıt ekranındaki parola kuralı ve alan sınırları.

<a id="asama-14"></a>

## 14. Auth API: istekler, cookie ve giriş kontrolleri

AuthController'ın her metodunu oku. Önce Register/Login, ardından Google/Refresh/Logout/GetCurrentUser/GetConfig. Destek modelleri hangi alanın nereden geldiğini gösterir.

- [ ] **145.** [todo.API/Controllers/AuthController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/AuthController.cs) — **Ana:** Register/Login/Google/Refresh/Logout/GetCurrentUser/GetConfig: modeller, cookie ve cevap.
- [ ] **146.** [todo.API/Models/Requests/RegisterRequest.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Requests/RegisterRequest.cs) — **Yan:** HTTP isteğinde kabul edilen alanlar; varsa zorunluluk/validation işaretleri.
- [ ] **147.** [todo.API/Models/Requests/LoginRequest.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Requests/LoginRequest.cs) — **Yan:** HTTP isteğinde kabul edilen alanlar; varsa zorunluluk/validation işaretleri.
- [ ] **148.** [todo.API/Models/Requests/GoogleLoginRequest.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Requests/GoogleLoginRequest.cs) — **Yan:** HTTP isteğinde kabul edilen alanlar; varsa zorunluluk/validation işaretleri.
- [ ] **149.** [todo.API/Models/Responses/AuthConfigResponse.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Responses/AuthConfigResponse.cs) — **Yan:** Dosyanın tamamını oku: hangi değeri/sözleşmeyi sağlıyor, kim kullanıyor ve hangi akışa destek oluyor?
- [ ] **150.** [todo.API/Authentication/RefreshTokenCookie.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/RefreshTokenCookie.cs) — **Yan:** Write/Read/Delete/CreateOptions: ham token'ın cookie ile taşınması.
- [ ] **151.** [todo.API/Authentication/RequireClientHeaderAttribute.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/RequireClientHeaderAttribute.cs) — **Yan:** OnActionExecuting: auth yazma isteğindeki gerekli başlığın kontrolü.
- [ ] **152.** [todo.API/Authentication/AuthorizationPolicies.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/AuthorizationPolicies.cs) — **Yan:** AdminOnly gibi politika adları.
- [ ] **153.** [todo.API/Authentication/RateLimitingPolicies.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/RateLimitingPolicies.cs) — **Yan:** İstek sınırlandırma politikasının adı.

<a id="asama-15"></a>

## 15. Parola ile kayıt ve giriş: kullanıcı nereden geliyor?

Önce kayıt olma, sonra mevcut kullanıcıyla giriş. IPasswordHasher ve IUserRepository çağrılarını DI'deki gerçek sınıflara bağla.

- [ ] **154.** [todo.Application/Features/Authentication/RegisterWithPassword/RegisterWithPasswordCommand.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/RegisterWithPassword/RegisterWithPasswordCommand.cs) — **Yan:** Bu kullanım senaryosuna taşınan alanları controller'daki oluşturma koduyla eşleştir.
- [ ] **155.** [todo.Application/Features/Authentication/RegisterWithPassword/RegisterWithPasswordCommandValidator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/RegisterWithPassword/RegisterWithPasswordCommandValidator.cs) — **Yan:** Kurucudaki RuleFor kuralları: hangi girdi hangi nedenle reddediliyor?
- [ ] **156.** [todo.Application/Features/Authentication/RegisterWithPassword/RegisterWithPasswordUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/RegisterWithPassword/RegisterWithPasswordUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.
- [ ] **157.** [todo.Application/Features/Authentication/LoginWithPassword/LoginWithPasswordCommand.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/LoginWithPassword/LoginWithPasswordCommand.cs) — **Yan:** Bu kullanım senaryosuna taşınan alanları controller'daki oluşturma koduyla eşleştir.
- [ ] **158.** [todo.Application/Features/Authentication/LoginWithPassword/LoginWithPasswordCommandValidator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/LoginWithPassword/LoginWithPasswordCommandValidator.cs) — **Yan:** Kurucudaki RuleFor kuralları: hangi girdi hangi nedenle reddediliyor?
- [ ] **159.** [todo.Application/Features/Authentication/LoginWithPassword/LoginWithPasswordUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/LoginWithPassword/LoginWithPasswordUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.
- [ ] **160.** [todo.Application/Abstractions/Persistence/IUserRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Persistence/IUserRepository.cs) — **Yan:** Kullanıcıyı kimlik/e-posta/Google subject ile arama sözleşmesi.
- [ ] **161.** [todo.Infrastructure/Persistence/UserRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/UserRepository.cs) — **Ana:** GetByIdAsync/GetByEmailAsync/GetByGoogleSubjectIdAsync/AddAsync.
- [ ] **162.** [todo.Application/Abstractions/Authentication/IPasswordHasher.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Authentication/IPasswordHasher.cs) — **Yan:** Parola hash üretme ve doğrulama sözleşmesi.
- [ ] **163.** [todo.Infrastructure/Services/Authentication/AspNetCorePasswordHasher.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/AspNetCorePasswordHasher.cs) — **Yan:** HashPassword/VerifyPassword: Microsoft Identity altyapısına uyarlama.
- [ ] **164.** [todo.Domain/Entities/User.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Entities/User.cs) — **Ana:** RegisterWithPassword/RegisterWithGoogle/LinkGoogleAccount/PromoteToAdmin ve alan kuralları.
- [ ] **165.** [todo.Domain/Common/EmailNormalizer.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Common/EmailNormalizer.cs) — **Yan:** E-posta karşılaştırmasında kullanılan ortak normalleştirme.
- [ ] **166.** [todo.Domain/Common/PasswordPolicy.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Common/PasswordPolicy.cs) — **Yan:** Parola kuralının ortak Domain kaynağı.
- [ ] **167.** [todo.Domain/Enums/UserRole.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Enums/UserRole.cs) — **Yan:** User/Admin rol değerleri.

<a id="asama-16"></a>

## 16. Oturum hazırlama, access/refresh token, yenileme ve çıkış

PrepareAsync → SaveChangesAsync → CreateSession sırasını burada tamamla. Access token ile refresh token'ın farklı üretim, saklama ve iptal yollarını izle.

- [ ] **168.** [todo.Application/Features/Authentication/Common/SessionIssuer.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/Common/SessionIssuer.cs) — **Ana:** PrepareAsync → kaydetme çağıranda → CreateSession; iki token'ın ayrı üretim anları.
- [ ] **169.** [todo.Application/Features/Authentication/Common/PendingAuthSession.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/Common/PendingAuthSession.cs) — **Yan:** Henüz kaydedilmemiş oturumun tuttuğu kullanıcı/token/ham değer.
- [ ] **170.** [todo.Application/Features/Authentication/Common/AuthSession.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/Common/AuthSession.cs) — **Yan:** API'ye dönen sonuç ve cookie için gereken refresh bilgisinin bir araya gelmesi.
- [ ] **171.** [todo.Application/Features/Authentication/Common/RefreshTokenSettings.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/Common/RefreshTokenSettings.cs) — **Yan:** Refresh token süresinin Application'a verilen biçimi.
- [ ] **172.** [todo.Application/Contracts/AuthResultDto.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Contracts/AuthResultDto.cs) — **Yan:** Kurucular ve dışarı açılan alanlar: Domain nesnesinden hangi bilgiler alınıp istemciye veriliyor?
- [ ] **173.** [todo.Application/Contracts/UserDto.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Contracts/UserDto.cs) — **Yan:** Kurucular ve dışarı açılan alanlar: Domain nesnesinden hangi bilgiler alınıp istemciye veriliyor?
- [ ] **174.** [todo.Application/Abstractions/Authentication/IAccessTokenGenerator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Authentication/IAccessTokenGenerator.cs) — **Yan:** Access token üretme sözleşmesi.
- [ ] **175.** [todo.Application/Abstractions/Authentication/AccessToken.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Authentication/AccessToken.cs) — **Yan:** Üretilen token değeri ve bitiş zamanının taşınması.
- [ ] **176.** [todo.Infrastructure/Services/Authentication/JwtAccessTokenGenerator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/JwtAccessTokenGenerator.cs) — **Ana:** GenerateToken: claim'ler, imza, süre ve JsonWebTokenHandler.CreateToken.
- [ ] **177.** [todo.Infrastructure/Services/Authentication/JwtSettings.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/JwtSettings.cs) — **Yan:** Token üreticisinin kullandığı doğrulanmış ayarlar.
- [ ] **178.** [todo.Application/Abstractions/Authentication/ISecureTokenGenerator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Authentication/ISecureTokenGenerator.cs) — **Yan:** Rastgele token ve hash üretme sözleşmesi.
- [ ] **179.** [todo.Infrastructure/Services/Authentication/SecureTokenGenerator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/SecureTokenGenerator.cs) — **Ana:** GenerateToken/ComputeHash: rastgele refresh değeri ve SHA-256 hash.
- [ ] **180.** [todo.Domain/Entities/RefreshToken.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Entities/RefreshToken.cs) — **Ana:** IsActive/Revoke ve token ilişkileri; iptal ile soft delete farkı.
- [ ] **181.** [todo.Application/Abstractions/Persistence/IRefreshTokenRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Persistence/IRefreshTokenRepository.cs) — **Yan:** Refresh token kaydı arama ve takibe ekleme sözleşmesi.
- [ ] **182.** [todo.Infrastructure/Persistence/RefreshTokenRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/RefreshTokenRepository.cs) — **Ana:** GetByTokenHashAsync/GetActiveForUserAsync/AddAsync ve kullanıcı ilişkisi.
- [ ] **183.** [todo.Application/Features/Authentication/RefreshSession/RefreshSessionUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/RefreshSession/RefreshSessionUseCase.cs) — **Ana:** HandleAsync: hash ile bul → durumu kontrol et → döndür/iptal et → yenisini kaydet → oturum.
- [ ] **184.** [todo.Application/Features/Authentication/Logout/LogoutUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/Logout/LogoutUseCase.cs) — **Ana:** HandleAsync: token yoksa/zaten iptalse başarı; varsa Revoke ve kaydet.
- [ ] **185.** [todo.Application/Features/Authentication/GetCurrentUser/GetCurrentUserUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/GetCurrentUser/GetCurrentUserUseCase.cs) — **Ana:** HandleAsync: doğrulanmış UserId ile kullanıcıyı okuyup UserDto döndür.

<a id="asama-17"></a>

## 17. Google kimliği ve rol politikası

Google'dan gelen kimliğin doğrulanmasını kendi kullanıcı/oturum akışından ayır. Hesap bulma, bağlama, oluşturma ve rol yükseltme dallarını ayrı ayrı takip et.

- [ ] **186.** [todo.Application/Features/Authentication/LoginWithGoogle/LoginWithGoogleCommand.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/LoginWithGoogle/LoginWithGoogleCommand.cs) — **Yan:** Bu kullanım senaryosuna taşınan alanları controller'daki oluşturma koduyla eşleştir.
- [ ] **187.** [todo.Application/Features/Authentication/LoginWithGoogle/LoginWithGoogleCommandValidator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/LoginWithGoogle/LoginWithGoogleCommandValidator.cs) — **Yan:** Kurucudaki RuleFor kuralları: hangi girdi hangi nedenle reddediliyor?
- [ ] **188.** [todo.Application/Features/Authentication/LoginWithGoogle/LoginWithGoogleUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/LoginWithGoogle/LoginWithGoogleUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.
- [ ] **189.** [todo.Application/Abstractions/Authentication/IGoogleTokenValidator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Authentication/IGoogleTokenValidator.cs) — **Yan:** Google ID token doğrulama sözleşmesi.
- [ ] **190.** [todo.Application/Abstractions/Authentication/GoogleUserInfo.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Authentication/GoogleUserInfo.cs) — **Yan:** Doğrulanmış Google kimliğinin Application'a taşınan alanları.
- [ ] **191.** [todo.Infrastructure/Services/Authentication/GoogleIdTokenValidator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/GoogleIdTokenValidator.cs) — **Ana:** ValidateAsync: GoogleJsonWebSignature ile doğrulama ve GoogleUserInfo sonucu.
- [ ] **192.** [todo.Infrastructure/Services/Authentication/GoogleAuthenticationSettings.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/GoogleAuthenticationSettings.cs) — **Yan:** Google doğrulayıcısının kullandığı ayarlar ve etkinlik bilgisi.
- [ ] **193.** [todo.Application/Abstractions/Authentication/IRoleAssignmentPolicy.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Authentication/IRoleAssignmentPolicy.cs) — **Yan:** GetRoleForEmail sözleşmesi.
- [ ] **194.** [todo.Infrastructure/Services/Authentication/ConfiguredRoleAssignmentPolicy.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/ConfiguredRoleAssignmentPolicy.cs) — **Yan:** GetRoleForEmail: ayarlanmış Admin e-postalarıyla rol kararı.

<a id="asama-18"></a>

## 18. Admin kullanıcı listesinin gidiş ve dönüşü

Web guard yönlendirmesi ile API AdminOnly kararını ayır. Sorgu DTO üretir; ekran yalnız kullanıcıları listeler.

- [ ] **195.** [todo.Web/src/app/features/admin/admin-users-page.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/admin/admin-users-page.ts) — **Ana:** ngOnInit/loadUsers/changePage: kullanıcı sayfasını okuyup signal'lara yazma.
- [ ] **196.** [todo.Web/src/app/features/admin/admin-users-page.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/admin/admin-users-page.html) — **Yan:** Gösterilen değerler, input/output bağları ve olayın çağırdığı metot. Yanındaki .ts dosyasıyla birlikte oku.
- [ ] **197.** [todo.Web/src/app/features/admin/admin-users-page.scss](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/admin/admin-users-page.scss) — **Yan:** Bileşenin görünümü, yerleşimi ve ekran boyutu kuralları; kayıt/iş kuralı çalıştırmaz.
- [ ] **198.** [todo.Web/src/app/features/admin/admin-api.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/admin/admin-api.service.ts) — **Ana:** getUsers: sayfalı Admin HTTP isteği.
- [ ] **199.** [todo.API/Controllers/AdminUsersController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/AdminUsersController.cs) — **Ana:** AdminOnly koşulu ve GetAll() üzerinden kullanıcı okuması.
- [ ] **200.** [todo.Application/Features/Users/GetUsers/GetUsersUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Users/GetUsers/GetUsersUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.
- [ ] **201.** [todo.Application/Abstractions/Persistence/IUserReadQueries.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Persistence/IUserReadQueries.cs) — **Yan:** Kullanıcı DTO listesi için Application sözleşmesi.
- [ ] **202.** [todo.Infrastructure/Persistence/UserReadQueries.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/UserReadQueries.cs) — **Ana:** GetUsersAsync: AsNoTracking/projection/sayfalama.

<a id="asama-19"></a>

## 19. Reminder: Hangfire'dan SMTP'ye ve kayıt sonucuna

Bu yol kullanıcı HTTP isteğinden başlamaz. Job → use case → specification/repository → composer/sender → Domain işareti → SaveChanges → job sonucu sırasını izle.

- [ ] **203.** [todo.API/BackgroundJobs/BackgroundJobsSetup.cs](C:/Users/Bahadir/Desktop/todo/todo.API/BackgroundJobs/BackgroundJobsSetup.cs) — **Yan:** AddTodoBackgroundJobs/UseTodoBackgroundJobs: SQL depolama, worker, schedule ve dashboard.
- [ ] **204.** [todo.API/BackgroundJobs/TodoReminderJob.cs](C:/Users/Bahadir/Desktop/todo/todo.API/BackgroundJobs/TodoReminderJob.cs) — **Ana:** RunAsync: use case çağrısı, loglar, Hangfire retry ve çakışan çalışma kontrolü.
- [ ] **205.** [todo.Application/Features/Reminders/SendDueTodoReminders/SendDueTodoRemindersUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Reminders/SendDueTodoReminders/SendDueTodoRemindersUseCase.cs) — **Ana:** Önce HandleAsync/ExecuteAsync, sonra kurucu: girdi, bağımlılık, erken hata, kaydetme/okuma ve dönüş değerini bul.
- [ ] **206.** [todo.Application/Features/Reminders/SendDueTodoReminders/ReminderSettings.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Reminders/SendDueTodoReminders/ReminderSettings.cs) — **Yan:** LeadTime/Schedule/BatchSize/retry/saat dilimi değerlerinin Application biçimi.
- [ ] **207.** [todo.Application/Features/Reminders/SendDueTodoReminders/ReminderRunResult.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Reminders/SendDueTodoReminders/ReminderRunResult.cs) — **Yan:** Aday/başarı/hata ve SMTP hizmet durumu sonucu.
- [ ] **208.** [todo.Domain/Specifications/TodoReminderSpecification.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Specifications/TodoReminderSpecification.cs) — **Ana:** ToExpression/IsSatisfiedBy; DueRule/RetryRule ve derlenmiş bellek kontrolleri.
- [ ] **209.** [todo.Application/Abstractions/Persistence/ITodoReminderRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Persistence/ITodoReminderRepository.cs) — **Yan:** GetTodosDueForReminderAsync sözleşmesi; aynı dosyadaki TodoReminderCandidate modeli.
- [ ] **210.** [todo.Infrastructure/Persistence/TodoReminderRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoReminderRepository.cs) — **Ana:** Specification filtresi → görev/liste/kullanıcı join'i → batch → adaylar.
- [ ] **211.** [todo.Application/Features/Reminders/SendDueTodoReminders/TodoReminderEmailComposer.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Reminders/SendDueTodoReminders/TodoReminderEmailComposer.cs) — **Ana:** Compose: tarih, konu, düz metin ve HTML içeriğinin hazırlanması.
- [ ] **212.** [todo.Application/Abstractions/Email/EmailMessage.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Email/EmailMessage.cs) — **Yan:** İçerik hazırlayıcıdan göndericiye giden e-posta verisi.
- [ ] **213.** [todo.Application/Abstractions/Email/IEmailSender.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Email/IEmailSender.cs) — **Yan:** IsConfigured ve SendAsync sözleşmesi; gerçek sınıfı DI'den bul.
- [ ] **214.** [todo.Infrastructure/Services/Email/MailKitEmailSender.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Email/MailKitEmailSender.cs) — **Ana:** SendAsync: MimeMessage → Connect/Authenticate/Send/Disconnect → hata türü.
- [ ] **215.** [todo.Infrastructure/Services/Email/SmtpSettings.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Email/SmtpSettings.cs) — **Yan:** SMTP bağlantı değerleri ve yapılandırılmış olma koşulu.
- [ ] **216.** [todo.Application/Abstractions/Email/EmailRejectedException.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Email/EmailRejectedException.cs) — **Yan:** Bir mesajın reddedilmesi durumu; use case'teki retry dalıyla bağla.
- [ ] **217.** [todo.Application/Abstractions/Email/EmailServiceUnavailableException.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Email/EmailServiceUnavailableException.cs) — **Yan:** SMTP hizmetine erişilememe; job'ın erken bitiş/retry dalıyla bağla.

**Bağlantı:** Reminder'ın sonucu Web'e HTTP cevabı olarak dönmez. Gönderim bilgisi veritabanına, çalışma sonucu job/log/Hangfire'a gider.

<a id="asama-20"></a>

## 20. Ayarların gerçek sınıfları ve başlangıç doğrulaması

Başlangıçta gördüğün OptionsSetup bağlantısını burada tamamla. Ayarın JSON bölümü, Options nesnesi, ToSettings sonucu ve kullanan servis arasındaki yolu kur.

- [ ] **218.** [todo.Infrastructure/Configuration/JwtOptions.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Configuration/JwtOptions.cs) — **Yan:** SectionName, ayar alanları ve varsa ToSettings dönüşümü; bu ayarı kullanan sınıfı bul.
- [ ] **219.** [todo.Infrastructure/Configuration/SmtpOptions.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Configuration/SmtpOptions.cs) — **Yan:** SectionName, ayar alanları ve varsa ToSettings dönüşümü; bu ayarı kullanan sınıfı bul.
- [ ] **220.** [todo.Infrastructure/Configuration/ReminderOptions.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Configuration/ReminderOptions.cs) — **Yan:** SectionName, ayar alanları ve varsa ToSettings dönüşümü; bu ayarı kullanan sınıfı bul.
- [ ] **221.** [todo.Infrastructure/Configuration/SecurityOptions.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Configuration/SecurityOptions.cs) — **Yan:** SectionName, ayar alanları ve varsa ToSettings dönüşümü; bu ayarı kullanan sınıfı bul.
- [ ] **222.** [todo.Infrastructure/Configuration/GoogleAuthenticationOptions.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Configuration/GoogleAuthenticationOptions.cs) — **Yan:** SectionName, ayar alanları ve varsa ToSettings dönüşümü; bu ayarı kullanan sınıfı bul.
- [ ] **223.** [todo.Infrastructure/Configuration/AuthorizationOptions.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Configuration/AuthorizationOptions.cs) — **Yan:** SectionName, ayar alanları ve varsa ToSettings dönüşümü; bu ayarı kullanan sınıfı bul.
- [ ] **224.** [todo.Infrastructure/Configuration/RuntimeOptionsValidator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Configuration/RuntimeOptionsValidator.cs) — **Yan:** Validate/ValidateConstruction: hangi ayar başlangıçta neden reddediliyor?

<a id="asama-21"></a>

## 21. Başlatıcı ve veritabanı hazırlama aracı

Bunlar HTTP kullanım senaryosu değil. BAT'ın PowerShell'e, PowerShell'in migration aracına, aracın Infrastructure'a nasıl geçtiğini oku.

- [ ] **225.** [HerSeyiBaslat.bat](C:/Users/Bahadir/Desktop/todo/HerSeyiBaslat.bat) — **Yan:** PowerShell başlatıcısına geçiş ve çıkış kodu.
- [ ] **226.** [tools/Start-Todo.ps1](C:/Users/Bahadir/Desktop/todo/tools/Start-Todo.ps1) — **Yan:** Ön koşullar, veritabanı hazırlığı ve API/Web süreçlerinin başlatılması.
- [ ] **227.** [tools/TodoDatabaseMigrator/Program.cs](C:/Users/Bahadir/Desktop/todo/tools/TodoDatabaseMigrator/Program.cs) — **Yan:** Ayarları oku → context oluştur → TodoSchemaMigrator.MigrateAsync() → başarı/hata çıkışı.
- [ ] **228.** [tools/TodoDatabaseMigrator/TodoDatabaseMigrator.csproj](C:/Users/Bahadir/Desktop/todo/tools/TodoDatabaseMigrator/TodoDatabaseMigrator.csproj) — **Yan:** Hedef .NET sürümü, NuGet paketleri, proje referansları ve varsa özel derleme ayarları.
- [ ] **229.** [todo.Infrastructure/Persistence/TodoSchemaMigrator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoSchemaMigrator.cs) — **Ana:** MigrateAsync ve normalize adlar için özel transaction/hazırlık yolu.
- [ ] **230.** [todo.Infrastructure/Persistence/TodoDbContextDesignTimeFactory.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContextDesignTimeFactory.cs) — **Yan:** CreateDbContext: EF tasarım araçlarının context'i nasıl oluşturduğu.

<a id="asama-22"></a>

## 22. Migration geçmişi: tablo yapısı nasıl bu hâle geldi?

Tarih sırasıyla her ana migration'ın Up/Down metodunu, hemen ardından aynı migration'ın Designer dosyasını oku. Designer otomatik model tarifidir; en sonda snapshot'ı incele.

- [ ] **231.** [todo.Infrastructure/Persistence/Migrations/20260926081423_InitialTodoSchema.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20260926081423_InitialTodoSchema.cs) — **Yan:** Up(): şemaya ne ekliyor/değiştiriyor? Down(): geri alma tarifi ne? Ardından aynı adlı Designer dosyasına geç.
- [ ] **232.** [todo.Infrastructure/Persistence/Migrations/20260926081423_InitialTodoSchema.Designer.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20260926081423_InitialTodoSchema.Designer.cs) — **Yan:** EF'nin bu migration sonrası model tarifi; otomatik üretilmiş destek kodu. Aynı adlı ana migration ile karşılaştır.
- [ ] **233.** [todo.Infrastructure/Persistence/Migrations/20261006102802_AddUsersAndTodoListOwnership.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261006102802_AddUsersAndTodoListOwnership.cs) — **Yan:** Up(): şemaya ne ekliyor/değiştiriyor? Down(): geri alma tarifi ne? Ardından aynı adlı Designer dosyasına geç.
- [ ] **234.** [todo.Infrastructure/Persistence/Migrations/20261006102802_AddUsersAndTodoListOwnership.Designer.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261006102802_AddUsersAndTodoListOwnership.Designer.cs) — **Yan:** EF'nin bu migration sonrası model tarifi; otomatik üretilmiş destek kodu. Aynı adlı ana migration ile karşılaştır.
- [ ] **235.** [todo.Infrastructure/Persistence/Migrations/20261006105600_AddRefreshTokens.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261006105600_AddRefreshTokens.cs) — **Yan:** Up(): şemaya ne ekliyor/değiştiriyor? Down(): geri alma tarifi ne? Ardından aynı adlı Designer dosyasına geç.
- [ ] **236.** [todo.Infrastructure/Persistence/Migrations/20261006105600_AddRefreshTokens.Designer.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261006105600_AddRefreshTokens.Designer.cs) — **Yan:** EF'nin bu migration sonrası model tarifi; otomatik üretilmiş destek kodu. Aynı adlı ana migration ile karşılaştır.
- [ ] **237.** [todo.Infrastructure/Persistence/Migrations/20261006143805_AddTodoReminders.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261006143805_AddTodoReminders.cs) — **Yan:** Up(): şemaya ne ekliyor/değiştiriyor? Down(): geri alma tarifi ne? Ardından aynı adlı Designer dosyasına geç.
- [ ] **238.** [todo.Infrastructure/Persistence/Migrations/20261006143805_AddTodoReminders.Designer.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261006143805_AddTodoReminders.Designer.cs) — **Yan:** EF'nin bu migration sonrası model tarifi; otomatik üretilmiş destek kodu. Aynı adlı ana migration ile karşılaştır.
- [ ] **239.** [todo.Infrastructure/Persistence/Migrations/20261006151109_AddUniqueNames.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261006151109_AddUniqueNames.cs) — **Yan:** Up(): şemaya ne ekliyor/değiştiriyor? Down(): geri alma tarifi ne? Ardından aynı adlı Designer dosyasına geç.
- [ ] **240.** [todo.Infrastructure/Persistence/Migrations/20261006151109_AddUniqueNames.Designer.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261006151109_AddUniqueNames.Designer.cs) — **Yan:** EF'nin bu migration sonrası model tarifi; otomatik üretilmiş destek kodu. Aynı adlı ana migration ile karşılaştır.
- [ ] **241.** [todo.Infrastructure/Persistence/Migrations/20261008090452_AddReminderRetryTracking.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261008090452_AddReminderRetryTracking.cs) — **Yan:** Up(): şemaya ne ekliyor/değiştiriyor? Down(): geri alma tarifi ne? Ardından aynı adlı Designer dosyasına geç.
- [ ] **242.** [todo.Infrastructure/Persistence/Migrations/20261008090452_AddReminderRetryTracking.Designer.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261008090452_AddReminderRetryTracking.Designer.cs) — **Yan:** EF'nin bu migration sonrası model tarifi; otomatik üretilmiş destek kodu. Aynı adlı ana migration ile karşılaştır.
- [ ] **243.** [todo.Infrastructure/Persistence/Migrations/20261013075641_AddNormalizedNameColumns.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261013075641_AddNormalizedNameColumns.cs) — **Yan:** Up(): şemaya ne ekliyor/değiştiriyor? Down(): geri alma tarifi ne? Ardından aynı adlı Designer dosyasına geç.
- [ ] **244.** [todo.Infrastructure/Persistence/Migrations/20261013075641_AddNormalizedNameColumns.Designer.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261013075641_AddNormalizedNameColumns.Designer.cs) — **Yan:** EF'nin bu migration sonrası model tarifi; otomatik üretilmiş destek kodu. Aynı adlı ana migration ile karşılaştır.
- [ ] **245.** [todo.Infrastructure/Persistence/Migrations/20261013080022_EnforceNormalizedNameUniqueness.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261013080022_EnforceNormalizedNameUniqueness.cs) — **Yan:** Up(): şemaya ne ekliyor/değiştiriyor? Down(): geri alma tarifi ne? Ardından aynı adlı Designer dosyasına geç.
- [ ] **246.** [todo.Infrastructure/Persistence/Migrations/20261013080022_EnforceNormalizedNameUniqueness.Designer.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261013080022_EnforceNormalizedNameUniqueness.Designer.cs) — **Yan:** EF'nin bu migration sonrası model tarifi; otomatik üretilmiş destek kodu. Aynı adlı ana migration ile karşılaştır.
- [ ] **247.** [todo.Infrastructure/Persistence/Migrations/20261013110501_AddAuditMetadata.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261013110501_AddAuditMetadata.cs) — **Yan:** Up(): şemaya ne ekliyor/değiştiriyor? Down(): geri alma tarifi ne? Ardından aynı adlı Designer dosyasına geç.
- [ ] **248.** [todo.Infrastructure/Persistence/Migrations/20261013110501_AddAuditMetadata.Designer.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261013110501_AddAuditMetadata.Designer.cs) — **Yan:** EF'nin bu migration sonrası model tarifi; otomatik üretilmiş destek kodu. Aynı adlı ana migration ile karşılaştır.
- [ ] **249.** [todo.Infrastructure/Persistence/Migrations/20261015091041_AddSoftDeletion.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261015091041_AddSoftDeletion.cs) — **Yan:** Up(): şemaya ne ekliyor/değiştiriyor? Down(): geri alma tarifi ne? Ardından aynı adlı Designer dosyasına geç.
- [ ] **250.** [todo.Infrastructure/Persistence/Migrations/20261015091041_AddSoftDeletion.Designer.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261015091041_AddSoftDeletion.Designer.cs) — **Yan:** EF'nin bu migration sonrası model tarifi; otomatik üretilmiş destek kodu. Aynı adlı ana migration ile karşılaştır.
- [ ] **251.** [todo.Infrastructure/Persistence/Migrations/TodoDbContextModelSnapshot.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/TodoDbContextModelSnapshot.cs) — **Yan:** Migration'lardan sonra EF'nin hatırladığı modelin bütünü.

<a id="asama-23"></a>

## 23. Derleme, paketler, proje bağlantıları ve yayın ayarı

Buradaki dosyalar iş kuralı çalıştırmaz; projelerin nasıl derlenip bağlandığını ve frontend'in hangi ortam ayarıyla sunulduğunu belirler.

- [ ] **252.** [TodoApi.sln](C:/Users/Bahadir/Desktop/todo/TodoApi.sln) — **Yan:** Çözümdeki projeler ve proje düzeni.
- [ ] **253.** [global.json](C:/Users/Bahadir/Desktop/todo/global.json) — **Yan:** Bu projedeki test çalıştırıcısı ayarı; dosyanın gerçek içeriğine bak.
- [ ] **254.** [dotnet-tools.json](C:/Users/Bahadir/Desktop/todo/dotnet-tools.json) — **Yan:** Yerel dotnet-ef aracı ve sürüm kaydı.
- [ ] **255.** [todo.Domain/todo.Domain.csproj](C:/Users/Bahadir/Desktop/todo/todo.Domain/todo.Domain.csproj) — **Yan:** Hedef .NET sürümü, NuGet paketleri, proje referansları ve varsa özel derleme ayarları.
- [ ] **256.** [todo.Application/todo.Application.csproj](C:/Users/Bahadir/Desktop/todo/todo.Application/todo.Application.csproj) — **Yan:** Hedef .NET sürümü, NuGet paketleri, proje referansları ve varsa özel derleme ayarları.
- [ ] **257.** [todo.Infrastructure/todo.Infrastructure.csproj](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/todo.Infrastructure.csproj) — **Yan:** Hedef .NET sürümü, NuGet paketleri, proje referansları ve varsa özel derleme ayarları.
- [ ] **258.** [todo.API/todo.API.csproj](C:/Users/Bahadir/Desktop/todo/todo.API/todo.API.csproj) — **Yan:** Hedef .NET sürümü, NuGet paketleri, proje referansları ve varsa özel derleme ayarları.
- [ ] **259.** [todo.Web/package.json](C:/Users/Bahadir/Desktop/todo/todo.Web/package.json) — **Yan:** Angular/RxJS/NG-ZORRO ve geliştirme paketleri; scripts girişleri.
- [ ] **260.** [todo.Web/package-lock.json](C:/Users/Bahadir/Desktop/todo/todo.Web/package-lock.json) — **Yan:** Bağımlılıkların çözülmüş sürümleri ve bütünlük kayıtları; iş akışı kodu değildir.
- [ ] **261.** [todo.Web/angular.json](C:/Users/Bahadir/Desktop/todo/todo.Web/angular.json) — **Yan:** Angular derleme, geliştirme sunucusu ve test hedefleri.
- [ ] **262.** [todo.Web/tsconfig.json](C:/Users/Bahadir/Desktop/todo/todo.Web/tsconfig.json) — **Yan:** TypeScript derleme seçenekleri, dosya kapsamı ve ana tsconfig'e bağlılık.
- [ ] **263.** [todo.Web/tsconfig.app.json](C:/Users/Bahadir/Desktop/todo/todo.Web/tsconfig.app.json) — **Yan:** TypeScript derleme seçenekleri, dosya kapsamı ve ana tsconfig'e bağlılık.
- [ ] **264.** [todo.Web/tsconfig.spec.json](C:/Users/Bahadir/Desktop/todo/todo.Web/tsconfig.spec.json) — **Yan:** TypeScript derleme seçenekleri, dosya kapsamı ve ana tsconfig'e bağlılık.
- [ ] **265.** [todo.Web/proxy.conf.json](C:/Users/Bahadir/Desktop/todo/todo.Web/proxy.conf.json) — **Yan:** Bu ortamda frontend isteklerinin API'ye yönlendirilmesi; normal geliştirme ile e2e ayarını ayır.
- [ ] **266.** [todo.Web/tools/check-feature-boundaries.mjs](C:/Users/Bahadir/Desktop/todo/todo.Web/tools/check-feature-boundaries.mjs) — **Yan:** Frontend özellikleri arasındaki import sınırlarını denetleyen araç.
- [ ] **267.** [todo.Web/tools/configure-deployment.mjs](C:/Users/Bahadir/Desktop/todo/todo.Web/tools/configure-deployment.mjs) — **Yan:** Yayın için runtime ayarı ve ilgili güvenlik/ortam yapılandırmasını hazırlayan araç.

<a id="asama-24"></a>

## 24. Domain testleri: iş kuralının hangi durumlarda korunması bekleniyor?

Domain dosyalarını okuduktan sonra başarı ve sınır örneklerini karşılaştır. Testler üretim isteğinin çağrı zincirinde çalışmaz.

- [ ] **268.** [todo.Tests/todo.Domain.Tests/TodoItemTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Domain.Tests/TodoItemTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **269.** [todo.Tests/todo.Domain.Tests/TodoListTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Domain.Tests/TodoListTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **270.** [todo.Tests/todo.Domain.Tests/DuplicateNameRuleTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Domain.Tests/DuplicateNameRuleTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **271.** [todo.Tests/todo.Domain.Tests/TodoReminderRuleTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Domain.Tests/TodoReminderRuleTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **272.** [todo.Tests/todo.Domain.Tests/UserTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Domain.Tests/UserTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **273.** [todo.Tests/todo.Domain.Tests/PasswordPolicyTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Domain.Tests/PasswordPolicyTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **274.** [todo.Tests/todo.Domain.Tests/RefreshTokenTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Domain.Tests/RefreshTokenTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **275.** [todo.Tests/todo.Domain.Tests/todo.Domain.Tests.csproj](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Domain.Tests/todo.Domain.Tests.csproj) — **Yan:** Hedef .NET sürümü, NuGet paketleri, proje referansları ve varsa özel derleme ayarları.

<a id="asama-25"></a>

## 25. Application testleri ve sahte bağımlılıklar

Use case'in hangi girdide hangi repository/servisi kullanmasının beklendiğini oku. TestDoubles gerçek SQL/SMTP yerine geçen test nesneleridir.

- [ ] **276.** [todo.Tests/todo.Application.Tests/AuthenticationUseCaseTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/AuthenticationUseCaseTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **277.** [todo.Tests/todo.Application.Tests/DuplicateNameUseCaseTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/DuplicateNameUseCaseTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **278.** [todo.Tests/todo.Application.Tests/PageRequestTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/PageRequestTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **279.** [todo.Tests/todo.Application.Tests/ResultTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/ResultTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **280.** [todo.Tests/todo.Application.Tests/SessionUseCaseTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/SessionUseCaseTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **281.** [todo.Tests/todo.Application.Tests/TestDoubles/FakeAuthenticationServices.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/TestDoubles/FakeAuthenticationServices.cs) — **Yan:** Gerçek dış bağımlılık yerine geçen fake nesne: tuttuğu veri ve testin değiştirebildiği sonuçlar.
- [ ] **282.** [todo.Tests/todo.Application.Tests/TestDoubles/FakeCurrentUser.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/TestDoubles/FakeCurrentUser.cs) — **Yan:** Gerçek dış bağımlılık yerine geçen fake nesne: tuttuğu veri ve testin değiştirebildiği sonuçlar.
- [ ] **283.** [todo.Tests/todo.Application.Tests/TestDoubles/FakeRefreshTokenRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/TestDoubles/FakeRefreshTokenRepository.cs) — **Yan:** Gerçek dış bağımlılık yerine geçen fake nesne: tuttuğu veri ve testin değiştirebildiği sonuçlar.
- [ ] **284.** [todo.Tests/todo.Application.Tests/TestDoubles/FakeReminderServices.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/TestDoubles/FakeReminderServices.cs) — **Yan:** Gerçek dış bağımlılık yerine geçen fake nesne: tuttuğu veri ve testin değiştirebildiği sonuçlar.
- [ ] **285.** [todo.Tests/todo.Application.Tests/TestDoubles/FakeTodoListRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/TestDoubles/FakeTodoListRepository.cs) — **Yan:** Gerçek dış bağımlılık yerine geçen fake nesne: tuttuğu veri ve testin değiştirebildiği sonuçlar.
- [ ] **286.** [todo.Tests/todo.Application.Tests/TestDoubles/FakeUnitOfWork.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/TestDoubles/FakeUnitOfWork.cs) — **Yan:** Gerçek dış bağımlılık yerine geçen fake nesne: tuttuğu veri ve testin değiştirebildiği sonuçlar.
- [ ] **287.** [todo.Tests/todo.Application.Tests/TestDoubles/FakeUserRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/TestDoubles/FakeUserRepository.cs) — **Yan:** Gerçek dış bağımlılık yerine geçen fake nesne: tuttuğu veri ve testin değiştirebildiği sonuçlar.
- [ ] **288.** [todo.Tests/todo.Application.Tests/TestResults.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/TestResults.cs) — **Yan:** Testte Result başarı/hata beklentilerini okumayı kolaylaştıran yardımcılar.
- [ ] **289.** [todo.Tests/todo.Application.Tests/TestUseCasePipelines.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/TestUseCasePipelines.cs) — **Yan:** Testte use case zincirinin oluşturulmasına yardım eden kod.
- [ ] **290.** [todo.Tests/todo.Application.Tests/todo.Application.Tests.csproj](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/todo.Application.Tests.csproj) — **Yan:** Hedef .NET sürümü, NuGet paketleri, proje referansları ve varsa özel derleme ayarları.
- [ ] **291.** [todo.Tests/todo.Application.Tests/TodoListAuthorizationTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/TodoListAuthorizationTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **292.** [todo.Tests/todo.Application.Tests/TodoListUseCaseTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/TodoListUseCaseTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **293.** [todo.Tests/todo.Application.Tests/TodoReminderUseCaseTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/TodoReminderUseCaseTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **294.** [todo.Tests/todo.Application.Tests/TodoUseCaseTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/TodoUseCaseTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **295.** [todo.Tests/todo.Application.Tests/UseCasePipelineTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/UseCasePipelineTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **296.** [todo.Tests/todo.Application.Tests/ValidatorTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Application.Tests/ValidatorTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.

<a id="asama-26"></a>

## 26. Infrastructure testleri

EF modeli, transaction, soft delete, audit, sorgu, specification, ayar ve dış servis davranışlarının beklentilerini oku. SQLite ile SQL Server kontrolünün farkına dikkat et.

- [ ] **297.** [todo.Tests/todo.Infrastructure.Tests/SqlServerTestDatabase.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/SqlServerTestDatabase.cs) — **Yan:** Testlere özel SQL Server veritabanı/context hazırlığı.
- [ ] **298.** [todo.Tests/todo.Infrastructure.Tests/TodoDbContextModelTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/TodoDbContextModelTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **299.** [todo.Tests/todo.Infrastructure.Tests/UnitOfWorkTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/UnitOfWorkTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **300.** [todo.Tests/todo.Infrastructure.Tests/ReadQueryTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/ReadQueryTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **301.** [todo.Tests/todo.Infrastructure.Tests/TodoReminderSpecificationTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/TodoReminderSpecificationTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **302.** [todo.Tests/todo.Infrastructure.Tests/SoftDeletionTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/SoftDeletionTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **303.** [todo.Tests/todo.Infrastructure.Tests/AuditPersistenceTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/AuditPersistenceTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **304.** [todo.Tests/todo.Infrastructure.Tests/SqlServerNameRulesTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/SqlServerNameRulesTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **305.** [todo.Tests/todo.Infrastructure.Tests/OptionsConfigurationTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/OptionsConfigurationTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **306.** [todo.Tests/todo.Infrastructure.Tests/AuthenticationServiceTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/AuthenticationServiceTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **307.** [todo.Tests/todo.Infrastructure.Tests/EmailSenderTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/EmailSenderTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **308.** [todo.Tests/todo.Infrastructure.Tests/TestResults.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/TestResults.cs) — **Yan:** Testte Result başarı/hata beklentilerini okumayı kolaylaştıran yardımcılar.
- [ ] **309.** [todo.Tests/todo.Infrastructure.Tests/TestUseCasePipelines.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/TestUseCasePipelines.cs) — **Yan:** Testte use case zincirinin oluşturulmasına yardım eden kod.
- [ ] **310.** [todo.Tests/todo.Infrastructure.Tests/todo.Infrastructure.Tests.csproj](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/todo.Infrastructure.Tests.csproj) — **Yan:** Hedef .NET sürümü, NuGet paketleri, proje referansları ve varsa özel derleme ayarları.

<a id="asama-27"></a>

## 27. API testleri ve test sunucusu

Önce TodoApiFactory; ardından rota, kimlik, yetki, hata cevabı ve kayıt akışlarının uçtan uca beklentileri. Test context'i normal çalışan API bağlamı değildir.

- [ ] **311.** [todo.Tests/todo.API.Tests/TodoApiFactory.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/TodoApiFactory.cs) — **Yan:** WebApplicationFactory ve Testing ortamının ayarları.
- [ ] **312.** [todo.Tests/todo.API.Tests/ControllerRouteTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/ControllerRouteTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **313.** [todo.Tests/todo.API.Tests/AuthorizationAttributeTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/AuthorizationAttributeTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **314.** [todo.Tests/todo.API.Tests/SecurityPipelineTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/SecurityPipelineTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **315.** [todo.Tests/todo.API.Tests/AuthenticationPipelineTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/AuthenticationPipelineTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **316.** [todo.Tests/todo.API.Tests/UseCaseRegistrationTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/UseCaseRegistrationTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **317.** [todo.Tests/todo.API.Tests/UnitOfWorkApiTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/UnitOfWorkApiTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **318.** [todo.Tests/todo.API.Tests/PagedReadApiTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/PagedReadApiTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **319.** [todo.Tests/todo.API.Tests/AuditApiTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/AuditApiTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **320.** [todo.Tests/todo.API.Tests/ResultHttpMappingTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/ResultHttpMappingTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **321.** [todo.Tests/todo.API.Tests/ErrorHandlingTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/ErrorHandlingTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **322.** [todo.Tests/todo.API.Tests/DomainExceptionHandlerTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/DomainExceptionHandlerTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **323.** [todo.Tests/todo.API.Tests/ApplicationExceptionHandlerTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/ApplicationExceptionHandlerTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **324.** [todo.Tests/todo.API.Tests/OptionsStartupTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/OptionsStartupTests.cs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **325.** [todo.Tests/todo.API.Tests/todo.API.Tests.csproj](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.API.Tests/todo.API.Tests.csproj) — **Yan:** Hedef .NET sürümü, NuGet paketleri, proje referansları ve varsa özel derleme ayarları.

<a id="asama-28"></a>

## 28. Frontend birim testleri ve yardımcı araç testleri

Her .spec dosyasını aynı adlı üretim dosyasıyla birlikte karşılaştır. Araç testleri mimari sınır/yayın ayarı betiklerini denetler.

- [ ] **326.** [todo.Web/src/app/app.routes.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/app.routes.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **327.** [todo.Web/src/app/core/auth/auth.guards.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/auth/auth.guards.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **328.** [todo.Web/src/app/core/auth/auth.service.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/auth/auth.service.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **329.** [todo.Web/src/app/core/config/runtime-config.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/config/runtime-config.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **330.** [todo.Web/src/app/core/errors/api-error.service.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/errors/api-error.service.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **331.** [todo.Web/src/app/core/http/api-error.interceptor.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/http/api-error.interceptor.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **332.** [todo.Web/src/app/core/http/auth.interceptor.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/http/auth.interceptor.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **333.** [todo.Web/src/app/features/admin/admin-users-page.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/admin/admin-users-page.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **334.** [todo.Web/src/app/features/auth/login-page.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/auth/login-page.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **335.** [todo.Web/src/app/features/auth/registration-validation.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/auth/registration-validation.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **336.** [todo.Web/src/app/features/todos/components/todo-form.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-form.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **337.** [todo.Web/src/app/features/todos/components/todo-list-view.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-list-view.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **338.** [todo.Web/src/app/features/todos/todo-api.service.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-api.service.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **339.** [todo.Web/src/app/features/todos/todo-page.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-page.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **340.** [todo.Web/src/app/features/todos/todo.facade.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo.facade.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **341.** [todo.Web/tools/check-feature-boundaries.spec.mjs](C:/Users/Bahadir/Desktop/todo/todo.Web/tools/check-feature-boundaries.spec.mjs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **342.** [todo.Web/tools/configure-deployment.spec.mjs](C:/Users/Bahadir/Desktop/todo/todo.Web/tools/configure-deployment.spec.mjs) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.

<a id="asama-29"></a>

## 29. Tarayıcı üzerinden uçtan uca kontroller

Önce runner ve Playwright ayarını, sonra fixture ve kullanıcı senaryolarını oku. Bu dosyalar browser/API birlikte çalışırken gözlenen davranışı tarif eder.

- [ ] **343.** [todo.Web/e2e/run-e2e.mjs](C:/Users/Bahadir/Desktop/todo/todo.Web/e2e/run-e2e.mjs) — **Yan:** E2E senaryoları için ortamı ve süreçleri yöneten runner.
- [ ] **344.** [todo.Web/e2e/playwright.config.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/e2e/playwright.config.ts) — **Yan:** Tarayıcı testlerinin adresi, zaman sınırları ve çalıştırma ayarları.
- [ ] **345.** [todo.Web/e2e/fixtures.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/e2e/fixtures.ts) — **Yan:** E2E senaryolarının ortak hazırlığı ve test verisi/bağlamı.
- [ ] **346.** [todo.Web/e2e/serve-production.mjs](C:/Users/Bahadir/Desktop/todo/todo.Web/e2e/serve-production.mjs) — **Yan:** Derlenmiş frontend'i test için sunan yardımcı sunucu.
- [ ] **347.** [todo.Web/e2e/proxy.conf.json](C:/Users/Bahadir/Desktop/todo/todo.Web/e2e/proxy.conf.json) — **Yan:** Bu ortamda frontend isteklerinin API'ye yönlendirilmesi; normal geliştirme ile e2e ayarını ayır.
- [ ] **348.** [todo.Web/e2e/auth.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/e2e/auth.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.
- [ ] **349.** [todo.Web/e2e/todo-app.spec.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/e2e/todo-app.spec.ts) — **Yan:** Test adları → hazırlık → çalıştırılan metot → doğrulamalar. Başarı, sınır ve hata beklentilerini üretim dosyasıyla eşleştir.

<a id="asama-30"></a>

## 30. Editör, biçimlendirme ve Git yardımcı ayarları

Bunlar normal isteğin adımı değil; geliştirme ortamını düzenleyen yan dosyalardır. Uygulama servisleriyle editör MCP/başlatma ayarlarını karıştırma.

- [ ] **350.** [.gitignore](C:/Users/Bahadir/Desktop/todo/.gitignore) — **Yan:** Git'in izlemeyeceği bağımlılık, derleme çıktısı ve yerel dosya kalıpları.
- [ ] **351.** [todo.Tests/Directory.Build.props](C:/Users/Bahadir/Desktop/todo/todo.Tests/Directory.Build.props) — **Yan:** Test projelerinin ortak MSBuild ayarları.
- [ ] **352.** [todo.Web/.editorconfig](C:/Users/Bahadir/Desktop/todo/todo.Web/.editorconfig) — **Yan:** Editör/biçimlendirme kuralları; kodun iş davranışını değil yazım düzenini etkiler.
- [ ] **353.** [todo.Web/.gitignore](C:/Users/Bahadir/Desktop/todo/todo.Web/.gitignore) — **Yan:** Git'in izlemeyeceği bağımlılık, derleme çıktısı ve yerel dosya kalıpları.
- [ ] **354.** [todo.Web/.prettierrc](C:/Users/Bahadir/Desktop/todo/todo.Web/.prettierrc) — **Yan:** Editör/biçimlendirme kuralları; kodun iş davranışını değil yazım düzenini etkiler.
- [ ] **355.** [todo.Web/.vscode/extensions.json](C:/Users/Bahadir/Desktop/todo/todo.Web/.vscode/extensions.json) — **Yan:** Editörün görev, debug, eklenti veya MCP ayarı; normal HTTP kullanım senaryosunun parçası değildir.
- [ ] **356.** [todo.Web/.vscode/launch.json](C:/Users/Bahadir/Desktop/todo/todo.Web/.vscode/launch.json) — **Yan:** Editörün görev, debug, eklenti veya MCP ayarı; normal HTTP kullanım senaryosunun parçası değildir.
- [ ] **357.** [todo.Web/.vscode/mcp.json](C:/Users/Bahadir/Desktop/todo/todo.Web/.vscode/mcp.json) — **Yan:** Editörün görev, debug, eklenti veya MCP ayarı; normal HTTP kullanım senaryosunun parçası değildir.
- [ ] **358.** [todo.Web/.vscode/tasks.json](C:/Users/Bahadir/Desktop/todo/todo.Web/.vscode/tasks.json) — **Yan:** Editörün görev, debug, eklenti veya MCP ayarı; normal HTTP kullanım senaryosunun parçası değildir.

<a id="asama-31"></a>

## 31. Proje belgeleri ve statik kaynaklar

Kodu okuduktan sonra notlar ve review ile karşılaştır. PDF ve favicon okunacak C#/TypeScript kaynak kodu değildir; belge veya görsel varlık olarak incele.

- [ ] **359.** [docs/egitim/01-transaction-unit-of-work.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/01-transaction-unit-of-work.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **360.** [docs/egitim/02-domain-kurallari.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/02-domain-kurallari.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **361.** [docs/egitim/03-isim-esitligi.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/03-isim-esitligi.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **362.** [docs/egitim/04-specification.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/04-specification.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **363.** [docs/egitim/05-okuma-sorgulari-sayfalama.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/05-okuma-sorgulari-sayfalama.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **364.** [docs/egitim/06-decorator-pipeline.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/06-decorator-pipeline.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **365.** [docs/egitim/07-result-http-mapping.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/07-result-http-mapping.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **366.** [docs/egitim/08-options-fail-fast.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/08-options-fail-fast.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **367.** [docs/egitim/09-audit-soft-delete-karari.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/09-audit-soft-delete-karari.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **368.** [docs/egitim/10-feature-based-angular.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/10-feature-based-angular.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **369.** [docs/egitim/11-component-facade.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/11-component-facade.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **370.** [docs/egitim/12-signals-computed.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/12-signals-computed.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **371.** [docs/egitim/13-reactive-forms.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/13-reactive-forms.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **372.** [docs/egitim/14-routing-lazy-loading.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/14-routing-lazy-loading.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **373.** [docs/egitim/15-rxjs-switchmap.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/15-rxjs-switchmap.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **374.** [docs/egitim/16-ortak-hata-yonetimi.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/16-ortak-hata-yonetimi.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **375.** [docs/egitim/17-runtime-config-di-csp.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/17-runtime-config-di-csp.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **376.** [docs/egitim/hangfireTürleri.md](C:/Users/Bahadir/Desktop/todo/docs/egitim/hangfireTürleri.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.
- [ ] **377.** [docs/notlar.md](C:/Users/Bahadir/Desktop/todo/docs/notlar.md) — **Yan:** İşlemleri nedenleriyle anlatan mevcut not; okuduğun kaynak akışlarıyla karşılaştır.
- [ ] **378.** [docs/Todo-Mimari-Rehberi.pdf](C:/Users/Bahadir/Desktop/todo/docs/Todo-Mimari-Rehberi.pdf) — **Yan:** Üretilmiş rehber belgesi; kaynak kod satırı değil. İlgili gerçek dosyayla karşılaştır.
- [ ] **379.** [docs/Todo-Satir-Satir-Kod-Rehberi.pdf](C:/Users/Bahadir/Desktop/todo/docs/Todo-Satir-Satir-Kod-Rehberi.pdf) — **Yan:** Üretilmiş rehber belgesi; kaynak kod satırı değil. İlgili gerçek dosyayla karşılaştır.
- [ ] **380.** [Fullstack Development Haritasi.md](<C:/Users/Bahadir/Desktop/todo/Fullstack Development Haritasi.md>) — **Yan:** Genel fullstack haritasını gerçek proje kapsamıyla karşılaştır; örnek teknoloji her zaman projede kullanılan teknoloji değildir.
- [ ] **381.** [todo-app-code-review-v5.md](C:/Users/Bahadir/Desktop/todo/todo-app-code-review-v5.md) — **Yan:** Review geri bildirimlerini ve kodda hangi kararlara dönüştüklerini karşılaştır.
- [ ] **382.** [todo.Web/public/favicon.ico](C:/Users/Bahadir/Desktop/todo/todo.Web/public/favicon.ico) — **Yan:** Tarayıcı sekmesi simgesi; görsel varlık, çalıştırılan uygulama kodu değil.
- [ ] **383.** [todo.Web/README.md](C:/Users/Bahadir/Desktop/todo/todo.Web/README.md) — **Yan:** İlgili konu veya çalışma ortamı açıklaması; adı geçen davranışı gerçek kaynak dosyalarından kontrol et.

## Bir dosyayı bitirirken yazacağım kısa not

“Bu dosya … işini yapıyor. Onu … çağırıyor. … verisini alıyor. … nedeniyle bu sorumluluk burada. Başarıda … döndürüyor; hata durumunda … oluyor.”

Bir sınıf adı veya arayüzde takılırsam yalnız tanımını aramak yerine çağıranı, DI kaydını ve gerçek metot gövdesini birlikte bulurum. İşlemlerin gerekçeli anlatımı için [notlar.md](C:/Users/Bahadir/Desktop/todo/docs/notlar.md) ile paralel ilerleyebilirim.

