using Playnite.SDK;

namespace DLsiteMetadata;

public class DLsiteItemOption(string name, string value, string link) : GenericItemOption(name, value)
{
    public string Link { get; set; } = link;
}