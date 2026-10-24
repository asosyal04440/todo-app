# Signals, computed ve zoneless state:

## 1. Uygulamada gerçek sorun neydi?

TodoFacade normal alanları değiştiriyor, ekranı güncellemek için ayrıca Subject
bildirimi ve markForCheck kullanıyordu. Bir bildirim unutulursa veri değişse bile
zoneless ekran eski değeri gösterebilirdi.

## 2. Eklediğim parçalar

Facade state'ini signal yaptım. TodoListView ve TodoListHeader görev input'larını
signal'a çevirdim; tamamlanan görev sayısını computed ile türettim. Liste/görev yükleme
durumları için idle, loading, loaded ve error değerlerini ekledim.

## 3. Kaldırdığım parçalar

- TodoFacade.changes Subject'i ve bütün changes.next çağrıları.
- TodoPage içindeki abonelik, takeUntilDestroyed ve ChangeDetectorRef.
- İki görünüm component'inin her çağrıda filtre yapan completedCount metotları.

TodoPage template'i artık `[todos]="facade.todos()"` okuyor. Olaylar da
`facade.isCreatingTodo.set(true)` gibi state'i değiştiriyor. Mevcut login/admin
ekranlarının signal state'iyle aynı reaktivite yaklaşımını kullanıyorum.

Signal fonksiyonu iki kez okununca TypeScript ilk null kontrolünü ikinci okumaya
taşımaz. İşlem başında `const selectedList = this.selectedList()` alıyorum;
null kontrolünden sonra o işlem için seçilen listeyi kullanıyorum. Bu ayrıca
await sırasında liste seçimi değişirse hangi listeye işlem yaptığımı açık tutar.

## 4. Kodun adım adım akışı

### Signal nedir?

Normal değişkeni deftere yazılmış sayı gibi düşün. Sayı değişir ama defteri
okuyan kişi bunu kendiliğinden öğrenmez. Signal ise değiştiğinde onu okuyan
ekrana haber veren bir değerdir. Angular template'te signal okunduğunu takip eder.

Önce:

```typescript
todos: TodoItem[] = [];
this.todos = page.items;
this.changes.next(); // ayrıca sayfa markForCheck yapıyordu
```

Şimdi `features/todos/todo.facade.ts`:

```typescript
readonly todos = signal<TodoItem[]>([]);
this.todos.set(page.items);
```

`todos()` okumak; `todos.set(yeniDeger)` değiştirmek demektir. `readonly`,
signal nesnesini başka bir nesneyle değiştirmeyi engeller; set kullanmayı engellemez.
Sayaçta `listCreationVersion.update(value => value + 1)` mevcut değerden yenisini üretir.

### Computed ne işe yarıyor?

Bir değeri başka değerlerden hesaplıyorum:

```typescript
readonly completedCount = computed(
  () => this.todos().filter(todo => todo.isCompleted).length,
);
```

TodoListView ve TodoListHeader'da görev input'u signal oldu. Görevler değişince
hesap bağımlılığı değişir; tamamlanan sayısı yeniden hesaplanır. Ayrı bir sayacı
elle artırıp azaltmadığım için görev verisiyle sayaç birbirinden kopmaz.

## 5. Nerelerde uygulandı?

### Yükleme durumu

Facade'da `listLoadState` / `todoLoadState` için idle, loading, loaded, error var.
`isLoadingLists` / `isLoadingTodos`, bu durumdan computed ile türetiliyor.
Ekranın yükleniyor sorusuna ayrı, bağımsız bir boolean tutarak cevap vermiyorum.
Liste kaydetme ve görev kaydetme ayrı işlemler olduğundan kendi signal'ları var.

## 6. Ne kazandım?

Asenkron işlem sonunda ekranı ayrıca dürtmeyi unutma riski kalktı. State'in
okunması, değiştirilmesi ve türetilmesi kodda görünür. Dizileri map/set ile yeni
değer olarak değiştiriyorum; array.push ile signal'ın haber vermediği mutasyon yapmıyorum.

Kaynak: [Angular Signals](https://angular.dev/guide/signals).
