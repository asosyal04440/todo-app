# Routing ve lazy loading:

## 1. Uygulamada gerçek sorun neydi?

app.routes.ts üç ekranı normal import ederek uygulamanın ilk paketine alıyordu. Yalnızca
giriş ekranını kullanan kişi görev ve admin ekranlarının kodunu da indiriyordu.

### Router ne yapıyor?

Adres çubuğundaki URL'yi ekrana bağlar: /login giriş, /admin/users kullanıcı
yönetimi, / görev ekranı. Guard, o ekrana girmeye izin olup olmadığını kontrol eder.

## 2. Eklediğim parçalar

Auth, todos ve admin feature'ları için ayrı route dosyaları ekledim. Kök route'ta
loadChildren, feature route'larında loadComponent kullanıyorum.

### Route dosyalarındaki değişiklikler

auth.routes.ts, todos.routes.ts, admin.routes.ts eklendi. Kök dosyanın üç
eager sayfa import'u ve component alanları kaldırıldı. Yerlerini loadChildren
aldı; feature route'larında loadComponent kullanıldı.

authGuard, guestGuard, adminGuard; title, pathMatch ve wildcard yönlendirmesi
korundu. Lazy yükleme erişim kontrolünün yerine geçmez; API de yetki kontrolünü
yapmaya devam eder. Feature'lar diğer feature'ı import etmiyor.

## 3. Kaldırdığım parçalar

Kök dosyanın üç eager sayfa import'unu ve component alanını kaldırdım. Bunları dinamik
import kullanan lazy yükleme fonksiyonlarıyla değiştirdim.

## 4. Kodun adım adım akışı

### Lazy ne demek?

Bir kitabın bütün bölümlerini başta taşımanın yerine okuyacağın bölümü açınca
almak gibi. Önceden app.routes.ts üç sayfayı normal import edip component
alanına veriyordu; girişte admin ve görev ekranı da ilk JavaScript içindeydi.

Şimdi kök dosyada:

```typescript
{
  path: 'admin/users',
  loadChildren: () => import('./features/admin/admin.routes')
    .then(module => module.ADMIN_ROUTES),
  canActivate: [adminGuard],
  title: 'Kullanıcılar | Todo',
}
```

`import(...)` dinamik import: bundler ayrı paket üretir. Promise sonuçlandığında
then, yüklenen dosyanın route tanımını alır. loadChildren alt route'ları yükler.

`features/admin/admin.routes.ts`:

```typescript
export const ADMIN_ROUTES: Routes = [
  {
    path: "",
    loadComponent: () =>
      import("./admin-users-page").then((module) => module.AdminUsersPage),
  },
];
```

Buradaki boş path, kökteki /admin/users adresinin devamıdır; yeni bir URL
eklemiyorum. loadComponent ise gerçek sayfa component'ini yükler.

## 5. Nerelerde uygulandı?

Giriş /login, kullanıcı yönetimi /admin/users ve görev ekranı / adresinde kalıyor.
Guest, auth ve admin guard'ları; title, pathMatch ve wildcard yönlendirmesi korundu. API
yetki kontrolleri de çalışmaya devam ediyor.

## 6. Ne kazandım?

### Kazanç ve sınır

İlk JavaScript'te kullanılmayan ekran kodu daha az. İlk kez bir ekrana geçerken
onun paketini indirmek gerekir. Ortak Angular/NG-ZORRO kodu ve global stylesheet
paketi yine ortak kalabilir; lazy loading bütün CSS'i otomatik küçültmez.

Kaynak: [Angular route loading strategies](https://angular.dev/guide/routing/loading-strategies).

## 7. Tarayıcıda nasıl kanıtlarım?

Build ayarındaki `namedChunks: true`, dosyaları Network'te
`admin-users-page-...js` gibi okunur isimlerle gösterir. Bu seçenek tek başına
lazy loading oluşturmaz; lazy sınırı dinamik import kullanan route tanımlarıdır.

1. Admin hesabıyla görev ekranına gir. F12 → Network → Disable cache seç.
2. Görev ekranını tam yenile; JS filtresinde admin-users-page dosyası gelmemeli.
3. Network listesini temizleyip Kullanıcılar'a tıkla: ilk geçişte admin route ve
   component JS dosyaları gelmeli. `/api/admin/users` ise ayrı veri isteğidir.
4. Görevlere dönüp tekrar Kullanıcılar'a gir: yüklenen modül bellekte olduğundan
   tekrar JS isteği beklenmez. Disable cache bu modül belleğini temizlemez.

`npm run test:lazy`, geliştirme ve production çıktısında bu üç durumu temiz
Chromium oturumlarıyla kontrol eder. API yalnızca bu testte taklit edilir;
gerçek veritabanına yazılmaz. JSON Network kanıtı ve ekran görüntüsü
`todo.Web/test-results/lazy-loading` içinde saklanır.
