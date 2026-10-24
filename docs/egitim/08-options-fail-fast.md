# Options Pattern ve fail-fast:

## 1. Uygulamada gerçek sorun neydi?

JWT, SMTP, refresh token, hatırlatma ve istek limiti ayarları elle okunup
dönüştürülüyordu. JWT ayarları token üretimi ve API doğrulamasında ayrı okunuyordu. Ayar
gruplarını Options mekanizmasına bağladım; yanlış ayar açılışta reddediliyor.

## 2. Eklediğim parçalar

Dosyalar `todo.Infrastructure/Configuration` altında:

- JwtOptions: access token ve refresh token ayarlarının ortak girdisi.
- SmtpOptions: host, port, TLS seçimi, kullanıcı/parola ve gönderen bilgileri.
- ReminderOptions: saat/dakika cinsinden hatırlatma ayarları ve saat dilimi.
- GoogleAuthenticationOptions: isteğe bağlı Google client ID.
- AuthorizationOptions: admin email listesi.
- SecurityOptions: giriş işlemlerinin dakika başına istek limiti.
- OptionsSetup: binding ve ValidateOnStart kayıtları.
- RuntimeOptionsValidator: IValidateOptions sözleşmesiyle JWT, SMTP,
  hatırlatma ve istek limiti doğrulaması.

Validator, mevcut immutable Settings sınıflarının guard'larını kullanır.
Örneğin minimum JWT anahtar uzunluğu veya uzak sunucuda şifresiz SMTP yasağı
iki yerde tekrar yazılmadı. Constructor'ın ayar hatası, OptionsValidationException
oluşturan doğrulama sonucuna çevrilir. Sayı/enum/boolean biçimi yanlışsa binder
zaten açılışta hata verir.

GoogleIdTokenValidator, MailKitEmailSender ve AuthController da ilgili IOptions
bağımlılığını kullanır. RateLimiterOptions, SecurityOptions değerinden kurulur.

Application'daki SessionIssuer ve reminder use case'i mevcut immutable
RefreshTokenSettings / ReminderSettings nesnelerini almayı sürdürür. Bunlar
Infrastructure DI kaydında doğrulanmış Options değerlerinden oluşturulur.
Application'a configuration veya IOptions bağımlılığı eklenmedi.

## 3. Kaldırdığım parçalar

JwtSettings, SmtpSettings, GoogleAuthenticationSettings ve
ConfiguredRoleAssignmentPolicy içindeki FromConfiguration metotları kaldırıldı.
Infrastructure'daki ReadRefreshTokenSettings, ReadReminderSettings, ReadBoolean
ve ReadPositiveInteger yardımcıları kaldırıldı. API'deki elle istek limiti okuma
ve ikinci JWT config okuması da kaldırıldı.

Infrastructure projesine .NET 8 uyumlu
Microsoft.Extensions.Options.ConfigurationExtensions 8.0.0 eklendi.

## 4. Kodun adım adım akışı

### Önce / sonra

Önceden JwtSettings.FromConfiguration içinde:

```csharp
string lifetimeText = section["AccessTokenLifetimeMinutes"] ?? string.Empty;
bool lifetimeIsNumber = int.TryParse(lifetimeText, out int accessTokenLifetimeMinutes);
```

Benzer okuma kodları SMTP, refresh token, hatırlatma ve istek limiti için vardı.
JWT hem Infrastructure kaydında hem API authentication kurulumunda ayrı okunuyordu.

Şimdi `todo.Infrastructure/Configuration/OptionsSetup.cs` içinde gerçek kayıt:

```csharp
services.AddOptions<JwtOptions>()
    .Bind(configuration.GetSection(JwtOptions.SectionName))
    .ValidateOnStart();
```

- AddOptions: bu ayar grubunu .NET'e tanıt.
- Bind: `Jwt` bölümündeki değerleri aynı isimli property'lere doldur.
- ValidateOnStart: doğrulamayı ilk kullanıcı isteğine bırakmadan açılışta çalıştır.

`JwtOptions` bir ayar formu gibi. Issuer, Audience, SigningKey,
AccessTokenLifetimeMinutes ve RefreshTokenLifetimeDays alanları var.
Sayıya/enum'a/boolean'a dönüşümü artık framework yapıyor; bozuk türü reddediyor.

### Servis ayarı nasıl alıyor?

`JwtAccessTokenGenerator` constructor'ı artık `IOptions<JwtOptions>` alıyor:

```csharp
_jwtSettings = jwtSettings.Value.ToSettings();
```

Value, framework'ün bağlayıp doğruladığı ayar nesnesidir. ToSettings bu girdiyi
mevcut, değiştirilemeyen çalışma ayarına dönüştürür. Token üretirken tekrar
configuration sözlüğünü okumuyorum.

API'nin JwtBearerOptions kaydı da aynı `IOptions<JwtOptions>` kaynağını kullanır.
Token'ı üreten ve doğrulayan iki tarafın issuer/audience/imza ayarları ortak.

## 5. Nerelerde uygulandı?

JwtAccessTokenGenerator ve API JwtBearer kurulumunda aynı IOptions<JwtOptions> kaynağını
kullanıyorum. MailKitEmailSender, GoogleIdTokenValidator, AuthController ve rate limiter
kendi ayar grubunu alıyor. Application, Options'tan üretilen mevcut immutable çalışma
ayarlarını kullanmayı sürdürüyor.

## 6. Ne kazandım?

### Davranış ve kazanç

- Token süresi -10, SMTP portu 65536 veya istek limiti 0 ise açılış başarısız.
- JWT anahtarı eksik/kısa veya saat dilimi bilinmiyorsa açılış başarısız.
- Port yerine yazı veya Enabled yerine geçersiz değer varsa binder hata verir.
- SMTP credentials eksikse mail kapalı kalır. Google client ID boşsa Google
  girişi kapalı kalır; bu iki özellik isteğe bağlıdır.
- Uzak sunucuda şifresiz SMTP reddedilir. Parolanın başındaki/sonundaki
  boşluklar kesilmez; gönderen adresinin kullanıcı adından türetilmesi korunur.
- Mevcut config anahtarları, varsayılan değerler ve secret/env kaynakları kullanılır.

Hangfire servislerinin eklenmesi DI kurulurken Enabled bayrağıyla belirlenir;
çalışma sırasında reminder ayarları Options'tan üretilen ReminderSettings'ten
alınır. IOptions, uygulama ömrü boyunca kullanılan ayardır; bu değişiklikte çalışma
esnasında otomatik ayar değiştirme akışı eklenmedi.

