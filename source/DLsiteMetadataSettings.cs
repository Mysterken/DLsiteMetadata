using System.Collections.Generic;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace DLsiteMetadata;

public class DLsiteMetadataSettings : ObservableObject
{
    private bool _includeIllustrators;
    private bool _includeMusicCreators;
    private bool _includeScenarioWriters;
    private bool _includeVoiceActors;
    
    private bool _includeProductFormat;
    private bool _includeFileFormat;

    private int _maxSearchResults = 30;
    private string _pageLanguage = "English";

    private string _searchCategory = "All categories";

    [DontSerialize]
    public List<string> AvailableSearchCategory { get; } =
    [
        "All categories",
        "All ages Doujin / Indie Games",
        "All ages PC Games",
        "Adult Doujin / Indie Games",
        "Adult H Games"
    ];

    [DontSerialize]
    public List<int> MaxSearchResultsSteps { get; } =
    [
        30,
        50,
        100
    ];

    [DontSerialize]
    public List<string> AvailableLanguages { get; } =
    [
        "Japanese",
        "English",
        "Simplified Chinese",
        "Traditional Chinese",
        "Korean",
        "Spanish",
        "German",
        "French",
        "Indonesian",
        "Italian",
        "Portuguese",
        "Swedish",
        "Thai",
        "Vietnamese"
    ];

    public string SearchCategory
    {
        get => _searchCategory;
        set => SetValue(ref _searchCategory, value);
    }

    public string PageLanguage
    {
        get => _pageLanguage;
        set => SetValue(ref _pageLanguage, value);
    }

    public bool IncludeIllustrators
    {
        get => _includeIllustrators;
        set => SetValue(ref _includeIllustrators, value);
    }

    public bool IncludeScenarioWriters
    {
        get => _includeScenarioWriters;
        set => SetValue(ref _includeScenarioWriters, value);
    }

    public bool IncludeMusicCreators
    {
        get => _includeMusicCreators;
        set => SetValue(ref _includeMusicCreators, value);
    }

    public bool IncludeVoiceActors
    {
        get => _includeVoiceActors;
        set => SetValue(ref _includeVoiceActors, value);
    }
    
    public bool IncludeProductFormat
    {
        get => _includeProductFormat;
        set => SetValue(ref _includeProductFormat, value);
    }
    
    public bool IncludeFileFormat
    {
        get => _includeFileFormat;
        set => SetValue(ref _includeFileFormat, value);
    }

    public int MaxSearchResults
    {
        get => _maxSearchResults;
        set => SetValue(ref _maxSearchResults, value);
    }

    public SupportedLanguages GetSupportedLanguage()
    {
        return _pageLanguage switch
        {
            "Japanese" => SupportedLanguages.ja_JP,
            "English" => SupportedLanguages.en_US,
            "Simplified Chinese" => SupportedLanguages.zh_CN,
            "Traditional Chinese" => SupportedLanguages.zh_TW,
            "Korean" => SupportedLanguages.ko_KR,
            "Spanish" => SupportedLanguages.es_ES,
            "German" => SupportedLanguages.de_DE,
            "French" => SupportedLanguages.fr_FR,
            "Indonesian" => SupportedLanguages.id_ID,
            "Italian" => SupportedLanguages.it_IT,
            "Portuguese" => SupportedLanguages.pt_BR,
            "Swedish" => SupportedLanguages.sv_SE,
            "Thai" => SupportedLanguages.th_TH,
            "Vietnamese" => SupportedLanguages.vi_VN,
            _ => SupportedLanguages.en_US
        };
    }

    public List<string> GetAvailableSearchCategory()
    {
        return AvailableSearchCategory;
    }

    public static string GetSearchCategoryPath(string searchCategory)
    {
        return searchCategory switch
        {
            "All ages Doujin / Indie Games" => "home",
            "All ages PC Games" => "soft",
            "Adult Doujin / Indie Games" => "maniax",
            "Adult H Games" => "pro",
            _ => "home"
        };
    }
}

public class DLsiteMetadataSettingsViewModel : ObservableObject, ISettings
{
    private readonly DLsiteMetadataPlugin _plugin;

    private DLsiteMetadataSettings _settings;

    public DLsiteMetadataSettingsViewModel(DLsiteMetadataPlugin plugin)
    {
        // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
        _plugin = plugin;

        // Load saved settings.
        var savedSettings = plugin.LoadPluginSettings<DLsiteMetadataSettings>();

        // LoadPluginSettings returns null if no saved data is available.
        Settings = savedSettings ?? new DLsiteMetadataSettings();
    }

    private DLsiteMetadataSettings editingClone { get; set; }

    public DLsiteMetadataSettings Settings
    {
        get => _settings;
        set
        {
            _settings = value;
            OnPropertyChanged();
        }
    }

    public void BeginEdit()
    {
        // Code executed when settings view is opened and user starts editing values.
        editingClone = Serialization.GetClone(Settings);
    }

    public void CancelEdit()
    {
        // Code executed when user decides to cancel any changes made since BeginEdit was called.
        // This method should revert any changes made to Option1 and Option2.
        Settings = editingClone;
    }

    public void EndEdit()
    {
        // Code executed when user decides to confirm changes made since BeginEdit was called.
        // This method should save settings made to Option1 and Option2.
        _plugin.SavePluginSettings(Settings);
    }

    public bool VerifySettings(out List<string> errors)
    {
        // Code execute when user decides to confirm changes made since BeginEdit was called.
        // Executed before EndEdit is called and EndEdit is not called if false is returned.
        // List of errors is presented to user if verification fails.
        errors = new List<string>();

        if (!Settings.AvailableSearchCategory.Contains(Settings.SearchCategory))
            errors.Add("Selected category is not supported.");

        if (!Settings.AvailableLanguages.Contains(Settings.PageLanguage))
            errors.Add("Selected language is not supported.");

        if (!Settings.MaxSearchResultsSteps.Contains(Settings.MaxSearchResults))
            errors.Add("Selected search results is not in the list of steps.");

        return true;
    }
}