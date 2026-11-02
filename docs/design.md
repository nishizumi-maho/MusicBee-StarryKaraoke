# Starry Karaoke: MusicBee plugin (design)

A new plugin, independent of Desktop Lyrics. It has its own project, repository and DLL
(`mb_StarryKaraoke.dll`) and does not touch Desktop Lyrics or the Lyrics Panel (BETA).

## What it does

- **Window:** opens under *View > Starry Karaoke*. It is a normal resizable window, with F11 or
  double-click for fullscreen and Esc to leave it. Position and size are saved.
- **Look:** the same as the NOX LUX video.
  - A night-sky gradient with twinkling stars.
  - At the top, "✦ title — artist ✦".
  - The current line in the middle, with the previous and next lines dimmed above and below.
  - `♪ ♪ ♪` pulsing during instrumental breaks and a countdown before the first line.
  - A gold progress bar at the bottom.
- **Lyrics:** the ones MusicBee loads (`NowPlaying_GetLyrics`), through its own LRC parser:
  - simple LRC and multiple timestamps per line;
  - `[offset:]`;
  - LRC with per-word timing (`<mm:ss.xx>`, as LYRICS BOCA writes it), lighting up word by
    word;
  - without per-word timing, the highlight sweeps across each line over an estimated time.
- **Romaji:** uses RomajiCore with NMeCab, the same engine as Desktop Lyrics, and the dictionary
  already in `Plugins\RomajiLyrics\IpaDic`. The romaji follows the word timing.
- **Japanese:** a setting with three options: romaji only, Japanese with romaji below, or
  Japanese only.

## Transitions

- **Soft edge:** the gold fill advances with a feathered edge (gradient mask) and a
  glow, instead of a hard cut.
- **Line-change fade:** the incoming line brightens and the outgoing one dims gradually (about
  350 ms).
- **Current line grows:** the sung line grows to about 108% and returns to normal size
  when it ends.
- **Smooth scrolling:** lines slide to their new position with ease-in-out.

## Settings

They live in their own window, opened by right-clicking the window or from the plugin's
preferences in MusicBee:

- font and size;
- colors: lit, unlit, glow and background;
- Japanese mode;
- number of stars, stars on or off;
- transition speed;
- lyrics delay (ms);
- show or hide the header and the progress bar.

Settings are saved as JSON in MusicBee's data folder.

## How it is built

- .NET Framework 4.8, like Desktop Lyrics.
- The window is **WPF**, which draws with hardware acceleration. Glow (BlurEffect), a
  gradient opacity mask, scale and fade run smoothly at 60 fps even in fullscreen. With GDI+, as in
  Desktop Lyrics, that would be heavy.
- RomajiCore is included as a copy in `external/RomajiCore`, so the repository stays self-contained.
- Unit tests (xunit) for the parser, the per-character timings and the per-word romaji.
- `build.ps1` produces the zip, with contents ready to go into the Plugins folder.
