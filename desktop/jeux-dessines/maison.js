// Les dessins de l'activité « Range la maison » (Apprendre). genere.js en tire Games/MaisonArt.cs (pour l'application) ;
// le banc d'essai s'en sert pour produire de vraies images sans Windows. Les outils de dessin sont dans outils.js.
//
// Une maison de poupée vue en coupe, sur le pré : la cuisine, la salle de bain et la chambre côte à côte sous le toit.
// Chaque pièce est dessinée dans son propre repère (480 de large, 430 de haut : le mur jusqu'à 380, puis le sol) ;
// la maison elle-même (toit, murs, soubassement) est dans le repère de la scène (1600 × 900).
// Les objets à ranger ne sont pas ici : ce sont les images de la banque d'images de l'application.
const { N, P, A, T, TT, GL, GR, GV, GH, r1, ci, el, rr, pol, alea } = require('./outils.js');
const communs = require('./communs.js');

const S = {};
communs(S);
require('./paysage.js')(S);

const BOIS = (y0, y1, clair = '#D9A066', fonce = '#A8713A') => GV(y0, y1, clair, fonce);
const ciel = (y0, y1) => GV(y0, y1, '#9AD8FF', '#E8F8FF');

// ═════════════ La cuisine ═════════════
const rayures = []; for (let x = 20; x < 480; x += 40) rayures.push(T(`M${x},0 V380`, '#FFD98A', 6, .45));
const damier = []; for (let i = 0; i < 12; i++) for (let j = 0; j < 2; j++) if ((i + j) % 2 === 0) damier.push(A(`M${i * 40},${380 + j * 25} h40 v25 h-40 Z`, '#E8A070', .85));
S.m_cuisine = [
  A('M0,0 H480 V380 H0 Z', GV(0, 380, '#FFF6D8', '#FFE8A8')), ...rayures,
  A('M0,380 H480 V430 H0 Z', '#F6DEB8'), ...damier, T('M0,380 H480', '#B5773A', 5, .7),
  // la fenêtre et son rideau
  P(rr(272, 56, 150, 126, 10), ciel(56, 182), 6), T('M347,56 V182 M272,119 H422', '#FFFFFF', 7), T('M347,56 V182 M272,119 H422', N, 2, .35),
  A(ci(390, 86, 16), '#FFE066'), A('M276,160 q30,-26 70,-6 q36,-14 72,6 v18 h-142 Z', '#8ED070', .9),
  P('M258,40 h178 v30 q-22,14 -44,0 q-23,14 -45,0 q-22,14 -44,0 q-23,14 -45,0 Z', GV(40, 80, '#F5655C', '#D9382E'), 5), T('M258,54 h178', '#FFFFFF', 4, .6),
  // la suspension
  T('M150,0 V44', '#3A3454', 5), P('M112,88 Q150,26 188,88 Z', GV(40, 88, '#FFE066', '#F2A51C'), 5), P(ci(150, 92, 11), '#FFF6C4', 3),
  // l'étagère et ses bocaux
  P(rr(156, 168, 96, 12, 4), BOIS(168, 180), 4),
  P(rr(164, 128, 24, 40, 6), '#FF9A8A', 4), P(rr(194, 136, 24, 32, 6), '#9AD0FF', 4), P(rr(224, 124, 22, 44, 6), '#B8F0C0', 4),
  A(rr(162, 122, 28, 8, 3), '#7E5218'), A(rr(192, 130, 28, 8, 3), '#7E5218'), A(rr(222, 118, 26, 8, 3), '#7E5218'),
  // le frigo
  P(rr(22, 136, 112, 244, 16), GL(22, 0, 134, 0, [0, '#9CC8E8'], [.3, '#EAF6FF'], [1, '#A8D0EC']), 6),
  T('M22,226 H134', N, 4, .7), T('M40,160 v44 M40,246 v70', '#6C8FB0', 7), A(rr(100, 150, 16, 60, 8), '#FFFFFF', .6),
  // le meuble : le plan de travail, les portes, le four
  P(rr(150, 270, 312, 110, 8), BOIS(270, 380, '#E3B27A', '#B5773A'), 6),
  P(rr(162, 290, 84, 80, 8), BOIS(290, 370, '#F0C896', '#C98F4E'), 4), P(rr(256, 290, 84, 80, 8), BOIS(290, 370, '#F0C896', '#C98F4E'), 4),
  A(ci(234, 330, 6), '#7E5218'), A(ci(268, 330, 6), '#7E5218'),
  P(rr(352, 284, 98, 88, 10), GV(284, 372, '#5A6684', '#2A2440'), 5), P(rr(364, 306, 74, 52, 8), GV(306, 358, '#3A3454', '#FFB84A'), 4), T('M366,296 h70', '#DCE6F4', 5),
  P(rr(142, 256, 328, 20, 7), GV(256, 276, '#FFFFFF', '#DCE6F4'), 5),
  // le robinet, la casserole sur le feu
  T('M214,256 V226 Q214,208 236,212', N, 13), T('M214,256 V226 Q214,208 236,212', '#C9D3E8', 7),
  T('M356,238 H322', N, 13), T('M356,238 H322', '#8A5A32', 7),
  P(rr(356, 220, 88, 38, 8), GL(356, 0, 444, 0, [0, '#8793AD'], [.3, '#F4F7FF'], [1, '#8793AD']), 5), P(rr(350, 210, 100, 14, 7), '#C9D3E8', 5), P(ci(400, 206, 8), '#3A3454', 3),
];

// ═════════════ La salle de bain ═════════════
const carrelage = []; for (let x = 48; x < 480; x += 48) carrelage.push(T(`M${x},120 V380`, '#FFFFFF', 4, .7)); for (let y = 168; y < 380; y += 48) carrelage.push(T(`M0,${y} H480`, '#FFFFFF', 4, .7));
const dalles = []; for (let i = 0; i < 12; i++) for (let j = 0; j < 2; j++) if ((i + j) % 2 === 0) dalles.push(A(`M${i * 40},${380 + j * 25} h40 v25 h-40 Z`, '#5A9AD0', .7));
S.m_bain = [
  A('M0,0 H480 V380 H0 Z', GV(0, 380, '#EAF8FF', '#C4E6FA')), A('M0,120 H480 V380 H0 Z', '#A8D8F4', .55), ...carrelage,
  T('M0,120 H480', '#48A0F8', 10, .8),
  A('M0,380 H480 V430 H0 Z', '#8EC4E8'), ...dalles, T('M0,380 H480', '#3A78B4', 5, .7),
  // le hublot
  P(ci(396, 72, 44), ciel(28, 116), 6), T('M396,28 V116 M352,72 H440', '#FFFFFF', 6), A(ci(378, 56, 9), '#FFFFFF', .8),
  // le porte-serviette
  T('M40,150 H180', N, 12), T('M40,150 H180', '#C9D3E8', 6),
  P(rr(64, 148, 84, 100, 8), GV(148, 248, '#FFB0D0', '#F27AB0'), 5), T('M64,176 h84 M64,220 h84', '#FFFFFF', 6, .8),
  // la baignoire, sa mousse, le canard
  P('M34,268 H306 Q306,350 256,356 H84 Q34,350 34,268 Z', GV(268, 356, '#FFFFFF', '#D6E4F4'), 6),
  P(rr(92, 352, 22, 28, 8), '#C9D3E8', 5), P(rr(226, 352, 22, 28, 8), '#C9D3E8', 5),
  ...[[70, 248, 26], [112, 236, 30], [160, 244, 26], [206, 234, 30], [250, 246, 26], [136, 256, 22], [184, 258, 22], [228, 258, 20]].map(([x, y, r]) => P(ci(x, y, r), GR(x - r * .3, y - r * .3, r * 1.4, [0, '#FFFFFF'], [1, '#DCEFFF']), 3, '#9CC6EA')),
  P(rr(24, 256, 292, 22, 11), GV(256, 278, '#FFFFFF', '#DCE6F4'), 6),
  T('M300,256 V214 Q300,196 280,198', N, 13), T('M300,256 V214 Q300,196 280,198', '#C9D3E8', 7),
  P(el(64, 240, 24, 18), GR(58, 234, 30, [0, '#FFF29A'], [1, '#F2B92C']), 4), P(ci(84, 220, 14), GR(80, 216, 18, [0, '#FFF29A'], [1, '#F2B92C']), 4), P('M96,220 l14,3 l-14,5 Z', '#FF9A2E', 3), A(ci(87, 217, 2.6), N),
  // le lavabo et son miroir
  P(el(388, 178, 48, 58), GV(120, 236, '#F4FBFF', '#BFE3FA'), 7), T('M364,150 q10,-18 30,-22', '#FFFFFF', 6, .9),
  P(rr(370, 300, 36, 80, 8), GV(300, 380, '#FFFFFF', '#D6E4F4'), 5),
  P('M330,270 H446 Q446,308 388,310 Q330,308 330,270 Z', GV(270, 310, '#FFFFFF', '#D6E4F4'), 6),
  T('M388,270 V252 Q388,240 400,242', N, 11), T('M388,270 V252 Q388,240 400,242', '#C9D3E8', 6),
  // le tapis de bain
  P(el(170, 406, 112, 16), GV(390, 422, '#FFB0D0', '#F27AB0'), 4),
];

// ═════════════ La chambre ═════════════
const semisEtoiles = []; for (let i = 0; i < 16; i++) { const x = r1(20 + alea(i * 3 + 4) * 440), y = r1(16 + alea(i * 7 + 2) * 340), k = r1(.3 + alea(i + 5) * .3); semisEtoiles.push(A(`M${x},${r1(y - 20 * k)} Q${r1(x + 3 * k)},${r1(y - 3 * k)} ${r1(x + 20 * k)},${y} Q${r1(x + 3 * k)},${r1(y + 3 * k)} ${x},${r1(y + 20 * k)} Q${r1(x - 3 * k)},${r1(y + 3 * k)} ${r1(x - 20 * k)},${y} Q${r1(x - 3 * k)},${r1(y - 3 * k)} ${x},${r1(y - 20 * k)} Z`, '#FFFFFF', .7)); }
S.m_chambre = [
  A('M0,0 H480 V380 H0 Z', GV(0, 380, '#F2E8FF', '#DCCBFA')), ...semisEtoiles,
  A('M0,380 H480 V430 H0 Z', BOIS(380, 430)), T('M60,380 l-14,50 M150,380 l-8,50 M240,380 v50 M330,380 l8,50 M420,380 l14,50', '#8A5A32', 3, .5), T('M0,380 H480', '#7E4E22', 5, .7),
  // la fenêtre sur la nuit
  P(rr(176, 52, 132, 122, 10), GV(52, 174, '#3B2C86', '#7A6AD6'), 6), T('M242,52 V174 M176,113 H308', '#FFFFFF', 7), T('M242,52 V174 M176,113 H308', N, 2, .35),
  A('M286,70 a18,18 0 1 0 6,34 a14,14 0 1 1 -6,-34 Z', '#FFE98A'), A(ci(204, 78, 3), '#FFFFFF'), A(ci(222, 140, 2.5), '#FFFFFF'), A(ci(276, 148, 3), '#FFFFFF'),
  P('M162,38 h160 v26 q-20,12 -40,0 q-20,12 -40,0 q-20,12 -40,0 q-20,12 -40,0 Z', GV(38, 76, '#9A6CF0', '#6A3CC6'), 5),
  // la suspension étoile
  T('M96,0 V40', '#3A3454', 5), P('M96,40 L106,62 L130,64 L112,80 L118,104 L96,92 L74,104 L80,80 L62,64 L86,62 Z', GR(92, 66, 40, [0, '#FFF6A8'], [1, '#F2B92C']), 4, '#B5780C'),
  // l'étagère aux livres
  P(rr(44, 196, 104, 12, 4), BOIS(196, 208), 4),
  P(rr(52, 150, 16, 46, 3), '#F2473C', 3.5), P(rr(70, 158, 16, 38, 3), '#3F8EF2', 3.5), P(rr(88, 146, 18, 50, 3), '#FFC12E', 3.5), P(rr(108, 160, 16, 36, 3), '#3DBA62', 3.5),
  // l'armoire
  P(rr(342, 116, 118, 264, 12), BOIS(116, 380, '#E3B27A', '#B5773A'), 6), T('M401,128 V370', '#7E4E22', 4), A(ci(390, 250, 6), '#7E5218'), A(ci(412, 250, 6), '#7E5218'),
  P(rr(334, 106, 134, 18, 8), BOIS(106, 124, '#F0C896', '#C98F4E'), 5), A(rr(354, 132, 14, 110, 7), '#FFFFFF', .25),
  // le lit
  P(rr(24, 232, 34, 148, 12), BOIS(232, 380, '#E3B27A', '#B5773A'), 6), P(rr(300, 300, 26, 80, 10), BOIS(300, 380, '#E3B27A', '#B5773A'), 6),
  P(rr(44, 310, 270, 54, 12), GV(310, 364, '#FFFFFF', '#DCE6F4'), 5),
  P(rr(54, 284, 86, 46, 20), GV(284, 330, '#FFFFFF', '#E6ECF6'), 5),
  P('M128,300 H312 Q318,300 318,308 V366 H128 Q118,332 128,300 Z', GV(300, 366, '#8EC4F8', '#4A8EE0'), 5),
  ...[[170, 322], [224, 344], [272, 320], [198, 350], [290, 350]].map(([x, y]) => A(`M${x},${y - 9} L${x + 3},${y - 3} L${x + 9},${y - 2} L${x + 4},${y + 3} L${x + 6},${y + 9} L${x},${y + 5} L${x - 6},${y + 9} L${x - 4},${y + 3} L${x - 9},${y - 2} L${x - 3},${y - 3} Z`, '#FFF6A8')),
  // le tapis
  P(el(196, 408, 124, 16), GV(392, 424, '#FFD0A0', '#FF9A5C'), 4),
];

// ═════════════ La lumière d'une pièce, son ombre, le cadre de celle qu'on regarde (repère de la pièce) ═════════════
S.m_halo = [A('M0,0 H480 V430 H0 Z', GR(240, 70, 430, [0, '#FFF6C4', .8], [.55, '#FFE98A', .3], [1, '#FFE98A', 0]))];
S.m_ombre = [A('M0,0 H480 V430 H0 Z', '#1B1238', .42)];
S.m_cadre = [T(rr(7, 7, 466, 416, 14), '#2A1C4E', 20), T(rr(7, 7, 466, 416, 14), '#FFD23D', 12)];

// ═════════════ La maison (repère de la scène) : le toit, les murs, le soubassement ═════════════
const tuiles = [];
for (let k = 0; k < 4; k++) { const y = 70 + k * 34, g = 6 + (200 - y) * 244 / 170, d = 1594 - (200 - y) * 244 / 170; let dd = `M${r1(g)},${y}`; for (let x = g; x < d; x += 46) dd += ` q23,16 46,0`; tuiles.push(T(dd, '#8E1A14', 4, .45)); }
S.m_cheminee = [
  P(rr(1150, 8, 82, 110, 6), GV(8, 118, '#D98A5C', '#A8563A'), 6), T('M1150,40 h82 M1150,72 h82 M1190,8 v32 M1170,40 v32 M1212,40 v32', '#7E3A22', 3, .55),
  P(rr(1140, 0, 102, 20, 6), GV(0, 20, '#F0C896', '#C98F4E'), 5),
];
S.m_maison = [
  // l'ombre de la maison sur le pré
  A(el(800, 668, 800, 30), GR(800, 668, 800, [0, '#1E5A24', .5], [.8, '#1E5A24', .2], [1, '#1E5A24', 0])),
  // le soubassement de pierre
  P(rr(40, 640, 1520, 30, 8), GV(640, 670, '#DCE2EC', '#A8B2C4'), 6), T('M200,640 v30 M420,640 v30 M640,640 v30 M860,640 v30 M1080,640 v30 M1300,640 v30', '#8793AD', 3, .6),
  // les murs et les cloisons
  P(rr(52, 196, 20, 448, 5), GV(196, 644, '#FFF4DC', '#E8D4A8'), 6), P(rr(1528, 196, 20, 448, 5), GV(196, 644, '#FFF4DC', '#E8D4A8'), 6),
  P(rr(548, 204, 14, 440, 4), GV(204, 644, '#FFF4DC', '#E8D4A8'), 5), P(rr(1038, 204, 14, 440, 4), GV(204, 644, '#FFF4DC', '#E8D4A8'), 5),
  // le toit et ses tuiles
  P('M6,206 L250,30 H1350 L1594,206 Z', GV(30, 206, '#F0705E', '#B5382E'), 7),
  ...tuiles,
  A('M240,36 H1360 L1372,46 H228 Z', '#FFFFFF', .25),
  P(rr(30, 192, 1540, 22, 8), GV(192, 214, '#FFF4DC', '#E0C898'), 6),
  // l'œil-de-bœuf
  P(ci(800, 112, 42), ciel(70, 154), 7), T('M800,70 V154 M758,112 H842', '#FFFFFF', 6),
];
// la plaque du nom d'une pièce (l'origine est son milieu)
S.m_plaque = [P(rr(-140, -28, 280, 56, 18), GV(-28, 28, '#5A3A1C', '#3A2408'), 5)];
// le tapis où l'objet attend d'être rangé (l'origine est son milieu) et la lumière qui l'éclaire
S.m_tapis = [
  A(el(0, 10, 230, 40), GR(0, 10, 230, [0, '#1E5A24', .5], [1, '#1E5A24', 0])),
  P(el(0, 0, 210, 44), GR(-40, -12, 220, [0, '#C8A8FF'], [.7, '#8E5CF0'], [1, '#6A3CC6']), 6), T(el(0, -2, 168, 32), '#FFE066', 5, .9),
];
S.m_projecteur = [A('M-90,-330 H90 L210,0 H-210 Z', GL(0, -330, 0, 0, [0, '#FFF6C4', 0], [1, '#FFF6C4', .45]))];
// des buissons devant la maison
S.m_buisson = [
  A(el(0, 0, 110, 14), GR(0, 0, 110, [0, '#1E5A24', .45], [1, '#1E5A24', 0])),
  P('M-96,0 a44,44 0 0 1 18,-66 a50,50 0 0 1 78,-22 a48,48 0 0 1 78,26 a40,40 0 0 1 16,62 Z', GV(-100, 0, '#6CC054', '#3E8E3A'), 6),
  A(el(-30, -58, 26, 14), '#C8F5A0', .5), A(el(40, -50, 22, 12), '#C8F5A0', .45),
  ...[[-50, -30], [10, -66], [56, -24], [-10, -22]].map(([x, y]) => A(ci(x, y, 7), '#FF8FB0')),
];

// ═════════════ La scène ═════════════
const SCENES = {
  maison: [
    { n: 'lointain', s: 'v_lointain', x: 0, y: 0 }, { n: 'pre', s: 'v_pre', x: 0, y: 0 },
  ],
};
const CIELS = { maison: ['#6CBFFF', '#E4F6FF', '#4EA63E', 1] };
const LISTES = {
  // les pièces, de gauche à droite : le dessin, le nom écrit sur la plaque
  pieces: [['m_cuisine', 'La cuisine'], ['m_bain', 'La salle de bain'], ['m_chambre', 'La chambre']],
};

module.exports = { S, SCENES, CIELS, LISTES };
