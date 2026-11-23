using System;
using System.Collections.Generic;
using System.Globalization;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le moteur de l'activité « Dans mon corps ».
    //
    //  Un bonhomme dont on voit l'intérieur attend ses organes : le cerveau, les
    //  poumons, le cœur, l'estomac, le foie, les intestins, la vessie. La voix en
    //  nomme un ; l'enfant le remet à sa place. Une fois posé, l'organe se met à
    //  vivre (le cœur bat, les poumons se gonflent…), la voix dit à quoi il sert,
    //  et l'écran de droite allume sa courbe. Trois niveaux :
    //    0 — un seul endroit brille : il suffit de le regarder (sans échec) ;
    //    1 — trois endroits brillent : il faut trouver le bon ;
    //    2 — trois organes sont proposés : il faut reconnaître celui qu'on nomme.
    //  Le bonhomme tourne un peu vers l'endroit qu'on regarde : les organes, à
    //  des profondeurs différentes, glissent les uns devant les autres (du relief).
    //
    //  Comme les autres jeux « dessinés », le moteur ne connaît ni l'écran ni
    //  Windows (voir VecMoteur.cs) : le banc d'essai en tire de vraies images.
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class CorpsMoteur : VecMoteur
    {
        private const double AX = 800;                     // l'axe du bonhomme
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
            public string Cle, Nom, Dit, Couleur; public double X, Y, L, H, Z; public bool EnPlace; public double Age = 9;
            public VecObjet O;
        }
        private sealed class Piece { public VecObjet O; public double X, Y, Z; }       // un morceau du bonhomme : sa place, sa profondeur
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
        private double _depuis, _tour, _saut, _sautT = 9, _sautH, _sauts, _ox, _oy, _muet;
        private Organe _cible;
        private readonly VecObjet _socleLueur, _pupilleG, _pupilleD, _bouche, _ravi, _oh, _pomme;
        private VecObjet _etiquette;
        private double _volX0, _volY0, _volE0, _pommeT = -1;
        private readonly VecObjet[] _courbes, _plates;
        private readonly Bac _etoiles, _coeurs, _air, _eclairs, _bulles, _globules;

        public CorpsMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Decor, Corps, Interface, Effets };
            var ciel = CorpsArt.Ciels["salle"]; CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            foreach (var p in CorpsArt.Scenes["salle"]) Decor.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = p.Dessin }.En(p.X, p.Y, p.E, p.R));
            foreach (var l in CorpsArt.Listes["organes"])
                _organes.Add(new Organe { Cle = l[0], Nom = l[1], X = double.Parse(l[2], Inv), Y = double.Parse(l[3], Inv), L = double.Parse(l[4], Inv), H = double.Parse(l[5], Inv), Z = double.Parse(l[6], Inv), Dit = l[7], Couleur = l[8] });

            // --- Le socle, le bonhomme, l'intérieur, les organes (du plus profond au plus proche), la vitre, le visage ---
            _socleLueur = Pose(Decor, "c_socle_lueur", AX, 862);
            Pose(Decor, "c_socle", AX, 862);
            Morceau("c_corps", 0, 0, 0); Morceau("c_fenetres", 0, 0, 0.12);
            var parProfondeur = new List<Organe>(_organes); parProfondeur.Sort((a, b) => a.Z.CompareTo(b.Z));
            foreach (var o in parProfondeur) { o.O = Morceau("o_" + o.Cle, o.X, o.Y, o.Z).O; o.O.Op = 0; }
            _globules = new Bac(this, Corps, "c_globule", 14);
            _air = new Bac(this, Corps, "c_air", 14);
            _bulles = new Bac(this, Corps, "bulle", 10);
            _eclairs = new Bac(this, Corps, "c_eclair", 12);
            _pomme = Pose(Corps, "c_pomme", 0, 0); _pomme.Op = 0;
            Morceau("c_vitre", 0, 0, 1.0); Morceau("c_visage", 0, 0, 1.25);
            _pupilleG = Morceau("c_pupille", -42, 234, 1.45).O; _pupilleD = Morceau("c_pupille", 42, 234, 1.45).O;      // (le blanc des yeux est dans c_visage)
            _bouche = Morceau("c_bouche", 0, 276, 1.25).O; _ravi = Morceau("c_bouche_ravi", 0, 276, 1.25).O; _oh = Morceau("c_bouche_oh", 0, 278, 1.25).O;

            // --- L'écran : une courbe pour le cœur, une pour les poumons, une pour le cerveau ---
            Pose(Interface, "c_ecran", 1168, 150);
            string[] icones = { "c_ic_coeur", "c_ic_poumons", "c_ic_cerveau" }, couleurs = { "#FF6A7E", "#7FD3FF", "#FFB0C8" };
            _courbes = new VecObjet[3]; _plates = new VecObjet[3];
            for (int i = 0; i < 3; i++)
            {
                Pose(Interface, icones[i], 1168 + 46, 150 + 64 + i * 87, 0.85);
                _plates[i] = Interface.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#3A6C9A", Epaisseur = 4, Points = new[] { 1168 + 78.0, 150 + 64 + i * 87, 1168 + 350, 150 + 64 + i * 87 } });
                _courbes[i] = Interface.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = couleurs[i], Epaisseur = 5, Op = 0 });
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
            foreach (var o in _organes) { o.EnPlace = false; o.O.Op = 0; }
            _ordre.Clear();
            if (Niveau == 0) foreach (var c in new[] { "cerveau", "coeur", "poumons", "estomac", "intestins" }) _ordre.Add(O(c));      // pour commencer : les cinq plus connus, toujours dans le même ordre
            else
            {
                _ordre.AddRange(_organes);
                for (int i = _ordre.Count - 1; i > 0; i--) { int j = _alea.Next(i + 1); var t = _ordre[i]; _ordre[i] = _ordre[j]; _ordre[j] = t; }
            }
            _rang = 0; _fautesManche = 0;
            _etat = Etat.Ouvre; _depuis = 0;
            Dit(_premiere ? "Oh ! Il manque des choses dans ce corps. Remets chaque organe à sa place !" : "On recommence ! Remets chaque organe à sa place.");
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
                c.E0 = Math.Min(1.7, Math.Min(250 / _cible.L, 220 / _cible.H));
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
                    c.E0 = Math.Min(1.5, Math.Min(230 / o.L, 180 / o.H));
                    c.Image = Interface.Ajoute(Dessin("o_" + o.Cle).En(c.X + 150, c.Y + 125, c.E0));
                    _cartes.Add(c);
                }
                _lueurs.Add(FaisLueur(_cible));      // l'endroit où il ira brille doucement
            }
            Dit(Question(_cible)); _muet = 1.0;
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
            c.EnPlace = true; c.Age = 0; c.O.Op = 1;
            foreach (var l in _lueurs) { l.Halo.Op = 0; l.Bord.Op = 0; l.Tirets.Op = 0; l.Non.Op = 0; }
            double x = AX + c.X, y = c.Y;
            for (int i = 0; i < 9; i++) { double a = Math.PI * 2 * i / 9 + Hasard(); _etoiles.Lance(x, y, Math.Cos(a) * 260 * Entre(.6, 1), Math.Sin(a) * 260 * Entre(.6, 1) - 50, 380, Entre(.5, .8), Entre(.7, 1.2), 0, Entre(-200, 200)); }
            Son("note", 2 + _rang);
            Saute(34, .4, 1);
            Reussites++;
            // chaque organe a son petit effet en arrivant
            if (c.Cle == "coeur") for (int i = 0; i < 5; i++) _coeurs.Lance(x + Entre(-30, 30), y, Entre(-80, 80), Entre(-260, -150), 60, 1.2, Entre(.8, 1.3));
            if (c.Cle == "estomac") _pommeT = 0;
            if (c.Cle == "cerveau") for (int i = 0; i < 8; i++) _eclairs.Lance(x + Entre(-80, 80), y + Entre(-50, 40), Entre(-40, 40), Entre(-90, -20), 60, Entre(.5, .9), Entre(.9, 1.5), 0, Entre(-120, 120));
            if (c.Cle == "vessie") Son("plouf", 0);
            Dit(c.Dit);
            _etat = Etat.Explique; _depuis = 0;
        }

        private void Saute(double hauteur, double duree, int fois) { _sautH = hauteur; _sautT = 0; _sauts = fois; _sautD = duree; }
        private double _sautD = .4;

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
                    double x = Mel(_volX0, AX + _cible.X + _cible.Z * Relief * _tour, w), y = Mel(_volY0, _cible.Y - _saut, w) - 130 * 4 * u * (1 - u);
                    foreach (var o in _aRetirer) { o.X = x; o.Y = y; o.E = Mel(_volE0, 1, w) * (1 + .2 * Math.Sin(Math.PI * u)); o.R = 14 * Math.Sin(Math.PI * 2 * u); }
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
                            Dit("Bravo ! Le corps est complet. Tout fonctionne !");
                            Saute(60, .45, 4);
                            Bravo();
                        }
                    }
                    break;
                case Etat.Fete:
                    if (Math.Floor(_depuis / 0.3) != Math.Floor((_depuis - dt) / 0.3)) for (int i = 0; i < 5; i++) _etoiles.Lance(AX + Entre(-300, 300), Entre(120, 620), Entre(-60, 60), Entre(-160, -40), 260, Entre(.6, 1), Entre(.8, 1.4), 0, Entre(-200, 200));
                    if (_depuis > 5.2 && (!VoixOccupee() || _depuis > 9)) { _etat = Etat.Vide; _depuis = 0; Son("pof", 0); }
                    break;
                case Etat.Vide:
                    // les organes repartent : ils rapetissent et s'éteignent
                    foreach (var o in _organes) if (o.EnPlace) { o.O.Op = Borne(1 - _depuis / 0.6); }
                    if (_depuis >= 0.9) NouvelleManche();
                    break;
            }

            AvanceCorps(dt, regarde);
            AvanceCartes(dt);
            AvanceEcran(dt);
            _etoiles.Avance(dt); _coeurs.Avance(dt); _air.Avance(dt); _eclairs.Avance(dt); _bulles.Avance(dt); _globules.Avance(dt);
            AvanceMots(dt);
        }

        // ------------------------------------------------------------------
        //  Le bonhomme et ses organes
        // ------------------------------------------------------------------
        private void AvanceCorps(double dt, bool regarde)
        {
            // il tourne un peu vers ce que l'enfant regarde ; il saute quand un organe arrive
            double vers = regarde ? Borne((_rx - AX) / 560, -1, 1) : 0.5 * Math.Sin(_temps * 0.5);
            _tour += (vers - _tour) * Math.Min(1, dt * 4);
            if (_sautT < _sautD) { _sautT += dt; double u = Borne(_sautT / _sautD); _saut = _sautH * 4 * u * (1 - u); if (u >= 1) { _saut = 0; if (--_sauts > 0) _sautT = 0; } }
            else _saut = 0;
            double respire = 1.5 * Math.Sin(_temps * 2.1);
            foreach (var p in _pieces)
            {
                p.O.X = AX + p.X + p.Z * Relief * _tour; p.O.Y = p.Y - _saut + (p.Z > 0.1 ? respire * 0.3 : 0);
                if (p.Z == 0) p.O.SX = 1 - 0.035 * Math.Abs(_tour);
            }
            _socleLueur.E = 1 + .06 * Math.Sin(_temps * 2.4); _socleLueur.Op = .7 + .3 * Math.Sin(_temps * 2.4);

            // ses yeux suivent l'enfant ; sa bouche dit ce qu'il ressent
            double ex = AX, ey = 234 - _saut, tx = regarde ? _rx : AX + 300 * Math.Sin(_temps * .6), ty = regarde ? _ry : 300;
            double dx = tx - ex, dy = ty - ey, d = Math.Max(1, Math.Sqrt(dx * dx + dy * dy)), f = Math.Min(1, d / 200);
            _ox += (dx / d * f - _ox) * Math.Min(1, dt * 9); _oy += (dy / d * f - _oy) * Math.Min(1, dt * 9);
            _pupilleG.X += _ox * 8; _pupilleG.Y += _oy * 9; _pupilleD.X += _ox * 8; _pupilleD.Y += _oy * 9;
            bool content = _etat == Etat.Explique || _etat == Etat.Fete || _etat == Etat.Vol, curieux = _etat == Etat.Attend && _depuis < 1.6;
            _ravi.Op = content ? 1 : 0; _oh.Op = !content && curieux ? 1 : 0; _bouche.Op = content || curieux ? 0 : 1;

            // --- Les organes en place vivent ---
            foreach (var o in _organes)
            {
                if (!o.EnPlace || _etat == Etat.Vide) { if (_etat == Etat.Vide && o.EnPlace) o.O.E = Borne(1 - _depuis / 0.7, 0.01, 1); continue; }
                o.Age += dt;
                double pop = o.Age < 0.5 ? Rebond(o.Age / 0.45) : 1, sx = 1, sy = 1, r = 0;
                double x = AX + o.X + o.Z * Relief * _tour, y = o.Y - _saut;
                switch (o.Cle)
                {
                    case "coeur":
                    {
                        // deux battements serrés, une pause : « boum-boum »
                        double ph = (_temps * 1.25) % 1, bat = Math.Max(Cloche(ph, .06, .05), .7 * Cloche(ph, .24, .05));
                        sx = sy = 1 + .13 * bat;
                        if (ph < dt * 1.25) _globules.Lance(x + Entre(-10, 10), y + 30, Entre(-70, 70), Entre(40, 120), 0, .9, Entre(.6, 1));
                        break;
                    }
                    case "poumons":
                    {
                        double souffle = Math.Sin(_temps * 2.1);
                        sx = 1 + .06 * souffle; sy = 1 + .05 * souffle;
                        // l'air entre par le nez quand ils se gonflent
                        if (Math.Cos(_temps * 2.1) > 0.3 && Hasard() < dt * 9) _air.Lance(AX + 18 * _tour + Entre(-6, 6), 262 - _saut, Entre(-8, 8), 110, 0, .75, Entre(.5, .9));
                        break;
                    }
                    case "cerveau":
                        r = 1.5 * Math.Sin(_temps * 1.7);
                        if (Hasard() < dt * 2.2) _eclairs.Lance(x + Entre(-80, 80), y + Entre(-60, 30), Entre(-20, 20), Entre(-50, -10), 40, Entre(.4, .7), Entre(.7, 1.1), 0, Entre(-90, 90));
                        break;
                    case "estomac":
                        r = 3 * Math.Sin(_temps * 2.6); sx = 1 + .03 * Math.Sin(_temps * 5.2);
                        if (Hasard() < dt * 1.6) _bulles.Lance(x + Entre(-20, 20), y + 20, Entre(-10, 10), -40, 0, .7, Entre(.3, .5));
                        break;
                    case "foie": sx = sy = 1 + .025 * Math.Sin(_temps * 1.6); break;
                    case "intestins": sx = 1 + .035 * Math.Sin(_temps * 3.4); sy = 1 - .035 * Math.Sin(_temps * 3.4); break;
                    case "vessie": sy = 1 + .05 * Math.Sin(_temps * 1.3); sx = 1 + .03 * Math.Sin(_temps * 1.3 + 1); break;
                }
                o.O.X = x; o.O.Y = y; o.O.SX = pop * sx; o.O.SY = pop * sy; o.O.R = r;
            }

            // la pomme descend de la bouche à l'estomac
            if (_pommeT >= 0)
            {
                _pommeT += dt; var e = O("estomac");
                double u = Borne(_pommeT / 1.0);
                _pomme.X = Mel(AX + 20 * _tour, AX + e.X - 20 + e.Z * Relief * _tour, Doux(u)); _pomme.Y = Mel(282, e.Y - 20, u) - _saut;
                _pomme.E = Mel(1.1, .7, u); _pomme.R = 300 * u; _pomme.Op = Borne((1 - u) * 5);
                if (u >= 1) { _pommeT = -1; _pomme.Op = 0; for (int i = 0; i < 4; i++) _bulles.Lance(_pomme.X + Entre(-14, 14), _pomme.Y, Entre(-30, 30), Entre(-70, -30), 0, .7, Entre(.3, .6)); }
            }

            // --- Les endroits qui brillent ---
            foreach (var l in _lueurs)
            {
                var o = l.Pour;
                double x = AX + o.X + o.Z * Relief * _tour, y = o.Y - _saut;
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

        /// <summary>L'écran : chaque courbe reste plate tant que son organe manque, puis se met à vivre.</summary>
        private void AvanceEcran(double dt)
        {
            string[] cles = { "coeur", "poumons", "cerveau" };
            for (int i = 0; i < 3; i++)
            {
                bool vit = O(cles[i]).EnPlace && _etat != Etat.Vide;
                _courbes[i].Op = vit ? 1 : 0; _plates[i].Op = vit ? 0.25 : 1;
                if (!vit) continue;
                const int n = 68;
                var pts = new double[n * 2];
                double x0 = 1168 + 78, y0 = 150 + 64 + i * 87;
                for (int k = 0; k < n; k++)
                {
                    double x = k * 4.0, t = _temps - (n - k) * 0.03, v;
                    if (i == 0) { double ph = (t * 1.25) % 1; if (ph < 0) ph += 1; v = -30 * Cloche(ph, .06, .018) + 9 * Cloche(ph, .1, .02) - 8 * Cloche(ph, .24, .05); }
                    else if (i == 1) v = -20 * Math.Sin(t * 2.1);
                    else v = -9 * Math.Sin(t * 13) - 7 * Math.Sin(t * 21 + 1) - 5 * Math.Sin(t * 34 + 2);
                    pts[k * 2] = x0 + x; pts[k * 2 + 1] = y0 + v;
                }
                _courbes[i].Points = pts; _courbes[i].Etat++;
            }
        }
    }
}
