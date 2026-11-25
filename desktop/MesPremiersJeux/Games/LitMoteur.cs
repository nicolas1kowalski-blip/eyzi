using System;
using System.Collections.Generic;
using System.Globalization;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le décor animé de l'activité « Sur, sous, devant, derrière » (Apprendre).
    //
    //  Une chambre et son lit, vu de côté. Une peluche saute SUR le lit, SOUS
    //  le lit, DEVANT le lit ou DERRIÈRE le lit (on ne voit plus que sa tête).
    //  En bas, quatre cartes montrent les quatre places en petit : c'est elles
    //  que l'enfant choisit. L'activité (SpatialGame.cs) pose les questions et
    //  parle.
    //
    //  Les places : 0 sur, 1 sous, 2 devant, 3 derrière ; 4 : à côté du lit, en
    //  attendant. Ce que le moteur signale à l'activité (par Demande) :
    //  « choix:2 » — la carte choisie, de gauche à droite (0 à 3).
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class LitMoteur : VecMoteur
    {
        private const double LitX = 800, LitY = 520;
        private const double CarteY = 765, Mini = 0.42;
        private static double CarteX(int k) => 215 + k * 390;

        public readonly VecCalque Fond = new VecCalque(), Arriere = new VecCalque(), LitFond = new VecCalque(), Milieu = new VecCalque(), LitDevant = new VecCalque(), Avant = new VecCalque(), Cartes = new VecCalque(), Effets = new VecCalque();
        private readonly VecCalque[] _calques;
        public override VecCalque[] Calques => _calques;
        public override Dictionary<string, VecForme[]> Dessins => ReperesArt.Dessins;
        protected override VecCalque CalqueDesMots => Effets;
        public override double Horizon => 640;

        private sealed class Carte { public readonly List<VecObjet> Objets = new List<VecObjet>(); public VecObjet Halo, Voile, Cadre; public double Secoue = 9, Saute = 9; public bool Refusee, Vue; public int Place; }
        private readonly Carte[] _cartes = new Carte[4];
        private readonly string[][] _peluches;
        private readonly VecObjet _ombre;
        private VecObjet _perso;
        private VecCalque _calquePerso;
        private readonly Bac _etoiles;
        private readonly List<VecZone> _zones = new List<VecZone>();
        private int _etatZones;
        public override List<VecZone> Zones => _zones;
        public override int EtatDesZones => _etatZones;

        /// <summary>La place que l'activité attend (pour l'indice et le banc d'essai), ou -1.</summary>
        public int Attendu = -1;
        private bool _ouvert, _actif = true;
        private int _peluche, _place = 4, _vers = 4;
        private double _hauteur = 196, _saut = -1, _x0, _y0, _e0, _joie = 9, _sansChoix;

        public LitMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Fond, Arriere, LitFond, Milieu, LitDevant, Avant, Cartes, Effets };
            foreach (var p in ReperesArt.Scenes["chambre"]) Fond.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = p.Dessin }.En(p.X, p.Y, p.E, p.R));
            Pose(LitFond, "c_lit_fond", LitX, LitY); Pose(LitDevant, "c_lit_devant", LitX, LitY);
            _ombre = Pose(Avant, "c_ombre", 0, 0); _ombre.Op = 0;
            _etoiles = new Bac(this, Effets, "etoile", 36);
            var l = ReperesArt.Listes["peluches"]; _peluches = new string[l.Length][];
            for (int i = 0; i < l.Length; i++) _peluches[i] = l[i];
            var ciel = ReperesArt.Ciels["chambre"];
            CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            Prepare(0, new[] { 0, 1, 2, 3 }, 4);
        }

        public override void Demarre(int scene = 0) { _ouvert = true; }

        /// <summary>Où se tient la peluche à cette place : son pied, sa taille, et le calque (derrière le lit, entre ses pieds, devant).</summary>
        private void Lieu(int place, out double x, out double y, out double e, out VecCalque calque)
        {
            switch (place)
            {
                case 0: x = LitX + 60; y = LitY - 242; e = 0.8; calque = Avant; break;
                case 1: x = LitX; y = LitY - 4; e = Math.Min(0.74, 140 / _hauteur); calque = Milieu; break;
                case 2: x = LitX; y = LitY + 92; e = 0.95; calque = Avant; break;
                case 3: x = LitX + 60; y = LitY - 168; e = 0.8; calque = Arriere; break;
                default: x = LitX + 520; y = LitY + 84; e = 0.95; calque = Avant; break;
            }
        }

        // ------------------------------------------------------------------
        //  Ce que l'activité demande
        // ------------------------------------------------------------------
        /// <summary>Installe une question : la peluche, l'ordre des quatre cartes (la place que montre chacune), et la place où la peluche se trouve.</summary>
        public void Prepare(int peluche, IList<int> ordre, int depart)
        {
            _peluche = Math.Max(0, Math.Min(_peluches.Length - 1, peluche));
            string dessin = _peluches[_peluche][0];
            _hauteur = double.Parse(_peluches[_peluche][2], CultureInfo.InvariantCulture);
            if (_perso != null) _calquePerso.Retire(_perso);
            _place = _vers = depart; _saut = -1; Attendu = -1; _actif = true; _sansChoix = 0;
            Lieu(depart, out double x, out double y, out double e, out _calquePerso);
            _perso = _calquePerso.Ajoute(Dessin(dessin).En(x, y, e));

            // les quatre cartes : chacune montre le lit en petit, et la peluche à sa place
            for (int k = 0; k < 4; k++)
            {
                if (_cartes[k] != null) foreach (var o in _cartes[k].Objets) Cartes.Retire(o);
                var c = new Carte { Place = ordre[k] }; _cartes[k] = c;
                double cx = CarteX(k), lx = cx - 12, ly = CarteY + 66;
                Lieu(c.Place, out double px, out double py, out double pe, out VecCalque pc);
                Func<VecObjet> mini = () => Dessin(dessin).En(lx + (px - LitX) * Mini, ly + (py - LitY) * Mini, pe * Mini);
                c.Halo = Dessin("c_carte_halo").En(cx, CarteY); c.Halo.Op = 0; c.Objets.Add(c.Halo);
                c.Objets.Add(Dessin("c_carte").En(cx, CarteY));
                if (pc == Arriere) c.Objets.Add(mini());
                c.Objets.Add(Dessin("c_lit_fond").En(lx, ly, Mini));
                if (pc == Milieu) c.Objets.Add(mini());
                c.Objets.Add(Dessin("c_lit_devant").En(lx, ly, Mini));
                if (pc == Avant) c.Objets.Add(mini());
                c.Voile = Dessin("c_carte_voile").En(cx, CarteY); c.Voile.Op = 0; c.Objets.Add(c.Voile);
                c.Cadre = Dessin("c_carte_cadre").En(cx, CarteY); c.Cadre.Op = 0; c.Objets.Add(c.Cadre);
                foreach (var o in c.Objets) Cartes.Ajoute(o);
            }
            RefaitZones();
        }

        /// <summary>La peluche saute à cette place (0 sur, 1 sous, 2 devant, 3 derrière, 4 à côté).</summary>
        public void Va(int place)
        {
            if (place == _vers && _saut < 0) { _joie = 0; return; }
            _vers = place; _saut = 0; _x0 = _perso.X; _y0 = _perso.Y; _e0 = _perso.E;
            Son("pop", 0);
        }

        /// <summary>Les cartes peuvent-elles être choisies ?</summary>
        public void Actif(bool oui) { _actif = oui; _sansChoix = 0; if (oui) foreach (var c in _cartes) { c.Refusee = false; } RefaitZones(); }

        /// <summary>Ce n'est pas la bonne carte : elle se secoue et se voile.</summary>
        public void Refuse(int k) { if (k >= 0 && k < 4) { _cartes[k].Secoue = 0; _cartes[k].Refusee = true; RefaitZones(); } }

        /// <summary>C'est la bonne carte (ou une carte découverte) : elle saute de joie.</summary>
        public void Fete(int k)
        {
            if (k < 0 || k > 3) return;
            _cartes[k].Saute = 0; _cartes[k].Vue = true;
            for (int n = 0; n < 8; n++) _etoiles.Lance(CarteX(k) + Entre(-120, 120), CarteY + Entre(-100, 60), Entre(-160, 160), Entre(-320, -80), 500, 0.9, Entre(0.7, 1.2), 0, Entre(-200, 200));
        }

        private void RefaitZones()
        {
            _zones.Clear();
            if (_actif) for (int k = 0; k < 4; k++) if (!_cartes[k].Refusee) _zones.Add(new VecZone { Id = "c:" + k, X = CarteX(k) - 180, Y = CarteY - 130, L = 360, H = 260 });
            _etatZones++;
        }

        public override void Appuie(string id)
        {
            if (!_actif || id == null || id.Length < 3 || id[0] != 'c') return;
            if (int.TryParse(id.Substring(2), out int k)) Demande("choix:" + k);
        }

        // ------------------------------------------------------------------
        //  Une image de plus
        // ------------------------------------------------------------------
        public override void Avance(double dt)
        {
            if (!_ouvert) return;
            dt = Math.Min(dt, 0.05);
            _temps += dt; _sansChoix += dt; _joie += dt;

            // la peluche : elle saute d'une place à l'autre, en changeant de plan à mi-chemin
            Lieu(_vers, out double x1, out double y1, out double e1, out VecCalque calque);
            if (_saut >= 0)
            {
                _saut += dt;
                double u = Math.Min(1, _saut / 0.7), d = Doux(u);
                if (u >= 0.5 && _calquePerso != calque) { _calquePerso.Retire(_perso); calque.Ajoute(_perso); _calquePerso = calque; }
                _perso.X = Mel(_x0, x1, d); _perso.Y = Mel(_y0, y1, d) - 170 * Math.Sin(Math.PI * u);
                _perso.E = Mel(_e0, e1, d); _perso.R = 14 * Math.Sin(Math.PI * u) * (x1 >= _x0 ? 1 : -1);
                if (u >= 1)
                {
                    _saut = -1; _place = _vers; _joie = 0;
                    Son("pof", 0);
                    for (int n = 0; n < 7; n++) _etoiles.Lance(x1 + Entre(-50, 50), y1 - 60 * e1, Entre(-200, 200), Entre(-360, -100), 700, 0.8, Entre(0.5, 1), 0, Entre(-200, 200));
                }
            }
            else
            {
                double hop = _joie < 0.9 ? 20 * Math.Abs(Math.Sin(_joie / 0.3 * Math.PI)) * (1 - _joie / 0.9) : 0;
                _perso.X = x1; _perso.Y = y1 - hop; _perso.R = 2.2 * Math.Sin(_temps * 1.6);
                _perso.SX = e1; _perso.SY = e1 * (1 + 0.015 * Math.Sin(_temps * 2.4));
            }
            // son ombre, là où il y a un sol à voir
            bool auSol = _saut < 0 && (_place == 2 || _place == 4 || _place == 0);
            _ombre.X = x1; _ombre.Y = y1 + 2; _ombre.E = e1; _ombre.Op += ((auSol ? 1 : 0) - _ombre.Op) * Math.Min(1, dt * 10);

            // les cartes
            bool vu = _temps - _vu < 0.4;
            for (int k = 0; k < 4; k++)
            {
                var c = _cartes[k];
                c.Secoue += dt; c.Saute += dt;
                double dx = c.Secoue < 0.5 ? 14 * Math.Sin(c.Secoue * 44) * (1 - c.Secoue / 0.5) : 0;
                double dy = c.Saute < 1.2 ? -30 * Math.Abs(Math.Sin(c.Saute / 0.4 * Math.PI)) * (1 - c.Saute / 1.2) : 0;
                bool regardee = _actif && vu && !c.Refusee && Math.Abs(_rx - CarteX(k)) < 180 && Math.Abs(_ry - CarteY) < 130;
                // l'indice : au bout d'un moment sans choix, la carte attendue brille doucement
                bool indice = _actif && Attendu >= 0 && c.Place == Attendu && _sansChoix > 14 && Math.Sin(_temps * 5) > 0;
                c.Halo.Op += ((c.Saute < 1.6 ? 1 : indice ? 0.8 : 0) - c.Halo.Op) * Math.Min(1, dt * 10);
                c.Cadre.Op = regardee ? 1 : 0;
                c.Voile.Op = c.Refusee ? Borne((c.Secoue - 0.3) / 0.3) : 0;
                if (dx != 0 || dy != 0 || c.Secoue < 0.6 || c.Saute < 1.3)
                {
                    // toute la carte bouge d'un bloc : chaque objet garde son écart au milieu de la carte
                    foreach (var o in c.Objets) { if (!_base.TryGetValue(o, out var b)) { b = new[] { o.X, o.Y }; _base[o] = b; } o.X = b[0] + dx; o.Y = b[1] + dy; }
                }
            }

            _etoiles.Avance(dt);
            AvanceMots(dt);
        }
        private readonly Dictionary<VecObjet, double[]> _base = new Dictionary<VecObjet, double[]>();

        public override bool ProchaineCible(out double x, out double y)
        {
            x = 800; y = 420;
            if (!_actif || _saut >= 0) return false;
            int k = Attendu >= 0 ? Array.FindIndex(_cartes, c => c.Place == Attendu && !c.Refusee) : Array.FindIndex(_cartes, c => !c.Vue);
            if (k < 0) return false;
            x = CarteX(k); y = CarteY; return true;
        }
    }
}
