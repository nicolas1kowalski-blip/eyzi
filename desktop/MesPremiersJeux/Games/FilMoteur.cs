using System;
using System.Collections.Generic;
using System.Globalization;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le décor animé de « Un de plus, un de moins » (Apprendre, les nombres).
    //
    //  Des oiseaux sont posés sur deux fils, cinq par fil au plus. Un de plus :
    //  un oiseau arrive et attend en battant des ailes ; un de moins : le
    //  dernier s'agite, prêt à partir. L'enfant choisit la pancarte du nombre
    //  qu'il y aura ; l'oiseau vient alors se poser, ou s'envole. L'activité
    //  (MathGame.cs) choisit les nombres, pose la question et parle.
    //
    //  Ce que le moteur signale à l'activité (par Demande) : « choix:4 ».
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class FilMoteur : VecMoteur
    {
        private const double FilG = 110, FilD = 1490;
        private static readonly double[] Fils = { 288, 468 };      // la hauteur des deux fils, à leurs attaches
        private const double Creux = 36;                           // de combien un fil pend en son milieu
        private const double AttenteX = 1410, AttenteY = 150;      // là où l'oiseau qui arrive attend

        public readonly VecCalque Ciel = new VecCalque(), Decor = new VecCalque(), Perchoir = new VecCalque(), Devant = new VecCalque(), Effets = new VecCalque();
        private readonly VecCalque[] _calques;
        public override VecCalque[] Calques => _calques;
        public override Dictionary<string, VecForme[]> Dessins => NombresArt.Dessins;
        protected override VecCalque CalqueDesMots => Effets;
        public override double Horizon => 560;

        private sealed class Oiseau
        {
            public VecObjet Pose, Haut, Bas;       // posé, les ailes en l'air, les ailes en bas
            public double X, Y, Phase, Sens = 1, Saut = 9, Taille = 1;
            public int Place;
            public bool Vole;
        }
        private readonly List<Oiseau> _oiseaux = new List<Oiseau>();
        private readonly VecObjet[] _fils = new VecObjet[2];
        private readonly double[] _rebond = { 9, 9 };
        private readonly VecObjet _soleil, _rayons, _signe;
        private readonly VecObjet[] _nuages;
        private readonly VecPancartes _pancartes;
        private readonly Bac _etoiles, _notes;
        private readonly string[][] _plumages;
        private readonly List<VecZone> _zones = new List<VecZone>();
        private int _etatZones;
        public override List<VecZone> Zones => _zones;
        public override int EtatDesZones => _etatZones;

        private Oiseau _mobile;            // celui qui arrive, ou celui qui va partir
        private bool _plus, _ouvert, _fini;
        private int _n, _reponse;
        private double _trajet = -1, _x0, _y0, _x1, _y1, _chant = 9;

        public FilMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Ciel, Decor, Perchoir, Devant, Effets };
            _rayons = Pose(Ciel, "rayons", 330, 96, 0.75); _soleil = Pose(Ciel, "soleil", 330, 96, 0.75);
            _nuages = new[] { Pose(Ciel, "nuage", 700, 90, 0.6), Pose(Ciel, "nuage", 1180, 60, 0.45), Pose(Ciel, "nuage", 1560, 200, 0.4) };
            foreach (var p in NombresArt.Scenes["fil"]) Decor.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = p.Dessin }.En(p.X, p.Y, p.E, p.R));
            for (int f = 0; f < 2; f++) _fils[f] = Perchoir.Ajoute(new VecObjet { Genre = VecGenre.Ligne, Couleur = "#3A3454", Epaisseur = 6 });
            _pancartes = new VecPancartes(Devant, 742);
            _signe = Effets.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = "+1", Taille = 110, Couleur = "#B07AF2" }.En(1210, 96));
            _notes = new Bac(this, Effets, "note", 12);
            _etoiles = new Bac(this, Effets, "etoile", 24);

            var l = NombresArt.Listes["plumages"]; _plumages = new string[l.Length][];
            for (int i = 0; i < l.Length; i++) _plumages[i] = new[] { "c", l[i][0], "clair", l[i][1], "fonce", l[i][2] };
            var ciel = NombresArt.Ciels["fil"];
            CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            Prepare(3, true, new[] { 3, 4, 5 });
        }

        public override void Demarre(int scene = 0) { _ouvert = true; }

        // la place d'un oiseau : cinq par fil, le fil du haut d'abord
        private double PlaceX(int place) => 320 + (place % 5) * 240;
        private double Pend(int fil, double x)
        {
            double u = (x - (FilG + FilD) / 2) / ((FilD - FilG) / 2), r = _rebond[fil];
            double danse = r < 0.9 ? 10 * Math.Sin(r * 22) * (1 - r / 0.9) : 0;
            return Fils[fil] + (Creux + danse) * (1 - u * u);
        }
        private double PlaceY(int place) => Pend(place / 5, PlaceX(place));

        private Oiseau Nouveau(int place, int couleur)
        {
            var o = new Oiseau { Place = place, Phase = place * 1.9, Sens = place % 3 == 1 ? -1 : 1 };
            var c = _plumages[couleur % _plumages.Length];
            o.Pose = Pose(Perchoir, "oiseau_pose", 0, 0, 1, c);
            o.Haut = Pose(Perchoir, "oiseau_vol_a", 0, 0, 1, c); o.Haut.Op = 0;
            o.Bas = Pose(Perchoir, "oiseau_vol_b", 0, 0, 1, c); o.Bas.Op = 0;
            return o;
        }

        // ------------------------------------------------------------------
        //  Ce que l'activité demande
        // ------------------------------------------------------------------
        /// <summary>Installe une question : n oiseaux sur les fils, un de plus ou un de moins, et les pancartes proposées.</summary>
        public void Prepare(int n, bool plus, IList<int> choix)
        {
            foreach (var o in _oiseaux) { Perchoir.Retire(o.Pose); Perchoir.Retire(o.Haut); Perchoir.Retire(o.Bas); }
            _oiseaux.Clear();
            _n = Math.Max(1, Math.Min(10, n)); _plus = plus; _reponse = plus ? _n + 1 : _n - 1; _fini = false; _trajet = -1; _chant = 9;
            int depart = _alea.Next(_plumages.Length);
            for (int i = 0; i < _n; i++) { var o = Nouveau(i, depart + i); o.X = PlaceX(i); o.Y = PlaceY(i); _oiseaux.Add(o); }
            if (plus)
            {
                _mobile = Nouveau(_n, depart + _n); _mobile.Vole = true; _mobile.X = AttenteX; _mobile.Y = AttenteY; _mobile.Sens = -1;
                _oiseaux.Add(_mobile);
            }
            else _mobile = _oiseaux[_n - 1];
            _signe.Texte = plus ? "+1" : "−1"; _signe.Etat++; _signe.Op = 1;
            _pancartes.Installe(choix, 800, choix.Count > 4 ? 200 : 232);
            RefaitZones();
        }

        public void Refuse(int v) { _pancartes.Refuse(v); RefaitZones(); }

        /// <summary>C'est la bonne pancarte : l'oiseau vient se poser (un de plus) ou s'envole (un de moins).</summary>
        public void Reponse(int v)
        {
            double x = _pancartes.Fete(v);
            if (!double.IsNaN(x)) for (int k = 0; k < 8; k++) _etoiles.Lance(x + Entre(-80, 80), _pancartes.Y + Entre(-90, 60), Entre(-160, 160), Entre(-320, -80), 500, 0.9, Entre(0.7, 1.2), 0, Entre(-200, 200));
            _fini = true; RefaitZones();
            _trajet = 0; _x0 = _mobile.X; _y0 = _mobile.Y;
            if (_plus) { _x1 = PlaceX(_n); _y1 = PlaceY(_n); }
            else { _mobile.Vole = true; _x1 = 1720; _y1 = -80; _rebond[_mobile.Place / 5] = 0; Son("pof", 0); }
        }

        private void RefaitZones()
        {
            _zones.Clear();
            if (!_fini) _pancartes.Zones(_zones);
            _etatZones++;
        }

        public override void Appuie(string id)
        {
            if (_fini || id == null || id.Length < 3 || id[0] != 'c') return;
            if (int.TryParse(id.Substring(2), out int v)) Demande("choix:" + v);
        }

        // ------------------------------------------------------------------
        //  Une image de plus
        // ------------------------------------------------------------------
        public override void Avance(double dt)
        {
            if (!_ouvert) return;
            dt = Math.Min(dt, 0.05);
            _temps += dt;

            _rayons.R += 8 * dt; _soleil.E = 0.75 * (1 + 0.02 * Math.Sin(_temps * 2));
            for (int i = 0; i < _nuages.Length; i++) { _nuages[i].X += (6 + i * 3) * dt; if (_nuages[i].X > 1800) _nuages[i].X = -200; }

            // les fils : ils pendent, et dansent un instant quand un oiseau se pose ou s'envole
            for (int f = 0; f < 2; f++)
            {
                _rebond[f] += dt;
                var pts = new double[42];
                for (int i = 0; i <= 20; i++) { double x = FilG + (FilD - FilG) * i / 20; pts[i * 2] = x; pts[i * 2 + 1] = Pend(f, x); }
                _fils[f].Points = pts; _fils[f].Etat++;
            }

            // le trajet de l'oiseau qui vient se poser, ou qui s'en va
            if (_trajet >= 0)
            {
                _trajet += dt;
                double duree = _plus ? 1.0 : 1.3, u = Math.Min(1, _trajet / duree), d = Doux(u);
                _mobile.X = Mel(_x0, _x1, d);
                _mobile.Y = Mel(_y0, _plus ? PlaceY(_n) : _y1, d) - (_plus ? 90 : 60) * Math.Sin(Math.PI * u);
                _mobile.Sens = _x1 > _x0 ? 1 : -1;
                if (!_plus) _mobile.Taille = 1 - 0.3 * u;
                if (u >= 1)
                {
                    _trajet = -1;
                    if (_plus)
                    {
                        _mobile.Vole = false; _mobile.Saut = 0; _rebond[_n / 5] = 0; _chant = 0;
                        Son("pof", 0);
                        for (int k = 0; k < 7; k++) _etoiles.Lance(_mobile.X + Entre(-40, 40), _mobile.Y - 50, Entre(-200, 200), Entre(-360, -100), 700, 0.8, Entre(0.5, 1), 0, Entre(-200, 200));
                    }
                    else { _mobile.Pose.Op = 0; _mobile.Haut.Op = 0; _mobile.Bas.Op = 0; _oiseaux.Remove(_mobile); _chant = 0; }
                    _signe.Op = 0;
                }
            }

            // les oiseaux chantent quand le compte est bon
            _chant += dt;
            if (_chant < 1.6)
                foreach (var o in _oiseaux)
                    if (!o.Vole && o.Saut > 0.5 && Hasard() < dt * 3) { o.Saut = 0; _notes.Lance(o.X + 30 * o.Sens, o.Y - 110, Entre(-30, 30), -Entre(70, 130), 0, 1.1, Entre(0.9, 1.3), 0, Entre(-40, 40)); }

            foreach (var o in _oiseaux)
            {
                o.Saut += dt;
                bool agite = !_fini && !_plus && o == _mobile;          // celui qui va partir bat des ailes sur place
                if (!o.Vole) { o.X = PlaceX(o.Place); o.Y = PlaceY(o.Place); }
                double saut = o.Saut < 0.35 ? 22 * Math.Sin(o.Saut / 0.35 * Math.PI) : agite ? 12 * Math.Abs(Math.Sin(_temps * 7)) : 0;
                double plane = o.Vole && _trajet < 0 ? 10 * Math.Sin(_temps * 5 + o.Phase) : 0;
                double x = o.X, y = o.Y - saut + plane;
                bool ailes = o.Vole || agite;
                bool haut = ailes && Math.Sin(_temps * 22 + o.Phase) > 0;
                double respire = 1 + 0.025 * Math.Sin(_temps * 2.6 + o.Phase);
                foreach (var v in new[] { o.Pose, o.Haut, o.Bas })
                {
                    v.X = x; v.Y = y; v.SX = o.Sens * o.Taille; v.SY = o.Taille * (v == o.Pose ? respire : 1);
                    v.R = o.Vole ? -8 * o.Sens : 2.5 * Math.Sin(_temps * 1.4 + o.Phase);
                }
                o.Pose.Op = ailes ? 0 : 1; o.Haut.Op = ailes && haut ? 1 : 0; o.Bas.Op = ailes && !haut ? 1 : 0;
            }

            // le signe « +1 » ou « −1 » bat doucement
            _signe.E = 1 + 0.06 * Math.Sin(_temps * 4); _signe.R = -4 + 3 * Math.Sin(_temps * 2);

            _pancartes.Avance(dt, _temps, !_fini, _temps - _vu < 0.4, _rx, _ry);
            _etoiles.Avance(dt); _notes.Avance(dt);
            AvanceMots(dt);
        }

        public override bool ProchaineCible(out double x, out double y)
        {
            x = 800; y = 420;
            if (_fini || !_pancartes.PlaceDe(_reponse, out double px)) return false;
            x = px; y = _pancartes.Y; return true;
        }
    }
}
