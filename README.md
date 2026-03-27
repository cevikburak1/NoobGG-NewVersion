<div align="center">

# 🎮 NoobGg

### Find your perfect squad. Build rooms. Chat in real time. Play smarter.

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/React-18+-61DAFB?style=for-the-badge&logo=react&logoColor=black" />
  <img src="https://img.shields.io/badge/TypeScript-Ready-3178C6?style=for-the-badge&logo=typescript&logoColor=white" />
  <img src="https://img.shields.io/badge/MongoDB-Database-47A248?style=for-the-badge&logo=mongodb&logoColor=white" />
  <img src="https://img.shields.io/badge/Redis-Cache-DC382D?style=for-the-badge&logo=redis&logoColor=white" />
  <img src="https://img.shields.io/badge/SignalR-Realtime-7A42F4?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Docker-Containerized-2496ED?style=for-the-badge&logo=docker&logoColor=white" />
</p>

<p align="center">
  <b>NoobGg</b> is a modern teammate-finding platform for gamers.  
  Players can discover compatible teammates, create rooms, join parties, chat in real time, manage game-specific profiles, and build better squads faster.
</p>

---

[✨ Features](#-features) •
[🧱 Architecture](#-architecture) •
[⚙️ Tech-Stack](#️-tech-stack) •
[🚀 Getting-Started](#-getting-started) •
[📸 Screenshots](#-screenshots) •
[🗺️ Roadmap](#️-roadmap)

</div>

---

# 📖 About The Project

**NoobGg** is a gaming social platform designed to help players find the right teammates based on:

- 🎯 game preference
- 🏆 rank / skill level
- 🌍 region
- 🗣️ language
- 🎙️ voice / text communication preference
- ⏰ play schedule
- 🎮 game-specific profiles

Instead of randomly adding players or using scattered chat servers, users can join a platform built specifically for **team discovery**, **room creation**, and **real-time communication**.

---

# ✨ Features

## 👤 User System
- Register / login
- Email verification
- Onboarding flow
- Profile completion
- JWT authentication
- Role-based authorization

## 🧾 Profiles
- Public player profiles
- Bio, region, languages, schedule
- Online / active visibility
- Game-specific profile cards
- Rank, role, playstyle, communication preferences

## 🎮 Game Discovery
- Steam-synced game catalog
- Search and filter supported games
- Game detail pages
- Game-based player discovery

## 🔎 Discover Players
- Filter players by:
  - game
  - rank
  - region
  - language
  - activity
  - communication style

## 🏠 Room System
- Create rooms
- Join rooms
- Leave rooms
- Close rooms
- View room members
- Max room capacity support

## 💬 Realtime Communication
- Room chat
- Direct messages
- Live presence / online state
- Toast notifications
- Unread message tracking

## 🛡️ Safety & Moderation
- Report users
- Block users
- Moderation-ready backend structure
- Safer interaction flow

## 💎 Subscription Ready
- Plan page
- Free / Plus / Pro concept
- Backend subscription foundation
- Monetization-ready structure

---

# 🎯 Why NoobGg?

Finding teammates in multiplayer games is often messy, slow, and unreliable.

**NoobGg solves this by combining:**
- player discovery
- game-specific identity
- room-based coordination
- direct messaging
- real-time social interaction

All in one product-focused platform.

---

# 🧱 Architecture

NoobGg is built with a **modern full-stack architecture** focused on scalability, maintainability, and real-time user experience.

## Backend
- **ASP.NET Core (.NET 8)**
- **CQRS**
- **MongoDB**
- **Redis**
- **SignalR**
- **JWT Authentication**
- **Docker**

## Frontend
- **React**
- **TypeScript**
- **Vite**
- **Tailwind CSS**
- **React Router**
- **TanStack Query**
- **Zod / React Hook Form**
- **Framer Motion**

---

# ⚙️ Tech Stack

<div align="center">

| Layer | Technologies |
|------|-------------|
| Frontend | React, TypeScript, Vite, Tailwind CSS, TanStack Query |
| Backend | ASP.NET Core, CQRS, SignalR, JWT |
| Database | MongoDB |
| Cache / Presence | Redis |
| Realtime | SignalR |
| DevOps | Docker, Docker Compose |
| Validation | FluentValidation / Zod |
| UI / UX | Responsive dark gaming-style interface |

</div>

---

# 📂 Project Structure

```bash
NoobGg/
├── backend/
│   ├── NoobGg.Api
│   ├── NoobGg.Application
│   ├── NoobGg.Domain
│   ├── NoobGg.Infrastructure
│   └── NoobGg.Persistence
│
├── frontend/
│   └── noobgg-web
│
├── docker-compose.yml
└── README.md
```

---

# 🚀 Getting Started

## Prerequisites

Make sure you have installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/)
- [Node.js](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/)
- [MongoDB](https://www.mongodb.com/) or Docker version
- [Redis](https://redis.io/) or Docker version

---

## 1️⃣ Clone the repository

```bash
git clone https://github.com/your-username/noobgg.git
cd noobgg
```

---

## 2️⃣ Start dependencies with Docker

```bash
docker-compose up -d
```

This will start:
- MongoDB
- Redis
- other configured containers

---

## 3️⃣ Run backend

```bash
cd backend
dotnet restore
dotnet run
```

---

## 4️⃣ Run frontend

```bash
cd frontend/noobgg-web
npm install
npm run dev
```

---

# 🔐 Environment Variables

Example backend configuration:

```env
ASPNETCORE_ENVIRONMENT=Development
MongoDb__ConnectionString=mongodb://localhost:27017
MongoDb__DatabaseName=NoobGgDb
Redis__ConnectionString=localhost:6379
Jwt__Issuer=NoobGg
Jwt__Audience=NoobGgUsers
Jwt__Secret=your-super-secret-key
```

Example frontend configuration:

```env
VITE_API_BASE_URL=http://localhost:5000
VITE_SIGNALR_URL=http://localhost:5000/hubs
```

---

# 🧪 Main User Flow

1. User registers
2. Email verification is completed
3. Profile information is filled in
4. User adds game profiles
5. User searches players / games / rooms
6. User creates or joins a room
7. Users communicate in real time
8. Users can continue chatting via direct messages

---

# 📸 Screenshots

> Add your screenshots here for a more impressive GitHub page.

## Discover
```md
![Discover](./docs/screenshots/discover.png)
```

## Player Profile
```md
![Profile](./docs/screenshots/profile.png)
```

## Messages
```md
![Messages](./docs/screenshots/messages.png)
```

## Rooms
```md
![Rooms](./docs/screenshots/rooms.png)
```

## Subscription Plans
```md
![Plans](./docs/screenshots/plans.png)
```

---

# 🔄 Core Modules

- **Authentication**
- **Profile Management**
- **Game Catalog**
- **Game Profiles**
- **Player Discovery**
- **Room Management**
- **Realtime Chat**
- **Direct Messaging**
- **Presence Tracking**
- **Reports / Blocks**
- **Subscription Infrastructure**

---

# 🗺️ Roadmap

## ✅ Current
- [x] Register / login
- [x] Email verification
- [x] Profile onboarding
- [x] Game discovery
- [x] Player filtering
- [x] Room system
- [x] Room chat
- [x] Direct messaging
- [x] Presence / active users
- [x] Subscription base

## 🚧 In Progress
- [ ] Notification center
- [ ] Settings / privacy page
- [ ] Moderation UI
- [ ] Avatar upload
- [ ] Profile banner upload
- [ ] Realtime hardening
- [ ] UI polish

## 🔮 Planned
- [ ] Room invite system
- [ ] Favorite players
- [ ] Recommended players
- [ ] Recommended rooms
- [ ] Production monitoring
- [ ] Payment integration
- [ ] Advanced moderation tools

---

# 🛠️ Development Goals

NoobGg is being built with these priorities:

- clean architecture
- maintainable codebase
- scalable realtime communication
- strong UX for social gaming flows
- production readiness
- modular feature growth

---

# 🤝 Contributing

Contributions, ideas, and improvements are welcome.

If you'd like to contribute:

1. Fork the repository
2. Create your feature branch
3. Commit your changes
4. Push your branch
5. Open a pull request

---

# 📌 Use Cases

NoobGg can be used for:

- finding a duo / trio / squad
- discovering active players in a specific game
- joining small temporary parties
- organizing casual or competitive sessions
- building a more social multiplayer experience

---

# 🧠 Future Vision

The long-term goal of NoobGg is to become a complete social coordination platform for online gamers with:

- smarter teammate recommendations
- stronger social graph
- better moderation and safety
- richer game identity
- premium features and scalable community tools

---

# 📄 License

This project is licensed under the **MIT License**.  
You can change this section depending on your preferred license.

---

# 👨‍💻 Author

**Your Name**  
GitHub: [@your-github-username](https://github.com/your-github-username)

---

<div align="center">

### ⭐ If you like this project, consider giving it a star!

**NoobGg — Find teammates, build rooms, and play better together.**

</div>
