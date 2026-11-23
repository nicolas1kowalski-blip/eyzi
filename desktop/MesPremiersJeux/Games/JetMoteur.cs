using System;
using System.Collections.Generic;
using System.Linq;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le moteur du jeu « Le jet d'eau ».
    //
    //  Il ne connaît ni l'écran ni Windows : il tient une liste d'objets à dessiner
    //  (rangés par calques) et les fait bouger image par image. L'application le
    //  dessine avec WPF (WaterJetGame.cs) ; le banc d'essai (desktop/jetdeau/banc)
    //  le fait tourner sous Linux et en tire de vraies images. C'est le même code
    //  des deux côtés : ce qu'on voit sur les images est ce que fait le jeu.
    // ══════════════════════════════════════════════════════════════════════

    internal enum JetGenre { Dessin, Plein, Ligne, Texte }

    /// <summary>Un objet à dessiner : un dessin de JetArt, une forme pleine, une ligne ou un mot.</summary>
    internal sealed class JetObjet
    {
        public JetGenre Genre;
        public string Dessin;             // Dessin : la clé dans JetArt.Dessins
        public string[] Couleurs;         // Dessin : les couleurs « $nom » (nom, valeur, nom, valeur…)
        public double X, Y, R, SX = 1, SY = 1, Op = 1;
        public double E { get => SY; set { SX = value; SY = value; } }
        public string Couleur;            // Plein, Ligne, Texte
        public double[] Points;           // Plein, Ligne : x0, y0, x1, y1… dans le repère de l'objet
        public int Etat;                  // change quand Points ou Texte changent (le dessinateur refait alors la forme)
        public double Epaisseur, Tiret, Espace, Decalage;   // Ligne (tirets en unités d'épaisseur)
        public string Decoupe;            // Plein : un dessin dont la première forme découpe l'objet (ex. l'eau dans l'aquarium)
        public string Texte; public double Taille;          // Texte : centré sur l'origine, cerné de sombre
        public JetObjet En(double x, double y, double e = 1, double r = 0) { X = x; Y = y; E = e; R = r; return this; }
    }

    /// <summary>Un calque : ses objets, du fond vers le devant.</summary>
    internal sealed class JetCalque
    {
        public readonly List<JetObjet> Objets = new List<JetObjet>();
        public int Etat;                  // change quand la liste change
        public JetObjet Ajoute(JetObjet o) { Objets.Add(o); Etat++; return o; }
        public JetObjet Avant(JetObjet repere, JetObjet o) { int i = Objets.IndexOf(repere); if (i < 0) Objets.Add(o); else Objets.Insert(i, o); Etat++; return o; }
        public JetObjet Apres(JetObjet repere, JetObjet o) { int i = Objets.IndexOf(repere); if (i < 0) Objets.Add(o); else Objets.Insert(i + 1, o); Etat++; return o; }
        public void Retire(JetObjet o) { if (Objets.Remove(o)) Etat++; }
        public void Vide() { if (Objets.Count > 0) { Objets.Clear(); Etat++; } }
    }

    internal sealed partial class JetMoteur
    {
        public const double W = 1600, H = 900;
        private const double PivotY = 1076, LongueurLance = 404, Raccourci = 0.62;      // la lance est vue de derrière, dans sa longueur : elle paraît plus courte

        // Les calques, du fond vers le devant.
        public readonly JetCalque Ciel = new JetCalque(), Decor = new JetCalque(), Eau = new JetCalque(), Avant = new JetCalque(), Effets = new JetCalque(), Rideau = new JetCalque();
        public JetCalque[] Calques => new[] { Ciel, Decor, Eau, Avant, Effets, Rideau };

        /// <summary>Le fond de la page : couleur du haut du ciel, du bas du ciel, et du sol sous l'image.</summary>
        public string CielHaut = "#7CC8FF", CielBas = "#E4F6FF", Sol = "#63B24C";
        /// <summary>La hauteur (dans la scène) où le ciel laisse la place au sol.</summary>
        public const double Horizon = 506;

        // Ce que le moteur demande à l'application.
        public Action<string> Dit = s => { };             // faire parler la voix
        public Action<string, int> Son = (s, i) => { };   // « klaxon », « pschhh », « plouf », « pop », « note » (+ numéro)
        public Action Bravo = () => { };                  // la fête de fin de scène (confettis, étoile)
        public Func<bool> VoixOccupee = () => false;

        /// <summary>La force du jet, de 0 à 1 (pour le volume du bruit de l'eau).</summary>
        public double Pression { get; private set; }
        public int Reussites { get; private set; }
        public int Numero { get; private set; } = -1;
        public int NombreDeScenes => _scenes.Count;

        private readonly Random _alea;
        private double _temps, _vu = -10, _sansProgres, _indice, _depuis;
        private double _rx = W / 2, _ry = 420, _cx = W / 2, _cy = 420;      // le regard, et le point visé (qui le suit en douceur)
        private readonly JetObjet _lance, _arceauBord, _arceau, _arceauReflet, _gantPoignee, _boucheEau, _jetBord, _jetCorps, _jetCoeur, _jetReflet, _jetFil, _jetFil2, _gerbeA, _gerbeB, _brume, _soleil, _rayons, _lueur, _vague;
        private double _angle, _coup, _egoutte, _prochaineOnde, _prochaineGoutte;
        private bool _coulait;
        private readonly JetObjet[] _nuages;
        private readonly Bac _gouttes, _gadoue, _bulles, _mousse, _vapeur, _fumee, _braises, _ondes, _etoiles, _coeurs;
        private readonly Bac[] _bacs;
        private readonly List<Scene> _scenes = new List<Scene>();
        private Scene _scene;
        private enum Etat { Entree, Joue, Fete, Sortie }
        private Etat _etat;
        private bool _touche;
        private sealed class Mot { public JetObjet O; public double Age, X, Y; }
        private readonly List<Mot> _mots = new List<Mot>();

        public JetMoteur(int graine = 0)
        {
            _alea = graine == 0 ? new Random() : new Random(graine);

            _lueur = Pose(Ciel, "lueur", 1380, 150);
            _rayons = Pose(Ciel, "rayons", 1380, 150);
            _soleil = Pose(Ciel, "soleil", 1380, 150);
            _nuages = new[] { Pose(Ciel, "nuage", 300, 150), Pose(Ciel, "nuage", 960, 96, 0.7), Pose(Ciel, "nuage", 1560, 250, 0.55) };

            // Ce qui vole dans la scène : la boue, la mousse, les bulles, la fumée…
            _gadoue = new Bac(this, Eau, "gadoue", 24);
            _mousse = new Bac(this, Eau, "mousse", 26);
            _bulles = new Bac(this, Eau, "bulle", 26);
            _fumee = new Bac(this, Eau, "fumee", 20);
            _vapeur = new Bac(this, Eau, "vapeur", 28);
            _braises = new Bac(this, Eau, "braise", 24);
            _ondes = new Bac(this, Eau, "onde", 10);

            // Au premier plan : la lance, l'arceau de sa vanne et le gant qui le tire, l'eau dans sa bouche ;
            // puis le jet (il sort de la lance, donc il passe devant elle), la gerbe et les gouttes.
            _lance = Pose(Avant, "lance", W / 2, PivotY);
            _arceauBord = Avant.Ajoute(new JetObjet { Genre = JetGenre.Ligne, Couleur = "#2A1C4E", Epaisseur = 24 });
            _arceau = Avant.Ajoute(new JetObjet { Genre = JetGenre.Ligne, Couleur = "#B9C2D6", Epaisseur = 15 });
            _arceauReflet = Avant.Ajoute(new JetObjet { Genre = JetGenre.Ligne, Couleur = "#FFFFFF", Epaisseur = 5, Op = 0.8 });
            _gantPoignee = Pose(Avant, "gant_poignee", 0, 0);
            _boucheEau = Pose(Avant, "bouche_eau", 0, 0);
            _boucheEau.Op = 0;
            _jetBord = Avant.Ajoute(new JetObjet { Genre = JetGenre.Plein, Couleur = "#1E78D6", Op = 0 });
            _jetCorps = Avant.Ajoute(new JetObjet { Genre = JetGenre.Plein, Couleur = "#48B0F8", Op = 0 });
            _jetCoeur = Avant.Ajoute(new JetObjet { Genre = JetGenre.Plein, Couleur = "#9ADCFF", Op = 0 });
            _jetReflet = Avant.Ajoute(new JetObjet { Genre = JetGenre.Ligne, Couleur = "#FFFFFF", Epaisseur = 5, Op = 0 });
            _jetFil2 = Avant.Ajoute(new JetObjet { Genre = JetGenre.Ligne, Couleur = "#CDEEFF", Epaisseur = 4, Tiret = 2, Espace = 7, Op = 0 });
            _jetFil = Avant.Ajoute(new JetObjet { Genre = JetGenre.Ligne, Couleur = "#FFFFFF", Epaisseur = 5, Tiret = 3, Espace = 5, Op = 0 });
            _brume = Pose(Avant, "brume", 0, 0); _brume.Op = 0;
            _gerbeA = Pose(Avant, "gerbe_a", 0, 0); _gerbeA.Op = 0;
            _gerbeB = Pose(Avant, "gerbe_b", 0, 0); _gerbeB.Op = 0;
            _gouttes = new Bac(this, Avant, "goutte", 64);

            _etoiles = new Bac(this, Effets, "etoile", 44);
            _coeurs = new Bac(this, Effets, "coeur", 16);
            _vague = Pose(Rideau, "vague", -100, H + 40);
            _bacs = new[] { _gouttes, _gadoue, _bulles, _mousse, _vapeur, _fumee, _braises, _ondes, _etoiles, _coeurs };

            _scenes.Add(new SceneVoiture(this));
            _scenes.Add(new SceneFeu(this));
            _scenes.Add(new SceneRemplir(this));
            _scenes.Add(new SceneJardin(this));
            _scenes.Add(new SceneRoue(this));
        }

        /// <summary>Ouvre la première scène (à appeler une fois Dit, Son et Bravo branchés).</summary>
        public void Demarre(int scene = 0) { if (_scene == null) { Ouvre(scene); _etat = Etat.Entree; _depuis = 0; } }

        /// <summary>Ce qu'il reste à arroser, s'il en reste (pour le coup de pouce, et pour le regard automatique du banc d'essai).</summary>
        public bool ProchaineCible(out double x, out double y) { x = y = 0; return _scene != null && _etat == Etat.Joue && _scene.Reste(out x, out y); }

        // ------------------------------------------------------------------
        //  Ce que l'application dit au moteur
        // ------------------------------------------------------------------
        /// <summary>L'enfant regarde ce point de la scène (ou y pose le pointeur).</summary>
        public void Regard(double x, double y) { _rx = x; _ry = y; _vu = _temps; }

        // ------------------------------------------------------------------
        //  Petits outils
        // ------------------------------------------------------------------
        private JetObjet Pose(JetCalque calque, string dessin, double x, double y, double e = 1, params string[] couleurs)
            => calque.Ajoute(new JetObjet { Genre = JetGenre.Dessin, Dessin = dessin, Couleurs = couleurs != null && couleurs.Length > 0 ? couleurs : null }.En(x, y, e));
        private static JetObjet Dessin(string dessin, params string[] couleurs)
            => new JetObjet { Genre = JetGenre.Dessin, Dessin = dessin, Couleurs = couleurs != null && couleurs.Length > 0 ? couleurs : null };

        private double Hasard() => _alea.NextDouble();
        private double Entre(double a, double b) => a + (b - a) * _alea.NextDouble();
        private static double Borne(double v, double a = 0, double b = 1) => Math.Max(a, Math.Min(b, v));
        private static double Mel(double a, double b, double u) => a + (b - a) * u;
        private static double Doux(double x) { x = Borne(x); return x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2; }
        private static double Sortant(double x) { x = Borne(x); return 1 - Math.Pow(1 - x, 3); }
        private static double Rebond(double x) => x <= 0 ? 0 : x >= 1 ? 1 : Math.Pow(2, -9 * x) * Math.Sin((x * 10 - 0.75) * (2 * Math.PI / 3)) + 1;
        private static double Distance(double x1, double y1, double x2, double y2) => Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));

        private void Progres() { _sansProgres = 0; _indice = 0; }
        private void Reussite() { Reussites++; }

        /// <summary>Un éclat d'étoiles.</summary>
        private void Eclat(double x, double y, int n = 7, double force = 260)
        {
            for (int i = 0; i < n; i++)
            {
                double a = Math.PI * 2 * i / n + Hasard();
                _etoiles.Lance(x, y, Math.Cos(a) * force * Entre(0.6, 1), Math.Sin(a) * force * Entre(0.6, 1) - 60, 420, Entre(0.5, 0.8), Entre(0.7, 1.3), 0, Entre(-200, 200));
            }
        }

        /// <summary>Un mot de bande dessinée (« Tut tut ! », « Pschhh ! ») qui saute puis s'envole.</summary>
        private void Ecris(string texte, double x, double y, string couleur, double taille = 64)
        {
            double demi = texte.Length * taille * 0.32;
            var o = Effets.Ajoute(new JetObjet { Genre = JetGenre.Texte, Texte = texte, Taille = taille, Couleur = couleur, Op = 0 });
            _mots.Add(new Mot { O = o, X = Borne(x, 40 + demi, W - 40 - demi), Y = Math.Max(80, y) });
        }

        // ------------------------------------------------------------------
        //  Les petites choses qui volent : gouttes, bulles, vapeur, étoiles…
        // ------------------------------------------------------------------
        private sealed class Grain { public JetObjet O; public double X, Y, VX, VY, G, Vie, Age, Taille, Gonfle, Tourne; public bool Vivant; }

        private sealed class Bac
        {
            private readonly List<Grain> _grains = new List<Grain>();
            public Bac(JetMoteur m, JetCalque calque, string dessin, int nombre)
            {
                for (int i = 0; i < nombre; i++) { var o = m.Pose(calque, dessin, 0, 0); o.Op = 0; _grains.Add(new Grain { O = o }); }
            }
            public void Lance(double x, double y, double vx, double vy, double pesanteur, double vie, double taille = 1, double gonfle = 0, double tourne = 0)
            {
                foreach (var g in _grains)
                {
                    if (g.Vivant) continue;
                    g.X = x; g.Y = y; g.VX = vx; g.VY = vy; g.G = pesanteur; g.Vie = vie; g.Age = 0; g.Taille = taille; g.Gonfle = gonfle; g.Tourne = tourne; g.Vivant = true;
                    return;
                }
            }
            public void Avance(double dt)
            {
                foreach (var g in _grains)
                {
                    if (!g.Vivant) continue;
                    g.Age += dt;
                    if (g.Age >= g.Vie) { g.Vivant = false; g.O.Op = 0; continue; }
                    g.VY += g.G * dt; g.X += g.VX * dt; g.Y += g.VY * dt;
                    double u = g.Age / g.Vie;
                    g.O.X = g.X; g.O.Y = g.Y;
                    g.O.E = g.Taille * (1 + g.Gonfle * u) * Math.Min(1, g.Age * 14);
                    g.O.R += g.Tourne * dt;
                    g.O.Op = Math.Min(1, (1 - u) * 3);
                }
            }
            public void Vide() { foreach (var g in _grains) { g.Vivant = false; g.O.Op = 0; } }
        }

        /// <summary>Plusieurs objets qui bougent ensemble (la voiture, ses yeux, sa bouche, sa boue…).</summary>
        private sealed class Membre { public JetObjet O; public double X, Y, SX = 1, SY = 1, R; public double E { set { SX = value; SY = value; } } }
        private sealed class Groupe
        {
            public double X, Y, R, SX = 1, SY = 1;
            public readonly List<Membre> Membres = new List<Membre>();
            public Membre Ajoute(JetObjet o, double x = 0, double y = 0, double e = 1, double r = 0) { var m = new Membre { O = o, X = x, Y = y, SX = e, SY = e, R = r }; Membres.Add(m); return m; }
            public void Place()
            {
                double a = R * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
                foreach (var m in Membres)
                {
                    double x = m.X * SX, y = m.Y * SY;
                    m.O.X = X + x * c - y * s; m.O.Y = Y + x * s + y * c;
                    m.O.R = R + m.R; m.O.SX = SX * m.SX; m.O.SY = SY * m.SY;
                }
            }
            /// <summary>Où se trouve, dans la scène, un point donné dans le repère du groupe.</summary>
            public void Monde(double x, double y, out double mx, out double my)
            {
                double a = R * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
                x *= SX; y *= SY; mx = X + x * c - y * s; my = Y + x * s + y * c;
            }
        }

        // ------------------------------------------------------------------
        //  D'une scène à l'autre
        // ------------------------------------------------------------------
        private void Ouvre(int numero)
        {
            Numero = ((numero % _scenes.Count) + _scenes.Count) % _scenes.Count;
            _scene = _scenes[Numero];
            Decor.Vide();
            foreach (var b in _bacs) b.Vide();
            var objets = new Dictionary<string, JetObjet>();
            foreach (var p in JetArt.Scenes[_scene.Cle])
            {
                var o = Decor.Ajoute(new JetObjet { Genre = JetGenre.Dessin, Dessin = p.Dessin, Couleurs = _scene.Couleurs(p.Nom) ?? p.C }.En(p.X, p.Y, p.E, p.R));
                objets[p.Nom] = o;
            }
            var ciel = JetArt.Ciels[_scene.Cle];
            CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            bool jour = ciel[3] == "jour";
            foreach (var o in Ciel.Objets) o.Op = jour ? 1 : 0;
            _scene.Prepare(objets);
            _sansProgres = 0; _indice = 0;
            Dit(_scene.Consigne);
        }

        // ------------------------------------------------------------------
        //  Une image du jeu
        // ------------------------------------------------------------------
        public void Avance(double dt)
        {
            if (dt <= 0 || _scene == null) return;
            dt = Math.Min(dt, 0.1);
            _temps += dt;

            // --- Le regard : l'eau coule tant qu'il est sur la scène ---
            bool regarde = _temps - _vu < 0.4 && _rx > -80 && _rx < W + 80 && _ry > -80 && _ry < H + 80;
            Pression = Borne(Pression + (regarde ? dt * 4.5 : -dt * 5));
            double suit = Math.Min(1, dt * 12);
            _cx += (Borne(_rx, 30, W - 30) - _cx) * suit;
            _cy += (Borne(_ry, 60, 800) - _cy) * suit;

            // --- La lance : au premier plan, elle glisse et pivote pour viser un peu au-dessus du point regardé (l'eau retombe dessus) ---
            double loin = Borne((_cy - 250) / 520);            // 0 : loin (haut de l'image) ; 1 : tout près
            double taille = Mel(1.22, 1.4, loin), longueur = LongueurLance * Raccourci * taille;
            double bx = W / 2 + (_cx - W / 2) * 0.3;
            // on baisse les mains pour viser près : le bout de la lance reste toujours en dessous du point regardé
            double pivot = Borne(_cy + 80, 640, 850) + longueur;
            double leve = Mel(70, 24, loin);
            double vise = Borne(Math.Atan2(_cx - bx, pivot - (_cy - leve)), -1.02, 1.02);
            _angle += (vise - _angle) * Math.Min(1, dt * 16);          // elle a du poids : elle suit avec un rien de retard
            bool coule = Pression > 0.03;
            if (coule && !_coulait) _coup = 1;                          // le coup de recul quand l'eau s'ouvre
            if (!coule && _coulait) { _egoutte = 0.9; _prochaineGoutte = 0.1; }
            _coulait = coule;
            _coup *= Math.Exp(-dt * 9);
            double vibre = Pression * (2.6 * Math.Sin(_temps * 43) + 1.8 * Math.Sin(_temps * 61));
            _lance.X = bx + vibre * 0.5; _lance.Y = pivot + 6 * Pression + vibre + 16 * _coup; _lance.R = _angle * 180 / Math.PI; _lance.SX = taille; _lance.SY = taille * Raccourci;
            double ux = Math.Sin(_angle), uy = -Math.Cos(_angle);
            double tx = _lance.X + ux * longueur, ty = _lance.Y + uy * longueur;

            // l'arceau de la vanne recule quand l'eau s'ouvre, et le gant droit avec lui
            double recule = Mel(-92, 66, Doux(Pression));
            var arc = new double[26];
            for (int i = 0; i <= 12; i++) { double th = Math.PI * i / 12; arc[i * 2] = -76 * Math.Cos(th); arc[i * 2 + 1] = -216 + recule * Math.Sin(th); }
            foreach (var a in new[] { _arceauBord, _arceau, _arceauReflet })
            {
                a.X = _lance.X; a.Y = _lance.Y; a.R = _lance.R; a.SX = _lance.SX; a.SY = _lance.SY;
                if (a == _arceauReflet) { var r = (double[])arc.Clone(); for (int i = 1; i < r.Length; i += 2) r[i] -= 3; a.Points = r; } else a.Points = arc;
                a.Etat++;
            }
            DansLaLance(0, -216 + recule, out double gx, out double gy);
            _gantPoignee.X = gx; _gantPoignee.Y = gy; _gantPoignee.R = _lance.R; _gantPoignee.SX = taille; _gantPoignee.SY = taille * 0.82;
            DansLaLance(0, -LongueurLance, out double mx, out double my);
            _boucheEau.X = mx; _boucheEau.Y = my; _boucheEau.R = _lance.R; _boucheEau.SX = taille * (1 + 0.06 * Math.Sin(_temps * 37)); _boucheEau.SY = taille * Raccourci; _boucheEau.Op = Borne(Pression * 3);

            // --- Le jet : il sort dans l'axe de la lance, large, puis retombe en s'affinant sur le point regardé ---
            double portee = Distance(tx, ty, _cx, _cy);
            bool trace = coule && portee > 50 && (_cx - tx) * ux + (_cy - ty) * uy > 20;
            if (trace) TraceJet(tx, ty, ux, uy, portee, loin, taille, dt);
            _jetBord.Op = _jetCorps.Op = trace ? 0.96 : 0; _jetCoeur.Op = trace ? 0.9 : 0; _jetReflet.Op = trace ? 0.75 : 0; _jetFil.Op = trace ? 0.85 : 0; _jetFil2.Op = trace ? 0.7 : 0;
            _touche = trace && Pression > 0.9;
            double k = Mel(0.55, 1.15, loin);
            if (_touche)
            {
                // là où l'eau arrive : une brume, une couronne d'eau qui palpite, des gouttes qui giclent et qui coulent, des ronds qui s'élargissent
                for (int i = 0; i < 2; i++)
                    _gouttes.Lance(_cx + Entre(-10, 10) * k, _cy, Entre(-240, 240) * k, Entre(-400, -120) * k, 1000, Entre(0.35, 0.65), Entre(0.5, 1.0) * k);
                if (Hasard() < 0.5) _gouttes.Lance(_cx + Entre(-34, 34) * k, _cy + Entre(0, 24) * k, Entre(-20, 20), Entre(40, 130), 320, Entre(0.5, 0.8), Entre(0.5, 0.8) * k);
                _prochaineOnde -= dt;
                if (_prochaineOnde <= 0) { _prochaineOnde = 0.17; _ondes.Lance(_cx, _cy + 6 * k, 0, 0, 0, 0.5, 0.45 * k, 2.2); }
                bool pair = Math.Floor(_temps * 13) % 2 == 0;
                var g = pair ? _gerbeA : _gerbeB; (pair ? _gerbeB : _gerbeA).Op = 0;
                g.X = _cx; g.Y = _cy; g.E = k * (0.78 + 0.26 * Math.Abs(Math.Sin(_temps * 29))); g.R = 14 * Math.Sin(_temps * 17); g.Op = 0.96;
                _brume.X = _cx; _brume.Y = _cy; _brume.E = k * (1 + 0.1 * Math.Sin(_temps * 9)); _brume.Op = 0.85;
            }
            else { _gerbeA.Op = 0; _gerbeB.Op = 0; _brume.Op = Math.Max(0, _brume.Op - dt * 4); }
            // la lance s'égoutte quand l'eau s'arrête
            if (_egoutte > 0)
            {
                _egoutte -= dt; _prochaineGoutte -= dt;
                if (_prochaineGoutte <= 0) { _prochaineGoutte = 0.26; _gouttes.Lance(tx + Entre(-20, 20), ty + 6, Entre(-20, 20), 30, 800, 0.7, Entre(0.8, 1.1)); }
            }

            // --- Le ciel ---
            _rayons.R += dt * 14; _lueur.E = 1 + 0.05 * Math.Sin(_temps * 1.7);
            for (int i = 0; i < _nuages.Length; i++) { var n = _nuages[i]; n.X += dt * (7 + i * 3); if (n.X > W + 200) n.X = -200; }

            // --- La scène ---
            _depuis += dt;
            _scene.T += dt;
            switch (_etat)
            {
                case Etat.Entree:
                    // le rideau d'eau retombe et découvre la scène
                    _vague.Y = Mel(-140, H + 60, Doux(_depuis / 0.7)); _vague.X = -100 - 60 * Math.Sin(_temps * 5);
                    if (_depuis >= 0.7) { _etat = Etat.Joue; _depuis = 0; }
                    break;
                case Etat.Joue:
                    if (_touche) _scene.Arrose(_cx, _cy, dt);
                    if (_scene.Finie)
                    {
                        _etat = Etat.Fete; _depuis = 0; _indice = 0;
                        Reussites++;
                        Dit(_scene.Felicite);
                        Bravo();
                    }
                    else
                    {
                        // Le coup de pouce : rien n'avance depuis un moment → la consigne est redite, ce qui reste à faire remue.
                        _sansProgres += dt;
                        if (_indice > 0) _indice -= dt;
                        if (_sansProgres > 12 && !VoixOccupee()) { _sansProgres = 0; _indice = 3; Dit(_scene.Consigne); }
                    }
                    break;
                case Etat.Fete:
                    _scene.Fete(_depuis, dt);
                    if (_depuis >= _scene.DureeFete) { _etat = Etat.Sortie; _depuis = 0; }
                    break;
                case Etat.Sortie:
                    // le rideau d'eau monte et recouvre tout, puis la scène suivante s'ouvre
                    _scene.Fete(_scene.DureeFete + _depuis, dt);
                    _vague.Y = Mel(H + 60, -140, Doux(_depuis / 0.7)); _vague.X = -100 - 60 * Math.Sin(_temps * 5);
                    if (_depuis >= 0.75) { Ouvre(Numero + 1); _etat = Etat.Entree; _depuis = 0; }
                    break;
            }
            _scene.Anime(dt);
            foreach (var b in _bacs) b.Avance(dt);

            // --- Les mots de bande dessinée ---
            for (int i = _mots.Count - 1; i >= 0; i--)
            {
                var m = _mots[i];
                m.Age += dt;
                if (m.Age > 1.4) { Effets.Retire(m.O); _mots.RemoveAt(i); continue; }
                m.O.E = Rebond(m.Age / 0.45);
                m.O.X = m.X; m.O.Y = m.Y - 50 * Sortant(m.Age / 1.4);
                m.O.R = -6 + 4 * Math.Sin(m.Age * 9);
                m.O.Op = Borne((1.4 - m.Age) / 0.35);
            }
        }

        /// <summary>Où se trouve, dans la scène, un point donné dans le repère de la lance.</summary>
        private void DansLaLance(double x, double y, out double mx, out double my)
        {
            double c = Math.Cos(_angle), s = Math.Sin(_angle);
            x *= _lance.SX; y *= _lance.SY;
            mx = _lance.X + x * c - y * s; my = _lance.Y + x * s + y * c;
        }

        private const int Pas = 24;

        private void TraceJet(double tx, double ty, double ux, double uy, double portee, double loin, double taille, double dt)
        {
            // Une courbe qui part dans l'axe de la lance et retombe sur le point visé ; on n'en trace que le début tant que la pression monte.
            double sx = tx + ux * portee * 0.5, sy = ty + uy * portee * 0.5;
            double p = Pression, large0 = 36 * taille, large1 = Mel(7, 15, loin);
            int n = Pas + 1;
            var bord = new double[n * 4]; var corps = new double[n * 4]; var coeur = new double[n * 4];
            var reflet = new double[n * 2]; var fil = new double[n * 2]; var fil2 = new double[n * 2];
            int ou = _alea.Next(3, Pas - 2);      // d'où part la gouttelette de cette image
            for (int i = 0; i <= Pas; i++)
            {
                double u = p * i / Pas, v = 1 - u;
                double x = v * v * tx + 2 * v * u * sx + u * u * _cx, y = v * v * ty + 2 * v * u * sy + u * u * _cy;
                double dx = 2 * v * (sx - tx) + 2 * u * (_cx - sx), dy = 2 * v * (sy - ty) + 2 * u * (_cy - sy), d = Math.Max(1e-6, Math.Sqrt(dx * dx + dy * dy));
                double nx = -dy / d, ny = dx / d;
                // l'eau ondule un peu, et se défait en arrivant
                double onde = 1 + 0.05 * Math.Sin(_temps * 26 - i * 0.9) + (u > 0.6 ? 0.2 * (u - 0.6) / 0.4 * Math.Sin(_temps * 34 - i * 1.7) : 0);
                double l = Mel(large0, large1, Math.Pow(u, 0.75)) * onde;
                int a = i * 2, b = (2 * Pas + 1 - i) * 2;
                bord[a] = x + nx * (l + 4.5); bord[a + 1] = y + ny * (l + 4.5); bord[b] = x - nx * (l + 4.5); bord[b + 1] = y - ny * (l + 4.5);
                corps[a] = x + nx * l; corps[a + 1] = y + ny * l; corps[b] = x - nx * l; corps[b + 1] = y - ny * l;
                // le cœur clair et le reflet sont du côté gauche : la lumière vient de là
                double cx = x - nx * l * 0.2, cy = y - ny * l * 0.2;
                coeur[a] = cx + nx * l * 0.5; coeur[a + 1] = cy + ny * l * 0.5; coeur[b] = cx - nx * l * 0.5; coeur[b + 1] = cy - ny * l * 0.5;
                reflet[a] = x - nx * l * 0.66; reflet[a + 1] = y - ny * l * 0.66;
                fil[a] = x - nx * l * 0.1; fil[a + 1] = y - ny * l * 0.1;
                fil2[a] = x + nx * l * 0.5; fil2[a + 1] = y + ny * l * 0.5;
                // de fines gouttelettes se détachent du jet et retombent
                if (i == ou && p > 0.6)
                {
                    double cote = Hasard() < 0.5 ? -1 : 1, e = Mel(0.4, 0.75, loin);
                    _gouttes.Lance(x + nx * l * cote, y + ny * l * cote, dx / d * 140 + nx * cote * Entre(50, 130), dy / d * 140 + ny * cote * Entre(50, 130) - 30, 700, Entre(0.3, 0.5), Entre(0.5, 0.9) * e);
                }
            }
            foreach (var (o, pts) in new[] { (_jetBord, bord), (_jetCorps, corps), (_jetCoeur, coeur), (_jetReflet, reflet), (_jetFil, fil), (_jetFil2, fil2) }) { o.Points = pts; o.Etat++; }
            _jetFil.Decalage -= dt * 24; _jetFil2.Decalage -= dt * 15;
            // pendant que le jet s'allonge, son bout fait déjà une petite gerbe
            if (p < 0.9 && p > 0.15)
            {
                _gerbeA.X = fil[Pas * 2]; _gerbeA.Y = fil[Pas * 2 + 1]; _gerbeA.E = 0.38 * Mel(0.6, 1.1, loin); _gerbeA.R = _temps * 300; _gerbeA.Op = 0.9;
            }
        }

        // ------------------------------------------------------------------
        //  Les scènes
        // ------------------------------------------------------------------
        private abstract class Scene
        {
            protected readonly JetMoteur J;
            protected Dictionary<string, JetObjet> O;
            /// <summary>Secondes depuis l'ouverture de la scène.</summary>
            public double T;
            protected Scene(JetMoteur jeu) { J = jeu; }
            public abstract string Cle { get; }
            public abstract string Consigne { get; }
            public abstract string Felicite { get; }
            public abstract bool Finie { get; }
            public virtual double DureeFete => 3.6;
            public virtual string[] Couleurs(string nom) => null;
            public void Prepare(Dictionary<string, JetObjet> objets) { O = objets; T = 0; Construit(); }
            protected abstract void Construit();
            /// <summary>L'eau touche le point (x, y) pendant dt secondes.</summary>
            public abstract void Arrose(double x, double y, double dt);
            /// <summary>Un endroit qu'il reste à arroser (faux s'il n'y en a plus).</summary>
            public abstract bool Reste(out double x, out double y);
            /// <summary>À chaque image, quel que soit l'état.</summary>
            public virtual void Anime(double dt) { }
            /// <summary>La scène est réussie depuis u secondes.</summary>
            public virtual void Fete(double u, double dt) { }
            protected static bool Passe(double u, double dt, double t) => u >= t && u - dt < t;
            protected double Remue => J._indice > 0 ? Math.Sin(J._temps * 22) : 0;
        }

        // ══════════ 1. La voiture pleine de boue ══════════
        private sealed class SceneVoiture : Scene
        {
            private static readonly string[][] Teintes =
            {
                new[] { "#FF5A4E", "#C9302A" }, new[] { "#4FB8FF", "#2A7FD0" }, new[] { "#FFB84A", "#E0861C" },
                new[] { "#6EDC8C", "#3AA85C" }, new[] { "#B58BFF", "#7E50D6" }, new[] { "#FF8FC1", "#D9558F" },
            };
            private static int _tour = -1;
            private sealed class Tache { public Membre M; public double V = 1, E; }
            private readonly List<Tache> _taches = new List<Tache>();
            private readonly Groupe _g = new Groupe();
            private JetObjet _ombre;
            private Membre[] _pupilles, _paupieres, _bouches, _joues, _halos, _essuie;
            private readonly JetObjet[] _mouches = new JetObjet[3];
            private readonly double[] _envol = new double[3];
            private double _chatouille, _saut;
            private bool _fete;
            private double X0 = 800, Y0 = 700, E0 = 1;

            public SceneVoiture(JetMoteur j) : base(j) { }
            public override string Cle => "voiture";
            public override string Consigne => "La voiture est pleine de boue ! Arrose-la pour la laver !";
            public override string Felicite => "Bravo ! La voiture est toute propre !";
            public override bool Finie => _taches.All(t => t.V <= 0);
            public override double DureeFete => 4;
            private string[] Teinte => new[] { "corps", Teintes[(_tour + 1 + Teintes.Length) % Teintes.Length][0], "corps2", Teintes[(_tour + 1 + Teintes.Length) % Teintes.Length][1] };
            public override string[] Couleurs(string nom) => nom == "auto" || nom == "panneau" ? Teinte : null;

            protected override void Construit()
            {
                var teinte = Teinte;
                _tour++;
                _fete = false; _chatouille = 0; _saut = 0;
                _ombre = O["ombre"];
                var auto = O["auto"];
                _g.Membres.Clear();
                X0 = auto.X; Y0 = auto.Y; E0 = auto.E;
                _g.X = X0; _g.Y = Y0; _g.R = 0; _g.SX = _g.SY = E0;
                _g.Ajoute(auto);
                // le visage se glisse entre la carrosserie et la boue
                JetObjet Sur(string dessin, params string[] c) => J.Decor.Avant(O["boue0"], Dessin(dessin, c));
                _halos = new[] { _g.Ajoute(Sur("auto_halo"), -176, -150), _g.Ajoute(Sur("auto_halo"), 176, -150) };
                _pupilles = new[] { _g.Ajoute(Sur("auto_pupille"), -176, -150), _g.Ajoute(Sur("auto_pupille"), 176, -150) };
                _paupieres = new[] { _g.Ajoute(Sur("auto_paupiere", teinte), -176, -150), _g.Ajoute(Sur("auto_paupiere", teinte), 176, -150) };
                _bouches = new[] { "auto_bouche_triste", "auto_bouche_moyen", "auto_bouche_sourire", "auto_bouche_rire" }.Select(b => _g.Ajoute(Sur(b))).ToArray();
                _joues = new[] { _g.Ajoute(Sur("auto_joue"), -104, -168), _g.Ajoute(Sur("auto_joue"), 104, -168) };
                _essuie = new[] { _g.Ajoute(Sur("essuie"), -70, -244, 1, -64), _g.Ajoute(Sur("essuie"), 70, -244, 1, 64) };
                _taches.Clear();
                for (int i = 0; O.ContainsKey("boue" + i); i++)
                {
                    var o = O["boue" + i];
                    _taches.Add(new Tache { M = _g.Ajoute(o, (o.X - X0) / E0, (o.Y - Y0) / E0, o.E / E0, o.R), E = o.E / E0 });
                }
                for (int i = 0; i < 3; i++) { _mouches[i] = J.Pose(J.Decor, "mouche", X0, Y0 - 400); _envol[i] = -1; }
                foreach (var h in _halos) h.O.Op = 0;
                foreach (var jo in _joues) jo.O.Op = 0;
            }

            private double Propre => _taches.Count == 0 ? 1 : 1 - _taches.Sum(t => Math.Max(0, t.V)) / _taches.Count;

            public override void Arrose(double x, double y, double dt)
            {
                bool lave = false;
                foreach (var t in _taches)
                {
                    if (t.V <= 0) continue;
                    var o = t.M.O;
                    if (Distance(x, y, o.X, o.Y) > 70 * t.E * E0 * (0.55 + 0.45 * t.V) + 46) continue;
                    lave = true;
                    t.V -= dt * 1.05;
                    if (J.Hasard() < 0.7) J._gadoue.Lance(o.X + J.Entre(-34, 34), o.Y + J.Entre(-10, 30), J.Entre(-110, 110), J.Entre(20, 190), 900, J.Entre(0.4, 0.75), J.Entre(0.7, 1.5));
                    if (J.Hasard() < 0.5) J._mousse.Lance(o.X + J.Entre(-46, 46), o.Y + J.Entre(-36, 30), J.Entre(-14, 14), J.Entre(30, 80), 60, J.Entre(0.9, 1.5), J.Entre(0.6, 1.2), 0.5);
                    if (J.Hasard() < 0.3) J._bulles.Lance(o.X + J.Entre(-40, 40), o.Y + J.Entre(-30, 30), J.Entre(-50, 50), J.Entre(-130, -50), -60, J.Entre(0.7, 1.2), J.Entre(0.6, 1.6));
                    if (t.V <= 0)
                    {
                        o.Op = 0;
                        J.Eclat(o.X, o.Y, 6, 220);
                        J.Son("pop", 0);
                        J.Reussite();
                    }
                }
                // sur la carrosserie, l'eau mousse et chatouille la voiture
                if (Math.Abs(x - _g.X) < 320 && y > _g.Y - 410 && y < _g.Y - 30)
                {
                    _chatouille = 0.35;
                    if (!lave && J.Hasard() < 0.3) J._bulles.Lance(x + J.Entre(-24, 24), y, J.Entre(-50, 50), J.Entre(-130, -50), -60, J.Entre(0.5, 1), J.Entre(0.5, 1.2));
                }
                if (lave) J.Progres();
            }

            public override bool Reste(out double x, out double y)
            {
                var t = _taches.FirstOrDefault(k => k.V > 0);
                x = t?.M.O.X ?? 0; y = t?.M.O.Y ?? 0;
                return t != null;
            }

            public override void Anime(double dt)
            {
                double propre = Propre;
                if (_chatouille > 0) _chatouille -= dt;
                bool rit = _chatouille > 0 || _fete;

                // la voiture respire, frétille quand l'eau la chatouille, saute pendant la fête
                _g.X = X0; _g.Y = Y0 - _saut;
                _g.SY = E0 * (1 + 0.012 * Math.Sin(T * 3) + 0.04 * Borne(_saut / 60)); _g.SX = E0 * (1 - 0.006 * Math.Sin(T * 3) - 0.03 * Borne(_saut / 60));
                _g.R = (_chatouille > 0 ? 1.3 * Math.Sin(J._temps * 26) : 0) + (_fete ? 2.5 * Math.Sin(T * 9) : 0);
                _ombre.SX = E0 * (1 - 0.25 * Borne(_saut / 60)); _ombre.SY = _ombre.SX; _ombre.Op = 1 - 0.4 * Borne(_saut / 60);

                // les yeux suivent l'eau ; les paupières tombent quand elle est sale, se plissent quand elle rit, clignent de temps en temps
                bool cligne = (T % 3.4) > 3.25;
                double paupiere = cligne ? 1 : _fete ? 0.04 : _chatouille > 0 ? 0.36 : Mel(0.6, 0.06, propre);
                for (int i = 0; i < 2; i++)
                {
                    double ex = i == 0 ? -176 : 176;
                    _g.Monde(ex, -150, out double mx, out double my);
                    double dx = J._cx - mx, dy = J._cy - my, d = Math.Max(1, Math.Sqrt(dx * dx + dy * dy)), k = Math.Min(17, d / 14);
                    _pupilles[i].X = ex + dx / d * k; _pupilles[i].Y = -150 + dy / d * k + (rit ? 0 : 5 * (1 - propre));
                    _pupilles[i].E = rit ? 1.1 : 1;
                    _paupieres[i].SY = Math.Max(0.04, paupiere); _paupieres[i].O.Op = paupiere > 0.05 ? 1 : 0;
                    _joues[i].O.Op = Borne(_joues[i].O.Op + (rit ? dt * 5 : -dt * 3));
                }
                int bouche = _fete ? 3 : Math.Min(3, (propre < 0.34 ? 0 : propre < 0.67 ? 1 : 2) + (_chatouille > 0 ? 1 : 0));
                for (int i = 0; i < _bouches.Length; i++) _bouches[i].O.Op = i == bouche ? 1 : 0;

                foreach (var t in _taches)
                {
                    if (t.V <= 0) continue;
                    t.M.E = t.E * (0.5 + 0.5 * t.V);
                    t.M.R += 0;
                    t.M.O.Op = 1;
                }
                _g.Place();
                if (J._indice > 0) foreach (var t in _taches) if (t.V > 0) t.M.O.R += 14 * Remue;

                // les mouches tournent autour de la voiture sale, et s'en vont une à une quand elle devient propre
                for (int i = 0; i < 3; i++)
                {
                    var m = _mouches[i];
                    if (_envol[i] < 0 && propre >= (i + 1) / 3.5) _envol[i] = T;
                    double a = T * (2.1 + i * 0.5) + i * 2.1;
                    double x = X0 + (230 - i * 50) * Math.Cos(a), y = Y0 - 380 + i * 30 + 60 * Math.Sin(a * 2);
                    if (_envol[i] >= 0) { double v = T - _envol[i]; x += 500 * v * (i == 1 ? -1 : 1); y -= 700 * v * v; m.Op = Borne(1 - v / 0.9); }
                    m.X = x; m.Y = y; m.R = 20 * Math.Sin(J._temps * 40 + i); m.E = 1.1;
                }
                // tant qu'elle est bien sale, ça sent mauvais
                if (propre < 0.5 && J.Hasard() < dt * 1.6)
                    J._vapeur.Lance(X0 + J.Entre(-230, 230), Y0 - J.Entre(380, 420), J.Entre(-10, 10), J.Entre(-50, -30), 0, J.Entre(1.2, 1.8), J.Entre(0.4, 0.7), 1.2);
            }

            public override void Fete(double u, double dt)
            {
                _fete = true;
                if (Passe(u, dt, 0.1)) { J.Son("klaxon", 0); J.Ecris("Tut tut !", X0, Y0 - 520, "#FFC83D", 96); }
                if (Passe(u, dt, 2.0)) { J.Son("klaxon", 0); J.Ecris("Merci !", X0, Y0 - 520, "#FF8FC1", 84); }
                _saut = u < 3.2 ? 56 * Math.Abs(Math.Sin(u * 6.5)) : 0;
                foreach (var h in _halos) h.O.Op = u < 3.4 ? 0.55 + 0.45 * Math.Sin(u * 16) : 0;
                _essuie[0].R = -64 + 70 * (0.5 + 0.5 * Math.Sin(u * 9)); _essuie[1].R = 64 - 70 * (0.5 + 0.5 * Math.Sin(u * 9));
                if (J.Hasard() < dt * 12) J._etoiles.Lance(X0 + J.Entre(-330, 330), Y0 - J.Entre(40, 420), 0, -30, 0, 0.55, J.Entre(0.8, 1.8), 0, 160);
                if (J.Hasard() < dt * 4) J._coeurs.Lance(X0 + J.Entre(-260, 260), Y0 - 400, J.Entre(-30, 30), J.Entre(-120, -80), 0, J.Entre(1, 1.5), J.Entre(1, 1.8));
            }
        }
    }
}
