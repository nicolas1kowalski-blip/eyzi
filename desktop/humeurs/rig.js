// Le personnage d'Eyzi (le « i » du logo), vu de face, et ses outils d'animation.
const N="#2A1C4E", SVGNS="http://www.w3.org/2000/svg";
const el=(n,a,p)=>{const e=document.createElementNS(SVGNS,n);for(const k in a)e.setAttribute(k,a[k]);(p||document.getElementById('s')).appendChild(e);return e;};
const clamp=(x,a=0,b=1)=>Math.max(a,Math.min(b,x));
const seg=(t,a,b)=>clamp((t-a)/(b-a));
const ease=x=>x<.5?2*x*x:1-Math.pow(-2*x+2,2)/2;
const out=x=>1-Math.pow(1-x,3);
const back=x=>{const c=1.70158;return 1+(c+1)*Math.pow(x-1,3)+c*Math.pow(x-1,2);};
const elast=x=>x<=0?0:x>=1?1:Math.pow(2,-9*x)*Math.sin((x*10-.75)*(2*Math.PI/3))+1;
const cloche=x=>Math.sin(Math.PI*clamp(x));
const lerp=(a,b,x)=>a+(b-a)*x;
const rnd=i=>{const x=Math.sin(i*127.1+311.7)*43758.5453;return x-Math.floor(x);};
const rad=a=>a*Math.PI/180;
const mix=(c1,c2,x)=>{const p=c=>[1,3,5].map(i=>parseInt(c.substr(i,2),16));const a=p(c1),b=p(c2);return '#'+a.map((v,i)=>Math.round(lerp(v,b[i],clamp(x))).toString(16).padStart(2,'0')).join('');};
const coeurD=(x,y,k)=>`M${x},${y+9*k} C${x-22*k},${y-6*k} ${x-10*k},${y-20*k} ${x},${y-9*k} C${x+10*k},${y-20*k} ${x+22*k},${y-6*k} ${x},${y+9*k} Z`;

function creerPerso(parent){
  const defs=el('defs',{},parent.ownerSVGElement||parent);
  defs.innerHTML=`<radialGradient id="gTete" cx="38%" cy="30%" r="80%"><stop offset="0" stop-color="#FFE27A"/><stop offset=".62" stop-color="#FFD24A"/><stop offset="1" stop-color="#F0A81C"/></radialGradient>
  <linearGradient id="gCorps" x1="0" x2="1"><stop offset="0" stop-color="#FFDF6E"/><stop offset=".6" stop-color="#FFD24A"/><stop offset="1" stop-color="#EFA51A"/></linearGradient>
  <radialGradient id="gCoeur" cx="35%" cy="28%" r="80%"><stop offset="0" stop-color="#FF8FA8"/><stop offset=".6" stop-color="#FF4F7B"/><stop offset="1" stop-color="#D92B5C"/></radialGradient>`;
  const ombre=el('ellipse',{cy:2,rx:110,ry:16,fill:N,opacity:.13},parent);
  const G=el('g',{},parent);
  const P={G,ombre};
  const mkBras=g=>({g,c:el('path',{fill:'none',stroke:N,'stroke-width':40,'stroke-linecap':'round','stroke-linejoin':'round'},g),f:el('path',{fill:'none',stroke:'#FFD24A','stroke-width':27,'stroke-linecap':'round','stroke-linejoin':'round'},g),pouce:el('circle',{r:11,fill:'#FFD24A',stroke:N,'stroke-width':6},g),m:el('circle',{r:23,fill:'#FFD24A',stroke:N,'stroke-width':7},g),r:el('path',{fill:'none',stroke:'#fff','stroke-width':5,'stroke-linecap':'round',opacity:.55},g)});
  P.gDos=mkBras(el('g',{},G)); P.dDos=mkBras(el('g',{},G));
  P.sousCorps=el('g',{},G);
  const corps=el('g',{},G);
  P.corpsF=el('path',{fill:'url(#gCorps)',stroke:N,'stroke-width':9,'stroke-linejoin':'round'},corps);
  P.corpsR=el('path',{d:'M-28,-168 q-8,40 -4,84',fill:'none',stroke:'#fff','stroke-width':9,'stroke-linecap':'round',opacity:.5},corps);
  P.surCorps=el('g',{},G);
  const tete=el('g',{},G); P.tete=tete;
  P.teteF=el('circle',{r:92,fill:'url(#gTete)',stroke:N,'stroke-width':9},tete);
  P.teinte=el('circle',{r:88,fill:'#FF7A7A',opacity:0},tete);
  el('path',{d:'M-62,-40 q14,-34 52,-44',fill:'none',stroke:'#fff','stroke-width':12,'stroke-linecap':'round',opacity:.6},tete);
  P.joue1=el('ellipse',{cx:-52,cy:30,rx:19,ry:14,fill:'#FF7A7A',opacity:0},tete);
  P.joue2=el('ellipse',{cx:62,cy:34,rx:15,ry:12,fill:'#FF7A7A',opacity:0},tete);
  const oe=el('g',{},tete); P.oeil=oe;
  P.blanc=el('ellipse',{fill:'#fff',stroke:N,'stroke-width':8},oe);
  P.pupille=el('circle',{fill:N},oe); P.eclat=el('circle',{fill:'#fff'},oe); P.eclat2=el('circle',{fill:'#fff',opacity:0},oe);
  const cpo=el('clipPath',{id:'clipOeil'},defs); P.clipOeil=el('ellipse',{},cpo);
  P.paupiere=el('path',{fill:'#F0B62A',stroke:N,'stroke-width':8,'stroke-linejoin':'round','clip-path':'url(#clipOeil)'},oe);
  P.contourOeil=el('ellipse',{fill:'none',stroke:N,'stroke-width':8},oe);
  P.heureux=el('path',{fill:'none',stroke:N,'stroke-width':12,'stroke-linecap':'round',opacity:0},tete);
  P.serre=el('path',{fill:'none',stroke:N,'stroke-width':11,'stroke-linecap':'round','stroke-linejoin':'round',opacity:0},tete);
  P.ferme=el('path',{fill:'none',stroke:N,'stroke-width':12,'stroke-linecap':'round',opacity:0},tete);
  P.oeilCoeur=el('path',{fill:'url(#gCoeur)',stroke:N,'stroke-width':7,'stroke-linejoin':'round',opacity:0},tete);
  P.oeilCoeurR=el('path',{fill:'none',stroke:'#fff','stroke-width':6,'stroke-linecap':'round',opacity:0},tete);
  P.sourcil=el('path',{fill:'none',stroke:N,'stroke-width':11,'stroke-linecap':'round'},tete);
  P.bouche=el('path',{fill:'#5B1230',stroke:N,'stroke-width':8,'stroke-linejoin':'round','stroke-linecap':'round'},tete);
  P.langue=el('path',{fill:'#FF6F91',opacity:0},tete);
  P.dents=el('path',{fill:'#fff',stroke:N,'stroke-width':5,'stroke-linejoin':'round',opacity:0},tete);
  P.surTete=el('g',{},tete);
  P.gFace=mkBras(el('g',{},G)); P.dFace=mkBras(el('g',{},G));

  function bras(dos,face,ex,ey,o){ const b=o.devant?face:dos; dos.g.setAttribute('opacity',o.devant?0:1); face.g.setAttribute('opacity',o.devant?1:0);
    const mx=o.x,my=o.y,pl=o.plie??.4; const cx=(ex+mx)/2+pl*(my-ey)*.5, cy=(ey+my)/2-pl*(mx-ex)*.5; const d=`M${ex},${ey} Q${cx},${cy} ${mx},${my}`;
    b.c.setAttribute('d',d); b.f.setAttribute('d',d); const r=o.main??23; b.m.setAttribute('cx',mx); b.m.setAttribute('cy',my); b.m.setAttribute('r',r);
    const pa=rad(o.pouce??-70); b.pouce.setAttribute('cx',mx+Math.cos(pa)*r*.95); b.pouce.setAttribute('cy',my+Math.sin(pa)*r*.95); b.pouce.setAttribute('opacity',o.pouce===null?0:1);
    b.r.setAttribute('d',`M${mx-r*.45},${my-r*.35} q${r*.2},${-r*.35} ${r*.55},${-r*.3}`); }

  P.pose=function(o){
    o=Object.assign({x:640,sol:610,angle:0,sx:1,sy:1,saut:0,ventre:0,tAngle:0,tdx:0,tdy:0,tsx:1,tsy:1,
      oeil:'normal',grand:1,px:0,py:0,pupille:1,lid:0,brillant:0,coeur:1,
      sourcilY:0,sourcilTilt:0,sourcilArc:0,bouche:'sourire',k:1,ouvre:0,joues:0,teinte:0,teinteCouleur:'#FF7A7A',
      g:{x:-70,y:-72},d:{x:70,y:-72}},o);
    G.setAttribute('transform',`translate(${o.x},${o.sol+o.saut}) rotate(${o.angle}) scale(${o.sx},${o.sy})`);
    ombre.setAttribute('cx',o.x); ombre.setAttribute('cy',o.sol+2); ombre.setAttribute('rx',110*o.sx*(1+o.saut/260)); ombre.setAttribute('opacity',.13*(1+o.saut/200));
    const v=o.ventre; P.corpsF.setAttribute('d',`M-52,-52 C${-52-v},-90 ${-52-v},-140 -52,-176 A52,52 0 0 1 52,-176 C${52+v},-140 ${52+v},-90 52,-52 A52,52 0 0 1 -52,-52 Z`);
    tete.setAttribute('transform',`translate(${o.tdx},${-262+o.tdy}) rotate(${o.tAngle}) scale(${o.tsx},${o.tsy})`);
    P.teinte.setAttribute('opacity',o.teinte); P.teinte.setAttribute('fill',o.teinteCouleur);
    P.joue1.setAttribute('opacity',.7*o.joues); P.joue2.setAttribute('opacity',.7*o.joues);
    // l'œil
    const ox=16, oy=-20, R=40*o.grand;
    const m=o.oeil; oe.setAttribute('opacity',m==='normal'?1:0); P.heureux.setAttribute('opacity',m==='heureux'?1:0); P.serre.setAttribute('opacity',m==='serre'?1:0); P.ferme.setAttribute('opacity',m==='ferme'?1:0);
    P.oeilCoeur.setAttribute('opacity',m==='coeur'?1:0); P.oeilCoeurR.setAttribute('opacity',m==='coeur'?.8:0);
    for(const e of [P.blanc,P.clipOeil,P.contourOeil]){ e.setAttribute('cx',ox); e.setAttribute('cy',oy); e.setAttribute('rx',R); e.setAttribute('ry',R); }
    const pr=18*o.pupille, pxx=ox+clamp(o.px,-1,1)*(R-pr-5), pyy=oy+clamp(o.py,-1,1)*(R-pr-5);
    P.pupille.setAttribute('cx',pxx); P.pupille.setAttribute('cy',pyy); P.pupille.setAttribute('r',pr);
    P.eclat.setAttribute('cx',pxx+pr*.35); P.eclat.setAttribute('cy',pyy-pr*.4); P.eclat.setAttribute('r',pr*.34);
    P.eclat2.setAttribute('cx',pxx-pr*.35); P.eclat2.setAttribute('cy',pyy+pr*.35); P.eclat2.setAttribute('r',pr*.2); P.eclat2.setAttribute('opacity',o.brillant);
    const ly=oy-R+2*R*o.lid; P.paupiere.setAttribute('d',`M${ox-R},${oy} A${R},${R} 0 0 1 ${ox+R},${oy} L${ox+R},${ly} Q${ox},${ly+12*o.lid} ${ox-R},${ly} Z`); P.paupiere.setAttribute('opacity',o.lid>.03?1:0);
    P.heureux.setAttribute('d',`M${ox-34},${oy+10} Q${ox},${oy-34} ${ox+34},${oy+10}`);
    P.ferme.setAttribute('d',`M${ox-34},${oy-6} Q${ox},${oy+26} ${ox+34},${oy-6}`);
    P.serre.setAttribute('d',`M${ox-40},${oy-26} L${ox-10},${oy} L${ox-40},${oy+26} M${ox+40},${oy-26} L${ox+10},${oy} L${ox+40},${oy+26}`);
    P.oeilCoeur.setAttribute('d',coeurD(ox,oy+2,2.6*o.coeur)); P.oeilCoeurR.setAttribute('d',`M${ox-30*o.coeur},${oy-14*o.coeur} q${6*o.coeur},${-16*o.coeur} ${20*o.coeur},${-14*o.coeur}`);
    const sy=oy-R-18-o.sourcilY; P.sourcil.setAttribute('d',`M${ox-40},${sy+o.sourcilTilt} Q${ox},${sy-10-o.sourcilArc} ${ox+42},${sy-o.sourcilTilt}`);
    // la bouche
    const bx=-6, by=46, k=o.k; P.langue.setAttribute('opacity',0); P.dents.setAttribute('opacity',0); P.bouche.setAttribute('fill','#5B1230');
    if(o.bouche==='sourire') { P.bouche.setAttribute('d',`M${bx-30*k},${by-4} Q${bx},${by+26*k} ${bx+32*k},${by-6} Q${bx},${by+10*k} ${bx-30*k},${by-4} Z`); }
    else if(o.bouche==='rire') { const a=o.ouvre; P.bouche.setAttribute('d',`M${bx-36},${by-10} Q${bx},${by-2} ${bx+38},${by-12} Q${bx+34},${by+16+34*a} ${bx},${by+18+36*a} Q${bx-34},${by+16+34*a} ${bx-36},${by-10} Z`);
      P.langue.setAttribute('d',`M${bx-20},${by+16+30*a} q20,${-16-8*a} 42,-2 q-20,${16} -42,2 Z`); P.langue.setAttribute('opacity',a>.25?1:0);
      P.dents.setAttribute('d',`M${bx-30},${by-8} Q${bx},${by-1} ${bx+32},${by-10} L${bx+30},${by+2} Q${bx},${by+10} ${bx-28},${by+3} Z`); P.dents.setAttribute('opacity',a>.25?1:0); }
    else if(o.bouche==='o') { const r=6+12*o.ouvre; P.bouche.setAttribute('d',`M${bx-r},${by+4} a${r},${r*1.25} 0 1 0 ${2*r},0 a${r},${r*1.25} 0 1 0 ${-2*r},0 Z`); }
    else if(o.bouche==='bisou') { P.bouche.setAttribute('fill','#FF6F91'); P.bouche.setAttribute('d',`M${bx-4},${by-12} q16,4 4,12 q14,6 -4,14 q-12,-4 -6,-13 q-8,-8 6,-13 Z`); }
    else if(o.bouche==='dents') { P.bouche.setAttribute('fill','#fff'); P.bouche.setAttribute('d',`M${bx-34},${by-12} h68 a10,10 0 0 1 10,10 v6 a10,10 0 0 1 -10,10 h-68 a10,10 0 0 1 -10,-10 v-6 a10,10 0 0 1 10,-10 Z M${bx-18},${by-12} v26 M${bx},${by-12} v26 M${bx+18},${by-12} v26`); }
    else if(o.bouche==='vague') { P.bouche.setAttribute('fill',N); P.bouche.setAttribute('d',`M${bx-28},${by+4} q7,-10 14,0 t14,0 t14,0 t14,0 q-28,4 -56,0 Z`); }
    else if(o.bouche==='triste') { P.bouche.setAttribute('fill',N); P.bouche.setAttribute('d',`M${bx-26*k},${by+14} Q${bx},${by-16*k} ${bx+28*k},${by+14} Q${bx},${by-6*k} ${bx-26*k},${by+14} Z`); }
    else if(o.bouche==='plat') { P.bouche.setAttribute('fill',N); P.bouche.setAttribute('d',`M${bx-22*k},${by+2} h${46*k} v4 h${-46*k} Z`); }
    else if(o.bouche==='langue') { P.bouche.setAttribute('d',`M${bx-30},${by-4} Q${bx},${by+24} ${bx+32},${by-6} Q${bx},${by+8} ${bx-30},${by-4} Z`);
      const L=18+26*o.ouvre; P.langue.setAttribute('d',`M${bx-14},${by+8} q-4,${L} 14,${L+2} q20,0 16,${-L-2} Z`); P.langue.setAttribute('opacity',1); P.langue.setAttribute('stroke',N); P.langue.setAttribute('stroke-width',6); }
    else if(o.bouche==='petit') { P.bouche.setAttribute('fill',N); P.bouche.setAttribute('d',`M${bx-16},${by+2} q16,${10*k} 32,0 q-16,4 -32,0 Z`); }
    bras(P.gDos,P.gFace,-40,-172,o.g); bras(P.dDos,P.dFace,40,-172,o.d);
    P.derniere=o;
  };
  // Un point du personnage (repère : pieds en 0,0) dans le repère de la scène ; « tete » : point donné dans le repère de la tête.
  P.monde=function(x,y,tete){ const o=P.derniere; if(tete){ const a=rad(o.tAngle); const X=x*o.tsx, Y=y*o.tsy; x=X*Math.cos(a)-Y*Math.sin(a)+o.tdx; y=X*Math.sin(a)+Y*Math.cos(a)-262+o.tdy; }
    const X=x*o.sx, Y=y*o.sy, a=rad(o.angle); return [o.x+X*Math.cos(a)-Y*Math.sin(a), o.sol+o.saut+X*Math.sin(a)+Y*Math.cos(a)]; };
  P.main=function(c){ const o=P.derniere, m=c==='g'?o.g:o.d; return P.monde(m.x,m.y); };
  return P;
}
