# Küçük component'ler ve Facade:

## 1. Uygulamada gerçek sorun neydi?

TodoPage hem ekranın tamamını çiziyor hem liste/görev HTTP işlemlerini, iki ayrı formu,
tarih dönüşümlerini ve stilleri yönetiyordu. Bir değişiklik için büyük dosyanın farklı
sorumluluklarına dokunmak gerekiyordu.

### Component ve facade ne demek?

Component, ekranda belirli bir parçanın sorumlusudur. Örneğin TodoRow tek bir
görevin başlığını, önceliğini, tamamlandı kutusunu ve butonlarını gösterir.
Liste oluşturma veya bütün kayıtları sunucudan indirme işi onun değildir.

Facade, daha fazla parçadan oluşan bir işi tek giriş noktasından kullanmamızı
sağlar. Burada görevi kaydetmek isteyen sayfa `facade.createTodo(request)` der.
Hangi listeye kaydedileceği, HTTP çağrısı, sayfalama, başarı/hata mesajları ve
yeniden yükleme facade tarafından yönetilir.

Bir restoran düşün: siparişi alan kişinin hem masayı kurması hem alışverişe
gitmesi hem yemeği pişirmesi gerekmiyor. Her parçanın sorumlusu var; siparişin
izleyeceği yol ise belli. Bu ayrım kodda da nerede çalışacağını bulmayı kolaylaştırır.

### Önce / sonra

Eski TodoPage 515 satırlık TypeScript sınıfında şunları birlikte tutuyordu:

- Liste ve görev HTTP işlemleri, sayfalama ve hata yorumlama.
- Yeni görev ve düzenlenen görev için iki ayrı alan grubu.
- Tarih dönüşümleri, öncelik metinleri/renkleri.
- Oturum menüsü, sidebar ve bütün ekranın HTML/SCSS'i.

Şimdi `todo-page.ts` 41, `todo-page.html` 78, `todo-page.scss` 51 satır.
Kodun tamamı kaybolmadı; her bölüm kendi sorumluluğunun yanına taşındı.

```text
TodoPage — parçaları ve olayları birleştirir
├── WorkspaceLayout — sidebar / ana içerik yerleşimi
├── WorkspaceHeader — başlık, yeni görev, hesap menüsü
├── TodoListSidebar — liste seçimi, oluşturma, liste sayfalama
├── TodoListHeader — seçili listenin adı, yeniden adlandırma, silme
├── TodoForm — yeni görev formu
└── TodoListView — görev listesi, boş/yükleniyor durumları, görev sayfalama
    └── TodoRow — tek görev
        └── TodoForm — aynı formun düzenleme kullanımı

TodoPage → TodoFacade → TodoApiService → API
```

## 2. Eklediğim parçalar

Beş feature component'i, WorkspaceLayout, WorkspaceHeader ve sayfaya özel TodoFacade
ekledim. Görev oluşturma ve düzenleme için aynı TodoForm'u kullanıyorum.

### Parçalar ve sorumlulukları

| Değişiklik | Gerekçe ve kazanç |
|---|---|
| TodoFacade eklendi | Ortak state, CRUD, sayfalama ve API koordinasyonu ekran HTML'inden ayrı yönetiliyor |
| Beş feature component'i eklendi | Menü, başlık, liste, satır ve form ayrı sorumluluklarla bulunuyor |
| WorkspaceLayout / WorkspaceHeader eklendi | Genel yerleşim ve oturum menüsü ayrı yerde |
| İki görev formu tek TodoForm'a dönüştü | Yeni alan eklerken iki ayrı formu değiştirmek gerekmiyor |
| Tarih dönüşümleri todo-date-time.ts'e taşındı | Form dönüşümü API koordinasyonuna karışmıyor |
| Eski sayfa metotları/alanları kaldırıldı | TodoPage doğrudan HTTP, tarih veya form alanı yönetmiyor |
| Stiller component'lere taşındı | Bir parçanın görünümü kendi dosyasında; mobil kuralları yanında |

## 3. Kaldırdığım parçalar

TodoPage içindeki HTTP/CRUD metotlarını facade'a taşıdım. Yeni görev ve düzenlenen görev
için ayrı tutulan alan gruplarını kaldırdım; iki formun HTML'ini tek form component'inde
birleştirdim. Tarih yardımcıları ve component stilleri kendi dosyalarına taşındı.

## 4. Kodun adım adım akışı

### Container / presentational ayrımı

Container, parçaların nasıl bağlanacağını bilir. Bu uygulamada TodoPage container'dır.
Oturumu ve facade'ı alır; hangi verinin hangi component'e gideceğini bağlar.

Presentational component veriyi gösterir ve kullanıcı niyetini olay olarak
dışarı verir. TodoRow, TodoForm, TodoListSidebar gibi component'ler API servisi
enjekte etmez. Böylece bir formu görmek için SQL Server açmak gerekmez.

### Input / Output bağlantısı

TodoRow'da:

```typescript
@Input({ required: true }) todo!: TodoItem;
@Output() readonly deleteRequested = new EventEmitter<TodoItem>();
```

`Input`: üst component'in verdiği görev. `required`, bu component kullanılırken
görev verilmesini zorunlu tutar. `!`, Angular'ın alanı input olarak dolduracağını
TypeScript'e anlatır.

`Output`: “Kullanıcı bu görevi silmek istiyor” olayı. Olayı yaymak SQL'den satır
silmez; işlemi kim yapacaksa ona niyeti bildirir.

```html
<app-todo-row
  [todo]="todo"
  (deleteRequested)="deleteRequested.emit($event)"
/>
```

Köşeli parantezle içeri veri gider; normal parantezle dışarıdan olay dinlenir.
`$event`, olayın taşıdığı değerdir; burada silinmesi istenen TodoItem.
TodoListView, satırın olayını dışarı taşır; sayfa bunu facade'ın deleteTodo
metoduna bağlar. Component'ler birbirlerinin state'ini gizlice değiştirmez.

### Görev oluşturma akışı

`components/todo-form.ts`:

```typescript
this.submitted.emit({
  title: this.title.trim(),
  description: this.description.trim() || null,
  dueAtUtc: toUtcDateTimeValue(this.dueAtLocal),
  priority: this.priority,
});
```

Form, girilen alanları API'nin beklediği TodoRequest şekline çevirir. Hangi
listeye kayıt yapılacağını bilmez; HTTP göndermez.

`todo-page.html`:

```html
<app-todo-form
  [saving]="facade.isSavingTodo"
  (submitted)="facade.createTodo($event)"
  (cancelled)="facade.isCreatingTodo = false"
/>
```

Formun olayı facade'a gider; facade'ın kaydetme durumu forma geri gelir.
Kaydederken form yeniden submit göndermez. Başarılı kayıtta facade formu
kapatır; hata olursa açık bırakır, kullanıcının yazdığı alanlar formda kalır.

`todo.facade.ts` içinde:

```typescript
const todoListId = this.selectedList.id;
await firstValueFrom(this.todoApi.createTodo(todoListId, { ...request, title }));
```

Facade, seçili listenin ID'sini ekleyerek servisi çağırır. Oluşturma sonrasında
son görev sayfasını yükleme, formu kapatma ve mesaj verme de burada yapılır.
TodoApiService ise HTTP endpoint'ini bilir. Facade ve HTTP servisi aynı iş değildir.

### Tek form hem oluşturma hem düzenleme için nasıl çalışıyor?

`TodoForm.todo` boşsa yeni görev; TodoItem verilirse düzenleme formu.
Formdaki `ngOnChanges`, görev input'u değiştiğinde başlık/açıklama/öncelik/tarihi
görevden doldurur. Kaydetme bayrağı değişmesi taslağı yeniden doldurmaz;
başarısız istekte kullanıcının yazdığı metin kaybolmaz.

TodoRow, formun sonucuna orijinal görev bilgisini ekler:

```html
<app-todo-form
  [todo]="todo"
  [saving]="saving"
  (submitted)="updateRequested.emit({ todo, request: $event })"
/>
```

Sayfa `facade.saveTodo($event.todo, $event.request)` çağırır. Facade böylece
hangi görevi hangi yeni bilgilerle güncelleyeceğini bilir.

Eski newTodoTitle / editedTodoTitle, description, dueAtLocal ve priority
alan grupları kaldırıldı. Form component'inde tek alan grubu var. Her form
örneğinin kendi taslağı bulunur; bir form diğerinin metnini değiştirmez.

Form kapatılıp yeniden açıldığında yeni bir form örneği oluşur: yeni görev
taslağı temiz başlar; düzenleme kayıtlı görevden başlar. Eski ekrandaki gizli
taslağı kapatıp tekrar açınca koruma davranışı yerine bu açık yaşam döngüsü seçildi.
Boş görev başlığı ve geçersiz tarih uyarısı form içinde gösteriliyor.

## 5. Nerelerde uygulandı?

### Liste formlarının alanları nerede?

Yeni liste adı TodoListSidebar'da; yeniden adlandırma taslağı TodoListHeader'da.
Facade yalnızca gönderilen adla işlemi yapar. Başarılı liste oluşturmada
`listCreationVersion` artar; sidebar bu input değişince alanını temizler.
Başarısız kayıtta sayı değişmediğinden taslak korunur.

Liste başlığı, yeniden adlandırma başladığında veya seçili liste değiştiğinde
adı doldurur. Liste güncellendiğinde facade hem selectedList'i hem sidebar'ın
todoLists kaydını günceller.

### State başka oturuma/sayfaya sızmasın

TodoFacade global `providedIn: 'root'` ile kayıtlı değildir. TodoPage'de:

```typescript
providers: [TodoFacade],
```

Her açılan görev sayfası kendi facade örneğini alır. Kapanan sayfanın liste
seçimi veya form açma durumu yeni sayfanın başlangıç state'i olmaz. Oturum
bilgisini global AuthService yönetmeye devam eder.

### Zoneless uygulamada ekran nasıl haberdar oluyor?

Bu aşamada facade normal alanları ve `changes` isimli Subject'i kullanıyor.
Asenkron işlem yükleme/kaydetme durumunu veya sonuç verisini değiştirdiğinde
bu akışa bildirim gönderiyor. Sayfa tek yerde dinliyor:

```typescript
this.facade.changes
  .pipe(takeUntilDestroyed())
  .subscribe(() => this.changeDetector.markForCheck());
```

Subject, bildirim yayımlayan akıştır. markForCheck, Angular'a bu sayfanın tekrar
kontrol edilmesini söyler. takeUntilDestroyed, sayfa kapandığında aboneliği
bitirir. Sunum component'leri input'lar üzerinden yeni değerleri alır.

Bu, mevcut düz alanları zoneless ekrana bağlayan geçiştir. Bildirim köprüsünün
signal/computed ile değiştirilmesi, Reactive Forms, ortak hata yönetimi ve RxJS iptal
akışı ayrı notlarda açıklanıyor. Mevcut todoLoadVersion kontrolü ve sayfalama korunuyor.

### Stilleri neden böldüm?

Angular component stilleri kendi template'ine uygulanır. Sayfanın eski CSS'ini
olduğu yerde bırakıp sadece HTML'i child component'e taşımak, görünümü bozar.

Her CSS seçicisi ilgili parçaya taşındı; ortak seçici grupları component
sahipliğine göre ayrıldı. Mobil media kuralları da aynı parçaya taşındı.
WorkspaceLayout yerleşimi; TodoRow görev kartı/checkbox; TodoForm form alanları
ve kendi editor kartı gibi kendi stillerini taşıyor. Stil bütçeleri yükseltilmedi.

Core layout, todos feature'ını import etmiyor. Listeye özgü sidebar ise
features/todos içinde kaldı; genel yerleşim ile liste işlemlerini karıştırmıyorum.

## 6. Ne kazandım?

Sayfa yalnızca component'leri ve olayları bağlıyor. Formu veya görev satırını
bağımsız geliştirebiliyorum; HTTP akışını facade içinde yönetiyorum.

Angular kaynakları:
[Input properties](https://angular.dev/guide/components/inputs),
[component'e özel provider](https://angular.dev/guide/di/defining-dependency-providers).
