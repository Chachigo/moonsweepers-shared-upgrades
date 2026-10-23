#!/bin/sh
# Lance Moon Sweepers avec BepInEx : force winhttp natif (doorstop) dans le préfixe Proton, puis lance via Steam.
REG=/mnt/Fast2/SteamLibrary/steamapps/compatdata/5335510/pfx/user.reg
grep -q 'AppDefaults\\\\MoonSweepers.exe\\\\DllOverrides' "$REG" ||
  printf '\n[Software\\\\Wine\\\\AppDefaults\\\\MoonSweepers.exe\\\\DllOverrides] %s\n"winhttp"="native,builtin"\n' "$(date +%s)" >> "$REG"
exec steam steam://rungameid/5335510
