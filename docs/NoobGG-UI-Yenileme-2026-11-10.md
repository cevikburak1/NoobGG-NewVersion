# NoobGG — Arayüz yenilemesi

10 Kasım 2026. Bu değişiklik, önceki hata düzeltmelerini içeren `ab32a4a` commit'i üzerine uygulanmıştır.

## Tasarım yönü

Koyu mürekkep zemin, lime ana vurgu, lavanta yardımcı vurgu. Oyun topluluğu hissini koruyan, içerik okunabilirliğini öne çıkaran ortak bir tasarım dili kullanıldı. Büyük başlıklar, daha belirgin aksiyonlar, sade form alanları ve tutarlı panel/kart yapısı oluşturuldu.

| Alan | Token / karar |
|---|---|
| Zemin | `#0c0e14` |
| Panel | `#141720` |
| Ana vurgu | `#c5f277`; üzerinde koyu metin |
| Yardımcı vurgu | `#bba4fb` |
| Ana metin | `#f3f4f8` |
| İkincil metin | `#a5adbf` |
| Başlık fontu | Bricolage Grotesque Variable |
| Arayüz fontu | Plus Jakarta Sans Variable |

Font dosyaları npm paketlerinden build'e dahil edilir; Google Fonts isteği kaldırılmıştır. Türkçe karakterler için Latin Extended font alt kümesi bulunur.

## Uygulanan değişiklikler

- Landing: yeni değer önerisi, katalog verisiyle çalışan oyun vitrini, belirgin kayıt/oda keşif aksiyonları, üç adımlı ürün tanıtımı ve footer.
- Giriş/kayıt: masaüstünde marka/ürün anlatımı ve formun ayrıldığı iki kolon; mobilde sade, tek kolon form.
- Uygulama çatısı: yeni marka işareti, keşfe yönlendiren arama bağlantısı, gruplandırılmış sidebar, aktif sayfa vurgusu, kullanıcı menüsü.
- Mobil menü: Odalar, Keşfet, Mesajlar ve Loncalar; kalan sayfalar erişilebilir “Diğer” panelinde. Moderasyon bağlantısı sadece ilgili rollere görünür.
- Oda keşfi: yeni hero, gerçek katalogdan oyun seçme şeridi, kartların görsel düzeni, filtre davranışları; geçmiş oturumlar açılır bir bölüme alındı.
- Keşfet/Loncalar: yeni sayfa başlık düzeni, ortak kartlar ve yatay taşma düzeltmeleri.
- Profil, abonelik ve ayarlar: panel, başlık, fiyat ve vurgu düzenleri yenilendi. Abonelik fiyatı backend kaynağından gelmeye devam eder.
- Mesajlaşma: yeni panel/yan liste görünümü; önceki mobil composer ve bağlantı düzeltmeleri korunur.
- Ortak bileşenler: butonlar, input/select/textarea, kartlar, sekmeler, modal, dil seçimi, scrollbar, seçim/focus renkleri.
- Arka plan sahnesi: yeni paletle uyumlu ve içerik alanlarında daha düşük yoğunlukta.
- Oyun görselleri: kapak mevcutsa kullanılır. Kapak yoksa, sentetik placeholder ise veya yüklenemiyorsa oyunun adından türeyen yerel SVG/renk kompozisyonu görünür. Bu görseller resmî oyun kapakları değildir.

Her ekranın iş mantığı ayrı ayrı yeniden yazılmadı. Ortak tasarım bileşenleri uygulama genelini etkiler; yukarıdaki ana sayfalarda ayrıca yerleşim değişiklikleri yapıldı.

## Etkileşim ve erişilebilirlik

- Modal body seviyesinde portal ile açılır; üst öğelerin transform/overflow sınırlarına takılmaz.
- Dialog rolü, başlık ilişkisi, Escape, odak sınırı ve kapanınca odağın geri verilmesi eklendi.
- Mobil “Diğer” panelinde klavye odağı ve scroll kilidi yönetilir.
- Ortak sekmelerde ARIA rolleri ile ok/Home/End tuşları çalışır.
- Ana içeriğe atlama bağlantısı ve görünür focus halkası bulunur.
- Hareket azaltma tercihi ve WebGL fallback davranışları korunur.
- Türkçe/İngilizce yeni landing/auth metinleri ve menü etiketleri eklendi. Uygulamanın eski sayfalarındaki tüm metinlerin tam çevirisi bu değişikliğin kapsamı değildir.

## Doğrulama

Son tam tarayıcı turunda **27 test başarılı, 1 bilinçli atlama, 0 başarısız**. Atlanan test, aynı moderasyon yaptırımının mobilde ikinci kez uygulanmasını önler.

Mevcut 21 başarılı akışın üzerine 6 arayüz kontrolü eklendi:

1. Landing/login/register görünümünün masaüstü ve mobilde Türkçe/İngilizce yerleşimi.
2. Odalar, keşif, loncalar, forum, mesajlar, profil düzenleme, ayarlar ve abonelikte viewport taşma kontrolü.
3. Oyun görseli üzerinden filtre seçme ve temizleme.
4. Oda oluşturma modalını açma ve Escape ile kapatma.
5. Mobil menüden sayfa açma ve scroll kilidinin temizlenmesi.
6. Ekran görüntüleri üzerinden ana sayfaların görsel kontrolü.

Kontroller Chromium masaüstü ve Pixel 7 emülasyonuyla, sentetik geliştirme verileri üzerinde yapıldı. Fiziksel iPhone/Safari ve Firefox testi yapılmadı. Mevcut giriş, gizlilik, abonelik, refresh token ve iki tarayıcılı canlı DM regresyonları da geçti.

Frontend production build ve ESLint başarılı. Büyük App chunk uyarısı sürüyor; route bazlı lazy-loading ayrı bir performans işi olarak kalıyor. Bu çalışma gerçek ödeme entegrasyonunu veya satışa çıkış engellerini tamamlamaz.

## Kurulum ve teslim

Kod bu çalışma ortamında değiştirildi. GitHub'a uzak dal yazımı önceki denemede 403 ile engellendiği için yeni tasarım da bir patch teslimi olarak hazırlanmıştır; üretime dağıtım yapılmadı.

Paket iki alternatif içerir:

- Önceki bugfix patch'i uygulandıysa yalnızca UI patch'ini kullan.
- İlk kaynak commit `1fff9df09e32a3a07d8036c3a59ee76d15e83ae0` üzerindeysen iki commit'i içeren tam patch'i kullan.
- İkisini birlikte uygulama. Temiz çalışma ağacında, ayrı bir dalda çalış.

Patch uygulamasından sonra `cd client && npm ci` çalıştır; font bağımlılıkları güncellendi. Dev API için Vite proxy hedefini önceki rapordaki gibi ayarla. Testler için izole seeded geliştirme ortamı gerekir.

```bash
cd client
npm ci
npm run build
npm run lint
E2E_API=1 npm run test:e2e -- --workers=1
```

Ekran görüntüleri iç doğrulama için kullanılmıştır; teslim paketine dahil edilmemiştir.
