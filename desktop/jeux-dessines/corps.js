// Les dessins de l'activité « Dans mon corps ». genere.js en tire Games/CorpsArt.cs (pour l'application) ;
// le banc d'essai s'en sert pour produire de vraies images sans Windows. Les outils de dessin sont dans outils.js.
//
// Le bonhomme est dessiné autour de son axe (x = 0), le haut de l'image en y = 0. Chaque organe est dessiné autour
// de son propre milieu ; la liste « organes » dit où il va dans le corps et ce que la voix en dit.
const { N, P, A, T, TT, GL, GR, GV, GH, r1, ci, el, rr, pol, alea } = require('./outils.js');
const communs = require('./communs.js');

const S = {};

communs(S);

const PEAU = '#BDE4FF', PEAU_OMBRE = '#8FC4EE';
const etoileD = (x, y, R, r = R * .45, n = 5, tour = -90) => [...Array(n * 2)].map((_, i) => { const [dx, dy] = pol(tour + i * 180 / n, i % 2 ? r : R); return (i ? 'L' : 'M') + r1(x + dx) + ',' + r1(y + dy); }).join(' ') + ' Z';
// un petit visage d'organe : deux yeux, un sourire, des joues
const frimousse = (x, y, k = 1) => [A(ci(x - 11 * k, y, 4.6 * k), N), A(ci(x + 11 * k, y, 4.6 * k), N), A(ci(x - 9.6 * k, y - 1.6 * k, 1.6 * k), '#FFFFFF'), A(ci(x + 12.4 * k, y - 1.6 * k, 1.6 * k), '#FFFFFF'),
  T(`M${r1(x - 7 * k)},${r1(y + 8 * k)} q${r1(7 * k)},${r1(8 * k)} ${r1(14 * k)},0`, N, 3.2 * k), A(el(x - 20 * k, y + 8 * k, 5.5 * k, 3.5 * k), '#FF6A8A', .5), A(el(x + 20 * k, y + 8 * k, 5.5 * k, 3.5 * k), '#FF6A8A', .5)];

// ═════════════ La salle ═════════════
S.c_salle = [
  A('M-10,-10 H1610 V700 H-10 Z', GV(0, 700, '#E4F8F2', '#C8EEE4')),
  ...[...Array(40)].map((_, i) => { const x = 60 + (i % 10) * 164 + (Math.floor(i / 10) % 2) * 82, y = 70 + Math.floor(i / 10) * 160; return T(`M${x - 9},${y} h18 M${x},${y - 9} v18`, '#FFFFFF', 6, .55); }),
  A('M-10,700 H1610 V910 H-10 Z', GV(700, 900, '#A9D8CC', '#8CC6B8')),
  T('M-10,700 H1610', '#FFFFFF', 10, .8), T('M-10,712 H1610', '#7FB8AA', 4, .6),
  T('M200,712 L120,900 M520,712 L480,900 M1080,712 L1120,900 M1400,712 L1480,900 M-10,790 H1610', '#7FB8AA', 3, .35),
  // l'armoire à pharmacie
  P(rr(60, 40, 150, 120, 14), GV(40, 160, '#FFFFFF', '#E3F0EC'), 5), T('M135,44 V156', '#B9D6CE', 3), P('M112,82 h18 v-18 h20 v18 h18 v20 h-18 v18 h-20 v-18 h-18 Z', '#3CCB6C', 4), A(ci(126, 138, 4), '#8793AD'), A(ci(144, 138, 4), '#8793AD'),
  // la toise
  P(rr(1038, 150, 44, 552, 10), GV(150, 702, '#FFF6DC', '#F2E2B8'), 5),
  T([...Array(13)].map((_, i) => `M1040,${190 + i * 40} h${i % 2 ? 16 : 26}`).join(' '), '#C29258', 4), ...[0, 1, 2].map(i => P(etoileD(1066, 216 + i * 160, 9), ['#FF7AB0', '#48B0F8', '#FFD23D'][i], 2.5)),
  // la table de l'écran, ses tiroirs, des livres et une pomme
  P(rr(1130, 520, 440, 30, 10), GV(520, 550, '#FFFFFF', '#DCEAE6'), 5), P(rr(1156, 550, 388, 152, 8), GV(550, 702, '#F4FAF8', '#CFE4DE'), 5),
  P(rr(1176, 570, 164, 50, 8), '#FFFFFF', 4), P(rr(1360, 570, 164, 50, 8), '#FFFFFF', 4), P(rr(1176, 634, 348, 50, 8), '#FFFFFF', 4), T('M1240,595 h36 M1424,595 h36 M1330,659 h40', '#8793AD', 6),
  P(rr(1428, 482, 96, 18, 4), '#48B0F8', 4), P(rr(1436, 464, 84, 18, 4), '#FF9A3D', 4), P(rr(1444, 500, 80, 20, 4), '#3CCB6C', 4),
  // la plante
  P('M452,700 L466,640 H530 L544,700 Z', GV(640, 700, '#FF9A6A', '#D9623A'), 5), P(rr(458, 628, 80, 18, 8), '#FFB08A', 4.5),
  P('M498,630 Q450,560 470,500 Q510,560 498,630 Z', GV(500, 630, '#8FD46E', '#4EA84A'), 4.5), P('M498,630 Q560,570 548,510 Q500,560 498,630 Z', GV(510, 630, '#A8E67E', '#5CB850'), 4.5), P('M498,632 Q486,540 500,470 Q520,540 498,632 Z', GV(470, 632, '#B4F08A', '#5CB850'), 4.5),
];
// le socle lumineux où se tient le bonhomme (l'origine est en son milieu)
S.c_socle = [
  A(el(0, 16, 300, 46), N, .16),
  P(el(0, 8, 280, 44), GV(-36, 52, '#DDF0FF', '#9AC4EE'), 6), P(el(0, 0, 280, 44), GV(-44, 44, '#FFFFFF', '#CFE6FA'), 6),
  T(el(0, 0, 216, 32), '#7FD3FF', 6, .9), T(el(0, 0, 150, 21), '#B8E8FF', 5, .9), A(el(0, 0, 90, 12), '#7FD3FF', .5),
];
S.c_socle_lueur = [A(el(0, 0, 300, 60), GR(0, 0, 300, [0, '#B8F0FF', .8], [.5, '#7FD3FF', .35], [1, '#7FD3FF', 0]))];

// ═════════════ L'écran : trois courbes s'allument quand le cœur, les poumons et le cerveau sont en place ═════════════
S.c_ecran = [
  A(rr(8, 14, 380, 330, 30), N, .16),
  P(rr(0, 0, 380, 330, 30), GV(0, 330, '#F4F8FF', '#C9D3E8'), 6), P(rr(20, 20, 340, 262, 18), GV(20, 282, '#16324F', '#0E2238'), 5),
  T('M20,108 H360 M20,194 H360', '#2F5F8A', 2.5, .7), T([...Array(8)].map((_, i) => `M${58 + i * 40},22 V280`).join(' '), '#2F5F8A', 1.5, .4),
  A(ci(40, 306, 8), '#3CCB6C'), A(ci(66, 306, 8), '#FFD23D'), P(rr(250, 296, 96, 20, 10), '#8793AD', 3.5),
  P('M150,330 h80 l14,40 h-108 Z', GV(330, 370, '#C9D3E8', '#8793AD'), 5),
];
S.c_ic_coeur = [P('M0,12 C-28,-8 -12,-26 0,-12 C12,-26 28,-8 0,12 Z', '#FF5A6E', 3.5)];
S.c_ic_poumons = [P('M-3,-12 Q-22,-14 -24,8 Q-22,18 -6,16 Z', '#FF9DB0', 3.5), P('M3,-12 Q22,-14 24,8 Q22,18 6,16 Z', '#FF9DB0', 3.5), T('M0,-20 V-6', N, 4)];
S.c_ic_cerveau = [P('M-22,4 Q-28,-14 -8,-18 Q4,-26 16,-16 Q30,-10 24,6 Q14,18 -6,16 Q-18,16 -22,4 Z', '#FFB0C8', 3.5), T('M-8,-10 q8,6 0,14 M6,-12 q-6,8 4,16', '#D9608A', 2.5)];

// ═════════════ Le bonhomme ═════════════
const MEMBRES = 'M-140,344 Q-204,430 -214,566 M140,344 Q204,430 214,566', JAMBES = 'M-62,690 L-66,838 M62,690 L66,838';
const TRONC = 'M-150,326 Q-150,298 -112,296 L112,296 Q150,298 150,326 L140,610 Q138,708 72,716 L-72,716 Q-138,708 -140,610 Z';
S.c_corps = [
  // le contour sombre de tout le corps d'abord, puis la couleur par-dessus : une seule silhouette
  T(MEMBRES, N, 70), T(JAMBES, N, 92), T('M0,286 V322', N, 84), P(TRONC, N, 14), P(el(0, 186, 116, 130), N, 14), P(el(-116, 204, 20, 28), N, 12), P(el(116, 204, 20, 28), N, 12),
  P(ci(-216, 574, 34), N, 12), P(ci(216, 574, 34), N, 12),
  T(MEMBRES, PEAU, 56), T(JAMBES, PEAU, 78), T('M0,286 V322', PEAU, 70), A(TRONC, PEAU), A(el(0, 186, 116, 130), PEAU), A(el(-116, 204, 20, 28), PEAU), A(el(116, 204, 20, 28), PEAU),
  A(ci(-216, 574, 34), PEAU), A(ci(216, 574, 34), PEAU), T('M-116,194 q-8,10 0,22 M116,194 q8,10 0,22', PEAU_OMBRE, 4),
  // un peu d'ombre du côté droit, un reflet du côté gauche : il a du volume
  A('M150,326 L140,610 Q138,708 72,716 L40,716 Q110,690 112,600 L118,326 Z', PEAU_OMBRE, .55), T('M158,350 Q214,430 226,548', PEAU_OMBRE, 14, .7), T('M84,700 L88,830 M-40,700 L-44,830', PEAU_OMBRE, 14, .6),
  A('M60,300 Q112,196 60,70 Q130,110 116,196 Q112,270 60,300 Z', PEAU_OMBRE, .5),
  T('M-126,350 Q-178,420 -190,520', '#FFFFFF', 12, .6), T('M-132,330 L-126,560', '#FFFFFF', 10, .5), T('M-84,700 L-86,820', '#FFFFFF', 10, .5), T('M-96,150 Q-88,96 -40,72', '#FFFFFF', 12, .6),
  // les baskets
  ...[-1, 1].flatMap(k => [P(`M${k * 28},826 Q${k * 24},866 ${k * 50},868 L${k * 130},868 Q${k * 150},864 ${k * 144},846 Q${k * 124},822 ${k * 96},824 Z`, GV(822, 868, '#FF8A7A', '#D9302A'), 6),
    A(`M${k * 104},836 Q${k * 142},842 ${k * 144},856 L${k * 142},866 L${k * 104},866 Z`, '#FFFFFF'), T(`M${k * 30},866 H${k * 138}`, '#FFFFFF', 6), T(`M${k * 30},868 H${k * 138}`, N, 2.5), T(`M${k * 56},832 l${k * 12},14 M${k * 72},830 l${k * 12},14`, '#FFFFFF', 4.5)]),
];
// l'intérieur : la fenêtre du crâne et celle du tronc, où vont les organes
S.c_fenetres = [
  P(el(0, 126, 92, 60), GR(0, 114, 110, [0, '#3A64A8'], [1, '#20386C']), 5, '#5B8FD6'),
  P('M-104,320 Q-110,302 -84,300 L84,300 Q110,302 104,320 L112,524 Q116,644 60,686 Q0,706 -60,686 Q-116,644 -112,524 Z', GR(0, 440, 300, [0, '#3A64A8'], [1, '#1E3566']), 5, '#5B8FD6'),
  T('M-106,380 H106 M-110,460 H110 M-112,540 H112 M-104,620 H104 M-56,302 V690 M0,302 V702 M56,302 V690', '#6C9AE0', 1.5, .3),
];
// la vitre par-dessus les organes : deux reflets
S.c_vitre = [T('M-86,330 Q-96,420 -94,520', '#FFFFFF', 9, .28), T('M-66,326 Q-72,380 -70,430', '#FFFFFF', 5, .2), T('M-70,112 Q-56,86 -24,78', '#FFFFFF', 7, .3)];
// le visage : le blanc des yeux, les joues ; la pupille et les bouches sont posées par le jeu
S.c_visage = [P(el(-42, 232, 22, 25), '#FFFFFF', 5), P(el(42, 232, 22, 25), '#FFFFFF', 5), A(el(-80, 262, 18, 12), '#FF8FA8', .55), A(el(80, 262, 18, 12), '#FF8FA8', .55)];
S.c_pupille = [A(ci(0, 0, 11), N), A(ci(3.5, -4, 3.8), '#FFFFFF')];
S.c_bouche = [T('M-26,-4 Q0,20 26,-4', N, 6.5)];
S.c_bouche_ravi = [P('M-30,-8 Q0,34 30,-8 Q0,4 -30,-8 Z', '#7A1E3A', 5), A('M-12,12 Q0,4 12,12 Q0,22 -12,12 Z', '#FF7A9A')];
S.c_bouche_oh = [P(el(0, 4, 13, 16), '#7A1E3A', 5)];

// ═════════════ Les organes (chacun autour de son milieu) ═════════════
S.o_cerveau = [
  P('M-78,14 Q-92,-22 -62,-40 Q-44,-62 -12,-56 Q10,-70 36,-56 Q70,-56 80,-24 Q94,4 70,26 Q56,46 20,44 Q-10,54 -40,42 Q-70,42 -78,14 Z', GR(-20, -24, 110, [0, '#FFD0DE'], [.6, '#FF9DBA'], [1, '#E0688E']), 5.5),
  T('M-2,-56 Q-10,-20 0,44', '#D9608A', 4, .8), T('M-56,-30 q16,6 12,22 q-6,14 10,22 M-30,-46 q10,14 0,26 M28,-48 q-12,14 2,28 M58,-34 q-18,8 -10,24 q10,10 -4,22 M-64,16 q14,-6 26,4 M40,22 q14,-10 26,-2', '#D9608A', 3.5, .8),
  T('M-58,-32 Q-40,-50 -16,-48', '#FFFFFF', 6, .6), ...frimousse(0, 6, 1.15),
];
S.o_poumons = [
  ...TT('M0,-88 V-40 M0,-42 Q-16,-30 -30,-22 M0,-42 Q16,-30 30,-22', '#9AD0F5', 9),
  P('M-12,-46 Q-66,-62 -88,10 Q-98,72 -52,76 Q-10,74 -10,20 Z', GR(-58, -14, 100, [0, '#FFD0D8'], [.6, '#FF9DB0'], [1, '#E0687E']), 5.5), P('M12,-46 Q66,-62 88,10 Q98,72 52,76 Q10,74 10,20 Z', GR(42, -14, 100, [0, '#FFD0D8'], [.6, '#FF9DB0'], [1, '#E0687E']), 5.5),
  T('M-30,-22 q-18,14 -20,40 M-36,-6 q-18,4 -30,22 M-30,10 q-8,16 -4,36 M30,-22 q18,14 20,40 M36,-6 q18,4 30,22 M30,10 q8,16 4,36', '#D9506A', 3.5, .7),
  T('M-74,-10 Q-62,-40 -34,-46', '#FFFFFF', 6, .6), ...frimousse(-50, 30, .95), ...frimousse(50, 30, .95),
];
S.o_coeur = [
  ...TT('M-8,-30 Q-12,-52 -30,-56', '#5B8FE8', 11), ...TT('M12,-30 Q14,-56 34,-60 M8,-34 Q4,-50 14,-64', '#F0483E', 11),
  P('M0,42 C-52,6 -44,-40 -12,-34 C-2,-34 2,-28 4,-24 C10,-40 46,-40 44,-4 C42,18 20,32 0,42 Z', GR(-12, -14, 60, [0, '#FF9A8A'], [.55, '#F0483E'], [1, '#B5261F']), 5.5),
  T('M-30,-12 Q-28,-26 -14,-28', '#FFFFFF', 6, .7), T('M4,-24 Q0,0 8,22', '#B5261F', 3, .6), ...frimousse(2, 4, .85),
];
S.o_estomac = [
  ...TT('M-22,-62 Q-24,-44 -14,-34', '#FFB08A', 13),
  P('M-22,-36 Q20,-56 44,-22 Q60,12 34,40 Q8,62 -24,48 Q-46,40 -52,56 Q-64,48 -58,30 Q-48,10 -28,16 Q-16,0 -22,-36 Z', GR(6, -10, 80, [0, '#FFD6BE'], [.6, '#FFA47A'], [1, '#E0703E']), 5.5),
  T('M-8,-34 Q20,-38 34,-14', '#FFFFFF', 6, .6), T('M-10,30 q14,8 30,0', '#D9623A', 3, .6), ...frimousse(10, 6, .95),
];
S.o_foie = [
  P('M-70,-14 Q-66,-40 -26,-38 Q30,-40 64,-22 Q78,-12 66,2 Q30,30 -20,34 Q-64,36 -72,10 Q-74,-2 -70,-14 Z', GR(-20, -16, 100, [0, '#D98A6A'], [.6, '#B0563C'], [1, '#7E3422']), 5.5),
  T('M-52,-22 Q-30,-32 0,-30', '#FFC2A8', 6, .6), T('M4,-34 Q-2,-6 6,24', '#7E3422', 3, .5), ...frimousse(-22, 0, .95),
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
  T('M-24,-4 Q-18,-14 -6,-16', '#FFFFFF', 5, .7), ...frimousse(0, 8, .8),
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
const LISTES = { organes: [
  ['cerveau', 'le cerveau', '0', '128', '172', '112', '0.9', "Le cerveau ! C'est lui qui pense, et qui commande tout le corps.", '#FF7AB0'],
  ['poumons', 'les poumons', '0', '372', '200', '170', '0.3', "Les poumons ! Ils se remplissent d'air quand on respire.", '#48B0F8'],
  ['coeur', 'le cœur', '14', '388', '100', '104', '0.7', 'Le cœur ! Il bat pour envoyer le sang partout. Boum boum !', '#F0483E'],
  ['estomac', "l'estomac", '58', '488', '124', '128', '0.5', "L'estomac ! C'est là que va ce qu'on mange.", '#FF9A3D'],
  ['foie', 'le foie', '-50', '474', '156', '80', '0.45', 'Le foie ! Il nettoie le sang et garde des forces.', '#B0563C'],
  ['intestins', 'les intestins', '0', '572', '180', '124', '0.35', 'Les intestins ! Ils finissent de digérer le repas.', '#F2A07A'],
  ['vessie', 'la vessie', '0', '656', '90', '70', '0.4', "La vessie ! Elle garde le pipi jusqu'aux toilettes.", '#FFD23D'],
] };

// { n: nom, s: dessin, x, y, e: échelle, r: rotation, c: couleurs }
const SCENES = { salle: [{ n: 'salle', s: 'c_salle', x: 0, y: 0 }] };
const CIELS = { salle: ['#E4F8F2', '#C8EEE4', '#8CC6B8', 1] };

module.exports = { S, SCENES, CIELS, LISTES };
