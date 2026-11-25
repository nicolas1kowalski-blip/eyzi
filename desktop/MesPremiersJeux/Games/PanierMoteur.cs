using System;
using System.Collections.Generic;
using System.Globalization;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le décor animé de l'activité « Le panier » (Apprendre, les nombres).
    //
    //  Un verger : l'arbre et ses fruits à gauche, le panier d'osier à droite,
    //  et le grand chiffre des fruits déjà dans le panier. L'activité
    //  (CompleteGame.cs) choisit les nombres, pose les questions et parle ; ce
    //  moteur montre : le fruit regardé se décroche, vole jusqu'au panier, la
    //  voix compte, le chiffre grossit. Au deuxième acte, l'enfant choisit une
    //  pancarte à chiffre, puis les fruits qui manquaient tombent un à un pour
    //  vérifier.
    //
    //  Ce que le moteur signale à l'activité (par Demande) :
    //    « plein »     le panier est complet (acte 1) ;
    //    « choix:3 »   l'enfant a choisi la pancarte 3 (acte 2) ;
    //    « verifie »   la vérification est finie (acte 2).
    //
    //  Comme les jeux « dessinés », il ne connaît ni l'écran ni Windows
    //  (voir VecMoteur.cs) : le banc d'essai en tire de vraies images.
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class PanierMoteur : VecMoteur
    {
        private const double PanierX = 1120, PanierY = 806;     // le bas du panier
        private const double RangY = 586;                        // la hauteur des fruits dans le panier
        private const double TailleStock = 1.25, TaillePanier = 0.8;
        private static readonly double[] Pancartes = { 150, 350, 550, 750 };
        private const double PancarteY = 652;

        public readonly VecCalque Ciel = new VecCalque(), Decor = new VecCalque(), Osier = new VecCalque(), Devant = new VecCalque(), Effets = new VecCalque();
        private readonly VecCalque[] _calques;
        public override VecCalque[] Calques => _calques;
        public override Dictionary<string, VecForme[]> Dessins => NombresArt.Dessins;
        protected override VecCalque CalqueDesMots => Effets;
        public override double Horizon => 560;

        private sealed class Fruit { public VecObjet O; public double X, Y, Phase, Pris = -1; public bool Parti; public int Rang; }
        private sealed class Vol { public VecObjet O; public double T, X0, Y0, X1, Y1, E0; public int Place; }
        private sealed class Pancarte { public VecObjet Planche, Chiffre, Halo, Voile; public int V; public double X, Secoue = 9, Saute = 9; public bool Refusee; }
        private sealed class Papillon { public VecObjet Corps, G, D; public double X, Y, ButX, ButY, Phase, Sens = 1; }

        private readonly List<Fruit> _stock = new List<Fruit>();
        private readonly List<VecObjet> _dansPanier = new List<VecObjet>(), _places = new List<VecObjet>();
        private readonly List<Vol> _vols = new List<Vol>();
        private readonly List<Pancarte> _pancartes = new List<Pancarte>();
        private readonly List<Papillon> _papillons = new List<Papillon>();
        private readonly List<KeyValuePair<double, int>> _aFaire = new List<KeyValuePair<double, int>>();   // la vérification : à quel instant part quel fruit
        private readonly List<VecZone> _zones = new List<VecZone>();
        private int _etatZones;
        public override List<VecZone> Zones => _zones;
        public override int EtatDesZones => _etatZones;

        private readonly VecObjet _soleil, _rayons, _arbre, _anse, _devant, _compteur;
        private readonly VecObjet[] _nuages;
        private readonly double[][] _branches;
        private readonly Bac _etoiles, _feuilles;
        private string _fruit = "fruit_pomme";
        private int _total = 4, _dedans, _arrives;
        private bool _cueillette, _ouvert, _fini;
        private double _bosse = 9, _indice = 9, _secousse = 9, _finDans = -1;
        private string _finDit;

        public PanierMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Ciel, Decor, Osier, Devant, Effets };
            _rayons = Pose(Ciel, "rayons", 1430, 104, 0.8); _soleil = Pose(Ciel, "soleil", 1430, 104, 0.8);
            _nuages = new[] { Pose(Ciel, "nuage", 250, 110, 0.7), Pose(Ciel, "nuage", 980, 70, 0.5), Pose(Ciel, "nuage", 1300, 200, 0.4) };
            foreach (var p in NombresArt.Scenes["verger"])
            {
                var o = Decor.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = p.Dessin }.En(p.X, p.Y, p.E, p.R));
                if (p.Nom == "arbre") _arbre = o;
            }
            _anse = Pose(Osier, "panier_fond", PanierX, PanierY);
            _devant = Pose(Devant, "panier_devant", PanierX, PanierY);
            _feuilles = new Bac(this, Effets, "feuille", 14);
            _etoiles = new Bac(this, Effets, "etoile", 28);
            _compteur = Effets.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = "0", Taille = 190, Couleur = "#FF8A2E" }.En(PanierX, 300));

            var b = NombresArt.Listes["branches"]; _branches = new double[b.Length][];
            for (int i = 0; i < b.Length; i++) _branches[i] = new[] { double.Parse(b[i][0], CultureInfo.InvariantCulture), double.Parse(b[i][1], CultureInfo.InvariantCulture) };

            string[][] ailes = { new[] { "c", "#FF7AB0", "clair", "#FFD0E4" }, new[] { "c", "#FFB02E", "clair", "#FFE9A8" } };
            for (int i = 0; i < 2; i++)
            {
                var p = new Papillon { X = 760 + i * 500, Y = 480 + i * 60, ButX = 800, ButY = 500, Phase = i * 2.1 };
                p.G = Pose(Effets, "papillon_aile", 0, 0, 1, ailes[i]); p.D = Pose(Effets, "papillon_aile", 0, 0, 1, ailes[i]); p.Corps = Pose(Effets, "papillon_corps", 0, 0);
                _papillons.Add(p);
            }

            var ciel = NombresArt.Ciels["verger"];
            CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            Prepare("fruit_pomme", 4, 2, true, 0);
        }

        public override void Demarre(int scene = 0) { _ouvert = true; }

        // ------------------------------------------------------------------
        //  Ce que l'activité demande
        // ------------------------------------------------------------------
        /// <summary>Installe une question : le fruit, le total du panier, ceux qui y sont déjà ; soit on cueille (acte 1), soit on choisit parmi des pancartes 1…choix (acte 2).</summary>
        public void Prepare(string fruit, int total, int dedans, bool cueillette, int choix)
        {
            foreach (var f in _stock) Decor.Retire(f.O);
            foreach (var o in _dansPanier) Osier.Retire(o);
            foreach (var o in _places) Osier.Retire(o);
            foreach (var v in _vols) Effets.Retire(v.O);
            foreach (var p in _pancartes) { Devant.Retire(p.Halo); Devant.Retire(p.Planche); Devant.Retire(p.Chiffre); Devant.Retire(p.Voile); }
            _stock.Clear(); _dansPanier.Clear(); _places.Clear(); _vols.Clear(); _pancartes.Clear(); _aFaire.Clear();
            _fruit = fruit; _total = Math.Max(1, Math.Min(6, total)); _dedans = Math.Max(0, Math.Min(_total, dedans)); _arrives = _dedans;
            _cueillette = cueillette; _fini = false; _finDans = -1; _indice = 9;

            for (int i = 0; i < _branches.Length; i++)
            {
                var f = new Fruit { X = _branches[i][0], Y = _branches[i][1], Phase = i * 1.3, Rang = i };
                f.O = Pose(Decor, _fruit, f.X, f.Y, cueillette ? TailleStock : 1.0);
                _stock.Add(f);
            }
            for (int i = 0; i < _total; i++)
            {
                if (i < _dedans) _dansPanier.Add(Pose(Osier, _fruit, PlaceX(i), RangY, TaillePanier));
                else _places.Add(Pose(Osier, "place", PlaceX(i), RangY, TaillePanier));
            }
            for (int v = 1; v <= Math.Min(Pancartes.Length, choix); v++)
            {
                var p = new Pancarte { V = v, X = Pancartes[v - 1] };
                p.Halo = Pose(Devant, "pancarte_halo", p.X, PancarteY); p.Halo.Op = 0;
                p.Planche = Pose(Devant, "pancarte", p.X, PancarteY);
                p.Chiffre = Devant.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = v.ToString(), Taille = 124, Couleur = "#FF7A2E" }.En(p.X, PancarteY - 2));
                p.Voile = Pose(Devant, "pancarte_voile", p.X, PancarteY); p.Voile.Op = 0;
                _pancartes.Add(p);
            }
            Compte(false);
            RefaitZones();
        }

        /// <summary>Ce n'est pas la bonne pancarte : elle se secoue et se voile.</summary>
        public void Refuse(int v)
        {
            var p = _pancartes.Find(q => q.V == v);
            if (p == null) return;
            p.Secoue = 0; p.Refusee = true;
            RefaitZones();
        }

        /// <summary>L'indice après une erreur : les places vides du panier clignotent.</summary>
        public void Indice() { _indice = 0; }

        /// <summary>C'est la bonne pancarte : elle saute de joie, et les fruits qui manquaient tombent un à un dans le panier.</summary>
        public void Verifie(int v)
        {
            var p = _pancartes.Find(q => q.V == v);
            if (p != null) { p.Saute = 0; for (int k = 0; k < 8; k++) _etoiles.Lance(p.X + Entre(-80, 80), PancarteY + Entre(-90, 60), Entre(-160, 160), Entre(-320, -80), 500, 0.9, Entre(0.7, 1.2), 0, Entre(-200, 200)); }
            _fini = true; RefaitZones();
            int manque = _total - _dedans;
            for (int i = 0; i < manque; i++) _aFaire.Add(new KeyValuePair<double, int>(_temps + 1.6 + i * 0.95, i));
            _finDit = "verifie";
        }

        private double PlaceX(int i) { double pas = Math.Min(78, 400.0 / _total); return PanierX + (i - (_total - 1) / 2.0) * pas; }

        private void RefaitZones()
        {
            _zones.Clear();
            if (!_fini)
            {
                if (_cueillette && _dedans + _vols.Count < _total)
                    foreach (var f in _stock) if (!f.Parti) _zones.Add(new VecZone { Id = "s:" + f.Rang, X = f.X - 88, Y = f.Y - 88, L = 176, H = 176 });
                foreach (var p in _pancartes) if (!p.Refusee) _zones.Add(new VecZone { Id = "c:" + p.V, X = p.X - 98, Y = PancarteY - 100, L = 196, H = 196 });
            }
            _etatZones++;
        }

        public override void Appuie(string id)
        {
            if (_fini || id == null || id.Length < 3) return;
            int n; if (!int.TryParse(id.Substring(2), out n)) return;
            if (id[0] == 's')
            {
                var f = _stock.Find(q => q.Rang == n);
                if (f == null || f.Parti || _dedans + _vols.Count >= _total) return;
                Decroche(f);
                RefaitZones();
            }
            else if (id[0] == 'c') Demande("choix:" + n);
        }

        /// <summary>Un fruit quitte sa branche et part vers la prochaine place libre du panier.</summary>
        private void Decroche(Fruit f)
        {
            f.Parti = true; f.O.Op = 0;
            int place = _dedans + _vols.Count;
            var o = Pose(Effets, _fruit, f.X, f.Y, f.O.E);
            _vols.Add(new Vol { O = o, X0 = f.X, Y0 = f.Y, X1 = PlaceX(place), Y1 = RangY, E0 = f.O.E, Place = place });
            _secousse = 0;
            for (int k = 0; k < 4; k++) _feuilles.Lance(f.X + Entre(-40, 40), f.Y + Entre(-50, 10), Entre(-50, 50), Entre(20, 90), 180, Entre(1.1, 1.7), Entre(0.8, 1.3), 0, Entre(-220, 220));
            Son("pof", 0);
        }

        private void Compte(bool bosse)
        {
            _compteur.Texte = _arrives.ToString(CultureInfo.InvariantCulture); _compteur.Etat++;
            if (bosse) _bosse = 0;
        }

        // ------------------------------------------------------------------
        //  Une image de plus
        // ------------------------------------------------------------------
        public override void Avance(double dt)
        {
            if (!_ouvert) return;
            dt = Math.Min(dt, 0.05);
            _temps += dt;

            // le ciel
            _rayons.R += 8 * dt; _soleil.E = 0.8 * (1 + 0.02 * Math.Sin(_temps * 2));
            for (int i = 0; i < _nuages.Length; i++) { _nuages[i].X += (6 + i * 3) * dt; if (_nuages[i].X > 1800) _nuages[i].X = -200; }
            // l'arbre frémit quand on cueille
            _secousse += dt;
            _arbre.R = (_secousse < 0.5 ? 1.6 * Math.Sin(_secousse * 40) * (1 - _secousse / 0.5) : 0) + 0.35 * Math.Sin(_temps * 0.9);

            // les fruits sur l'arbre : ils se balancent, et celui qu'on regarde grossit
            foreach (var f in _stock)
            {
                if (f.Parti) continue;
                bool vu = _cueillette && !_fini && _temps - _vu < 0.4 && Distance(_rx, _ry, f.X, f.Y) < 105;
                double taille = (_cueillette ? TailleStock : 1.0) * (vu ? 1.1 : 1);
                f.O.E += (taille - f.O.E) * Math.Min(1, dt * 10);
                f.O.R = (vu ? 9 : 4) * Math.Sin(_temps * (vu ? 9 : 1.7) + f.Phase);
                f.O.X = f.X; f.O.Y = f.Y + 2 * Math.Sin(_temps * 1.7 + f.Phase);
            }

            // la vérification : les fruits qui manquaient partent chacun à son tour
            for (int i = _aFaire.Count - 1; i >= 0; i--)
                if (_temps >= _aFaire[i].Key)
                {
                    var libre = _stock.Find(q => !q.Parti);
                    if (libre != null) Decroche(libre);
                    _aFaire.RemoveAt(i);
                }

            // les fruits qui volent vers le panier
            for (int i = _vols.Count - 1; i >= 0; i--)
            {
                var v = _vols[i];
                v.T += dt;
                double u = Math.Min(1, v.T / 0.75), d = Doux(u);
                v.O.X = Mel(v.X0, v.X1, d); v.O.Y = Mel(v.Y0, v.Y1, d) - 190 * Math.Sin(Math.PI * u) * (1 - 0.35 * u);
                v.O.E = Mel(v.E0, TaillePanier, u); v.O.R = 360 * d * (v.X1 > v.X0 ? 1 : -1);
                if (u < 1) continue;
                Effets.Retire(v.O); _vols.RemoveAt(i);
                Arrive(v.Place);
            }

            // le grand chiffre fait un bond quand il change
            _bosse += dt;
            _compteur.E = 1 + (_bosse < 0.45 ? 0.38 * Math.Sin(_bosse / 0.45 * Math.PI) : 0);
            _compteur.R = _bosse < 0.45 ? -5 * Math.Sin(_bosse / 0.45 * Math.PI * 2) : 0;
            _compteur.Y = 300 + 5 * Math.Sin(_temps * 1.8);

            // les places vides : elles battent doucement, et clignotent pour l'indice
            _indice += dt;
            foreach (var o in _places)
            {
                bool clignote = _indice < 2.4;
                o.Op = clignote ? 0.35 + 0.65 * Math.Abs(Math.Sin(_indice * Math.PI / 0.4)) : 0.75 + 0.15 * Math.Sin(_temps * 3);
                o.E = TaillePanier * (clignote ? 1 + 0.18 * Math.Abs(Math.Sin(_indice * Math.PI / 0.4)) : 1);
                o.R += 20 * dt;
            }

            // les pancartes
            foreach (var p in _pancartes)
            {
                p.Secoue += dt; p.Saute += dt;
                double dx = p.Secoue < 0.5 ? 16 * Math.Sin(p.Secoue * 44) * (1 - p.Secoue / 0.5) : 0;
                double dy = p.Saute < 1.6 ? -46 * Math.Abs(Math.Sin(p.Saute / 0.4 * Math.PI)) * (1 - p.Saute / 1.6) : 0;
                bool vu = !_fini && !p.Refusee && _temps - _vu < 0.4 && Math.Abs(_rx - p.X) < 98 && Math.Abs(_ry - PancarteY) < 100;
                double e = vu ? 1.06 : 1;
                foreach (var o in new[] { p.Planche, p.Halo, p.Voile }) { o.X = p.X + dx; o.Y = PancarteY + dy; o.E += (e - o.E) * Math.Min(1, dt * 10); }
                p.Chiffre.X = p.X + dx; p.Chiffre.Y = PancarteY - 2 + dy; p.Chiffre.E = p.Planche.E; p.Chiffre.Op = p.Refusee && p.Secoue > 0.5 ? 0.45 : 1;
                p.Voile.Op = p.Refusee ? Borne((p.Secoue - 0.3) / 0.3) : 0;
                p.Halo.Op = p.Saute < 2.4 ? 0.7 + 0.3 * Math.Sin(_temps * 8) : vu ? 0.6 : 0;
            }

            // le panier se tasse quand un fruit tombe dedans
            double tasse = _bosse < 0.3 ? 1 - 0.04 * Math.Sin(_bosse / 0.3 * Math.PI) : 1;
            _devant.SY = tasse; _anse.SY = tasse;

            AvancePapillons(dt);

            // la fin d'une question : on laisse le dernier chiffre se dire, puis on prévient l'activité
            if (_finDans >= 0 && _temps >= _finDans) { _finDans = -1; Demande(_finDit); }

            _etoiles.Avance(dt); _feuilles.Avance(dt);
            AvanceMots(dt);
        }

        /// <summary>Un fruit se pose dans le panier : on le compte.</summary>
        private void Arrive(int place)
        {
            double x = PlaceX(place);
            // la place vide laisse la sienne au fruit
            if (_places.Count > 0) { Osier.Retire(_places[0]); _places.RemoveAt(0); }
            _dansPanier.Add(Pose(Osier, _fruit, x, RangY, TaillePanier));
            _dedans++; _arrives = _dedans; Reussites++;
            Compte(true);
            Son("note", _dedans);
            Dit(_dedans.ToString(CultureInfo.InvariantCulture) + " !");
            for (int k = 0; k < 6; k++) _etoiles.Lance(x + Entre(-30, 30), RangY - 30, Entre(-180, 180), Entre(-360, -120), 700, 0.7, Entre(0.5, 0.9), 0, Entre(-200, 200));
            if (_dedans >= _total && _vols.Count == 0 && _aFaire.Count == 0)
            {
                if (_cueillette) { _fini = true; _finDit = "plein"; RefaitZones(); }
                _finDans = _temps + 1.1;
            }
            else if (_cueillette) RefaitZones();
        }

        private void AvancePapillons(double dt)
        {
            foreach (var p in _papillons)
            {
                if (Distance(p.X, p.Y, p.ButX, p.ButY) < 30) { p.ButX = Entre(720, 1540); p.ButY = Entre(380, 560); }
                double dx = p.ButX - p.X, dy = p.ButY - p.Y, d = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
                p.X += dx / d * 70 * dt; p.Y += dy / d * 70 * dt + 26 * Math.Sin(_temps * 5 + p.Phase) * dt;
                p.Sens = dx > 0 ? 1 : -1;
                double bat = 0.25 + 0.75 * Math.Abs(Math.Sin(_temps * 13 + p.Phase)), penche = 14 * p.Sens;
                p.Corps.En(p.X, p.Y, 0.9, penche);
                p.G.X = p.X; p.G.Y = p.Y; p.G.R = penche; p.G.SX = 0.9 * bat; p.G.SY = 0.9;
                p.D.X = p.X; p.D.Y = p.Y; p.D.R = penche; p.D.SX = -0.9 * bat; p.D.SY = 0.9;
            }
        }

        public override bool ProchaineCible(out double x, out double y)
        {
            x = 800; y = 420;
            if (_fini) return false;
            if (_cueillette)
            {
                var f = _stock.Find(q => !q.Parti);
                if (f == null || _dedans + _vols.Count >= _total) return false;
                x = f.X; y = f.Y; return true;
            }
            var p = _pancartes.Find(q => q.V == _total - _dedans && !q.Refusee);
            if (p == null) return false;
            x = p.X; y = PancarteY; return true;
        }
    }
}
