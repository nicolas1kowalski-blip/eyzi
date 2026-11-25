using System;
using System.Collections.Generic;
using System.Globalization;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le décor animé de « La bande numérique » (Apprendre, les nombres).
    //
    //  Le petit train des nombres : la locomotive tire autant de wagons qu'il y
    //  a de nombres, chacun porte le sien, sauf un, marqué « ? ». Le train
    //  arrive par la gauche et s'arrête ; l'enfant choisit la pancarte du
    //  nombre qui manque ; le nombre s'écrit sur le wagon, le train siffle et
    //  repart. L'activité (MathGame.cs) choisit les nombres, pose la question
    //  et parle.
    //
    //  Ce que le moteur signale à l'activité (par Demande) : « choix:6 ».
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class TrainMoteur : VecMoteur
    {
        private const double RailY = 612;
        private const double Loco = 236, Pas = 132;      // la longueur de la locomotive et d'un wagon

        public readonly VecCalque Ciel = new VecCalque(), Decor = new VecCalque(), Voie = new VecCalque(), Devant = new VecCalque(), Effets = new VecCalque();
        private readonly VecCalque[] _calques;
        public override VecCalque[] Calques => _calques;
        public override Dictionary<string, VecForme[]> Dessins => NombresArt.Dessins;
        protected override VecCalque CalqueDesMots => Effets;
        public override double Horizon => 560;

        private sealed class Piece { public VecObjet O; public double Dx, Dy; public bool Roue; public double Rayon; public int Wagon = -1; public bool Texte; }
        private readonly List<Piece> _pieces = new List<Piece>();
        private readonly VecObjet _soleil, _rayons;
        private readonly VecObjet[] _nuages;
        private VecObjet _halo, _trou;
        private readonly VecPancartes _pancartes;
        private readonly Bac _etoiles, _fumees;
        private readonly string[][] _couleurs;
        private readonly List<VecZone> _zones = new List<VecZone>();
        private int _etatZones;
        public override List<VecZone> Zones => _zones;
        public override int EtatDesZones => _etatZones;

        private enum Etat { Arrive, Arret, Repart }
        private Etat _etat;
        private bool _ouvert, _fini;
        private int _max, _manque;
        private double _k = 1, _x, _arret, _depuis, _fumee, _vague = 9, _bosse = 9, _vitesse;

        public TrainMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Ciel, Decor, Voie, Devant, Effets };
            _rayons = Pose(Ciel, "rayons", 1430, 104, 0.8); _soleil = Pose(Ciel, "soleil", 1430, 104, 0.8);
            _nuages = new[] { Pose(Ciel, "nuage", 250, 110, 0.7), Pose(Ciel, "nuage", 900, 70, 0.5), Pose(Ciel, "nuage", 1250, 210, 0.4) };
            foreach (var p in NombresArt.Scenes["train"]) Decor.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = p.Dessin }.En(p.X, p.Y, p.E, p.R));
            _pancartes = new VecPancartes(Devant, 772);
            _fumees = new Bac(this, Effets, "fumee", 16);
            _etoiles = new Bac(this, Effets, "etoile", 30);
            var l = NombresArt.Listes["wagons"]; _couleurs = new string[l.Length][];
            for (int i = 0; i < l.Length; i++) _couleurs[i] = new[] { "c", l[i][0], "clair", l[i][1], "fonce", l[i][2] };
            var ciel = NombresArt.Ciels["train"];
            CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            Prepare(5, 3, new[] { 2, 3, 4 });
        }

        public override void Demarre(int scene = 0) { _ouvert = true; }

        private Piece Ajoute(VecObjet o, double dx, double dy, int wagon = -1) { var p = new Piece { O = o, Dx = dx, Dy = dy, Wagon = wagon }; _pieces.Add(p); return p; }
        private void Roue(double dx, double rayon, int wagon = -1) { var p = Ajoute(Pose(Voie, "roue", 0, 0), dx, -rayon, wagon); p.Roue = true; p.Rayon = rayon; }

        /// <summary>Installe une question : un train de « max » wagons, celui du nombre « manque » sans son nombre, et les pancartes proposées.</summary>
        public void Prepare(int max, int manque, IList<int> choix)
        {
            foreach (var p in _pieces) Voie.Retire(p.O);
            _pieces.Clear();
            _max = Math.Max(3, Math.Min(10, max)); _manque = Math.Max(1, Math.Min(_max, manque)); _fini = false;
            double longueur = Loco + _max * Pas;
            _k = Math.Min(1.3, 1500 / longueur);
            // le train, de la queue (le wagon 1, à gauche) à la locomotive (à droite) ; les places sont comptées depuis sa queue
            _halo = null; _trou = null;
            int couleur = _alea.Next(_couleurs.Length);
            for (int i = 0; i < _max; i++)
            {
                double cx = (i + 0.5) * Pas;
                if (i + 1 == _manque) { _halo = Pose(Voie, "wagon_halo", 0, 0); Ajoute(_halo, cx, -22, i); }
                Ajoute(Pose(Voie, "wagon", 0, 0, 1, _couleurs[(couleur + i) % _couleurs.Length]), cx, -22, i);
                string texte = i + 1 == _manque ? "?" : (i + 1).ToString(CultureInfo.InvariantCulture);
                var t = Ajoute(Voie.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = texte, Taille = texte.Length > 1 ? 62 : 80, Couleur = i + 1 == _manque ? "#FFD23D" : "#FFFFFF" }), cx, -112, i);
                t.Texte = true;
                if (i + 1 == _manque) _trou = t.O;
                Roue(cx - 34, 22, i); Roue(cx + 34, 22, i);
            }
            double lx = _max * Pas + Loco / 2;
            Ajoute(Pose(Voie, "loco", 0, 0), lx, -22);
            Roue(lx - 70, 22); Roue(lx - 6, 22); Roue(lx + 60, 22);

            _arret = 800 - longueur * _k / 2; _x = _arret - 1750; _etat = Etat.Arrive; _depuis = 0; _vitesse = 0;
            _pancartes.Installe(choix, 800, choix.Count > 4 ? 200 : 232);
            RefaitZones();
        }

        public void Refuse(int v) { _pancartes.Refuse(v); RefaitZones(); }

        /// <summary>C'est le bon nombre : il s'écrit sur le wagon, le train siffle et repart.</summary>
        public void Reponse(int v)
        {
            double x = _pancartes.Fete(v);
            if (!double.IsNaN(x)) for (int k = 0; k < 8; k++) _etoiles.Lance(x + Entre(-80, 80), _pancartes.Y + Entre(-90, 60), Entre(-160, 160), Entre(-320, -80), 500, 0.9, Entre(0.7, 1.2), 0, Entre(-200, 200));
            _fini = true; RefaitZones();
            if (_trou != null)
            {
                string texte = _manque.ToString(CultureInfo.InvariantCulture);
                _trou.Texte = texte; _trou.Taille = texte.Length > 1 ? 62 : 80; _trou.Couleur = "#FFFFFF"; _trou.Etat++;
                _bosse = 0;
                double wx = _x + (_manque - 0.5) * Pas * _k;
                for (int k = 0; k < 10; k++) _etoiles.Lance(wx + Entre(-50, 50), RailY - 130 * _k, Entre(-220, 220), Entre(-420, -120), 700, 0.9, Entre(0.6, 1.1), 0, Entre(-200, 200));
            }
            _vague = 0; _etat = Etat.Arret; _depuis = 0;
        }

        private void RefaitZones()
        {
            _zones.Clear();
            if (!_fini && _etat == Etat.Arret) _pancartes.Zones(_zones);
            _etatZones++;
        }

        public override void Appuie(string id)
        {
            if (_fini || _etat != Etat.Arret || id == null || id.Length < 3 || id[0] != 'c') return;
            if (int.TryParse(id.Substring(2), out int v)) Demande("choix:" + v);
        }

        public override void Avance(double dt)
        {
            if (!_ouvert) return;
            dt = Math.Min(dt, 0.05);
            _temps += dt; _depuis += dt;

            _rayons.R += 8 * dt; _soleil.E = 0.8 * (1 + 0.02 * Math.Sin(_temps * 2));
            for (int i = 0; i < _nuages.Length; i++) { _nuages[i].X += (6 + i * 3) * dt; if (_nuages[i].X > 1800) _nuages[i].X = -200; }

            double avant = _x;
            switch (_etat)
            {
                case Etat.Arrive:
                    _x = _arret - 1750 * (1 - Sortant(_depuis / 2.6));
                    if (_depuis >= 2.6) { _x = _arret; _etat = Etat.Arret; _depuis = 0; RefaitZones(); }
                    break;
                case Etat.Arret:
                    if (_fini && _depuis > 1.5) { _etat = Etat.Repart; _depuis = 0; _vitesse = 0; Son("sifflet", 0); }
                    break;
                case Etat.Repart:
                    _vitesse = Math.Min(1500, _vitesse + 900 * dt); _x += _vitesse * dt;
                    break;
            }
            double roule = _x - avant;

            // la fumée sort de la cheminée tant que le train roule
            double cheminee = _x + (_max * Pas + Loco / 2 + 70) * _k;
            _fumee -= dt;
            if (_fumee <= 0 && cheminee > -60 && cheminee < 1660)
            {
                _fumee = Math.Abs(roule) > 0.5 ? 0.12 : 0.6;
                _fumees.Lance(cheminee, RailY - 240 * _k, Entre(-60, -10) - roule * 6, -Entre(60, 120), 0, Entre(1.0, 1.5), Entre(0.5, 0.8) * _k, 1.4);
            }

            _vague += dt; _bosse += dt;
            foreach (var p in _pieces)
            {
                // les wagons sautent l'un après l'autre quand le compte est bon
                double saut = 0;
                if (p.Wagon >= 0 && _vague < 2) { double u = _vague - p.Wagon * 0.07; if (u > 0 && u < 0.35) saut = 16 * Math.Sin(u / 0.35 * Math.PI) * _k; }
                double cahot = Math.Abs(roule) > 0.5 ? 1.5 * Math.Sin(_temps * 26 + p.Dx * 0.05) : 0;
                p.O.X = _x + p.Dx * _k; p.O.Y = RailY + p.Dy * _k - saut + (p.Roue ? 0 : cahot);
                p.O.E = _k;
                if (p.Roue) p.O.R += roule / (p.Rayon * _k) * 180 / Math.PI;
            }
            if (_halo != null) { _halo.Op = _fini ? Math.Max(0, 1 - _bosse / 0.6) : 0.8 + 0.2 * Math.Sin(_temps * 5); _halo.E = _k * (1.12 + 0.06 * Math.Sin(_temps * 5)); }
            if (_trou != null) _trou.E = _k * (_fini ? 1 + (_bosse < 0.5 ? 0.6 * Math.Sin(_bosse / 0.5 * Math.PI) : 0) : 1 + 0.12 * Math.Sin(_temps * 5));

            _pancartes.Avance(dt, _temps, !_fini && _etat == Etat.Arret, _temps - _vu < 0.4, _rx, _ry);
            _etoiles.Avance(dt); _fumees.Avance(dt);
            AvanceMots(dt);
        }

        public override bool ProchaineCible(out double x, out double y)
        {
            x = 800; y = 420;
            if (_fini || _etat != Etat.Arret || !_pancartes.PlaceDe(_manque, out double px)) return false;
            x = px; y = _pancartes.Y; return true;
        }
    }
}
