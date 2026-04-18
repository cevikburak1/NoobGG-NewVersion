# NoobGg — Müşteri Durum Raporu

**Belge türü:** Yönetim / müşteri sunumu (ürün + teknik olgunluk)  
**Kaynak:** Depo içi kod, [README.md](../README.md), [Musteri-Urun-Envanteri-NoobGg.md](./Musteri-Urun-Envanteri-NoobGg.md)  
**Tarih:** Nisan 2026  

---

## 1. Yönetici özeti

**NoobGg**, oyuncuların birbirini ve oyunları keşfettiği, **oda (room)** ve **lonca (guild)** oluşturup yönettiği, **anlık sohbet ve doğrudan mesaj (DM)** ile iletişim kurduğu, **bildirim**, **arkadaş / engelleme / favoriler**, **abonelik planları** ve uygun rollerde **raporlama / moderasyon API** katmanını içeren bir **oyuncu topluluğu ve eşleştirme** web platformudur.

**Bugünkü olgunluk:** Çekirdek oyuncu deneyimi (kayıt, profil, keşif, odalar, loncalar, DM, sosyal grafik, bildirimler, Elo/lider tablosu) **işlevsel ve geniş kapsamlı** şekilde kodlanmış durumda. **Moderasyon paneli arayüzü** henüz ürün seviyesinde değil (placeholder). **Abonelik** arayüzü ve API tarafı varken **gerçek ödeme entegrasyonu** ve **fatura geçmişi** ürün notlarında “kapsam dışı / zayıf” olarak işaretleniyor. **Otomatik test** script’i istemci paketinde tanımlı değil; kalite güvencesi ağırlıklı manuel ve entegrasyon pratiğine dayanıyor.

**Önerilen odak:** Önce moderasyon ürün akışını (liste + inceleme + kullanıcı rapor formu) tamamlamak; ardından abonelik/mock ve bildirim tercihleri tutarlılığı; paralelde i18n ve test omurgası.

---

## 2. Ürün ne sunuyor? (müşteri dili)

| Alan | Kullanıcıya fayda |
|------|-------------------|
| **Hesap ve güven** | Kayıt, giriş, e-posta doğrulama, JWT + yenileme akışı |
| **Profil** | Zengin oyuncu profili, oyun bazlı alt profiller, görsel yükleme |
| **Keşif** | Oyuncu ve oyun keşfi; filtreler ve öneriler (kural tabanlı skorlama) |
| **Odalar** | Oda listesi, filtre, oluşturma; detayda üyeler, sohbet, davet, Elo ile ilgili akışlar |
| **Loncalar** | Lonca listesi/detayı, üyelik ve yönetim aksiyonları |
| **Mesajlaşma** | DM arayüzü ve SignalR ile anlık iletişim |
| **Sosyal** | Arkadaşlık istekleri, engelleme, favori oyuncular |
| **Bildirimler** | Akış, okundu işaretleme, anlık bildirim + toast |
| **Rekabet** | Maç kaydı, lider tablosu, geçmiş |
| **Planlar** | Plan seçimi ve abonelik yönetimi (UI + API; ödeme sağlayıcısı yok) |
| **Moderasyon (backend)** | Rapor oluşturma ve inceleme API’leri; **panel UI eksik** |

Teknik özet: **ASP.NET Core 8**, **MongoDB**, **Redis**, **SignalR**; istemci **React 19 + Vite 6**, React Router 7, TanStack Query. Ayrıntı: [README.md](../README.md), [Musteri-Urun-Envanteri-NoobGg.md](./Musteri-Urun-Envanteri-NoobGg.md).

---

## 3. Sayfa ve rota envanteri

Rota tanımı: [client/src/app/router.tsx](../client/src/app/router.tsx).

**Koruma notu:** Çoğu uygulama sayfası `ProtectedRoute` + `RequireProfile` ile korunur. **`/subscriptions` ve `/leaderboard`** `AppLayout` altında ancak **`RequireProfile` yok** — giriş yapmamış veya profili eksik kullanıcılar da bu sayfaları görebilir (ürün kararı olarak netleştirilmeli).

| Route | Sayfa dosyası | Amaç | Tipik kullanıcı aksiyonları |
|-------|---------------|------|----------------------------|
| `/` | [landing.tsx](../client/src/pages/landing.tsx) | Tanıtım, ürüne yönlendirme | Kayıt / giriş / iç linkler |
| `/login`, `/register` | login, register | Kimlik | Form gönder, sayfalar arası geçiş |
| `/verify-email` | verifyEmail | E-posta doğrulama | Kod gir, yeniden gönder |
| `/onboarding` | onboarding | İlk kurulum | Çok adımlı profil/oyun bilgisi |
| `/rooms` | roomList | Oda keşfi | Filtre, arama, oda oluştur, odaya gir |
| `/rooms/:roomId` | roomDetail | Oda içi | Sohbet, üye/davet, oda aksiyonları |
| `/guilds`, `/guilds/:guildId` | guildList, guildDetail | Loncalar | Arama, filtre, oluştur, detay aksiyonları |
| `/discover` | discover | Keşif | Sekmeler, filtreler, sayfalama, profil/oyun linkleri |
| `/games/:gameId` | gameDetail | Oyun detayı | Favori / yönlendirme benzeri CTA’lar |
| `/profile/:userId` | profile | Profil görüntüleme | Mesaj, arkadaşlık, favori, engelle vb. |
| `/profile/edit` | editProfile | Profil düzenleme | Kaydet, geri |
| `/profile/games` | gameProfiles | Oyun profilleri | Ekle/düzenle/sil |
| `/messages` | messages | DM | Konuşma seç, mesaj gönder |
| `/friends` | friends | Arkadaşlar | İstek kabul/red, mesaj |
| `/favorites` | favorites | Favoriler | Mesaj, keşfe dön |
| `/notifications` | notifications | Bildirimler | Filtre, okundu, davet kabul/red |
| `/settings` | settings | Ayarlar | Gizlilik, güvenlik, hesap; bazı metinler “yakında” |
| `/subscriptions` | subscriptions | Planlar | Dönem seçimi, plan seç / iptal |
| `/leaderboard` | leaderboard | Sıralama | Oyun seç, sayfalama, maç kaydı (girişli) |
| `/moderation` | moderation | Moderasyon | **Şu an anlamlı aksiyon yok (iskelet)** |

**Navigasyon:** Masaüstü [sidebar.tsx](../client/src/components/layout/sidebar.tsx), mobil [mobileNav.tsx](../client/src/components/layout/mobileNav.tsx) — mobilde daha az sekme; Friends / Messages / Plans gibi öğeler tam mobil alt çubukta olmayabilir (UX tutarlılığı için gözden geçirme önerilir).

---

## 4. Özellik durumu (Tamam / Kısmen / Placeholder)

| Özellik | Durum | Kısa gerekçe |
|---------|--------|----------------|
| Auth, profil, oyun profilleri | **Tamam** | Rotalar ve feature modülleri mevcut |
| Odalar, loncalar, sohbet/DM (SignalR) | **Tamam** | Hub’lar ve istemci provider’lar dokümante |
| Keşif, öneriler (kural tabanlı) | **Tamam** | AI öneri yok ([envanter §6.14](./Musteri-Urun-Envanteri-NoobGg.md)) |
| Arkadaş, engel, favori | **Tamam** | |
| Bildirimler (uygulama içi) | **Kısmen** | Guild tipleri / DM deep link tam hizalı değil ([envanter §6.9](./Musteri-Urun-Envanteri-NoobGg.md)) |
| Ayarlar | **Kısmen** | E-posta/push tercih metni “coming soon” ([settings.tsx satır 272](../client/src/pages/settings.tsx)) |
| Abonelik UI | **Kısmen** | API’den plan yoksa `mockPlans` fallback ([subscriptions.tsx satır 36–38](../client/src/pages/subscriptions.tsx)) |
| Gerçek ödeme | **Yok** | [Envanter §6.12](./Musteri-Urun-Envanteri-NoobGg.md) |
| Moderasyon paneli | **Placeholder** | Sadece başlık + TODO ([moderation.tsx](../client/src/pages/moderation.tsx)) |
| Son kullanıcı rapor formu | **Kısmen** | API var; UI bağlantısı sınırlı ([envanter §6.11](./Musteri-Urun-Envanteri-NoobGg.md)) |
| Otomatik test (client) | **Yok** | `package.json` içinde `test` script’i yok ([client/package.json](../client/package.json)) |

---

## 5. Boşluklar, riskler ve teknik borç sinyalleri

**Ürün / güven riski**

- **Moderasyon:** Yönetici/moderatör “panel hazır” beklentisi oluşturmamalı; arayüz iskelet. Topluluk büyüdükçe rapor kuyruğu yönetilemez.
- **Abonelik:** Mock plan fallback, canlıda yanlış fiyat/özellik algısı veya hukuki yanlış anlatım riski taşıyabilir — ortam bazlı netleştirme şart.
- **`/subscriptions` ve `/leaderboard` profil zorunluluğu:** Davranış kasıtlı değilse, onboarding tamamlanmadan dönüşüm veya sıralama gösterimi tutarsız olabilir.

**Operasyon ve kalite**

- İstemci tarafında **merkezi i18n yok**; metinler çoğunlukla İngilizce, bazı yerlerde karışık dil — kurumsal müşteri için zayıf nokta.
- **Test altyapısı eksik** — regresyon maliyeti yüksek.
- SignalR bağlantı hatalarının bir kısmında **sessiz yakalama** (`catch(() => {})`) kullanımı destek ve teşhis zorlaştırır (ör. [roomProvider.tsx](../client/src/providers/roomProvider.tsx), envanter taraması notu).
- `react-hooks/exhaustive-deps` baskıları — gelecekte state bug riski.

---

## 6. Ne ekleyebiliriz, neyi güncellemeliyiz?

### 6.1 Yüksek iş değeri, görece net kapsam (önerilen ilk dalga)

1. **Moderasyon paneli (MVP)**  
   Rapor listesi, filtre, detay modalı, inceleme aksiyonları. İstemcide `features/moderation` API/hook’ları zaten tanımlı; [moderation.tsx](../client/src/pages/moderation.tsx) ile birleştirilmeli.

2. **Son kullanıcı “rapor et” akışı**  
   Profil ve oda bağlamından tutarlı giriş noktası + doğrulama mesajları.

3. **Abonelik sayfası sertleştirmesi**  
   Production’da mock fallback kapatma veya sadece dev’de gösterme; API hata durumunda açık boş/hata UI.

4. **Rota tutarlılığı**  
   `/subscriptions` ve `/leaderboard` için `RequireProfile` / `ProtectedRoute` kararı (ürün gereksinimine göre).

### 6.2 Orta vade (ürün olgunlaştırma)

- E-posta ve (varsa) push bildirim tercihleri — ayarlardaki “coming soon” ile hizalı backend + UI.
- Bildirim tipi birliği (backend ↔ frontend union) ve DM deep link iyileştirmesi.
- i18n (Türkçe/İngilizce) ve metin denetimi.
- Mobil navigasyon ile masaüstü özellik paritesi veya bilinçli “daha fazla” menüsü.
- CI’da `npm run lint` (ve istenirse API build).

### 6.3 İleri vade (farklılaşma / ölçek)

- Ödeme sağlayıcısı, fatura geçmişi, plan değişiklik geçmişi.
- Zengin presence (DND, odada vb.), kayıtlı filtre setleri.
- Turnuva, sesli oda, PWA/native — [envanter §8](./Musteri-Urun-Envanteri-NoobGg.md) ile uyumlu “bilinçli kapsam dışı” başlıkları.

---

## 7. Öncelik matrisi (özet)

| Öncelik | Madde | İş değeri | Not |
|---------|--------|-----------|-----|
| P0 | Moderasyon paneli MVP | Güven, platform sürdürülebilirliği | En büyük “söz verilmiş ama eksik” boşluk |
| P0 | Rapor et (kullanıcı) akışı | Güven, içerik kalitesi | API hazır, ürün bağlantısı eksik |
| P1 | Mock plan / abonelik davranışı | Güven, yanıltıcı UX önleme | Hızlı teknik kazanım |
| P1 | Subscriptions/Leaderboard guard kararı | Tutarlı onboarding | Düşük efor, net karar |
| P2 | Bildirim tercihleri + tip hizası | Memnuniyet, destek yükü | Orta efor |
| P2 | i18n | Kurumsal sunum | Orta-yüksek efor |
| P3 | Test omurgası | Kalite, hız | Uzun vadeli verim |

---

## 8. Başarı ölçütleri (KPI) önerisi

- **Aktivasyon:** Kayıt → e-posta doğrulama → onboarding tamamlama oranı.
- **Eşleşme:** Oda oluşturma / katılma, DM başlatma (haftalık aktif).
- **Sosyal:** Arkadaşlık kabul oranı, favori kullanımı.
- **Güven:** Rapor başına ortalama çözüm süresi (moderasyon paneli sonrası).
- **Gelir (ödeme sonrası):** Plan dönüşümü, churn; şu an için “plan sayfası ziyareti” ve “assign/cancel” API kullanımı vekil olabilir.
- **Kararlılık:** SignalR bağlantı başarı oranı, client error log sayısı.

---

## 9. Hemen başlanacak aksiyonlar (checklist)

- [ ] Moderasyon sayfası için ürün gereksinimi yaz (roller, durumlar, SLA).  
- [ ] `features/moderation` → `moderation.tsx` entegrasyonu (liste + detay + aksiyon).  
- [ ] Profil ve oda UI’da “Rapor et” entry + form.  
- [ ] `subscriptions.tsx`: mock fallback politikasını ortam bazlı netleştir.  
- [ ] `router.tsx`: `/subscriptions` ve `/leaderboard` için profil/giriş politikası kararını uygula.  
- [ ] `npm run lint` için CI job (isteğe bağlı ama önerilir).  
- [ ] En az bir kritik modül için otomatik test (API client veya bir hook).

---

## 10. Sonuç

NoobGg, **oyuncu topluluğu ve gerçek zamanlı iletişim** tarafında **güçlü ve geniş bir MVP+** seviyesindedir. Müşteriye veya yatırımcıya sunarken **moderasyon arayüzü** ve **ticarileştirme (ödeme) gerçekliği** konusunda **şeffaf** olunmalı; bir sonraki dalga yatırımın en yüksek getirisi **güven ve moderasyon ürününü tamamlamak** ile **abonelik ekranının prod-güvenli hale getirilmesi** üzerinedir.

*Bu rapor depo kaynaklarına dayanır; canlı ortamda ek yapılandırma farkı olabilir ([envanter dipnot](./Musteri-Urun-Envanteri-NoobGg.md)).*
