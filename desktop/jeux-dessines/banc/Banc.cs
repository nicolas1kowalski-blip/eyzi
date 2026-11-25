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
        // « chamboule », « chamboule1 », « chamboule2 » : Chamboule-tout (au niveau 0, 1 ou 2)
        bool peche = scene.StartsWith("peche");
        bool patate = scene.StartsWith("patate"), corps = scene.StartsWith("corps");
        bool chamboule = scene.StartsWith("chamboule");
        bool panier = scene == "panier";
        bool fil = scene == "fil";
        bool gouter = scene == "gouter", train = scene == "train";
        bool maison = scene == "maison";
        bool valise = scene == "valise";
        bool lit = scene == "lit";
        VecMoteur m = lit ? new LitMoteur(graine) : valise ? new ValiseMoteur(graine) : maison ? new MaisonMoteur(graine) : gouter ? new GouterMoteur(graine) : train ? new TrainMoteur(graine) : fil ? new FilMoteur(graine) : panier ? new PanierMoteur(graine) : chamboule ? new ChambouleMoteur(graine) : corps ? new CorpsMoteur(graine) : patate ? new PatateMoteur(graine) { Prenom = "Laura" } : peche ? (VecMoteur)new PecheMoteur(graine) : new JetMoteur(graine);
        // Patatou : ce que l'enfant choisit, écrit à l'avance (l'instant, la zone)
        var choix = new List<KeyValuePair<double, string>>();
        if (patate) foreach (var c in "1.6 c:0|4.0 c:1|6.4 c:0|11.0 f:3|12.2 c:3|14.5 f:4|15.6 c:7|18.0 f:5|19.0 c:0|21.5 f:6|22.5 c:0|25.0 photo|28.0 surprise|33.5 photo|36.5 f:3|37.5 c:10|40 f:0|41 c:4|43 nu".Split('|'))
            choix.Add(new KeyValuePair<double, string>(double.Parse(c.Split(' ')[0], Inv), c.Split(' ')[1]));
        double voixJusqua = 0;
        var journal = new StringBuilder();
        double t = 0;
        m.Dit = s => { journal.AppendLine(t.ToString("0.00", Inv) + "  VOIX  " + s); voixJusqua = t + 0.5 + s.Length * 0.055; };
        if (patate || corps || panier || fil || gouter || train || maison || valise || lit) m.VoixOccupee = () => t < voixJusqua;
        double pose = 0; bool trompe = false;
        // « panier » : le banc joue le rôle de l'activité (elle choisit les nombres et juge les réponses)
        var pm = m as PanierMoteur; int tour = 0; double suite = -1;
        string[] fruits = { "fruit_pomme", "fruit_poire", "fruit_orange", "fruit_citron" };
        int[][] tours = { new[] { 4, 2, 1 }, new[] { 5, 3, 1 }, new[] { 4, 1, 0 }, new[] { 6, 3, 0 } };      // le total, ceux déjà là, on cueille (1) ou on devine (0)
        Action installe = () => { var q = tours[tour % tours.Length]; pm.Prepare(fruits[tour % fruits.Length], q[0], q[1], q[2] == 1, q[2] == 1 ? 0 : 4); };
        // « fil » : un de plus, un de moins
        var fm = m as FilMoteur;
        int[][] filTours = { new[] { 3, 1 }, new[] { 5, 0 }, new[] { 7, 1 }, new[] { 9, 1 }, new[] { 2, 0 } };      // le nombre d'oiseaux, un de plus (1) ou un de moins (0)
        if (fm != null)
        {
            installe = () => { var q = filTours[tour % filTours.Length]; int r = q[1] == 1 ? q[0] + 1 : q[0] - 1; var c = new List<int>(); for (int v = Math.Max(1, r - 1); c.Count < 4; v++) c.Add(v); fm.Prepare(q[0], q[1] == 1, c); };
            installe();
            fm.Demande = s =>
            {
                journal.AppendLine(t.ToString("0.00", Inv) + "  DEMANDE " + s);
                var q = filTours[tour % filTours.Length]; int v = int.Parse(s.Substring(6));
                if (v == (q[1] == 1 ? q[0] + 1 : q[0] - 1)) { fm.Reponse(v); suite = t + 3.4; } else fm.Refuse(v);
            };
        }
        // « gouter » : partager en deux ; « train » : la bande numérique
        var gm = m as GouterMoteur; int[] parts = { 2, 3, 1, 5, 4 };
        if (gm != null)
        {
            installe = () => { int h = parts[tour % parts.Length]; var c = new List<int>(); for (int v = Math.Max(1, h - 1); c.Count < 4; v++) c.Add(v); gm.Prepare(h * 2, "Laura", c); };
            installe();
            gm.Demande = s => { journal.AppendLine(t.ToString("0.00", Inv) + "  DEMANDE " + s); int v = int.Parse(s.Substring(6)), h = parts[tour % parts.Length]; if (v == h) { gm.Reponse(v); suite = t + 3.8 + h * 2 * 0.34; } else gm.Refuse(v); };
        }
        var tm = m as TrainMoteur; int[][] trains = { new[] { 5, 3 }, new[] { 7, 6 }, new[] { 10, 4 }, new[] { 10, 9 } };
        if (tm != null)
        {
            installe = () => { var q = trains[tour % trains.Length]; var c = new List<int>(); for (int v = Math.Max(1, Math.Min(q[0] - 3, q[1] - 1)); c.Count < 4; v++) c.Add(v); tm.Prepare(q[0], q[1], c); };
            installe();
            tm.Demande = s => { journal.AppendLine(t.ToString("0.00", Inv) + "  DEMANDE " + s); int v = int.Parse(s.Substring(6)); if (v == trains[tour % trains.Length][1]) { tm.Reponse(v); suite = t + 3.2; } else tm.Refuse(v); };
        }
        // « maison » : le banc joue l'activité « Range la maison » (les pièces à découvrir, puis huit objets à ranger)
        var mm = m as MaisonMoteur; var vues = new bool[3]; int rangement = -1;
        string[][] objets = { new[] { "casserole", "0" }, new[] { "brosse-a-dents", "1" }, new[] { "ours-peluche", "2" }, new[] { "savon", "1" }, new[] { "bol", "0" }, new[] { "pull", "2" }, new[] { "reveil", "2" }, new[] { "tasse", "0" } };
        if (mm != null)
        {
            installe = () =>
            {
                rangement++;
                if (rangement < objets.Length) mm.Montre(objets[rangement][0], int.Parse(objets[rangement][1])); else mm.Fete();
            };
            mm.Demande = s =>
            {
                journal.AppendLine(t.ToString("0.00", Inv) + "  DEMANDE " + s);
                int i = int.Parse(s.Substring(6));
                if (rangement < 0) { mm.Decouvre(i); vues[i] = true; if (vues[0] && vues[1] && vues[2]) suite = t + 3.6; }
                else if (i == int.Parse(objets[rangement][1])) { mm.Range(i); suite = t + 2.2; }
                else mm.Refuse(i);
            };
        }
        // « valise » : le banc joue l'activité « La valise du voyageur » (trois pays, trois bons habits sur six)
        var vm = m as ValiseMoteur; int ranges = 0;
        string[] meteos = { "neige", "soleil", "pluie" };
        string[][] valises = { new[] { "maillot", "echarpe", "lunettes", "moufles", "tongs", "manteau" }, new[] { "casquette", "echarpe", "lunettes", "moufles", "teeshirt", "manteau" }, new[] { "parapluie", "maillot", "bottes", "lunettes", "impermeable", "chapeau" } };
        bool[][] bonsHabits = { new[] { false, true, false, true, false, true }, new[] { true, false, true, false, true, false }, new[] { true, false, true, false, true, false } };
        if (vm != null)
        {
            installe = () => { ranges = 0; vm.Prepare(meteos[tour % 3], valises[tour % 3], bonsHabits[tour % 3]); };
            installe();
            vm.Demande = s =>
            {
                journal.AppendLine(t.ToString("0.00", Inv) + "  DEMANDE " + s);
                if (s == "range") { if (++ranges >= 3) { vm.Ferme(); suite = t + 3.6; } return; }
                int i = int.Parse(s.Substring(6));
                if (bonsHabits[tour % 3][i]) vm.Range(i); else vm.Refuse(i);
            };
        }
        // « lit » : le banc joue l'activité « Sur, sous, devant, derrière » (on découvre, où est-il ?, mets-le…)
        var lm = m as LitMoteur; int acte = 1, vuesLit = 0; int[] ordreLit = { 0, 1, 2, 3 }; int cibleLit = -1;
        int[][] questions = { new[] { 2, 1, 3 }, new[] { 2, 2, 0 }, new[] { 3, 0, 1 }, new[] { 3, 1, 3 }, new[] { 3, 2, 2 } };      // l'acte, la peluche, la place
        int[][] ordres = { new[] { 2, 0, 3, 1 }, new[] { 1, 3, 0, 2 }, new[] { 3, 2, 1, 0 }, new[] { 0, 2, 1, 3 }, new[] { 2, 3, 0, 1 } };
        if (lm != null)
        {
            lm.Prepare(0, ordreLit, 4);
            installe = () =>
            {
                var q = questions[(tour - 1) % questions.Length]; acte = q[0]; cibleLit = q[2]; ordreLit = ordres[(tour - 1) % ordres.Length];
                lm.Prepare(q[1], ordreLit, 4); if (acte == 2) lm.Va(cibleLit); lm.Attendu = cibleLit;
            };
            lm.Demande = s =>
            {
                journal.AppendLine(t.ToString("0.00", Inv) + "  DEMANDE " + s);
                int k = int.Parse(s.Substring(6));
                if (acte == 1) { lm.Va(ordreLit[k]); lm.Fete(k); if (++vuesLit >= 4) { lm.Actif(false); suite = t + 3.2; } }
                else if (ordreLit[k] == cibleLit) { lm.Fete(k); lm.Actif(false); lm.Va(cibleLit); suite = t + 3.2; }
                else lm.Refuse(k);
            };
        }
        if (pm != null)
        {
            installe();
            pm.Demande = s =>
            {
                journal.AppendLine(t.ToString("0.00", Inv) + "  DEMANDE " + s);
                var q = tours[tour % tours.Length];
                if (s.StartsWith("choix:")) { int v = int.Parse(s.Substring(6)); if (v == q[0] - q[1]) pm.Verifie(v); else { pm.Refuse(v); pm.Indice(); } }
                else suite = t + 2.4;
            };
        }
        m.Son = (s, i) => journal.AppendLine(t.ToString("0.00", Inv) + "  SON   " + s + (i != 0 ? " " + i : ""));
        m.Bravo = () => journal.AppendLine(t.ToString("0.00", Inv) + "  BRAVO");
        m.Demarre(chamboule ? (scene.Length > 9 ? int.Parse(scene.Substring(9)) : 0) : corps ? (scene.Length > 5 ? int.Parse(scene.Substring(5)) : 0) : peche ? (scene.Length > 5 ? int.Parse(scene.Substring(5)) : 0) : Math.Max(0, Array.IndexOf(scenesDuJet, scene)));
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
            else if (corps || panier || fil || gouter || train || maison || valise || lit)
            {
                if (suite >= 0 && t >= suite) { suite = -1; tour++; installe(); pose = 0; }
                // le regard va sur la bonne zone, y reste une seconde, et la choisit ; une fois sur quatre il se trompe d'abord
                if (m.ProchaineCible(out double zx, out double zy))
                {
                    if (pose == 0) trompe = alea.NextDouble() < 0.3 && m.Zones.Count > 1;
                    if (trompe) { var autre = m.Zones.Find(q => Math.Abs(q.X + q.L / 2 - zx) + Math.Abs(q.Y + q.H / 2 - zy) > 1); if (autre != null) { zx = autre.X + autre.L / 2; zy = autre.Y + autre.H / 2; } }
                    gx += (zx - gx) * 0.14; gy += (zy - gy) * 0.14; pose += 1 / 30.0;
                    if (pose > 1.6) { var z = m.Zones.Find(q => gx >= q.X && gx <= q.X + q.L && gy >= q.Y && gy <= q.Y + q.H); if (z != null) { m.Appuie(z.Id); journal.AppendLine(t.ToString("0.00", Inv) + "  CHOIX " + z.Id); } pose = 0; }
                }
                else { pose = 0; gx += (800 + 260 * Math.Sin(t * 0.9) - gx) * 0.06; gy += (330 + 80 * Math.Sin(t * 1.3) - gy) * 0.06; }
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
                    case VecGenre.Image:
                        // une image de la banque d'images de l'application (dossier donné par EYZI_IMAGES)
                        corps.Append($"<image href=\"file://{Path.Combine(Environment.GetEnvironmentVariable("EYZI_IMAGES") ?? ".", o.Image + ".png")}\" x=\"-100\" y=\"-100\" width=\"200\" height=\"200\"/>");
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
