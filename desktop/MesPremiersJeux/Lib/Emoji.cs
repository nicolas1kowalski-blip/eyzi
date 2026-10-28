using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 🎨 Les emojis EN COULEUR. Sur certaines tablettes, WPF dessine les emojis
    /// en noir et blanc (police sans couleur) : ici, chaque emoji d'un texte est
    /// remplacé par sa petite image Twemoji (Images\emoji\&lt;code&gt;.png, CC BY 4.0,
    /// seulement ceux que l'application utilise). Un emoji sans image reste un
    /// caractère : rien ne casse.
    /// </summary>
    public static class Emoji
    {
        // Un emoji : un caractère de base (plan astral ou symboles du plan de base),
        // puis d'éventuels sélecteur de variante, teinte de peau, et suites avec ZWJ.
        private static readonly Regex Rx = new Regex(
            @"(?:[\uD83C][\uDDE6-\uDDFF]){2}" +
            @"|(?:[\uD83C-\uD83E][\uDC00-\uDFFF]|[☀-➿⬀-⯿⌀-⏿〰〽㊗㊙©®™ℹ↔-↙↩↪⌚⌛Ⓜ▪▫▶◀◻-◾])" +
            @"(?:️|\uD83C[\uDFFB-\uDFFF]|‍(?:[\uD83C-\uD83E][\uDC00-\uDFFF]|[☀-➿])️?)*",
            RegexOptions.Compiled);

        private static readonly Dictionary<string, ImageSource> Cache = new Dictionary<string, ImageSource>();
        private static string Dir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "emoji");

        private static string Code(string emoji, bool keepFe0f)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < emoji.Length; i++)
            {
                int cp = char.ConvertToUtf32(emoji, i);
                if (char.IsHighSurrogate(emoji[i])) i++;
                if (cp == 0xFE0F && !keepFe0f) continue;
                if (sb.Length > 0) sb.Append('-');
                sb.Append(cp.ToString("x"));
            }
            return sb.ToString();
        }

        /// <summary>L'image d'un emoji, ou null s'il n'en a pas.</summary>
        public static ImageSource Find(string emoji)
        {
            if (string.IsNullOrEmpty(emoji)) return null;
            lock (Cache)
            {
                if (Cache.TryGetValue(emoji, out var hit)) return hit;
                ImageSource src = null;
                foreach (var code in new[] { Code(emoji, false), Code(emoji, true) })
                {
                    var p = Path.Combine(Dir, code + ".png");
                    if (!File.Exists(p)) continue;
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = new MemoryStream(File.ReadAllBytes(p));
                        bmp.EndInit();
                        bmp.Freeze();
                        src = bmp;
                        break;
                    }
                    catch { }
                }
                Cache[emoji] = src;
                return src;
            }
        }

        public static bool Has(string text) => !string.IsNullOrEmpty(text) && Rx.IsMatch(text);

        /// <summary>Une image d'emoji seule, à la taille voulue (null si pas d'image).</summary>
        public static Image Picture(string emoji, double size)
        {
            var src = Find(emoji);
            if (src == null) return null;
            return new Image { Source = src, Width = size, Height = size, Stretch = Stretch.Uniform, SnapsToDevicePixels = true };
        }

        /// <summary>Remplace, dans ce TextBlock, chaque emoji par son image (même taille que la police).</summary>
        public static void Fix(TextBlock tb)
        {
            if (tb == null) return;
            string text = tb.Text;
            if (!Has(text)) return;
            double size = tb.FontSize * 1.18;
            var inlines = new List<Inline>();
            int last = 0;
            bool any = false;
            foreach (Match m in Rx.Matches(text))
            {
                var img = Picture(m.Value, size);
                if (img == null) continue; // pas d'image : le caractère reste
                if (m.Index > last) inlines.Add(new Run(text.Substring(last, m.Index - last)));
                img.Margin = new Thickness(1, 0, 1, -size * 0.2);
                inlines.Add(new InlineUIContainer(img) { BaselineAlignment = BaselineAlignment.TextBottom });
                last = m.Index + m.Length;
                any = true;
            }
            if (!any) return;
            if (last < text.Length) inlines.Add(new Run(text.Substring(last)));
            tb.Inlines.Clear();
            foreach (var i in inlines) tb.Inlines.Add(i);
        }

        /// <summary>Parcourt un arbre (logique puis visuel) et colore tous les emojis trouvés.</summary>
        public static void FixTree(DependencyObject root)
        {
            if (root == null) return;
            try
            {
                if (root is TextBlock tb) { Fix(tb); return; }
                if (root is ContentControl cc && cc.Content is string s && Has(s))
                {
                    var t = new TextBlock { Text = s, FontSize = cc.FontSize, FontWeight = cc.FontWeight, Foreground = cc.Foreground, TextAlignment = TextAlignment.Center };
                    Fix(t);
                    cc.Content = t;
                    return;
                }
                foreach (var child in LogicalTreeHelper.GetChildren(root))
                    if (child is DependencyObject d) FixTree(d);
                if (root is Visual v)
                {
                    int n = VisualTreeHelper.GetChildrenCount(v);
                    for (int i = 0; i < n; i++) FixTree(VisualTreeHelper.GetChild(v, i));
                }
            }
            catch { }
        }
    }
}
