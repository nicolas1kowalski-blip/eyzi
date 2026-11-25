// Le paysage de campagne que plusieurs activités se partagent : les collines, les arbres du fond et la barrière
// (v_lointain), le pré, ses touffes d'herbe et ses fleurs (v_pre). La scène fait 1600 × 900, l'horizon est vers 560.
const { N, P, A, T, TT, GL, GR, GV, GH, r1, ci, el, rr, pol, alea } = require('./outils.js');

/** Ajoute le paysage à S. */
module.exports = function paysage(S) {
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
};
