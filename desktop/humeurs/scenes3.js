// La suite des humeurs : rigolo, ça ne va pas, les actions. Mêmes règles que scenes.js (outils de dessin : scenes2.js).
FORMES.flocon=(g,c)=>{ for(let i=0;i<3;i++) el('path',{d:'M0,-14 L0,14 M-5,-9 L0,-4 L5,-9 M-5,9 L0,4 L5,9',fill:'none',stroke:c||'#8FC8FF','stroke-width':4,'stroke-linecap':'round',transform:`rotate(${i*60})`},g); };
FORMES.onde=(g,c)=>el('path',{d:'M0,-34 q-26,34 0,68',fill:'none',stroke:c||N,'stroke-width':8,'stroke-linecap':'round'},g);
FORMES.chaleur=(g,c)=>el('path',{d:'M0,30 q14,-15 0,-30 q-14,-15 0,-30',fill:'none',stroke:c||'#FFB347','stroke-width':6,'stroke-linecap':'round'},g);
const ETOILE=`<path d="M0,-16 L4,-5 L16,-5 L6,3 L10,15 L0,8 L-10,15 L-6,3 L-16,-5 L-4,-5 Z" fill="#FFE066" stroke="${N}" stroke-width="4" stroke-linejoin="round"/>`;

// ───────────────────────────── RIGOLO (suite) ─────────────────────────────
scene('clown',{titre:'Je fais le clown',dit:'Je fais le clown !',cat:'rigolo',duree:6.2,vignette:2.5,cadre:[295,0,600]},(t,o,S)=>{
  const jongle=fen(t,1.2,4.2,.2), pouet=fen(t,4.5,5.7,.12), ph=t*5, presse=Math.pow(Math.abs(Math.sin((t-4.5)*7)),2)*pouet;
  o.x=600+18*Math.sin(ph/2)*jongle; o.angle=4*Math.sin(ph)*jongle; o.saut=-8*Math.abs(Math.sin(ph))*jongle-22*presse; o.tAngle=5*Math.sin(ph)*jongle+8*pouet;
  o.px=.6*Math.sin(ph)*jongle; o.py=-.6*jongle+.3*pouet; o.grand=1.1+.1*presse; o.sourcilY=8+6*presse;
  o.oeil=pouet>.5&&presse>.5?'heureux':'normal'; o.bouche=jongle>.3?'rire':pouet>.3?'langue':'sourire'; o.ouvre=.5+.3*presse; o.k=1.2; o.joues=.7;
  const g=mel(R.g,{x:-118,y:-140-34*Math.max(0,Math.sin(ph)),plie:.6,main:25},jongle), d=mel(R.d,{x:118,y:-140-34*Math.max(0,-Math.sin(ph)),plie:-.6,main:25},jongle);
  o.g=mel(g,BRAS.hancheG,pouet); o.d=mel(d,{x:26,y:-236,plie:-1,devant:true,main:22},pouet);
  const pop=elast(seg(t,.4,.9));
  S.objet('nez',`${O(0,0,22,'#F0483E',7)}<path d="M-10,-8 q4,-8 12,-8" fill="none" stroke="#fff" stroke-width="5" stroke-linecap="round" opacity=".8"/>`,P.surTete);
  S.poser('nez',-2,22,{sx:pop*(1+.35*presse),sy:pop*(1-.3*presse),op:t>.4?1:0});
  S.objet('melon',`${L('M0,-132 Q14,-166 4,-186','#4E9B3A',5)}${O(4,-192,13,'#FF8FC1',5)}${O(4,-192,5,'#FFE066',3)}${T('M-54,-76 Q-52,-136 0,-136 Q52,-136 54,-76 Z','#6EDC8C',8)}${L('M-76,-76 L76,-76','#6EDC8C',10)}<path d="M-50,-90 Q0,-100 50,-90" fill="none" stroke="#FF4F7B" stroke-width="10"/>`,P.surTete);
  S.poser('melon',0,k(t,[[.7,-300],[1.1,6,'in'],[1.2,-8,'out'],[1.3,0],[5.8,0],[6.2,-320,'in']]),{op:t>.7?1:0,rot:-7});
  S.objet('noeud',`${T('M0,0 L-36,-20 L-36,20 Z','#FF4F7B',6)}${T('M0,0 L36,-20 L36,20 Z','#FF4F7B',6)}${O(0,0,10,'#FFE066',5)}`,P.surCorps);
  S.poser('noeud',0,-150,{sx:elast(seg(t,.6,1.1)),rot:360*ease(seg(t,4.6,5.3)),op:t>.6?1:0});
  const BC=['#FF4F7B','#5CC8FF','#6EDC8C'];
  for(let i=0;i<3;i++){ S.objet('balle'+i,`${O(0,0,19,BC[i],6)}<path d="M-8,-8 q4,-6 10,-6" fill="none" stroke="#fff" stroke-width="4" stroke-linecap="round" opacity=".8"/>`,true);
    const tt=Math.min(t,4.2), a=tt*4.2+i*2.094, s=Math.sin(a), v=Math.max(0,t-4.2);
    S.poser('balle'+i,o.x+128*Math.cos(a)+(i-1)*190*v,Math.min(SOL-19,SOL-176-290*Math.max(0,s)+14*Math.max(0,-s)+900*v*v),{op:t>1.1?1:0,sx:clamp((t-1.1)*6)}); }
  S.texte('a',t,1.4,2.6,'Hop ! Hop !',o.x-420,SOL-380,{taille:50,couleur:'#5CC8FF',tremble:2});
  S.texte('b',t,2.8,4.0,'Et hop !',o.x+150,SOL-400,{taille:50,couleur:'#6EDC8C',tremble:2});
  S.texte('c',t,4.6,5.7,'POUET !',o.x-400,SOL-340,{taille:66,couleur:'#F0483E',tremble:4,grossit:.15});
  S.particules('etoiles',t,1.3,5.6,{n:5,forme:'etoile',x:600,y:SOL-300,dx:660,dy:300,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:.9,rot:120,cadence:.3});
});

scene('chatouilles',{titre:'Ça chatouille !',dit:'Hi hi hi ! Ça chatouille !',cat:'rigolo',duree:5.8,vignette:2.4,x:540},(t,o,S)=>{
  const arrive=ease(seg(t,.3,1.1)), guili=fen(t,1.1,4.6,.15), sortie=ease(seg(t,4.6,5.3)), fin=fen(t,4.7,5.7,.2), w=Math.sin(t*19), w2=Math.sin(t*7);
  o.x=540-24*guili+6*w*guili; o.angle=-9*guili+7*w*guili; o.saut=-10*Math.abs(w)*guili; o.sy=1-.04*Math.abs(w)*guili; o.tAngle=-10*w2*guili+6*w*guili; o.ventre=5*Math.abs(w)*guili;
  o.px=.75*arrive*(1-guili); o.py=.3*arrive*(1-guili); o.oeil=guili>.4?(w2>-.3?'heureux':'serre'):'normal'; o.grand=1+.12*arrive;
  o.bouche=guili>.3?'rire':fin>.3?'sourire':'petit'; o.ouvre=.6+.4*Math.abs(Math.sin(t*13)); o.k=1.1; o.joues=.9*Math.max(guili,fin); o.sourcilY=8*guili;
  o.g=mel(mel(R.g,{x:-112+30*w,y:-200+30*Math.cos(t*19),plie:.5},guili),{x:-20,y:-290,plie:.9,devant:true},fin); o.d=mel(R.d,{x:72+16*w,y:-120+14*Math.cos(t*17),plie:-.9,devant:true},guili);
  S.objet('plume',`${T('M0,0 C40,-54 150,-64 236,-22 C160,12 60,32 0,0 Z','#8FD3FF',7)}<path d="M0,0 Q110,-24 256,-24" fill="none" stroke="${N}" stroke-width="5" stroke-linecap="round"/><path d="M60,-12 l14,-22 M100,-18 l14,-24 M140,-22 l14,-22 M70,-10 l18,14 M112,-16 l18,16 M152,-20 l18,14" stroke="#5CAEE6" stroke-width="4" stroke-linecap="round"/>`,true);
  const cx=540+38+10*Math.sin(t*23), cy=SOL-128+30*Math.sin(t*9);
  S.poser('plume',lerp(lerp(1300,cx,arrive),1330,sortie),lerp(lerp(SOL-300,cy,arrive),SOL-430,sortie),{rot:-8+10*Math.sin(t*23)*guili});
  return ()=>{ const [ex,ey]=P.monde(16,-20,true);
    S.particules('larmes',t,1.6,4.6,{n:10,forme:'goutte',x:ex,y:ey,dx:60,vx:0,vy:-200,ex:420,ey:120,g:900,vie:.7,taille:.8,cadence:.12});
    S.texte('z',t,1.2,2.3,'guili guili',720,SOL-220,{taille:42,couleur:'#5CAEE6',tremble:2});
    S.texte('a',t,1.6,3.0,'HI HI HI !',90,SOL-380,{taille:58,couleur:'#FFC83D',tremble:4});
    S.texte('b',t,3.1,4.5,'Arrête ! Hi hi !',80,SOL-330,{taille:44,couleur:'#FF8FC1',tremble:3});
    S.texte('c',t,4.9,5.7,'ouf…',o.x+120,SOL-350,{taille:44,couleur:'#7FD3FF',monte:14}); };
});

scene('oups',{titre:'Oups !',dit:'Oups !',cat:'rigolo',duree:6.0,vignette:3.3,x:560},(t,o,S)=>{
  const marche=seg(t,0,1.5), enMarche=marche<1?1:0, pas=Math.abs(Math.sin(marche*Math.PI*4)), bute=cl(t,1.5,1.9), choc=fen(t,1.55,3.7,.1), gene=ease(seg(t,3.9,4.4))*(1-ease(seg(t,5.6,5.95)));
  o.x=lerp(260,560,marche)+30*out(seg(t,1.5,1.9)); o.saut=-22*pas*enMarche; o.angle=4*enMarche+16*bute; o.sy=1-.04*(1-clamp(pas*3))*enMarche;
  o.tAngle=4*Math.sin(t*8)*enMarche+10*bute+8*choc*(1-bute)-6*gene;
  o.px=k(t,[[0,.5],[1.5,.5],[1.9,.8],[2.6,.8],[3.0,.55],[3.6,.55],[4.0,0]]); o.py=k(t,[[0,.1],[1.5,.1],[1.9,.5],[2.2,.9],[2.7,.9],[3.0,.1],[3.6,.1],[4.0,0]]);
  o.grand=1+.3*choc; o.pupille=1-.35*choc; o.sourcilY=14*choc+6*gene; o.sourcilTilt=-8*gene;
  o.bouche=t<1.5?'langue':t<3.9?'o':'dents'; o.ouvre=t<1.5?.5:.7; o.joues=.4+.5*gene; o.k=1.1;
  o.d=mel({x:118,y:-170,plie:-.5},{x:96,y:-110,plie:-.5},gene); o.g=mel(R.g,{x:-72,y:-334,plie:.9},gene);
  S.objet('cornet',`${T('M-24,-34 L24,-34 L0,46 Z','#E8B36B',7)}<path d="M-14,-34 L8,22 M2,-34 L16,-4 M14,-34 L-8,22 M-2,-34 L-16,-4" stroke="#C98A4B" stroke-width="3"/>`,true);
  const BOULE=`${O(0,0,31,'#FF9EC4',7)}<path d="M-14,-12 q5,-9 15,-10" fill="none" stroke="#fff" stroke-width="6" stroke-linecap="round" opacity=".8"/>${O(4,-30,9,'#F0483E',5)}`;
  S.objet('boule',BOULE,true);
  S.objet('splat',`${T('M-74,0 Q-82,-22 -44,-20 Q-32,-42 0,-30 Q30,-46 46,-20 Q84,-22 74,0 Z','#FF9EC4',7)}<path d="M-30,-14 q10,-10 26,-8" fill="none" stroke="#fff" stroke-width="5" stroke-linecap="round" opacity=".8"/>${O(-96,-6,8,'#FF9EC4',5)}${O(98,-8,7,'#FF9EC4',5)}${O(10,-34,8,'#F0483E',5)}`,true);
  return ()=>{ const [mx,my]=P.main('d'), v=t-1.6;
    S.poser('cornet',mx,my,{rot:6*Math.sin(t*8)*enMarche+24*bute+10*gene});
    if(v<0) S.poser('boule',mx,my-58,{rot:6*Math.sin(t*8)});
    else if(v<.537) S.poser('boule',736+190*v,SOL-202-240*v+1100*v*v,{rot:300*v});
    S.poser('splat',838,SOL,{op:v>=.537?1:0,sx:elast(seg(t,2.137,2.6)),sy:.4+.6*elast(seg(t,2.137,2.6))});
    S.particules('gicle',t,2.14,2.2,{n:9,forme:'goutte',x:838,y:SOL-10,vx:0,vy:-260,ex:500,ey:160,g:900,vie:.6,taille:.8,cadence:.006,boucle:false,couleur:'#FF9EC4'});
    S.particules('notes',t,.1,1.4,{n:4,forme:'texte',mot:['♪','♫'],x:o.x-40,y:SOL-380,vx:-50,vy:-60,ex:40,ey:20,vie:1,taille:1,cadence:.3,couleurs:COUL,onde:8});
    S.texte('a',t,2.14,3.0,'SPLAT !',900,SOL-110,{taille:52,couleur:'#FF8FC1',tremble:3,grossit:.2});
    S.texte('b',t,2.8,3.9,'Oups…',o.x-330,SOL-360,{taille:62,couleur:'#B58BFF',grossit:.15});
    S.texte('c',t,4.4,5.7,'hé hé…',o.x-300,SOL-300,{taille:46,couleur:'#9AA3B8',monte:10});
    S.particules('sueur',t,4.1,5.6,{n:2,forme:'goutte',x:o.x+84,y:SOL-330,vx:30,vy:30,ex:20,ey:10,g:400,vie:.7,taille:1,cadence:.6}); };
});

scene('cachecache',{titre:'Caché… coucou !',dit:'Caché ! Coucou !',cat:'rigolo',duree:5.6,vignette:3.5},(t,o,S)=>{
  const cache=ease(seg(t,.4,.8))*(1-ease(seg(t,3.0,3.15))), peekG=fen(t,1.5,2.0,.1), peekD=fen(t,2.3,2.8,.1), coucou=ease(seg(t,3.0,3.2))*(1-ease(seg(t,5.1,5.5))), b=Math.abs(Math.sin((t-3.0)*6))*coucou;
  o.sy=1-.08*cache+.08*cl(t,3.0,3.3); o.saut=-70*cl(t,3.0,3.5)-14*b*seg(t,3.5,3.7); o.angle=4*peekG-4*peekD+3*Math.sin(t*6)*coucou; o.tAngle=8*peekG-8*peekD+5*Math.sin(t*6)*coucou; o.tdy=6*cache;
  o.px=-.85*peekG+.85*peekD; o.grand=1+.2*coucou; o.brillant=coucou; o.oeil=coucou>.5&&t>4.0?'heureux':'normal';
  o.bouche=coucou>.3?'rire':'petit'; o.ouvre=.8; o.k=1.2; o.joues=.5+.4*coucou; o.sourcilY=12*coucou;
  let g=mel(R.g,{x:-12-62*peekG,y:-284,plie:1.0,devant:true,main:34},cache), d=mel(R.d,{x:44+56*peekD,y:-284,plie:-1.0,devant:true,main:34},cache);
  o.g=mel(g,{x:-146,y:-244+10*Math.sin(t*12),plie:.25,main:28},coucou); o.d=mel(d,{x:146,y:-244+10*Math.sin(t*12+2),plie:-.25,main:28},coucou);
  S.texte('a',t,.9,2.9,'Caché…',o.x-360,SOL-330,{taille:54,couleur:'#B58BFF',monte:8});
  S.texte('q',t,1.5,2.8,'?',o.x+150,SOL-340,{taille:70,couleur:'#5CC8FF',tremble:1});
  S.texte('b',t,3.05,4.8,'COUCOU !',100,SOL-400,{taille:64,couleur:'#FFB347',grossit:.18,tremble:2});
  S.texte('c',t,4.0,5.1,'Hi hi !',o.x+150,SOL-370,{taille:48,couleur:'#FF8FC1',monte:12});
  S.particules('eclat',t,3.05,3.1,{n:10,forme:'etoile',x:o.x,y:SOL-300,dx:520,dy:380,vx:0,vy:-40,ex:500,ey:400,vie:.6,taille:1.1,rot:220,cadence:.004,boucle:false});
  S.particules('etoiles',t,3.3,5.0,{n:5,forme:'etoile',x:600,y:SOL-300,dx:640,dy:300,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:.9,rot:120,cadence:.3});
});

scene('hoquet',{titre:"J'ai le hoquet",dit:"Hic ! J'ai le hoquet !",cat:'rigolo',duree:6.0,vignette:.94},(t,o,S)=>{
  const H=[.8,1.9,2.8], hic=Math.max(...H.map(h=>cl(t,h,h+.28))), gros=cl(t,4.9,5.35), retient=fen(t,3.3,4.8,.2), apres=Math.max(...H.map(h=>fen(t,h+.3,h+.85,.12)));
  o.saut=-34*hic-60*gros; o.sy=1+.09*hic+.12*gros-.03*retient; o.sx=1-.05*hic+.04*retient; o.tdy=-10*hic-12*gros; o.tAngle=-6*hic+2*Math.sin(t*30)*retient;
  o.grand=1+.25*Math.max(hic,gros); o.pupille=1-.2*Math.max(hic,gros); o.sourcilY=14*Math.max(hic,gros)+4*retient; o.px=.5*Math.sin(t*2.2)*(1-hic)*(1-retient);
  o.bouche=(hic>.2||gros>.2)?'o':retient>.4?'plat':'petit'; o.ouvre=.7*hic+1.1*gros; o.k=retient>.4?.7:1; o.tsx=1+.1*retient; o.joues=.5+.5*retient; o.teinte=.5*retient*seg(t,3.5,4.7);
  o.d=mel(mel(R.d,{x:10,y:-212,plie:-.9,devant:true},apres),{x:10,y:-208,plie:-.9,devant:true},retient); o.g=mel(R.g,{x:-16,y:-216,plie:.9,devant:true},retient);
  if(gros>.05){ o.g=mel(o.g,{x:-140,y:-240,plie:.2,main:27},gros); o.d=mel(o.d,{x:140,y:-240,plie:-.2,main:27},gros); }
  return ()=>{ const [bx,by]=P.monde(-6,46,true);
    [.8,1.9,2.8,4.9].forEach((h,i)=>S.particules('bulle'+i,t,h,h+.05,{n:i===3?5:2,forme:'rond',x:bx+34,y:by-4,vx:150,vy:-150,ex:i===3?300:60,ey:60,vie:1.0,taille:.7,gonfle:.9,cadence:.03,boucle:false,couleur:'#DDF3FF'}));
    S.texte('a',t,.8,1.5,'hic !',o.x+120,SOL-340,{taille:50,couleur:'#5CC8FF',grossit:.3});
    S.texte('b',t,1.9,2.6,'hic !',o.x-280,SOL-350,{taille:54,couleur:'#B58BFF',grossit:.3});
    S.texte('c',t,2.8,3.4,'HIC !',o.x+120,SOL-370,{taille:62,couleur:'#FF8FC1',grossit:.3});
    S.texte('d',t,3.5,4.7,'je retiens…',o.x-420,SOL-320,{taille:40,couleur:'#9AA3B8',tremble:1});
    S.texte('e',t,4.9,5.9,'HIIIC !',o.x-400,SOL-400,{taille:78,couleur:'#FFB347',tremble:5,grossit:.2});
    S.particules('choc',t,4.9,4.95,{n:8,forme:'trait',x:o.x,y:SOL-6,dx:260,vx:0,vy:-40,ex:300,ey:20,vie:.3,taille:.9,cadence:.005,incline:120,boucle:false}); };
});

scene('grimace',{titre:'Grimace !',dit:'Je fais des grimaces !',cat:'rigolo',duree:6.0,vignette:1.4},(t,o,S)=>{
  const g1=fen(t,.6,2.0,.15), g2=fen(t,2.3,3.7,.15), g3=fen(t,4.0,5.5,.15), w=Math.sin(t*14);
  // 1 : il tire sur ses joues, l'œil louche, la langue sort — 2 : la tête écrasée, les dents, l'œil énorme — 3 : l'œil qui tourne, les mains en oreilles
  o.tsx=1+.26*g1+.2*g2; o.tsy=1-.08*g1-.22*g2; o.tAngle=4*w*g1+12*Math.sin(t*9)*g3; o.angle=3*w*(g1+g3); o.saut=-10*Math.abs(Math.sin(t*9))*g3; o.tdy=10*g2;
  o.px=g1>.3?.95:g3>.3?Math.cos(t*13)*.9:0; o.py=g1>.3?.6:g3>.3?Math.sin(t*13)*.9:0; o.grand=1+.45*g2-.1*g1; o.pupille=1-.5*g2+.2*g3; o.sourcilY=14*g2-8*g1; o.sourcilTilt=14*g1-10*g3*w;
  o.bouche=g1>.3?'langue':g2>.3?'dents':g3>.3?'langue':'sourire'; o.ouvre=g1>.3?.9+.3*w:.5+.5*Math.abs(Math.sin(t*20)); o.k=1.2; o.joues=.5;
  let g=mel(R.g,{x:-110-8*w,y:-236,plie:.9,devant:true,main:20},g1), d=mel(R.d,{x:116+8*w,y:-236,plie:-.9,devant:true,main:20},g1);
  g=mel(g,{x:-52,y:-332,plie:.7,devant:true,main:24},g2); d=mel(d,{x:62,y:-332,plie:-.7,devant:true,main:24},g2);
  o.g=mel(g,{x:-74,y:-372+12*Math.sin(t*18),plie:.6,main:27,pouce:-90},g3); o.d=mel(d,{x:74,y:-372+12*Math.sin(t*18+2),plie:-.6,main:27,pouce:-90},g3);
  return ()=>{ const [bx,by]=P.monde(-6,84,true);
    S.particules('postillons',t,.8,1.9,{n:8,forme:'goutte',x:bx,y:by,vx:-60,vy:60,ex:360,ey:200,g:500,vie:.5,taille:.45,cadence:.09,couleur:'#BFE8FF'});
    S.particules('postillons2',t,4.2,5.4,{n:8,forme:'goutte',x:bx,y:by,vx:60,vy:60,ex:360,ey:200,g:500,vie:.5,taille:.45,cadence:.09,couleur:'#BFE8FF'});
    S.texte('a',t,.7,1.9,'Bleuuuh !',o.x-440,SOL-370,{taille:54,couleur:'#6EDC8C',tremble:3});
    S.texte('b',t,2.4,3.6,'Gniiii !',o.x+140,SOL-380,{taille:56,couleur:'#FFB347',tremble:3});
    S.texte('c',t,4.1,5.4,'Gnagnagna !',70,SOL-330,{taille:46,couleur:'#B58BFF',tremble:4}); };
});

// ───────────────────────────── ÇA NE VA PAS (suite) ─────────────────────────────
scene('mal',{titre:"J'ai mal",dit:"Aïe ! J'ai mal !",cat:'pasbien',duree:6.2,vignette:4.3,x:520},(t,o,S)=>{
  const marche=seg(t,0,1.0), enMarche=marche<1?1:0, pas=Math.abs(Math.sin(marche*Math.PI*3)), choc=cl(t,1.1,1.5), saute=fen(t,1.4,3.2,.15), assis=ease(seg(t,3.3,3.8))*(1-ease(seg(t,5.8,6.15))), s=Math.abs(Math.sin((t-1.4)*8));
  o.x=lerp(300,520,marche); o.saut=-20*pas*enMarche-50*s*saute; o.angle=4*enMarche-5*saute+3*Math.sin(t*16)*saute;
  o.sy=1-.08*choc-.03*assis; o.tsy=1-.2*choc; o.tdy=14*choc+6*assis; o.tAngle=8*assis+4*Math.sin(t*16)*saute;
  o.oeil=(choc>.2||saute>.3)?'serre':'normal'; o.lid=.3*assis; o.py=.5*assis; o.px=enMarche?.4:.3*assis; o.grand=1+.1*assis; o.brillant=assis; o.sourcilTilt=-14*Math.max(saute,assis); o.sourcilY=10*choc;
  o.bouche=t<1.1?'sourire':(choc>.2||saute>.3)?'rire':'triste'; o.ouvre=1; o.k=.8; o.teinte=.25*saute;
  o.g=mel(mel(R.g,{x:-120+20*Math.sin(t*16),y:-300,plie:.6,main:26},saute),{x:-66,y:-60,plie:.2},assis); o.d=mel(mel(R.d,{x:120+20*Math.sin(t*16+2),y:-300,plie:-.6,main:26},saute),{x:84+6*Math.sin(t*10),y:-322,plie:-.8,devant:true},assis);
  S.objet('bosse',`${O(0,0,25,'#FF9E9E',7)}<g transform="rotate(35)"><rect x="-22" y="-7" width="44" height="14" rx="5" fill="#fff" stroke="${N}" stroke-width="4"/></g><g transform="rotate(-35)"><rect x="-22" y="-7" width="44" height="14" rx="5" fill="#fff" stroke="${N}" stroke-width="4"/></g>`,P.surTete);
  S.poser('bosse',26,-88,{sx:elast(seg(t,1.5,2.3))*(1+.07*Math.sin(t*12)),op:t>1.5?1:0});
  S.objet('cube',`${T('M-30,-30 h60 v60 h-60 Z','#5CC8FF',7)}<text x="0" y="14" font-size="40" font-weight="900" font-family="Arial Black,Arial" fill="#fff" stroke="${N}" stroke-width="2" text-anchor="middle">A</text>`,true);
  const v=clamp(t-1.1,0,.65), ch=seg(t,.45,1.1); S.poser('cube',520+230*v,t<1.1?lerp(-60,SOL-386,ch*ch):Math.min(SOL-30,SOL-386-300*v+1300*v*v),{rot:t<1.1?40*ch:40+320*v/.65*.9});
  for(let i=0;i<3;i++) S.objet('et'+i,ETOILE,true);
  return ()=>{ const tour=fen(t,1.3,5.9,.2);
    for(let i=0;i<3;i++){ const a=t*4.5+i*2.094, [x,y]=P.monde(104*Math.cos(a),-112+18*Math.sin(a),true); S.poser('et'+i,x,y,{op:tour,rot:t*200,sx:.9}); }
    const [ex,ey]=P.monde(16,10,true);
    S.particules('larmes',t,1.5,5.6,{n:8,forme:'goutte',x:ex,y:ey,dx:70,vx:0,vy:60,ex:170,ey:40,g:700,vie:.75,taille:1,cadence:.15});
    S.particules('eclat',t,1.1,1.15,{n:8,forme:'trait',x:o.x+20,y:SOL-370,vx:0,vy:-60,ex:700,ey:300,vie:.3,taille:1,cadence:.004,incline:160,boucle:false,couleur:'#F0483E'});
    S.texte('a',t,1.1,1.8,'BOUM !',o.x+110,SOL-420,{taille:54,couleur:'#FFB347',tremble:4,grossit:.2});
    S.texte('b',t,1.5,2.7,'AÏE !',o.x-340,SOL-380,{taille:80,couleur:'#F0483E',tremble:6,grossit:.15});
    S.texte('c',t,2.8,3.9,'aïe aïe aïe…',o.x+130,SOL-330,{taille:44,couleur:'#F0483E',tremble:3});
    S.texte('d',t,4.2,5.7,'ça fait mal…',70,SOL-330,{taille:42,couleur:'#9AA3B8',monte:8}); };
});

scene('chaud',{titre:"J'ai chaud",dit:"J'ai chaud !",cat:'pasbien',duree:6.0,vignette:2.6,x:540},(t,o,S)=>{
  const chaud=ease(seg(t,.3,1.4)), evente=fen(t,1.6,4.0,.2), fond=ease(seg(t,4.1,5.2))*(1-ease(seg(t,5.65,5.98))), halete=Math.sin(t*10);
  o.sy=1-.04*chaud-.2*fond+.012*halete; o.sx=1+.03*chaud+.2*fond; o.angle=-3*chaud*(1-fond); o.tAngle=-8*chaud*(1-fond)+4*Math.sin(t*3)*evente+12*fond; o.tdy=6*chaud+10*fond;
  o.teinte=.5*chaud; o.teinteCouleur='#FF7A3D'; o.lid=.35*chaud+.3*fond; o.py=.2; o.sourcilTilt=-10*chaud;
  o.bouche=chaud>.4?'langue':'sourire'; o.ouvre=.7+.3*halete; o.joues=.6*chaud;
  o.g=mel(R.g,{x:-66-10*fond,y:-60,plie:.2},chaud); o.d=mel(mel(R.d,{x:66,y:-60,plie:-.2},chaud),{x:172+14*Math.sin(t*19),y:-186+8*Math.cos(t*19),plie:-.5,main:26},evente);
  S.objet('rayonsS',RAYONS,false); S.poser('rayonsS',940,SOL-360,{rot:t*30,sx:1.3+.1*Math.sin(t*6)});
  S.objet('disque',DISQUE,false); S.poser('disque',940,SOL-360,{sx:1.3+.05*Math.sin(t*6)});
  S.particules('ondes',t,.3,5.6,{n:8,forme:'chaleur',x:560,y:SOL-60,dx:980,dy:60,vx:0,vy:-90,ex:0,ey:20,vie:1.3,taille:1,cadence:.17,devant:false,op:.75,couleur:'#FFB347'});
  S.objet('flaque',`<ellipse rx="170" ry="20" fill="#BFE3FF" stroke="${N}" stroke-width="7"/><path d="M-100,-4 q30,-8 60,-4" fill="none" stroke="#fff" stroke-width="6" stroke-linecap="round"/>`,false);
  S.poser('flaque',o.x,SOL+4,{sx:ease(seg(t,3.0,5.2)),op:t>3?1:0});
  S.objet('eventail',`${[-50,-25,0,25,50].map(a=>`<path d="M0,0 L0,-84" stroke="${N}" stroke-width="4" transform="rotate(${a})"/>`).join('')}${T('M-66,-58 A88,88 0 0 1 66,-58 L38,-33 A50,50 0 0 0 -38,-33 Z','#FF8FC1',6)}<path d="M-30,-72 L-18,-44 M0,-82 L0,-50 M30,-72 L18,-44" stroke="#fff" stroke-width="4" opacity=".7"/>${O(0,0,7,'#FFC83D',4)}`,true);
  return ()=>{ const [mx,my]=P.main('d');
    S.poser('eventail',mx,my,{op:evente>.1?1:0,rot:-10+26*Math.sin(t*19),sx:elast(seg(t,1.6,2.1))});
    S.particules('sueur',t,.5,5.6,{n:9,forme:'goutte',x:o.x,y:SOL-330,dx:190,vx:0,vy:50,ex:160,ey:30,g:600,vie:.6,taille:.9,cadence:.1});
    S.particules('vent',t,1.8,4.0,{n:4,forme:'trait',x:o.x+60,y:SOL-270,dy:60,vx:-200,vy:0,ex:60,ey:10,vie:.35,taille:.6,cadence:.12,incline:0,rot:0,couleur:'#BFE8FF'});
    S.texte('a',t,.6,1.6,'pfff…',o.x-280,SOL-330,{taille:46,couleur:'#FF9A5A',monte:12});
    S.texte('b',t,1.8,3.5,"J'ai chaud !",70,SOL-390,{taille:52,couleur:'#FF7A3D',tremble:1});
    S.texte('c',t,4.3,5.6,'je fonds…',o.x-340,SOL-250,{taille:44,couleur:'#FF9A5A',monte:-20}); };
});

scene('froid',{titre:"J'ai froid",dit:"Brrr ! J'ai froid !",cat:'pasbien',duree:6.2,vignette:2.2},(t,o,S)=>{
  const froid=ease(seg(t,.3,1.0)), tr=Math.sin(t*50), mieux=ease(seg(t,4.3,5.0)), fris=froid*(1-.8*mieux);
  o.x=600+3.5*tr*fris; o.angle=1.6*tr*fris; o.sy=1-.05*fris+.01*tr*fris; o.sx=1-.03*fris; o.tAngle=2*tr*fris+6*mieux; o.tdy=8*fris;
  o.teinte=.55*froid*(1-.7*mieux); o.teinteCouleur='#9CCBFF'; o.lid=.2*fris; o.pupille=1-.2*fris; o.sourcilTilt=-10*fris; o.sourcilY=4+6*mieux;
  o.oeil=mieux>.7?'heureux':'normal'; o.bouche=mieux>.6||froid<.4?'sourire':'dents'; o.joues=.7*mieux; o.k=1.1;
  const fr=Math.sin(t*14)*fris; o.g=mel(R.g,{x:46,y:-124+10*fr,plie:1,devant:true},froid); o.d=mel(R.d,{x:-46,y:-106-10*fr,plie:-1,devant:true},froid);
  S.objet('neige',T('M-580,4 L-580,-10 Q-480,-34 -400,-12 Q-300,-36 -200,-10 Q-100,-30 0,-12 Q120,-36 240,-10 Q340,-32 440,-12 Q520,-28 580,-8 L580,4 Z','#fff',6),false);
  S.poser('neige',640,SOL+2,{sy:ease(seg(t,.8,4.5)),op:t>.8?1:0});
  S.particules('flocons',t,.2,6.0,{n:18,forme:'flocon',x:640,y:SOL-520,dx:1150,dy:60,vx:-30,vy:150,ex:40,ey:60,vie:3.2,taille:1,et:.8,rot:60,cadence:.17,onde:12,devant:false,fondu:.15});
  S.particules('flocons2',t,.4,6.0,{n:8,forme:'flocon',x:640,y:SOL-520,dx:1150,dy:60,vx:-40,vy:190,ex:40,ey:60,vie:2.6,taille:1.3,et:.6,rot:80,cadence:.33,onde:14,fondu:.15});
  S.objet('echarpe',`${T('M14,-162 L44,-160 L52,-84 L22,-82 Z','#F0483E',7)}<path d="M19,-136 L48,-134 M21,-110 L50,-108" stroke="#fff" stroke-width="9"/><path d="M26,-82 l-2,16 M36,-82 l0,16 M46,-83 l2,16" stroke="${N}" stroke-width="5" stroke-linecap="round"/>${L('M-54,-168 Q0,-150 54,-168','#F0483E',20)}<path d="M-30,-164 l6,22 M0,-158 l0,22 M30,-164 l-6,22" stroke="#fff" stroke-width="7"/>`,P.surCorps);
  S.poser('echarpe',0,k(t,[[3.2,-460],[3.7,0,'in'],[3.8,-12,'out'],[3.9,0]]),{op:t>3.2?1:0});
  S.objet('bonnetL',`${O(0,-136,22,'#fff',7)}${T('M-88,-46 C-84,-134 84,-134 88,-46 Z','#F0483E',8)}<path d="M-60,-84 l20,-24 l20,24 l20,-24 l20,24 l20,-24 l20,24" fill="none" stroke="#fff" stroke-width="7" stroke-linejoin="round"/>${L('M-90,-44 Q0,-66 90,-44','#fff',18)}`,P.surTete);
  S.poser('bonnetL',0,k(t,[[3.7,-300],[4.2,6,'in'],[4.3,-8,'out'],[4.4,0]]),{op:t>3.7?1:0});
  return ()=>{ const [bx,by]=P.monde(-6,46,true);
    S.particules('buee',t,.8,4.4,{n:4,forme:'rond',x:bx-20,y:by,vx:-70,vy:-30,ex:30,ey:30,vie:.9,taille:.7,gonfle:1.6,cadence:.42,couleur:'#fff',op:.85});
    S.texte('a',t,.8,2.2,'Brrrr…',o.x+130,SOL-340,{taille:56,couleur:'#7FB6FF',tremble:4});
    S.texte('b',t,2.3,3.3,'glagla glagla',70,SOL-330,{taille:44,couleur:'#7FB6FF',tremble:4});
    S.texte('c',t,4.6,6.0,'Aaah… ça va mieux',70,SOL-390,{taille:40,couleur:'#FF8FC1'});
    S.particules('coeurs',t,4.7,6.0,{n:3,forme:'coeur',x:o.x,y:SOL-400,dx:300,vx:0,vy:-60,ex:20,ey:20,vie:1.1,taille:.8,cadence:.4}); };
});

scene('ennui',{titre:"Je m'ennuie",dit:"Je m'ennuie.",cat:'pasbien',duree:6.4,vignette:2.2,x:560},(t,o,S)=>{
  const mou=ease(seg(t,.2,.9)), soupir=cl(t,2.9,4.1), baille=cl(t,4.6,5.9);
  const fx=830+180*Math.cos(t*1.9)+40*Math.sin(t*5.3), fy=SOL-320+90*Math.sin(t*2.7)+20*Math.cos(t*6.1);
  o.angle=4*mou; o.sy=1-.05*mou+.04*cl(t,2.9,3.5)-.03*cl(t,3.5,4.1); o.tAngle=14*mou-8*baille; o.tdx=10*mou; o.tdy=10*mou;
  o.px=clamp((fx-576)/260,-1,1)*(1-baille); o.py=clamp((fy-(SOL-282))/150,-1,1)*(1-baille); o.lid=.45*mou+.4*baille; o.sourcilY=-4*mou+8*baille; o.sourcilTilt=-4;
  o.oeil=baille>.6?'ferme':'normal'; o.bouche=baille>.2?'o':soupir>.3?'o':'plat'; o.ouvre=baille>.2?1.5*baille:.35; o.k=.7;
  o.d=mel(R.d,{x:70,y:-218,plie:-1,devant:true},mou); o.g=mel(R.g,{x:-66,y:-84+8*Math.abs(Math.sin(t*12)),plie:.3},mou);
  S.objet('pendule',`${L('M0,0 L0,92','#B9783F',6)}${O(0,106,17,'#FFC83D',6)}`,false); S.poser('pendule',220,SOL-310,{rot:22*Math.sin(t*Math.PI*2)});
  S.objet('horloge',`${O(0,0,72,'#fff',9)}${[0,1,2,3,4,5,6,7,8,9,10,11].map(i=>`<path d="M0,-60 L0,-${i%3?54:48}" stroke="${N}" stroke-width="${i%3?4:7}" stroke-linecap="round" transform="rotate(${i*30})"/>`).join('')}<path d="M0,0 L-22,-22" stroke="${N}" stroke-width="9" stroke-linecap="round"/>`,false); S.poser('horloge',220,SOL-370,{});
  S.objet('aiguille',`<path d="M0,6 L0,-50" stroke="#F0483E" stroke-width="6" stroke-linecap="round"/>${O(0,0,7,N,0)}`,false); S.poser('aiguille',220,SOL-370,{rot:Math.floor(t*2)*6+back(clamp((t*2%1)*5))*6-6});
  S.objet('mouche',`<ellipse cx="-7" cy="-10" rx="10" ry="6" fill="#fff" stroke="${N}" stroke-width="3" opacity=".9"/><ellipse cx="7" cy="-10" rx="10" ry="6" fill="#fff" stroke="${N}" stroke-width="3" opacity=".9"/><ellipse rx="11" ry="9" fill="${N}"/>`,true);
  S.poser('mouche',fx,fy,{rot:20*Math.sin(t*30)});
  S.texte('z',t,0,6.4,'bzzz',fx+18,fy-18,{taille:22,couleur:'#9AA3B8'});
  S.texte('t1',t,.5,1.4,'tic…',310,SOL-300,{taille:36,couleur:'#B9783F',monte:8});
  S.texte('t2',t,1.5,2.4,'tac…',310,SOL-250,{taille:36,couleur:'#B9783F',monte:8});
  S.texte('t3',t,3.5,4.4,'tic…',310,SOL-300,{taille:36,couleur:'#B9783F',monte:8});
  S.texte('t4',t,4.5,5.4,'tac…',310,SOL-250,{taille:36,couleur:'#B9783F',monte:8});
  S.texte('a',t,1.2,2.8,"Je m'ennuie…",640,SOL-440,{taille:42,couleur:'#9AA3B8'});
  S.texte('b',t,3.0,4.2,'pfffff…',o.x-290,SOL-110,{taille:44,couleur:'#9AA3B8',monte:10});
  S.texte('c',t,4.8,5.9,'ouaaah…',o.x-300,SOL-110,{taille:42,couleur:'#8FA3FF',monte:12});
  S.particules('souffle',t,3.3,4.0,{n:3,forme:'rond',x:o.x-30,y:SOL-212,vx:-110,vy:10,ex:30,ey:30,vie:.7,taille:.8,gonfle:1.4,cadence:.18,couleur:'#fff',boucle:false});
});

scene('bruit',{titre:'Trop de bruit !',dit:'Il y a trop de bruit !',cat:'pasbien',duree:6.4,vignette:2.0,cadre:[270,20,600],x:470},(t,o,S)=>{
  const bruit=fen(t,.5,4.25,.1), boum=Math.pow(Math.abs(Math.sin(t*9)),3)*bruit, mains=ease(seg(t,.8,1.2))*(1-ease(seg(t,4.6,5.0))), stop=fen(t,3.5,4.3,.1), calme=ease(seg(t,4.6,5.2));
  o.x=470-10*bruit+4*Math.sin(t*40)*boum; o.angle=-6*mains*(1-stop)+2*Math.sin(t*40)*boum; o.sy=1-.07*mains*(1-stop)-.03*boum+.08*stop; o.tdy=12*mains*(1-stop); o.tAngle=-8*mains*(1-stop); o.tsx=1+.05*stop; o.tsy=1+.05*stop;
  o.oeil=bruit>.3&&stop<.4?'serre':calme>.7&&t>5.4?'heureux':'normal'; o.sourcilTilt=-12*mains*(1-stop)+16*stop; o.sourcilY=-10*stop+6*calme;
  o.bouche=stop>.3?'rire':mains>.4?'vague':calme>.5?'sourire':'petit'; o.ouvre=1; o.k=1; o.teinte=.4*stop;
  o.g=mel(R.g,{x:-100,y:-258,plie:.9,devant:true,main:29},mains); o.d=mel(R.d,{x:104,y:-258,plie:-.9,devant:true,main:29},mains);
  S.objet('enceinte',`${T('M-64,0 L-64,-156 L64,-156 L64,0 Z','#5B5F73',8)}${O(0,-98,38,'#8E9BB8',7)}${O(0,-98,15,N,0)}${O(0,-32,17,'#8E9BB8',6)}<path d="M-52,-146 L-52,-14" stroke="#fff" stroke-width="6" stroke-linecap="round" opacity=".25"/>`,false);
  S.poser('enceinte',940,SOL,{sx:1+.1*boum,sy:1+.07*boum,rot:5*calme});
  S.particules('ondes',t,.5,4.2,{n:8,forme:'onde',x:850,y:SOL-100,vx:-330,vy:0,ex:0,ey:120,vie:.85,taille:1,gonfle:1.6,cadence:.11,couleur:'#F0483E',devant:false});
  S.particules('notes',t,.5,4.2,{n:8,forme:'texte',mot:['♪','♫'],x:900,y:SOL-200,vx:-160,vy:-150,ex:300,ey:160,vie:.9,taille:1.3,cadence:.12,couleurs:COUL,rot:120});
  S.texte('a',t,.6,1.5,'BOUM',690,SOL-330,{taille:60,couleur:'#F0483E',tremble:6,grossit:.2});
  S.texte('b',t,1.3,2.3,'TUUUT !',70,SOL-400,{taille:54,couleur:'#FFB347',tremble:6,grossit:.2});
  S.texte('c',t,2.1,3.1,'BLAM !',640,SOL-420,{taille:64,couleur:'#B58BFF',tremble:6,grossit:.2});
  S.texte('d',t,2.7,3.5,'DZING',80,SOL-230,{taille:50,couleur:'#5CC8FF',tremble:6,grossit:.2});
  S.texte('e',t,3.5,4.5,'STOP !',o.x,SOL-430,{taille:84,couleur:'#F0483E',tremble:5,grossit:.15,ancre:'middle'});
  S.texte('f',t,4.7,5.7,'oh… pardon',850,SOL-190,{taille:30,couleur:'#9AA3B8'});
  S.texte('g',t,5.1,6.3,'ouf… c’est calme',o.x+120,SOL-340,{taille:40,couleur:'#6EDC8C',monte:10});
  S.particules('sueurE',t,4.6,5.6,{n:2,forme:'goutte',x:1000,y:SOL-150,vx:30,vy:30,ex:20,ey:10,g:400,vie:.7,taille:1,cadence:.5});
});

scene('non',{titre:'Non !',dit:'Non !',cat:'pasbien',duree:5.2,vignette:3.4,cadre:[407,20,600]},(t,o,S)=>{
  const non=fen(t,.5,4.7,.2), doigt=fen(t,.6,2.5,.2), croise=ease(seg(t,2.6,2.9))*(1-ease(seg(t,4.6,5.0))), sec=Math.sin(t*11)*(doigt+.7*fen(t,3.3,4.4,.2)), tape=Math.pow(Math.max(0,Math.sin(t*5.5)),8)*croise;
  o.tAngle=4*sec; o.tdx=20*sec; o.px=-.6*sec; o.angle=-2*sec; o.saut=-14*tape; o.sy=1-.02*non;
  o.lid=.18*non; o.sourcilY=-14*non; o.sourcilTilt=16*non; o.oeil=fen(t,2.9,3.3,.06)>.5?'serre':'normal';
  o.bouche=non<.3?'plat':(fen(t,1.2,2.3,.1)>.4||fen(t,3.0,4.3,.1)>.4)?'rire':'triste'; o.ouvre=.8; o.k=.9;
  let g=mel(R.g,BRAS.hancheG,doigt), d=mel(R.d,{x:120+26*Math.sin(t*11),y:-262,plie:-.5,main:18,pouce:-170},doigt);
  o.g=mel(g,{x:66,y:-138,plie:.15,devant:true,main:26,pouce:null},croise); o.d=mel(d,{x:-66,y:-138,plie:-.15,devant:true,main:26,pouce:null},croise);
  S.objet('croix',`${L('M-52,-52 L52,52','#F0483E',22)}${L('M52,-52 L-52,52','#F0483E',22)}`,true);
  S.poser('croix',o.x+270,SOL-300,{sx:elast(seg(t,2.7,3.3))*(1-ease(seg(t,4.6,5.0))),rot:6*Math.sin(t*22),op:t>2.7?1:0});
  S.texte('a',t,1.1,2.4,'Non, non, non !',70,SOL-390,{taille:46,couleur:'#F0483E',tremble:2});
  S.texte('b',t,2.9,4.4,'NON !',o.x-390,SOL-330,{taille:88,couleur:'#F0483E',tremble:4,grossit:.15});
  S.particules('choc',t,2.9,4.5,{n:6,forme:'trait',x:o.x,y:SOL-6,dx:260,vx:0,vy:-40,ex:300,ey:20,vie:.22,taille:.9,cadence:.14,incline:120});
});

// ───────────────────────────── LES ACTIONS (suite) ─────────────────────────────
const BALLON=`${O(0,0,44,'#fff',7)}${['#FF4F7B','#5CC8FF','#FFE066'].map((c,i)=>`<path d="M0,0 L0,-44 A44,44 0 0 1 38.1,-22 Z" fill="${c}" stroke="${N}" stroke-width="4" stroke-linejoin="round" transform="rotate(${i*120})"/>`).join('')}${O(0,0,8,'#fff',4)}<path d="M-26,-22 q6,-12 20,-14" fill="none" stroke="#fff" stroke-width="5" stroke-linecap="round" opacity=".7"/>`;
scene('ballon',{titre:'Je joue au ballon',dit:'Je joue au ballon !',cat:'actions',duree:6.0,vignette:2.5},(t,o,S)=>{
  const hy=SOL-389, jong=t>=1.3&&t<3.7, u=jong?((t-1.3)%.6)/.6:0, c=jong||(t>=1.2&&t<1.3)?1-clamp(Math.min(u,1-u)*7):0, tient=ease(seg(t,3.7,4.1))*(1-ease(seg(t,5.6,5.95))), b=Math.abs(Math.sin((t-4.1)*5.5))*fen(t,4.2,5.5,.2);
  let bx,by;
  if(t<.45){ const a=seg(t,0,.45); bx=lerp(1200,990,a); by=lerp(SOL-330,SOL-36,a*a); }
  else if(t<.85) [bx,by]=arc(seg(t,.45,.85),990,SOL-36,810,SOL-36,190);
  else if(t<1.3) [bx,by]=arc(seg(t,.85,1.3),810,SOL-36,600,hy,110);
  else if(jong){ bx=600; by=hy-78*4*u*(1-u)+10*c; }
  else { [bx,by]=arc(seg(t,3.7,4.1),600,hy,600,hy-26,40); }
  o.saut=-14*b; by+=t>=4.1?o.saut:0; o.tsy=1-.12*c; o.tdy=10*c; o.sy=1-.03*c+.03*b; o.angle=2*Math.sin(t*5)*(jong?1:0);
  o.px=clamp((bx-616)/300,-1,1)*(1-tient); o.py=clamp((by-(SOL-282))/200,-1,1)*(1-tient); o.grand=1.1; o.sourcilY=8;
  o.oeil=tient>.6?'heureux':'normal'; o.bouche=tient>.4?'rire':jong?'sourire':'petit'; o.ouvre=.7; o.k=1.2; o.joues=.5+.3*tient;
  const eq=fen(t,.85,3.7,.2); o.g=mel(mel(R.g,{x:-134,y:-150+16*Math.sin(t*9),plie:.2},eq),{x:-48,y:-404,plie:.9,main:25},tient); o.d=mel(mel(R.d,{x:134,y:-150+16*Math.sin(t*9+2),plie:-.2},eq),{x:48,y:-404,plie:-.9,main:25},tient);
  S.objet('ballon',BALLON,true); S.poser('ballon',bx,by,{rot:t<1.3?-t*260:t*70,sx:.8,sy:.8*(1-.1*c)});
  S.texte('a',t,.45,.95,'boing',1000,SOL-100,{taille:36,couleur:'#9AA3B8',monte:10});
  S.texte('b',t,.85,1.35,'boing',800,SOL-100,{taille:36,couleur:'#9AA3B8',monte:10});
  [1.3,1.9,2.5,3.1].forEach((h,i)=>S.texte('t'+i,t,h,h+.5,'toc !',i%2?o.x-250:o.x+130,SOL-400,{taille:46,couleur:COUL[i],grossit:.3}));
  S.texte('c',t,3.75,4.6,'Et hop !',o.x-360,SOL-330,{taille:54,couleur:'#FFB347',grossit:.2});
  S.texte('d',t,4.6,5.7,'Je joue !',o.x+140,SOL-330,{taille:52,couleur:'#5CC8FF',grossit:.15});
  S.particules('etoiles',t,4.1,5.6,{n:5,forme:'etoile',x:600,y:SOL-300,dx:640,dy:300,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:.9,rot:120,cadence:.28});
});

scene('lis',{titre:'Je lis',dit:'Je lis un livre.',cat:'actions',duree:6.4,vignette:4.4,x:520},(t,o,S)=>{
  const livre=ease(seg(t,.4,.9))*(1-ease(seg(t,5.9,6.3))), lit=fen(t,1.0,3.2,.2), ooh=fen(t,3.5,5.7,.2), u=seg(t,3.4,5.6);
  const fx=o.x+110+340*u, fy=SOL-330-110*Math.sin(u*2.4)+26*Math.sin(u*13);
  o.px=lit>.3?lerp(-.6,.6,(t*1.3)%1):clamp((fx-536)/300,-1,1)*ooh; o.py=.75*livre*(1-ooh)+clamp((fy-(SOL-282))/200,-1,1)*ooh; o.tAngle=4*livre-8*ooh; o.tdy=8*livre*(1-ooh); o.lid=.2*lit;
  o.grand=1+.25*ooh; o.brillant=ooh; o.sourcilY=12*ooh; o.bouche=ooh>.4?(t>4.6?'rire':'o'):'sourire'; o.ouvre=.6; o.k=.9; o.joues=.4+.4*ooh;
  o.g=mel(R.g,{x:-72,y:-130,plie:.7,devant:true},livre); o.d=mel(R.d,{x:72,y:-130,plie:-.7,devant:true},livre);
  S.objet('livre',`${T('M-96,-60 L0,-70 L96,-60 L96,-52 L0,-62 L-96,-52 Z','#fff',5)}${T('M-96,-52 L-4,-62 L-4,58 L-96,50 Z','#7E3FF2',7)}${T('M96,-52 L4,-62 L4,58 L96,50 Z','#7E3FF2',7)}<path d="M-82,-38 L-18,-45 M-82,34 L-18,41" stroke="#B58BFF" stroke-width="5" stroke-linecap="round"/><g transform="translate(50,-4) scale(1.5)">${ETOILE}</g>${O(-84,34,14,'#FFD24A',6)}${O(84,34,14,'#FFD24A',6)}`,true);
  S.objet('fusee',`${T('M-7,26 Q0,52 7,26 Z','#FFB347',4)}${T('M-12,10 L-26,30 L-12,24 Z','#F0483E',4)}${T('M12,10 L26,30 L12,24 Z','#F0483E',4)}${T('M0,-36 C19,-18 18,14 12,26 L-12,26 C-18,14 -19,-18 0,-36 Z','#fff',5)}${T('M0,-36 C8,-28 12,-20 13,-12 L-13,-12 C-12,-20 -8,-28 0,-36 Z','#F0483E',5)}${O(0,4,7,'#5CC8FF',4)}`,true);
  return ()=>{ const [cx,cy]=P.monde(0,-150);
    S.poser('livre',cx,cy,{op:livre>.05?1:0,sx:.95*elast(seg(t,.4,1.0)),rot:2*Math.sin(t*2)});
    S.poser('fusee',fx,fy,{op:u>0&&u<1?1:0,sx:.4+.8*out(clamp(u*3)),rot:lerp(10,120,u)+10*Math.sin(u*13)});
    S.particules('lettres',t,1.1,3.3,{n:6,forme:'texte',mot:['a','b','c','d','e','f'],x:cx,y:cy-230,dx:240,vx:0,vy:-70,ex:160,ey:30,vie:1.3,taille:1.2,cadence:.3,couleurs:COUL,onde:8,incline:40});
    S.particules('magie',t,3.4,5.6,{n:10,forme:'etoile',x:cx+40,y:cy-220,dx:260,vx:60,vy:-160,ex:340,ey:140,g:300,vie:1.0,taille:.9,rot:200,cadence:.11});
    S.texte('a',t,1.1,3.0,'Il était une fois…',70,SOL-400,{taille:40,couleur:'#B58BFF',monte:8});
    S.texte('b',t,3.6,4.7,'Oooh !',o.x-340,SOL-340,{taille:58,couleur:'#5CC8FF',grossit:.15});
    S.texte('c',t,4.8,6.0,'Encore une page !',o.x+110,SOL-120,{taille:38,couleur:'#FF8FC1'}); };
});

scene('bain',{titre:'Le bain',dit:'Je prends mon bain !',cat:'actions',duree:6.2,vignette:3.0},(t,o,S)=>{
  const plonge=seg(t,.3,.9), frotte=fen(t,1.6,3.6,.2), chante=fen(t,3.8,5.9,.2), w=Math.sin(t*12), bal=Math.sin(t*3.2);
  o.sol=SOL-16; o.saut=lerp(-560,0,plonge*plonge)-10*cl(t,.9,1.2)+4*Math.sin(t*3); o.sy=1-.06*cl(t,.9,1.15); o.angle=5*bal*chante; o.tAngle=6*w*frotte*.5+7*bal*chante;
  o.oeil=frotte>.4?'ferme':chante>.4&&bal>0?'heureux':'normal'; o.grand=1.08; o.sourcilY=8;
  o.bouche=chante>.3?(Math.sin(t*9)>0?'o':'rire'):frotte>.3?'sourire':t<1.5?'rire':'sourire'; o.ouvre=.7; o.k=1.2; o.joues=.6;
  let g=mel(R.g,{x:-150,y:-176,plie:-.3},clamp(plonge*2-1)), d=mel(R.d,{x:150,y:-176,plie:.3},clamp(plonge*2-1));
  g=mel(g,{x:-56+14*w,y:-338,plie:.8,devant:true},frotte); d=mel(d,{x:66-14*w,y:-342,plie:-.8,devant:true},frotte);
  o.g=mel(g,{x:-150,y:-230+20*Math.sin(t*6.4),plie:.2},chante); o.d=mel(d,{x:150,y:-176,plie:.3},chante);
  S.objet('mousseT',`${O(-30,-82,23,'#fff',6)}${O(40,-78,18,'#fff',6)}${O(14,-94,27,'#fff',6)}${O(-4,-116,19,'#fff',6)}<circle cx="-6" cy="-84" r="20" fill="#fff"/><circle cx="20" cy="-84" r="18" fill="#fff"/>`,P.surTete);
  S.poser('mousseT',0,0,{sx:elast(seg(t,1.9,2.7)),op:t>1.9?1:0});
  S.objet('robinet',`${L('M-236,-176 L-236,-244 Q-236,-268 -210,-268 L-188,-268 L-188,-252','#B9C2D6',12)}${O(-236,-236,10,'#F0483E',5)}`,true); S.poser('robinet',600,SOL,{});
  S.objet('baignoire',`${O(-150,-8,13,'#FFC83D',6)}${O(150,-8,13,'#FFC83D',6)}${T('M-196,-160 L196,-160 Q192,-36 150,-18 L-150,-18 Q-192,-36 -196,-160 Z','#fff',8)}<path d="M-170,-146 Q-166,-56 -138,-36" fill="none" stroke="#DCEBFA" stroke-width="12" stroke-linecap="round"/><rect x="-212" y="-180" width="424" height="28" rx="14" fill="#EAF4FF" stroke="${N}" stroke-width="8"/>`,true); S.poser('baignoire',600,SOL,{});
  S.objet('mousse',[[-176,26],[-132,30],[-86,24],[-40,30],[8,26],[54,30],[100,24],[146,30],[180,22]].map((m,i)=>O(m[0],-188-(i%2?8:0),m[1],'#fff',6)).join('')+`<rect x="-190" y="-190" width="380" height="22" fill="#fff"/>`,true);
  S.poser('mousse',600,SOL,{sy:1+.04*Math.sin(t*4),op:t>.85?1:0});
  S.objet('canard',`${T('M-34,0 Q-44,-30 -10,-28 L8,-28 Q40,-30 36,-2 Q20,16 -10,14 Q-28,14 -34,0 Z','#FFD84A',6)}${O(16,-42,20,'#FFD84A',6)}${T('M32,-46 l22,4 l-22,8 Z','#FF8A3D',5)}<circle cx="20" cy="-48" r="4" fill="${N}"/><path d="M-22,-8 q12,10 26,0" fill="none" stroke="#F0A81C" stroke-width="5" stroke-linecap="round"/>`,true);
  S.poser('canard',760,SOL-208-36*cl(t,4.4,4.8)+4*Math.sin(t*3.4),{rot:8*Math.sin(t*3.4),op:t>.95?1:0,sx:.9});
  S.particules('plouf',t,.9,.95,{n:16,forme:'goutte',x:600,y:SOL-180,dx:300,vx:0,vy:-420,ex:700,ey:300,g:1100,vie:.8,taille:1,cadence:.004,boucle:false});
  S.particules('bulles',t,1.0,6.0,{n:12,forme:'rond',x:600,y:SOL-200,dx:400,vx:0,vy:-70,ex:40,ey:30,vie:1.6,taille:.9,et:1.2,cadence:.14,couleur:'#EAF8FF',onde:10});
  S.particules('goutte',t,1.2,6.0,{n:2,forme:'goutte',x:412,y:SOL-246,vx:0,vy:60,ex:0,ey:0,g:500,vie:.45,taille:.8,cadence:.7});
  S.texte('a',t,.9,1.8,'PLOUF !',90,SOL-330,{taille:62,couleur:'#5CC8FF',tremble:3,grossit:.2});
  S.texte('b',t,1.9,3.3,'frotte frotte',o.x+150,SOL-410,{taille:40,couleur:'#7FB6FF',tremble:2});
  S.texte('c',t,3.9,5.1,'la la la ♪',100,SOL-390,{taille:46,couleur:'#B58BFF',monte:12});
  S.texte('d',t,4.4,5.3,'coin coin !',800,SOL-290,{taille:34,couleur:'#FF8A3D'});
});

scene('dents',{titre:'Je me brosse les dents',dit:'Je me brosse les dents.',cat:'actions',duree:5.8,vignette:2.0},(t,o,S)=>{
  const sort=ease(seg(t,.3,.7)), brosse=fen(t,1.0,3.8,.15), fini=ease(seg(t,4.0,4.4))*(1-ease(seg(t,5.5,5.8))), w=Math.sin(t*24), cote=Math.sin(t*2.2);
  o.tAngle=5*cote*brosse-4*fini; o.angle=2*cote*brosse; o.tsx=1+.03*Math.abs(w)*brosse; o.sy=1+.03*fini;
  o.px=.1; o.py=.3*brosse; o.lid=.12*brosse; o.oeil=fini>.5?'heureux':'normal'; o.sourcilY=6+6*fini; o.grand=1.05;
  o.bouche=brosse>.3?'dents':fini>.3?'rire':'sourire'; o.ouvre=.9; o.k=1.2; o.joues=.4+.4*fini;
  let d=mel(R.d,{x:124,y:-196,plie:-.5},sort); d=mel(d,{x:92+16*w,y:-206+8*cote,plie:-.9,devant:true},brosse); o.d=mel(d,{x:142,y:-252,plie:-.4},fini); o.g=mel(R.g,BRAS.hancheG,Math.max(brosse,fini));
  S.objet('mousseB',`${O(-26,0,13,'#fff',5)}${O(-6,8,16,'#fff',5)}${O(18,4,14,'#fff',5)}${O(34,-6,10,'#fff',5)}<circle cx="-6" cy="2" r="12" fill="#fff"/>`,P.surTete);
  S.poser('mousseB',-6,74,{sx:brosse*ease(seg(t,1.2,3.4)),op:brosse>.05?1:0});
  S.objet('brosse',`${L('M14,2 L-70,-4','#5CC8FF',10)}${T('M-104,-20 h36 v18 h-36 Z','#fff',5)}<path d="M-96,-20 v18 M-86,-20 v18 M-76,-20 v18" stroke="#B9C2D6" stroke-width="3"/><rect x="-106" y="-4" width="40" height="10" rx="5" fill="#5CC8FF" stroke="${N}" stroke-width="5"/>`,true);
  return ()=>{ const [mx,my]=P.main('d'), [bx,by]=P.monde(-6,46,true);
    S.poser('brosse',mx,my,{op:sort>.1?1:0,rot:fini>.3?60*fini:0,sx:elast(seg(t,.3,.8))});
    S.particules('bullesD',t,1.2,3.8,{n:8,forme:'rond',x:bx,y:by+10,dx:80,vx:0,vy:-60,ex:260,ey:80,vie:.9,taille:.45,et:1,cadence:.11,couleur:'#fff'});
    S.particules('ting',t,4.2,4.25,{n:1,forme:'etoile',x:bx+18,y:by+8,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:2.2,rot:220,boucle:false,couleur:'#fff'});
    S.texte('a',t,1.2,2.4,'frotte frotte',o.x+150,SOL-330,{taille:42,couleur:'#5CC8FF',tremble:2});
    S.texte('b',t,2.5,3.7,'en haut… en bas…',70,SOL-340,{taille:38,couleur:'#7FB6FF'});
    S.texte('c',t,4.2,5.5,'Tout propre !',70,SOL-390,{taille:52,couleur:'#6EDC8C',grossit:.12});
    S.particules('etoiles',t,4.2,5.5,{n:5,forme:'etoile',x:600,y:SOL-300,dx:600,dy:300,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:.9,rot:120,cadence:.26,couleur:'#fff'}); };
});

scene('idee',{titre:"J'ai une idée !",dit:"J'ai une idée !",cat:'actions',duree:5.6,vignette:3.4,cadre:[295,0,600]},(t,o,S)=>{
  const pense=fen(t,.3,2.5,.2), eureka=ease(seg(t,2.6,2.8))*(1-ease(seg(t,5.1,5.5))), b=Math.abs(Math.sin((t-2.7)*6))*eureka;
  o.angle=3*pense-3*eureka; o.tAngle=9*pense-6*eureka; o.saut=-40*cl(t,2.6,3.05)-10*b*seg(t,3.1,3.3); o.sy=1-.03*pense+.07*cl(t,2.6,2.9);
  o.px=-.6*pense; o.py=-.65*pense-.3*eureka; o.lid=.15*pense; o.sourcilY=4*pense+14*eureka; o.sourcilTilt=7*pense; o.grand=1+.22*eureka; o.brillant=eureka;
  o.bouche=eureka>.3?'rire':'petit'; o.ouvre=.75; o.k=eureka>.3?1.2:-.5; o.joues=.6*eureka;
  o.d=mel(mel(R.d,{x:26,y:-196,plie:-1,devant:true,main:20,pouce:-60},pense),{x:126,y:-296,plie:-.4,main:18,pouce:-170},eureka); o.g=mel(R.g,BRAS.hancheG,Math.max(pense,eureka));
  for(let i=0;i<3;i++){ S.objet('q'+i,O(0,0,8+4*i,'#fff',5),false); S.poser('q'+i,o.x-96-i*40,SOL-384-i*26,{op:pense>.3&&(t*2.4%4)>i+.6?1:0}); }
  S.objet('halo',`<circle r="92" fill="#FFF3A8" opacity=".7"/>`,false);
  S.objet('rayonsA',[0,1,2,3,4,5,6,7].map(i=>`<path d="M0,-62 L0,-84" stroke="${N}" stroke-width="11" stroke-linecap="round" transform="rotate(${i*45+22})"/><path d="M0,-62 L0,-84" stroke="#FFC83D" stroke-width="5" stroke-linecap="round" transform="rotate(${i*45+22})"/>`).join(''),true);
  S.objet('ampoule',`<rect x="-16" y="26" width="32" height="20" rx="5" fill="#B9C2D6" stroke="${N}" stroke-width="6"/><path d="M-16,34 h32" stroke="${N}" stroke-width="3"/>${T('M0,-46 C32,-46 42,-18 29,2 C22,12 18,16 18,26 L-18,26 C-18,16 -22,12 -29,2 C-42,-18 -32,-46 0,-46 Z','#FFE066',7)}<path d="M-10,24 L-10,2 l6,-10 l8,10 l6,-10 l-0,0 L10,24" fill="none" stroke="#F0A81C" stroke-width="4" stroke-linejoin="round"/><path d="M-18,-26 q4,-10 14,-12" fill="none" stroke="#fff" stroke-width="6" stroke-linecap="round" opacity=".8"/>`,true);
  return ()=>{ const [hx,hy]=P.monde(0,-160,true), e=.85*elast(seg(t,2.6,3.1))*(1-ease(seg(t,5.1,5.5)));
    S.poser('halo',hx,hy,{sx:e*(1+.08*Math.sin(t*14)),op:t>2.6?1:0});
    S.poser('rayonsA',hx,hy,{sx:e*(1+.1*Math.sin(t*14)),rot:t*40,op:t>2.6?1:0});
    S.poser('ampoule',hx,hy,{sx:e,op:t>2.6?1:0,rot:4*Math.sin(t*6)});
    S.texte('a',t,.5,2.3,'hmmm…',o.x+130,SOL-300,{taille:46,couleur:'#B58BFF',monte:12});
    S.texte('b',t,2.6,3.3,'TING !',o.x+130,SOL-400,{taille:54,couleur:'#FFC83D',grossit:.3});
    S.texte('c',t,3.3,5.0,"J'ai une idée !",70,SOL-330,{taille:44,couleur:'#FFB347',grossit:.1});
    S.particules('eclat',t,2.62,2.68,{n:8,forme:'etoile',x:hx,y:hy,vx:0,vy:0,ex:700,ey:700,vie:.5,taille:1,rot:220,cadence:.005,boucle:false}); };
});

scene('encore',{titre:'Encore !',dit:'Encore ! Encore !',cat:'actions',duree:5.0,vignette:1.63,cadre:[430,20,600]},(t,o,S)=>{
  const dem=fen(t,.4,4.6,.2), b=Math.abs(Math.sin(t*7.5))*dem, w=Math.sin(t*15);
  o.saut=-40*b; o.sy=1-.07*(1-clamp(b*3))*dem+.04*b; o.sx=1+.05*(1-clamp(b*3))*dem; o.angle=4*Math.sin(t*3.75)*dem; o.tAngle=-5*dem+4*w*dem;
  o.grand=1+.22*dem; o.brillant=dem; o.py=-.2*dem; o.sourcilY=12*dem; o.sourcilTilt=-6*dem;
  o.oeil=fen(t,2.4,2.8,.07)>.5?'heureux':'normal'; o.bouche=dem>.3?'rire':'sourire'; o.ouvre=.6+.3*b; o.k=1.2; o.joues=.7*dem;
  o.g=mel(R.g,{x:-126,y:-232+16*w,plie:.25,main:26+4*w},dem); o.d=mel(R.d,{x:126,y:-232-16*w,plie:-.25,main:26-4*w},dem);
  S.objet('boucle',`${L('M-52,8 A52,52 0 0 1 34,-40','#6EDC8C',12)}${T('M24,-66 L60,-30 L14,-22 Z','#6EDC8C',6)}${L('M52,-8 A52,52 0 0 1 -34,40','#B58BFF',12)}${T('M-24,66 L-60,30 L-14,22 Z','#B58BFF',6)}`,true);
  S.poser('boucle',o.x+280,SOL-320,{sx:1.15*elast(seg(t,.8,1.4))*(1-ease(seg(t,4.5,4.9))),rot:t*170,op:t>.8?1:0});
  S.texte('a',t,.6,2.1,'Encore !',o.x-420,SOL-390,{taille:64,couleur:'#B58BFF',grossit:.2,tremble:2});
  S.texte('b',t,2.4,3.4,'Encore !',o.x-400,SOL-300,{taille:50,couleur:'#FF8FC1',tremble:3});
  S.texte('c',t,3.4,4.5,'ENCORE !',o.x-440,SOL-190,{taille:58,couleur:'#FFB347',tremble:3,grossit:.15});
  S.particules('etoiles',t,.8,4.5,{n:6,forme:'etoile',x:600,y:SOL-300,dx:660,dy:320,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:.9,rot:120,cadence:.25});
});

scene('oui',{titre:'Oui !',dit:'Oui !',cat:'actions',duree:4.8,vignette:2.0,cadre:[430,20,600]},(t,o,S)=>{
  const oui=fen(t,.4,4.3,.2), n=Math.sin(t*10)*(fen(t,.5,2.4,.2)+.7*fen(t,3.0,4.1,.2)), pouce=ease(seg(t,1.2,1.5))*(1-ease(seg(t,4.2,4.6))), b=Math.abs(Math.sin(t*5))*oui;
  o.tdy=14*n; o.tAngle=5*n; o.py=.4*n; o.saut=-14*b; o.sy=1-.02*n*oui; o.angle=2*Math.sin(t*5)*oui;
  o.oeil=oui>.4&&n>.2?'heureux':'normal'; o.grand=1.1; o.brillant=oui; o.sourcilY=10*oui; o.bouche=oui>.3?'rire':'sourire'; o.ouvre=.6; o.k=1.2; o.joues=.7*oui;
  o.d=mel(R.d,{x:140,y:-226+8*n,plie:-.4,main:29,pouce:-90},pouce); o.g=mel(R.g,{x:-140,y:-226+8*n,plie:.4,main:29,pouce:-90},pouce*seg(t,2.6,3.0));
  S.objet('coche',`${O(0,0,62,'#3CB371',8)}${L('M-28,2 L-8,24 L32,-22','#fff',13)}`,true);
  S.poser('coche',o.x+270,SOL-310,{sx:elast(seg(t,1.2,1.8))*(1-ease(seg(t,4.2,4.6))),rot:5*Math.sin(t*10),op:t>1.2?1:0});
  S.texte('a',t,1.3,2.6,'OUI !',o.x-370,SOL-350,{taille:88,couleur:'#3CB371',grossit:.15,tremble:2});
  S.texte('b',t,3.0,4.2,'Oui oui oui !',70,SOL-390,{taille:50,couleur:'#6EDC8C',tremble:2});
  S.particules('etoiles',t,1.3,4.2,{n:6,forme:'etoile',x:600,y:SOL-300,dx:660,dy:320,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:.9,rot:120,cadence:.25});
});
