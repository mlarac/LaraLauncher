using LaraLauncher.Core.Services;

namespace LaraLauncher.Tests;

public sealed class WebAppAndKeyboardTests
{
    [Theory]
    [InlineData("youtube.com", "https://youtube.com", "Youtube")]
    [InlineData("www.netflix.com", "https://www.netflix.com", "Netflix")]
    [InlineData("https://twitch.tv/directory", "https://twitch.tv/directory", "Twitch")]
    [InlineData("http://localhost:8096", "http://localhost:8096", "Localhost")]
    [InlineData("tv.apple.com", "https://tv.apple.com", "Apple")]
    public void WebAppUrlNormalizer_ValidInputs_NormalizeAndSuggestName(
        string input,
        string expectedUrl,
        string expectedName)
    {
        var result = WebAppUrlNormalizer.Normalize(input);

        Assert.True(result.IsValid);
        Assert.Equal(expectedUrl, result.NormalizedUrl);
        Assert.Equal(expectedName, result.SuggestedName);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("solo_texto")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/Windows/notepad.exe")]
    public void WebAppUrlNormalizer_InvalidOrUnsafeInputs_FailWithMessage(string input)
    {
        var result = WebAppUrlNormalizer.Normalize(input);

        Assert.False(result.IsValid);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public void VirtualKeyboardLayout_HasExact5x10GridAndAppliesKeysCorrectly()
    {
        Assert.Equal(10, VirtualKeyboardLayout.Columns);
        Assert.Equal(50, VirtualKeyboardLayout.Keys.Count);

        var keyT = VirtualKeyboardLayout.Keys.Single(k => k.LowerLabel == "t");
        var keyDotCom = VirtualKeyboardLayout.Keys.Single(k => k.LowerLabel == ".com");
        var keySpace = VirtualKeyboardLayout.Keys.Single(k => k.Kind == VirtualKeyKind.Space);
        var keyBackspace = VirtualKeyboardLayout.Keys.Single(k => k.Kind == VirtualKeyKind.Backspace);
        var keyClear = VirtualKeyboardLayout.Keys.Single(k => k.Kind == VirtualKeyKind.Clear);

        string text = string.Empty;
        text = VirtualKeyboardLayout.ApplyKey(text, keyT, isUpperCase: true);
        Assert.Equal("T", text);

        text = VirtualKeyboardLayout.ApplyKey(text, keyT, isUpperCase: false);
        Assert.Equal("Tt", text);

        text = VirtualKeyboardLayout.ApplyKey(text, keyDotCom, isUpperCase: false);
        Assert.Equal("Tt.com", text);

        text = VirtualKeyboardLayout.ApplyKey(text, keySpace, isUpperCase: false);
        Assert.Equal("Tt.com ", text);

        text = VirtualKeyboardLayout.ApplyKey(text, keyBackspace, isUpperCase: false);
        Assert.Equal("Tt.com", text);

        text = VirtualKeyboardLayout.ApplyKey(text, keyClear, isUpperCase: false);
        Assert.Equal(string.Empty, text);

        // Backspace on empty string remains empty safely.
        Assert.Equal(string.Empty, VirtualKeyboardLayout.ApplyKey(string.Empty, keyBackspace, isUpperCase: false));
    }
}
