// Les dessins de l'activité « Le corps humain ». genere.js en tire Games/CorpsArt.cs (pour l'application) ;
// le banc d'essai s'en sert pour produire de vraies images sans Windows. Les outils de dessin sont dans outils.js.
//
// Ici, pas de personnage de dessin animé : un corps humain translucide, comme dans une visionneuse d'anatomie, sur un
// fond sombre. Il est dessiné autour de son axe (x = 0), le haut de l'image en y = 0, cadré de la tête aux cuisses pour
// que les organes soient grands. Chaque organe est dessiné autour de son propre milieu, à sa taille dans le corps ;
// la liste « organes » dit où il va et ce que la voix en dit.
const { N, P, A, T, TT, GL, GR, GV, GH, r1, ci, el, rr, pol, alea } = require('./outils.js');
const communs = require('./communs.js');

const S = {};

communs(S);

const NUIT = '#0E1528', VERRE = '#DDF0FF', CYAN = '#7FE0FF';
const O = (d, f, s, w = 3) => P(d, f, w, s);                       // un organe : un trait fin, de sa propre couleur en plus sombre
const TB = (d, c, w, bord) => [T(d, bord, w + 5), T(d, c, w)];     // un tuyau : un trait et son bord

// ═════════════ Le fond : un studio sombre, un halo de lumière derrière le corps ═════════════
S.r_fond = [
  A('M-10,-10 H1610 V910 H-10 Z', GV(0, 900, '#1C2748', '#0C1224')),
  A(ci(800, 430, 620), GR(800, 430, 620, [0, '#4670C0', .55], [.55, '#2A4688', .28], [1, '#2A4688', 0])),
  // une grille très discrète, des repères dans les coins : on est dans une visionneuse
  T([...Array(13)].map((_, i) => `M${100 + i * 117},0 V900`).join(' ') + [...Array(8)].map((_, i) => ` M0,${60 + i * 112} H1600`).join(''), '#8FB4F0', 1.5, .07),
  T('M40,70 V40 H70 M1530,40 H1560 V70 M40,830 V860 H70 M1530,860 H1560 V830', CYAN, 4, .45),
  A('M-10,-10 H1610 V120 H-10 Z', '#060A16', .25), A('M-10,790 H1610 V910 H-10 Z', '#060A16', .3),
];

// ═════════════ Le corps ═════════════
// la moitié du contour (k = 1 : le côté droit de l'image ; k = -1 : le gauche), de la tête à la cuisse
const demi = k => `M0,30 C${k * 40},30 ${k * 60},62 ${k * 58},100 C${k * 57},132 ${k * 44},160 ${k * 22},174 L${k * 24},206 Q${k * 40},226 ${k * 120},240 C${k * 178},244 ${k * 214},280 ${k * 214},330 C${k * 220},420 ${k * 236},500 ${k * 246},580 L${k * 262},700 C${k * 270},740 ${k * 268},790 ${k * 250},800 C${k * 232},800 ${k * 226},760 ${k * 226},720 C${k * 216},620 ${k * 196},520 ${k * 184},440 L${k * 166},352 C${k * 150},440 ${k * 128},500 ${k * 128},556 C${k * 128},600 ${k * 150},630 ${k * 152},668 C${k * 158},740 ${k * 150},840 ${k * 140},912 L${k * 30},912 C${k * 24},850 ${k * 14},790 ${k * 4},740 L0,735`;
// la lumière vient de la gauche : le corps est clair de ce côté, sombre de l'autre, comme un volume
const VOLUME = GL(-270, 0, 270, 0, [0, '#3C5A86'], [.14, '#6E93C4'], [.36, '#A9CBEE'], [.6, '#7C9FCC'], [.86, '#456794'], [1, '#2C4670']);
S.r_corps = [
  T(demi(-1), CYAN, 16, .16), T(demi(1), CYAN, 16, .16),
  A(demi(-1) + ' L2,735 L2,30 Z', VOLUME), A(demi(1) + ' L-2,735 L-2,30 Z', VOLUME),
  // les ombres qui modèlent : sous le menton, les pectoraux, la taille, entre les jambes
  A('M-22,174 Q0,196 22,174 L24,206 Q0,222 -24,206 Z', '#1E3358', .5),
  T('M-150,330 Q-90,370 -16,348 M150,330 Q90,370 16,348', '#2C4670', 9, .35), T('M0,250 V560', '#2C4670', 5, .22), T('M-70,600 Q0,640 70,600', '#2C4670', 6, .22),
  T('M-120,244 Q-60,232 -12,238 M120,244 Q60,232 12,238', '#DDF0FF', 5, .35),
  A('M-4,740 C-14,790 -24,850 -30,912 L30,912 C24,850 14,790 4,740 Z', '#0C1224', .55),
  // le visage, à peine suggéré : l'arcade, le nez, la bouche
  T('M-34,92 Q-20,84 -8,92 M8,92 Q20,84 34,92', '#2C4670', 5, .5), T('M0,96 V124 q-8,8 0,10 q8,-2 0,-10', '#2C4670', 3.5, .45), T('M-14,146 Q0,152 14,146', '#2C4670', 4, .45),
  A(el(-21, 104, 10, 5), '#1E3358', .45), A(el(21, 104, 10, 5), '#1E3358', .45),
  // le liseré de lumière sur les bords
  T(demi(-1), VERRE, 3.5, .95), T(demi(1), VERRE, 3, .6),
  T('M-52,60 Q-58,96 -48,134 M-196,300 Q-206,400 -222,520 M-140,470 Q-122,520 -122,560', '#FFFFFF', 6, .5),
];
// l'intérieur, plus sombre : c'est là qu'on voit les organes
S.r_interieur = [
  A('M0,232 C70,236 150,250 150,300 C140,420 112,500 112,556 C112,600 134,640 132,680 C120,726 40,746 0,746 C-40,746 -120,726 -132,680 C-134,640 -112,600 -112,556 C-112,500 -140,420 -150,300 C-150,250 -70,236 0,232 Z', GR(0, 470, 300, [0, '#1E3466', .62], [.7, '#16264A', .58], [1, '#16264A', .3])),
  A(el(0, 92, 50, 52), GR(0, 92, 54, [0, '#16264A', .75], [.75, '#101C3A', .6], [1, '#101C3A', .2])),
];
// le squelette, à peine visible, pour qu'on sente qu'il est là : le crâne, la colonne, le bassin, les grands os
S.r_squelette = [
  A('M-44,96 Q-46,44 0,42 Q46,44 44,96 Q44,128 30,146 Q14,160 0,160 Q-14,160 -30,146 Q-44,128 -44,96 Z', '#EAF4FF', .13),
  ...[...Array(25)].map((_, i) => A(rr(-9 - (i > 18 ? (i - 18) : 0), 186 + i * 22, 18 + (i > 18 ? (i - 18) * 2 : 0), 15, 5), '#EAF4FF', .2)),
  A('M-118,640 Q-70,610 -22,664 L22,664 Q70,610 118,640 Q128,700 76,726 Q40,738 20,712 L-20,712 Q-40,738 -76,726 Q-128,700 -118,640 Z', '#EAF4FF', .17),
  T('M-190,286 L-210,470 M190,286 L210,470', '#EAF4FF', 15, .14), T('M-214,492 L-244,694 M214,492 L244,694', '#EAF4FF', 11, .14), T('M-92,734 L-96,912 M92,734 L96,912', '#EAF4FF', 22, .14),
  T('M-8,236 Q-70,224 -152,246 M8,236 Q70,224 152,246', '#EAF4FF', 8, .22),
];
// les côtes : derrière les organes, elles ne les cachent pas
const cote = (k, y, w) => `M${k * 10},${y} Q${k * w},${y - 10} ${k * (w + 2)},${y + 22} Q${k * w},${y + 38} ${k * (w - 46)},${y + 36}`;
const COTES = [[258, 112], [284, 128], [310, 138], [336, 142], [362, 140], [388, 132], [414, 118], [440, 100]];
S.r_cotes = [
  T(COTES.flatMap(([y, w]) => [cote(-1, y, w), cote(1, y, w)]).join(' '), '#EAF4FF', 7, .16), A(rr(-9, 250, 18, 156, 8), '#EAF4FF', .14),
];
// la lueur claire derrière un organe posé : il se détache bien du corps
S.r_halo = [A(ci(0, 0, 100), GR(0, 0, 100, [0, '#FFFFFF', .7], [.6, '#DDF4FF', .4], [1, '#DDF4FF', 0]))];
// la vitre : deux reflets par-dessus tout
S.r_reflets = [T('M-136,300 Q-128,420 -104,540', '#FFFFFF', 7, .14), T('M-36,58 Q-20,46 4,46', '#FFFFFF', 5, .2)];

// ═════════════ Les organes ═════════════
S.o_cerveau = [
  ...TB('M0,34 V56', '#E3B0A6', 9, '#8E4A54'),
  O('M-52,10 Q-58,-30 -30,-40 Q-14,-46 0,-38 Q14,-46 30,-40 Q58,-30 52,10 Q48,32 28,38 Q10,44 0,36 Q-10,44 -28,38 Q-48,32 -52,10 Z', GR(-16, -18, 74, [0, '#FFE0E6'], [.5, '#FF9DB8'], [1, '#E0608A']), '#8E4A54'),
  A('M-26,34 Q0,50 26,34 Q0,44 -26,34 Z', '#B8727A', .8), T('M0,-38 V36', '#8E4A54', 3),
  T('M-42,-8 q10,-12 20,-2 q8,8 14,-4 M-46,12 q12,-8 22,2 q8,6 14,-2 M-34,-28 q10,8 22,-2 M-30,28 q10,-8 20,0 M42,-8 q-10,-12 -20,-2 q-8,8 -14,-4 M46,12 q-12,-8 -22,2 q-8,6 -14,-2 M34,-28 q-10,8 -22,-2 M30,28 q-10,-8 -20,0', '#C2487A', 3, .9),
  T('M-42,-14 Q-34,-32 -14,-36', '#FFFFFF', 4.5, .5),
];
const POUMON = k => GR(k * 62, -10, 140, [0, '#FFD0D4'], [.5, '#FF8A9A'], [1, '#D9506A']);
S.o_poumons = [
  ...TB('M0,-108 V-52', '#DCE8F5', 15, '#6E8AAA'), T('M-7,-98 h14 M-7,-86 h14 M-7,-74 h14 M-7,-62 h14', '#8FA8C4', 2.5),
  ...TB('M0,-54 Q-20,-42 -46,-30 M0,-54 Q20,-42 46,-30', '#DCE8F5', 10, '#6E8AAA'),
  O('M-16,-64 Q-70,-86 -104,-10 Q-126,60 -114,92 Q-70,106 -22,86 Q-10,40 -12,-20 Z', POUMON(-1), '#7E3444'),
  O('M16,-64 Q70,-86 104,-10 Q126,60 114,92 Q78,106 42,90 Q32,62 16,40 Q10,10 12,-20 Z', POUMON(1), '#7E3444'),
  T('M-110,8 Q-70,-2 -18,30 M-114,52 Q-80,56 -28,72 M110,18 Q72,8 32,50', '#7E3444', 2.5, .7),
  T('M-46,-30 q-22,10 -32,36 M-46,-30 q-6,26 -18,54 M-62,-6 q-18,6 -30,24 M-56,22 q-14,10 -22,28 M46,-30 q22,10 32,36 M46,-30 q8,24 22,46 M64,-6 q16,6 26,24 M60,22 q12,10 20,28', '#B85868', 3, .85),
  T('M-92,-14 Q-76,-52 -40,-62 M40,-62 Q64,-58 80,-36', '#FFFFFF', 5, .4),
];
S.o_coeur = [
  ...TB('M-26,-16 V-52', '#4A7FD6', 10, '#1E3E86'), ...TB('M-8,-22 C-10,-42 -26,-48 -38,-42', '#5B8FE8', 11, '#1E3E86'), ...TB('M2,-24 C0,-56 32,-60 36,-34', '#E0403E', 13, '#6A1420'),
  T('M10,-52 V-64 M22,-54 V-66', '#E0403E', 6),
  O('M-6,-22 C-40,-34 -52,4 -34,30 C-22,48 -4,56 8,52 C34,44 48,18 40,-8 C34,-28 10,-30 -6,-22 Z', GR(-14, -8, 66, [0, '#FF8A7A'], [.5, '#F0323E'], [1, '#A81424']), '#5E1018'),
  A('M-34,30 C-22,48 -4,56 8,52 C34,44 48,18 40,-8 C36,20 10,44 -34,30 Z', '#6A1420', .35),
  T('M-2,-18 Q8,10 0,46 M10,-6 Q26,6 30,24 M-18,0 Q-22,18 -12,34', '#6A1420', 2.5, .8), T('M-32,-8 Q-30,-22 -16,-24', '#FFFFFF', 4.5, .55),
];
S.o_foie = [
  O(el(-6, 34, 10, 15), GR(-8, 30, 16, [0, '#A8D08A'], [1, '#4E8A3E']), '#2E5E26', 2.5),
  O('M-84,-12 Q-80,-42 -30,-42 Q36,-44 76,-24 Q92,-12 80,4 Q40,34 -24,40 Q-78,42 -88,10 Z', GR(-34, -22, 130, [0, '#F09A72'], [.5, '#C8562E'], [1, '#8A3418']), '#44180E'),
  A('M80,4 Q40,34 -24,40 Q-78,42 -88,10 Q-50,26 0,22 Q50,18 80,4 Z', '#44180E', .3),
  T('M6,-42 Q-2,-6 8,32', '#64281C', 2.5, .8), T('M-66,-22 Q-40,-36 -4,-36', '#FFD2C2', 5, .45),
];
S.o_estomac = [
  ...TB('M-24,-66 Q-26,-46 -16,-34', '#E6AC94', 12, '#8E4A3A'),
  O('M-24,-36 Q22,-58 46,-22 Q62,14 36,42 Q10,64 -24,50 Q-46,42 -52,56 Q-62,48 -58,30 Q-48,10 -28,16 Q-16,0 -24,-36 Z', GR(8, -12, 84, [0, '#FFE2CC'], [.5, '#FFB07A'], [1, '#E0783E']), '#8E4A3A'),
  T('M-6,-20 Q22,-22 32,2 M-12,2 Q14,-2 24,22 M-22,26 Q0,24 10,40', '#B06A54', 2.5, .75), T('M-8,-36 Q20,-42 36,-18', '#FFFFFF', 5, .45),
];
const GROS = 'M-78,46 V-34 Q-78,-56 -56,-56 H56 Q78,-56 78,-34 V34 Q78,56 56,56 H30', GRELE = 'M-48,-28 H36 Q52,-28 52,-14 Q52,0 36,0 H-36 Q-52,0 -52,14 Q-52,28 -36,28 H36 Q50,28 50,40';
S.o_intestins = [
  T(GROS, '#A84A2E', 36), T(GROS, '#FF9A6A', 30),
  T('M-78,34 h0 M-78,12 h0 M-78,-10 h0 M-78,-32 h0 M-50,-56 h0 M-26,-56 h0 M-2,-56 h0 M22,-56 h0 M46,-56 h0 M78,-30 h0 M78,-8 h0 M78,14 h0 M78,36 h0', '#B06A54', 7, .6), T('M-88,40 V-30 Q-88,-64 -56,-66 H40', '#F4C0AE', 4, .55),
  T(GRELE, '#C2487A', 21), T(GRELE, '#FFB8CC', 16), T('M-44,-33 H34 M-34,-5 H30 M-30,23 H34', '#FFFFFF', 3.5, .45),
];
S.o_vessie = [
  ...TB('M-28,-32 Q-22,-16 -12,-8 M28,-32 Q22,-16 12,-8', '#E6C36A', 6, '#7A5E1A'),
  O('M0,-18 Q-36,-16 -34,8 Q-30,28 0,28 Q30,28 34,8 Q36,-16 0,-18 Z', GR(-10, -6, 42, [0, '#FFF8C0'], [.5, '#FFE066'], [1, '#E8A21C']), '#7A5E1A'),
  T('M-22,-4 Q-16,-13 -4,-14', '#FFFFFF', 4, .6),
];

// ═════════════ Ce qui s'allume : l'endroit à trouver, le balayage, les étiquettes, le cœur qui bat ═════════════
S.c_lueur = [A(ci(0, 0, 100), GR(0, 0, 100, [0, '#FFFFFF', .95], [.45, '#9AE8FF', .6], [1, '#48C8FF', 0]))];
S.c_non = [T('M-22,-22 L22,22 M22,-22 L-22,22', NUIT, 16), T('M-22,-22 L22,22 M22,-22 L-22,22', '#FF5A6E', 9)];
S.r_balayage = [A('M-330,-34 H330 V34 H-330 Z', GL(0, -34, 0, 34, [0, '#7FE0FF', 0], [.5, '#B8F2FF', .45], [1, '#7FE0FF', 0])), T('M-320,0 H320', '#EAFBFF', 3, .9)];
S.r_onde = [T(ci(0, 0, 100), '#B8F2FF', 7, .9)];
S.r_etincelle = [A('M0,-14 Q2,-2 14,0 Q2,2 0,14 Q-2,2 -14,0 Q-2,-2 0,-14 Z', '#EAFBFF', .95)];
S.r_etiquette = [A(rr(4, -22, 330, 56, 28), '#060A16', .35), P(rr(0, -28, 330, 56, 28), GV(-28, 28, '#31467A', '#1D2B52'), 2.5, '#8FB8F0'), A(ci(30, 0, 10), '$c'), T(ci(30, 0, 10), '#FFFFFF', 2, .7)];
S.r_point = [A(ci(0, 0, 6.5), '#EAFBFF'), T(ci(0, 0, 12), CYAN, 2.5, .8)];
S.r_pouls = [A(rr(4, 6, 390, 130, 26), '#060A16', .35), P(rr(0, 0, 390, 130, 26), GV(0, 130, '#22335E', '#141F40'), 2.5, '#8FB8F0'),
  P('M36,74 C16,58 26,42 36,54 C46,42 56,58 36,74 Z', '#FF5A6E', 2.5, '#8A1C26'), T('M70,30 V100 M130,30 V100 M190,30 V100 M250,30 V100 M310,30 V100 M70,65 H366', '#5B8FD6', 1.5, .25)];
S.c_carte = [A(rr(8, 14, 330, 330, 34), '#060A16', .4), P(rr(0, 0, 330, 330, 34), GV(0, 330, '#31467A', '#1B2850'), 3, '#8FB8F0'), T(rr(12, 12, 306, 306, 24), '$c', 5, .9), A('M34,12 H296 Q318,12 318,34 V110 Q170,150 12,110 V34 Q12,12 34,12 Z', '#FFFFFF', .05)];
S.c_petite_carte = [A(rr(6, 10, 300, 250, 30), '#060A16', .4), P(rr(0, 0, 300, 250, 30), GV(0, 250, '#31467A', '#1B2850'), 3, '#8FB8F0'), T(rr(10, 10, 280, 230, 22), '$c', 4.5, .9), A('M32,10 H268 Q290,10 290,32 V84 Q150,116 10,84 V32 Q10,10 32,10 Z', '#FFFFFF', .05)];
S.c_air = [A(ci(0, 0, 9), GR(0, 0, 9, [0, '#FFFFFF', .9], [1, '#9AE8FF', 0]))];
S.c_globule = [P(ci(0, 0, 6), GR(-2, -2, 7, [0, '#FF9A8A'], [1, '#D9302A']), 2, '#6A1420')];

// Les organes : clé, le nom (avec son article), où il va (x, y), la place qu'il prend (largeur, hauteur), sa profondeur
// (les côtes passent devant les poumons et le cœur ; tout glisse un peu quand le corps tourne), ce que la voix dit une
// fois qu'il est en place, sa couleur.
const LISTES = { organes: [
  ['cerveau', 'le cerveau', '0', '92', '108', '92', '0.6', "Le cerveau ! C'est lui qui pense, et qui commande tout le corps.", '#F2A8B0'],
  ['poumons', 'les poumons', '0', '366', '236', '204', '0.5', "Les poumons ! Ils se remplissent d'air quand on respire.", '#6CC4F8'],
  ['coeur', 'le cœur', '18', '382', '98', '120', '0.7', 'Le cœur ! Il bat pour envoyer le sang partout. Boum boum !', '#FF5A6E'],
  ['estomac', "l'estomac", '58', '522', '124', '130', '0.45', "L'estomac ! C'est là que va ce qu'on mange.", '#F2A27A'],
  ['foie', 'le foie', '-52', '512', '180', '90', '0.4', 'Le foie ! Il nettoie le sang et garde des forces.', '#C8705A'],
  ['intestins', 'les intestins', '0', '624', '196', '126', '0.3', 'Les intestins ! Ils finissent de digérer le repas.', '#E8A690'],
  ['vessie', 'la vessie', '0', '712', '80', '64', '0.34', "La vessie ! Elle garde le pipi jusqu'aux toilettes.", '#F2D270'],
] };

// { n: nom, s: dessin, x, y, e: échelle, r: rotation, c: couleurs }
const SCENES = { studio: [{ n: 'fond', s: 'r_fond', x: 0, y: 0 }] };
const CIELS = { studio: ['#1C2748', '#111A33', '#0C1224', 0] };

module.exports = { S, SCENES, CIELS, LISTES };
