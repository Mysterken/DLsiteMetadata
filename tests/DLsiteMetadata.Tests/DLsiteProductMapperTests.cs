using System;
using System.Linq;
using DLsiteMetadata.Enums;
using DLsiteMetadata.Tests.TestSupport;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DLsiteMetadata.Tests;

public class DLsiteProductMapperTests
{
    private static DLsiteScrapperResult Map(string fixture, SupportedLanguages language = SupportedLanguages.en_US,
        string target = "Genres")
    {
        return DLsiteProductMapper.Map(Fixture.Product(fixture), language, target);
    }

    private static DLsiteScrapperResult MapJson(string productJson)
    {
        return DLsiteProductMapper.Map(JObject.Parse(productJson), SupportedLanguages.en_US, "Genres");
    }

    [Fact]
    public void Voice_work_is_mapped_completely()
    {
        var result = Map("voice_en");

        Assert.Equal("(17-Hour ASMR) Soothing Girl Helps You Sleep Immediately (Ear Cleaning, Massage, Whispering)",
            result.Title);
        Assert.Equal("Patra Suou", result.Circle);
        Assert.Equal(new DateTime(2023, 5, 26), result.ReleaseDate);
        Assert.Equal(DLsiteScrapperResult.AgeRating.AllAges, result.Age);
        Assert.Equal(new[] { "周防パトラ" }, result.Author);
        Assert.Equal(new[] { "周防パトラ" }, result.ScenarioWriters);
        Assert.Equal(new[] { "椎架ゆの" }, result.Illustrators);
        Assert.Equal(new[] { "周防パトラ" }, result.VoiceActors);
        Assert.Null(result.MusicCreators);
        Assert.Null(result.Series);
        Assert.Equal(new[] { "Voice / ASMR" }, result.GameProductFormat);
        Assert.Equal(new[] { "WAV" }, result.FileFormat);
        Assert.Equal("7.68GB", result.Filesize);
        Assert.Equal(new[] { "Japanese" }, result.SupportedLanguages);
        Assert.Contains("ASMR", result.Genres);
        Assert.InRange(result.Rating.Value, 4.9f, 5f);
        Assert.Equal("https://www.dlsite.com/home/work/=/product_id/RJ01005844.html", result.Links["DLsite"]);
    }

    [Fact]
    public void Images_start_with_the_key_visual_and_are_absolute()
    {
        var result = Map("voice_en");

        Assert.EndsWith("RJ01005844_img_main.jpg", result.MainImage);
        Assert.EndsWith("RJ01005844_img_sam_mini.jpg", result.Icon);
        Assert.True(result.ProductImages.Count > 1);
        Assert.Equal(result.MainImage, result.ProductImages.First());
        Assert.All(result.ProductImages, url => Assert.StartsWith("https://img.dlsite.jp/", url));
    }

    [Fact]
    public void Game_lists_extra_formats_and_readable_file_type()
    {
        var result = Map("game_en");

        Assert.Equal(new[] { "Action" }, result.GameProductFormat);
        Assert.Equal(new[] { "Music" }, result.ProductFormat);
        Assert.Equal(new[] { "Application" }, result.FileFormat);
        Assert.Equal("156.54MB", result.Filesize);
        Assert.Equal(new DateTime(2012, 10, 27), result.ReleaseDate);
        Assert.Null(result.Rating);
        Assert.Null(result.Author);
    }

    [Fact]
    public void Adult_work_with_series_and_jpeg_pages()
    {
        var result = Map("adult_series_en");

        Assert.Equal(DLsiteScrapperResult.AgeRating.Adult, result.Age);
        Assert.Equal("絶頂リフレ", result.Series);
        Assert.Equal(new[] { "JPEG" }, result.FileFormat);
        Assert.Equal(new[] { "Manga" }, result.GameProductFormat);
        Assert.Equal(new[] { "ぽちたろ" }, result.Author);
    }

    [Fact]
    public void Credits_come_from_roles_not_translated_labels()
    {
        var result = Map("series_music_en");

        Assert.Equal(new[] { "Yostar" }, result.ScenarioWriters);
        Assert.Equal(new[] { "自寺ニレ" }, result.Illustrators);
        Assert.Equal(new[] { "守屋亨香" }, result.VoiceActors);
        Assert.Equal(new[] { "青春×フェティシズム" }, result.MusicCreators);
    }

    [Fact]
    public void Texts_follow_the_requested_locale()
    {
        var english = Map("cg_en");
        var japanese = Map("cg_ja", SupportedLanguages.ja_JP);

        Assert.Equal(new[] { "Breasts", "Gal", "Pure Love" }, english.Genres);
        Assert.Equal(new[] { "おっぱい", "ギャル", "純愛" }, japanese.Genres);
        Assert.Equal(new[] { "CG + Illustrations" }, english.GameProductFormat);
        Assert.Equal(new[] { "CG・イラスト" }, japanese.GameProductFormat);
        Assert.Equal(new[] { "Japanese" }, english.SupportedLanguages);
        Assert.Equal(new[] { "日本語" }, japanese.SupportedLanguages);
    }

    [Fact]
    public void Categories_go_to_the_configured_target()
    {
        var asGenres = Map("cg_en", target: "Genres");
        var asTags = Map("cg_en", target: "Tags");

        Assert.NotNull(asGenres.Genres);
        Assert.Null(asGenres.Tags);
        Assert.NotNull(asTags.Tags);
        Assert.Null(asTags.Genres);
        Assert.Equal(asGenres.Genres, asTags.Tags);
    }

    [Fact]
    public void Translated_edition_lists_its_own_language()
    {
        var result = Map("manga_translated_en");

        Assert.Equal(new[] { "English" }, result.SupportedLanguages);
        Assert.Equal(new DateTime(2026, 11, 10), result.ReleaseDate);
    }

    [Fact]
    public void Description_renders_headings_images_and_text()
    {
        var result = Map("series_music_en");

        Assert.Contains("<h3>コンセプト</h3>", result.Description);
        Assert.Contains("<img src=\"https://img.dlsite.jp/", result.Description);
        Assert.True(result.Description.IndexOf((char)13) < 0, "carriage returns are stripped");
        Assert.DoesNotContain("//img.dlsite.jp", result.Description.Replace("https://img.dlsite.jp", ""));
    }

    [Fact]
    public void Description_lists_the_tracks()
    {
        var result = Map("tracklist_en");

        Assert.Contains("<ol>", result.Description);
        Assert.Contains("<li>", result.Description);
        Assert.Contains("(10:14)", result.Description);
    }

    [Fact]
    public void Description_falls_back_to_the_short_introduction()
    {
        var result = MapJson("{\"product_id\":\"RJ1\",\"introductions\":null,\"description\":\"Short text\"}");

        Assert.Equal("Short text", result.Description);
    }

    [Fact]
    public void Description_does_not_encode_entities_twice_or_double_prefix_images()
    {
        var result = MapJson(@"{""product_id"":""RJ1"",""introductions"":[
            {""type"":""text"",""title"":""Q&amp;A"",""body"":[{""intro"":""<img src='//img.dlsite.jp/a.jpg'><img src='https://img.dlsite.jp/b.jpg'>""}]},
            {""type"":""track_list"",""title"":"""",""body"":[{""name"":""A &amp; B"",""duration"":""1:00""}]}]}");

        Assert.Contains("<h3>Q&amp;A</h3>", result.Description);
        Assert.Contains("src='https://img.dlsite.jp/a.jpg'", result.Description);
        Assert.Contains("src='https://img.dlsite.jp/b.jpg'", result.Description);
        Assert.DoesNotContain("https:https:", result.Description);
        Assert.Contains("<li>A &amp; B (1:00)</li>", result.Description);
    }

    [Fact]
    public void Release_date_is_the_japanese_calendar_day()
    {
        // 2023-05-25 15:00 UTC is already the 26th in Japan.
        var result = MapJson("{\"product_id\":\"RJ1\",\"release_date\":1685026800}");

        Assert.Equal(new DateTime(2023, 5, 26), result.ReleaseDate);
    }

    [Theory]
    [InlineData(1, DLsiteScrapperResult.AgeRating.AllAges)]
    [InlineData(2, DLsiteScrapperResult.AgeRating.RRated)]
    [InlineData(3, DLsiteScrapperResult.AgeRating.Adult)]
    public void Age_category_maps_to_age_rating(int category, DLsiteScrapperResult.AgeRating expected)
    {
        Assert.Equal(expected, MapJson($"{{\"product_id\":\"RJ1\",\"age_category\":{category}}}").Age);
    }

    [Fact]
    public void Unknown_age_category_is_left_out()
    {
        Assert.Null(MapJson("{\"product_id\":\"RJ1\",\"age_category\":9}").Age);
    }

    [Theory]
    [InlineData("8243437783", "7.68GB")]
    [InlineData("413855948", "394.68MB")]
    [InlineData("2048", "2KB")]
    [InlineData("512", "512B")]
    public void File_size_is_formatted_like_the_product_page(string bytes, string expected)
    {
        Assert.Equal(expected, MapJson($"{{\"product_id\":\"RJ1\",\"contents_file_size\":{bytes}}}").Filesize);
    }

    [Fact]
    public void Empty_file_size_is_left_out()
    {
        Assert.Null(MapJson("{\"product_id\":\"RJ1\",\"contents_file_size\":0,\"file_size\":null}").Filesize);
    }

    [Fact]
    public void Unrated_work_has_no_rating()
    {
        Assert.Null(MapJson("{\"product_id\":\"RJ1\",\"rating\":{\"count\":0,\"average\":0}}").Rating);
        Assert.Null(MapJson("{\"product_id\":\"RJ1\",\"rating\":null}").Rating);
    }

    [Fact]
    public void Generic_chinese_language_flag_is_not_listed()
    {
        var result = MapJson("{\"product_id\":\"RJ1\",\"work_languages\":[\"CHI_HANS\",\"JPN\",\"CHI\",\"XXX\"]}");

        Assert.Equal(new[] { "Chinese Simplified", "Japanese" }, result.SupportedLanguages);
    }

    /// <summary>
    ///     DLsite sends JSON null for unused objects (e.g. "title" for a work without series). Indexing such a
    ///     token threw in a live run, which is what this guards.
    /// </summary>
    [Fact]
    public void Null_objects_and_missing_fields_only_drop_their_field()
    {
        var result = MapJson(@"{""product_id"":""RJ1"",""work_name"":""Name"",""maker"":null,""title"":null,
            ""work_type"":null,""image_main"":null,""image_samples"":null,""rating"":null,""creators"":null,
            ""genres"":null,""icons"":null,""introductions"":null,""work_languages"":null,""release_date"":null}");

        Assert.Equal("Name", result.Title);
        Assert.Null(result.Circle);
        Assert.Null(result.Series);
        Assert.Null(result.GameProductFormat);
        Assert.Null(result.Genres);
        Assert.Empty(result.ProductImages);
        Assert.Null(result.MainImage);
        Assert.Null(result.Description);
        Assert.Null(result.ReleaseDate);
    }

    [Fact]
    public void Reshaped_fields_do_not_throw()
    {
        var result = MapJson(@"{""product_id"":""RJ1"",""work_name"":""Name"",""maker"":""text"",""creators"":{""a"":1},
            ""genres"":[null,""x"",{""genre_name"":null}],""icons"":[1,null],""introductions"":[null,{""type"":1}]}");

        Assert.Equal("Name", result.Title);
    }

    [Fact]
    public void Links_fall_back_to_a_built_url()
    {
        var result = MapJson("{\"product_id\":\"RJ1\",\"site_id\":\"girls\"}");

        Assert.Equal("https://www.dlsite.com/girls/work/=/product_id/RJ1.html", result.Links["DLsite"]);
    }
}
