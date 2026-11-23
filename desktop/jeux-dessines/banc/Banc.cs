// Le banc d'essai des jeux « dessinés » : il fait tourner le VRAI moteur d'un jeu (Games/JetMoteur.cs, Games/PecheMoteur.cs…,
// avec ses dessins) sous Linux, lui joue un regard écrit à l'avance, et écrit une image SVG par instant demandé.
// Usage : mono banc.exe <dossier de sortie> <scène de départ> <durée en s> <une image toutes les n> [graine]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MesPremiersJeux.Games;

static class Banc
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static string N(double v) => Math.Round(v, 2).ToString(Inv);

    static void Main(string[] a)
    {
        string dossier = a[0], scene = a.Length > 1 ? a[1] : "voiture";
        double duree = a.Length > 2 ? double.Parse(a[2], Inv) : 20; int chaque = a.Length > 3 ? int.Parse(a[3]) : 15;
        Directory.CreateDirectory(dossier);
        int graine = a.Length > 4 ? int.Parse(a[4]) : 7;
        string[] scenesDuJet = { "voiture", "feu", "remplir", "jardin", "roue" };
        // « peche », « peche1 », « peche2 » : La pêche (au niveau 0, 1 ou 2) ; sinon une scène du jet d'eau
        bool peche = scene.StartsWith("peche");
        bool patate = scene.StartsWith("patate");
        VecMoteur m = patate ? new PatateMoteur(graine) { Prenom = "Laura" } : peche ? (VecMoteur)new PecheMoteur(graine) : new JetMoteur(graine);
        // Patatou : ce que l'enfant choisit, écrit à l'avance (l'instant, la zone)
        var choix = new List<KeyValuePair<double, string>>();
        if (patate) foreach (var c in "1.6 c:0|4.0 c:1|6.4 c:0|11.0 f:3|12.2 c:3|14.5 f:4|15.6 c:7|18.0 f:5|19.0 c:0|21.5 f:6|22.5 c:0|25.0 photo|28.0 surprise|33.5 photo|36.5 f:3|37.5 c:10|40 f:0|41 c:4|43 nu".Split('|'))
            choix.Add(new KeyValuePair<double, string>(double.Parse(c.Split(' ')[0], Inv), c.Split(' ')[1]));
        double voixJusqua = 0;
        var journal = new StringBuilder();
        double t = 0;
        m.Dit = s => { journal.AppendLine(t.ToString("0.00", Inv) + "  VOIX  " + s); voixJusqua = t + 0.5 + s.Length * 0.055; };
        if (patate) m.VoixOccupee = () => t < voixJusqua;
        m.Son = (s, i) => journal.AppendLine(t.ToString("0.00", Inv) + "  SON   " + s + (i != 0 ? " " + i : ""));
        m.Bravo = () => journal.AppendLine(t.ToString("0.00", Inv) + "  BRAVO");
        m.Demarre(peche ? (scene.Length > 5 ? int.Parse(scene.Substring(5)) : 0) : Math.Max(0, Array.IndexOf(scenesDuJet, scene)));
        // Le regard automatique : il va vers ce qu'il reste à arroser, y reste, et regarde parfois ailleurs une seconde.
        double gx = 800, gy = 420, ailleurs = 0; var alea = new Random(3);
        int image = 0;
        for (int i = 0; t < duree; i++)
        {
            t = i / 30.0;
            if (patate)
            {
                // le regard va sur la carte un peu avant de la choisir ; sinon il regarde Patatou
                double vx = 615 + 120 * Math.Sin(t * 0.8), vy = 380 + 40 * Math.Sin(t * 1.1);
                while (choix.Count > 0 && choix[0].Key <= t) { m.Appuie(choix[0].Value); journal.AppendLine(t.ToString("0.00", Inv) + "  CHOIX " + choix[0].Value); choix.RemoveAt(0); }
                if (choix.Count > 0 && choix[0].Key - t < 0.9) { var z = m.Zones.Find(q => q.Id == choix[0].Value); if (z != null) { vx = z.X + z.L / 2; vy = z.Y + z.H / 2; } }
                gx += (vx - gx) * 0.2; gy += (vy - gy) * 0.2;
                m.Regard(gx, gy);
            }
            else if (ailleurs > 0) ailleurs -= 1 / 30.0;
            else
            {
                if (m.ProchaineCible(out double cx, out double cy)) { gx += (cx - gx) * 0.22; gy += (cy - gy) * 0.22; }
                else { gx += (800 + 260 * Math.Sin(t * 0.9) - gx) * 0.08; gy += (430 + 90 * Math.Sin(t * 1.3) - gy) * 0.08; }
                m.Regard(gx + 9 * Math.Sin(t * 7.3), gy + 7 * Math.Cos(t * 5.9));      // un regard n'est jamais tout à fait immobile
                if (alea.NextDouble() < 0.004) ailleurs = 1.0;
            }
            m.Avance(1 / 30.0);
            if (i % chaque == 0) File.WriteAllText(Path.Combine(dossier, (image++).ToString("0000") + ".svg"), Svg(m));
        }
        File.WriteAllText(Path.Combine(dossier, "journal.txt"), journal.ToString());
        Console.WriteLine(image + " images ; " + m.Reussites + " réussites ; journal :\n" + journal);
    }

    // ───────────── Le dessin d'une image, en SVG ─────────────
    static string Couleur(string v, string[] c)
    {
        if (v == null) return null;
        if (v[0] != '$') return v;
        if (c != null) for (int i = 0; i + 1 < c.Length; i += 2) if (c[i] == v.Substring(1)) return c[i + 1];
        return "#FF00FF";
    }

    static string Svg(VecMoteur m)
    {
        var defs = new StringBuilder(); var corps = new StringBuilder(); int id = 0;
        double h = m.Horizon / VecMoteur.H * 100;
        defs.Append($"<linearGradient id=\"fond\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"{m.CielHaut}\"/><stop offset=\"{N(h)}%\" stop-color=\"{m.CielBas}\"/><stop offset=\"{N(h)}%\" stop-color=\"{m.Sol}\"/><stop offset=\"1\" stop-color=\"{m.Sol}\"/></linearGradient>");
        corps.Append("<rect width=\"1600\" height=\"900\" fill=\"url(#fond)\"/>");
        foreach (var calque in m.Calques)
            foreach (var o in calque.Objets)
            {
                if (o.Op <= 0.003) continue;
                corps.Append($"<g transform=\"translate({N(o.X)},{N(o.Y)}) rotate({N(o.R)}) scale({o.SX.ToString("0.####", Inv)},{o.SY.ToString("0.####", Inv)})\" opacity=\"{N(o.Op)}\">");
                switch (o.Genre)
                {
                    case VecGenre.Dessin:
                        foreach (var f in m.Dessins[o.Dessin])
                        {
                            string fond = "none";
                            if (f.G != null)
                            {
                                var p = f.G.Split('|'); var e = p[0].Split(';'); string g = "g" + (id++);
                                defs.Append(e[0] == "L"
                                    ? $"<linearGradient id=\"{g}\" gradientUnits=\"userSpaceOnUse\" x1=\"{e[1]}\" y1=\"{e[2]}\" x2=\"{e[3]}\" y2=\"{e[4]}\">"
                                    : $"<radialGradient id=\"{g}\" gradientUnits=\"userSpaceOnUse\" cx=\"{e[1]}\" cy=\"{e[2]}\" r=\"{e[3]}\">");
                                foreach (var st in p.Skip(1)) { var x = st.Split(';'); defs.Append($"<stop offset=\"{x[0]}\" stop-color=\"{Couleur(x[1], o.Couleurs)}\" stop-opacity=\"{x[2]}\"/>"); }
                                defs.Append(e[0] == "L" ? "</linearGradient>" : "</radialGradient>");
                                fond = "url(#" + g + ")";
                            }
                            else if (f.F != null) fond = Couleur(f.F, o.Couleurs);
                            string trait = f.S != null && f.W > 0 ? Couleur(f.S, o.Couleurs) : "none";
                            corps.Append($"<path d=\"{f.D}\" fill=\"{fond}\" stroke=\"{trait}\" stroke-width=\"{N(f.W)}\" opacity=\"{N(f.O)}\" stroke-linejoin=\"round\" stroke-linecap=\"round\"/>");
                        }
                        break;
                    case VecGenre.Plein:
                        if (o.Points != null)
                        {
                            string clip = "";
                            if (o.Decoupe != null) { string c = "c" + (id++); defs.Append($"<clipPath id=\"{c}\"><path d=\"{m.Dessins[o.Decoupe][0].D}\"/></clipPath>"); clip = $" clip-path=\"url(#{c})\""; }
                            corps.Append($"<polygon points=\"{string.Join(" ", o.Points.Select(N))}\" fill=\"{o.Couleur}\"{clip}/>");
                        }
                        break;
                    case VecGenre.Ligne:
                        if (o.Points != null)
                            corps.Append($"<polyline points=\"{string.Join(" ", o.Points.Select(N))}\" fill=\"none\" stroke=\"{o.Couleur}\" stroke-width=\"{N(o.Epaisseur)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\""
                                + (o.Tiret > 0 ? $" stroke-dasharray=\"{N(o.Tiret * o.Epaisseur)} {N(o.Espace * o.Epaisseur)}\" stroke-dashoffset=\"{N(o.Decalage * o.Epaisseur)}\"" : "") + "/>");
                        break;
                    case VecGenre.Texte:
                        corps.Append($"<text font-family=\"Arial Black,Arial\" font-weight=\"900\" font-size=\"{N(o.Taille)}\" text-anchor=\"middle\" dominant-baseline=\"middle\" fill=\"{o.Couleur}\" stroke=\"#2A1C4E\" stroke-width=\"10\" stroke-linejoin=\"round\" paint-order=\"stroke\">{o.Texte}</text>");
                        break;
                }
                corps.Append("</g>");
            }
        return "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1600\" height=\"900\" viewBox=\"0 0 1600 900\"><defs>" + defs + "</defs>" + corps + "</svg>";
    }
}
