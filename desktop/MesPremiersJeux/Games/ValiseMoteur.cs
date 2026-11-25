using System;
using System.Collections.Generic;
using System.Globalization;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le décor animé de l'activité « La valise du voyageur » (Apprendre).
    //
    //  Le pays où l'on part est derrière, avec son temps : la neige qui tombe,
    //  le grand soleil du désert, la pluie. Au premier plan, la valise ouverte ;
    //  autour, six habits sur leurs pastilles. L'habit regardé vole dans la
    //  valise si c'est un bon choix ; sinon il se secoue, et la voix en rit.
    //  Trois bons habits : le couvercle se ferme, on colle l'étiquette.
    //  L'activité (SuitcaseGame.cs) choisit le pays et les habits, et parle.
    //
    //  Ce que le moteur signale à l'activité (par Demande) : « choix:4 »
    //  (l'habit choisi, de 0 à 5), puis « range » quand il est dans la valise.
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class ValiseMoteur : VecMoteur
    {
        private const double ValiseX = 800, ValiseY = 632;      // le milieu de la charnière
        private const double TailleHabit = 1.12;

        public readonly VecCalque Fond = new VecCalque(), Meteo = new VecCalque(), Habits = new VecCalque(), Bagage = new VecCalque(), Effets = new VecCalque();
        private readonly VecCalque[] _calques;
        public override VecCalque[] Calques => _calques;
        public override Dictionary<string, VecForme[]> Dessins => ValiseArt.Dessins;
        protected override VecCalque CalqueDesMots => Effets;
        public override double Horizon => 548;

        private sealed class Habit { public VecObjet Pastille, Halo, O; public double X, Y, Phase, Secoue = 9; public bool Bon, Parti; public int Rang; }
        private sealed class Vol { public VecObjet O; public double T, X0, Y0, X1; public int Place; }
        private readonly List<Habit> _habits = new List<Habit>();
        private readonly List<VecObjet> _dedans = new List<VecObjet>();
        private readonly List<Vol> _vols = new List<Vol>();
        private readonly Dictionary<string, VecObjet> _pays = new Dictionary<string, VecObjet>();
        private readonly Dictionary<string, string> _dessins = new Dictionary<string, string>();
        private readonly double[][] _places;
        private readonly VecObjet _soleil, _rayons, _lueur, _couvercle, _fond, _devant, _dessus, _etiquette, _ombre;
        private readonly VecObjet[] _nuages;
        private readonly Bac _flocons, _gouttes, _eclats, _chaleurs, _fumee, _etoiles;
        private readonly List<VecZone> _zones = new List<VecZone>();
        private int _etatZones;
        public override List<VecZone> Zones => _zones;
        public override int EtatDesZones => _etatZones;

        private string _meteo = "neige";
        private bool _ouvert, _fini;
        private double _tasse = 9, _ferme = -1, _colle = 9, _sansChoix, _souffle;

        public ValiseMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Fond, Meteo, Habits, Bagage, Effets };
            _lueur = Pose(Fond, "lueur", 800, 96, 1.2); _rayons = Pose(Fond, "rayons", 800, 96, 1.1); _soleil = Pose(Fond, "soleil", 800, 96, 1.1);
            foreach (var m in new[] { "neige", "soleil", "pluie" })
                foreach (var p in ValiseArt.Scenes[m]) _pays[m] = Fond.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = p.Dessin }.En(p.X, p.Y, p.E, p.R));
            _fumee = new Bac(this, Fond, "brume", 8);
            _nuages = new[] { Pose(Fond, "nuage_gris", 260, 110, 1.2), Pose(Fond, "nuage_gris", 800, 70, 1.5), Pose(Fond, "nuage_gris", 1320, 120, 1.3) };
            _chaleurs = new Bac(this, Meteo, "chaleur", 10);
            _flocons = new Bac(this, Meteo, "flocon", 46);
            _gouttes = new Bac(this, Meteo, "pluie", 70);
            _eclats = new Bac(this, Meteo, "eclat_pluie", 12);

            _ombre = Pose(Bagage, "valise_ombre", ValiseX, ValiseY);
            _couvercle = Pose(Bagage, "valise_couvercle", ValiseX, ValiseY);
            _fond = Pose(Bagage, "valise_fond", ValiseX, ValiseY);
            _devant = Pose(Bagage, "valise_devant", ValiseX, ValiseY);
            _dessus = Pose(Bagage, "valise_dessus", ValiseX, ValiseY); _dessus.Op = 0;
            _etiquette = Pose(Bagage, "etiquette", ValiseX + 150, ValiseY + 110); _etiquette.Op = 0;
            _etoiles = new Bac(this, Effets, "etoile", 36);

            foreach (var l in ValiseArt.Listes["habits"]) _dessins[l[0]] = l[1];
            var pl = ValiseArt.Listes["places"]; _places = new double[pl.Length][];
            for (int i = 0; i < pl.Length; i++) _places[i] = new[] { double.Parse(pl[i][0], CultureInfo.InvariantCulture), double.Parse(pl[i][1], CultureInfo.InvariantCulture) };
            Prepare("neige", new[] { "echarpe", "maillot", "moufles", "lunettes", "manteau", "tongs" }, new[] { true, false, true, false, true, false });
        }

        public override void Demarre(int scene = 0) { _ouvert = true; }

        // ------------------------------------------------------------------
        //  Ce que l'activité demande
        // ------------------------------------------------------------------
        /// <summary>Installe un voyage : le temps qu'il fait (« neige », « soleil », « pluie »), les six habits, et lesquels sont de bons choix.</summary>
        public void Prepare(string meteo, IList<string> habits, IList<bool> bons)
        {
            foreach (var h in _habits) { Habits.Retire(h.Pastille); Habits.Retire(h.Halo); Habits.Retire(h.O); }
            foreach (var o in _dedans) Bagage.Retire(o);
            foreach (var v in _vols) Effets.Retire(v.O);
            _habits.Clear(); _dedans.Clear(); _vols.Clear();
            _meteo = _pays.ContainsKey(meteo ?? "") ? meteo : "soleil";
            foreach (var k in _pays) k.Value.Op = k.Key == _meteo ? 1 : 0;
            var ciel = ValiseArt.Ciels[_meteo];
            CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            bool soleil = _meteo == "soleil", pluie = _meteo == "pluie";
            _soleil.Op = soleil ? 1 : 0; _rayons.Op = soleil ? 1 : 0; _lueur.Op = soleil ? 1 : 0;
            foreach (var n in _nuages) n.Op = pluie ? 1 : 0;
            _flocons.Vide(); _gouttes.Vide(); _eclats.Vide(); _chaleurs.Vide(); _fumee.Vide();

            for (int i = 0; i < Math.Min(_places.Length, habits.Count); i++)
            {
                string dessin; if (!_dessins.TryGetValue(habits[i] ?? "", out dessin)) dessin = "h_teeshirt";
                var h = new Habit { X = _places[i][0], Y = _places[i][1], Phase = i * 1.1, Bon = i < bons.Count && bons[i], Rang = i };
                h.Halo = Pose(Habits, "pastille_halo", h.X, h.Y); h.Halo.Op = 0;
                h.Pastille = Pose(Habits, "pastille", h.X, h.Y);
                h.O = Pose(Habits, dessin, h.X, h.Y, TailleHabit);
                _habits.Add(h);
            }
            _fini = false; _ferme = -1; _colle = 9; _sansChoix = 0;
            _couvercle.SY = 1; _couvercle.Op = 1; _fond.Op = 1; _dessus.Op = 0; _etiquette.Op = 0;
            RefaitZones();
        }

        /// <summary>Ce n'est pas un bon choix : l'habit se secoue.</summary>
        public void Refuse(int i) { var h = _habits.Find(q => q.Rang == i); if (h != null) h.Secoue = 0; }

        /// <summary>C'est un bon choix : l'habit vole en cloche jusqu'à la valise.</summary>
        public void Range(int i)
        {
            var h = _habits.Find(q => q.Rang == i);
            if (h == null || h.Parti) return;
            h.Parti = true; h.O.Op = 0; _sansChoix = 0;
            int place = _dedans.Count + _vols.Count;
            var o = Pose(Effets, h.O.Dessin, h.X, h.Y, TailleHabit);
            _vols.Add(new Vol { O = o, X0 = h.X, Y0 = h.Y, X1 = ValiseX - 160 + (place % 3) * 160, Place = place });
            Son("pop", 0);
            RefaitZones();
        }

        /// <summary>La valise est prête : le couvercle se ferme, on colle l'étiquette.</summary>
        public void Ferme() { _fini = true; _ferme = 0; RefaitZones(); }

        private void RefaitZones()
        {
            _zones.Clear();
            if (!_fini) foreach (var h in _habits) if (!h.Parti) _zones.Add(new VecZone { Id = "c:" + h.Rang, X = h.X - 108, Y = h.Y - 108, L = 216, H = 216 });
            _etatZones++;
        }

        public override void Appuie(string id)
        {
            if (_fini || id == null || id.Length < 3 || id[0] != 'c') return;
            if (int.TryParse(id.Substring(2), out int i)) Demande("choix:" + i);
        }

        // ------------------------------------------------------------------
        //  Une image de plus
        // ------------------------------------------------------------------
        public override void Avance(double dt)
        {
            if (!_ouvert) return;
            dt = Math.Min(dt, 0.05);
            _temps += dt; _sansChoix += dt;

            // le temps qu'il fait
            switch (_meteo)
            {
                case "neige":
                    if (Hasard() < dt * 16) _flocons.Lance(Entre(-40, 1640), -20, Entre(-30, 30), Entre(90, 170), 0, 7.5, Entre(0.6, 1.4), 0, Entre(-60, 60));
                    _souffle -= dt;
                    if (_souffle <= 0) { _souffle = 0.6; _fumee.Lance(1117, 372, Entre(10, 30), -Entre(30, 55), 0, 2.4, 0.14, 2.2); }
                    break;
                case "pluie":
                    for (int k = 0; k < 3; k++) if (Hasard() < dt * 26) _gouttes.Lance(Entre(0, 1900), -40, -240, 920, 0, 1.05, Entre(0.8, 1.3));
                    if (Hasard() < dt * 9) _eclats.Lance(Entre(40, 1560), Entre(600, 880), 0, 0, 0, 0.3, Entre(0.6, 1), 0.5);
                    for (int i = 0; i < _nuages.Length; i++) { _nuages[i].X += (8 + i * 4) * dt; if (_nuages[i].X > 1900) _nuages[i].X = -300; }
                    break;
                default:
                    _rayons.R += 10 * dt; _soleil.E = 1.1 * (1 + 0.03 * Math.Sin(_temps * 2)); _lueur.E = 1.2 * (1 + 0.06 * Math.Sin(_temps * 1.4));
                    if (Hasard() < dt * 2.2) _chaleurs.Lance(Entre(120, 1480), Entre(560, 640), Entre(-10, 10), -Entre(26, 44), 0, 2.6, Entre(0.9, 1.5));
                    break;
            }

            // les habits : ils se dandinent, celui qu'on regarde s'éclaire, celui qui n'est pas bon se secoue
            bool vu = _temps - _vu < 0.4;
            foreach (var h in _habits)
            {
                h.Secoue += dt;
                h.Pastille.Op = h.Parti ? 0 : 1;
                if (h.Parti) { h.Halo.Op = 0; continue; }
                bool regarde = !_fini && vu && Distance(_rx, _ry, h.X, h.Y) < 112;
                // l'indice : au bout d'un moment sans choix, les bons habits brillent doucement
                bool indice = !_fini && h.Bon && _sansChoix > 14 && Math.Sin(_temps * 5 + h.Phase) > 0.2;
                h.Halo.Op += ((regarde ? 1 : indice ? 0.7 : 0) - h.Halo.Op) * Math.Min(1, dt * 10);
                double dx = h.Secoue < 0.6 ? 18 * Math.Sin(h.Secoue * 40) * (1 - h.Secoue / 0.6) : 0;
                h.O.X = h.X + dx; h.O.Y = h.Y + 3 * Math.Sin(_temps * 1.9 + h.Phase);
                h.O.R = 6 * Math.Sin(_temps * (1.3 + h.Rang * 0.12) + h.Phase);
                h.O.E += (TailleHabit * (regarde ? 1.1 : 1) - h.O.E) * Math.Min(1, dt * 10);
            }

            // l'habit qui vole vers la valise
            for (int n = _vols.Count - 1; n >= 0; n--)
            {
                var v = _vols[n];
                v.T += dt;
                double u = Math.Min(1, v.T / 0.75), d = Doux(u);
                v.O.X = Mel(v.X0, v.X1, d); v.O.Y = Mel(v.Y0, ValiseY - 46, d) - 190 * Math.Sin(Math.PI * u) * (1 - 0.3 * u);
                v.O.E = Mel(TailleHabit, 0.82, u); v.O.R = 360 * d * (v.X1 > v.X0 ? 1 : -1);
                if (u < 1) continue;
                Effets.Retire(v.O); _vols.RemoveAt(n);
                // il dépasse de la valise : on le range entre le fond et le devant
                var dedans = Bagage.Avant(_devant, Dessin(v.O.Dessin));
                dedans.En(v.X1, ValiseY - 46, 0.82, (v.Place % 3 - 1) * 8);
                _dedans.Add(dedans);
                _tasse = 0; Reussites++;
                Son("splat", 0);
                for (int k = 0; k < 7; k++) _etoiles.Lance(v.X1 + Entre(-40, 40), ValiseY - 30, Entre(-220, 220), Entre(-380, -120), 700, 0.8, Entre(0.5, 1), 0, Entre(-200, 200));
                Demande("range");
            }

            // la valise se tasse quand un habit tombe dedans ; le couvercle se referme à la fin
            _tasse += dt;
            double tasse = _tasse < 0.3 ? 1 - 0.07 * Math.Sin(_tasse / 0.3 * Math.PI) : 1;
            _devant.SY = tasse; _fond.SY = tasse;
            if (_ferme >= 0)
            {
                _ferme += dt;
                double u = Math.Min(1, _ferme / 0.5);
                _couvercle.SY = Math.Max(0.02, 1 - u * u);
                foreach (var o in _dedans) o.Op = 1 - u;
                if (u >= 1 && _dessus.Op == 0)
                {
                    _dessus.Op = 1; _couvercle.Op = 0; _fond.Op = 0; _colle = 0; _etiquette.Op = 1;
                    Son("tambour", 0);
                    for (int k = 0; k < 16; k++) _etoiles.Lance(ValiseX + Entre(-240, 240), ValiseY + Entre(-20, 120), Entre(-260, 260), Entre(-460, -140), 700, 1.0, Entre(0.6, 1.2), 0, Entre(-200, 200));
                }
            }
            _colle += dt;
            _etiquette.E = _colle < 0.45 ? Rebond(_colle / 0.45) : 1; _etiquette.R = -12 + 4 * Math.Sin(_temps * 2);

            _flocons.Avance(dt); _gouttes.Avance(dt); _eclats.Avance(dt); _chaleurs.Avance(dt); _fumee.Avance(dt); _etoiles.Avance(dt);
            AvanceMots(dt);
        }

        public override bool ProchaineCible(out double x, out double y)
        {
            x = 800; y = 420;
            if (_fini) return false;
            var h = _habits.Find(q => q.Bon && !q.Parti);
            if (h == null) return false;
            x = h.X; y = h.Y; return true;
        }
    }
}
