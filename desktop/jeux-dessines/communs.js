// Les dessins que les jeux ont en commun : le ciel, l'eau qui gicle, le rideau d'eau, les étoiles et les cœurs.
const { N, P, A, T, TT, GL, GR, GV, GH, r1, ci, el, rr, pol, alea } = require('./outils.js');

/** Ajoute les dessins communs à S. */
module.exports = function communs(S) {
  // ═════════════ Le ciel et ce qui vole ═════════════
  S.lueur = [A(ci(0, 0, 260), GR(0, 0, 260, [0, '#FFF8D0', .95], [.35, '#FFF1A8', .5], [1, '#FFF1A8', 0]))];
  S.soleil = [P(ci(0, 0, 66), GR(-18, -20, 90, [0, '#FFF29A'], [1, '#FFC93D']), 7, '#E8A21C'), T('M-36,-24 q10,-24 36,-28', '#fff', 9, .75)];
  S.rayons = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11].map(i => { const [x1, y1] = pol(i * 30, 88), [x2, y2] = pol(i * 30, i % 2 ? 118 : 134); return T(`M${x1},${y1} L${x2},${y2}`, '#FFD65A', 10, .9); });
  S.nuage = [
    A('M-150,34 a52,52 0 0 1 10,-98 a70,70 0 0 1 126,-34 a58,58 0 0 1 104,26 a50,50 0 0 1 40,96 a40,30 0 0 1 -30,26 L-130,50 a30,30 0 0 1 -20,-16 Z', GV(-110, 50, '#FFFFFF', '#DDEBFA')),
    A('M-124,36 q130,-22 250,0 q-20,16 -40,14 L-110,50 q-14,-4 -14,-14 Z', '#C9DDF2', .7),
    T('M-110,-40 q20,-38 62,-36', '#fff', 10, .9),
  ];
  S.oiseau = [T('M-26,-4 q12,-16 26,2 q14,-18 26,-2', N, 6)];

  // ═════════════ L'eau ═════════════
  S.goutte = [P('M0,-15 q-12,16 0,24 q12,-8 0,-24 Z', GV(-15, 10, '#DDF5FF', '#48B0F8'), 3, '#1E78D6'), A(ci(-3, -1, 3), '#fff', .9)];
  S.bulle = [P(ci(0, 0, 13), GR(-4, -5, 16, [0, '#FFFFFF', .95], [1, '#CDEBFF', .55]), 3, '#7FB6E6'), T('M-6,-6 q3,-5 9,-4', '#fff', 3)];
  S.mousse = [P('M-26,6 a16,16 0 0 1 6,-26 a20,20 0 0 1 36,-4 a16,16 0 0 1 14,28 a12,12 0 0 1 -12,10 L-16,14 a10,10 0 0 1 -10,-8 Z', GV(-30, 16, '#FFFFFF', '#DCEFFF'), 3, '#9CC6EA')];
  // la gerbe : une couronne de langues d'eau arrondies (deux formes qui alternent)
  const couronne = (n, dedans, dehors, tour) => { let d = ''; for (let i = 0; i <= n; i++) { const a = tour + i * 360 / n, [x, y] = pol(a, dedans), [cx, cy] = pol(a - 180 / n, dehors * (.78 + .22 * alea(i + n))); d += i ? ` Q${cx},${cy} ${x},${y}` : `M${x},${y}`; } return d + ' Z'; };
  for (const [k, n, r, t] of [['a', 9, 100, 0], ['b', 8, 92, 22]]) S['gerbe_' + k] = [
    A(couronne(n, 34, r + 22, t), GR(0, 0, r + 22, [0, '#FFFFFF', .0], [.5, '#DDF5FF', .55], [1, '#A6E2FF', .0])),
    P(couronne(n, 30, r, t + 180 / n), GR(0, 0, r, [0, '#FFFFFF', 1], [.55, '#DDF5FF', .95], [1, '#7FD3FF', .75]), 3.5, '#3E9BE8', .96),
    A(ci(0, 0, 30), GR(0, 0, 30, [0, '#FFFFFF', 1], [1, '#FFFFFF', .2])),
    ...[...Array(n)].map((_, i) => { const [x, y] = pol(t + i * 360 / n + 12, r + 14 + 16 * alea(i * 3 + n)); return P(ci(x, y, 5 + 4 * alea(i + 7)), '#DDF5FF', 2.5, '#3E9BE8'); }),
  ];
  S.brume = [A(ci(0, 0, 150), GR(0, 0, 150, [0, '#FFFFFF', .55], [.5, '#EAF8FF', .25], [1, '#EAF8FF', 0]))];
  S.onde = [T(el(0, 0, 60, 20), '#FFFFFF', 8, .9), T(el(0, 0, 60, 20), '#7FD3FF', 3)];
  S.vague = [   // le rideau d'eau qui passe d'une scène à l'autre (origine : en haut à gauche de la crête)
    A('M-100,60 q100,-90 200,0 t200,0 t200,0 t200,0 t200,0 t200,0 t200,0 t200,0 t200,0 V2200 H-100 Z', GV(0, 1400, '#5CC0FF', '#2E7FD6')),
    T('M-100,60 q100,-90 200,0 t200,0 t200,0 t200,0 t200,0 t200,0 t200,0 t200,0 t200,0', '#FFFFFF', 26, .95),
    T('M-100,110 q100,-90 200,0 t200,0 t200,0 t200,0 t200,0 t200,0 t200,0 t200,0 t200,0', '#BFE8FF', 12, .7),
  ];
  S.etoile = [P('M0,-22 Q3,-3 22,0 Q3,3 0,22 Q-3,3 -22,0 Q-3,-3 0,-22 Z', GR(0, 0, 22, [0, '#FFFFFF'], [1, '#FFE066']), 3, '#E8A21C')];
  S.coeur = [P('M0,14 C-32,-10 -14,-30 0,-14 C14,-30 32,-10 0,14 Z', GR(-6, -10, 30, [0, '#FF9DB8'], [1, '#F0406E']), 4)];
};
