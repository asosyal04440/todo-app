# Hatırlatma kararını tek yerde tanımlamak:

## 1. Uygulamada gerçek sorun neydi?

Görev tamamlanmamış, hatırlatması gönderilmemiş ve tarihi yaklaşmış olmalı.
Başarısız denemeler için de deneme hakkı ve bekleme süresi kontrol edilmeli.

Bu şartlar `TodoItem.NeedsReminder` / `CanRetryReminder`, gerçek repository
içinde ayrı ayrı yazılıydı. Koşullar bugün aynı
sonucu veriyordu. Risk, bir kopya değiştirilirken diğerinin unutulmasıydı.
Repository uygun görevi getirmezse Application'ın sonraki kontrolü onu
kurtaramaz; görev o aşamaya ulaşmaz. Gereğinden fazla görev getirirse uygun
olmayan görevler gönderim sayısı sınırını doldurabilir.

## 2. Eklediğim parçalar

`todo.Domain/Specifications/TodoReminderSpecification.cs` eklendi.
Specification burada “hatırlatma alabilecek görevin tarifi” demek.

Dosyanın başındaki iki Expression gerçek şartları bir kez tanımlar:

```csharp
private static readonly Expression<Func<TodoItem, DateTime, DateTime, bool>> DueRule =
    (todo, nowUtc, dueBeforeUtc) =>
        !todo.IsDeleted
        && !todo.IsCompleted
        && todo.ReminderSentAtUtc == null
        && todo.DueAtUtc != null
        && todo.DueAtUtc > nowUtc
        && todo.DueAtUtc <= dueBeforeUtc;

private static readonly Expression<Func<TodoItem, int, DateTime, bool>> RetryRule =
    (todo, maximumAttempts, lastFailedAttemptBeforeUtc) =>
        !todo.IsDeleted
        && todo.ReminderAttemptCount < maximumAttempts
        && (todo.ReminderLastAttemptAtUtc == null
            || todo.ReminderLastAttemptAtUtc <= lastFailedAttemptBeforeUtc);
```

Expression şartları incelenebilir bir tarif olarak tutar. EF bu tarifi SQL'e
çevirebilir. Aynı tariften yalnızca bir kez derlenen fonksiyonlar da bellekteki
görevi kontrol eder. Domain'in EF bağımlılığı yok; Expression .NET'in türüdür.

Dosyadaki `Bind` ve `ParameterBinder` yardımcıları iş kuralı eklemez. Zaman
ve deneme sınırlarını tarife yerleştirir, şartları birleştirir. SQL'e
çevrilemeyebilecek özel metot çağrıları veya `Invoke` üretmez. Yakalanan
değerler EF'in sorgu parametreleri oluşturmasına uygun tutulur. İlk okumada
bu teknik yardımcıları anlaman gerekmiyor; koşullara ve kullanım yerlerine bak.

## 3. Kaldırdığım parçalar

### Çıkardığım veya değiştirdiğim kod

#### Repository

Uzun `where` koşul kopyası kaldırıldı. Yerine Domain'in tarifi kullanılıyor:

```csharp
from todo in _todoDbContext.TodoItems.Where(specification.ToExpression())
```

Repository görevi liste ve sahibiyle eşleştirir, tarihe göre sıralar ve
`Take(maxCount)` ile sınırlar. Filtre, sıralama ve sınır veritabanında uygulanır.

#### Application ve repository arayüzü

Application bir çalıştırma için tarifi oluşturur:

```csharp
var specification = new TodoReminderSpecification(
    nowUtc,
    _reminderSettings.LeadTime,
    _reminderSettings.MaximumAttempts,
    _reminderSettings.RetryDelay);
```

Repository'ye ayrı ayrı tarih sınırları ve deneme sayısı yerine bu nesne
gönderilir. Repository arayüzü buna göre değişti. Gönderim öncesindeki son
kontrol de aynı nesneyi kullanır:

```csharp
if (specification.IsSatisfiedBy(todo) == false)
{
    continue;
}
```

E-posta gönderimi ve deneme sonucunu Unit of Work ile kaydetme akışı devam eder.

#### TodoItem

`NeedsReminder` ve `CanRetryReminder` isimleri korunur. İçlerindeki koşul
kopyaları kaldırılır; ortak tariften derlenen fonksiyonlara yönlenir.

## 4. Kodun adım adım akışı

Application, o çalıştırmanın zaman ve deneme sınırlarıyla TodoReminderSpecification
oluşturur. Repository ToExpression ile uygun görevleri SQL'de filtreler; gönderim
öncesinde IsSatisfiedBy aynı şartları bellekte kontrol eder. Gönderim sonucunu mevcut
Unit of Work akışıyla kaydeder.

## 5. Nerelerde uygulandı?

Ortak tarif Domain'deki TodoReminderSpecification, TodoItem kontrolleri ve
TodoReminderRepository tarafından kullanılıyor.

## 6. Ne kazandım?

- Uygunluk şartları Domain'de bir kez tanımlanıyor.
- SQL sorgusu ve bellekteki kontrol aynı tanımdan üretiliyor.
- Üçüncü deneme hakkı olan görev repository tarafından dışlanmıyor.
- Uygun olmayan erken görevler gönderim sayısı sınırını doldurmuyor.
- Domain, SQL Server'a veya Entity Framework'e bağımlı olmadı.

