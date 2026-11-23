// Fabrique ../MesPremiersJeux/Games/JetArt.cs à partir de art.js :   node genere.js
// (les images du jeu, elles, se font avec le banc d'essai : voir banc/LISEZ-MOI)
const fs = require('fs'), path = require('path');
const { S, SCENES, CIELS } = require('./art.js');
// les couleurs en #RRGGBB majuscules (la forme courte #fff est allongée)
const coul = v => v == null || v[0] !== '#' ? v : ('#' + (v.length === 4 ? [...v.slice(1)].map(c => c + c).join('') : v.slice(1))).toUpperCase();
const q = s => s == null ? 'null' : '"' + s + '"';
const n = v => String(Math.round(v * 100) / 100);
// un dégradé devient un texte : « L;x1;y1;x2;y2|position;couleur;opacité|… » ou « R;cx;cy;r|… »
const degrade = g => (g.g === 'L' ? ['L', g.x1, g.y1, g.x2, g.y2] : ['R', g.cx, g.cy, g.r]).map(v => typeof v === 'number' ? n(v) : v).join(';') + '|' + g.s.map(a => [n(a[0]), coul(a[1]), n(a[2] ?? 1)].join(';')).join('|');

let o = `// FICHIER FABRIQUÉ par desktop/jetdeau/genere.js à partir de desktop/jetdeau/art.js — ne pas le modifier à la main.
using System.Collections.Generic;

namespace MesPremiersJeux.Games
{
    /// <summary>Les dessins du jeu « Le jet d'eau » : des formes vectorielles (tracé, remplissage ou dégradé, trait) et la place de chaque objet dans les scènes.</summary>
    internal static class JetArt
    {
        /// <summary>D : le tracé ; F : la couleur de remplissage ; G : ou un dégradé ; S : la couleur du trait ; W : son épaisseur ; O : l'opacité.</summary>
        internal struct Forme { public string D, F, G, S; public double W, O; public Forme(string d, string f, string g, string s, double w, double o) { D = d; F = f; G = g; S = s; W = w; O = o; } }
        internal struct Pose { public string Nom, Dessin; public double X, Y, E, R; public string[] C; public Pose(string nom, string dessin, double x, double y, double e, double r, string[] c) { Nom = nom; Dessin = dessin; X = x; Y = y; E = e; R = r; C = c; } }

        internal static readonly Dictionary<string, Forme[]> Dessins = new Dictionary<string, Forme[]>
        {
`;
let formes = 0;
for (const k of Object.keys(S)) {
  o += `            { "${k}", new[] {\n`;
  for (const f of S[k]) { formes++; const g = f.f && typeof f.f === 'object';
    o += `                new Forme(${q(f.d)}, ${g ? 'null' : q(coul(f.f))}, ${g ? q(degrade(f.f)) : 'null'}, ${q(coul(f.s))}, ${n(f.w)}, ${n(f.o)}),\n`; }
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

        /// <summary>Le ciel de chaque scène : couleur du haut, couleur du bas, couleur du sol sous l'image, et « jour » ou « soir ».</summary>
        internal static readonly Dictionary<string, string[]> Ciels = new Dictionary<string, string[]>
        {
${Object.entries(CIELS).map(([k, v]) => `            { "${k}", new[] { "${coul(v[0])}", "${coul(v[1])}", "${coul(v[2])}", "${v[3] ? 'jour' : 'soir'}" } },`).join('\n')}
        };
    }
}
`;
const f = path.join(__dirname, '..', 'MesPremiersJeux', 'Games', 'JetArt.cs');
fs.writeFileSync(f, o); console.log('écrit', f, ':', Object.keys(S).length, 'dessins,', formes, 'formes');
