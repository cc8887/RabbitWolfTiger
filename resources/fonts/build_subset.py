#!/usr/bin/env python3
"""Rebuild the bundled Chinese UI font from the pinned official Noto CJK font.
Usage: python resources/fonts/build_subset.py /path/to/NotoSansCJKsc-Regular.otf
Requires fonttools==4.61.1. Source download remains explicit; this script makes no network calls.
"""
from pathlib import Path
import hashlib
import json
import sys
from fontTools import subset
from fontTools.ttLib import TTFont

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
SOURCE_SHA256 = "2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b"
SOURCE_URL = "https://raw.githubusercontent.com/notofonts/noto-cjk/165c01b46ea533872e002e0785ff17e44f6d97d8/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf"
SOURCE_FILES = ["ui/localization/UiText.cs"]

def main():
    source = Path(sys.argv[1])
    assert hashlib.sha256(source.read_bytes()).hexdigest() == SOURCE_SHA256, "Source font checksum mismatch"
    chars = set(range(0x20, 0x7f))  # All digits, Latin shortcut keys, and ASCII punctuation.
    for relative in SOURCE_FILES:
        chars.update(ord(c) for c in (ROOT / relative).read_text(encoding="utf-8") if not c.isspace())
    chars.update([0x20, 0x3000, 0x00b7, 0x2014, 0x2026, 0x2190, 0x2191, 0x2192, 0x2193])
    font = TTFont(source, recalcTimestamp=False)
    missing = chars - set(font.getBestCmap())
    assert not missing, f"Source font lacks characters: {missing}"
    copyright_text = font['name'].getDebugName(0)
    options = subset.Options()
    options.name_IDs = ['*']
    options.name_legacy = True
    options.name_languages = ['*']
    worker = subset.Subsetter(options=options)
    worker.populate(unicodes=sorted(chars))
    worker.subset(font)
    # This is a modified/subset font: use our own family name, preserve copyright and license records.
    names = {1: "RWT Sans SC", 2: "Regular", 3: "RWT-Sans-SC-UI-1", 4: "RWT Sans SC Regular", 6: "RWTSansSC-Regular", 16: "RWT Sans SC", 17: "Regular"}
    for record in font['name'].names:
        if record.nameID in names:
            record.string = names[record.nameID].encode(record.getEncoding())
    cff = font['CFF '].cff
    cff.fontNames = ['RWTSansSC-Regular']
    cff.topDictIndex[0].FamilyName = 'RWT Sans SC'
    cff.topDictIndex[0].FullName = 'RWT Sans SC Regular'
    output = HERE / 'RwtSansSC-Regular.otf'
    font.save(output)
    result = TTFont(output)
    assert chars <= set(result.getBestCmap()), "Subset lost a required character"
    (HERE / 'ui_glyphs.txt').write_text(''.join(chr(c) for c in sorted(chars)) + '\n', encoding='utf-8')
    metadata = {"source_url": SOURCE_URL, "source_sha256": SOURCE_SHA256, "copyright": copyright_text,
                "license": "SIL Open Font License 1.1; see OFL.txt", "modified_family": "RWT Sans SC",
                "character_sources": SOURCE_FILES, "extra_ranges": ["U+0020–U+007E"],
                "required_codepoints": len(chars), "output_sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
                "output_bytes": output.stat().st_size, "fonttools_version": "4.61.1"}
    (HERE / 'provenance.json').write_text(json.dumps(metadata, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(metadata, ensure_ascii=False))

if __name__ == '__main__':
    main()
