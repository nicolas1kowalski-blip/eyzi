using System;
using System.Collections.Generic;
using System.Globalization;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le moteur de l'activité « Le corps humain ».
    //
    //  Un corps humain translucide, comme dans une visionneuse d'anatomie, attend ses
    //  organes : le cerveau, les poumons, le cœur, l'estomac, le foie, les intestins,
    //  la vessie. La voix en nomme un ; l'enfant le remet à sa place. Une fois posé,
    //  l'organe se met à vivre (le cœur bat, les poumons se gonflent…), la voix dit à
    //  quoi il sert, et une étiquette reliée à l'organe s'ajoute à droite. Trois niveaux :
    //    0 — un seul endroit brille : il suffit de le regarder (sans échec) ;
    //    1 — trois endroits brillent : il faut trouver le bon ;
    //    2 — trois organes sont proposés : il faut reconnaître celui qu'on nomme.
    //  Le corps tourne un peu vers l'endroit qu'on regarde : le squelette, les organes
    //  et les côtes, à des profondeurs différentes, glissent les uns devant les autres.
    //
    //  Comme les autres jeux « dessinés », le moteur ne connaît ni l'écran ni
    //  Windows (voir VecMoteur.cs) : le banc d'essai en tire de vraies images.
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class CorpsMoteur : VecMoteur
    {
        private const double AX = 800;                     // l'axe du corps
        private const double Relief = 17;                  // de combien glisse ce qui est tout devant quand il tourne
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public readonly VecCalque Decor = new VecCalque(), Corps = new VecCalque(), Interface = new VecCalque(), Effets = new VecCalque();
        private readonly VecCalque[] _calques;
        public override VecCalque[] Calques => _calques;
        public override Dictionary<string, VecForme[]> Dessins => CorpsArt.Dessins;
        protected override VecCalque CalqueDesMots => Effets;
        public override double Horizon => 450;

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
            public VecObjet O, Etiquette, Nom2, Point, Fil; public int Rang;
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
        private double _depuis, _tour, _muet, _balayage = -1, _tourne;
        private Organe _cible;
        private readonly VecObjet _barre, _pouls, _plat;
        private readonly VecObjet[] _ondes = new VecObjet[4];
        private readonly double[] _ondeAge = { 9, 9, 9, 9 };
        private readonly double[] _ondeX = new double[4], _ondeY = new double[4];
        private int _onde, _poses;
        private VecObjet _etiquette;
        private double _volX0, _volY0, _volE0;
        private readonly Bac _etoiles, _air, _globules;

        public CorpsMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Decor, Corps, Interface, Effets };
            var ciel = CorpsArt.Ciels["studio"]; CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            foreach (var p in CorpsArt.Scenes["studio"]) Decor.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = p.Dessin }.En(p.X, p.Y, p.E, p.R));
            foreach (var l in CorpsArt.Listes["organes"])
                _organes.Add(new Organe { Cle = l[0], Nom = l[1], X = double.Parse(l[2], Inv), Y = double.Parse(l[3], Inv), L = double.Parse(l[4], Inv), H = double.Parse(l[5], Inv), Z = double.Parse(l[6], Inv), Dit = l[7], Couleur = l[8] });

            // --- Le corps, son intérieur, le squelette à peine visible, les organes (du plus profond au plus proche), les côtes, les reflets ---
            Morceau("r_corps", 0, 0, 0); Morceau("r_interieur", 0, 0, 0.1); Morceau("r_squelette", 0, 0, 0.18);
            var parProfondeur = new List<Organe>(_organes); parProfondeur.Sort((a, b) => a.Z.CompareTo(b.Z));
            foreach (var o in parProfondeur) { o.O = Pose(Corps, "o_" + o.Cle, AX + o.X, o.Y); o.O.Op = 0; }
            _globules = new Bac(this, Corps, "c_globule", 14);
            _air = new Bac(this, Corps, "c_air", 14);
            Morceau("r_cotes", 0, 0, 0.85); Morceau("r_reflets", 0, 0, 1.0);
            for (int i = 0; i < _ondes.Length; i++) { _ondes[i] = Pose(Corps, "r_onde", 0, 0); _ondes[i].Op = 0; }
            _barre = Pose(Corps, "r_balayage", AX, 0); _barre.Op = 0;

            // --- À droite : une étiquette par organe posé, reliée à lui par un fil ; en bas, le pouls ---
            foreach (var o in _organes)
            {
                o.Fil = Interface.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#B8F2FF", Epaisseur = 2.5, Op = 0 });
                o.Point = Pose(Interface, "r_point", 0, 0); o.Point.Op = 0;
                o.Etiquette = Pose(Interface, "r_etiquette", 1200, 0, 1, "c", o.Couleur); o.Etiquette.Op = 0;
                o.Nom2 = Interface.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = o.Nom, Taille = 30, Couleur = "#FFFFFF", Op = 0 });
            }
            Pose(Interface, "r_pouls", 1170, 724);
            _plat = Interface.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#5B8FD6", Epaisseur = 4, Points = new[] { 1240.0, 789, 1536, 789 } });
            _pouls = Interface.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#FF6A7E", Epaisseur = 5, Op = 0 });

            _etoiles = new Bac(this, Effets, "r_etincelle", 50);
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
            foreach (var o in _organes) { o.EnPlace = false; o.O.Op = 0; o.Etiquette.Op = o.Nom2.Op = o.Point.Op = o.Fil.Op = 0; }
            _ordre.Clear(); _poses = 0; _balayage = 0;
            if (Niveau == 0) foreach (var c in new[] { "cerveau", "coeur", "poumons", "estomac", "intestins" }) _ordre.Add(O(c));      // pour commencer : les cinq plus connus, toujours dans le même ordre
            else
            {
                _ordre.AddRange(_organes);
                for (int i = _ordre.Count - 1; i > 0; i--) { int j = _alea.Next(i + 1); var t = _ordre[i]; _ordre[i] = _ordre[j]; _ordre[j] = t; }
            }
            _rang = 0; _fautesManche = 0;
            _etat = Etat.Ouvre; _depuis = 0;
            Dit(_premiere ? "Voici le corps humain. Il lui manque ses organes ! Remets chacun à sa place." : "On recommence ! Remets chaque organe à sa place.");
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
                _etiquette = Interface.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = _cible.Nom, Taille = 50, Couleur = "#FFFFFF" }.En(250, 574));
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
            l.Bord = Corps.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#0E1528", Epaisseur = 9, Tiret = 1.6, Espace = 1.6 });
            l.Tirets = Corps.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#B8F2FF", Epaisseur = 6, Tiret = 2.4, Espace = 2.4 });
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
            double x = AX + c.X + c.Z * Relief * _tour, y = c.Y;
            for (int i = 0; i < 10; i++) { double a = Math.PI * 2 * i / 10 + Hasard(); _etoiles.Lance(x, y, Math.Cos(a) * 220 * Entre(.6, 1), Math.Sin(a) * 220 * Entre(.6, 1), 0, Entre(.5, .8), Entre(.6, 1.2), 0, Entre(-200, 200)); }
            Onde(x, y); Son("note", 2 + _rang);
            Reussites++;
            // son étiquette s'ajoute à droite, reliée à lui
            c.Rang = _poses++;
            c.Etiquette.Y = c.Nom2.Y = 96 + c.Rang * 84;
            c.Etiquette.Op = c.Nom2.Op = c.Point.Op = c.Fil.Op = 1;
            if (c.Cle == "vessie") Son("plouf", 0);
            Dit(c.Dit);
            _etat = Etat.Explique; _depuis = 0;
        }

        /// <summary>Une onde de lumière qui s'élargit autour d'un point.</summary>
        private void Onde(double x, double y) { int i = _onde++ % _ondes.Length; _ondeAge[i] = 0; _ondeX[i] = x; _ondeY[i] = y; }

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
                    double x = Mel(_volX0, AX + _cible.X + _cible.Z * Relief * _tour, w), y = Mel(_volY0, _cible.Y, w) - 130 * 4 * u * (1 - u);
                    foreach (var o in _aRetirer) { o.X = x; o.Y = y; o.E = Mel(_volE0, 1, w) * (1 + .2 * Math.Sin(Math.PI * u)); o.R = 14 * Math.Sin(Math.PI * 2 * u); }
                    if (Hasard() < dt * 30) _etoiles.Lance(x + Entre(-20, 20), y + Entre(-20, 20), 0, 20, 0, .4, Entre(.3, .6), 0, 200);
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
                            _tourne = 5.2; _balayage = 0;
                            Bravo();
                        }
                    }
                    break;
                case Etat.Fete:
                    if (Math.Floor(_depuis / 0.45) != Math.Floor((_depuis - dt) / 0.45)) { var c = O("coeur"); Onde(AX + c.X + c.Z * Relief * _tour, c.Y); for (int i = 0; i < 6; i++) _etoiles.Lance(AX + Entre(-260, 260), Entre(60, 760), Entre(-30, 30), Entre(-60, -10), 0, Entre(.6, 1), Entre(.6, 1.3), 0, Entre(-200, 200)); }
                    if (_depuis > 5.2 && (!VoixOccupee() || _depuis > 9)) { _etat = Etat.Vide; _depuis = 0; Son("pof", 0); _balayage = 0; }
                    break;
                case Etat.Vide:
                    // le balayage repasse : les organes s'éteignent, leurs étiquettes aussi
                    foreach (var o in _organes) if (o.EnPlace) { double op = Borne(1 - _depuis / 0.8); o.O.Op = op; o.Etiquette.Op = o.Nom2.Op = o.Point.Op = o.Fil.Op = op; }
                    if (_depuis >= 1.1) NouvelleManche();
                    break;
            }

            AvanceCorps(dt, regarde);
            AvanceCartes(dt);
            AvanceEcran(dt);
            _etoiles.Avance(dt); _air.Avance(dt); _globules.Avance(dt);
            AvanceMots(dt);
        }

        // ------------------------------------------------------------------
        //  Le bonhomme et ses organes
        // ------------------------------------------------------------------
        private void AvanceCorps(double dt, bool regarde)
        {
            // --- Le corps tourne un peu vers ce que l'enfant regarde ; à la fin, il se montre sous tous les angles ---
            double vers = regarde ? Borne((_rx - AX) / 560, -1, 1) : 0.6 * Math.Sin(_temps * 0.5);
            if (_tourne > 0) { _tourne -= dt; vers = Math.Sin(_temps * 2.2); }
            _tour += (vers - _tour) * Math.Min(1, dt * 4);
            bool respire = O("poumons").EnPlace;
            double souffle = respire ? Math.Sin(_temps * 2.1) : 0;
            foreach (var p in _pieces)
            {
                p.O.X = AX + p.X + p.Z * Relief * _tour; p.O.Y = p.Y;
                if (p.Z == 0) p.O.SX = 1 - 0.04 * Math.Abs(_tour);                       // de trois quarts, il paraît un peu plus étroit
                if (p.O.Dessin == "r_cotes") p.O.SX = 1 + .012 * souffle;
            }

            // --- Le balayage : une barre de lumière descend le long du corps ---
            if (_balayage >= 0)
            {
                _balayage += dt;
                double u = _balayage / 1.5;
                _barre.Y = Mel(20, 900, u); _barre.X = AX; _barre.Op = u < 1 ? Math.Min(1, Math.Min(u * 6, (1 - u) * 6)) : 0;
                if (u >= 1) _balayage = -1;
            }
            for (int i = 0; i < _ondes.Length; i++)
            {
                if (_ondeAge[i] > 0.8) { _ondes[i].Op = 0; continue; }
                _ondeAge[i] += dt; double u = _ondeAge[i] / 0.8;
                _ondes[i].X = _ondeX[i]; _ondes[i].Y = _ondeY[i]; _ondes[i].E = 0.3 + 1.9 * Sortant(u); _ondes[i].Op = Borne(1 - u) * 0.9;
            }

            // --- Les organes en place vivent ---
            foreach (var o in _organes)
            {
                if (!o.EnPlace) continue;
                o.Age += dt;
                double pop = o.Age < 0.5 ? Rebond(o.Age / 0.45) : 1, sx = 1, sy = 1, r = 0;
                double x = AX + o.X + o.Z * Relief * _tour, y = o.Y;
                switch (o.Cle)
                {
                    case "coeur":
                    {
                        // deux battements serrés, une pause : « boum-boum »
                        double ph = (_temps * 1.25) % 1, bat = Math.Max(Cloche(ph, .06, .05), .7 * Cloche(ph, .24, .05));
                        sx = sy = 1 + .12 * bat;
                        if (ph < dt * 1.25) for (int i = 0; i < 2; i++) _globules.Lance(x + Entre(-8, 8), y + Entre(-40, 40), Entre(-90, 90), Entre(-60, 140), 0, .8, Entre(.6, 1));
                        break;
                    }
                    case "poumons":
                        sx = 1 + .05 * souffle; sy = 1 + .04 * souffle;
                        // l'air descend par la trachée quand ils se gonflent
                        if (Math.Cos(_temps * 2.1) > 0.3 && Hasard() < dt * 9) _air.Lance(x + Entre(-4, 4), 170, Entre(-6, 6), 150, 0, .7, Entre(.6, 1));
                        break;
                    case "cerveau":
                        if (Hasard() < dt * 3) _etoiles.Lance(x + Entre(-46, 46), y + Entre(-36, 30), 0, 0, 0, Entre(.3, .5), Entre(.3, .6), 0, Entre(-90, 90));
                        break;
                    case "estomac": r = 2.5 * Math.Sin(_temps * 2.6); sx = 1 + .03 * Math.Sin(_temps * 5.2); break;
                    case "foie": sx = sy = 1 + .02 * Math.Sin(_temps * 1.6); break;
                    case "intestins": sx = 1 + .03 * Math.Sin(_temps * 3.4); sy = 1 - .03 * Math.Sin(_temps * 3.4); break;
                    case "vessie": sy = 1 + .05 * Math.Sin(_temps * 1.3); sx = 1 + .03 * Math.Sin(_temps * 1.3 + 1); break;
                }
                o.O.X = x; o.O.Y = y; o.O.SX = pop * sx; o.O.SY = pop * sy; o.O.R = r;

                // son étiquette : le fil part de l'organe et rejoint la pastille
                double ex = 1200 + 30 * (1 - Sortant(o.Age / 0.4)), ey = o.Etiquette.Y, px = x + o.L * 0.34, py = y - o.H * 0.1;
                o.Etiquette.X = ex; o.Nom2.X = ex + 180;
                o.Point.X = px; o.Point.Y = py; o.Point.E = 1 + .15 * Math.Sin(_temps * 4 + o.Rang);
                o.Fil.Points = new[] { px, py, ex - 70, ey, ex, ey }; o.Fil.Etat++;
            }

            // --- Les endroits qui brillent ---
            foreach (var l in _lueurs)
            {
                var o = l.Pour;
                double x = AX + o.X + o.Z * Relief * _tour, y = o.Y;
                bool vu = _etat == Etat.Attend || _etat == Etat.Presente;
                double bat = .5 + .5 * Math.Sin(_temps * 5), doux = Niveau == 2 ? .6 : 1;
                if (l.Faux >= 0)
                {
                    // ce n'était pas là : une croix, puis l'endroit s'éteint
                    l.Faux += dt;
                    l.Non.X = x + 8 * Math.Sin(l.Faux * 40) * Borne(1 - l.Faux * 2); l.Non.Y = y; l.Non.Op = Borne((1.2 - l.Faux) * 3); l.Non.E = Rebond(l.Faux / 0.3);
                    l.Halo.Op = 0; l.Bord.Op = 0; l.Tirets.Op = 0;
                    continue;
                }
                l.Halo.X = x; l.Halo.Y = y; l.Halo.SX = (o.L / 2 + 30) / 100 * (1 + .08 * bat); l.Halo.SY = (o.H / 2 + 26) / 100 * (1 + .08 * bat); l.Halo.Op = vu ? (.7 + .3 * bat) * doux : 0;
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

        /// <summary>Le pouls : la ligne reste plate tant que le cœur manque, puis se met à battre.</summary>
        private void AvanceEcran(double dt)
        {
            bool vit = O("coeur").EnPlace && _etat != Etat.Vide;
            _pouls.Op = vit ? 1 : 0; _plat.Op = vit ? 0.2 : 1;
            if (!vit) return;
            const int n = 75;
            var pts = new double[n * 2];
            for (int k = 0; k < n; k++)
            {
                double t = _temps - (n - k) * 0.03, ph = (t * 1.25) % 1; if (ph < 0) ph += 1;
                pts[k * 2] = 1240 + k * 4.0; pts[k * 2 + 1] = 789 - 38 * Cloche(ph, .06, .018) + 11 * Cloche(ph, .1, .02) - 10 * Cloche(ph, .24, .05);
            }
            _pouls.Points = pts; _pouls.Etat++;
        }
    }
}
