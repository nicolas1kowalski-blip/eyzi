// Les dessins du jeu « Chamboule-tout ». genere.js en tire Games/ChambouleArt.cs (pour l'application) ;
// le banc d'essai s'en sert pour produire de vraies images du jeu sans Windows. Les outils de dessin sont dans outils.js.
//
// On est devant le stand de la fête foraine : le comptoir au premier plan, la table et ses boîtes de conserve
// au fond, sous l'auvent rayé et ses ampoules. La scène fait 1600 × 900.
const { N, P, A, T, TT, GL, GR, GV, GH, r1, ci, el, rr, pol, alea } = require('./outils.js');
const communs = require('./communs.js');

const S = {};
communs(S);

// ═════════════ Les matières ═════════════
// un cylindre vu de côté : sombre sur les bords, un éclat près du bord gauche
const CYL = (x0, x1, sombre, clair, moyen, ombre) => GL(x0, 0, x1, 0, [0, sombre], [.2, clair], [.48, moyen], [.82, ombre], [1, sombre]);
const FER = (x0, x1) => CYL(x0, x1, '#6C7893', '#FFFFFF', '#C9D3E8', '#8793AD');
const BOIS_V = (y0, y1, clair = '#E3B27A', fonce = '#C98F4E') => GV(y0, y1, clair, fonce);
const POTEAU = (x0, x1) => GL(x0, 0, x1, 0, [0, '#5E3516'], [.3, '#B57A40'], [.6, '#96602E'], [1, '#5E3516']);

// ═════════════ Le fond du stand : le rideau, la lumière ═════════════
const plis = [];
for (let i = 0; i < 16; i++) {
  const x = i * 100;
  plis.push(A(`M${x},0 q34,450 0,900 h46 q-30,-450 0,-900 Z`, '#120A38', .16));
  plis.push(A(`M${x + 56},0 q-22,450 0,900 h12 q22,-450 0,-900 Z`, '#FFFFFF', .045));
}
const semis = [];   // de petites étoiles brodées, très discrètes
for (let i = 0; i < 26; i++) {
  const x = r1(90 + alea(i * 3 + 1) * 1420), y = r1(170 + alea(i * 5 + 2) * 400), k = r1(.35 + alea(i + 9) * .4);
  semis.push(A(`M${x},${r1(y - 22 * k)} Q${r1(x + 3 * k)},${r1(y - 3 * k)} ${r1(x + 22 * k)},${y} Q${r1(x + 3 * k)},${r1(y + 3 * k)} ${x},${r1(y + 22 * k)} Q${r1(x - 3 * k)},${r1(y + 3 * k)} ${r1(x - 22 * k)},${y} Q${r1(x - 3 * k)},${r1(y - 3 * k)} ${x},${r1(y - 22 * k)} Z`, '#FFD86B', .2));
}
S.d_fond = [
  A('M0,0 H1600 V900 H0 Z', GV(0, 900, '#3B2C86', '#231A56')),
  ...plis, ...semis,
  // la lumière des projecteurs sur les boîtes
  A('M0,0 H1600 V900 H0 Z', GR(800, 420, 620, [0, '#FFE9B0', .36], [.55, '#FFD98A', .12], [1, '#FFD98A', 0])),
  // le bas du rideau dans l'ombre
  A('M0,560 H1600 V900 H0 Z', GL(0, 560, 0, 900, [0, '#0E0830', 0], [1, '#0E0830', .55])),
];

// ═════════════ Les côtés : les poteaux et les étagères des lots ═════════════
const etagere = (x, y, l) => [
  A(`M${x},${y + 22} h${l} v26 h${-l} Z`, GL(0, y + 22, 0, y + 48, [0, '#0E0830', .4], [1, '#0E0830', 0])),
  P(`M${x + 26},${y + 20} l0,58 l44,-58 Z`, '#8A5226', 5), P(`M${x + l - 26},${y + 20} l0,58 l-44,-58 Z`, '#8A5226', 5),
  P(rr(x, y, l, 24, 7), BOIS_V(y, y + 24), 6), T(`M${x + 10},${y + 7} h${l - 20}`, '#FFE9C4', 4, .7),
];
S.d_cotes = [
  // les panneaux de côté, plus sombres
  A('M0,0 H330 V900 H0 Z', GL(0, 0, 330, 0, [0, '#0E0830', .55], [1, '#0E0830', 0])),
  A('M1270,0 H1600 V900 H1270 Z', GL(1270, 0, 1600, 0, [0, '#0E0830', 0], [1, '#0E0830', .55])),
  ...etagere(52, 330, 262), ...etagere(52, 566, 262), ...etagere(1286, 330, 262), ...etagere(1286, 566, 262),
  P('M-30,-20 H58 V920 H-30 Z', POTEAU(-30, 58), 7), T('M22,0 V900', '#5E3516', 3, .5), T('M40,60 q6,60 -2,120 M14,400 q8,70 0,140', '#5E3516', 3, .45),
  P('M1542,-20 H1630 V920 H1542 Z', POTEAU(1542, 1630), 7), T('M1578,0 V900', '#5E3516', 3, .5), T('M1560,200 q8,70 0,140 M1590,520 q-8,60 0,120', '#5E3516', 3, .45),
];

// ═════════════ L'auvent rayé, sa poutre et le fil des ampoules ═════════════
const festons = [];
for (let i = 0; i < 16; i++) {
  const x = i * 100, rouge = i % 2 === 0;
  festons.push(P(`M${x},24 h100 v52 a50,50 0 0 1 -100,0 Z`, rouge ? GV(24, 126, '#F2473C', '#B5261F') : GV(24, 126, '#FFFAF0', '#E4D6C0'), 6));
  festons.push(T(`M${x + 16},40 v30`, '#FFFFFF', 7, rouge ? .28 : .6));
}
S.d_auvent = [
  // l'ombre de l'auvent sur le rideau
  A('M0,60 H1600 V230 H0 Z', GL(0, 110, 0, 230, [0, '#0E0830', .55], [1, '#0E0830', 0])),
  ...festons,
  // le galon doré et ses pompons
  T('M0,76 H1600', '#FFD23D', 7, .95),
  ...[...Array(17)].map((_, i) => P(ci(i * 100, 80, 9), GR(i * 100 - 3, 77, 12, [0, '#FFF6A8'], [1, '#E8A21C']), 3)),
  // la poutre
  P('M-20,-20 H1620 V30 H-20 Z', BOIS_V(-20, 30, '#B57A40', '#7A4A22'), 7), T('M0,6 H1600', '#E3B27A', 4, .6),
  // le fil des ampoules
  T('M0,138 q50,26 100,0 t100,0 t100,0 t100,0 t100,0 t100,0 t100,0 t100,0 t100,0 t100,0 t100,0 t100,0 t100,0 t100,0 t100,0 t100,0', '#1B1238', 4),
];
// une ampoule (l'origine est son centre) : son halo et son verre prennent la couleur « c »
S.ampoule = [
  A(ci(0, 4, 40), GR(0, 4, 40, [0, '$c', .7], [.4, '$c', .28], [1, '$c', 0])),
  P(rr(-7, -15, 14, 11, 3), '#3A3454', 3),
  P('M-11,0 a11,11 0 1 1 22,0 q0,8 -5,13 h-12 q-5,-5 -5,-13 Z', GR(-3, -2, 16, [0, '#FFFFFF'], [.5, '$clair'], [1, '$c']), 3),
  A(el(-4, -3, 3, 4), '#FFFFFF', .9),
];

// ═════════════ La table des boîtes ═════════════
const nappe = [];
for (let i = 0; i < 8; i++) nappe.push(A(`M${342 + i * 124},700 q16,100 0,200 h30 q-14,-100 0,-200 Z`, '#0E2C74', .16));
const etoileOr = (x, y, k) => P(`M${x},${r1(y - 26 * k)} L${r1(x + 7 * k)},${r1(y - 8 * k)} L${r1(x + 26 * k)},${r1(y - 8 * k)} L${r1(x + 11 * k)},${r1(y + 4 * k)} L${r1(x + 16 * k)},${r1(y + 23 * k)} L${x},${r1(y + 12 * k)} L${r1(x - 16 * k)},${r1(y + 23 * k)} L${r1(x - 11 * k)},${r1(y + 4 * k)} L${r1(x - 26 * k)},${r1(y - 8 * k)} L${r1(x - 7 * k)},${r1(y - 8 * k)} Z`, GR(x - 5, y - 8, 30 * k, [0, '#FFF6A8'], [1, '#F2B92C']), 4, '#B5780C');
S.d_table = [
  // l'ombre portée sur le rideau, derrière la table
  A(el(800, 640, 560, 46), GR(800, 640, 560, [0, '#0E0830', .5], [1, '#0E0830', 0])),
  // la nappe bleue, son galon et ses étoiles
  P('M322,688 H1278 V910 H322 Z', GV(688, 900, '#3A7BE8', '#1E4FA8'), 6),
  ...nappe,
  A('M322,688 H1278 V740 H322 Z', GL(0, 688, 0, 740, [0, '#0A1F5A', .5], [1, '#0A1F5A', 0])),
  T('M326,716 H1274', '#FFD23D', 9), T('M326,716 H1274', '#FFF6A8', 3, .8),
  etoileOr(470, 762, .82), etoileOr(690, 762, .82), etoileOr(910, 762, .82), etoileOr(1130, 762, .82),
  // le plateau, vu un peu de dessus
  P('M308,664 H1292 V690 H308 Z', GV(664, 690, '#A8713A', '#70431C'), 6),
  A('M352,604 H1248 L1292,664 H308 Z', GL(0, 604, 0, 664, [0, '#D6A468'], [1, '#EFC592'])),
  T('M480,604 L452,664 M640,604 L626,664 M800,604 V664 M960,604 L974,664 M1120,604 L1148,664', '#9A6A34', 3, .45),
  T('M352,604 H1248 L1292,664 H308 Z', N, 6),
  T('M318,660 H1282', '#FFF1D6', 4, .8),
];
// l'ombre d'une boîte posée (l'origine est le bas de la boîte)
S.boite_ombre = [A(el(6, 2, 62, 13), GR(6, 2, 62, [0, '#3A2408', .5], [.6, '#3A2408', .22], [1, '#3A2408', 0]))];

// ═════════════ Une boîte de conserve (l'origine est son centre ; 100 de large, 116 d'un bord à l'autre) ═════════════
// son étiquette prend les couleurs « coul », « clair » et « fonce »
S.boite = [
  // le corps en fer-blanc
  P('M-50,-58 V58 Q0,76 50,58 V-58 Z', FER(-50, 50), 5),
  // les joncs du haut et du bas
  T('M-50,-44 Q0,-26 50,-44 M-50,-35 Q0,-17 50,-35 M-50,39 Q0,57 50,39 M-50,48 Q0,66 50,48', '#5A6684', 3, .7),
  T('M-48,-40 Q0,-22 48,-40 M-48,43 Q0,61 48,43', '#FFFFFF', 2, .7),
  // l'étiquette de papier
  A('M-50,-27 Q0,-9 50,-27 V31 Q0,49 -50,31 Z', GL(-50, 0, 50, 0, [0, '$fonce'], [.22, '$clair'], [.5, '$coul'], [.85, '$fonce'], [1, '$fonce'])),
  T('M-50,-27 Q0,-9 50,-27 M-50,31 Q0,49 50,31', N, 3, .55),
  T('M-48,-19 Q0,-1 48,-19 M-48,23 Q0,41 48,23', '#FFFFFF', 3.5, .85),
  // le reflet le long du cylindre, l'ombre à droite
  A('M-34,-50 q8,4 12,5 V60 q-6,-1 -12,-4 Z', '#FFFFFF', .42),
  A('M32,-52 q10,-2 18,-6 V58 q-8,4 -18,6 Z', '#1B1238', .2),
  // le couvercle
  P(el(0, -58, 50, 15), GL(-50, -70, 50, -46, [0, '#F4F7FF'], [.5, '#C9D3E8'], [1, '#8793AD']), 5),
  T(el(0, -58, 40, 10.5), '#6C7893', 3, .8), A(el(0, -57, 37, 9), GL(-40, -66, 40, -48, [0, '#AEB9D2'], [1, '#E9EEFA'])),
  T('M-26,-62 q14,-6 30,-5', '#FFFFFF', 3, .9),
];
// ce qui est peint sur l'étiquette (même origine que la boîte)
const pastille = [A(el(0, 4, 23, 22), '#FFFAF0'), T(el(0, 4, 23, 22), N, 2.5, .5)];
const etoile5 = (cx, cy, R, r) => { let d = ''; for (let i = 0; i < 10; i++) { const [x, y] = pol(-90 + i * 36, i % 2 ? r : R); d += (i ? 'L' : 'M') + r1(cx + x) + ',' + r1(cy + y) + ' '; } return d + 'Z'; };
S.em_etoile = [...pastille, P(etoile5(0, 5, 18, 8), GR(-3, 0, 20, [0, '#FFE98A'], [1, '#F2A51C']), 2.5, '#B5780C')];
S.em_coeur = [...pastille, P('M0,19 C-24,1 -12,-15 0,-3 C12,-15 24,1 0,19 Z', GR(-4, -2, 22, [0, '#FF9DB8'], [1, '#E8305E']), 2.5, '#A81C40')];
S.em_cible = [...pastille, A(ci(0, 4, 17), '#E8382E'), A(ci(0, 4, 11), '#FFFAF0'), A(ci(0, 4, 5.5), '#E8382E')];
S.em_visage = [
  ...pastille,
  A(ci(-8, -2, 3.6), N), A(ci(8, -2, 3.6), N), A(ci(-7, -3, 1.2), '#FFFFFF'), A(ci(9, -3, 1.2), '#FFFFFF'),
  T('M-11,8 Q0,19 11,8', N, 3), P(ci(0, 5, 4.5), GR(-1, 4, 6, [0, '#FF9A8A'], [1, '#E8382E']), 1.5),
  A(ci(-14, 7, 4), '#FF9DB8', .7), A(ci(14, 7, 4), '#FF9DB8', .7),
];
S.em_soleil = [
  ...pastille,
  ...[...Array(8)].map((_, i) => { const [x1, y1] = pol(i * 45, 12), [x2, y2] = pol(i * 45, 19); return T(`M${x1},${r1(y1 + 4)} L${x2},${r1(y2 + 4)}`, '#F2A51C', 3.5); }),
  P(ci(0, 4, 9), GR(-2, 2, 11, [0, '#FFF29A'], [1, '#FFC12E']), 2, '#B5780C'),
];

// ═════════════ La balle (l'origine est son centre) ═════════════
S.balle = [
  P(ci(0, 0, 46), GR(-15, -17, 70, [0, '#FF9482'], [.5, '#EC3E32'], [1, '#9E1812']), 5),
  T('M-40,-23 Q0,8 40,-23 M-40,23 Q0,-8 40,23', '#FFF6E8', 5, .95),
  T('M-30,-20 l-3,-9 M-16,-12 l-2,-10 M0,-9 v-10 M16,-12 l2,-10 M30,-20 l3,-9 M-30,20 l-3,9 M-16,12 l-2,10 M0,9 v10 M16,12 l2,10 M30,20 l3,9', '#FFF6E8', 2.5, .8),
  A(el(-17, -21, 13, 8), '#FFFFFF', .55),
];
S.balle_ombre = [A(el(0, 0, 44, 10), GR(0, 0, 44, [0, '#3A2408', .45], [1, '#3A2408', 0]))];

// ═════════════ Le comptoir, au premier plan ═════════════
const losanges = [];
for (let i = 0; i < 17; i++) { const x = i * 100; losanges.push(P(`M${x},838 l40,24 l-40,24 l-40,-24 Z`, i % 2 ? GV(838, 886, '#FFE066', '#F2B92C') : GV(838, 886, '#F2473C', '#B5261F'), 4)); }
S.d_comptoir = [
  // l'ombre du comptoir sur la nappe
  A('M0,744 H1600 V790 H0 Z', GL(0, 744, 0, 790, [0, '#0E0830', 0], [1, '#0E0830', .45])),
  P('M-20,822 H1620 V920 H-20 Z', GV(822, 900, '#B57A40', '#7E4E22'), 7),
  T('M200,822 V900 M400,822 V900 M600,822 V900 M800,822 V900 M1000,822 V900 M1200,822 V900 M1400,822 V900', '#5E3516', 3, .4),
  ...losanges,
  P('M-20,782 H1620 V824 H-20 Z', GV(782, 824, '#F4CE98', '#D9A066'), 7),
  T('M0,791 H1600', '#FFF1D6', 5, .9), T('M90,806 q120,-8 260,0 M700,810 q160,-8 300,0 M1180,804 q120,8 260,0', '#B5773A', 3, .4),
];
// le panier des balles (l'origine est le bas du panier)
const osier = [];
for (let i = 0; i < 5; i++) osier.push(T(`M${-84 + i * 3},${-22 - i * 17} Q0,${-10 - i * 17} ${84 - i * 3},${-22 - i * 17}`, '#8A5A1E', 3, .65));
for (let i = 0; i < 9; i++) osier.push(T(`M${-70 + i * 17.5},-6 l${(i - 4) * 2.2},-92`, '#8A5A1E', 3, .5));
S.panier = [
  A(el(0, 0, 104, 16), GR(0, 0, 104, [0, '#3A2408', .5], [1, '#3A2408', 0])),
  // les balles qui dépassent
  P(ci(-40, -108, 40), GR(-52, -122, 60, [0, '#FF9482'], [.5, '#EC3E32'], [1, '#9E1812']), 5), T('M-74,-126 Q-40,-100 -6,-126', '#FFF6E8', 4, .95),
  P(ci(38, -112, 40), GR(26, -126, 60, [0, '#FFE98A'], [.5, '#FFC12E'], [1, '#C98A0E']), 5), T('M4,-130 Q38,-104 72,-130', '#FFF6E8', 4, .95),
  P(ci(0, -128, 38), GR(-12, -142, 58, [0, '#9AD0FF'], [.5, '#3F8EF2'], [1, '#1E4FA8']), 5), T('M-32,-146 Q0,-122 32,-146', '#FFF6E8', 4, .95),
  // le panier d'osier
  P('M-96,-104 Q0,-88 96,-104 L78,-6 Q0,10 -78,-6 Z', GL(-96, 0, 96, 0, [0, '#9A6A26'], [.3, '#E8B866'], [.6, '#C9923E'], [1, '#8A5A1E']), 6),
  ...osier,
  P('M-102,-112 Q0,-94 102,-112 Q104,-98 98,-96 Q0,-78 -98,-96 Q-104,-98 -102,-112 Z', GL(-102, 0, 102, 0, [0, '#A8742E'], [.3, '#F2CA80'], [1, '#9A6A26']), 5),
];

// ═════════════ Les lots : quatre peluches assises (dans peluches.js, partagées avec les activités de nombres) ═════════════
require('./peluches.js')(S);

// ═════════════ Les éclats ═════════════
const eclat = (n, R, r) => { let d = ''; for (let i = 0; i < n * 2; i++) { const [x, y] = pol(-90 + i * 180 / n, i % 2 ? r : R * (.82 + .18 * alea(i + n))); d += (i ? 'L' : 'M') + x + ',' + y + ' '; } return d + 'Z'; };
S.impact = [
  A(ci(0, 0, 110), GR(0, 0, 110, [0, '#FFFFFF', .7], [.5, '#FFF29A', .3], [1, '#FFF29A', 0])),
  P(eclat(9, 92, 40), GR(0, 0, 92, [0, '#FFFFFF'], [.6, '#FFE066'], [1, '#FF9A2E']), 5, '#E8701C'),
  A(eclat(7, 46, 20), '#FFFFFF'),
];
S.poussiere = [A(ci(0, 0, 30), GR(0, 0, 30, [0, '#FFF6E0', .85], [.5, '#F2DCB0', .4], [1, '#F2DCB0', 0]))];
S.confetti_a = [A(rr(-8, -5, 16, 10, 2), '#F2473C')];
S.confetti_b = [A(rr(-8, -5, 16, 10, 2), '#FFD23D')];
S.confetti_c = [A(rr(-8, -5, 16, 10, 2), '#48A0F8')];
S.confetti_d = [A(rr(-8, -5, 16, 10, 2), '#5CC86A')];
// le faisceau qui éclaire le lot gagné (l'origine est le bas du faisceau)
S.faisceau = [A('M-70,-360 L70,-360 L150,0 L-150,0 Z', GL(0, -360, 0, 0, [0, '#FFF6C4', 0], [1, '#FFF6C4', .5])), A(el(0, 0, 150, 22), GR(0, 0, 150, [0, '#FFF6C4', .6], [1, '#FFF6C4', 0]))];

// ═════════════ La scène ═════════════
const SCENES = {
  stand: [
    { n: 'fond', s: 'd_fond', x: 0, y: 0 }, { n: 'table', s: 'd_table', x: 0, y: 0 }, { n: 'cotes', s: 'd_cotes', x: 0, y: 0 },
    { n: 'auvent', s: 'd_auvent', x: 0, y: 0 }, { n: 'comptoir', s: 'd_comptoir', x: 0, y: 0 }, { n: 'panier', s: 'panier', x: 268, y: 806 },
  ],
};
// le fond de la page autour de la scène : le bois des poteaux en haut, le bois du comptoir en bas
const CIELS = { stand: ['#7A4A22', '#7A4A22', '#7E4E22', 0] };
// Les lots : la clé du dessin, ce que la voix dit, et sa place sur les étagères
const LISTES = {
  lots: [
    ['lot_ours', 'le nounours', 183, 330], ['lot_lapin', 'le lapin', 183, 566],
    ['lot_canard', 'le canard', 1417, 330], ['lot_pingouin', 'le pingouin', 1417, 566],
  ],
  // les étiquettes des boîtes : couleur, clair, foncé
  etiquettes: [
    ['#F2473C', '#FFB0A6', '#A81C16'], ['#3F8EF2', '#B4DCFF', '#1E4FA8'], ['#3DBA62', '#B8F0C0', '#1E7A3A'],
    ['#FFC12E', '#FFF2A8', '#C98A0E'], ['#FF8A2E', '#FFD6A0', '#C2560C'], ['#9A6CF0', '#DCCBFF', '#5A30B0'], ['#F25F9A', '#FFC2DC', '#B02A66'],
  ],
  emblemes: [['em_etoile'], ['em_coeur'], ['em_visage'], ['em_cible'], ['em_soleil']],
};

module.exports = { S, SCENES, CIELS, LISTES };
