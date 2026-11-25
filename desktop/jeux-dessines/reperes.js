// Les dessins de l'activité « Sur, sous, devant, derrière » (Apprendre). genere.js en tire Games/ReperesArt.cs (pour
// l'application) ; le banc d'essai s'en sert pour produire de vraies images sans Windows. Les outils sont dans outils.js.
//
// Une chambre et son lit, vu de côté. Une peluche va SUR le lit, SOUS le lit, DEVANT le lit ou DERRIÈRE le lit. Le lit
// est en deux dessins (le fond, puis le devant) pour que la peluche puisse passer derrière lui, ou entre ses pieds.
// En bas, quatre cartes montrent les quatre places en petit. La scène fait 1600 × 900.
const { N, P, A, T, TT, GL, GR, GV, GH, r1, ci, el, rr, pol, alea } = require('./outils.js');
const communs = require('./communs.js');

const S = {};
communs(S);
require('./peluches.js')(S);

const BOIS = (y0, y1, clair = '#E3B27A', fonce = '#B5773A') => GV(y0, y1, clair, fonce);

// ═════════════ La chambre ═════════════
const pois = []; for (let i = 0; i < 40; i++) pois.push(A(ci(r1(40 + alea(i * 3 + 1) * 1520), r1(30 + alea(i * 5 + 2) * 440), r1(7 + alea(i + 3) * 6)), '#FFFFFF', .5));
S.c_chambre = [
  A('M0,0 H1600 V520 H0 Z', GV(0, 520, '#FFE9D0', '#FFD2B0')), ...pois,
  // le sol de bois
  A('M0,520 H1600 V640 H0 Z', BOIS(520, 640, '#D9A066', '#B5773A')), T('M0,520 H1600', '#7E4E22', 6, .7), A('M0,506 H1600 V522 H0 Z', '#FFFFFF', .7),
  T('M180,522 l-40,118 M480,522 l-24,118 M800,522 v118 M1120,522 l24,118 M1420,522 l40,118', '#8A5A32', 4, .45),
  // la fenêtre
  P(rr(1180, 90, 230, 210, 14), GV(90, 300, '#9AD8FF', '#E8F8FF'), 7), T('M1295,90 V300 M1180,195 H1410', '#FFFFFF', 9), T('M1295,90 V300 M1180,195 H1410', N, 2.5, .35),
  A(ci(1360, 140, 24), '#FFE066'), A('M1186,268 q50,-40 110,-10 q60,-24 108,10 v26 h-218 Z', '#8ED070', .9),
  P('M1160,70 h270 v40 q-34,18 -68,0 q-34,18 -67,0 q-34,18 -68,0 q-33,18 -67,0 Z', GV(70, 124, '#7FB6F2', '#4A8EE0'), 6),
  // un cadre et une guirlande de fanions
  P(rr(170, 110, 150, 120, 10), '#FFFFFF', 7), A(rr(184, 124, 122, 92, 6), GV(124, 216, '#BFE8FF', '#E8F8FF')), A('M184,216 l40,-46 l30,26 l24,-18 l28,38 Z', '#8ED070'), A(ci(280, 150, 12), '#FFE066'),
  T('M420,60 Q700,130 980,60', '#8A5A32', 4), ...[...Array(7)].map((_, i) => { const t = (i + .5) / 7, x = 420 + 560 * t, y = 60 + 140 * t * (1 - t); return P(`M${r1(x - 22)},${r1(y)} h44 l-22,40 Z`, ['#F2473C', '#FFD23D', '#48A0F8', '#5CC86A'][i % 4], 4); }),
  // le tapis
  P(el(800, 590, 520, 46), GR(800, 580, 520, [0, '#FFC8DC'], [.7, '#FF9DC0'], [1, '#F27AA8']), 6), T(el(800, 588, 430, 34), '#FFFFFF', 6, .7),
];

// ═════════════ Le lit, vu de côté (l'origine est le milieu de son pied, au sol) ═════════════
// derrière : les pieds du fond, la tête de lit, et l'ombre sous le lit
S.c_lit_fond = [
  A(el(0, 0, 360, 26), GR(0, 0, 360, [0, '#5E3516', .5], [1, '#5E3516', 0])),
  P(rr(-300, -150, 26, 146, 8), BOIS(-150, 0, '#B5773A', '#7E4E22'), 6), P(rr(274, -150, 26, 146, 8), BOIS(-150, 0, '#B5773A', '#7E4E22'), 6),
  A('M-316,-158 H316 V-8 H-316 Z', GL(0, -158, 0, -8, [0, '#3A2408', .55], [1, '#3A2408', .2])),
  P('M-348,-176 V-330 Q-348,-372 -306,-372 H-296 Q-254,-372 -254,-330 V-176 Z', BOIS(-372, -176), 7), A(rr(-336, -350, 16, 150, 8), '#FFFFFF', .28),
];
// devant : le sommier, le matelas, la couverture, l'oreiller, les pieds de devant
S.c_lit_devant = [
  P(rr(-340, -158, 28, 158, 8), BOIS(-158, 0), 6), P(rr(312, -158, 28, 158, 8), BOIS(-158, 0), 6),
  P(rr(-352, -196, 704, 44, 12), BOIS(-196, -152), 7), T('M-336,-184 H336', '#FFE9C4', 5, .7),
  P(rr(-330, -246, 660, 58, 18), GV(-246, -188, '#FFFFFF', '#DCE6F4'), 6),
  P('M-150,-252 H324 Q334,-252 334,-242 V-170 H-150 Q-166,-210 -150,-252 Z', GV(-252, -170, '#8EC4F8', '#4A8EE0'), 6),
  ...[[-80, -214], [10, -232], [100, -206], [190, -230], [270, -200], [-30, -190], [150, -186]].map(([x, y]) => A(`M${x},${y - 11} L${x + 3.5},${y - 3.5} L${x + 11},${y - 2.5} L${x + 5},${y + 3.5} L${x + 7.5},${y + 11} L${x},${y + 6} L${x - 7.5},${y + 11} L${x - 5},${y + 3.5} L${x - 11},${y - 2.5} L${x - 3.5},${y - 3.5} Z`, '#FFF6A8')),
  P(rr(-318, -290, 150, 62, 28), GV(-290, -228, '#FFFFFF', '#E6ECF6'), 6), T('M-296,-274 q40,-10 100,0', '#FFFFFF', 5, .9),
  P('M340,-176 V-250 Q340,-282 372,-282 Q404,-282 404,-250 V-176 Z', BOIS(-282, -176), 7),
];
// l'ombre de la peluche posée (l'origine est son pied)
S.c_ombre = [A(el(0, 0, 86, 13), GR(0, 0, 86, [0, '#3A2408', .45], [1, '#3A2408', 0]))];

// ═════════════ Une carte (l'origine est son milieu ; 340 × 250), son halo, son voile ═════════════
S.c_carte = [
  A(rr(-164, -112, 340, 250, 30), '#1B1238', .25),
  P(rr(-170, -125, 340, 250, 30), GV(-125, 125, '#FFFFFF', '#F2E6D6'), 8),
  A(rr(-156, -111, 312, 222, 20), GV(-111, 111, '#FFE9D0', '#FFD2B0')), A('M-156,52 H156 V91 Q156,111 136,111 H-136 Q-156,111 -156,91 Z', '#D9A066'),
];
S.c_carte_halo = [A(rr(-200, -155, 400, 310, 50), GR(0, 0, 250, [0, '#FFF6A8', .95], [.7, '#FFD23D', .6], [1, '#FFD23D', 0]))];
S.c_carte_voile = [A(rr(-170, -125, 340, 250, 30), '#5A6684', .6)];
S.c_carte_cadre = [T(rr(-170, -125, 340, 250, 30), '#FFD23D', 14)];

const SCENES = { chambre: [{ n: 'chambre', s: 'c_chambre', x: 0, y: 0 }] };
const CIELS = { chambre: ['#FFE9D0', '#FFD2B0', '#B5773A', 1] };
const LISTES = {
  // les peluches : le dessin, « le nounours », sa hauteur (pour qu'elle tienne sous le lit)
  peluches: [['lot_ours', 'le nounours', 196], ['lot_canard', 'le canard', 212], ['lot_pingouin', 'le pingouin', 190]],
  // les quatre places : la clé, le mot, la phrase
  places: [['sur', 'SUR', 'sur le lit'], ['sous', 'SOUS', 'sous le lit'], ['devant', 'DEVANT', 'devant le lit'], ['derriere', 'DERRIÈRE', 'derrière le lit']],
};

module.exports = { S, SCENES, CIELS, LISTES };
