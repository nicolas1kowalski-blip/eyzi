using System;
using System.Collections.Generic;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le décor animé de l'activité « Dans l'ordre » (Apprendre).
    //
    //  En haut, trois cadres numérotés 1, 2, 3 ; en bas, trois cartes. Une
    //  histoire en trois images est d'abord montrée dans l'ordre, dans les
    //  cadres ; puis les images descendent, mélangées, sur les cartes, et
    //  l'enfant les remet une à une : celle qu'il regarde monte dans le premier
    //  cadre libre si c'est la bonne. L'activité (SequenceGame.cs) choisit
    //  l'histoire, pose les questions et parle.
    //
    //  Ce que le moteur signale à l'activité (par Demande) : « choix:1 » — la
    //  carte choisie, de gauche à droite (0 à 2).
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class OrdreMoteur : VecMoteur
    {
        private static readonly double[] Colonnes = { 350, 800, 1250 };
        private const double CadreY = 300, CarteY = 728, TailleCadre = 1.5, TailleCarte = 1.15;

        public readonly VecCalque Decor = new VecCalque(), Cadres = new VecCalque(), Images = new VecCalque(), Effets = new VecCalque();
        private readonly VecCalque[] _calques;
        public override VecCalque[] Calques => _calques;
        public override Dictionary<string, VecForme[]> Dessins => OrdreArt.Dessins;
        protected override VecCalque CalqueDesMots => Effets;
        public override double Horizon => 560;

        private sealed class Image { public VecObjet O; public double X, Y, E, X0, Y0, E0, X1, Y1, E1, T = -1, Retard, Saut = 9, Secoue = 9; public int Carte = -1; public bool Refusee; }
        private readonly Image[] _images = new Image[3];
        private readonly VecObjet[] _halos = new VecObjet[3], _cartes = new VecObjet[3], _cartesHalo = new VecObjet[3], _cartesCadre = new VecObjet[3];
        private readonly double[] _lumiere = { 9, 9, 9 };
        private readonly Bac _etoiles;
        private readonly List<VecZone> _zones = new List<VecZone>();
        private int _etatZones;
        public override List<VecZone> Zones => _zones;
        public override int EtatDesZones => _etatZones;

        private bool _ouvert, _actif;
        private int _suivant;                 // le prochain cadre à remplir (0, 1, 2)
        private double _sansChoix, _fin = 9;

        public OrdreMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Decor, Cadres, Images, Effets };
            foreach (var p in OrdreArt.Scenes["ordre"]) Decor.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = p.Dessin }.En(p.X, p.Y, p.E, p.R));
            for (int i = 0; i < 3; i++)
            {
                _halos[i] = Pose(Cadres, "o_cadre_halo", Colonnes[i], CadreY); _halos[i].Op = 0;
                Pose(Cadres, "o_cadre", Colonnes[i], CadreY);
                Pose(Cadres, "o_numero", Colonnes[i], CadreY - 196);
                Cadres.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = (i + 1).ToString(), Taille = 58, Couleur = "#FFFFFF" }.En(Colonnes[i], CadreY - 196));
                if (i < 2) Pose(Cadres, "o_fleche", (Colonnes[i] + Colonnes[i + 1]) / 2, CadreY);
                _cartesHalo[i] = Pose(Cadres, "o_carte_halo", Colonnes[i], CarteY); _cartesHalo[i].Op = 0;
                _cartes[i] = Pose(Cadres, "o_carte", Colonnes[i], CarteY); _cartes[i].Op = 0;
                _cartesCadre[i] = Pose(Cadres, "o_carte_cadre", Colonnes[i], CarteY); _cartesCadre[i].Op = 0;
            }
            _etoiles = new Bac(this, Effets, "etoile", 40);
            var ciel = OrdreArt.Ciels["ordre"];
            CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            Prepare(new[] { "s_graine", "s_pousse", "s_fleur" });
        }

        public override void Demarre(int scene = 0) { _ouvert = true; }

        // ------------------------------------------------------------------
        //  Ce que l'activité demande
        // ------------------------------------------------------------------
        /// <summary>Montre une histoire : ses trois images, dans l'ordre, dans les trois cadres.</summary>
        public void Prepare(IList<string> dessins)
        {
            for (int i = 0; i < 3; i++)
            {
                if (_images[i] != null) Images.Retire(_images[i].O);
                var im = new Image { X = Colonnes[i], Y = CadreY, E = TailleCadre };
                im.O = Pose(Images, dessins[i], im.X, im.Y, 0.1); im.Saut = -0.15 * i;
                _images[i] = im;
                _cartes[i].Op = 0;
            }
            _actif = false; _suivant = 3; _fin = 9;
            RefaitZones();
        }

        /// <summary>Le cadre i s'éclaire et son image saute : c'est d'elle qu'on parle.</summary>
        public void Raconte(int i) { if (i >= 0 && i < 3) { _lumiere[i] = 0; _images[i].Saut = 0; } }

        /// <summary>Les images descendent sur les cartes, mélangées : « ordre[k] » est l'image (0, 1, 2) qui va sur la carte k.</summary>
        public void Melange(IList<int> ordre)
        {
            for (int k = 0; k < 3; k++)
            {
                var im = _images[ordre[k]];
                im.Carte = k; im.Refusee = false;
                Part(im, Colonnes[k], CarteY, TailleCarte, 0.15 * k);
                _cartes[k].Op = 1;
            }
            _suivant = 0; _actif = true; _sansChoix = 0;
            Son("pof", 0);
            RefaitZones();
        }

        /// <summary>C'est la bonne image : elle monte dans le premier cadre libre.</summary>
        public void Place(int k)
        {
            var im = Array.Find(_images, q => q.Carte == k);
            if (im == null || _suivant > 2) return;
            Part(im, Colonnes[_suivant], CadreY, TailleCadre, 0);
            _lumiere[_suivant] = -0.5;        // le cadre s'éclaire quand elle arrive
            im.Carte = -1; _cartes[k].Op = 0.35; _suivant++; _sansChoix = 0;
            foreach (var q in _images) q.Refusee = false;
            Son("pop", 0);
            RefaitZones();
        }

        /// <summary>Ce n'est pas encore celle-là : elle se secoue.</summary>
        public void Refuse(int k) { var im = Array.Find(_images, q => q.Carte == k); if (im != null) { im.Secoue = 0; im.Refusee = true; RefaitZones(); } }

        /// <summary>L'histoire est dans l'ordre : les trois images sautent l'une après l'autre, les étoiles pleuvent.</summary>
        public void Fin() { _actif = false; _fin = 0; for (int i = 0; i < 3; i++) { _images[i].Saut = -0.25 * i; _lumiere[i] = -0.25 * i; } RefaitZones(); }

        private void Part(Image im, double x, double y, double e, double retard)
        {
            im.X0 = im.X; im.Y0 = im.Y; im.E0 = im.E; im.X1 = x; im.Y1 = y; im.E1 = e; im.T = 0; im.Retard = retard;
        }

        private void RefaitZones()
        {
            _zones.Clear();
            if (_actif) foreach (var im in _images) if (im.Carte >= 0 && !im.Refusee) _zones.Add(new VecZone { Id = "c:" + im.Carte, X = Colonnes[im.Carte] - 150, Y = CarteY - 150, L = 300, H = 300 });
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
            _temps += dt; _sansChoix += dt; _fin += dt;

            bool vu = _temps - _vu < 0.4;
            for (int i = 0; i < 3; i++)
            {
                var im = _images[i];
                // elle vole d'un cadre à une carte, ou d'une carte à un cadre
                if (im.T >= 0)
                {
                    im.T += dt;
                    double u = Borne((im.T - im.Retard) / 0.7), d = Doux(u);
                    im.X = Mel(im.X0, im.X1, d); im.Y = Mel(im.Y0, im.Y1, d) - 110 * Math.Sin(Math.PI * u); im.E = Mel(im.E0, im.E1, d);
                    if (u >= 1)
                    {
                        im.T = -1; im.Saut = 0;
                        if (im.Carte < 0) { Son("note", i * 2); for (int n = 0; n < 8; n++) _etoiles.Lance(im.X + Entre(-80, 80), im.Y + Entre(-80, 40), Entre(-200, 200), Entre(-360, -100), 700, 0.8, Entre(0.5, 1), 0, Entre(-200, 200)); }
                    }
                }
                im.Saut += dt; im.Secoue += dt;
                bool regardee = _actif && vu && im.Carte >= 0 && !im.Refusee && im.T < 0 && Math.Abs(_rx - Colonnes[im.Carte]) < 150 && Math.Abs(_ry - CarteY) < 150;
                double saut = im.Saut > 0 && im.Saut < 0.45 ? 26 * Math.Sin(im.Saut / 0.45 * Math.PI) : 0;
                double arrive = im.Saut < 0 ? 0.1 : im.Saut < 0.4 && im.T < 0 && im.O.E < im.E * 0.5 ? im.E * Rebond(im.Saut / 0.4) : -1;
                im.O.X = im.X + (im.Secoue < 0.5 ? 16 * Math.Sin(im.Secoue * 44) * (1 - im.Secoue / 0.5) : 0);
                im.O.Y = im.Y - saut;
                double taille = im.E * (regardee ? 1.08 : 1);
                if (arrive >= 0) im.O.E = Math.Max(0.1, arrive); else im.O.E += (taille - im.O.E) * Math.Min(1, dt * 12);
                im.O.R = im.Carte >= 0 && im.T < 0 ? 3 * Math.Sin(_temps * 1.8 + i * 1.7) : 0;
                im.O.Op = im.Refusee ? 0.55 : 1;

                if (im.Carte >= 0)
                {
                    // l'indice : au bout d'un moment sans choix, la bonne carte brille doucement
                    bool indice = _actif && i == _suivant && _sansChoix > 14 && Math.Sin(_temps * 5) > 0;
                    _cartesHalo[im.Carte].Op += ((indice ? 0.8 : 0) - _cartesHalo[im.Carte].Op) * Math.Min(1, dt * 10);
                    _cartesCadre[im.Carte].Op = regardee ? 1 : 0;
                }

                // la lumière du cadre
                _lumiere[i] += dt;
                _halos[i].Op += ((_lumiere[i] > 0 && _lumiere[i] < 1.6 ? 1 : 0) - _halos[i].Op) * Math.Min(1, dt * 10);
            }
            for (int k = 0; k < 3; k++) if (Array.Find(_images, q => q.Carte == k) == null) { _cartesHalo[k].Op = 0; _cartesCadre[k].Op = 0; }

            // la fête : les étoiles pleuvent sur les trois cadres
            if (_fin < 2.2 && Hasard() < dt * 16) _etoiles.Lance(Entre(180, 1420), Entre(60, 200), Entre(-60, 60), Entre(80, 220), 260, 1.4, Entre(0.7, 1.4), 0, Entre(-240, 240));

            _etoiles.Avance(dt);
            AvanceMots(dt);
        }

        public override bool ProchaineCible(out double x, out double y)
        {
            x = 800; y = 420;
            if (!_actif || _suivant > 2) return false;
            var im = _images[_suivant];
            if (im.Carte < 0 || im.T >= 0 || im.Refusee) return false;
            x = Colonnes[im.Carte]; y = CarteY; return true;
        }
    }
}
