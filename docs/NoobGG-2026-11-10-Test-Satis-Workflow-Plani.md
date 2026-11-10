# NoobGG — Test, satışa hazırlık ve workflow planı

Tarih: 10 Kasım 2026. İncelenen kaynak: `cevikburak1/NoobGG-NewVersion`, başlangıç commit'i `1fff9df` (master). Bu rapor mevcut kodun incelenmesine ve bu oturumda çalıştırılan testlere dayanır. Önceki raporlardaki eksikler güncel kodla yeniden karşılaştırılmıştır.

## Karar

Ürün, kontrollü bir ücretsiz beta için güçlü bir başlangıç sunuyor. Ücretli müşteriye açılış için henüz hazır değil. Temel engeller: gerçek ödeme sağlayıcısı bulunmaması, planda satılan limitlerin ilgili komutlarda uygulanmaması, hesap kurtarma ve kötüye kullanım korumalarının eksikliği, doğrulanmamış rekabet sonuçları ve üretim operasyonlarının kanıtlanmamış olması.

Bu çalışma yeni özellik geliştirme projesi değil; doğrulanan bugların düzeltilmesi, test kapsamının genişletilmesi ve kalan işlerin planlanmasıdır. n8n kurulmadı, dış servislere bildirim gönderilmedi, gerçek ödeme alınmadı.

## Çalışan ortam ve doğrulama

React/Vite frontend, .NET 8 API, MongoDB 7 ve Redis 7 çalıştırıldı. Frontend 3000, API 5000 portunda çalıştırıldı; Playwright kendi frontend sunucusunu 3001 portunda açtı. Tüm servisler ve veritabanlı testler aynı yürütme ortamında tutuldu. Kullanılan veri geliştirme seeder'ının sentetik verisidir; üretim verisi kullanılmadı.

Bu oturumda Playwright MCP aracı sağlanmadığı için testler doğrudan Playwright Test ile yürütüldü. Sistem Chromium'u ortam kısıtlaması nedeniyle başlatılamadı; indirilen Chromium Headless Shell ile gerçek tarayıcı testleri çalıştı. Mobil testler Pixel 7 viewport emülasyonudur; gerçek iPhone/Safari veya fiziksel cihaz testi değildir.

| Kontrol | Sonuç | Sınır |
|---|---|---|
| API Release build | Başarılı, 0 hata / 0 uyarı | Docker image build'i bu ortamda çalıştırılmadı |
| Backend testleri | 14 başarılı | MongoDB bağlantısı açık olarak verilerek integration testleri gerçekten çalıştırıldı |
| Frontend production build | Başarılı | Yaklaşık 1,8 MB sıkıştırılmamış App chunk'ı için boyut uyarısı sürüyor |
| ESLint | Hata ve uyarılar temizlendi | Güvenlik denetimi veya çalışma zamanı garantisi değildir |
| Tam Playwright turu | 21 başarılı, 1 atlanan, 0 başarısız | Moderasyon yaptırımı mobilde tekrar edilmemesi için tek kez çalışıyor |
| Son mobil görsel QA | İlgili 2 DM testi ayrıca başarılı | Header/composer çakışması ve görünüm ekran görüntüsüyle kontrol edildi |
| Gerçek ödeme / SMTP / RAWG | Doğrulanmadı | Sağlayıcılar bağlı değil; RAWG sync bu çalışma için kapatıldı |

İlk yeni regresyon turunda özel oda erişimi ve dönem sonu iptal kontrolleri masaüstü/mobilde başarısız oldu. Düzeltmelerden sonra aynı kontroller geçti. Test çalıştırmalarında sentetik odalar temizlendi, abonelik test kullanıcısı Free plana döndürüldü, değiştirilen profil gizlilik ayarı geri yüklendi.

### Test matrisi

| Alan | Gerçekten kontrol edilen davranış |
|---|---|
| Landing / 3D | Masaüstü/mobil render, görünür canvas, hareket, reduced-motion, WebGL yokken statik fallback |
| Giriş / oturum | Formdan giriş, korumalı sayfaya geçiş, sayfa yenilemede oturumun korunması |
| Ana ekranlar | Rooms, Discover, Guilds, Community, Compare, Tournaments, Messages, Friends, Favorites, Notifications, Settings, Edit Profile, Game Profiles, Leaderboard, Subscriptions: SPA gezinme, görünür ana içerik/başlık, JavaScript hatası ve API 500 kontrolü |
| Özel oda | Anonim/yabancı kullanıcı detay isteği 404; creatorId filtresiyle listeye sızmaması; doğrudan katılımın 403 olması; davet kabulünden sonra üye erişimi |
| Profil gizliliği | Private profilin kısıtlı görünmesi; games alt endpoint'inin anonim erişimi reddetmesi; sahibinin erişebilmesi |
| DM | İki ayrı browser context arasında canlı mesaj teslimi, API geçmişine yazılması, mobil composer genişliği, konuşma listesine geri dönüş |
| Abonelik | Yetkisiz moderasyon 403; yapılandırılmamış checkout 503; dönem sonunda iptalde Plus hakkının ve entitlement'ın sürmesi; yenilemenin kapanması |
| Moderasyon | Rapor oluşturma/inceleme, geçici yaptırım, mevcut access token'ın reddi ve yeniden girişin engellenmesi |
| Refresh token | Aynı token ile beş eşzamanlı istekte yalnızca bir başarılı rotasyon |
| Matchmaking | MongoDB üzerinde eşzamanlı kuyruk girişinde tek aktif kayıt ve tek eşleşme odası |
| Oda kapanışı | Üyeler kaldırılmadan önce katılımcılarıyla tek oyun oturumu kaydı |
| Doğrulama e-postası | Yeniden gönderilen kod altı haneli; eski kod geçersizleşiyor; yeni kod ile hesap gerçekten doğrulanıyor (MongoDB + test e-posta servisi) |

**Henüz doğrulanmayan kapsam:** Her guild/forum/turnuva yazma işlemi, tüm rol kombinasyonları, oda sohbetinin iki kullanıcıyla uçtan uca akışı, kapasite/yük/uzun süreli bağlantı testleri, dosya yükleme içeriği güvenliği, gerçek e-posta teslimi, ödeme callback'i ve iade, Safari/Firefox, gerçek mobil cihazlar, üretim kurulum/restore. “Her şey hatasız” sonucu çıkarılmamalıdır. Ana ekran smoke testleri içerideki tüm buton ve mutasyonların testi değildir.

## Düzeltilen sorunlar

| No | Sorun ve önceki davranış | Yapılan düzeltme / doğrulama |
|---|---|---|
| B01 / P0 | Özel odanın detay endpoint'i anonim çağrıda üye listesi dahil bilgi döndürüyordu | Özel oda detayını sadece sahip/üyeler okuyabiliyor; yabancılara 404. Playwright regresyonu geçti |
| B02 / P0 | `creatorId` filtresi başka kullanıcının özel odalarını da listeliyordu | Özel oda listeleme hakkı yalnızca aynı kullanıcıya verildi; anonim liste kontrolü geçti |
| B03 / P0 | Özel odanın `join` endpoint'i davet kabulü gerektirmiyordu | Doğrudan katılım reddediliyor; mevcut davet kabulü akışı korunuyor ve test edildi |
| B04 / P0 | Ana profil gizliyken `/profiles/{id}/games` alt endpoint'i gizlilik kontrolü yapmıyordu | Ana profilin erişim kararı alt endpoint'te de uygulanıyor; sahip/yabancı testi geçti |
| B05 / P0 | Dönem sonunda iptal `Cancelled` durumuna geçtiğinde ücretli haklar anında Free oluyordu | Geçerli tarih aralığındaki iptal edilmiş abonelik de hak sağlıyor; yeni atamada önceki Active/Cancelled kayıtlar kapatılıyor; current-plan sorgusu düzeltildi |
| B06 / P1 | Fiyat ekranı backend'in sunmadığı yıllık %20 indirim uyduruyordu | Fiyat, para birimi ve faturalama aralığı backend planından gösteriliyor; sahte yıllık toggle kaldırıldı |
| B07 / P1 | Yeniden gönderilen doğrulama token'ı 32 karakterdi; arayüz ve validator altı rakam istiyordu | Kayıt ve yeniden gönderimde kriptografik altı haneli kod; validator yalnızca rakam kabul ediyor; integration testi geçti |
| B08 / P1 | Bir refresh token eşzamanlı çağrılarda birden çok kez döndürülebiliyordu | Veritabanında aktif token'a koşullu atomik revocation eklendi; beş istekte tek kazanan testi geçti |
| B09 / P1 | Bağlantı nesnesi yokken DM gönderimi sessizce başarılı dönebiliyordu | Açık bağlantı olmadan hata; bağlantı kurulmadan gönderme butonu kapalı |
| B10 / P1 | Mobil DM ekranındaki sabit sidebar composer alanını daraltıyordu; geri dönüşte URL hedefi sohbeti yeniden açabiliyordu | Mobilde liste/konuşma ayrı görünüm, geri dönüş, hedef kullanıcı işleminin tek kez ele alınması; mesaj kaydırması yalnızca kendi panelinde, composer ve başlık menülerin dışında; iki tarayıcılı test geçti |
| B11 / P1 | Docker API portu ile Vite proxy hedefi farklı; aynı origin kullanımında upload dosyaları proxy'lenmiyordu | Yapılandırılabilir `VITE_PROXY_TARGET`, `/uploads` proxy'si, E2E için açık API hedefi |
| B12 / P2 | Client kayıt kuralları backend'in reddettiği şifre ve kullanıcı adlarını kabul ediyordu | Uzunluk, karakter, büyük/küçük harf ve rakam kuralları hizalandı |
| B13 / P2 | Eksik hook bağımlılıkları / uyumsuz `watch` kullanımı ve gereksiz her-path 3D kurulumları | İlgili hook'lar düzeltildi, `useWatch` kullanıldı, 3D kurulum bağımlılığı sahne moduna indirildi; lint temiz |
| B14 / P1 | MongoDB değişkeni verilmezse integration testleri test etmeden yeşil olabiliyordu | Eksik bağlantı açık test hatası veriyor; CI zaten MongoDB servis ve değişkenini sağlıyor |

B05 gerçek ödeme sağlayıcısında otomatik tahsilatı durdurma entegrasyonu içermez. Sağlayıcı bağlı olmadığı için bu ayrı bir açılış engelidir. B08 mevcut token tekrar kullanımı politikasını korur; çoklu sekme refresh koordinasyonu ayrıca tasarlanmalıdır. B11 yerel API varsayılanını 5071 olarak korur; Docker için 5000 açık olarak ayarlanır.

## Satışa çıkışın önündeki engeller

Öncelik: P0 ücretli yayına çıkışı engeller; P1 beta sonrası ilk sürüme alınmalı; P2 büyüme ve kalite yatırımıdır. Aşağıdakiler kod incelemesiyle görülen eksiklerdir; saldırı testi veya hukuki uygunluk kararı değildir.

| Öncelik | Engel / kanıt | Yapılacak iş | Kabul kriteri |
|---|---|---|---|
| P0 | `UnconfiguredPaymentProvider` DI ile kayıtlı; checkout kullanılabilir değil | Şirketin çalışabildiği ödeme sağlayıcısını seç, gerçek checkout, fatura/mutabakat, yenileme, dönem sonu iptal ve iade akışları | Sandbox'ta satın alma → imzalı callback → hak; yenileme/iptal/iade; aynı event 10 kez gönderilince tek sonuç |
| P0 | Webhook order'ı önce Processing yapıyor; crash/exception sonrası kurtarma ve ödeme tutar/para birimi doğrulaması tamamlanmamış | Siparişte beklenen tutar/para birimi/plan sürümü snapshot'ı, idempotency, Processing lease ve reconciliation | İşlem ortasında servis kapatılıp açıldığında ücret/hak kaybı veya çift hak oluşmuyor |
| P0 | `CreateRoomCommandHandler` ve `AddGameProfileCommandHandler` plan limitlerini sorgulamıyor | Günlük oda kullanım sayacı ve game-profile kotasını sunucu tarafında atomik uygula | Free limiti aşılamıyor; eşzamanlı isteklerle bypass edilemiyor; upgrade/downgrade sınırları tanımlı |
| P0 | Login/register/verify/resend tarafında görünür rate-limit/attempt-lock altyapısı yok | IP + hesap bazlı sınır, doğrulama deneme sayacı, güvenli resend cooldown, abuse logları | OTP deneme sınırı aşıldığında engel; yanlış parola taramasında 429; reverse proxy ve çoklu instance davranışı testli |
| P0 | `RecordMatchResult` kullanıcı beyanıyla iki oyuncunun Elo'sunu değiştirebiliyor | Sonucu doğrulanmış session/provider verisine bağla veya karşı taraf onayı; tekrar gönderimi önle | Tek taraflı sahte kazanımla başkasının Elo'su değişmiyor; yarış halinde çift puan yok |
| P0 | Şifre sıfırlama için e-posta arayüzü var, kullanıcıya açık tam forgot/reset akışı yok | Tek kullanımlık ve süreli reset, hesap varlığını sızdırmayan yanıtlar, session iptali | Unutulan şifreyle yeni şifre belirleme; eski link yeniden kullanılamıyor |
| P0 | Silme isteği yalnızca `DeletionRequestedAt` yazıyor | Retention/anonymization/export süreci, başarısız işlerin takibi, kullanıcıya durum ekranı | Sentetik hesabın ilgili verileri belgelenmiş politikaya göre işleniyor; geri yüklemede silme talepleri korunuyor |
| P0 | Upload'lar yerel wwwroot; Compose'ta upload volume yok; health yalnızca sabit healthy yanıtı | Kalıcı dosya depolama, içerik doğrulama, DB/Redis readiness, yedek/restore, hata izleme | Container yeniden kurulduğunda dosya kaybolmuyor; DB kapalıyken readiness başarısız; restore tatbikatı başarılı |
| P1 | `PlanSeedInitializer` her açılışta plan fiyatını ve alanlarını varsayılanlarla güncelliyor | Plan sürümleme, seed'in yalnızca eksik plan eklemesi, kontrollü fiyat migration'ı | Restart yönetici/sağlayıcı fiyatını değiştirmiyor; eski abonelik fiyatı korunuyor |
| P1 | Presence takibi `InMemoryPresenceTracker`; Redis backplane tek başına presence paylaşımı sağlamaz | Çoklu instance presence store / heartbeat / expiry; bağlantı kesilmesi testi | Farklı API instance'larına bağlanan kullanıcılar tutarlı durum görüyor |
| P1 | Guild reminder işinde teslimden önce ReminderSentAt işaretleniyor | Kullanıcı/kanal bazlı idempotent teslim ve yeniden deneme | İlk gönderimde kesinti olsa da kalan kullanıcılar hatırlatma alıyor; aynı kişiye çift yok |
| P1 | Frontend App chunk yaklaşık 1,8 MB | Route lazy-loading, chart/3D kodunu ayırma, isteğe bağlı 3D, gerçek cihaz ölçümü | Önce/sonra bundle ve etkileşime hazır olma ölçümü; mobilde ürün akışları stabil |
| P1 | Ayarlarda email/push tercihleri “coming soon” | Kanal bazlı tercih, sessiz saatler, çıkış bağlantısı, teslim kaydı | Tercihini kapatan kullanıcıya pazarlama bildirimi gitmiyor |

Tam hukuki metinler, içerik/oyun görseli kullanım hakları, müşteri destek/iade politikası ve ülke bazlı satış koşulları yayına çıkış iş listesine ayrıca alınmalıdır. Bu rapor bunların uygun olduğunu doğrulamaz.

## Ürün stratejisi ve yeni özellikler

İlk hedef kitle önerim: belirli oyunlarda düzenli takım bulmak isteyen oyuncular ve topluluğunu yöneten guild yöneticileri. İlk pilotu 2–3 oyun ve tek dil/bölge grubuyla daraltmak, eşleşme bekleme süresini ve kullanıcı yoğunluğunu ölçmeyi kolaylaştırır. Bu bir ürün hipotezidir; talep doğrulaması yapılmış değildir.

| Sıra | Özellik | Kullanıcı faydası | Ölçülecek sonuç | Tahmini efor* |
|---|---|---|---|---|
| 1 | Oyun, rol, dil, saat uygunluğu ile kısa onboarding | İlk doğru oyuncu/odaya hızlı ulaşma | Kayıttan ilk oda katılımına dönüşüm ve süre | 3–5 gün |
| 2 | Planlı oyun buluşması + RSVP + hatırlatma | Ekibin aynı saatte toplanması | RSVP → katılım, tekrar buluşma | 3–6 gün; guild event altyapısı mevcut |
| 3 | Kaydedilmiş filtreler ve uygun oda bildirimi | Sürekli arama ihtiyacını azaltma | Bildirim → oda katılımı; kapatma oranı | 4–7 gün |
| 4 | İyi takım arkadaşını tekrar davet / favori ekip | Başarılı eşleşmenin devamı | Aynı ekiple ikinci oturum; D7/D30 tutma | 3–5 gün |
| 5 | Guild yönetici paneli ve temel analiz | Topluluk yöneticisinin aktif üyeleri görmesi | Haftalık aktif guild, etkinlik katılımı | 5–8 gün; mevcut analytics üzerine |
| 6 | Açıklanabilir öneri gerekçesi | Neden bu oyuncunun önerildiğini anlama | Öneri tıklaması → gerçek eşleşme | 2–4 gün; mevcut recommendation üzerine |
| 7 | Oturum sonrası kontrollü değerlendirme | Güvenilir takım arkadaşı bulma | Tekrar eşleşme, report oranı | 5–8 gün; karşılıklı doğrulama ve abuse kontrolü dahil |
| 8 | PWA / daha iyi mobil gezinme | Uygulamaya hızlı geri dönme | Mobil tekrar kullanım | 4–7 gün; native mobil uygulama kapsam dışı |
| 9 | Guild/oyun odaklı AI özetleri | Uzun topluluk içeriğini hızlı anlama | Okunma, maliyet/aktif kullanıcı | 4–7 gün + değerlendirme ve maliyet limiti |

*Eforlar tek geliştirici için yaklaşık uygulama günüdür; QA, tasarım kararı, dış sağlayıcı onayı ve bilinmeyen entegrasyonlar süreyi artırabilir. Kesin teslim tarihi değildir.

Ücretlendirme hipotezi: temel eşleşme, raporlama, engelleme ve temel iletişim Free kalmalı. Plus için kayıtlı filtre/uygunluk bildirimi, planlı ekip ve kişiselleştirme; guild yöneticileri için yönetim/analiz araçları denenebilir. Rekabet puanı satın aldıran veya güvenlik özelliklerini ücretli yapan modelden kaçın. Mevcut Plus/Pro özellik anahtarlarını gerçek sunucu davranışıyla eşleştirmeden fiyat sayfasında vaat etme. Nihai paket/fiyatı pilot kullanım ve maliyet verisiyle belirle; bu rapor pazar fiyat araştırması içermez.

## Workflow / n8n nerede kullanılmalı?

Önerim n8n'i bildirim, operasyon ve içerik süreçlerinin orkestrasyonu için kullanmak. Eşleştirme, oda kapasitesi, mesaj yetkisi ve abonelik hakkı gibi tutarlılık gerektiren kararlar API'de kalmalı. Bu ayrım önerilen mimaridir; mevcut n8n bağlantısı yoktur.

| Öncelik / senaryo | Tetikleyici | Adımlar | Dikkat / başarı ölçütü |
|---|---|---|---|
| P1: Etkinlik hatırlatma | `guild.event.created` + zaman kontrolü | API'den güncel event/RSVP → katılım ve kanal tercihi → uygulama içi/email/bağlı Discord → teslim kaydı | Mevcut GuildEventReminderJob ile aynı bildirimi iki kez gönderme; tek sorumlu seç; RSVP değişimini gönderim anında tekrar kontrol et |
| P1: Moderasyon önceliklendirme | `report.created` | Tekrar/spam kontrolü → bağlam özeti → moderatör kuyruğu → onaylanan API işlemi → audit | AI çıktısı doğrudan ban vermesin; bildirim içeriğinde minimum kişisel veri |
| P1: Sistem hata alarmı | Error Trigger / API health alarmı | Hata sınıflandır → tekrarları grupla → görev oluştur → sorumlu operasyon kanalına ilet | Alert fırtınasına cooldown; failure halinde ayrı izleme kanalı |
| P1: Onboarding yardımı | `user.email_verified` | Tamamlanmamış adımı kontrol et → kısa yardım → ilk uygun oda önerisi → gönderim kaydı | İzin ve iletişim tercihi; aynı event'te çift email yok; ilk oda dönüşümünü ölç |
| P2: Uygun oda uyarısı | `room.created` | Kayıtlı filtre sahiplerini API'den getir → güncel görünürlük/block kontrolü → kota/cooldown → bildir | Özel oda veya engelli kullanıcı bilgisi sızdırma; bildirim yüküne limit |
| P2: Haftalık guild özeti | Haftalık zamanlayıcı | Anonimleştirilmiş istatistik → kısa özet → yönetici onayı/kanal → arşiv | Mesaj içeriklerini gereksiz yere AI sağlayıcısına taşıma |
| P2: Ödeme operasyonu | Sağlayıcıdan doğrulanmış `billing.payment_failed` | API sipariş durumunu kontrol et → bilgilendirme/destek → mutabakat kaydı | Aboneliği n8n doğrudan DB yazarak açmasın; sağlayıcı doğrulaması API'de |
| P2: Oyun kataloğu kalite kuyruğu | Sync sonucu / periyodik kontrol | Eksik açıklama/görsel tespit → editör taslağı → onay → API güncelleme | Var olan RAWG sync job'ını ikinci kez koşturma; kullanım haklarını koru |

### Önerilen bağlantı sözleşmesi

1. API iş değişikliğini ve outbox event'ini tutarlı şekilde kaydeder. Transaction kullanılacaksa MongoDB replica set gereksinimi ayrıca kurulmalı; mevcut standalone ortamda çok dokümanlı transaction varmış gibi davranılmamalı.
2. Dispatcher teslim edilmemiş event'i lease ile alır; HTTPS üretim webhook'una gönderir. n8n Webhook node'unun test ve üretim URL'leri ayrıdır; yayında üretim URL'si kullanılmalıdır [1].
3. İlk sürümde header authentication ve minimum yetkili servis kimliği kullanılabilir. HMAC tercih edilirse ham body + timestamp + eventId imzasını doğrulayan gateway/özel doğrulama adımı açıkça uygulanmalı; Webhook node'unun bunu otomatik yaptığını varsayma.
4. n8n eventId'yi kalıcı inbox/delivery tablosunda atomik unique kayıtla sahiplenir. Callback başarıyla tamamlandığında sent; hata durumunda attempt sayısı, nextAttemptAt ve son hata tutulur.
5. Önerilen retry: kısa ağ hatası/429/5xx için sınırlı artan bekleme, kalıcı 4xx için inceleme. Hata iş akışı Error Trigger ile ayrı tanımlanır [2].
6. Her gönderimden hemen önce API'den son izin, üyelik, block ve event durumunu kontrol et. Event içinde taşınan eski tercihleri tek kaynak sayma.
7. n8n'in DB'ye doğrudan kullanıcı/abonelik yetkisiyle yazmasına izin verme. Dar kapsamlı internal API endpoint'leri, correlationId ve audit kullan.

Önerilen event zarfı:

```json
{
  "eventId": "unique-id",
  "eventType": "guild.event.reminder_due",
  "schemaVersion": 1,
  "occurredAt": "2026-11-10T16:00:00Z",
  "correlationId": "request-id",
  "data": { "guildId": "guild-id", "eventId": "event-id" }
}
```

Token, parola, ödeme kartı veya tam mesaj geçmişini event'e koyma. Event zarfındaki `eventId` idempotency içindir; `data.eventId` guild etkinliğinin kimliğidir. İlk sürüm tek n8n instance ve kalıcı veritabanıyla başlayabilir. Yük arttığında main/worker + Redis queue mimarisi değerlendirilebilir [3]. Ürünün Redis'iyle ortak kapasite kullanımı yerine ayrı kaynak/izolasyon bütçesi belirle.

### İlk workflow için somut teslim planı

**Tercihim: guild etkinlik hatırlatma + hata operasyonu.** Etkinlik ve RSVP alanları zaten var; yeni değer ölçülebilir, ödeme kadar kritik değil.

| İş | Çıktı | Kabul testi | Efor |
|---|---|---|---|
| Event sahipliği kararı | Mevcut reminder job / n8n arasında tek gönderim sorumlusu | Aynı event iki yoldan gönderilemiyor | 0,5 gün |
| Teslim modeli ve API | Outbox/inbox, lease, internal recipient lookup, delivery endpoint | Aynı event 10 kez → tek teslim | 2–4 gün |
| n8n akışı | Authenticated webhook, tercih sorgusu, kanal kolu, callback, error workflow | Yanlış kimlik reddediliyor, 429 yeniden deneniyor | 1–2 gün |
| Ops ve gözlem | Başarısız işler ekranı / uyarı / tekrar oynatma | Worker kapanıp açıldığında iş kaybolmuyor | 1–2 gün |
| Kontrollü pilot | Sentetik kullanıcılar, ardından izinli küçük grup | RSVP iptali, engelleme, sessiz saat, çift event testleri | 1–2 gün |

Kanal servislerinin anahtarları ve gönderim kimliği kurulum sırasında sağlanmalı. Gerçek kullanıcılara otomatik mesaj gönderimi ayrı rollout kararıdır.

### n8n maliyeti ve ticari kullanım

Maliyet hesabı: n8n barındırma/lisans + kalıcı DB/Redis + email/push/Discord altyapısı + workflow execution hacmi + varsa AI token maliyeti + izleme/operasyon. Güncel plan fiyatları karşılaştırılmadı; sabit aylık ücret varsayılmadı. Önce execution/aktif kullanıcı ve teslim maliyetini ölç, günlük tavan ve alarm ekle.

n8n'in resmi açıklaması organizasyon içindeki workflow geliştirmesi ile müşterilerin kendi workflow mantığını oluşturabildiği ürünleri ayırıyor [4]. İlk tasarımı ekibin yönettiği operasyon akışları olarak tut; kullanıcıya otomasyon editörü veya workflow yapılandırma ürünü açmadan önce seçilen kullanımın lisans koşullarını doğrula.

## Aşamalı geliştirme planı

| Aşama | İşler | Çıkış kriteri | Tahmini uygulama eforu |
|---|---|---|---|
| 0 — Bu çalışma | Bug düzeltmeleri, gerçek servislerle doğrulama, test ve plan | İlgili regresyonlar yeşil, kalan riskler kayıtlı | Tamamlandı |
| 1 — Kontrollü beta | Rate-limit/OTP sınırı, şifre kurtarma, Elo doğrulama kararı, privacy/role matrisi, readiness/restore | Kritik yetki/gizlilik açığı yok; sentetik uçtan uca kayıt→oda→iletişim testi | 8–12 gün |
| 2 — Ödeme hazırlığı | Sağlayıcı sandbox, sipariş snapshot'ı, webhook recovery/idempotency, atomik plan limitleri, fiyat sürümleme | Satın alma/iptal/iade/yenileme ve eşzamanlı limit testleri | 10–16 gün + sağlayıcı onayı |
| 3 — Workflow pilotu | Etkinlik hatırlatma ve hata operasyonu | Tek teslim, tercih kontrolü, retry/yeniden oynatma, ölçülen başarı | 5–9 gün |
| 4 — Retention | Planlı ekip, kaydedilmiş filtreler, tekrar davet, onboarding funnel | D7/D30 ve ilk oda dönüşümüyle öncesi/sonrası karşılaştırma | 8–14 gün |
| 5 — Büyüme | Guild yönetim paketi, PWA, route splitting, ölçülü AI | Pilot segmentte değer ve birim maliyet doğrulanmış | Pilot sonucuna bağlı |

Bu aşamalar feature bazında bölünebilir; bütün tabloyu tek sprint taahhüdü olarak değerlendirme. Önce küçük bir pilot topluluğuyla eşleşme yoğunluğunu ve tekrar kullanımını doğrulamak, ödeme altyapısını tamamladıktan sonra fiyat testine geçmek daha kontrollü olur.

## Ücretli açılış kabul listesi

- Gerçek sağlayıcı sandbox yaşam döngüsü ve sipariş mutabakatı kanıtlı.
- Ücretli vaatler sunucuda uygulanıyor; istemci manipülasyonu ve eşzamanlı istekle bypass yok.
- OTP/login brute-force koruması, şifre sıfırlama ve temel session yönetimi tamam.
- Özel oda, block, DM, profil, guild rolü ve moderasyon için pozitif/negatif yetki matrisi yeşil.
- Doğrulanmamış beyan rekabet puanını tek taraflı değiştiremiyor.
- Dosyalar/DB kalıcı; gerçek restore ve başarısız servis senaryoları testli.
- Veri silme/ihracat ve iletişim tercihleri gerçekten çalışıyor; destek ve iade akışı tanımlı.
- Gerçek mobil cihazda ana akışlar ve yavaş ağ davranışı kontrol edilmiş.
- Yük deneyi için kabul edilen eşzamanlı kullanıcı ve p95 süre hedefleri önce tanımlanmış, sonra ölçülmüş.
- Demo seed üretimde kapalı; production secrets ve izinli origin'ler açıkça yönetiliyor.

## Yeniden çalıştırma

Yerelde backend için .NET 8, MongoDB ve Redis; frontend için Node ve npm gerekir. Repo kökünde `docker compose up -d --build` mevcut geliştirme stack'ini açar. Bu komut üretim deployment tarifi değildir.

```bash
cd client
npm ci
# Docker API ile aynı-origin proxy:
# .env.local: VITE_API_URL=  ve VITE_PROXY_TARGET=http://localhost:5000
npm run dev
```

```bash
# Repo kökünde; izole geliştirme MongoDB'si açık olmalı:
MONGO_TEST_URI=mongodb://localhost:27017 dotnet test NoobGg.sln
cd client
npx playwright install chromium
E2E_API=1 npm run test:e2e
npm run build
npm run lint
```

`E2E_API_URL` farklı bir API adresini seçer. E2E testleri seeded geliştirme hesaplarını ve bazı mutasyonları kullanır; yalnızca izole geliştirme/test veritabanına yöneltilmelidir. Daha sonra test başına kullanıcı/veri fixture'larıyla tam izolasyon eklenmeli. Bu oturumun ortamına özel headless shell yolu standart kurulum için gerekli değildir.

## Resmî kaynaklar

1. n8n Webhook, test/production URL ayrımı: https://docs.n8n.io/integrations/builtin/core-nodes/n8n-nodes-base.webhook/
2. n8n Error Trigger ve hata iş akışları: https://docs.n8n.io/flow-logic/error-handling/
3. n8n queue mode, main/worker/Redis: https://docs.n8n.io/hosting/scaling/queue-mode/
4. n8n ticari kullanım açıklaması (6 Ekim 2026 güncellemesi): https://support.n8n.io/article/can-i-use-your-license-for-my-use-case

Kod temelli bulguların referansları ilgili handler/service adlarıyla tablolarda verilmiştir. Dış kaynaklar workflow altyapısına ilişkindir; ürünün satışa hazır olmadığı kararı bu çalışma kapsamındaki kod ve test bulgularına dayanır.
