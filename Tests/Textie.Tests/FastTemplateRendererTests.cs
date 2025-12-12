using System;
using Textie.Core.Spammer;
using Textie.Core.Templates;
using Xunit;

namespace Textie.Tests;

public class FastTemplateRendererTests
{
    private readonly FastTemplateRenderer _renderer = new();
    private readonly Random _random = new(42); // Fixed seed for reproducible tests

    [Fact]
    public void Render_WithNoPlaceholders_ReturnsOriginalTemplate()
    {
        var result = _renderer.Render("Hello World", CreateContext(1, 10));
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Render_WithEmptyString_ReturnsEmpty()
    {
        var result = _renderer.Render("", CreateContext(1, 10));
        Assert.Equal("", result);
    }

    [Fact]
    public void Render_WithNullString_ReturnsEmpty()
    {
        var result = _renderer.Render(null!, CreateContext(1, 10));
        Assert.Equal("", result);
    }

    [Fact]
    public void Render_IndexPlaceholder_ReplacesCorrectly()
    {
        var result = _renderer.Render("Message {index} of {total}", CreateContext(5, 20));
        Assert.Equal("Message 5 of 20", result);
    }

    [Fact]
    public void Render_IndexPlaceholder_CaseInsensitive()
    {
        var result = _renderer.Render("{INDEX} {Index} {index}", CreateContext(3, 10));
        Assert.Equal("3 3 3", result);
    }

    [Fact]
    public void Render_TotalPlaceholder_ReplacesCorrectly()
    {
        var result = _renderer.Render("Total: {total}", CreateContext(1, 100));
        Assert.Equal("Total: 100", result);
    }

    [Fact]
    public void Render_TimestampPlaceholder_ContainsColons()
    {
        var result = _renderer.Render("Time: {timestamp}", CreateContext(1, 1));
        // Timestamp format is HH:mm:ss
        Assert.Contains(":", result);
        Assert.StartsWith("Time: ", result);
    }

    [Fact]
    public void Render_GuidPlaceholder_ReturnsValidGuid()
    {
        var result = _renderer.Render("{guid}", CreateContext(1, 1));
        Assert.True(Guid.TryParse(result, out _), $"Expected valid GUID but got: {result}");
    }

    [Fact]
    public void Render_RandomPlaceholder_ReturnsNumber()
    {
        var result = _renderer.Render("{random}", CreateContext(1, 1));
        Assert.True(int.TryParse(result, out _), $"Expected integer but got: {result}");
    }

    [Fact]
    public void Render_RandShorthand_ReturnsNumber()
    {
        var result = _renderer.Render("{rand}", CreateContext(1, 1));
        Assert.True(int.TryParse(result, out _), $"Expected integer but got: {result}");
    }

    [Fact]
    public void Render_UnknownPlaceholder_PreservesOriginal()
    {
        var result = _renderer.Render("Hello {unknown} world", CreateContext(1, 1));
        Assert.Equal("Hello {unknown} world", result);
    }

    [Fact]
    public void Render_MalformedPlaceholder_PreservesOriginal()
    {
        var result = _renderer.Render("Hello {unclosed", CreateContext(1, 1));
        Assert.Equal("Hello {unclosed", result);
    }

    [Fact]
    public void Render_MultiplePlaceholders_ReplacesAll()
    {
        var result = _renderer.Render("[{index}/{total}] Message", CreateContext(7, 50));
        Assert.Equal("[7/50] Message", result);
    }

    [Fact]
    public void Render_AdjacentPlaceholders_ReplacesCorrectly()
    {
        var result = _renderer.Render("{index}{total}", CreateContext(1, 2));
        Assert.Equal("12", result);
    }

    private SpamTemplateContext CreateContext(int index, int total)
        => new(index, total, _random);
}
