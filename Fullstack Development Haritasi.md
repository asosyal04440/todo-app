## Genel Fullstack Sistem Haritası

```mermaid
flowchart TB
  U(["Kullanıcı"])

  subgraph CLIENT["İstemci cihazı"]
    direction LR
    subgraph BROWSER["Tarayıcı"]
      FE["Frontend<br/>React · Vue · Angular"]
    end
    subgraph MOBILE["Mobil cihaz"]
      MA["Mobil uygulama<br/>Flutter · React Native · Swift · Kotlin"]
    end
  end

  subgraph CLOUD["Sunucu / Cloud ortamı"]
    subgraph C0["Container: Web sunucu"]
      STATIC["Frontend build dosyaları<br/>HTML · JS · CSS"]
    end
    subgraph C1["Container: Backend / API"]
      direction TB
      L1["Presentation katmanı<br/>Controller · Endpoint"]
      L2["Application katmanı<br/>Service · Use case"]
      L3["Domain katmanı<br/>Entity · iş kuralları"]
      L4["Infrastructure katmanı<br/>Repository · Cache client · Queue client"]
      L5["Persistence / ORM katmanı<br/>Entity Framework · Slick · Hibernate · Prisma · SQLAlchemy"]
      L1 --> L2 --> L3
      L2 --> L4
      L4 -. "Clean Arch: Domain interface'ini uygular" .-> L3
      L4 --> L5
    end
    subgraph C2["Container: Background job"]
      BJ["Worker<br/>e-posta · rapor · resim işleme"]
    end
    subgraph C3["Container: Database"]
      DB[("Database<br/>PostgreSQL · MySQL · MongoDB")]
    end
    subgraph C4["Container: Cache"]
      CA[("Cache<br/>Redis · Memcached")]
    end
    subgraph C5["Container: Message Queue"]
      MQ[["Message Queue<br/>RabbitMQ · Kafka · SQS"]]
    end
  end

  U -- "tıklar, yazar" --> FE
  U -- "dokunur" --> MA
  BROWSER -- "Frontend kodunu indirir" --> STATIC
  FE -- "HTTP istek · JSON" --> L1
  MA -- "HTTP istek · JSON" --> L1
  L5 -- "SQL" --> DB
  L4 -- "oku / yaz" --> CA
  L4 -- "iş ekler" --> MQ
  MQ -- "iş verir" --> BJ
  BJ -- "sonucu yazar" --> DB
```

**Ok türleri:** Düz ok isteği veya veriyi gösterir. Ok, işi başlatan kutudan çıkar. Kesikli ok kod bağımlılığını gösterir.


## Development Süreci

```mermaid
flowchart TB
  subgraph DEV["Geliştirici bilgisayarı"]
    direction LR
    IDE["Editör / IDE"]
    CODE["Kaynak kod"]
    GIT["Git<br/>yerel repo · commit · branch"]
    subgraph GITAREA["Git'in üç alanı"]
      direction TB
      WD["Çalışma dizini"]
      STAGE["Staging alanı"]
      LOCALREPO["Yerel repository"]
      WD -- "git add" --> STAGE
      STAGE -- "git commit" --> LOCALREPO
    end
    IDE -- "kod yazar" --> CODE
    CODE --> GIT
    GIT -. "yönetir" .-> GITAREA
  end

  subgraph HOST["Kod barındırma platformu<br/>GitHub · GitLab · Bitbucket · Azure DevOps"]
    direction LR
    REMOTE["Remote repo<br/>pull request · code review"]
    CICD["CI/CD pipeline<br/>test · build"]
    REG["Container registry<br/>imaj deposu"]
    REMOTE --> CICD --> REG
  end

  CLOUD["Sunucu / Cloud ortamı<br/>Genel Fullstack Sistem Haritası"]

  LOCALREPO -- "git push" --> REMOTE
  REMOTE -- "git pull" --> LOCALREPO
  REG -- "deploy" --> CLOUD
```

**Ok türleri:** Düz ok kodun veya komutun yönünü gösterir. Kesikli ok "yönetir" ilişkisini gösterir.

**İki harita nerede birleşir:** `deploy` oku bu diyagramdan çıkar. Ok, Genel Fullstack Sistem Haritası'ndaki Sunucu / Cloud ortamına gider. Yeni container'lar orada çalışmaya başlar.


## Sık Kullanılan Tool'lar

| Tool ismi | İşlevi | Full stack haritasında nereye oturuyor |
|---|---|---|
| SSMS | SQL Server veritabanını yönetir. Sorgu çalıştırır, tablo ve index oluşturur. Yalnızca SQL Server ile çalışır. | Geliştirici bilgisayarında çalışır. Database kutusuna bağlanır. |
| DBeaver | Birçok veritabanını tek arayüzden yönetir: PostgreSQL, MySQL, SQL Server, SQLite. Sorgu çalıştırır, veriyi tablo olarak gösterir. | Geliştirici bilgisayarında çalışır. Database kutusuna bağlanır. |
| Redis Insight | Redis içindeki anahtarları ve değerleri gösterir. Değer ekler, siler, TTL kontrol eder. | Geliştirici bilgisayarında çalışır. Cache kutusuna bağlanır. |
| Postman | Backend'e HTTP isteği gönderir ve cevabı gösterir. Frontend olmadan API'yi test eder. | Geliştirici bilgisayarında çalışır. Backend / API kutusunun Presentation katmanına bağlanır. Frontend'in yerini alır. |
| Tarayıcı DevTools | Frontend'in HTML, CSS ve JS kodunu inceler. Network sekmesi Backend'e giden istekleri ve cevapları gösterir. | Tarayıcının içinde çalışır. Frontend kutusuna bakar. Frontend → Backend okunu izler. |
| Docker Desktop | Container'ları geliştirici bilgisayarında çalıştırır, durdurur ve loglarını gösterir. | Geliştirici bilgisayarında çalışır. Cloud'daki container yapısının yerel kopyasını kurar. |

Ortak nokta: Bu tool'lar sistemin parçası değildir. Geliştirici bu tool'larla çalışan kutuların içine bakar.