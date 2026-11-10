using System;
using System.Collections.Generic;

namespace MesPremiersJeux.Views.Creer.Volume
{
    /// <summary>
    /// DU DESSIN AU VOLUME : à partir d'une image (un dessin de l'application), on
    /// fabrique la forme à sculpter. La silhouette est gonflée comme une ronde-bosse
    /// (plus on est loin du bord, plus c'est haut), et les traits sombres du dessin
    /// deviennent des sillons, de sorte que chaque partie (une oreille, un œil, une
    /// patte) se bombe à part. Calcul pur, sans rien de l'interface.
    /// </summary>
    public static class Relief
    {
        public sealed class Figure
        {
            public int S;            // côté du carré
            public float[] Hauteur;  // 0 (dehors) à ~0,86, S×S
            public byte[] Couleur;   // les couleurs du dessin, B G R par pixel (pour la peinture)
            public bool Vide;        // rien d'exploitable dans l'image
        }

        /// <summary>bgra : S×S pixels B G R A prémultipliés (ce que rend WPF).</summary>
        public static Figure DepuisImage(byte[] bgra, int S)
        {
            int n = S * S;
            var lum = new float[n]; var sat = new float[n];
            var opaque = new bool[n]; var couleur = new byte[n * 3];
            int nOpaque = 0;
            for (int p = 0, i = 0; p < n; p++, i += 4)
            {
                int a = bgra[i + 3];
                if (a < 128) continue;
                opaque[p] = true; nOpaque++;
                float b = Math.Min(1f, bgra[i] / (float)a), g = Math.Min(1f, bgra[i + 1] / (float)a), r = Math.Min(1f, bgra[i + 2] / (float)a);
                couleur[p * 3] = (byte)(b * 255); couleur[p * 3 + 1] = (byte)(g * 255); couleur[p * 3 + 2] = (byte)(r * 255);
                lum[p] = 0.299f * r + 0.587f * g + 0.114f * b;
                float mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b));
                sat[p] = mx > 1e-3f ? (mx - mn) / mx : 0;
            }
            var fig = new Figure { S = S, Hauteur = new float[n], Couleur = couleur };
            if (nOpaque < n / 100) { fig.Vide = true; return fig; }

            // 1. La silhouette : ce qui est opaque, moins le « pâle » relié au bord (ombre au sol, liseré blanc).
            var pale = new bool[n];
            for (int p = 0; p < n; p++) pale[p] = !opaque[p] || (lum[p] > 0.72f && sat[p] < 0.18f);
            var dehors = Inonder(pale, S, true);
            var masque = new bool[n]; int nMasque = 0;
            for (int p = 0; p < n; p++) masque[p] = opaque[p] && !dehors[p];
            masque = Dilater(Dilater(Eroder(Eroder(masque, S), S), S), S); // enlève les miettes et les ponts fins
            masque = PlusGrande(masque, S);
            // les trous fermés (un blanc d'œil isolé) font partie de la figure
            var vide = new bool[n];
            for (int p = 0; p < n; p++) vide[p] = !masque[p];
            var autour = Inonder(vide, S, true);
            for (int p = 0; p < n; p++) { if (!masque[p] && !autour[p]) masque[p] = true; if (masque[p]) nMasque++; }
            if (nMasque < 0.4 * nOpaque) { masque = opaque; nMasque = nOpaque; }

            // 2. Le gonflement général.
            var d = Distance(masque, S);
            float dmax = 0; for (int p = 0; p < n; p++) if (d[p] > dmax) dmax = d[p];
            float D = Math.Max(8f, 0.55f * dmax);
            // 3. Les traits sombres, à l'intérieur, deviennent des sillons.
            var sansTraits = new bool[n];
            for (int p = 0; p < n; p++) sansTraits[p] = masque[p] && !(lum[p] < 0.30f && d[p] > 7.5f);
            var d2 = Distance(sansTraits, S);
            var h = new float[n];
            for (int p = 0; p < n; p++)
            {
                if (!masque[p]) continue;
                h[p] = Dome(d[p], D) * (0.72f + 0.28f * Dome(d2[p], 16f));
            }
            h = Flou(Flou(Flou(h, S, 2), S, 2), S, 2);
            for (int p = 0; p < n; p++) if (!masque[p]) h[p] = 0;
            h = Flou(h, S, 1);

            // 4. Posée en bas, centrée ; un peu moins haute que le bloc.
            int x0 = S, x1 = -1, y1 = -1;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) if (h[y * S + x] > 0.02f) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y > y1) y1 = y; }
            if (x1 < 0) { fig.Vide = true; return fig; }
            int dx = (S - 1 - x1 - x0) / 2, dy = Math.Max(0, S - 5 - y1);
            var coul2 = new byte[n * 3];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int sx = x - dx, sy = y - dy;
                    if (sx < 0 || sy < 0 || sx >= S || sy >= S) continue;
                    int p = y * S + x, q = sy * S + sx;
                    fig.Hauteur[p] = h[q] * 0.86f;
                    coul2[p * 3] = couleur[q * 3]; coul2[p * 3 + 1] = couleur[q * 3 + 1]; coul2[p * 3 + 2] = couleur[q * 3 + 2];
                    // (0, 0, 0) veut dire « pas de peinture ici » : un trait noir du dessin devient presque noir.
                    if (fig.Hauteur[p] > 0.01f && coul2[p * 3] == 0 && coul2[p * 3 + 1] == 0 && coul2[p * 3 + 2] == 0) coul2[p * 3] = 1;
                }
            fig.Couleur = coul2;
            return fig;
        }

        /// <summary>La même figure, deux fois plus petite (moyenne de quatre pixels) : pour un étage de totem.</summary>
        public static Figure Reduire(Figure f)
        {
            int s = f.S / 2;
            var r = new Figure { S = s, Hauteur = new float[s * s], Couleur = new byte[s * s * 3], Vide = f.Vide };
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    int a = (2 * y) * f.S + 2 * x, b = a + 1, c = a + f.S, d = c + 1, p = y * s + x;
                    r.Hauteur[p] = (f.Hauteur[a] + f.Hauteur[b] + f.Hauteur[c] + f.Hauteur[d]) * 0.25f;
                    // la couleur du pixel le plus haut des quatre (pas de mélange avec le vide)
                    int m = a; if (f.Hauteur[b] > f.Hauteur[m]) m = b; if (f.Hauteur[c] > f.Hauteur[m]) m = c; if (f.Hauteur[d] > f.Hauteur[m]) m = d;
                    r.Couleur[p * 3] = f.Couleur[m * 3]; r.Couleur[p * 3 + 1] = f.Couleur[m * 3 + 1]; r.Couleur[p * 3 + 2] = f.Couleur[m * 3 + 2];
                }
            return r;
        }

        private static float Dome(float d, float D)
        {
            float t = d / D; if (t > 1) t = 1; if (t < 0) t = 0;
            float v = t * (2 - t);
            return (float)Math.Sqrt(v < 0 ? 0 : v > 1 ? 1 : v);
        }

        /// <summary>Les pixels « vrais » reliés au bord de l'image (par les côtés).</summary>
        private static bool[] Inonder(bool[] ok, int S, bool depuisLeBord)
        {
            var vu = new bool[S * S];
            var pile = new Stack<int>();
            void Pousser(int p) { if (ok[p] && !vu[p]) { vu[p] = true; pile.Push(p); } }
            for (int i = 0; i < S; i++) { Pousser(i); Pousser((S - 1) * S + i); Pousser(i * S); Pousser(i * S + S - 1); }
            while (pile.Count > 0)
            {
                int p = pile.Pop(); int x = p % S, y = p / S;
                if (x > 0) Pousser(p - 1); if (x < S - 1) Pousser(p + 1); if (y > 0) Pousser(p - S); if (y < S - 1) Pousser(p + S);
            }
            return vu;
        }

        private static bool[] Eroder(bool[] m, int S)
        {
            var r = new bool[m.Length];
            for (int y = 1; y < S - 1; y++) for (int x = 1; x < S - 1; x++) { int p = y * S + x; r[p] = m[p] && m[p - 1] && m[p + 1] && m[p - S] && m[p + S]; }
            return r;
        }

        private static bool[] Dilater(bool[] m, int S)
        {
            var r = new bool[m.Length];
            for (int y = 1; y < S - 1; y++) for (int x = 1; x < S - 1; x++) { int p = y * S + x; r[p] = m[p] || m[p - 1] || m[p + 1] || m[p - S] || m[p + S]; }
            return r;
        }

        private static bool[] PlusGrande(bool[] m, int S)
        {
            var etiq = new int[m.Length]; int meilleur = 0, taille = 0, courant = 0;
            var pile = new Stack<int>();
            for (int s = 0; s < m.Length; s++)
            {
                if (!m[s] || etiq[s] != 0) continue;
                courant++; int t = 0; etiq[s] = courant; pile.Push(s);
                while (pile.Count > 0)
                {
                    int p = pile.Pop(); t++; int x = p % S, y = p / S;
                    if (x > 0 && m[p - 1] && etiq[p - 1] == 0) { etiq[p - 1] = courant; pile.Push(p - 1); }
                    if (x < S - 1 && m[p + 1] && etiq[p + 1] == 0) { etiq[p + 1] = courant; pile.Push(p + 1); }
                    if (y > 0 && m[p - S] && etiq[p - S] == 0) { etiq[p - S] = courant; pile.Push(p - S); }
                    if (y < S - 1 && m[p + S] && etiq[p + S] == 0) { etiq[p + S] = courant; pile.Push(p + S); }
                }
                if (t > taille) { taille = t; meilleur = courant; }
            }
            var r = new bool[m.Length];
            for (int p = 0; p < m.Length; p++) r[p] = etiq[p] == meilleur && meilleur != 0;
            return r;
        }

        /// <summary>Distance (en pixels) de chaque pixel du masque au pixel hors masque le plus proche (exacte, Felzenszwalb).</summary>
        public static float[] Distance(bool[] m, int S)
        {
            const float INF = 1e12f;
            var f = new float[S * S];
            for (int p = 0; p < f.Length; p++) f[p] = m[p] ? INF : 0;
            var col = new float[S]; var res = new float[S]; var v = new int[S]; var z = new float[S + 1];
            for (int x = 0; x < S; x++)
            {
                for (int y = 0; y < S; y++) col[y] = f[y * S + x];
                Dt1(col, res, v, z, S);
                for (int y = 0; y < S; y++) f[y * S + x] = res[y];
            }
            for (int y = 0; y < S; y++)
            {
                Array.Copy(f, y * S, col, 0, S);
                Dt1(col, res, v, z, S);
                for (int x = 0; x < S; x++) f[y * S + x] = (float)Math.Sqrt(res[x]);
            }
            // le bord de l'image compte comme « dehors »
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                int p = y * S + x; if (!m[p]) continue;
                float b = Math.Min(Math.Min(x + 1, S - x), Math.Min(y + 1, S - y));
                if (f[p] > b) f[p] = b;
            }
            return f;
        }

        private static void Dt1(float[] f, float[] d, int[] v, float[] z, int n)
        {
            const float INF = 1e12f;
            int k = 0; v[0] = 0; z[0] = -INF; z[1] = INF;
            for (int q = 1; q < n; q++)
            {
                float s;
                while (true)
                {
                    int vk = v[k];
                    s = ((f[q] + (float)q * q) - (f[vk] + (float)vk * vk)) / (2f * q - 2f * vk);
                    if (s <= z[k] && k > 0) k--; else break;
                }
                k++; v[k] = q; z[k] = s; z[k + 1] = INF;
            }
            k = 0;
            for (int q = 0; q < n; q++)
            {
                while (z[k + 1] < q) k++;
                int vk = v[k];
                d[q] = (q - vk) * (float)(q - vk) + f[vk];
            }
        }

        /// <summary>Flou « boîte » de rayon r (trois passes valent un flou gaussien).</summary>
        public static float[] Flou(float[] a, int S, int r)
        {
            var t = new float[a.Length]; var b = new float[a.Length];
            float inv = 1f / (2 * r + 1);
            for (int y = 0; y < S; y++)
            {
                float s = 0; int o = y * S;
                for (int k = -r; k <= r; k++) s += a[o + Math.Min(S - 1, Math.Max(0, k))];
                for (int x = 0; x < S; x++)
                {
                    t[o + x] = s * inv;
                    s += a[o + Math.Min(S - 1, x + r + 1)] - a[o + Math.Max(0, x - r)];
                }
            }
            for (int x = 0; x < S; x++)
            {
                float s = 0;
                for (int k = -r; k <= r; k++) s += t[Math.Min(S - 1, Math.Max(0, k)) * S + x];
                for (int y = 0; y < S; y++)
                {
                    b[y * S + x] = s * inv;
                    s += t[Math.Min(S - 1, y + r + 1) * S + x] - t[Math.Max(0, y - r) * S + x];
                }
            }
            return b;
        }
    }
}
