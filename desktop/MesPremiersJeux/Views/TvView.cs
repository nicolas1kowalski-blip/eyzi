using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// 📺 LA TÉLÉ : une télécommande au regard pour la télé LG de la maison.
    /// Dix-huit grandes touches : allumer, éteindre, le son, les chaînes, pause,
    /// YouTube, Netflix, Disney+, et les flèches pour se déplacer dans les menus.
    /// </summary>
    public sealed class TvView : UserControl
    {
        private static readonly Brush Dark = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));
        private readonly TextBlock _status;
        private readonly UniformGrid _grid;
        private readonly TextBlock _empty;
        private Settings _settings;
        private bool _busy;
        public event Action BackRequested;

        public TvView()
        {
            var root = new Grid { Background = new LinearGradientBrush(Color.FromRgb(0xE6, 0xEE, 0xFF), Color.FromRgb(0xFF, 0xF3, 0xE6), 90) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var head = new DockPanel { Margin = new Thickness(16, 12, 16, 4), LastChildFill = true };
            var back = new Button { Content = "⬅  Les jeux", Style = (Style)Application.Current.Resources["BackButton"], Margin = new Thickness(0, 0, 14, 0) };
            back.Click += (s, e) => BackRequested?.Invoke();
            DockPanel.SetDock(back, Dock.Left);
            head.Children.Add(back);
            var title = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            title.Children.Add(new TextBlock { Text = "📺 La télé", FontSize = 30, FontWeight = FontWeights.Bold, Foreground = Dark });
            _status = new TextBlock { FontSize = 16, Foreground = Dark, Opacity = 0.75, TextWrapping = TextWrapping.Wrap };
            title.Children.Add(_status);
            head.Children.Add(title);
            root.Children.Add(head);

            _grid = new UniformGrid { Columns = 6, Rows = 3, Margin = new Thickness(14, 6, 14, 14) };
            Grid.SetRow(_grid, 1);
            root.Children.Add(_grid);

            // Rangée 1 : marche / arrêt, le son, pause.
            Touche("🔌", "Allumer", Color.FromRgb(0xD9, 0xF5, 0xE0), async () => { LgTv.WakeOnLan(_settings.TvMac); if (!LgTv.Connected) { await Task.Delay(6000); await Connecter(); } return true; });
            Touche("⏻", "Éteindre", Color.FromRgb(0xFF, 0xE0, 0xE0), async () => await LgTv.TurnOff() != null);
            Touche("🔊", "Plus fort", Color.FromRgb(0xFF, 0xF0, 0xC8), async () => await LgTv.VolumeUp() != null);
            Touche("🔉", "Moins fort", Color.FromRgb(0xFF, 0xF0, 0xC8), async () => await LgTv.VolumeDown() != null);
            Touche("🔇", "Muet", Color.FromRgb(0xFF, 0xF0, 0xC8), async () => await LgTv.ToggleMute() != null);
            Touche("⏯", "Pause", Color.FromRgb(0xE2, 0xD4, 0xFF), async () => { var r = await LgTv.Pause(); if (r != null && Cloud.JsonGet(r, "returnValue") == "false") r = await LgTv.Play(); return r != null; });
            // Rangée 2 : les chaînes, les applications, les menus.
            Touche("⏫", "Chaîne +", Color.FromRgb(0xE2, 0xD4, 0xFF), async () => await LgTv.ChannelUp() != null);
            Touche("⏬", "Chaîne −", Color.FromRgb(0xE2, 0xD4, 0xFF), async () => await LgTv.ChannelDown() != null);
            Touche("▶️", "YouTube", Color.FromRgb(0xFF, 0xE0, 0xE0), async () => await LgTv.Launch("youtube.leanback.v4") != null);
            Touche("🎬", "Netflix", Color.FromRgb(0xFF, 0xE0, 0xE0), async () => await LgTv.Launch("netflix") != null);
            Touche("🏰", "Disney+", Color.FromRgb(0xFF, 0xE0, 0xE0), async () => await LgTv.Launch("com.disney.disneyplus-prod") != null);
            Touche("🏠", "Menu télé", Color.FromRgb(0xDE, 0xF7, 0xEA), () => LgTv.ButtonAsync("HOME"));
            // Rangée 3 : se déplacer, valider, revenir.
            Touche("⬅️", "Gauche", Color.FromRgb(0xE6, 0xEE, 0xFF), () => LgTv.ButtonAsync("LEFT"));
            Touche("⬆️", "Haut", Color.FromRgb(0xE6, 0xEE, 0xFF), () => LgTv.ButtonAsync("UP"));
            Touche("⬇️", "Bas", Color.FromRgb(0xE6, 0xEE, 0xFF), () => LgTv.ButtonAsync("DOWN"));
            Touche("➡️", "Droite", Color.FromRgb(0xE6, 0xEE, 0xFF), () => LgTv.ButtonAsync("RIGHT"));
            Touche("✅", "OK", Color.FromRgb(0xDE, 0xF7, 0xEA), () => LgTv.ButtonAsync("ENTER"));
            Touche("↩️", "Retour", Color.FromRgb(0xDE, 0xF7, 0xEA), () => LgTv.ButtonAsync("BACK"));

            _empty = new TextBlock
            {
                Text = "📺\nDemande à un grand de régler la télé dans les réglages ⚙, page Licence & nuage.",
                TextAlignment = TextAlignment.Center, FontSize = 26, Foreground = Dark, TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed,
            };
            Grid.SetRow(_empty, 1);
            root.Children.Add(_empty);

            Content = root;
            Emoji.FixTree(root);
            LgTv.StatusChanged += s => Dispatcher.BeginInvoke(new Action(() => MontrerEtat(s)));
        }

        private void Touche(string icone, string label, Color fond, Func<Task<bool>> action)
        {
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"], Margin = new Thickness(6), Background = new SolidColorBrush(fond),
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    Children = { Art.Visual("", icone, 56), new TextBlock { Text = label, FontSize = 22, FontWeight = FontWeights.Bold, Foreground = Dark, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) } },
                },
            };
            b.Click += async (s, e) =>
            {
                if (_busy) return;
                _busy = true;
                try
                {
                    Speech.SayAlways(label);
                    if (!LgTv.Connected && label != "Allumer" && !await Connecter()) return;
                    bool ok = await action();
                    if (!ok) _status.Text = "La télé n'a pas voulu : " + LgTv.LastError;
                    else GazeStats.RecordComm("tv-" + label);
                }
                catch (Exception ex) { _status.Text = "Oups : " + ex.Message; }
                finally { _busy = false; }
            };
            _grid.Children.Add(b);
        }

        private async Task<bool> Connecter()
        {
            if (string.IsNullOrWhiteSpace(_settings?.TvIp)) { _status.Text = "La télé n'est pas réglée."; return false; }
            _status.Text = "Je cherche la télé…";
            var key = await LgTv.ConnectAsync(_settings.TvIp, _settings.TvKey);
            if (key == null) { _status.Text = "Pas de télé : " + LgTv.LastError; return false; }
            if (key != _settings.TvKey) { _settings.TvKey = key; _settings.Save(); }
            if (string.IsNullOrEmpty(_settings.TvMac)) { var mac = await LgTv.GetMacAsync(); if (mac != null) { _settings.TvMac = mac; _settings.Save(); } }
            return true;
        }

        private void MontrerEtat(string s)
        {
            switch (s)
            {
                case "pret": _status.Text = "La télé est connectée. Regarde une touche."; break;
                case "connexion": _status.Text = "Je cherche la télé…"; break;
                case "erreur": _status.Text = "Pas de télé : " + LgTv.LastError; break;
                default: _status.Text = "Regarde une touche : je me connecte à la télé."; break;
            }
        }

        /// <summary>On entre dans l'écran : on lit les réglages et on se connecte en arrière-plan.</summary>
        public void Enter()
        {
            _settings = Settings.Load();
            bool ok = !string.IsNullOrWhiteSpace(_settings.TvIp);
            _grid.Visibility = ok ? Visibility.Visible : Visibility.Collapsed;
            _empty.Visibility = ok ? Visibility.Collapsed : Visibility.Visible;
            MontrerEtat(LgTv.Status);
            if (ok && !LgTv.Connected) _ = Connecter();
        }
    }
}
