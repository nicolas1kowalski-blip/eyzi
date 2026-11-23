using System;
using System.Collections.Generic;
using System.Globalization;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le moteur de l'activité « Dans mon corps ».
    //
    //  Oscar, le squelette de la classe, pend à sa potence. Il lui manque ses organes :
    //  le cerveau, les poumons, le cœur, l'estomac, le foie, les intestins, la vessie.
    //  La voix en nomme un ; l'enfant le remet à sa place. Une fois posé, l'organe se
    //  met à vivre (le cœur bat, les poumons se gonflent…), la voix dit à quoi il sert,
    //  et son dessin se colore au tableau. Trois niveaux :
    //    0 — un seul endroit brille : il suffit de le regarder (sans échec) ;
    //    1 — trois endroits brillent : il faut trouver le bon ;
    //    2 — trois organes sont proposés : il faut reconnaître celui qu'on nomme.
    //  Oscar est articulé : il se balance, salue, hausse les épaules, danse et claque
    //  des dents. Il tourne un peu vers l'endroit qu'on regarde : la colonne derrière,
    //  les organes au milieu et les côtes devant glissent les uns sur les autres.
    //
    //  Comme les autres jeux « dessinés », le moteur ne connaît ni l'écran ni
    //  Windows (voir VecMoteur.cs) : le banc d'essai en tire de vraies images.
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class CorpsMoteur : VecMoteur
    {
        private const double AX = 800, Crochet = 44;        // l'axe d'Oscar, la hauteur du crochet où il pend
        private const double Relief = 17;                  // de combien glisse ce qui est tout devant quand il tourne
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public readonly VecCalque Decor = new VecCalque(), Corps = new VecCalque(), Interface = new VecCalque(), Effets = new VecCalque();
        private readonly VecCalque[] _calques;
        public override VecCalque[] Calques => _calques;
        public override Dictionary<string, VecForme[]> Dessins => CorpsArt.Dessins;
        protected override VecCalque CalqueDesMots => Effets;
        public override double Horizon => 700;

        /// <summary>Le niveau, de 0 à 2 (l'application le retient d'une fois sur l'autre).</summary>
        public int Niveau { get => _niveau; set => _niveau = Math.Max(0, Math.Min(2, value)); }
        private int _niveau;
        /// <summary>La manche qui vient de finir a-t-elle été réussie presque sans erreur ?</summary>
        public bool MancheReussie { get; private set; }
        public override int Etapes => _ordre.Count;
        public override int Etape => _rang;

        private sealed class Organe
        {
            public string Cle, Nom, Dit, Couleur; public double X, Y, L, H, Z, Taille = 1; public bool EnPlace; public double Age = 9, Chute, VX;
            public VecObjet O, Mini, Coche;
        }
        private sealed class Piece { public VecObjet O; public double X, Y, Z, R; }       // un morceau d'Oscar : sa place, sa profondeur, son angle
        private sealed class Lueur { public VecObjet Halo, Bord, Tirets, Non; public Organe Pour; public double Faux = -1; }
        private sealed class Carte { public VecObjet Fond, Image; public Organe Pour; public double X, Y, Faux = -1, E0; }

        private readonly List<Organe> _organes = new List<Organe>();
        private Organe O(string cle) => _organes.Find(o => o.Cle == cle);
        private readonly List<Piece> _pieces = new List<Piece>();
        private readonly List<Organe> _ordre = new List<Organe>();
        private readonly List<Lueur> _lueurs = new List<Lueur>();
        private readonly List<Carte> _cartes = new List<Carte>();
        private readonly List<VecObjet> _aRetirer = new List<VecObjet>();
        private readonly List<VecZone> _zones = new List<VecZone>();
        private int _etatZones;
        public override List<VecZone> Zones => _zones;
        public override int EtatDesZones => _etatZones;

        private enum Etat { Ouvre, Presente, Attend, Vol, Explique, Fete, Vide }
        private Etat _etat;
        private bool _ouvert, _aide, _premiere = true;
        private int _rang, _fautes, _fautesManche;
        private double _depuis, _tour, _ox, _oy, _muet, _phi, _secousse = 9, _claque, _gesteT;
        private string _geste = "repos";
        private readonly double[] _angles = { 10, 6, 10, 6, 3, 0, 3, 0 };      // bras gauche (haut, bas), bras droit, jambe gauche, jambe droite
        private Organe _cible;
        private readonly VecObjet _pomme;
        private readonly Piece _machoire, _oeilG, _oeilD, _pupilleG, _pupilleD;
        private readonly Piece[] _humerus = new Piece[2], _avantbras = new Piece[2], _mains = new Piece[2], _femurs = new Piece[2], _tibias = new Piece[2], _pieds = new Piece[2];
        private VecObjet _etiquette;
        private double _volX0, _volY0, _volE0, _pommeT = -1;
        private readonly Bac _etoiles, _coeurs, _air, _eclairs, _bulles, _globules;

        public CorpsMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Decor, Corps, Interface, Effets };
            var ciel = CorpsArt.Ciels["salle"]; CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            foreach (var p in CorpsArt.Scenes["salle"]) Decor.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = p.Dessin }.En(p.X, p.Y, p.E, p.R));
            foreach (var l in CorpsArt.Listes["organes"])
                _organes.Add(new Organe { Cle = l[0], Nom = l[1], X = double.Parse(l[2], Inv), Y = double.Parse(l[3], Inv), L = double.Parse(l[4], Inv), H = double.Parse(l[5], Inv), Z = double.Parse(l[6], Inv), Dit = l[7], Couleur = l[8], Taille = double.Parse(l[9], Inv) });

            // --- Oscar : tous ses morceaux, posés du plus profond au plus proche (la colonne derrière, les côtes devant) ---
            Pose(Corps, "c_potence", AX, 0);
            var plan = new List<Tuple<string, double, double, double>>
            {
                Tuple.Create("os_colonne", 0.0, 0.0, -0.6), Tuple.Create("os_bassin", 0.0, 0.0, -0.25), Tuple.Create("os_cou", 0.0, 0.0, 0.55),
                Tuple.Create("os_femur", -1.0, 0.0, 0.0), Tuple.Create("os_femur", 1.0, 0.0, 0.0), Tuple.Create("os_tibia", -1.0, 0.0, 0.02), Tuple.Create("os_tibia", 1.0, 0.0, 0.02), Tuple.Create("os_pied", -1.0, 0.0, 0.04), Tuple.Create("os_pied", 1.0, 0.0, 0.04),
                Tuple.Create("os_humerus", -1.0, 0.0, 0.6), Tuple.Create("os_humerus", 1.0, 0.0, 0.6), Tuple.Create("os_avantbras", -1.0, 0.0, 0.62), Tuple.Create("os_avantbras", 1.0, 0.0, 0.62), Tuple.Create("os_main", -1.0, 0.0, 0.64), Tuple.Create("os_main", 1.0, 0.0, 0.64),
                Tuple.Create("os_cotes", 0.0, 0.0, 0.8), Tuple.Create("os_epaules", 0.0, 0.0, 0.82), Tuple.Create("os_crane", 0.0, 152.0, 0.9), Tuple.Create("os_machoire", 0.0, 230.0, 0.92),
                Tuple.Create("os_oeil", -36.0, 156.0, 1.0), Tuple.Create("os_oeil", 36.0, 156.0, 1.0), Tuple.Create("os_pupille", -36.0, 156.0, 1.05), Tuple.Create("os_pupille", 36.0, 156.0, 1.05),
            };
            foreach (var o in _organes) plan.Add(Tuple.Create("o_" + o.Cle, o.X, o.Y, o.Z));
            plan.Sort((a, b) => a.Item4.CompareTo(b.Item4));
            foreach (var m in plan)
            {
                bool membre = m.Item1 == "os_femur" || m.Item1 == "os_tibia" || m.Item1 == "os_pied" || m.Item1 == "os_humerus" || m.Item1 == "os_avantbras" || m.Item1 == "os_main";
                var p = Morceau(m.Item1, membre ? 0 : m.Item2, m.Item3, m.Item4);
                int k = m.Item2 < 0 ? 0 : 1;
                switch (m.Item1)
                {
                    case "os_femur": _femurs[k] = p; break; case "os_tibia": _tibias[k] = p; break; case "os_pied": _pieds[k] = p; break;
                    case "os_humerus": _humerus[k] = p; break; case "os_avantbras": _avantbras[k] = p; break; case "os_main": _mains[k] = p; break;
                    case "os_machoire": _machoire = p; break;
                    case "os_oeil": if (k == 0) _oeilG = p; else _oeilD = p; break;
                    case "os_pupille": if (k == 0) _pupilleG = p; else _pupilleD = p; break;
                }
                if (m.Item1.StartsWith("o_")) { var o = O(m.Item1.Substring(2)); o.O = p.O; o.O.Op = 0; _pieces.Remove(p); }
            }
            _globules = new Bac(this, Effets, "c_globule", 14);
            _air = new Bac(this, Effets, "c_air", 14);
            _bulles = new Bac(this, Effets, "bulle", 10);
            _eclairs = new Bac(this, Effets, "c_eclair", 12);
            _pomme = Pose(Effets, "c_pomme", 0, 0); _pomme.Op = 0;

            // --- Le tableau : le dessin de chaque organe, pâle tant qu'il manque, coloré et coché quand il est en place ---
            for (int i = 0; i < _organes.Count; i++)
            {
                var o = _organes[i];
                double x = 1240 + (i % 2) * 208, y = 132 + (i / 2) * 92; if (i == _organes.Count - 1) x = 1344;
                Pose(Interface, "c_rond_craie", x, y, 1);
                o.Mini = Pose(Interface, "o_" + o.Cle, x, y, Math.Min(0.5, Math.Min(74 / o.L, 62 / o.H) * o.Taille)); o.Mini.Op = 0.22;
                o.Coche = Pose(Interface, "c_coche", x + 40, y + 26); o.Coche.Op = 0;
            }

            _etoiles = new Bac(this, Effets, "etoile", 50);
            _coeurs = new Bac(this, Effets, "coeur", 14);
        }

        private Piece Morceau(string dessin, double x, double y, double z)
        {
            var p = new Piece { O = Pose(Corps, dessin, AX + x, y), X = x, Y = y, Z = z };
            _pieces.Add(p);
            return p;
        }

        public override void Demarre(int scene = 0)
        {
            if (_ouvert) return;
            if (scene > 0) Niveau = scene;
            _ouvert = true;
            NouvelleManche();
        }

        /// <summary>Là où il faut regarder (pour le coup de pouce, et pour le regard automatique du banc d'essai).</summary>
        public override bool ProchaineCible(out double x, out double y)
        {
            x = y = 0;
            if (_etat != Etat.Attend || _cible == null) return false;
            var z = _zones.Find(q => q.Id == _cible.Cle);
            if (z == null) return false;
            x = z.X + z.L / 2; y = z.Y + z.H / 2;
            return true;
        }

        // ------------------------------------------------------------------
        //  Une manche
        // ------------------------------------------------------------------
        private void NouvelleManche()
        {
            foreach (var o in _organes) { o.EnPlace = false; o.O.Op = 0; o.Chute = 0; o.Mini.Op = 0.22; o.Coche.Op = 0; }
            _ordre.Clear();
            if (Niveau == 0) foreach (var c in new[] { "cerveau", "coeur", "poumons", "estomac", "intestins" }) _ordre.Add(O(c));      // pour commencer : les cinq plus connus, toujours dans le même ordre
            else
            {
                _ordre.AddRange(_organes);
                for (int i = _ordre.Count - 1; i > 0; i--) { int j = _alea.Next(i + 1); var t = _ordre[i]; _ordre[i] = _ordre[j]; _ordre[j] = t; }
            }
            _rang = 0; _fautesManche = 0;
            _etat = Etat.Ouvre; _depuis = 0;
            Dit(_premiere ? "Voici Oscar, le squelette. Il lui manque plein de choses ! Remets chaque organe à sa place." : "Patatras ! Tout est retombé. On recommence !");
            Geste("salut", 2.6);
            _premiere = false; _muet = 3.2;
        }

        private string Question(Organe o) => Niveau == 0 ? "Voici " + o.Nom + ". Regarde l'endroit qui brille !" : Niveau == 1 ? "Où va " + o.Nom + " ?" : "Trouve " + o.Nom + " !";

        /// <summary>Présente l'organe suivant : sa carte à gauche, les endroits qui brillent (ou trois organes au choix).</summary>
        private void Presente()
        {
            _cible = _ordre[_rang]; _fautes = 0; _aide = false;
            _etat = Etat.Presente; _depuis = 0;
            Range();
            if (Niveau < 2)
            {
                // la carte de l'organe, son nom dessous
                var c = new Carte { Pour = _cible, X = 85, Y = 190 };
                c.Fond = Interface.Ajoute(Dessin("c_carte", "c", _cible.Couleur).En(c.X, c.Y));
                c.E0 = Math.Min(1.7, Math.Min(250 / _cible.L, 220 / _cible.H)) * _cible.Taille;
                c.Image = Interface.Ajoute(Dessin("o_" + _cible.Cle).En(c.X + 165, c.Y + 165, c.E0));
                _cartes.Add(c);
                _etiquette = Interface.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = _cible.Nom, Taille = 52, Couleur = "#FFFFFF" }.En(250, 574));
                // les endroits : le bon, et au niveau 1 deux autres qui ne le touchent pas
                var lieux = new List<Organe> { _cible };
                if (Niveau == 1)
                {
                    var autres = _organes.FindAll(o => o != _cible && !o.EnPlace && !SeTouchent(o, _cible));
                    while (lieux.Count < 3 && autres.Count > 0)
                    {
                        var a = autres[_alea.Next(autres.Count)]; autres.Remove(a);
                        if (lieux.TrueForAll(l => !SeTouchent(l, a))) lieux.Add(a);
                    }
                }
                foreach (var o in lieux) _lueurs.Add(FaisLueur(o));
            }
            else
            {
                // trois organes au choix : le bon et deux autres, mélangés
                var choix = new List<Organe> { _cible };
                var autres = _organes.FindAll(o => o != _cible);
                while (choix.Count < 3 && autres.Count > 0) { var a = autres[_alea.Next(autres.Count)]; autres.Remove(a); choix.Add(a); }
                for (int i = choix.Count - 1; i > 0; i--) { int j = _alea.Next(i + 1); var t = choix[i]; choix[i] = choix[j]; choix[j] = t; }
                for (int i = 0; i < choix.Count; i++)
                {
                    var o = choix[i];
                    var c = new Carte { Pour = o, X = 100, Y = 40 + i * 282 };
                    c.Fond = Interface.Ajoute(Dessin("c_petite_carte", "c", o.Couleur).En(c.X, c.Y));
                    c.E0 = Math.Min(1.5, Math.Min(230 / o.L, 180 / o.H)) * o.Taille;
                    c.Image = Interface.Ajoute(Dessin("o_" + o.Cle).En(c.X + 150, c.Y + 125, c.E0));
                    _cartes.Add(c);
                }
                _lueurs.Add(FaisLueur(_cible));      // l'endroit où il ira brille doucement
            }
            Dit(Question(_cible)); _muet = 1.0;
            Geste("pense", 2.2);
        }

        private static void Zone(Organe o, out double x, out double y, out double l, out double h)
        {
            l = Math.Max(o.L + 30, 136); h = Math.Max(o.H + 24, 112); x = AX + o.X - l / 2; y = o.Y - h / 2;
        }
        private static bool SeTouchent(Organe a, Organe b)
        {
            Zone(a, out double ax, out double ay, out double al, out double ah); Zone(b, out double bx, out double by, out double bl, out double bh);
            return ax < bx + bl + 8 && bx < ax + al + 8 && ay < by + bh + 8 && by < ay + ah + 8;
        }

        private Lueur FaisLueur(Organe o)
        {
            var l = new Lueur { Pour = o };
            l.Halo = Corps.Ajoute(Dessin("c_lueur"));
            l.Bord = Corps.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#2A1C4E", Epaisseur = 9, Tiret = 1.6, Espace = 1.6 });
            l.Tirets = Corps.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#FFE066", Epaisseur = 6, Tiret = 2.4, Espace = 2.4 });
            l.Non = Corps.Ajoute(Dessin("c_non")); l.Non.Op = 0;
            double rx = o.L / 2 + 10, ry = o.H / 2 + 8;
            var pts = new double[74];
            for (int i = 0; i <= 36; i++) { double a = Math.PI * 2 * i / 36; pts[i * 2] = rx * Math.Cos(a); pts[i * 2 + 1] = ry * Math.Sin(a); }
            l.Bord.Points = pts; l.Tirets.Points = pts;
            return l;
        }

        /// <summary>Retire les cartes, l'étiquette et les lueurs de l'organe précédent.</summary>
        private void Range()
        {
            foreach (var c in _cartes) { Interface.Retire(c.Fond); Interface.Retire(c.Image); }
            _cartes.Clear();
            if (_etiquette != null) { Interface.Retire(_etiquette); _etiquette = null; }
            foreach (var l in _lueurs) { Corps.Retire(l.Halo); Corps.Retire(l.Bord); Corps.Retire(l.Tirets); Corps.Retire(l.Non); }
            _lueurs.Clear();
            if (_zones.Count > 0) { _zones.Clear(); _etatZones++; }
        }

        private void RefaisLesZones()
        {
            _zones.Clear();
            if (Niveau < 2) foreach (var l in _lueurs) { if (l.Faux >= 0) continue; Zone(l.Pour, out double x, out double y, out double w, out double h); _zones.Add(new VecZone { Id = l.Pour.Cle, X = x, Y = y, L = w, H = h }); }
            else foreach (var c in _cartes) { if (c.Faux >= 0) continue; _zones.Add(new VecZone { Id = c.Pour.Cle, X = c.X, Y = c.Y, L = 300, H = 250 }); }
            _etatZones++;
        }

        // ------------------------------------------------------------------
        //  Ce que l'enfant choisit
        // ------------------------------------------------------------------
        public override void Appuie(string id)
        {
            if (!_ouvert || _etat != Etat.Attend || _cible == null || id == null) return;
            if (id == _cible.Cle) { Reussi(); return; }
            // ce n'est pas le bon : on l'écarte en douceur, sans rien enlever de ce qui est fait
            _fautes++; _fautesManche++;
            var pris = O(id);
            if (Niveau < 2) { var l = _lueurs.Find(q => q.Pour == pris); if (l != null) l.Faux = 0; Dit("Non, " + _cible.Nom + (_cible.Nom.StartsWith("les ") ? " ne vont pas là." : " ne va pas là.") + " Cherche encore !"); }
            else { var c = _cartes.Find(q => q.Pour == pris); if (c != null) c.Faux = 0; Dit("Ça, c'est " + (pris != null ? pris.Nom : "autre chose") + ". Cherche " + _cible.Nom + " !"); }
            Son("pof", 0);
            Geste("hausse", 1.4);
            RefaisLesZones();
            _depuis = 0; _muet = 2.5;
        }

        private void Reussi()
        {
            var c = _cartes.Find(q => q.Pour == _cible);
            _etat = Etat.Vol; _depuis = 0;
            _zones.Clear(); _etatZones++;
            // l'organe quitte sa carte et vole jusqu'à sa place
            _volX0 = c.Image.X; _volY0 = c.Image.Y; _volE0 = c.E0;
            c.Image.Op = 0;
            _aRetirer.Clear();
            var o = Dessin("o_" + _cible.Cle).En(_volX0, _volY0, _volE0);
            Effets.Ajoute(o); _aRetirer.Add(o);
            Son("pop", 0);
        }

        private void EnPlace()
        {
            foreach (var o in _aRetirer) Effets.Retire(o);
            _aRetirer.Clear();
            var c = _cible;
            c.EnPlace = true; c.Age = 0; c.O.Op = 1; c.Mini.Op = 1; c.Coche.Op = 1;
            foreach (var l in _lueurs) { l.Halo.Op = 0; l.Bord.Op = 0; l.Tirets.Op = 0; l.Non.Op = 0; }
            Monde(c.X, c.Y, c.Z, out double x, out double y);
            for (int i = 0; i < 9; i++) { double a = Math.PI * 2 * i / 9 + Hasard(); _etoiles.Lance(x, y, Math.Cos(a) * 260 * Entre(.6, 1), Math.Sin(a) * 260 * Entre(.6, 1) - 50, 380, Entre(.5, .8), Entre(.7, 1.2), 0, Entre(-200, 200)); }
            Son("note", 2 + _rang);
            _secousse = 0; _claque = 0.9; Geste("haut", 1.3);      // il sursaute de joie et claque des dents
            Reussites++;
            // chaque organe a son petit effet en arrivant
            if (c.Cle == "coeur") for (int i = 0; i < 5; i++) _coeurs.Lance(x + Entre(-30, 30), y, Entre(-80, 80), Entre(-260, -150), 60, 1.2, Entre(.8, 1.3));
            if (c.Cle == "estomac") _pommeT = 0;
            if (c.Cle == "cerveau") for (int i = 0; i < 8; i++) _eclairs.Lance(x + Entre(-80, 80), y + Entre(-50, 40), Entre(-40, 40), Entre(-90, -20), 60, Entre(.5, .9), Entre(.9, 1.5), 0, Entre(-120, 120));
            if (c.Cle == "vessie") Son("plouf", 0);
            Dit(c.Dit);
            _etat = Etat.Explique; _depuis = 0;
        }

        private void Geste(string geste, double duree) { _geste = geste; _gesteT = duree; }

        /// <summary>Où se trouve, dans l'image, un point d'Oscar : il se balance sous son crochet, et ce qui est devant glisse quand il tourne.</summary>
        private void Monde(double x, double y, double z, out double mx, out double my)
        {
            double a = _phi * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a), dy = y - Crochet;
            mx = AX + x * c - dy * s + z * Relief * _tour; my = Crochet + x * s + dy * c;
        }

        // ------------------------------------------------------------------
        //  Une image
        // ------------------------------------------------------------------
        public override void Avance(double dt)
        {
            if (dt <= 0 || !_ouvert) return;
            dt = Math.Min(dt, 0.1);
            _temps += dt; _depuis += dt;
            if (_muet > 0) _muet -= dt;
            bool regarde = _temps - _vu < 1.2;

            switch (_etat)
            {
                case Etat.Ouvre:
                    if (_depuis > 2.6 && (_muet <= 0 && !VoixOccupee() || _depuis > 6)) Presente();
                    break;
                case Etat.Presente:
                    if (_depuis >= 0.45) { _etat = Etat.Attend; _depuis = 0; RefaisLesZones(); }
                    break;
                case Etat.Attend:
                    // le coup de pouce : la question est redite ; après deux erreurs ou un long moment, seul le bon endroit reste
                    if (!_aide && (_fautes >= 2 || _depuis > 26))
                    {
                        _aide = true;
                        foreach (var l in _lueurs) if (l.Pour != _cible && l.Faux < 0) l.Faux = 0;
                        foreach (var c in _cartes) if (c.Pour != _cible && c.Faux < 0) c.Faux = 0;
                        RefaisLesZones();
                    }
                    if (_depuis > 13 && _muet <= 0 && !VoixOccupee()) { _depuis = 0.01; _muet = 2; Dit(Question(_cible)); }
                    break;
                case Etat.Vol:
                {
                    double u = Borne(_depuis / 0.7), w = Doux(u);
                    Monde(_cible.X, _cible.Y, _cible.Z, out double bx, out double by);
                    double x = Mel(_volX0, bx, w), y = Mel(_volY0, by, w) - 130 * 4 * u * (1 - u);
                    foreach (var o in _aRetirer) { o.X = x; o.Y = y; o.E = Mel(_volE0, _cible.Taille, w) * (1 + .2 * Math.Sin(Math.PI * u)); o.R = 14 * Math.Sin(Math.PI * 2 * u); }
                    if (Hasard() < dt * 30) _etoiles.Lance(x + Entre(-20, 20), y + Entre(-20, 20), 0, 40, 100, .4, Entre(.3, .6), 0, 200);
                    if (u >= 1) EnPlace();
                    break;
                }
                case Etat.Explique:
                    if (_depuis > 2.4 && (!VoixOccupee() || _depuis > 8))
                    {
                        _rang++;
                        if (_rang < _ordre.Count) Presente();
                        else
                        {
                            Range();
                            _etat = Etat.Fete; _depuis = 0;
                            MancheReussie = _fautesManche <= 1;
                            Dit("Bravo ! Oscar a tout ce qu'il lui faut. Regarde comme il danse !");
                            Geste("danse", 5.2); _claque = 5;
                            Bravo();
                        }
                    }
                    break;
                case Etat.Fete:
                    if (Math.Floor(_depuis / 0.3) != Math.Floor((_depuis - dt) / 0.3)) for (int i = 0; i < 5; i++) _etoiles.Lance(AX + Entre(-300, 300), Entre(120, 620), Entre(-60, 60), Entre(-160, -40), 260, Entre(.6, 1), Entre(.8, 1.4), 0, Entre(-200, 200));
                    if (_depuis > 5.2 && (!VoixOccupee() || _depuis > 9)) { _etat = Etat.Vide; _depuis = 0; Son("pof", 0); _secousse = 0; foreach (var o in _organes) { o.Chute = 0.001; o.VX = Entre(-160, 160); } }
                    break;
                case Etat.Vide:
                    // patatras : les organes retombent par terre (voir AvanceCorps)
                    if (_depuis >= 1.3) NouvelleManche();
                    break;
            }

            AvanceCorps(dt, regarde);
            AvanceCartes(dt);
            _etoiles.Avance(dt); _coeurs.Avance(dt); _air.Avance(dt); _eclairs.Avance(dt); _bulles.Avance(dt); _globules.Avance(dt);
            AvanceMots(dt);
        }

        // ------------------------------------------------------------------
        //  Le bonhomme et ses organes
        // ------------------------------------------------------------------
        private void AvanceCorps(double dt, bool regarde)
        {
            // --- Il tourne un peu vers ce que l'enfant regarde ; il se balance sous son crochet, davantage quand il est content ---
            double vers = regarde ? Borne((_rx - AX) / 560, -1, 1) : 0.5 * Math.Sin(_temps * 0.5);
            _tour += (vers - _tour) * Math.Min(1, dt * 4);
            _secousse += dt;
            if (_gesteT > 0) { _gesteT -= dt; if (_gesteT <= 0) _geste = "repos"; }
            if (_claque > 0) _claque -= dt;
            bool danse = _geste == "danse";
            double rythme = Math.Sin(_temps * 8);
            _phi = 1.3 * Math.Sin(_temps * 1.2) + 5 * Math.Exp(-_secousse * 2.6) * Math.Sin(_secousse * 15) + (danse ? 4.5 * Math.Sin(_temps * 4) : 0);

            // --- Ses gestes : les angles des bras et des jambes (0 : l'os pend ; positif : il s'écarte du corps) ---
            double[] v = { 10 + 2 * Math.Sin(_temps * 1.3), 6, 10 - 2 * Math.Sin(_temps * 1.3), 6, 3, 0, 3, 0 };
            switch (_geste)
            {
                case "salut": v[2] = 150; v[3] = 150 + 26 * Math.Sin(_temps * 11); break;
                case "haut": v[0] = v[2] = 152; v[1] = 172 + 8 * rythme; v[3] = 172 - 8 * rythme; break;
                case "pense": v[2] = 26; v[3] = 158; break;                                   // la main au menton
                case "hausse": v[0] = v[2] = 34; v[1] = v[3] = 118; break;                    // « je ne sais pas »
                case "danse":
                    v[0] = 80 + 60 * rythme; v[1] = v[0] + 50; v[2] = 80 - 60 * rythme; v[3] = v[2] + 50;
                    v[4] = 22 * Math.Max(0, rythme); v[5] = -30 * Math.Max(0, rythme); v[6] = 22 * Math.Max(0, -rythme); v[7] = -30 * Math.Max(0, -rythme);
                    break;
            }
            double suit = Math.Min(1, dt * 9), tremble = Math.Exp(-_secousse * 3) * 14;
            for (int i = 0; i < 8; i++) _angles[i] += (v[i] + tremble * Math.Sin(_secousse * 31 + i * 1.7) - _angles[i]) * suit;

            // --- Les morceaux fixes (colonne, bassin, côtes, crâne…), puis les os articulés ---
            bool parle = VoixOccupee() && _muet <= 0.2 || VoixOccupee();
            double machoire = 3 + (parle ? 9 * Math.Abs(Math.Sin(_temps * 15)) : 0) + (_claque > 0 ? 9 * Math.Abs(Math.Sin(_temps * 36)) : 0);
            _machoire.Y = 230 + machoire;
            foreach (var p in _pieces) { Monde(p.X, p.Y, p.Z, out double x, out double y); p.O.X = x; p.O.Y = y; p.O.R = _phi + p.R; }
            for (int k = 0; k < 2; k++)
            {
                int cote = k == 0 ? -1 : 1;
                Os(_humerus[k], cote * 128, 286, cote, _angles[k * 2], 128, out double cx, out double cy);
                Os(_avantbras[k], cx, cy, cote, _angles[k * 2 + 1], 118, out double px, out double py);
                Os(_mains[k], px, py, cote, _angles[k * 2 + 1], 0, out _, out _);
                Os(_femurs[k], cote * 58, 602, cote, _angles[4 + k * 2], 128, out double gx, out double gy);
                Os(_tibias[k], gx, gy, cote, _angles[4 + k * 2] + _angles[5 + k * 2], 118, out double ax, out double ay);
                Monde(ax, ay, _pieds[k].Z, out double fx, out double fy);
                _pieds[k].O.X = fx; _pieds[k].O.Y = fy; _pieds[k].O.SX = cote; _pieds[k].O.R = _phi;
            }

            // ses yeux suivent l'enfant
            Monde(0, 156, 1, out double ex, out double ey);
            double tx = regarde ? _rx : AX + 300 * Math.Sin(_temps * .6), ty = regarde ? _ry : 300;
            double ddx = tx - ex, ddy = ty - ey, d = Math.Max(1, Math.Sqrt(ddx * ddx + ddy * ddy)), f = Math.Min(1, d / 200);
            _ox += (ddx / d * f - _ox) * Math.Min(1, dt * 9); _oy += (ddy / d * f - _oy) * Math.Min(1, dt * 9);
            _pupilleG.O.X += _ox * 7; _pupilleG.O.Y += _oy * 8; _pupilleD.O.X += _ox * 7; _pupilleD.O.Y += _oy * 8;

            // --- Les organes en place vivent ---
            foreach (var o in _organes)
            {
                if (!o.EnPlace) continue;
                Monde(o.X, o.Y, o.Z, out double x, out double y);
                if (o.Chute > 0)
                {
                    // la manche est finie : il tombe par terre, rebondit, s'éteint
                    o.Chute += dt;
                    double t = o.Chute, sol = 852 - o.H * o.Taille / 2, chute = y + 900 * t * t;
                    if (chute > sol) { double apres = t - Math.Sqrt(Math.Max(0, (sol - y) / 900)); chute = sol - 60 * Math.Abs(Math.Sin(apres * 9)) * Math.Exp(-apres * 4); }
                    o.O.X = x + o.VX * t; o.O.Y = chute; o.O.R = o.VX * t * 0.8; o.O.E = o.Taille; o.O.Op = Borne((1.25 - t) * 4);
                    continue;
                }
                o.Age += dt;
                double pop = o.Age < 0.5 ? Rebond(o.Age / 0.45) : 1, sx = 1, sy = 1, r = 0;
                switch (o.Cle)
                {
                    case "coeur":
                    {
                        // deux battements serrés, une pause : « boum-boum »
                        double ph = (_temps * 1.25) % 1, bat = Math.Max(Cloche(ph, .06, .05), .7 * Cloche(ph, .24, .05));
                        sx = sy = 1 + .14 * bat;
                        if (ph < dt * 1.25) _globules.Lance(x + Entre(-10, 10), y + 26, Entre(-70, 70), Entre(40, 120), 0, .8, Entre(.6, 1));
                        break;
                    }
                    case "poumons":
                    {
                        double souffle = Math.Sin(_temps * 2.1);
                        sx = 1 + .07 * souffle; sy = 1 + .05 * souffle;
                        // l'air entre par le nez quand ils se gonflent
                        if (Math.Cos(_temps * 2.1) > 0.3 && Hasard() < dt * 9) { Monde(0, 196, 1, out double nx, out double ny); _air.Lance(nx + Entre(-6, 6), ny, Entre(-8, 8), 120, 0, .8, Entre(.5, .9)); }
                        break;
                    }
                    case "cerveau":
                        r = 1.5 * Math.Sin(_temps * 1.7);
                        if (Hasard() < dt * 2.2) _eclairs.Lance(x + Entre(-70, 70), y + Entre(-50, 20), Entre(-20, 20), Entre(-50, -10), 40, Entre(.4, .7), Entre(.7, 1.1), 0, Entre(-90, 90));
                        break;
                    case "estomac":
                        r = 3 * Math.Sin(_temps * 2.6); sx = 1 + .03 * Math.Sin(_temps * 5.2);
                        if (Hasard() < dt * 1.6) _bulles.Lance(x + Entre(-16, 16), y + 16, Entre(-10, 10), -40, 0, .7, Entre(.3, .5));
                        break;
                    case "foie": sx = sy = 1 + .025 * Math.Sin(_temps * 1.6); break;
                    case "intestins": sx = 1 + .035 * Math.Sin(_temps * 3.4); sy = 1 - .035 * Math.Sin(_temps * 3.4); break;
                    case "vessie": sy = 1 + .05 * Math.Sin(_temps * 1.3); sx = 1 + .03 * Math.Sin(_temps * 1.3 + 1); break;
                }
                o.O.X = x; o.O.Y = y; o.O.SX = pop * sx * o.Taille; o.O.SY = pop * sy * o.Taille; o.O.R = _phi + r;
            }

            // la pomme descend de la bouche à l'estomac
            if (_pommeT >= 0)
            {
                _pommeT += dt; var e = O("estomac");
                double u = Borne(_pommeT / 1.0);
                Monde(0, 236, 1, out double bx, out double by); Monde(e.X - 16, e.Y - 16, e.Z, out double sx2, out double sy2);
                _pomme.X = Mel(bx, sx2, Doux(u)); _pomme.Y = Mel(by, sy2, u);
                _pomme.E = Mel(1.1, .7, u); _pomme.R = 300 * u; _pomme.Op = Borne((1 - u) * 5);
                if (u >= 1) { _pommeT = -1; _pomme.Op = 0; for (int i = 0; i < 4; i++) _bulles.Lance(_pomme.X + Entre(-14, 14), _pomme.Y, Entre(-30, 30), Entre(-70, -30), 0, .7, Entre(.3, .6)); }
            }

            // --- Les endroits qui brillent ---
            foreach (var l in _lueurs)
            {
                var o = l.Pour;
                Monde(o.X, o.Y, o.Z, out double x, out double y);
                bool vu = _etat == Etat.Attend || _etat == Etat.Presente;
                double bat = .5 + .5 * Math.Sin(_temps * 5), doux = Niveau == 2 ? .7 : 1;
                if (l.Faux >= 0)
                {
                    // ce n'était pas là : une croix, puis l'endroit s'éteint
                    l.Faux += dt;
                    l.Non.X = x + 8 * Math.Sin(l.Faux * 40) * Borne(1 - l.Faux * 2); l.Non.Y = y; l.Non.Op = Borne((1.2 - l.Faux) * 3); l.Non.E = Rebond(l.Faux / 0.3);
                    l.Halo.Op = 0; l.Bord.Op = 0; l.Tirets.Op = 0;
                    continue;
                }
                l.Halo.X = x; l.Halo.Y = y; l.Halo.SX = (o.L / 2 + 34) / 100 * (1 + .08 * bat); l.Halo.SY = (o.H / 2 + 30) / 100 * (1 + .08 * bat); l.Halo.Op = vu ? (.8 + .2 * bat) * doux : 0;
                foreach (var t in new[] { l.Bord, l.Tirets }) { t.X = x; t.Y = y; t.E = 1 + .03 * bat; t.Decalage = -_temps * 5; t.Op = vu ? doux : 0; }
            }
        }

        /// <summary>Pose un os articulé : il part de (x, y), s'écarte de « angle » degrés du côté donné ; rend le bout de l'os.</summary>
        private void Os(Piece p, double x, double y, int cote, double angle, double longueur, out double bx, out double by)
        {
            double a = angle * Math.PI / 180;
            bx = x + cote * Math.Sin(a) * longueur; by = y + Math.Cos(a) * longueur;
            Monde(x, y, p.Z, out double mx, out double my);
            p.O.X = mx; p.O.Y = my; p.O.R = _phi - cote * angle; p.O.SX = cote;
        }

        private static double Cloche(double x, double milieu, double largeur) { double u = (x - milieu) / largeur; return Math.Exp(-u * u); }

        private void AvanceCartes(double dt)
        {
            foreach (var c in _cartes)
            {
                double cx = c.X + (Niveau < 2 ? 165 : 150), cy = c.Y + (Niveau < 2 ? 165 : 125);
                if (c.Faux >= 0)
                {
                    // ce n'était pas celui-là : la carte fait non, puis pâlit
                    c.Faux += dt;
                    double secoue = 12 * Math.Sin(c.Faux * 40) * Borne(1 - c.Faux * 2.2), op = Math.Max(.28, 1 - c.Faux * 1.6);
                    c.Fond.X = c.X + secoue; c.Image.X = cx + secoue; c.Fond.Op = op; if (c.Image.Op > 0) c.Image.Op = op;
                    continue;
                }
                // l'organe sur sa carte respire un peu, pour donner envie
                double bat = Math.Sin(_temps * 3 + c.Y * .01);
                c.Image.E = c.E0 * (1 + .04 * bat) * Math.Min(1, _etat == Etat.Presente ? Rebond(_depuis / 0.4) : 1);
                c.Image.R = 3 * Math.Sin(_temps * 1.7 + c.Y * .02);
            }
        }
    }
}
