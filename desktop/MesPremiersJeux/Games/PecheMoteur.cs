using System;
using System.Collections.Generic;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le moteur du jeu « La pêche ».
    //
    //  On est assis au bout du ponton, la canne à la main : le bouchon va sur l'eau
    //  là où l'on regarde. Quand il reste près d'un poisson, celui-ci s'approche du
    //  leurre, le rond autour du bouchon se remplit… et il mord : la canne plie, le
    //  poisson saute hors de l'eau et retombe dans le seau. Cinq poissons : le seau
    //  est plein, c'est la fête, on les remet à l'eau et on recommence.
    //
    //  Comme les autres jeux « dessinés », le moteur ne connaît ni l'écran ni
    //  Windows (voir VecMoteur.cs) : le banc d'essai en tire de vraies images.
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class PecheMoteur : VecMoteur
    {
        private const double MainX = 1352, MainY = 852;          // la main qui tient la canne
        private const double TaillePoignee = 0.8, Prise = 262;   // la taille de la poignée, et la distance (dans son dessin) de son haut à la main
        private const double SeauX = 218, SeauY = 876;
        private const int Plein = 5;                             // les poissons qu'il faut pour remplir le seau
        private const string Consigne = "Regarde un poisson… et il mord ! Remplis le seau !";
        private static readonly double[] Vitesses = { 1, 1.6, 2.4 };       // les poissons nagent plus vite d'un niveau à l'autre
        private static readonly double[] Rayons = { 175, 150, 128 };       // … et il faut que le bouchon soit plus près d'eux

        // Les calques, du fond vers le devant : le ciel, le paysage et l'étang, les poissons sous l'eau,
        // le voile de l'eau et ce qui flotte, le ponton et la canne, ce qui vole, le rideau d'eau.
        public readonly VecCalque Ciel = new VecCalque(), Decor = new VecCalque(), Fond = new VecCalque(), Voile = new VecCalque(), Avant = new VecCalque(), Effets = new VecCalque(), Rideau = new VecCalque();
        private readonly VecCalque[] _calques;
        public override VecCalque[] Calques => _calques;
        public override Dictionary<string, VecForme[]> Dessins => PecheArt.Dessins;
        protected override VecCalque CalqueDesMots => Effets;
        public override double Horizon => 304;
        public override int Etapes => Plein;
        public override int Etape => _prises;

        /// <summary>Le niveau, de 0 à 2 : la vitesse des poissons (l'application le retient d'une fois sur l'autre).</summary>
        public int Niveau { get => _niveau; set => _niveau = Math.Max(0, Math.Min(2, value)); }
        private int _niveau;

        // ───────────── Les espèces ─────────────
        private sealed class Espece { public string Nom; public bool Raye, Dore; public string[] C; }
        private static Espece E(string nom, string corps, string clair, string fonce, string nageoire, string raie = null, bool dore = false)
            => new Espece { Nom = nom, Raye = raie != null, Dore = dore, C = new[] { "corps", corps, "clair", clair, "fonce", fonce, "nageoire", nageoire, "raie", raie ?? "#000000" } };
        private static readonly Espece[] Especes =
        {
            E("un poisson rouge", "#F0483E", "#FFA896", "#B5261F", "#FF7A5C"),
            E("un poisson orange", "#FF9A3D", "#FFD89A", "#D9701A", "#FFB85C"),
            E("un poisson bleu", "#48A0F8", "#B4E2FF", "#1E68C6", "#7FC4FF"),
            E("un poisson jaune", "#FFD23D", "#FFF6A8", "#E0A010", "#FFE066"),
            E("un poisson vert", "#5CC86A", "#C0F4B0", "#2E9E4A", "#8EE08A"),
            E("un poisson rose", "#FF7AB0", "#FFCCE2", "#D94A86", "#FFA0C8"),
            E("un poisson violet", "#9A6CF0", "#D8C8FF", "#6A3CC6", "#B896FF"),
            E("un poisson rayé", "#FFD23D", "#FFF6B0", "#E0A010", "#FFB81C", "#3A3454"),
            E("un poisson rayé", "#EAF4FF", "#FFFFFF", "#9AB4E0", "#48A0F8", "#1E68C6"),
            E("le poisson doré", "#FFC93D", "#FFFBD0", "#E08E12", "#FFE066", null, true),
        };

        private sealed class Poisson
        {
            public int Espece; public string Nom; public bool Dore, Parti;
            public readonly Groupe G = new Groupe();
            public Membre Queue, Nageoire, Pupille, Sourire, Rond, Ravi;
            public VecObjet Ombre;
            public readonly List<VecObjet> Objets = new List<VecObjet>();
            public double X, Y, Vx, Vy, ButX, ButY, Allure, Gabarit, Phase, Sens = 1, SensVu = 1, Interet, Change, Bulle, OeilX, OeilY;
        }
        private sealed class Vol { public Poisson P; public double T, Duree, X0, Y0, X1, Y1, Haut, E0, E1; public bool VersLeSeau; }
        private sealed class Tete { public VecObjet O; public int Espece; public double X, Y, Age; }
        private sealed class Feuille { public VecObjet O, Fleur; public double X, Y, E, Creux; }
        private sealed class Gerbe { public VecObjet O; public double Age = 9, X, Y, E; }

        private readonly List<Poisson> _poissons = new List<Poisson>();
        private readonly List<Vol> _vols = new List<Vol>();
        private readonly List<Tete> _tetes = new List<Tete>();
        private readonly List<Feuille> _feuilles = new List<Feuille>();
        private readonly List<int> _sac = new List<int>();          // les espèces à venir, mélangées
        private Poisson _candidat, _pris;

        private enum Etat { Entree, Peche, Ferre, Envol, Fete, Relache, Sortie }
        private Etat _etat;
        private enum Mode { Pend, Lance, Flotte, Plonge, Suit }
        private Mode _mode = Mode.Pend;
        private bool _ouvert;
        private int _prises, _manche, _relaches;
        private double _depuis, _sansPrise, _aide, _arrivee, _prochaineOnde, _tape;

        // le bouchon, la canne
        private double _bx = 800, _by = 560, _bvx, _lx0, _ly0, _lanceT, _plonge;
        private double _tx = 1060, _ty = 250, _charge, _tour, _px, _py;       // le bout de la canne, sa charge (0 à 1), la manivelle, là où pend le bouchon
        private readonly VecObjet _lueur, _rayons, _soleil, _chemin, _teinte, _vague, _leurre, _reflet, _rond, _progresBord, _progres, _bouchon, _seauDevant;
        private readonly VecObjet _canneBord, _canneCorps, _canneOmbre, _canneReflet, _poignee, _manivelle, _gantManivelle, _filBord, _fil;
        private readonly VecObjet[] _nuages, _oiseaux, _roseaux, _eclats, _noeuds, _anneaux, _genoux, _bottes;
        private readonly double[] _eclatPhase;
        private readonly Gerbe[] _gerbes;
        private int _gerbe;
        private readonly Bac _bulles, _ondes, _gouttes, _etoiles, _coeurs, _lucioles;
        private bool _soir, _dejaDit;
        private readonly Bac[] _bacs;

        // le canard, la grenouille, la libellule
        private readonly Groupe _canard = new Groupe();
        private readonly Membre _cAile, _cBec, _cBecOuvert;
        private double _cx = 760, _cy = 420, _cButX = 900, _cButY = 430, _cSens = 1, _cSensVu = 1, _coin, _cRepos, _cPres, _cOnde;
        private readonly VecObjet _grenouille, _grenouilleSaut, _libellule, _libelluleAiles;
        private int _gSur, _gVers;
        private double _gAttente = 5, _gT, _gPose = 9;
        private bool _gSaute;

        public PecheMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Ciel, Decor, Fond, Voile, Avant, Effets, Rideau };

            // --- Le ciel ---
            _lueur = Pose(Ciel, "lueur", 1380, 108, 0.8);
            _rayons = Pose(Ciel, "rayons", 1380, 108, 0.8);
            _soleil = Pose(Ciel, "soleil", 1380, 108, 0.8);
            _nuages = new[] { Pose(Ciel, "nuage", 250, 96, 0.78), Pose(Ciel, "nuage", 880, 62, 0.52), Pose(Ciel, "nuage", 1580, 170, 0.42) };
            _oiseaux = new[] { Pose(Ciel, "oiseau", 520, 150, 0.8), Pose(Ciel, "oiseau", 590, 176, 0.6), Pose(Ciel, "oiseau", 1100, 90, 0.5) };

            // --- Le paysage et l'étang ; les rives passent par-dessus l'eau ---
            VecPose rives = default(VecPose);
            foreach (var p in PecheArt.Scenes["etang"])
            {
                if (p.Nom == "rives") { rives = p; continue; }
                Decor.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = p.Dessin }.En(p.X, p.Y, p.E, p.R));
            }
            _chemin = Pose(Decor, "p_chemin", 1380, 300);

            // --- Sous l'eau : les bulles, le leurre, puis les poissons (ajoutés à chaque manche) ---
            _bulles = new Bac(this, Fond, "bulle", 22);
            _leurre = Pose(Fond, "leurre", 0, 0); _leurre.Op = 0;

            // --- Le voile de l'eau et ce qui flotte dessus ---
            Pose(Voile, "p_voile", 0, 0);
            _ondes = new Bac(this, Voile, "onde", 18);
            _eclats = new VecObjet[12]; _eclatPhase = new double[12];
            for (int i = 0; i < _eclats.Length; i++) { _eclats[i] = Pose(Voile, "eclat", 0, 0); _eclatPhase[i] = Entre(0, 6); PoseEclat(i); }
            Voile.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = rives.Dessin }.En(rives.X, rives.Y, rives.E, rives.R));
            double[,] roseaux = { { 318, 344, 0.5 }, { 226, 414, 0.74 }, { 128, 480, 0.95 }, { 34, 506, 1.0 }, { 1366, 334, 0.46 }, { 1462, 396, 0.7 }, { 1574, 444, 0.92 } };
            _roseaux = new VecObjet[roseaux.GetLength(0)];
            for (int i = 0; i < _roseaux.Length; i++) _roseaux[i] = Pose(Voile, "roseau", roseaux[i, 0], roseaux[i, 1], roseaux[i, 2]);
            // les nénuphars : les quatre premiers sont ceux de la grenouille, les deux autres portent une fleur
            double[,] feuilles = { { 540, 412, 0.6 }, { 1046, 456, 0.72 }, { 500, 596, 0.92 }, { 1250, 590, 1.0 }, { 436, 470, 0.7 }, { 1356, 492, 0.78 }, { 1150, 392, 0.5 } };
            for (int i = 0; i < feuilles.GetLength(0); i++)
            {
                var f = new Feuille { X = feuilles[i, 0], Y = feuilles[i, 1], E = feuilles[i, 2] };
                f.O = Pose(Voile, "nenuphar", f.X, f.Y, f.E); f.O.R = i * 67 % 40 - 20;
                if (i >= 4) f.Fleur = Pose(Voile, "nenuphar_fleur", f.X - 6 * f.E, f.Y - 4 * f.E, f.E);
                _feuilles.Add(f);
            }
            _grenouille = Pose(Voile, "grenouille", 0, 0);
            _grenouilleSaut = Pose(Voile, "grenouille_saut", 0, 0); _grenouilleSaut.Op = 0;
            // le canard
            _canard.Ajoute(Pose(Voile, "canard", 0, 0));
            _cBec = _canard.Ajoute(Pose(Voile, "canard_bec", 0, 0), 86, -62);
            _cBecOuvert = _canard.Ajoute(Pose(Voile, "canard_bec_ouvert", 0, 0), 86, -62); _cBecOuvert.O.Op = 0;
            _canard.Ajoute(Pose(Voile, "canard_pupille", 0, 0), 69, -76);
            _cAile = _canard.Ajoute(Pose(Voile, "canard_aile", 0, 0), 10, -22);
            // le bouchon, son reflet, le rond qui se remplit quand un poisson s'approche
            _reflet = Pose(Voile, "bouchon_reflet", 0, 0); _reflet.Op = 0;
            _rond = Pose(Voile, "rond", 0, 0); _rond.Op = 0;
            _progresBord = Voile.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#2A1C4E", Epaisseur = 17, Op = 0 });
            _progres = Voile.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#FFD23D", Epaisseur = 10, Op = 0 });
            _bouchon = Pose(Voile, "bouchon", 0, 0);
            _libellule = Pose(Voile, "libellule", 0, 0);
            _libelluleAiles = Pose(Voile, "libellule_ailes", 0, 0);

            // --- Au premier plan : le ponton, les genoux et les bottes, le seau, la canne et la ligne ---
            Pose(Avant, "ponton", 0, 0);
            _genoux = new[] { Pose(Avant, "genou", 706, 900, 0.9), Pose(Avant, "genou", 896, 900, 0.9) };
            _bottes = new[] { Pose(Avant, "botte", 706, 826, 0.9), Pose(Avant, "botte", 896, 826, 0.9) };
            Pose(Avant, "seau_fond", SeauX, SeauY);
            _seauDevant = Pose(Avant, "seau_devant", SeauX, SeauY);
            _filBord = Avant.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#2A1C4E", Epaisseur = 4.5, Op = 0.45 });
            _fil = Avant.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#FFFFFF", Epaisseur = 2.2 });
            _canneBord = Avant.Ajoute(new VecObjet { Genre = VecGenre.Plein, Couleur = "#2A1C4E" });
            _canneCorps = Avant.Ajoute(new VecObjet { Genre = VecGenre.Plein, Couleur = "#F2C14E" });
            _canneOmbre = Avant.Ajoute(new VecObjet { Genre = VecGenre.Plein, Couleur = "#C98A1E" });
            _canneReflet = Avant.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#FFF6C8", Epaisseur = 2.5, Op = 0.85 });
            _noeuds = new VecObjet[6];
            for (int i = 0; i < _noeuds.Length; i++) _noeuds[i] = Avant.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#8A5A0A", Epaisseur = 3.5 });
            _anneaux = new[] { Pose(Avant, "anneau", 0, 0), Pose(Avant, "anneau", 0, 0), Pose(Avant, "anneau", 0, 0) };
            _gantManivelle = Pose(Avant, "gant_manivelle", 0, 0);
            _poignee = Pose(Avant, "canne_poignee", 0, 0);
            _manivelle = Pose(Avant, "manivelle", 0, 0);

            // --- Ce qui vole ---
            _gerbes = new[] { new Gerbe { O = Pose(Effets, "gerbe_a", 0, 0) }, new Gerbe { O = Pose(Effets, "gerbe_b", 0, 0) }, new Gerbe { O = Pose(Effets, "gerbe_a", 0, 0) }, new Gerbe { O = Pose(Effets, "gerbe_b", 0, 0) } };
            foreach (var g in _gerbes) g.O.Op = 0;
            _gouttes = new Bac(this, Effets, "goutte", 70);
            _etoiles = new Bac(this, Effets, "etoile", 44);
            _coeurs = new Bac(this, Effets, "coeur", 14);
            _lucioles = new Bac(this, Effets, "luciole", 16);
            // la lumière du soir : un voile chaud sur toute l'image, sous le rideau d'eau
            _teinte = Rideau.Ajoute(new VecObjet { Genre = VecGenre.Plein, Couleur = "#FF6A3A", Points = new[] { -20.0, -20, W + 20, -20, W + 20, H + 20, -20, H + 20 }, Op = 0 });
            _vague = Pose(Rideau, "vague", -100, -140);
            _bacs = new[] { _bulles, _ondes, _gouttes, _etoiles, _coeurs, _lucioles };
        }

        /// <summary>Ouvre le jeu (à appeler une fois Dit, Son et Bravo branchés) ; scene : le niveau à imposer, pour le banc d'essai (0 : celui qui est réglé).</summary>
        public override void Demarre(int scene = 0)
        {
            if (_ouvert) return;
            if (scene > 0) { Niveau = scene % 10; _manche = scene / 10; }     // (les dizaines : la manche de départ — une sur deux se joue au soleil couchant)
            _ouvert = true;
            Ouvre();
        }

        /// <summary>Le poisson le plus proche du bouchon (pour le regard automatique du banc d'essai).</summary>
        public override bool ProchaineCible(out double x, out double y)
        {
            x = y = 0;
            if (_etat != Etat.Peche) return false;
            Poisson proche = null; double dmin = double.MaxValue;
            foreach (var p in _poissons)
            {
                if (p.Parti || p.X < 90 || p.X > W - 90) continue;
                double d = Distance(p.X, p.Y, _bx, _by);
                if (d < dmin) { dmin = d; proche = p; }
            }
            if (proche == null) return false;
            x = proche.X + proche.Sens * 30; y = proche.Y;
            return true;
        }

        // ------------------------------------------------------------------
        //  La scène en profondeur
        // ------------------------------------------------------------------
        /// <summary>0 : au fond de l'étang (en haut de l'image) ; 1 : tout près du ponton.</summary>
        private static double Prof(double y) => Borne((y - 330) / 430);
        private static double Taille(double y) => Mel(0.5, 1.12, Prof(y));
        /// <summary>Jusqu'où l'eau est libre vers le bas : le seau, les bottes et la main occupent le bord de l'image.</summary>
        private static double Bas(double x) => x < 410 ? 522 : x > 1110 ? 640 : x > 600 && x < 1000 ? 706 : 750;
        private static double Gauche(double y) => Math.Max(84, 392 - (y - 300) * 1.6);
        private static double Droite(double y) => Math.Min(W - 84, 1278 + (y - 300) * 1.8);
        private static bool Libre(double x, double y) => y >= 386 && y <= Bas(x) - 26 && x >= Gauche(y) + 30 && x <= Droite(y) - 30;

        // ------------------------------------------------------------------
        //  Une manche : de nouveaux poissons, le seau vide
        // ------------------------------------------------------------------
        private void Ouvre()
        {
            foreach (var p in _poissons.ToArray()) Retire(p);
            foreach (var v in _vols) Retire(v.P);
            _vols.Clear();
            foreach (var t in _tetes) Avant.Retire(t.O);
            _tetes.Clear();
            foreach (var b in _bacs) b.Vide();
            _prises = 0; _candidat = null; _pris = null; _sansPrise = 0; _aide = 0; _arrivee = 0; _charge = 0;
            int nombre = 5 + (Niveau > 0 ? 1 : 0);
            for (int i = 0; i < nombre; i++)
            {
                var p = Cree(Tire());
                for (int essai = 0; essai < 60; essai++)
                {
                    p.X = Entre(160, W - 160); p.Y = Entre(390, 740);
                    if (!Libre(p.X, p.Y)) continue;
                    bool seul = true;
                    foreach (var q in _poissons) if (Distance(q.X, q.Y, p.X, p.Y) < 190) seul = false;
                    if (seul) break;
                }
                p.Sens = p.SensVu = Hasard() < 0.5 ? -1 : 1;
                NouveauBut(p);
                Plonge(p);
            }
            // une manche sur deux, le soleil se couche derrière les montagnes
            _soir = _manche % 2 == 1;
            var ciel = PecheArt.Ciels[_soir ? "soir" : "etang"];
            CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            double sx = _soir ? 984 : 1380, sy = _soir ? 226 : 108;
            foreach (var o in new[] { _lueur, _rayons, _soleil }) { o.X = sx; o.Y = sy; }
            _chemin.X = sx; _teinte.Op = _soir ? 0.17 : 0;
            _mode = Mode.Pend;
            _etat = Etat.Entree; _depuis = 0;
            Dit(!_dejaDit ? Consigne : "Encore des poissons ! Remplis le seau !");
            _dejaDit = true;
            _manche++;
        }

        /// <summary>L'espèce du prochain poisson : toutes passent avant de revenir, le poisson doré une fois par tour.</summary>
        private int Tire()
        {
            if (_sac.Count == 0)
            {
                for (int i = 0; i < Especes.Length; i++) _sac.Add(i);
                for (int i = _sac.Count - 1; i > 0; i--) { int j = _alea.Next(i + 1); int t = _sac[i]; _sac[i] = _sac[j]; _sac[j] = t; }
            }
            int e = _sac[_sac.Count - 1]; _sac.RemoveAt(_sac.Count - 1);
            return e;
        }

        private Poisson Cree(int espece)
        {
            var e = Especes[espece];
            var p = new Poisson { Espece = espece, Nom = e.Nom, Dore = e.Dore, Allure = Entre(60, 92), Gabarit = e.Dore ? 0.86 : Entre(0.86, 1.04), Phase = Entre(0, 6), Change = Entre(1, 4), Bulle = Entre(1, 5) };
            p.Ombre = Dessin("poisson_ombre");
            p.Objets.Add(p.Ombre);
            p.Queue = Ajoute(p, "poisson_queue", e.Raye ? -50 : -64, 0, e.C);
            Ajoute(p, e.Raye ? "poisson_raye" : "poisson", 0, 0, e.C);
            p.Nageoire = Ajoute(p, "poisson_nageoire", e.Raye ? 2 : 8, 14, e.C);
            p.OeilX = e.Raye ? 46 : 48; p.OeilY = e.Raye ? -13 : -11;
            p.Pupille = Ajoute(p, "poisson_pupille", p.OeilX, p.OeilY);
            double bx = e.Raye ? 62 : 68, by = e.Raye ? 16 : 14;
            p.Sourire = Ajoute(p, "poisson_bouche", bx, by);
            p.Rond = Ajoute(p, "poisson_bouche_o", bx + 4, by); p.Rond.O.Op = 0;
            p.Ravi = Ajoute(p, "poisson_bouche_ravi", bx, by); p.Ravi.O.Op = 0;
            if (e.Dore) Ajoute(p, "couronne", 16, -50, null).R = 10;
            return p;
        }
        private static Membre Ajoute(Poisson p, string dessin, double x, double y, string[] couleurs = null)
        {
            var o = Dessin(dessin, couleurs);
            p.Objets.Add(o);
            return p.G.Ajoute(o, x, y);
        }
        /// <summary>Met un poisson dans l'étang (sous le voile de l'eau).</summary>
        private void Plonge(Poisson p)
        {
            foreach (var o in p.Objets) { Effets.Retire(o); Fond.Ajoute(o); }
            p.Ombre.Op = 1;
            if (!_poissons.Contains(p)) _poissons.Add(p);
        }
        /// <summary>Sort un poisson de l'eau : il passe devant tout le reste.</summary>
        private void Sors(Poisson p)
        {
            foreach (var o in p.Objets) { Fond.Retire(o); Effets.Ajoute(o); }
            p.Ombre.Op = 0;
            _poissons.Remove(p);
        }
        private void Retire(Poisson p)
        {
            foreach (var o in p.Objets) { Fond.Retire(o); Effets.Retire(o); }
            _poissons.Remove(p);
        }
        private static void Bouche(Poisson p, int laquelle)
        {
            p.Sourire.O.Op = laquelle == 0 ? 1 : 0; p.Rond.O.Op = laquelle == 1 ? 1 : 0; p.Ravi.O.Op = laquelle == 2 ? 1 : 0;
        }
        private void NouveauBut(Poisson p)
        {
            p.Change = Entre(2.5, 6);
            if (p.Parti) { p.ButX = p.X < W / 2 ? -260 : W + 260; p.ButY = Borne(p.Y + Entre(-40, 40), 480, 620); return; }
            for (int essai = 0; essai < 40; essai++)
            {
                double x = Entre(150, W - 150), y = Entre(390, 740);
                if (Libre(x, y) && Math.Abs(x - p.X) > 160) { p.ButX = x; p.ButY = y; return; }
            }
            p.ButX = 800; p.ButY = 560;
        }

        /// <summary>Place un groupe tourné vers la droite (sens 1) ou vers la gauche (sens -1), penché de « pente » degrés vers l'avant.</summary>
        private static void Miroir(Groupe g, double sens, double x, double y, double e, double pente)
        {
            double signe = sens < 0 ? -1 : 1;
            if (Math.Abs(sens) < 0.14) sens = 0.14 * signe;       // en plein demi-tour, il est vu de face : très mince
            g.X = x; g.Y = y; g.SX = e * sens; g.SY = e; g.R = signe * pente;
            g.Place();
            if (signe < 0) foreach (var m in g.Membres) m.O.R = g.R - m.R;
        }
        private void Montre(Poisson p, double x, double y, double e, double pente, double battement)
        {
            p.Phase += battement;
            p.Queue.R = 20 * Math.Sin(p.Phase); p.Queue.SX = 1 - 0.14 * Math.Abs(Math.Sin(p.Phase));
            p.Nageoire.R = 18 * Math.Sin(p.Phase * 0.6 + 1);
            Miroir(p.G, p.SensVu, x, y, e, pente);
        }

        private void Gicle(double x, double y, double e)
        {
            var g = _gerbes[_gerbe++ % _gerbes.Length];
            g.Age = 0; g.X = x; g.Y = y; g.E = e; g.O.R = Entre(0, 360);
        }
        private void Eclat(double x, double y, int n = 7, double force = 260)
        {
            for (int i = 0; i < n; i++)
            {
                double a = Math.PI * 2 * i / n + Hasard();
                _etoiles.Lance(x, y, Math.Cos(a) * force * Entre(0.6, 1), Math.Sin(a) * force * Entre(0.6, 1) - 60, 420, Entre(0.5, 0.8), Entre(0.7, 1.3), 0, Entre(-200, 200));
            }
        }
        private void Eclabousse(double x, double y, int n, double e)
        {
            for (int i = 0; i < n; i++) _gouttes.Lance(x + Entre(-16, 16) * e, y, Entre(-260, 260) * e, Entre(-520, -180) * e, 1100, Entre(0.45, 0.8), Entre(0.6, 1.1) * e);
        }
        private void PoseEclat(int i)
        {
            double y = Entre(330, 780), x = Entre(Gauche(y), Droite(y));
            _eclats[i].X = x; _eclats[i].Y = y; _eclats[i].E = Mel(0.35, 1.0, Prof(y)) * Entre(0.7, 1.1);
        }

        // ------------------------------------------------------------------
        //  Une image du jeu
        // ------------------------------------------------------------------
        public override void Avance(double dt)
        {
            if (dt <= 0 || !_ouvert) return;
            dt = Math.Min(dt, 0.1);
            _temps += dt; _depuis += dt;
            bool regarde = _temps - _vu < 0.5 && _rx > -80 && _rx < W + 80 && _ry > -80 && _ry < H + 80;

            AvanceDecor(dt);
            switch (_etat)
            {
                case Etat.Entree:
                    // le rideau d'eau retombe et découvre l'étang
                    _vague.Y = Mel(-140, H + 60, Doux(_depuis / 0.7)); _vague.X = -100 - 60 * Math.Sin(_temps * 5);
                    if (_depuis >= 0.7) { _etat = Etat.Peche; _depuis = 0; Lance(regarde); }
                    break;
                case Etat.Peche:
                    _sansPrise += dt;
                    if (_aide > 0) _aide -= dt;
                    // Le coup de pouce : rien ne mord depuis un moment → la consigne est redite, les poissons ralentissent
                    // et le plus proche vient de lui-même vers le bouchon.
                    if (_sansPrise > 14 && !VoixOccupee()) { _sansPrise = 0; _aide = 9; Dit(Consigne); }
                    if (_arrivee > 0) { _arrivee -= dt; if (_arrivee <= 0) Arrivee(); }
                    break;
                case Etat.Ferre: AvanceFerre(dt); break;
                case Etat.Envol: break;      // le poisson vole vers le seau (voir AvanceVols)
                case Etat.Fete:
                    if (Math.Floor(_depuis / 0.3) != Math.Floor((_depuis - dt) / 0.3))
                    {
                        Eclat(SeauX + Entre(-150, 190), SeauY - Entre(200, 420), 6, 220);
                        _coeurs.Lance(SeauX + Entre(-90, 90), SeauY - 230, Entre(-60, 60), Entre(-260, -160), 60, 1.3, Entre(0.9, 1.5));
                    }
                    if (_depuis >= 2.8) { _etat = Etat.Relache; _depuis = 0; _relaches = 0; Dit("Et hop ! On les remet à l'eau !"); }
                    break;
                case Etat.Relache:
                    // un par un, les poissons sautent du seau et retournent dans l'étang
                    if (_relaches < Plein && _depuis >= 0.5 + _relaches * 0.45 && _tetes.Count > 0)
                    {
                        var t = _tetes[_tetes.Count - 1]; _tetes.RemoveAt(_tetes.Count - 1);
                        Avant.Retire(t.O);
                        var p = Cree(t.Espece);
                        foreach (var o in p.Objets) Effets.Ajoute(o);
                        p.Ombre.Op = 0; p.Sens = p.SensVu = 1; Bouche(p, 2);
                        double x1 = 520 + _relaches * 150 + Entre(-40, 40), y1 = Entre(470, 640);
                        _vols.Add(new Vol { P = p, Duree = 0.85, X0 = t.X, Y0 = t.Y - 30, X1 = x1, Y1 = y1, Haut = 250, E0 = 0.7, E1 = Taille(y1) * p.Gabarit });
                        Gicle(t.X, SeauY - 200, 0.5); Eclabousse(t.X, SeauY - 200, 5, 0.7);
                        Son("pop", 0);
                        _relaches++;
                    }
                    if (_relaches >= Plein && _vols.Count == 0 && _depuis >= 0.5 + Plein * 0.45 + 1.5) { _etat = Etat.Sortie; _depuis = 0; }
                    break;
                case Etat.Sortie:
                    // le rideau d'eau monte et recouvre tout, puis une nouvelle manche s'ouvre
                    _vague.Y = Mel(H + 60, -140, Doux(_depuis / 0.7)); _vague.X = -100 - 60 * Math.Sin(_temps * 5);
                    if (_depuis >= 0.75) Ouvre();
                    break;
            }
            AvanceBouchon(dt, regarde);
            AvancePoissons(dt, regarde);
            AvanceVols(dt);
            AvanceCanard(dt);
            AvanceGrenouille(dt);
            AvanceCanne(dt);
            AvanceSeau(dt);
            foreach (var g in _gerbes)
            {
                if (g.Age > 0.5) { g.O.Op = 0; continue; }
                g.Age += dt;
                double u = g.Age / 0.5;
                g.O.X = g.X; g.O.Y = g.Y - 20 * g.E * Sortant(u); g.O.E = g.E * (0.35 + 0.85 * Sortant(u)); g.O.SY = g.O.SX * 0.8; g.O.Op = Borne((1 - u) * 2.2) * 0.96;
            }
            foreach (var b in _bacs) b.Avance(dt);
            AvanceMots(dt);
        }

        // ------------------------------------------------------------------
        //  Le ciel, les roseaux, les nénuphars, les reflets, la libellule
        // ------------------------------------------------------------------
        private void AvanceDecor(double dt)
        {
            _rayons.R += dt * 14; _lueur.E = 0.8 * (1 + 0.05 * Math.Sin(_temps * 1.7));
            for (int i = 0; i < _nuages.Length; i++) { var n = _nuages[i]; n.X += dt * (6 + i * 3); if (n.X > W + 200) n.X = -200; }
            for (int i = 0; i < _oiseaux.Length; i++)
            {
                var o = _oiseaux[i]; o.X += dt * (34 + i * 9); o.Y += dt * 6 * Math.Sin(_temps * 0.7 + i);
                o.SY = o.SX * (0.6 + 0.4 * Math.Sin(_temps * 7 + i * 2));
                if (o.X > W + 80) { o.X = -80; o.Y = Entre(70, 190); }
            }
            for (int i = 0; i < _roseaux.Length; i++) _roseaux[i].R = 3.2 * Math.Sin(_temps * 1.1 + i * 1.7) + 1.2 * Math.Sin(_temps * 2.3 + i);
            for (int i = 0; i < _feuilles.Count; i++)
            {
                var f = _feuilles[i];
                f.Creux = Math.Max(0, f.Creux - dt * 2.5);
                double y = f.Y + (1.6 * Math.Sin(_temps * 1.3 + i * 2.1) + 7 * f.Creux * Math.Sin(f.Creux * 9)) * f.E;
                f.O.Y = y; f.O.X = f.X + 2 * Math.Sin(_temps * 0.6 + i) * f.E;
                if (f.Fleur != null) { f.Fleur.X = f.O.X - 6 * f.E; f.Fleur.Y = y - 4 * f.E; }
            }
            // l'eau scintille : chaque éclat s'allume, s'éteint, et change de place
            for (int i = 0; i < _eclats.Length; i++)
            {
                double avant = Math.Sin(_eclatPhase[i]);
                _eclatPhase[i] += dt * (1.6 + (i % 4) * 0.35);
                double s = Math.Sin(_eclatPhase[i]);
                if (avant < 0 && s >= 0) PoseEclat(i);
                _eclats[i].Op = s > 0 ? s * s * 0.95 : 0;
                _eclats[i].R = _eclatPhase[i] * 20;
            }
            // le soir, des lucioles s'allument au bord de l'eau
            if (_soir && Hasard() < dt * 2.2)
            {
                double y = Entre(250, 520), x = Hasard() < 0.5 ? Entre(0, Gauche(y) + 60) : Entre(Droite(y) - 60, W);
                _lucioles.Lance(x, y, Entre(-14, 14), Entre(-22, -6), 0, Entre(1.6, 2.6), Entre(0.5, 1.0));
            }
            // la libellule se promène au-dessus de l'étang
            double lx = 800 + 560 * Math.Sin(_temps * 0.31), ly = 372 + 70 * Math.Sin(_temps * 0.53 + 1) + 10 * Math.Sin(_temps * 3.1);
            double vers = Math.Cos(_temps * 0.31) >= 0 ? 1 : -1, el = Mel(0.55, 0.8, Borne((ly - 300) / 150));
            foreach (var o in new[] { _libellule, _libelluleAiles }) { o.X = lx; o.Y = ly; o.SX = el * vers; o.SY = el; o.R = vers * 6 * Math.Sin(_temps * 2); }
            _libelluleAiles.SY = el * (0.35 + 0.65 * Math.Abs(Math.Sin(_temps * 38)));
        }

        // ------------------------------------------------------------------
        //  Le bouchon : il va là où l'on regarde
        // ------------------------------------------------------------------
        private void Lance(bool regarde)
        {
            _mode = Mode.Lance; _lanceT = 0; _lx0 = _px; _ly0 = _py;
        }

        private void AvanceBouchon(double dt, bool regarde)
        {
            // là où l'on regarde, ramené sur l'eau libre : le leurre pend sous le bouchon, c'est lui qu'on amène sur le poisson
            double gy = Borne(_ry - 44 * Taille(_ry), 376, 750), gx = Borne(_rx, Gauche(gy), Droite(gy));
            gy = Math.Min(gy, Bas(gx));
            double e = Taille(_by);
            switch (_mode)
            {
                case Mode.Pend:
                    _bx = _px; _by = _py;
                    break;
                case Mode.Lance:
                {
                    // il part du bout de la canne et retombe sur l'eau
                    _lanceT += dt;
                    double u = Borne(_lanceT / 0.55), ax = regarde ? gx : 800, ay = regarde ? gy : 560;
                    _bx = Mel(_lx0, ax, Doux(u)); _by = Mel(_ly0, ay, u) - 150 * 4 * u * (1 - u) * 0.5;
                    if (u >= 1)
                    {
                        _mode = Mode.Flotte; _bx = ax; _by = ay; _plonge = 0;
                        Son("pop", 0);
                        Gicle(_bx, _by, 0.34 * Taille(_by)); _ondes.Lance(_bx, _by, 0, 0, 0, 0.7, 0.5 * Taille(_by), 2.4);
                    }
                    break;
                }
                case Mode.Flotte:
                    if (regarde)
                    {
                        double suit = Math.Min(1, dt * 5.5), ax = _bx;
                        _bx += (gx - _bx) * suit; _by += (gy - _by) * suit;
                        _bvx = (_bx - ax) / dt;
                    }
                    else _bvx *= 0.9;
                    _plonge = Math.Max(0, _plonge - dt * 4);
                    // il laisse des ronds dans l'eau, davantage quand il glisse
                    _prochaineOnde -= dt * (1 + Math.Min(4, Math.Abs(_bvx) / 120));
                    if (_prochaineOnde <= 0) { _prochaineOnde = 1.5; _ondes.Lance(_bx, _by + 4 * e, 0, 0, 0, 1.0, 0.5 * e, 1.8); }
                    break;
                case Mode.Plonge:
                    _plonge = Math.Min(1, _plonge + dt * 7);
                    break;
                case Mode.Suit:
                    break;      // il suit le poisson (voir AvanceVols)
            }
            e = Taille(_by);
            bool surLEau = _mode == Mode.Flotte || _mode == Mode.Plonge;
            double flotte = surLEau ? 3.5 * e * Math.Sin(_temps * 2.6) : 0, tremble = _plonge * 5 * Math.Sin(_temps * 50);
            double taille = surLEau ? e : Mel(0.62, e, _mode == Mode.Lance ? Borne(_lanceT / 0.55) : 0);
            _bouchon.X = _bx + tremble; _bouchon.Y = _by + flotte + 34 * e * _plonge; _bouchon.E = taille;
            _bouchon.R = surLEau ? Borne(_bvx * 0.035, -16, 16) + 4 * Math.Sin(_temps * 1.7) : 8 * Math.Sin(_temps * 2.2);
            _bouchon.Op = _mode == Mode.Suit && _depuis < 0.5 ? 0 : 1 - 0.55 * _plonge;
            _reflet.X = _bx; _reflet.Y = _by + flotte; _reflet.E = e; _reflet.SX = e * (1 + 0.08 * Math.Sin(_temps * 5)); _reflet.Op = surLEau ? 1 - _plonge : 0;
            _rond.X = _bx; _rond.Y = _by + 3 * e; _rond.E = e * (1 + 0.05 * Math.Sin(_temps * 3)); _rond.Op = surLEau ? 0.75 : 0;
            // le leurre brille sous l'eau, au bout de la ligne
            _leurre.X = _bx + 3 * Math.Sin(_temps * 2.1) * e; _leurre.Y = _by + 50 * e; _leurre.E = e; _leurre.R = 8 * Math.Sin(_temps * 1.9);
            _leurre.Op = _mode == Mode.Flotte ? 0.95 : 0;

            // le rond qui se remplit : c'est le poisson qui s'approche
            double part = _candidat != null ? Borne(_candidat.Interet) : 0;
            if (part > 0.02 && _mode == Mode.Flotte)
            {
                int n = Math.Max(2, (int)Math.Ceiling(part * 40) + 1);
                var pts = new double[n * 2];
                for (int i = 0; i < n; i++) { double a = -Math.PI / 2 + Math.PI * 2 * part * i / (n - 1); pts[i * 2] = 82 * Math.Cos(a); pts[i * 2 + 1] = 29 * Math.Sin(a); }
                foreach (var o in new[] { _progresBord, _progres }) { o.Points = pts; o.Etat++; o.X = _bx; o.Y = _by + 3 * e; o.E = e; }
                _progresBord.Op = 0.55; _progres.Op = 1;
            }
            else { _progresBord.Op = 0; _progres.Op = 0; }
        }

        // ------------------------------------------------------------------
        //  Les poissons
        // ------------------------------------------------------------------
        private void AvancePoissons(double dt, bool regarde)
        {
            double eb = Taille(_by), lx = _bx, ly = _by + 50 * eb;
            // le poisson le plus proche du leurre, s'il est assez près, s'y intéresse
            Poisson proche = null; double dmin = double.MaxValue;
            if (_etat == Etat.Peche && _mode == Mode.Flotte && regarde)
                foreach (var p in _poissons)
                {
                    if (p.Parti) continue;
                    double d = Distance(p.X + p.Sens * 40 * Taille(p.Y), p.Y, lx, ly);
                    if (d < dmin) { dmin = d; proche = p; }
                }
            double rayon = (_aide > 0 ? 380 : Rayons[Niveau]) * Mel(0.75, 1.1, Prof(_by));
            _candidat = proche != null && dmin < rayon ? proche : null;

            foreach (var p in _poissons.ToArray())
            {
                double e = Taille(p.Y) * p.Gabarit;
                double vitesse = p.Allure * Vitesses[Niveau] * Mel(0.6, 1.1, Prof(p.Y)) * (_aide > 0 ? 0.5 : 1) * (p.Parti ? 2.2 : 1);
                if (p == _candidat)
                {
                    // il vient poser sa bouche sur le leurre
                    p.Interet += dt / (_aide > 0 ? 0.85 : 1.1);
                    if (Math.Abs(lx - p.X) > 50 * e) p.Sens = lx >= p.X ? 1 : -1;
                    p.ButX = lx - p.Sens * 70 * e; p.ButY = ly - 10 * e;
                    vitesse = Math.Max(vitesse * 1.3, 170);
                }
                else
                {
                    p.Interet = Math.Max(0, p.Interet - dt * 1.3);
                    p.Change -= dt;
                    if (p.Change <= 0 || Distance(p.X, p.Y, p.ButX, p.ButY) < 24) NouveauBut(p);
                }
                double dx = p.ButX - p.X, dy = p.ButY - p.Y, d = Math.Sqrt(dx * dx + dy * dy), vx = 0, vy = 0;
                if (d > 3) { double v = Math.Min(vitesse, d * 3.5); vx = dx / d * v; vy = dy / d * v; }
                double prend = Math.Min(1, dt * (p == _candidat ? 6 : 2.2));
                p.Vx += (vx - p.Vx) * prend; p.Vy += (vy - p.Vy) * prend;
                p.X += p.Vx * dt; p.Y += p.Vy * dt;
                if (p != _candidat && Math.Abs(p.Vx) > 8) p.Sens = p.Vx > 0 ? 1 : -1;
                p.SensVu += (p.Sens - p.SensVu) * Math.Min(1, dt * 8);
                if (p.Parti && (p.X < -200 || p.X > W + 200)) { Retire(p); continue; }

                // son visage : il regarde le leurre et ouvre la bouche quand il s'y intéresse
                bool interesse = p == _candidat && p.Interet > 0.1;
                Bouche(p, interesse ? 1 : 0);
                double ox = 3 + 1.5 * Math.Sin(_temps * 0.8 + p.Phase), oy = 1.5 * Math.Sin(_temps * 0.5 + p.Phase);
                if (p == _candidat)
                {
                    double mx = (lx - p.X) * (p.Sens < 0 ? -1 : 1), my = ly - p.Y, m = Math.Max(1, Math.Sqrt(mx * mx + my * my));
                    ox = mx / m * 5; oy = my / m * 5;
                }
                p.Pupille.X = p.OeilX + ox; p.Pupille.Y = p.OeilY + oy;
                double allure = Math.Sqrt(p.Vx * p.Vx + p.Vy * p.Vy);
                Montre(p, p.X, p.Y + 4 * e * Math.Sin(_temps * 1.7 + p.Phase), e, Borne(p.Vy * 0.22, -16, 16), dt * (4.5 + allure * 0.06));
                p.Ombre.X = p.X + 12 * e; p.Ombre.Y = p.Y + 60 * e; p.Ombre.E = e;

                // de temps en temps, une bulle
                p.Bulle -= dt;
                if (p.Bulle <= 0)
                {
                    p.Bulle = Entre(2.5, 6);
                    _bulles.Lance(p.X + p.Sens * 70 * e, p.Y - 6 * e, Entre(-8, 8), -46 * e, -20, 1.1, Entre(0.35, 0.6) * e, 0.5);
                }
                // le poisson doré scintille
                if (p.Dore && Hasard() < dt * 2.5) _etoiles.Lance(p.X + Entre(-60, 60) * e, p.Y + Entre(-40, 40) * e, 0, -20, 0, 0.55, Entre(0.3, 0.55) * e, 0, 120);
            }
            if (_candidat != null && _candidat.Interet >= 1) Ferre(_candidat);
        }

        /// <summary>Un nouveau poisson entre par le côté.</summary>
        private void Arrivee()
        {
            int vivants = 0;
            foreach (var p in _poissons) if (!p.Parti) vivants++;
            if (vivants >= 5 + (Niveau > 0 ? 1 : 0)) return;
            var n = Cree(Tire());
            bool gauche = Hasard() < 0.5;
            n.X = gauche ? -140 : W + 140; n.Y = Entre(500, 610); n.Sens = n.SensVu = gauche ? 1 : -1;
            NouveauBut(n);
            Plonge(n);
        }

        // ------------------------------------------------------------------
        //  Ça mord !
        // ------------------------------------------------------------------
        private void Ferre(Poisson p)
        {
            _pris = p; _candidat = null;
            _etat = Etat.Ferre; _depuis = 0; _mode = Mode.Plonge;
            _sansPrise = 0; _aide = 0;
            foreach (var q in _poissons) q.Interet = 0;
            Bouche(p, 2);
            double e = Taille(_by);
            Son("plouf", 0);
            Ecris("Ça mord !", _bx, _by - 190 * e, "#FFE066", 74);
            Dit("Ça mord ! " + char.ToUpper(p.Nom[0]) + p.Nom.Substring(1) + " !");
            Gicle(_bx, _by, 0.55 * e); Eclabousse(_bx, _by, 6, 0.7 * e);
            _ondes.Lance(_bx, _by, 0, 0, 0, 0.7, 0.6 * e, 2.6);
        }

        private void AvanceFerre(double dt)
        {
            // le poisson tire sur la ligne : le bouchon s'enfonce, la canne plie, le moulinet tourne
            var p = _pris;
            double eb = Taille(_by), e = Taille(p.Y) * p.Gabarit, lx = _bx, ly = _by + 50 * eb;
            _charge = Math.Min(1, _charge + dt * 5);
            _tour += dt * 760;
            double suit = Math.Min(1, dt * 10);
            p.X += (lx - p.Sens * 66 * e + 10 * Math.Sin(_temps * 31) - p.X) * suit; p.Y += (ly - 6 * e + 8 * Math.Sin(_temps * 23) - p.Y) * suit;
            p.SensVu += (p.Sens - p.SensVu) * Math.Min(1, dt * 8);
            p.Pupille.X = p.OeilX + 2; p.Pupille.Y = p.OeilY - 3;
            Montre(p, p.X, p.Y, e, -8 + 24 * Math.Sin(_temps * 27), dt * 34);
            p.Ombre.X = p.X + 12 * e; p.Ombre.Y = p.Y + 60 * e; p.Ombre.E = e;
            if (Math.Floor(_depuis / 0.16) != Math.Floor((_depuis - dt) / 0.16))
            {
                _ondes.Lance(_bx, _by, 0, 0, 0, 0.6, 0.5 * eb, 2.6);
                Eclabousse(_bx, _by, 2, 0.6 * eb);
                _bulles.Lance(p.X + Entre(-40, 40) * e, p.Y - 20 * e, Entre(-20, 20), -60 * e, -20, 0.8, Entre(0.4, 0.7) * e, 0.5);
            }
            if (_depuis < 0.95) return;

            // … et il sort de l'eau dans une gerbe, pour voler jusqu'au seau
            Sors(p);
            _plonge = 0;
            p.Sens = p.SensVu = -1;
            _etat = Etat.Envol; _depuis = 0; _mode = Mode.Suit;
            _vols.Add(new Vol { P = p, VersLeSeau = true, Duree = 1.0, X0 = _bx, Y0 = _by - 10 * eb, X1 = SeauX + 6, Y1 = SeauY - 232, Haut = 270, E0 = e, E1 = 0.92 });
            Gicle(_bx, _by, 1.05 * eb); Eclabousse(_bx, _by, 14, eb);
            _ondes.Lance(_bx, _by, 0, 0, 0, 0.9, 0.7 * eb, 3);
            Son("pop", 0);
        }

        private void AvanceVols(double dt)
        {
            for (int i = _vols.Count - 1; i >= 0; i--)
            {
                var v = _vols[i]; var p = v.P;
                v.T += dt;
                double u = Borne(v.T / v.Duree);
                double x = Mel(v.X0, v.X1, u), y = Mel(v.Y0, v.Y1, u) - v.Haut * 4 * u * (1 - u), e = Mel(v.E0, v.E1, u);
                // il fait la culbute en l'air, la queue qui frétille
                Montre(p, x, y, e, -34 + 394 * Doux(u), dt * 36);
                p.Pupille.X = p.OeilX + 2; p.Pupille.Y = p.OeilY - 2;
                if (Hasard() < dt * 26) _gouttes.Lance(x + Entre(-30, 30) * e, y + 20 * e, Entre(-40, 40), Entre(-20, 60), 800, Entre(0.35, 0.6), Entre(0.5, 0.9) * e);
                if (v.VersLeSeau)
                {
                    // le bouchon reste accroché au poisson jusqu'au sommet, puis revient pendre au bout de la canne
                    if (u < 0.5) { _bx = x - 40 * e; _by = y; }
                    else { double r = Doux((u - 0.5) / 0.5); _bx = Mel(_bx, _px, r); _by = Mel(_by, _py, r); _charge = Math.Max(0, _charge - dt * 5); }
                }
                if (u < 1) continue;
                _vols.RemoveAt(i);
                if (v.VersLeSeau) DansLeSeau(p);
                else
                {
                    // retour à l'étang : une gerbe, et il file sous l'eau
                    Bouche(p, 0);
                    p.X = x; p.Y = y; p.Vx = 120; p.Vy = 0; p.Parti = true;
                    Plonge(p);
                    NouveauBut(p);
                    double ee = Taille(y);
                    Gicle(x, y, 0.9 * ee); Eclabousse(x, y, 9, ee); _ondes.Lance(x, y, 0, 0, 0, 0.9, 0.7 * ee, 3);
                    Son("plouf", 0);
                }
            }
        }

        /// <summary>Le poisson retombe dans le seau : on voit sa tête qui dépasse.</summary>
        private void DansLeSeau(Poisson p)
        {
            Retire(p);
            double[] places = { 0, -54, 54, -94, 94 };
            double dx = places[_prises % places.Length], bord = -202 + 22 * (1 - dx / 128 * (dx / 128));
            var t = new Tete { Espece = p.Espece, X = SeauX + dx, Y = SeauY + bord - 2 };
            t.O = Dessin("tete", Especes[p.Espece].C).En(t.X, t.Y, 0.88);
            Avant.Avant(_seauDevant, t.O);
            _tetes.Add(t);
            _prises++; Reussites++;
            _pris = null; _tape = 1; _mode = Mode.Pend;
            Son("plouf", 0);
            Gicle(SeauX, SeauY - 206, 0.8); Eclabousse(SeauX, SeauY - 210, 10, 0.9);
            Eclat(SeauX, SeauY - 300, 8);
            Ecris("Plouf !", SeauX + 70, SeauY - 400, "#9ADCFF", 62);
            if (_prises >= Plein)
            {
                _etat = Etat.Fete; _depuis = 0;
                Dit("Le seau est plein ! Quelle pêche !");
                Bravo();
            }
            else
            {
                _etat = Etat.Peche; _depuis = 0; _arrivee = 1.2;
                Lance(true);
            }
        }

        // ------------------------------------------------------------------
        //  Le canard farceur
        // ------------------------------------------------------------------
        private void AvanceCanard(double dt)
        {
            double e = Taille(_cy) * 0.92;
            if (Distance(_cx, _cy, _cButX, _cButY) < 20) { _cButY = Entre(350, 470); _cButX = Entre(Gauche(_cButY) + 120, Droite(_cButY) - 120); }
            double dx = _cButX - _cx, dy = _cButY - _cy, d = Math.Max(1, Math.Sqrt(dx * dx + dy * dy)), v = _coin > 0 ? 90 : 24;
            _cx += dx / d * v * dt; _cy += dy / d * v * dt;
            if (Math.Abs(dx) > 12) _cSens = dx > 0 ? 1 : -1;
            _cSensVu += (_cSens - _cSensVu) * Math.Min(1, dt * 5);

            // le bouchon reste près de lui : « Coin coin ! »
            bool pres = _etat == Etat.Peche && _mode == Mode.Flotte && Distance(_bx, _by, _cx, _cy - 30 * e) < 125 * e;
            _cPres = pres ? _cPres + dt : 0;
            if (_cRepos > 0) _cRepos -= dt;
            if (_cPres > 0.6 && _cRepos <= 0)
            {
                _coin = 1.6; _cRepos = 9; _cPres = 0;
                Ecris("Coin coin !", _cx, _cy - 190 * e, "#FFE066", 62);
                Son("pop", 0);
                if (!VoixOccupee()) Dit("Coin coin ! Ce n'est pas un poisson, c'est le canard !");
                // il s'éloigne en battant des ailes
                _cButY = Entre(350, 450); _cButX = _bx < W / 2 ? Droite(_cButY) - 140 : Gauche(_cButY) + 140;
            }
            double saut = 0;
            if (_coin > 0)
            {
                _coin -= dt;
                bool ouvert = Math.Floor(_coin * 8) % 2 == 0;
                _cBec.O.Op = ouvert ? 0 : 1; _cBecOuvert.O.Op = ouvert ? 1 : 0;
                _cAile.R = 46 * Math.Abs(Math.Sin(_coin * 22));
                saut = -12 * e * Math.Abs(Math.Sin(_coin * 11));
            }
            else { _cBec.O.Op = 1; _cBecOuvert.O.Op = 0; _cAile.R = 3 * Math.Sin(_temps * 1.4); }
            Miroir(_canard, _cSensVu, _cx, _cy + 2.5 * e * Math.Sin(_temps * 2.1) + saut, e, 2.5 * Math.Sin(_temps * 1.6));
            _cOnde -= dt * (_coin > 0 ? 5 : 1);
            if (_cOnde <= 0) { _cOnde = 1.7; _ondes.Lance(_cx, _cy + 8 * e, 0, 0, 0, 1.1, 0.9 * e, 1.3); }
        }

        // ------------------------------------------------------------------
        //  La grenouille saute de nénuphar en nénuphar
        // ------------------------------------------------------------------
        private void AvanceGrenouille(double dt)
        {
            var f = _feuilles[_gSur];
            _gPose += dt;
            if (!_gSaute)
            {
                _gAttente -= dt;
                bool pres = _mode == Mode.Flotte && Distance(_bx, _by, f.X, f.Y - 20 * f.E) < 120 * f.E;
                if (_gAttente <= 0 || (pres && _gPose > 1.2))
                {
                    _gVers = (_gSur + 1 + _alea.Next(3)) % 4;
                    _gSaute = true; _gT = 0; f.Creux = 1;
                    if (pres) Ecris("Coâ !", f.X, f.Y - 150 * f.E, "#B4F08A", 56);
                }
                else
                {
                    // assise : elle s'écrase un peu en retombant, sa gorge bat
                    double ecrase = 1 - 0.22 * Math.Exp(-_gPose * 7) * Math.Cos(_gPose * 18);
                    _grenouille.X = f.O.X; _grenouille.Y = f.O.Y + 2 * f.E; _grenouille.SX = f.E * (2 - ecrase); _grenouille.SY = f.E * ecrase * (1 + 0.025 * Math.Sin(_temps * 5));
                    _grenouille.Op = 1; _grenouilleSaut.Op = 0;
                }
            }
            if (_gSaute)
            {
                var v = _feuilles[_gVers];
                _gT += dt;
                double u = Borne(_gT / 0.75), sens = v.X >= f.X ? 1 : -1, e = Mel(f.E, v.E, u);
                double x = Mel(f.X, v.X, u), y = Mel(f.Y, v.Y, u) - 150 * e * 4 * u * (1 - u) - 14 * e;
                _grenouilleSaut.X = x; _grenouilleSaut.Y = y; _grenouilleSaut.SX = e * sens; _grenouilleSaut.SY = e; _grenouilleSaut.R = sens * Mel(-38, 38, u);
                _grenouilleSaut.Op = 1; _grenouille.Op = 0;
                if (u >= 1)
                {
                    _gSaute = false; _gSur = _gVers; _gAttente = Entre(5, 10); _gPose = 0;
                    v.Creux = 1;
                    _ondes.Lance(v.X, v.Y + 6 * v.E, 0, 0, 0, 0.9, 0.9 * v.E, 1.6);
                }
            }
        }

        // ------------------------------------------------------------------
        //  La canne, la ligne, le moulinet
        // ------------------------------------------------------------------
        private const int Pas = 18;

        private void AvanceCanne(double dt)
        {
            if (_etat != Etat.Ferre && _etat != Etat.Envol) _charge = Math.Max(0, _charge - dt * 3);
            // Le bout de la canne se tient au-dessus du bouchon, un peu en retrait ; les mains ne bougent presque pas.
            bool surLEau = _mode == Mode.Flotte || _mode == Mode.Plonge || _mode == Mode.Lance;
            double hx = MainX + (_bx - 800) * 0.04 + 5 * _charge * Math.Sin(_temps * 37), hy = MainY + 4 * _charge * Math.Sin(_temps * 29) + 2 * Math.Sin(_temps * 1.3);
            double prof = Prof(_by);
            double vx = surLEau ? _bx + (hx - _bx) * 0.36 : 1010, vy = surLEau ? Math.Max(70, _by - Mel(250, 205, prof)) : 250;
            double suit = Math.Min(1, dt * 7);
            _tx += (vx - _tx) * suit; _ty += (vy - _ty) * suit;
            // quand ça mord, le bout de la canne est tiré vers le bouchon
            double tx = _tx + (_bx - _tx) * 0.16 * _charge, ty = _ty + (_by - _ty) * 0.34 * _charge + 3 * Math.Sin(_temps * 2.3);
            // la canne est une courbe : elle part dans l'axe de la poignée et plie sous la charge
            double cdx = tx - hx, cdy = ty - hy, long_ = Math.Max(1, Math.Sqrt(cdx * cdx + cdy * cdy));
            double nx = -cdy / long_, ny = cdx / long_;
            if (nx * (_bx - (hx + tx) / 2) + ny * (_by - (hy + ty) / 2) > 0) { nx = -nx; ny = -ny; }      // elle se bombe du côté opposé à la charge
            double ventre = long_ * (0.05 + 0.2 * _charge);
            double mx = hx + cdx * 0.55 + nx * ventre, my = hy + cdy * 0.55 + ny * ventre;

            double u0 = Borne(Prise * TaillePoignee / long_, 0.05, 0.7);
            int n = Pas + 1;
            var bord = new double[n * 4]; var corps = new double[n * 4]; var ombre = new double[n * 4]; var reflet = new double[n * 2];
            double bx0 = 0, by0 = 0, ax0 = 0, ay0 = 0, cote = 1;
            for (int i = 0; i <= Pas; i++)
            {
                double w = (double)i / Pas, u = Mel(u0, 1, w), v = 1 - u;
                double x = v * v * hx + 2 * v * u * mx + u * u * tx, y = v * v * hy + 2 * v * u * my + u * u * ty;
                double dx = 2 * v * (mx - hx) + 2 * u * (tx - mx), dy = 2 * v * (my - hy) + 2 * u * (ty - my), d = Math.Max(1e-6, Math.Sqrt(dx * dx + dy * dy));
                double px = -dy / d, py = dx / d, l = Mel(9.5, 2.4, w);
                if (i == 0) { bx0 = x; by0 = y; ax0 = dx / d; ay0 = dy / d; cote = px * nx + py * ny > 0 ? -1 : 1; }      // le côté du bouchon : c'est là que sont les anneaux
                int a = i * 2, b = (2 * Pas + 1 - i) * 2;
                bord[a] = x + px * (l + 3.5); bord[a + 1] = y + py * (l + 3.5); bord[b] = x - px * (l + 3.5); bord[b + 1] = y - py * (l + 3.5);
                corps[a] = x + px * l; corps[a + 1] = y + py * l; corps[b] = x - px * l; corps[b + 1] = y - py * l;
                ombre[a] = x + px * l * cote; ombre[a + 1] = y + py * l * cote; ombre[b] = x + px * l * 0.3 * cote; ombre[b + 1] = y + py * l * 0.3 * cote;
                reflet[a] = x - px * l * 0.5 * cote; reflet[a + 1] = y - py * l * 0.5 * cote;
                // les nœuds du bambou, les anneaux
                if (i % 3 == 1 && i / 3 < _noeuds.Length) { var k = _noeuds[i / 3]; k.Points = new[] { x + px * l, y + py * l, x - px * l, y - py * l }; k.Etat++; }
                int quel = i == 6 ? 0 : i == 12 ? 1 : i == Pas ? 2 : -1;
                if (quel >= 0) { var an = _anneaux[quel]; an.X = x + px * (l + 8) * cote; an.Y = y + py * (l + 8) * cote; an.E = Mel(1.05, 0.6, w); }
                if (i == Pas) { _px = x + px * (l + 8) * cote; _py = y + py * (l + 8) * cote; }
            }
            foreach (var (o, pts) in new[] { (_canneBord, bord), (_canneCorps, corps), (_canneOmbre, ombre), (_canneReflet, reflet) }) { o.Points = pts; o.Etat++; }
            double boutX = _px, boutY = _py;
            // là où pend le bouchon quand il n'est pas sur l'eau : sous le bout de la canne
            _px = boutX + 10 * Math.Sin(_temps * 1.9); _py = boutY + 170;

            // la poignée, dans l'axe du bas de la canne ; le moulinet et la main qui le tourne
            double angle = Math.Atan2(ax0, -ay0) * 180 / Math.PI;
            _poignee.X = bx0; _poignee.Y = by0; _poignee.R = angle; _poignee.E = TaillePoignee;
            double c = Math.Cos(angle * Math.PI / 180), s = Math.Sin(angle * Math.PI / 180);
            double ox = -98 * TaillePoignee, oy = 110 * TaillePoignee, axeX = bx0 + ox * c - oy * s, axeY = by0 + ox * s + oy * c;
            if (_etat == Etat.Peche && _mode == Mode.Flotte) _tour += dt * 20 * Math.Sin(_temps * 0.7);
            _manivelle.X = axeX; _manivelle.Y = axeY; _manivelle.R = _tour; _manivelle.E = TaillePoignee;
            double ta = _tour * Math.PI / 180;
            _gantManivelle.X = axeX + 64 * TaillePoignee * Math.Sin(ta); _gantManivelle.Y = axeY - 64 * TaillePoignee * Math.Cos(ta); _gantManivelle.E = TaillePoignee; _gantManivelle.R = -8;

            // la ligne : du bout de la canne au bouchon (ou au poisson qui s'envole), tendue quand ça tire
            double fx = _bouchon.X, fy = _bouchon.Y - 116 * _bouchon.SY;
            if (_mode == Mode.Suit && _bouchon.Op < 0.5) { fx = _bx + 40; fy = _by; }
            double mou = (1 - _charge) * Mel(46, 26, prof) + 5 * Math.Sin(_temps * 1.4);
            if (!surLEau) mou = 6;
            double qx = (boutX + fx) / 2 + 8 * Math.Sin(_temps * 1.1) * (1 - _charge), qy = (boutY + fy) / 2 + mou;
            var fil = new double[26];
            for (int i = 0; i <= 12; i++) { double u = i / 12.0, v = 1 - u; fil[i * 2] = v * v * boutX + 2 * v * u * qx + u * u * fx; fil[i * 2 + 1] = v * v * boutY + 2 * v * u * qy + u * u * fy; }
            _filBord.Points = fil; _filBord.Etat++; _fil.Points = fil; _fil.Etat++;
        }

        // ------------------------------------------------------------------
        //  Le seau et ses poissons, les bottes qui se balancent
        // ------------------------------------------------------------------
        private void AvanceSeau(double dt)
        {
            for (int i = 0; i < _tetes.Count; i++)
            {
                var t = _tetes[i];
                t.Age += dt;
                double saute = _etat == Etat.Fete || _etat == Etat.Relache ? 34 * Math.Abs(Math.Sin(_temps * 6.5 + i * 1.3)) : 0;
                t.O.X = t.X + 2 * Math.Sin(_temps * 1.6 + i * 2); t.O.Y = t.Y + 5 * Math.Sin(_temps * 2.4 + i * 1.9) - saute + 40 * (1 - Rebond(t.Age / 0.6));
                t.O.R = 7 * Math.Sin(_temps * 2 + i * 1.4);
            }
            // les bottes se balancent au bord du ponton, et tapent de joie à chaque poisson
            _tape = Math.Max(0, _tape - dt * 1.4);
            for (int i = 0; i < _bottes.Length; i++)
            {
                double balance = Math.Sin(_temps * 1.5 + i * Math.PI) * 0.07 + _tape * 0.16 * Math.Sin(_temps * 22 + i * Math.PI);
                _bottes[i].SY = 0.9 * (0.92 + balance); _bottes[i].SX = 0.9;
                _bottes[i].R = 3 * Math.Sin(_temps * 1.5 + i * Math.PI);
            }
        }
    }
}
