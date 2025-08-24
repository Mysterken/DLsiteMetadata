using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DLsiteMetadata.Exceptions;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace DLsiteMetadata;

public class DLsiteMetadataProvider(
    IPlayniteAPI playniteApi,
    MetadataRequestOptions options,
    DLsiteMetadataSettings settings)
    : OnDemandMetadataProvider
{
    private const string WorkIdPattern = @"(?:RJ|RE|BJ|VJ)(?:\d{6}|\d{8})";
    private static readonly ILogger Logger = LogManager.GetLogger();

    private List<MetadataField> _availableFields;

    private DLsiteScrapperResult _gameData;
    private bool IsBackgroundDownload => options.IsBackgroundDownload;
    public override List<MetadataField> AvailableFields => _availableFields ??= GetAvailableFields();

    public override string GetDescription(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.Description)) return base.GetDescription(args);

        return _gameData.Description;
    }

    public override IEnumerable<MetadataProperty> GetFeatures(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.Features)) return base.GetFeatures(args);

        var features = new List<MetadataProperty>();

        if (_gameData.GameProductFormat != null && !settings.AssignGameProductFormatToGenre)
            AddFeatures(_gameData.GameProductFormat);

        if (_gameData.FileFormat != null && settings.IncludeFileFormat)
            AddFeatures(_gameData.FileFormat);

        if (_gameData.ProductFormat != null && settings.IncludeProductFormat)
            AddFeatures(_gameData.ProductFormat);

        return features;

        void AddFeatures(IEnumerable<string> formats)
        {
            features.AddRange(formats.Select(format =>
            {
                var property = playniteApi.Database.Features
                    .FirstOrDefault(feature =>
                        feature.Name?.Equals(format, StringComparison.OrdinalIgnoreCase) == true);

                return property is null
                    ? (MetadataProperty)new MetadataNameProperty(format)
                    : new MetadataIdProperty(property.Id);
            }));
        }
    }

    public override string GetName(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.Name)) return base.GetName(args);

        return _gameData.Title;
    }

    public override IEnumerable<MetadataProperty> GetAgeRatings(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.AgeRating)) return base.GetAgeRatings(args);

        var age = _gameData.Age switch
        {
            DLsiteScrapperResult.AgeRating.AllAges => "All ages",
            DLsiteScrapperResult.AgeRating.RRated => "R-Rated",
            DLsiteScrapperResult.AgeRating.Adult => "Adult",
            _ => null
        };

        if (age is null) return base.GetAgeRatings(args);

        var ageRating = playniteApi.Database.AgeRatings
            .Where(x => x.Name is not null)
            .FirstOrDefault(rating => rating.Name.Equals(age, StringComparison.OrdinalIgnoreCase));

        var property = ageRating is null
            ? (MetadataProperty)new MetadataNameProperty(age)
            : new MetadataIdProperty(ageRating.Id);

        return new[] { property };
    }

    public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.Genres)) return base.GetGenres(args);

        if (_gameData.GameProductFormat != null && settings.AssignGameProductFormatToGenre)
        {
            _gameData.Genres ??= [];
            _gameData.Genres?.AddRange(_gameData.GameProductFormat);
        }

        if (_gameData.SupportedLanguages != null && settings.SupportedLanguagesMappingTarget == "Genres")
        {
            _gameData.Genres ??= [];
            _gameData.Genres?.AddRange(_gameData.SupportedLanguages);
        }

        if (_gameData.Genres == null) return new List<MetadataProperty>();

        var genres = _gameData.Genres
            .Select(genre => (genre,
                playniteApi.Database.Genres.Where(x => x.Name is not null)
                    .FirstOrDefault(x => x.Name.Equals(genre, StringComparison.OrdinalIgnoreCase))))
            .Select(tuple =>
            {
                var (genre, property) = tuple;
                if (property is not null) return (MetadataProperty)new MetadataIdProperty(property.Id);
                return new MetadataNameProperty(genre);
            })
            .ToList();

        return genres;
    }

    public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.Tags)) return base.GetTags(args);

        if (_gameData.SupportedLanguages != null && settings.SupportedLanguagesMappingTarget == "Tags")
        {
            _gameData.Tags ??= [];
            _gameData.Tags?.AddRange(_gameData.SupportedLanguages);
        }

        if (_gameData.Tags == null) return new List<MetadataProperty>();
        
        var tags = _gameData.Tags
            .Select(tag => (tag,
                playniteApi.Database.Tags.Where(x => x.Name is not null)
                    .FirstOrDefault(x => x.Name.Equals(tag, StringComparison.OrdinalIgnoreCase))))
            .Select(tuple =>
            {
                var (tag, property) = tuple;
                if (property is not null) return (MetadataProperty)new MetadataIdProperty(property.Id);
                return new MetadataNameProperty(tag);
            })
            .ToList();

        return tags;
    }

    public override MetadataFile GetIcon(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.Icon)) return base.GetIcon(args);

        return new MetadataFile(_gameData.Icon);
    }

    public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.Developers)) return base.GetDevelopers(args);

        var staff = new List<string>();

        void AddStaff(IEnumerable<string> members)
        {
            if (members != null) staff.AddRange(members);
        }

        AddStaff(_gameData.Author);

        if (_gameData.Circle != null && (_gameData.Author == null || !_gameData.Author.Contains(_gameData.Circle)))
            staff.Add(_gameData.Circle);
        if (settings.IncludeIllustrators) AddStaff(_gameData.Illustrators);
        if (settings.IncludeScenarioWriters) AddStaff(_gameData.ScenarioWriters);
        if (settings.IncludeMusicCreators) AddStaff(_gameData.MusicCreators);
        if (settings.IncludeVoiceActors) AddStaff(_gameData.VoiceActors);

        return staff.Select(name =>
        {
            var company = playniteApi.Database.Companies
                .FirstOrDefault(c => c.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);

            return company is null
                ? (MetadataProperty)new MetadataNameProperty(name)
                : new MetadataIdProperty(company.Id);
        }).ToList();
    }

    public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.Links)) return base.GetLinks(args);

        var links = new List<Link>();

        if (_gameData.Links != null) links.AddRange(_gameData.Links.Select(link => new Link(link.Key, link.Value)));

        return links;
    }

    public override int? GetCommunityScore(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.CommunityScore)) return base.GetCommunityScore(args);

        if (_gameData.Rating == null) return base.GetCommunityScore(args);

        return (int)(_gameData.Rating * 20);
    }

    public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
    {
        var publisher = playniteApi.Database.Companies
            .Where(x => x.Name is not null)
            .FirstOrDefault(company => company.Name.Equals("DLsite", StringComparison.OrdinalIgnoreCase));

        var property = publisher is null
            ? (MetadataProperty)new MetadataNameProperty("DLsite")
            : new MetadataIdProperty(publisher.Id);

        return new[] { property };
    }

    public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.ReleaseDate)) return base.GetReleaseDate(args);

        if (_gameData.ReleaseDate == null) return base.GetReleaseDate(args);

        return new ReleaseDate(_gameData.ReleaseDate.Value);
    }

    public override IEnumerable<MetadataProperty> GetSeries(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.Series)) return base.GetSeries(args);

        var series = playniteApi.Database.Series
            .Where(x => x.Name is not null)
            .FirstOrDefault(series => series.Name.Equals(_gameData.Series, StringComparison.OrdinalIgnoreCase));

        var property = series is null
            ? (MetadataProperty)new MetadataNameProperty(_gameData.Series)
            : new MetadataIdProperty(series.Id);

        return new[] { property };
    }

    public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.CoverImage)) return base.GetCoverImage(args);

        return new MetadataFile(_gameData.MainImage);
    }

    public override MetadataFile GetBackgroundImage(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.BackgroundImage)) return base.GetBackgroundImage(args);

        if (IsBackgroundDownload)
        {
            var image = _gameData.ProductImages?.FirstOrDefault();
            return image is null ? base.GetBackgroundImage(args) : new MetadataFile(image);
        }

        var imageFileOption = playniteApi.Dialogs.ChooseImageFile(
            _gameData.ProductImages?.Select(image => new ImageFileOption(image)).ToList(),
            "Select Background Image");

        return imageFileOption is null ? base.GetBackgroundImage(args) : new MetadataFile(imageFileOption.Path);
    }

    private void GetMetadata()
    {
        if (_gameData != null) return;

        var dlsiteLink = options.GameData.Links?.FirstOrDefault(
            link => link.Name.Equals("DLsite", StringComparison.OrdinalIgnoreCase)
        )?.Url;
        var gameName = options.GameData?.Name;

        var isGameNameIdOrUrl = IsValidId(gameName) || IsValidUrl(gameName);

        var scrapper = new DLsiteScrapper(Logger);

        if (!IsValidUrl(dlsiteLink) && !isGameNameIdOrUrl)
        {
            var selectedGame = (DLsiteItemOption)playniteApi.Dialogs.ChooseItemWithSearch(
                null,
                query =>
                {
                    var searchResult = new List<DLsiteSearchResult>();

                    if (settings.SearchCategory == "All categories")
                    {
                        var searchTasks = settings.GetAvailableSearchCategory()
                            .Where(category => category != "All categories")
                            .Select(category => scrapper.ScrapSearchPage(
                                DLsiteMetadataSettings.GetSearchCategoryPath(category),
                                query,
                                settings.MaxSearchResults,
                                settings.GetSupportedLanguage()))
                            .ToList();

                        var searchResults = Task.WhenAll(searchTasks);

                        searchResults.Wait();

                        searchResults.Result
                            .SelectMany(x => x)
                            .ToList()
                            .ForEach(game =>
                            {
                                var source = game.Link.Replace(DLsiteScrapper.SiteBaseUrl, "").Split('/');
                                var addExcerpt = source.Length > 0 ? source[0] : "Unknown source";

                                game.Excerpt += $"\n(From {addExcerpt})";
                                searchResult.Add(game);
                            });
                    }
                    else
                    {
                        var searchTask = scrapper.ScrapSearchPage(
                            DLsiteMetadataSettings.GetSearchCategoryPath(settings.SearchCategory),
                            query,
                            settings.MaxSearchResults,
                            settings.GetSupportedLanguage());

                        searchTask.Wait();

                        searchResult.AddRange(searchTask.Result);
                    }

                    if (searchResult.Count == 0)
                    {
                        Logger.Error($"No search results found for {options.GameData.Name}");
                        return null;
                    }

                    return
                    [
                        ..searchResult.Select(
                            game => new DLsiteItemOption(game.Title, game.Excerpt, game.Link)
                        ).ToList()
                    ];
                }, gameName, "Search DLsite");

            if (selectedGame == null) return;

            dlsiteLink = selectedGame.Link;
        }

        try
        {
            var link = dlsiteLink ?? gameName;
            var url = IsValidUrl(link) ? link : $"https://www.dlsite.com/home/work/=/product_id/{gameName}.html";

            var gameTask = scrapper.ScrapGamePage(url, settings.GetSupportedLanguage(), settings.CategoryMappingTarget);
            gameTask.Wait();

            _gameData = gameTask.Result;

            if (_gameData == null) Logger.Warn($"Failed to get metadata for {gameName ?? "the selected game"}");
        }
        catch (AggregateException ae)
        {
            // When using Task.Wait(), exceptions are wrapped in AggregateException
            if (ae.InnerExceptions.Count == 1 && ae.InnerException is ProductUnavailableException ex)
                playniteApi.Dialogs.ShowErrorMessage(
                    $"Product unavailable: {ex.Message}",
                    "DLsiteMetadata Error"
                );
            else
                playniteApi.Dialogs.ShowErrorMessage(
                    $"Failed to fetch metadata from DLsite for {gameName ?? "the selected game"}",
                    "DLsiteMetadata Error"
                );
        }
    }

    private List<MetadataField> GetAvailableFields()
    {
        if (_gameData == null) GetMetadata();

        var fields = new List<MetadataField>();

        if (_gameData == null) return fields;

        if (_gameData.Title != null) fields.Add(MetadataField.Name);

        if (_gameData.Age != null) fields.Add(MetadataField.AgeRating);

        if (_gameData.Description != null) fields.Add(MetadataField.Description);

        if (_gameData.Genres != null) fields.Add(MetadataField.Genres);

        if (settings.AssignGameProductFormatToGenre && 
            _gameData.GameProductFormat != null &&
            !fields.Contains(MetadataField.Genres))
            fields.Add(MetadataField.Genres);

        var addDevelopers = _gameData.Author != null ||
                            _gameData.Circle != null ||
                            (settings.IncludeIllustrators && _gameData.Illustrators != null) ||
                            (settings.IncludeScenarioWriters && _gameData.ScenarioWriters != null) ||
                            (settings.IncludeMusicCreators && _gameData.MusicCreators != null) ||
                            (settings.IncludeVoiceActors && _gameData.VoiceActors != null);

        if (addDevelopers) fields.Add(MetadataField.Developers);

        var addFeatures = (_gameData.FileFormat != null && settings.IncludeFileFormat) ||
                          (_gameData.ProductFormat != null && settings.IncludeProductFormat);

        if (addFeatures) fields.Add(MetadataField.Features);

        if (settings.SupportedLanguagesMappingTarget != "None" && _gameData.SupportedLanguages != null)
        {
            var targetField = settings.SupportedLanguagesMappingTarget == "Genres" 
                ? MetadataField.Genres 
                : MetadataField.Tags;
    
            if (!fields.Contains(targetField)) 
                fields.Add(targetField);
        }

        if (_gameData.Icon != null) fields.Add(MetadataField.Icon);

        if (_gameData.Links != null) fields.Add(MetadataField.Links);

        if (_gameData.Rating != null) fields.Add(MetadataField.CommunityScore);

        if (_gameData.ReleaseDate != null) fields.Add(MetadataField.ReleaseDate);

        if (_gameData.Series != null) fields.Add(MetadataField.Series);

        if (_gameData.Tags != null) fields.Add(MetadataField.Tags);

        if (_gameData.MainImage != null) fields.Add(MetadataField.CoverImage);

        if (_gameData.ProductImages != null) fields.Add(MetadataField.BackgroundImage);

        return fields;
    }

    private static bool IsValidId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        var match = Regex.Match(id, WorkIdPattern);
        return match.Success;
    }

    private static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        var match = Regex.Match(url,
            $@"https://www\.dlsite\.com/(home|soft|maniax|pro)/work/=/product_id/{WorkIdPattern}");
        return match.Success;
    }
}