# Domain invariant ve kuralın tek sahibi:

## 1. Uygulamada gerçek sorun neydi?

Bir iş kuralı farklı kod yollarından gelen işlemlerde de korunmalı. API doğrulaması
erken ve anlaşılır hata verir; TodoItem gibi Domain nesneleri kendi geçerli durumlarını
korur. Başlık için uygulamadaki mevcut 120 karakter sınırını esas aldım.

## 2. Eklediğim parçalar

Domain sabitlerini diğer katmanlara açtım. E-posta normalizasyonunu ve şifre
politikasını Domain altında topladım; frontend limitlerini de ortak bir tanıma bağladım.

### Diğer değişiklikler

| Kod | Değişiklik | Kazanç |
|---|---|---|
| `TodoItem`, `TodoList`, `User` | Mevcut uzunluk sabitleri `public` oldu | Kuralın değerini diğer katmanlar okuyabilir |
| `SharedValidationRules` | Liste adı, başlık ve açıklama için üç kopya sabit kaldırıldı | Application Domain sabitlerini kullanır |
| `TodoDbContext` | İlgili `HasMaxLength` sayıları Domain sabitleriyle değiştirildi | Model ayarları aynı kaynaktan beslenir |
| Kayıt ve giriş validator'ları | E-posta/ad limitleri `User` sabitlerinden; şifre sınırları `PasswordPolicy`'den okunur | Bağımsız limit tanımları azalır |
| `EmailNormalizer` | Application/Features/Authentication/Common'dan Domain/Common'a taşındı | Hesabı arama ve oluşturma aynı normalizasyonu kullanır |
| `User` | Kendi `Trim().ToLowerInvariant()` kopyası ortak normalizer çağrısıyla değiştirildi | E-posta normalizasyonunun tek uygulaması var |
| `PasswordPolicy` | Domain/Common altında şifre uzunlukları ve harf/rakam kontrolü eklendi | Şifre politikasının sahibi Domain |
| `validation-rules.ts` | Frontend limitleri tek dosyada toplandı | HTML ve şifre kontrolü aynı istemci değerlerinden beslenir |
| Login/Todo HTML ve component'leri | Sayısal `maxlength` değerleri ortak sabitlere bağlandı | Ekranlar birbirinden bağımsız limit kopyaları tutmaz |

Şifre kuralı kayıt olurken uygulanır. Mevcut bir hesabın girişinde şifre
gücü yeniden değerlendirilmez; kimlik doğrulama doğru şifreyi kontrol eder.
`User` nesnesi şifrenin kendisini değil hash'ini alır; bu yüzden düz şifrenin
harf/rakam kontrolü, hash oluşturulmadan önce registration validator'ında
Domain politikasını kullanarak yapılır.

Frontend'deki harf/rakam kontrolü backend'in Unicode karakter kontrolüyle
eşleştirildi. Örneğin `Şifre١٢٣` backend'de kabul edilirken frontend'in eski
`\d` kontrolü bu rakamları tanımıyordu. İki tarafta uzunluk hesabı UTF-16
birimleriyle yapılır; karakter denetimi de .NET `char` davranışıyla eş tutuldu.

## 3. Kaldırdığım parçalar

Application ve EF ayarlarındaki bağımsız limit kopyalarını çıkardım. User içindeki ayrı
e-posta normalizasyonunu ortak EmailNormalizer çağrısıyla değiştirdim.

## 4. Kodun adım adım akışı

### Bir örnek üzerinden eklenen ve çıkarılan kod

Eskiden Domain kendi başlık sınırını tanımlıyordu:

```csharp
// todo.Domain/Entities/TodoItem.cs
private const int MaximumTitleLength = 120;
```

Application'da aynı sınırın bağımsız kopyası vardı:

```csharp
// todo.Application/Common/Validation/SharedValidationRules.cs
public const int TodoTitleMaximumLength = 120;
// ...
.MaximumLength(TodoTitleMaximumLength)
```

EF ayarı da rakamı yeniden yazıyordu:

```csharp
todoItemTitleProperty.HasMaxLength(120);
```

Şimdi Domain'deki sabit diğer katmanlara açık:

```csharp
// TodoItem.cs
public const int MaximumTitleLength = 120;
```

`public`, diğer katmanların bu değeri okuyabilmesini sağlar. `const`, değerin
sabit olduğunu belirtir. `int`, tam sayı türüdür. Bu satırın sahibi Domain'dir.

Application'daki kopya sabiti çıkardım. Doğrulama artık Domain'den okuyor:

```csharp
.MaximumLength(TodoItem.MaximumTitleLength)
```

EF de aynı tanımı kullanıyor:

```csharp
todoItemTitleProperty.HasMaxLength(TodoItem.MaximumTitleLength);
```

`TodoItem.MaximumTitleLength`, “TodoItem sınıfının başlık sınırını oku” demektir.
Bu değeri okumak için bir TodoItem nesnesi oluşturmak gerekmez.

Domain'in `ValidateAndNormalizeTitle` kontrolü başlık oluşturulurken ve
değiştirilirken çalışmaya devam eder. Application'ın kontrolü atlanmış olsa bile
Domain boş veya çok uzun başlığı reddeder. Geçersiz bir yeniden adlandırma,
eski geçerli başlığı değiştirmez.

## 5. Nerelerde uygulandı?

### Tek kaynak kavramının pratik sınırı

C# tarafında bu uzunlukların otoriter tanımı Domain'dir. Angular C# sabitini
doğrudan okuyamaz. Review'in önerdiği basit yaklaşımı kullandım:
`todo.Web/src/app/shared/validation-rules.ts` istemci tarafındaki tek merkezdir.
Bu dosya Domain limitlerinin elle eş tutulan kopyasıdır; otomatik üretilmez.
Bir sınırı değiştirirken bu dosya da güncellenmelidir.

Bir gün başlık sınırını 120'den 160'a çıkarırsam:

1. `TodoItem.MaximumTitleLength` güncellenir. Backend yeniden derlendiğinde
   Application ve EF bu yeni değeri kullanır.
2. Frontend ortak limit dosyası eş tutulur ve frontend yeniden derlenir.
3. SQL Server'daki mevcut kolonun boyutunu değiştirmek için EF migration
   oluşturulup uygulanır. Kodda sabiti değiştirmek canlı tabloyu değiştirmez.

Bu değişiklikte sayıların değeri değişmediği için yeni migration gerekmedi.

## 6. Neler oldu?

İş kuralının sahibi Domain oldu. Bir backend sınırını değiştirdiğimde Application ve EF
aynı tanımı kullanıyor; farklı katmanların farklı limitler uygulama riski azalıyor.
