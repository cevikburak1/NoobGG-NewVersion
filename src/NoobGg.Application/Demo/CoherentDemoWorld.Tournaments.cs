using NoobGg.Application.Common.Constants;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Demo;

public sealed partial class CoherentDemoWorld
{
    private void BuildTournaments(List<User> users, List<Game> games)
    {
        var guilds = Rows<Guild>(CollectionNames.Guilds);
        for (var c = 0; c < 10; c++)
        {
            var cohort = users.Skip(c * 20).Take(20).ToList();
            var season = Add(CollectionNames.Seasons, $"g{c}", new Season { Name = "Demo Topluluk Sezonu", GameId = games[c].Id,
                StartsAt = Epoch.AddDays(-40), EndsAt = Epoch.AddDays(50) }, Epoch.AddDays(-50));
            foreach (var state in new[] { TournamentStatus.Registration, TournamentStatus.InProgress, TournamentStatus.Completed })
            {
                var entrants = state == TournamentStatus.Registration ? cohort.Take(16).ToList() : state == TournamentStatus.InProgress ? cohort.Skip(12).Take(8).ToList() : cohort.Take(8).ToList();
                var tournament = Add(CollectionNames.Tournaments, $"{c}-{state}", new Tournament { Name = $"{games[c].Name} — {state switch { TournamentStatus.Registration => "Yeni Sezon Kupası", TournamentStatus.InProgress => "Hafta Ortası Kupası", _ => "Geçen Hafta Kupası" }}",
                    Description = "Demo topluluk turnuvası. Kayıt için ilgili oyun profili gerekir. Katılımcılar ve tur sonuçları aynı turnuvanın gerçek demo kayıtlarına bağlıdır.",
                    GameId = games[c].Id, OrganizerId = cohort[0].Id, GuildId = guilds[c].Id, SeasonId = season.Id,
                    Format = TournamentFormat.SingleElimination, Status = state, MaxParticipants = state == TournamentStatus.Registration ? 32 : 8,
                    CurrentParticipants = entrants.Count, RegistrationDeadline = state == TournamentStatus.Registration ? Epoch.AddDays(3) : state == TournamentStatus.InProgress ? Epoch.AddDays(-2) : Epoch.AddDays(-12),
                    StartsAt = state == TournamentStatus.Registration ? Epoch.AddDays(4) : state == TournamentStatus.InProgress ? Epoch.AddDays(-1) : Epoch.AddDays(-10),
                    CurrentRound = state == TournamentStatus.Registration ? 0 : state == TournamentStatus.InProgress ? 1 : 3, TotalRounds = state == TournamentStatus.Registration ? 0 : 3,
                    PrizeBadges = ["Community Champion", "Finalist"] }, Epoch.AddDays(-20));
                var entries = entrants.Select((user, n) => Add(CollectionNames.TournamentEntries, $"{tournament.Id}-{n}", new TournamentEntry {
                    TournamentId = tournament.Id, ParticipantId = user.Id, EntryType = TournamentEntryType.Player, Seed = n + 1
                }, Epoch.AddDays(-15).AddMinutes(n))).ToList();
                if (state == TournamentStatus.Registration) continue;
                var rounds = new List<List<TournamentMatch>>();
                for (var round = 1; round <= 3; round++)
                {
                    var matches = new List<TournamentMatch>();
                    for (var m = 0; m < (8 >> round); m++)
                    {
                        var p1 = round == 1 ? entrants[m * 2].Id : rounds[round - 2][m * 2].WinnerId;
                        var p2 = round == 1 ? entrants[m * 2 + 1].Id : rounds[round - 2][m * 2 + 1].WinnerId;
                        var finished = state == TournamentStatus.Completed || round == 1 && m < 2;
                        var match = Add(CollectionNames.TournamentMatches, $"{tournament.Id}-{round}-{m}", new TournamentMatch { TournamentId = tournament.Id,
                            Round = round, MatchNumber = m + 1, Participant1Id = p1, Participant2Id = p2,
                            WinnerId = finished ? p1 : null, Status = finished ? TournamentMatchStatus.Completed : round == 1 ? TournamentMatchStatus.InProgress : TournamentMatchStatus.Pending },
                            tournament.StartsAt!.Value.AddHours(round));
                        if (finished && p2 != null)
                        {
                            var loser = entries.Single(e => e.ParticipantId == p2); loser.IsEliminated = true;
                            loser.Placement = round == 1 ? 5 : round == 2 ? 3 : 2;
                        }
                        matches.Add(match);
                    }
                    rounds.Add(matches);
                }
                for (var round = 0; round < 2; round++)
                    for (var m = 0; m < rounds[round].Count; m++) rounds[round][m].NextMatchId = rounds[round + 1][m / 2].Id;
                if (state == TournamentStatus.Completed)
                {
                    var champion = entries.Single(e => e.ParticipantId == rounds[2][0].WinnerId);
                    champion.Placement = 1; champion.EarnedBadges = ["Community Champion"];
                    entries.Single(e => e.Placement == 2).EarnedBadges = ["Finalist"];
                }
            }
        }
    }
}
