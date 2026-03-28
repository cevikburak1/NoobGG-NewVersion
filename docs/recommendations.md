# Recommendation System - Scoring Design & Future Notes

## Overview

Lightweight, deterministic recommendation engine for NoobGg. No ML, no external dependencies.
Recommendations are computed on-the-fly per request based on the authenticated user's game profiles.

## Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/recommendations/players` | Required | Recommended players for the current user |
| GET | `/api/recommendations/rooms` | Required | Recommended rooms for the current user |

Both accept optional `?gameId=` filter and `?limit=` (1-50, default 10).

---

## Player Scoring (max ~100 points)

| Factor | Points | Condition |
|--------|--------|-----------|
| Shared game(s) | +30 | At least one game in common (required to appear) |
| Same region | +20 | Best-match game profile shares a region |
| Same experience level | +15 | Exact match on the shared game |
| Similar experience level | +8 | One tier apart (e.g., Intermediate vs Advanced) |
| Shared language(s) | +10/lang (max 20) | Languages overlap between game profiles |
| Communication compatibility | +10 | Matching comm preference or either has "Both" |
| Looking for team | +10 | Candidate has LFT enabled |
| Currently online | +5 | Real-time presence check |

### Exclusions

- Self
- Blocked users (both directions)
- Already-friends
- Deactivated accounts
- Private profiles
- Unverified / banned users

### Tiebreaking

Equal scores are shuffled randomly (`Guid.NewGuid()`) to ensure variety on repeated requests.

---

## Room Scoring (max ~90 points)

| Factor | Points | Condition |
|--------|--------|-----------|
| Plays the game | +30 | Room's game matches one of user's game profiles |
| Same region | +20 | Room region matches any of user's game profile regions |
| Language match | +15 | Room language is in user's known languages |
| Rank fits range | +10 | User's rank for that game falls within room's rank range |
| Available capacity | 0-10 (scaled) | `(spotsLeft / maxMembers) * 10` |
| Just created (<1h) | +5 | Room age < 1 hour |
| Recently created (<6h) | +3 | Room age < 6 hours |

### Exclusions

- Rooms the user already joined
- Rooms the user created
- Non-public rooms
- Non-open rooms (closed, full, in-progress)

### Rank Comparison

Current implementation uses lexicographic comparison. A future improvement should use
game-specific rank ordinal maps (e.g., Iron=1, Bronze=2, Silver=3, ..., Radiant=9 for Valorant).

---

## Architecture

```
Frontend (React)
  └─ GET /api/recommendations/players|rooms
        └─ RecommendationsController
              └─ MediatR Send
                    └─ GetRecommendedPlayersQueryHandler / GetRecommendedRoomsQueryHandler
                          └─ MongoDB queries + in-memory scoring
                          └─ IPresenceTracker (for online status)
```

No new database collections, no new services to register.
Handlers are auto-discovered by MediatR's assembly scanning.
Validators are auto-discovered by FluentValidation's assembly scanning.

---

## Frontend

- Route: `/recommendations`
- Sidebar: "For You" link with sparkle icon
- Tabs: Players | Rooms
- Game filter dropdown (search + select)
- Cards show score badge and match reasons
- Auto-refresh every 60 seconds

---

## Future Extension Notes

### Short-term improvements

1. **Game-specific rank ordinals**: Replace lexicographic rank comparison with per-game
   rank tier mappings for accurate rank-range matching.

2. **Caching**: Cache recommendation results in Redis for 1-5 minutes per user+gameId
   to reduce MongoDB load under heavy traffic.

3. **Pagination**: Current implementation returns a flat list (up to 50). Add cursor-based
   pagination for larger result sets.

4. **Activity-based scoring**: Factor in `LastLoginAt` and `HoursPlayed` for better
   recency and engagement signals.

### Medium-term

5. **Interaction feedback loop**: Track "viewed profile", "sent friend request", "joined room"
   events from recommendations. Use click-through rate to tune scoring weights.

6. **Subscription tier boost**: Use existing `PremiumFeature.PriorityMatchmaking` entitlement
   to give premium users a slight boost in others' recommendation lists.

7. **Tag-based room scoring**: Score rooms higher when their tags match the user's
   preferred play styles or game roles.

8. **Availability window matching**: Use `UserProfile.Availability` to boost players
   whose available times overlap with the current user.

### Long-term (if ML is desired)

9. **Collaborative filtering**: "Users who played with X also played with Y" based on
   room membership history.

10. **Embedding-based similarity**: Compute user embeddings from game profiles, ranks,
    playtime, and interactions for nearest-neighbor recommendations.

11. **A/B testing framework**: Serve different scoring weights to different user cohorts
    and measure engagement metrics.
