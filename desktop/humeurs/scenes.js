// Les humeurs et les actions du personnage d'Eyzi. Une scène = scene(clé, {titre, dit, cat, duree, vignette}, (t, o, S) => …)
// « o » est la pose de l'image (voir rig.js) ; la fonction peut rendre une fonction appelée APRÈS la pose (pour suivre une main).
const mel=(a,b,u)=>({x:lerp(a.x,b.x,u),y:lerp(a.y,b.y,u),plie:lerp(a.plie??.4,b.plie??.4,u),devant:u>.5?b.devant:a.devant,main:u>.5?(b.main??23):(a.main??23),pouce:u>.5?b.pouce:a.pouce});
const R=BRAS.repos;

// ───────────────────────────── LA JOIE ─────────────────────────────
scene('tropcool',{titre:'Trop cool !',dit:'Trop cool !',cat:'joie',duree:5.6,vignette:2.2},(t,o,S)=>{
  const pose=ease(seg(t,1.2,1.7))*(1-ease(seg(t,5.1,5.5))), glisse=fen(t,2.3,4.3,.3);
  o.x=600+46*Math.sin((t-2.3)*3.1)*glisse; o.angle=5*pose+7*Math.cos((t-2.3)*3.1)*glisse; o.saut=-10*Math.abs(Math.sin((t-2.3)*6.2))*glisse;
  o.tAngle=-6*pose+5*Math.sin(t*6.2)*glisse; o.k=lerp(.7,1.2,pose); o.sourcilY=8*pose; o.sourcilTilt=-6*pose;
  const clin=fen(t,4.5,5.0,.08); o.oeil=clin>.5?'heureux':'normal'; o.px=.2;
  o.g=mel(R.g,BRAS.hancheG,pose); o.d=mel(R.d,{x:122,y:-212,plie:-.5,main:27,pouce:-95},ease(seg(t,1.5,1.9))*(1-ease(seg(t,5.0,5.4))));
  const ly=k(t,[[.5,-330],[1.0,8,'in'],[1.1,-12,'out'],[1.2,0],[4.45,0],[4.6,-52,'out'],[4.95,-52],[5.05,0,'in'],[5.2,0],[5.6,-360,'in']]);
  S.objet('lunettes',`<path d="M-58,-34 L-100,-50" stroke="${N}" stroke-width="10" stroke-linecap="round"/><path d="M86,-34 L104,-44" stroke="${N}" stroke-width="10" stroke-linecap="round"/><rect x="-60" y="-52" width="150" height="62" rx="27" fill="${N}"/><path d="M-36,-36 l30,0 M4,-36 l10,0" stroke="#fff" stroke-width="7" stroke-linecap="round" opacity=".75"/>`,P.surTete);
  S.poser('lunettes',0,ly,{op:t>.5?1:0,rot:k(t,[[.5,-14],[1.2,0]])});
  S.particules('eclat',t,1.15,1.2,{n:1,forme:'etoile',x:o.x+70,y:SOL-300,vx:0,vy:0,ex:0,ey:0,vie:.5,taille:2.2,rot:200,boucle:false,couleur:'#fff'});
  S.particules('eclat2',t,3.3,3.35,{n:1,forme:'etoile',x:o.x+60,y:SOL-300,vx:0,vy:0,ex:0,ey:0,vie:.5,taille:2,rot:200,boucle:false,couleur:'#fff'});
  S.texte('a',t,1.9,3.0,'Trop cool !',o.x+150,SOL-330,{taille:54,couleur:'#5CC8FF',monte:20,grossit:.15});
  S.particules('etoiles',t,1.8,5.0,{n:6,forme:'etoile',x:600,y:SOL-250,dx:520,dy:260,vx:0,vy:-14,ex:0,ey:0,vie:.9,taille:.9,rot:90,cadence:.45});
});

scene('youpi',{titre:'Youpi !',dit:'Youpi !',cat:'joie',duree:5.0,vignette:1.35},(t,o,S)=>{
  const elan=fen(t,.25,.6,.12), sauts=fen(t,.6,4.5,.2), ph=(t-.6)*5.4, tour=seg(t,2.55,3.15);
  const h=Math.abs(Math.sin(ph)); o.saut=-96*h*sauts; const sol=1-clamp(h*3);
  o.sy=1-.14*elan+.08*h*sauts-.12*sol*sauts; o.sx=(1+.1*elan+.1*sol*sauts)*(tour>0&&tour<1?Math.cos(tour*Math.PI*2):1);
  o.angle=6*Math.sin(ph*.5)*sauts; o.tAngle=-6*Math.sin(ph*.5)*sauts;
  o.oeil=sauts>.3?'heureux':'normal'; o.grand=1.15; o.bouche=sauts>.2?'rire':'sourire'; o.ouvre=.6+.3*h; o.joues=.8*sauts; o.sourcilY=10*sauts;
  const haut=sauts*(.55+.45*h);
  o.g=mel(R.g,{x:-126+16*Math.sin(t*17),y:-236,plie:.3,main:25},haut); o.d=mel(R.d,{x:126+16*Math.sin(t*17+2),y:-236,plie:-.3,main:25},haut);
  S.particules('conf',t,.7,4.3,{n:26,forme:'confetti',x:600,y:SOL-520,dx:900,dy:40,vx:0,vy:200,ex:90,ey:120,g:60,vie:2.2,taille:1,rot:320,cadence:.085,couleurs:COUL,onde:14,devant:true});
  S.texte('a',t,.65,1.9,'YOUPI !',o.x-330,SOL-380,{taille:66,couleur:'#FFB347',tremble:3,grossit:.25});
  S.texte('b',t,3.2,4.4,'Youpiiii !',o.x+130,SOL-400,{taille:54,couleur:'#FF4F7B',tremble:2,grossit:.2});
  S.particules('etoiles',t,.7,4.3,{n:5,forme:'etoile',x:600,y:SOL-300,dx:600,dy:300,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:1.1,rot:120,cadence:.3});
});

scene('rigole',{titre:'Je rigole !',dit:'Ha ha ha ! Je rigole !',cat:'joie',duree:6.0,vignette:1.6},(t,o,S)=>{
  o.x=520;
  const pouffe=fen(t,.3,1.2,.15), rit=fen(t,1.1,2.6,.2), tombe=ease(seg(t,2.5,2.9))*(1-ease(seg(t,4.6,5.1))), sol=fen(t,2.9,4.6,.15), fin=fen(t,5.1,5.9,.2);
  const sec=Math.sin(t*24);
  o.angle=3*sec*pouffe+5*sec*rit*(1-tombe)+82*tombe+4*Math.sin(t*20)*sol; o.saut=-10*Math.abs(sec)*rit*(1-tombe)-14*Math.abs(Math.sin(t*10))*sol-54*tombe;
  o.sy=1-.05*Math.abs(sec)*(pouffe+rit)+.03*sol*Math.sin(t*20); o.tAngle=-14*rit*(1-tombe)+8*sec*rit*(1-tombe)+10*Math.sin(t*10)*sol; o.ventre=6*Math.abs(sec)*(rit+sol);
  o.oeil=(rit>.3||sol>.3)?'heureux':'normal'; o.lid=.4*pouffe; o.px=.3*pouffe;
  o.bouche=(rit>.2||sol>.2)?'rire':pouffe>.3?'petit':'sourire'; o.ouvre=.6+.4*Math.abs(Math.sin(t*12)); o.k=1.1; o.joues=Math.max(pouffe,rit,sol,fin)*.9; o.sourcilY=8*rit;
  o.g=mel(mel(R.g,BRAS.boucheG,pouffe),{x:-10,y:-110,plie:.7,devant:true},Math.max(rit,sol)); o.d=mel(R.d,{x:120+20*sec,y:-150+20*Math.cos(t*24),plie:-.4},Math.max(rit*.8,sol));
  if(fin>.2){ o.g=mel(R.g,{x:-20,y:-290,plie:.9,devant:true},fin); o.bouche='sourire'; o.oeil='normal'; o.lid=.25; }
  return ()=>{ const [ex,ey]=P.monde(16,-20,true);
    S.particules('larmes',t,1.2,4.6,{n:12,forme:'goutte',x:ex,y:ey,dx:60,vx:0,vy:-200,ex:420,ey:120,g:900,vie:.7,taille:.9,cadence:.11});
    S.texte('a',t,1.2,2.5,'HA HA HA !',o.x-360,SOL-380,{taille:62,couleur:'#FFC83D',tremble:4});
    S.texte('b',t,3.0,4.5,'HI HI HI HA HA !',o.x+10,SOL-300,{taille:50,couleur:'#FF8FC1',tremble:4});
    S.texte('c',t,5.1,5.9,'ouf…',o.x+110,SOL-360,{taille:44,couleur:'#7FD3FF',monte:16}); };
});

scene('bravo',{titre:'Bravo !',dit:'Bravo !',cat:'joie',duree:5.2,vignette:3.4},(t,o,S)=>{
  const tape=fen(t,.5,2.6,.15), f=Math.sin(t*26), ferme=clamp(f*1.6+.4), pouces=ease(seg(t,2.8,3.2))*(1-ease(seg(t,4.7,5.1)));
  o.saut=-8*Math.abs(Math.sin(t*13))*tape-30*Math.abs(Math.sin((t-2.9)*6))*pouces; o.angle=3*Math.sin(t*6.5)*tape; o.tAngle=4*Math.sin(t*6.5);
  o.oeil=(tape>.3||pouces>.3)?'heureux':'normal'; o.grand=1.1; o.bouche=pouces>.3?'rire':'sourire'; o.ouvre=.7; o.k=1.15; o.joues=.7; o.sourcilY=8;
  o.g=mel(R.g,{x:lerp(-78,-16,ferme),y:-150,plie:.7,devant:true,main:25},tape); o.d=mel(R.d,{x:lerp(78,16,ferme),y:-150,plie:-.7,devant:true,main:25},tape);
  if(pouces>.02){ o.g=mel(o.g,{x:-132,y:-226,plie:.4,main:27,pouce:-90},pouces); o.d=mel(o.d,{x:132,y:-226,plie:-.4,main:27,pouce:-90},pouces); }
  S.particules('clap',t,.55,2.55,{n:8,forme:'etoile',x:o.x,y:SOL-150,vx:0,vy:-60,ex:560,ey:420,vie:.32,taille:.8,rot:200,cadence:.12});
  S.texte('a',t,.6,1.5,'clap clap',o.x+120,SOL-210,{taille:44,couleur:'#FFC83D',tremble:2});
  S.texte('b',t,1.5,2.5,'clap clap clap',o.x-390,SOL-230,{taille:44,couleur:'#FFB347',tremble:2});
  S.texte('c',t,2.9,4.7,'BRAVO !',o.x,SOL-410,{taille:72,couleur:'#6EDC8C',grossit:.25,tremble:2,ancre:'middle'});
  S.particules('conf',t,2.9,4.4,{n:16,forme:'confetti',x:600,y:SOL-500,dx:800,vx:0,vy:210,ex:80,ey:100,vie:1.9,rot:300,cadence:.09,couleurs:COUL,onde:12});
});

// ───────────────────────────── TENDRE ─────────────────────────────
scene('coucou',{titre:'Coucou !',dit:'Coucou !',cat:'tendre',duree:5.2,vignette:2.3},(t,o,S)=>{
  const arr=seg(t,0,1.3), pas=Math.abs(Math.sin(arr*Math.PI*4)), salue=fen(t,1.5,4.4,.2), sort=ease(seg(t,4.6,5.2));
  o.x=lerp(60,600,out(arr))+lerp(0,640,sort*sort); o.saut=-40*pas*(1-seg(t,1.2,1.3))-30*Math.abs(Math.sin(sort*Math.PI*3));
  o.angle=k(t,[[0,10],[1.15,10],[1.35,-9,'out'],[1.6,0,'back'],[4.6,0],[5.2,12]]); o.sy=1-.06*(1-clamp(pas*3))*(1-seg(t,1.3,1.4));
  o.tAngle=7*Math.sin(t*7)*salue; o.grand=1+.15*salue; o.brillant=salue; o.px=-.1; o.k=1.2; o.joues=.6*salue; o.sourcilY=10*salue;
  const clin=fen(t,3.3,3.7,.07); o.oeil=clin>.5?'heureux':'normal'; o.bouche=salue>.4&&t<3.2?'rire':'sourire'; o.ouvre=.55;
  o.d=mel(R.d,{x:132+34*Math.sin(t*13),y:-268+8*Math.cos(t*13),plie:-.35,main:28,pouce:-150},salue); o.g=mel(R.g,BRAS.hancheG,salue);
  S.texte('a',t,1.6,3.2,'Coucou !',o.x-330,SOL-380,{taille:64,couleur:'#5CC8FF',grossit:.2});
  S.texte('b',t,3.5,4.6,'Coucou coucou !',o.x-400,SOL-330,{taille:48,couleur:'#B58BFF',monte:14});
  S.particules('etoiles',t,3.35,3.4,{n:1,forme:'etoile',x:o.x+70,y:SOL-330,vx:60,vy:-60,ex:0,ey:0,vie:.6,taille:1.6,rot:200,boucle:false});
  S.particules('pas',t,.1,1.2,{n:5,forme:'rond',x:o.x-40,y:SOL-8,vx:-60,vy:-30,ex:20,ey:20,vie:.5,taille:.8,gonfle:1,cadence:.2,devant:false,couleur:'#EFE6FA',op:.8});
});

scene('merci',{titre:'Merci !',dit:'Merci beaucoup !',cat:'tendre',duree:5.8,vignette:2.9},(t,o,S)=>{
  const cache=1-ease(seg(t,1.1,1.6)), tend=ease(seg(t,1.9,2.5))*(1-ease(seg(t,3.9,4.4))), coeur=ease(seg(t,4.3,4.8))*(1-ease(seg(t,5.5,5.8)));
  o.angle=3*Math.sin(t*3)*cache-16*tend; o.tAngle=-8*tend+6*Math.sin(t*3)*coeur; o.px=k(t,[[0,.6],[.5,-.6],[1.0,.5],[1.5,0]]); o.lid=.2*cache; o.grand=1+.12*tend; o.brillant=tend;
  o.oeil=coeur>.5?'heureux':'normal'; o.k=lerp(.8,1.25,Math.max(tend,coeur)); o.joues=.4+.5*Math.max(tend,coeur); o.sourcilY=8*tend;
  o.g=mel(BRAS.dosG,R.g,1-cache); o.g=mel(o.g,{x:8,y:-128,plie:.8,devant:true},coeur);
  const sorti=ease(seg(t,1.15,1.65)); let d=mel(BRAS.dosD,{x:118,y:-150,plie:-.4},sorti); d=mel(d,{x:-150,y:-176,plie:-.25,devant:true},tend); o.d=mel(d,{x:118,y:-150,plie:-.4},ease(seg(t,3.9,4.4))*(1-coeur));
  S.objet('fleur',`<path d="M0,0 Q-6,-50 0,-104" fill="none" stroke="${N}" stroke-width="15" stroke-linecap="round"/><path d="M0,0 Q-6,-50 0,-104" fill="none" stroke="#4E9B3A" stroke-width="8" stroke-linecap="round"/><path d="M-2,-46 q30,-22 50,-4 q-28,14 -50,4 Z" fill="#6CBF4B" stroke="${N}" stroke-width="5"/>`+[0,1,2,3,4,5,6].map(i=>`<ellipse rx="13" ry="20" transform="translate(0,-104) rotate(${i*51.4}) translate(0,-22)" fill="#FF8FC1" stroke="${N}" stroke-width="5"/>`).join('')+`<circle cx="0" cy="-104" r="14" fill="#FFC83D" stroke="${N}" stroke-width="5"/>`,true);
  return ()=>{ const [mx,my]=P.main('d'); S.poser('fleur',mx,my,{op:sorti>.1?1:0,rot:lerp(14,-34,tend)+4*Math.sin(t*5),sx:elast(seg(t,1.2,1.7))});
    S.particules('eclat',t,1.5,1.55,{n:5,forme:'etoile',x:mx,y:my-100,vx:0,vy:-30,ex:300,ey:300,vie:.6,taille:.9,rot:200,cadence:.01,boucle:false});
    S.texte('a',t,2.3,3.9,'Merci !',o.x-380,SOL-360,{taille:64,couleur:'#FF8FC1',grossit:.2});
    S.particules('coeurs',t,2.4,5.3,{n:7,forme:'coeur',x:o.x,y:SOL-300,dx:420,dy:120,vx:0,vy:-90,ex:20,ey:30,vie:1.5,taille:1,cadence:.4,onde:10,incline:40}); };
});

// ───────────────────────────── RIGOLO ─────────────────────────────
scene('nananere',{titre:'Nananère !',dit:'Nananère !',cat:'rigolo',duree:5.2,vignette:1.5},(t,o,S)=>{
  const jeu=fen(t,.5,4.7,.2), w=Math.sin(t*11);
  o.angle=11*w*jeu; o.saut=-22*Math.abs(Math.sin(t*11))*jeu; o.tAngle=-9*w*jeu; o.sy=1-.05*(1-Math.abs(w))*jeu;
  const clin=Math.sin(t*5.5)>0; o.oeil=jeu>.4?(clin?'heureux':'normal'):'normal'; o.px=.2*w; o.lid=.15; o.sourcilY=8*jeu; o.sourcilTilt=7*w*jeu;
  o.bouche=jeu>.3?'langue':'sourire'; o.ouvre=.6+.5*Math.abs(Math.sin(t*22)); o.joues=.5*jeu;
  o.g=mel(R.g,{x:-108,y:-292+14*Math.sin(t*22),plie:.55,main:28,pouce:-20},jeu); o.d=mel(R.d,{x:108,y:-292+14*Math.sin(t*22+2),plie:-.55,main:28,pouce:-160},jeu);
  return ()=>{ const [bx,by]=P.monde(-6,84,true);
    S.particules('postillons',t,.8,4.5,{n:10,forme:'goutte',x:bx,y:by,vx:-60,vy:60,ex:360,ey:200,g:500,vie:.5,taille:.45,cadence:.09,couleur:'#BFE8FF'});
    S.texte('a',t,.7,2.2,'Nananère !',o.x-420,SOL-390,{taille:60,couleur:'#B58BFF',tremble:3});
    S.texte('b',t,2.4,3.4,'prrrrt !',o.x+130,SOL-260,{taille:50,couleur:'#FF8FC1',tremble:4});
    S.texte('c',t,3.4,4.6,'Nanana-nè-reuh !',o.x-500,SOL-410,{taille:50,couleur:'#5CC8FF',tremble:3});
    S.particules('notes',t,.7,4.5,{n:6,forme:'texte',mot:['♪','♫'],x:o.x,y:SOL-380,dx:420,vx:0,vy:-70,ex:60,ey:20,vie:1.1,taille:1,cadence:.35,couleurs:COUL,onde:8}); };
});

scene('chut',{titre:'Chut…',dit:'Chut !',cat:'rigolo',duree:5.6,vignette:2.2},(t,o,S)=>{
  const doigt=ease(seg(t,1.0,1.4))*(1-ease(seg(t,5.1,5.5))), marche=fen(t,2.4,5.0,.2), pas=Math.abs(Math.sin((t-2.4)*5));
  o.x=660-150*ease(seg(t,2.4,5.0)); o.saut=-16*pas*marche; o.sy=1-.07*(1-clamp(pas*2.5))*marche; o.angle=-5*marche;
  o.px=k(t,[[0,.7],[.45,.7],[.7,-.7],[1.0,.6],[1.5,.3],[2.4,-.3],[3.6,-.4],[3.9,.8],[4.3,.8],[4.6,-.4]]); o.lid=.22; o.grand=1.05; o.sourcilY=6*doigt;
  o.bouche=doigt>.4?'o':'petit'; o.ouvre=.25; o.k=.6; o.tAngle=4*doigt;
  o.d=mel(R.d,{x:-2,y:-208,plie:-1.1,devant:true,main:17,pouce:-90},doigt); o.g=mel(R.g,{x:-96,y:-120,plie:.5},marche);
  S.objet('chat',`<ellipse cx="0" cy="-30" rx="84" ry="42" fill="#F2A65A" stroke="${N}" stroke-width="7"/><path d="M70,-22 q46,6 34,-34 q-8,-18 -22,-8" fill="none" stroke="${N}" stroke-width="20" stroke-linecap="round"/><path d="M70,-22 q46,6 34,-34 q-8,-18 -22,-8" fill="none" stroke="#F2A65A" stroke-width="9" stroke-linecap="round"/><circle cx="-56" cy="-52" r="36" fill="#F6B46E" stroke="${N}" stroke-width="7"/><path d="M-84,-74 l-4,-30 l26,14 Z M-40,-84 l14,-26 l12,30 Z" fill="#F2A65A" stroke="${N}" stroke-width="6" stroke-linejoin="round"/><path d="M-74,-52 q7,7 14,0 M-50,-50 q7,7 14,0" fill="none" stroke="${N}" stroke-width="5" stroke-linecap="round"/><path d="M-62,-38 l4,4 l4,-4" fill="none" stroke="${N}" stroke-width="4" stroke-linecap="round"/><path d="M-30,-34 q20,-14 44,-6 M-20,-18 q24,-10 50,0" fill="none" stroke="#D98A3C" stroke-width="5" stroke-linecap="round"/>`,false);
  const resp=Math.sin(t*2.2); S.poser('chat',930,SOL,{sx:1+.02*resp,sy:1+.05*resp});
  S.particules('zzz',t,0,5.6,{n:3,forme:'texte',mot:'z',x:880,y:SOL-110,vx:26,vy:-60,ex:10,ey:10,vie:1.6,taille:.9,gonfle:.5,cadence:.55,couleur:'#8FA3FF'});
  S.texte('a',t,1.3,2.6,'Chuuut…',o.x-300,SOL-360,{taille:54,couleur:'#B58BFF',monte:14});
  S.texte('b',t,3.0,4.6,'chut… il dort…',o.x-330,SOL-380,{taille:40,couleur:'#8FA3FF',monte:10});
});

// ───────────────────────────── ÇA NE VA PAS ─────────────────────────────
scene('triste',{titre:'Je suis triste',dit:'Je suis triste.',cat:'pasbien',duree:6.2,vignette:3.0},(t,o,S)=>{
  const monte=ease(seg(t,.2,1.2)), pleure=fen(t,1.3,5.6,.3), hoq=Math.pow(Math.max(0,Math.sin(t*5.2)),6), essuie=fen(t,4.2,5.2,.2);
  o.angle=-3*monte; o.tAngle=-12*monte+3*Math.sin(t*5.2)*pleure; o.tdy=6*monte; o.saut=-8*hoq*pleure; o.sy=1-.03*monte-.03*hoq*pleure;
  o.px=-.25; o.py=.6*monte; o.lid=.3*monte; o.grand=1+.12*monte; o.brillant=monte; o.sourcilY=2; o.sourcilTilt=-13*monte; o.sourcilArc=-4*monte;
  o.oeil=pleure>.6&&Math.sin(t*5.2)>.75?'serre':'normal'; o.bouche='triste'; o.k=.75+.2*monte+.12*Math.sin(t*26)*pleure; o.teinte=.12*monte; o.teinteCouleur='#9DB8FF';
  o.g=mel(R.g,{x:-66,y:-60,plie:.2},monte); o.d=mel(mel(R.d,{x:66,y:-60,plie:-.2},monte),{x:44+8*Math.sin(t*14),y:-286,plie:-1.0,devant:true},essuie);
  S.objet('nuage',`<path d="M-110,10 a44,44 0 0 1 20,-84 a62,62 0 0 1 116,-18 a50,50 0 0 1 80,56 a34,34 0 0 1 -14,46 Z" fill="#B9C2D6" stroke="${N}" stroke-width="8" stroke-linejoin="round"/><path d="M-70,-46 q20,-30 54,-26" fill="none" stroke="#DCE2F0" stroke-width="9" stroke-linecap="round"/>`,true);
  const na=ease(seg(t,.9,1.9))*(1-ease(seg(t,5.6,6.2))); S.poser('nuage',o.x+lerp(360,0,out(seg(t,.9,1.9)))+10*Math.sin(t*1.7),SOL-415+6*Math.sin(t*2.3),{op:na,sx:.85});
  S.particules('pluie',t,1.9,5.5,{n:14,forme:'trait',x:o.x,y:SOL-400,dx:200,vx:-30,vy:520,ex:0,ey:60,vie:.8,taille:.8,cadence:.07,couleur:'#7FB6FF',incline:0,devant:false});
  S.objet('flaque',`<ellipse rx="150" ry="20" fill="#A9D4FF" stroke="${N}" stroke-width="7"/><path d="M-90,-4 q30,-8 60,-4" fill="none" stroke="#E2F2FF" stroke-width="6" stroke-linecap="round"/>`,false);
  S.poser('flaque',o.x,SOL+4,{sx:ease(seg(t,2.0,5.2))*(1-ease(seg(t,5.7,6.2))),op:t>2?1:0});
  return ()=>{ const [ex,ey]=P.monde(16,10,true);
    S.particules('larmes',t,1.3,5.6,{n:10,forme:'goutte',x:ex,y:ey,dx:70,vx:0,vy:70,ex:150,ey:40,g:700,vie:.75,taille:1,cadence:.13});
    S.texte('a',t,1.5,2.7,'snif…',o.x+120,SOL-300,{taille:44,couleur:'#7FB6FF',monte:12});
    S.texte('b',t,3.0,4.3,'bouhouhou…',o.x-420,SOL-300,{taille:50,couleur:'#7FB6FF',tremble:2});
    S.texte('c',t,4.6,5.6,'snif snif',o.x+110,SOL-320,{taille:42,couleur:'#9DC4FF',monte:12}); };
});

scene('colere',{titre:'Grrr !',dit:'Grrr ! Je suis en colère !',cat:'pasbien',duree:5.8,vignette:3.5},(t,o,S)=>{
  const monte=ease(seg(t,.2,2.6)), crie=fen(t,3.0,4.3,.12), calme=ease(seg(t,4.5,5.6)), tr=Math.sin(t*52);
  const tape=Math.pow(Math.max(0,Math.sin((t-1.2)*7.5)),8)*fen(t,1.2,2.9,.1);
  const rouge=monte*(1-calme);
  o.angle=1.8*tr*rouge+2.5*tr*crie; o.saut=-26*tape; o.sy=1-.07*rouge*(1-crie)+.09*crie; o.sx=1+.06*rouge*(1-crie)-.03*crie; o.tAngle=-6*crie+2*tr*crie; o.tsx=1+.06*crie; o.tsy=1+.06*crie;
  o.teinte=.78*rouge; o.teinteCouleur='#F0483E'; o.px=-.1; o.py=.15; o.pupille=1-.35*rouge; o.grand=1+.1*crie; o.lid=.16*rouge*(1-crie);
  o.sourcilY=-16*rouge; o.sourcilTilt=20*rouge; o.oeil=crie>.6&&Math.sin(t*20)>0?'serre':'normal';
  o.bouche=crie>.3?'rire':rouge>.25?'dents':'plat'; o.ouvre=1; o.k=.9;
  o.g=mel(R.g,{x:-80+5*tr,y:-66,plie:-.2,main:26,pouce:null},rouge); o.d=mel(R.d,{x:80+5*tr,y:-66,plie:.2,main:26,pouce:null},rouge);
  if(crie>.05){ o.g=mel(o.g,{x:-136,y:-230,plie:.3,main:27,pouce:null},crie); o.d=mel(o.d,{x:136,y:-230,plie:-.3,main:27,pouce:null},crie); }
  if(calme>.05){ o.bouche=calme>.6?'petit':'o'; o.ouvre=.4; o.k=.5; }
  return ()=>{ const [hx,hy]=P.monde(0,-70,true);
    S.particules('vapG',t,1.0,4.4,{n:7,forme:'rond',x:hx-70,y:hy,vx:-150,vy:-170,ex:60,ey:60,vie:.6,taille:.9,gonfle:1.6,cadence:.11,couleur:'#fff',devant:false});
    S.particules('vapD',t,1.05,4.4,{n:7,forme:'rond',x:hx+70,y:hy,vx:150,vy:-170,ex:60,ey:60,vie:.6,taille:.9,gonfle:1.6,cadence:.11,couleur:'#fff',devant:false});
    S.particules('choc',t,1.25,2.9,{n:6,forme:'trait',x:o.x,y:SOL-6,dx:260,vx:0,vy:-40,ex:300,ey:20,vie:.22,taille:.9,cadence:.14,incline:120});
    S.texte('a',t,1.3,2.0,'boum !',o.x+110,SOL-70,{taille:44,couleur:'#FFB347',tremble:3});
    S.texte('b',t,2.1,2.9,'boum boum !',o.x-380,SOL-80,{taille:44,couleur:'#FFB347',tremble:3});
    S.texte('c',t,3.0,4.4,'GRRRRR !',o.x,SOL-420,{taille:84,couleur:'#F0483E',tremble:7,grossit:.2,ancre:'middle'});
    S.objet('eclair',`<path d="M0,-40 L-22,4 L-4,4 L-16,44 L24,-8 L6,-8 L18,-40 Z" fill="#FFE066" stroke="${N}" stroke-width="6" stroke-linejoin="round"/>`,true);
    S.objet('eclair2',`<path d="M0,-40 L-22,4 L-4,4 L-16,44 L24,-8 L6,-8 L18,-40 Z" fill="#FFE066" stroke="${N}" stroke-width="6" stroke-linejoin="round"/>`,true);
    const e=crie*(.6+.4*Math.abs(Math.sin(t*30))); S.poser('eclair',o.x-210,SOL-300,{op:e,rot:-20,sx:1.6}); S.poser('eclair2',o.x+210,SOL-290,{op:e,rot:24,sx:1.5});
    S.texte('d',t,4.7,5.7,'pffff…',o.x+120,SOL-330,{taille:46,couleur:'#B9C2D6',monte:16});
    S.particules('souffle',t,4.7,5.5,{n:4,forme:'rond',x:o.x-20,y:SOL-212,vx:-120,vy:-10,ex:30,ey:30,vie:.7,taille:.8,gonfle:1.4,cadence:.16,couleur:'#fff'}); };
});

scene('peur',{titre:"J'ai peur !",dit:"J'ai peur !",cat:'pasbien',duree:6.0,vignette:1.9},(t,o,S)=>{
  const alerte=ease(seg(t,.55,.8)), tremble=fen(t,.9,5.4,.2), cache=fen(t,2.3,3.9,.18), souris=seg(t,3.9,5.0), sursaut=Math.pow(Math.sin(Math.PI*seg(t,4.15,4.75)),.7)||0, tr=Math.sin(t*46);
  o.x=600+3*tr*tremble; o.angle=1.5*tr*tremble-8*alerte*(1-cache); o.saut=-90*sursaut; o.sy=1-.05*tremble+.1*sursaut+.02*tr*tremble;
  o.teinte=.62*alerte*(1-seg(t,5.4,6.0)); o.teinteCouleur='#BFD9FF';
  o.px=k(t,[[0,0],[.55,0],[.7,.9],[1.4,.9],[1.6,-.8],[1.9,.8],[2.2,-.6],[3.9,.2],[4.1,.7],[5.2,-.9],[5.8,0]]); o.py=.1+ .5*souris*(1-seg(t,4.6,5.2)); o.grand=1+.32*alerte; o.pupille=1-.45*alerte; o.sourcilY=16*alerte; o.sourcilTilt=-8*alerte;
  o.bouche=alerte>.3?'o':'sourire'; o.ouvre=.5+.6*sursaut+.1*tr*tremble; if(tremble>.3&&cache<.3&&sursaut<.1&&t<2.3) o.bouche='vague';
  let g=mel(R.g,{x:-58,y:-196,plie:.9,devant:true},alerte), d=mel(R.d,{x:62,y:-196,plie:-.9,devant:true},alerte);
  const peek=fen(t,3.2,3.9,.12); g=mel(g,{x:-12,y:-286,plie:1.0,devant:true,main:30},cache); d=mel(d,{x:46,y:lerp(-286,-232,peek),plie:-1.0,devant:true,main:30},cache);
  if(sursaut>.05){ g=mel(g,{x:-140,y:-240,plie:.2,main:28},sursaut); d=mel(d,{x:140,y:-240,plie:-.2,main:28},sursaut); } o.g=g; o.d=d;
  S.texte('a',t,.55,1.3,'CRAC !',820,SOL-250,{taille:54,couleur:'#8FA3FF',tremble:4});
  S.texte('b',t,1.5,2.5,"J'ai peur…",o.x-380,SOL-340,{taille:50,couleur:'#8FB8FF',tremble:3});
  S.texte('c',t,4.2,5.0,'AAAAH !',o.x-360,SOL-430,{taille:70,couleur:'#5C8DFF',tremble:6});
  S.particules('sueur',t,.9,5.2,{n:6,forme:'goutte',x:o.x,y:SOL-340,dx:170,vx:0,vy:40,ex:160,ey:30,g:500,vie:.6,taille:.8,cadence:.16});
  S.objet('souris',`<path d="M40,-10 q50,-20 80,10" fill="none" stroke="${N}" stroke-width="9" stroke-linecap="round"/><path d="M40,-10 q50,-20 80,10" fill="none" stroke="#F3A7B8" stroke-width="4" stroke-linecap="round"/><ellipse cx="0" cy="-20" rx="46" ry="26" fill="#C9CBD6" stroke="${N}" stroke-width="6"/><circle cx="-28" cy="-44" r="15" fill="#F3A7B8" stroke="${N}" stroke-width="5"/><circle cx="-6" cy="-48" r="13" fill="#F3A7B8" stroke="${N}" stroke-width="5"/><circle cx="-30" cy="-22" r="5" fill="${N}"/><circle cx="-46" cy="-16" r="5" fill="#F27A9B" stroke="${N}" stroke-width="3"/>`,true);
  S.poser('souris',lerp(1160,60,souris),SOL-2-8*Math.abs(Math.sin(souris*40)),{op:souris>0&&souris<1?1:0});
  S.texte('d',t,4.0,4.9,'couic !',lerp(1160,60,souris)-20,SOL-70,{taille:34,couleur:'#F3A7B8'});
});

scene('boude',{titre:'Je boude',dit:'Je boude.',cat:'pasbien',duree:5.8,vignette:1.7},(t,o,S)=>{
  const b=ease(seg(t,.3,.9))*(1-ease(seg(t,5.4,5.8))), coup=fen(t,3.0,3.5,.1), pied=Math.pow(Math.max(0,Math.sin(t*8)),10)*b;
  o.angle=5*b; o.tAngle=13*b-16*coup; o.sy=1-.03*b-.03*pied; o.px=lerp(0,.95,b)-1.5*coup; o.py=-.2*b; o.lid=.34*b; o.sourcilY=-10*b; o.sourcilTilt=12*b;
  o.bouche='triste'; o.k=.6+.1*Math.sin(t*3); if(b<.3){ o.bouche='plat'; } o.joues=.4*b; o.tsx=1+.04*b;
  o.g=mel(R.g,BRAS.croiseG,b); o.d=mel(R.d,BRAS.croiseD,b);
  S.objet('gribouillis',`<path d="M-70,10 C-110,-30 -40,-80 -10,-40 C10,-90 90,-70 60,-20 C110,-30 100,40 50,20 C40,60 -30,50 -30,14 C-60,50 -110,30 -70,10 M-40,-10 C-10,-40 30,-30 30,0 C30,20 -10,30 -20,6" fill="none" stroke="${N}" stroke-width="9" stroke-linecap="round" stroke-linejoin="round"/>`,true);
  S.poser('gribouillis',o.x+10+6*Math.sin(t*9),SOL-425+5*Math.cos(t*11),{op:b,rot:6*Math.sin(t*7),sx:.8+.05*Math.sin(t*13)});
  S.texte('a',t,.9,2.0,'hmpf !',o.x-300,SOL-330,{taille:54,couleur:'#9AA3B8',monte:10});
  S.texte('b',t,3.5,4.6,'HMPF !',o.x+130,SOL-340,{taille:58,couleur:'#7C869E',tremble:2});
  S.particules('vap',t,3.5,4.2,{n:3,forme:'rond',x:o.x+70,y:SOL-330,vx:120,vy:-90,ex:30,ey:30,vie:.6,taille:.8,gonfle:1.4,cadence:.14,couleur:'#fff',boucle:false});
});

scene('malade',{titre:'Je suis malade',dit:'Je suis malade.',cat:'pasbien',duree:6.2,vignette:1.4},(t,o,S)=>{
  const monte=ease(seg(t,2.4,3.3)), at=seg(t,3.35,3.6), apres=ease(seg(t,3.6,4.2)), fris=Math.sin(t*38)*fen(t,.3,2.4,.2), fin=fen(t,4.4,5.9,.2);
  const coup=Math.sin(Math.PI*clamp(at));
  o.x=600+2*fris; o.angle=2*fris+8*monte*(1-clamp(at*3))-20*coup; o.tAngle=16*monte*(1-clamp(at*3))-30*coup+6*fin; o.sy=1-.03+.08*monte*(1-clamp(at*3))-.1*coup; o.tdy=4*fin;
  o.teinte=.5; o.teinteCouleur='#D9EDB8'; o.lid=.4-.3*monte+.25*fin; o.py=.3; o.px=-.1; o.sourcilTilt=-10; o.sourcilY=2+10*monte*(1-apres);
  o.oeil=coup>.2?'serre':'normal'; o.bouche=coup>.2?'rire':monte>.3&&apres<.5?'o':'vague'; o.ouvre=coup>.2?1:.3+.9*monte; o.joues=.9*apres;
  o.g=mel(R.g,{x:-60,y:-66,plie:.2},1); o.d=mel(R.d,mel({x:60,y:-66,plie:-.2},{x:40+8*Math.sin(t*12),y:-200,plie:-1,devant:true},fin),1);
  S.objet('thermo',`<rect x="-9" y="-78" width="18" height="84" rx="9" fill="#fff" stroke="${N}" stroke-width="6"/><rect class="niv" x="-4" y="-40" width="8" height="40" fill="#F0483E"/><circle cx="0" cy="8" r="13" fill="#F0483E" stroke="${N}" stroke-width="6"/>`,P.surTete);
  const niv=S.objets.thermo.querySelector('.niv'); const h=lerp(14,62,ease(seg(t,.3,2.2))); niv.setAttribute('y',-h); niv.setAttribute('height',h);
  S.poser('thermo',-30,58,{rot:-118+3*Math.sin(t*9),op:t<3.38?1:0,sx:.95});
  S.objet('thermoVol',`<rect x="-9" y="-78" width="18" height="84" rx="9" fill="#fff" stroke="${N}" stroke-width="6"/><rect x="-4" y="-62" width="8" height="62" fill="#F0483E"/><circle cx="0" cy="8" r="13" fill="#F0483E" stroke="${N}" stroke-width="6"/>`,true);
  const v=t-3.38; S.poser('thermoVol',o.x-90-420*v,Math.min(SOL-12,SOL-210-260*v+900*v*v),{rot:-118-500*Math.min(v,.85),op:v>0&&t<5.9?1:0});
  S.texte('a',t,.5,2.0,'brrr… 39°',o.x+110,SOL-320,{taille:44,couleur:'#F0483E',tremble:2});
  S.texte('b',t,2.4,3.3,'aaa… aaaa…',o.x+100,SOL-390,{taille:46,couleur:'#9AA3B8',monte:14});
  S.texte('c',t,3.4,4.4,'ATCHOUM !',o.x-470,SOL-340,{taille:76,couleur:'#6EDC8C',tremble:6,grossit:.15});
  S.particules('spray',t,3.4,3.5,{n:14,forme:'goutte',x:o.x-70,y:SOL-210,vx:-420,vy:-20,ex:300,ey:300,g:500,vie:.6,taille:.55,cadence:.006,boucle:false,couleur:'#CFEFFF'});
  S.texte('d',t,4.6,5.8,'snif… pardon',o.x+110,SOL-330,{taille:40,couleur:'#9AA3B8',monte:10});
});

scene('dodo',{titre:'Dodo…',dit:"J'ai sommeil. Je veux faire dodo.",cat:'pasbien',duree:6.6,vignette:4.2},(t,o,S)=>{
  const baille=Math.sin(Math.PI*seg(t,.4,1.8)), etire=fen(t,.9,1.9,.3), dort=ease(seg(t,2.6,3.4))*(1-ease(seg(t,6.2,6.6)));
  const sursaut=Math.sin(Math.PI*seg(t,4.25,4.6)), resp=Math.sin(t*2.6);
  o.angle=3*dort+2*resp*dort; o.tAngle=-10*baille+lerp(0,20,dort)*(1-sursaut)+3*resp*dort-8*sursaut; o.tdy=10*dort*(1-sursaut); o.sy=1+.05*etire-.04*dort+.025*resp*dort; o.saut=-10*sursaut;
  o.lid=k(t,[[0,.25],[.4,.3],[1.0,.9],[1.8,.5],[2.6,.75],[3.2,1]]); o.oeil=(baille>.5||dort>.75&&sursaut<.3)?'ferme':'normal'; if(sursaut>.3){ o.lid=.1; o.grand=1.2; o.pupille=.7; }
  o.py=.3; o.sourcilY=8*baille-4*dort; o.bouche=baille>.15?'o':dort>.5?'o':'petit'; o.ouvre=baille>.15?1.7*baille:.25+.15*resp; o.k=.5; o.joues=.4*dort;
  o.g=mel(mel(R.g,BRAS.hautG,etire),{x:-64,y:-58,plie:.15},dort); o.d=mel(mel(R.d,{x:10,y:-206,plie:-1,devant:true},clamp(baille*1.6)*(1-etire)),mel(BRAS.hautD,{x:64,y:-58,plie:-.15},dort),Math.max(etire,dort));
  S.objet('lune',`<path d="M30,-70 a74,74 0 1 0 40,128 a58,58 0 1 1 -40,-128 Z" fill="#FFE9A8" stroke="${N}" stroke-width="7" stroke-linejoin="round"/>`,false);
  S.poser('lune',930,SOL-360+6*Math.sin(t*1.3),{op:ease(seg(t,2.2,3.2))*(1-ease(seg(t,6.1,6.6))),rot:-12});
  S.particules('etoilesNuit',t,2.6,6.0,{n:7,forme:'etoile',x:600,y:SOL-380,dx:900,dy:200,vx:0,vy:0,ex:0,ey:0,vie:1.2,taille:.8,rot:30,cadence:.4,devant:false});
  S.objet('bonnet',`<path d="M-86,-52 C-70,-140 40,-170 96,-64 Z" fill="#7E8BF2" stroke="${N}" stroke-width="8" stroke-linejoin="round"/><path d="M40,-140 q70,-10 78,60" fill="none" stroke="${N}" stroke-width="30" stroke-linecap="round"/><path d="M40,-140 q70,-10 78,60" fill="none" stroke="#7E8BF2" stroke-width="16" stroke-linecap="round"/><circle cx="120" cy="-74" r="19" fill="#fff" stroke="${N}" stroke-width="7"/><path d="M-90,-50 q90,-34 190,-10" fill="none" stroke="${N}" stroke-width="30" stroke-linecap="round"/><path d="M-90,-50 q90,-34 190,-10" fill="none" stroke="#fff" stroke-width="16" stroke-linecap="round"/>`,P.surTete);
  S.poser('bonnet',0,k(t,[[1.9,-300],[2.4,6,'in'],[2.5,-8,'out'],[2.6,0],[6.2,0],[6.6,-320,'in']]),{op:t>1.9?1:0});
  return ()=>{ const [hx,hy]=P.monde(60,-90,true), [bx,by]=P.monde(-30,60,true);
    S.particules('zzz',t,3.2,6.1,{n:4,forme:'texte',mot:'z',x:hx,y:hy,vx:44,vy:-80,ex:10,ey:10,vie:1.7,taille:1.0,gonfle:.7,cadence:.5,couleur:'#8FA3FF'});
    S.objet('bulle',`<circle r="20" fill="#DDF3FF" stroke="${N}" stroke-width="5" opacity=".9"/><path d="M-9,-8 q5,-6 12,-4" fill="none" stroke="#fff" stroke-width="4" stroke-linecap="round"/>`,true);
    S.poser('bulle',bx-18,by,{op:dort>.9&&sursaut<.05&&t<4.25?1:0,sx:.4+.9*(.5+.5*resp)});
    S.texte('a',t,.5,1.8,'Ouaaah…',o.x+120,SOL-340,{taille:50,couleur:'#8FA3FF',monte:14});
    S.texte('b',t,4.25,4.9,'hein ?!',o.x-280,SOL-360,{taille:44,couleur:'#FFB347',tremble:2});
    S.texte('c',t,5.0,6.0,'ron… pschhh…',o.x-410,SOL-300,{taille:40,couleur:'#8FA3FF',monte:10}); };
});

// ───────────────────────────── LES ACTIONS ─────────────────────────────
scene('miam',{titre:'Miam !',dit:"Miam ! C'est bon !",cat:'actions',duree:6.4,vignette:1.0},(t,o,S)=>{
  const c1=fen(t,1.35,1.75,.12), c2=fen(t,3.0,3.4,.12), mache=Math.max(fen(t,1.75,2.9,.12),fen(t,3.4,4.3,.12)), frotte=fen(t,4.5,6.0,.2), m=Math.sin(t*17);
  const croque=Math.max(c1,c2);
  o.angle=3*Math.sin(t*3)*(1-croque)-6*croque; o.tAngle=-10*croque+5*m*mache+6*Math.sin(t*4)*frotte; o.sy=1-.02*Math.abs(m)*mache; o.tsx=1+.05*Math.abs(m)*mache; o.ventre=6*frotte;
  o.px=k(t,[[0,-.75],[1.3,-.75],[1.8,0]]); o.py=k(t,[[0,.2],[1.3,.2],[1.8,0]]); o.grand=1+.18*(1-seg(t,1.0,1.4)); o.brillant=1-seg(t,1.2,1.5);
  o.oeil=(mache>.4||frotte>.4)?'heureux':'normal'; o.joues=.5+.4*Math.max(mache,frotte); o.sourcilY=8;
  o.bouche=croque>.3?'rire':mache>.3?'vague':frotte>.3?'langue':t<1.3?'langue':'sourire'; o.ouvre=croque>.3?1:.25+.2*Math.sin(t*6); o.k=1.15;
  o.g=mel({x:-124,y:-150,plie:.3,devant:true},{x:-22,y:-206,plie:.95,devant:true},croque); o.d=mel(R.d,{x:14+26*Math.cos(t*7),y:-108+18*Math.sin(t*7),plie:-.7,devant:true},frotte);
  const part=(n)=>`<g><path d="M-52,10 L52,10 L52,-34 Q0,-52 -52,-34 Z" fill="#F7C873" stroke="${N}" stroke-width="7" stroke-linejoin="round"/><path d="M-52,-12 Q0,-28 52,-12" fill="none" stroke="#FF8FC1" stroke-width="9"/><path d="M-52,-34 Q0,-52 52,-34 Q30,-66 0,-58 Q-30,-70 -52,-34 Z" fill="#FFF3F8" stroke="${N}" stroke-width="7" stroke-linejoin="round"/><circle cx="0" cy="-66" r="12" fill="#F0483E" stroke="${N}" stroke-width="5"/>${n>0?`<path d="M60,-76 a26,26 0 0 0 -22,34 a22,22 0 0 0 4,40 l24,12 Z" fill="#F9F1F4" stroke="none"/><path d="M60,-70 a26,26 0 0 0 -22,30 a22,22 0 0 0 4,40" fill="none" stroke="${N}" stroke-width="7" stroke-linecap="round"/>`:''}${n>1?`<path d="M-66,-76 a30,30 0 0 1 30,40 a22,22 0 0 1 -6,40 l-30,12 Z" fill="#F9F1F4" stroke="none"/><path d="M-62,-70 a30,30 0 0 1 28,34 a22,22 0 0 1 -6,40" fill="none" stroke="${N}" stroke-width="7" stroke-linecap="round"/>`:''}</g>`;
  S.objet('g0',part(0),true); S.objet('g1',part(1),true); S.objet('g2',part(2),true);
  return ()=>{ const [mx,my]=P.main('g'); const etat=t<1.55?0:t<3.2?1:2; const vis=t<4.4?1:0;
    ['g0','g1','g2'].forEach((n,i)=>S.poser(n,mx-10,my-26,{op:i===etat?vis:0,rot:-8+4*Math.sin(t*4),sx:.95}));
    S.particules('miettes1',t,1.55,1.6,{n:8,forme:'miette',x:mx+30,y:my-40,vx:0,vy:-140,ex:340,ey:200,g:900,vie:.7,cadence:.005,boucle:false,couleurs:['#F7C873','#FFF3F8','#D99A4E']});
    S.particules('miettes2',t,3.2,3.25,{n:8,forme:'miette',x:mx+30,y:my-40,vx:0,vy:-140,ex:340,ey:200,g:900,vie:.7,cadence:.005,boucle:false,couleurs:['#F7C873','#FFF3F8','#D99A4E']});
    S.texte('a',t,.2,1.2,'Oooh…',o.x+120,SOL-340,{taille:46,couleur:'#FF8FC1',monte:10});
    S.texte('b',t,1.5,2.4,'CROUNCH',o.x-400,SOL-330,{taille:58,couleur:'#D99A4E',tremble:4});
    S.texte('c',t,3.15,4.0,'miam miam',o.x-400,SOL-300,{taille:48,couleur:'#D99A4E',tremble:3});
    S.texte('d',t,4.6,6.0,'Miam ! Trop bon !',o.x-470,SOL-350,{taille:48,couleur:'#FF8FC1',grossit:.15});
    S.particules('coeurs',t,4.6,5.9,{n:4,forme:'coeur',x:o.x,y:SOL-330,dx:300,vx:0,vy:-80,ex:20,ey:20,vie:1.2,taille:.9,cadence:.35}); };
});

scene('soif',{titre:"J'ai soif",dit:"J'ai soif !",cat:'actions',duree:6.4,vignette:2.7},(t,o,S)=>{
  const chaud=1-ease(seg(t,3.6,4.3)), verre=ease(seg(t,1.3,1.8))*(1-ease(seg(t,5.6,6.1))), boit=fen(t,2.0,4.0,.15), ah=fen(t,4.2,5.6,.2), halete=Math.sin(t*9)*(1-seg(t,1.3,1.7));
  o.angle=-3*chaud*(1-boit)+2*Math.sin(t*3)*ah; o.tAngle=-6*chaud*(1-boit)-9*boit+6*ah; o.sy=1-.03*chaud+.03*halete*.5; o.tdy=5*chaud*(1-boit);
  o.teinte=.3*chaud; o.teinteCouleur='#FF9A5A'; o.lid=.35*chaud*(1-boit); o.py=.2*chaud; o.px=-.3*boit; o.sourcilTilt=-9*chaud; o.sourcilY=8*ah;
  o.oeil=ah>.4?'heureux':boit>.5&&Math.sin(t*5)>.6?'ferme':'normal'; o.bouche=boit>.3?'o':ah>.3?'rire':chaud>.5?'langue':'sourire'; o.ouvre=boit>.3?.3:ah>.3?.8:.7+.3*halete; o.joues=.6*ah; o.tsx=1+.03*Math.sin(t*14)*boit;
  o.g=mel(mel(R.g,{x:-66,y:-60,plie:.2},chaud),{x:-70,y:-112,plie:.7,devant:true},verre); o.g=mel(o.g,{x:-46,y:-102,plie:.8,devant:true},boit);
  S.objet('soleil',[0,1,2,3,4,5,6,7].map(i=>`<path d="M0,-84 L0,-112" stroke="${N}" stroke-width="13" stroke-linecap="round" transform="rotate(${i*45})"/><path d="M0,-84 L0,-112" stroke="#FFC83D" stroke-width="6" stroke-linecap="round" transform="rotate(${i*45})"/>`).join('')+`<circle r="64" fill="#FFD84A" stroke="${N}" stroke-width="8"/><path d="M-36,-22 q10,-22 34,-26" fill="none" stroke="#fff" stroke-width="8" stroke-linecap="round" opacity=".7"/>`,false);
  S.poser('soleil',930,SOL-380,{rot:t*20,op:.25+.75*chaud,sx:1+.05*Math.sin(t*4)});
  S.particules('sueur',t,.2,3.4,{n:5,forme:'goutte',x:o.x,y:SOL-340,dx:170,vx:0,vy:40,ex:140,ey:30,g:500,vie:.6,taille:.8,cadence:.2});
  S.objet('verre',`<path d="M-34,-92 L34,-92 L26,0 L-26,0 Z" fill="#EAF8FF" stroke="${N}" stroke-width="7" stroke-linejoin="round" opacity=".95"/><g class="eau"><path d="M-31,-70 L31,-70 L26,-3 L-26,-3 Z" fill="#6FC6FF"/></g><path d="M-22,-80 L-17,-12" stroke="#fff" stroke-width="6" stroke-linecap="round" opacity=".8"/><path d="M14,-10 L40,-150 L10,-166" fill="none" stroke="${N}" stroke-width="13" stroke-linecap="round" stroke-linejoin="round"/><path d="M14,-10 L40,-150 L10,-166" fill="none" stroke="#FF6F91" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/>`,true);
  const niveau=1-ease(seg(t,2.1,4.0)); const eau=S.objets.verre.querySelector('.eau'); eau.setAttribute('transform',`translate(0,-3) scale(1,${Math.max(.02,niveau)}) translate(0,3)`);
  return ()=>{ const [mx,my]=P.main('g'); S.poser('verre',mx+6,my+34,{op:verre>.05?1:0,rot:4,sx:.9*elast(seg(t,1.3,1.8))});
    S.particules('bulles',t,2.1,4.0,{n:5,forme:'rond',x:mx+6,y:my+10,dx:30,vx:0,vy:-50,ex:10,ey:10,vie:.5,taille:.35,cadence:.12,couleur:'#fff'});
    S.texte('a',t,.3,1.4,'pfff… quelle chaleur',o.x-470,SOL-330,{taille:40,couleur:'#FF9A5A',monte:10});
    S.texte('b',t,2.1,3.0,'glou glou',o.x+110,SOL-280,{taille:46,couleur:'#6FC6FF',tremble:2});
    S.texte('c',t,3.0,4.0,'glou glou glou',o.x+110,SOL-330,{taille:46,couleur:'#6FC6FF',tremble:2});
    S.texte('d',t,4.3,5.6,'Aaaah !',o.x-360,SOL-360,{taille:62,couleur:'#5CC8FF',grossit:.2});
    S.particules('frais',t,4.3,5.4,{n:6,forme:'etoile',x:o.x,y:SOL-300,dx:400,dy:240,vx:0,vy:-20,ex:0,ey:0,vie:.6,taille:.9,rot:150,cadence:.16,couleur:'#BFE8FF'}); };
});

scene('musique',{titre:'Musique !',dit:"J'écoute de la musique !",cat:'actions',duree:5.6,vignette:1.9},(t,o,S)=>{
  const joue=fen(t,.9,5.2,.25), bat=t*8.4, b=Math.abs(Math.sin(bat/2)), tour=seg(t,3.2,3.8);
  o.angle=8*Math.sin(bat/2)*joue; o.saut=-20*b*joue; o.sy=1-.05*(1-b)*joue; o.sx=tour>0&&tour<1?Math.cos(tour*Math.PI*2):1; o.tAngle=9*Math.sin(bat)*joue; o.tdy=4*Math.sin(bat)*joue;
  o.oeil=joue>.4?'heureux':'normal'; o.k=1.15; o.bouche=Math.sin(bat/4)>0&&joue>.4?'rire':'sourire'; o.ouvre=.5; o.joues=.6*joue; o.sourcilY=8*joue;
  const alt=Math.sin(bat/2)>0;
  o.g=mel(R.g,alt?{x:-140,y:-250,plie:.2,main:26,pouce:-60}:{x:-100,y:-110,plie:.6},joue); o.d=mel(R.d,alt?{x:100,y:-110,plie:-.6}:{x:140,y:-250,plie:-.2,main:26,pouce:-120},joue);
  S.objet('barres',[0,1,2,3,4,5,6].map(i=>`<rect class="b${i}" x="${-210+i*60}" y="-10" width="40" height="10" rx="10" fill="${COUL[i%6]}" stroke="${N}" stroke-width="6" opacity=".85"/>`).join(''),false);
  S.poser('barres',600,SOL-6,{op:joue});
  for(let i=0;i<7;i++){ const e=S.objets.barres.querySelector('.b'+i); const h=30+150*Math.abs(Math.sin(bat/2+i*1.1))*(.5+.5*rnd(i)); e.setAttribute('y',-h); e.setAttribute('height',h); }
  S.objet('casque',`<path d="M-100,6 C-110,-150 110,-150 100,6" fill="none" stroke="${N}" stroke-width="26" stroke-linecap="round"/><path d="M-100,6 C-110,-150 110,-150 100,6" fill="none" stroke="#7E3FF2" stroke-width="13" stroke-linecap="round"/><rect x="-128" y="-34" width="46" height="84" rx="20" fill="#FF4F7B" stroke="${N}" stroke-width="7"/><rect x="82" y="-34" width="46" height="84" rx="20" fill="#FF4F7B" stroke="${N}" stroke-width="7"/>`,P.surTete);
  S.poser('casque',0,k(t,[[.2,-300],[.7,6,'in'],[.8,-8,'out'],[.9,0],[5.2,0],[5.6,-320,'in']]),{op:t>.2?1:0});
  return ()=>{ const [hx,hy]=P.monde(0,0,true);
    S.particules('notesG',t,1.0,5.1,{n:5,forme:'texte',mot:['♪','♫'],x:hx-130,y:hy,vx:-110,vy:-90,ex:60,ey:60,vie:1.1,taille:1.1,cadence:.24,couleurs:COUL,onde:8,rot:30});
    S.particules('notesD',t,1.1,5.1,{n:5,forme:'texte',mot:['♫','♪'],x:hx+130,y:hy,vx:110,vy:-90,ex:60,ey:60,vie:1.1,taille:1.1,cadence:.24,couleurs:COUL,onde:8,rot:30});
    S.texte('a',t,1.2,2.6,'tchic tchic boum',o.x-470,SOL-390,{taille:44,couleur:'#B58BFF',tremble:2});
    S.texte('b',t,3.9,5.0,'Yeah !',o.x+130,SOL-390,{taille:58,couleur:'#FF4F7B',grossit:.2}); };
});

scene('nesaispas',{titre:'Je ne sais pas',dit:'Je ne sais pas.',cat:'actions',duree:5.4,vignette:3.2},(t,o,S)=>{
  const pense=fen(t,.3,2.3,.25), h1=Math.sin(Math.PI*seg(t,2.6,3.6)), h2=Math.sin(Math.PI*seg(t,3.9,4.9)), h=Math.max(h1,h2);
  o.angle=3*pense+ -3*h; o.tAngle=9*pense+12*h*(h1>h2?1:-1); o.tdy=12*h; o.sy=1+.03*h;
  o.px=-.65*pense; o.py=-.65*pense; o.lid=.1; o.sourcilY=4*pense+14*h; o.sourcilTilt=7*pense; o.sourcilArc=6*h; o.grand=1+.08*h;
  o.bouche=h>.3?'plat':pense>.3?'petit':'sourire'; o.k=h>.3?.8:-.5;
  o.d=mel(R.d,{x:26,y:-196,plie:-1,devant:true,main:20,pouce:-60},pense); o.g=mel(R.g,BRAS.hancheG,pense);
  if(h>.02){ o.g=mel(o.g,{x:-150,y:-190,plie:-.5,main:27,pouce:-90},h); o.d=mel(o.d,{x:150,y:-190,plie:.5,main:27,pouce:-90},h); }
  S.texte('a',t,.5,2.2,'hmmm…',o.x+120,SOL-330,{taille:46,couleur:'#B58BFF',monte:12});
  S.particules('quoi',t,.6,4.8,{n:5,forme:'texte',mot:'?',x:o.x-20,y:SOL-400,dx:300,vx:0,vy:-50,ex:30,ey:10,vie:1.1,taille:1.5,gonfle:.3,cadence:.5,couleurs:['#B58BFF','#5CC8FF','#FFB347'],incline:50});
  S.texte('b',t,2.7,3.7,'Je ne sais pas…',o.x-470,SOL-340,{taille:46,couleur:'#9AA3B8'});
  S.texte('c',t,4.0,4.9,'aucune idée !',o.x+110,SOL-360,{taille:44,couleur:'#9AA3B8'});
});
