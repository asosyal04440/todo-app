# Code Review Bulgulari


## 1. Okuma Rehberi

Her bulgu aynı şablonla yazıldı:

- **Konum:** İlgili dosya ve satırlar
- **Sorun:** Kodda ne gördüğümüz
- **Neden önemli:** Enterprise bir projede doğuracağı sonuç
- **Çözüm yönü:** Hazır çözüm değil, ne yapılması gerektiğine dair yön.

| Seviye | Anlamı |
|---|---|
| 🔴 **Kritik** | Hedef mimariye aykırı ya da veri tutarlılığını veya canlıya çıkışı doğrudan etkileyen konu. Önce bunlar ele alınmalı. |
| 🟠 **Orta** | Çalışıyor ama büyüdükçe bakım, performans ya da tutarlılık sorunu çıkaracak konu. |

---

## 2. Korunması Gereken Güçlü Yönler

Bunları refactor sırasında **bozmamaya** dikkat et:

- **Oturum güvenliği:** Access token yalnızca bellekte tutuluyor. Refresh token `HttpOnly + Secure + SameSite=Strict` cookie'de ve dar bir path'te. Rotation ve reuse detection var ([RefreshSessionUseCase.cs:85-98](../todo-app/todo.Application/Features/Authentication/RefreshSession/RefreshSessionUseCase.cs#L85-L98)).
- **Kimlik doğrulama sertleştirmesi:** Login'de timing saldırısına karşı sahte hash hesaplanıyor ([LoginWithPasswordUseCase.cs:66-71](../todo-app/todo.Application/Features/Authentication/LoginWithPassword/LoginWithPasswordUseCase.cs#L66-L71)). Auth endpoint'lerinde rate limiting, security header middleware ve CSRF'e karşı custom header kontrolü var.
- **Yetkisiz erişimde 404 dönülmesi:** Başkasına ait kaynağın var olup olmadığı dışarı sızdırılmıyor.
- **Zengin domain modeli:** Entity'lerde `private set` kullanılmış. Doğrulama constructor ve metotlarda yapılıyor, `TodoItem` yalnızca `TodoList` üzerinden oluşturuluyor (`internal` constructor).
- **Test edilebilir zaman:** Application katmanında `TimeProvider` kullanılıyor.
- **Hata sözleşmesi:** `IExceptionHandler` zinciri ve `ProblemDetails` + `traceId` ile tutarlı hata cevapları dönülüyor.
- **Arka plan işi:** Hangfire işinde `DisableConcurrentExecution`, retry ve deneme sayacı var.
- **Testler:** Domain, Application, Infrastructure ve API katmanları ayrı test projelerinde. Frontend'de guard, interceptor ve servis testleri ile Playwright E2E var.

---

## 3. Backend Bulguları (Clean Architecture)

### 🔴 Kritik

#### BE-K1 — Transaction sınırı (Unit of Work) yok; her repository tüm `DbContext`'i commit ediyor

- **Konum:**
  - [TodoListRepository.cs:102-105](../todo-app/todo.Infrastructure/Persistence/TodoListRepository.cs#L102-L105), [UserRepository.cs:79-82](../todo-app/todo.Infrastructure/Persistence/UserRepository.cs#L79-L82), [RefreshTokenRepository.cs:66-69](../todo-app/todo.Infrastructure/Persistence/RefreshTokenRepository.cs#L66-L69)
  - [LoginWithPasswordUseCase.cs:87-89](../todo-app/todo.Application/Features/Authentication/LoginWithPassword/LoginWithPasswordUseCase.cs#L87-L89) + [SessionIssuer.cs:85-86](../todo-app/todo.Application/Features/Authentication/Common/SessionIssuer.cs#L85-L86)
  - [LoginWithGoogleUseCase.cs:115-143](../todo-app/todo.Application/Features/Authentication/LoginWithGoogle/LoginWithGoogleUseCase.cs#L115-L143) ve [LoginWithGoogleUseCase.cs:148-161](../todo-app/todo.Application/Features/Authentication/LoginWithGoogle/LoginWithGoogleUseCase.cs#L148-L161)
- **Sorun:** Her repository arayüzünde `SaveChangesAsync` var ve hepsi aynı scoped `DbContext`'i commit ediyor. Tek bir iş akışı birden fazla commit'e bölünmüş:
  - Login'de önce kullanıcı kaydediliyor (rehash), ardından `SessionIssuer` refresh token için ikinci bir commit yapıyor.
  - Google login'de `RevokeExistingSessionsAsync` revoke ettiği token'ları kendisi kaydetmiyor. Bu değişiklikler, `_userRepository.SaveChangesAsync` aynı context'i paylaştığı için *tesadüfen* kaydediliyor.
- **Neden önemli:**
  - İkinci commit başarısız olursa iş yarım kalır.
  - `IUserRepository.SaveChangesAsync`'in refresh token'ları da kaydettiği koddan anlaşılmıyor. Bu, gizli bir bağımlılık (hidden coupling). Biri repository'leri ayırırsa ya da test double'ları farklı davranırsa revocation sessizce kaybolur.
  - Enterprise projelerde "bir use case = bir transaction" kuralı genelde değişmez.
- **Çözüm yönü:**
  - Commit sorumluluğunu repository'lerden al ve Application katmanındaki tek bir soyutlamaya (Unit of Work) ver. Repository'ler yalnızca kayıt eklesin ya da değiştirsin; commit tek noktadan, Unit of Work üzerindeki `SaveChangesAsync` ile yapılsın.
  - Use case'in sonunda tek commit olsun. `SessionIssuer` gibi yardımcılar commit etmesin, yalnızca değişikliği hazırlasın.
  - Retry stratejisi açık olduğu için (`EnableRetryOnFailure`), birden fazla `SaveChanges` gerektiren bir akış olursa bunu execution strategy içinde bir transaction'a sarmak gerekir. Bunun için şunlara bakılabilir: `Database.CreateExecutionStrategy()`, `ExecuteAsync`, `BeginTransactionAsync`.

---

### 🟠 Orta

#### BE-O1 — Cross-cutting concern'ler her use case'te elle tekrarlanıyor; uygulama katmanında log yok

- **Konum:** Bütün `*UseCase.cs` dosyaları. Örnek: [CreateTodoUseCase.cs:49](../todo-app/todo.Application/Features/Todos/CreateTodo/CreateTodoUseCase.cs#L49), [UpdateTodoUseCase.cs:56](../todo-app/todo.Application/Features/Todos/UpdateTodo/UpdateTodoUseCase.cs#L56), [DependencyInjection.cs:41-60](../todo-app/todo.Application/DependencyInjection.cs#L41-L60)
- **Sorun:**
  - Her use case `_validator.ValidateOrThrowAsync(...)` çağrısını kendisi yapıyor. Validator'ı olmayan use case'lerde de doğrulama ya hiç yok ya da başka yerde yapılıyor.
  - Use case'ler DI'a tek tek elle kaydediliyor.
  - Application katmanında hiç `ILogger` yok. Bir işlemin neden başarısız olduğu yalnızca exception handler'dan izlenebiliyor.
- **Neden önemli:** Yeni bir use case eklerken validation, logging, transaction ve yetki kontrolünü hatırlamak geliştiriciye kalıyor. Biri unutulduğunda bunu fark edecek bir mekanizma yok. 20 use case'te yönetilebilir, 200 use case'te yönetilemez.
- **Çözüm yönü:**
  - Use case'leri ortak bir sözleşme (ör. `IUseCase<TRequest, TResponse>` ya da mediator handler'ı) arkasına al.
  - Validation, logging, (BE-K1 sonrasında) transaction ve performans ölçümünü bu sözleşmenin etrafında **decorator / pipeline** olarak bir kez yaz. Yaygın bir yöntem MediatR `IPipelineBehavior` kullanmak (ör. bir `ValidationBehavior`); böylece validation tek bir yerde ve her istekte otomatik çalışır. MediatR kullanmak zorunlu değil. Önemli olan, cross-cutting işlerin use case'in içinde değil etrafında olması.
  - Log ve performans ölçümü de handler'ların içinde değil bu pipeline'da olsun. Her handler'a ayrı stopwatch ve log kodu yazmak hızla tekrara dönüşür.
  - Kayıt işini assembly taramasıyla otomatikleştir.

#### BE-O2 — Sonuç sözleşmesi belirsiz: `null` / `bool` ile "bulunamadı", "yetkisiz" ve "başarısız" ayırt edilemiyor

- **Konum:** [GetTodoListByIdUseCase.cs:40-50](../todo-app/todo.Application/Features/TodoLists/GetTodoListById/GetTodoListByIdUseCase.cs#L40-L50), [DeleteTodoUseCase.cs:43-60](../todo-app/todo.Application/Features/Todos/DeleteTodo/DeleteTodoUseCase.cs#L43-L60), [TodosController.cs:78-81](../todo-app/todo.API/Controllers/TodosController.cs#L78-L81)
- **Sorun:** Use case'ler `TodoItemDto?` ya da `bool` dönüyor. Controller `null` gördüğünde `404` dönüyor. "Liste yok", "liste başkasına ait" ve "todo yok" durumları aynı değerle ifade ediliyor. Diğer hatalar exception ile akıyor. Sonuçta iki farklı hata kanalı var.
- **Neden önemli:** Dışarıya hepsi için `404` dönmek doğru bir güvenlik kararı ve korunmalı. Ama bu ayrımın içeride de kaybolması log, audit ve test tarafında körlük yaratıyor. Yeni bir durum eklendiğinde (ör. "arşivlenmiş liste") ifade edecek bir yer yok.
- **Çözüm yönü:**
  - Use case'lerin dönüşünü "başarı + değer" veya "tipli hata" (NotFound, Forbidden, Conflict, Validation…) taşıyan bir sonuç tipine çevir. Bunun için yaygın desen `Result<T>`: başarı, hata ve validation hatası tek bir tipte taşınır. Hata türünü string mesaj yerine tipli bir alan olarak taşımayı düşün; böylece HTTP eşlemesi mesaj metnine bağlı kalmaz.
  - HTTP koduna eşleme tek bir yerde yapılsın (controller base veya extension).
  - Hangi durumların dışarıya `404` olarak maskeleneceği o eşleme noktasında bilinçli bir karar olarak yazılsın.
  - Exception'ları gerçekten beklenmeyen durumlara bırak.

#### BE-O3 — Okuma tarafı ölçeklenmiyor: sayfalama yok, her okumada aggregate'in tamamı yükleniyor

- **Konum:**
  - [GetTodoListByIdUseCase.cs:36-38](../todo-app/todo.Application/Features/TodoLists/GetTodoListById/GetTodoListByIdUseCase.cs#L36-L38): Yalnızca liste adı dönülüyor ama tüm todo'lar `Include` ediliyor ([TodoListRepository.cs:43-56](../todo-app/todo.Infrastructure/Persistence/TodoListRepository.cs#L43-L56)).
  - [GetTodosInListUseCase.cs](../todo-app/todo.Application/Features/Todos/GetTodosInList/GetTodosInListUseCase.cs), [GetUsersUseCase.cs](../todo-app/todo.Application/Features/Users/GetUsers/GetUsersUseCase.cs) + [UserRepository.cs:55-67](../todo-app/todo.Infrastructure/Persistence/UserRepository.cs#L55-L67): Sınırsız liste dönüyor.
  - [GetTodoByIdUseCase.cs:37-62](../todo-app/todo.Application/Features/Todos/GetTodoById/GetTodoByIdUseCase.cs#L37-L62): Tek bir todo için listenin bütün todo'ları belleğe alınıyor.
- **Sorun:** Yazma modeli (aggregate) okuma için de kullanılıyor. Hiçbir listeleme endpoint'inde sayfalama, filtreleme ya da sıralama yok. Okumalar change tracking açık şekilde yapılıyor.
- **Neden önemli:** Admin kullanıcı listesi ve bir listedeki todo sayısı büyüdüğünde bellek, ağ ve DB yükü doğrusal artar. Enterprise projelerde sayfalanmamış liste endpoint'i genelde code review'dan geçmez.
- **Çözüm yönü:**
  - Okuma ve yazma yollarını ayır. Yazmada aggregate'i yükle. Okumada doğrudan DTO'ya **projeksiyon** yapan, `AsNoTracking` sorgular kullan (hafif bir CQRS ayrımı).
  - Liste dönen her endpoint'e sayfalama sözleşmesi ekle (sayfa no / boyut / toplam) ve sayfa boyutuna üst sınır koy (ör. validator'da 1-100). Bir seçenek, filtre/sıralama/sayfalamayı bir `Specification<T>` ile tanımlayıp repository'nin `ListAsync(spec)` ile `TotalCount + Data` dönmesi ve sonucun `PagedList<T>` ile sarılması.
  - Specification deseni bir seçenek. Daha basit bir query object da yeterli olur.

#### BE-O4 — Aynı iş kuralı 3-4 farklı yerde tanımlanmış (single source of truth yok)

- **Konum:**
  - Uzunluk limitleri: [TodoItem.cs:8-9](../todo-app/todo.Domain/Entities/TodoItem.cs#L8-L9), [TodoList.cs:9](../todo-app/todo.Domain/Entities/TodoList.cs#L9), [User.cs:8-10](../todo-app/todo.Domain/Entities/User.cs#L8-L10), [SharedValidationRules.cs:8-10](../todo-app/todo.Application/Common/Validation/SharedValidationRules.cs#L8-L10), [TodoDbContext.cs:152](../todo-app/todo.Infrastructure/Persistence/TodoDbContext.cs#L152), [TodoDbContext.cs:191](../todo-app/todo.Infrastructure/Persistence/TodoDbContext.cs#L191), [TodoDbContext.cs:197](../todo-app/todo.Infrastructure/Persistence/TodoDbContext.cs#L197), [LoginWithPasswordCommandValidator.cs:11-16](../todo-app/todo.Application/Features/Authentication/LoginWithPassword/LoginWithPasswordCommandValidator.cs#L11-L16) (`256`, `128` sihirli sayılar)
  - E-posta normalizasyonu: [EmailNormalizer.cs](../todo-app/todo.Application/Features/Authentication/Common/EmailNormalizer.cs) ve [User.cs:200-228](../todo-app/todo.Domain/Entities/User.cs#L200-L228)
  - Şifre politikası: [RegisterWithPasswordCommandValidator.cs:24-29](../todo-app/todo.Application/Features/Authentication/RegisterWithPassword/RegisterWithPasswordCommandValidator.cs#L24-L29) ve [login-page.ts:145-152](../todo-app/todo.Web/src/app/pages/login/login-page.ts#L145-L152)
- **Sorun:** Bir limiti değiştirmek için 3-4 dosyayı birlikte güncellemek gerekiyor. Biri unutulursa doğrulama, DB ve UI birbirinden farklı davranır.
- **Çözüm yönü:**
  - Kuralın **sahibi** Domain olmalı. Limitleri Domain'de tek bir yerde (ör. entity'ye ait sabitler ya da value object) tanımla.
  - Application validator'ları ve EF konfigürasyonu bu tanımı kullansın.
  - Frontend limitlerini en azından tek bir sabit dosyada topla.

#### BE-O5 — Benzersizlik kuralı katmanlar arasında farklı yorumlanıyor

- **Konum:** [NameComparison.cs:7-11](../todo-app/todo.Domain/Common/NameComparison.cs#L7-L11) (tr-TR, büyük/küçük harf duyarsız), [TodoList.cs:159-174](../todo-app/todo.Domain/Entities/TodoList.cs#L159-L174), [TodoListRepository.cs:58-78](../todo-app/todo.Infrastructure/Persistence/TodoListRepository.cs#L58-L78) (DB collation'ına göre `==`), [TodoDbContext.cs:166-168](../todo-app/todo.Infrastructure/Persistence/TodoDbContext.cs#L166-L168), [TodoDbContext.cs:242-244](../todo-app/todo.Infrastructure/Persistence/TodoDbContext.cs#L242-L244) (unique index)
- **Sorun:**
  - Todo başlığı benzersizliği Domain'de **Türkçe kültürle** kontrol ediliyor. Liste adı benzersizliği ise **DB collation'ı** ile kontrol ediliyor. Son savunma hattı olan unique index de collation'a bağlı ve collation hiçbir yerde açıkça tanımlanmamış.
  - Türkçe'de `I/ı` ve `İ/i` eşleşmeleri Latin collation'dan farklı. Bu yüzden Domain "farklı" derken DB "aynı" diyebilir. Kullanıcı o durumda anlamlı "aynı isimde görev var" mesajı yerine genel bir `409 conflict` görür.
  - İki benzer kural (liste adı, todo başlığı) iki farklı katmanda yaşıyor.
- **Çözüm yönü:**
  - "İki isim ne zaman aynıdır?" sorusunun cevabını tek bir yerde tanımla.
  - Bu kuralı DB tarafıyla hizala: collation'ı açıkça seç ya da normalize edilmiş bir karşılaştırma kolonu kullan.
  - İki benzersizlik kontrolünü aynı katmana ve aynı desene taşı.
  - Bu uyuşmazlığı yakalayan bir test yaz.

#### BE-O6 — Konfigürasyon okuma elle yazılmış; Options pattern kullanılmıyor

- **Konum:** [JwtSettings.cs:92-117](../todo-app/todo.Infrastructure/Services/Authentication/JwtSettings.cs#L92-L117), [SmtpSettings.cs:123-157](../todo-app/todo.Infrastructure/Services/Email/SmtpSettings.cs#L123-L157), [DependencyInjection.cs:77-158](../todo-app/todo.Infrastructure/DependencyInjection.cs#L77-L158), [AuthenticationSetup.cs:84-102](../todo-app/todo.API/Authentication/AuthenticationSetup.cs#L84-L102)
- **Sorun:** Her ayar sınıfı kendi `FromConfiguration`, `TryParse` ve hata mesajını yazıyor. `JwtSettings` hem Infrastructure'da hem API'de ayrı ayrı okunuyor. Yaklaşık 150 satırlık parse kodu framework'ün zaten sunduğu bir işi tekrar ediyor.
- **Neden önemli:** Bakım maliyeti yüksek. Ayrıca ekipteki diğer geliştiricilerin beklediği standart (`IOptions<T>`) kullanılmamış. Startup'ta fail-fast yapma fikri doğru, sadece araç seçimi yanlış.
- **Çözüm yönü:**
  - Ayar sınıflarını Options pattern ile bağla.
  - Doğrulamayı deklaratif yap (data annotations ya da `IValidateOptions`).
  - Startup'ta doğrulama (`ValidateOnStart`) ile bugünkü fail-fast davranışını koru.

#### BE-O7 — Audit bilgisi yok

- **Konum:** [TodoItem.cs](../todo-app/todo.Domain/Entities/TodoItem.cs), [TodoList.cs](../todo-app/todo.Domain/Entities/TodoList.cs), [User.cs](../todo-app/todo.Domain/Entities/User.cs)
- **Sorun:** Yalnızca `CreatedAtUtc` var. "Kim, ne zaman değiştirdi?" sorusunun cevabı yok. Admin rolü başkalarının listelerini silebiliyor ([TodoListAccessPolicy.cs:21-24](../todo-app/todo.Application/Authorization/TodoListAccessPolicy.cs#L21-L24)) ama bunun izi kalmıyor.
- **Çözüm yönü:**
  - Ortak bir taban entity / audit sözleşmesi tanımla (ör. `Entity` → `AuditableEntity` hiyerarşisi; `CreatedBy`, `UpdatedAt`, `UpdatedBy`… alanlarıyla).
  - Audit alanlarını entity'lerde elle değil, persistence katmanında (`SaveChanges` öncesi veya interceptor ile) `ICurrentUser` + `TimeProvider` kullanarak doldur. Zamanı UTC tut.
  - Soft delete'in bu domain için gerekip gerekmediğine bilinçli karar ver. Gerekirse soft delete + EF global query filter kombinasyonunu incele.

#### BE-O8 — Infrastructure katmanına iş kuralı sızmış

- **Konum:**
  - Hatırlatma uygunluğu: [TodoReminderRepository.cs:33-39](../todo-app/todo.Infrastructure/Persistence/TodoReminderRepository.cs#L33-L39) ↔ [TodoItem.cs:214-255](../todo-app/todo.Domain/Entities/TodoItem.cs#L214-L255) (`NeedsReminder`, `CanRetryReminder`)
  - İsim eşitliği: [TodoListRepository.cs:58-78](../todo-app/todo.Infrastructure/Persistence/TodoListRepository.cs#L58-L78) ↔ [NameComparison.cs:7-11](../todo-app/todo.Domain/Common/NameComparison.cs#L7-L11)
- **Sorun:** Clean Architecture'da repository veriyi getirir, iş kuralına karar vermez. Bu projede iki yerde Infrastructure karar veriyor:
  - **Hatırlatma uygunluğu:** "Bir göreve hatırlatma gönderilmeli mi?" kuralının sahibi Domain. Ama aynı kural `TodoReminderRepository`'deki LINQ sorgusunda koşul koşul yeniden yazılmış: tamamlanmamış olma, daha önce gönderilmemiş olma, bitiş tarihinin şimdi ile süre sınırı arasında olması, deneme sayısı ve son deneme zamanı. Use case adayları Domain metotlarıyla tekrar süzdüğü için bugün yanlış e-posta gitmiyor, ama kural iki yerde yaşıyor.
  - **İsim eşitliği:** "İki liste adı ne zaman aynıdır?" sorusunun cevabını `TodoListRepository`'deki `==` karşılaştırması, yani veritabanının collation'ı veriyor. Todo başlıklarında aynı soruya Domain'deki `NameComparison` cevap veriyor. Aynı iş kuralının bir yarısı Domain'de, diğer yarısı Infrastructure'da (iki tarafın farklı sonuç üretmesi BE-O5'te ele alındı).
- **Neden önemli:**
  - Kural Domain'de değişip Infrastructure'daki kopyası güncellenmezse sessiz hatalar oluşur. Hatırlatma sorgusu daha dar kalırsa hatırlatması gereken görevler hiç aday olmaz ve e-posta gitmez. Daha geniş kalırsa gereksiz adaylar batch limitini (`Take`) doldurup gerçek adayları dışarıda bırakabilir.
  - İş kuralı veritabanı ayarına bağlı kaldığında, DB değişikliği (collation, sağlayıcı) uygulamanın davranışını kod değişmeden değiştirir.
  - Domain'in iş kurallarının tek sahibi olması Clean Architecture'ın temel varsayımı. Bu varsayım bozulduğunda kuralı test etmek için veritabanı gerekir.
- **Çözüm yönü:**
  - Hatırlatma uygunluğunu Domain'de **tek bir ifade** olarak tanımla (ör. `Expression<Func<TodoItem, bool>>` döndüren bir specification). Repository sorgusu ve bellek içi kontrol aynı ifadeyi kullansın. Bu mümkün değilse sorgu filtresinin yalnızca bir performans ön filtresi olduğunu açıkça belirt ve Domain kuralının sorgudan daha dar kalmasını garanti eden bir test yaz.
  - İsim eşitliğinin anlamını Domain'de tanımla. Repository'ye yalnızca Domain'in ürettiği bir değerle (ör. normalize edilmiş bir karşılaştırma kolonu) eşitlik sorgusu yapmak kalsın.
  - Refactor sonrasında Infrastructure'daki her sorguyu "bu koşul bir iş kararı mı, teknik bir filtre mi?" sorusuyla gözden geçir.

---

## 4. Frontend Bulguları (Feature-based Architecture)

### 🔴 Kritik

#### FE-K1 — Proje feature-based değil, katman bazlı (layer-based) yapılandırılmış

- **Konum:** [src/app/](../todo-app/todo.Web/src/app/) → `pages/`, `services/`, `models/`, `guards/`, `interceptors/`
- **Sorun:** Dosyalar *ne oldukları*na göre gruplanmış (servisler bir klasörde, modeller başka bir klasörde). *Hangi işe ait oldukları*na göre gruplanmamış. "Todo" özelliğinin parçaları `pages/todos`, `services/todo-api.service.ts` ve `models/todo.models.ts` olarak üç klasöre dağılmış. "Auth" ile ilgili kod beş farklı klasörde.
- **Neden önemli:** Hedef mimari feature-based. Bu yapıda:
  - Bir özelliği silmek ya da başka ekibe devretmek tüm klasörlere dokunmayı gerektirir.
  - Özellikler arası bağımlılıklar görünmez hale gelir.
  - Lazy loading sınırları doğal olarak oluşmaz.
- **Çözüm yönü:** Önce özellik sınırlarını belirle (ör. auth, todo-lists, todos, admin). Sonra her özelliği kendi klasörüne topla. Uygulama genelinde tek örneği olan altyapıyı `core`'a (auth, interceptor'lar, error-handling, logging, layout), birden fazla özelliğin kullandığı sunumsal parçaları `shared`'a koy. Her feature kendi içinde `components`, `pages`, `models`, `services` alt klasörlerine sahip olabilir. Hedef iskelet şuna benzer:

  ```
  app/
    core/        → auth oturumu, interceptor'lar, guard'lar, global hata yönetimi, config
    shared/      → özellikten bağımsız UI parçaları, pipe'lar
    features/
      auth/      → login sayfası + ilgili modeller/servisler
      todos/     → listeler ve görevler (sayfalar, bileşenler, servis, modeller)
      admin/     → kullanıcı yönetimi
  ```

  Ardından **bağımlılık kuralını** belirle ve uygula: feature'lar birbirini import etmez, yalnızca `core` ve `shared`'ı kullanır.

#### FE-K2 — Ana ekran tek bir "god component"

- **Konum:** [todo-page.ts](../todo-app/todo.Web/src/app/pages/todos/todo-page.ts) (455 satır, 20+ state alanı), [todo-page.html](../todo-app/todo.Web/src/app/pages/todos/todo-page.html) (345 satır), [todo-page.scss](../todo-app/todo.Web/src/app/pages/todos/todo-page.scss) (709 satır, ~11 KB; [angular.json](../todo-app/todo.Web/angular.json)'daki 12 KB uyarı bütçesine çok yakın)
- **Sorun:** Tek bir bileşen şunların hepsini yapıyor:
  - Liste CRUD'u
  - Todo CRUD'u
  - İki ayrı formun state'i (`newTodo*` ve `editedTodo*` alanları)
  - Tarih dönüşümleri
  - Öncelik etiket ve renk eşlemesi
  - HTTP hata yorumlama
  - Sidebar ve layout
- **Neden önemli:** Bu bileşen yeniden kullanılamaz, parça parça test edilemez ve iki geliştirici aynı anda üzerinde çalışamaz. Her yeni özellik buraya eklenerek dosyayı daha da büyütür.
- **Çözüm yönü:**
  - **Container / presentational** ayrımı uygula. Sayfa bileşeni (`pages`) yalnızca veriyi ve akışı yönetsin. Liste sidebar'ı, todo listesi, todo satırı ve todo formu `input`/`output` ile çalışan küçük bileşenler (`components`) olsun. `list`, `detail`, `create`, `edit` gibi sorumluluk bazlı bir bölünme iyi bir başlangıç noktası.
  - Layout parçalarını (sidebar, üst bar) `core/layout` gibi ayrı bir yere al.
  - State'i ve API çağrılarını feature'a ait bir state servisine (facade/store) taşı.
  - Create ve edit formları için tek bir form bileşeni yeterli olabilir.
  - Stilleri bileşenlere böl.

#### FE-K3 — Ortam konfigürasyonu yok; API adresi ve CSP `localhost`'a sabitlenmiş

- **Konum:** [api-url.ts:7-13](../todo-app/todo.Web/src/app/core/api-url.ts#L7-L13), [index.html:5-8](../todo-app/todo.Web/src/index.html#L5-L8) (`connect-src ... http://localhost:7070 http://localhost:17070`)
- **Sorun:** `environments` dosyası yok. API adresi koda gömülü bir varsayılan ve dokümante edilmemiş bir `window.__TODO_API_URL__` override'ı ile belirleniyor. Asıl engel CSP meta etiketi: `connect-src` yalnızca localhost portlarına izin veriyor. Bu yüzden production build'i başka bir API adresine istek atamaz. Override kullanılsa bile tarayıcı isteği engeller.
- **Neden önemli:** Uygulama kod değişikliği yapılmadan test, staging ya da prod ortamına çıkamaz. Enterprise projelerde "aynı build, farklı ortam" beklenen standart.
- **Çözüm yönü:**
  - Ortam bazlı konfigürasyon stratejisi belirle. Build-time `environments/environment.ts` + `environment.prod.ts` (ve `fileReplacements`) kullanılabilir. "Aynı build, farklı ortam" hedefleniyorsa deploy anında okunan bir runtime config dosyasını da değerlendir.
  - API adresini servislere doğrudan import yerine bir DI token'ı ile ver.
  - CSP'yi ortam bazlı üret ya da web sunucusu header'ına taşı.

---

### 🟠 Orta

#### FE-O1 — Liste değiştirirken race condition: yanlış listenin görevleri gösterilebiliyor

- **Konum:** [todo-page.ts:105-111](../todo-app/todo.Web/src/app/pages/todos/todo-page.ts#L105-L111), [todo-page.ts:200-212](../todo-app/todo.Web/src/app/pages/todos/todo-page.ts#L200-L212)
- **Sorun:** `selectList` önce `selectedList`'i değiştiriyor, sonra `loadTodos`'u bekliyor. `loadTodos` cevap geldiğinde hangi liste için çağrıldığını kontrol etmeden `this.todos`'u eziyor.
- **Senaryo:** Kullanıcı A listesine tıklar ve istek yavaştır. Hemen ardından B listesine tıklar. B'nin cevabı önce, A'nınki sonra gelir. Ekranda **B seçili görünür ama A'nın görevleri listelenir.** Bu durumda yapılan düzenleme ya da silme işlemi B'nin id'si ile A'nın todo id'sine gider.
- **Neden önemli:** `firstValueFrom` + `async/await` deseni, RxJS'in iptal mekanizmasını devre dışı bırakıyor.
- **Çözüm yönü:**
  - "Seçili liste" değişikliğini bir akış (stream / signal) olarak modelle. Yeni seçim geldiğinde önceki isteği iptal eden bir operatör kullan. Bunun için şunlara bakılabilir: `switchMap`, `takeUntil`, Angular `rxResource` / `resource`.
  - Ya da cevap geldiğinde hâlâ ilgili listenin seçili olup olmadığını doğrula.
  - Bu senaryo için bir unit test yaz.

#### FE-O2 — State yönetimi tutarsız: bazı yerlerde signal, bazı yerlerde düz alan + manuel `markForCheck`

- **Konum:** [todo-page.ts:40-66](../todo-app/todo.Web/src/app/pages/todos/todo-page.ts#L40-L66) (düz alanlar, her `finally`'de `changeDetector.markForCheck()`), [login-page.ts:38-42](../todo-app/todo.Web/src/app/pages/login/login-page.ts#L38-L42) ve [admin-users-page.ts:22-24](../todo-app/todo.Web/src/app/pages/admin/admin-users-page.ts#L22-L24) (signal)
- **Sorun:** Uygulama zoneless çalışıyor (`zone.js` yok). Bu yüzden düz alanlar değiştiğinde ekran kendiliğinden güncellenmiyor ve her async metodun sonunda `markForCheck` çağrılması gerekiyor. Bir yerde unutulursa UI güncellenmez. Aynı projede iki farklı reaktivite modeli kullanılmış.
- **Çözüm yönü:** Zoneless bir uygulamada state için tek bir standart seç: signal'ler ve türetilmiş değerler için `computed`. Bu standardı FE-K2'deki feature state servisi ile birlikte uygula. `ChangeDetectorRef` ihtiyacı ortadan kalkmalı. Yükleme durumlarını birden fazla boolean yerine tek bir durum alanıyla (ör. `NOT_LOADED / LOADING / LOADED` değerlerine sahip bir `LoadingState` enum'u) ifade etmeyi de düşün.

#### FE-O3 — Hata yönetimi her sayfada ayrı ayrı yazılmış

- **Konum:** [todo-page.ts:418-454](../todo-app/todo.Web/src/app/pages/todos/todo-page.ts#L418-L454), [login-page.ts:157-183](../todo-app/todo.Web/src/app/pages/login/login-page.ts#L157-L183), [admin-users-page.ts:33-38](../todo-app/todo.Web/src/app/pages/admin/admin-users-page.ts#L33-L38)
- **Sorun:** `ProblemDetails` parse etme, durum koduna göre mesaj seçme ve "API'ye bağlanılamadı" kontrolü her sayfada farklı biçimde tekrar yazılmış. Validation hataları (`errors` sözlüğü) hiç işlenmiyor; yalnızca ilk mesaj (`detail`) gösteriliyor.
- **Çözüm yönü:**
  - HTTP hatasını uygulamanın kendi hata modeline çeviren tek bir `core` servisi yaz (ör. `core/error-handling/ErrorHandlerService`). Bu modeli gösteren ortak bir bileşeni (ör. `shared/list-errors`) tüm sayfalarda kullan.
  - Global olanları (`0`, `5xx`, `429`) bir interceptor'da işle.
  - Sayfalar yalnızca kendilerine özgü durumlarla ilgilensin (ör. "aynı isim zaten var").
  - Alan bazlı validation hatalarını forma bağla.

#### FE-O4 — Template-driven formlar ve elle yazılmış doğrulama

- **Konum:** [login-page.ts:130-155](../todo-app/todo.Web/src/app/pages/login/login-page.ts#L130-L155), [todo-page.ts:113-120](../todo-app/todo.Web/src/app/pages/todos/todo-page.ts#L113-L120), [todo-page.ts:148-159](../todo-app/todo.Web/src/app/pages/todos/todo-page.ts#L148-L159), [todo-page.ts:214-225](../todo-app/todo.Web/src/app/pages/todos/todo-page.ts#L214-L225)
- **Sorun:** Formlar `ngModel` + bileşen alanları ile kurulmuş. Doğrulama `if` blokları ve toast mesajları ile elle yapılıyor. "Boş olamaz" kontrolü beş ayrı yerde tekrar ediyor. Alan bazlı hata gösterimi ve dirty/touched durumu yok.
- **Çözüm yönü:** Reactive Forms'a (`FormBuilder` + validator'lar) ya da Angular'ın signal tabanlı form API'sine geç. Kuralları deklaratif validator'lar olarak tanımla. Limitleri backend ile aynı kaynaktan al (bkz. BE-O4).

#### FE-O5 — Lazy loading yok

- **Konum:** [app.routes.ts:1-28](../todo-app/todo.Web/src/app/app.routes.ts#L1-L28)
- **Sorun:** Tüm sayfalar eager `component:` ile yükleniyor. Login ekranını açan kullanıcı admin sayfasının kodunu da indiriyor.
- **Çözüm yönü:** FE-K1'den sonra her feature kendi route tanımını dışarı açsın. Kök router bu tanımları lazy yüklesin (`loadComponent`, ya da feature başına ayrı route dosyası için `loadChildren`). Mevcut guard'larını (`authGuard`, `adminGuard`) bu yapıda korumayı unutma.
