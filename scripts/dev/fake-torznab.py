#!/usr/bin/env python3
"""Minimal fake Torznab indexer for UI testing.

Serves caps and canned search results for any query.
"""
import http.server
import urllib.parse

CAPS = """<?xml version="1.0" encoding="UTF-8"?>
<caps>
  <server version="1.0" title="FakeTorznab" />
  <limits max="100" default="50" />
  <searching>
    <search available="yes" supportedParams="q" />
  </searching>
  <categories>
    <category id="4000" name="PC">
      <subcat id="4050" name="Games" />
    </category>
    <category id="1000" name="Console" />
  </categories>
</caps>
"""

ITEM_TEMPLATE = """    <item>
      <title>{title}</title>
      <guid>fake-{n}</guid>
      <link>http://localhost:9899/dl/{n}.torrent</link>
      <comments>http://localhost:9899/details/{n}</comments>
      <pubDate>Sat, 04 Jul 2026 10:00:00 +0000</pubDate>
      <size>{size}</size>
      <category>4050</category>
      <enclosure url="http://localhost:9899/dl/{n}.torrent" length="{size}" type="application/x-bittorrent" />
      <torznab:attr name="category" value="4050" xmlns:torznab="http://torznab.com/schemas/2015/feed" />
      <torznab:attr name="seeders" value="{seeders}" xmlns:torznab="http://torznab.com/schemas/2015/feed" />
      <torznab:attr name="peers" value="{peers}" xmlns:torznab="http://torznab.com/schemas/2015/feed" />
    </item>
"""

RELEASES = [
    ("Hades.v1.38116.Repack-FAKE", 5_368_709_120, 150, 180),
    ("Hades-CODEX", 6_442_450_944, 80, 95),
    ("Hades.v1.0.GOG", 5_100_000_000, 40, 44),
    ("Hades.Update.v1.38290-FAKE", 500_000_000, 25, 30),
    ("Hades.MULTi10.Repack.All.DLC-FAKE", 5_900_000_000, 12, 20),
    ("Hades.The.Blood.Price.DLC-FAKE", 200_000_000, 18, 22),
]


def rss(items: str) -> str:
    return (
        '<?xml version="1.0" encoding="UTF-8"?>\n'
        '<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom" '
        'xmlns:torznab="http://torznab.com/schemas/2015/feed">\n'
        "  <channel>\n    <title>FakeTorznab</title>\n" + items + "  </channel>\n</rss>\n"
    )


class Handler(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        parsed = urllib.parse.urlparse(self.path)
        qs = urllib.parse.parse_qs(parsed.query)
        t = (qs.get("t") or ["search"])[0]

        # Serve a minimal valid single-file torrent so grabs (e.g. into a
        # blackhole download client) succeed end-to-end.
        if parsed.path.startswith("/dl/"):
            n = parsed.path[len("/dl/") :].split(".")[0]
            name = f"fake-release-{n}".encode()
            pieces = (int(n or 0) % 256).to_bytes(1, "big") * 20
            data = (
                b"d8:announce27:http://localhost:9899/announce"
                b"4:infod6:lengthi1024e4:name" + str(len(name)).encode() + b":" + name +
                b"12:piece lengthi16384e6:pieces20:" + pieces + b"ee"
            )
            self.send_response(200)
            self.send_header("Content-Type", "application/x-bittorrent")
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)
            return

        if t == "caps":
            body = CAPS
        else:
            items = "".join(
                ITEM_TEMPLATE.format(title=title, n=i, size=size, seeders=s, peers=s + l)
                for i, (title, size, s, l) in enumerate(RELEASES)
            )
            body = rss(items)

        data = body.encode()
        self.send_response(200)
        self.send_header("Content-Type", "application/xml; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def log_message(self, *args):
        pass


if __name__ == "__main__":
    http.server.HTTPServer(("127.0.0.1", 9899), Handler).serve_forever()
