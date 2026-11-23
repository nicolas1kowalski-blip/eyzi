using System;
using System.Collections.Generic;

namespace MesPremiersJeux.Games
{
    // ══════════════════════════════════════════════════════════════════════
    //  La base des jeux « dessinés » (Le jet d'eau, La pêche…).
    //
    //  Un moteur de jeu ne connaît ni l'écran ni Windows : il tient une liste
    //  d'objets à dessiner (rangés par calques) et les fait bouger image par
    //  image. L'application le dessine avec WPF (VecToile.cs) ; le banc d'essai
    //  (desktop/jeux-dessines/banc) le fait tourner sous Linux et en tire de
    //  vraies images. C'est le même code des deux côtés : ce qu'on voit sur les
    //  images est ce que fait le jeu.
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Une forme d'un dessin. D : le tracé ; F : la couleur de remplissage ; G : ou un dégradé ; S : la couleur du trait ; W : son épaisseur ; O : l'opacité.</summary>
    internal struct VecForme { public string D, F, G, S; public double W, O; public VecForme(string d, string f, string g, string s, double w, double o) { D = d; F = f; G = g; S = s; W = w; O = o; } }
    /// <summary>Un objet posé dans une scène : son nom, son dessin, sa place, sa taille, son angle, ses couleurs.</summary>
    internal struct VecPose { public string Nom, Dessin; public double X, Y, E, R; public string[] C; public VecPose(string nom, string dessin, double x, double y, double e, double r, string[] c) { Nom = nom; Dessin = dessin; X = x; Y = y; E = e; R = r; C = c; } }

    internal enum VecGenre { Dessin, Plein, Ligne, Texte }

    /// <summary>Un objet à dessiner : un dessin du jeu, une forme pleine, une ligne ou un mot.</summary>
    internal sealed class VecObjet
    {
        public VecGenre Genre;
        public string Dessin;             // Dessin : la clé dans les dessins du jeu
        public string[] Couleurs;         // Dessin : les couleurs « $nom » (nom, valeur, nom, valeur…)
        public double X, Y, R, SX = 1, SY = 1, Op = 1;
        public double E { get => SY; set { SX = value; SY = value; } }
        public string Couleur;            // Plein, Ligne, Texte
        public double[] Points;           // Plein, Ligne : x0, y0, x1, y1… dans le repère de l'objet
        public int Etat;                  // change quand Points ou Texte changent (le dessinateur refait alors la forme)
        public double Epaisseur, Tiret, Espace, Decalage;   // Ligne (tirets en unités d'épaisseur)
        public string Decoupe;            // Plein : un dessin dont la première forme découpe l'objet (ex. l'eau dans l'aquarium)
        public string Texte; public double Taille;          // Texte : centré sur l'origine, cerné de sombre
        public VecObjet En(double x, double y, double e = 1, double r = 0) { X = x; Y = y; E = e; R = r; return this; }
    }

    /// <summary>Un calque : ses objets, du fond vers le devant.</summary>
    internal sealed class VecCalque
    {
        public readonly List<VecObjet> Objets = new List<VecObjet>();
        public int Etat;                  // change quand la liste change
        public VecObjet Ajoute(VecObjet o) { Objets.Add(o); Etat++; return o; }
        public VecObjet Avant(VecObjet repere, VecObjet o) { int i = Objets.IndexOf(repere); if (i < 0) Objets.Add(o); else Objets.Insert(i, o); Etat++; return o; }
        public VecObjet Apres(VecObjet repere, VecObjet o) { int i = Objets.IndexOf(repere); if (i < 0) Objets.Add(o); else Objets.Insert(i + 1, o); Etat++; return o; }
        public void Retire(VecObjet o) { if (Objets.Remove(o)) Etat++; }
        public void Vide() { if (Objets.Count > 0) { Objets.Clear(); Etat++; } }
    }

    /// <summary>Une zone que l'enfant peut choisir (une carte, un bouton) : l'application pose dessus un bouton invisible.</summary>
    internal sealed class VecZone { public string Id; public double X, Y, L, H; }

    internal abstract class VecMoteur
    {
        public const double W = 1600, H = 900;

        /// <summary>Les calques, du fond vers le devant.</summary>
        public abstract VecCalque[] Calques { get; }
        /// <summary>Les dessins du jeu (fabriqués par desktop/jeux-dessines/genere.js).</summary>
        public abstract Dictionary<string, VecForme[]> Dessins { get; }

        /// <summary>Le fond de la page : couleur du haut du ciel, du bas du ciel, et du sol sous l'image.</summary>
        public string CielHaut = "#7CC8FF", CielBas = "#E4F6FF", Sol = "#63B24C";
        /// <summary>La hauteur (dans la scène) où le ciel laisse la place au sol.</summary>
        public virtual double Horizon => 506;

        // Ce que le moteur demande à l'application.
        public Action<string> Dit = s => { };             // faire parler la voix
        public Action<string, int> Son = (s, i) => { };   // « klaxon », « pschhh », « plouf », « pop », « note » (+ numéro)
        public Action Bravo = () => { };                  // la fête (confettis, étoile)
        public Func<bool> VoixOccupee = () => false;
        public Action<string> Demande = s => { };         // autre chose : « photo » (garder une image du jeu)…

        /// <summary>Les zones que l'enfant peut choisir, s'il y en a ; EtatDesZones change quand elles changent.</summary>
        public virtual List<VecZone> Zones => null;
        public virtual int EtatDesZones => 0;
        /// <summary>L'enfant a choisi une zone (au regard, au doigt ou à la souris).</summary>
        public virtual void Appuie(string id) { }

        /// <summary>Le temps de fixation réglé pour l'enfant, en secondes : un jeu qui fait attendre le regard (le poisson qui mord) attend ce temps-là.</summary>
        public double Fixation = 0.9;

        public int Reussites { get; protected set; }
        /// <summary>La force du bruit de fond, de 0 à 1 (l'eau qui coule…).</summary>
        public virtual double Bruit => 0;
        /// <summary>Les étapes du jeu et celle en cours (les petits points en haut de l'écran) ; 0 étape : pas de points.</summary>
        public virtual int Etapes => 0;
        public virtual int Etape => 0;

        /// <summary>Ouvre le jeu (à appeler une fois Dit, Son et Bravo branchés).</summary>
        public abstract void Demarre(int scene = 0);
        /// <summary>Avance le jeu de dt secondes.</summary>
        public abstract void Avance(double dt);
        /// <summary>Ce qu'il reste à viser, s'il en reste (pour le regard automatique du banc d'essai).</summary>
        public abstract bool ProchaineCible(out double x, out double y);

        protected readonly Random _alea;
        protected double _temps, _vu = -10;
        protected double _rx = W / 2, _ry = 420;          // le regard
        /// <summary>Le calque où s'écrivent les mots de bande dessinée.</summary>
        protected abstract VecCalque CalqueDesMots { get; }

        protected VecMoteur(int graine) { _alea = graine == 0 ? new Random() : new Random(graine); }

        // ------------------------------------------------------------------
        //  Ce que l'application dit au moteur
        // ------------------------------------------------------------------
        /// <summary>L'enfant regarde ce point de la scène (ou y pose le pointeur).</summary>
        public void Regard(double x, double y) { _rx = x; _ry = y; _vu = _temps; }

        // ------------------------------------------------------------------
        //  Petits outils
        // ------------------------------------------------------------------
        protected VecObjet Pose(VecCalque calque, string dessin, double x, double y, double e = 1, params string[] couleurs)
            => calque.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = dessin, Couleurs = couleurs != null && couleurs.Length > 0 ? couleurs : null }.En(x, y, e));
        protected static VecObjet Dessin(string dessin, params string[] couleurs)
            => new VecObjet { Genre = VecGenre.Dessin, Dessin = dessin, Couleurs = couleurs != null && couleurs.Length > 0 ? couleurs : null };

        protected double Hasard() => _alea.NextDouble();
        protected double Entre(double a, double b) => a + (b - a) * _alea.NextDouble();
        protected static double Borne(double v, double a = 0, double b = 1) => Math.Max(a, Math.Min(b, v));
        protected static double Mel(double a, double b, double u) => a + (b - a) * u;
        protected static double Doux(double x) { x = Borne(x); return x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2; }
        protected static double Sortant(double x) { x = Borne(x); return 1 - Math.Pow(1 - x, 3); }
        protected static double Rebond(double x) => x <= 0 ? 0 : x >= 1 ? 1 : Math.Pow(2, -9 * x) * Math.Sin((x * 10 - 0.75) * (2 * Math.PI / 3)) + 1;
        protected static double Distance(double x1, double y1, double x2, double y2) => Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));

        /// <summary>Un mot de bande dessinée (« Tut tut ! », « Pschhh ! ») qui saute puis s'envole.</summary>
        protected void Ecris(string texte, double x, double y, string couleur, double taille = 64)
        {
            double demi = texte.Length * taille * 0.32;
            var o = CalqueDesMots.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = texte, Taille = taille, Couleur = couleur, Op = 0 });
            _mots.Add(new Mot { O = o, X = Borne(x, 40 + demi, W - 40 - demi), Y = Math.Max(80, y) });
        }

        protected sealed class Mot { public VecObjet O; public double Age, X, Y; }
        protected readonly List<Mot> _mots = new List<Mot>();

        /// <summary>Fait vivre les mots de bande dessinée (à appeler à chaque image).</summary>
        protected void AvanceMots(double dt)
        {
            for (int i = _mots.Count - 1; i >= 0; i--)
            {
                var m = _mots[i];
                m.Age += dt;
                if (m.Age > 1.4) { CalqueDesMots.Retire(m.O); _mots.RemoveAt(i); continue; }
                m.O.E = Rebond(m.Age / 0.45);
                m.O.X = m.X; m.O.Y = m.Y - 50 * Sortant(m.Age / 1.4);
                m.O.R = -6 + 4 * Math.Sin(m.Age * 9);
                m.O.Op = Borne((1.4 - m.Age) / 0.35);
            }
        }

        // ------------------------------------------------------------------
        //  Les petites choses qui volent : gouttes, bulles, vapeur, étoiles…
        // ------------------------------------------------------------------
        protected sealed class Grain { public VecObjet O; public double X, Y, VX, VY, G, Vie, Age, Taille, Gonfle, Tourne; public bool Vivant; }

        protected sealed class Bac
        {
            private readonly List<Grain> _grains = new List<Grain>();
            public Bac(VecMoteur m, VecCalque calque, string dessin, int nombre)
            {
                for (int i = 0; i < nombre; i++) { var o = m.Pose(calque, dessin, 0, 0); o.Op = 0; _grains.Add(new Grain { O = o }); }
            }
            public void Lance(double x, double y, double vx, double vy, double pesanteur, double vie, double taille = 1, double gonfle = 0, double tourne = 0)
            {
                foreach (var g in _grains)
                {
                    if (g.Vivant) continue;
                    g.X = x; g.Y = y; g.VX = vx; g.VY = vy; g.G = pesanteur; g.Vie = vie; g.Age = 0; g.Taille = taille; g.Gonfle = gonfle; g.Tourne = tourne; g.Vivant = true;
                    return;
                }
            }
            public void Avance(double dt)
            {
                foreach (var g in _grains)
                {
                    if (!g.Vivant) continue;
                    g.Age += dt;
                    if (g.Age >= g.Vie) { g.Vivant = false; g.O.Op = 0; continue; }
                    g.VY += g.G * dt; g.X += g.VX * dt; g.Y += g.VY * dt;
                    double u = g.Age / g.Vie;
                    g.O.X = g.X; g.O.Y = g.Y;
                    g.O.E = g.Taille * (1 + g.Gonfle * u) * Math.Min(1, g.Age * 14);
                    g.O.R += g.Tourne * dt;
                    g.O.Op = Math.Min(1, (1 - u) * 3);
                }
            }
            public void Vide() { foreach (var g in _grains) { g.Vivant = false; g.O.Op = 0; } }
        }

        /// <summary>Plusieurs objets qui bougent ensemble (la voiture, ses yeux, sa bouche, sa boue…).</summary>
        protected sealed class Membre { public VecObjet O; public double X, Y, SX = 1, SY = 1, R; public double E { set { SX = value; SY = value; } } }
        protected sealed class Groupe
        {
            public double X, Y, R, SX = 1, SY = 1;
            public readonly List<Membre> Membres = new List<Membre>();
            public Membre Ajoute(VecObjet o, double x = 0, double y = 0, double e = 1, double r = 0) { var m = new Membre { O = o, X = x, Y = y, SX = e, SY = e, R = r }; Membres.Add(m); return m; }
            public void Place()
            {
                double a = R * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
                foreach (var m in Membres)
                {
                    double x = m.X * SX, y = m.Y * SY;
                    m.O.X = X + x * c - y * s; m.O.Y = Y + x * s + y * c;
                    m.O.R = R + m.R; m.O.SX = SX * m.SX; m.O.SY = SY * m.SY;
                }
            }
            /// <summary>Où se trouve, dans la scène, un point donné dans le repère du groupe.</summary>
            public void Monde(double x, double y, out double mx, out double my)
            {
                double a = R * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
                x *= SX; y *= SY; mx = X + x * c - y * s; my = Y + x * s + y * c;
            }
        }

    }
}
