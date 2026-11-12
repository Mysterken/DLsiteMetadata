"""Regenerates source/SupportedLanguageNames.cs.

DLsite's product API only returns language *codes* (JPN, ENG, CHI_HANS...). The localized names are
taken from the canonical URL of DLsite's server-rendered listing pages, which embed them as
`lang_options[0]/<name>`, once per locale.

Usage (from the repository root):
    python tools/generate_language_names.py            # fetch from DLsite and rewrite the .cs file
    python tools/generate_language_names.py --cache f  # reuse a previously fetched JSON file
"""
import argparse
import concurrent.futures as cf
import json
import os
import re
import sys
import urllib.parse
import urllib.request

# Order must match the SupportedLanguages enum.
LOCALES = "ja_JP en_US zh_CN zh_TW ko_KR es_ES de_DE fr_FR id_ID it_IT pt_BR sv_SE th_TH vi_VN".split()
CODES = "JPN ENG CHI_HANS CHI_HANT CHI KO_KR SPA GER FRE IND ITA POR SWE THA VIE NM".split()
OUTPUT = os.path.join(os.path.dirname(__file__), "..", "source", "SupportedLanguageNames.cs")

TEMPLATE = '''using System;
using System.Collections.Generic;
using DLsiteMetadata.Enums;

namespace DLsiteMetadata;

/// <summary>
///     Localized names of the language codes found in the product API's <c>work_languages</c>.
///     The names are not part of the API response, they were collected from DLsite's own listing pages
///     with <c>tools/generate_language_names.py</c>. Each row follows the order of <see cref="SupportedLanguages" />.
/// </summary>
public static class SupportedLanguageNames
{
    private static readonly Dictionary<string, string[]> Names = new(StringComparer.OrdinalIgnoreCase)
    {
%(rows)s
    };

    /// <summary>
    ///     "CHI" is the generic Chinese flag set next to the simplified/traditional ones,
    ///     DLsite does not list it on the product page.
    /// </summary>
    private static readonly HashSet<string> HiddenCodes = new(StringComparer.OrdinalIgnoreCase) { "CHI" };

    public static IReadOnlyCollection<string> Codes => Names.Keys;

    public static bool TryGet(string code, SupportedLanguages language, out string name)
    {
        name = null;

        if (code == null || HiddenCodes.Contains(code) || !Names.TryGetValue(code, out var row)) return false;

        name = row[(int)language];
        return true;
    }

    public static bool IsComplete(string code)
    {
        return Names.TryGetValue(code, out var row) && row.Length == Enum.GetValues(typeof(SupportedLanguages)).Length;
    }
}
'''


def get(url, locale):
    request = urllib.request.Request(
        url, headers={"User-Agent": "Mozilla/5.0", "Cookie": "locale=%s; loginchecked=1" % locale})
    try:
        with urllib.request.urlopen(request, timeout=40) as response:
            return response.read().decode("utf8", errors="replace")
    except Exception:
        return ""


def fetch_one(job):
    code, locale = job
    page = get("https://www.dlsite.com/maniax/fsr/=/language/jp/work_category%%5B0%%5D/doujin/"
               "options%%5B0%%5D/%s/order/trend/?locale=%s" % (code, locale), locale)
    canonical = re.search(r'rel="canonical" href="([^"]*)"', page)
    match = canonical and re.search(r"lang_options%5B0%5D/([^/\"]+)", canonical.group(1))
    return code, locale, urllib.parse.unquote_plus(match.group(1)) if match else None


def fetch_all():
    names = {}
    with cf.ThreadPoolExecutor(12) as pool:
        for code, locale, name in pool.map(fetch_one, [(c, l) for c in CODES for l in LOCALES]):
            names.setdefault(code, {})[locale] = name
    return names


def cs_string(value):
    return '"%s"' % value.replace("\\", "\\\\").replace('"', '\\"')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--cache", help="JSON file to read instead of fetching (and to save into when fetching)")
    args = parser.parse_args()

    if args.cache and os.path.exists(args.cache):
        with open(args.cache, encoding="utf8") as handle:
            names = json.load(handle)
    else:
        names = fetch_all()
        if args.cache:
            with open(args.cache, "w", encoding="utf8") as handle:
                json.dump(names, handle, ensure_ascii=False, indent=1)

    missing = [(c, l) for c in CODES for l in LOCALES if not names.get(c, {}).get(l)]
    if missing:
        sys.exit("Missing names, refusing to write: %s" % missing)

    rows = "\n".join(
        '        ["%s"] = [%s],' % (code, ", ".join(cs_string(names[code][l]) for l in LOCALES)) for code in CODES)

    with open(OUTPUT, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(TEMPLATE % {"rows": rows})
    print("Wrote %s" % os.path.abspath(OUTPUT))


if __name__ == "__main__":
    main()
