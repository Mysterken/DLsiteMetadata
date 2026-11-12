using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DLsiteMetadata;

/// <summary>
///     Reads the data DLsite's server-rendered work page embeds for its Nuxt frontend
///     (<c>&lt;script id="__NUXT_DATA__"&gt;</c>). It holds the same <c>{ meta, data }</c> response the product API
///     returns, so it serves as a fallback source when the API itself is unavailable.
///     The payload is serialized with "devalue": one flat array where every object property and array item is an
///     index into that array. Some arrays are tagged wrappers (<c>["ShallowReactive", index]</c>).
/// </summary>
public static class NuxtPayloadReader
{
    private static readonly Regex PayloadScript = new(
        @"<script[^>]*id=""__NUXT_DATA__""[^>]*>(?<json>.*?)</script>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly HashSet<string> ReactiveWrappers = new()
        { "ShallowReactive", "Reactive", "Ref", "ShallowRef", "EmptyRef", "EmptyShallowRef" };

    /// <summary>The product's <c>{ meta, data }</c> response from the page, <c>null</c> when it is not there.</summary>
    public static JObject TryReadProductResponse(string html, string productId)
    {
        if (string.IsNullOrEmpty(html)) return null;

        var script = PayloadScript.Match(html);
        if (!script.Success) return null;

        JArray pool;
        try
        {
            pool = JArray.Parse(script.Groups["json"].Value);
        }
        catch (JsonException)
        {
            return null;
        }

        if (pool.Count == 0) return null;

        var root = Resolve(pool, 0, new Dictionary<int, JToken>());

        return root?["data"]?["dlsite-product-" + productId] as JObject;
    }

    private static JToken Resolve(JArray pool, int index, Dictionary<int, JToken> resolved)
    {
        // Negative indexes are devalue's markers for undefined, NaN and infinities.
        if (index < 0 || index >= pool.Count) return JValue.CreateNull();

        if (resolved.TryGetValue(index, out var known)) return known;

        var raw = pool[index];
        JToken result;

        if (raw is JObject obj)
        {
            var copy = new JObject();
            resolved[index] = copy;

            foreach (var property in obj.Properties()) copy[property.Name] = ResolveReference(pool, property.Value, resolved);

            result = copy;
        }
        else if (raw is JArray array)
        {
            if (array.Count == 2 && array[0].Type == JTokenType.String &&
                ReactiveWrappers.Contains((string)array[0]) && array[1].Type == JTokenType.Integer)
            {
                result = Resolve(pool, (int)array[1], resolved);
            }
            else
            {
                var copy = new JArray();
                resolved[index] = copy;

                foreach (var item in array) copy.Add(ResolveReference(pool, item, resolved));

                result = copy;
            }
        }
        else
        {
            result = raw.DeepClone();
        }

        resolved[index] = result;
        return result;
    }

    /// <summary>Integers inside objects and arrays point into the pool, everything else is a literal.</summary>
    private static JToken ResolveReference(JArray pool, JToken value, Dictionary<int, JToken> resolved)
    {
        return value.Type == JTokenType.Integer ? Resolve(pool, (int)value, resolved) : value.DeepClone();
    }
}
