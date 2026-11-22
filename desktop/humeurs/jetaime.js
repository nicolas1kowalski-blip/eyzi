const DUREE=6.4, SOL=610, X=600;
const fond=document.getElementById('fond'), devant=document.getElementById('devant');
const flottants=[...Array(10)].map(()=>{const g=el('g',{opacity:0},fond); el('path',{fill:'url(#gCoeur)',stroke:N,'stroke-width':5,'stroke-linejoin':'round',d:coeurD(0,0,1.5)},g); el('path',{d:'M-12,-10 q2,-6 8,-7',fill:'none',stroke:'#fff','stroke-width':3.5,'stroke-linecap':'round',opacity:.8},g); return g;});
const P=creerPerso(document.getElementById('persoG'));
const petitCoeur=el('path',{fill:'url(#gCoeur)',stroke:N,'stroke-width':5,'stroke-linejoin':'round',opacity:0},P.surCorps);
const pops=[...Array(7)].map(()=>{const g=el('g',{opacity:0},devant); el('path',{fill:'url(#gCoeur)',stroke:N,'stroke-width':5,'stroke-linejoin':'round',d:coeurD(0,0,1.2)},g); return g;});
const grandG=el('g',{opacity:0},devant); const grand=el('path',{fill:'url(#gCoeur)',stroke:N,'stroke-width':9,'stroke-linejoin':'round',d:coeurD(0,0,4.6)},grandG); const grandR=el('path',{d:'M-40,-32 q4,-18 22,-22',fill:'none',stroke:'#fff','stroke-width':9,'stroke-linecap':'round',opacity:.85},grandG); const grandR2=el('circle',{cx:-43,cy:-12,r:5,fill:'#fff',opacity:.85},grandG);
const etoiles=[...Array(6)].map(()=>el('path',{fill:'#FFE066',stroke:N,'stroke-width':4,'stroke-linejoin':'round',opacity:0},devant));
const eclats=[...Array(12)].map(()=>{const g=el('g',{opacity:0},devant); el('path',{fill:'url(#gCoeur)',stroke:N,'stroke-width':5,'stroke-linejoin':'round',d:coeurD(0,0,1.1)},g); return g;});
const txt=el('text',{'font-weight':900,'font-family':'Arial Black,Arial',stroke:N,'stroke-width':2.5,opacity:0},devant);
const etoileD=(x,y,r)=>{let d='';for(let i=0;i<8;i++){const a=i*Math.PI/4, rr=i%2?r*.38:r; d+=(i?' L':'M')+(x+Math.sin(a)*rr)+','+(y-Math.cos(a)*rr);} return d+' Z';};
const battement=t=>{const p=(t%0.85)/0.85; return Math.exp(-Math.pow((p-.08)/.05,2))+.7*Math.exp(-Math.pow((p-.3)/.06,2));};

function rendu(T){
  const t=((T%DUREE)+DUREE)%DUREE;
  const retour=ease(seg(t,5.9,6.4));                         // il redevient timide pour la boucle
  const timide=Math.max(1-ease(seg(t,0.9,1.5)),retour);
  const rougit=Math.max(ease(seg(t,0.15,0.9)),.0);
  const leve=ease(seg(t,0.9,1.5))*(1-retour);
  const oc=seg(t,1.7,2.2);
  const ouvreBras=cloche(seg(t,2.35,3.0));
  const joint=ease(seg(t,2.75,3.15))*(1-ease(seg(t,4.15,4.35)));
  const gc=seg(t,2.95,3.6);                                  // le grand cœur apparaît
  const elan=cloche(seg(t,4.0,4.3));
  const envoi=seg(t,4.3,4.95);
  const bisou= t>4.05&&t<5.0 ? 1:0;
  const calin=ease(seg(t,5.0,5.35))*(1-retour);
  const b=battement(t);
  const o={x:X,sol:SOL};
  o.angle = 5*Math.sin(t*3.2)*timide + 2.5*Math.sin(t*5)*leve*(1-calin) + 7*Math.sin(t*4.2)*calin - 5*elan + 6*out(envoi)*(1-calin);
  o.saut = -18*Math.abs(Math.sin((t-3.1)*7))*clamp((t-3.1)*4)*clamp((4.0-t)*4);
  o.sy = 1 + .03*b*leve - .05*elan + .04*cloche(envoi); o.sx = 1 - .02*b*leve + .04*elan;
  o.tAngle = -12*timide + 8*Math.sin(t*4.2)*calin + 4*Math.sin(t*5)*leve*(1-calin) - 6*elan;
  o.oeil = (t>1.7 && t<4.05 && retour<.01) ? 'coeur' : (bisou||calin>.3) && retour<.4 ? 'heureux' : 'normal';
  o.coeur = elast(oc)*(1+.14*b);
  o.px = -.5*timide; o.py = .7*timide; o.lid=.38*timide; o.grand=1+.22*leve; o.pupille=1+.3*leve; o.brillant=leve;
  o.sourcilY = -4*timide + 10*leve; o.sourcilArc = 8*leve;
  o.bouche = bisou ? 'bisou' : 'sourire'; o.k = lerp(.45,1.15,leve) + .1*calin; if(timide>.6) { o.bouche='petit'; o.k=.9; }
  o.joues = Math.min(1,rougit)*(1-0*retour); o.teinte=.10*leve;
  // les bras : dans le dos (timide) → grands ouverts → joints sur le cœur → bisou soufflé → câlin
  let g={x:-22,y:-52,plie:-.9}, d={x:22,y:-52,plie:.9};
  const L=(a,bb,k)=>({x:lerp(a.x,bb.x,k),y:lerp(a.y,bb.y,k),plie:lerp(a.plie??.4,bb.plie??.4,k),devant:k>.5?bb.devant:a.devant,main:bb.main});
  const repos={g:{x:-72,y:-74,plie:.4},d:{x:72,y:-74,plie:-.4}};
  g=L(g,repos.g,leve); d=L(d,repos.d,leve);
  g=L(g,{x:-150,y:-222,plie:.25},ouvreBras*(1-joint)); d=L(d,{x:150,y:-222,plie:-.25},ouvreBras*(1-joint));
  g=L(g,{x:-70,y:-118,plie:.75,devant:true},joint); d=L(d,{x:70,y:-118,plie:-.75,devant:true},joint);
  if(t>4.0&&t<5.0){ const k=out(envoi); g=L(g,{x:-110,y:-150,plie:.3},clamp((t-4.05)*5)); d=L({x:14,y:-214,plie:-1,devant:true},{x:150,y:-250,plie:-.3,devant:true,main:26},k); }
  g=L(g,{x:44,y:-120,plie:1.0,devant:true},calin); d=L(d,{x:-44,y:-98,plie:-1.0,devant:true},calin);
  o.g=g; o.d=d;
  P.pose(o);
  // le petit cœur qui bat sur la poitrine
  petitCoeur.setAttribute('d',coeurD(0,-122,1.0+.45*b)); petitCoeur.setAttribute('opacity', (t>0.2&&t<2.9)||t>5.1 ? clamp((t-.2)*4)*(t<2.9?1-seg(t,2.7,2.9):clamp((t-5.1)*4)*(1-retour*0)) : 0);
  // les cœurs qui éclosent autour de la tête
  pops.forEach((p,i)=>{ const naiss=1.75+i*0.16, age=t-naiss; if(age<0||age>1.5||t>5.9){p.setAttribute('opacity',0);return;}
    const a=rad(-170+i*27+rnd(i)*10), r=150+rnd(i+3)*30; const x=X+Math.cos(a)*r, y=SOL-262+Math.sin(a)*r-age*40;
    p.setAttribute('transform',`translate(${x},${y}) rotate(${-14+rnd(i)*28}) scale(${elast(clamp(age/.5))*(0.8+rnd(i+6)*.6)})`); p.setAttribute('opacity',1-clamp((age-1.0)/.5)); });
  // le grand cœur, tenu entre les mains, puis envoyé
  const cy0=SOL-150+o.saut;
  let gs=elast(gc)*(1+.10*b), gx=X, gy=cy0, go=t>2.95?1:0, gr=0;
  if(t>4.0){ gs*=1-.12*elan; }
  if(envoi>0){ const k=envoi; gs=lerp(1,2.6,out(k)); gx=X-10; gy=lerp(cy0,SOL-250,out(k)); go=1-clamp((k-.72)/.28); gr=-8*k; }
  if(t>=4.95) go=0;
  grandG.setAttribute('transform',`translate(${gx},${gy}) rotate(${gr}) scale(${gs})`); grandG.setAttribute('opacity',go);
  etoiles.forEach((e,i)=>{ const on=t>3.3&&t<4.2; const a=rad(i*60+t*40), r=120+16*Math.sin(t*5+i); const tw=.5+.5*Math.sin(t*9+i*2); e.setAttribute('d',etoileD(X+Math.cos(a)*r,cy0+Math.sin(a)*r*.8,10+10*tw)); e.setAttribute('opacity',on?tw:0); });
  // l'éclatement : douze petits cœurs
  eclats.forEach((e,i)=>{ const age=t-4.78; if(age<0||age>1.0){e.setAttribute('opacity',0);return;} const a=rad(i*30+10), r=60+ (260+rnd(i)*120)*out(clamp(age/.8));
    e.setAttribute('transform',`translate(${X-10+Math.cos(a)*r},${SOL-250+Math.sin(a)*r*.8+age*age*90}) rotate(${age*200*(i%2?1:-1)}) scale(${1.5-age})`); e.setAttribute('opacity',1-clamp((age-.6)/.4)); });
  // les cœurs qui montent comme des bulles
  flottants.forEach((f,i)=>{ const debut=1.9+i*0.36, age=t-debut; if(age<0||t>6.0){f.setAttribute('opacity',0);return;} const ph=(age*.42)%1; const x=X-400+i*88+ (i%2?30:-20)+26*Math.sin(ph*7+i), y=SOL-20-ph*470;
    f.setAttribute('transform',`translate(${x},${y}) rotate(${12*Math.sin(ph*9+i)}) scale(${.7+rnd(i)*.9})`); f.setAttribute('opacity',Math.min(clamp(ph*6),1-clamp((ph-.75)/.25))*(1-clamp((t-5.6)/.4))*.95); });
  // les mots
  let a='',ax=0,ay=0,as=44,ao=0,ac='#FF4F7B';
  if(t>0.3&&t<1.6){ a='boum boum'; ax=X+84; ay=SOL-118; as=30+8*b; ao=.9*clamp((t-.3)*3)*(1-seg(t,1.3,1.6)); ac='#FF8FA8'; }
  if(t>4.3&&t<4.95){ a='smack !'; ax=X+150; ay=SOL-330-30*envoi; as=52+10*cloche(envoi); ao=cloche(seg(t,4.3,4.95)); }
  txt.textContent=a; txt.setAttribute('x',ax); txt.setAttribute('y',ay); txt.setAttribute('font-size',as); txt.setAttribute('opacity',ao); txt.setAttribute('fill',ac);
}
