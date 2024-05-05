using System;
using System.Collections.Generic;
using System.Windows.Controls;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace DLsiteMetadata;

[UsedImplicitly]
public class DLsiteMetadataPlugin : MetadataPlugin
{
    private static readonly ILogger Logger = LogManager.GetLogger();

    public DLsiteMetadataPlugin(IPlayniteAPI api) : base(api)
    {
        Settings = new DLsiteMetadataSettingsViewModel(this);
        Properties = new MetadataPluginProperties
        {
            HasSettings = true
        };
    }

    private DLsiteMetadataSettingsViewModel Settings { get; }

    public override Guid Id { get; } = Guid.Parse("db03c8d9-645a-4359-aafb-01cda945f301");

    public override List<MetadataField> SupportedFields { get; } = new()
    {
        MetadataField.Description
        // Include addition fields if supported by the metadata source
    };

    // Change to something more appropriate
    public override string Name => "DLsite";

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        return new DLsiteMetadataProvider(options, this);
    }

    public override ISettings GetSettings(bool firstRunSettings)
    {
        return Settings;
    }

    public override UserControl GetSettingsView(bool firstRunSettings)
    {
        return new DLsiteMetadataSettingsView();
    }
}