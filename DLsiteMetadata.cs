using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DLsiteMetadata
{
    public class DLsiteMetadata : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private DLsiteMetadataSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("db03c8d9-645a-4359-aafb-01cda945f301");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Description
            // Include addition fields if supported by the metadata source
        };

        // Change to something more appropriate
        public override string Name => "Custom Metadata";

        public DLsiteMetadata(IPlayniteAPI api) : base(api)
        {
            settings = new DLsiteMetadataSettingsViewModel(this);
            Properties = new MetadataPluginProperties
            {
                HasSettings = true
            };
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new DLsiteMetadataProvider(options, this);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new DLsiteMetadataSettingsView();
        }
    }
}