using Bancada.Domain;

namespace Bancada.Domain.Tests;

public sealed class ChallengeTests
{
    [Fact]
    public void Active_challenge_accepts_submissions_during_its_period()
    {
        var now = DateTimeOffset.UtcNow;
        var challenge = new Challenge("Até a última migalha", "Use pão amanhecido.", now.AddDays(-1), now.AddDays(5), ChallengeStatus.Active);

        Assert.True(challenge.AcceptsSubmissions(now));
    }

    [Fact]
    public void Closed_challenge_does_not_accept_submissions()
    {
        var now = DateTimeOffset.UtcNow;
        var challenge = new Challenge("Sabores do quintal", "Use ervas frescas.", now.AddDays(-7), now.AddDays(-1), ChallengeStatus.Closed);

        Assert.False(challenge.AcceptsSubmissions(now));
    }
}
