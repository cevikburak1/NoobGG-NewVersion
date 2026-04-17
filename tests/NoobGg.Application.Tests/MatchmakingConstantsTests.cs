using NoobGg.Application.Common;
using Xunit;

namespace NoobGg.Application.Tests;

public class MatchmakingConstantsTests
{
    [Fact]
    public void Fallback_after_seconds_matches_product_default()
    {
        Assert.Equal(45, MatchmakingConstants.FallbackAfterSeconds);
    }
}
