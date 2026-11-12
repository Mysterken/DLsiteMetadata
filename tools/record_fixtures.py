"""Records live DLsite responses as fixtures for the test project.

The fixtures pin the *shape* of DLsite's product API, so a change on DLsite's side shows up as a failing test
(after re-recording) instead of a broken plugin. Re-run this script, review the diff, and fix the mapper when
the recorded shape changed.

Usage (from the repository root):
    python tools/record_fixtures.py
"""
import os
import re
import urllib.request

OUT = os.path.join(os.path.dirname(__file__), "..", "tests", "DLsiteMetadata.Tests", "Fixtures")

# name -> (site, product id, locale)
API_FIXTURES = {
    "voice_en": ("home", "RJ01005844", "en_US"),
    "cg_en": ("maniax", "RJ01034407", "en_US"),
    "cg_ja": ("maniax", "RJ01034407", "ja_JP"),
    "game_en": ("home", "RJ104685", "en_US"),
    "manga_translated_en": ("home", "RJ01739317", "en_US"),
    "adult_series_en": ("maniax", "RJ01073400", "en_US"),
    "series_music_en": ("maniax", "RJ01753547", "en_US"),
    "tracklist_en": ("maniax", "RJ01638386", "en_US"),
    "hidden_by_country": ("maniax", "RJ01099639", "en_US"),
    "not_found": ("maniax", "RJ99999999", "en_US"),
    "wrong_site": ("maniax", "RJ01723044", "en_US"),
}


def get(url, locale):
    request = urllib.request.Request(
        url, headers={"User-Agent": "Mozilla/5.0", "Cookie": "locale=%s; loginchecked=1" % locale})
    with urllib.request.urlopen(request, timeout=60) as response:
        return response.read().decode("utf8")


def main():
    os.makedirs(OUT, exist_ok=True)

    for name, (site, product_id, locale) in API_FIXTURES.items():
        # "wrong_site" asks under a site the product does not live on (it is a girls work), to record the
        # API's "Incorrect Site Id" answer, which names the right site.
        url = ("https://www.dlsite.com/api/product/%s/fetchProduct/%s?product_id=%s&product_type=work&locale=%s"
               % (site, product_id, product_id, locale))
        body = get(url, locale)
        with open(os.path.join(OUT, "api_%s.json" % name), "w", encoding="utf8", newline="\n") as handle:
            handle.write(body)
        print("recorded api_%s.json (%d bytes)" % (name, len(body)))

    # The Nuxt payload of a work page, without the rest of the (large) page.
    page = get("https://www.dlsite.com/maniax/work/=/product_id/RJ01034407.html", "en_US")
    payload = re.search(r'<script[^>]*id="__NUXT_DATA__"[^>]*>.*?</script>', page, re.S).group(0)
    with open(os.path.join(OUT, "page_payload_cg_en.html"), "w", encoding="utf8", newline="\n") as handle:
        handle.write("<!doctype html><html><body>%s</body></html>" % payload)
    print("recorded page_payload_cg_en.html (%d bytes)" % len(payload))


if __name__ == "__main__":
    main()
