# Starry Karaoke for MusicBee

A karaoke window for MusicBee with a starry night sky. The current line lights up word by word in
gold, with a soft edge and a glow. Japanese lyrics can be shown as romaji.

## Use

- **View > Starry Karaoke** opens and closes the window.
- **F11** or a double-click toggles fullscreen, and **Esc** leaves it.
- **Right-click** opens a menu with fullscreen, the Japanese mode (romaji only, Japanese with
  romaji under it, or Japanese only) and *Configurações...*.
- The settings are also under *Preferences > Plugins > Starry Karaoke > Configure*. They cover:
  - font, text size and colours;
  - stars and how many;
  - glow;
  - the current line growing;
  - transition speed;
  - lyrics delay;
  - title and progress bar.

## Lyrics

The plugin plays the lyrics MusicBee has for the song. If the `.lrc` next to the audio file has word
timing (`<mm:ss.xx>`, as LYRICS BOCA writes it) and MusicBee's copy does not, the plugin uses that
`.lrc`. Lyrics timed only per line are swept across each line over an estimated time.

## Install

1. Close MusicBee.
2. Copy the contents of the release zip into the Plugins folder (`%APPDATA%\MusicBee\Plugins`, or
   `<MusicBee>\Plugins` for the portable version).
3. Start MusicBee.

`RomajiCore.dll`, `LibNMeCab.dll` and `RomajiLyrics\IpaDic` are shared with Desktop Lyrics (romaji
fork). If they are already there, copying them over is harmless.

## Build

`.\build.ps1` runs the tests, builds the plugin and writes `dist\MusicBee-StarryKaraoke-<version>.zip`.
It needs the .NET SDK and builds for .NET Framework 4.8.
