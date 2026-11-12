using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DLsiteMetadata.Tests.TestSupport;

/// <summary>Responses recorded from DLsite (see <c>tools/record_fixtures.py</c>).</summary>
public static class Fixture
{
    public static string Text(string fileName)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", fileName);

        return File.ReadAllText(path, Encoding.UTF8);
    }

    /// <summary>A recorded product API response, <c>{ meta, data }</c>.</summary>
    public static JObject Response(string name)
    {
        return JObject.Parse(Text($"api_{name}.json"));
    }

    /// <summary>The <c>data</c> object of a recorded product API response.</summary>
    public static JObject Product(string name)
    {
        return (JObject)Response(name)["data"];
    }
}
