using GlobalPulse.Api.Services;
using Xunit;

namespace GlobalPulse.Tests;

public class EventClassifierTests
{
    [Theory]
    [InlineData("news", "Earthquake strikes coast", "disaster")]
    [InlineData("disaster", "Weekly summary", "disaster")]
    [InlineData("news", "Missile attack reported", "conflict")]
    [InlineData("news", "Ransomware hits hospital", "cyber")]
    [InlineData("news", "Parliament passes treaty", "political")]
    [InlineData("news", "Local bakery opens", "news")]
    public void ClassifyCategory_picks_expected_category(string feed, string title, string expected) =>
        Assert.Equal(expected, EventClassifier.ClassifyCategory(feed, title));

    [Theory]
    [InlineData("Nuclear plant incident", 5)]
    [InlineData("Dozens killed in storm", 4)]
    [InlineData("Explosion downtown", 3)]
    [InlineData("Weather warning issued", 2)]
    [InlineData("Quiet day", 1)]
    public void GuessSeverity_scores_titles(string title, int expected) =>
        Assert.Equal(expected, EventClassifier.GuessSeverity(title));

    [Fact]
    public void StripHtml_removes_tags_and_trims() =>
        Assert.Equal("Hello world", EventClassifier.StripHtml("  <p>Hello <b>world</b></p> "));

    [Theory]
    [InlineData("short", 10, "short")]
    [InlineData("exactly10!", 10, "exactly10!")]
    [InlineData("this is too long", 4, "this")]
    [InlineData("", 5, "")]
    public void Truncate_never_throws_and_caps_length(string input, int max, string expected) =>
        Assert.Equal(expected, EventClassifier.Truncate(input, max));

    [Fact]
    public void ExtractCountry_finds_name_ignoring_case() =>
        Assert.Equal("France", EventClassifier.ExtractCountry("talks held in FRANCE today"));

    [Fact]
    public void ExtractCountry_returns_null_when_none() =>
        Assert.Null(EventClassifier.ExtractCountry("nothing relevant here"));
}
