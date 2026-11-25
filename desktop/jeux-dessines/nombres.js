// Les dessins des activités de nombres d'« Apprendre ». genere.js en tire Games/NombresArt.cs (pour l'application) ;
// le banc d'essai s'en sert pour produire de vraies images sans Windows. Les outils de dessin sont dans outils.js.
//
// Chaque activité a son décor (1600 × 900) :
//   - « Le panier » : un verger, l'arbre et ses fruits, le panier d'osier à remplir.
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

// ═════════════ Les scènes ═════════════
const SCENES = {
  verger: [
    { n: 'lointain', s: 'v_lointain', x: 0, y: 0 }, { n: 'pre', s: 'v_pre', x: 0, y: 0 },
    { n: 'arbre_ombre', s: 'arbre_ombre', x: 400, y: 648 }, { n: 'arbre', s: 'arbre', x: 380, y: 640 },
  ],
};
const CIELS = { verger: ['#6CBFFF', '#E4F6FF', '#4EA63E', 1] };
const LISTES = {
  // les fruits : le dessin, un, plusieurs, féminin
  fruits: [['fruit_pomme', 'pomme', 'pommes'], ['fruit_poire', 'poire', 'poires'], ['fruit_orange', 'orange', 'oranges'], ['fruit_citron', 'citron', 'citrons']],
  // là où pendent les fruits à cueillir (dans la scène)
  branches: [[170, 330], [300, 172], [432, 330], [562, 172], [652, 362]],
};

module.exports = { S, SCENES, CIELS, LISTES };
