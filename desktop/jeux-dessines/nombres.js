// Les dessins des activités de nombres d'« Apprendre ». genere.js en tire Games/NombresArt.cs (pour l'application) ;
// le banc d'essai s'en sert pour produire de vraies images sans Windows. Les outils de dessin sont dans outils.js.
//
// Chaque activité a son décor (1600 × 900) :
//   - « Le panier » : un verger, l'arbre et ses fruits, le panier d'osier à remplir ;
//   - « Un de plus, un de moins » : les oiseaux sur les fils ;
//   - « Partager en deux » : le goûter sur la nappe, avec Nounours ;
//   - « La bande numérique » : le petit train des nombres.
const { N, P, A, T, TT, GL, GR, GV, GH, r1, ci, el, rr, pol, alea } = require('./outils.js');
const communs = require('./communs.js');

const S = {};
communs(S);

// ═════════════ Le paysage du verger ═════════════
const touffe = (x, y, k) => `M${x},${y} q${-8 * k},${-30 * k} ${-16 * k},${-34 * k} q${12 * k},${10 * k} ${14 * k},${24 * k} q${2 * k},${-26 * k} ${10 * k},${-40 * k} q${6 * k},${18 * k} ${4 * k},${40 * k} q${8 * k},${-18 * k} ${18 * k},${-26 * k} q${-6 * k},${20 * k} ${-10 * k},${36 * k} Z`;
const fleur = (x, y, c) => [T(`M${x},${y} v-26`, '#3E8E3A', 4), ...[0, 72, 144, 216, 288].map(a => { const [dx, dy] = pol(a - 90, 9); return A(ci(x + dx, y - 30 + dy, 7), c); }), A(ci(x, y - 30, 5), '#FFD23D')];
S.v_lointain = [
  // les collines
  A('M0,470 Q260,360 560,440 T1040,420 T1600,380 V620 H0 Z', GV(380, 600, '#A8DC8E', '#8ECF78')),
  A('M0,520 Q300,440 640,500 T1240,470 T1600,500 V640 H0 Z', GV(460, 620, '#8ED070', '#74C05C')),
  // les arbres du fond
  ...[[930, 470, 1], [1010, 462, .8], [1470, 452, 1.1], [1380, 462, .7], [60, 492, .9]].flatMap(([x, y, k]) => [
    A(rr(x - 6 * k, y - 20 * k, 12 * k, 50 * k, 4), '#8A6238'), A(ci(x, y - 50 * k, 38 * k), '#5FAE52'), A(ci(x - 20 * k, y - 34 * k, 26 * k), '#5FAE52'), A(ci(x + 22 * k, y - 36 * k, 28 * k), '#6CBA5C'), A(ci(x - 8 * k, y - 62 * k, 16 * k), '#8ED070', .7)]),
  // la barrière
  ...[...Array(11)].map((_, i) => P(rr(820 + i * 78, 500, 16, 74, 6), GV(500, 574, '#F4E6CC', '#D9C4A0'), 4)),
  P(rr(806, 516, 810, 14, 6), GV(516, 530, '#F4E6CC', '#D9C4A0'), 4), P(rr(806, 546, 810, 14, 6), GV(546, 560, '#F4E6CC', '#D9C4A0'), 4),
];
S.v_pre = [
  A('M0,560 Q400,530 800,552 T1600,548 V900 H0 Z', GV(540, 900, '#7CCB5E', '#4EA63E')),
  // des bandes d'herbe plus claires
  A('M0,640 Q500,600 1000,640 T1600,630 V690 Q1100,650 600,690 T0,700 Z', '#9ADB78', .35),
  A('M0,780 Q500,740 1000,780 T1600,770 V830 Q1100,790 600,830 T0,840 Z', '#9ADB78', .28),
  ...[[70, 720, 1], [250, 860, 1.2], [760, 660, .9], [930, 870, 1.1], [1470, 700, 1], [1540, 860, 1.2], [690, 800, .8], [1370, 610, .8]].map(([x, y, k]) => A(touffe(x, y, k), '#3E9A38', .85)),
  ...fleur(110, 800, '#FFFFFF'), ...fleur(840, 720, '#FF9DC0'), ...fleur(1500, 780, '#FFFFFF'), ...fleur(730, 880, '#FFE066'), ...fleur(1420, 880, '#FF9DC0'), ...fleur(40, 640, '#FFE066'),
];

// ═════════════ L'arbre (l'origine est le pied du tronc) ═════════════
const boule = (x, y, r, clair, fonce) => A(ci(x, y, r), GR(x - r * .3, y - r * .4, r * 1.5, [0, clair], [.7, fonce], [1, fonce]));
S.arbre_ombre = [A(el(0, 0, 330, 44), GR(0, 0, 330, [0, '#1E5A24', .5], [.7, '#1E5A24', .25], [1, '#1E5A24', 0]))];
S.arbre = [
  // le tronc et ses branches
  P('M-52,6 Q-30,-70 -40,-170 Q-46,-230 -110,-290 L-84,-306 Q-30,-262 -14,-214 Q-10,-270 -30,-340 L2,-346 Q14,-280 22,-214 Q44,-266 104,-300 L124,-278 Q66,-236 50,-170 Q40,-70 60,6 Q0,22 -52,6 Z', GL(-60, 0, 60, 0, [0, '#6B4422'], [.35, '#A9744A'], [.7, '#8A5A32'], [1, '#5E3A1C']), 7),
  T('M-14,-30 q8,-50 0,-110 M18,-10 q-6,-40 4,-90 M-2,-150 q6,-30 2,-60', '#5E3A1C', 4, .5),
  // le feuillage : de gros paquets ronds, sombres dessous, clairs dessus
  P('M-300,-330 a96,96 0 0 1 20,-150 a110,110 0 0 1 150,-110 a130,130 0 0 1 240,-10 a112,112 0 0 1 160,120 a96,96 0 0 1 10,160 a90,90 0 0 1 -130,70 a110,110 0 0 1 -180,10 a110,110 0 0 1 -170,-10 a90,90 0 0 1 -100,-80 Z', GV(-620, -180, '#58B24A', '#2F7E2E'), 7),
  boule(-190, -470, 96, '#8EDB6A', '#58B24A'), boule(0, -540, 118, '#9AE272', '#5FB84E'), boule(190, -470, 100, '#8EDB6A', '#58B24A'),
  boule(-90, -390, 92, '#7CCE5E', '#4FA844'), boule(110, -380, 96, '#7CCE5E', '#4FA844'), boule(-250, -350, 70, '#6CC054', '#469C3E'), boule(270, -340, 72, '#6CC054', '#469C3E'),
  // des reflets de feuilles
  ...[[-200, -520, 30], [-20, -610, 40], [170, -530, 30], [60, -450, 26], [-120, -430, 24], [250, -390, 22]].map(([x, y, r]) => A(el(x, y, r, r * .6), '#C8F5A0', .45)),
  ...[[-260, -300], [-150, -290], [-20, -280], [120, -286], [240, -300], [-210, -400], [200, -410]].map(([x, y]) => T(`M${x},${y} q14,-14 30,-4`, '#2F7E2E', 5, .5)),
];
// une feuille qui tombe
S.feuille = [P('M0,-14 Q14,-6 0,16 Q-14,-6 0,-14 Z', GV(-14, 16, '#9AE272', '#4FA844'), 2.5, '#2F7E2E'), T('M0,-10 V12', '#2F7E2E', 1.5, .7)];

// ═════════════ Les fruits (l'origine est le centre ; environ 96 de haut) ═════════════
const queue = T('M0,-40 q2,-14 12,-20', '#6B4422', 6);
const feuilleDeFruit = P('M10,-52 Q34,-70 46,-50 Q28,-36 10,-52 Z', GV(-70, -36, '#9AE272', '#4FA844'), 3, '#2F7E2E');
S.fruit_pomme = [
  queue, feuilleDeFruit,
  P('M0,-34 C-22,-54 -58,-32 -50,8 C-44,40 -18,52 0,44 C18,52 44,40 50,8 C58,-32 22,-54 0,-34 Z', GR(-16, -16, 76, [0, '#FF9482'], [.45, '#EC3E32'], [1, '#A81C16']), 5),
  A(el(-22, -14, 13, 18), '#FFFFFF', .45), T('M-8,-33 Q0,-27 8,-33', '#8E1A14', 3, .6),
];
S.fruit_poire = [
  queue, feuilleDeFruit,
  P('M0,-40 C-18,-44 -26,-22 -24,-8 C-52,4 -52,48 0,50 C52,48 52,4 24,-8 C26,-22 18,-44 0,-40 Z', GR(-14, 0, 78, [0, '#F4F7A0'], [.5, '#C6DC4E'], [1, '#86A822']), 5),
  A(el(-18, 10, 12, 18), '#FFFFFF', .4), ...[[14, 22], [22, 4], [6, 34], [-8, 40]].map(([x, y]) => A(ci(x, y, 2.2), '#86A822', .8)),
];
S.fruit_orange = [
  T('M0,-44 v-8', '#6B4422', 6), P('M2,-50 Q24,-66 38,-50 Q22,-38 2,-50 Z', GV(-66, -38, '#9AE272', '#4FA844'), 3, '#2F7E2E'),
  P(ci(0, 2, 48), GR(-16, -14, 78, [0, '#FFD58A'], [.45, '#FF9A2E'], [1, '#D2660C']), 5),
  A(el(-20, -16, 13, 17), '#FFFFFF', .45), A(ci(0, -42, 5), '#6E8E2A'), ...[[18, 14], [26, -6], [6, 28], [-12, 30], [-28, 12], [14, -22]].map(([x, y]) => A(ci(x, y, 1.8), '#D2660C', .6)),
];
S.fruit_citron = [
  feuilleDeFruit,
  P('M-56,4 Q-48,-34 0,-40 Q48,-34 56,4 Q62,10 56,16 Q44,46 0,46 Q-44,46 -56,16 Q-62,10 -56,4 Z', GR(-16, -12, 80, [0, '#FFFBC0'], [.45, '#FFE23D'], [1, '#D9A810']), 5),
  A(el(-20, -12, 15, 14), '#FFFFFF', .5), ...[[16, 18], [28, 0], [0, 28], [-18, 26]].map(([x, y]) => A(ci(x, y, 1.8), '#D9A810', .6)),
];
// la place d'un fruit qui manque dans le panier : un rond en pointillés
S.place = [A(ci(0, 0, 44), '#FFFFFF', .22), ...[...Array(10)].map((_, i) => { const [x1, y1] = pol(i * 36, 44), [x2, y2] = pol(i * 36 + 20, 44); return T(`M${x1},${y1} A44,44 0 0 1 ${x2},${y2}`, '#FFFFFF', 6, .9); })];

// ═════════════ Le panier d'osier (l'origine est le bas du panier, au milieu) ═════════════
const OSIER = (x0, x1) => GL(x0, 0, x1, 0, [0, '#9A6A26'], [.3, '#EBBE6E'], [.6, '#CC9642'], [1, '#8A5A1E']);
const tressage = [];
for (let i = 0; i < 6; i++) tressage.push(T(`M${-214 + i * 7},${-26 - i * 30} Q0,${-2 - i * 30} ${214 - i * 7},${-26 - i * 30}`, '#7E5218', 4, .6));
for (let i = 0; i < 15; i++) tressage.push(T(`M${-182 + i * 26},-14 l${(i - 7) * 5},-176`, '#7E5218', 4, .45));
S.panier_fond = [
  A(el(0, 4, 290, 34), GR(0, 4, 290, [0, '#1E5A24', .55], [.7, '#1E5A24', .25], [1, '#1E5A24', 0])),
  // l'anse
  T('M-206,-196 C-200,-470 200,-470 206,-196', N, 34), T('M-206,-196 C-200,-470 200,-470 206,-196', '#B5803A', 22), T('M-200,-210 C-190,-452 190,-452 200,-210', '#EBBE6E', 6, .8),
  ...[...Array(13)].map((_, i) => { const t = (i + 1) / 14, x = -206 * (1 - t) ** 3 + 3 * -200 * (1 - t) ** 2 * t + 3 * 200 * (1 - t) * t * t + 206 * t ** 3, y = -196 * (1 - t) ** 3 + 3 * -470 * (1 - t) ** 2 * t + 3 * -470 * (1 - t) * t * t + -196 * t ** 3; return T(`M${r1(x - 9)},${r1(y - 9)} l18,18`, '#7E5218', 4, .55); }),
  // l'intérieur, dans l'ombre
  P(el(0, -196, 222, 40), GV(-236, -156, '#5E3A12', '#8A5A1E'), 6),
];
S.panier_devant = [
  P('M-222,-196 Q0,-150 222,-196 L184,-14 Q0,22 -184,-14 Z', OSIER(-222, 222), 7),
  ...tressage,
  A('M120,-180 q60,-8 100,-16 L186,-14 q-30,10 -70,14 Z', '#5E3A12', .22),
  // le bord tressé
  P('M-232,-212 Q0,-166 232,-212 Q238,-190 226,-184 Q0,-138 -226,-184 Q-238,-190 -232,-212 Z', GL(-232, 0, 232, 0, [0, '#A8742E'], [.3, '#F6D08A'], [.65, '#D9A650'], [1, '#9A6A26']), 6),
  ...[...Array(16)].map((_, i) => { const x = -210 + i * 28, y = -203 + 44 * (1 - ((x / 232) ** 2)); return T(`M${x},${r1(y - 8)} l14,18`, '#7E5218', 4, .55); }),
];

// ═════════════ Une pancarte à chiffre (l'origine est le milieu de la planche) ═════════════
S.pancarte = [
  A(el(0, 196, 70, 12), GR(0, 196, 70, [0, '#1E5A24', .5], [1, '#1E5A24', 0])),
  P(rr(-13, 60, 26, 136, 6), GL(-13, 0, 13, 0, [0, '#8A5A32'], [.4, '#C9955E'], [1, '#6B4422']), 6),
  P(rr(-92, -92, 184, 176, 22), GV(-92, 84, '#FFF4DC', '#F2D9A8'), 7),
  T('M-74,-52 q70,-10 148,0 M-76,0 q74,8 152,0 M-74,50 q70,-8 148,0', '#D9B878', 3, .5),
  A(ci(-70, -70, 6), '#B58A4A'), A(ci(70, -70, 6), '#B58A4A'), A(ci(-70, 62, 6), '#B58A4A'), A(ci(70, 62, 6), '#B58A4A'),
];
// le halo de la pancarte choisie, et son voile quand ce n'est pas la bonne
S.pancarte_halo = [A(rr(-112, -112, 224, 216, 36), GR(0, -4, 150, [0, '#FFF6A8', .9], [.7, '#FFE066', .5], [1, '#FFE066', 0]))];
S.pancarte_voile = [A(rr(-92, -92, 184, 176, 22), '#5A6684', .55)];

// ═════════════ La vie du verger ═════════════
S.papillon_aile = [P('M0,0 C-10,-34 -46,-38 -44,-10 C-42,6 -20,8 0,0 Z', GR(-24, -16, 40, [0, '$clair'], [1, '$c']), 2.5), P('M0,2 C-8,26 -34,30 -32,12 C-30,2 -14,0 0,2 Z', GR(-18, 14, 30, [0, '$clair'], [1, '$c']), 2.5), A(ci(-28, -16, 6), '#FFFFFF', .7)];
S.papillon_corps = [P(el(0, 0, 4, 16), '#3A3454', 2), T('M-1,-14 q-6,-12 -12,-14 M1,-14 q6,-12 12,-14', '#3A3454', 2.5)];

// ═════════════ « Un de plus, un de moins » : les oiseaux sur le fil ═════════════
// un oiseau tout rond (l'origine est là où il se pose) ; il prend les couleurs « c », « clair » et « fonce »
const plumage = () => GR(-14, -68, 84, [0, '$clair'], [.55, '$c'], [1, '$fonce']);
const corpsOiseau = [
  P('M-32,-38 Q-70,-46 -88,-24 Q-64,-12 -38,-18 Z', '$fonce', 4),
  P(el(0, -52, 50, 46), plumage(), 5),
  A(el(10, -38, 30, 27), '#FFF8E8', .9),
  P('M44,-62 L68,-55 L44,-46 Z', GV(-62, -46, '#FFC84A', '#F2901C'), 3.5),
  A(ci(22, -68, 10), '#FFFFFF'), A(ci(25, -68, 5.5), N), A(ci(27, -70, 2), '#FFFFFF'),
  A(ci(30, -50, 7), '#FF8FA8', .55),
  T('M-6,-96 q2,-14 16,-12 M4,-97 q8,-10 18,-4', '$fonce', 5),
];
const pattes = [T('M-10,-10 v10 M12,-10 v10', '#E8901C', 5), T('M-18,0 h16 M4,0 h16', '#E8901C', 5)];
S.oiseau_pose = [...pattes, ...corpsOiseau, P('M-30,-62 Q6,-76 20,-48 Q0,-24 -28,-36 Z', GV(-76, -24, '$c', '$fonce'), 4), T('M-18,-52 q14,-4 26,4 M-18,-42 q12,-2 20,4', '$fonce', 3, .6)];
S.oiseau_vol_a = [...corpsOiseau, P('M-22,-64 Q-58,-140 -4,-158 Q26,-118 16,-64 Z', GV(-158, -64, '$clair', '$c'), 4), T('M-18,-96 q8,-30 14,-44 M-2,-90 q6,-22 8,-40', '$fonce', 3, .6)];
S.oiseau_vol_b = [...corpsOiseau, P('M-24,-58 Q-70,-22 -34,16 Q10,-6 14,-52 Z', GV(-58, 16, '$c', '$fonce'), 4), T('M-28,-30 q8,14 4,30 M-14,-34 q10,10 8,26', '$fonce', 3, .6)];
// un poteau et ses deux traverses (l'origine est le pied)
const POTEAU_BOIS = (x0, x1) => GL(x0, 0, x1, 0, [0, '#6B4422'], [.35, '#B5804E'], [.7, '#8A5A32'], [1, '#5E3A1C']);
S.poteau = [
  A(el(0, 2, 60, 12), GR(0, 2, 60, [0, '#1E5A24', .5], [1, '#1E5A24', 0])),
  P(rr(-17, -500, 34, 502, 10), POTEAU_BOIS(-17, 17), 6), T('M-4,-460 v430', '#5E3A1C', 3, .4),
  P(rr(-96, -380, 192, 24, 9), GV(-380, -356, '#B5804E', '#7E4E22'), 6), P(rr(-96, -200, 192, 24, 9), GV(-200, -176, '#B5804E', '#7E4E22'), 6),
  ...[-78, 78].flatMap(x => [P(rr(x - 9, -402, 18, 24, 6), '#DCE6F4', 4), P(rr(x - 9, -222, 18, 24, 6), '#DCE6F4', 4)]),
];
// une note de musique, quand les oiseaux chantent
S.note = [P('M-10,14 a12,9 0 1 0 14,-6 V-30 l22,-8 v12 l-14,5 V8', GV(-38, 22, '#D0A8FF', '#9A5CF0'), 3.5)];

// ═════════════ « Partager en deux » : le goûter sur la nappe ═════════════
require('./peluches.js')(S);      // le nounours invité
const carreaux = [];
for (let i = 0; i < 9; i++) { const xh = 150 + i * 162.5, xb = -40 + i * 210; if (i % 2 === 0) carreaux.push(A(`M${xh},452 h81 L${xb + 105},920 h-105 Z`, '#FFFFFF', .3)); }
for (let k = 0; k < 5; k++) { const y0 = 452 + [0, 62, 150, 262, 398][k], y1 = y0 + [28, 40, 52, 64, 70][k], g = y => 150 - (y - 452) * 190 / 468, d = y => 1450 + (y - 452) * 190 / 468; carreaux.push(A(`M${r1(g(y0))},${y0} H${r1(d(y0))} L${r1(d(y1))},${y1} H${r1(g(y1))} Z`, '#FFFFFF', .3)); }
S.g_nappe = [
  A('M110,470 H1490 L1680,930 H-80 Z', '#2F7A2E', .35),
  P('M150,452 H1450 L1640,920 H-40 Z', GV(452, 900, '#F5655C', '#D9382E'), 6),
  ...carreaux,
  T('M150,452 H1450', '#FFD0C8', 5, .8),
];
// une assiette (l'origine est son centre)
S.assiette = [
  A(el(0, 12, 214, 60), GR(0, 12, 214, [0, '#6B1A14', .4], [1, '#6B1A14', 0])),
  P(el(0, 0, 200, 60), GV(-60, 60, '#FFFFFF', '#DCE6F4'), 6),
  T(el(0, -3, 152, 42), '#B8C8E0', 4, .9), A(el(0, -2, 146, 39), GV(-40, 40, '#F2F6FC', '#FFFFFF')),
  T('M-150,-18 q40,-22 110,-28', '#FFFFFF', 6, .9),
];
// un petit gâteau (l'origine est le bas ; 92 de large, 112 de haut)
S.gateau = [
  A(el(0, 0, 44, 8), GR(0, 0, 44, [0, '#6B1A14', .35], [1, '#6B1A14', 0])),
  P('M-36,-52 H36 L27,0 Q0,7 -27,0 Z', GL(-36, 0, 36, 0, [0, '#E05C9A'], [.3, '#FFB0D0'], [.6, '#F27AB0'], [1, '#C2407E']), 5),
  T('M-24,-50 l5,48 M-12,-50 l3,52 M0,-50 v54 M12,-50 l-3,52 M24,-50 l-5,48', '#C2407E', 3, .6),
  P('M-44,-50 Q-52,-74 -30,-78 Q-34,-100 -8,-98 Q0,-118 20,-100 Q44,-100 36,-78 Q54,-70 44,-50 Q0,-38 -44,-50 Z', GR(-12, -86, 70, [0, '#FFFFFF'], [.6, '#FFF1D6'], [1, '#EBCB98']), 5),
  T('M-30,-66 Q0,-56 30,-68 M-18,-86 Q4,-78 22,-88', '#D9B67E', 3, .6),
  P(ci(4, -108, 11), GR(0, -112, 14, [0, '#FF9482'], [1, '#D92A22']), 3.5), T('M6,-118 q4,-10 12,-12', '#6B4422', 3),
  A(ci(1, -111, 3), '#FFFFFF', .8),
  ...[[-26, -62, '#48A0F8'], [16, -60, '#5CC86A'], [-6, -72, '#FFC12E'], [28, -74, '#B07AF2'], [-22, -84, '#FF8A4A']].map(([x, y, c]) => T(`M${x},${y} l6,3`, c, 4)),
];
// le carton du prénom, posé derrière l'assiette de l'enfant (l'origine est le bas du carton)
S.carton = [
  A(el(0, 2, 150, 14), GR(0, 2, 150, [0, '#6B1A14', .35], [1, '#6B1A14', 0])),
  P('M-150,0 L-132,-96 H132 L150,0 Z', GV(-96, 0, '#FFFFFF', '#E6ECF6'), 6), T('M-124,-84 H124', '#FFD23D', 7), T('M-136,-12 H136', '#FFD23D', 7),
];

// ═════════════ « La bande numérique » : le petit train des nombres ═════════════
const TOLE = (x0, x1, a, b, c, d) => GL(x0, 0, x1, 0, [0, d], [.25, b], [.6, a], [1, d]);
S.t_rails = [
  A('M0,-6 H1600 V44 H0 Z', GV(-6, 44, '#C9B48E', '#A8926A')),
  ...[...Array(28)].map((_, i) => P(rr(i * 60 - 20, 2, 34, 26, 5), GV(2, 28, '#8A5A32', '#5E3A1C'), 3)),
  T('M0,0 H1600', '#2A1C4E', 12), T('M0,0 H1600', '#B8C4DC', 6), T('M0,-2 H1600', '#FFFFFF', 2, .8),
];
// un wagon (l'origine est le bas du wagon, au milieu ; 124 de large) aux couleurs « c », « clair », « fonce »
S.wagon = [
  T('M-76,-30 H76', '#3A3454', 9),
  P(rr(-58, -150, 116, 118, 16), GL(-58, 0, 58, 0, [0, '$fonce'], [.2, '$clair'], [.55, '$c'], [1, '$fonce']), 6),
  P(rr(-66, -160, 132, 22, 9), GV(-160, -138, '$clair', '$c'), 5),
  P(rr(-64, -40, 128, 20, 8), GV(-40, -20, '#5A6684', '#3A3454'), 5),
  T('M-46,-128 V-48 M46,-128 V-48', '$fonce', 4, .5), A(rr(-44, -132, 14, 60, 6), '#FFFFFF', .22),
];
S.wagon_halo = [A(rr(-92, -186, 184, 190, 40), GR(0, -92, 130, [0, '#FFF6A8', .95], [.65, '#FFD23D', .55], [1, '#FFD23D', 0]))];
// la locomotive (l'origine est le bas, au milieu ; 230 de large), tournée vers la droite
S.loco = [
  T('M-116,-30 H96', '#3A3454', 9),
  // la cabine
  P(rr(-110, -196, 92, 164, 14), GL(-110, 0, -18, 0, [0, '#1E4FA8'], [.3, '#7FC0FF'], [.6, '#3F8EF2'], [1, '#1E4FA8']), 6),
  P(rr(-94, -176, 60, 56, 10), GV(-176, -120, '#DDF2FF', '#9AD0FF'), 5), T('M-84,-166 l22,0', '#FFFFFF', 5, .9),
  P(rr(-124, -212, 120, 24, 10), GV(-212, -188, '#FFE066', '#F2A51C'), 6),
  // la chaudière
  P('M-22,-140 H70 Q104,-140 104,-106 V-40 H-22 Z', GV(-140, -40, '#FF7A6C', '#C2281E'), 6),
  T('M10,-138 V-42 M52,-138 V-42', '#8E1A14', 5, .55), A('M-16,-132 H70 Q92,-130 96,-112 H-16 Z', '#FFFFFF', .28),
  P(rr(14, -166, 34, 30, 12), GV(-166, -136, '#FFE98A', '#F2A51C'), 5),
  // la cheminée
  P('M58,-140 L54,-196 H44 V-212 H98 V-196 H88 L84,-140 Z', GV(-212, -140, '#5A6684', '#2A2440'), 6),
  // le phare et le chasse-pierres
  P(ci(104, -92, 15), GR(100, -96, 18, [0, '#FFFFFF'], [1, '#FFE066']), 5),
  P('M96,-44 L132,-18 H88 Z', GV(-44, -18, '#FFE066', '#F2A51C'), 5),
  P(rr(-116, -42, 216, 22, 8), GV(-42, -20, '#5A6684', '#3A3454'), 5),
];
S.roue = [P(ci(0, 0, 22), GR(-5, -6, 26, [0, '#8793AD'], [1, '#3A3454']), 5), A(ci(0, 0, 13), '#DCE6F4'), T('M-13,0 H13 M0,-13 V13 M-9,-9 L9,9 M-9,9 L9,-9', '#5A6684', 3), P(ci(0, 0, 5), '#F2A51C', 2.5)];
S.fumee = [A(ci(0, 0, 34), GR(-8, -8, 40, [0, '#FFFFFF', .95], [.6, '#E6ECF6', .7], [1, '#E6ECF6', 0]))];

// ═════════════ Les scènes ═════════════
const SCENES = {
  verger: [
    { n: 'lointain', s: 'v_lointain', x: 0, y: 0 }, { n: 'pre', s: 'v_pre', x: 0, y: 0 },
    { n: 'arbre_ombre', s: 'arbre_ombre', x: 400, y: 648 }, { n: 'arbre', s: 'arbre', x: 380, y: 640 },
  ],
  fil: [
    { n: 'lointain', s: 'v_lointain', x: 0, y: 0 }, { n: 'pre', s: 'v_pre', x: 0, y: 0 },
    { n: 'poteau_g', s: 'poteau', x: 110, y: 668 }, { n: 'poteau_d', s: 'poteau', x: 1490, y: 668 },
  ],
  gouter: [
    { n: 'lointain', s: 'v_lointain', x: 0, y: 0 }, { n: 'pre', s: 'v_pre', x: 0, y: 0 }, { n: 'nappe', s: 'g_nappe', x: 0, y: 0 },
  ],
  train: [
    { n: 'lointain', s: 'v_lointain', x: 0, y: 0 }, { n: 'pre', s: 'v_pre', x: 0, y: 0 }, { n: 'rails', s: 't_rails', x: 0, y: 612 },
  ],
};
const CIELS = { verger: ['#6CBFFF', '#E4F6FF', '#4EA63E', 1], fil: ['#6CBFFF', '#E4F6FF', '#4EA63E', 1], gouter: ['#6CBFFF', '#E4F6FF', '#4EA63E', 1], train: ['#6CBFFF', '#E4F6FF', '#4EA63E', 1] };
const LISTES = {
  // les fruits : le dessin, un, plusieurs, féminin
  fruits: [['fruit_pomme', 'pomme', 'pommes'], ['fruit_poire', 'poire', 'poires'], ['fruit_orange', 'orange', 'oranges'], ['fruit_citron', 'citron', 'citrons']],
  // les plumages des oiseaux : couleur, clair, foncé
  plumages: [['#48A0F8', '#C4E6FF', '#1E68C6'], ['#F25F5C', '#FFC4BC', '#B02A26'], ['#FFC12E', '#FFF2A8', '#D98A0E'], ['#5CC86A', '#C8F6C0', '#2E8E3E'], ['#B07AF2', '#E6D4FF', '#6A3CC6'], ['#FF8A4A', '#FFD6B0', '#C2560C']],
  // les couleurs des wagons : couleur, clair, foncé
  wagons: [['#F2A51C', '#FFE9A8', '#B5780C'], ['#3DBA62', '#C0F4C8', '#1E7A3A'], ['#3F8EF2', '#C4E2FF', '#1E4FA8'], ['#B07AF2', '#E6D4FF', '#6A3CC6'], ['#F25F9A', '#FFC8E0', '#B02A66']],
  // là où pendent les fruits à cueillir (dans la scène)
  branches: [[170, 330], [300, 172], [432, 330], [562, 172], [652, 362]],
};

module.exports = { S, SCENES, CIELS, LISTES };
