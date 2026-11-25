using System;
using System.Collections.Generic;
using System.Globalization;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// Les pancartes à chiffre des activités de nombres : une rangée de planches plantées dans l'herbe, que
    /// l'enfant choisit du regard. Celle qu'on regarde grossit, celle qui n'est pas la bonne se secoue et se
    /// voile, la bonne saute de joie. Les dessins (« pancarte », « pancarte_halo », « pancarte_voile ») sont
    /// dans NombresArt.
    /// </summary>
    internal sealed class VecPancartes
    {
        private sealed class Planche { public VecObjet Bois, Chiffre, Halo, Voile; public int V; public double X, Secoue = 9, Saute = 9; public bool Refusee; }
        private readonly List<Planche> _planches = new List<Planche>();
        private readonly VecCalque _calque;
        private readonly double _y;
        public VecPancartes(VecCalque calque, double y) { _calque = calque; _y = y; }

        public double Y => _y;
        public int Nombre => _planches.Count;

        private VecObjet Dessin(string cle, double x) => _calque.Ajoute(new VecObjet { Genre = VecGenre.Dessin, Dessin = cle }.En(x, _y));

        /// <summary>Plante les pancartes de ces valeurs, réparties autour de « milieu », à « pas » l'une de l'autre.</summary>
        public void Installe(IList<int> valeurs, double milieu, double pas = 216)
        {
            Vide();
            for (int i = 0; i < valeurs.Count; i++)
            {
                var p = new Planche { V = valeurs[i], X = milieu + (i - (valeurs.Count - 1) / 2.0) * pas };
                p.Halo = Dessin("pancarte_halo", p.X); p.Halo.Op = 0;
                p.Bois = Dessin("pancarte", p.X);
                string texte = p.V.ToString(CultureInfo.InvariantCulture);
                p.Chiffre = _calque.Ajoute(new VecObjet { Genre = VecGenre.Texte, Texte = texte, Taille = texte.Length > 1 ? 96 : 124, Couleur = "#FF7A2E" }.En(p.X, _y - 2));
                p.Voile = Dessin("pancarte_voile", p.X); p.Voile.Op = 0;
                _planches.Add(p);
            }
        }

        public void Vide()
        {
            foreach (var p in _planches) { _calque.Retire(p.Halo); _calque.Retire(p.Bois); _calque.Retire(p.Chiffre); _calque.Retire(p.Voile); }
            _planches.Clear();
        }

        /// <summary>Ce n'est pas la bonne : elle se secoue, se voile, et ne peut plus être choisie.</summary>
        public void Refuse(int v) { var p = _planches.Find(q => q.V == v); if (p != null) { p.Secoue = 0; p.Refusee = true; } }

        /// <summary>C'est la bonne : elle saute de joie. Rend sa place (pour y lancer des étoiles), ou NaN.</summary>
        public double Fete(int v) { var p = _planches.Find(q => q.V == v); if (p == null) return double.NaN; p.Saute = 0; return p.X; }

        public bool PlaceDe(int v, out double x) { var p = _planches.Find(q => q.V == v && !q.Refusee); x = p != null ? p.X : 0; return p != null; }

        /// <summary>Ajoute à la liste les zones des pancartes qu'on peut encore choisir.</summary>
        public void Zones(List<VecZone> zones)
        {
            foreach (var p in _planches) if (!p.Refusee) zones.Add(new VecZone { Id = "c:" + p.V, X = p.X - 98, Y = _y - 100, L = 196, H = 196 });
        }

        /// <summary>Fait vivre les pancartes. « actif » : on peut encore choisir ; (rx, ry) : le regard, s'il est « vu ».</summary>
        public void Avance(double dt, double temps, bool actif, bool vu, double rx, double ry)
        {
            foreach (var p in _planches)
            {
                p.Secoue += dt; p.Saute += dt;
                double dx = p.Secoue < 0.5 ? 16 * Math.Sin(p.Secoue * 44) * (1 - p.Secoue / 0.5) : 0;
                double dy = p.Saute < 1.6 ? -46 * Math.Abs(Math.Sin(p.Saute / 0.4 * Math.PI)) * (1 - p.Saute / 1.6) : 0;
                bool regardee = actif && vu && !p.Refusee && Math.Abs(rx - p.X) < 98 && Math.Abs(ry - _y) < 100;
                double e = regardee ? 1.06 : 1;
                foreach (var o in new[] { p.Bois, p.Halo, p.Voile }) { o.X = p.X + dx; o.Y = _y + dy; o.E += (e - o.E) * Math.Min(1, dt * 10); }
                p.Chiffre.X = p.X + dx; p.Chiffre.Y = _y - 2 + dy; p.Chiffre.E = p.Bois.E; p.Chiffre.Op = p.Refusee && p.Secoue > 0.5 ? 0.45 : 1;
                p.Voile.Op = p.Refusee ? Math.Max(0, Math.Min(1, (p.Secoue - 0.3) / 0.3)) : 0;
                p.Halo.Op = p.Saute < 2.4 ? 0.7 + 0.3 * Math.Sin(temps * 8) : regardee ? 0.6 : 0;
            }
        }
    }
}
