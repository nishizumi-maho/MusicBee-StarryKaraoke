using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MusicBeePlugin
{
    /// <summary>Settings, applied live while the window is open. Cancel puts the old ones back.</summary>
    internal sealed class SettingsWindow : Window
    {
        private readonly Settings _original;
        private readonly Settings _edit;
        private readonly Action<Settings> _preview;
        private readonly StackPanel _panel = new StackPanel { Margin = new Thickness(16) };
        public Settings Result { get; private set; }

        public SettingsWindow(Settings current, Action<Settings> preview)
        {
            _original = current.Clone();
            _edit = current.Clone();
            _preview = preview;
            Title = "Starry Karaoke — Configurações";
            Width = 460;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Section("Texto");
            var fonts = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(x => x).ToList();
            if (!fonts.Contains(_edit.FontFamily)) fonts.Insert(0, _edit.FontFamily);
            var font = new ComboBox { ItemsSource = fonts, SelectedItem = _edit.FontFamily, IsEditable = true };
            font.SelectionChanged += (s, e) => { if (font.SelectedItem is string f) Set(x => x.FontFamily = f); };
            font.LostFocus += (s, e) => Set(x => x.FontFamily = font.Text);
            Row("Fonte", font);
            Row("Tamanho do texto", Slider(0.5, 2.0, _edit.TextScale, v => Set(x => x.TextScale = v)));

            var jp = new ComboBox { ItemsSource = new[] { "Só romaji", "Japonês + romaji embaixo", "Só japonês" }, SelectedIndex = (int)_edit.Japanese };
            jp.SelectionChanged += (s, e) => Set(x => x.Japanese = (JapaneseMode)jp.SelectedIndex);
            Row("Músicas em japonês", jp);

            var offset = new TextBox { Text = _edit.LyricsOffsetMs.ToString(CultureInfo.InvariantCulture) };
            offset.TextChanged += (s, e) => { if (int.TryParse(offset.Text, out var v)) Set(x => x.LyricsOffsetMs = v); };
            Row("Atraso da letra (ms, + = depois)", offset);

            Section("Cores");
            ColorRow("Aceso", () => _edit.LitColor, v => _edit.LitColor = v);
            ColorRow("Ponta do brilho", () => _edit.HeadColor, v => _edit.HeadColor = v);
            ColorRow("Linha atual (apagada)", () => _edit.UnlitColor, v => _edit.UnlitColor = v);
            ColorRow("Outras linhas", () => _edit.DimColor, v => _edit.DimColor = v);
            ColorRow("Glow", () => _edit.GlowColor, v => _edit.GlowColor = v);
            ColorRow("Fundo (cima)", () => _edit.BackgroundTop, v => _edit.BackgroundTop = v);
            ColorRow("Fundo (baixo)", () => _edit.BackgroundBottom, v => _edit.BackgroundBottom = v);
            ColorRow("Título", () => _edit.AccentColor, v => _edit.AccentColor = v);

            Section("Efeitos");
            Check("Estrelas", _edit.ShowStars, v => Set(x => x.ShowStars = v));
            Row("Quantidade de estrelas", Slider(0.25, 2.0, _edit.StarDensity, v => Set(x => x.StarDensity = v)));
            Check("Glow nas letras acesas", _edit.Glow, v => Set(x => x.Glow = v));
            Check("Linha atual cresce", _edit.GrowCurrentLine, v => Set(x => x.GrowCurrentLine = v));
            Row("Velocidade das transições", Slider(0.5, 2.0, _edit.TransitionSpeed, v => Set(x => x.TransitionSpeed = v)));
            Check("Mostrar título", _edit.ShowHeader, v => Set(x => x.ShowHeader = v));
            Check("Mostrar barra de progresso", _edit.ShowProgress, v => Set(x => x.ShowProgress = v));
            Check("Tela cheia mostra só a letra", _edit.FullscreenLyricsOnly, v => Set(x => x.FullscreenLyricsOnly = v));

            var monitorHz = KaraokeWindow.MonitorRefreshRate();
            var winFps = new ComboBox
            {
                ItemsSource = new[] { $"Monitor ({monitorHz} Hz)", "30", "60", "120", "144", "240" },
                SelectedIndex = 0
            };
            if (_edit.WindowFps > 0) winFps.SelectedItem = _edit.WindowFps.ToString(CultureInfo.InvariantCulture);
            winFps.SelectionChanged += (s, e) => Set(x => x.WindowFps = winFps.SelectedIndex <= 0 ? 0 : int.Parse((string)winFps.SelectedItem, CultureInfo.InvariantCulture));
            Row("FPS da janela", winFps);

            Section("Introdução instrumental");
            Check("Pular a introdução automaticamente ao tocar", _edit.AutoSkipIntro, v => Set(x => x.AutoSkipIntro = v));
            Check("Vídeo gravado começa direto na letra", _edit.VideoSkipIntro, v => Set(x => x.VideoSkipIntro = v));
            Row("Começar quanto antes da letra (s)", Slider(0, 10, _edit.IntroLeadMs / 1000.0, v => Set(x => x.IntroLeadMs = (int)(v * 1000))));

            Section("Vídeo gravado");
            var res = new ComboBox { ItemsSource = new[] { "1280x720", "1920x1080", "2560x1440", "3840x2160" }, IsEditable = true, Text = _edit.VideoResolution };
            res.SelectionChanged += (s, e) => { if (res.SelectedItem is string r) Set(x => x.VideoResolution = r); };
            res.LostFocus += (s, e) => Set(x => x.VideoResolution = res.Text);
            Row("Resolução", res);
            var fps = new ComboBox { ItemsSource = new[] { 24, 25, 30, 50, 60, 120, 144, 240 }, SelectedItem = _edit.VideoFps };
            fps.SelectionChanged += (s, e) => { if (fps.SelectedItem is int f) Set(x => x.VideoFps = f); };
            Row("FPS do vídeo (mais = demora mais)", fps);
            var ff = new TextBox { Text = _edit.FfmpegPath };
            ff.TextChanged += (s, e) => Set(x => x.FfmpegPath = ff.Text.Trim().Trim('"'));
            Row("ffmpeg.exe (vazio = automático)", ff);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            var reset = new Button { Content = "Padrão", Width = 84, Margin = new Thickness(0, 0, 8, 0) };
            reset.Click += (s, e) => { Result = Keep(new Settings()); Close(); };
            var ok = new Button { Content = "OK", Width = 84, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
            ok.Click += (s, e) => { Result = _edit; Close(); };
            var cancel = new Button { Content = "Cancelar", Width = 84, IsCancel = true };
            buttons.Children.Add(reset);
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            _panel.Children.Add(buttons);

            Content = new ScrollViewer { Content = _panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = SystemParameters.WorkArea.Height * 0.9 };
            Closed += (s, e) => { if (Result == null) _preview(_original); };
        }

        /// <summary>Defaults, but keeping where the window is.</summary>
        private Settings Keep(Settings d)
        {
            d.WindowLeft = _original.WindowLeft; d.WindowTop = _original.WindowTop;
            d.WindowWidth = _original.WindowWidth; d.WindowHeight = _original.WindowHeight;
            d.Fullscreen = _original.Fullscreen; d.WindowOpen = _original.WindowOpen;
            return d;
        }

        private void Set(Action<Settings> change)
        {
            change(_edit);
            _preview(_edit.Clone());
        }

        private void Section(string title) =>
            _panel.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, FontSize = 14, Margin = new Thickness(0, _panel.Children.Count == 0 ? 0 : 14, 0, 6) });

        private void Row(string label, FrameworkElement control)
        {
            var g = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            g.ColumnDefinitions.Add(new ColumnDefinition());
            var l = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(control, 1);
            g.Children.Add(l);
            g.Children.Add(control);
            _panel.Children.Add(g);
        }

        private void Check(string label, bool value, Action<bool> changed)
        {
            var c = new CheckBox { Content = label, IsChecked = value, Margin = new Thickness(0, 3, 0, 3) };
            c.Checked += (s, e) => changed(true);
            c.Unchecked += (s, e) => changed(false);
            _panel.Children.Add(c);
        }

        private static Slider Slider(double min, double max, double value, Action<double> changed)
        {
            var s = new Slider { Minimum = min, Maximum = max, Value = Math.Max(min, Math.Min(max, value)), VerticalAlignment = VerticalAlignment.Center };
            s.ValueChanged += (o, e) => changed(e.NewValue);
            return s;
        }

        private void ColorRow(string label, Func<string> get, Action<string> set)
        {
            var swatch = new Border { Width = 46, Height = 20, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 8, 0) };
            var hex = new TextBox { Text = get(), Width = 90, VerticalContentAlignment = VerticalAlignment.Center };
            void Show() => swatch.Background = new SolidColorBrush(Settings.ParseColor(get(), Colors.Transparent));
            Show();
            var pick = new Button { Content = "...", Width = 30, Margin = new Thickness(8, 0, 0, 0) };
            pick.Click += (s, e) =>
            {
                var c = Settings.ParseColor(get(), Colors.White);
                using (var dlg = new System.Windows.Forms.ColorDialog { FullOpen = true, Color = System.Drawing.Color.FromArgb(c.R, c.G, c.B) })
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        hex.Text = Settings.ToHex(Color.FromRgb(dlg.Color.R, dlg.Color.G, dlg.Color.B));
            };
            hex.TextChanged += (s, e) =>
            {
                try { ColorConverter.ConvertFromString(hex.Text); } catch (Exception) { return; }
                Set(x => set(hex.Text));
                Show();
            };
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(swatch);
            row.Children.Add(hex);
            row.Children.Add(pick);
            Row(label, row);
        }
    }
}
