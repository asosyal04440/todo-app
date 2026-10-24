# Environment, DI token, CSP ve production

## Asıl amaç

API adresini her serviste ayrı yazmadan geliştirme ve production ortamını
seçebilmek; tarayıcının bağlantı izinlerini de seçilen API ile uyumlu tutmak.

Bu notun önceki sürümü açılışta okunan runtime JSON yaklaşımını anlatıyordu.
Proje artık mentörün örneğindeki build-time environment/fileReplacements
yöntemini kullanıyor. İkisi farklı yöntemlerdir; proxy bunların yerine geçmez.

## Hangi dosya ne yapıyor?

| Dosya                                         | Görevi                                                                    |
| --------------------------------------------- | ------------------------------------------------------------------------- |
| todo.Web/src/environments/environment.ts      | Geliştirme ayarı: production false, apiUrl /api                           |
| todo.Web/src/environments/environment.prod.ts | Production ayarı: production true, apiUrl /api                            |
| todo.Web/angular.json                         | Production build'de environment dosyasını fileReplacements ile değiştirir |
| todo.Web/src/main.ts                          | Seçilen environment'ı doğrular, API adresini DI'ya verir                  |
| todo.Web/src/app/core/config/api-config.ts    | API adresini doğrular; CSP bağlantı kaynağını hesaplar                    |
| todo.Web/src/app/core/config/api-url.token.ts | API_URL anahtarı ve API isteği eşleştirmesi                               |
| todo.Web/proxy.conf.json                      | Geliştirmede /api isteklerini backend 7070'ye iletir                      |
| todo.Web/src/index.html                       | Kaynak CSP; aynı origin ve mevcut Google giriş izinleri                   |
| todo.Web/tools/build.mjs                      | Angular build'i çalıştırır; seçilen ayarla çıktı CSP'sini eşleştirir      |
| todo.Web/tools/configure-deployment.mjs       | Build çıktısının CSP connect-src bölümünü günceller                       |

## Environment seçimi

Servisler environment dosyasını doğrudan import etmez. main.ts temel dosyayı
import eder:

```typescript
import { environment } from "./environments/environment";
```

angular.json production ayarı:

```json
"fileReplacements": [
  {
    "replace": "src/environments/environment.ts",
    "with": "src/environments/environment.prod.ts"
  }
]
```

Bu değiştirme derleme sırasında yapılır. Tarayıcıdaki kullanıcı ortam seçmez.

```text
environment.ts veya environment.prod.ts
  → main.ts, parseApiConfig ile doğrular
  → { provide: API_URL, useValue: config.apiUrl }
  → AuthService/TodoApiService/AdminApiService/interceptor'lar inject(API_URL) ile alır
```

npm start geliştirme, npm run build:production production ayarını kullanır.
npm run build:development geliştirme çıktısı üretir; varsayılan npm run build
production'dır. API adresi değiştiğinde yeniden build gerekir; runtime JSON
ile sonradan adres değiştirme akışı kaldırıldı.

## 4200 ile 7070 aynı şey değil

```text
Tarayıcı → http://localhost:4200/api/todo-lists
  → Angular geliştirme proxy'si
  → http://localhost:7070/api/todo-lists
```

proxy.conf.json frontend portunu değiştirmez. Frontend'in portu ng serve --port
veya angular.json serve ayarıyla belirlenir. Environment ise API adresini belirler.

Production'da /api, uygulamanın açık olduğu origin'in API yoludur. Hosting
sunucusu bu yolu backend'e yönlendirmelidir; Angular dev proxy'si production
hosting'e taşınmaz. Gerçek deployment adresi belirtilmediği için production'da
da /api tercih edilir. Farklı bir API origin'i gerekiyorsa production
environment'ında gerçek adres verilir ve yeniden build alınır.

## CSP bağlantısı

index.html içindeki connect-src 'self', aynı origin /api isteklerine izin verir.
npm run build:production, Angular CLI'den sonra seçilen environment'a göre
çıktının CSP'sini düzenler. API https://api.company.example/api ise bağlantı
izinlerine sadece https://api.company.example eklenir. Eski API origin'i birikmez.
Google giriş izinleri korunur; connect-src * kullanılmaz.

Doğrudan ng build yalnızca Angular derlemesini yapar; harici API origin'i için
CSP eşleştirmesini de yapan npm run build komutlarını kullan.
Geliştirmede /api proxy'si kullanıldığı için kaynak CSP'deki 'self' yeterlidir.
Geliştirme API'sini doğrudan başka origin'e çevirirsen kaynak CSP'nin de buna
izin vermesi gerekir.

Hosting ayrıca CSP header gönderiyorsa orada da aynı API origin'ine izin gerekir.
Meta ve header politikaları birlikte uygulanır. CORS ise farklı bir kontroldür:
backend'in hangi frontend origin'ine yanıtını okutacağıyla ilgilidir.

Environment dosyaları frontend paketine girer; veritabanı parolası veya JWT
imzalama anahtarı bu dosyalarda tutulmaz.

## Kontroller

```powershell
npm run test:deployment
npm test -- --watch=false
npm run build:development
npm run build:production
npm run test:lazy
```

Son testin Network kanıtı ve manuel F12 adımları todo.Web/README.md içinde.
Lazy loading: ilk route geçişinde JS chunk yüklenir; aynı SPA oturumundaki
ikinci geçişte yüklü modül tekrar indirilmez.

Kaynaklar: [Angular environments](https://angular.dev/tools/cli/environments),
[Angular proxy](https://angular.dev/tools/cli/serve),
[CSP connect-src](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Security-Policy/connect-src).
