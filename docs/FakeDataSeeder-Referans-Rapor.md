# FakeDataSeeder — Referans Raporu

Bu belge `FakeDataSeeder` ile MongoDB’ye eklenen geliştirme verileri için kalıcı referanstır. Kaynak kod: `src/NoobGg.Api/BackgroundJobs/FakeDataSeeder.cs`.

---

## 1. Giriş bilgileri (tüm kullanıcılar)

| Alan | Değer |
|------|--------|
| **Düz metin şifre** | `Test1234!` |
| **Hash** | Her kullanıcı kaydında `_hasher.Hash("Test1234!")` çağrılır. BCrypt her çağrıda farklı salt kullandığı için `PasswordHash` alanı kullanıcıdan kullanıcıya **farklı string** olabilir; **giriş için her zaman düz metin `Test1234!` kullanılır.** |

**Not:** Seeder bir kez çalıştıysa ve API tekrar başlatıldığında kullanıcı sayısı zaten ≥ 1000 ise seeder tekrar çalışmaz; eklenen hesaplar bu kalıplara göre kalır.

---

## 2. Kullanıcı kimlik kalıpları (i = 0 … 999)

Döngü indeksi `i` ile üretilir.

| Alan | Format | Örnek (i = 0) | Örnek (i = 999) |
|------|--------|---------------|-----------------|
| **E-posta** | `player{i:D4}@noobgg.test` | `player0000@noobgg.test` | `player0999@noobgg.test` |
| **Kullanıcı adı** | `Player_{i:D4}` | `Player_0000` | `Player_0999` |
| **E-posta doğrulandı** | Evet (`IsEmailVerified = true`) | | |
| **Profil tamam** | Evet (`IsProfileComplete = true`) | | |
| **Banlı** | Hayır (`IsBanned = false`) | | |

İndeks `i`, e-posta ve kullanıcı adındaki dört haneli sayı ile **bire bir** eşleşir (`player0420@noobgg.test` ↔ `Player_0420`).

---

## 3. Roller (indeks i’ye göre)

| i aralığı | Rol (enum) |
|-----------|------------|
| `0`, `1` | Admin |
| `2`, `3`, `4` | Moderator |
| `5` … `999` | User |

### Sabit örnekler (hızlı test)

| i | E-posta | Kullanıcı adı | Rol |
|---|---------|---------------|-----|
| 0 | player0000@noobgg.test | Player_0000 | Admin |
| 1 | player0001@noobgg.test | Player_0001 | Admin |
| 2 | player0002@noobgg.test | Player_0002 | Moderator |
| 3 | player0003@noobgg.test | Player_0003 | Moderator |
| 4 | player0004@noobgg.test | Player_0004 | Moderator |
| 5 | player0005@noobgg.test | Player_0005 | User |

---

## 4. Seeder’ın çalışma koşulu

- `users` koleksiyonundaki belge sayısı **≥ 1000** ise seeder **hiçbir şey eklemez** (idempotent atlama).
- Aksi halde tam paket eklenir (aşağıdaki bölüm 6’daki hedef hacimler).

---

## 5. Rastgele üretim (tutarlı seed)

- `Random` örneği `new Random(42)` ile sabitlenir; aynı kod ve boş DB ile tekrarlanabilir bir dağılım hedeflenir (gerçek Id’ler GUID olduğu için tam birebir klon yoktur, sayısal/ilişkisel dağılım benzer kalır).

---

## 6. Eklenen veri hacmi (tek başarılı koşu için tipik değerler)

Aşağıdaki sayılar bir kez seed sonrası loglardan veya koddaki hedeflerle uyumludur; oyun sayısı DB’de zaten yeterliyse ek oyun eklenmez.

| Koleksiyon / veri | Yaklaşık miktar |
|-------------------|-----------------|
| Kullanıcı | 1000 |
| UserProfile | 1000 |
| UserSettings | 1000 |
| UserGameProfile | Kullanıcı başına 1–4 oyun (~2500+) |
| Room | 150 |
| RoomMember | ~500+ |
| Message (oda sohbeti) | ~1700+ |
| Friendship | ~500 |
| Conversation (DM) | 300 |
| DirectMessage | ~1700+ |
| Notification | 400 |
| Favorite | 200 |

Oyunlar: koleksiyonda en az 10 oyun varsa, seed sırasında **mevcut oyunlardan (ör. ilk 500)** kullanılır; yoksa kod içi `GenerateFakeGames()` ile yerel oyun listesi eklenir.

---

## 7. Profil ve ayar özeti (üretim kuralları)

- **UserProfile:** `DisplayName` = `Player {i:D4}`, rastgele ülke / timezone / bio havuzları, isteğe bağlı `Availability` (weekdays/weekends).
- **UserSettings:** çoğunlukla herkese açık profil; küçük oranda `Private`; `DmPermission`, bildirim bayrakları ve `DefaultLookingForTeam` rastgele dağılımlı.
- **UserGameProfile:** seçilen oyunlara rank, rol, bölge, dil(ler), deneyim seviyesi, iletişim tercihi, `LookingForTeam`, oyun saati.

---

## 8. Odalar ve etkileşimler

- **Rooms:** başlık oyun adı + bölge + tip etiketi; `Region`, `Language`, `Status` (Open/Full/InProgress/Closed), tag havuzu.
- **RoomMember:** oluşturucu Owner, diğerleri Member; üye sayısı odanın `MaxMembers` ile uyumlu üretilir.
- **Room messages:** kapalı olmayan odalarda 0–24 arası mesaj; gönderenler odanın üyeleri arasından.
- **Friendships:** benzersiz çiftler, kabaca %75 `Accepted`.
- **DM:** 300 benzersiz konuşma çifti, konuşma başına 1–11 mesaj; son mesaj alanları güncellenir.

---

## 9. 1000 kullanıcı listesi çıkarma (dosyada tek tek yazmak yerine)

Tüm hesaplar aynı şifreyi paylaştığı için raporda 1000 satır tekrar etmek gerekmez. İhtiyaç halinde:

**Elektronik tablo / CSV:** A sütununa `0`–`999` yazıp formül ver:

- E-posta: `="player"&TEXT(A2,"0000")&"@noobgg.test"`
- Kullanıcı adı: `="Player_"&TEXT(A2,"0000")`
- Şifre (tüm satırlar): `Test1234!`

**MongoDB (örnek):** `users` koleksiyonunda `@noobgg.test` ile filtreleyip `Email`, `Username`, `Role` projeksiyonu alınabilir.

---

## 10. Güvenlik uyarısı

Bu hesaplar ve şifre **yalnızca yerel/geliştirme** ortamı içindir. Üretim ortamında bu seeder çalışmamalı veya yapılandırmayla kapatılmalıdır; `noobgg.test` alan adı gerçek e-posta değildir.

---

*Belge, `FakeDataSeeder` ile uyumlu tutulmak üzere kod değişince güncellenmelidir.*
