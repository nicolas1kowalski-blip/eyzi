// Les dessins de l'activité « Dans mon corps ». genere.js en tire Games/CorpsArt.cs (pour l'application) ;
// le banc d'essai s'en sert pour produire de vraies images sans Windows. Les outils de dessin sont dans outils.js.
//
// Oscar, le squelette de la classe, est dessiné autour de son axe (x = 0), le haut de l'image en y = 0. Ses bras et
// ses jambes sont des os séparés (l'origine à l'articulation du haut, l'os vers le bas) : le jeu les articule.
// Chaque organe est dessiné autour de son propre milieu ; la liste « organes » dit où il va et ce que la voix en dit.
const { N, P, A, T, TT, GL, GR, GV, GH, r1, ci, el, rr, pol, alea } = require('./outils.js');
const communs = require('./communs.js');

const S = {};

communs(S);

const etoileD = (x, y, R, r = R * .45, n = 5, tour = -90) => [...Array(n * 2)].map((_, i) => { const [dx, dy] = pol(tour + i * 180 / n, i % 2 ? r : R); return (i ? 'L' : 'M') + r1(x + dx) + ',' + r1(y + dy); }).join(' ') + ' Z';
const OS = '#FFF8E7', OS_OMBRE = '#E3D3AE', CREUX = '#2A1C4E';
const IVOIRE = (x, y, r) => GR(x, y, r, [0, '#FFFFFF'], [.55, '#FFF6DF'], [1, '#E3D3AE']);
// un os long : deux têtes rondes, un corps droit, une ombre d'un côté
const os_long = (l, w, tete = w * .95) => [
  T(`M0,${r1(tete * .6)} V${r1(l - tete * .6)}`, N, w + 9), P(ci(-tete * .45, tete * .5, tete * .62), OS, 4.5), P(ci(tete * .45, tete * .5, tete * .62), OS, 4.5), P(ci(-tete * .45, l - tete * .5, tete * .62), OS, 4.5), P(ci(tete * .45, l - tete * .5, tete * .62), OS, 4.5),
  T(`M0,${r1(tete * .6)} V${r1(l - tete * .6)}`, OS, w), T(`M${r1(w * .22)},${r1(tete)} V${r1(l - tete)}`, OS_OMBRE, w * .3, .8), T(`M${r1(-w * .22)},${r1(tete * 1.4)} V${r1(l * .6)}`, '#FFFFFF', w * .18, .9),
];

// ═════════════ La salle de classe ═════════════
S.c_salle = [
  A('M-10,-10 H1610 V700 H-10 Z', GV(0, 700, '#FFF6E2', '#FBE7C2')),
  A('M-10,560 H1610 V700 H-10 Z', GV(560, 700, '#BFE3D6', '#A6D4C4')), T('M-10,560 H1610', '#FFFFFF', 8, .9),
  T([...Array(20)].map((_, i) => `M${40 + i * 82},566 V696`).join(' '), '#8FC4B2', 3, .5),
  // le parquet
  A('M-10,700 H1610 V910 H-10 Z', GV(700, 900, '#E2B27A', '#C28E52')), T('M-10,700 H1610', '#8A5F36', 6),
  T('M-10,760 H1610 M-10,832 H1610', '#9A6B3C', 3, .5), T([...Array(11)].map((_, i) => `M${90 + i * 150},${i % 2 ? 700 : 760} v${i % 2 ? 60 : 72}`).join(' ') + [...Array(10)].map((_, i) => ` M${160 + i * 150},832 v70`).join(''), '#9A6B3C', 3, .5),
  // l'horloge
  P(ci(150, 104, 62), GV(42, 166, '#FFFFFF', '#E6ECF8'), 7), T(ci(150, 104, 50), '#48B0F8', 5), T('M150,104 V68 M150,104 L176,116', N, 6), A(ci(150, 104, 6), '#F0483E'),
  T([...Array(12)].map((_, i) => { const [x, y] = pol(i * 30, 42), [x2, y2] = pol(i * 30, 48); return `M${r1(150 + x)},${r1(104 + y)} L${r1(150 + x2)},${r1(104 + y2)}`; }).join(' '), N, 3.5),
  // le tableau, sa craie, son éponge
  P(rr(1118, 60, 452, 404, 14), GV(60, 464, '#D9A866', '#A8703C'), 7), P(rr(1136, 78, 416, 368, 6), GV(78, 446, '#3E8E6A', '#2A6E50'), 5),
  T('M1160,110 q60,-10 120,2 M1400,420 q50,-12 110,-2', '#FFFFFF', 3, .12), P(rr(1118, 458, 452, 16, 6), '#B5844C', 5),
  P(rr(1160, 446, 44, 12, 4), '#FFFFFF', 3.5), P(rr(1470, 440, 60, 18, 5), '#F2C14E', 3.5), A(rr(1470, 440, 60, 7, 3), '#8A8AA0'),
  // le bureau, des livres, une pomme
  P(rr(1150, 560, 400, 26, 8), GV(560, 586, '#EBC08A', '#C29258'), 5.5), P('M1176,586 H1524 V700 H1176 Z', GV(586, 700, '#D9A866', '#B5844C'), 5.5), P(rr(1200, 606, 300, 44, 8), '#EBC08A', 4), T('M1330,628 h40', '#8A5F36', 6),
  P(rr(1190, 540, 100, 20, 4), '#48B0F8', 4), P(rr(1198, 522, 86, 18, 4), '#FF9A3D', 4), P(rr(1206, 506, 72, 16, 4), '#9A6CF0', 4),
  P('M1462,538 C1436,526 1432,560 1450,560 C1456,562 1468,562 1474,560 C1492,560 1488,526 1462,538 Z', GR(1454, 542, 30, [0, '#FF9A8A'], [1, '#D9302A']), 4), T('M1462,538 q2,-10 8,-12', '#7A5230', 3.5), P('M1466,530 q10,-10 16,-2 q-8,6 -16,2 Z', '#5CB850', 2.5),
];
S.c_coche = [P(ci(0, 0, 15), '#3CCB6C', 4), T('M-7,0 l5,6 l10,-12', '#FFFFFF', 4.5)];
S.c_rond_craie = [T(ci(0, 0, 40), '#FFFFFF', 3.5, .5)];

// ═════════════ Oscar ═════════════
// sa potence à roulettes : le pied, le mât derrière lui, le crochet au-dessus du crâne
S.c_potence = [
  A(el(0, 884, 250, 22), N, .16),
  ...TT('M-190,872 H190', '#8793AD', 16), P(ci(-190, 878, 13), '#3A3454', 4.5), P(ci(190, 878, 13), '#3A3454', 4.5), P(ci(0, 882, 13), '#3A3454', 4.5),
  T('M0,872 V26 Q0,6 22,6', N, 22), T('M0,872 V26 Q0,6 22,6', '#B9C2D6', 13), T('M-3,860 V40', '#FFFFFF', 3, .7),
  P(ci(0, 44, 9), '#F2C14E', 4),
];
S.os_colonne = [
  ...[...Array(15)].flatMap((_, i) => { const y = 262 + i * 23, w = 30 + (i > 9 ? (i - 9) * 2.2 : 0); return [T(`M${-w / 2 - 9},${y + 8} h${w + 18}`, N, 12), T(`M${-w / 2 - 9},${y + 8} h${w + 18}`, OS_OMBRE, 6), P(rr(-w / 2, y, w, 17, 6), IVOIRE(-4, y + 4, 30), 4.5)]; }),
];
S.os_bassin = [
  P('M-104,528 Q-64,504 -24,548 L24,548 Q64,504 104,528 Q116,582 70,608 Q44,620 24,598 L-24,598 Q-44,620 -70,608 Q-116,582 -104,528 Z', IVOIRE(-30, 540, 150), 7),
  A(el(-50, 582, 16, 13), CREUX, .85), A(el(50, 582, 16, 13), CREUX, .85), T('M-16,556 q16,10 32,0 M-12,572 q12,8 24,0', OS_OMBRE, 4), T('M-90,534 Q-64,520 -38,544', '#FFFFFF', 6, .8),
  P(ci(-58, 602, 14), OS, 5), P(ci(58, 602, 14), OS, 5),
];
// les côtes et le sternum, devant les poumons et le cœur ; les clavicules et les épaules
const cote = (k, y, w, courte) => `M${k * 10},${y} Q${k * w},${y - 12} ${k * (w + 4)},${y + 20} Q${k * w},${y + 34} ${k * (w - (courte ? 16 : 40))},${y + 31}`;
const COTES = [[292, 90], [316, 106], [340, 116], [364, 120], [388, 116], [412, 106, 1], [434, 90, 1]];
S.os_cotes = [
  T(COTES.flatMap(([y, w, c]) => [cote(-1, y, w, c), cote(1, y, w, c)]).join(' '), N, 19), T(COTES.flatMap(([y, w, c]) => [cote(-1, y, w, c), cote(1, y, w, c)]).join(' '), OS, 11),
  T(COTES.map(([y, w]) => `M${-w * .5},${y - 6} q${-w * .3},2 ${-w * .44},14`).join(' '), '#FFFFFF', 3.5, .8), T(COTES.map(([y, w]) => `M${w * .96},${y + 14} q2,10 -6,16`).join(' '), OS_OMBRE, 4, .9),
  P(rr(-13, 280, 26, 132, 11), IVOIRE(-4, 300, 60), 5.5), T('M-11,312 h22 M-11,338 h22 M-11,364 h22 M-11,388 h22', OS_OMBRE, 3),
];
S.os_epaules = [
  T('M-12,278 Q-70,258 -124,278 M12,278 Q70,258 124,278', N, 21), T('M-12,278 Q-70,258 -124,278 M12,278 Q70,258 124,278', OS, 13), T('M-30,270 Q-70,256 -110,268', '#FFFFFF', 4, .8),
  P(ci(-128, 286, 17), IVOIRE(-132, 282, 20), 5.5), P(ci(128, 286, 17), IVOIRE(124, 282, 20), 5.5),
];
// le crâne (l'origine est en son milieu) : ouvert sur le dessus, pour recevoir le cerveau
S.os_crane = [
  P('M-90,-18 Q-94,-94 0,-98 Q94,-94 90,-18 Q90,22 66,36 L60,64 Q40,76 0,76 Q-40,76 -60,64 L-66,36 Q-90,22 -90,-18 Z', IVOIRE(-30, -40, 150), 7),
  A('M90,-18 Q90,22 66,36 L60,64 Q40,76 20,76 Q52,40 56,-10 Q58,-60 30,-92 Q94,-80 90,-18 Z', OS_OMBRE, .6),
  P(el(0, -52, 72, 40), GR(0, -60, 80, [0, '#4A3A7A'], [1, '#1E1638']), 5.5), T('M-60,-68 Q-30,-88 10,-88', '#8E7ACF', 4, .6),
  P(el(-36, 4, 26, 28), CREUX, 4), P(el(36, 4, 26, 28), CREUX, 4), P('M0,26 L-11,46 Q0,52 11,46 Z', CREUX, 3.5),
  P(rr(-40, 56, 80, 22, 7), '#FFFFFF', 4.5), T('M-24,58 v18 M-8,58 v18 M8,58 v18 M24,58 v18', N, 3),
  T('M-70,-20 Q-66,6 -56,22', '#FFFFFF', 7, .8), T('M60,-58 l-10,10 l8,6 l-8,10', OS_OMBRE, 3.5),
];
S.os_machoire = [
  P('M-58,-10 Q-60,26 -32,36 Q0,42 32,36 Q60,26 58,-10 L42,-8 Q0,6 -42,-8 Z', IVOIRE(-16, 10, 70), 6.5),
  P(rr(-34, -14, 68, 18, 6), '#FFFFFF', 4.5), T('M-20,-12 v14 M-7,-12 v14 M7,-12 v14 M20,-12 v14', N, 3), T('M-40,14 Q0,26 40,14', OS_OMBRE, 4, .8),
];
S.os_cou = [...[0, 1, 2].flatMap(i => [P(rr(-15, 232 + i * 12, 30, 11, 5), IVOIRE(-3, 236 + i * 12, 20), 4)])];
S.os_oeil = [P(ci(0, 0, 17), '#FFFFFF', 3.5), A(ci(-5, -6, 4), '#EAF4FF')];
S.os_pupille = [A(ci(0, 0, 8.5), N), A(ci(2.6, -3, 3), '#FFFFFF')];
// les os des bras et des jambes (l'origine est à l'articulation du haut)
S.os_humerus = os_long(128, 15);
S.os_avantbras = [...os_long(118, 8, 9).map(f => ({ ...f, d: f.d })), ...os_long(118, 8, 9)].map((f, i, t) => ({ ...f, d: f.d.replace(/M(-?[\d.]+),/g, (m, x) => 'M' + r1(parseFloat(x) + (i < t.length / 2 ? -7 : 7)) + ',') }));
S.os_main = [
  P(rr(-17, -2, 34, 24, 9), IVOIRE(-6, 6, 26), 5),
  ...[-12, -4, 4, 12].flatMap((x, i) => [T(`M${x},22 V${44 + (i === 1 || i === 2 ? 8 : 0)}`, N, 11), T(`M${x},22 V${44 + (i === 1 || i === 2 ? 8 : 0)}`, OS, 6), T(`M${x - 3},34 h6`, OS_OMBRE, 2.5)]),
  T('M-17,8 L-30,24', N, 11), T('M-17,8 L-30,24', OS, 6),
];
S.os_femur = os_long(128, 19);
S.os_tibia = [...os_long(118, 13, 13), T('M12,14 V104', N, 11), T('M12,14 V104', OS, 5)];
S.os_pied = [
  P('M-14,-4 Q-16,22 0,24 L52,26 Q66,24 62,14 Q44,2 14,-2 Z', IVOIRE(10, 6, 50), 5.5), T('M30,8 L56,16 M24,16 L58,22', OS_OMBRE, 3), T('M-4,2 Q10,-2 24,4', '#FFFFFF', 4, .8),
];

// ═════════════ Les organes (chacun autour de son milieu) ═════════════
S.o_cerveau = [
  P('M-78,14 Q-92,-22 -62,-40 Q-44,-62 -12,-56 Q10,-70 36,-56 Q70,-56 80,-24 Q94,4 70,26 Q56,46 20,44 Q-10,54 -40,42 Q-70,42 -78,14 Z', GR(-20, -24, 110, [0, '#FFD0DE'], [.6, '#FF9DBA'], [1, '#E0688E']), 5.5),
  T('M-2,-56 Q-10,-20 0,44', '#D9608A', 4, .8), T('M-56,-30 q16,6 12,22 q-6,14 10,22 M-30,-46 q10,14 0,26 M28,-48 q-12,14 2,28 M58,-34 q-18,8 -10,24 q10,10 -4,22 M-64,16 q14,-6 26,4 M40,22 q14,-10 26,-2', '#D9608A', 3.5, .8),
  T('M-58,-32 Q-40,-50 -16,-48', '#FFFFFF', 6, .6),
];
S.o_poumons = [
  ...TT('M0,-88 V-40 M0,-42 Q-16,-30 -30,-22 M0,-42 Q16,-30 30,-22', '#9AD0F5', 9),
  P('M-12,-46 Q-66,-62 -88,10 Q-98,72 -52,76 Q-10,74 -10,20 Z', GR(-58, -14, 100, [0, '#FFD0D8'], [.6, '#FF9DB0'], [1, '#E0687E']), 5.5), P('M12,-46 Q66,-62 88,10 Q98,72 52,76 Q10,74 10,20 Z', GR(42, -14, 100, [0, '#FFD0D8'], [.6, '#FF9DB0'], [1, '#E0687E']), 5.5),
  T('M-30,-22 q-18,14 -20,40 M-36,-6 q-18,4 -30,22 M-30,10 q-8,16 -4,36 M30,-22 q18,14 20,40 M36,-6 q18,4 30,22 M30,10 q8,16 4,36', '#D9506A', 3.5, .7),
  T('M-74,-10 Q-62,-40 -34,-46', '#FFFFFF', 6, .6),
];
S.o_coeur = [
  ...TT('M-8,-30 Q-12,-52 -30,-56', '#5B8FE8', 11), ...TT('M12,-30 Q14,-56 34,-60 M8,-34 Q4,-50 14,-64', '#F0483E', 11),
  P('M0,42 C-52,6 -44,-40 -12,-34 C-2,-34 2,-28 4,-24 C10,-40 46,-40 44,-4 C42,18 20,32 0,42 Z', GR(-12, -14, 60, [0, '#FF9A8A'], [.55, '#F0483E'], [1, '#B5261F']), 5.5),
  T('M-30,-12 Q-28,-26 -14,-28', '#FFFFFF', 6, .7), T('M4,-24 Q0,0 8,22', '#B5261F', 3, .6),
];
S.o_estomac = [
  ...TT('M-22,-62 Q-24,-44 -14,-34', '#FFB08A', 13),
  P('M-22,-36 Q20,-56 44,-22 Q60,12 34,40 Q8,62 -24,48 Q-46,40 -52,56 Q-64,48 -58,30 Q-48,10 -28,16 Q-16,0 -22,-36 Z', GR(6, -10, 80, [0, '#FFD6BE'], [.6, '#FFA47A'], [1, '#E0703E']), 5.5),
  T('M-8,-34 Q20,-38 34,-14', '#FFFFFF', 6, .6), T('M-10,30 q14,8 30,0', '#D9623A', 3, .6),
];
S.o_foie = [
  P('M-70,-14 Q-66,-40 -26,-38 Q30,-40 64,-22 Q78,-12 66,2 Q30,30 -20,34 Q-64,36 -72,10 Q-74,-2 -70,-14 Z', GR(-20, -16, 100, [0, '#D98A6A'], [.6, '#B0563C'], [1, '#7E3422']), 5.5),
  T('M-52,-22 Q-30,-32 0,-30', '#FFC2A8', 6, .6), T('M4,-34 Q-2,-6 6,24', '#7E3422', 3, .5),
];
S.o_intestins = [
  // le gros intestin fait le tour ; le petit se replie au milieu
  T('M-70,34 V-34 Q-70,-52 -52,-52 H52 Q70,-52 70,-34 V30 Q70,50 50,50 H24', N, 34), T('M-70,34 V-34 Q-70,-52 -52,-52 H52 Q70,-52 70,-34 V30 Q70,50 50,50 H24', '#F2A07A', 24),
  T('M-70,26 h0 M-70,4 h0 M-70,-18 h0 M-44,-52 h0 M-18,-52 h0 M8,-52 h0 M34,-52 h0 M70,-22 h0 M70,0 h0 M70,22 h0', '#C9623A', 5, .5),
  T('M-42,-22 H30 Q44,-22 44,-10 Q44,2 30,2 H-30 Q-44,2 -44,14 Q-44,26 -30,26 H20', N, 22), T('M-42,-22 H30 Q44,-22 44,-10 Q44,2 30,2 H-30 Q-44,2 -44,14 Q-44,26 -30,26 H20', '#FFB0C0', 14),
  T('M-36,-26 H26', '#FFFFFF', 4, .6), T('M-76,-30 V20', '#FFD6BE', 5, .6),
];
S.o_vessie = [
  ...TT('M-30,-34 Q-24,-18 -14,-10 M30,-34 Q24,-18 14,-10', '#FFD98A', 8),
  P('M0,-20 Q-40,-18 -38,8 Q-34,32 0,32 Q34,32 38,8 Q40,-18 0,-20 Z', GR(-10, -4, 46, [0, '#FFF6B8'], [.6, '#FFE066'], [1, '#E8B020']), 5.5),
  T('M-24,-4 Q-18,-14 -6,-16', '#FFFFFF', 5, .7),
];
// l'endroit qui attend un organe : une lueur (le jeu dessine autour un rond de tirets qui tourne)
S.c_lueur = [A(ci(0, 0, 100), GR(0, 0, 100, [0, '#FFFFFF', 1], [.5, '#FFF6B8', .7], [1, '#FFE066', 0]))];
S.c_non = [T('M-22,-22 L22,22 M22,-22 L-22,22', N, 16), T('M-22,-22 L22,22 M22,-22 L-22,22', '#F0483E', 9)];

// ═════════════ Les cartes, les petites choses qui s'animent ═════════════
S.c_carte = [A(rr(8, 14, 330, 330, 34), N, .16), P(rr(0, 0, 330, 330, 34), GV(0, 330, '#FFFFFF', '#EAF4FF'), 6), T(rr(12, 12, 306, 306, 24), '$c', 6, .8)];
S.c_petite_carte = [A(rr(6, 10, 300, 250, 30), N, .16), P(rr(0, 0, 300, 250, 30), GV(0, 250, '#FFFFFF', '#EAF4FF'), 6), T(rr(10, 10, 280, 230, 22), '$c', 5, .8)];
S.c_air = [A(ci(0, 0, 10), GR(-3, -3, 12, [0, '#FFFFFF', .95], [1, '#CDEBFF', .5]))];
S.c_pomme = [P('M0,-12 C-22,-22 -26,8 -10,18 C-4,22 4,22 10,18 C26,8 22,-22 0,-12 Z', GR(-6, -4, 26, [0, '#FF9A8A'], [1, '#D9302A']), 3.5), T('M0,-12 q2,-10 8,-12', '#7A5230', 3.5), P('M4,-18 q10,-10 16,-2 q-8,6 -16,2 Z', '#5CB850', 2.5)];
S.c_eclair = [P('M4,-18 L-10,2 L-1,2 L-6,18 L10,-4 L1,-4 Z', '#FFE066', 2.5, '#E8A21C')];
S.c_goutte = [P('M0,-10 q-8,11 0,16 q8,-5 0,-16 Z', '#FFE066', 2.5, '#E8B020')];
S.c_globule = [P(ci(0, 0, 7), GR(-2, -2, 8, [0, '#FF9A8A'], [1, '#D9302A']), 2.5), A(ci(0, 0, 2.6), '#B5261F', .6)];

// Les organes : clé, le nom (avec son article), où il va (x, y), la place qu'il prend (largeur, hauteur), sa profondeur
// (pour le petit effet de relief quand le bonhomme tourne), ce que la voix dit une fois qu'il est en place, sa couleur.
// Les organes : clé, le nom (avec son article), où il va (x, y), la place qu'il prend (largeur, hauteur), sa profondeur
// (les côtes passent devant les poumons et le cœur ; tout glisse un peu quand Oscar tourne), ce que la voix dit une fois
// qu'il est en place, sa couleur, sa taille dans le corps.
const LISTES = { organes: [
  ['cerveau', 'le cerveau', '0', '96', '150', '98', '0.95', "Le cerveau ! C'est lui qui pense, et qui commande tout le corps.", '#FF7AB0', '0.84'],
  ['poumons', 'les poumons', '0', '360', '176', '150', '0.4', "Les poumons ! Ils se remplissent d'air quand on respire.", '#48B0F8', '0.9'],
  ['coeur', 'le cœur', '14', '374', '84', '92', '0.5', 'Le cœur ! Il bat pour envoyer le sang partout. Boum boum !', '#F0483E', '0.86'],
  ['estomac', "l'estomac", '54', '486', '102', '106', '0.36', "L'estomac ! C'est là que va ce qu'on mange.", '#FF9A3D', '0.84'],
  ['foie', 'le foie', '-52', '480', '138', '68', '0.3', 'Le foie ! Il nettoie le sang et garde des forces.', '#B0563C', '0.88'],
  ['intestins', 'les intestins', '0', '556', '150', '94', '0.14', 'Les intestins ! Ils finissent de digérer le repas.', '#F2A07A', '0.82'],
  ['vessie', 'la vessie', '0', '628', '74', '56', '0.2', "La vessie ! Elle garde le pipi jusqu'aux toilettes.", '#FFD23D', '0.82'],
] };

// { n: nom, s: dessin, x, y, e: échelle, r: rotation, c: couleurs }
const SCENES = { salle: [{ n: 'salle', s: 'c_salle', x: 0, y: 0 }] };
const CIELS = { salle: ['#FFF6E2', '#FBE7C2', '#C28E52', 1] };

module.exports = { S, SCENES, CIELS, LISTES };
