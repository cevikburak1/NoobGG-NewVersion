# NoobGg – Detaylı Proje ve Güncelleme Raporu

Bu belge, depodaki mevcut yapıya ve uygulanan değişikliklere dayanır. Amaç: geliştirme desteği için tek bir referans metin. **Yeni özellik önerisi veya test prosedürü içermez.**

---

## 1. Ürün tanımı (NoobGg nedir, ne yapar)

**NoobGg**, oyuncuların **oda oluşturup katılabildiği**, **oyun ve oyuncu keşfettiği**, **profil ve oyun profilleri yönettiği**, **arkadaşlık / engelleme / favoriler**, **oda içi sohbet**, **doğrudan mesaj (DM)**, **bildirimler**, **abonelik planları** ve (roller uygunsa) **moderasyon** akışlarını bir araya getiren bir **oyuncu eşleştirme ve topluluk** web uygulamasıdır.

Kullanıcı akışı (koddan): çoğu ana özellik [`client/src/app/router.tsx`](../client/src/app/router.tsx) üzerinde **ProtectedRoute** ve **RequireProfile** ile korunur; yani giriş + tamamlanmış profil beklentisi vardır. **`/subscriptions`** bu layout içinde ancak `RequireProfile` olmadan tanımlıdır. **`/moderation`** `RequireRole` ile `Moderator` veya `Admin` rolüne bağlıdır.

---

## 2. Teknoloji yığını (özet)

| Katman | Teknoloji |
|--------|-----------|
| Backend | ASP.NET Core, MediatR, FluentValidation, MongoDB (C# driver), Redis (SignalR backplane ve önbellek/presence ile ilişkili kullanım), JWT |
| Frontend | React 19, Vite 6, TypeScript, React Router 7, TanStack Query, Zustand (auth), Axios, Tailwind CSS v4, Framer Motion |
| Gerçek zamanlı | SignalR hub’ları: `/hubs/chat`, `/hubs/room`, `/hubs/dm`, `/hubs/notifications` ([`src/NoobGg.Api/Program.cs`](../src/NoobGg.Api/Program.cs)) |

---

## 3. Backend mimarisi ve API yüzeyi

**Projeler:** `NoobGg.Domain` (entity/enum), `NoobGg.Application` (özellik dilimleri, handler’lar, DTO’lar), `NoobGg.Infrastructure` (MongoDB, Redis, dış servisler), `NoobGg.Api` (controller’lar, hub’lar, middleware).

**Controller listesi ve rolü** ([`src/NoobGg.Api/Controllers/`](../src/NoobGg.Api/Controllers/)):

- **AuthController** – Kimlik doğrulama, token yenileme
- **UsersController** – Oyuncu keşfi (`discover`), toplu/varlık (presence) uçları
- **ProfilesController** – Profil okuma/güncelleme, oyun profilleri, avatar/banner
- **GamesController** – Oyun kataloğu / arama / detay
- **RoomsController** – Oda CRUD benzeri akışlar, katıl/ayrıl, davetler, listeleme (anonim izinli aksiyonlar controller üzerinde işaretli)
- **ChatController** – Oda mesajları (HTTP tarafı)
- **DirectMessagesController** – DM konuşmaları ve mesajlar
- **FriendsController** – Arkadaşlık istekleri ve listeler
- **BlocksController** – Engelleme
- **FavoritesController** – Favoriler
- **NotificationsController** – Bildirimler
- **SettingsController** – Kullanıcı ayarları
- **SubscriptionsController** – Planlar ve abonelik
- **ReportsController** / **ModerationController** – Şikayet ve moderasyon
- **HealthController** – Sağlık kontrolü
- **RecommendationsController** – Öneriler (aşağıda detay)

---

## 4. Öneri sistemi (eklenen backend)

**Rota:** `api/recommendations` — **[Authorize]** zorunlu.

| Endpoint | Açıklama |
|----------|----------|
| `GET /api/recommendations/players?limit=` | Giriş yapan kullanıcı için önerilen oyuncu listesi. `limit` 1–20 arası sıkıştırılır. |
| `GET /api/recommendations/rooms?limit=` | Giriş yapan kullanıcı için önerilen oda listesi. Aynı limit kuralı. |

**Oyuncu önerisi** — [`GetRecommendedPlayersQueryHandler.cs`](../src/NoobGg.Application/Features/Recommendations/Queries/GetRecommendedPlayers/GetRecommendedPlayersQueryHandler.cs):

- Kullanıcının **oyun profili yoksa** boş liste döner.
- Adaylar: kullanıcının oynadığı **oyunlardan en az birini paylaşan** diğer kullanıcılar (`UserGameProfile` üzerinden).
- Hariç tutulanlar: kendisi, blok ilişkisi, **deaktif** ayarı olanlar, **Private** profil görünürlüğü olanlar.
- Aday filtreleri: `User` için e-posta doğrulanmış, banlı değil.
- **Skorlama (deterministik):**
  - Ortak oyun: +30; ek ortak oyun başına +5, toplam ek bonus en fazla +10
  - Adayın “üst” profili (LFT öncelikli, sonra `HoursPlayed`): bölge kullanıcının herhangi bir oyun profilindeki bölge setindeyse +20
  - Deneyim seviyesi: kullanıcının en çok saatli profilindeki seviye ile karşılaştırma — aynı +15, seviye farkı 1 ise +8, 2 ise +3
  - İletişim: tercih kullanıcının setindeyse +10; yoksa “Both” uyumu +6
  - İkisi de LFT ise +15
  - `IPresenceTracker` ile çevrimiçi ise +10
- Sıralama: skor azalan; eşitlikte `Random.Shared.Next()` ile karıştırma; sonra `Take(limit)`.
- DTO: `score`, `matchReasons`, oyun listesi, arkadaşlık durumu vb. — [`RecommendedPlayerResponse.cs`](../src/NoobGg.Application/Features/Recommendations/DTOs/RecommendedPlayerResponse.cs)

**Oda önerisi** — [`GetRecommendedRoomsQueryHandler.cs`](../src/NoobGg.Application/Features/Recommendations/Queries/GetRecommendedRooms/GetRecommendedRoomsQueryHandler.cs):

- Oyun profili yoksa boş liste.
- Aday odalar: **public**, **Closed değil**, kullanıcının **üye olmadığı** odalar; en yeni 100 oda çekilip skorlanır.
- **Skorlama:**
  - Oda oyunu kullanıcının oyunlarındaysa +35 (+ metin gerekçe)
  - Bölge eşleşmesi +20
  - Oda dili, kullanıcının oyun profillerindeki diller kümesindeyse +15
  - `Open` ve doluluk %80 altı +15; açık ama doluysa +5
  - Yaş: &lt;1 saat +15, &lt;6 saat +10, &lt;24 saat +5
- Sıralama: oyuncu önerisi ile aynı mantık.
- DTO: [`RecommendedRoomResponse.cs`](../src/NoobGg.Application/Features/Recommendations/DTOs/RecommendedRoomResponse.cs)

---

## 5. Frontend mimarisi (özet)

- **Sayfalar:** [`client/src/pages/`](../client/src/pages/)
- **Özellik modülleri:** [`client/src/features/`](../client/src/features/) — `api`, `hooks`, `types` (ve bazılarında `schemas`)
- **Ortak UI:** [`client/src/components/ui/`](../client/src/components/ui/), [`client/src/components/layout/`](../client/src/components/layout/), [`client/src/components/common/`](../client/src/components/common/)
- **Sorgu anahtarları:** [`client/src/lib/queryKeys.ts`](../client/src/lib/queryKeys.ts) — `recommendations.players(limit)`, `recommendations.rooms(limit)` eklendi.

**Öneri frontend’i** — [`client/src/features/recommendations/`](../client/src/features/recommendations/):

- `api.ts` → `GET /api/recommendations/players`, `GET /api/recommendations/rooms`
- `hooks.ts` → `useRecommendedPlayers`, `useRecommendedRooms` (staleTime ~5 dk)
- `types.ts` → API yanıtı tipleri

---

## 6. Sayfa bazlı işlevler ve UI güncellemeleri

Aşağıda her route için **ürün davranışı** (koddan çıkan) ve **yapılan görsel/UX cilası** (varsa) ayrı maddeler halinde verilir.

### Genel layout (tüm AppLayout altı)

- **[`navbar.tsx`](../client/src/components/layout/navbar.tsx):** Profil açılır menüsüne **Messages** kısayolu; `useConversations` ile **okunmamış DM sayısı** rozeti; `DropdownItem` bileşenine `badge` desteği.
- **[`sidebar.tsx`](../client/src/components/layout/sidebar.tsx):** Aktif link için **sol kenar primary çizgisi** + `rounded-lg` uyumu (navigasyon vurgusu).

### Paylaşılan bileşenler (cilalananlar)

- **Button, Input, Select, Textarea:** `rounded-md` → `rounded-lg` ([`button.tsx`](../client/src/components/ui/button.tsx), [`input.tsx`](../client/src/components/ui/input.tsx), vb.)
- **Card:** `rounded-lg` → `rounded-xl`, padding `p-4` → `p-5` ([`card.tsx`](../client/src/components/ui/card.tsx))
- **Badge:** `default` varyantı kontrast iyileştirmesi ([`badge.tsx`](../client/src/components/ui/badge.tsx))
- **Modal:** `rounded-xl`, gölge güçlendirildi ([`modal.tsx`](../client/src/components/ui/modal.tsx))
- **EmptyState:** İkon için kutu, tipografi aralığı ([`emptyState.tsx`](../client/src/components/common/emptyState.tsx))
- **GameCard:** Sürekli gradient overlay, Metacritic rozeti, `aspect-video`, hover ölçek süresi ([`gameCard.tsx`](../client/src/components/common/gameCard.tsx))

---

### `/` – Landing

- Genel tanıtım / girişe yönlendirme sayfası (`LandingPage`). Bu rapor kapsamında sayfaya özel dosya düzeltmesi kaydı yok.

### `/login`, `/register` – AuthLayout

- Kimlik doğrulama formları. Rapor kapsamında bu sayfalara özel cilası kaydı yok.

### `/verify-email`

- E-posta doğrulama akışı.

### `/onboarding` – ProtectedRoute

- Yeni kullanıcı profil tamamlama.

### `/discover` – DiscoverPage (RequireProfile)

**Ürün:** Oyunlar sekmesi (browse, arama, tür/platform/chip filtreleri, sayfalama); Oyuncular sekmesi (`useDiscoverPlayers`, filtreler, oyun filtre dropdown’u, LFT, bölge, seviye). Oyun kartları `GameCard` ile.

**Bu çalışmada:**

- Hero: daha kompakt; başlık/alt başlık ve `StatPill` yerleşimi; arama kutusu stili.
- Filtre çubuğu: `border-border/70`, `bg-surface/80`.
- Yükleme: `LoadingGrid` / `LoadingPlayerGrid`; boş durum: `DiscoverEmpty`.
- **Önerilen oyuncular:** Giriş + `activeTab === 'players'` + `hasActiveFilters === false` iken `useRecommendedPlayers(6)` ile **“Recommended for you”** bölümü; `RecommendedPlayerCard` (primary tonlu kart, `matchReasons`, skor). Kaynak: [`discover.tsx`](../client/src/pages/discover.tsx).

### `/rooms` – RoomListPage (RequireProfile)

**Ürün:** Filtreler (bölge, dil, durum, oyun araması), `useRooms`, sayfalama, oda oluşturma modal’ı, `RoomCard` grid.

**Bu çalışmada:**

- Başlıkta toplam oda sayısı (`totalCount`).
- Filtre alanı çerçeveli grup (`rounded-xl border ... p-4`).
- `RoomCard`: kapasite çubuğu kalınlığı, durum rozeti görsel üzerinde, üye ikonu, dolu odada danger vurgu, “Join →” her zaman görünür, başlık hover’da primary, tarih kısa format.
- **Önerilen odalar:** Giriş + filtre yok + ilk sayfa → `useRecommendedRooms(6)`, `RecommendedRoomCard`. Kaynak: [`roomList.tsx`](../client/src/pages/roomList.tsx).

### `/rooms/:roomId` – RoomDetailPage (RequireProfile)

**Ürün:** Oda detayı, üyelik, sohbet (üye ise), SignalR oda hub ile uyumlu özellikler (ilgili hook’lar).

**Bu çalışmada:** Geri link pill stili; “Join to chat” boş durumu ikon kutusu ve hiyerarşi; yan panel başlıkları uppercase, sayım, online nokta pulse.

### `/games/:gameId` – GameDetailPage (RequireProfile)

- Tek oyun detayı ve ilgili aksiyonlar (feature hooks). Rapor kapsamında bu sayfaya özel cilası kaydı yok.

### `/profile/:userId` – ProfilePage (RequireProfile)

- Başka kullanıcı / kendi profilin görüntüleme, arkadaşlık, favori, engel, istatistikler. Bu rapor kapsamında bu sayfa için açık diff kaydı yok.

### `/profile/edit` – EditProfilePage

- Profil düzenleme formu.

### `/profile/games` – GameProfilesPage

- Oyun profilleri listesi / ekleme-düzenleme.

### `/messages` – MessagesPage (RequireProfile)

**Ürün:** Konuşma listesi, DM mesajları, SignalR DM.

**Bu çalışmada:** Kenar çubuğu genişliği (`w-72`), bağlantı durumu header’da; boş durumlar; baloncuk genişliği ve alıcı rengi (`bg-surface-hover`); composer alanı ve gönder ikonu; `ConversationItem` aktif kenarlık ve okunmamış vurgu; `wrap-break-word` sınıf uyumu.

### `/friends` – FriendsPage

- Arkadaş listesi ve istekler.

### `/favorites` – FavoritesPage

- Favori oyuncular.

### `/notifications` – NotificationsPage

- Bildirim listesi.

### `/settings` – SettingsPage

- Gizlilik, bildirim, hesap ayarları (settings feature).

### `/subscriptions` – SubscriptionsPage

- Abonelik planları (router’da `RequireProfile` yok).

### `/moderation` – ModerationPage (RequireRole)

- Moderatör rapor inceleme arayüzü.

---

## 7. Yol haritası biçiminde özet zaman çizelgesi (raporlama amaçlı)

Bu bölüm **tarih atamaz**; depodaki ve anlatılan işlerin mantıksal sırasını özetler.

1. **Çekirdek ürün:** Auth, profil, oyun kataloğu, odalar, sohbet, DM, arkadaşlık, bildirimler, abonelik, moderasyon — backend controller’lar ve eşlenik `features/*` ile.
2. **Ortak tasarım dili:** Tailwind tema değişkenleri, `ui` primitives, layout.
3. **Ürün yüzeyi cilası (bir çalışma turu):** Paylaşılan UI + Discover, Room list, Room detail, Messages, Navbar dropdown, Sidebar, GameCard, EmptyState.
4. **Öneri katmanı (bir çalışma turu):** Application `Recommendations` özelliği, `RecommendationsController`, client `features/recommendations`, Discover ve Room list’e bölümler, `queryKeys` genişlemesi.

---

## 8. Dosya referans özeti

| Alan | Ana dosyalar |
|------|----------------|
| Backend öneri | [`RecommendationsController.cs`](../src/NoobGg.Api/Controllers/RecommendationsController.cs), [`Features/Recommendations/`](../src/NoobGg.Application/Features/Recommendations/) |
| Frontend öneri | [`client/src/features/recommendations/`](../client/src/features/recommendations/), [`queryKeys.ts`](../client/src/lib/queryKeys.ts) |
| UI cilası sayfaları | [`discover.tsx`](../client/src/pages/discover.tsx), [`roomList.tsx`](../client/src/pages/roomList.tsx), [`roomDetail.tsx`](../client/src/pages/roomDetail.tsx), [`messages.tsx`](../client/src/pages/messages.tsx) |
| UI cilası layout/common | [`navbar.tsx`](../client/src/components/layout/navbar.tsx), [`sidebar.tsx`](../client/src/components/layout/sidebar.tsx), [`components/ui/`](../client/src/components/ui/), [`emptyState.tsx`](../client/src/components/common/emptyState.tsx), [`gameCard.tsx`](../client/src/components/common/gameCard.tsx) |

---

*Belge sonu: ileri yönlü öneri, test planı veya yol haritası tavsiyesi içermez.*
