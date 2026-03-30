using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Elo.Helpers;

public static class EloCalculator
{
    private const int KFactor = 32;

    public static (int change1, int change2) Calculate(int elo1, int elo2, bool player1Won)
    {
        var expected1 = 1.0 / (1.0 + Math.Pow(10, (elo2 - elo1) / 400.0));
        var expected2 = 1.0 - expected1;

        var score1 = player1Won ? 1.0 : 0.0;
        var score2 = 1.0 - score1;

        var change1 = (int)Math.Round(KFactor * (score1 - expected1));
        var change2 = (int)Math.Round(KFactor * (score2 - expected2));

        return (change1, change2);
    }

    public static RankTier GetTier(int eloPoints) => eloPoints switch
    {
        < 1000 => RankTier.Bronze,
        < 1500 => RankTier.Silver,
        < 2000 => RankTier.Gold,
        < 2500 => RankTier.Platinum,
        < 3000 => RankTier.Diamond,
        < 3500 => RankTier.Master,
        _ => RankTier.Grandmaster
    };
}
