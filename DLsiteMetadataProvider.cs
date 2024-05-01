using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLsiteMetadata
{
    public class DLsiteMetadataProvider : OnDemandMetadataProvider
    {
        private readonly MetadataRequestOptions options;
        private readonly DLsiteMetadata plugin;

        public override List<MetadataField> AvailableFields => throw new NotImplementedException();

        public DLsiteMetadataProvider(MetadataRequestOptions options, DLsiteMetadata plugin)
        {
            this.options = options;
            this.plugin = plugin;
        }

        // Override additional methods based on supported metadata fields.
        public override string GetDescription(GetMetadataFieldArgs args)
        {
            return options.GameData.Name + " description";
        }
    }
}