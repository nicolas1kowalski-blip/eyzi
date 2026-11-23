// Les outils pour décrire les dessins des jeux « dessinés » (Le jet d'eau, La pêche…).
// Un dessin = une liste de formes { d (tracé SVG), f (remplissage : couleur, « $nom » ou dégradé), s (trait), w, o (opacité) }.
// Une couleur « $nom » est choisie par le jeu au moment de dessiner (ex. la carrosserie).
const N = '#2A1C4E';
const P = (d, f, w = 7, s = N, o = 1) => ({ d, f, s, w, o });      // forme cernée
const A = (d, f, o = 1) => ({ d, f, s: null, w: 0, o });            // aplat sans trait
const T = (d, s, w, o = 1) => ({ d, f: null, s, w, o });            // trait
const TT = (d, c, w) => [T(d, N, w + 8), T(d, c, w)];               // trait cerné
// dégradés (coordonnées dans le repère du dessin) ; un arrêt = [position, couleur, opacité]
const GL = (x1, y1, x2, y2, ...s) => ({ g: 'L', x1, y1, x2, y2, s });
const GR = (cx, cy, r, ...s) => ({ g: 'R', cx, cy, r, s });
const GV = (y1, y2, c1, c2) => GL(0, y1, 0, y2, [0, c1], [1, c2]);   // de haut en bas
const GH = (x1, x2, c1, c2) => GL(x1, 0, x2, 0, [0, c1], [1, c2]);   // de gauche à droite
const r1 = v => Math.round(v * 10) / 10;
const ci = (cx, cy, r) => `M${r1(cx - r)},${r1(cy)} a${r},${r} 0 1 0 ${2 * r},0 a${r},${r} 0 1 0 ${-2 * r},0 Z`;
const el = (cx, cy, rx, ry) => `M${r1(cx - rx)},${r1(cy)} a${rx},${ry} 0 1 0 ${2 * rx},0 a${rx},${ry} 0 1 0 ${-2 * rx},0 Z`;
const rr = (x, y, w, h, r) => `M${x + r},${y} h${w - 2 * r} a${r},${r} 0 0 1 ${r},${r} v${h - 2 * r} a${r},${r} 0 0 1 ${-r},${r} h${-(w - 2 * r)} a${r},${r} 0 0 1 ${-r},${-r} v${-(h - 2 * r)} a${r},${r} 0 0 1 ${r},${-r} Z`;
const pol = (a, r) => [r1(Math.cos(a * Math.PI / 180) * r), r1(Math.sin(a * Math.PI / 180) * r)];
// décale toutes les formes d'un dessin (les tracés n'utilisent que des majuscules absolues en tête : M x,y puis du relatif)
const alea = i => { const x = Math.sin(i * 127.1 + 311.7) * 43758.5453; return x - Math.floor(x); };

module.exports = { N, P, A, T, TT, GL, GR, GV, GH, r1, ci, el, rr, pol, alea };
