#!/usr/bin/env python3
import json
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile

ROOT = Path(__file__).resolve().parent.parent
manifest = json.loads((ROOT / "package/manifest.json").read_text(encoding="utf-8"))
dll = ROOT / "src/bin/Release/netstandard2.1/RagnavikGameplay.dll"
if not dll.is_file():
    raise SystemExit("Build the plugin before packaging.")

archive = ROOT / "artifacts" / f"LostKode-Ragnavik_Gameplay-{manifest['version_number']}.zip"
archive.parent.mkdir(parents=True, exist_ok=True)
files = {
    "CHANGELOG.md": ROOT / "CHANGELOG.md",
    "README.md": ROOT / "README.md",
    "config/lostkode.ragnavik.gameplay.cfg": ROOT / "package/config/lostkode.ragnavik.gameplay.cfg",
    "icon.png": ROOT / "package/icon.png",
    "manifest.json": ROOT / "package/manifest.json",
    "plugins/RagnavikGameplay/RagnavikGameplay.dll": dll,
}
missing = [str(path) for path in files.values() if not path.is_file()]
if missing:
    raise SystemExit("Missing package inputs: " + ", ".join(missing))

with ZipFile(archive, "w", ZIP_DEFLATED) as output:
    for name, source in files.items():
        output.write(source, name)
print(archive)
