using System;
using System.Collections.Generic;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  Le décor animé de l'activité « Range la maison » (Apprendre).
    //
    //  Une maison de poupée vue en coupe : la cuisine, la salle de bain et la
    //  chambre côte à côte sous le toit. Premier acte : un regard sur une pièce
    //  l'allume et l'anime (la casserole fume, la mousse fait des bulles, la
    //  chambre dort). Deuxième acte : un objet attend sur son tapis, devant la
    //  maison ; l'enfant regarde une pièce, et l'objet s'y envole si c'est la
    //  bonne. L'activité (TidyHouseGame.cs) choisit les objets, pose les
    //  questions et parle.
    //
    //  Ce que le moteur signale à l'activité (par Demande) : « piece:1 » (la
    //  pièce choisie, de gauche à droite : 0, 1, 2).
    // ══════════════════════════════════════════════════════════════════════

    internal sealed class MaisonMoteur : VecMoteur
    {
        private const double PieceY = 210, PieceL = 480, PieceH = 430;
        private const double AttenteX = 800, AttenteY = 768, TailleAttente = 1.2;

        public readonly VecCalque Ciel = new VecCalque(), Decor = new VecCalque(), Pieces = new VecCalque(), Ranges = new VecCalque(), Murs = new VecCalque(), Devant = new VecCalque(), Effets = new VecCalque();
        private readonly VecCalque[] _calques;
        public override VecCalque[] Calques => _calques;
        public override Dictionary<string, VecForme[]> Dessins => MaisonArt.Dessins;
        protected override VecCalque CalqueDesMots => Effets;
        public override double Horizon => 560;

        private sealed class Piece { public VecObjet Fond, Halo, Ombre, Cadre; public double X, Allumee = 9, Refus = 9, Lumiere; public int Rangs; }
        private sealed class Vol { public VecObjet O; public double T, X0, Y0, X1, Y1, E1; public int Piece; }
        private readonly Piece[] _pieces = new Piece[3];
        private readonly List<VecObjet> _ranges = new List<VecObjet>();
        private readonly List<Vol> _vols = new List<Vol>();
        private readonly VecObjet _soleil, _rayons, _tapis, _projecteur;
        private readonly VecObjet[] _nuages;
        private VecObjet _objet;
        private readonly Bac _etoiles, _vapeur, _bulles, _fumee;
        private readonly List<VecZone> _zones = new List<VecZone>();
        private int _etatZones;
        public override List<VecZone> Zones => _zones;
        public override int EtatDesZones => _etatZones;

        private bool _ouvert, _choix, _fete;
        private int _bonne = -1;
        private double _arrive = 9, _non = 9, _sansChoix, _souffle, _feteVue;

        private static double PieceX(int i) => 70 + i * 490;

        public MaisonMoteur(int graine = 0) : base(graine)
        {
            _calques = new[] { Ciel, Decor, Pieces, Ranges, Murs, Devant, Effets };
            _rayons = Pose(Ciel, "rayons", 1520, 58, 0.5); _soleil = Pose(Ciel, "soleil", 1520, 58, 0.5);
            _nuages = new[] { Pose(Ciel, "nuage", 180, 80, 0.55), Pose(Ciel, "nuage", 1560, 250, 0.4) };
            foreach (var p in MaisonArt.Scenes["maison"]) Decor.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = p.Dessin }.En(p.X, p.Y, p.E, p.R));
            _fumee = new Bac(this, Decor, "brume", 8);
            Pose(Decor, "m_cheminee", 0, 0);

            var noms = MaisonArt.Listes["pieces"];
            for (int i = 0; i < 3; i++)
            {
                var p = new Piece { X = PieceX(i) };
                p.Fond = Pose(Pieces, noms[i][0], p.X, PieceY);
                p.Halo = Pose(Pieces, "m_halo", p.X, PieceY); p.Halo.Op = 0;
                _pieces[i] = p;
            }
            _vapeur = new Bac(this, Pieces, "brume", 10);
            _bulles = new Bac(this, Pieces, "bulle", 14);
            // l'ombre et le cadre passent par-dessus les objets rangés
            Pose(Murs, "m_maison", 0, 0);
            for (int i = 0; i < 3; i++)
            {
                var p = _pieces[i];
                p.Ombre = Pose(Murs, "m_ombre", p.X, PieceY); p.Ombre.Op = 0;
                p.Cadre = Pose(Murs, "m_cadre", p.X, PieceY); p.Cadre.Op = 0;
                Pose(Murs, "m_plaque", p.X + PieceL / 2, 164);
                Murs.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = noms[i][1], Taille = noms[i][1].Length > 12 ? 30 : 36, Couleur = "#FFF6DC" }.En(p.X + PieceL / 2, 164));
            }
            Pose(Devant, "m_buisson", 14, 724, 0.8); Pose(Devant, "m_buisson", 1592, 728, 0.85);
            _projecteur = Pose(Devant, "m_projecteur", AttenteX, AttenteY + 96); _projecteur.Op = 0;
            _tapis = Pose(Devant, "m_tapis", AttenteX, AttenteY + 96); _tapis.Op = 0;
            _etoiles = new Bac(this, Effets, "etoile", 40);

            var ciel = MaisonArt.Ciels["maison"];
            CielHaut = ciel[0]; CielBas = ciel[1]; Sol = ciel[2];
            Decouverte();
        }

        public override void Demarre(int scene = 0) { _ouvert = true; }

        // ------------------------------------------------------------------
        //  Ce que l'activité demande
        // ------------------------------------------------------------------
        /// <summary>Premier acte : la maison est vide et rangée, on regarde les pièces une à une.</summary>
        public void Decouverte()
        {
            foreach (var o in _ranges) Ranges.Retire(o);
            foreach (var v in _vols) Effets.Retire(v.O);
            _ranges.Clear(); _vols.Clear();
            if (_objet != null) { Devant.Retire(_objet); _objet = null; }
            foreach (var p in _pieces) { p.Rangs = 0; p.Allumee = 9; p.Refus = 9; }
            _tapis.Op = 0; _projecteur.Op = 0; _fete = false; _bonne = -1; _choix = true;
            RefaitZones();
        }

        /// <summary>La pièce i s'allume et s'anime (premier acte).</summary>
        public void Decouvre(int i)
        {
            if (i < 0 || i > 2) return;
            _pieces[i].Allumee = 0;
            for (int k = 0; k < 6; k++) _etoiles.Lance(PieceX(i) + Entre(60, 420), PieceY + Entre(40, 200), Entre(-80, 80), Entre(-160, -40), 200, 0.9, Entre(0.6, 1), 0, Entre(-200, 200));
            if (i == 2) Ecris("Zzz…", PieceX(2) + 190, PieceY + 240, "#DCCBFA", 54);
        }

        /// <summary>Deuxième acte : cet objet (une image de la banque) attend d'être rangé dans la pièce « bonne ».</summary>
        public void Montre(string image, int bonne)
        {
            if (_objet != null) Devant.Retire(_objet);
            _objet = Devant.Ajoute(new VecObjet { Genre = VecGenre.Image, Image = image }.En(AttenteX, AttenteY, 0.1));
            _arrive = 0; _bonne = bonne; _choix = true; _sansChoix = 0; _tapis.Op = 1;
            Son("pop", 0);
            RefaitZones();
        }

        /// <summary>Ce n'est pas la bonne pièce : elle s'assombrit un instant et l'objet fait non.</summary>
        public void Refuse(int i) { if (i >= 0 && i <= 2) _pieces[i].Refus = 0; _non = 0; }

        /// <summary>C'est la bonne pièce : l'objet s'y envole et y prend sa place.</summary>
        public void Range(int i)
        {
            if (_objet == null || i < 0 || i > 2) return;
            var p = _pieces[i];
            int rang = p.Rangs++;
            bool devant = rang < 4;
            double x = p.X + (devant ? 70 : 96) + (rang % 4) * (devant ? 113 : 96), y = PieceY + (devant ? 364 : 224);
            Devant.Retire(_objet);
            var o = Effets.Ajoute(_objet);
            _vols.Add(new Vol { O = o, X0 = _objet.X, Y0 = _objet.Y, X1 = x, Y1 = y, E1 = devant ? 0.56 : 0.46, Piece = i });
            _objet = null; _choix = false; _bonne = -1;
            RefaitZones();
        }

        /// <summary>La maison est rangée : toutes les pièces s'allument, les étoiles pleuvent.</summary>
        public void Fete() { _fete = true; _choix = false; _tapis.Op = 0; _feteVue = 0; RefaitZones(); }

        private void RefaitZones()
        {
            _zones.Clear();
            if (_choix && !_fete) for (int i = 0; i < 3; i++) _zones.Add(new VecZone { Id = "p:" + i, X = PieceX(i), Y = PieceY, L = PieceL, H = PieceH });
            _etatZones++;
        }

        public override void Appuie(string id)
        {
            if (!_choix || _fete || id == null || id.Length < 3 || id[0] != 'p') return;
            if (int.TryParse(id.Substring(2), out int i)) Demande("piece:" + i);
        }

        // ------------------------------------------------------------------
        //  Une image de plus
        // ------------------------------------------------------------------
        public override void Avance(double dt)
        {
            if (!_ouvert) return;
            dt = Math.Min(dt, 0.05);
            _temps += dt; _sansChoix += dt;

            _rayons.R += 8 * dt; _soleil.E = 0.5 * (1 + 0.02 * Math.Sin(_temps * 2));
            for (int i = 0; i < _nuages.Length; i++) { _nuages[i].X += (5 + i * 3) * dt; if (_nuages[i].X > 1800) _nuages[i].X = -200; }
            // la cheminée fume
            _souffle -= dt;
            if (_souffle <= 0) { _souffle = 0.7; _fumee.Lance(1192, 4, Entre(10, 40), -Entre(30, 60), 0, 2.2, 0.16, 2.2); }

            bool vu = _temps - _vu < 0.4;
            for (int i = 0; i < 3; i++)
            {
                var p = _pieces[i];
                p.Allumee += dt; p.Refus += dt;
                bool regardee = _choix && !_fete && vu && _rx >= p.X && _rx <= p.X + PieceL && _ry >= PieceY && _ry <= PieceY + PieceH;
                bool allumee = p.Allumee < 3.4 || _fete;
                // l'indice : au bout d'un moment sans réponse, la bonne pièce clignote doucement
                bool indice = _choix && i == _bonne && _sansChoix > 12 && Math.Sin(_temps * 5) > 0;
                double lumiere = allumee ? 1 : indice ? 0.7 : regardee ? 0.45 : 0;
                p.Lumiere += (lumiere - p.Lumiere) * Math.Min(1, dt * 8);
                p.Halo.Op = 0.6 * p.Lumiere * (allumee ? 0.85 + 0.15 * Math.Sin(_temps * 6) : 1);
                p.Cadre.Op = regardee ? 1 : 0;
                p.Ombre.Op = p.Refus < 0.9 ? Math.Sin(p.Refus / 0.9 * Math.PI) : 0;
                p.Fond.X = p.X + (p.Refus < 0.5 ? 9 * Math.Sin(p.Refus * 44) * (1 - p.Refus / 0.5) : 0);
                // la pièce allumée s'anime : la casserole fume, la baignoire fait des bulles
                if (allumee && Hasard() < dt * 7)
                {
                    if (i == 0) _vapeur.Lance(p.X + 400 + Entre(-14, 14), PieceY + 204, Entre(-14, 14), -Entre(50, 90), 0, 1.4, 0.14, 1.6);
                    if (i == 1) _bulles.Lance(p.X + Entre(60, 270), PieceY + 240, Entre(-20, 20), -Entre(60, 120), -30, 1.5, Entre(0.7, 1.3));
                }
            }

            // l'objet qui attend : il arrive en sautant, flotte un peu, et fait non si on se trompe
            _arrive += dt; _non += dt;
            if (_objet != null)
            {
                double e = TailleAttente * (_arrive < 0.45 ? Rebond(_arrive / 0.45) : 1 + 0.03 * Math.Sin(_temps * 2.6));
                _objet.E = e;
                _objet.X = AttenteX + (_non < 0.5 ? 22 * Math.Sin(_non * 40) * (1 - _non / 0.5) : 0);
                _objet.Y = AttenteY - 10 + 8 * Math.Sin(_temps * 2.2);
                _objet.R = 3 * Math.Sin(_temps * 1.7);
                _projecteur.Op = 0.8 + 0.2 * Math.Sin(_temps * 3);
            }
            else _projecteur.Op = Math.Max(0, _projecteur.Op - dt * 3);

            // l'objet qui s'envole vers sa pièce
            for (int n = _vols.Count - 1; n >= 0; n--)
            {
                var v = _vols[n];
                v.T += dt;
                double u = Math.Min(1, v.T / 0.85), d = Doux(u);
                v.O.X = Mel(v.X0, v.X1, d); v.O.Y = Mel(v.Y0, v.Y1, d) - 130 * Math.Sin(Math.PI * u);
                v.O.E = Mel(TailleAttente, v.E1, d); v.O.R = 360 * d * (v.X1 > v.X0 ? 1 : -1);
                if (u < 1) continue;
                Effets.Retire(v.O); Ranges.Ajoute(v.O); v.O.R = 0; v.O.E = v.E1; v.O.X = v.X1; v.O.Y = v.Y1;
                _ranges.Add(v.O); _vols.RemoveAt(n);
                _pieces[v.Piece].Allumee = 1.8; Reussites++;
                Son("pof", 0);
                for (int k = 0; k < 9; k++) _etoiles.Lance(v.X1 + Entre(-40, 40), v.Y1 - 20, Entre(-220, 220), Entre(-380, -100), 700, 0.8, Entre(0.5, 1), 0, Entre(-200, 200));
            }

            // la fête : les étoiles pleuvent sur la maison
            if (_fete && _temps - _feteVue > 0.07)
            {
                _feteVue = _temps;
                _etoiles.Lance(Entre(80, 1520), Entre(40, 200), Entre(-60, 60), Entre(80, 220), 260, 1.6, Entre(0.7, 1.4), 0, Entre(-240, 240));
            }

            _etoiles.Avance(dt); _vapeur.Avance(dt); _bulles.Avance(dt); _fumee.Avance(dt);
            AvanceMots(dt);
        }

        public override bool ProchaineCible(out double x, out double y)
        {
            x = 800; y = 420;
            if (!_choix || _fete) return false;
            // au premier acte, une pièce pas encore allumée ; au deuxième, la bonne pièce
            int i = _bonne >= 0 ? _bonne : Array.FindIndex(_pieces, p => p.Allumee > 8);
            if (i < 0) return false;
            x = PieceX(i) + PieceL / 2; y = PieceY + PieceH / 2;
            return true;
        }
    }
}
