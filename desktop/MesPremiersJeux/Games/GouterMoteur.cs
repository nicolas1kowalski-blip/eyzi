using System;
using System.Collections.Generic;
using System.Globalization;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le décor animé de « Partager en deux » (Apprendre, les nombres).
    //
    //  Un goûter sur la nappe : les gâteaux au milieu, l'assiette de Nounours
    //  à gauche, celle de l'enfant (avec le carton de son prénom) à droite.
    //  L'enfant choisit la pancarte du nombre de gâteaux pour chacun ; les
    //  gâteaux partent alors un à un, un pour Nounours, un pour lui, et le
    //  nombre de chaque assiette s'affiche. L'activité (MathGame.cs) choisit
    //  les nombres, pose la question et parle.
    //
    //  Ce que le moteur signale à l'activité (par Demande) : « choix:3 ».
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class GouterMoteur : VecMoteur
    {
        private const double AssietteY = 612, GaucheX = 300, DroiteX = 1300;
        private const double TailleTable = 0.95, TailleAssiette = 0.72;

        public readonly VecCalque Ciel = new VecCalque(), Decor = new VecCalque(), Table = new VecCalque(), Devant = new VecCalque(), Effets = new VecCalque();
        private readonly VecCalque[] _calques;
        public override VecCalque[] Calques => _calques;
        public override Dictionary<string, VecForme[]> Dessins => NombresArt.Dessins;
        protected override VecCalque CalqueDesMots => Effets;
        public override double Horizon => 560;

        private sealed class Gateau { public VecObjet O; public double X, Y, X0, Y0, X1, Y1, Depart = -1, T = -1, Phase; public bool Pose, Gauche; }
        private readonly List<Gateau> _gateaux = new List<Gateau>();
        private readonly VecObjet _soleil, _rayons, _ours, _nom, _compteG, _compteD;
        private readonly VecObjet[] _nuages;
        private readonly VecPancartes _pancartes;
        private readonly Bac _etoiles;
        private readonly List<VecZone> _zones = new List<VecZone>();
        private int _etatZones;
        public override List<VecZone> Zones => _zones;
        public override int EtatDesZones => _etatZones;

        private bool _ouvert, _fini;
        private int _total, _part, _aGauche, _aDroite;
        private double _saut = 9, _bosseG = 9, _bosseD = 9;

        public GouterMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Ciel, Decor, Table, Devant, Effets };
            _rayons = Pose(Ciel, "rayons", 1430, 104, 0.8); _soleil = Pose(Ciel, "soleil", 1430, 104, 0.8);
            _nuages = new[] { Pose(Ciel, "nuage", 250, 110, 0.7), Pose(Ciel, "nuage", 900, 70, 0.5), Pose(Ciel, "nuage", 1250, 210, 0.4) };
            foreach (var p in NombresArt.Scenes["gouter"]) Decor.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = p.Dessin }.En(p.X, p.Y, p.E, p.R));
            // Nounours derrière son assiette, le carton du prénom derrière celle de l'enfant
            _ours = Pose(Table, "lot_ours", GaucheX, 566, 1.0);
            Pose(Table, "carton", DroiteX, 566);
            _nom = Table.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = "", Taille = 54, Couleur = "#FF7A2E" }.En(DroiteX, 518));
            Pose(Table, "assiette", GaucheX, AssietteY); Pose(Table, "assiette", DroiteX, AssietteY);
            _pancartes = new VecPancartes(Devant, 772);
            _compteG = Effets.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = "", Taille = 130, Couleur = "#FF8A2E", Op = 0 }.En(GaucheX + 190, 470));
            _compteD = Effets.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = "", Taille = 130, Couleur = "#FF8A2E", Op = 0 }.En(DroiteX - 190, 420));
            _etoiles = new Bac(this, Effets, "etoile", 30);
            var ciel = NombresArt.Ciels["gouter"];
            CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            Prepare(4, "Laura", new[] { 1, 2, 3 });
        }

        public override void Demarre(int scene = 0) { _ouvert = true; }

        /// <summary>Installe une question : les gâteaux à partager, le prénom de l'enfant, les pancartes proposées.</summary>
        public void Prepare(int total, string prenom, IList<int> choix)
        {
            foreach (var g in _gateaux) Table.Retire(g.O);
            _gateaux.Clear();
            _total = Math.Max(2, Math.Min(10, total)) / 2 * 2; _part = _total / 2; _fini = false; _aGauche = 0; _aDroite = 0;
            _compteG.Op = 0; _compteD.Op = 0;
            prenom = (prenom ?? "").Trim();
            _nom.Texte = prenom; _nom.Taille = Math.Max(28, Math.Min(56, 420.0 / Math.Max(4, prenom.Length))); _nom.Etat++;
            // les gâteaux au milieu de la nappe : cinq par rang au plus
            int parRang = _total <= 5 ? _total : (_total + 1) / 2;
            for (int i = 0; i < _total; i++)
            {
                int rang = i / parRang, dansRang = i % parRang, n = rang == 0 ? parRang : _total - parRang;
                var g = new Gateau { X = 800 + (dansRang - (n - 1) / 2.0) * 98, Y = (_total <= 5 ? 566 : 520) + rang * 92, Phase = i * 1.4 };
                g.O = Pose(Table, "gateau", g.X, g.Y, TailleTable);
                _gateaux.Add(g);
            }
            _pancartes.Installe(choix, 800, 232);
            RefaitZones();
        }

        public void Refuse(int v) { _pancartes.Refuse(v); RefaitZones(); }

        /// <summary>C'est la bonne pancarte : les gâteaux partent un à un, un pour Nounours, un pour l'enfant.</summary>
        public void Reponse(int v)
        {
            double x = _pancartes.Fete(v);
            if (!double.IsNaN(x)) for (int k = 0; k < 8; k++) _etoiles.Lance(x + Entre(-80, 80), _pancartes.Y + Entre(-90, 60), Entre(-160, 160), Entre(-320, -80), 500, 0.9, Entre(0.7, 1.2), 0, Entre(-200, 200));
            _fini = true; RefaitZones();
            double pas = Math.Min(66, 300.0 / Math.Max(1, _part));
            for (int i = 0; i < _gateaux.Count; i++)
            {
                var g = _gateaux[i];
                g.Gauche = i % 2 == 0; g.Depart = _temps + 0.7 + i * 0.34;
                g.X0 = g.X; g.Y0 = g.Y;
                g.X1 = (g.Gauche ? GaucheX : DroiteX) + (i / 2 - (_part - 1) / 2.0) * pas; g.Y1 = AssietteY + 14;
            }
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

        public override void Avance(double dt)
        {
            if (!_ouvert) return;
            dt = Math.Min(dt, 0.05);
            _temps += dt;

            _rayons.R += 8 * dt; _soleil.E = 0.8 * (1 + 0.02 * Math.Sin(_temps * 2));
            for (int i = 0; i < _nuages.Length; i++) { _nuages[i].X += (6 + i * 3) * dt; if (_nuages[i].X > 1800) _nuages[i].X = -200; }

            foreach (var g in _gateaux)
            {
                if (g.Depart >= 0 && g.T < 0 && _temps >= g.Depart) { g.T = 0; g.Depart = -1; Table.Retire(g.O); Table.Ajoute(g.O); }
                if (g.T >= 0 && !g.Pose)
                {
                    g.T += dt;
                    double u = Math.Min(1, g.T / 0.55), d = Doux(u);
                    g.X = Mel(g.X0, g.X1, d); g.Y = Mel(g.Y0, g.Y1, d) - 150 * Math.Sin(Math.PI * u);
                    g.O.E = Mel(TailleTable, TailleAssiette, u); g.O.R = (g.Gauche ? -1 : 1) * 14 * Math.Sin(Math.PI * u);
                    if (u >= 1) Arrive(g);
                }
                else if (!g.Pose) { g.O.E = TailleTable * (1 + 0.02 * Math.Sin(_temps * 2.4 + g.Phase)); g.O.R = 2 * Math.Sin(_temps * 1.6 + g.Phase); }
                g.O.X = g.X; g.O.Y = g.Y;
            }

            // Nounours : il se balance, et saute quand un gâteau arrive dans son assiette
            _saut += dt;
            _ours.Y = 566 - (_saut < 0.4 ? 26 * Math.Sin(_saut / 0.4 * Math.PI) : 0);
            _ours.R = 2.4 * Math.Sin(_temps * 1.5);

            // les deux nombres des assiettes
            _bosseG += dt; _bosseD += dt;
            _compteG.E = 1 + (_bosseG < 0.4 ? 0.35 * Math.Sin(_bosseG / 0.4 * Math.PI) : 0);
            _compteD.E = 1 + (_bosseD < 0.4 ? 0.35 * Math.Sin(_bosseD / 0.4 * Math.PI) : 0);

            _pancartes.Avance(dt, _temps, !_fini, _temps - _vu < 0.4, _rx, _ry);
            _etoiles.Avance(dt);
            AvanceMots(dt);
        }

        private void Arrive(Gateau g)
        {
            g.Pose = true; g.O.R = 0; g.O.E = TailleAssiette; Reussites++;
            Son("pop", 0);
            if (g.Gauche) { _aGauche++; _saut = 0; _bosseG = 0; _compteG.Texte = _aGauche.ToString(CultureInfo.InvariantCulture); _compteG.Etat++; _compteG.Op = 1; }
            else { _aDroite++; _bosseD = 0; _compteD.Texte = _aDroite.ToString(CultureInfo.InvariantCulture); _compteD.Etat++; _compteD.Op = 1; }
            for (int k = 0; k < 4; k++) _etoiles.Lance(g.X1 + Entre(-20, 20), g.Y1 - 60, Entre(-160, 160), Entre(-300, -100), 600, 0.6, Entre(0.4, 0.8), 0, Entre(-200, 200));
        }

        public override bool ProchaineCible(out double x, out double y)
        {
            x = 800; y = 420;
            if (_fini || !_pancartes.PlaceDe(_part, out double px)) return false;
            x = px; y = _pancartes.Y; return true;
        }
    }
}
