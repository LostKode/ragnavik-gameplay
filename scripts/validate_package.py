#!/usr/bin/env python3
from pathlib import Path
import sys
import zipfile

archive = Path(sys.argv[1])
required = {
    "manifest.json", "icon.png", "README.md", "CHANGELOG.md",
    "config/lostkode.ragnavik.gameplay.cfg",
    "plugins/RagnavikGameplay/RagnavikGameplay.dll",
}
if not archive.is_file():
    raise SystemExit(f"missing package archive: {archive}")
with zipfile.ZipFile(archive) as package:
    corrupt = package.testzip()
    names = set(package.namelist())
if corrupt:
    raise SystemExit(f"corrupt package entry: {corrupt}")
missing = required - names
if missing:
    raise SystemExit(f"missing package entries: {sorted(missing)}")
forbidden = [name for name in names if name.endswith((".pdb", ".zip"))]
if forbidden:
    raise SystemExit(f"forbidden package entries: {sorted(forbidden)}")
print(f"package layout valid: {archive}")
