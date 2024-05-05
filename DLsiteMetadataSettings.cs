using System.Collections.Generic;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace DLsiteMetadata;

public class DLsiteMetadataSettings : ObservableObject
{
    private string _option1 = string.Empty;
    private bool _option2;
    private bool _optionThatWontBeSaved;

    public string Option1
    {
        get => _option1;
        set => SetValue(ref _option1, value);
    }

    public bool Option2
    {
        get => _option2;
        set => SetValue(ref _option2, value);
    }

    // Playnite serializes settings object to a JSON object and saves it as text file.
    // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
    [DontSerialize]
    public bool OptionThatWontBeSaved
    {
        get => _optionThatWontBeSaved;
        set => SetValue(ref _optionThatWontBeSaved, value);
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
        return true;
    }
}