namespace Cominomi.Shared.Tests;

public class TokenEstimatorTests
{
    [Fact]
    public void Estimate_NullOrEmpty_ReturnsZero()
    {
        Assert.Equal(0, TokenEstimator.Estimate(null));
        Assert.Equal(0, TokenEstimator.Estimate(""));
    }

    [Fact]
    public void Estimate_AsciiOnly_UsesAsciiRatio()
    {
        // 40 ASCII chars / 4.0 = 10 tokens
        var text = new string('a', 40);
        Assert.Equal(10, TokenEstimator.Estimate(text));
    }

    [Fact]
    public void Estimate_KoreanOnly_UsesNonAsciiRatio()
    {
        // 15 Korean chars / 1.5 = 10 tokens
        var text = new string('가', 15);
        Assert.Equal(10, TokenEstimator.Estimate(text));
    }

    [Fact]
    public void Estimate_MixedText_CombinesBothRatios()
    {
        // 8 ASCII / 4.0 = 2, 3 Korean / 1.5 = 2 → 4 tokens
        var text = "Hello!! 안녕하";
        Assert.Equal(4, TokenEstimator.Estimate(text));
    }

    [Fact]
    public void Truncate_ShortText_ReturnsOriginal()
    {
        var text = "Hello world";
        var result = TokenEstimator.Truncate(text, 100);
        Assert.Equal(text, result);
    }

    [Fact]
    public void Truncate_LongText_TruncatesWithMarker()
    {
        var text = new string('a', 400); // 100 tokens
        var result = TokenEstimator.Truncate(text, 10);
        Assert.Contains("[...truncated,", result);
        Assert.True(result.Length < text.Length);
    }

    [Fact]
    public void Truncate_KoreanText_TruncatesCorrectly()
    {
        var text = new string('가', 150); // 100 tokens
        var result = TokenEstimator.Truncate(text, 10);
        Assert.Contains("[...truncated,", result);
        Assert.True(result.Length < text.Length);
    }

    [Fact]
    public void Truncate_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, TokenEstimator.Truncate(null!, 100));
        Assert.Equal(string.Empty, TokenEstimator.Truncate("", 100));
    }

    [Fact]
    public void Truncate_MarkerShowsTokenCount()
    {
        var text = new string('a', 400); // 100 tokens
        var result = TokenEstimator.Truncate(text, 10);
        Assert.Contains("100 tokens total", result);
    }
}
