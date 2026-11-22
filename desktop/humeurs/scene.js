// Le moteur des scènes d'humeur : des paramètres animés par images clés, et une boîte d'effets
// (particules, nuages, mots écrits, objets). Chaque scène est une fonction (t, o, S) qui règle la pose « o ».
const SOL=610;
const fondG=document.getElementById('fond'), devantG=document.getElementById('devant');
const P=creerPerso(document.getElementById('persoG'));
// k(t, [[t0,v0],[t1,v1,'ease'],…]) : la valeur à l'instant t (interpolation ; le type d'accélération se lit sur le point d'arrivée)
const COURBES={lin:x=>x, ease, out, back, elast, in:x=>x*x};
function k(t,pts){ if(t<=pts[0][0]) return pts[0][1]; for(let i=1;i<pts.length;i++){ if(t<=pts[i][0]){ const a=pts[i-1], b=pts[i]; const u=(t-a[0])/Math.max(1e-6,b[0]-a[0]); return lerp(a[1],b[1],(COURBES[b[2]||'ease'])(u)); } } return pts[pts.length-1][1]; }
const entre=(t,a,b)=>t>=a&&t<b;
const fen=(t,a,b,f=.15)=>clamp((t-a)/f)*clamp((b-t)/f);          // 1 entre a et b, avec un fondu
const pt=(t,a,b,o1,o2)=>({x:lerp(o1.x,o2.x,ease(seg(t,a,b))),y:lerp(o1.y,o2.y,ease(seg(t,a,b))),plie:lerp(o1.plie??.4,o2.plie??.4,ease(seg(t,a,b))),devant:seg(t,a,b)>.5?o2.devant:o1.devant,main:o2.main,pouce:o2.pouce});
// les poses de bras courantes (repère du personnage)
const BRAS={ repos:{g:{x:-72,y:-74,plie:.4},d:{x:72,y:-74,plie:-.4}}, hautG:{x:-128,y:-222,plie:.3}, hautD:{x:128,y:-222,plie:-.3},
  hancheG:{x:-58,y:-96,plie:.9}, hancheD:{x:58,y:-96,plie:-.9}, dosG:{x:-24,y:-52,plie:-.9}, dosD:{x:24,y:-52,plie:.9},
  boucheG:{x:-4,y:-214,plie:.9,devant:true}, boucheD:{x:8,y:-214,plie:-.9,devant:true}, ventreG:{x:-8,y:-104,plie:.6,devant:true}, ventreD:{x:8,y:-104,plie:-.6,devant:true},
  croiseG:{x:40,y:-128,plie:1,devant:true}, croiseD:{x:-40,y:-112,plie:-1,devant:true}, cotéG:{x:-150,y:-170,plie:.2}, cotéD:{x:150,y:-170,plie:-.2} };
const FORMES={
  goutte:(g,c)=>el('path',{d:'M0,-12 q-9,13 0,19 q9,-6 0,-19 Z',fill:c||'#7FD3FF',stroke:N,'stroke-width':4},g),
  coeur:(g,c)=>{ el('path',{d:coeurD(0,0,1.3),fill:c||'url(#gCoeur)',stroke:N,'stroke-width':5,'stroke-linejoin':'round'},g); el('path',{d:'M-11,-9 q2,-6 8,-7',fill:'none',stroke:'#fff','stroke-width':3.5,'stroke-linecap':'round',opacity:.8},g); },
  etoile:(g,c)=>{ let d=''; for(let i=0;i<8;i++){const a=i*Math.PI/4, r=i%2?6:16; d+=(i?' L':'M')+(Math.sin(a)*r)+','+(-Math.cos(a)*r);} el('path',{d:d+' Z',fill:c||'#FFE066',stroke:N,'stroke-width':4,'stroke-linejoin':'round'},g); },
  rond:(g,c)=>{ el('circle',{r:12,fill:c||'#fff',stroke:N,'stroke-width':4.5},g); el('path',{d:'M-6,-5 q4,-4 9,-2',fill:'none',stroke:'#fff','stroke-width':3,'stroke-linecap':'round',opacity:.7},g); },
  confetti:(g,c)=>el('rect',{x:-8,y:-5,width:16,height:10,rx:3,fill:c||'#FF4F7B',stroke:N,'stroke-width':3},g),
  miette:(g,c)=>el('circle',{r:5,fill:c||'#D99A4E',stroke:N,'stroke-width':3},g),
  trait:(g,c)=>el('path',{d:'M0,-14 L0,14',stroke:c||N,'stroke-width':7,'stroke-linecap':'round'},g),
  texte:(g,c,m)=>{ const e=el('text',{'font-size':40,'font-weight':900,'font-family':'Arial Black,Arial',fill:c||'#7E3FF2',stroke:N,'stroke-width':2,'text-anchor':'middle'},g); e.textContent=m||'♪'; },
};
const COUL=['#FF4F7B','#5CC8FF','#6EDC8C','#B58BFF','#FFB347','#FFE066'];
const S={ pools:{}, objets:{},
  debut(){ for(const n in this.pools) for(const e of this.pools[n]) e.setAttribute('opacity',0); },
  pool(nom,n,devant,creer){ if(!this.pools[nom]) this.pools[nom]=[...Array(n)].map((_,i)=>{ const g=el('g',{opacity:0},devant?devantG:fondG); creer(g,i); return g; }); return this.pools[nom]; },
  // des particules : nées entre a et b à la cadence donnée, lancées de (x,y) avec une vitesse et une pesanteur
  particules(nom,t,a,b,o){ o=Object.assign({n:10,forme:'goutte',x:640,y:300,dx:0,dy:0,vx:0,vy:-100,ex:60,ey:40,g:0,vie:1.2,taille:1,et:.4,rot:0,devant:true,couleurs:null,mot:null,gonfle:0},o);
    const cad=o.cadence??(o.vie/o.n); const els=this.pool(nom,o.n,o.devant,(g,i)=>FORMES[o.forme](g,o.couleurs?o.couleurs[i%o.couleurs.length]:o.couleur,Array.isArray(o.mot)?o.mot[i%o.mot.length]:o.mot));
    els.forEach((e,i)=>{ const cycle=o.n*cad; let naiss=a+i*cad; if(o.boucle!==false&&t>naiss) naiss+=Math.floor((t-naiss)/cycle)*cycle; const age=t-naiss; if(age<0||age>o.vie||naiss>b) return;
      const r1=rnd(i*3.1+nom.length), r2=rnd(i*7.7+1+nom.length), r3=rnd(i*1.9+5);
      const x=o.x+(r1-.5)*o.dx+(o.vx+(r1-.5)*o.ex)*age, y=o.y+(r2-.5)*o.dy+(o.vy+(r2-.5)*o.ey)*age+.5*o.g*age*age+(o.onde?o.onde*Math.sin(age*6+i):0);
      const ech=o.taille*(1-o.et/2+r3*o.et)*(1+o.gonfle*age)*clamp(age*10);
      e.setAttribute('transform',`translate(${x},${y}) rotate(${o.rot*age*(i%2?1:-1)+(o.incline||0)*(r3-.5)}) scale(${ech})`); e.setAttribute('opacity',clamp((o.vie-age)/(o.vie*(o.fondu??.35)))*(o.op??1)); }); },
  // un mot écrit (bruit, parole)
  texte(nom,t,a,b,mot,x,y,o){ o=Object.assign({taille:50,couleur:'#FFC83D',tremble:0,monte:0,grossit:0},o); const e=this.pool('txt-'+nom,1,true,(g)=>{ el('text',{'font-weight':900,'font-family':'Arial Black,Arial',stroke:N,'stroke-width':3,'stroke-linejoin':'round','paint-order':'stroke'},g); })[0];
    if(t<a||t>b) return; const u=seg(t,a,b), tx=e.firstChild; tx.textContent=mot; tx.setAttribute('fill',o.couleur); tx.setAttribute('font-size',o.taille*(1+o.grossit*out(u))); tx.setAttribute('text-anchor',o.ancre||'start');
    e.setAttribute('transform',`translate(${x+o.tremble*Math.sin(t*47)},${y-o.monte*u+o.tremble*Math.cos(t*41)})`); e.setAttribute('opacity',Math.min(clamp(u*8),clamp((1-u)*5))); },
  // un objet dessiné une fois, puis posé à chaque image
  objet(nom,svg,devant){ if(!this.objets[nom]){ const g=el('g',{opacity:0},devant===true?devantG:devant||fondG); g.innerHTML=svg; this.objets[nom]=g; this.pools['obj-'+nom]=[g]; } return this.objets[nom]; },
  poser(nom,x,y,o){ o=Object.assign({rot:0,sx:1,sy:null,op:1},o); const g=this.objets[nom]; g.setAttribute('transform',`translate(${x},${y}) rotate(${o.rot}) scale(${o.sx},${o.sy??o.sx})`); g.setAttribute('opacity',o.op); return g; },
};
const SCENES={};
function scene(cle,info,f){ SCENES[cle]=Object.assign({cle,f},info); }
let COURANTE=null;
function rendu(T){ const sc=COURANTE, t=((T%sc.duree)+sc.duree)%sc.duree; S.debut();
  const o={x:sc.x??600,sol:SOL,angle:0,sx:1,sy:1,saut:0,ventre:0,tAngle:0,tdx:0,tdy:0,tsx:1,tsy:1,oeil:'normal',grand:1,px:0,py:0,pupille:1,lid:0,brillant:0,coeur:1,sourcilY:0,sourcilTilt:0,sourcilArc:0,bouche:'sourire',k:1,ouvre:0,joues:0,teinte:0,teinteCouleur:'#FF7A7A',g:{...BRAS.repos.g},d:{...BRAS.repos.d}};
  const apres=sc.f(t,o,S)||null; P.pose(o); if(apres) apres(); }
function demarrer(cle){ COURANTE=SCENES[cle]; document.getElementById('titre').textContent=COURANTE.titre; window.DUREE=COURANTE.duree; }
