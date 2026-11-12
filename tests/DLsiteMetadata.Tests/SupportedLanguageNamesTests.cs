using System.Linq;
using DLsiteMetadata.Enums;
using Xunit;

namespace DLsiteMetadata.Tests;

public class SupportedLanguageNamesTests
{
    [Fact]
    public void Every_code_is_named_in_every_locale()
    {
        Assert.NotEmpty(SupportedLanguageNames.Codes);
        Assert.All(SupportedLanguageNames.Codes, code => Assert.True(SupportedLanguageNames.IsComplete(code), code));
    }

    [Fact]
    public void Names_are_not_blank()
    {
        foreach (var code in SupportedLanguageNames.Codes.Where(code => code != "CHI"))
        foreach (SupportedLanguages language in System.Enum.GetValues(typeof(SupportedLanguages)))
        {
            Assert.True(SupportedLanguageNames.TryGet(code, language, out var name), $"{code} {language}");
            Assert.False(string.IsNullOrWhiteSpace(name), $"{code} {language}");
        }
    }

    [Theory]
    [InlineData("JPN", SupportedLanguages.ja_JP, "日本語")]
    [InlineData("ENG", SupportedLanguages.es_ES, "Inglés")]
    [InlineData("jpn", SupportedLanguages.en_US, "Japanese")]
    public void Names_are_localized(string code, SupportedLanguages language, string expected)
    {
        Assert.True(SupportedLanguageNames.TryGet(code, language, out var name));
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData("CHI")]
    [InlineData("XXX")]
    [InlineData(null)]
    public void Hidden_and_unknown_codes_have_no_name(string code)
    {
        Assert.False(SupportedLanguageNames.TryGet(code, SupportedLanguages.en_US, out _));
    }
}
