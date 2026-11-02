# Starry Karaoke for MusicBee

A karaoke window for MusicBee with a starry night sky. The current line lights up word by word in
gold, with a soft edge and a glow. Japanese lyrics can be shown as romaji.

## Features

- Word-by-word lyrics, with a soft gold edge, glow, line fades and smooth scrolling.
- Japanese mode: romaji only, Japanese with romaji below it, or Japanese only.
- Twinkling stars, a dimmed context of the previous and next lines, an intro countdown and a
  progress bar.
- Fullscreen mode, an auto-hiding transport bar and a skip-intro shortcut.
- **Video recording:** renders the whole song, with the lyrics and the audio, to an MP4 file.

## Use

- **View > Starry Karaoke** opens and closes the window.
- **F11** or a double-click toggles fullscreen, and **Esc** leaves it.
- **Right-click** opens a menu with fullscreen, the Japanese mode (romaji only, Japanese with
  romaji under it, or Japanese only), *Record song video...* and *Settings...*.
- The settings are also under *Preferences > Plugins > Starry Karaoke > Configure*. They cover:
  - font, text size and colours;
  - stars and how many;
  - glow;
  - the current line growing;
  - transition speed;
  - lyrics delay;
  - title and progress bar;
  - the instrumental intro;
  - video resolution, frame rate and the ffmpeg path.

## Lyrics

The plugin plays the lyrics MusicBee has for the song. If the `.lrc` next to the audio file has word
timing (`<mm:ss.xx>`, as LYRICS BOCA writes it) and MusicBee's copy does not, the plugin uses that
`.lrc`. Lyrics timed only per line are swept across each line over an estimated time.

## Video recording

*Record song video...* in the window's right-click menu (or *Tools > Starry Karaoke: record song
video*) asks where to save and renders the song offline, frame by frame, outside MusicBee. The
recording needs `ffmpeg.exe`: either in the Plugins folder, on `PATH`, installed with
`winget install Gyan.FFmpeg`, or set explicitly in the settings.

The settings control the resolution, the frame rate, and whether the recording starts at the intro
or right at the first lyric.

## Install

1. Close MusicBee.
2. Copy the contents of the release zip into the Plugins folder (`%APPDATA%\MusicBee\Plugins`, or
   `<MusicBee>\Plugins` for the portable version).
3. Start MusicBee.

`RomajiCore.dll`, `LibNMeCab.dll` and `RomajiLyrics\IpaDic` are shared with Desktop Lyrics (romaji
fork). If they are already there, copying them over is harmless.

The plugin targets .NET Framework 4.8, which is built into Windows 10 and 11.

## Build

`.\build.ps1` runs the tests, builds the plugin and writes `dist\MusicBee-StarryKaraoke-<version>.zip`.
It needs the .NET SDK and builds for .NET Framework 4.8.

## Third-party components

NMeCab, IPADic and RomajiCore keep their own licenses, included in the release zip under
`StarryKaraoke\licenses` and in the [licenses](licenses) folder.
