const DUREE=6.0, SOL=610, X=560;
const fond=document.getElementById('fond'), devant=document.getElementById('devant');
// la fleur, à droite, qui va se faner
const FX=X+350; const tige=el('path',{fill:'none',stroke:'#4E9B3A','stroke-width':10,'stroke-linecap':'round'},fond);
const tigeC=el('path',{fill:'none',stroke:N,'stroke-width':18,'stroke-linecap':'round'},fond); fond.insertBefore(tigeC,tige);
const feuille=el('path',{fill:'#6CBF4B',stroke:N,'stroke-width':6,'stroke-linejoin':'round'},fond);
const fleur=el('g',{},fond); const petales=[...Array(7)].map((_,i)=>el('ellipse',{rx:17,ry:25,stroke:N,'stroke-width':6},fleur)); const coeurF=el('circle',{r:17,fill:'#FFC83D',stroke:N,'stroke-width':6},fleur);
const petaleTombe=el('ellipse',{rx:17,ry:25,stroke:N,'stroke-width':6,opacity:0},fond);
const nuageG=el('g',{},fond); for(const e of [tigeC,tige,feuille,fleur,petaleTombe]) fond.appendChild(e);   // la fleur reste visible devant le nuage
const P=creerPerso(document.getElementById('persoG'));
const traits=[...Array(6)].map(()=>el('path',{fill:'none',stroke:N,'stroke-width':7,'stroke-linecap':'round',opacity:0},fond));
const bouffees=[...Array(13)].map((_,i)=>{const g=el('g',{opacity:0},nuageG); el('circle',{r:10,fill:i%3?'#DCEB74':'#C9DE5C',stroke:N,'stroke-width':7},g); el('path',{d:'M-5,-4 q4,-4 9,-2',fill:'none',stroke:'#F4FBC0','stroke-width':3,'stroke-linecap':'round'},g); return g;});
const odeurs=[...Array(4)].map(()=>el('path',{fill:'none',stroke:'#A9BE3F','stroke-width':7,'stroke-linecap':'round',opacity:0},devant));
const notes=[...Array(3)].map(()=>el('text',{'font-size':44,'font-family':'Arial',fill:'#7E3FF2',stroke:N,'stroke-width':1.5,opacity:0},devant));
notes.forEach((n,i)=>n.textContent=i%2?'♪':'♫');
const mouche=el('g',{opacity:0},devant); const ailes=[el('ellipse',{cx:-9,cy:-9,rx:12,ry:6.5,fill:'#fff',stroke:N,'stroke-width':3,opacity:.9},mouche),el('ellipse',{cx:9,cy:-9,rx:12,ry:6.5,fill:'#fff',stroke:N,'stroke-width':3,opacity:.9},mouche)]; el('circle',{r:10,fill:N},mouche); const mx1=el('path',{d:'M-6,-3 l5,5 m0,-5 l-5,5 M2,-3 l5,5 m0,-5 l-5,5',stroke:'#fff','stroke-width':2.2,'stroke-linecap':'round',opacity:0},mouche);
const txt=el('text',{'font-weight':900,'font-family':'Arial Black,Arial',stroke:N,'stroke-width':3,opacity:0},devant);
const txt2=el('text',{'font-weight':900,'font-family':'Arial Black,Arial',stroke:N,'stroke-width':2.5,opacity:0},devant);
const gouttes=[...Array(3)].map(()=>el('path',{fill:'#7FD3FF',stroke:N,'stroke-width':4,opacity:0},devant));

function rendu(T){
  const t=((T%DUREE)+DUREE)%DUREE;
  const siffle=1-ease(seg(t,0.9,1.15));
  const garg=cloche(seg(t,0.95,1.75));
  const retient=ease(seg(t,1.5,2.05))*(1-seg(t,2.5,2.58));
  const guette=t>2.1&&t<2.5?1:0;
  const pr=seg(t,2.55,3.25), force=Math.min(1,pr*7)*(1-Math.pow(pr,3));
  const decolle=cloche(seg(t,2.55,2.95));
  const surpris=cloche(seg(t,3.25,3.75));
  const rit= t>3.7&&t<4.9 ? Math.min(1,(t-3.7)*5)*Math.min(1,(4.9-t)*5) : 0;
  const evente= t>4.85&&t<5.75 ? Math.min(1,(t-4.85)*5)*Math.min(1,(5.75-t)*5) : 0;
  const fin=seg(t,5.6,6.0);
  const o={x:X,sol:SOL};
  o.angle = 4*Math.sin(t*4)*siffle + 1.6*Math.sin(t*46)*retient - 13*force + 3*Math.sin(t*26)*rit + 2*Math.sin(t*3)*evente;
  o.saut = -34*decolle;
  o.sy = 1 - 0.07*retient + 0.07*force - 0.04*rit*Math.abs(Math.sin(t*13)); o.sx = 1 + 0.07*retient - 0.04*force;
  o.ventre = 12*garg*Math.abs(Math.sin(t*13)) + 6*retient;
  o.tAngle = -8*siffle*Math.sin(t*4+1) + 6*garg - 8*force + 6*Math.sin(t*26)*rit; o.tdy = 5*retient;
  // l'œil
  o.oeil='normal'; o.px=.55*siffle; o.py=-.6*siffle; o.lid=.18*siffle; o.grand=1; o.pupille=1;
  if(garg>.15){ o.px=-.15; o.py=.85*garg; o.grand=1+.18*garg; }
  if(retient>.55) o.oeil='serre';
  if(guette){ o.oeil='normal'; o.px=Math.sin((t-2.1)*17); o.py=.1; o.grand=1.12; o.pupille=.8; }
  if(force>.1||decolle>.1) o.oeil='heureux';
  if(surpris>.1){ o.oeil='normal'; o.grand=1+.3*surpris; o.px=.75; o.py=.25; o.pupille=.7; }
  if(rit>.2) o.oeil='heureux';
  if(evente>.2){ o.oeil='normal'; o.px=.6; o.py=.1; o.lid=.3; }
  o.sourcilY = -8*retient + 14*surpris + 4*siffle; o.sourcilTilt = 12*retient - 4*rit; o.sourcilArc=6*surpris;
  // la bouche
  o.bouche='o'; o.ouvre=.3+.15*Math.sin(t*9);
  if(siffle<.5){ o.bouche='vague'; }
  if(retient>.3){ o.bouche='dents'; }
  if(guette){ o.bouche='petit'; o.k=-.6; }
  if(force>.1||decolle>.1){ o.bouche='rire'; o.ouvre=.7; }
  if(surpris>.1){ o.bouche='o'; o.ouvre=.75*surpris; }
  if(rit>.2){ o.bouche='rire'; o.ouvre=.45+.45*Math.abs(Math.sin(t*14)); }
  if(evente>.2||fin>0&&t>5.6){ o.bouche='sourire'; o.k=.9; }
  o.joues = Math.max(retient, .9*clamp((t-2.55)*3)*(1-fin)); o.teinte=.32*retient; 
  // les bras
  o.g={x:-72+6*Math.sin(t*4),y:-74}; o.d={x:72+6*Math.sin(t*4),y:-74};
  const surVentre=clamp(garg*1.6); if(surVentre>.05){ o.g={x:lerp(o.g.x,-6,surVentre),y:lerp(o.g.y,-104,surVentre),devant:surVentre>.5,plie:.6}; }
  if(retient>.05){ o.g={x:lerp(o.g.x,-34,retient),y:lerp(o.g.y,-30,retient),plie:-.7}; o.d={x:lerp(72,36,retient),y:lerp(-74,-28,retient),plie:.7}; }
  if(force>.05||decolle>.05){ const k=Math.max(force,decolle); o.g={x:lerp(-72,-128,k),y:lerp(-74,-214,k),plie:.3}; o.d={x:lerp(72,116,k),y:lerp(-74,-226,k),plie:-.3}; }
  if(rit>.05){ o.g={x:lerp(-72,-2,rit),y:lerp(-74,-214+6*Math.sin(t*26),rit),devant:rit>.5,plie:.9}; o.d={x:lerp(72,8,rit),y:lerp(-74,-104,rit),devant:rit>.5,plie:-.7}; }
  if(evente>.05){ o.d={x:lerp(72,132+34*Math.sin(t*17),evente),y:lerp(-74,-160+10*Math.cos(t*17),evente),plie:-.5,main:26}; o.g={x:-58,y:-96,plie:.9}; }
  P.pose(o);
  // ---- le nuage ----
  const OX=X+66, OY=SOL-64;
  bouffees.forEach((b,i)=>{ const naiss=2.57+i*0.04, age=t-naiss; if(age<0||fin>=1){b.setAttribute('opacity',0);return;}
    const a=rad(-34+rnd(i)*52), dist=(70+rnd(i+4)*230)*out(clamp(age/1.3))+age*14, r=(14+rnd(i+8)*8)+(24+rnd(i+2)*26)*out(clamp(age/1.1));
    const x=OX+Math.cos(a)*dist, y=OY+Math.sin(a)*dist-age*10+6*Math.sin(t*3+i);
    b.setAttribute('transform',`translate(${x},${y}) scale(${r/10})`); b.firstChild.setAttribute('stroke-width',7*10/r); b.lastChild.setAttribute('stroke-width',3*10/r*1.6);
    b.setAttribute('opacity',clamp(age*12)*(1-clamp((age-1.9)/1.0))*(1-fin)); });
  traits.forEach((l,i)=>{ const a=rad(-32+i*11), k=seg(t,2.55,2.8); const r1=40+k*110, r2=r1+50*(1-k)+20; l.setAttribute('d',`M${OX+Math.cos(a)*r1},${OY+Math.sin(a)*r1} L${OX+Math.cos(a)*r2},${OY+Math.sin(a)*r2}`); l.setAttribute('opacity',t>2.55&&t<2.85?1-k:0); });
  odeurs.forEach((od,i)=>{ const on=t>3.2&&t<5.7, ph=((t*.6+i*.25)%1); const x=X+190+i*52, y=SOL-150-ph*120; od.setAttribute('d',`M${x},${y} q14,-12 0,-26 q-14,-14 0,-28`); od.setAttribute('opacity',on?.8*(1-ph)*(1-fin):0); });
  // ---- la fleur ----
  const fane=ease(seg(t,3.15,3.9))*(1-elast(fin));
  const hx=FX+70*fane, hy=SOL-150+92*fane;
  const d=`M${FX},${SOL} Q${FX-10+30*fane},${SOL-110} ${hx},${hy}`; tige.setAttribute('d',d); tigeC.setAttribute('d',d);
  feuille.setAttribute('d',`M${FX},${SOL-50} q34,${-30+26*fane} 62,${-14+34*fane} q-30,${20} -62,14 Z`); feuille.setAttribute('fill',mix('#6CBF4B','#A7A35A',fane));
  fleur.setAttribute('transform',`translate(${hx},${hy}) rotate(${150*fane})`);
  petales.forEach((p,i)=>{ const a=i/7*360; p.setAttribute('transform',`rotate(${a}) translate(0,${-28+6*fane}) scale(${1-.12*fane},${1-.25*fane})`); p.setAttribute('fill',mix('#FF8FC1','#B9A98C',fane)); p.setAttribute('opacity',i===2&&fane>.5&&fin<.5?0:1); });
  coeurF.setAttribute('fill',mix('#FFC83D','#A98F3C',fane));
  const ch=seg(t,3.75,4.5); petaleTombe.setAttribute('opacity',ch>0&&fin<.5?1:0); petaleTombe.setAttribute('fill','#B9A98C');
  petaleTombe.setAttribute('transform',`translate(${hx+10+30*Math.sin(ch*6)},${Math.min(SOL-8,hy+ch*ch*120)}) rotate(${ch*240}) scale(.85,.7)`);
  // ---- les notes sifflées ----
  notes.forEach((n,i)=>{ const ph=((t*.9+i*.34)%1); n.setAttribute('x',X+60+ph*110+12*Math.sin(ph*8)); n.setAttribute('y',SOL-250-ph*120); n.setAttribute('opacity',t<1.0?(1-ph)*siffle:0); });
  // ---- la mouche : elle arrive, sent, et tombe raide ----
  const arr=seg(t,3.3,3.95), chute=seg(t,4.05,4.6);
  if(t>3.3&&fin<1){ let mx,my,r=0; if(chute<=0){ mx=lerp(X+520,X+250,ease(arr))+14*Math.sin(t*20); my=lerp(SOL-420,SOL-200,ease(arr))+12*Math.cos(t*23); } else { mx=X+250+20*chute; my=lerp(SOL-200,SOL-12,chute*chute); r=chute*540; }
    mouche.setAttribute('transform',`translate(${mx},${my}) rotate(${chute>=1?180:r})`); mouche.setAttribute('opacity',1-fin); mx1.setAttribute('opacity',arr>=1?1:0);
    ailes.forEach((a,i)=>a.setAttribute('ry',chute>0?6.5:3+4*Math.abs(Math.sin(t*60+i)))); } else mouche.setAttribute('opacity',0);
  // ---- la sueur quand il se retient ----
  gouttes.forEach((g,i)=>{ const ph=((t*1.6+i*.33)%1), x=X+(i-1)*70+ (i-1)*30*ph, y=SOL-350+ph*ph*70; g.setAttribute('d',`M${x},${y} q-8,13 0,19 q8,-6 0,-19 Z`); g.setAttribute('opacity',retient>.3?1-ph:0); });
  // ---- les bruits écrits ----
  let a='',ax=0,ay=0,as=50,ao=0,ac='#C9DE5C', b='',bxx=0,byy=0,bs=44,bo=0,bc='#FF8FC1';
  if(t>1.0&&t<1.7){ a='grrl… grl'; ax=X-250; ay=SOL-110+4*Math.sin(t*30); as=40; ao=cloche(seg(t,1.0,1.7)); ac='#F2B21E'; }
  if(t>2.57&&t<3.5){ const k=seg(t,2.57,2.8); a='PROOOUT !'; ax=X+70+6*Math.sin(t*50); ay=SOL-290+6*Math.cos(t*43); as=36+34*out(k); ao=1-seg(t,3.2,3.5); ac='#C9DE5C'; }
  if(t>3.3&&t<3.75){ b='oups…'; bxx=X-250; byy=SOL-360; bs=46; bo=cloche(seg(t,3.3,3.75)); bc='#7FD3FF'; }
  if(rit>.1){ b='hi hi hi !'; bxx=X-290; byy=SOL-350+5*Math.sin(t*26); bs=50; bo=rit; bc='#FFC83D'; }
  txt.textContent=a; txt.setAttribute('x',ax); txt.setAttribute('y',ay); txt.setAttribute('font-size',as); txt.setAttribute('opacity',ao); txt.setAttribute('fill',ac);
  txt2.textContent=b; txt2.setAttribute('x',bxx); txt2.setAttribute('y',byy); txt2.setAttribute('font-size',bs); txt2.setAttribute('opacity',bo); txt2.setAttribute('fill',bc);
}
