using System;

namespace MesPremiersJeux.Views.Creer.Volume
{
    public enum Genre { Pierre, Marbre, Bois, Glace, Bronze }

    /// <summary>
    /// UN VRAI VOLUME À TAILLER. Deux cartes de hauteurs : la CIBLE (la statue finie)
    /// et le COURANT (le bloc tel qu'il est). Chaque coup de ciseau enlève de la
    /// matière par facettes planes, jamais plus bas que la cible. L'image est
    /// calculée comme un objet éclairé : normales, lumière qui vient d'en haut à
    /// gauche, reflets, creux plus sombres, ombres portées, grain de la matière
    /// (veines du marbre, fil du bois, reflets du bronze, transparence de la glace).
    /// Calcul pur, sans rien de l'interface : il se teste hors de l'application.
    /// </summary>
    public sealed class Volume3D
    {
        public readonly int W, H;
        public readonly float[] T, C;
        private readonly float[] _c0;
        /// <summary>L'image : W×H pixels B G R A prémultipliés (transparente là où il n'y a plus de matière).</summary>
        public readonly byte[] Px;
        public Genre Matiere = Genre.Pierre;
        /// <summary>Peinte : la couleur du dessin d'origine, sur le volume fini (la lumière reste).</summary>
        public bool Peint;
        public byte[] Peinture; // B G R par pixel, ou null
        /// <summary>Le bois non touché garde son écorce (le totem).</summary>
        public bool Ecorce;
        private readonly float[] _n1, _n2, _n3, _n4, _rnd;
        private float _lx, _ly, _lz, _ux, _uy, _hx, _hy, _hz;
        private double _reste, _depart;
        private const float Echelle = 215f; // hauteur 1 = 215 pixels de relief

        public Volume3D(int w, int h, int graine)
        {
            W = Math.Max(4, w); H = Math.Max(4, h);
            int n = W * H;
            T = new float[n]; C = new float[n]; _c0 = new float[n]; Px = new byte[n * 4];
            _n1 = new float[n]; _n2 = new float[n]; _n3 = new float[n]; _n4 = new float[n]; _rnd = new float[n];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int p = y * W + x;
                    _n1[p] = Bruit(x / 90f, y / 90f, graine);
                    _n2[p] = Bruit(x / 22f, y / 22f, graine + 1);
                    _n3[p] = Bruit(x / 3.5f, y / 3.5f, graine + 2);
                    _n4[p] = Bruit(x / 2f, y / 40f, graine + 3);
                    _rnd[p] = Hash(x, y, graine + 4);
                }
            Lumiere(-0.45, -0.60, 0.66);
        }

        private static float Hash(int x, int y, int s)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + s * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }

        private static float Bruit(float x, float y, int s)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = Hash(xi, yi, s), b = Hash(xi + 1, yi, s), c = Hash(xi, yi + 1, s), d = Hash(xi + 1, yi + 1, s);
            return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
        }

        /// <summary>D'où vient la lumière (x vers la droite, y vers le bas, z vers celui qui regarde).</summary>
        public void Lumiere(double lx, double ly, double lz)
        {
            double n = Math.Sqrt(lx * lx + ly * ly + lz * lz); if (n < 1e-6) return;
            _lx = (float)(lx / n); _ly = (float)(ly / n); _lz = (float)(lz / n);
            double m = Math.Sqrt(_lx * _lx + _ly * _ly);
            if (m < 1e-4) { _ux = 0; _uy = 0; } else { _ux = (float)(_lx / m); _uy = (float)(_ly / m); }
            double hx = _lx, hy = _ly, hz = _lz + 1; double hn = Math.Sqrt(hx * hx + hy * hy + hz * hz);
            _hx = (float)(hx / hn); _hy = (float)(hy / hn); _hz = (float)(hz / hn);
        }

        /// <summary>Le bloc est prêt (cible et courant posés) : on retient l'état de départ et le volume à enlever.</summary>
        public void Figer()
        {
            Array.Copy(C, _c0, C.Length);
            double s = 0;
            for (int p = 0; p < C.Length; p++) { if (C[p] < T[p]) C[p] = T[p]; s += C[p] - T[p]; }
            _depart = _reste = Math.Max(1e-6, s);
        }

        /// <summary>La part de matière en trop qui reste à enlever (1 = bloc intact, 0 = statue finie).</summary>
        public double Reste => Math.Max(0, _reste / _depart);

        /// <summary>Un bloc brut de carrière : une face presque plane, taillée de grandes facettes, des arêtes adoucies.</summary>
        public void BlocBrut(Random rng, int margeX, int margeHaut)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = Math.Min(x - margeX, W - 1 - margeX - x), dy = Math.Min(y - margeHaut, H - 1 - y);
                    if (dx <= 0 || dy <= 0) { C[y * W + x] = 0; continue; }
                    float d = Math.Min(dx, dy), b = d / 26f; if (b > 1) b = 1;
                    C[y * W + x] = 1.18f * (0.80f + 0.20f * (float)Math.Sqrt(b));
                }
            for (int k = 0; k < 26; k++)
            {
                float px = (float)(rng.NextDouble() * W), py = (float)(rng.NextDouble() * H);
                float gx = (float)(rng.NextDouble() * 0.0032 - 0.0016), gy = (float)(rng.NextDouble() * 0.0032 - 0.0016), z0 = 1.05f + (float)rng.NextDouble() * 0.13f;
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        int p = y * W + x; if (C[p] <= 0) continue;
                        float plan = z0 + gx * (x - px) + gy * (y - py); if (plan < 0.9f) plan = 0.9f;
                        if (plan < C[p]) C[p] = plan;
                    }
            }
        }

        /// <summary>
        /// Un coup de ciseau au point (px, py) : trois éclats qui se chevauchent, chacun avec sa facette
        /// plane un peu inclinée. Renvoie vrai si de la matière est partie ; le rectangle touché est rendu.
        /// yMin/yMax bornent la zone qu'on a le droit de tailler (un étage du totem).
        /// </summary>
        public bool Coup(double px, double py, double R, double profondeur, Random rng, out int rx0, out int ry0, out int rx1, out int ry1, int yMin = 0, int yMax = int.MaxValue)
        {
            rx0 = W; ry0 = H; rx1 = -1; ry1 = -1;
            int cxp = (int)Math.Max(0, Math.Min(W - 1, px)), cyp = (int)Math.Max(0, Math.Min(H - 1, py));
            float c0 = C[cyp * W + cxp];
            double enleve = 0;
            if (yMax > H - 1) yMax = H - 1; if (yMin < 0) yMin = 0;
            for (int k = 0; k < 3; k++)
            {
                double cx = px + (rng.NextDouble() * 0.76 - 0.38) * R, cy = py + (rng.NextDouble() * 0.76 - 0.38) * R, r = R * (0.45 + rng.NextDouble() * 0.17);
                float z0 = c0 - (float)(profondeur * (0.6 + rng.NextDouble() * 0.8));
                float gx = (float)(rng.NextDouble() * 0.009 - 0.0045), gy = (float)(rng.NextDouble() * 0.009 - 0.0045);
                int x0 = (int)Math.Max(0, cx - r - 8), x1 = (int)Math.Min(W - 1, cx + r + 8), y0 = (int)Math.Max(yMin, cy - r - 8), y1 = (int)Math.Min(yMax, cy + r + 8);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        int p = y * W + x; float c = C[p];
                        if (c <= T[p]) continue;
                        double dist = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        float plan = z0 + gx * (float)(x - cx) + gy * (float)(y - cy) + (dist > r ? (float)(dist - r) * 0.25f : 0f);
                        if (plan >= c) continue;
                        float nv = plan > T[p] ? plan : T[p];
                        if (T[p] < 0.02f && nv < 0.15f) nv = 0; // hors de la statue, une lamelle mince tombe d'un coup
                        if (nv >= c) continue;
                        enleve += c - nv; C[p] = nv;
                        if (x < rx0) rx0 = x; if (x > rx1) rx1 = x; if (y < ry0) ry0 = y; if (y > ry1) ry1 = y;
                    }
            }
            if (rx1 < 0) return false;
            _reste -= enleve;
            rx0 = Math.Max(0, rx0 - 32); ry0 = Math.Max(0, ry0 - 32); rx1 = Math.Min(W - 1, rx1 + 34); ry1 = Math.Min(H - 1, ry1 + 34);
            Ombrer(rx0, ry0, rx1, ry1);
            return true;
        }

        /// <summary>Le polissage final : ce qui dépasse encore de la statue fond d'un coup de « k ». Faux quand tout est fini.</summary>
        public bool Polir(float k, int yMin = 0, int yMax = int.MaxValue)
        {
            bool encore = false; double enleve = 0;
            if (yMax > H - 1) yMax = H - 1; if (yMin < 0) yMin = 0;
            for (int p = yMin * W; p < (yMax + 1) * W; p++)
            {
                float e = C[p] - T[p]; if (e <= 0) continue;
                float nv = e * k < 0.004f ? T[p] : T[p] + e * k;
                enleve += C[p] - nv; C[p] = nv;
                if (nv > T[p]) encore = true;
            }
            _reste -= enleve;
            return encore;
        }

        /// <summary>Ce qui reste à enlever entre deux lignes (un étage du totem), en part du volume de départ de cette bande.</summary>
        public double ResteBande(int yMin, int yMax)
        {
            double r = 0, d = 0;
            for (int p = Math.Max(0, yMin) * W; p < Math.Min(H, yMax + 1) * W; p++) { r += Math.Max(0, C[p] - T[p]); d += Math.Max(0, _c0[p] - T[p]); }
            return d < 1e-6 ? 0 : r / d;
        }

        /// <summary>Toute l'image, par bandes de lignes calculées en même temps sur les cœurs du processeur.</summary>
        public void ToutOmbrer()
        {
            const int Bande = 32;
            int n = (H + Bande - 1) / Bande;
            System.Threading.Tasks.Parallel.For(0, n, b => Ombrer(0, b * Bande, W - 1, Math.Min(H - 1, b * Bande + Bande - 1)));
        }

        private float Hc(int x, int y)
        {
            if (x < 0) x = 0; else if (x >= W) x = W - 1;
            if (y < 0) y = 0; else if (y >= H) y = H - 1;
            return C[y * W + x];
        }

        private static float Sat(float v) => v < 0 ? 0 : v > 1 ? 1 : v;

        /// <summary>Calcule l'image du rectangle [x0..x1] × [y0..y1].</summary>
        public void Ombrer(int x0, int y0, int x1, int y1)
        {
            if (x0 < 0) x0 = 0; if (y0 < 0) y0 = 0; if (x1 > W - 1) x1 = W - 1; if (y1 > H - 1) y1 = H - 1;
            // Les réglages de la matière.
            float ar, ag, ab, ks, sh, wrap, amb, gain = 1.28f, alphaMat = 1f;
            switch (Matiere)
            {
                case Genre.Marbre: ar = 0.97f; ag = 0.96f; ab = 0.94f; ks = 0.45f; sh = 48; wrap = 0.45f; amb = 0.40f; break;
                case Genre.Bois: ar = 0.78f; ag = 0.52f; ab = 0.28f; ks = 0.16f; sh = 22; wrap = 0.20f; amb = 0.34f; break;
                case Genre.Glace: ar = 0.74f; ag = 0.88f; ab = 0.97f; ks = 0.95f; sh = 90; wrap = 0.55f; amb = 0.42f; alphaMat = 0.86f; break;
                case Genre.Bronze: ar = 0.80f; ag = 0.56f; ab = 0.22f; ks = 0.90f; sh = 60; wrap = 0.05f; amb = 0.16f; break;
                default: ar = 0.80f; ag = 0.79f; ab = 0.77f; ks = 0.08f; sh = 14; wrap = 0.15f; amb = 0.34f; break;
            }
            int ox = (int)Math.Round(_ux * 23), oy = (int)Math.Round(_uy * 23); // vers la lumière
            float pente = 0.005f * (_lz / Math.Max(0.2f, (float)Math.Sqrt(_lx * _lx + _ly * _ly))) / 0.88f;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    int p = y * W + x, i = p * 4;
                    float c = C[p];
                    float a = Sat(c * 40f);
                    // L'ombre de la statue sur le mur, là où il n'y a pas (ou plus) de matière.
                    float ombreMur = 0;
                    if (a < 1f)
                    {
                        int sx = x + ox, sy = y + oy;
                        float o = Sat(Hc(sx, sy) * 40f) + Sat(Hc(sx - 9, sy) * 40f) + Sat(Hc(sx + 9, sy) * 40f) + Sat(Hc(sx, sy - 9) * 40f) + Sat(Hc(sx, sy + 9) * 40f);
                        ombreMur = 0.38f * o / 5f;
                    }
                    if (a <= 0f)
                    {
                        Px[i] = Px[i + 1] = Px[i + 2] = 0; Px[i + 3] = (byte)(ombreMur * 255f);
                        continue;
                    }
                    float brut = Sat((c - T[p]) * 30f); // 1 là où la matière est encore brute
                    bool ecorce = Ecorce && Matiere == Genre.Bois && _c0[p] - c < 0.012f && brut > 0.2f;
                    bool peint = Peint && Peinture != null && brut < 0.5f && (Peinture[p * 3] | Peinture[p * 3 + 1] | Peinture[p * 3 + 2]) != 0;

                    // Le grain : une petite rugosité ajoutée à la hauteur (forte sur le brut, fine sur le poli).
                    float[] grainN = Matiere == Genre.Bois ? _n4 : _n3;
                    float amp;
                    if (ecorce) amp = 5.5f;
                    else if (peint) amp = 0.25f;
                    else switch (Matiere)
                    {
                        case Genre.Marbre: amp = 0.25f + 2.4f * brut; break;
                        case Genre.Bois: amp = 1.1f + 1.5f * brut; break;
                        case Genre.Glace: amp = 0.2f + 2.0f * brut; break;
                        case Genre.Bronze: amp = 0.35f + 2.0f * brut; break;
                        default: amp = 1.6f + 2.2f * brut; break;
                    }
                    int xm = x > 0 ? x - 1 : 0, xp = x < W - 1 ? x + 1 : W - 1, ym = y > 0 ? y - 1 : 0, yp = y < H - 1 ? y + 1 : H - 1;
                    float gx = ((C[y * W + xp] - C[y * W + xm]) * Echelle + (grainN[y * W + xp] - grainN[y * W + xm]) * amp) * 0.5f;
                    float gy = ((C[yp * W + x] - C[ym * W + x]) * Echelle + (grainN[yp * W + x] - grainN[ym * W + x]) * amp) * 0.5f;
                    float inv = 1f / (float)Math.Sqrt(gx * gx + gy * gy + 1f);
                    float nx = -gx * inv, ny = -gy * inv, nz = inv;

                    // La couleur de la matière en ce point.
                    float r, g, b;
                    float pks = ks, psh = sh, pwrap = wrap, pamb = amb, pgain = gain;
                    if (peint)
                    {
                        b = Peinture[p * 3] / 255f; g = Peinture[p * 3 + 1] / 255f; r = Peinture[p * 3 + 2] / 255f;
                        pks = 0.30f; psh = 40; pwrap = 0.25f; pamb = 0.42f; pgain = 1.12f;
                    }
                    else if (ecorce)
                    {
                        float t = 0.70f + 0.55f * _n4[p] + 0.25f * (_n3[p] - 0.5f);
                        r = 0.34f * t; g = 0.22f * t; b = 0.13f * t; pks = 0.04f;
                    }
                    else switch (Matiere)
                    {
                        case Genre.Marbre:
                        {
                            float turb = _n1[p] * 4f + _n2[p] * 1.2f;
                            float v = 1f - Math.Abs((float)Math.Sin(x * 0.014f + y * 0.007f + turb));
                            float v2 = v * v; v = v2 * v2 * v2 * v; // puissance 7 : des veines fines
                            float t = 1f - 0.42f * v;
                            r = ar * t; g = ag * t; b = ab * t + 0.03f * v;
                            break;
                        }
                        case Genre.Bois:
                        {
                            float warp = _n1[p] * 6f + _n2[p] * 1.6f;
                            float fil = (float)Math.Sin(x * 0.13f + warp + c * 5f);
                            float cerne = Sat((float)Math.Sin(x * 0.035f + warp * 0.6f) * 4f - 3f);
                            float t = 0.90f + 0.13f * fil - 0.22f * cerne;
                            r = ar * t; g = ag * t; b = ab * t;
                            break;
                        }
                        case Genre.Glace: { float t = 0.92f + 0.12f * _n2[p]; r = ar * t; g = ag * t; b = ab * t; break; }
                        case Genre.Bronze: { float t = 0.92f + 0.14f * _n1[p]; r = ar * t; g = ag * t; b = ab * t; break; }
                        default:
                        {
                            float t = 0.80f + 0.40f * (0.5f * _n1[p] + 0.3f * _n3[p] + 0.2f * _rnd[p] - 0.5f);
                            r = ar * t; g = ag * t; b = ab * t;
                            break;
                        }
                    }

                    // La lumière : diffuse (adoucie), reflet, creux assombris, ombre portée par ce qui est plus haut vers la lumière.
                    float ndl = nx * _lx + ny * _ly + nz * _lz;
                    float diff = Sat((ndl + pwrap) / (1f + pwrap));
                    float ndh = nx * _hx + ny * _hy + nz * _hz;
                    float spec = ndh > 0 ? (float)Math.Pow(ndh, psh) * pks : 0f;
                    float moy = (Hc(x - 10, y) + Hc(x + 10, y) + Hc(x, y - 10) + Hc(x, y + 10) + Hc(x - 7, y - 7) + Hc(x + 7, y - 7) + Hc(x - 7, y + 7) + Hc(x + 7, y + 7)) * 0.125f;
                    float ao = 1f - 1.6f * Math.Max(0f, moy - c); if (ao < 0.45f) ao = 0.45f;
                    float ombre = 1f;
                    for (int k = 6; k <= 20; k += 7)
                    {
                        float occ = Hc(x + (int)(_ux * k), y + (int)(_uy * k)) - c - k * pente;
                        if (occ > 0) { float f = 1f - 3f * occ; ombre *= f < 0.55f ? 0.55f : f; }
                    }
                    float lum = (pamb + (1f - pamb) * diff * ombre) * ao;
                    r = (r * lum + spec) * pgain; g = (g * lum + spec) * pgain; b = (b * lum + spec) * pgain;

                    if (!peint && !ecorce)
                    {
                        if (Matiere == Genre.Bronze)
                        {
                            // Le métal reflète un ciel : clair en haut, sombre à l'horizon, un éclat en haut à gauche.
                            float env = ny < 0 ? 0.55f + 0.75f * Sat(-ny * 1.6f) : 0.50f - 0.38f * Sat(ny * 2.2f);
                            float ex = nx + 0.35f, ey = ny + 0.45f;
                            env += 0.25f * (float)Math.Exp(-(ex * ex + ey * ey) / 0.02f);
                            float t = 0.92f + 0.14f * _n1[p];
                            r = 0.45f * r + 0.55f * (ar * t * env * 1.15f); g = 0.45f * g + 0.55f * (ag * t * env * 1.15f); b = 0.45f * b + 0.55f * (ab * t * env * 1.15f);
                        }
                        else if (Matiere == Genre.Glace)
                        {
                            float bord = (float)Math.Pow(1f - nz, 2.2) * 0.55f; // la glace blanchit sur ses bords
                            r += bord; g += bord; b += bord;
                        }
                    }
                    float af = a * (peint ? 1f : alphaMat);
                    float tot = af + ombreMur * (1f - af);
                    Px[i] = (byte)(Sat(b) * af * 255f); Px[i + 1] = (byte)(Sat(g) * af * 255f); Px[i + 2] = (byte)(Sat(r) * af * 255f); Px[i + 3] = (byte)(Sat(tot) * 255f);
                }
        }
    }

    /// <summary>
    /// LE TOTEM : un tronc (un cylindre couché dans la carte de hauteurs, avec son écorce) et
    /// trois étages. Quand un animal est choisi pour un étage, sa forme devient la cible de
    /// cette bande : un bas-relief qui ressort du tronc, le fond creusé autour.
    /// </summary>
    public static class Totem
    {
        public const int TW = 340, EH = 230, Etages = 3, FS = 220;

        public static Volume3D Tronc(int graine)
        {
            var v = new Volume3D(TW, EH * Etages, graine) { Matiere = Genre.Bois, Ecorce = true, Peinture = new byte[TW * EH * Etages * 3] };
            float c = (TW - 1) / 2f, r = TW / 2f;
            for (int y = 0; y < v.H; y++)
                for (int x = 0; x < TW; x++)
                {
                    float u = (x - c) / r; int p = y * TW + x;
                    float cyl = u * u >= 1 ? 0 : 0.10f + 0.90f * (float)Math.Sqrt(1 - u * u);
                    v.C[p] = cyl;
                    v.T[p] = cyl > 0.03f ? cyl - 0.03f : 0; // sous l'écorce, tant qu'aucun animal n'est choisi
                }
            v.Figer();
            return v;
        }

        /// <summary>La figure (un carré de FS pixels) devient la forme à tailler dans l'étage « i » (0 = en haut).</summary>
        public static void Etage(Volume3D v, int i, Relief.Figure fig)
        {
            int y0 = i * EH, fx0 = (TW - FS) / 2, fy0 = (EH - FS) / 2;
            for (int y = y0; y < y0 + EH; y++)
                for (int x = 0; x < TW; x++)
                {
                    int p = y * TW + x;
                    float cyl = v.C[p];
                    if (cyl <= 0) { v.T[p] = 0; continue; }
                    float fh = 0; int fx = x - fx0, fy = y - y0 - fy0;
                    v.Peinture[p * 3] = v.Peinture[p * 3 + 1] = v.Peinture[p * 3 + 2] = 0;
                    if (fig != null && fx >= 0 && fy >= 0 && fx < FS && fy < FS)
                    {
                        int q = fy * FS + fx;
                        fh = fig.Hauteur[q];
                        if (fh > 0.02f) { v.Peinture[p * 3] = fig.Couleur[q * 3]; v.Peinture[p * 3 + 1] = fig.Couleur[q * 3 + 1]; v.Peinture[p * 3 + 2] = fig.Couleur[q * 3 + 2]; }
                    }
                    float t = cyl * 0.55f + fh * 0.46f;
                    v.T[p] = Math.Max(0, Math.Min(cyl - 0.03f, t));
                }
            v.Figer();
        }
    }
}
