using System;
using System.IO;
using System.Windows.Media;
using System.Xml.Serialization;

namespace MusicBeePlugin
{
    /// <summary>Everything the user can change, saved as XML in MusicBee's persistent storage folder.</summary>
    public sealed class Settings
    {
        public string FontFamily = "Cascadia Mono, Consolas, Yu Gothic UI";
        /// <summary>Multiplies the text size the window picks from its height.</summary>
        public double TextScale = 1.0;
        public string LitColor = "#FFCE5C";
        public string HeadColor = "#FFFFEB";
        public string UnlitColor = "#9496B2";
        public string DimColor = "#4E5068";
        public string GlowColor = "#FFB43C";
        public string BackgroundTop = "#0A0A18";
        public string BackgroundBottom = "#16102C";
        public string AccentColor = "#8C78FF";
        public JapaneseMode Japanese = JapaneseMode.RomajiOnly;
        public bool ShowStars = true;
        /// <summary>0.25 (few) to 2 (many).</summary>
        public double StarDensity = 1.0;
        public bool Glow = true;
        public bool GrowCurrentLine = true;
        /// <summary>0.5 (slow) to 2 (fast): scroll, fades and the line growing.</summary>
        public double TransitionSpeed = 1.0;
        /// <summary>Added to the lyrics' timing: positive shows them later.</summary>
        public int LyricsOffsetMs = 0;
        public bool ShowHeader = true;
        public bool ShowProgress = true;
        /// <summary>Fullscreen shows only the lyrics (no title, no progress bar).</summary>
        public bool FullscreenLyricsOnly = true;
        /// <summary>ffmpeg.exe for recording videos; empty: found on PATH or next to the plugin.</summary>
        public string FfmpegPath = "";
        /// <summary>Jump over the instrumental intro whenever a song starts.</summary>
        public bool AutoSkipIntro;
        /// <summary>Recorded videos start just before the first sung line (audio cut at the same point).</summary>
        public bool VideoSkipIntro;
        /// <summary>How long before the first sung line a skipped intro resumes.</summary>
        public int IntroLeadMs = 3000;
        /// <summary>Frame rate of the karaoke window: 0 follows the monitor's refresh rate.</summary>
        public int WindowFps = 0;
        public string VideoResolution = "1920x1080";
        public int VideoFps = 30;

        public double WindowLeft = double.NaN, WindowTop = double.NaN, WindowWidth = 1100, WindowHeight = 680;
        public bool Fullscreen;
        public bool WindowOpen;

        public const string FileName = "mb_StarryKaraoke.xml";

        public static Settings Load(string folder, string fileName = FileName)
        {
            try
            {
                var path = Path.Combine(folder, fileName);
                if (File.Exists(path))
                    using (var f = File.OpenRead(path))
                        return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(f);
            }
            catch (Exception) { }
            return new Settings();
        }

        public void Save(string folder, string fileName = FileName)
        {
            var path = Path.Combine(folder, fileName);
            var tmp = path + ".tmp";
            using (var f = File.Create(tmp)) new XmlSerializer(typeof(Settings)).Serialize(f, this);
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        public Settings Clone()
        {
            var c = (Settings)MemberwiseClone();
            return c;
        }

        public static Color ParseColor(string hex, Color fallback)
        {
            try { return (Color)ColorConverter.ConvertFromString(hex); }
            catch (Exception) { return fallback; }
        }

        public static string ToHex(Color c) => c.A == 255 ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
