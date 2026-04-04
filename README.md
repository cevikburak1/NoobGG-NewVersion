<div align="center">

# 🎮 NoobGg

### Oyuncu eşleştirme · Canlı sohbet · Topluluk — hepsi tek yerde

**Oda aç, takım kur, DM at, bildirim al.**  
Modern **ASP.NET Core** + **React 19** yığını; **SignalR** ile gerçek zamanlı; **MongoDB** ve **Redis** ile ölçeklenebilir altyapı.

<br/>

[![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React_19-61DAFB?style=for-the-badge&logo=react&logoColor=222222)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![MongoDB](https://img.shields.io/badge/MongoDB-47A248?style=for-the-badge&logo=mongodb&logoColor=white)](https://www.mongodb.com/)
[![Redis](https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white)](https://redis.io/)
[![Vite](https://img.shields.io/badge/Vite_6-646CFF?style=for-the-badge&logo=vite&logoColor=white)](https://vitejs.dev/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind_v4-06B6D4?style=for-the-badge&logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)

<br/>

[Özellikler](#-özellikler) · [Mimari](#-mimari) · [Hızlı başlangıç](#-hızlı-başlangıç) · [Dokümantasyon](#-dokümantasyon)

</div>

---

## ✨ Özellikler

| | |
|:---|:---|
| 🎯 **Akıllı öneriler** | Ortak oyunlar, bölge, deneyim ve çevrimiçi duruma göre oyuncu & oda önerileri |
| 🏠 **Odalar** | Oluştur, katıl, davet et; oda içi sohbet |
| 💬 **Gerçek zamanlı** | SignalR hub’ları: sohbet, oda, DM, bildirimler |
| 👤 **Profiller** | Oyuncu profili, oyun profilleri, avatar & banner |
| 🤝 **Sosyal** | Arkadaşlık, engelleme, favoriler |
| 🔔 **Bildirimler** | Uygulama içi bildirim akışı |
| 💳 **Abonelik** | Planlar ve abonelik yönetimi |
| 🛡️ **Moderasyon** | Şikayet ve moderasyon akışları (rol bazlı) |
| 🔐 **Güvenlik** | JWT kimlik doğrulama; korumalı rotalar ve profil zorunluluğu |

---

## 🧱 Mimari

Katmanlı **Clean Architecture** hissi: Domain → Application → Infrastructure → API; frontend **feature-first** modüller ve paylaşılan UI bileşenleri.

```mermaid
flowchart LR
  subgraph client["🖥️ Client"]
    R[React 19 + Vite 6]
    T[TanStack Query]
    Z[Zustand]
    SR[@microsoft/signalr]
  end

  subgraph api["⚙️ NoobGg.Api"]
    C[Controllers]
    H[SignalR Hubs]
  end

  subgraph data["💾 Veri & önbellek"]
    M[(MongoDB)]
    RD[(Redis)]
  end

  R --> C
  SR --> H
  C --> M
  H --> RD
```

**Hub uçları (örnek):** `/hubs/chat` · `/hubs/room` · `/hubs/dm` · `/hubs/notifications`

---

## 🚀 Hızlı başlangıç

### Gereksinimler

- [.NET SDK](https://dotnet.microsoft.com/download) (backend için)
- [Node.js](https://nodejs.org/) 20+ (frontend için)
- [Docker](https://www.docker.com/) — API + MongoDB + Redis’i tek komutla açmak için

### API + veritabanları (Docker)

```bash
docker compose up --build
```

API varsayılan olarak **5000** portunda çalışacak şekilde yapılandırılmıştır; MongoDB **27017**, Redis **6379**.

### Frontend (geliştirme)

```bash
cd client
# .env oluştur: .env.example içeriğini kopyalayın; Docker API için VITE_API_URL=http://localhost:5000
npm install
npm run dev
```

Frontend, `VITE_API_URL` ile backend ve SignalR taban adresini alır ([`client/.env.example`](client/.env.example)).

### Çözüm dosyası

```bash
# Tüm .NET projeleri
dotnet build NoobGg.sln
```

---

## 📁 Repo yapısı (özet)

```
├── client/                 # React SPA (Vite, TypeScript, Tailwind v4)
├── src/
│   ├── NoobGg.Api/         # HTTP API, hub’lar, middleware
│   ├── NoobGg.Application/
│   ├── NoobGg.Domain/
│   └── NoobGg.Infrastructure/
├── docs/                   # Proje raporları ve referanslar
├── docker-compose.yml
└── NoobGg.sln
```

---

## 📚 Dokümantasyon

| Belge | İçerik |
|--------|--------|
| [`docs/NoobGg-Proje-Raporu.md`](docs/NoobGg-Proje-Raporu.md) | Ürün tanımı, API yüzeyi, öneri sistemi, frontend özeti |
| [`docs/FakeDataSeeder-Referans-Rapor.md`](docs/FakeDataSeeder-Referans-Rapor.md) | Seeder / test verisi referansı |
| [`NoobGgProductAnalysisReport.md`](NoobGgProductAnalysisReport.md) | Ürün analizi raporu |

---

<div align="center">

**NoobGg** — *“Noob” değil, takımın parçası ol.*

🌟 Yıldız atmayı unutma — fork’lamak serbest.

</div>

