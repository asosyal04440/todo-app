# RxJS, switchMap ve race condition:

## 1. Uygulamada gerçek sorun neydi?

A listesi için başlatılan yavaş istek, B seçildikten sonra sonuçlanıp B'nin görevlerini
ezebilirdi. Önceki sayaç eski cevabı reddediyordu ama HTTP okumasını durdurmuyordu.

### Race condition nedir?

A'ya tıkladın, sonra B'ye. A yavaş, B hızlı. Cevap sırası B → A olunca son
gelen A ekrandaki B'yi ezebilir. “En son cevap” ile “en son istediğin liste”
aynı şey değildir. Önceden todoLoadVersion sayacı eski cevabı reddediyordu;
HTTP isteği yine sürüyordu. Şimdi aboneliği gerçekten bırakıyorum.

## 2. Eklediğim parçalar

TodoFacade'a seçim/yenileme komut akışı ve fetchTodoPage ekledim. switchMap, catchError,
finalize ve takeUntilDestroyed ile okuma isteğinin yaşam döngüsünü yönetiyorum.

## 3. Kaldırdığım parçalar

### Değiştirdiğim kod

TodoFacade'da seçim/yenileme komut akışı, fetchTodoPage, switchMap, catchError,
finalize ve kapsam kapanışı eklendi. Eski todoLoadVersion sayacı ve yalnızca
cevabı kontrol eden loadTodos gövdesi kaldırıldı. Diğer metotların await
sözleşmesi korundu. Hem yanlış veri gösterme hem gereksiz eski okumayı sürdürme
sorunu çözüldü; yüklenen listeye önceki listenin görevleriyle işlem yapılmıyor.

## 4. Kodun adım adım akışı

### Observable / Subject / switchMap

Observable, zaman içinde değer/cevap gönderebilen kaynaktır. HttpClient'in
get metodu Observable verir. Abone olmak isteği başlatır; aboneliği bırakmak
HttpClient tarafında devam eden okumayı iptal eder.

Subject'e bir komut gönderiyorum: liste ID'si ve sayfa numarası. switchMap
her yeni komutta önce önceki iç aboneliği kapatıp yenisine geçer:

```typescript
this.todoLoads.pipe(
  switchMap(command => command === null ? EMPTY : defer(() => {
    this.todoLoadState.set('loading');
    this.todos.set([]);
    return this.fetchTodoPage(command.listId, command.pageNumber);
  }).pipe(/* sonucu kaydet, hatayı ele al, işi bitir */)),
  takeUntilDestroyed(destroyRef),
).subscribe();
```

defer, hazırlığı gerçek abonelik başladığında yapar. EMPTY, veri göndermeden
biten akıştır; seçili liste kalmayınca mevcut okumayı iptal etmek için kullanılır.

### Hata olunca sonraki seçim çalışıyor mu?

catchError iç isteğin içinde. Bu isteği bitirip EMPTY döndürür; bütün seçim
akışı ölmez. Sunucu bir kez 503 verirse sonraki liste seçimi yine çalışır.
catchError dışarıda olsaydı bütün Subject aboneliğini bitirme riski vardı.

finalize başarı, hata veya iptalde çalışır. Komutun Promise'ını bitirir;
eski çağrıyı await eden use case/sayfa sonsuza kadar beklemez. take(1), HTTP
sözleşmesindeki tek sayfa yanıtını alır; Subject complete edilmeden de
istek işi biter. Son sayfa silinmişse önceki sayfaya aynı akış içinde gidilir.

takeUntilDestroyed, sayfa/facade kapsamı kapandığında aktif okumayı bırakır.
Kapanmış facade'a yeni loadTodos çağrısı HTTP başlatmaz.

## 5. Nerelerde uygulandı?

### Yazma işlemlerini de switchMap ile iptal ettim mi?

Hayır. Sunucuya gitmiş bir INSERT/DELETE'in aboneliğini kapatmak işlemi geri
almaz. Yazma mevcut akışla tamamlanır. Ama cevabı ekrana uygulamadan önce
`this.selectedList()?.id === todoListId` kontrolü yapılır. A'nın geç tamamlanan
güncellemesi B'nin satırlarını/formunu değiştirmez.

## 6. Ne kazandım?

Yeni seçimde eski okuma iptal ediliyor ve önceki listenin satırları temizleniyor. Hata
sonraki seçimleri durdurmuyor; iptal edilen çağrının Promise'ı da tamamlanıyor.

Kaynak: [RxJS switchMap](https://rxjs.dev/api/index/function/switchMap).
