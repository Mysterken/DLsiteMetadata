using System.Collections.Generic;
using Playnite.SDK.Plugins;

namespace DLsiteMetadata;

public class DLsiteMetadataProvider(MetadataRequestOptions options, DLsiteMetadataPlugin plugin)
    : OnDemandMetadataProvider
{
    private readonly DLsiteMetadataPlugin _plugin = plugin;

    private List<MetadataField> _availableFields;

    private GameData _gameData;
    public override List<MetadataField> AvailableFields => _availableFields ??= GetAvailableFields();

    // Override additional methods based on supported metadata fields.
    public override string GetDescription(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.Description)) return base.GetDescription(args);

        return options.GameData.Name + " description";
    }

    public override string GetName(GetMetadataFieldArgs args)
    {
        if (!AvailableFields.Contains(MetadataField.Name)) return base.GetName(args);

        return options.GameData.Name;
    }

    private void GetMetadata()
    {
        if (_gameData != null) return;

        // TODO call the DLsiteScrapper to get the metadata and assign it to _gameData
    }

    private List<MetadataField> GetAvailableFields()
    {
        if (_gameData == null) GetMetadata();

        return new List<MetadataField>
        {
            MetadataField.Description,
            MetadataField.Name
        };
    }
}