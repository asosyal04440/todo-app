# Collation, normalizasyon ve unique index:

## 1. Uygulamada gerçek sorun neydi?

İki istek aynı anda “bu isim yok” cevabını alabilir. Ön kontrol tek başına benzersizliği
garanti etmez. Unique index, aynı anahtarla ikinci kaydı veritabanında engeller. Ön
kontrol, Domain ve index aynı eşitlik kuralını kullanmalı.

### Neden yalnızca Türkçe collation seçmedim?

Yerel TodoApiDb'nin ve ad/başlık kolonlarının collation'ı `Turkish_CI_AS`.
`I/ı`, `İ/i` örnekleri bu bilgisayarda Türkçe beklentimle uyumluydu.
Ek kontrolde Domain'in `ß/ss` ve `œ/oe` çiftlerini farklı, SQL Server'ın ise
aynı saydığı görüldü. `Turkish_100_CI_AS_SC` de bu farkı tek başına çözmedi.

Bu nedenle Domain'in ürettiği anahtarı saklayıp SQL Server'ın bu anahtarı
birebir karşılaştırmasını seçtim. Görünen ad korunur; karşılaştırma anahtarı
ayrı bir alandır.

## 2. Eklediğim parçalar

Domain normalizasyonuna dayanan NormalizedName / NormalizedTitle alanlarını ve bu
alanların birebir karşılaştırıldığı index düzenini ekledim.

### Entity'ler anahtarı adla birlikte tutar

`TodoList` içine:

```csharp
public string NormalizedName { get; private set; }
```

`TodoItem` içine:

```csharp
public string NormalizedTitle { get; private set; }
```

eklendi. Oluşturma ve yeniden adlandırma sırasında ortak Domain metodu
çağrılır. Kullanıcı `Plan` yazdıysa `Name` alanı `Plan`, `NormalizedName`
alanı `PLAN` olur. API'nin mevcut DTO'ları görünen adı taşır.

TodoList içindeki görev benzersizliği de aynı anahtarlara dayanır:

```csharp
string normalizedTitle = NameComparison.NormalizeForLookup(title);
// Her mevcut görev için:
string.Equals(existingTodo.NormalizedTitle, normalizedTitle, StringComparison.Ordinal)
```

Unicode normalizasyonu bazı girdileri uzatabildiği için anahtar alanlarına
asıl limitin üç katı yer ayrıldı: liste anahtarı 240, görev anahtarı 360.
Görünen adların 80/120 karakter sınırları devam eder.

## 3. Kaldırdığım parçalar

Kültürel karşılaştırmaya dayanan eski eşitlik kodunu ve görünen Name / Title alanlarını
kullanan unique index tanımlarını değiştirdim. Repository aramalarını ortak normalize
anahtara bağladım.

## 4. Kodun adım adım akışı

### Kural Domain'de

`todo.Domain/Common/NameComparison.cs` içindeki eski kültürel karşılaştırma:

```csharp
TurkishCompareInfo.Compare(first.Trim(), second.Trim(), CompareOptions.IgnoreCase) == 0
```

yerine ortak anahtar üretimi ve birebir anahtar karşılaştırması geldi:

```csharp
public static string NormalizeForLookup(string value)
{
    // Null ve geçersiz Unicode için kontroller gerçek metotta bulunur.
    return value.Trim()
        .Normalize(NormalizationForm.FormC)
        .ToUpper(TurkishCulture)
        .Normalize(NormalizationForm.FormC);
}

public static bool AreSame(string first, string second)
{
    return string.Equals(
        NormalizeForLookup(first),
        NormalizeForLookup(second),
        StringComparison.Ordinal);
}
```

- `Trim`: baştaki ve sondaki boşlukları temizler.
- `FormC`: aynı Unicode metninin birleşik/ayrık yazımlarını ortak biçime getirir.
  Örneğin tek karakter `é` ile `e` + birleştirici vurgu aynı anahtarı üretir.
- `ToUpper(TurkishCulture)`: harfleri Türkçe kurallarla büyütür.
- `Ordinal`: oluşan anahtarları birebir karşılaştırır.

Örnekler:

| İlk ad | İkinci ad | Aynı mı? |
|---|---|---|
| `Plan` | ` plan ` | Evet |
| `I` | `ı` | Evet |
| `İ` | `i` | Evet |
| `I` | `i` | Hayır |
| `IŞIK` | `ışık` | Evet |
| `ISIK` | `ışık` | Hayır; `S` ve `Ş` farklı harfler |
| `ş` | `s` | Hayır |
| `ß` | `ss` | Hayır |

Aksanlar ve farklı harfler kaldırılmaz. Bu açık politika, kültürel bir
karşılaştırıcının başka işaretleri eşdeğer saymasına dayanmaz.

### Repository'de arama değişti

Eski arama:

```csharp
todoList.OwnerUserId == ownerUserId && todoList.Name == name
```

Yeni arama:

```csharp
string normalizedName = NameComparison.NormalizeForLookup(name);

todoList.OwnerUserId == ownerUserId
    && todoList.NormalizedName == normalizedName
```

Aranacak anahtar SQL sorgusundan önce Domain'de üretilir. SQL sorgusu yalnızca
saklanan anahtarı arar; kendi ayrı harf dönüşümünü uygulamaz.

### Veritabanı anahtarları birebir karşılaştırır

SQL Server'da yeni anahtar kolonlarına `Latin1_General_100_BIN2` collation'ı
verildi. Anahtarlar zaten Domain tarafından normalize edildiği için burada
ek bir kültürel eşitlik yorumuna ihtiyaç yoktur. Bu ayar yeni iki anahtar
kolonuna uygulanır.

Eski unique index anahtarları:

```csharp
new { todoList.OwnerUserId, todoList.Name }
new { todoItem.TodoListId, todoItem.Title }
```

şunlarla değişti:

```csharp
new { todoList.OwnerUserId, todoList.NormalizedName }
new { todoItem.TodoListId, todoItem.NormalizedTitle }
```

Liste adı kullanıcıya göre, görev başlığı listeye göre benzersizdir.
Ön kontrol eşzamanlı istekleri kaçırsa bile DB aynı anahtarlı ikinci kaydı
reddeder; mevcut hata çeviricisi bunu conflict olarak API'ye taşır.

## 5. Nerelerde uygulandı?

### Eski kayıtların geçişi

İki migration eklendi:

1. `AddNormalizedNameColumns`: nullable anahtar kolonlarını ekler; eski
   index'ler ve adlar korunur.
2. `EnforceNormalizedNameUniqueness`: anahtarları zorunlu yapar ve unique
   index'leri yeni alanlara taşır.

Bu iki adım arasındaki veriyi `TodoSchemaMigrator` dönüştürür. Mevcut adları
SQL `UPPER` ile değil, yeni kayıtların kullandığı **aynı C# Domain metodu**
ile işler. Çakışan kayıt ID'lerini bildirir; kayıt silmez ve ad seçmez.

Backfill ve ikinci migration aynı transaction içindedir. Tablolar yazmaya
karşı kilitlenir; anahtarlar doldurulur; yeni index'ler kurulur; commit edilir.
Hata halinde bu transaction geri alınır. İlk hazırlık migration'ının eklediği
nullable kolonlar kalabilir; eski kayıtlar ve index'ler korunur. Çakışan
adlar düzeltildikten sonra araç yeniden çalıştırılabilir.

İkinci migration, backfill yapılmadan çalıştırılırsa eski index'i düşürmeden
önce anlaşılır bir hatayla durur. Boş varsayılan anahtar atayıp devam etmez.

### Başlatıcıyla geçiş

Migration aracı `tools/TodoDatabaseMigrator` altında; `TodoApi.sln`'ye eklendi.
`HerSeyiBaslat.bat`, API kapalıysa önce bekleyen migration'ları uygular,
ardından API'yi güncel kodla derleyerek açar. Araç bağlantı ayarını API
klasöründeki yapılandırmadan ve ortam değişkenlerinden okur.

## 6. Neler oldu

- Domain, ön kontrol ve unique index aynı isim eşitliği kuralını kullanıyor.
- Eşzamanlı isteklerde aynı anahtarlı ikinci kaydı veritabanı engelliyor.
- Geçiş sırasında mevcut adlar ve ilişkiler korunuyor; çakışma varsa işlem geri alınıyor.

