using Cominomi.Shared.Models;
using Cominomi.Shared.Services;

namespace Cominomi.Shared.Tests;

public class GitServiceDiffParsingTests
{
    [Theory]
    [InlineData("diff --git a/src/file.cs b/src/file.cs", "src/file.cs")]
    [InlineData("diff --git a/README.md b/README.md", "README.md")]
    [InlineData("diff --git a/a b/a", "a")]
    public void ExtractPathFromDiffHeader_NormalPaths(string header, string expected)
    {
        Assert.Equal(expected, GitService.ExtractPathFromDiffHeader(header));
    }

    [Theory]
    [InlineData("diff --git a/src/a b/config.txt b/src/a b/config.txt", "src/a b/config.txt")]
    [InlineData("diff --git a/a b/c b/a b/c", "a b/c")]
    [InlineData("diff --git a/x/a b/y/z b/x/a b/y/z", "x/a b/y/z")]
    public void ExtractPathFromDiffHeader_PathsContainingSpaceB(string header, string expected)
    {
        Assert.Equal(expected, GitService.ExtractPathFromDiffHeader(header));
    }

    [Theory]
    [InlineData("a/src/file.cs b/src/file.cs", "src/file.cs")]
    [InlineData("a/a b/c b/a b/c", "a b/c")]
    public void ExtractPathFromDiffHeader_ShortPrefixFormat(string header, string expected)
    {
        Assert.Equal(expected, GitService.ExtractPathFromDiffHeader(header));
    }

    [Theory]
    [InlineData("diff --git a/old.txt b/new.txt")]  // rename
    [InlineData("not a diff header")]
    [InlineData("")]
    public void ExtractPathFromDiffHeader_ReturnsNull_ForRenamesAndInvalid(string header)
    {
        Assert.Null(GitService.ExtractPathFromDiffHeader(header));
    }

}
