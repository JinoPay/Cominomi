using Seoro.Shared.Services.Git;

namespace Seoro.Shared.Tests;

public class WordDiffTests
{
    [Fact]
    public void Compute_IdenticalText_ReturnsAllUnchanged()
    {
        var (oldSeg, newSeg) = WordDiff.Compute("hello world", "hello world");
        Assert.Single(oldSeg);
        Assert.False(oldSeg[0].Changed);
        Assert.Equal("hello world", oldSeg[0].Text);
        Assert.Single(newSeg);
        Assert.False(newSeg[0].Changed);
    }

    [Fact]
    public void Compute_SingleWordChanged_HighlightsOnlyChangedWord()
    {
        var (oldSeg, newSeg) = WordDiff.Compute("hello world", "hello mars");

        var oldChanged = string.Concat(oldSeg.Where(s => s.Changed).Select(s => s.Text));
        var newChanged = string.Concat(newSeg.Where(s => s.Changed).Select(s => s.Text));

        Assert.Equal("world", oldChanged);
        Assert.Equal("mars", newChanged);

        var oldUnchanged = string.Concat(oldSeg.Where(s => !s.Changed).Select(s => s.Text));
        Assert.Contains("hello", oldUnchanged);
    }

    [Fact]
    public void Compute_PreservesWhitespaceAndPunctuation()
    {
        var (oldSeg, newSeg) = WordDiff.Compute("foo(bar)", "foo(baz)");
        var oldText = string.Concat(oldSeg.Select(s => s.Text));
        var newText = string.Concat(newSeg.Select(s => s.Text));
        Assert.Equal("foo(bar)", oldText);
        Assert.Equal("foo(baz)", newText);
    }

    [Fact]
    public void Compute_LongLineFallsBackToWholeLineHighlight()
    {
        var oldText = new string('a', 2000) + "x";
        var newText = new string('a', 2000) + "y";
        var (oldSeg, newSeg) = WordDiff.Compute(oldText, newText);
        Assert.Single(oldSeg);
        Assert.True(oldSeg[0].Changed);
        Assert.Single(newSeg);
        Assert.True(newSeg[0].Changed);
    }

    [Fact]
    public void Compute_EmptyStrings_ReturnsEmpty()
    {
        var (oldSeg, newSeg) = WordDiff.Compute("", "");
        Assert.Empty(oldSeg);
        Assert.Empty(newSeg);
    }
}
