# Todo Web

Bu klasör, Angular 21, TypeScript ve NG-ZORRO ile hazırlanmış web arayüzüdür. Verileri doğrudan SQL Server'dan okumaz; Todo API'ye HTTP/JSON istekleri gönderir.

## Çalıştırma

Projenin ana klasöründeki `HerSeyiBaslat.bat` dosyasını aç. Bu dosya SQL Server Express'i kontrol eder, API'yi ve Angular geliştirme sunucusunu başlatır; hazır olunca tarayıcıda `http://localhost:4200` adresini açar.

## Testler

.NET katman testlerini ana proje klasöründe çalıştır:

```powershell
dotnet test --solution .\TodoApi.sln --configuration E2E --no-restore
```

Angular bileşen ve HTTP servis testlerini `todo.Web` klasöründe çalıştır:

```powershell
npm test -- --watch=false
```

Tam uçtan uca testler Chromium tarayıcısında web arayüzü, gerçek .NET API ve SQL Server Express ile çalışır. İlk kullanımdan önce tarayıcıyı bir kez kur:

```powershell
npm run test:e2e:install
```

Ardından `todo.Web` klasöründe testi başlat:

```powershell
npm run test:e2e
```

Uçtan uca test komutu kendi geçici SQL veritabanını oluşturur, test API'sini `17070` portunda ve web arayüzünü `4201` portunda başlatır; çalışma bitince geçici veritabanını siler. Normal uygulama portlarındaki `7070` ve `4200` sunucularına veya onların veritabanına dokunmaz. SQL Server Express servisi açık olmalıdır.

## Önemli dosyalar

- `src/app/app.html`: sayfanın HTML görünümü ve kullanıcı etkileşimleri.
- `src/app/app.ts`: ekranda tutulan durum ve kullanıcı işlemleri.
- `src/app/services/todo-api.service.ts`: API'ye gönderilen HTTP istekleri.
- `src/app/models/todo.models.ts`: Angular ile API arasındaki JSON veri şekilleri.
- `src/app/app.config.ts`: Angular servisleri, HTTP istemcisi ve Türkçe yerel ayarlar.

Örnek bir görev ekleme akışı: form `app.ts` içindeki `createTodo` metodunu çağırır; bu metot `TodoApiService.createTodo` metoduna gider; servis `POST /api/todo-lists/{todoListId}/todos` isteğini gönderir. İsteğin devamı `todo.API` controller'ı, Application kullanım senaryosu, repository ve SQL Server üzerinden ilerler.
