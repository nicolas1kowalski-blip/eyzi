using System;
using System.Collections.Generic;
using System.Globalization;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le moteur du jeu « Chamboule-tout ».
    //
    //  On est devant le stand de la fête foraine. Les boîtes de conserve sont
    //  empilées sur la table du fond ; l'enfant en regarde une, un rond se
    //  remplit autour d'elle, et la balle part du comptoir. La boîte touchée
    //  s'envole, celles qu'elle portait dégringolent, rebondissent sur la table
    //  et tombent. Tout est tombé : on gagne une peluche, et de nouvelles boîtes
    //  arrivent.
    //
    //  Comme les autres jeux « dessinés », le moteur ne connaît ni l'écran ni
    //  Windows (voir VecMoteur.cs) : le banc d'essai en tire de vraies images.
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class ChambouleMoteur : VecMoteur
    {
        private const double TableY = 642;                    // là où posent les boîtes
        private const double TableG = 326, TableD = 1274;     // les bords du plateau
        private const double PretX = 800, PretY = 764, PretE = 1.2;   // la balle qui attend sur le comptoir
        private const double Pesanteur = 2600;
        private const double TailleLot = 0.74;                // les peluches sur leurs étagères
        private const double Vol = 0.38;                      // le temps que met la balle pour arriver
        private const string Consigne = "Regarde une boîte… et la balle part ! Fais tout tomber !";
        private static readonly double[] Tailles = { 1.16, 0.96, 0.82 };   // les boîtes rapetissent d'un niveau à l'autre
        private static readonly double[] Portees = { 1.5, 1.2, 1.0 };      // … et il faut les regarder de plus près
        private static readonly string[][] Ampoules = { new[] { "#FFD23D", "#FFF6C4" }, new[] { "#FF6A5C", "#FFD6D0" }, new[] { "#5CB6FF", "#DCF0FF" }, new[] { "#6CD67A", "#DCF8DE" } };
        private static readonly double[] Places = { 1030, 1172, 1314, 1456 };   // les lots gagnés, sur le comptoir

        // Les calques, du fond vers le devant : le rideau, ce qui vole derrière la table, la table,
        // les boîtes, le stand (étagères, lots, auvent, ampoules), le comptoir, ce qui vole devant.
        public readonly VecCalque Fond = new VecCalque(), Derriere = new VecCalque(), Meuble = new VecCalque(), Boites = new VecCalque(), Stand = new VecCalque(), Devant = new VecCalque(), Effets = new VecCalque();
        private readonly VecCalque[] _calques;
        public override VecCalque[] Calques => _calques;
        public override Dictionary<string, VecForme[]> Dessins => ChambouleArt.Dessins;
        protected override VecCalque CalqueDesMots => Effets;
        public override double Horizon => 822;

        /// <summary>Le niveau, de 0 à 2 : le nombre et la taille des boîtes (l'application le retient d'une fois sur l'autre).</summary>
        public int Niveau { get => _niveau; set => _niveau = Math.Max(0, Math.Min(2, value)); }
        private int _niveau;
        /// <summary>La dernière manche a été bien menée : peu de balles pour beaucoup de boîtes.</summary>
        public bool MancheReussie { get; private set; }

        private enum Mode { Arrive, Debout, Vole, Tombe, Parti }
        private sealed class Boite
        {
            public VecObjet O, Em, Ombre;
            public Mode Mode;
            public int Rang, Rebonds;
            public double X, Y, A, VX, VY, VA, E, E0, PoseY, Retard, Age, Lache = -1, Sens, Ecrase = 9;
            public bool Avant;        // elle tombe par le devant de la table
            public readonly List<Boite> Porte = new List<Boite>();   // les boîtes posées sur elle
        }
        private sealed class Lot { public VecObjet O; public string Dessin, Nom; public double X, Y, Saut = 9, Phase; }
        private sealed class Gagne { public VecObjet O; public int Place; public double T = 9, X0, Y0; }

        private readonly List<Boite> _boites = new List<Boite>();
        private readonly List<Lot> _lots = new List<Lot>();
        private readonly Gagne[] _gagnes = new Gagne[4];
        private readonly VecObjet[] _ampoules;
        private readonly VecObjet _pret, _pretOmbre, _balle, _balleLoin, _impact, _anneauBord, _anneauFond, _anneau, _faisceau;
        private readonly Bac _etoiles, _poussieres;
        private readonly Bac[] _confettis;

        private enum Etat { Entree, Jeu, Fin }
        private Etat _etat;
        private bool _ouvert;
        private int _manche, _balles, _total, _tombeesAuLancer, _dispo = -1, _lotGagne;
        private double _depuis, _sansLancer, _e = 1, _prochainBruit, _aide;
        private Boite _cible, _visee, _auto;
        private double _progres, _impactAge = 9, _bilan = -1;
        // la balle
        private double _volT = -1, _ax, _ay, _bx, _by, _bvx, _bvy, _be, _recharge, _roule = 9;
        private bool _loin;

        public ChambouleMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Fond, Derriere, Meuble, Boites, Stand, Devant, Effets };
            Pose(Fond, "d_fond", 0, 0);
            Pose(Meuble, "d_table", 0, 0);

            Pose(Stand, "d_cotes", 0, 0);
            foreach (var l in ChambouleArt.Listes["lots"])
            {
                var lot = new Lot { Dessin = l[0], Nom = l[1], X = double.Parse(l[2], CultureInfo.InvariantCulture), Y = double.Parse(l[3], CultureInfo.InvariantCulture), Phase = _lots.Count * 1.7 };
                lot.O = Pose(Stand, lot.Dessin, lot.X, lot.Y, TailleLot);
                _lots.Add(lot);
            }
            Pose(Stand, "d_auvent", 0, 0);
            _ampoules = new VecObjet[16];
            for (int i = 0; i < 16; i++) { var c = Ampoules[i % Ampoules.Length]; _ampoules[i] = Pose(Stand, "ampoule", 50 + i * 100, 161, 1, "c", c[0], "clair", c[1]); }

            Pose(Devant, "d_comptoir", 0, 0);
            Pose(Devant, "panier", 268, 806);
            _faisceau = Pose(Devant, "faisceau", 0, 812); _faisceau.Op = 0;
            _pretOmbre = Pose(Devant, "balle_ombre", PretX, 816, 1.3);
            _pret = Pose(Devant, "balle", PretX, PretY, PretE);

            _anneauBord = Effets.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#2A1C4E", Epaisseur = 17, Op = 0 });
            _anneauFond = Effets.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#FFFFFF", Epaisseur = 9, Op = 0 });
            _anneau = Effets.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#FFD23D", Epaisseur = 10, Op = 0 });
            _balleLoin = Pose(Derriere, "balle", 0, 0); _balleLoin.Op = 0;
            _poussieres = new Bac(this, Effets, "poussiere", 18);
            _impact = Pose(Effets, "impact", 0, 0); _impact.Op = 0;
            _balle = Pose(Effets, "balle", 0, 0); _balle.Op = 0;
            _etoiles = new Bac(this, Effets, "etoile", 36);
            _confettis = new[] { new Bac(this, Effets, "confetti_a", 16), new Bac(this, Effets, "confetti_b", 16), new Bac(this, Effets, "confetti_c", 16), new Bac(this, Effets, "confetti_d", 16) };

            var ciel = ChambouleArt.Ciels["stand"];
            CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
        }

        public override void Demarre(int scene = 0)
        {
            if (scene > 0) Niveau = scene;      // le banc d'essai choisit le niveau ainsi
            _ouvert = true; _manche = 0;
            Installe();
        }

        // ------------------------------------------------------------------
        //  Les boîtes d'une manche
        // ------------------------------------------------------------------
        /// <summary>Les dispositions d'un niveau : pour chacune, les boîtes (colonne en largeurs de boîte, rangée).</summary>
        private List<double[]> Disposition(int niveau, int n)
        {
            var p = new List<double[]>();
            Action<double, int, int> pyramide = (x0, baseN, rangs) => { for (int r = 0; r < rangs; r++) for (int c = 0; c < baseN - r; c++) p.Add(new[] { x0 + c + r * 0.5, r }); };
            Action<double, int> tour = (x, h) => { for (int r = 0; r < h; r++) p.Add(new[] { x, (double)r }); };
            switch (niveau)
            {
                case 0:
                    switch (n % 4)
                    {
                        case 0: pyramide(0, 3, 3); break;                                         // 6
                        case 1: tour(0, 2); tour(1.7, 3); tour(3.4, 2); break;                    // 7
                        case 2: pyramide(0, 2, 2); pyramide(2.9, 2, 2); break;                    // 6
                        default: pyramide(0, 4, 2); break;                                        // 7
                    }
                    break;
                case 1:
                    switch (n % 5)
                    {
                        case 0: pyramide(0, 4, 4); break;                                         // 10
                        case 1: tour(0, 3); tour(1.6, 4); tour(3.2, 3); tour(4.8, 4); break;      // 14
                        case 2: pyramide(0, 3, 3); pyramide(3.8, 3, 3); break;                    // 12
                        case 3: pyramide(0, 4, 3); tour(-1.5, 3); tour(4.5, 3); break;            // 15
                        default: tour(0, 4); pyramide(1.5, 3, 3); tour(5, 4); break;              // 14
                    }
                    break;
                default:
                    switch (n % 5)
                    {
                        case 0: pyramide(0, 5, 4); break;                                         // 14
                        case 1: tour(0, 4); tour(1.5, 3); tour(3, 4); tour(4.5, 3); tour(6, 4); break;   // 18
                        case 2: pyramide(0, 3, 3); tour(3.6, 4); pyramide(5.2, 3, 3); break;      // 16
                        case 3: pyramide(0, 6, 3); break;                                         // 15
                        default: tour(0, 4); pyramide(1.5, 4, 4); tour(6, 4); break;              // 18
                    }
                    break;
            }
            return p;
        }

        private void Installe()
        {
            foreach (var b in _boites) Enleve(b);
            _boites.Clear();
            _e = Tailles[_niveau];
            int choix; do { choix = _alea.Next(5); } while (choix == _dispo);
            _dispo = choix;
            var places = Disposition(_niveau, choix);
            double pas = 100 * _e + 7, min = double.MaxValue, max = double.MinValue;
            foreach (var q in places) { min = Math.Min(min, q[0]); max = Math.Max(max, q[0]); }
            double gauche = 800 - (max - min) * pas / 2;
            places.Sort((u, v) => u[1] != v[1] ? u[1].CompareTo(v[1]) : u[0].CompareTo(v[0]));   // du bas vers le haut : celles du dessus se dessinent devant
            var etiquettes = ChambouleArt.Listes["etiquettes"]; var emblemes = ChambouleArt.Listes["emblemes"];
            int couleur = _alea.Next(etiquettes.Length), embleme = _alea.Next(emblemes.Length), i = 0;
            foreach (var q in places)
            {
                int rang = (int)q[1];
                var et = etiquettes[(couleur + i * 3) % etiquettes.Length];
                var b = new Boite { Rang = rang, X = gauche + (q[0] - min) * pas, PoseY = TableY - 62 * _e - rang * 116 * _e, E0 = _e, E = _e, Mode = Mode.Arrive, Retard = 0.25 + rang * 0.2 + (i % 7) * 0.045 };
                if (rang == 0) { b.Ombre = Pose(Meuble, "boite_ombre", b.X, TableY + 2, _e); b.Ombre.Op = 0; }
                b.O = Pose(Boites, "boite", b.X, -200, _e, "coul", et[0], "clair", et[1], "fonce", et[2]); b.O.Op = 0;
                b.Em = Pose(Boites, emblemes[(embleme + i) % emblemes.Length][0], b.X, -200, _e); b.Em.Op = 0;
                _boites.Add(b); i++;
            }
            foreach (var b in _boites)
                foreach (var d in _boites)
                    if (d.Rang == b.Rang + 1 && Math.Abs(d.X - b.X) < pas * 0.8) b.Porte.Add(d);
            _total = _boites.Count; _balles = 0;
            _etat = Etat.Entree; _depuis = 0; _sansLancer = 0; _cible = null; _visee = null; _auto = null; _progres = 0; _bilan = -1;
        }

        private void Enleve(Boite b)
        {
            Boites.Retire(b.O); Boites.Retire(b.Em); Derriere.Retire(b.O); Derriere.Retire(b.Em);
            if (b.Ombre != null) { Meuble.Retire(b.Ombre); b.Ombre = null; }
            b.Mode = Mode.Parti;
        }

        private void Bruit(string nom, int n = 0)
        {
            if (_temps < _prochainBruit) return;
            _prochainBruit = _temps + 0.07;
            Son(nom, n);
        }

        // ------------------------------------------------------------------
        //  Une image de plus
        // ------------------------------------------------------------------
        public override void Avance(double dt)
        {
            if (!_ouvert) return;
            dt = Math.Min(dt, 0.05);
            _temps += dt; _depuis += dt;

            AvanceDecor(dt);
            AvanceBoites(dt);
            AvanceBalle(dt);
            AvanceVisee(dt);
            AvanceLots(dt);

            switch (_etat)
            {
                case Etat.Entree:
                    if (_boites.TrueForAll(b => b.Mode != Mode.Arrive))
                    {
                        _etat = Etat.Jeu; _depuis = 0; _sansLancer = 0;
                        if (_manche == 0) Dit(Consigne);
                    }
                    break;

                case Etat.Jeu:
                    _sansLancer += dt;
                    // un petit coup de pouce : une boîte du bas scintille, et la voix redit quoi faire
                    if (_sansLancer > 9 && _temps > _aide)
                    {
                        _aide = _temps + 1.1;
                        var b = _boites.Find(q => q.Mode == Mode.Debout && q.Rang == 0) ?? _boites.Find(q => q.Mode == Mode.Debout);
                        if (b != null) for (int k = 0; k < 5; k++) _etoiles.Lance(b.X + Entre(-50, 50) * _e, b.Y + Entre(-60, 60) * _e, Entre(-60, 60), Entre(-160, -60), 200, 0.8, Entre(0.5, 0.9), 0, Entre(-200, 200));
                        if (_sansLancer > 22 && !VoixOccupee()) { Dit(Consigne); _sansLancer = 9; }
                    }
                    if (_bilan >= 0 && _temps >= _bilan) Bilan();
                    if (_volT < 0 && _boites.TrueForAll(b => b.Mode != Mode.Debout)) Fini();
                    break;

                case Etat.Fin:
                    AvanceFin(dt);
                    break;
            }

            _etoiles.Avance(dt); _poussieres.Avance(dt);
            foreach (var c in _confettis) c.Avance(dt);
            AvanceMots(dt);
        }

        private void AvanceDecor(double dt)
        {
            bool fete = _etat == Etat.Fin && _depuis < 3.4;
            for (int i = 0; i < _ampoules.Length; i++)
            {
                double v = fete ? (((int)(_temps * 9) + i) % 3 == 0 ? 1 : 0.35) : 0.82 + 0.18 * Math.Sin(_temps * 2.2 + i * 1.9);
                _ampoules[i].Op += (v - _ampoules[i].Op) * Math.Min(1, dt * 14);
                _ampoules[i].R = 3 * Math.Sin(_temps * 1.3 + i * 0.8);
            }
            if (_impactAge < 0.22)
            {
                _impactAge += dt;
                double u = _impactAge / 0.22;
                _impact.E = _e * (0.32 + 0.5 * Sortant(u)); _impact.R += 240 * dt; _impact.Op = Borne((1 - u) * 2.2);
            }
            else _impact.Op = 0;
        }

        // ------------------------------------------------------------------
        //  Les boîtes : elles arrivent, tiennent debout, s'envolent ou dégringolent
        // ------------------------------------------------------------------
        private void AvanceBoites(double dt)
        {
            for (int n = _boites.Count - 1; n >= 0; n--)
            {
                var b = _boites[n];
                b.Age += dt;
                switch (b.Mode)
                {
                    case Mode.Arrive:
                    {
                        double u = (b.Age - b.Retard) / 0.4;
                        if (u < 0) break;
                        if (u >= 1)
                        {
                            b.Mode = Mode.Debout; b.Y = b.PoseY; b.Ecrase = 0;
                            if (b.Ombre != null) b.Ombre.Op = 1;
                            Bruit("boite", b.Rang % 3);
                            _poussieres.Lance(b.X - 40 * _e, b.Y + 56 * _e, -60, -20, 0, 0.35, 0.6 * _e, 0.8);
                            _poussieres.Lance(b.X + 40 * _e, b.Y + 56 * _e, 60, -20, 0, 0.35, 0.6 * _e, 0.8);
                        }
                        else { b.Y = b.PoseY - 640 * (1 - u * u); if (b.Ombre != null) b.Ombre.Op = u * u; }
                        b.O.Op = 1; b.Em.Op = 1;
                        break;
                    }

                    case Mode.Debout:
                        b.Y = b.PoseY;
                        if (b.Lache >= 0)
                        {
                            b.Lache -= dt;
                            b.A = b.Sens * 7 * (1 - Math.Max(0, b.Lache) / 0.2);      // elle penche avant de partir
                            if (b.Lache < 0) Degringole(b);
                        }
                        else b.A = b == _cible ? 2.4 * Math.Sin(_temps * 17) * _progres : 0;
                        break;

                    case Mode.Vole:        // touchée par la balle : elle part en arrière, derrière la table
                        b.VY += Pesanteur * dt; b.X += b.VX * dt; b.Y += b.VY * dt; b.A += b.VA * dt;
                        b.E = Math.Max(0.74 * b.E0, b.E - 0.55 * b.E0 * dt);
                        if (b.Y > 1000) { Enleve(b); continue; }
                        break;

                    case Mode.Tombe:       // elle n'est plus portée : elle tombe, rebondit sur la table et s'en va
                    {
                        b.VY += Pesanteur * dt; b.X += b.VX * dt; b.Y += b.VY * dt; b.A += b.VA * dt;
                        double a = b.A * Math.PI / 180, demi = (Math.Abs(Math.Cos(a)) * 62 + Math.Abs(Math.Sin(a)) * 50) * b.E;
                        if (b.Avant) b.E = Math.Min(1.3 * b.E0, b.E + 0.5 * b.E0 * dt);
                        else if (b.VY > 0 && b.Y + demi >= TableY && b.Y + demi < TableY + 60 && b.X > TableG && b.X < TableD)
                        {
                            b.Y = TableY - demi; b.Rebonds++;
                            Bruit("boite", _alea.Next(3));
                            _poussieres.Lance(b.X, TableY, Entre(-80, 80), -30, 0, 0.3, 0.7 * b.E, 0.9);
                            if (b.Rebonds == 1 && Hasard() < 0.42)
                            {
                                // elle bascule par le devant de la table
                                b.Avant = true; b.VY = -Entre(260, 420); b.VX *= 0.45; b.VA = b.Sens * Entre(160, 320);
                            }
                            else
                            {
                                double vers = Math.Abs(b.VX) > 50 ? Math.Sign(b.VX) : (b.X < 800 ? -1 : 1);
                                b.VY = -Math.Max(Entre(190, 300), b.VY * (b.Rebonds == 1 ? 0.4 : 0.3));
                                b.VX = vers * Math.Max(Math.Abs(b.VX), Entre(330, 520));
                                b.VA = vers * Entre(240, 460);
                            }
                        }
                        if (b.Y > 1000) { Enleve(b); continue; }
                        break;
                    }
                }
                if (b.Mode == Mode.Parti) continue;
                double ecrase = 1;
                if (b.Ecrase < 0.3) { b.Ecrase += dt; ecrase = 0.9 + 0.1 * Rebond(b.Ecrase / 0.3); }
                b.O.X = b.X; b.O.Y = b.Y + (1 - ecrase) * 62 * b.E; b.O.R = b.A; b.O.SX = b.E * (2 - ecrase); b.O.SY = b.E * ecrase;
                b.Em.X = b.O.X; b.Em.Y = b.O.Y; b.Em.R = b.A; b.Em.SX = b.O.SX; b.Em.SY = b.O.SY;
            }
        }

        /// <summary>La balle touche cette boîte : elle s'envole, et ce qu'elle portait va tomber.</summary>
        private void Frappe(Boite b, double ax)
        {
            double sens = Math.Abs(b.X - ax) > 6 * _e ? Math.Sign(b.X - ax) : (Hasard() < 0.5 ? -1 : 1);
            b.Mode = Mode.Vole; b.Lache = -1; b.Sens = sens;
            b.VX = sens * Entre(140, 420); b.VY = -Entre(560, 820); b.VA = sens * Entre(380, 760);
            Boites.Retire(b.O); Boites.Retire(b.Em); Derriere.Ajoute(b.O); Derriere.Ajoute(b.Em);
            Quitte(b);
        }

        /// <summary>Cette boîte n'est plus debout : son ombre s'efface, et les boîtes posées sur elle vont suivre.</summary>
        private void Quitte(Boite b)
        {
            if (b.Ombre != null) { Meuble.Retire(b.Ombre); b.Ombre = null; }
            Reussites++;
            foreach (var d in b.Porte)
            {
                if (d.Mode != Mode.Debout || d.Lache >= 0) continue;
                d.Sens = Math.Abs(b.X - d.X) > 4 ? Math.Sign(b.X - d.X) : (b.Sens != 0 ? b.Sens : 1);
                d.Lache = Entre(0.07, 0.2);
            }
        }

        private void Degringole(Boite b)
        {
            b.Mode = Mode.Tombe; b.Rebonds = 0;
            b.VX = b.Sens * Entre(50, 190); b.VY = Entre(-90, 30); b.VA = b.Sens * Entre(90, 250);
            // elle passe devant les boîtes encore debout
            Boites.Retire(b.O); Boites.Retire(b.Em); Boites.Ajoute(b.O); Boites.Ajoute(b.Em);
            Quitte(b);
        }

        // ------------------------------------------------------------------
        //  Le regard : la boîte visée, le rond qui se remplit, le lancer
        // ------------------------------------------------------------------
        private void AvanceVisee(double dt)
        {
            Boite cible = null;
            bool pret = _etat == Etat.Jeu && _volT < 0 && _recharge <= 0;
            if (pret && _temps - _vu < 0.35)
            {
                double mieux = Portees[_niveau];
                foreach (var b in _boites)
                {
                    if (b.Mode != Mode.Debout || b.Lache >= 0) continue;
                    double dx = (_rx - b.X) / (58 * _e), dy = (_ry - b.Y) / (66 * _e), d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < mieux) { mieux = d; cible = b; }
                }
            }
            if (cible != _cible) { _cible = cible; _progres = 0; }
            if (_cible != null)
            {
                _progres += dt / Math.Max(0.25, Fixation * 0.6);        // on vise vite : moins longtemps que la fixation habituelle
                if (_progres >= 1) { Lance(_cible); _progres = 0; _cible = null; }
            }

            if (_cible != null)
            {
                double r = 82 * _e;
                var tour = new double[82]; for (int i = 0; i <= 40; i++) { double a = i * Math.PI / 20; tour[i * 2] = Math.Cos(a) * r; tour[i * 2 + 1] = Math.Sin(a) * r; }
                int n = Math.Max(2, (int)Math.Round(_progres * 40) + 1);
                var arc = new double[n * 2];
                for (int i = 0; i < n; i++) { double a = -Math.PI / 2 + _progres * 2 * Math.PI * i / (n - 1); arc[i * 2] = Math.Cos(a) * r; arc[i * 2 + 1] = Math.Sin(a) * r; }
                _anneauFond.Points = tour; _anneauFond.Etat++; _anneauBord.Points = arc; _anneauBord.Etat++; _anneau.Points = arc; _anneau.Etat++;
                foreach (var o in new[] { _anneauFond, _anneauBord, _anneau }) { o.X = _cible.X; o.Y = _cible.Y; }
                _anneauFond.Op = 0.45; _anneauBord.Op = 0.6; _anneau.Op = 1;
            }
            else { _anneauFond.Op = 0; _anneauBord.Op = 0; _anneau.Op = 0; }
        }

        private void Lance(Boite b)
        {
            _visee = b;
            _ax = b.X + Borne(_rx - b.X, -46 * _e, 46 * _e);
            _ay = b.Y + Borne(_ry - b.Y, -40 * _e, 40 * _e);
            _volT = 0; _balles++; _sansLancer = 0; _tombeesAuLancer = Reussites;
            _pret.Op = 0; _pretOmbre.Op = 0; _recharge = 0.78; _roule = -0.3;
            Son("lancer", 0);
        }

        private void AvanceBalle(double dt)
        {
            // la balle qui vole du comptoir vers la boîte : elle monte un peu, rapetisse et tourne
            if (_volT >= 0)
            {
                _volT += dt;
                double u = Math.Min(1, _volT / Vol), but = 0.6 * _e;
                _balle.X = Mel(PretX, _ax, u); _balle.Y = Mel(PretY, _ay, u) - 90 * Math.Sin(Math.PI * u) * (1 - 0.4 * u);
                _balle.E = PretE * Math.Pow(but / PretE, u); _balle.R += 620 * dt; _balle.Op = 1;
                if (u >= 1) Touche();
            }
            // … puis qui retombe derrière la table
            if (_loin)
            {
                _bvy += Pesanteur * dt; _bx += _bvx * dt; _by += _bvy * dt; _be = Math.Max(0.4 * _e, _be - 0.5 * _e * dt);
                _balleLoin.X = _bx; _balleLoin.Y = _by; _balleLoin.E = _be; _balleLoin.R += 400 * dt; _balleLoin.Op = 1;
                if (_by > 960) { _loin = false; _balleLoin.Op = 0; }
            }
            // la balle suivante roule du panier jusqu'au milieu du comptoir
            if (_recharge > 0) _recharge -= dt;
            if (_roule < 0.5)
            {
                _roule += dt;
                double u = Sortant(_roule / 0.5), x = Mel(372, PretX, u);
                _pret.X = x; _pret.Y = PretY; _pret.R = (x - PretX) / (46 * PretE) * 180 / Math.PI; _pret.Op = _roule > 0 ? 1 : 0;
                _pretOmbre.X = x; _pretOmbre.Op = _pret.Op;
            }
            else
            {
                _pret.X = PretX; _pret.R = 0;
                // elle sautille quand une boîte est visée
                _pret.Y = PretY - (_cible != null ? 10 * Math.Abs(Math.Sin(_temps * 9)) * _progres : 0);
            }
        }

        private void Touche()
        {
            _volT = -1; _balle.Op = 0;
            _loin = true; _bx = _ax; _by = _ay; _be = 0.6 * _e;
            var touchees = new List<Boite>();
            if (_visee != null && _visee.Mode == Mode.Debout) touchees.Add(_visee);
            foreach (var b in _boites)
                if (b != _visee && b.Mode == Mode.Debout && _visee != null && b.Rang == _visee.Rang && Math.Abs(b.X - _ax) < 74 * _e) touchees.Add(b);
            double sens = touchees.Count > 0 && Math.Abs(touchees[0].X - _ax) > 6 * _e ? Math.Sign(_ax - touchees[0].X) : (Hasard() < 0.5 ? -1 : 1);
            _bvx = sens * Entre(60, 180); _bvy = -Entre(240, 420);
            foreach (var b in touchees) Frappe(b, _ax);
            _visee = null;

            _impact.X = _ax; _impact.Y = _ay; _impact.R = Entre(0, 90); _impactAge = 0;
            for (int k = 0; k < 7; k++) _etoiles.Lance(_ax, _ay, Entre(-360, 360), Entre(-460, -80), 900, Entre(0.5, 0.9), Entre(0.5, 1) * _e, 0, Entre(-360, 360));
            Son("choc", 0);
            foreach (var l in _lots) l.Saut = Entre(-0.12, 0);
            string[] mots = { "Bing !", "Paf !", "Bang !", "Clong !" }; string[] couleurs = { "#FFD23D", "#FF8A5C", "#7FD3FF", "#B8F0C0" };
            int m = _alea.Next(mots.Length);
            Ecris(touchees.Count >= 2 ? "Doublé !" : mots[m], _ax, _ay - 110 * _e, couleurs[m], 58);
            _bilan = _temps + 1.0;
        }

        /// <summary>Un peu après le choc : la voix dit combien de boîtes sont tombées d'un coup.</summary>
        private void Bilan()
        {
            _bilan = -1;
            int n = Reussites - _tombeesAuLancer;
            if (n < 2 || VoixOccupee() || _boites.TrueForAll(b => b.Mode != Mode.Debout)) return;
            Dit(n >= 5 ? "Quelle dégringolade !" : n >= 3 ? "Badaboum !" : "Deux d'un coup !");
        }

        // ------------------------------------------------------------------
        //  Tout est tombé : la fête, le lot, puis de nouvelles boîtes
        // ------------------------------------------------------------------
        private void Fini()
        {
            _etat = Etat.Fin; _depuis = 0; _bilan = -1; _cible = null;
            MancheReussie = _balles <= Math.Ceiling(_total * 0.6);
            _lotGagne = _manche % _lots.Count;
        }

        private double _feteVue;
        private void AvanceFin(double dt)
        {
            double t = _depuis, avant = t - dt;
            if (avant < 0.45 && t >= 0.45)
            {
                Dit("Tout est tombé ! Tu gagnes " + _lots[_lotGagne].Nom + " !");
                Son("tambour", 0);
                Bravo();
                Ecris("Bravo !", 800, 330, "#FFD23D", 96);
            }
            int[] notes = { 0, 2, 4, 7 };
            for (int k = 0; k < notes.Length; k++) if (avant < 0.6 + k * 0.16 && t >= 0.6 + k * 0.16) Son("note", notes[k]);
            // les confettis tombent de l'auvent
            if (t > 0.45 && t < 2.8 && t - _feteVue > 0.045)
            {
                _feteVue = t;
                var c = _confettis[_alea.Next(_confettis.Length)];
                c.Lance(Entre(120, 1480), Entre(120, 180), Entre(-90, 90), Entre(60, 220), 420, Entre(1.5, 2.3), Entre(0.8, 1.4), 0, Entre(-500, 500));
                if (_alea.Next(3) == 0) _etoiles.Lance(Entre(300, 1300), Entre(200, 520), Entre(-80, 80), Entre(-200, -40), 300, 0.9, Entre(0.7, 1.3), 0, Entre(-200, 200));
            }
            if (t < 0.5) _feteVue = 0;
            // le lot quitte son étagère et vient se poser sur le comptoir
            if (avant < 1.0 && t >= 1.0)
            {
                int place = _manche % _gagnes.Length;
                if (_gagnes[place] != null) { Devant.Retire(_gagnes[place].O); for (int k = 0; k < 6; k++) _etoiles.Lance(Places[place], 730, Entre(-200, 200), Entre(-300, -60), 600, 0.7, 0.8, 0, 200); }
                var lot = _lots[_lotGagne];
                var g = new Gagne { Place = place, T = 0, X0 = lot.X, Y0 = lot.Y, O = Devant.Avant(_pretOmbre, Dessin(lot.Dessin)) };
                g.O.En(lot.X, lot.Y, TailleLot);
                _gagnes[place] = g;
                _faisceau.X = Places[place];
                Son("pop", 0);
            }
            double f = t < 1.0 ? 0 : t < 3.4 ? Math.Min(1, (t - 1.0) / 0.5) : Math.Max(0, 1 - (t - 3.4) / 0.5);
            _faisceau.Op = f * (0.8 + 0.2 * Math.Sin(_temps * 5));
            if (t >= 4.2) { _manche++; _faisceau.Op = 0; Installe(); }
        }

        private void AvanceLots(double dt)
        {
            foreach (var l in _lots)
            {
                l.Saut += dt;
                double saut = l.Saut > 0 && l.Saut < 0.5 ? Math.Sin(l.Saut / 0.5 * Math.PI) * 26 : 0;
                l.O.Y = l.Y - saut; l.O.R = 2.2 * Math.Sin(_temps * 1.6 + l.Phase);
                l.O.SY = TailleLot * (1 + 0.012 * Math.Sin(_temps * 2.4 + l.Phase)); l.O.SX = TailleLot;
            }
            foreach (var g in _gagnes)
            {
                if (g == null) continue;
                if (g.T < 0.9)
                {
                    g.T += dt;
                    double u = Doux(g.T / 0.9);
                    g.O.X = Mel(g.X0, Places[g.Place], u); g.O.Y = Mel(g.Y0, 812, u) - 190 * Math.Sin(Math.PI * u);
                    g.O.E = Mel(TailleLot, 0.64, u); g.O.R = (g.X0 < 800 ? 1 : -1) * 360 * u;
                    if (g.T >= 0.9) { g.O.R = 0; Son("pof", 0); for (int k = 0; k < 6; k++) _etoiles.Lance(Places[g.Place] + Entre(-60, 60), 700 + Entre(-60, 40), Entre(-160, 160), Entre(-260, -80), 500, 0.8, Entre(0.6, 1), 0, 200); }
                }
                else
                {
                    g.T += dt;
                    double hop = g.T < 1.5 ? Math.Abs(Math.Sin((g.T - 0.9) / 0.3 * Math.PI)) * 16 * (1 - (g.T - 0.9) / 0.6) : 0;
                    g.O.X = Places[g.Place]; g.O.Y = 812 - Math.Max(0, hop); g.O.E = 0.64; g.O.R = 1.6 * Math.Sin(_temps * 1.4 + g.Place);
                }
            }
        }

        public override bool ProchaineCible(out double x, out double y)
        {
            x = 800; y = 420;
            if (_etat != Etat.Jeu) return false;
            if (_auto == null || _auto.Mode != Mode.Debout || _auto.Lache >= 0)
            {
                var debout = _boites.FindAll(b => b.Mode == Mode.Debout && b.Lache < 0);
                if (debout.Count == 0) return false;
                // une fois sur deux une boîte du bas (tout ce qu'elle porte tombe), sinon n'importe laquelle
                var bas = debout.FindAll(b => b.Rang == 0);
                _auto = bas.Count > 0 && _alea.Next(2) == 0 ? bas[_alea.Next(bas.Count)] : debout[_alea.Next(debout.Count)];
            }
            x = _auto.X + 20 * _e; y = _auto.Y;
            return true;
        }
    }
}
