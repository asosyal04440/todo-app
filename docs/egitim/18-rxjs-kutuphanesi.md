# RxJS nedir, hangi parçaları var, biz hangilerini kullandık?

Güncelleme: 27 Ekim 2026. Bu not, test dosyaları hariç güncel frontend koduna göre hazırlanmıştır.

**Önce `exhaustMap`, `groupBy` ve `mergeMap` ekledik. Sonrasında liste/sayfa seçimi için `combineLatest` ve `distinctUntilChanged`, ekranı yenilemek için `forkJoin` kullandık.**

## 1. Konunun can damarı

**RxJS, zaman içinde gelen olayları ve verileri nasıl işleyeceğimizi düzenleyen JavaScript kütüphanesidir.**

Bizim uygulamada:

```text
Liste seçtin → API isteği başladı → cevap geldi → görevler ekrana yazıldı

Bu sırada başka liste seçersen önceki isteğe ne olacak?
API hata döndürürse ne yapılacak?
Aynı görevin kutusuna üç kere tıklarsan kaç istek gönderilecek?
```

RxJS bu akışların nasıl ilerleyeceğini yönetmemize yardımcı olur. HTTP isteğini Angular'ın `HttpClient` servisi gönderir; onun döndürdüğü akışı RxJS ile işleriz.

**RxJS veritabanındaki değişikliği kendiliğinden öğrenmez.** Yeni bilgiyi almak için API isteği, WebSocket mesajı veya başka bir veri kaynağı gerekir.

## 2. Önce dört temel kelime

| Parça             | Basit anlamı                                                                     |
| ----------------- | -------------------------------------------------------------------------------- |
| `Observable`      | Değerlerin veya olayların gelebileceği akış. HTTP servislerimiz bunu döndürür.   |
| `.subscribe(...)` | “Bu akışı dinlemeye başla.” Angular HTTP akışına abone olmak isteği başlatır.    |
| `.pipe(...)`      | “Akışa uygulanacak işlemleri sırayla bağla.” Tek başına HTTP isteğini başlatmaz. |
| `.next(veri)`     | Subject üzerinde “Bu veriyi şu an dinleyenlere gönder.”                          |

Küçük bir örnek:

```typescript
import { map, of, tap } from "rxjs";

of(100)
  .pipe(
    map((fiyat) => fiyat * 0.9),
    tap((sonFiyat) => console.log("Hesaplandı:", sonFiyat)),
  )
  .subscribe((sonFiyat) => console.log("Sonuç:", sonFiyat));
```

Burada `of` 100 değerini yayımlar. `map` bunu 90'a dönüştürür. `tap` 90'ı değiştirmeden loglar. `subscribe` sonucu alır.

Bir aboneliğin `.unsubscribe()` metodu da dinlemeyi kapatır. Projemizde sayfanın ömrüne bağlı abonelikleri çoğunlukla Angular'ın `takeUntilDestroyed` yardımcısıyla temizliyoruz.

## 3. Güncel projede kullandığımız RxJS parçalarının tamamı

Aşağıdaki **22 parça**, uygulama kaynak kodundaki `rxjs` import'larından kontrol edildi. Hepsi fonksiyon değildir: `Observable`, `Subject` ve `BehaviorSubject` sınıf, `EMPTY` ise sabittir.

| Parça                   | Ne yapar?                                                                           | Bizde nerede, ne için kullanılıyor?                                                                                    |
| ----------------------- | ----------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| `Observable`            | Veri veya olay akışını temsil eder.                                                 | Auth, Todo ve Admin HTTP servislerinin dönüş tipi.                                                                     |
| `Subject`               | Dinleyenlere veri veya komut yayımlar.                                              | `TodoFacade` içindeki `todoReloads` ve `completionRequests` komut akışları.                                            |
| `BehaviorSubject`       | Başlangıç ve mevcut değeri tutar; yeni dinleyene mevcut değeri verir.               | `selectedListIds` ve `todoPages`, seçilen liste ve istenen sayfayı akışa verir.                                        |
| `combineLatest`         | Her kaynak en az bir değer verince, biri değiştiğinde güncel değerleri birleştirir. | Liste seçimi ve sayfa isteğini birlikte değerlendirir.                                                                 |
| `distinctUntilChanged`  | Bir önceki yayımlanan değerle aynı olanı eler.                                      | Liste ID'si ve sayfa numarası aynıysa normal seçim yeniden HTTP isteği oluşturmaz.                                     |
| `forkJoin`              | Kaynakların hepsi değer verip tamamlanınca son sonuçlarını birlikte verir.          | Yenile butonunda listeleri ve seçili listenin görevlerini paralel okuyup birlikte günceller.                           |
| `merge`                 | Kaynaklardaki olayları bekletmeden aynı akışa aktarır.                              | Normal seçimleri ve açıkça istenen yeniden yüklemeleri aynı `switchMap` akışında toplar.                               |
| `switchMap`             | Yeni değer gelince önceki iç aboneliği bırakıp yenisine geçer.                      | Yeni liste seçiminde eski görev okumasını bırakır. Auth interceptor'da token yenilemeden asıl isteğe geçişi de sağlar. |
| **`exhaustMap` — yeni** | Mevcut iç işlem bitene kadar yeni talepleri yok sayar.                              | Aynı görevin tamamlama isteği sürerken gelen tekrarları eler.                                                          |
| **`groupBy` — yeni**    | Değerleri seçilen anahtara göre ayrı akışlara ayırır.                               | Tamamlama komutlarını `todoId` değerine göre ayırır.                                                                   |
| **`mergeMap` — yeni**   | İç akışları eşzamanlı yürütebilir, sonuçlarını tek akışta toplar.                   | Farklı görev gruplarının aynı anda ilerlemesini sağlar.                                                                |
| `map`                   | Gelen değeri başka bir değere dönüştürür.                                           | `AuthService.refreshSession` içinde API cevabından `Session` üretir.                                                   |
| `tap`                   | Gelen değeri değiştirmeden ek işlem yapar.                                          | Giriş cevabı gelince oturumu başlatır; görev cevabı gelince ekranın state'ini günceller.                               |
| `catchError`            | Hatayı yakalar ve devam edilecek akışı döndürür.                                    | API hatalarını işler; yenileme hatasında `of(null)`, bazı görev hatalarında `EMPTY` döndürür.                          |
| `finalize`              | Tamamlanma, hata veya abonelik kapanışında temizlik yapar.                          | Yükleme işlemini bitirir, görev meşgul bilgisini kaldırır ve devam eden token yenileme kaydını temizler.               |
| `shareReplay(1)`        | Kaynağı paylaşır; son bir değeri yeni dinleyene de verir.                           | O anki token yenilemesini bekleyenlerin aynı HTTP isteğini paylaşmasını sağlar.                                        |
| `take(1)`               | İlk değeri alıp akışı tamamlar.                                                     | Görev okuma ve tamamlama isteklerinde tek sonuç alır.                                                                  |
| `of(...)`               | Verdiğimiz değerleri yayımlayıp biten akış oluşturur.                               | `of(null)` ile başarısız oturum yenilemesini bildirir; alınmış sayfa verisini akış olarak döndürür.                    |
| `EMPTY`                 | Hiç değer yayımlamadan tamamlanan akıştır.                                          | Seçim kalmadığında veya iç işlem hatası ele alındığında o iç akışı bitirir.                                            |
| `defer`                 | Verilen fonksiyonu abone olma anında çalıştırır.                                    | İstek hazırlığını ve ilgili yükleniyor/meşgul state'ini gerçek abonelik başladığında oluşturur.                        |
| `throwError(...)`       | Abone olunduğunda hata bildiren akış oluşturur.                                     | Interceptor'larda hatayı devamındaki kodlara iletir.                                                                   |
| `firstValueFrom(...)`   | Abone olup ilk değeri Promise olarak verir; sonra aboneliği kapatır.                | Login, admin, oturum ve CRUD işlemlerinde API cevabını `await` ile beklememizi sağlar.                                 |

### Birbirine benzeyenleri ayır

- **`map`:** “Gelen değerden başka bir değer üret.”
- **`tap`:** “Değer geçerken bir iş daha yap; yayımlanan değeri değiştirme.”
- **`of(null)`:** “Bir tane `null` değeri gönder, sonra bitir.”
- **`EMPTY`:** “Hiç değer gönderme, bitir.”
- **`throwError`:** “Hata bildir.”
- **`firstValueFrom`:** “Akışın ilk sonucunu `await` ile bekleyebileceğim Promise'a çevir.” Akış hata verirse Promise da reddedilir.

## 4. Subject ve BehaviorSubject farkı

**Can damarı: sonradan dinlemeye başlayana mevcut değerin verilip verilmemesi.**

| Özellik                                                       | `Subject`                    | `BehaviorSubject`                             |
| ------------------------------------------------------------- | ---------------------------- | --------------------------------------------- |
| Başlangıç değeri ister mi?                                    | Hayır.                       | Evet.                                         |
| Önceden gönderilmiş son değeri yeni dinleyene hemen verir mi? | Hayır.                       | Evet, aktif akışın mevcut değerini verir.     |
| Sonraki değerleri dinleyenlere iletir mi?                     | Evet.                        | Evet.                                         |
| Biz kullanıyor muyuz?                                         | Evet, komut yayımlamak için. | Evet, liste/sayfa isteklerini birleştirirken. |

```typescript
import { BehaviorSubject, Subject } from "rxjs";

const mesaj = new Subject<string>();
mesaj.next("Ev");
mesaj.subscribe((deger) => console.log("Subject:", deger));
// Burada “Ev” gelmez; gönderildiğinde henüz dinlemiyorduk.
mesaj.next("İş");
// Subject: İş

const seciliListe = new BehaviorSubject<string>("Ev");
seciliListe.next("İş");
seciliListe.subscribe((deger) => console.log("BehaviorSubject:", deger));
// Hemen BehaviorSubject: İş
```

Bizde ekranda gösterilecek durumu `signal` ve `computed` tutuyor. `Subject`, “yeniden yükle” veya “şu görevi tamamla” gibi komutları iletiyor. `BehaviorSubject` ise liste seçiminin ve sayfa isteğinin mevcut değerlerini `combineLatest` için hazır tutuyor.

**`signal` ve `computed`, RxJS değil Angular parçalarıdır.**

## 5. Yeni eklediğimiz exhaustMap akışı

### Önce sorun neydi?

TodoRow'daki tamamlama kutusuna tekrar tekrar tıklamak birden fazla PATCH isteği oluşturabiliyordu. Bir görevin cevabı beklenirken aynı görevi düzenlemek de çakışan işlemler başlatabilirdi.

### Şimdi hangi dosya ne yapıyor?

```text
TodoRow: Kullanıcı kutuya tıklar, completionRequested olayını gönderir.
  → TodoListView: Olayı TodoPage'e iletir.
  → TodoPage: facade.setTodoCompletion(...) çağırır.
  → TodoFacade: completionRequests Subject'ine komut gönderir.
  → groupBy: Komutu görev ID'sine ait gruba yönlendirir.
  → mergeMap: Farklı görev gruplarını birlikte dinler.
  → Her grubun exhaustMap'i: Meşgulse tekrarı eler, boşsa API isteğini başlatır.
```

Örnek:

```text
Görev 9'a tıkladın → PATCH başladı.
Görev 9'a tekrar tıkladın → işlem sürüyor, tekrar yok sayıldı.
Görev 10'a tıkladın → onun PATCH isteği başlayabilir.
Görev 9'un işlemi bitti → yeni tıklama kabul edilebilir.
```

Bu yüzden üçü birlikte var:

- **`groupBy`:** Hangi görevin isteği olduğunu ayırır.
- **`mergeMap`:** Farklı görevlerin birbirini beklemeden ilerlemesini sağlar.
- **`exhaustMap`:** Her görevde aynı anda tek tamamlama isteği yürütür. Arada elenen talepleri kuyruğa almaz.

### Kutuyu kim pasifleştiriyor?

`TodoFacade.completingTodoIds`, işlemde olan görev ID'lerini tutan bir signal'dır.

```text
completingTodoIds
  → TodoPage
  → TodoListView
  → TodoRow'un completing input'u
  → O görevin kutusu ve düzenle/sil butonları pasif olur.
```

Başarı veya hatadan sonra `finalize` meşgul bilgisini kaldırır. Hata yakalama iç isteğin içinde olduğu için bir hata, sonraki tamamlama komutlarını dinleyen akışı kapatmaz. Sayfa kapanınca `takeUntilDestroyed` abonelikleri temizler.

## 6. switchMap, exhaustMap, concatMap, mergeMap nasıl seçilir?

| İstediğim davranış                          | Seçenek      | Örnek                                     | Bizde durum                |
| ------------------------------------------- | ------------ | ----------------------------------------- | -------------------------- |
| “Son seçim geçerli; önceki iç akışı bırak.” | `switchMap`  | A listesinden B listesine geçmek.         | Kullanılıyor.              |
| “İşlem bitene kadar tekrarları kabul etme.” | `exhaustMap` | Aynı görevi tamamlamaya art arda basmak.  | Görev başına kullanılıyor. |
| “Her talep yapılsın, sırayla ilerlesin.”    | `concatMap`  | Toplu kayıtları sırayla göndermek.        | Henüz kullanılmıyor.       |
| “İşlemler birlikte ilerleyebilsin.”         | `mergeMap`   | Farklı görev gruplarını birlikte işlemek. | Kullanılıyor.              |

**`switchMap` gecikme olduğu için kendiliğinden yeni isteğe geçmez.** Kaynaktan yeni değer gelmesi gerekir. Yeni değer gelmezse mevcut akışı dinlemeye devam eder.

**Tarayıcı isteğini iptal etmek, backend'deki yazmayı geri almak değildir.** Sunucuda başlamış kayıt işlemi devam edebilir. `exhaustMap` de backend'de kayıtları geri almaz; istemcideki tekrar taleplerini yönetir.

## 7. Liste seçimi, sayfalama ve Yenile butonu

```text
selectedListIds + todoPages
  → combineLatest: hangi liste, hangi sayfa?
  → Listeye ait olmayan eski sayfa isteğini ele.
  → distinctUntilChanged: aynı liste + sayfa tekrarını ele.
  → merge: açıkça istenen yeniden yüklemeleri de bu akışa kat.
  → switchMap: yeni okuma gelince eskisini bırak.
```

`todoPages` içinde sayfa numarasıyla birlikte liste ID'si de vardır. Böylece A listesinin 2. sayfasından B listesine geçerken B'nin 2. sayfasına gereksiz istek gitmez; doğrudan B'nin 1. sayfası istenir. Aynı seçili listeye tekrar tıklamak mevcut sayfayı korur.

**Yenile ve CRUD sonrası yeniden okuma, aynı sorgu olsa da çalışır.** Bunlar `todoReloads` üzerinden tekrar elemesini aşar. Hata sonrası aynı listeyi seçerek tekrar denemek de mümkündür.

Yenile butonuna basınca `forkJoin` iki okumayı başlatır:

```text
GET /api/todo-lists?pageNumber=...          → soldaki listeler
GET /api/todo-lists/5/todos?pageNumber=...  → seçili listenin görevleri
                ↓ ikisi de bitince
          iki bölümü birlikte güncelle
```

Beklerken eski veriler görünür; değiştirme butonları pasiftir. İsteklerden biri hata verirse eski veriler korunur ve hata gösterilir. Seçili liste başka bir yerden silinmişse kalan uygun listeye geçilir; hiç liste kalmadıysa ekran boşaltılır. Sayfa aralığı küçülmüşse son geçerli sayfa okunur.

İlk açılışta seçilecek liste henüz bilinmediği için önce listeler, sonra o listenin görevleri alınır. `forkJoin` burada kullanılmaz. Yenile de sunucuda tek bir veritabanı anlık görüntüsü garantisi vermez; iki HTTP cevabının ekrana birlikte aktarılmasını sağlar.

## 8. Kütüphanede bulunan, uygulamada henüz kullanmadığımız diğer yaygın parçalar

Bu bölüm bir ihtiyaç listesi değildir. Bunlar RxJS'te bulunan başka araçlar; uygun bir özellik eklenirse değerlendirilebilir.

| Parça           | Ne yapar?                                                                             | Bizde olası kullanım                                                            |
| --------------- | ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| `ReplaySubject` | Ayarlanan geçmiş değerleri yeni dinleyene tekrar verir.                               | Son birkaç bildirimi sonradan açılan bir panele aktarmak.                       |
| `filter`        | Koşulu sağlayan değerleri geçirir.                                                    | Bir olay akışında yalnızca belirli olayları işlemek.                            |
| `debounceTime`  | Yeni değer gelmeden belirli süre geçmesini bekleyip son değeri verir.                 | Arama eklersek, kullanıcı yazmayı 300 ms bıraktığında aramak.                   |
| `concatMap`     | İç işlemleri sıraya koyar; biri tamamlanınca diğerine geçer.                          | Tümünün yapılması gereken toplu kayıtları sırayla göndermek.                    |
| `retry`         | Hata veren kaynağa yeniden abone olarak tekrar dener.                                 | Geçici bağlantı hatalarında GET isteğini sınırlı sayıda denemek.                |
| `timeout`       | Belirlenen süre koşulu aşılırsa hata bildirir.                                        | Uzun süre cevap gelmeyen isteği hata durumuna geçirmek.                         |
| `from`          | Promise veya dizi gibi kaynakları Observable'a çevirir.                               | Promise döndüren bir işlemi RxJS akışına katmak.                                |
| `fromEvent`     | DOM tıklaması gibi olaylardan akış oluşturur.                                         | Doğrudan bir tarayıcı olayını dinlemek gerektiğinde.                            |
| `timer`         | Belirlenen bekleme süresinden sonra değer yayımlar; isteğe bağlı tekrar da yapabilir. | Gecikmeli bir kontrol başlatmak.                                                |
| `interval`      | Belirli aralıklarla değer yayımlar.                                                   | Gerekiyorsa düzenli veri yenilemek. Aboneliği sayfa ömrüyle temizlemek gerekir. |

`retry`, yanlış veriyi düzeltmez: 400 doğrulama hatasına aynı veriyi yeniden göndermek çözüm değildir. Yazma isteklerini otomatik tekrarlamak mükerrer işlem oluşturabilir.

## 9. Angular yardımcısı: takeUntilDestroyed

```typescript
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
```

Bu, `rxjs` paketinin operatörü değildir; Angular'ın RxJS ile çalışmayı kolaylaştıran yardımcısıdır.

Bizde TodoFacade'ın bağlı olduğu TodoPage kapandığında aktif abonelikleri kapatır. Böylece eski sayfanın istekleri dinlenmeye devam etmez. Bu kapanış backend'de yapılmış bir yazmayı geri almaz.

## 10. Dosyaları nereden açarım?

| Dosya                                                                                                         | Bakılacak bölüm                                                                                                |
| ------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| [todo.facade.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo.facade.ts)                | `selectedListIds`, `todoPages`, `todoReloads`, `completionRequests`, `refreshWorkspace`, `fetchWorkspacePage`. |
| [auth.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/auth/auth.service.ts)                   | `login`, `refreshSession`, `restoreSession`, `logout`.                                                         |
| [auth.interceptor.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/http/auth.interceptor.ts)           | Token ekleme, yenileme ve isteği tekrar gönderme.                                                              |
| [api-error.interceptor.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/core/http/api-error.interceptor.ts) | `catchError` ile hatayı işleyip `throwError` ile iletme.                                                       |
| [todo-api.service.ts](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-api.service.ts)      | `HttpClient` çağrıları ve `Observable` dönüş tipleri.                                                          |
| [todo-page.html](C:/Users/Bahadir/Desktop/todo/todo.Web/src/app/features/todos/todo-page.html)                | Olayların facade'a bağlanması ve meşgul görevlerin görünüm component'lerine aktarılması.                       |

## 11. Kısa tekrar

```text
Observable        → Akış.
Subject.next      → Komutu/veriyi yayımla.
BehaviorSubject   → Mevcut değeri de tut.
combineLatest     → Değişen kaynakların güncel değerlerini birleştir.
distinctUntilChanged → Aynı sorguyu tekrar geçirme.
forkJoin          → Hepsi bitince sonuçları birlikte ver.
merge             → Seçim ve yenileme olaylarını aynı akışa al.
subscribe         → Dinlemeye başla.
pipe              → İşlemleri bağla.
switchMap         → Yeni gelince önceki iç akışı bırak.
exhaustMap        → Meşgulken tekrarları yok say.
groupBy           → Görevleri ID'lerine göre ayır.
mergeMap          → Farklı görevlerin akışlarını birlikte yürüt.
map               → Gelen değeri dönüştür.
tap               → Değeri değiştirmeden ek işlem yap.
catchError        → Hatayı ele al, yerine bir akış döndür.
finalize          → Tamamlanma, hata veya kapanışta temizle.
shareReplay(1)    → Kaynağı paylaş, son bir değeri de sakla.
take(1)           → İlk değeri alıp tamamla.
of                → Değer yayımlayan akış oluştur.
EMPTY             → Değer yayımlamadan tamamla.
defer             → Fonksiyonu abone olununca çalıştır.
throwError        → Hata bildiren akış oluştur.
firstValueFrom    → İlk değeri Promise olarak bekle.
```

## Kaynaklar ve kapsam

Bu not RxJS'in tüm API'sinin eksiksiz dökümü değildir. Güncel projede kullandığımız 22 RxJS parçasının tamamını, temel metotları ve diğer yaygın araçları kapsar. Tam katalog için resmî API listesini kullan.

- [RxJS API kataloğu](https://rxjs.dev/api)
- [RxJS giriş rehberi](https://github.com/ReactiveX/rxjs/blob/7.8.2/docs_app/content/guide/overview.md)
- [Operatörler ve kategorileri](https://github.com/ReactiveX/rxjs/blob/7.8.2/docs_app/content/guide/operators.md)
- [exhaustMap kaynak kodu ve açıklaması](https://github.com/ReactiveX/rxjs/blob/7.8.2/src/internal/operators/exhaustMap.ts)
- [groupBy kaynak kodu ve açıklaması](https://github.com/ReactiveX/rxjs/blob/7.8.2/src/internal/operators/groupBy.ts)
- [mergeMap kaynak kodu ve açıklaması](https://github.com/ReactiveX/rxjs/blob/7.8.2/src/internal/operators/mergeMap.ts)
- [BehaviorSubject kaynak kodu ve açıklaması](https://github.com/ReactiveX/rxjs/blob/7.8.2/src/internal/BehaviorSubject.ts)
- [combineLatest kaynak kodu ve açıklaması](https://github.com/ReactiveX/rxjs/blob/7.8.2/src/internal/observable/combineLatest.ts)
- [distinctUntilChanged kaynak kodu ve açıklaması](https://github.com/ReactiveX/rxjs/blob/7.8.2/src/internal/operators/distinctUntilChanged.ts)
- [forkJoin kaynak kodu ve açıklaması](https://github.com/ReactiveX/rxjs/blob/7.8.2/src/internal/observable/forkJoin.ts)
- [firstValueFrom kaynak kodu ve açıklaması](https://github.com/ReactiveX/rxjs/blob/7.8.2/src/internal/firstValueFrom.ts)
- [Angular takeUntilDestroyed](https://angular.dev/api/core/rxjs-interop/takeUntilDestroyed)
