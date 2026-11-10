# 200 kullanıcılı tutarlı demo

## Çalıştırma

Depo kökünde `docker compose up -d --build`, ardından `client` klasöründe `npm ci` ve `npm run dev -- --host 0.0.0.0` çalıştırın. API 5000 portunu kullanır. İstemci proxy ayarı için `client/.env.example` dosyasına bakın.

Compose, ayrı `noobgg_demo_200` veritabanını ve `DemoSeed:Scenario=coherent-200` senaryosunu seçer. Başlangıçta gerçek veri silinmez. Seeder Development dışında veya uygun isimde ayrı veritabanı olmadan çalışmaz; başka kullanıcılar bulunan bir veritabanına karışmayı reddeder. Başarıyla tamamlandıktan sonra yeniden başlatmak kullanıcı işlemlerini sıfırlamaz.

## Demo hesapları

`Player_0000`–`Player_0199`, ortak demo şifresi `Test1234!`.

| Hesap | İnceleme amacı |
| --- | --- |
| Player_0006 | Ücretli plan, sosyal içerik, lonca yönetim yetkisi |
| Player_0000 | Lonca sahibi |
| Player_0002 | Moderasyon |
| Player_0001 | Sistem yöneticisi |

Bunlar yalnızca yerel geliştirme hesaplarıdır. E-postalar `.example.test` alan adındadır. Abonelik ve ödeme kayıtları açıkça demo olarak işaretlenir; ödeme tahsil edilmez. Avatarlar ve oyun görselleri yerelde üretilir.

## Veri kapsamı

200 kullanıcı, 400 oyun profili, 10 oyun, 50 oda, 10 lonca, 30 lonca etkinliği, 400 DM konuşması ve 2.400 DM mesajı oluşturulur. Oda sohbetinde 800, lonca sohbetinde 400 mesaj bulunur. Topluluk için 11 pano, 400 gönderi, 1.200 yorum, 30 rehber; turnuvalar için 30 turnuva, 320 katılım ve 140 eşleşme vardır. Geçmiş oturum, geri bildirim, ELO geçmişi, arkadaşlık, bekleyen davet, bildirim ve moderasyon örnekleri de oluşturulur.

Üyelikler, roller, kapasite, oyun profili uygunluğu, davet durumları, arkadaşlık izinleri, katılımcılar, yorum/oy sayaçları ve turnuva ilerlemesi yazmadan önce doğrulanır. Turnuva aşamaları farklıdır; henüz oynanmamış bir eşleşmenin kazananı uydurulmaz. Canlı çevrimiçi kullanıcı varmış gibi gösterilmez. Filtreler ve kullanıcı eylemleri doğal olarak boş sonuç üretebilir.

Development ortamında `GET /api/demo/status`, tam kullanıcı sayısını, ilk kurulumdaki kayıt sayılarını ve doğrulama sonucunu verir. `countsAtSeed` alanı anlık istatistik değil, ilk kurulumun kaydıdır.

## Doğrulama

- `CoherentDemoWorldTests`: tam 200 hesap, deterministik kimlikler ve bozulmuş ilişkilerin yakalanması.
- MongoDB ile uygulama testleri: `MONGO_TEST_URI=mongodb://localhost:27017 dotnet test NoobGg.sln`.
- İstemci: `npm run lint`, `npm run build`.
- E2E: izole demo API açıkken `E2E_API=1 npm run test:e2e`. Bu testler oda ve abonelik gibi verileri değiştirir; sunum veritabanından ayrı çalıştırın.

Ticari hazırlık ve otomasyon önerileri `NoobGG-2026-11-10-Test-Satis-Workflow-Plani.md`, arayüz değişiklikleri `NoobGG-UI-Yenileme-2026-11-10.md` dosyasında bulunur.
