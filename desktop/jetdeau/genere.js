// Fabrique, à partir de art.js :
//   node genere.js            → ../MesPremiersJeux/Games/JetArt.cs (les dessins pour l'application)
//   node genere.js --apercu   → ./apercu/<scène>.png (les décors, pour relire sans Windows ; il faut Playwright)
const fs = require('fs'), path = require('path');
const { S, SCENES, CIELS } = require('./art.js');
// les couleurs en #RRGGBB majuscules (la forme courte #fff est allongée)
const coul = v => v == null || v[0] !== '#' ? v : ('#' + (v.length === 4 ? [...v.slice(1)].map(c => c + c).join('') : v.slice(1))).toUpperCase();
const ICI = __dirname;

// ───────────── Le code C# ─────────────
function cs() {
  const q = s => s == null ? 'null' : '"' + s + '"';
  const n = v => String(Math.round(v * 100) / 100);
  let o = `// FICHIER FABRIQUÉ par desktop/jetdeau/genere.js à partir de desktop/jetdeau/art.js — ne pas le modifier à la main.
using System.Collections.Generic;

namespace MesPremiersJeux.Games
{
    /// <summary>Les dessins du jeu « Le jet d'eau » : des formes vectorielles (tracé, remplissage, trait) et la place de chaque objet dans les scènes.</summary>
    internal static class JetArt
    {
        internal struct Forme { public string D, F, S; public double W, O; public Forme(string d, string f, string s, double w, double o) { D = d; F = f; S = s; W = w; O = o; } }
        internal struct Pose { public string Nom, Dessin; public double X, Y, E, R; public string[] C; public Pose(string nom, string dessin, double x, double y, double e, double r, string[] c) { Nom = nom; Dessin = dessin; X = x; Y = y; E = e; R = r; C = c; } }

        internal static readonly Dictionary<string, Forme[]> Dessins = new Dictionary<string, Forme[]>
        {
`;
  for (const k of Object.keys(S)) {
    o += `            { "${k}", new[] {\n`;
    for (const f of S[k]) o += `                new Forme(${q(f.d)}, ${q(coul(f.f))}, ${q(coul(f.s))}, ${n(f.w)}, ${n(f.o)}),\n`;
    o += `            } },\n`;
  }
  o += `        };

        internal static readonly Dictionary<string, Pose[]> Scenes = new Dictionary<string, Pose[]>
        {
`;
  for (const k of Object.keys(SCENES)) {
    o += `            { "${k}", new[] {\n`;
    for (const p of SCENES[k]) {
      const c = p.c ? 'new[] { ' + Object.entries(p.c).map(([a, b]) => `"${a}", "${coul(b)}"`).join(', ') + ' }' : 'null';
      o += `                new Pose("${p.n}", "${p.s}", ${n(p.x)}, ${n(p.y)}, ${n(p.e ?? 1)}, ${n(p.r ?? 0)}, ${c}),\n`;
    }
    o += `            } },\n`;
  }
  o += `        };

        /// <summary>Le ciel de chaque scène : couleur du haut, couleur du bas, et s'il fait jour (soleil et nuages).</summary>
        internal static readonly Dictionary<string, string[]> Ciels = new Dictionary<string, string[]>
        {
${Object.entries(CIELS).map(([k, v]) => `            { "${k}", new[] { "${coul(v[0])}", "${coul(v[1])}", "${v[2] ? 'jour' : 'soir'}" } },`).join('\n')}
        };
    }
}
`;
  return o;
}

// ───────────── L'aperçu ─────────────
const svgDessin = (nom, c = {}) => S[nom].map(f => {
  const col = v => v == null ? 'none' : v[0] === '$' ? (c[v.slice(1)] || '#FF00FF') : v;
  return `<path d="${f.d}" fill="${col(f.f)}" stroke="${col(f.s)}" stroke-width="${f.w}" opacity="${f.o}" stroke-linejoin="round" stroke-linecap="round"/>`;
}).join('');
const pose = p => `<g transform="translate(${p.x},${p.y}) rotate(${p.r || 0}) scale(${p.e ?? 1})">${svgDessin(p.s, p.c)}</g>`;
// ce que le jeu ajoute lui-même (ici seulement pour juger de l'ensemble)
const EN_PLUS = {
  voiture: [],
  feu: [],
  remplir: [{ s: 'poisson', x: 330, y: 560, c: { poisson: '#FF8A3D', poisson2: '#F0683E' } }, { s: 'poisson', x: 760, y: 610, e: .7, c: { poisson: '#5CC8FF', poisson2: '#3E9BE0' } }, { s: 'canard', x: 1240, y: 640, e: 1.2 }, { s: 'bulle', x: 300, y: 500 }],
  jardin: [0, 1, 2, 3, 4].flatMap(i => { const x = [250, 525, 800, 1075, 1350][i], y = 616, c = { petale: ['#FF8FC1', '#B58BFF', '#FF4F7B', '#5CC8FF', '#FFB347'][i] };
    return [{ s: 'tige', x, y, e: 1 }, { s: 'feuille', x, y: y - 60 }, { s: 'feuille', x, y: y - 96, e: -1, r: 0 }, i === 1 ? { s: 'bouton', x, y: y - 176, c } : { s: 'fleur', x, y: y - 190, c }]; })
    .concat([{ s: 'arcenciel', x: 800, y: 700 }, { s: 'papillon', x: 400, y: 300, c: { aile: '#FF8FC1' } }, { s: 'papillon', x: 1150, y: 260, e: .8, c: { aile: '#5CC8FF' } }]),
  roue: [0, 1, 2, 3, 4, 5, 6, 7].flatMap(i => { const t = (i + .5) / 8, x = (1 - t) * (1 - t) * 90 + 2 * t * (1 - t) * 800 + t * t * 1510, y = (1 - t) * (1 - t) * 96 + 2 * t * (1 - t) * 250 + t * t * 96;
    return [i < 4 ? { s: 'halo', x, y: y + 26 } : null, { s: 'ampoule', x, y: y + 26, c: { verre: i < 4 ? ['#FFE066', '#FF8FC1', '#6EDC8C', '#5CC8FF'][i] : '#E3E8F4' } }].filter(Boolean); })
    .concat([{ s: 'cheval', x: 1060, y: 560, c: { robe: '#fff' } }, { s: 'cheval', x: 1300, y: 590, c: { robe: '#FFD0E0' } }]),
};
const AVANT = [{ s: 'tuyau', x: 808, y: 820 }, { s: 'pompier', x: 712, y: 968, e: .62 }, { s: 'pupille', x: 712 + 16 * .62 + 7, y: 968 - 258 * .62 - 7, e: .62 }, { s: 'lance', x: 808, y: 820, r: 28 }];
const CIEL = [{ s: 'rayons', x: 1420, y: 130 }, { s: 'soleil', x: 1420, y: 130 }, { s: 'nuage', x: 260, y: 150 }, { s: 'nuage', x: 900, y: 110, e: .8 }];
function page(nom) {
  const ps = [...(CIELS[nom][2] ? CIEL : []), ...SCENES[nom], ...(EN_PLUS[nom] || []), ...AVANT];
  return `<!doctype html><meta charset="utf-8"><body style="margin:0"><svg width="1600" height="900" viewBox="0 0 1600 900" style="background:linear-gradient(${CIELS[nom][0]},${CIELS[nom][1]} 78%)">${ps.map(pose).join('')}
  <path d="M878,688 Q1000,380 1010,560" fill="none" stroke="#3E9BE0" stroke-width="30" stroke-linecap="round"/><path d="M878,688 Q1000,380 1010,560" fill="none" stroke="#6FC6FF" stroke-width="22" stroke-linecap="round"/><path d="M878,688 Q1000,380 1010,560" fill="none" stroke="#fff" stroke-width="7" stroke-linecap="round" stroke-dasharray="14 22" opacity=".8"/></svg>`;
}

(async () => {
  if (!process.argv.includes('--apercu')) {
    const f = path.join(ICI, '..', 'MesPremiersJeux', 'Games', 'JetArt.cs');
    fs.writeFileSync(f, cs()); console.log('écrit', f, Object.keys(S).length, 'dessins'); return;
  }
  const { chromium } = require('playwright');
  const tmp = process.env.HUMEURS_TMP || require('os').tmpdir(); fs.mkdirSync(path.join(ICI, 'apercu'), { recursive: true });
  const b = await chromium.launch(), p = await b.newPage({ viewport: { width: 1600, height: 900 } });
  for (const nom of Object.keys(SCENES)) { const f = path.join(tmp, 'jet-' + nom + '.html'); fs.writeFileSync(f, page(nom)); await p.goto('file://' + f); await p.screenshot({ path: path.join(ICI, 'apercu', nom + '.png') }); console.log('aperçu', nom); }
  await b.close();
})();
