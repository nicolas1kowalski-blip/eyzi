using System;
using System.Collections.Generic;
using System.Globalization;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le moteur du jeu « Patatou ».
    //
    //  Patatou est au milieu, sur son estrade. À gauche, les sept familles de pièces
    //  (yeux, nez, bouche, chapeau, cou, main, pieds) ; à droite, sur la même page, les
    //  pièces de la famille ouverte. L'enfant regarde une pièce : elle s'envole de sa
    //  carte et vient se poser sur Patatou, qui réagit. Dès qu'il a des yeux, un nez
    //  et une bouche, il prend vie : il cligne des yeux, suit l'enfant du regard,
    //  parle, salue, danse. En bas : la surprise, la photo, tout enlever.
    //
    //  Comme les autres jeux « dessinés », le moteur ne connaît ni l'écran ni
    //  Windows (voir VecMoteur.cs) : le banc d'essai en tire de vraies images. Les
    //  cartes et les boutons sont des « zones » : l'application pose dessus des
    //  boutons invisibles, que le regard choisit comme partout ailleurs.
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class PatateMoteur : VecMoteur
    {
        private const double CX = 615, CY = 462, SolY = 740;        // le milieu du corps, le dessus de l'estrade
        private const string Peau = "#EDBE64", Encre = "#2A1C4E";
        private const string Consigne = "Voici Patatou ! Choisis ses yeux, son nez, sa bouche… et il prend vie !";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // Les calques, du fond vers le devant.
        public readonly VecCalque Ciel = new VecCalque(), Decor = new VecCalque(), Corps = new VecCalque(), Interface = new VecCalque(), Effets = new VecCalque(), Devant = new VecCalque();
        private readonly VecCalque[] _calques;
        public override VecCalque[] Calques => _calques;
        public override Dictionary<string, VecForme[]> Dessins => PatateArt.Dessins;
        protected override VecCalque CalqueDesMots => Effets;
        public override double Horizon => 660;

        /// <summary>Le prénom de l'enfant (Patatou lui dit bonjour).</summary>
        public string Prenom = "toi";

        // ───────────── Les pièces ─────────────
        private sealed class Oeil { public double X, Y, RX, RY, E; public Membre Pupille; }
        private sealed class Piece
        {
            public string Cle, Nom, Pupille; public double X0, Y0, X1, Y1, Course, TX, TY; public string Effet = "";
            public readonly List<double[]> Yeux = new List<double[]>();
        }
        private sealed class Bout { public VecObjet O; public double X, Y, E = 1; public string Role = "base"; }
        private sealed class Famille
        {
            public string Cle, Clair, Fonce, Icone; public double AX, AY; public int Choix = -1; public double Age = 9;
            public readonly List<Piece> Pieces = new List<Piece>();
            public readonly List<Membre> Portes = new List<Membre>();        // ce que Patatou porte de cette famille
            public readonly List<VecObjet> Libres = new List<VecObjet>();     // … et ce qui ne suit pas son corps (l'objet en main, les chaussures)
            public VecObjet Coche;
        }
        private sealed class Vol { public Famille F; public int Index; public List<Bout> Bouts; public double T, D, X0, Y0, S0; public bool DuCiel; }
        private sealed class Chute { public VecObjet O; public double VX, VY, VR, Age; }
        private sealed class Photo { public readonly List<VecObjet> Objets = new List<VecObjet>(); public readonly List<double[]> Places = new List<double[]>(); public double X, Y, Age; }

        private readonly List<Famille> _familles = new List<Famille>();
        private Famille F(string cle) => _familles.Find(f => f.Cle == cle);
        private readonly List<Vol> _vols = new List<Vol>();
        private readonly List<Chute> _chutes = new List<Chute>();
        private readonly List<Photo> _photos = new List<Photo>();
        private readonly List<Oeil> _yeux = new List<Oeil>();

        // ───────────── Les zones que le regard peut choisir ─────────────
        private readonly List<VecZone> _zones = new List<VecZone>();
        private int _etatZones;
        public override List<VecZone> Zones => _zones;
        public override int EtatDesZones => _etatZones;

        // ───────────── Patatou ─────────────
        private readonly Groupe _g = new Groupe();
        private readonly VecObjet _rCape, _rPieds, _rCou, _rBouche, _rNez, _rYeux, _rYeuxDevant, _rChapeau, _rObjet;      // des repères dans le calque : où glisser chaque pièce
        private readonly VecObjet _pousse, _joues, _mainG, _mainD, _piedG, _piedD, _actif, _choisie, _flash, _soleil, _rayons, _lueur;
        private readonly VecObjet[] _jambes, _bras, _paupieres, _cils, _nuages;
        private readonly List<VecObjet> _cartes = new List<VecObjet>();
        private readonly Bac _etoiles, _coeurs, _confettis;
        private int _courante = -1;
        private bool _ouvert, _vivant, _dejaVivant;
        private double _eveil = -1, _depuisBravo = 99, _sansRien, _avance = -1, _surprise = -1, _clignement, _prochainClin = 2, _nu, _pose, _humeur = 3, _danse, _tape, _flashT;
        private int _surpriseFaite, _humeurGenre;
        // le saut, l'écrasement, les mains
        private double _sautT = 9, _sautD = .5, _sautH, _sauts, _ecrase, _hauteur, _vitesse, _chapeauDY, _penche;
        private double _gx = -214, _gy = 92, _dx = 214, _dy = 92, _ox, _oy;       // les mains (dans le repère du corps), le regard de Patatou
        private string _geste = "repos"; private double _gesteT;

        public PatateMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Ciel, Decor, Corps, Interface, Effets, Devant };
            var ciel = PatateArt.Ciels["jardin"]; CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            LisLesPieces();

            _lueur = Pose(Ciel, "lueur", 880, 130, 0.8); _rayons = Pose(Ciel, "rayons", 880, 130, 0.8); _soleil = Pose(Ciel, "soleil", 880, 130, 0.8);
            _nuages = new[] { Pose(Ciel, "nuage", 330, 170, 0.6), Pose(Ciel, "nuage", 760, 260, 0.42), Pose(Ciel, "nuage", 1020, 120, 0.5) };
            foreach (var p in PatateArt.Scenes["jardin"]) Decor.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = p.Dessin }.En(p.X, p.Y, p.E, p.R));

            // --- Patatou : la cape derrière, les jambes et les pieds, le corps, le visage, le chapeau, les bras et les mains ---
            _rCape = Repere();
            _jambes = new[] { Trait(Encre, 25), Trait(Peau, 16), Trait(Encre, 25), Trait(Peau, 16) };
            _piedG = Pose(Corps, "patate_pied", 0, 0); _piedD = Pose(Corps, "patate_pied", 0, 0);
            _rPieds = Repere();
            _g.Ajoute(Pose(Corps, "patate", 0, 0));
            _pousse = Pose(Corps, "patate_pousse", 0, 0); _g.Ajoute(_pousse);
            _joues = Pose(Corps, "patate_joues", 0, 0); _g.Ajoute(_joues); _joues.Op = 0;
            _rCou = Repere(); _rBouche = Repere(); _rNez = Repere(); _rYeux = Repere();
            _paupieres = new[] { Plein(Peau), Plein(Peau) };
            _cils = new[] { Trait(Encre, 5), Trait(Encre, 5) };
            _rYeuxDevant = Repere(); _rChapeau = Repere();
            _bras = new[] { Trait(Encre, 27), Trait(Peau, 18), Trait(Encre, 27), Trait(Peau, 18) };
            _rObjet = Repere();
            _mainG = Pose(Corps, "patate_main", 0, 0); _mainD = Pose(Corps, "patate_main", 0, 0);

            // --- Les panneaux : les familles à gauche, les pièces à droite, les trois boutons en bas ---
            Pose(Interface, "ui_rail", 8, 8); Pose(Interface, "ui_panneau", 1036, 8);
            for (int i = 0; i < _familles.Count; i++)
            {
                var f = _familles[i]; double x = 20, y = 22 + i * 123;
                Pose(Interface, "ui_onglet", x, y, 1, "c", f.Clair, "f", f.Fonce);
                Pose(Interface, "ui_pastille", x + 74, y + 56);
                int k = f.Pieces.FindIndex(p => p.Cle == f.Icone);
                var bouts = Assemble(f, Math.Max(0, k));
                Ajuste(bouts, f.Pieces[Math.Max(0, k)], x + 74, y + 56, 112, 80, 1.6);
                foreach (var b in bouts) Interface.Ajoute(b.O);
                f.Coche = Pose(Interface, "ui_coche", x + 132, y + 16); f.Coche.Op = 0;
            }
            _actif = Pose(Interface, "ui_onglet_actif", 20, 22);
            string[,] boutons = { { "surprise", "#FFA0C8", "#E0508C", "ic_surprise" }, { "photo", "#7FC4FF", "#2F7FE0", "ic_photo" }, { "nu", "#D5DEF0", "#8793AD", "ic_balai" } };
            for (int i = 0; i < 3; i++)
            {
                double x = 365 + i * 175, y = 808;
                Pose(Interface, "ui_bouton", x, y, 1, "c", boutons[i, 1], "f", boutons[i, 2]);
                Pose(Interface, boutons[i, 3], x + 75, y + 42, 1.05);
            }
            _choisie = Dessin("ui_carte_choisie"); _choisie.Op = 0;

            _etoiles = new Bac(this, Effets, "etoile", 60);
            _coeurs = new Bac(this, Effets, "coeur", 16);
            _confettis = new Bac(this, Effets, "bulle", 20);
            _flash = Devant.Ajoute(new VecObjet { Genre = VecGenre.Plein, Couleur = "#FFFFFF", Points = new[] { -20.0, -20, W + 20, -20, W + 20, H + 20, -20, H + 20 }, Op = 0 });
        }

        private VecObjet Repere() => Corps.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = Encre, Epaisseur = 1, Op = 0 });
        private VecObjet Trait(string couleur, double epaisseur) => Corps.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = couleur, Epaisseur = epaisseur });
        private VecObjet Plein(string couleur) => Corps.Ajoute(new VecObjet { Genre = VecGenre.Plein, Couleur = couleur, Op = 0 });
        private static double Nombre(string s) => double.Parse(s, Inv);

        private void LisLesPieces()
        {
            foreach (var l in PatateArt.Listes["familles"])
            {
                var f = new Famille { Cle = l[0], Clair = l[1], Fonce = l[2], AX = Nombre(l[3]), AY = Nombre(l[4]), Icone = l[5] };
                foreach (var q in PatateArt.Listes[f.Cle])
                {
                    var p = new Piece { Cle = q[0], Nom = q[1], X0 = Nombre(q[2]), Y0 = Nombre(q[3]), X1 = Nombre(q[4]), Y1 = Nombre(q[5]) };
                    var mots = q[6].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (f.Cle == "yeux" && mots.Length == 2)
                    {
                        p.Pupille = mots[0]; p.Course = Nombre(mots[1]);
                        foreach (var o in q[7].Split(';')) { var v = o.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); p.Yeux.Add(new[] { Nombre(v[0]), Nombre(v[1]), Nombre(v[2]), Nombre(v[3]), Nombre(v[4]) }); }
                    }
                    else if (mots.Length > 0) { p.Effet = mots[0]; if (mots.Length == 3) { p.TX = Nombre(mots[1]); p.TY = Nombre(mots[2]); } }
                    f.Pieces.Add(p);
                }
                _familles.Add(f);
            }
        }

        /// <summary>Les dessins d'une pièce, dans son propre repère : le fond d'abord, le devant ensuite.</summary>
        private List<Bout> Assemble(Famille f, int i)
        {
            var p = f.Pieces[i]; var l = new List<Bout>();
            if (Dessins.ContainsKey(p.Cle + "_derriere")) l.Add(new Bout { O = Dessin(p.Cle + "_derriere"), Role = "derriere" });
            l.Add(new Bout { O = Dessin(p.Cle) });
            foreach (var y in p.Yeux) l.Add(new Bout { O = Dessin(p.Pupille), X = y[0], Y = y[1], E = y[4], Role = "pupille" });
            if (Dessins.ContainsKey(p.Cle + "_devant")) l.Add(new Bout { O = Dessin(p.Cle + "_devant"), Role = "devant" });
            if (p.Effet == "tourne") l.Add(new Bout { O = Dessin(p.Cle + "_tourne"), X = p.TX, Y = p.TY, Role = "tourne" });
            return l;
        }
        private static void Place(List<Bout> l, double x, double y, double s, double r = 0)
        {
            double a = r * Math.PI / 180, c = Math.Cos(a), si = Math.Sin(a);
            foreach (var b in l) { b.O.X = x + (b.X * c - b.Y * si) * s; b.O.Y = y + (b.X * si + b.Y * c) * s; b.O.E = b.E * s; if (b.Role != "tourne") b.O.R = r; }
        }
        /// <summary>Met une pièce à la taille d'une case, centrée dedans ; rend l'échelle choisie.</summary>
        private static double Ajuste(List<Bout> l, Piece p, double cx, double cy, double largeur, double hauteur, double maxi)
        {
            double s = Math.Min(maxi, Math.Min(largeur / (p.X1 - p.X0), hauteur / (p.Y1 - p.Y0)));
            Place(l, cx - (p.X0 + p.X1) / 2 * s, cy - (p.Y0 + p.Y1) / 2 * s, s);
            return s;
        }

        // ------------------------------------------------------------------
        //  Ouvrir le jeu, ouvrir une famille
        // ------------------------------------------------------------------
        public override void Demarre(int scene = 0)
        {
            if (_ouvert) return;
            _ouvert = true;
            Ouvre(0);
            Dit(Consigne);
        }
        public override bool ProchaineCible(out double x, out double y) { x = y = 0; return false; }

        private static void CaseDe(int i, out double x, out double y) { x = 1049 + (i % 3) * 178; y = 24 + (i / 3) * 214; }

        /// <summary>Montre à droite les pièces d'une famille.</summary>
        private void Ouvre(int k)
        {
            _courante = k; var f = _familles[k];
            foreach (var o in _cartes) Interface.Retire(o);
            _cartes.Clear(); Interface.Retire(_choisie);
            for (int i = 0; i <= f.Pieces.Count; i++)
            {
                CaseDe(i, out double x, out double y);
                _cartes.Add(Interface.Ajoute(Dessin("ui_carte").En(x, y)));
                if (i < f.Pieces.Count)
                {
                    var bouts = Assemble(f, i);
                    Ajuste(bouts, f.Pieces[i], x + 85, y + 100, 138, 150, 1.5);
                    foreach (var b in bouts) _cartes.Add(Interface.Ajoute(b.O));
                }
                else _cartes.Add(Interface.Ajoute(Dessin("ui_rien").En(x + 85, y + 100)));        // la dernière carte : enlever
            }
            Interface.Ajoute(_choisie);
            _actif.Y = 22 + k * 123;
            MarqueLeChoix();
            RefaisLesZones();
        }
        private void MarqueLeChoix()
        {
            var f = _familles[_courante];
            if (f.Choix < 0) { _choisie.Op = 0; return; }
            CaseDe(f.Choix, out double x, out double y); _choisie.X = x; _choisie.Y = y; _choisie.Op = 1;
        }
        private void RefaisLesZones()
        {
            _zones.Clear();
            for (int i = 0; i < _familles.Count; i++) _zones.Add(new VecZone { Id = "f:" + i, X = 20, Y = 22 + i * 123, L = 148, H = 110 });
            for (int i = 0; i <= _familles[_courante].Pieces.Count; i++) { CaseDe(i, out double x, out double y); _zones.Add(new VecZone { Id = "c:" + i, X = x, Y = y, L = 170, H = 200 }); }
            string[] noms = { "surprise", "photo", "nu" };
            for (int i = 0; i < 3; i++) _zones.Add(new VecZone { Id = noms[i], X = 365 + i * 175, Y = 808, L = 150, H = 82 });
            _etatZones++;
        }

        // ------------------------------------------------------------------
        //  Ce que l'enfant choisit
        // ------------------------------------------------------------------
        public override void Appuie(string id)
        {
            if (!_ouvert || id == null) return;
            _sansRien = 0;
            if (id.StartsWith("f:")) { int k = int.Parse(id.Substring(2)); if (k >= 0 && k < _familles.Count) { _avance = -1; Ouvre(k); Son("pop", 0); } return; }
            if (id.StartsWith("c:"))
            {
                var f = _familles[_courante]; int i = int.Parse(id.Substring(2));
                if (i < 0 || i > f.Pieces.Count) return;
                if (i == f.Pieces.Count) { if (f.Choix >= 0) { Enleve(f, true); Son("pop", 0); MarqueLeChoix(); RegardeSiVivant(); } return; }
                if (i == f.Choix) { if (_vivant) Saute(30, .36, 1); return; }      // il la porte déjà
                Choisis(f, i, false);
                return;
            }
            if (_surprise >= 0) return;
            if (id == "surprise") { _surprise = 0; _surpriseFaite = 0; _avance = -1; Son("tambour", 0); Dit("Roulement de tambour…"); Geste("haut", 1.2); }
            else if (id == "photo") PrendsLaPhoto();
            else if (id == "nu") ToutEnlever();
        }

        /// <summary>La pièce quitte sa carte (ou tombe du ciel) et vole jusqu'à Patatou.</summary>
        private void Choisis(Famille f, int i, bool duCiel)
        {
            // un vol déjà parti pour cette famille se pose tout de suite
            for (int v = _vols.Count - 1; v >= 0; v--) if (_vols[v].F == f) { Atterris(_vols[v], false); _vols.RemoveAt(v); }
            var p = f.Pieces[i];
            var vol = new Vol { F = f, Index = i, Bouts = Assemble(f, i), D = duCiel ? 0.42 : 0.46, DuCiel = duCiel };
            Ancre(f, out double ax, out double ay);
            if (duCiel) { vol.X0 = ax + Entre(-60, 60); vol.Y0 = -160; vol.S0 = 1; }
            else
            {
                CaseDe(i, out double x, out double y);
                vol.S0 = Math.Min(1.5, Math.Min(138 / (p.X1 - p.X0), 150 / (p.Y1 - p.Y0)));
                vol.X0 = x + 85 - (p.X0 + p.X1) / 2 * vol.S0; vol.Y0 = y + 100 - (p.Y0 + p.Y1) / 2 * vol.S0;
            }
            foreach (var b in vol.Bouts) Effets.Ajoute(b.O);
            Place(vol.Bouts, vol.X0, vol.Y0, vol.S0);
            _vols.Add(vol);
            f.Choix = i; f.Coche.Op = 1;
            if (f == _familles[_courante]) MarqueLeChoix();
            if (!duCiel) { Son("pop", 0); Dit(Majuscule(p.Nom) + " !"); }
        }
        private static string Majuscule(string s) => char.ToUpper(s[0]) + s.Substring(1);

        /// <summary>Où une famille s'attache, dans l'image.</summary>
        private void Ancre(Famille f, out double x, out double y)
        {
            if (f.Cle == "main") { _g.Monde(_dx, _dy, out x, out y); return; }
            if (f.Cle == "pieds") { x = CX; y = SolY - 30; return; }
            _g.Monde(f.AX, f.AY, out x, out y);
        }

        private void Atterris(Vol v, bool fete)
        {
            foreach (var b in v.Bouts) Effets.Retire(b.O);
            Porte(v.F, v.Index);
            if (!fete) return;
            Ancre(v.F, out double x, out double y);
            for (int i = 0; i < 7; i++) { double a = Math.PI * 2 * i / 7 + Hasard(); _etoiles.Lance(x, y, Math.Cos(a) * 240 * Entre(.6, 1), Math.Sin(a) * 240 * Entre(.6, 1) - 60, 420, Entre(.45, .75), Entre(.7, 1.2), 0, Entre(-200, 200)); }
            _ecrase = 1;
            var p = v.F.Pieces[v.Index];
            if (!v.DuCiel)
            {
                // sa façon de réagir dépend de la pièce
                if (p.Cle == "n_clown" || p.Cle == "c_clown" || p.Cle == "p_clown" || p.Cle == "b_clown") Son("pouet", 0); else Son("note", 3 + _familles.IndexOf(v.F));
                if (_vivant)
                {
                    Saute(46, .42, v.F.Cle == "pieds" ? 3 : 1);
                    if (v.F.Cle == "chapeau" || v.F.Cle == "cou") Geste("haut", 1.0);
                    else if (v.F.Cle == "main") Geste("montre", 1.6);
                    else if (v.F.Cle == "pieds") _tape = 1.6;
                    else if (p.Cle == "b_bisou") { Geste("salut", 1.2); for (int i = 0; i < 4; i++) _coeurs.Lance(x + Entre(-30, 30), y, Entre(-70, 70), Entre(-240, -150), 60, 1.2, Entre(.8, 1.3)); }
                    else Geste("hanches", 1.0);
                }
                // au début, on passe tout seul des yeux au nez, puis à la bouche
                if (!_dejaVivant) _avance = 1.1;
            }
            RegardeSiVivant();
        }

        /// <summary>Pose une pièce sur Patatou (et range celle qu'il portait).</summary>
        private void Porte(Famille f, int i)
        {
            Enleve(f, false);
            f.Choix = i; f.Age = 0; f.Coche.Op = 1;
            var p = f.Pieces[i];
            if (f.Cle == "pieds")
            {
                for (int k = 0; k < 2; k++) { var o = Dessin(p.Cle); Corps.Avant(_rPieds, o); f.Libres.Add(o); }
                _piedG.Op = _piedD.Op = 0;
            }
            else if (f.Cle == "main")
            {
                foreach (var b in Assemble(f, i)) { Corps.Avant(_rObjet, b.O); f.Libres.Add(b.O); }
            }
            else
            {
                var repere = f.Cle == "yeux" ? _rYeux : f.Cle == "nez" ? _rNez : f.Cle == "bouche" ? _rBouche : f.Cle == "chapeau" ? _rChapeau : _rCou;
                if (f.Cle == "yeux") _yeux.Clear();
                foreach (var b in Assemble(f, i))
                {
                    Corps.Avant(b.Role == "derriere" ? _rCape : b.Role == "devant" ? _rYeuxDevant : repere, b.O);
                    var m = _g.Ajoute(b.O, f.AX + b.X, f.AY + b.Y, b.E);
                    f.Portes.Add(m);
                    if (b.Role == "pupille") { var y = p.Yeux[_yeux.Count]; _yeux.Add(new Oeil { X = f.AX + y[0], Y = f.AY + y[1], RX = y[2], RY = y[3], E = y[4], Pupille = m }); }
                }
                if (f.Cle == "chapeau") _pousse.Op = 0;
            }
            if (f == _familles[_courante]) MarqueLeChoix();
        }

        /// <summary>Retire ce que Patatou porte d'une famille ; « tombe » : la pièce saute et tombe par terre.</summary>
        private void Enleve(Famille f, bool tombe)
        {
            var objets = new List<VecObjet>();
            foreach (var m in f.Portes) { objets.Add(m.O); _g.Membres.Remove(m); }
            objets.AddRange(f.Libres);
            f.Portes.Clear(); f.Libres.Clear();
            foreach (var o in objets)
            {
                Corps.Retire(o);
                if (tombe) { Effets.Ajoute(o); _chutes.Add(new Chute { O = o, VX = Entre(-260, 260), VY = Entre(-520, -300), VR = Entre(-300, 300) }); }
            }
            if (f.Cle == "yeux") _yeux.Clear();
            if (f.Cle == "pieds") _piedG.Op = _piedD.Op = 1;
            if (f.Cle == "chapeau") _pousse.Op = 1;
            f.Choix = -1; f.Coche.Op = 0;
        }

        private bool AUnVisage => F("yeux").Choix >= 0 && F("nez").Choix >= 0 && F("bouche").Choix >= 0;

        /// <summary>Des yeux, un nez, une bouche : Patatou prend vie. Il en manque : il se rendort.</summary>
        private void RegardeSiVivant()
        {
            if (AUnVisage) { if (!_vivant && _eveil < 0) _eveil = 0; }
            else if (_vivant) { _vivant = false; _eveil = -1; Geste("repos", 0); }
        }

        private void ToutEnlever()
        {
            foreach (var v in _vols) foreach (var b in v.Bouts) Effets.Retire(b.O);
            _vols.Clear();
            bool avait = false;
            foreach (var f in _familles) { if (f.Choix >= 0) avait = true; Enleve(f, true); }
            MarqueLeChoix();
            _vivant = false; _eveil = -1; _avance = -1; _dejaVivant = false;
            if (!avait) return;
            Son("pof", 0);
            Dit("Oh ! Patatou est tout nu !");
            _nu = 2.2; Geste("croise", 2.2);
            Ouvre(0);
        }

        private void PrendsLaPhoto()
        {
            _flashT = 1; Son("clic", 0);
            Demande("photo");
            Dit("Clic-clac ! La photo de Patatou est dans l'album.");
            if (_vivant) { Geste("haut", 1.4); Saute(30, .4, 1); }
            Reussites++;
            // une petite photo de plus sur le fil : Patatou comme il est maintenant
            double[] places = { 262, 384, 846, 968 };
            if (_photos.Count >= places.Length) { foreach (var o in _photos[0].Objets) Decor.Retire(o); _photos.RemoveAt(0); }
            var ph = new Photo();
            const double k = 0.142;
            void Ajoute(VecObjet o, double x, double y, double e) { Decor.Ajoute(o); ph.Objets.Add(o); ph.Places.Add(new[] { x, y, e }); }
            Ajoute(Dessin("ui_polaroid"), 0, 0, 1);
            foreach (var f in _familles) if (f.Choix >= 0 && f.Cle == "cou") foreach (var b in Assemble(f, f.Choix)) if (b.Role == "derriere") Ajoute(b.O, (f.AX + b.X) * k, (f.AY + b.Y) * k - 6, b.E * k);
            Ajoute(Dessin("patate"), 0, -6, k);
            foreach (var f in _familles)
            {
                if (f.Choix < 0 || f.Cle == "main" || f.Cle == "pieds") continue;
                foreach (var b in Assemble(f, f.Choix)) if (b.Role != "derriere") Ajoute(b.O, (f.AX + b.X) * k, (f.AY + b.Y) * k - 6, b.E * k);
            }
            _photos.Add(ph);
            for (int i = 0; i < _photos.Count; i++) { double x = places[i], u = (x - 196) / 838; _photos[i].X = x; _photos[i].Y = 40 + 44 * 4 * u * (1 - u) + 62; }
        }

        private void Saute(double hauteur, double duree, int fois) { _sautH = hauteur; _sautD = duree; _sauts = fois; _sautT = 0; }
        private void Geste(string geste, double duree) { _geste = geste; _gesteT = duree; }

        // ------------------------------------------------------------------
        //  Une image du jeu
        // ------------------------------------------------------------------
        public override void Avance(double dt)
        {
            if (dt <= 0 || !_ouvert) return;
            dt = Math.Min(dt, 0.1);
            _temps += dt;
            bool regarde = _temps - _vu < 1.2;

            _rayons.R += dt * 12; _lueur.E = 0.8 * (1 + .05 * Math.Sin(_temps * 1.7));
            for (int i = 0; i < _nuages.Length; i++) { var n = _nuages[i]; n.X += dt * (5 + i * 2); if (n.X > 1140) n.X = 80; }

            // --- Le déroulé : le coup de pouce, le passage d'une famille à la suivante, la surprise, l'éveil ---
            _sansRien += dt; _depuisBravo += dt;
            if (_sansRien > 22 && !_vivant && !VoixOccupee()) { _sansRien = 0; Dit(Consigne); }
            if (_avance >= 0)
            {
                _avance -= dt;
                if (_avance < 0)
                {
                    int suivante = _familles.FindIndex(f => (f.Cle == "yeux" || f.Cle == "nez" || f.Cle == "bouche") && f.Choix < 0);
                    if (suivante >= 0 && suivante != _courante) Ouvre(suivante);
                }
            }
            if (_surprise >= 0)
            {
                // après le roulement de tambour, une pièce de chaque famille tombe du ciel
                _surprise += dt;
                while (_surpriseFaite < _familles.Count && _surprise >= 0.9 + _surpriseFaite * 0.2)
                {
                    var f = _familles[_surpriseFaite++];
                    Choisis(f, _alea.Next(f.Pieces.Count), true);
                }
                if (_surprise >= 0.9 + _familles.Count * 0.2 + 0.5)
                {
                    _surprise = -1;
                    Son("boum", 0);
                    Dit("Surprise ! " + Majuscule(Nom("yeux")) + ", " + Nom("nez") + ", " + Nom("bouche") + " et " + Nom("chapeau") + " !");
                    Saute(70, .5, 2); Geste("haut", 1.6);
                    for (int i = 0; i < 16; i++) _etoiles.Lance(CX + Entre(-220, 220), CY + Entre(-260, 100), Entre(-80, 80), Entre(-200, -40), 300, Entre(.6, 1), Entre(.8, 1.4), 0, Entre(-200, 200));
                }
            }
            if (_eveil >= 0)
            {
                double avant = _eveil; _eveil += dt;
                if (avant == 0) Son("sifflet", 0);
                if (_eveil < 0.8 && Hasard() < dt * 40) { double a = _eveil * 14 + Hasard(); _etoiles.Lance(CX + 200 * Math.Cos(a), CY + 160 - 420 * _eveil, 0, -60, 0, .6, Entre(.6, 1.1), 0, 200); }
                if (avant < 0.8 && _eveil >= 0.8)
                {
                    _vivant = true; _dejaVivant = true; _avance = -1;
                    Saute(110, .6, 1); Geste("haut", 1.4); _humeur = 4;
                    Dit("Oh ! Patatou prend vie ! Bonjour " + Prenom + ", je m'appelle Patatou ! Je suis trop beau !");
                    Reussites++;
                    if (_depuisBravo > 25) { _depuisBravo = 0; Bravo(); }
                    for (int i = 0; i < 18; i++) { double a = Math.PI * 2 * i / 18; _etoiles.Lance(CX, CY - 40, Math.Cos(a) * 380, Math.Sin(a) * 380 - 80, 300, Entre(.6, 1), Entre(.8, 1.5), 0, Entre(-200, 200)); }
                }
                if (_eveil >= 2.4) { _eveil = -1; Geste("salut", 1.6); }
            }

            AvanceCorps(dt, regarde);
            AvanceVols(dt);
            AvanceChutes(dt);
            AvancePhotos(dt);
            if (_flashT > 0) { _flashT = Math.Max(0, _flashT - dt * 2.4); _flash.Op = _flashT * _flashT; }
            _etoiles.Avance(dt); _coeurs.Avance(dt); _confettis.Avance(dt);
            AvanceMots(dt);
        }
        private string Nom(string famille) { var f = F(famille); return f.Choix >= 0 ? f.Pieces[f.Choix].Nom : "rien"; }

        // ------------------------------------------------------------------
        //  Patatou bouge
        // ------------------------------------------------------------------
        private void AvanceCorps(double dt, bool regarde)
        {
            // --- Ses petites envies quand il est vivant : saluer, sautiller, danser ---
            if (_vivant && _eveil < 0 && _surprise < 0 && _nu <= 0)
            {
                _humeur -= dt;
                if (_humeur <= 0)
                {
                    _humeur = Entre(4.5, 8); _humeurGenre = (_humeurGenre + 1 + _alea.Next(2)) % 4;
                    if (_humeurGenre == 0) Geste("salut", 1.8);
                    else if (_humeurGenre == 1) Saute(40, .4, 2);
                    else if (_humeurGenre == 2) { _danse = 2.4; Geste("danse", 2.4); }
                    else { Geste("hanches", 1.6); _tape = 1.2; }
                }
            }
            if (_danse > 0) _danse -= dt;
            if (_tape > 0) _tape -= dt;
            if (_nu > 0) _nu -= dt;
            if (_gesteT > 0) { _gesteT -= dt; if (_gesteT <= 0) _geste = "repos"; }

            // --- Le saut, l'écrasement à la réception ---
            double avant = _hauteur;
            if (_sautT < _sautD) { _sautT += dt; double u = Borne(_sautT / _sautD); _hauteur = _sautH * 4 * u * (1 - u); if (u >= 1) { _hauteur = 0; _ecrase = 1; if (--_sauts > 0) _sautT = 0; } }
            else _hauteur = 0;
            _vitesse = (_hauteur - avant) / dt;
            _ecrase = Math.Max(0, _ecrase - dt * 4.5);
            double ecrase = 0.1 * _ecrase * Math.Sin(_ecrase * Math.PI), respire = Math.Sin(_temps * 2.2), frisson = _nu > 0 ? 5 * Math.Sin(_temps * 44) : 0;
            double tremble = _eveil >= 0 && _eveil < 0.8 ? 4 * Math.Sin(_temps * 50) : 0;
            double balance = _danse > 0 ? Math.Sin(_temps * 7.5) : 0;
            _penche += ((_vivant ? 2.2 * Math.Sin(_temps * 1.3) : 0) + 9 * balance - _penche) * Math.Min(1, dt * 8);
            _g.X = CX + frisson + tremble + 16 * balance; _g.Y = CY - _hauteur + 216 * ecrase + (_vivant ? 2 * respire : 0);
            _g.SX = 1 + ecrase + .008 * respire; _g.SY = 1 - ecrase - .012 * respire; _g.R = _penche;

            // --- Les pièces : elles rebondissent en arrivant ; le chapeau décolle un peu quand il retombe ---
            foreach (var f in _familles)
            {
                f.Age += dt;
                double pop = f.Age < 0.5 ? Rebond(f.Age / 0.45) : 1;
                foreach (var m in f.Portes) { m.SX = m.SY = pop; }
            }
            _chapeauDY += (Borne(-_vitesse * 0.045, -4, 26) - _chapeauDY) * Math.Min(1, dt * 14);
            var chapeau = F("chapeau");
            foreach (var m in chapeau.Portes) { m.Y = chapeau.AY - _chapeauDY; m.R = -5 + 1.5 * Math.Sin(_temps * 2) + 6 * balance; }
            var nez = F("nez");
            foreach (var m in nez.Portes) m.R = _vivant ? 4 * Math.Sin(_temps * 3.1) * Borne(Math.Sin(_temps * .7)) : 0;
            // la bouche remue quand il parle
            var bouche = F("bouche");
            bool parle = _vivant && VoixOccupee();
            foreach (var m in bouche.Portes) { double pop = bouche.Age < 0.5 ? Rebond(bouche.Age / 0.45) : 1; m.SY = pop * (parle ? 1 + .26 * Math.Sin(_temps * 21) : 1); m.SX = pop * (parle ? 1 - .06 * Math.Sin(_temps * 21) : 1); }
            var cou = F("cou");
            foreach (var m in cou.Portes) if (m.O.Dessin.EndsWith("_derriere")) { m.SX = 1 + .05 * Math.Sin(_temps * 3.3) + .08 * Borne(_hauteur / 60); m.SY = 1 + .04 * Math.Sin(_temps * 2.7 + 1) - .1 * Borne(_hauteur / 60); }
            _joues.Op += ((_vivant || _nu > 0 ? 1 : 0) - _joues.Op) * Math.Min(1, dt * 4);
            // la petite pousse sur sa tête frétille
            foreach (var m in _g.Membres) if (m.O == _pousse) m.R = 5 * Math.Sin(_temps * 2.6) + 10 * balance;

            // --- Ses yeux suivent l'enfant ; il cligne ; avant de vivre, ils sont à moitié fermés ---
            double vx = 0, vy = 0;
            if (_vivant)
            {
                double ex, ey; _g.Monde(0, -76, out ex, out ey);
                double tx = regarde ? _rx : CX + 300 * Math.Sin(_temps * .6), ty = regarde ? _ry : CY - 120 + 120 * Math.Sin(_temps * .9);
                double ddx = tx - ex, ddy = ty - ey, d = Math.Max(1, Math.Sqrt(ddx * ddx + ddy * ddy)), force = Math.Min(1, d / 160);
                vx = ddx / d * force; vy = ddy / d * force;
            }
            _ox += (vx - _ox) * Math.Min(1, dt * 9); _oy += (vy - _oy) * Math.Min(1, dt * 9);
            _prochainClin -= dt;
            if (_prochainClin <= 0) { _prochainClin = Entre(2.2, 5); _clignement = 1; }
            if (_clignement > 0) _clignement = Math.Max(0, _clignement - dt * 6.5);
            double ferme = _vivant ? Math.Sin(_clignement * Math.PI) : 0.52;
            if (_nu > 0) ferme = Math.Max(ferme, 0.25);
            var yeux = F("yeux"); var piece = yeux.Choix >= 0 ? yeux.Pieces[yeux.Choix] : null;
            for (int i = 0; i < 2; i++)
            {
                if (i >= _yeux.Count || ferme < 0.03 || yeux.Age < 0.1) { _paupieres[i].Op = 0; _cils[i].Op = 0; if (i < _yeux.Count) Pupille(_yeux[i], piece); continue; }
                var o = _yeux[i]; Pupille(o, piece);
                Paupiere(o, ferme, _paupieres[i], _cils[i]);
            }
            _g.Place();

            // --- Les jambes et les pieds : posés sur l'estrade, ils tapent quand il est content ---
            var pieds = F("pieds");
            for (int cote = -1; cote <= 1; cote += 2)
            {
                int k = cote < 0 ? 0 : 1;
                double leve = _hauteur + (_tape > 0 ? 16 * Math.Max(0, Math.Sin(_temps * 13 + k * Math.PI)) : 0) + (_danse > 0 ? 12 * Math.Max(0, cote * balance) : 0);
                double px = CX + cote * 84 + 16 * balance, py = SolY - 36 - leve;
                _g.Monde(cote * 66, 196, out double hx, out double hy);
                foreach (var j in new[] { _jambes[k * 2], _jambes[k * 2 + 1] }) { j.Points = new[] { hx, hy, px, py + 6 }; j.Etat++; }
                var nu = cote < 0 ? _piedG : _piedD;
                nu.X = px; nu.Y = py; nu.SX = cote; nu.SY = 1;
                if (pieds.Libres.Count == 2) { var s = pieds.Libres[k]; double pop = pieds.Age < 0.5 ? Rebond(pieds.Age / 0.45) : 1; s.X = px; s.Y = py; s.SX = cote * pop; s.SY = pop; s.R = cote * (_tape > 0 ? -8 * Math.Max(0, Math.Sin(_temps * 13 + k * Math.PI)) : 0); }
            }

            // --- Les bras : chaque geste dit où vont les mains ---
            double gx = -214, gy = 92, dx = 214, dy = 92;
            bool tient = F("main").Choix >= 0;
            if (tient) { dx = 222; dy = 4; }
            switch (_geste)
            {
                case "haut": gx = -196; gy = -214 + 10 * Math.Sin(_temps * 12); dx = 196; dy = -214 + 10 * Math.Cos(_temps * 12); break;
                case "salut": gx = -214; gy = 92; if (!tient) { dx = 226 + 34 * Math.Sin(_temps * 11); dy = -150; } else { gx = -226 + 34 * Math.Sin(_temps * 11); gy = -150; } break;
                case "hanches": gx = -150; gy = 96; if (!tient) { dx = 150; dy = 96; } break;
                case "croise": gx = 40; gy = 60; dx = -40; dy = 110; break;
                case "montre": dx = 232; dy = -90 + 16 * Math.Sin(_temps * 9); break;
                case "danse": gx = -214; gy = -60 - 110 * Math.Max(0, balance); dx = 214; dy = -60 - 110 * Math.Max(0, -balance); break;
            }
            if (!_vivant && _nu <= 0 && _eveil < 0) { gx = -198; gy = 120; dx = tient ? 214 : 198; dy = tient ? 30 : 120; }
            double suit = Math.Min(1, dt * 9);
            _gx += (gx - _gx) * suit; _gy += (gy - _gy) * suit; _dx += (dx - _dx) * suit; _dy += (dy - _dy) * suit;
            Bras(-1, _gx, _gy, _mainG, _bras[0], _bras[1]);
            Bras(1, _dx, _dy, _mainD, _bras[2], _bras[3]);

            // --- Ce qu'il tient dans la main ---
            var main = F("main");
            if (main.Libres.Count > 0)
            {
                var p = main.Pieces[main.Choix];
                double pop = main.Age < 0.5 ? Rebond(main.Age / 0.45) : 1;
                double r = _penche + 6 * Math.Sin(_temps * 2.4) + (p.Effet == "flotte" ? 5 * Math.Sin(_temps * 1.3) : 0) + (_geste == "montre" ? 10 * Math.Sin(_temps * 9) : 0);
                double a = r * Math.PI / 180;
                foreach (var o in main.Libres)
                {
                    bool roue = o.Dessin.EndsWith("_tourne");
                    double lx = roue ? p.TX : 0, ly = roue ? p.TY : 0;
                    o.X = _mainD.X + (lx * Math.Cos(a) - ly * Math.Sin(a)) * pop; o.Y = _mainD.Y + (lx * Math.Sin(a) + ly * Math.Cos(a)) * pop; o.E = pop;
                    if (roue) o.R += dt * 300; else o.R = r;
                }
                if (p.Effet == "brille" && Hasard() < dt * 7)
                {
                    double sx = _mainD.X + (p.TX * Math.Cos(a) - p.TY * Math.Sin(a)), sy = _mainD.Y + (p.TX * Math.Sin(a) + p.TY * Math.Cos(a));
                    _etoiles.Lance(sx + Entre(-36, 36), sy + Entre(-36, 36), Entre(-30, 30), Entre(-60, 10), 60, Entre(.4, .7), Entre(.35, .7), 0, 160);
                }
            }
        }

        private void Pupille(Oeil o, Piece p)
        {
            if (p == null) return;
            o.Pupille.X = o.X + _ox * p.Course * o.E; o.Pupille.Y = o.Y + _oy * p.Course * o.E;
            o.Pupille.SX = o.Pupille.SY = o.E;
        }

        /// <summary>La paupière : le haut du blanc de l'œil, jusqu'à une ligne qui descend quand l'œil se ferme.</summary>
        private void Paupiere(Oeil o, double ferme, VecObjet plein, VecObjet cil)
        {
            double rx = o.RX + 4, ry = o.RY + 4, yc = -ry + 2 * ry * Borne(ferme, 0, 0.999), a0 = Math.Asin(Borne(yc / ry, -1, 1));
            const int n = 16;
            var pts = new double[(n + 1) * 2];
            for (int i = 0; i <= n; i++)
            {
                double a = Math.PI - a0 + (Math.PI + 2 * a0) * i / n;
                _g.Monde(o.X + rx * Math.Cos(a), o.Y + ry * Math.Sin(a), out pts[i * 2], out pts[i * 2 + 1]);
            }
            plein.Points = pts; plein.Etat++; plein.Op = 1;
            cil.Points = new[] { pts[0], pts[1], pts[n * 2], pts[n * 2 + 1] }; cil.Etat++; cil.Op = 1;
        }

        private void Bras(int cote, double mx, double my, VecObjet main, VecObjet bord, VecObjet peau)
        {
            _g.Monde(cote * 150, 18, out double ex, out double ey);
            _g.Monde(mx, my, out double hx, out double hy);
            // le coude : le bras se plie vers le bas et vers l'extérieur
            double cx = (ex + hx) / 2 + cote * 26, cy = (ey + hy) / 2 + 34;
            var pts = new double[22];
            for (int i = 0; i <= 10; i++) { double u = i / 10.0, v = 1 - u; pts[i * 2] = v * v * ex + 2 * v * u * cx + u * u * hx; pts[i * 2 + 1] = v * v * ey + 2 * v * u * cy + u * u * hy; }
            bord.Points = pts; bord.Etat++; peau.Points = pts; peau.Etat++;
            main.X = hx; main.Y = hy; main.SX = cote; main.SY = 1;
            main.R = Math.Atan2(hx - cx, -(hy - cy)) * 180 / Math.PI;
        }

        // ------------------------------------------------------------------
        //  Ce qui vole, ce qui tombe, les photos sur le fil
        // ------------------------------------------------------------------
        private void AvanceVols(double dt)
        {
            for (int i = _vols.Count - 1; i >= 0; i--)
            {
                var v = _vols[i];
                v.T += dt;
                double u = Borne(v.T / v.D), w = v.DuCiel ? u * u : Doux(u);
                Ancre(v.F, out double ax, out double ay);
                double x = Mel(v.X0, ax, w), y = Mel(v.Y0, ay, w) - (v.DuCiel ? 0 : 150 * 4 * u * (1 - u)), s = Mel(v.S0, 1, w) * (1 + .25 * Math.Sin(Math.PI * u));
                Place(v.Bouts, x, y, s, v.DuCiel ? 0 : -24 * Math.Sin(Math.PI * u));
                foreach (var b in v.Bouts) if (b.Role == "tourne") b.O.R += dt * 300;
                if (Hasard() < dt * 30) _etoiles.Lance(x + Entre(-20, 20), y + Entre(-20, 20), 0, 40, 100, .4, Entre(.3, .6), 0, 200);
                if (u >= 1) { _vols.RemoveAt(i); Atterris(v, true); }
            }
        }

        private void AvanceChutes(double dt)
        {
            for (int i = _chutes.Count - 1; i >= 0; i--)
            {
                var c = _chutes[i];
                c.Age += dt; c.VY += 1700 * dt; c.O.X += c.VX * dt; c.O.Y += c.VY * dt; c.O.R += c.VR * dt;
                c.O.Op = Borne((1.0 - c.Age) * 3);
                if (c.Age >= 1.0) { Effets.Retire(c.O); _chutes.RemoveAt(i); }
            }
        }

        private void AvancePhotos(double dt)
        {
            foreach (var p in _photos)
            {
                p.Age += dt;
                // elle arrive en se balançant, puis le vent la berce
                double r = 26 * Math.Exp(-p.Age * 2.2) * Math.Cos(p.Age * 9) + 2.5 * Math.Sin(_temps * 1.4 + p.X), e = Rebond(p.Age / 0.5);
                double a = r * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
                for (int i = 0; i < p.Objets.Count; i++)
                {
                    // la photo tourne autour de sa pince, en haut
                    double lx = p.Places[i][0], ly = p.Places[i][1] + 62;
                    var o = p.Objets[i];
                    o.X = p.X + (lx * c - ly * s) * e; o.Y = p.Y - 62 + (lx * s + ly * c) * e; o.R = r; o.E = p.Places[i][2] * e;
                }
            }
        }
    }
}
