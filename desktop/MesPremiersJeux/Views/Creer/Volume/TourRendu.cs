using System;

namespace MesPremiersJeux.Views.Creer.Volume
{
    /// <summary>
    /// LE TOUR DE POTIER, EN VOLUME : la pièce est un solide de révolution (un rayon
    /// par tranche de hauteur). Chaque image est calculée point par point : la
    /// rondeur, la lumière, le reflet, les fines rides laissées par les doigts, la
    /// terre humide qui défile en tournant, l'ouverture sombre du col ; après la
    /// cuisson, l'émail brillant qui reflète une fenêtre et le pied resté en terre.
    /// Calcul pur, sans rien de l'interface.
    /// </summary>
    public sealed class TourRendu
    {
        public const int Tranches = 44;
        public readonly int W, H;
        public readonly byte[] Px;          // B G R A opaques
        public readonly float[] R = new float[Tranches];
        public readonly int Haut, Bas; public readonly float Axe;
        public bool Cuit;                    // émaillée et cuite : brillante, ne tourne plus
        public float Gr = 0.16f, Gg = 0.42f, Gb = 0.72f; // la couleur de l'émail
        public double Phase;                 // l'angle du tour
        public float Chauffe;                // 0..1 : la pièce rougeoie dans le four
        private readonly byte[] _fond;

        public TourRendu(int w, int h)
        {
            W = w; H = h; Px = new byte[w * h * 4]; _fond = new byte[w * h * 4];
            Haut = (int)(h * 0.125); Bas = (int)(h * 0.84); Axe = w / 2f;
            // Le fond : le mur de l'atelier et le plateau du tour, un disque de métal vu de trois quarts.
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float t = y / (float)h;
                    float r = 0.97f * (1 - t) + 0.84f * t, g = 0.94f * (1 - t) + 0.76f * t, b = 0.89f * (1 - t) + 0.66f * t;
                    float ex = (x - Axe) / (w * 0.42f), ey = (y - (Bas + 6)) / (h * 0.082f), ey2 = (y - (Bas + 26)) / (h * 0.082f);
                    float d = ex * ex + ey * ey;
                    if (d < 1)
                    {
                        float s = 0.62f + 0.22f * (1 - d) + 0.06f * (float)Math.Cos(Math.Atan2(ey, ex) * 2);
                        r = 0.62f * s / 0.8f; g = 0.63f * s / 0.8f; b = 0.66f * s / 0.8f;
                    }
                    else if (ex * ex + ey2 * ey2 < 1 && y > Bas + 6) { r = 0.36f; g = 0.37f; b = 0.40f; }
                    int i = (y * w + x) * 4;
                    _fond[i] = Oct(b); _fond[i + 1] = Oct(g); _fond[i + 2] = Oct(r); _fond[i + 3] = 255;
                }
            Motte();
        }

        private static byte Oct(float v) => (byte)(v <= 0 ? 0 : v >= 1 ? 255 : v * 255f);
        private static float Sat(float v) => v < 0 ? 0 : v > 1 ? 1 : v;

        /// <summary>Une motte de terre toute neuve, un peu ventrue.</summary>
        public void Motte()
        {
            for (int i = 0; i < Tranches; i++) R[i] = W * (0.16f + 0.075f * (float)Math.Sin(i / (double)(Tranches - 1) * Math.PI));
            Cuit = false; Chauffe = 0;
        }

        /// <summary>La main appuie au point (x, y) : la terre s'y creuse ou s'y élargit, et un peu autour. Vrai si la forme a bougé.</summary>
        public bool Faconner(double x, double y)
        {
            if (Cuit || y < Haut - 30 || y > Bas + 10) return false;
            float cible = (float)Math.Max(W * 0.05, Math.Min(W * 0.40, Math.Abs(x - Axe)));
            double yi = (y - Haut) / (Bas - Haut) * (Tranches - 1);
            for (int i = 0; i < Tranches; i++)
            {
                double d = Math.Abs(i - yi);
                float k = (float)(Math.Exp(-d * d / 18.0) * 0.14);
                R[i] += (cible - R[i]) * k;
            }
            return true;
        }

        public void Rendre()
        {
            Array.Copy(_fond, Px, Px.Length);
            float rb = R[Tranches - 1], rt = R[0];
            // L'ombre de la pièce sur le plateau.
            for (int y = Bas - 4; y < Math.Min(H, Bas + (int)(rb * 0.3f) + 14); y++)
                for (int x = (int)Math.Max(0, Axe - rb * 1.4f); x < Math.Min(W, Axe + rb * 1.5f + 20); x++)
                {
                    float ex = (x - (Axe + 12)) / (rb * 1.18f), ey = (y - (Bas + 9)) / (rb * 0.24f + 4);
                    float d = ex * ex + ey * ey; if (d >= 1) continue;
                    float k = 1f - 0.42f * (1 - d);
                    int i = (y * W + x) * 4;
                    Px[i] = (byte)(Px[i] * k); Px[i + 1] = (byte)(Px[i + 1] * k); Px[i + 2] = (byte)(Px[i + 2] * k);
                }
            // La lumière.
            float lx = -0.45f, ly = -0.55f, lz = 0.70f; { float n = (float)Math.Sqrt(lx * lx + ly * ly + lz * lz); lx /= n; ly /= n; lz /= n; }
            float hx = lx, hy = ly, hz = lz + 1; { float n = (float)Math.Sqrt(hx * hx + hy * hy + hz * hz); hx /= n; hy /= n; hz /= n; }
            float pas = (Bas - Haut) / (float)(Tranches - 1);
            float rmax = 0; for (int i = 0; i < Tranches; i++) if (R[i] > rmax) rmax = R[i];
            int xa = (int)Math.Max(0, Axe - rmax - 2), xb = (int)Math.Min(W - 1, Axe + rmax + 2);
            int yFin = Math.Min(H - 1, Bas + (int)(rb * 0.17f) + 1);
            float pied = (Bas - Haut) * 0.065f;
            System.Threading.Tasks.Parallel.For(Haut, yFin + 1, y =>
            {
                float f = (y - Haut) / pas; if (f > Tranches - 1) f = Tranches - 1;
                int i0 = (int)f, i1 = Math.Min(Tranches - 1, i0 + 1); float w = f - i0;
                float rad = R[i0] * (1 - w) + R[i1] * w;
                int ia = Math.Max(0, i0 - 1), ib = Math.Min(Tranches - 1, i0 + 2);
                float pente = (R[ib] - R[ia]) / ((ib - ia) * pas);
                // Les rides de tournage : de fines stries horizontales, marquées sur la terre humide.
                float rides = (0.81f * (float)Math.Cos(y * 0.9f) + 0.185f * (float)Math.Cos(y * 0.37f + 1.3f)) * (Cuit ? 0.06f : 0.22f);
                for (int x = xa; x <= xb; x++)
                {
                    float u = (x - Axe) / rad;
                    float au = u < 0 ? -u : u;
                    if (au >= 1) continue;
                    if (y > Bas) { float bord = Bas + rb * 0.16f * (float)Math.Sqrt(1 - u * u); if (y > bord) continue; } // le dessous arrondi
                    float nz = (float)Math.Sqrt(1 - u * u);
                    float nx = u, ny = -pente - rides;
                    float inv = 1f / (float)Math.Sqrt(nx * nx + ny * ny + nz * nz); nx *= inv; ny *= inv; float nzz = nz * inv;
                    float ndl = Sat((nx * lx + ny * ly + nzz * lz + 0.15f) / 1.15f);
                    float ndh = nx * hx + ny * hy + nzz * hz;
                    float ang = (float)Math.Asin(u) + (float)Phase;
                    float tex = 0.5f + 0.06f * (float)Math.Sin(ang * 7 + Math.Sin(y * 0.05) * 2) + 0.06f * (float)Math.Sin(ang * 23 + y * 0.2f);
                    float r, g, b, spec;
                    if (!Cuit)
                    {
                        float t = 0.92f + 0.25f * (tex - 0.5f) * 2f;
                        r = 0.62f * t; g = 0.40f * t; b = 0.28f * t;
                        spec = ndh > 0 ? (float)Math.Pow(ndh, 35) * 0.55f : 0;
                    }
                    else
                    {
                        float t = 0.90f + 0.20f * (tex - 0.5f) * 2f;
                        if (y > Bas - pied) { r = 0.72f; g = 0.48f; b = 0.34f; spec = ndh > 0 ? (float)Math.Pow(ndh, 10) * 0.08f : 0; }
                        else
                        {
                            r = Gr * t; g = Gg * t; b = Gb * t;
                            spec = ndh > 0 ? (float)Math.Pow(ndh, 70) * 0.95f : 0;
                            float fen = (u + 0.42f) / 0.10f; // l'émail reflète une fenêtre : une bande claire
                            spec += 0.35f * (float)Math.Exp(-fen * fen) * Sat(1 - Math.Abs(pente) * 0.8f);
                        }
                    }
                    float lum = 0.30f + 0.85f * ndl;
                    r = r * lum + spec; g = g * lum + spec; b = b * lum + spec;
                    if (Chauffe > 0) { float c = Chauffe * 0.75f; r = r * (1 - c) + 1.0f * c; g = g * (1 - c) + 0.42f * c; b = b * (1 - c) + 0.10f * c; }
                    float couv = Sat((1 - au) * rad); // le bord fondu
                    int i = (y * W + x) * 4;
                    Px[i] = (byte)(Px[i] * (1 - couv) + Sat(b) * 255f * couv); Px[i + 1] = (byte)(Px[i + 1] * (1 - couv) + Sat(g) * 255f * couv); Px[i + 2] = (byte)(Px[i + 2] * (1 - couv) + Sat(r) * 255f * couv);
                }
            });
            // Le col : la lèvre, et l'intérieur sombre de la pièce.
            float ly0 = rt * 0.30f, my0 = rt * 0.26f;
            for (int y = (int)Math.Max(0, Haut - ly0 - 1); y <= Math.Min(H - 1, Haut + ly0 + 1); y++)
                for (int x = (int)Math.Max(0, Axe - rt - 1); x <= Math.Min(W - 1, Axe + rt + 1); x++)
                {
                    float ox = (x - Axe) / rt, oy = (y - Haut) / ly0;
                    float mx = (x - Axe) / (rt * 0.955f), my = (y - Haut) / my0;
                    bool levre = ox * ox + oy * oy < 1, bouche = mx * mx + my * my < 1;
                    if (!levre) continue;
                    if (!bouche && y >= Haut) continue; // devant, la lèvre se confond avec la panse
                    float r, g, b;
                    if (bouche)
                    {
                        float k = 0.6f + 0.5f * Sat(my * 0.5f + 0.5f);
                        if (Cuit) { r = Gr * 0.38f * k; g = Gg * 0.38f * k; b = Gb * 0.38f * k; } else { r = 0.22f * k; g = 0.13f * k; b = 0.09f * k; }
                    }
                    else if (Cuit) { r = Gr * 1.05f + 0.25f; g = Gg * 1.05f + 0.25f; b = Gb * 1.05f + 0.25f; }
                    else { r = 0.62f * 1.05f + 0.15f; g = 0.40f * 1.05f + 0.15f; b = 0.28f * 1.05f + 0.15f; }
                    if (Chauffe > 0) { float c = Chauffe * 0.75f; r = r * (1 - c) + 1.0f * c; g = g * (1 - c) + 0.42f * c; b = b * (1 - c) + 0.10f * c; }
                    int i = (y * W + x) * 4;
                    Px[i] = Oct(b); Px[i + 1] = Oct(g); Px[i + 2] = Oct(r);
                }
        }
    }
}
