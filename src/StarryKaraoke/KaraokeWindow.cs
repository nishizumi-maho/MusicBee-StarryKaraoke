using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MusicBeePlugin
{
    /// <summary>Song position between polls: runs on a stopwatch and eases towards each new reading, so
    /// small jitter in the player position never makes the sweep jump.</summary>
    internal sealed class PlaybackClock
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private double _basePos, _baseAt;
        public bool Playing { get; private set; }

        public double NowMs() => Playing ? _basePos + (_watch.Elapsed.TotalMilliseconds - _baseAt) : _basePos;

        public void Sync(double positionMs, bool playing)
        {
            var now = _watch.Elapsed.TotalMilliseconds;
            var predicted = NowMs();
            var drift = positionMs - predicted;
            _basePos = playing && Playing && Math.Abs(drift) < 250 ? predicted + drift * 0.15 : positionMs;
            _baseAt = now;
            Playing = playing;
        }
    }

    /// <summary>The karaoke window. F11 or a double-click toggles fullscreen, Esc leaves it, right-click
    /// opens the menu.</summary>
    internal sealed class KaraokeWindow : Window
    {
        private sealed class SceneLayer : FrameworkElement
        {
            private readonly KaraokeWindow _owner;
            private readonly KaraokeScene.Layer _layer;
            public SceneLayer(KaraokeWindow owner, KaraokeScene.Layer layer) { _owner = owner; _layer = layer; IsHitTestVisible = false; }
            protected override void OnRender(DrawingContext dc) => _owner._scene.Render(_layer, dc, RenderSize, _owner._songTime);
        }

        private readonly KaraokeScene _scene;
        private readonly PlaybackClock _clock = new PlaybackClock();
        private readonly SceneLayer _sky, _glow, _text;
        private readonly BlurEffect _blur = new BlurEffect { KernelType = KernelType.Gaussian, RenderingBias = RenderingBias.Performance };
        private readonly Func<(double PositionMs, bool Playing)> _poll;
        private readonly Stopwatch _frameWatch = Stopwatch.StartNew();
        private double _lastFrame, _lastPoll = -1e9;
        private double _songTime;
        private Settings _settings;
        private bool _fullscreen;
        private Rect _restore;

        /// <summary>The commands the window's buttons and keys use (for tests).</summary>
        internal PlayerControls Controls => _controls;

        public event EventHandler SettingsRequested;
        public event EventHandler RecordRequested;
        public event EventHandler SkipIntroRequested;
        public event Action<JapaneseMode> JapaneseModeChosen;

        private readonly TransportBar _transport;
        private readonly PlayerControls _controls;
        private double _songMs;
        private bool _playing;
        private Point _lastMouse;

        public KaraokeWindow(Settings settings, Func<(double, bool)> poll, PlayerControls controls)
        {
            RaiseFrameRate();
            // after any player command, read the position on the next frame instead of up to 200 ms later.
            // `player` is its own variable: the lambdas must call the plugin's commands, never this wrapper
            // (capturing a variable that later points at the wrapper made every seek call itself forever).
            var player = controls;
            _controls = new PlayerControls
            {
                PlayPause = () => { player.PlayPause?.Invoke(); _lastPoll = -1e9; },
                Previous = () => { player.Previous?.Invoke(); _lastPoll = -1e9; },
                Next = () => { player.Next?.Invoke(); _lastPoll = -1e9; },
                Seek = ms => { player.Seek?.Invoke(ms); _lastPoll = -1e9; }
            };
            _settings = settings;
            _poll = poll;
            _scene = new KaraokeScene(settings);
            Title = "Starry Karaoke";
            Background = Brushes.Black;
            MinWidth = 360;
            MinHeight = 240;
            Width = settings.WindowWidth > 100 ? settings.WindowWidth : 1100;
            Height = settings.WindowHeight > 100 ? settings.WindowHeight : 680;
            if (!double.IsNaN(settings.WindowLeft) && OnSomeScreen(settings.WindowLeft, settings.WindowTop, Width, Height))
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = settings.WindowLeft;
                Top = settings.WindowTop;
            }
            else WindowStartupLocation = WindowStartupLocation.CenterScreen;

            _sky = new SceneLayer(this, KaraokeScene.Layer.Sky);
            _glow = new SceneLayer(this, KaraokeScene.Layer.Glow) { Effect = _blur, Opacity = 0.85 };
            _text = new SceneLayer(this, KaraokeScene.Layer.Text);
            var grid = new Grid { Background = Brushes.Transparent, ClipToBounds = true };
            grid.Children.Add(_sky);
            grid.Children.Add(_glow);
            grid.Children.Add(_text);
            _transport = new TransportBar(_controls, Settings.ParseColor(settings.LitColor, Color.FromRgb(255, 206, 92)));
            grid.Children.Add(_transport);
            MouseMove += (s, e) =>
            {
                var p = e.GetPosition(this);
                if ((p - _lastMouse).Length < 2) return;   // ignore the jitter of a still mouse
                _lastMouse = p;
                _transport.OnWindowMouseMove(p, ActualHeight);
            };
            MouseLeave += (s, e) => _transport.Show(false);
            Content = grid;
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.Grayscale);
            RenderOptions.SetEdgeMode(_sky, EdgeMode.Unspecified);

            ContextMenu = BuildMenu();
            PreviewKeyDown += OnKey;   // preview: arrows and space reach us before any control can take them
            MouseDoubleClick += (s, e) => { if (e.ChangedButton == MouseButton.Left && !_transport.IsMouseOver) SetFullscreen(!_fullscreen); };
            Loaded += (s, e) => { if (settings.Fullscreen) SetFullscreen(true); };
            CompositionTarget.Rendering += OnFrame;
            Closed += (s, e) => CompositionTarget.Rendering -= OnFrame;
            ApplyBlur();
        }

        private static bool OnSomeScreen(double l, double t, double w, double h)
        {
            var r = new Rect(l, t, w, h);
            var v = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                             SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
            return r.IntersectsWith(v) && r.Left < v.Right - 60 && r.Top < v.Bottom - 60;
        }

        public void SetSong(List<KaraokeLine> lines, string title, double songMs)
        {
            _scene.SetSong(lines, title, songMs);
            _songMs = songMs;
            _lastPoll = -1e9;
        }

        public void ApplySettings(Settings s)
        {
            _settings = s;
            _scene.Apply(s);
            _scene.LyricsOnly = _fullscreen && s.FullscreenLyricsOnly;
            ApplyBlur();
            ContextMenu = BuildMenu();
        }

        private void ApplyBlur() => _glow.Visibility = _settings.Glow ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>Window bounds as they should be saved (the normal ones while fullscreen).</summary>
        public void StoreBounds(Settings s)
        {
            var b = _fullscreen ? _restore : (WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds);
            if (b.Width > 0 && b.Height > 0)
            {
                s.WindowLeft = b.Left; s.WindowTop = b.Top; s.WindowWidth = b.Width; s.WindowHeight = b.Height;
            }
            s.Fullscreen = _fullscreen;
        }

        // ---- frame rate: WPF paints at most at its desired frame rate (60 unless raised), so raise it to
        // the monitor's refresh rate; a fixed WindowFps below that skips frames instead.

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern bool EnumDisplaySettings(string device, int mode, ref DevMode devMode);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private struct DevMode
        {
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
            public short SpecVersion, DriverVersion, Size, DriverExtra;
            public int Fields, PositionX, PositionY, DisplayOrientation, DisplayFixedOutput;
            public short Color, Duplex, YResolution, TTOption, Collate;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 32)] public string FormName;
            public short LogPixels;
            public int BitsPerPel, PelsWidth, PelsHeight, DisplayFlags, DisplayFrequency;
        }

        /// <summary>Highest refresh rate among the monitors (60 if unknown).</summary>
        public static int MonitorRefreshRate()
        {
            var best = 0;
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                var dm = new DevMode { Size = (short)System.Runtime.InteropServices.Marshal.SizeOf(typeof(DevMode)) };
                if (EnumDisplaySettings(screen.DeviceName, -1, ref dm) && dm.DisplayFrequency > 1) best = Math.Max(best, dm.DisplayFrequency);
            }
            return best > 0 ? best : 60;
        }

        private static bool _frameRateRaised;

        private static void RaiseFrameRate()
        {
            if (_frameRateRaised) return;
            _frameRateRaised = true;
            try
            {
                System.Windows.Media.Animation.Timeline.DesiredFrameRateProperty.OverrideMetadata(
                    typeof(System.Windows.Media.Animation.Timeline),
                    new FrameworkPropertyMetadata { DefaultValue = MonitorRefreshRate() });
            }
            catch (Exception) { }   // already set in this process
        }

        private void OnFrame(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized || !IsVisible) return;
            var now = _frameWatch.Elapsed.TotalSeconds;
            if (_settings.WindowFps > 0 && now - _lastFrame < 1.0 / _settings.WindowFps - 0.0015) return;
            var dt = Math.Min(0.25, now - _lastFrame);
            _lastFrame = now;
            if (now - _lastPoll > 0.2)
            {
                _lastPoll = now;
                try
                {
                    var (pos, playing) = _poll();
                    _clock.Sync(pos, playing);
                    _playing = playing;
                }
                catch (Exception) { }
            }
            _songTime = _clock.NowMs() - _settings.LyricsOffsetMs;
            var size = _text.RenderSize;
            _blur.Radius = Math.Max(8, Math.Min(40, size.Height / 26));
            _scene.Update(dt, _songTime, size);
            _sky.InvalidateVisual();
            _glow.InvalidateVisual();
            _text.InvalidateVisual();
            _transport.Update(_clock.NowMs(), _songMs, _playing);
            _scene.HideProgress = _transport.IsShown;
        }

        private void OnKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11) SetFullscreen(!_fullscreen);
            else if (e.Key == Key.Escape && _fullscreen) SetFullscreen(false);
            else if (e.Key == Key.P) SkipIntroRequested?.Invoke(this, EventArgs.Empty);
            else if (e.Key == Key.Space) { _controls.PlayPause?.Invoke(); _transport.Show(true); }
            else if (e.Key == Key.Left) { _transport.Jump(-10000); _transport.Show(true); }
            else if (e.Key == Key.Right) { _transport.Jump(10000); _transport.Show(true); }
            else return;
            e.Handled = true;
        }

        private void SetFullscreen(bool on)
        {
            if (on == _fullscreen) return;
            if (on)
            {
                _restore = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                if (_restore.Width > 0) { Left = _restore.Left; Top = _restore.Top; Width = _restore.Width; Height = _restore.Height; }
            }
            _fullscreen = on;
            _scene.LyricsOnly = on && _settings.FullscreenLyricsOnly;
            ContextMenu = BuildMenu();
        }

        private ContextMenu BuildMenu()
        {
            var menu = new ContextMenu();
            var full = new MenuItem { Header = _fullscreen ? "Sair da tela cheia (Esc)" : "Tela cheia (F11)" };
            full.Click += (s, e) => SetFullscreen(!_fullscreen);
            menu.Items.Add(full);
            var skip = new MenuItem { Header = "Pular introdução (P)" };
            skip.Click += (s, e) => SkipIntroRequested?.Invoke(this, EventArgs.Empty);
            menu.Items.Add(skip);

            var jp = new MenuItem { Header = "Japonês" };
            foreach (var (mode, label) in new[] { (JapaneseMode.RomajiOnly, "Só romaji"),
                                                  (JapaneseMode.JapaneseAndRomaji, "Japonês + romaji"),
                                                  (JapaneseMode.JapaneseOnly, "Só japonês") })
            {
                var item = new MenuItem { Header = label, IsCheckable = true, IsChecked = _settings.Japanese == mode };
                item.Click += (s, e) => JapaneseModeChosen?.Invoke(mode);
                jp.Items.Add(item);
            }
            menu.Items.Add(jp);

            var record = new MenuItem { Header = "Gravar vídeo da música..." };
            record.Click += (s, e) => RecordRequested?.Invoke(this, EventArgs.Empty);
            menu.Items.Add(record);

            var settings = new MenuItem { Header = "Configurações..." };
            settings.Click += (s, e) => SettingsRequested?.Invoke(this, EventArgs.Empty);
            menu.Items.Add(settings);
            menu.Items.Add(new Separator());
            var close = new MenuItem { Header = "Fechar" };
            close.Click += (s, e) => Close();
            menu.Items.Add(close);
            return menu;
        }
    }
}
