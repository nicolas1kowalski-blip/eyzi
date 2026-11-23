using System;
using System.Collections.Generic;
using System.Linq;

namespace MesPremiersJeux.Games
{
    // Les scènes du jeu « Le jet d'eau » après la voiture (voir JetMoteur.cs pour le moteur et la première scène).
    internal sealed partial class JetMoteur
    {
        // ══════════ 2. Les pompiers : la maison a peur, les flammes dansent sur son toit ══════════
        private sealed class SceneFeu : Scene
        {
            private sealed class Feu { public JetObjet O, Lueur; public double V = 1, E, X, Y; public int I; }
            private readonly List<Feu> _feux = new List<Feu>();
            private readonly Groupe _g = new Groupe();
            private Membre[] _pupilles, _sourcils;
            private Membre _portePeur, _porteSourire, _chatPeur, _chatContent;
            private JetObjet _gyro, _ombre;
            private double _son, _saut, X0, Y0;
            private bool _fete;

            public SceneFeu(JetMoteur j) : base(j) { }
            public override string Cle => "feu";
            public override string Consigne => "Au feu ! La maison a peur ! Éteins toutes les flammes avec ton jet d'eau !";
            public override string Felicite => "Bravo ! Le feu est éteint ! La maison et le chat sont sauvés !";
            public override bool Finie => _feux.All(f => f.V <= 0);
            public override double DureeFete => 4;
            private double Peur => _feux.Count == 0 ? 0 : _feux.Sum(f => Math.Max(0, f.V)) / _feux.Count;

            protected override void Construit()
            {
                _fete = false; _son = 0; _saut = 0;
                var maison = O["maison"];
                _ombre = O["ombre"];
                X0 = maison.X; Y0 = maison.Y;
                _g.Membres.Clear(); _g.X = X0; _g.Y = Y0; _g.R = 0; _g.SX = _g.SY = 1;
                _g.Ajoute(maison);
                // le visage se glisse entre les murs et les flammes
                JetObjet Sur(string dessin) => J.Decor.Avant(O["flamme0"], Dessin(dessin));
                _chatPeur = _g.Ajoute(Sur("chat_peur"), 0, -392, 0.86); _chatContent = _g.Ajoute(Sur("chat_content"), 0, -392, 0.86);
                _pupilles = new[] { _g.Ajoute(Sur("maison_pupille"), -150, -200), _g.Ajoute(Sur("maison_pupille"), 150, -200) };
                _sourcils = new[] { _g.Ajoute(Sur("maison_sourcil"), -150, -292), _g.Ajoute(Sur("maison_sourcil"), 150, -292) };
                _portePeur = _g.Ajoute(Sur("maison_porte_peur")); _porteSourire = _g.Ajoute(Sur("maison_porte_sourire"));
                var camion = O["camion"];
                _gyro = J.Decor.Apres(camion, Dessin("gyro")).En(camion.X + 316 * camion.E, camion.Y - 262 * camion.E);
                _feux.Clear();
                for (int i = 0; O.ContainsKey("flamme" + i); i++)
                {
                    var o = O["flamme" + i];
                    var lueur = J.Decor.Avant(o, Dessin("flamme_lueur")).En(o.X, o.Y, o.E);
                    _feux.Add(new Feu { O = o, Lueur = lueur, E = o.E, X = o.X, Y = o.Y, I = i });
                }
            }

            public override bool Reste(out double x, out double y)
            {
                var f = _feux.FirstOrDefault(k => k.V > 0);
                x = f?.X ?? 0; y = f == null ? 0 : f.Y - 80 * f.E;
                return f != null;
            }

            public override void Arrose(double x, double y, double dt)
            {
                bool mouille = false;
                _son -= dt;
                foreach (var f in _feux)
                {
                    if (f.V <= 0) continue;
                    if (Distance(x, y, f.X, f.Y - 90 * f.E) > 92 * f.E + 46) continue;
                    mouille = true;
                    f.V -= dt * 0.85;
                    if (J.Hasard() < 0.6) J._vapeur.Lance(x + J.Entre(-30, 30), y + J.Entre(-10, 20), J.Entre(-60, 60), J.Entre(-150, -80), 0, J.Entre(0.6, 1), J.Entre(0.6, 1), 1.4);
                    if (_son <= 0) { J.Son("pschhh", 0); _son = 0.6; }
                    if (f.V <= 0)
                    {
                        f.O.Op = 0; f.Lueur.Op = 0;
                        for (int k = 0; k < 8; k++) J._vapeur.Lance(f.X + J.Entre(-30, 30), f.Y - J.Entre(10, 110) * f.E, J.Entre(-100, 100), J.Entre(-190, -70), 0, J.Entre(0.8, 1.3), J.Entre(0.9, 1.5), 1.6);
                        J.Decor.Avant(f.O, Dessin("suie")).En(f.X, f.Y - 4, f.E);
                        J.Ecris("Pschhh !", f.X, f.Y - 190 * f.E, "#BFE8FF", 58);
                        J.Son("pschhh", 0); _son = 0.6;
                        J.Reussite();
                    }
                }
                if (mouille) J.Progres();
            }

            public override void Anime(double dt)
            {
                double peur = Peur;
                // les flammes dansent, crachent des braises et de la fumée
                Feu proche = null; double dmin = 1e9;
                foreach (var f in _feux)
                {
                    if (f.V <= 0) continue;
                    double k = 0.4 + 0.6 * Math.Max(0, f.V);
                    f.O.SX = f.E * k * (1 + 0.07 * Math.Sin(T * 17 + f.I * 2));
                    f.O.SY = f.E * k * (1 + 0.12 * Math.Sin(T * 13 + f.I));
                    f.O.R = 6 * Math.Sin(T * 7 + f.I * 1.7) + 9 * Remue;
                    f.Lueur.E = f.E * k * (1 + 0.1 * Math.Sin(T * 9 + f.I)); f.Lueur.Op = 0.85;
                    if (J.Hasard() < dt * 5) J._braises.Lance(f.X + J.Entre(-30, 30) * f.E, f.Y - J.Entre(60, 190) * f.E * k, J.Entre(-50, 50), J.Entre(-190, -100), -40, J.Entre(0.6, 1.2), J.Entre(0.6, 1.3));
                    if (J.Hasard() < dt * 1.4) J._fumee.Lance(f.X + J.Entre(-14, 14), f.Y - 200 * f.E * k, J.Entre(-20, 20), J.Entre(-100, -70), 0, J.Entre(1.3, 2), J.Entre(0.6, 1), 1.4);
                    double d = Distance(X0, Y0 - 260, f.X, f.Y);
                    if (d < dmin) { dmin = d; proche = f; }
                }

                // la maison tremble de peur, puis saute de joie
                _g.X = X0 + 2.2 * Math.Sin(T * 37) * peur; _g.Y = Y0 - _saut; _g.R = 0.5 * Math.Sin(T * 29) * peur + (_fete ? 1.6 * Math.Sin(T * 8) : 0);
                _g.SY = 1 + 0.03 * Borne(_saut / 40); _g.SX = 1 - 0.02 * Borne(_saut / 40);
                _ombre.SX = _ombre.SY = 1 - 0.2 * Borne(_saut / 40);

                // ses yeux suivent l'eau (ou la flamme la plus proche), ses sourcils s'inquiètent
                double vx = J._touche || proche == null ? J._cx : proche.X, vy = J._touche || proche == null ? J._cy : proche.Y - 80;
                bool cligne = (T % 3.7) > 3.56;
                for (int i = 0; i < 2; i++)
                {
                    double ex = i == 0 ? -150 : 150;
                    _g.Monde(ex, -200, out double mx, out double my);
                    double dx = vx - mx, dy = vy - my, d = Math.Max(1, Math.Sqrt(dx * dx + dy * dy)), k = Math.Min(24, d / 10);
                    _pupilles[i].X = ex + dx / d * k; _pupilles[i].Y = -200 + dy / d * k;
                    _pupilles[i].E = (1 - 0.3 * peur) * (cligne ? 0.1 : 1); _pupilles[i].SX = 1 - 0.3 * peur;
                    _sourcils[i].R = (i == 0 ? 1 : -1) * (24 * peur - (_fete ? 10 : 0)); _sourcils[i].Y = -292 - 10 * peur - (_fete ? 8 : 0);
                }
                bool effrayee = peur > 0.001;
                _portePeur.O.Op = effrayee ? 1 : 0; _porteSourire.O.Op = effrayee ? 0 : 1;
                _portePeur.SY = 1 + 0.05 * Math.Sin(T * 11) * peur;
                _chatPeur.O.Op = effrayee ? 1 : 0; _chatContent.O.Op = effrayee ? 0 : 1;
                _chatPeur.R = 6 * Math.Sin(T * 23) * peur; _chatContent.R = 8 * Math.Sin(T * 5);
                _g.Place();
                if (effrayee) { _chatPeur.O.Op = 1; }

                // elle transpire
                if (peur > 0.25 && J.Hasard() < dt * 3)
                    J._gouttes.Lance(X0 + (J.Hasard() < 0.5 ? -1 : 1) * J.Entre(60, 250), Y0 - J.Entre(280, 320), J.Entre(-40, 40), J.Entre(-60, 0), 500, 0.7, J.Entre(0.7, 1));
                _gyro.Op = 0.5 + 0.5 * Math.Sin(T * 11); _gyro.E = 0.9 + 0.2 * Math.Sin(T * 11);
            }

            public override void Fete(double u, double dt)
            {
                _fete = true;
                if (Passe(u, dt, 0.2)) { J.Son("pop", 0); J.Ecris("Miaou !", X0 + 150, Y0 - 470, "#FFB347", 64); }
                if (Passe(u, dt, 1.6)) J.Ecris("Merci !", X0, Y0 - 620, "#FF8FC1", 88);
                _saut = u < 3 ? 34 * Math.Abs(Math.Sin(u * 6)) : 0;
                if (J.Hasard() < dt * 5) J._coeurs.Lance(X0 + J.Entre(-60, 60), Y0 - 440, J.Entre(-40, 40), J.Entre(-130, -80), 0, J.Entre(1, 1.5), J.Entre(1, 1.8));
                if (J.Hasard() < dt * 10) J._etoiles.Lance(X0 + J.Entre(-340, 340), Y0 - J.Entre(40, 540), 0, -30, 0, 0.55, J.Entre(0.8, 1.8), 0, 160);
            }
        }

        // ══════════ 3. Remplir : l'aquarium, le bocal et le bain du canard ══════════
        private sealed class SceneRemplir : Scene
        {
            private sealed class Cuve
            {
                public string Nom; public JetObjet Devant, Eau, Ecume, Triste, Content;
                public double X, Y, E, G, D, H, B, Sable, Niveau, Plein = -1, Taille;      // G, D, H, B : l'intérieur (gauche, droite, haut, bas), dans le repère du dessin
                public double Surface => B - (B - H) * Niveau;
            }
            private readonly List<Cuve> _cuves = new List<Cuve>();

            public SceneRemplir(JetMoteur j) : base(j) { }
            public override string Cle => "remplir";
            public override string Consigne => "Les poissons et le canard n'ont plus d'eau ! Remplis tout avec ton jet d'eau !";
            public override string Felicite => "Bravo ! Tout est rempli ! Ils sont contents !";
            public override bool Finie => _cuves.All(c => c.Plein >= 0);

            protected override void Construit()
            {
                _cuves.Clear();
                Cree("aquarium", -192, 192, -292, -8, -86, "poisson", 1.0, "p0", "#FFC27A", "p1", "#FF8A3D", "p2", "#E0582E");
                Cree("bocal", -170, 170, -238, -14, -112, "poisson", 0.78, "p0", "#A8E4FF", "p1", "#4FB8FF", "p2", "#2A7FD0");
                Cree("bassin", -272, 272, -162, -18, -18, "canard", 1.15);
            }

            private void Cree(string nom, double g, double d, double h, double b, double sable, string hote, double taille, params string[] couleurs)
            {
                var devant = O[nom];
                var c = new Cuve { Nom = nom, Devant = devant, X = devant.X, Y = devant.Y, E = devant.E, G = g, D = d, H = h, B = b, Sable = sable, Taille = taille };
                // l'eau : une forme bleue dont le dessus ondule, découpée à la forme du récipient ; puis une écume claire à la surface
                c.Eau = J.Decor.Avant(devant, new JetObjet { Genre = JetGenre.Plein, Couleur = "#5CC0FF", Decoupe = nom + "_dedans", Op = 0.6 }.En(c.X, c.Y, c.E));
                c.Ecume = J.Decor.Avant(devant, new JetObjet { Genre = JetGenre.Plein, Couleur = "#E4F6FF", Decoupe = nom + "_dedans", Op = 0.9 }.En(c.X, c.Y, c.E));
                // le poisson (ou le canard) se voit à travers la vitre, devant l'eau
                c.Triste = J.Decor.Avant(devant, Dessin(hote + "_triste", couleurs));
                c.Content = J.Decor.Avant(devant, Dessin(hote + "_content", couleurs));
                _cuves.Add(c);
            }

            public override bool Reste(out double x, out double y)
            {
                var c = _cuves.FirstOrDefault(k => k.Plein < 0);
                x = c?.X ?? 0; y = c == null ? 0 : c.Y + (c.H + c.B) / 2 * c.E;
                return c != null;
            }

            public override void Arrose(double x, double y, double dt)
            {
                foreach (var c in _cuves)
                {
                    if (c.Plein >= 0) continue;
                    if (x < c.X + (c.G - 26) * c.E || x > c.X + (c.D + 26) * c.E || y < c.Y + (c.H - 120) * c.E || y > c.Y + (c.B + 14) * c.E) continue;
                    c.Niveau = Math.Min(1, c.Niveau + dt * 0.3);
                    J.Progres();
                    if (J.Hasard() < 0.5) J._bulles.Lance(c.X + J.Entre(c.G + 20, c.D - 20) * c.E, c.Y + (c.Surface + J.Entre(10, 50)) * c.E, J.Entre(-10, 10), J.Entre(-70, -40), 0, J.Entre(0.4, 0.8), J.Entre(0.4, 0.9));
                    if (c.Niveau >= 1)
                    {
                        c.Plein = T;
                        J.Son("plouf", 0);
                        J.Ecris(c.Nom == "bassin" ? "Coin coin !" : "Plouf !", c.X, c.Y + (c.H - 90) * c.E, c.Nom == "bassin" ? "#FFC83D" : "#7FD3FF", 64);
                        J.Eclat(c.X, c.Y + (c.H - 10) * c.E, 8);
                        J.Reussite();
                    }
                }
            }

            public override void Anime(double dt)
            {
                const int n = 14;
                for (int i = 0; i < _cuves.Count; i++)
                {
                    var c = _cuves[i];
                    // le dessus de l'eau ondule
                    double s = c.Surface, vague = c.Niveau > 0.02 ? 5 : 0;
                    var eau = new double[(n + 3) * 2]; var ecume = new double[(n + 1) * 4];
                    for (int k = 0; k <= n; k++)
                    {
                        double x = c.G + (c.D - c.G) * k / n, y = s + vague * Math.Sin(x * 0.05 + T * 4 + i);
                        eau[k * 2] = x; eau[k * 2 + 1] = y;
                        ecume[k * 2] = x; ecume[k * 2 + 1] = y - 3;
                        int b = (2 * n + 1 - k) * 2; ecume[b] = x; ecume[b + 1] = y + 7;
                    }
                    eau[(n + 1) * 2] = c.D; eau[(n + 1) * 2 + 1] = c.B + 4; eau[(n + 2) * 2] = c.G; eau[(n + 2) * 2 + 1] = c.B + 4;
                    c.Eau.Points = eau; c.Eau.Etat++; c.Ecume.Points = ecume; c.Ecume.Etat++;
                    c.Ecume.Op = c.Niveau > 0.03 ? 0.85 : 0;
                    c.Devant.R = c.Plein < 0 ? 1.4 * Remue : 0;

                    bool content = c.Niveau >= 0.3;
                    double e = c.Taille * c.E, x0, y0, r = 0, sens = 1;
                    if (c.Nom == "bassin")
                    {
                        // le canard flotte : il monte avec l'eau, puis tourne sur lui-même quand c'est plein
                        x0 = c.X + 60 * Math.Sin(T * 0.9) * c.Niveau * c.E;
                        y0 = c.Y + Math.Min(c.B - 22, s - 6) * c.E + (c.Niveau > 0.03 ? 3 * Math.Sin(T * 3.1) : 0);
                        r = 6 * Math.Sin(T * 3.1) * Borne(c.Niveau * 4);
                        if (c.Plein >= 0 && T - c.Plein < 2.2) sens = Math.Cos((T - c.Plein) * 9);
                    }
                    else if (!content)
                    {
                        // pas assez d'eau : le poisson gigote sur le sable
                        x0 = c.X; y0 = c.Y + (c.Sable - 6 - 12 * Math.Abs(Math.Sin(T * 7 + i))) * c.E; r = 14 * Math.Sin(T * 14 + i);
                    }
                    else
                    {
                        // il nage d'un bord à l'autre, entre le sable et la surface ; il saute quand c'est plein
                        double large = Math.Max(10, (c.D - c.G) / 2 - 96 * c.Taille - (c.Nom == "bocal" ? 20 : 0)), a = T * 1.5 + i * 2;
                        double saut = c.Plein >= 0 && T - c.Plein < 1.2 ? -170 * c.Taille * Math.Sin(Math.PI * (T - c.Plein) / 1.2) : 0;
                        x0 = c.X + large * Math.Sin(a) * c.E;
                        y0 = c.Y + ((s + c.Sable) / 2 + 10 * Math.Sin(T * 2.3 + i) + saut) * c.E;
                        sens = Math.Cos(a) >= 0 ? 1 : -1;
                        if (saut < 0) r = -30 * Math.Cos(Math.PI * (T - c.Plein) / 1.2) * sens;
                    }
                    foreach (var h in new[] { c.Triste, c.Content })
                    {
                        h.X = x0; h.Y = y0; h.R = r; h.SX = e * sens; h.SY = e;
                    }
                    c.Triste.Op = content ? 0 : 1; c.Content.Op = content ? 1 : 0;
                }
            }

            public override void Fete(double u, double dt)
            {
                foreach (var c in _cuves)
                    if (J.Hasard() < dt * 2.4)
                        J._coeurs.Lance(c.X + J.Entre(-70, 70), c.Y + (c.H - 20) * c.E, J.Entre(-20, 20), J.Entre(-120, -80), 0, J.Entre(1, 1.5), J.Entre(1, 1.8));
                if (J.Hasard() < dt * 8) J._etoiles.Lance(J.Entre(120, 1480), J.Entre(330, 640), 0, -30, 0, 0.55, J.Entre(0.8, 1.6), 0, 160);
            }
        }

        // ══════════ 4. Le jardin : les fleurs ont soif ══════════
        private sealed class SceneJardin : Scene
        {
            private static readonly string[] Formes = { "fleur_ronde", "fleur_tulipe", "fleur_pointue", "fleur_ronde", "fleur_tulipe" };
            private static readonly string[][] Teintes =
            {
                new[] { "#FFB8D6", "#FF6FA8" }, new[] { "#D8BEFF", "#9A6AE8" }, new[] { "#FFD23D", "#FFAA20" }, new[] { "#B0E4FF", "#4FB8FF" }, new[] { "#FFB0A0", "#F0483E" },
            };
            private sealed class Plante { public JetObjet Pousse, Tige, F1, F2, Bouton, Fleur, Papillon; public double X, Y, V, Eclose = -1, HautX, HautY; public int I; }
            private readonly List<Plante> _plantes = new List<Plante>();
            private JetObjet _arc, _escargot;
            private readonly JetObjet[] _abeilles = new JetObjet[2];

            public SceneJardin(JetMoteur j) : base(j) { }
            public override string Cle => "jardin";
            public override string Consigne => "Les fleurs ont soif ! Arrose-les pour les faire pousser !";
            public override string Felicite => "Bravo ! Quel beau jardin ! Regarde l'arc-en-ciel !";
            public override bool Finie => _plantes.All(p => p.Eclose >= 0);
            public override double DureeFete => 4.4;

            protected override void Construit()
            {
                _plantes.Clear();
                _arc = J.Decor.Apres(O["lointain"], Dessin("arcenciel")).En(800, 540);
                _arc.Op = 0;
                _escargot = O["escargot"];
                for (int i = 0; O.ContainsKey("plant" + i); i++)
                {
                    var pousse = O["plant" + i];
                    var t = Teintes[i % Teintes.Length]; var a = Teintes[(i + 2) % Teintes.Length];
                    var p = new Plante { Pousse = pousse, X = pousse.X, Y = pousse.Y, I = i };
                    p.Tige = J.Decor.Apres(pousse, Dessin("tige"));
                    p.F1 = J.Decor.Apres(p.Tige, Dessin("feuille")); p.F2 = J.Decor.Apres(p.F1, Dessin("feuille"));
                    p.Bouton = J.Decor.Apres(p.F2, Dessin("bouton", "f0", t[0], "f1", t[1]));
                    p.Fleur = J.Decor.Apres(p.Bouton, Dessin(Formes[i % Formes.Length], "f0", t[0], "f1", t[1]));
                    p.Papillon = J.Decor.Ajoute(Dessin("papillon", "a0", a[0], "a1", a[1]));
                    p.Fleur.Op = 0; p.Papillon.Op = 0; p.Tige.Op = 0; p.F1.Op = 0; p.F2.Op = 0; p.Bouton.Op = 0;
                    _plantes.Add(p);
                }
                for (int i = 0; i < 2; i++) { _abeilles[i] = J.Decor.Ajoute(Dessin("abeille")); _abeilles[i].Op = 0; }
            }

            public override bool Reste(out double x, out double y)
            {
                var p = _plantes.FirstOrDefault(k => k.Eclose < 0);
                x = p?.X ?? 0; y = p == null ? 0 : p.Y - 70;
                return p != null;
            }

            public override void Arrose(double x, double y, double dt)
            {
                foreach (var p in _plantes)
                {
                    if (p.Eclose >= 0) continue;
                    if (Math.Abs(x - p.X) > 125 || y < p.Y - 330 || y > p.Y + 120) continue;
                    p.V = Math.Min(1, p.V + dt * 0.36);
                    J.Progres();
                    if (p.V >= 1)
                    {
                        p.Eclose = T;
                        J.Son("note", p.I * 2 % 8);
                        J.Eclat(p.HautX, p.HautY, 9);
                        J.Reussite();
                    }
                }
            }

            public override void Anime(double dt)
            {
                bool fete = Finie; int ecloses = 0;
                foreach (var p in _plantes)
                {
                    double k = p.V, longueur = 0.12 + 0.88 * Borne(k / 0.75);
                    double penche = 34 * (1 - Borne(k * 2.2)) * (p.I % 2 == 0 ? 1 : -1) + (fete ? 9 : 3) * Math.Sin(T * (fete ? 4 : 2) + p.I) + 6 * (p.Eclose < 0 ? Remue : 0);
                    double a = penche * Math.PI / 180, haut = 190 * longueur;
                    p.Pousse.Op = Borne(1 - k * 5); p.Pousse.R = 8 * Math.Sin(T * 2.4 + p.I) + 10 * Remue; p.Pousse.E = 1.6 + 0.08 * Math.Sin(T * 3 + p.I);
                    p.Tige.Op = k > 0.02 ? 1 : 0; p.Tige.X = p.X; p.Tige.Y = p.Y; p.Tige.SX = 1; p.Tige.SY = longueur; p.Tige.R = penche;
                    p.HautX = p.X + haut * Math.Sin(a); p.HautY = p.Y - haut * Math.Cos(a);
                    double e1 = Sortant((k - 0.22) / 0.15), e2 = Sortant((k - 0.4) / 0.15);
                    p.F1.X = p.X + 0.32 * haut * Math.Sin(a); p.F1.Y = p.Y - 0.32 * haut * Math.Cos(a); p.F1.E = e1; p.F1.R = penche - 14 + 4 * Math.Sin(T * 3 + p.I); p.F1.Op = e1 > 0 ? 1 : 0;
                    p.F2.X = p.X + 0.54 * haut * Math.Sin(a); p.F2.Y = p.Y - 0.54 * haut * Math.Cos(a); p.F2.SX = -e2; p.F2.SY = e2; p.F2.R = penche + 14 - 4 * Math.Sin(T * 3 + p.I); p.F2.Op = e2 > 0 ? 1 : 0;
                    double eb = p.Eclose >= 0 ? 0 : Sortant((k - 0.5) / 0.2);
                    p.Bouton.X = p.HautX; p.Bouton.Y = p.HautY; p.Bouton.E = eb * (1 + 0.5 * Borne((k - 0.7) / 0.3)); p.Bouton.R = penche; p.Bouton.Op = eb > 0 ? 1 : 0;
                    if (p.Eclose >= 0)
                    {
                        ecloses++;
                        double age = T - p.Eclose;
                        p.Fleur.Op = 1; p.Fleur.X = p.HautX; p.Fleur.Y = p.HautY - 20;
                        p.Fleur.E = 1.05 * Rebond(age / 0.7) * (1 + 0.04 * Math.Sin(T * 3 + p.I)); p.Fleur.R = penche + (fete ? 12 : 6) * Math.Sin(T * (fete ? 5 : 2.2) + p.I);
                        // un papillon arrive et tourne autour de la fleur
                        p.Papillon.Op = Borne((age - 0.4) / 0.5);
                        p.Papillon.X = p.HautX + 96 * Math.Sin(T * 1.7 + p.I * 2); p.Papillon.Y = p.HautY - 130 + 40 * Math.Sin(T * 2.9 + p.I);
                        p.Papillon.SX = 0.7 * Math.Max(0.15, Math.Abs(Math.Sin(T * 15 + p.I))); p.Papillon.SY = 0.7;
                        p.Papillon.R = 14 * Math.Sin(T * 1.7 + p.I * 2);
                    }
                }
                // l'escargot se promène sur le muret ; les abeilles arrivent dès qu'il y a des fleurs
                _escargot.X += dt * 7; _escargot.SX = _escargot.SY * (1 + 0.06 * Math.Sin(T * 3));
                for (int i = 0; i < 2; i++)
                {
                    var b = _abeilles[i];
                    b.Op = Borne(b.Op + (ecloses > i ? dt * 2 : -dt * 2));
                    double a = T * (1.1 + i * 0.4) + i * 3;
                    b.X = 800 + 520 * Math.Sin(a); b.Y = 330 + 70 * Math.Sin(a * 2) + 10 * Math.Sin(T * 20);
                    b.SX = Math.Cos(a) >= 0 ? 1 : -1; b.SY = 1; b.R = 8 * Math.Sin(T * 9 + i);
                }
            }

            public override void Fete(double u, double dt)
            {
                _arc.Op = Borne(u / 1.4);
                _arc.E = 0.9 + 0.1 * Sortant(u / 1.4);
                for (int i = 0; i < 5; i++)
                    if (Passe(u, dt, 0.3 + i * 0.22)) J.Son("note", Math.Min(7, i * 2));
                if (J.Hasard() < dt * 8) J._etoiles.Lance(J.Entre(120, 1480), J.Entre(160, 600), 0, -30, 0, 0.55, J.Entre(0.8, 1.6), 0, 160);
            }
        }

        // ══════════ 5. La roue à eau : elle allume la fête, le soir ══════════
        private sealed class SceneRoue : Scene
        {
            private static readonly string[][] Verres =
            {
                new[] { "#FFE98A", "#FFC23D" }, new[] { "#FFB8D6", "#FF6FA8" }, new[] { "#B0F0C4", "#4ED07A" }, new[] { "#B0E4FF", "#4FB8FF" },
                new[] { "#FFD08A", "#FF9A2E" }, new[] { "#D8BEFF", "#9A6AE8" }, new[] { "#FFB0A0", "#F0483E" }, new[] { "#FFE98A", "#FFC23D" },
            };
            private static readonly string[][] Robes = { new[] { "#FFFFFF", "#DCE4F2" }, new[] { "#FFE4EE", "#FFB0CC" }, new[] { "#E4F6FF", "#A8D8F8" } };
            private JetObjet _roue;
            private readonly JetObjet[] _eteinte = new JetObjet[8], _allumee = new JetObjet[8], _halo = new JetObjet[8], _chevaux = new JetObjet[3], _ciel = new JetObjet[16];
            private readonly double[] _px = new double[8], _py = new double[8];
            private double _vitesse, _angle, _tours, _manege;
            private int _lumieres;

            public SceneRoue(JetMoteur j) : base(j) { }
            public override string Cle => "roue";
            public override string Consigne => "Arrose la grande roue pour la faire tourner et allumer les lumières de la fête !";
            public override string Felicite => "Bravo ! Toute la fête est allumée !";
            public override bool Finie => _lumieres >= 8;
            public override double DureeFete => 5;

            protected override void Construit()
            {
                _roue = O["roue"];
                _vitesse = 0; _angle = 0; _tours = 0; _manege = 0; _lumieres = 0;
                // les étoiles du ciel, derrière les collines
                for (int i = 0; i < _ciel.Length; i++)
                    _ciel[i] = J.Decor.Avant(O["lointain"], Dessin("etoile_ciel")).En(60 + (i * 397) % 1500, 40 + (i * 211) % 300, 0.7 + (i % 3) * 0.3);
                for (int i = 0; i < 8; i++)
                {
                    // les ampoules pendent le long de la guirlande (la même courbe que le fil)
                    double t = (i + 0.5) / 8;
                    _px[i] = (1 - t) * (1 - t) * 40 + 2 * t * (1 - t) * 800 + t * t * 1560; _py[i] = (1 - t) * (1 - t) * 84 + 2 * t * (1 - t) * 250 + t * t * 84 + 32;
                    _halo[i] = J.Decor.Ajoute(Dessin("halo", "v0", Verres[i][0])).En(_px[i], _py[i]); _halo[i].Op = 0;
                    _eteinte[i] = J.Decor.Ajoute(Dessin("ampoule", "v0", "#F4F6FB", "v1", "#AEB8CE")).En(_px[i], _py[i]);
                    _allumee[i] = J.Decor.Ajoute(Dessin("ampoule", "v0", Verres[i][0], "v1", Verres[i][1])).En(_px[i], _py[i]); _allumee[i].Op = 0;
                }
                for (int i = 0; i < 3; i++)
                    _chevaux[i] = J.Decor.Avant(O["manege_toit"], Dessin("cheval", "r0", Robes[i][0], "r1", Robes[i][1]));
            }

            public override bool Reste(out double x, out double y) { x = _roue.X + 120; y = _roue.Y - 150; return !Finie; }

            public override void Arrose(double x, double y, double dt)
            {
                if (Distance(x, y, _roue.X, _roue.Y) > 300) return;
                _vitesse = Math.Min(330, _vitesse + dt * 270);
                J.Progres();
                if (J.Hasard() < 0.6)
                {
                    // l'eau gicle de la roue dans le sens où elle tourne
                    double a = J.Entre(0, Math.PI * 2);
                    J._gouttes.Lance(_roue.X + Math.Cos(a) * 270, _roue.Y + Math.Sin(a) * 270, -Math.Sin(a) * 300, Math.Cos(a) * 300 - 80, 700, J.Entre(0.4, 0.7), J.Entre(0.7, 1.3));
                }
            }

            public override void Anime(double dt)
            {
                bool fete = Finie;
                if (fete) _vitesse = Math.Max(_vitesse, 240);      // la fête tourne toute seule
                else _vitesse -= _vitesse * dt * 0.45;
                _angle += _vitesse * dt; _tours += _vitesse * dt / 360;
                _roue.R = _angle;
                _roue.E = 1 + 0.03 * Remue;

                while (_lumieres < Math.Min(8, (int)(_tours / 0.9)))
                {
                    int i = _lumieres++;
                    _allumee[i].Op = 1; _eteinte[i].Op = 0; _halo[i].Op = 1;
                    J.Son("note", i);
                    J.Eclat(_px[i], _py[i] + 10, 7, 200);
                    J.Reussite();
                }
                for (int i = 0; i < _lumieres; i++)
                {
                    _halo[i].E = 1 + 0.14 * Math.Sin(T * 6 + i);
                    _halo[i].Op = fete ? 0.6 + 0.4 * Math.Sin(T * 12 + i * 1.3) : 0.9;
                }
                for (int i = 0; i < _ciel.Length; i++) _ciel[i].Op = 0.45 + 0.55 * Math.Abs(Math.Sin(T * (0.8 + i % 4 * 0.3) + i));

                // le manège tourne avec la roue : les chevaux passent devant, puis derrière le mât
                _manege += _vitesse * dt * 0.55;
                for (int i = 0; i < 3; i++)
                {
                    double a = (_manege + i * 120) * Math.PI / 180, c = Math.Cos(a), e = 0.85 + 0.15 * c;
                    var s = _chevaux[i];
                    s.X = 1180 + 170 * Math.Sin(a); s.Y = 548 + 22 * Math.Sin(a * 2 + i);
                    s.SX = e * (c >= 0 ? 1 : -1); s.SY = e;
                    s.Op = c < -0.2 ? 0.5 : 1;
                }
            }

            public override void Fete(double u, double dt)
            {
                // un petit feu d'artifice d'étoiles dans le ciel du soir
                if (J.Hasard() < dt * 3.4)
                {
                    J.Eclat(J.Entre(200, 1400), J.Entre(120, 380), 10, 320);
                    if (J.Hasard() < 0.5) J.Son("pop", 0);
                }
            }
        }
    }
}
