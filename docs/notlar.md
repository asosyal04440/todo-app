# Projeyi işlemler üzerinden anlatma notu

Bir işlemi anlatırken kullanıcının yaptığı hareketten başlayıp ekrandaki sonuca kadar ilerliyorum. Her durakta ne yapıldığını, neden orada yapıldığını, hangi fonksiyonun çalıştığını ve hata olursa yolun nasıl değiştiğini açıklıyorum. Teknik kavramları bu akışın içinde yerine koyuyorum.

İnşaat sırası için önce uygulamanın bütünü ve açılışı, sonra görev ekleme yolu, ardından bu yolu taşıyan mekanizmalar var. Kayıt, giriş ve reminder bölümleri aynı yapının farklı başlangıç ve sonuçlarını gösteriyor. Her bölümü bağlantıdaki gerçek kodla birlikte okuyabilirim.

## İçindekiler

1. [Bu projeyi baştan sona nasıl anlatırım?](#s1)
2. [Uygulamayı çalıştırınca, daha kullanıcı işlem yapmadan neler hazırlanıyor?](#s2)
3. [Kullanıcı “Görev ekle” dediğinde neler oluyor?](#s3)
4. [Neden aynı işlemin çevresinde validation, decorator ve pipeline var?](#s4)
5. [Interface'in içinde gövde yoksa bu iş nasıl çalışıyor?](#s5)
6. [Veriyi kim saklıyor; SaveChanges çağrısı nasıl bu kadar iş yapıyor?](#s6)
7. [Ekrandaki listeler ve görevler nasıl okunuyor; neden hepsini birden getirmiyorum?](#s7)
8. [Liste veya görev değiştirildiğinde hangi kurallar devreye giriyor?](#s8)
9. [Kullanıcı kayıt olurken veya parolayla giriş yaparken token nereden geliyor?](#s9)
10. [Google ile girişte Google'ın token'ını mı kullanıyorum?](#s10)
11. [Access token bitince neden kullanıcı her seferinde tekrar giriş yapmıyor?](#s11)
12. [“Giriş yaptım” demek her veriye erişebileceğim anlamına mı geliyor?](#s12)
13. [Kullanıcı ekranda işlem yapmazken reminder nasıl mail gönderiyor?](#s13)
14. [İşlem başarısız olunca kullanıcıya ve geliştiriciye ne ulaşıyor?](#s14)
15. [await varsa iş aynı anda mı yürüyor; cevap gelince ekran nasıl değişiyor?](#s15)
16. [Bu yapı neden böyle; başka projede aynısını mı kullanırım?](#s16)

<a id="bolum-1"></a>
<a id="bolum-2"></a>
<a id="bolum-5"></a>
<a id="s1"></a>

## 1. Bu projeyi baştan sona nasıl anlatırım?

Bu uygulamada kullanıcı giriş yapıyor, kendisine ait listeler oluşturuyor ve bu listelerdeki görevlerini yönetiyor. Bitiş zamanı yaklaşan uygun görevler için arka planda e-posta hatırlatması gönderiliyor.

Kullanıcının gördüğü bölüm Angular ile yazılmış Web uygulaması. Bu bölüm tarayıcıda çalışıyor. Kullanıcı bir işlem istediğinde Web, ASP.NET Core API'ye HTTP isteği gönderiyor. API isteği karşılıyor; Application ilgili kullanım senaryosunu yürütüyor; Domain iş kurallarını uyguluyor; Infrastructure gerekiyorsa SQL Server veya e-posta sunucusu gibi teknik sistemlerle iletişim kuruyor. Sonuç API üzerinden Web'e dönüyor ve ekran güncelleniyor.

Bu ayrımı yapmamın nedeni, bir görevin geçerli olma kuralıyla o görevin SQL Server'a nasıl kaydedildiğinin farklı sorumluluklar olması. Veritabanı değişirse görev adının boş olamayacağı kuralını yeniden yazmak istemiyorum. Ekran değişirse de aynı kuralların mobil istemciden gelen isteğe uygulanmasını istiyorum.

İnşaat gibi düşünürsem API giriş kapısı, Application işi koordine eden bölüm, Domain kullanım kuralları, Infrastructure teknik tesisat, Web ise kullanıcının gördüğü ve kullandığı bölüm. Katmanların ayrı olması, bunların ayrı sunucular olduğu anlamına gelmiyor: API'yi başlattığımda diğer backend katmanlarındaki kod da aynı uygulama sürecinde kullanılıyor.

### Hangi parça nerede çalışıyor?

| Parça | Nerede çalışıyor? | Bu projede neden var? |
|---|---|---|
| Angular 21 | Tarayıcıda | Formları, listeleri ve kullanıcı etkileşimlerini yönetmek için. |
| ASP.NET Core / .NET 8 | Backend bilgisayarında | HTTP isteklerini karşılamak ve C# uygulama kodunu çalıştırmak için. |
| SQL Server | Veritabanı hizmetinin çalıştığı bilgisayarda | Verileri kalıcı olarak saklamak ve sorguları çalıştırmak için. |
| Hangfire worker'ları | Bu projede API sürecinde | Kullanıcının bir düğmeye basmasını beklemeden zamanlanmış işleri yürütmek için. |
| SMTP sunucusu | Yapılandırılmış posta sunucusunda | Hazırlanan e-postayı teslim almak için. |

Geliştirmede tarayıcı, API ve SQL Server aynı bilgisayarda olabilir. Bu, onların aynı program olduğu anlamına gelmez. SSMS'yi kapatmak SQL Server hizmetini kapatmaz; tarayıcıyı kapatmak da çalışan API sürecini durdurmaz.

C# backend'in dili, TypeScript frontend davranışlarının dili, HTML ekranın yapısı, SCSS/CSS görünümüdür. SQL/T-SQL veritabanıyla işlem yapmak içindir. Node.js frontend geliştirme ve derleme araçlarını çalıştırır; bu projedeki C# backend'in çalışma ortamı değildir.

**Kodda baktığım yer:** [Program.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Program.cs) backend başlangıcını; [main.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/main.ts) frontend başlangıcını gösterir. Katman bağlantıları [todo.Application.csproj](C:/Users/Bahadir/Desktop/todo/todo.Application/todo.Application.csproj), [todo.Infrastructure.csproj](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/todo.Infrastructure.csproj) ve [todo.API.csproj](C:/Users/Bahadir/Desktop/todo/todo.API/todo.API.csproj) içinde görülür.

<a id="k-6-7"></a>
<a id="akis-d"></a>
<a id="akis-e"></a>
<a id="s2"></a>

## 2. Uygulamayı çalıştırınca, daha kullanıcı işlem yapmadan neler hazırlanıyor?

Binada birinin kapıdan girmesinden önce tesisatın ve görev dağılımının hazırlanması gibi, uygulamada da önce hizmetleri ve isteklerin izleyeceği yolu hazırlıyorum. Başlangıç kodu her görev eklemede tekrar çalışmıyor.

### Backend hangi sırayla hazırlanıyor?

[Program.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Program.cs) önce bir başlangıç logger'ı oluşturuyor. Bunun nedeni, uygulamanın geri kalanı henüz hazır değilken oluşabilecek başlangıç hatasını da kaydedebilmek. Ardından `WebApplication.CreateBuilder(args)` ile ayarlar ve hizmet kayıtları için başlangıç nesnesi hazırlanıyor.

Controller ve JSON desteği ekleniyor. Böylece HTTP'den gelen JSON alanları C# istek nesnelerine, dönen DTO'lar da JSON cevaplarına dönüştürülebiliyor. ProblemDetails ve exception handler kayıtları, hataların ortak bir cevap yapısıyla sunulmasını sağlıyor.

Sonra üç önemli hazırlık var:

1. `AddApplication()`, kullanım senaryolarını, validator'ları, `UseCasePipeline` ve `SessionIssuer` gibi Application nesnelerini kaydediyor.
2. `AddInfrastructure(configuration)`, veritabanı bağlantısını ve repository, Unit of Work, token üretici, e-posta gönderici gibi gerçek sınıfları bağlıyor.
3. `AddTodoAuthentication()` ile kimlik ve yetki altyapısı; etkinse `AddTodoBackgroundJobs()` ile Hangfire hazırlanıyor.

Bu kayıtların yapılması, bütün işlemlerin hemen çalıştırılması değildir. Örneğin e-posta gönderici sınıfını kaydetmek e-posta göndermek anlamına gelmiyor. DI, ilgili nesne istendiğinde hangi sınıfı sağlayacağını öğrenmiş oluyor.

`builder.Build()` sonrasında `Use...` çağrıları gelen isteklerin geçtiği middleware sırasını kuruyor. `MapControllers()` controller adreslerini bağlıyor. `app.Run()` sunucuyu çalıştırıyor; bundan sonra gelen istekler bu hazırlanmış düzeni kullanıyor.

**Kod:** [DependencyInjection.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/DependencyInjection.cs): `AddApplication()`; [DependencyInjection.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/DependencyInjection.cs): `AddInfrastructure()`; [AuthenticationSetup.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/AuthenticationSetup.cs): `AddTodoAuthentication()`; [BackgroundJobsSetup.cs](C:/Users/Bahadir/Desktop/todo/todo.API/BackgroundJobs/BackgroundJobsSetup.cs): `AddTodoBackgroundJobs()`, `UseTodoBackgroundJobs()`.

### Ayarlar neden doğrudan servislerin içine yazılmıyor?

JWT süresi, SMTP adresi veya reminder sıklığı değişince iş akışı kodunu değiştirmek istemiyorum. Bu yüzden ayar sağlayıcılarından gelen değerleri Options Pattern ile tipli nesnelere bağlıyorum.

`AddTodoOptions()` → `Bind(...)` → `ValidateOnStart()` yolunda `JwtOptions`, `SmtpOptions`, `ReminderOptions`, `SecurityOptions` gibi nesneler hazırlanıyor. `RuntimeOptionsValidator.Validate()` gerekli kontrolleri yapıyor. Örneğin token imzalamak için gereken ayar geçersizse ilk kullanıcı girişinde bozulmasını beklemek yerine başlangıçta sorunu görüyorum.

Servisler `IOptions<T>.Value` veya bundan oluşturulan Settings nesnesini alıyor. `JwtAccessTokenGenerator` örneğinde `JwtOptions.ToSettings()` sonucu kullanılıyor. Application'a gerekli `RefreshTokenSettings` ve `ReminderSettings` sağlanırken altyapıdaki ayar biçimini bütün kullanım senaryolarına taşımıyorum.

`appsettings.json` tek olası kaynak değil; ortam değişkenleri ve geliştirme sırları gibi sağlayıcılar da son ayar değerlerini etkileyebilir. Bu nedenle dosyada yazan değerle çalışan ortamdaki değerin her zaman aynı olduğunu varsaymıyorum.

**Kod:** [OptionsSetup.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Configuration/OptionsSetup.cs), [RuntimeOptionsValidator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Configuration/RuntimeOptionsValidator.cs), [JwtOptions.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Configuration/JwtOptions.cs), [appsettings.json](C:/Users/Bahadir/Desktop/todo/todo.API/appsettings.json).

### Tarayıcı tarafında başlangıç nasıl oluyor?

`main.ts` içindeki `start()`, önce `runtime-config.json` dosyasını okuyor. `parseRuntimeConfig()` API adresini kontrol ediyor; sonra `bootstrapApplication()` bu adresi `API_URL` olarak Angular'a sağlıyor. API adresini kodun her yerine dağıtmamış oluyorum.

`app.config.ts`, HTTP interceptor'larını, router'ı ve başlangıç işini kuruyor. `provideAppInitializer()` → `AuthService.restoreSession()` ile mevcut refresh cookie üzerinden oturum geri yüklenmeye çalışılıyor. İlk sonuç null ise kod bir kez daha deniyor. Başarıda kullanıcı ve access token belleğe alınıyor; başarısızlıkta oturum boş kalıyor.

Router, `authGuard`, `guestGuard` ve `adminGuard` ile uygun sayfaya yönlendiriyor. Sayfalar lazy loading ile yükleniyor; başta bütün özellik ekranlarını birden yüklemek gerekmiyor. Görev ekranı açılınca `TodoPage.ngOnInit()` → `TodoFacade.loadTodoLists()` veri okumayı başlatıyor.

Runtime ayarı okunamazsa normal başlangıca devam etmek yerine `start().catch(...)` ekranda ayar hatasını gösteriyor. Yanlış API adresiyle sessizce çalışıyor gibi görünmesini istemiyorum.

**Kod:** [main.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/main.ts), [runtime-config.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/config/runtime-config.ts), [app.config.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/app.config.ts), [app.routes.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/app.routes.ts), [auth.guards.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/auth/auth.guards.ts), [auth.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/auth/auth.service.ts).

<a id="k-3-10"></a>
<a id="k-5-2"></a>
<a id="k-5-3"></a>
<a id="k-5-4"></a>
<a id="k-5-6"></a>
<a id="k-6-8"></a>
<a id="k-6-9"></a>
<a id="k-7-1"></a>
<a id="k-7-4"></a>
<a id="uc-ornek"></a>
<a id="akis-a"></a>
<a id="s3"></a>

## 3. Kullanıcı “Görev ekle” dediğinde neler oluyor?

İşlemi önce şöyle anlatabilirim:

> Formdan gelen bilgiyi Angular API'ye gönderiyor. API kullanıcı kimliğini kontrol edip görevi ekleme kullanım senaryosunu çağırıyor. Kullanım senaryosu listeyi getiriyor, kullanıcının o listeye erişebildiğini kontrol ediyor ve Domain üzerinden görevi oluşturuyor. Değişiklikleri Unit of Work ile kaydediyor. Kaydetme başarılı olunca görev DTO'su cevap olarak dönüyor. Angular da seçili listenin görevlerini yeniden okuyup ekranı güncelliyor.

Bu anlatımı koddaki gerçek adımlara bağladığımda neden her parçanın ayrı olduğunu da açıklayabilirim.

### 3.1. Form, kullanıcının girdisini nasıl topluyor?

`TodoForm.submit()` önce formun geçerli olup olmadığına bakıyor. Başlık ve açıklamayı düzenliyor, yerel tarih bilgisini UTC değerine çeviriyor ve `submitted.emit(request)` ile bir olay çıkarıyor.

Formun görevi SQL kaydı yapmak değil; kullanıcı girdisini hazırlamak. `todo-page.html` içindeki `(submitted)="facade.createTodo($event)"` bağlantısı bu olayı `TodoFacade.createTodo()` metoduna aktarıyor.

NG-ZORRO burada buton, giriş alanı ve seçim kutusu gibi hazır bileşenleri sağlıyor. İş kuralını veya veritabanı kaydını yapmıyor. Hazır bileşen kullanmamın nedeni ekranın temel parçalarını yeniden yazmak yerine uygulamanın davranışına odaklanmak.

**Kod:** [todo-form.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-form.ts), [todo-form.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/components/todo-form.html), [todo-page.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-page.html).

### 3.2. Facade ve API servisi neden ayrı?

`TodoFacade.createTodo()` seçili listeyi biliyor, “kaydediliyor” durumunu açıyor ve sonucun ekranı nasıl etkileyeceğini yönetiyor. `TodoApiService.createTodo()` ise hangi adrese hangi HTTP isteğinin gönderileceğini biliyor.

Bu ayrım sayesinde HTTP adresi değiştiğinde formu ve bütün ekran durumlarını değiştirmek zorunda kalmıyorum. Aynı şekilde yükleniyor göstergesinin davranışı değiştiğinde HTTP servisinin görevi değişmiyor.

`TodoApiService.createTodo()`, POST `/api/todo-lists/{id}/todos` isteğini oluşturuyor. JSON, başlık ve diğer alanları ortak bir veri biçiminde taşıyor. JSON'un kendisi görevin geçerli olduğuna karar vermiyor. Bu iletişimde giden mesaj HTTP request, gelen mesaj HTTP response; yani istek ve cevap. Bunların gövdesinde taşıdığım veriyi JSON biçiminde yazıyorum.

Facade `firstValueFrom(...)` ile HTTP akışının sonucunu `await` ederek bekliyor. HTTP isteği yalnızca “işlem istendi” demek; başarıyı sunucunun cevabından öğrenecek.

**Kod:** [todo.facade.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo.facade.ts): `createTodo()`; [todo-api.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-api.service.ts): `createTodo()`.

### 3.3. İstek API kapısından nasıl geçiyor?

Web'de `authInterceptor`, uygun isteğe `Authorization: Bearer ...` başlığını ekliyor. Access token yoksa önce refresh akışı gerekebilir.

API'de kimlik doğrulama token'ın imzasını, süresini ve beklenen diğer bilgileri kontrol ediyor. `OnTokenValidated`, kullanıcı kaydının hâlâ mevcut ve kullanılabilir olduğunu repository üzerinden denetliyor. Kullanıcı silinmişse yalnızca eski token'ın imzasına bakarak devam edilmiyor.

`[Authorize]` koşulu geçildikten sonra `TodosController.Create()` rota içindeki liste kimliğini ve JSON'dan oluşan `CreateTodoRequest` alanlarını alıyor. Bunlardan `CreateTodoCommand` hazırlayıp kullanım senaryosuna gönderiyor.

Request API sınırındaki giriş verisi, Command Application'a iletilen işlem bilgisi, `TodoItemDto` ise dışarı döndürülecek sonuç biçimi. Domain'deki `TodoItem` nesnesini olduğu gibi her sınırdan taşımak yerine hangi alanı alıp verdiğimi açık tutuyorum.

**Kod:** [auth.interceptor.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/http/auth.interceptor.ts), [AuthenticationSetup.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/AuthenticationSetup.cs), [TodosController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/TodosController.cs), [CreateTodoRequest.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Requests/CreateTodoRequest.cs), [CreateTodoCommand.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/CreateTodo/CreateTodoCommand.cs).

### 3.4. Controller neden bütün işi yapmıyor?

Controller “görev ekleme” işleminin HTTP tarafını biliyor. Listeyi bulma, sahiplik kontrolü, görevi oluşturma ve kaydetme sırasını `CreateTodoUseCase` yönetiyor.

Hepsini controller'a yazsaydım kullanım senaryosunu HTTP'ye bağlamış olurdum. Başka bir giriş noktasından aynı işi çalıştırmak ve işi tek başına doğrulamak zorlaşırdı. Bu projede controller'ı gelen veriyi işleme aktaran ve sonucu HTTP'ye çeviren ince bir bölüm olarak tutuyorum.

`CreateTodoUseCase.ExecuteAsync()` doğrudan `HandleAsync()` içine atlamıyor. Önce loglama, beklenen hata yönetimi ve validation decorator'larından geçiyor. Bu ortak davranışların ayrıntısı [4. bölümde](#s4).

**Kod:** [TodosController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/TodosController.cs): `Create()`; [UseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/UseCase.cs): `ExecuteAsync()`; [CreateTodoUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/CreateTodo/CreateTodoUseCase.cs): `HandleAsync()`.

### 3.5. Listeyi neden getiriyorum; kullanıcı bilgisi nereden geliyor?

`HandleAsync()`, `ITodoListRepository.GetByIdWithTodosAsync()` çağrısıyla listeyi görevleriyle birlikte istiyor. DI bu çağrıyı `TodoListRepository` sınıfına bağlıyor. Repository EF Core üzerinden SQL Server'a sorgu gönderiyor; liste nesnesi Application'a geri dönüyor.

Liste yoksa kullanım senaryosu burada hata sonucu veriyor. Varsa `TodoListAccessPolicy.CanAccess()` mevcut kullanıcının Admin veya liste sahibi olup olmadığına bakıyor. Kullanıcı kimliğini istek gövdesindeki bir “userId” alanına güvenerek almıyorum; doğrulanmış kimliği `ICurrentUser` üzerinden okuyorum.

Listeyi mevcut görevleriyle istememin nedeni de Domain'in aynı listedeki başlık tekrarını kontrol etmesi. Bu özel yazma yolunda sadece boş bir liste nesnesi yeterli olmaz.

**Kod:** [CreateTodoUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/CreateTodo/CreateTodoUseCase.cs), [ITodoListRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Persistence/ITodoListRepository.cs), [TodoListRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoListRepository.cs), [TodoListAccessPolicy.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Authorization/TodoListAccessPolicy.cs), [HttpContextCurrentUser.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/HttpContextCurrentUser.cs).

### 3.6. Domain görevi nasıl oluşturuyor?

`TodoList.AddTodo()`, önce `new TodoItem(...)` ile görev nesnesini oluşturuyor. Görev kendi alan kurallarını kontrol ediyor. Ardından `EnsureTitleIsUnique()`, aynı listede başka aktif görevde aynı normalize başlığın olup olmadığına bakıyor. Uygunsa görev liste koleksiyonuna ekleniyor.

Burada henüz “SQL Server'a kayıt edildi” diyemem. Bellekte bir nesne oluştu ve EF'nin takip ettiği liste değişti. Kalıcı yazma sonraki adımda gerçekleşecek.

Bu kuralları Domain'de tutmamın nedeni, görev nesnesini kullanan farklı kullanım senaryolarının da geçerli görev davranışına uyması. Ekrandaki kontrol kaldırılsa bile kuralın korunmasını istiyorum.

**Kod:** [TodoList.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Entities/TodoList.cs): `AddTodo()`, `EnsureTitleIsUnique()`; [TodoItem.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Entities/TodoItem.cs): kurucu ve alan kontrolleri.

### 3.7. Hangi satır gerçekten kaydediyor?

`await _unitOfWork.SaveChangesAsync(cancellationToken)` çağrısı DI üzerinden `EfUnitOfWork.SaveChangesAsync()` metoduna, oradan `TodoDbContext.SaveChangesAsync()` metoduna gidiyor.

Context audit alanlarını ve varsa soft delete durumlarını hazırladıktan sonra EF Core'un gerçek kaydetme davranışını çağırıyor. EF SQL komutlarını üretip SQL Server'a gönderiyor. SQL Server kalıcı yazmayı gerçekleştiriyor.

Bu yüzden sonucu hazırlamayı kaydetmenin arkasına koyuyorum. Kaydetme başarısızken kullanıcıya “görev oluşturuldu” demek istemiyorum. Mekanizmanın içi [6. bölümde](#s6) açılıyor.

**Kod:** [IUnitOfWork.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Persistence/IUnitOfWork.cs), [EfUnitOfWork.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/EfUnitOfWork.cs), [TodoDbContext.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContext.cs).

### 3.8. Sonuç nasıl geri dönüyor; ekran neden yeniden sorgu yapıyor?

Kaydetme tamamlanınca use case `new TodoItemDto(newTodo)` oluşturup `Result<TodoItemDto>.Success(...)` döndürüyor. Sonuç decorator zincirinden ters yönde geçerken loglanıyor ve controller'a ulaşıyor.

Controller `ToActionResult()` üzerinden `CreatedAtAction()` ile 201 cevabı oluşturuyor. Görev bilgisi JSON olarak Web'e dönüyor. `CreatedAtAction()` yeni kaynağın adresini de bildirir; adresini verdiği GET metodunu kendisi ayrıca çalıştırmaz.

`TodoFacade.createTodo()` cevabı alınca görevin bulunacağı sayfayı hesaplayıp `loadTodos()` çağırıyor. Çünkü ekran sayfalı bir liste; yalnızca yeni nesneyi diziye eklemek sayfa sınırı ve toplam kayıt bilgisini doğru tutmaya yetmeyebilir. Yeniden okuma sunucunun güncel sayfa sonucunu getiriyor.

Gelen GET sonucu signal'lara yazılıyor ve Angular ekranı güncelliyor. Form kapanıyor, başarı bildirimi gösteriliyor. `finally` içindeki kod kaydediliyor durumunu kapatıyor. Kullanıcı bu sırada başka listeye geçtiyse eski listenin sonucu yeni ekranı değiştirmesin diye liste kimliği kontrol ediliyor.

Bir adım hata verirse normal başarı sırası devam etmiyor. Facade'in `catch` kolu `showError()` ile hatayı ele alıyor. “İstek gönderildi”, “sunucu başarılı dedi” ve “cevap alınamadı” farklı durumlar; cevap kaybolduğunda kaydın kesin yapılmadığını varsayamam.

**Kod:** [TodoItemDto.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Contracts/TodoItemDto.cs), [ResultHttpMapping.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Responses/ResultHttpMapping.cs), [todo.facade.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo.facade.ts): `createTodo()`, `loadTodos()`, `showError()`.

<a id="k-3-2"></a>
<a id="k-6-4"></a>
<a id="k-6-5"></a>
<a id="s4"></a>

## 4. Neden aynı işlemin çevresinde validation, decorator ve pipeline var?

Bir görev ekleme kodunda asıl iş listeyi bulmak, erişimi kontrol etmek, görevi oluşturmak ve kaydetmek. Loglama ve genel hata çevirme gibi davranışları her use case'in içine tekrar yazarsam aynı kod birçok yere dağılır. Birini değiştirip diğerlerini unutma ihtimalim artar.

Decorator bu ortak davranışlardan birini asıl işin etrafına ekliyor. Pipeline ise bunların hangi sırayla sarılacağını kuruyor. İkisi aynı sorunun farklı tarafları: biri davranışı ekliyor, diğeri zinciri düzenliyor.

### Sıra neden böyle?

`UseCase` kurucusu, `pipeline.Wrap(new Handler(HandleAsync), ...)` ile zinciri hazırlıyor. `Wrap()` çağrısı iş kurallarını o anda çalıştırmıyor; nesneleri birbirine bağlıyor.

Çalıştırma sırası şu:

```text
ExecuteAsync()
→ LoggingDecorator.ExecuteAsync()
→ ExpectedErrorDecorator.ExecuteAsync()
→ ValidationDecorator.ExecuteAsync()
→ Handler.ExecuteAsync()
→ ilgili kullanım senaryosunun HandleAsync()
```

Loglama en dışta olduğu için doğrulama reddini ve asıl işin sonucunu da görebiliyor. Beklenen hata yöneticisi içerideki bilinen hataları Result'a çevirebiliyor. Validation, asıl işlem başlamadan girdiyi kontrol ediyor. Girdi uygunsa Handler, kendisine verilmiş `HandleAsync` fonksiyonuna işi aktarıyor.

`ExecuteAsync` özel bir C# komutu değil; bütün bu nesnelerin aynı sözleşmeyle çağrılmasını sağlayan metot adı. Hepsinin ortak biçimde çağrılabilmesi, bir davranışın sonraki davranışı sarmasını mümkün kılıyor.

Sonuç şu yönde geri dönüyor: `HandleAsync → Handler → Validation → ExpectedError → Logging → controller/job`. Validation başarısızsa asıl iş hiç başlamıyor; dönüş validation noktasından başlıyor.

**Kod:** [UseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/UseCase.cs), [UseCasePipeline.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/UseCasePipeline.cs), [LoggingDecorator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/LoggingDecorator.cs), [ExpectedErrorDecorator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/ExpectedErrorDecorator.cs), [ValidationDecorator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/ValidationDecorator.cs).

### Ekran kontrol ediyorsa backend neden yeniden kontrol ediyor?

Frontend kontrolü kullanıcıya hızlı geri bildirim veriyor. Boş başlık yazdığında sunucuya gidip gelmeden uyarı gösterebiliyorum. Fakat API'ye Postman'dan, başka bir istemciden veya değiştirilmiş bir ekrandan da istek gönderilebilir.

FluentValidation, Application'a gelen komutun alanlarını kontrol ediyor. `CreateTodoCommandValidator` içindeki `RuleFor(...)` çağrıları kuralları tanımlıyor; `ValidationDecorator` içindeki `ValidateAsync()` bunları çalıştırıyor.

Domain'in kontrolü ise nesnenin iş açısından geçerli kalmasını sağlıyor. Örneğin liste aynı başlıklı iki aktif görevi kabul etmiyor. Veritabanındaki unique indeks de iki isteğin aynı anda ön kontrolü geçmesi gibi durumlarda son tutarlılık korumasını sağlıyor.

Bunlar aynı yerdeki gereksiz tekrar gibi görünse de farklı sınırları koruyor: kullanıcı deneyimi, işlem girdisi, iş modeli ve kalıcı veri tutarlılığı.

**Kod:** [CreateTodoCommandValidator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/CreateTodo/CreateTodoCommandValidator.cs), [SharedValidationRules.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Validation/SharedValidationRules.cs), [TodoList.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Entities/TodoList.cs), [TodoDbContext.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContext.cs): `ConfigureTodoItem()`.

### API middleware zinciriyle aynı şey mi?

API middleware'i HTTP isteğini sarıyor: CORS, kimlik doğrulama, yetkilendirme, rate limiting, HTTP loglama gibi işler burada. Application pipeline'ı kullanım senaryosunu sarıyor.

Ayrımın nedeni, bütün işlemlerin HTTP isteğinden başlamaması. Reminder use case'ini Hangfire job'ı çağırıyor. O kullanım senaryosu HTTP controller'dan geçmese de Application'ın loglama, hata ve validation düzenini kullanabiliyor.

<a id="k-6-1"></a>
<a id="k-6-2"></a>
<a id="s5"></a>

## 5. Interface'in içinde gövde yoksa bu iş nasıl çalışıyor?

`CreateTodoUseCase` bir listeye ihtiyaç duyduğunda doğrudan SQL Server kodunu çağırmıyor; `ITodoListRepository` üzerinden ihtiyaç duyduğu davranışı söylüyor.

Bu arayüz bir sözleşme: “Bana kimliği verilen listeyi görevleriyle getirebilen bir parça gerekli.” Bunun nasıl yapılacağı Infrastructure'daki `TodoListRepository` sınıfında.

`AddInfrastructure()` içindeki kayıt şu bağlantıyı kuruyor:

```csharp
services.AddScoped<ITodoListRepository, TodoListRepository>();
services.AddScoped<IUnitOfWork, EfUnitOfWork>();
services.AddSingleton<IEmailSender, MailKitEmailSender>();
```

Dolayısıyla akış `ITodoListRepository.GetByIdWithTodosAsync()` noktasında bitmiyor. DI'nin verdiği nesne `TodoListRepository` olduğu için o metodun gövdesi çalışıyor. Aynı şekilde `IEmailSender.SendAsync()`, bu projede `MailKitEmailSender.SendAsync()` metoduna ulaşıyor.

Arayüzü Application'da tutmamın nedeni Application'ın kendi ihtiyacını tanımlaması. Teknik çözümü Infrastructure sağlıyor. Böylece Application'daki iş sırası MailKit veya EF Core sınıflarına doğrudan bağlanmıyor.

Constructor, bu ihtiyaçların nesneye verildiği yer. `CreateTodoUseCase`, kurucusunda repository, mevcut kullanıcı, Unit of Work ve pipeline alıyor. Kullanım sırasında nesneleri rastgele kendisi üretmek yerine hazırlanmış ihtiyaçlarını kullanıyor.

### Neden repository ve Unit of Work aynı context'i kullanmalı?

`AddDbContext<TodoDbContext>()`, repository ve Unit of Work kayıtları bu projede scoped çalışıyor. Aynı HTTP isteğinin DI kapsamında bunlara aynı context veriliyor. Böylece repository'nin getirdiği nesnedeki değişikliği Unit of Work kaydedebiliyor.

Ayrı context'ler kullansalardı birinin takip ettiği değişiklik diğerinin kaydetme çağrısında görünmeyebilirdi. “İkisi de SaveChanges adlı bir metot kullanıyor” yeterli açıklama değil; aynı iş kapsamındaki değişikliklerin ortak takip edilmesi gerekiyor.

Bu, bütün kullanıcıların tek bir global context paylaştığı anlamına gelmez. Başka istek başka kapsam alır; background job çalıştırıldığında da kendi DI kapsamı kullanılır.

### Kodu izlerken nasıl devam ederim?

Önce use case'teki ihtiyacı görüyorum. Arayüzün ne istediğine bakıyorum. Sonra `DependencyInjection.cs` içindeki eşlemeyi bulup gerçek sınıfa geçiyorum. Son olarak o sınıfın metodunun EF, SMTP veya başka hangi teknik parçayı kullandığını izliyorum.

Web'deki `inject(TodoApiService)` de Angular'ın DI sisteminden frontend servisi ister. Bu, backend'deki C# nesnesini tarayıcıya getirmez; iki uygulamanın nesne düzenleri ayrıdır.

**Kod:** [ITodoListRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Persistence/ITodoListRepository.cs), [TodoListRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoListRepository.cs), [DependencyInjection.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/DependencyInjection.cs), [CreateTodoUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/CreateTodo/CreateTodoUseCase.cs), [IEmailSender.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Email/IEmailSender.cs), [MailKitEmailSender.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Email/MailKitEmailSender.cs), [todo-page.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-page.ts).

<a id="k-3-1"></a>
<a id="bolum-4"></a>
<a id="k-4-1"></a>
<a id="k-4-2"></a>
<a id="k-4-3"></a>
<a id="k-4-4"></a>
<a id="k-5-5"></a>
<a id="k-6-3"></a>
<a id="k-7-18"></a>
<a id="s6"></a>

## 6. Veriyi kim saklıyor; SaveChanges çağrısı nasıl bu kadar iş yapıyor?

Burada üç farklı parçayı ayırıyorum. SQL Server verileri yöneten sistem. SSMS, SQL Server'a bağlanıp tabloları incelemek için kullandığım program. Entity Framework Core (EF Core) ise backend kodunun veritabanıyla çalışmasını sağlayan ORM kütüphanesi.

Uygulamanın yolu `C# kodu → EF Core → SQL Server`; benim inceleme yolum `SSMS → SQL Server`. SSMS bu nedenle uygulamanın zorunlu bir ara durağı değil.

Bağlantıda `Server=localhost\\SQLEXPRESS;Database=TodoApiDb` yazıyor. `localhost` bilgisayarı, `SQLEXPRESS` o bilgisayardaki SQL Server örneğini, `TodoApiDb` seçilen veritabanını belirtiyor. Bir örnek birden fazla veritabanı barındırabilir. Örnek adını veritabanı adıyla karıştırmıyorum. Express bir ürün edisyonu; SQLEXPRESS ise örnek adı olduğundan yalnız bu isimle kurulu edisyonu kesinleştiremiyorum.

### EF Core neyi biliyor?

`TodoDbContext`, EF Core'un verilerle çalışma noktası. `Users`, `TodoLists`, `TodoItems`, `RefreshTokens` ve `AuditRecords` bu context üzerinden erişilen kümeler.

`OnModelCreating()` ve `Configure...` metotları nesnenin hangi tabloya gittiğini, anahtarları, alan sınırlarını, ilişkileri, indeksleri ve filtreleri tarif ediyor. `UseTodoSqlServer()` ise EF'nin SQL Server sağlayıcısını kullanmasını ayarlıyor. Böylece aynı iş modeliyle hangi veritabanı teknolojisi arasında eşleme yapılacağı belli oluyor.

Foreign key, örneğin `RefreshToken.UserId` ile token'ın hangi kullanıcıya ait olduğunu gösteriyor. Bu ilişkiyi kurmak tek başına kullanıcı ve oturum yazmalarını atomik yapmıyor; o, kaydetme ve transaction konusudur.

**Kod:** [appsettings.json](C:/Users/Bahadir/Desktop/todo/todo.API/appsettings.json), [TodoDbContextConfiguration.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContextConfiguration.cs), [TodoDbContext.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContext.cs).

### Bellekteki değişiklik ne zaman kalıcı kayda dönüşüyor?

Repository `AddAsync()` çağırınca nesneyi EF takibine ekliyor. Bir liste görevleriyle takip edilerek okunmuşsa `TodoList.AddTodo()` ile eklenen görev de EF'nin değişiklik değerlendirmesinde görülüyor.

Bu hazırlıkların ardından şu yol çalışıyor:

```text
Application: IUnitOfWork.SaveChangesAsync()
→ Infrastructure: EfUnitOfWork.SaveChangesAsync()
→ TodoDbContext.SaveChangesAsync()
→ soft delete ve audit hazırlığı
→ base.SaveChangesAsync()
→ EF Core'un SQL komutları
→ SQL Server'daki kalıcı yazma
```

Unit of Work'ü ortak kayıt noktası olarak kullanmamın nedeni, bir kullanım senaryosunun yaptığı ilişkili değişiklikleri tek yerde tamamlamak. Her repository kendi başına kaydetseydi “kullanıcı kaydedildi, refresh token kaydedilemedi” gibi parçalı bir iş tasarlamak kolaylaşırdı.

### Commit ve rollback'i neden kendi use case'imde görmüyorum?

Normal kayıt yolunda EF Core, tek SaveChanges içindeki gerekli yazmalar için veritabanı transaction'ını yönetiyor. İşlem başarılı tamamlandığında commit oluyor; yazma başarısızsa o transaction'ın veritabanı değişiklikleri geri alınıyor.

Örneğin kayıt olurken kullanıcı ve ilk refresh token hazırlanıp tek SaveChanges ile yazılıyor. Oturum cevabı bundan sonra oluşturuluyor. Bu sıralamanın amacı, tamamlanmamış kaydı başarılı oturum gibi dışarı vermemek.

Rollback'in sınırını da açıklayabilmeliyim: bütün C# nesnelerini eski hâline getirme garantisi değildir ve gönderilmiş bir e-postayı geri alamaz. SMTP ve SQL Server tek bir yerel veritabanı transaction'ının içinde değil.

Özel şema hazırlama işinde ise `TodoSchemaMigrator.MigrateAsync()` içinde açık `BeginTransactionAsync()` ve `CommitAsync()` var. Bu özel yol ile normal kullanım senaryosundaki EF yönetimini birbirine karıştırmıyorum.

Kaydetme hatasında context'in `DetachFailedAudit()` metodu, o deneme için hazırlanmış audit satırlarını takipten çıkarır; soft delete hazırlığının ilgili durumunu da yeniden denemeye uygun hâle getirir. Amaç başarısız denemenin geçmiş kayıtlarının sonraki denemede çoğalmamasıdır. Bu temizlik bütün nesne alanlarını genel olarak eski hâline döndürmek değildir. `DatabaseErrorTranslator.Translate()` veritabanı hatasını uygulamanın anlayacağı hata türüne çevirir.

**Kod:** [IUnitOfWork.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Persistence/IUnitOfWork.cs), [EfUnitOfWork.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/EfUnitOfWork.cs), [TodoDbContext.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContext.cs): `SaveChangesAsync()`; [RegisterWithPasswordUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/RegisterWithPassword/RegisterWithPasswordUseCase.cs): `HandleAsync()`; [TodoSchemaMigrator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoSchemaMigrator.cs): `MigrateAsync()`.

<a id="k-7-17"></a>
<a id="k-7-22"></a>
<a id="akis-l"></a>
<a id="akis-m"></a>
<a id="akis-p"></a>
<a id="akis-q"></a>
<a id="s7"></a>

## 7. Ekrandaki listeler ve görevler nasıl okunuyor; neden hepsini birden getirmiyorum?

Görev ekleme bir yazma işi. Listeleme ise kullanıcının göreceği belirli alanları okuma işi. Bu projede bazı okuma işlemlerini `ITodoReadQueries/TodoReadQueries` üzerinden doğrudan DTO üreten sorgularla yapıyorum.

Bunun nedeni her okumada değiştirilecek zengin Domain nesneleri oluşturmaya ve takip etmeye ihtiyaç olmaması. `AsNoTracking()`, EF'nin bu sonuçları güncelleme amacıyla takip etmemesini sağlar. Ekrana gereken alanlar `Select(...)` ile seçilir.

### İlk açılışta liste ve görevler hangi sırayla geliyor?

1. `TodoPage.ngOnInit()` → `TodoFacade.loadTodoLists()` → `TodoApiService.getTodoLists()` çağrılır.
2. GET `/api/todo-lists` isteği `TodoListsController.GetAll()` metoduna gelir. `PageQuery.ToPageRequest()` sayfa bilgilerini Application biçimine çevirir.
3. `GetTodoListsUseCase.ExecuteAsync()` pipeline'dan `HandleAsync()` metoduna ulaşır; `TodoReadQueries.GetListsAsync(currentUser.UserId, ...)` çağrılır. Liste okuması kullanıcının kendi listeleriyle sınırlıdır.
4. `ReadQueryPaging.ReadPageAsync()` kayıtları ve toplam sayıyı okur. `PagedResult<TodoListDto>` controller'a, oradan 200 JSON cevabı olarak Web'e döner.
5. `loadTodoLists()` gelen sayfayı signal'lara yazar; önceki/istenen listeyi veya ilk listeyi seçer. Sonra `loadTodos()` ile o listenin görevlerini ister. Liste yoksa görev alanını boşaltır.

Görev okumasında yol `TodoApiService.getTodos()` → `TodosController.GetForList()` → `GetTodosInListUseCase` → `TodoReadQueries.GetTodosAsync()` şeklindedir. Read query önce `GetAccessErrorAsync()` ile liste ve erişimi kontrol eder; `AccessibleTodos()` ile uygun görevleri seçer. Başka kullanıcının listesini yalnız rota kimliğini değiştirerek okuyamamak gerekir.

**Kod:** [todo-page.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-page.ts), [todo.facade.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo.facade.ts), [todo-api.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-api.service.ts), [TodoListsController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/TodoListsController.cs), [TodosController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/TodosController.cs), [TodoReadQueries.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoReadQueries.cs).

### Sayfalama yalnızca alttaki “2. sayfa” düğmesi mi?

Düğme kullanıcı tarafıdır. Asıl veri sınırlaması Infrastructure'daki sorguda yapılır. Sayfa değişince `changeListPage()` veya `changeTodoPage()` yeni sayfa numarasıyla yeniden istek oluşturur.

`ReadPageAsync()`, önce `CountAsync()` ile toplamı okur. Ardından `Skip(page.Offset).Take(page.PageSize).ToListAsync()` yalnız istenen aralığı getirir. `PageRequest` varsayılan boyutu 20, üst sınırı 100 olarak kontrol eder; istemcinin sınırsız boyut istemesini kabul etmez.

Bunun nedeni yalnız ekranı düzenli göstermek değil. Bütün kayıtları her istekte veritabanından uygulamaya, oradan tarayıcıya taşımak veri büyüdükçe gereksiz yük olur. Kayıt sırası da `OrderBy(Id)` ile belirlenir; “ikinci sayfa”nın hangi sıraya göre olduğu açık tutulur.

Cevapta yalnız kayıtlar yok: `Items`, `TotalCount`, `PageNumber`, `PageSize`, `TotalPages` birlikte taşınır. Son sayfadaki görev silinmişse facade geçersiz kalan sayfayı fark edip son geçerli sayfayı yeniden okuyabilir.

Sayma ve kayıt okuma iki ayrı sorgudur; burada bunları aynı anlık görüntüye bağlayan özel bir snapshot işlemi yazılmış değil.

**Kod:** [PageQuery.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Requests/PageQuery.cs), [PageRequest.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Contracts/PageRequest.cs), [ReadQueryPaging.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/ReadQueryPaging.cs), [PagedResult.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Contracts/PagedResult.cs).

### LINQ burada hangi işi yapıyor?

`Where` filtreyi, `OrderBy` sırayı, `Select` alınacak alanları, `Skip/Take` sayfa aralığını ifade ediyor. Bu C# sorgu ifadelerine LINQ diyorum.

EF'nin sorgusu üzerinde kullanıldığında uygun ifade SQL'e çevrilip SQL Server'da çalışır. Bellekteki bir dizi üzerinde LINQ çalıştırmak ise veritabanına gitmeden C# içinde işlem yapabilir. Dolayısıyla “LINQ yazdım, kesin SQL çalıştı” demiyorum; hangi veri kaynağı üzerinde çalıştığına bakıyorum.

`GetByIdWithTodosAsync()` gibi repository okumasında değiştirilecek Domain nesnesi getiriyorum. Read query yolunda ise ekran için DTO seçiyorum. Okuma ve yazmanın ihtiyaçları farklı olduğu için iki yolu ayırmışım.

### Tek kayıt okumaları nerede?

`TodoListsController.GetById()` → `GetTodoListByIdUseCase` → `TodoReadQueries.GetListAsync()`, tek listeyi verir. `TodosController.GetById()` → `GetTodoByIdUseCase` → `GetTodoAsync()`, tek görevi verir. Erişim bilgileri de sorguya dahil edilir.

Bu endpoint'ler API'de var; mevcut `TodoApiService` içinde bunlar için ayrı getById metodu bulunmuyor. Angular düzenleme ekranı listede zaten bulunan görev bilgisini kullanıyor. API'de endpoint bulunması, ekranda mutlaka ona özel bir düğme olduğu anlamına gelmez.

**Kod:** [GetTodoListsUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/GetTodoLists/GetTodoListsUseCase.cs), [GetTodoListByIdUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/GetTodoListById/GetTodoListByIdUseCase.cs), [GetTodosInListUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/GetTodosInList/GetTodosInListUseCase.cs), [GetTodoByIdUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/GetTodoById/GetTodoByIdUseCase.cs).

<a id="k-7-14"></a>
<a id="k-7-15"></a>
<a id="k-7-19"></a>
<a id="akis-k"></a>
<a id="akis-n"></a>
<a id="akis-o"></a>
<a id="akis-r"></a>
<a id="akis-s"></a>
<a id="akis-t"></a>
<a id="s8"></a>

## 8. Liste veya görev değiştirildiğinde hangi kurallar devreye giriyor?

Görev ekleme yolunu anladıktan sonra diğer yazma işlemlerinde aynı omurga var: Web isteği → controller → pipeline → use case → erişim/Domain → ortak kaydetme → DTO veya boş başarı cevabı.

Her işlemde farklı olan şeyi açıklamam gerekiyor. “Hepsinde SaveChanges var” demek, neyin niye değiştiğini anlatmaya yetmiyor.

### Liste oluştururken sahip kimliği neden kullanıcıdan alınmıyor?

`TodoFacade.createList()` → `TodoApiService.createTodoList()` → `TodoListsController.Create()` → `CreateTodoListUseCase.HandleAsync()` yolu çalışır.

Use case sahip kimliğini `ICurrentUser.UserId` üzerinden alır. İstekten rastgele bir sahip kimliği kabul etseydim kullanıcı başka kişinin adına liste oluşturmayı deneyebilirdi.

`ExistsWithNameForOwnerAsync()` aynı sahibin aynı normalize adla aktif listesi var mı kontrol eder. `new TodoList(name, userId)` Domain kurallarını uygular; `AddAsync()` ve `SaveChangesAsync()` kaydeder. 201 ve `TodoListDto` geri döner. Facade listeleri yeniden okuyup yeni listeyi seçer.

Liste adını değiştirirken yol `saveListName()` → `renameTodoList()` → `TodoListsController.Rename()` → `RenameTodoListUseCase` olur. Önce liste ve erişim kontrol edilir; isim aramasında değiştirilen listenin kendisi hariç tutulur. `TodoList.Rename()` sonrası kaydedilir; 200 ile dönen DTO seçili listeye ve diziye yazılır.

**Kod:** [CreateTodoListUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/CreateTodoList/CreateTodoListUseCase.cs), [RenameTodoListUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/RenameTodoList/RenameTodoListUseCase.cs), [TodoListRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoListRepository.cs), [TodoList.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Entities/TodoList.cs).

### Görevi düzenlemek neden yalnız alanlara değer atamak değil?

`TodoFacade.saveTodo()` → `TodoApiService.updateTodo()` → `TodosController.Update()` → `UpdateTodoUseCase` çalışır. Liste görevleriyle getirilir, erişim kontrol edilir ve ilgili görev bulunur.

`EnsureAChangedDueDateIsNotInThePast()` değiştirilmiş bitiş tarihini kontrol eder. `TodoList.RenameTodo()` başlığın aynı listede çakışmamasını sağlar. `ChangeDescription()`, `SetDueAtUtc()` ve `ChangePriority()` diğer alanları uygular.

Bitiş tarihi değiştiğinde `SetDueAtUtc()` eski reminder gönderim bilgisini ve deneme sayaçlarını sıfırlar. Bunun nedeni eski tarihe göre gönderilmiş hatırlatmanın yeni tarih için aynı kararı temsil etmemesi.

Kaydetme sonrası 200 ile güncel DTO gelir. `replaceTodo()` mevcut dizideki kaydı değiştirir. Eklemede bütün sayfayı yeniden okumama rağmen burada tek kaydı değiştirebilmemin nedeni, mevcut sayfadaki mevcut kaydı güncelliyor olmamdır.

**Kod:** [UpdateTodoUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/UpdateTodo/UpdateTodoUseCase.cs), [TodoItem.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Entities/TodoItem.cs), [todo.facade.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo.facade.ts).

### Tamamlandı işareti hangi ayrı yolu kullanıyor?

`setTodoCompletion()` → `setTodoCompletionStatus()` → PATCH `.../completion` → `TodosController.SetCompletionStatus()` → `SetTodoCompletionStatusUseCase`.

Controller `IsCompleted` alanının gönderildiğini kontrol eder. Use case listeyi, erişimi ve görevi bulur; Domain'de `MarkCompleted()` veya `MarkIncomplete()` çağrılır. Kaydetme sonrası dönen DTO yine `replaceTodo()` ile ekrana yazılır.

Ayrı bir işlem olmasının nedeni bütün görevi yeniden göndermek yerine bu belirli niyeti ifade etmek. Tamamlanmış görev reminder seçimindeki “tamamlanmamış olmalı” şartını geçemez.

**Kod:** [TodosController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/TodosController.cs), [SetTodoCompletionStatusUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/SetTodoCompletionStatus/SetTodoCompletionStatusUseCase.cs).

### Sil dediğimde neden satır hâlâ veritabanında duruyor?

Tek görev için yol `TodoFacade.deleteTodo()` → `TodoApiService.deleteTodo()` → `TodosController.Delete()` → `DeleteTodoUseCase` olur. Domain'de `TodoList.RemoveTodo()` koleksiyondan çıkarır; EF ilişki takibi bunu silme durumuna alır.

Liste için `deleteList()` → `deleteTodoList()` → `TodoListsController.Delete()` → `DeleteTodoListUseCase` çalışır. Burada `TodoListRepository.Delete()` listeyi silinecek olarak işaretler.

Gerçek kaydetmede `ExpandSoftDeletionAsync()` bağlı alt kayıtları hazırlar. `PrepareAudit()`, normal silme durumunu `IsDeleted = true`, `DeletedAtUtc` ve `DeletedBy` güncellemesine çevirir. Satırın kalmasının nedeni soft delete kullanmam: geçmişi inceleyebilmek ve silme bilgisini tutmak istiyorum.

`OnModelCreating()` içindeki query filter'lar silinmiş kayıtları normal okumalardan çıkarır. Liste silindiğinde alt görevler de soft delete edilir. Başarıda 204 döner; facade ilgili sayfayı yeniden okur.

Kullanıcı için de altyapıda silme yayılımı var; fakat mevcut Admin ekranında kullanıcı silme endpoint'i yok. Ayrıca refresh token'larda soft delete alanları yerine `Revoke()` kullanılıyor. Bütün tablolara aynı silme davranışı uygulanmış değil.

**Kod:** [DeleteTodoUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/DeleteTodo/DeleteTodoUseCase.cs), [DeleteTodoListUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/TodoLists/DeleteTodoList/DeleteTodoListUseCase.cs), [TodoDbContext.SoftDeletion.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContext.SoftDeletion.cs), [TodoDbContext.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContext.cs).

### Kimin değiştirdiğini nerede tutuyorum?

Domain'deki `AuditableEntity` oluşturma, değiştirme ve silme alanlarını taşıyor. Bu alanları kaydetme noktasında `TodoDbContext.PrepareAudit()` dolduruyor. Böylece her kullanım senaryosunda aynı zaman/kişi atamalarını tekrar yazmıyorum.

Yapan kişi `HttpContextCurrentUser.UserIdOrNull` üzerinden doğrulanmış kimlikten gelir; istemcinin “bu işlemi şu kişi yaptı” alanına güvenmiyorum. Kullanıcı isteği olmayan reminder job'ında bu bilgi boş kalabilir.

Güncelleme ve silmede `CreateAuditRecord()` ile ayrı `AuditRecords` satırı hazırlanır. Bu kayıt ile iş verisi aynı SaveChanges içinde yazılır. Oluşturmada `CreatedAtUtc/CreatedBy` alanları doldurulur; mevcut kod ayrıca “Created” türünde bir geçmiş satırı üretmiyor. Geçmiş satırları da eski/yeni bütün alanların tam kopyası değildir; işlem metadatası taşır.

**Kod:** [AuditableEntity.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Common/AuditableEntity.cs), [AuditRecord.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Auditing/AuditRecord.cs), [HttpContextCurrentUser.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/HttpContextCurrentUser.cs), [TodoDbContext.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContext.cs): `PrepareAudit()`, `CreateAuditRecord()`.

### İki kişi aynı anda değiştirirse ne oluyor?

Ön kontrol tek başına yeterli değil: iki istek aynı anda aynı başlığın boşta olduğunu görebilir. Aktif kayıtlardaki unique indeks son aşamada çakışmayı engeller. `DatabaseErrorTranslator.Translate()` bunu uygulama çakışmasına; API de 409 cevabına çevirir.

Başka isteğin kaydı silmesi gibi durumlarda `IsDeleted` concurrency token'ı eski durumla yazmayı algılayabilir. `ConcurrencyConflictException`, pipeline üzerinden hata Result'ına çevrilir. `TodoFacade.showError()` bu özel problem türünde uyarı verip `loadTodoLists()` ile ekranı yeniler.

Buradaki sınırı bilmeliyim: her alan değişikliğini yakalayan genel bir rowversion mekanizması yok. “İki eşzamanlı güncellemenin her türlüsünü çözdüm” diyemem.

**Kod:** [TodoDbContext.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContext.cs): `ConfigureAudit()`; [DatabaseErrorTranslator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/DatabaseErrorTranslator.cs), [ExpectedErrorDecorator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/ExpectedErrorDecorator.cs), [ResultHttpMapping.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Responses/ResultHttpMapping.cs), [todo.facade.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo.facade.ts).

<a id="k-3-8"></a>
<a id="k-7-5"></a>
<a id="k-7-9"></a>
<a id="akis-b"></a>
<a id="akis-f"></a>
<a id="s9"></a>

## 9. Kullanıcı kayıt olurken veya parolayla giriş yaparken token nereden geliyor?

Kayıt olma yeni kullanıcı oluşturur. Giriş yapma mevcut kullanıcının kimliğini doğrular. İkisinin sonunda ortak oturum hazırlama sırası kullanılır; bu yüzden token üretme mantığını iki use case'in içine kopyalamıyorum.

### Kayıt olma akışında neyi birlikte kaydediyorum?

`LoginPage.submit()` kayıt modundaysa `AuthService.register()` isteğini çalıştırır. `AuthController.Register()` HTTP verisinden `RegisterWithPasswordCommand` oluşturur. `RegisterWithPasswordUseCase` pipeline'dan geçip `HandleAsync()` içine girer.

E-posta `EmailNormalizer.Normalize()` ile düzenlenir; `UserRepository.GetByEmailAsync()` aynı aktif hesabı arar. Hesap varsa genel hata düzeniyle kayıt reddedilir. Yoksa `AspNetCorePasswordHasher.HashPassword()` parola hash'i üretir. `User.RegisterWithPassword()` yeni kullanıcıyı oluşturur; repository onu takibe ekler.

Parolayı düz metin saklamamamın nedeni, kimlik kontrolünde parolanın kendisini geri okumaya ihtiyaç olmaması. Girişte verilen parola kayıtlı hash ile doğrulanabilir. Buradaki PasswordHasher, Microsoft Identity altyapısının teknik uygulamasıdır; Application yalnız `IPasswordHasher` sözleşmesini kullanır.

Ardından `SessionIssuer.PrepareAsync(newUser)` ilk refresh token'ı hazırlar. Kullanıcı ve refresh token tek `SaveChangesAsync()` ile kaydedilir. Sonra `CreateSession()` cevap oluşturur. Böylece kullanıcı kaydı tamamlanmamışken başarılı giriş cevabı üretmiyorum.

Controller, refresh cookie'yi yazıp 201 ve `AuthResultDto` döndürür. `AuthService` içindeki `tap()` → `startSession()` kullanıcıyı ve access token'ı belleğe alır. `LoginPage.signIn()` ana sayfaya yönlendirir.

**Kod:** [login-page.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/auth/login-page.ts), [auth.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/auth/auth.service.ts), [AuthController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/AuthController.cs), [RegisterWithPasswordUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/RegisterWithPassword/RegisterWithPasswordUseCase.cs), [AspNetCorePasswordHasher.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/AspNetCorePasswordHasher.cs), [User.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Entities/User.cs).

### Girişte yeni kullanıcı kaydı mı oluşturuluyor?

Normal parola girişinde mevcut kullanıcı aranıyor. Yol `AuthService.login()` → `AuthController.Login()` → `LoginWithPasswordUseCase.ExecuteAsync()` → pipeline → `HandleAsync()`.

Normalize e-posta ile kullanıcı getirilir. `AspNetCorePasswordHasher.VerifyPassword()` doğrulama sonucunu döndürür. Başarısızsa oturum oluşturulmaz. Hesap bulunmadığında veya parola girişi olmayan hesapta kod bir hash işlemi yapıp aynı genel kimlik hatasını döndürür; farklı hata mesajlarıyla gereksiz hesap bilgisi açığa çıkarmamayı ve süre farkını azaltmayı amaçlar.

Hash'in yenilenmesi gerekiyorsa `HashPassword()` ve `User.ReplacePasswordHash()` ile değişiklik hazırlanır. Sonra yine `PrepareAsync → SaveChangesAsync → CreateSession` sırası yürür. Controller 200 döndürür; Web oturumunu kurar.

**Kod:** [LoginWithPasswordUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/LoginWithPassword/LoginWithPasswordUseCase.cs), [AspNetCorePasswordHasher.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/AspNetCorePasswordHasher.cs), [SessionIssuer.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/Common/SessionIssuer.cs).

### PrepareAsync neden access token'ı hemen vermiyor?

`PrepareAsync()`, `SecureTokenGenerator.GenerateToken()` ile güvenli rastgele refresh değeri üretir; `ComputeHash()` SHA-256 hash'ini çıkarır. `new RefreshToken(user, hash, ...)` ve `RefreshTokenRepository.AddAsync()` ile kayda hazırlık yapar.

Veritabanında ham refresh token yerine hash bulunur. Ham değer, kullanıcıya cookie olarak verilecek oturum hazırlığında tutulur. Bu aşamada dönen `PendingAuthSession`, hazırlanmış fakat henüz tamamlanmamış oturumu temsil eder.

Kaydetme bittikten sonra `CreateSession()`, `IAccessTokenGenerator.GenerateToken(user)` çağırır. DI'nin seçtiği `JwtAccessTokenGenerator`, Microsoft.IdentityModel kütüphanesindeki `JsonWebTokenHandler.CreateToken()` ile imzalı access token üretir. Kullanıcı kimliği, e-posta, ad ve rol gibi gerekli bilgiler token'a konur.

`AuthSession`, API'nin ihtiyaç duyduğu refresh bilgisini ve dışarı dönecek `AuthResultDto` nesnesini birleştirir. Controller ham refresh'i cookie'ye, access token'ı JSON cevabına koyar. UserDto'ya parola hash'i koymamamın nedeni, veritabanındaki bütün alanların istemciye gönderilmesinin gerekmemesi.

**Kod:** [SessionIssuer.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/Common/SessionIssuer.cs): `PrepareAsync()`, `CreateSession()`; [SecureTokenGenerator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/SecureTokenGenerator.cs): `GenerateToken()`, `ComputeHash()`; [RefreshTokenRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/RefreshTokenRepository.cs): `AddAsync()`; [JwtAccessTokenGenerator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/JwtAccessTokenGenerator.cs): `GenerateToken()`.

<a id="k-3-7"></a>
<a id="k-7-7"></a>
<a id="akis-g"></a>
<a id="s10"></a>

## 10. Google ile girişte Google'ın token'ını mı kullanıyorum?

Google, kullanıcının Google kimliğini doğrulamak için ID token veriyor. Bu, uygulamamın kendi kullanıcı kaydı ve yetkileri yerine geçmiyor. Google kimliği doğrulandıktan sonra Todo uygulamamın kendi oturumunu oluşturuyorum.

### Ekrandaki Google düğmesinden backend'e ne geliyor?

`LoginPage.setUpGoogleSignIn()` → `AuthService.getAuthConfig()` → `AuthController.GetConfig()` ile ClientId okunur. Buradaki küçük ayar okumasının ayrı Application use case'i yok.

`renderGoogleButton()`, `GoogleIdentityService.renderButton()` çağırır. `loadScript()` Google istemcisini yükler; kullanıcı giriş yaptığında callback içindeki `response.credential` gelir.

Bu ID token `AuthService.loginWithGoogle()` ile POST `/api/auth/google` adresine gönderilir. `AuthController.LoginWithGoogle()`, `LoginWithGoogleCommand` oluşturur ve ilgili kullanım senaryosuna iletir.

**Kod:** [login-page.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/auth/login-page.ts), [google-identity.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/auth/google-identity.service.ts), [auth.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/auth/auth.service.ts), [AuthController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/AuthController.cs).

### Kullanıcı bilgisine neden doğrudan güvenmiyorum?

İstemciden “benim e-postam şu” diye gelen bir metin kimlik kanıtı değildir. `GoogleIdTokenValidator.ValidateAsync()`, Google.Apis.Auth kütüphanesinin `GoogleJsonWebSignature.ValidateAsync()` çağrısını kullanır. Yapılandırılmış ClientId doğrulamada beklenen audience olarak kullanılır. Use case ayrıca e-postanın doğrulanmış olmasını ister.

Doğrulanan bilgiler `GoogleUserInfo` olarak Application'a döner. Kullanıcı önce Google subject kimliğiyle, gerekirse normalize e-postayla aranır.

E-postadan bulunan mevcut hesap varsa `User.LinkGoogleAccount()` bağlantı kurar. Parola hesabına bağlama durumunda `RevokeExistingSessionsAsync()` eski refresh oturumlarını iptale hazırlar. Hesap yoksa `User.RegisterWithGoogle()` ile yeni kullanıcı hazırlanır.

Rol politikası bu Google giriş akışında `GetRoleForEmail()` ile değerlendirilir; gerekiyorsa `User.PromoteToAdmin()` uygulanır. Bu atama kararını Google token'ındaki herhangi bir istemci alanından almak yerine uygulamanın kendi politikasına bırakıyorum.

Sonunda ortak `PrepareAsync → SaveChangesAsync → CreateSession` sırası gelir. API refresh cookie'yi ve kendi access token'ını döndürür. `startSession()` Web oturumunu kurar.

**Kod:** [LoginWithGoogleUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/LoginWithGoogle/LoginWithGoogleUseCase.cs), [GoogleIdTokenValidator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/GoogleIdTokenValidator.cs), [ConfiguredRoleAssignmentPolicy.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/ConfiguredRoleAssignmentPolicy.cs), [User.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Entities/User.cs).

**Bu akışın kritik ayrımı:** Google'dan gelen ID token backend'de kimliği doğrulamak için kullanılır; sonraki görev isteklerinde gönderilen Bearer token uygulamamın kendi ürettiği access token'dır.

<a id="k-7-6"></a>
<a id="k-7-8"></a>
<a id="akis-h"></a>
<a id="akis-i"></a>
<a id="akis-j"></a>
<a id="s11"></a>

## 11. Access token bitince neden kullanıcı her seferinde tekrar giriş yapmıyor?

Access token'ın ömrü kısa tutulabilir. Süresi bittiğinde uzun süre kullanılabilir bir oturumu yalnız o token'a bağlamak yerine refresh token üzerinden yeni bir access token alıyorum. Bu iki token'ın görevi farklı olduğu için saklama ve doğrulama yolları da farklı.

Access token Web'deki `AuthService` signal'ında, yani bellekte tutuluyor. Refresh token'ı API `RefreshTokenCookie.Write()` ile tarayıcıya yazıyor. HttpOnly olduğu için Angular ham refresh değerini okuyup işlemiyor; ilgili istekte tarayıcı cookie'yi taşıyor.

Cookie'de `Secure`, `SameSite=Strict` ve `/api/auth` yolu tanımlı. `authInterceptor`, auth isteklerine `withCredentials` ve `X-Todo-Client` başlığını ekliyor. Bu ayarların amacı refresh bilgisini sıradan görev JSON'una veya JavaScript'in yönettiği bir alana dağıtmamak.

### Yenileme isteği hangi sırayla çalışıyor?

1. `AuthService.refreshSession()`, başlangıçta `restoreSession()` veya korunan istek sırasında `authInterceptor` tarafından çağrılabilir.
2. POST `/api/auth/refresh` isteğinde `AuthController.Refresh()`, `RefreshTokenCookie.Read()` ile ham değeri okur.
3. `RefreshSessionUseCase` pipeline'dan geçer; `HandleAsync()` boş token'ı reddeder.
4. `ComputeHash()` ile hash hesaplanır. `RefreshTokenRepository.GetByTokenHashAsync()` kaydı getirir; `IsRevoked`, `IsActive(now)` ve aktif kullanıcı kontrolleri yapılır.
5. `SessionIssuer.PrepareAsync(user, ..., storedToken)` yeni refresh değeri oluşturur ve eski kayıtta `Revoke(now, newHash)` çağırır.
6. Tek SaveChanges eski iptali ve yeni kaydı birlikte yazar. `CreateSession()` yeni access token üretir.
7. API yeni refresh cookie'yi ve 200 AuthResultDto'yu döndürür; `startSession()` Web belleğini günceller. Bekleyen korunan istek yeni access token ile gönderilebilir.

Eski refresh token'ı değiştirmemin nedeni aynı değerin sürekli kullanılmasını önlemek ve tekrar kullanımını fark edebilmek. Yeni döndürülmüş token'ın eski değeri kısa sürede tekrar gelirse 30 saniyelik eşzamanlılık toleransı toplu oturum iptalini önleyebilir; eski token'ı geçerli yapmaz. Bu toleransın dışında eski iptal edilmiş token'ın yeniden kullanımı, `RevokeAllSessionsAsync()` ile aktif refresh token'ların iptaline götürür.

Web'deki `refreshInFlight` ve RxJS `shareReplay(1)`, aynı anda gelen yenileme taleplerinin tek yenileme akışını paylaşmasına yardımcı olur. Böylece birkaç API isteği token bittiğini aynı anda fark ettiğinde gereksiz ayrı rotasyonlar azaltılır.

**Kod:** [auth.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/auth/auth.service.ts), [auth.interceptor.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/http/auth.interceptor.ts), [RefreshTokenCookie.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/RefreshTokenCookie.cs), [RefreshSessionUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/RefreshSession/RefreshSessionUseCase.cs), [SessionIssuer.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/Common/SessionIssuer.cs), [RefreshTokenRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/RefreshTokenRepository.cs), [RefreshToken.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Entities/RefreshToken.cs).

### Çıkış yapmak yalnız ekrandan kullanıcıyı silmek mi?

`AuthService.logout()` → `AuthController.Logout()` → `LogoutUseCase` yolu refresh token'ı hash ile bulur. Token mevcut ve iptal edilmemişse `Revoke()` çağrılıp kaydedilir. Token yoksa veya zaten iptalse çıkış yine başarılı kabul edilir; tekrar çıkış isteğinin hata üretmesi gerekmez.

API cookie'yi silip 204 döndürür. Web'de `endSession()` belleği boşaltır ve girişe yönlendirir. Bu ayrım önemlidir: arayüz oturumu ile sunucudaki refresh kaydı iki ayrı yerde duruyor.

Web, HTTP hatası olsa da yerel oturumu kapatıyor. Bu durumda sunucudaki iptalin kesin tamamlandığını söyleyemem. Ayrıca önceden verilmiş access token bu çıkışla otomatik olarak anında topluca iptal edilmiyor; kendi geçerlilik süresi ve API doğrulamaları ayrı.

**Kod:** [LogoutUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/Logout/LogoutUseCase.cs), [AuthController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/AuthController.cs), [RefreshTokenCookie.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/RefreshTokenCookie.cs), [auth.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/auth/auth.service.ts).

### Güncel kullanıcıyı nereden okuyabiliyorum?

GET `/api/auth/me` → `AuthController.GetCurrentUser()` → `GetCurrentUserUseCase` → `UserRepository.GetByIdAsync(currentUser.UserId)` yoluyla UserDto döner. Bu bir okuma işlemidir.

Mevcut Angular AuthService ayrıca bir getMe çağrısı yapmıyor; normal giriş/refresh cevabı zaten kullanıcı bilgisini taşıyor. API'nin bu endpoint'i sunması, Web'in her durumda onu çağırdığı anlamına gelmiyor.

**Kod:** [GetCurrentUserUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Authentication/GetCurrentUser/GetCurrentUserUseCase.cs), [AuthController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/AuthController.cs).

<a id="bolum-7"></a>
<a id="k-7-2"></a>
<a id="k-7-10"></a>
<a id="k-7-11"></a>
<a id="k-7-12"></a>
<a id="k-7-13"></a>
<a id="akis-u"></a>
<a id="s12"></a>

## 12. “Giriş yaptım” demek her veriye erişebileceğim anlamına mı geliyor?

Kimlik doğrulama “kimsin?” sorusunu, yetkilendirme ise “bu işlemi yapabilir misin?” sorusunu karşılıyor. Geçerli token sahibi olmak başka kullanıcının listesini değiştirmeye tek başına yetmez.

`AddTodoAuthentication()` içindeki `AddJwtBearer()` kaydı JwtBearer doğrulayıcısını kurar. `UseAuthentication()`, bu JWT doğrulamasıyla `HttpContext.User` kimliğini hazırlar. `HttpContextCurrentUser` bu kimlikten `UserId` ve `IsAdmin` bilgisini Application'ın `ICurrentUser` sözleşmesiyle sunar. Application HTTP sınıfını doğrudan okumadan mevcut kullanıcıyı kullanabilir.

Kaynak erişiminde `TodoListAccessPolicy.CanAccess()`, Admin veya sahip olma koşuluna bakar; Domain'deki `TodoList.IsOwnedBy()` sahiplik karşılaştırmasını yapar. Böylece gelen liste kimliğinin gerçekten erişilebilir olması kontrol edilir.

### Admin ekranına girince ne farklı çalışıyor?

Web'deki `adminGuard()` uygun yönlendirmeyi sağlar. Ancak kullanıcı Web kodunu atlayabildiği için asıl yetki kararı backend'de de bulunur.

GET `/api/admin/users` isteği `[Authorize(Policy = AdminOnly)]` kontrolünden geçer. Uygunsa `AdminUsersController.GetAll()` → `GetUsersUseCase` → `UserReadQueries.GetUsersAsync()` → `ReadPageAsync()` çalışır. Kullanıcı DTO'ları 200 ile döner.

`AdminUsersPage.ngOnInit()` veya `changePage()` → `loadUsers()` → `AdminApiService.getUsers()` okumasını başlatır. Cevapta signal'lar güncellenir. Normal kullanıcının geçerli kimliği olsa bile bu endpoint 403 verir. Ekran kullanıcı listesini okur; buradan kullanıcı silme veya rol değiştirme endpoint'i olduğu sonucunu çıkarmıyorum.

**Kod:** [AuthenticationSetup.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/AuthenticationSetup.cs), [HttpContextCurrentUser.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/HttpContextCurrentUser.cs), [TodoListAccessPolicy.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Authorization/TodoListAccessPolicy.cs), [AdminUsersController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/AdminUsersController.cs), [GetUsersUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Users/GetUsers/GetUsersUseCase.cs), [UserReadQueries.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/UserReadQueries.cs), [admin-users-page.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/admin/admin-users-page.ts), [admin-api.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/admin/admin-api.service.ts).

### HTTPS, CORS ve rate limiting niye ayrıca var?

Bunlar aynı güvenlik sorusunu çözmüyor.

**HTTPS**, HTTP verisinin taşındığı bağlantıyı TLS ile korur. Kullanıcının hangi listeye erişebileceğine karar vermez. `Program.cs` Production ortamında `UseHsts()` ve `UseHttpsRedirection()` kuruyor; geliştirme adresinin de otomatik aynı şekilde çalıştığını varsaymıyorum.

**CORS**, başka origin'de çalışan frontend'in API cevabını tarayıcıda okuyabilmesine ilişkin politikadır. `AddCors()`, `WithOrigins()` ve `UseCors("TodoWeb")` ile izin verilen adresler tanımlanır. Bu, Postman dahil bütün istemcileri engelleyen bir kimlik sistemi değildir.

**Rate limiting**, giriş/kayıt/Google giriş uç noktalarına aynı istemci adresinden belirli sürede aşırı istek gelmesini sınırlar. `AddTodoAuthentication()` içinde politika hazırlanır, `UseRateLimiter()` ile uygulanır. Limit aşılırsa use case'e gitmeden 429 döner. Bunun nedeni pahalı parola doğrulama ve giriş denemelerine kontrolsüz yük binmesini azaltmak.

Auth yazma isteklerindeki `RequireClientHeaderAttribute.OnActionExecuting()`, `X-Todo-Client` başlığını ister. Başlığın varlığı kullanıcı kimliği kanıtı değildir; token/cookie doğrulaması ve yetki kontrolleri ayrıca gerekir.

JWT de bu görevlerin yerine geçmez: imza token'ın doğrulanmasına hizmet eder; payload'ı gizli bir veritabanı kasası yapmaz. Kullanıcı rolü taşımakla bağlantıyı şifrelemek farklı işlerdir.

**Kod:** [Program.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Program.cs), [AuthenticationSetup.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/AuthenticationSetup.cs), [RequireClientHeaderAttribute.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Authentication/RequireClientHeaderAttribute.cs), [auth.interceptor.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/http/auth.interceptor.ts), [JwtAccessTokenGenerator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Authentication/JwtAccessTokenGenerator.cs).

<a id="k-3-4"></a>
<a id="k-3-5"></a>
<a id="k-3-6"></a>
<a id="k-6-6"></a>
<a id="k-7-3"></a>
<a id="k-7-23"></a>
<a id="akis-c"></a>
<a id="s13"></a>

## 13. Kullanıcı ekranda işlem yapmazken reminder nasıl mail gönderiyor?

Reminder, bir HTTP isteğinin arkasında bekleyen iş değil. Hangfire zamanı geldiğinde işi başlatıyor. Kullanım senaryosu veritabanında uygun görevleri arayıp sahiplerine e-posta gönderiyor. Tarayıcı kapalı olabilir; bu projede worker API sürecinde olduğu için API'nin çalışması gerekir.

### Önce zamanlayıcı neyi kuruyor?

`AddTodoBackgroundJobs()`, Hangfire'ın SQL Server depolamasını ve worker'larını kuruyor. `UseTodoBackgroundJobs()`, reminder ve SMTP yapılandırması uygunsa `AddOrUpdate<TodoReminderJob>()` ile tekrar eden işi kaydediyor.

Dosyadaki schedule `*/5 * * * *`: beş dakikada bir tarama planlanıyor. Bu, her göreve ayrı job oluşturmak değil. Aynı tarama işi o anda uygun görevleri topluca arıyor.

Zamanı gelince Hangfire → `TodoReminderJob.RunAsync()` → `SendDueTodoRemindersUseCase.ExecuteAsync()` → Application pipeline → `HandleAsync()` yolu çalışır. Job burada zamanlayıcıyla iş mantığı arasındaki bağlantıdır. Hangi göreve mail gideceğinin kuralları Hangfire ayarının içine yazılmamış.

**Kod:** [BackgroundJobsSetup.cs](C:/Users/Bahadir/Desktop/todo/todo.API/BackgroundJobs/BackgroundJobsSetup.cs), [TodoReminderJob.cs](C:/Users/Bahadir/Desktop/todo/todo.API/BackgroundJobs/TodoReminderJob.cs), [SendDueTodoRemindersUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Reminders/SendDueTodoReminders/SendDueTodoRemindersUseCase.cs).

### “Uygun görev” ne demek; neden Specification var?

Tarama anındaki UTC zaman ve ayarlarla `TodoReminderSpecification` hazırlanıyor. Görev silinmemiş, tamamlanmamış, daha önce hatırlatma gönderilmemiş ve zamanı yaklaşmış olmalı. Yeniden deneme kuralları da uygun olmalı.

Ayar dosyasında yaklaşma penceresi 24 saat, batch boyutu 50, görev deneme üst sınırı 3 ve tekrar aralığı 15 dakika. **Beş dakika tarama sıklığı; 24 saat uygunluk penceresi.** İkisi aynı süreyi anlatmıyor. Diğer ayar sağlayıcıları çalışan ortamdaki değerleri değiştirebilir.

Bu kuralları sorguya ve bellek kontrolüne ayrı ayrı yazsaydım birini değiştirip diğerini unutabilirdim. Specification'ın amacı aynı kural tanımını iki kullanımda da değerlendirmek.

`ToExpression()`, EF'nin sorguya çevirebileceği `Expression<Func<TodoItem, bool>>` verir. Expression, kuralın incelenebilir yapısıdır; EF bunu SQL'e çevirebilir. `IsSatisfiedBy(todo)` ise aynı temel kurallardan derlenmiş fonksiyonlarla bellekte true/false hesaplar. Bu ikinci kontrol yeni bir veritabanı okuması değildir.

Domain kuralı tarif eder; SQL'e çevirme işini EF Core yapar. Bu sayede Domain, SQL Server sorgu metni bilmek zorunda kalmaz.

**Kod:** [TodoReminderSpecification.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Specifications/TodoReminderSpecification.cs): `ToExpression()`, `IsSatisfiedBy()`, `NeedsReminder()`, `CanRetryReminder()`.

### Mail adresini nereden buluyor?

`TodoReminderRepository.GetTodosDueForReminderAsync()`, `Where(specification.ToExpression())` ile uygun görevleri filtreler. Sonra görev → liste → sahip kullanıcı ilişkisini birleştirir:

```text
TodoItem.TodoListId
→ TodoList.Id
→ TodoList.OwnerUserId
→ User.Id
→ User.Email
```

`OrderBy`, `Take(batchSize)` ve `ToListAsync()` ile adaylar okunur. Sonuç `TodoReminderCandidate` nesneleridir: görev, liste adı ve alıcı kullanıcı bilgisi birlikte gelir.

Candidate “aday” demektir. Bu isim, mailin gönderildiğini değil gönderim için değerlendirilecek veri hazırlandığını anlatır. Use case her adayda `IsSatisfiedBy()` kontrolünü yapar; uygun değilse atlar.

**Kod:** [TodoReminderRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoReminderRepository.cs), [ITodoReminderRepository.cs içindeki TodoReminderCandidate](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Persistence/ITodoReminderRepository.cs).

### İçeriği kim hazırlıyor; gerçekten hangi satır gönderiyor?

`TodoReminderEmailComposer.Compose()` görev ve alıcı bilgisinden `EmailMessage` üretir. Tarihi gösterim saat dilimine çevirir; konu satırındaki satır sonlarını kaldırır; HTML'e giren dinamik metinleri encode eder. İçerik oluşturma ile gönderme ayrı sorumluluklar olduğu için burada SMTP bağlantısı açılmaz.

`IEmailSender.SendAsync(message)` DI üzerinden `MailKitEmailSender.SendAsync()` gövdesine ulaşır. MimeKit ile `MimeMessage` ve `BodyBuilder.ToMessageBody()` hazırlanır. MailKit bağlantı işini gerçekleştirir:

```text
ConnectAsync()
→ gerekiyorsa AuthenticateAsync()
→ SendAsync(mimeMessage, cancellationToken)
→ DisconnectAsync()
```

MimeKit mesajın yapısını, MailKit SMTP ile iletişimi sağlar. SMTP sunucusunun mesajı kabul etmesi, kullanıcının maili okuduğunun kanıtı değildir.

**Kod:** [TodoReminderEmailComposer.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Reminders/SendDueTodoReminders/TodoReminderEmailComposer.cs), [IEmailSender.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Abstractions/Email/IEmailSender.cs), [MailKitEmailSender.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Email/MailKitEmailSender.cs).

### Başarı neden ayrıca veritabanına yazılıyor?

Gönderim tamamlanınca `TodoItem.MarkReminderSent(now)` çağrılır ve `IUnitOfWork.SaveChangesAsync()` ile kaydedilir. Sonraki tarama “daha önce gönderilmedi” koşulunda bu görevi elemelidir; yalnız bellekte işaretlemek API yeniden başladığında yeterli olmaz.

Use case sonunda `Result<ReminderRunResult>` job'a döner. `RunAsync()` aday, başarı ve hata sayılarını loglar; Hangfire çalışma durumunu saklar. Sonuç Angular'a HTTP cevabı değildir: e-posta ve kalıcı reminder bilgisi işin çıktılarıdır.

Geliştirme ortamında ve ilgili kurulum etkinse `/hangfire` paneli bu işlerin durumunu gösterir. Panelin yerel istek yetki filtresi vardır. `Succeeded` görmek mutlaka mail gitti demek değil; hiç aday bulunmadan veya yönetilmiş mesaj hatalarıyla da job tamamlanabilir.

### Gönderim hata verirse hangi yeniden deneme çalışıyor?

Tek mesajın reddi gibi hata durumunda use case `RecordFailedReminderAttempt()` ile sayacı ve son deneme zamanını günceller, kaydeder ve diğer adaya geçer. Sonraki tarama Specification'ın deneme sayısı ve bekleme aralığı koşullarına göre yeniden aday seçebilir.

SMTP hizmetine erişilemiyorsa use case taramayı erken bitirip `EmailServiceUnavailable` bilgisini döndürür. Job bunu exception'a çevirir. Bu dalda görev deneme sayacı artırılmıyor; sorun belirli mesajın kendisinden çok hizmete erişim olarak ele alınıyor.

Job üzerinde `AutomaticRetry(Attempts = 3)` ve `DisableConcurrentExecution` bulunur. Bunlar Hangfire seviyesidir. EF'nin geçici SQL hataları için yeniden denemesi de ayrı mekanizmadır. Görev sayacı, Hangfire retry'si ve SQL bağlantı retry'si aynı sayaç değildir.

Mail gönderildikten sonra SaveChanges başarısız olabilir. SQL transaction gönderilmiş maili geri alamaz. Bu nedenle mevcut tasarım için “her koşulda tam bir kez mail gönderilir” garantisi veremem; arada hata olursa tekrar gönderim ihtimali vardır.

**Kod:** [SendDueTodoRemindersUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Reminders/SendDueTodoReminders/SendDueTodoRemindersUseCase.cs) hata kolları; [TodoReminderJob.cs](C:/Users/Bahadir/Desktop/todo/todo.API/BackgroundJobs/TodoReminderJob.cs) retry ayarları; [TodoDbContextConfiguration.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoDbContextConfiguration.cs) SQL retry ayarı; [TodoItem.cs](C:/Users/Bahadir/Desktop/todo/todo.Domain/Entities/TodoItem.cs): `MarkReminderSent()`, `RecordFailedReminderAttempt()`.

<a id="k-3-3"></a>
<a id="k-6-10"></a>
<a id="akis-v"></a>
<a id="s14"></a>

## 14. İşlem başarısız olunca kullanıcıya ve geliştiriciye ne ulaşıyor?

Kullanıcıya işlemin neden yapılamadığını anlaşılır biçimde bildirmek istiyorum. Geliştirici olarak da hatanın hangi istekte, hangi işlemde ve ne kadar sürede oluştuğunu izlemek istiyorum. Kullanıcı cevabı ve log bu yüzden farklı çıktılar.

### Beklenen hata ile beklenmeyen hata neden ayrılıyor?

Liste bulunamadığında veya erişim reddedildiğinde use case doğrudan `Result.Failure` döndürebilir. Domain kuralı gibi bilinen exception'ları `ExpectedErrorDecorator` uygun hata Result'ına dönüştürür. Başarı yolunda `Result.Success(value)` taşınır.

Result, Application'ın HTTP durum kodunu bilmeden sonucu ifade etmesini sağlar. Aynı use case'i job çağırdığında HTTP koduna ihtiyacı yoktur. Controller ise `ResultHttpMapping.ToActionResult()` ile sonucu HTTP cevabına çevirir.

Beklenmeyen exception'ların hepsi bu decorator'da tüketilmez. API'deki handler'lar kalan hatalara bakar. `DatabaseExceptionHandler.TryHandleAsync()`, uygun veritabanı hatasını 503'e çevirebilir. `UnhandledExceptionHandler.TryHandleAsync()`, beklenmeyen hata için 500 ve referans bilgisi üretir.

Hata cevabındaki ProblemDetails ortak biçim sağlar. `traceId`, kullanıcıdan gelen hata referansıyla ilgili logları ilişkilendirmeye yardımcı olur. İç teknik exception'ı her ayrıntısıyla kullanıcının önüne koymak yerine destek için izlenebilir bir cevap üretilir.

**Kod:** [Result.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Results/Result.cs), [AppError.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Results/AppError.cs), [ExpectedErrorDecorator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/ExpectedErrorDecorator.cs), [ResultHttpMapping.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Models/Responses/ResultHttpMapping.cs), [DatabaseExceptionHandler.cs](C:/Users/Bahadir/Desktop/todo/todo.API/ExceptionHandling/DatabaseExceptionHandler.cs), [UnhandledExceptionHandler.cs](C:/Users/Bahadir/Desktop/todo/todo.API/ExceptionHandling/UnhandledExceptionHandler.cs), [ProblemDetailsWriter.cs](C:/Users/Bahadir/Desktop/todo/todo.API/ExceptionHandling/ProblemDetailsWriter.cs).

### Durum kodunu bu projeden nasıl açıklarım?

| Sonuç | API'nin anlattığı | Projedeki örnek |
|---|---|---|
| 200 | İşlem başarılı; sonuç içeriği var. | Liste okuma, giriş veya güncelleme. |
| 201 | Yeni kaynak oluşturuldu. | Görev, liste veya kullanıcı kaydı. |
| 204 | Başarılı; cevap gövdesi yok. | Silme ve çıkış. |
| 400 | Girdi uygun değil. | Validation veya gerekli başlık/alan eksikliği. |
| 401 | Geçerli kimlik doğrulaması yok. | Geçersiz kimlik bilgisi veya token. |
| 403 | Bu işlem için yetki yok. | Normal kullanıcının Admin endpoint'ine erişmesi. |
| 404 | Kaynak bulunamadı veya varlığı açıklanmıyor. | Bulunamayan ya da erişilemeyen liste/görev. |
| 409 | Mevcut veri durumuyla çakışma var. | İsim tekrarı veya algılanan concurrency uyuşmazlığı. |
| 429 | İstek sınırı aşıldı. | Çok fazla giriş denemesi. |
| 500 | Beklenmeyen sunucu hatası. | Genel exception handler yolu. |
| 503 | Hizmet işlemi şu anda gerçekleştiremiyor. | Uygun veritabanı erişim hataları. |

Bazı kaynak erişim reddini 404'e çevirmemin nedeni, başka kullanıcıya ait kaydın varlığını da açıklamamak. Admin rolü reddinin 403 olmasıyla bunu aynı mekanizma gibi anlatmıyorum.

### Hata Web'de neden bir kez daha işleniyor?

`authInterceptor` korunan 401 durumunda refresh deneyebilir. Kalan HTTP hatası dıştaki `apiErrorInterceptor` tarafından `ApiErrorService.normalize()` ile ortak hata nesnesine dönüştürülür. Ağ hatası, alan hatası ve sunucu hatasının ekranın her yerinde farklı biçimlerde ele alınmasını azaltıyorum.

Bazı ağ/429/5xx durumları `report()` ile NG-ZORRO bildirimi gösterir. Facade veya sayfanın `catch` kolu kendi yerel hatasını/form durumunu yönetir. `applyFieldErrors()` sunucudan gelen alan hatalarını form kontrollerine bağlar.

Bu ikinci katman sunucudaki iş kuralını değiştirmez; gelen hata bilgisini kullanıcı arayüzüne uygun hâle getirir. Örneğin aynı 409 durumu görev başlığında alan uyarısı olarak, concurrency durumunda “liste yenilendi” mesajı olarak gösterilebilir.

**Kod:** [api-error.interceptor.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/http/api-error.interceptor.ts), [api-error.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/errors/api-error.service.ts), [todo.facade.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo.facade.ts): `showError()`; [login-page.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/auth/login-page.ts): `signIn()`.

### Serilog'u burada neden kullanıyorum?

`LoggingDecorator.ExecuteAsync()`, `ILogger` üzerinden işlem başlangıcını, sonucunu, süresini ve iptali kaydeder. Application log kaydı ister; API'de kurulan Serilog bu kayıtları yapılandırılan konsol/dosya gibi hedeflere iletir.

`UseSerilogRequestLogging()` ayrıca HTTP yöntemi, yol, durum kodu, süre ve kullanıcı bilgisiyle isteği kaydeder. HTTP isteği ile kullanım senaryosu farklı seviyeler olduğu için ikisini ayrı izleyebilirim.

Serilog hata sonucu üretip işlemi kurtarmaz. Log benim sonradan ne olduğunu anlayabilmem içindir; kullanıcıya dönecek Result/ProblemDetails ayrı mekanizmadır. Başlangıç hatalarının `Log.Fatal(...)` ile yazılması da API henüz normal istek alacak durumda değilken sorunu görebilmek içindir.

**Kod:** [Program.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Program.cs), [LoggingDecorator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/LoggingDecorator.cs), [TodoReminderJob.cs](C:/Users/Bahadir/Desktop/todo/todo.API/BackgroundJobs/TodoReminderJob.cs).

<a id="k-3-9"></a>
<a id="k-7-20"></a>
<a id="k-7-21"></a>
<a id="k-7-24"></a>
<a id="s15"></a>

## 15. await varsa iş aynı anda mı yürüyor; cevap gelince ekran nasıl değişiyor?

Asenkronluk ile paralelliği aynı şey gibi anlatmıyorum. Bir I/O işlemi beklenirken o bekleyiş için thread'i gereksiz yere bloke etmemek mümkün; fakat bu, metottaki bütün sonraki satırların hemen çalışması demek değil.

Görev ekleme use case'inde sıra hâlâ şöyle:

```text
await listeyi getir
→ erişimi ve Domain kurallarını kontrol et
→ await değişiklikleri kaydet
→ sonucu oluştur
```

Liste gelmeden onun sahibini kontrol edemem; kaydetme tamamlanmadan başarılı kayıt cevabı üretmem. `await` bu bağımlılıkları korur. I/O bekleyişi sırasında uygulamanın diğer işler için çalışabilmesine olanak sağlar. Beklenen işlem zaten tamamlanmışsa devam etmek için ayrıca bekleme oluşması da gerekmez.

Backend'deki `Task<Result<TodoItemDto>>`, işlemin tamamlandığında vereceği sonucu temsil eder. Task bir thread değildir; adı Async olan her metot da kendiliğinden yeni thread açmaz. `Task` dönen SaveChanges tamamlandığında ayrıca DTO vermez; üstteki kullanım senaryosu kendi sonucunu hazırlar.

**Kod:** [CreateTodoUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Todos/CreateTodo/CreateTodoUseCase.cs): `HandleAsync()`; [TodoListRepository.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoListRepository.cs): `GetByIdWithTodosAsync()`; [EfUnitOfWork.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/EfUnitOfWork.cs): `SaveChangesAsync()`.

### Frontend neden Observable, Promise ve signal kullanıyor?

`TodoApiService.getTodos()` gibi HTTP metotları RxJS Observable döndürür. Observable, sonucun/hatanın bir akış olarak gelmesini sağlar. `firstValueFrom()` bu akışın ilk değerini Promise'e dönüştürür; facade bazı işlerde bunu `await` ile sıralı biçimde kullanır.

Liste seçimi hızlı değişebildiği için görev okumasında farklı bir ihtiyaç var. `loadTodos()` komutu `todoLoads.next(...)` ile verir; facade kurucusundaki `switchMap` yeni seçim geldiğinde önceki okuma aboneliğini bırakır. Eski listenin yavaş cevabının yeni seçimi ezmesini önlemeye yardımcı olur.

`tap()` gelen sayfanın görev ve sayfa bilgilerini signal'lara yazar. Signal ekranın dayandığı mevcut değeri tutar; Angular template bu değer değişince görünümü günceller. `catchError()` hata durumunu, `finalize()` tamamlanma/temizlik işini yönetir. `takeUntilDestroyed()` bileşen kapsamı bittiğinde aboneliğin sürüp kalmasını önlemeye yardım eder.

Dolayısıyla RxJS iletişim akışını, signal ekranda kullanılacak durumu yönetiyor. NG-ZORRO'nun butonu veya mesajı ise bu durumu ve etkileşimi kullanıcıya sunuyor.

**Kod:** [todo.facade.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo.facade.ts): `constructor()`, `loadTodos()`, `fetchTodoPage()`, `replaceTodo()`; [todo-page.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-page.html).

### İptal isteği gelirse ne oluyor?

ASP.NET Core'un controller'a verdiği `CancellationToken`, kullanım senaryosuna, repository'ye ve EF çağrılarına aktarılır. Amaç, istemcinin artık beklemediği işi destekleyen alt adımlara bildirmek.

`ValidationDecorator` ve reminder döngüsünde `ThrowIfCancellationRequested()` gibi kontroller var. İptal normal doğrulama hatasına çevrilip yutulmamalı; üst akış iptali anlayabilmeli.

İptal sinyali her şeyi anında durdurma veya bitmiş commit'i geri alma garantisi değildir. Cevap kaybolduğunda veya istemci vazgeçtiğinde sunucuda hiçbir şeyin gerçekleşmediği sonucuna otomatik gitmiyorum.

Background job da kendi token'ıyla iptal edilebilir. Hangfire işin ne zaman başlayacağını, async/await ise içindeki I/O bekleyişinin nasıl yönetileceğini ilgilendirir; birbirlerinin yerine geçmezler.

**Kod:** [TodosController.cs](C:/Users/Bahadir/Desktop/todo/todo.API/Controllers/TodosController.cs), [ValidationDecorator.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Common/Execution/ValidationDecorator.cs), [SendDueTodoRemindersUseCase.cs](C:/Users/Bahadir/Desktop/todo/todo.Application/Features/Reminders/SendDueTodoReminders/SendDueTodoRemindersUseCase.cs), [MailKitEmailSender.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Services/Email/MailKitEmailSender.cs).

<a id="bolum-3"></a>
<a id="k-4-5"></a>
<a id="k-5-1"></a>
<a id="bolum-6"></a>
<a id="k-7-16"></a>
<a id="akis-w"></a>
<a id="s16"></a>

## 16. Bu yapı neden böyle; başka projede aynısını mı kullanırım?

Bu projede katmanlı ve Clean Architecture yaklaşımına dayalı bir düzen kullanıyorum. Asıl hedef belirli sayıda klasör oluşturmak değil; iş kurallarının ve kullanım senaryolarının ekran/veritabanı ayrıntıları tarafından belirlenmesini azaltmak.

Kod referanslarında Application → Domain; Infrastructure → Application ve Domain yönü var. API başlangıçta Application ve Infrastructure'ı bağlayabiliyor; çünkü uygulamanın hangi gerçek sınıflarla çalışacağını orada kuruyorum.

Çalışma anında ise Application arayüz üzerinden Infrastructure'dan hizmet alıyor. “Kod bağımlılığı hangi yöne?” sorusu ile “çalışırken kim kimi çağırıyor?” sorusunu ayırmam gerekiyor. Application'ın arayüz çağrısıyla SQL'e ulaşması, Application projesinin EF sınıfına doğrudan bağımlı olduğu anlamına gelmiyor.

### Aynı kalıbı her yerde kullanmanın sınırı ne?

EF Core zaten değişiklik takibi ve kaydetme davranışı sağlar. `EfUnitOfWork` bu projede ince bir uyarlayıcıdır; bütün transaction mekanizmasını o birkaç satırda yeniden yazmadım. Application'ın ortak kaydetme sözleşmesini EF'den ayırdım.

Repository ve read query ayrımı da her projede zorunlu bir kural değil. Burada değiştireceğim Domain nesnesini getirme ihtiyacıyla ekrana sayfalı DTO okuma ihtiyacını ayırmışım. Bu seçim kod ve arayüz sayısını artırıyor; karşılığında sorumlulukları ve erişim yollarını açıklaştırıyor.

Decorator zinciri ortak davranışların tekrarını azaltıyor ama çağrıyı takip ederken birden fazla sınıftan geçmeyi gerektiriyor. Bu yüzden yalnız “Decorator kullandım” demem yetmez; hangi davranışın hangi sırada çalıştığını ve hatanın nereye döndüğünü açıklamalıyım.

Bunları kurumsal kod yapan şey isimleri değil. Hangi değişikliğin nerede yapılacağını, hangi sınırın neyi koruduğunu ve tasarımın hangi garantiyi vermediğini açıklayabilmem gerekiyor.

### Yarın bir ihtiyaç değişirse nereyi incelerim?

| Değişen ihtiyaç | İlk baktığım yer | Neden? |
|---|---|---|
| Görev adıyla ilgili iş kuralı | Domain ve bunu kullanan validator/ekran sınırları | Kuralın kaynağını ve kullanıcıya erken geri bildirimi birlikte değerlendirmek için. |
| Veritabanı teknolojisi | Infrastructure, EF sağlayıcısı ve migration düzeni | SQL'e erişim ayrıntısı orada; bu yine de sıfır maliyetli bir değişim değildir. |
| API adresi | Web runtime config | HTTP adresi kaynak kodun her yerine dağılmasın diye. |
| SMTP adresi veya token süresi | İlgili Options/Settings ve ayar sağlayıcıları | İş akışını değiştirmeden çalışma ayarını değiştirmek için. |
| E-posta içerik metni | TodoReminderEmailComposer | İçerik hazırlama SMTP gönderiminden ayrıldığı için. |
| Hatırlatma uygunluk kuralı | TodoReminderSpecification | Sorgu ve bellek değerlendirmesi aynı kural kaynağını kullansın diye. |
| Görsel bileşen veya form yerleşimi | Web component ve template | Ekran değişimi Domain'in işini değiştirmesin diye. |

### Kod değişince tablo yapısı nasıl güncelleniyor?

Migration, normal görev kaydıyla aynı işlem değil. Sütun, tablo ve indeks gibi veritabanı yapısını sürümlü adımlarla değiştirmek için kullanılıyor.

`HerSeyiBaslat.bat`, `tools/Start-Todo.ps1` aracını çağırıyor. Veritabanı hazırlığında `TodoDatabaseMigrator` çalıştırılıyor. Bu aracın `Program.cs` kodu ayarları okuyup context oluşturuyor ve `TodoSchemaMigrator.MigrateAsync()` çağırıyor. EF'nin `IMigrator.MigrateAsync()` yolu bekleyen migration'ları uyguluyor.

Normalize adlar için özel hazırlık gerekirse eski veriler kontrol edilip anahtarlar dolduruluyor; ilgili grup açık transaction içinde tamamlanıyor. Çakışma varsa sessizce kayıt silmek yerine hata veriliyor. Amaç yeni unique kuralını eski veriye de tutarlı biçimde uygulamak.

Bu akışın ayrı araçta olması, her görev ekleme HTTP isteğinde şema hazırlığı yapılmadığını gösteriyor. Normal istek hazır şemada veriyi değiştiriyor; migration ise şemanın kendisini değiştiriyor.

Bazı otomatik kontroller SQLite üzerinde çalışıyor. Bu, normal uygulamanın SQL Server yerine SQLite kullandığı anlamına gelmez. Aynı ORM'yi kullanmak veritabanlarının her davranışını eşit yapmaz; SQL Server'a özel collation, indeks ve transaction davranışlarını o sistemle değerlendirmek gerekir.

**Kod:** [HerSeyiBaslat.bat](C:/Users/Bahadir/Desktop/todo/HerSeyiBaslat.bat), [Start-Todo.ps1](C:/Users/Bahadir/Desktop/todo/tools/Start-Todo.ps1), [Program.cs](C:/Users/Bahadir/Desktop/todo/tools/TodoDatabaseMigrator/Program.cs), [TodoSchemaMigrator.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/TodoSchemaMigrator.cs), [20261015091041_AddSoftDeletion.cs](C:/Users/Bahadir/Desktop/todo/todo.Infrastructure/Persistence/Migrations/20261015091041_AddSoftDeletion.cs), [UnitOfWorkTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/UnitOfWorkTests.cs), [SqlServerNameRulesTests.cs](C:/Users/Bahadir/Desktop/todo/todo.Tests/todo.Infrastructure.Tests/SqlServerNameRulesTests.cs).

