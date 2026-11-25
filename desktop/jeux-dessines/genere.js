// Fabrique les dessins des jeux pour l'application :   node genere.js
//   jet.js   → ../MesPremiersJeux/Games/JetArt.cs      (Le jet d'eau)
//   peche.js → ../MesPremiersJeux/Games/PecheArt.cs    (La pêche)
//   patate.js → ../MesPremiersJeux/Games/PatateArt.cs  (Patatou)
//   corps.js → ../MesPremiersJeux/Games/CorpsArt.cs    (Dans mon corps)
//   chamboule.js → ../MesPremiersJeux/Games/ChambouleArt.cs (Chamboule-tout)
//   nombres.js → ../MesPremiersJeux/Games/NombresArt.cs  (les décors des activités de nombres)
//   maison.js → ../MesPremiersJeux/Games/MaisonArt.cs    (Range la maison)
//   valise.js → ../MesPremiersJeux/Games/ValiseArt.cs    (La valise du voyageur)
//   reperes.js → ../MesPremiersJeux/Games/ReperesArt.cs  (Sur, sous, devant, derrière)
// (les images des jeux, elles, se font avec le banc d'essai : voir banc/LISEZ-MOI.md)
const fs = require('fs'), path = require('path');
// les couleurs en #RRGGBB majuscules (la forme courte #fff est allongée)
const coul = v => v == null || v[0] !== '#' ? v : ('#' + (v.length === 4 ? [...v.slice(1)].map(c => c + c).join('') : v.slice(1))).toUpperCase();
const q = s => s == null ? 'null' : '"' + s + '"';
const n = v => String(Math.round(v * 100) / 100);
// un dégradé devient un texte : « L;x1;y1;x2;y2|position;couleur;opacité|… » ou « R;cx;cy;r|… »
const degrade = g => (g.g === 'L' ? ['L', g.x1, g.y1, g.x2, g.y2] : ['R', g.cx, g.cy, g.r]).map(v => typeof v === 'number' ? n(v) : v).join(';') + '|' + g.s.map(a => [n(a[0]), coul(a[1]), n(a[2] ?? 1)].join(';')).join('|');

for (const [source, classe, jeu] of [['jet', 'JetArt', "Le jet d'eau"], ['peche', 'PecheArt', 'La pêche'], ['patate', 'PatateArt', 'Patatou'], ['corps', 'CorpsArt', 'Dans mon corps'], ['chamboule', 'ChambouleArt', 'Chamboule-tout'], ['nombres', 'NombresArt', 'Les nombres (Apprendre)'], ['maison', 'MaisonArt', 'Range la maison'], ['valise', 'ValiseArt', 'La valise du voyageur'], ['reperes', 'ReperesArt', 'Sur, sous, devant, derrière']]) {
  if (!fs.existsSync(path.join(__dirname, source + '.js'))) continue;
  const { S, SCENES, CIELS, LISTES } = require('./' + source + '.js');
  let o = `// FICHIER FABRIQUÉ par desktop/jeux-dessines/genere.js à partir de desktop/jeux-dessines/${source}.js — ne pas le modifier à la main.
using System.Collections.Generic;

namespace MesPremiersJeux.Games
{
    /// <summary>Les dessins du jeu « ${jeu} » : des formes vectorielles (tracé, remplissage ou dégradé, trait) et la place de chaque objet dans les scènes.</summary>
    internal static class ${classe}
    {
        internal static readonly Dictionary<string, VecForme[]> Dessins = new Dictionary<string, VecForme[]>
        {
`;
  let formes = 0;
  for (const k of Object.keys(S)) {
    o += `            { "${k}", new[] {\n`;
    for (const f of S[k]) { formes++; const g = f.f && typeof f.f === 'object';
      o += `                new VecForme(${q(f.d)}, ${g ? 'null' : q(coul(f.f))}, ${g ? q(degrade(f.f)) : 'null'}, ${q(coul(f.s))}, ${n(f.w)}, ${n(f.o)}),\n`; }
    o += `            } },\n`;
  }
  o += `        };

        internal static readonly Dictionary<string, VecPose[]> Scenes = new Dictionary<string, VecPose[]>
        {
`;
  for (const k of Object.keys(SCENES)) {
    o += `            { "${k}", new[] {\n`;
    for (const p of SCENES[k]) {
      const c = p.c ? 'new[] { ' + Object.entries(p.c).map(([a, b]) => `"${a}", "${coul(b)}"`).join(', ') + ' }' : 'null';
      o += `                new VecPose("${p.n}", "${p.s}", ${n(p.x)}, ${n(p.y)}, ${n(p.e ?? 1)}, ${n(p.r ?? 0)}, ${c}),\n`;
    }
    o += `            } },\n`;
  }
  o += `        };

        /// <summary>Le ciel de chaque scène : couleur du haut, couleur du bas, couleur du sol sous l'image, et « jour » ou « soir ».</summary>
        internal static readonly Dictionary<string, string[]> Ciels = new Dictionary<string, string[]>
        {
${Object.entries(CIELS).map(([k, v]) => `            { "${k}", new[] { "${coul(v[0])}", "${coul(v[1])}", "${coul(v[2])}", "${v[3] ? 'jour' : 'soir'}" } },`).join('\n')}
        };${LISTES ? `

        /// <summary>Les listes du jeu (les familles de pièces et leurs pièces : clé, ce que la voix dit, la boîte de la vignette, des précisions).</summary>
        internal static readonly Dictionary<string, string[][]> Listes = new Dictionary<string, string[][]>
        {
${Object.entries(LISTES).map(([k, lignes]) => `            { "${k}", new[] {\n${lignes.map(l => '                new[] { ' + l.map(v => JSON.stringify(String(v))).join(', ') + ' },').join('\n')}\n            } },`).join('\n')}
        };` : ''}
    }
}
`;
  const f = path.join(__dirname, '..', 'MesPremiersJeux', 'Games', classe + '.cs');
  fs.writeFileSync(f, o); console.log('écrit', classe + '.cs', ':', Object.keys(S).length, 'dessins,', formes, 'formes');
}
