# Audit ve soft delete:

## 1. Uygulamada gerçek sorun neydi?

Listeyi veya görevi sildiğimde satır veritabanından kaldırılıyordu. AuditRecords
hangi kaydın kim tarafından, ne zaman silindiğini saklıyordu; silinen kaydın
başlığı, açıklaması ve diğer içeriği ise kendi tablosunda kalmıyordu.

Yeni beklenti, silinen iş verisinin de korunması. Bunun için User, TodoList ve
TodoItem kayıtlarına soft delete ekledim. Silme isteği artık satırı kaldırmak
yerine silindi olarak işaretliyor.

Audit ve soft delete farklı ihtiyaçları karşılıyor. Soft delete kaydı ve son
silme bilgisini tutuyor; audit ise güncelleme ve silme işlemlerinin geçmişini tutuyor.

## 2. Eklediğim parçalar

Ortak `todo.Domain/Common/AuditableEntity.cs` sınıfına şu alanları ekledim:

```csharp
public bool IsDeleted { get; private set; }
public DateTime? DeletedAtUtc { get; private set; }
public int? DeletedBy { get; private set; }
```

Bu sınıftan türeyen Users, TodoLists ve TodoItems tablolarında aynı kolonlar var.
Oluşturma ve son güncelleme bilgileri de ortak sınıfta duruyor:

| Alan | Anlamı |
|---|---|
| CreatedAtUtc / CreatedBy | Oluşturulma zamanı ve oluşturan kullanıcı |
| UpdatedAtUtc / UpdatedBy | Son güncelleme zamanı ve değiştiren kullanıcı |
| IsDeleted | Kayıt silinmiş olarak işaretli mi? |
| DeletedAtUtc / DeletedBy | Silinme zamanı ve silen kullanıcı |

Zamanları UTC olarak TimeProvider'dan, işlemi yapan kullanıcıyı ICurrentUser'dan
alıyorum. Anonim kayıt ve arka plan işi gibi bir kullanıcı bulunmayan durumda
kullanıcı alanı null kalabilir. Listenin sahibi ile işlemi yapan kişi farklı
olabilir: admin silerse DeletedBy adminin ID'si olur.

`TodoDbContext.SoftDeletion.cs`, silinen kullanıcı ve listelerin alt kayıtlarını
yükleyip aynı silme işlemine dahil ediyor. `AddSoftDeletion` migration'ı kolonları,
aktif kayıtlara uygulanan benzersiz indeksleri ve refresh token ilişkisindeki
korumayı ekliyor. Eski satırlar IsDeleted=false ve boş silme bilgileriyle korunuyor.

## 3. Kaldırdığım parçalar

Uygulamanın kullanıcı, liste ve görev için fiziksel silme davranışını değiştirdim.
Use case'lerdeki silme istekleri devam ediyor; DbContext bunları veritabanına DELETE
göndermek yerine metadata güncellemesine çeviriyor.

Tüm satırları kapsayan eski benzersiz indeksleri değiştirdim. Liste adı, görev
başlığı, e-posta ve Google kullanıcı kimliği yalnızca aktif kayıtlar arasında
benzersiz. Böylece silinen bir kaydın adı yeni kayıt oluşturmayı engellemiyor.

Refresh token'ları kullanıcı silindiğinde otomatik fiziksel silen ilişkiyi kaldırdım.
Token'ların mevcut RevokedAtUtc alanını kullanarak oturumları iptal ediyorum.
AuditRecords geçmişi korunuyor; bu tabloya silinip gizlenme davranışı eklemiyorum.

## 4. Kodun adım adım akışı

Örneğin bir liste silinirken:

```text
DeleteTodoListUseCase
  → Repository listeyi silinmek üzere işaretler
  → UnitOfWork.SaveChangesAsync
  → DbContext gerekirse henüz yüklenmemiş görevleri getirir
  → Liste ve aktif görevler için Deleted audit kayıtları hazırlanır
  → DELETE yerine IsDeleted / DeletedAtUtc / DeletedBy güncellenir
  → İşaretlemeler ve audit kayıtları aynı transaction içinde kaydedilir
```

`TodoDbContext.cs` içindeki ilgili bölüm:

```csharp
records.Add(CreateAuditRecord(entry, AuditAction.Deleted, actorId, now));
entry.State = EntityState.Unchanged;
entry.Property(entity => entity.IsDeleted).CurrentValue = true;
entry.Property(entity => entity.DeletedAtUtc).CurrentValue = now;
entry.Property(entity => entity.DeletedBy).CurrentValue = actorId;
```

State'i Unchanged yapmamın amacı bekleyen fiziksel silmeyi iptal etmek. Sonra yalnızca
silme alanlarını değiştiriyorum. EF bu alanlar için UPDATE oluşturuyor; kaydın asıl
verileri ve ilişkileri korunuyor. Silme işleminde eski güncelleme bilgilerini değiştirmiyorum.

Liste silinirken daha önce tek başına silinmiş görevlerin ilk silinme bilgileri
korunuyor. Kullanıcı silinirse aktif listeleri ve görevleri de işaretleniyor;
refresh token'ları iptal ediliyor. İşaretleme veya audit yazımı hata verirse birlikte
rollback oluyor. Aynı context ile tekrar denemede silme niyeti ve Deleted audit türü korunuyor.

IsDeleted ayrıca concurrency token. Başka istek satırı sildikten sonra eski
bir kopyayı kaydetmeye çalışan istek sessizce silinmiş kaydı değiştiremiyor.

## 5. Nerelerde uygulandı?

### Normal sorgulardan gizleme

Global query filter, her sorguya otomatik eklenen ortak Where koşulu gibi çalışıyor.
Users için temel koşul:

```csharp
modelBuilder.Entity<User>().HasQueryFilter(user => !user.IsDeleted);
```

Liste sorguları ayrıca sahibinin, görev sorguları da bağlı listenin aktif olmasını
arıyor. Böylece sayfalama, toplam sayıları, tek kayıt okuma ve yönetici okumalarında
silinmiş kayıtlar görünmüyor. Admin olmak bu filtreleri otomatik kaldırmıyor.

Silinmiş kayıtlar yalnızca özellikle `IgnoreQueryFilters()` kullanılan iç sorgularla
okunabiliyor. Normal API endpoint'lerine böyle bir seçenek eklemedim.

### Hatırlatma ve oturum

TodoReminderSpecification içindeki uygunluk kuralları da IsDeleted kontrolü yapıyor.
Silinmiş görevler SQL sorgusunda seçilmiyor ve bellekteki son kontrolden geçemiyor.

JWT doğrulamasında kullanıcının hâlâ aktif olduğunu repository üzerinden kontrol
ediyorum. Silinen kullanıcı daha önce aldığı access token ile API'ye erişemiyor.
Refresh token sorguları da silinmiş kullanıcıların token'larını gizliyor; eski
hesabın token'ı aynı e-postayla oluşturulan yeni hesaba bağlanmıyor.

### Geçiş ve erişim sınırları

Migration mevcut TodoDatabaseMigrator ve HerSeyiBaslat.bat akışına bağlı.
Önceki fiziksel silmelerde kaldırılan veriler bu değişiklikle geri gelmez.
Yeni silmelerde satır ve içerik korunur. Bu aşamada çöp kutusu veya geri yükleme
endpoint'i eklemedim; geri yüklemede isim çakışmaları ve alt kayıtlar ayrıca ele alınmalı.

Bu mekanizma DbContext'in SaveChanges akışında çalışır. Doğrudan SQL veya
ExecuteDelete gibi bu akışı atlayan yazımlar otomatik soft delete/audit oluşturmaz.
AuditRecords işlemin türünü, hedefini, zamanını ve aktörünü saklar; alanların tüm
eski/yeni değerlerini kopyalayan bir sürüm geçmişi değildir.

## 6. Ne kazandım?

- Silinen iş verisi kendi tablosunda korunuyor.
- Kim, ne zaman sildi sorusunun cevabı doğrudan satırda bulunuyor.
- Güncelleme ve silme geçmişi ayrı audit tablosunda kalıyor.
- Silinmiş kayıtlar ekranlara, toplam sayılara ve hatırlatmalara karışmıyor.
- Silinen hesabın eski oturumları erişim sağlayamıyor.
- Silinmiş kayıtların isimleri yeni aktif kayıtlarda kullanılabiliyor.
- Use case'lere tekrar tekrar silme metadata'sı doldurma kodu yazmıyorum.

Kaynaklar: [EF Core global query filters](https://learn.microsoft.com/en-us/ef/core/querying/filters),
[filtreli indeksler](https://learn.microsoft.com/en-us/ef/core/modeling/indexes#index-filter).
