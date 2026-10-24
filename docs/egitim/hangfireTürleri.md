| Yöntem | Ne demek? | Örnek | Kullanılan metot |
|---|---|---|---|
| **Fire-and-forget** | Kuyruğa koy; uygun worker alıp çalıştırsın. | “Kayıt olan kullanıcıya hoş geldin e-postası gönder.” | `BackgroundJob.Enqueue` |
| **Delayed** | Belirlenen süre sonrasında çalıştırılmak üzere planla. | “15 dakika sonra e-posta gönder.” | `BackgroundJob.Schedule` |
| **Recurring** | Belirlenen takvime göre tekrar tekrar çalıştır. | “Her gün rapor oluştur.” | `RecurringJob.AddOrUpdate` |
| **Continuation** | Önceki iş başarıyla tamamlanınca sonraki işi başlat. | “Raporu hazırla, ardından e-postayla gönder.” | `BackgroundJob.ContinueJobWith` |