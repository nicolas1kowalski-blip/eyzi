// La suite des humeurs : la joie et la tendresse. Mêmes règles que scenes.js.
// Petits outils de dessin : une forme cernée, un rond cerné, un trait cerné ; une cloche sur un intervalle ; un arc de saut.
const T=(d,f,w=7)=>`<path d="${d}" fill="${f}" stroke="${N}" stroke-width="${w}" stroke-linejoin="round" stroke-linecap="round"/>`;
const O=(cx,cy,r,f,w=7)=>`<circle cx="${cx}" cy="${cy}" r="${r}" fill="${f}" stroke="${N}" stroke-width="${w}"/>`;
const L=(d,c,w=8)=>`<path d="${d}" fill="none" stroke="${N}" stroke-width="${w+8}" stroke-linecap="round" stroke-linejoin="round"/><path d="${d}" fill="none" stroke="${c}" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round"/>`;
const cl=(t,a,b)=>Math.sin(Math.PI*seg(t,a,b));
const arc=(u,x0,y0,x1,y1,h)=>[lerp(x0,x1,u),lerp(y0,y1,u)-h*4*u*(1-u)];
const RAYONS=[0,1,2,3,4,5,6,7].map(i=>`<path d="M0,-84 L0,-112" stroke="${N}" stroke-width="13" stroke-linecap="round" transform="rotate(${i*45})"/><path d="M0,-84 L0,-112" stroke="#FFC83D" stroke-width="6" stroke-linecap="round" transform="rotate(${i*45})"/>`).join('');
const DISQUE=`<circle r="64" fill="#FFD84A" stroke="${N}" stroke-width="8"/><path d="M-36,-22 q10,-22 34,-26" fill="none" stroke="#fff" stroke-width="8" stroke-linecap="round" opacity=".7"/>`;
const GROSCOEUR=`<path d="${coeurD(0,0,3)}" fill="url(#gCoeur)" stroke="${N}" stroke-width="3.2" stroke-linejoin="round"/><path d="M-26,-22 q5,-14 19,-16" fill="none" stroke="#fff" stroke-width="4" stroke-linecap="round" opacity=".8"/>`;
const confettis=(S,t,a,b,n=18)=>S.particules('conf',t,a,b,{n,forme:'confetti',x:600,y:SOL-500,dx:860,dy:40,vx:0,vy:210,ex:80,ey:110,g:60,vie:2,rot:300,cadence:.09,couleurs:COUL,onde:12});

// ───────────────────────────── LA JOIE (suite) ─────────────────────────────
scene('super',{titre:'Super !',dit:'Super !',cat:'joie',duree:5.2,vignette:2.0},(t,o,S)=>{
  const plie=fen(t,.55,1.0,.15), pop=ease(seg(t,1.0,1.25))*(1-ease(seg(t,4.6,5.1))), reb=Math.abs(Math.sin((t-1.0)*5.2))*pop;
  o.sy=1-.13*plie+.06*cl(t,1.0,1.35); o.sx=1+.09*plie; o.saut=-70*cl(t,1.0,1.5)-12*reb*seg(t,1.5,1.7);
  o.angle=-5*pop+2*Math.sin(t*5.2)*pop; o.tAngle=7*pop+4*Math.sin(t*5.2)*pop;
  const clin=fen(t,2.5,2.95,.07)+fen(t,3.9,4.3,.07); o.oeil=clin>.5?'heureux':'normal'; o.grand=1+.12*pop; o.brillant=pop; o.px=.15;
  o.bouche=pop>.4?'rire':'sourire'; o.ouvre=.6; o.k=1.15; o.joues=.7*pop; o.sourcilY=10*pop;
  o.d=mel(R.d,{x:150,y:-236+8*Math.sin(t*10.4),plie:-.45,main:31,pouce:-92},pop); o.g=mel(R.g,BRAS.hancheG,pop);
  S.objet('rayons',[...Array(12)].map((_,i)=>`<path d="M0,0 L-24,-236 L24,-236 Z" fill="${i%2?'#FFE38A':'#FFD0E0'}" opacity=".6" transform="rotate(${i*30})"/>`).join(''),false);
  S.poser('rayons',o.x,SOL-236,{rot:t*28,sx:back(seg(t,1.0,1.5))*(1-ease(seg(t,4.5,5.0))),op:t>1?1:0});
  return ()=>{ const [mx,my]=P.main('d'), [ex,ey]=P.monde(40,-40,true);
    S.particules('ting',t,1.25,1.3,{n:1,forme:'etoile',x:mx+6,y:my-46,vx:0,vy:0,ex:0,ey:0,vie:.55,taille:2.4,rot:220,boucle:false,couleur:'#fff'});
    S.particules('ting2',t,2.6,2.65,{n:1,forme:'etoile',x:ex+30,y:ey-10,vx:0,vy:0,ex:0,ey:0,vie:.5,taille:1.8,rot:220,boucle:false,couleur:'#fff'});
    S.texte('a',t,1.25,2.9,'SUPER !',o.x-410,SOL-390,{taille:74,couleur:'#FFB347',grossit:.2,tremble:2});
    S.texte('b',t,3.2,4.5,'Top !',o.x-330,SOL-330,{taille:58,couleur:'#6EDC8C',grossit:.2});
    S.particules('etoiles',t,1.3,4.5,{n:7,forme:'etoile',x:600,y:SOL-280,dx:660,dy:320,vx:0,vy:-20,ex:0,ey:0,vie:.8,taille:1,rot:120,cadence:.3}); };
});

scene('danse',{titre:'Je danse !',dit:'Je danse !',cat:'joie',duree:6.0,vignette:2.1},(t,o,S)=>{
  const joue=fen(t,.8,5.6,.25), bat=t*7.4, ph=Math.sin(bat/2), b=Math.abs(ph), tour=seg(t,3.0,3.7), vague=fen(t,3.9,5.4,.2), disco=fen(t,.9,2.9,.15);
  o.x=600+70*Math.sin(bat/4)*joue; o.angle=9*ph*joue; o.saut=-24*b*joue; o.sy=1-.06*(1-b)*joue; o.sx=tour>0&&tour<1?Math.cos(tour*Math.PI*2):1;
  o.tAngle=-8*ph*joue; o.tdx=8*ph*joue; o.ventre=4*b*joue;
  o.oeil=joue>.4?'heureux':'normal'; o.bouche=joue>.4?'rire':'sourire'; o.ouvre=.55+.25*b; o.k=1.15; o.joues=.7*joue; o.sourcilY=8*joue;
  const haut=ph>0;   // disco : un bras pointe en l'air, l'autre vers le bas, en alternance
  let g=mel(R.g,haut?{x:-70,y:-110,plie:.8}:{x:-150,y:-262,plie:.15,main:26,pouce:-70},disco), d=mel(R.d,haut?{x:150,y:-262,plie:-.15,main:26,pouce:-110}:{x:70,y:-110,plie:-.8},disco);
  g=mel(g,{x:-150,y:-180+50*Math.sin(bat),plie:.5*Math.cos(bat)},vague); d=mel(d,{x:150,y:-180+50*Math.sin(bat-1.6),plie:-.5*Math.cos(bat-1.6)},vague);   // la vague
  if(tour>0&&tour<1){ g={...BRAS.hautG}; d={...BRAS.hautD}; } o.g=g; o.d=d;
  S.objet('boule',`<path d="M0,-300 L0,-66" stroke="${N}" stroke-width="6"/>${O(0,0,66,'#C9D3E8',8)}<path d="M-62,-20 Q0,-36 62,-20 M-62,22 Q0,38 62,22 M-22,-62 Q-38,0 -22,62 M22,-62 Q38,0 22,62" fill="none" stroke="#8E9BB8" stroke-width="4"/><rect x="-44" y="-48" width="20" height="20" rx="3" fill="#fff" opacity=".9"/><rect x="6" y="-12" width="18" height="18" rx="3" fill="#fff" opacity=".7"/><rect x="-36" y="14" width="16" height="16" rx="3" fill="#FFD0E0" opacity=".9"/><rect x="26" y="-44" width="16" height="16" rx="3" fill="#BFE8FF" opacity=".9"/>`,false);
  S.poser('boule',940,k(t,[[0,-120],[.8,SOL-340,'back'],[5.5,SOL-340],[6.0,-120,'in']]),{rot:5*Math.sin(t*3)});
  S.particules('spots',t,.9,5.5,{n:12,forme:'rond',x:600,y:SOL-250,dx:1040,dy:420,vx:0,vy:0,ex:0,ey:0,vie:.5,taille:1.7,et:.8,cadence:.11,couleurs:COUL,devant:false,op:.5});
  return ()=>{ const [hx,hy]=P.monde(0,0,true);
    S.particules('notesG',t,1.0,5.4,{n:5,forme:'texte',mot:['♪','♫'],x:hx-130,y:hy,vx:-110,vy:-90,ex:60,ey:60,vie:1.1,taille:1.1,cadence:.24,couleurs:COUL,onde:8,rot:30});
    S.particules('notesD',t,1.1,5.4,{n:5,forme:'texte',mot:['♫','♪'],x:hx+130,y:hy,vx:110,vy:-90,ex:60,ey:60,vie:1.1,taille:1.1,cadence:.24,couleurs:COUL,onde:8,rot:30});
    S.texte('a',t,1.0,2.6,'Je danse !',130,SOL-400,{taille:58,couleur:'#B58BFF',tremble:2,grossit:.15});
    S.texte('b',t,3.0,3.8,'Et hop !',150,SOL-330,{taille:52,couleur:'#FFB347',grossit:.2});
    S.texte('c',t,4.1,5.3,'Yeah !',160,SOL-400,{taille:60,couleur:'#FF4F7B',grossit:.2,tremble:2}); };
});

scene('chante',{titre:'Je chante !',dit:'La la la ! Je chante !',cat:'joie',duree:5.8,vignette:3.9},(t,o,S)=>{
  const micro=ease(seg(t,.4,.9))*(1-ease(seg(t,5.3,5.7))), chante=fen(t,1.0,3.3,.15), final=fen(t,3.4,5.0,.15), v=Math.sin(t*9), bal=Math.sin(t*3.2);
  o.x=600+26*bal*chante; o.angle=6*bal*chante-7*final; o.tAngle=7*bal*chante-16*final; o.sy=1+.05*final-.02*Math.abs(v)*chante; o.saut=-8*Math.abs(Math.sin(t*6.4))*chante;
  o.oeil=final>.4?'ferme':(chante>.4&&bal>.3?'heureux':'normal'); o.sourcilY=6*chante+14*final; o.sourcilTilt=-6*final;
  o.bouche=final>.3?'rire':chante>.3?(v>0?'o':'rire'):'sourire'; o.ouvre=final>.3?1:.5+.5*Math.abs(v); o.joues=.6*Math.max(chante,final); o.k=1.1; o.tsy=1+.04*final;
  o.d=mel(R.d,{x:58,y:-146,plie:-1,devant:true,main:25},micro); o.g=mel(mel(R.g,{x:-120+20*bal,y:-130,plie:.4},chante),{x:-150,y:-250,plie:.2,main:27},final);
  S.objet('micro',`${L('M0,6 L0,74','#5B5F73',12)}${O(0,-16,27,'#C9D3E8')}<path d="M-22,-24 Q0,-34 22,-24 M-26,-12 Q0,-22 26,-12 M-22,0 Q0,-10 22,0 M-12,-40 Q-18,-16 -12,6 M12,-40 Q18,-16 12,6" fill="none" stroke="#8E9BB8" stroke-width="3.5"/><rect x="-13" y="8" width="26" height="12" rx="4" fill="#FF4F7B" stroke="${N}" stroke-width="5"/>`,true);
  return ()=>{ const [mx,my]=P.main('d'), [bx,by]=P.monde(-6,46,true);
    S.poser('micro',mx-2,my-36,{rot:-12+4*bal,op:micro>.05?1:0,sx:elast(seg(t,.4,.95))});
    S.particules('notes',t,1.0,5.0,{n:9,forme:'texte',mot:['♪','♫'],x:bx-40,y:by-20,vx:-150,vy:-130,ex:140,ey:80,vie:1.3,taille:1.2,gonfle:.3,cadence:.19,couleurs:COUL,onde:10,rot:30});
    S.particules('notes2',t,3.5,5.0,{n:6,forme:'texte',mot:['♫','♪'],x:bx+60,y:by-60,vx:150,vy:-150,ex:120,ey:80,vie:1.2,taille:1.3,gonfle:.3,cadence:.22,couleurs:COUL,onde:10,rot:30});
    S.texte('a',t,1.1,2.2,'La la la…',o.x-400,SOL-330,{taille:50,couleur:'#5CC8FF',monte:14});
    S.texte('b',t,2.2,3.3,'la la laaa !',o.x-440,SOL-390,{taille:50,couleur:'#B58BFF',monte:14});
    S.texte('c',t,3.5,5.0,'LAAAAA !',o.x+110,SOL-400,{taille:66,couleur:'#FF4F7B',tremble:4,grossit:.2});
    S.particules('etoiles',t,3.5,5.0,{n:6,forme:'etoile',x:600,y:SOL-300,dx:640,dy:300,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:1,rot:120,cadence:.22}); };
});

scene('fete',{titre:"C'est la fête !",dit:"C'est la fête !",cat:'joie',duree:6.0,vignette:2.2,cadre:[295,0,600]},(t,o,S)=>{
  const fete=fen(t,1.0,5.6,.25), s1=fen(t,1.5,2.7,.12), s2=fen(t,3.6,4.6,.12), souffle=Math.max(s1,s2), b=Math.abs(Math.sin(t*6));
  o.saut=-18*b*fete*(1-souffle); o.angle=5*Math.sin(t*3)*fete; o.tAngle=-6*souffle+5*Math.sin(t*6)*fete*(1-souffle); o.sy=1-.04*(1-b)*fete; o.tsx=1+.05*souffle;
  o.oeil=fete>.4&&souffle<.3?'heureux':'normal'; o.grand=1+.1*souffle; o.px=-.3*souffle; o.joues=.5*fete+.5*souffle; o.sourcilY=8*fete;
  o.bouche=souffle>.3?'o':fete>.3?'rire':'sourire'; o.ouvre=souffle>.3?.35:.7; o.k=1.15;
  o.g=mel(mel(R.g,{x:-136,y:-232+14*Math.sin(t*12),plie:.3,main:25},fete),{x:-40,y:-196,plie:.95,devant:true},souffle); o.d=mel(R.d,{x:136,y:-232+14*Math.sin(t*12+2),plie:-.3,main:25},fete);
  S.objet('chapeau',`${T('M-60,-66 L10,-214 L64,-62 Q0,-86 -60,-66 Z','#5CC8FF',8)}<path d="M-38,-112 Q4,-124 44,-112 M-16,-158 Q8,-166 28,-158" fill="none" stroke="#FFE066" stroke-width="12" stroke-linecap="round"/>${O(10,-216,16,'#FF4F7B',7)}`,P.surTete);
  S.poser('chapeau',0,k(t,[[.3,-300],[.8,6,'in'],[.9,-8,'out'],[1.0,0],[5.6,0],[6.0,-320,'in']]),{op:t>.3?1:0,rot:-8});
  const ballon=(c)=>`<path d="M0,56 q-14,60 4,150" fill="none" stroke="${N}" stroke-width="4"/><ellipse rx="46" ry="56" fill="${c}" stroke="${N}" stroke-width="7"/><path d="M-8,54 l8,14 l8,-14 Z" fill="${c}" stroke="${N}" stroke-width="5" stroke-linejoin="round"/><path d="M-24,-26 q6,-16 22,-20" fill="none" stroke="#fff" stroke-width="7" stroke-linecap="round" opacity=".75"/>`;
  [['#FF4F7B',-140,150,SOL-410],['#6EDC8C',-200,256,SOL-330],['#B58BFF',1420,1040,SOL-410],['#FFB347',1480,1136,SOL-320]].forEach((bl,i)=>{ S.objet('bal'+i,ballon(bl[0]),false);
    S.poser('bal'+i,lerp(bl[1],bl[2],out(seg(t,.6+i*.15,1.9+i*.15)))+10*Math.sin(t*1.7+i),bl[3]+12*Math.sin(t*2.1+i*2),{rot:5*Math.sin(t*1.9+i),op:1-seg(t,5.6,6.0)}); });
  S.objet('sansgene',`<path d="M-20,-13 L-168,-17 a17,17 0 1 0 0,34 L-20,13 Z" fill="#FF4F7B" stroke="${N}" stroke-width="6" stroke-linejoin="round"/><path d="M-60,-15 L-60,15 M-100,-16 L-100,16 M-140,-17 L-140,17" stroke="#FFE066" stroke-width="10"/><circle cx="-170" cy="0" r="7" fill="${N}"/><rect x="-24" y="-13" width="30" height="26" rx="6" fill="#FFE066" stroke="${N}" stroke-width="6"/>`,true);
  return ()=>{ const [bx,by]=P.monde(-12,52,true); const lon=Math.max(cl(t,1.5,2.7),cl(t,3.6,4.6));
    S.poser('sansgene',bx,by,{sx:.22+.78*Math.pow(lon,.5),sy:1,rot:-5+3*Math.sin(t*30)*lon,op:souffle>.08?1:0});
    S.texte('a',t,1.6,2.7,'Pouêêt !',o.x-410,SOL-170,{taille:50,couleur:'#FF4F7B',tremble:4});
    S.texte('b',t,2.8,3.9,"C'est la fête !",o.x+130,SOL-170,{taille:40,couleur:'#B58BFF',grossit:.1});
    S.texte('c',t,3.7,4.6,'Pouêt pouêt !',o.x-450,SOL-120,{taille:40,couleur:'#FFB347',tremble:4});
    confettis(S,t,1.0,5.0,24); };
});

scene('fier',{titre:'Je suis fier',dit:'Je suis fier de moi !',cat:'joie',duree:5.6,vignette:2.6},(t,o,S)=>{
  const fier=ease(seg(t,1.2,1.8))*(1-ease(seg(t,5.1,5.5))), frotte=fen(t,3.4,4.6,.2), resp=Math.sin(t*3);
  o.sy=1+.05*fier+.012*resp*fier-.05*cl(t,1.05,1.3); o.ventre=9*fier+2*resp*fier; o.angle=-3*fier; o.tAngle=-12*fier+10*frotte; o.tdy=-4*fier;
  o.px=k(t,[[0,0],[.4,0],[.7,-.2],[1.2,-.1],[1.8,.2]]); o.py=k(t,[[0,0],[.4,-.7],[1.0,.8],[1.5,.7],[1.9,0],[3.4,0],[3.6,.8],[4.5,.8],[4.8,0]]);
  o.oeil=fier>.6&&t<3.3?'ferme':'normal'; o.sourcilY=10*fier; o.bouche='sourire'; o.k=.9+.45*fier; o.joues=.6*fier; o.grand=1+.12*cl(t,.5,1.2);
  o.g=mel(R.g,BRAS.hancheG,fier); o.d=mel(mel(R.d,BRAS.hancheD,fier),{x:30+10*Math.sin(t*16),y:-136+8*Math.cos(t*16),plie:-.9,devant:true},frotte);
  S.objet('rayons',[...Array(12)].map((_,i)=>`<path d="M0,0 L-24,-236 L24,-236 Z" fill="${i%2?'#FFE9A8':'#FFF3C4'}" opacity=".6" transform="rotate(${i*30})"/>`).join(''),false);
  S.poser('rayons',o.x,SOL-236,{rot:t*14,sx:ease(seg(t,1.3,2.0))*(1-ease(seg(t,5.0,5.5))),op:t>1.3?1:0});
  S.objet('medaille',`${T('M-34,-204 L-12,-204 L8,-140 L-8,-134 Z','#FF4F7B',6)}${T('M34,-204 L12,-204 L-8,-140 L8,-134 Z','#5CC8FF',6)}${O(0,-118,28,'#FFC83D',7)}<circle cx="0" cy="-118" r="19" fill="none" stroke="#F0A81C" stroke-width="4"/><path d="M0,-132 l4,9 l10,1 l-7,7 l2,10 l-9,-5 l-9,5 l2,-10 l-7,-7 l10,-1 Z" fill="#fff" stroke="${N}" stroke-width="3" stroke-linejoin="round"/>`,P.surCorps);
  S.poser('medaille',0,k(t,[[.4,-460],[1.1,0,'in'],[1.2,-14,'out'],[1.3,0]]),{op:t>.4?1:0});
  return ()=>{ const [mx,my]=P.monde(0,-118);
    S.particules('ting',t,1.3,1.35,{n:1,forme:'etoile',x:mx+16,y:my-14,vx:0,vy:0,ex:0,ey:0,vie:.55,taille:2,rot:220,boucle:false,couleur:'#fff'});
    S.particules('ting2',t,4.5,4.55,{n:1,forme:'etoile',x:mx+16,y:my-14,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:2.4,rot:220,boucle:false,couleur:'#fff'});
    S.texte('a',t,1.9,3.3,'Je suis fier !',o.x-480,SOL-380,{taille:50,couleur:'#FFB347',grossit:.12});
    S.texte('b',t,3.6,4.6,'hé hé !',o.x+130,SOL-360,{taille:48,couleur:'#5CC8FF',monte:12});
    S.particules('etoiles',t,1.8,5.0,{n:6,forme:'etoile',x:600,y:SOL-280,dx:640,dy:320,vx:0,vy:-16,ex:0,ey:0,vie:.9,taille:.9,rot:90,cadence:.4}); };
});

scene('gagne',{titre:"J'ai gagné !",dit:"J'ai gagné !",cat:'joie',duree:6.0,vignette:3.0,cadre:[295,0,600]},(t,o,S)=>{
  const course=seg(t,0,.9), bond=seg(t,.9,1.45), leve=ease(seg(t,1.7,2.2))*(1-ease(seg(t,5.5,5.9))), b=Math.abs(Math.sin((t-2.2)*5.5))*fen(t,2.3,5.4,.2);
  o.x=lerp(170,430,course)+lerp(0,170,ease(bond)); o.sol=SOL-30*ease(bond);
  o.saut=-30*Math.abs(Math.sin(course*Math.PI*3))*(course<1?1:0)-150*cl(t,.9,1.45)-14*b;
  o.angle=k(t,[[0,10],[.9,10],[1.45,0]])+3*Math.sin(t*5.5)*leve; o.sy=1-.1*cl(t,1.45,1.7)+.03*b; o.sx=1+.08*cl(t,1.45,1.7);
  o.oeil=leve>.5?'heureux':'normal'; o.grand=1.1; o.px=.4*(1-bond); o.bouche=leve>.3?'rire':'sourire'; o.ouvre=.7+.2*b; o.joues=.8*leve; o.sourcilY=10*leve; o.k=1.15; o.tAngle=4*Math.sin(t*5.5)*leve;
  o.g=mel(R.g,{x:-60,y:-360,plie:.9,main:25},leve); o.d=mel(R.d,{x:60,y:-360,plie:-.9,main:25},leve);
  S.objet('podium',`${T('M-260,0 L-260,-20 L-130,-20 L-130,0 Z','#C9D3E8')}${T('M130,0 L130,-13 L260,-13 L260,0 Z','#E3B98A')}${T('M-130,0 L-130,-30 L130,-30 L130,0 Z','#FFD84A')}<text x="0" y="-5" font-size="26" font-weight="900" font-family="Arial Black,Arial" fill="${N}" text-anchor="middle">1</text><text x="-195" y="-2" font-size="18" font-weight="900" font-family="Arial Black,Arial" fill="${N}" text-anchor="middle">2</text><text x="195" y="0" font-size="13" font-weight="900" font-family="Arial Black,Arial" fill="${N}" text-anchor="middle">3</text>`,false);
  S.poser('podium',600,SOL,{});
  S.objet('coupe',`${T('M-30,0 L30,0 L24,-14 L-24,-14 Z','#B9783F',6)}${T('M-8,-14 L8,-14 L6,-34 L-6,-34 Z','#F0A81C',6)}<path d="M-34,-84 q-30,2 -22,26 q6,16 26,14" fill="none" stroke="${N}" stroke-width="15" stroke-linecap="round"/><path d="M-34,-84 q-30,2 -22,26 q6,16 26,14" fill="none" stroke="#FFC83D" stroke-width="6" stroke-linecap="round"/><path d="M34,-84 q30,2 22,26 q-6,16 -26,14" fill="none" stroke="${N}" stroke-width="15" stroke-linecap="round"/><path d="M34,-84 q30,2 22,26 q-6,16 -26,14" fill="none" stroke="#FFC83D" stroke-width="6" stroke-linecap="round"/>${T('M-40,-96 L40,-96 Q40,-40 0,-32 Q-40,-40 -40,-96 Z','#FFD84A',7)}<path d="M-24,-84 q0,26 14,38" fill="none" stroke="#fff" stroke-width="7" stroke-linecap="round" opacity=".75"/>`,true);
  return ()=>{ const [cx,cy]=P.monde(0,-356);
    S.poser('coupe',cx,cy,{op:t>1.7?1:0,sx:.7*elast(seg(t,1.7,2.3)),rot:4*Math.sin(t*5.5)});
    S.particules('ting',t,2.25,2.3,{n:1,forme:'etoile',x:cx+24,y:cy-52,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:2,rot:220,boucle:false,couleur:'#fff'});
    S.particules('pas',t,.05,.9,{n:5,forme:'rond',x:o.x-40,y:SOL-8,vx:-60,vy:-30,ex:20,ey:20,vie:.5,taille:.8,gonfle:1,cadence:.18,devant:false,couleur:'#EFE6FA',op:.8});
    S.texte('a',t,2.2,3.7,'GAGNÉ !',110,SOL-330,{taille:66,couleur:'#FFB347',grossit:.15,tremble:2});
    S.texte('b',t,3.9,5.3,'Youhou !',770,SOL-350,{taille:54,couleur:'#6EDC8C',grossit:.2});
    confettis(S,t,2.2,5.0,22);
    S.particules('etoiles',t,2.2,5.2,{n:6,forme:'etoile',x:600,y:SOL-300,dx:700,dy:300,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:1,rot:120,cadence:.25}); };
});

scene('surprise',{titre:'Surprise !',dit:'Oh ! Une surprise !',cat:'joie',duree:6.0,vignette:2.9,cadre:[300,10,620],x:470},(t,o,S)=>{
  const voit=ease(seg(t,.5,.9)), tire=fen(t,1.4,2.2,.15), joie=fen(t,2.5,5.6,.2), b=Math.abs(Math.sin((t-2.5)*5.6))*joie, v=t-2.2;
  o.angle=6*voit*(1-joie)+8*tire-10*cl(t,2.2,2.6); o.saut=-80*cl(t,2.2,2.7)-20*b*seg(t,2.8,3.0); o.tAngle=6*voit*(1-joie)+4*Math.sin(t*5.6)*joie; o.sy=1+.06*cl(t,2.2,2.5)-.03*tire;
  o.px=k(t,[[0,0],[.5,0],[.9,.85],[2.2,.85],[2.5,.5],[3.6,.5],[4.0,0]]); o.py=k(t,[[0,0],[.5,0],[.9,.35],[2.2,.35],[2.45,-.6],[3.6,-.6],[4.0,0]]);
  o.grand=1+.12*voit+.25*cl(t,2.2,3.0); o.pupille=1-.3*cl(t,2.2,2.8); o.brillant=joie; o.sourcilY=8*voit+10*joie;
  o.oeil=joie>.5&&t>3.9?'heureux':'normal'; o.bouche=t>2.2&&t<3.2?'o':joie>.3?'rire':voit>.5?'petit':'sourire'; o.ouvre=t<3.2?.9:.7; o.k=1.1; o.joues=.7*joie;
  o.d=mel(R.d,{x:150+10*Math.sin(t*30),y:-150,plie:-.3},tire);
  if(joie>.02){ o.g=mel(R.g,{x:-132,y:-236+12*Math.sin(t*11),plie:.3,main:26},joie); o.d=mel(o.d,{x:132,y:-236+12*Math.sin(t*11+2),plie:-.3,main:26},joie); }
  S.objet('ballonS',`<path d="M0,56 q-14,60 4,170" fill="none" stroke="${N}" stroke-width="4"/><ellipse rx="46" ry="56" fill="#FF4F7B" stroke="${N}" stroke-width="7"/><path d="M-8,54 l8,14 l8,-14 Z" fill="#FF4F7B" stroke="${N}" stroke-width="5" stroke-linejoin="round"/><path d="M-24,-26 q6,-16 22,-20" fill="none" stroke="#fff" stroke-width="7" stroke-linecap="round" opacity=".75"/>`,true);
  S.poser('ballonS',800+12*Math.sin(t*1.8),lerp(SOL-90,SOL-400,out(seg(t,2.25,3.6)))+8*Math.sin(t*2.4),{op:t>2.25?1:0,sx:.4+.6*out(seg(t,2.25,3.0)),rot:5*Math.sin(t*2)});
  S.objet('boite',`${T('M-78,0 L-78,-112 L78,-112 L78,0 Z','#B58BFF',8)}<rect x="-15" y="-112" width="30" height="112" fill="#FFE066" stroke="${N}" stroke-width="6"/><path d="M-58,-92 L-58,-20" stroke="#fff" stroke-width="8" stroke-linecap="round" opacity=".5"/>`,true);
  S.poser('boite',800+3*Math.sin(t*34)*tire,SOL,{sx:1+.03*Math.sin(t*34)*tire,sy:1+.08*cl(t,2.2,2.5)});
  S.objet('couvercle',`${T('M0,-40 C-56,-96 -76,-30 0,-40 C56,-96 76,-30 0,-40 Z','#FFE066',7)}${T('M-90,0 L-90,-40 L90,-40 L90,0 Z','#C9A8FF',8)}<rect x="-15" y="-40" width="30" height="40" fill="#FFE066" stroke="${N}" stroke-width="6"/>`,true);
  const vv=clamp(v,0,.822); S.poser('couvercle',800+(v>0?300*vv:0)+3*Math.sin(t*34)*tire,Math.min(SOL,SOL-112-1100*vv+1500*vv*vv)-10*cl(t,1.5,2.2)*Math.abs(Math.sin(t*20)),{rot:v>0?360*vv/.822:4*Math.sin(t*34)*tire});
  S.particules('jet',t,2.2,4.4,{n:14,forme:'etoile',x:800,y:SOL-120,dx:80,vx:0,vy:-640,ex:560,ey:200,g:1000,vie:1.1,taille:1,rot:260,cadence:.09});
  S.particules('jetC',t,2.25,4.4,{n:8,forme:'coeur',x:800,y:SOL-120,dx:80,vx:0,vy:-560,ex:460,ey:200,g:800,vie:1.2,taille:.9,cadence:.16,incline:50});
  S.particules('jetF',t,2.2,4.4,{n:16,forme:'confetti',x:800,y:SOL-120,dx:80,vx:0,vy:-700,ex:700,ey:260,g:1100,vie:1.2,taille:.9,rot:400,cadence:.07,couleurs:COUL});
  S.texte('a',t,.7,1.5,'Oh ?',o.x-250,SOL-360,{taille:54,couleur:'#B58BFF',monte:12});
  S.texte('b',t,2.2,2.9,'POP !',880,SOL-250,{taille:58,couleur:'#FFB347',tremble:4,grossit:.3});
  S.texte('c',t,3.0,4.8,'SURPRISE !',90,SOL-410,{taille:62,couleur:'#FF4F7B',grossit:.12,tremble:2});
});

// ───────────────────────────── TENDRE (suite) ─────────────────────────────
scene('bisou',{titre:'Bisou !',dit:"Je t'envoie un bisou !",cat:'tendre',duree:5.6,vignette:2.1},(t,o,S)=>{
  const m1=fen(t,.5,1.45,.15), s1=fen(t,1.5,2.7,.15), m2=fen(t,2.9,3.65,.15), s2=fen(t,3.7,5.0,.15), smack=Math.max(cl(t,1.2,1.5),cl(t,3.4,3.7));
  o.angle=-4*m1+7*s1+4*m2-7*s2; o.tAngle=-6*(m1+m2)+8*s1-8*s2; o.sy=1-.03*(m1+m2)+.04*(s1+s2); o.tsx=1-.05*smack;
  o.oeil=(m1+m2)>.5?'ferme':(s1+s2)>.5?'heureux':'normal'; o.bouche=(m1+m2)>.3||fen(t,1.45,1.9,.08)>.5||fen(t,3.65,4.1,.08)>.5?'bisou':'sourire'; o.k=1.2; o.joues=.5+.5*Math.max(m1,m2,s1,s2); o.sourcilY=6*(s1+s2);
  let d=mel(R.d,{x:12,y:-212,plie:-1,devant:true,main:24},ease(seg(t,.5,.8))); d=mel(d,{x:162,y:-226,plie:-.2,main:28,pouce:-150},ease(seg(t,1.45,1.7))); o.d=mel(d,R.d,ease(seg(t,2.6,2.9)));
  let g=mel(R.g,{x:-20,y:-212,plie:1,devant:true,main:24},ease(seg(t,2.9,3.2))); g=mel(g,{x:-162,y:-226,plie:.2,main:28,pouce:-30},ease(seg(t,3.65,3.9))); o.g=mel(g,R.g,ease(seg(t,4.9,5.3)));
  S.objet('gros',GROSCOEUR,true);
  S.poser('gros',o.x-250,SOL-330-40*seg(t,3.9,5.2),{sx:lerp(.3,2.6,out(seg(t,3.9,5.1))),op:t>3.9?1-seg(t,4.8,5.3):0,rot:-8+6*Math.sin(t*6)});
  return ()=>{ const [dx,dy]=P.main('d'), [gx,gy]=P.main('g');
    S.particules('c1',t,1.6,2.6,{n:6,forme:'coeur',x:dx+20,y:dy-20,vx:190,vy:-130,ex:80,ey:90,vie:1.2,taille:.8,gonfle:.9,cadence:.15,onde:8,incline:40});
    S.particules('c2',t,3.8,4.7,{n:5,forme:'coeur',x:gx-20,y:gy-20,vx:-190,vy:-130,ex:80,ey:90,vie:1.2,taille:.8,gonfle:.9,cadence:.16,onde:8,incline:40});
    S.texte('a',t,1.25,2.2,'Smack !',o.x-340,SOL-340,{taille:58,couleur:'#FF8FC1',grossit:.2});
    S.texte('b',t,3.45,4.5,'SMACK !',o.x+120,SOL-370,{taille:60,couleur:'#FF4F7B',grossit:.2,tremble:2});
    S.particules('etoiles',t,1.5,5.0,{n:5,forme:'etoile',x:600,y:SOL-300,dx:640,dy:300,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:.8,rot:120,cadence:.35,couleur:'#FFD0E0'}); };
});

const OURS=`${O(-34,-128,17,'#B9783F')}${O(34,-128,17,'#B9783F')}${O(-50,-62,15,'#B9783F')}${O(50,-62,15,'#B9783F')}<ellipse cx="0" cy="-48" rx="46" ry="50" fill="#C98A4B" stroke="${N}" stroke-width="7"/><ellipse cx="0" cy="-40" rx="24" ry="28" fill="#E9C08E"/>${O(-42,-8,17,'#B9783F')}${O(42,-8,17,'#B9783F')}${O(0,-108,40,'#C98A4B')}<ellipse cx="0" cy="-98" rx="17" ry="13" fill="#E9C08E"/><circle cx="-14" cy="-116" r="5" fill="${N}"/><circle cx="14" cy="-116" r="5" fill="${N}"/><ellipse cx="0" cy="-103" rx="6" ry="4.5" fill="${N}"/><path d="M-7,-94 q7,6 14,0" fill="none" stroke="${N}" stroke-width="3.5" stroke-linecap="round"/>`;
scene('calin',{titre:'Câlin',dit:'Je veux un câlin.',cat:'tendre',duree:6.2,vignette:3.7,x:520},(t,o,S)=>{
  const ouvre=ease(seg(t,.5,1.0)), saute=seg(t,2.0,2.5), serre=ease(seg(t,2.45,2.8)), berce=Math.sin((t-2.8)*3.4)*serre;
  o.x=520+14*berce; o.angle=6*berce-4*ouvre*(1-serre); o.tAngle=10*serre+5*berce; o.tdy=6*serre; o.sy=1-.03*serre+.05*cl(t,2.4,2.7); o.ventre=4*serre;
  o.px=.6*ouvre*(1-serre); o.grand=1+.18*ouvre*(1-serre); o.brillant=ouvre; o.oeil=serre>.6?'ferme':'normal'; o.sourcilTilt=-8*ouvre*(1-serre); o.sourcilY=6*ouvre;
  o.bouche='sourire'; o.k=1+.3*serre; o.joues=.4*ouvre+.5*serre;
  o.g=mel(mel(R.g,{x:-150,y:-170,plie:.15,main:26},ouvre),{x:50,y:-132,plie:1,devant:true},serre); o.d=mel(mel(R.d,{x:150,y:-170,plie:-.15,main:26},ouvre),{x:-50,y:-92,plie:-1,devant:true},serre);
  S.objet('oursBras',OURS,P.surCorps); S.poser('oursBras',6,-34,{op:t>=2.45?1:0,sx:1,rot:-6+4*berce});
  S.objet('oursSol',OURS,true);
  const marche=seg(t,.9,2.0), [jx,jy]=arc(saute,790,SOL,534,SOL-40,170);
  S.poser('oursSol',saute>0?jx:lerp(1240,790,ease(marche)),saute>0?jy:SOL-26*Math.abs(Math.sin(marche*Math.PI*4)),{op:t<2.45?1:0,sx:1.05,rot:saute>0?-20*saute:6*Math.sin(marche*Math.PI*4)});
  S.texte('a',t,.6,1.9,'Un câlin ?',o.x-420,SOL-360,{taille:52,couleur:'#FF8FC1',monte:12});
  S.texte('b',t,3.0,4.4,'Mmmmh…',o.x+140,SOL-340,{taille:50,couleur:'#FF8FC1',monte:14});
  S.texte('c',t,4.6,5.8,'tout doux…',o.x+130,SOL-300,{taille:42,couleur:'#B58BFF',monte:10});
  S.particules('coeurs',t,2.8,6.0,{n:7,forme:'coeur',x:520,y:SOL-330,dx:380,dy:100,vx:0,vy:-90,ex:20,ey:30,vie:1.5,taille:1,cadence:.4,onde:10,incline:40});
});

scene('aurevoir',{titre:'Au revoir !',dit:'Au revoir ! À bientôt !',cat:'tendre',duree:5.6,vignette:1.4,x:560},(t,o,S)=>{
  const salue=fen(t,.4,2.7,.2), part=seg(t,2.9,5.5), enRoute=part>0?1:0, pas=Math.abs(Math.sin(part*Math.PI*6)), retourne=fen(t,3.5,4.7,.15);
  o.x=560+900*(part*part*.55+part*.45); o.saut=-12*Math.abs(Math.sin(t*6.5))*salue-34*pas*enRoute; o.angle=3*Math.sin(t*6.5)*salue+8*ease(seg(t,2.9,3.2));
  o.sy=1-.05*(1-clamp(pas*3))*enRoute; o.tAngle=6*Math.sin(t*6.5)*salue-12*retourne; o.px=.2*salue+.6*enRoute-1.5*retourne;
  o.grand=1.08; o.brillant=salue; o.oeil=fen(t,1.9,2.3,.07)>.5?'heureux':'normal'; o.bouche=salue>.4||retourne>.4?'rire':'sourire'; o.ouvre=.5; o.k=1.15; o.joues=.6; o.sourcilY=8*salue;
  o.d=mel(R.d,{x:138+34*Math.sin(t*12),y:-270+8*Math.cos(t*12),plie:-.35,main:28,pouce:-150},Math.max(salue,retourne)); o.g={x:-80,y:-62,plie:.12};
  S.objet('valise',`<path d="M-16,8 v-12 a8,8 0 0 1 8,-8 h16 a8,8 0 0 1 8,8 v12" fill="none" stroke="${N}" stroke-width="7"/>${T('M-46,8 h92 a8,8 0 0 1 8,8 v38 a8,8 0 0 1 -8,8 h-92 a8,8 0 0 1 -8,-8 v-38 a8,8 0 0 1 8,-8 Z','#F0483E',7)}<path d="M-26,10 v50 M26,10 v50" stroke="#FFC83D" stroke-width="8"/>${T('M-6,26 l4,8 l9,1 l-7,6 l2,9 l-8,-5 l-8,5 l2,-9 l-7,-6 l9,-1 Z','#fff',3)}`,true);
  return ()=>{ const [gx,gy]=P.main('g'); S.poser('valise',gx,gy-4,{rot:6*Math.sin(t*6.5)*salue+8*Math.sin(part*Math.PI*6)*enRoute});
    S.texte('a',t,.6,2.5,'Au revoir !',120,SOL-390,{taille:60,couleur:'#5CC8FF',grossit:.15});
    S.texte('b',t,3.6,5.0,'À bientôt !',170,SOL-330,{taille:54,couleur:'#B58BFF',monte:14});
    S.particules('pas',t,3.0,5.4,{n:6,forme:'rond',x:o.x-50,y:SOL-8,vx:-60,vy:-30,ex:20,ey:20,vie:.5,taille:.8,gonfle:1,cadence:.2,devant:false,couleur:'#EFE6FA',op:.8});
    S.particules('coeurs',t,3.6,5.2,{n:4,forme:'coeur',x:o.x-110,y:SOL-300,vx:-70,vy:-70,ex:40,ey:30,vie:1.2,taille:.9,cadence:.36,onde:8,incline:40}); };
});

scene('silteplait',{titre:"S'il te plaît",dit:"S'il te plaît !",cat:'tendre',duree:5.4,vignette:2.6},(t,o,S)=>{
  const prie=ease(seg(t,.4,.9))*(1-ease(seg(t,5.0,5.35))), insiste=fen(t,2.9,4.8,.2), g2=Math.abs(Math.sin(t*7))*insiste;
  o.angle=-7*prie; o.tAngle=10*prie+3*Math.sin(t*2.5)*prie; o.sy=1-.05*prie-.04*g2; o.tdy=4*prie;
  o.grand=1+.34*prie; o.pupille=1+.28*prie; o.brillant=prie; o.py=-.25*prie; o.px=.1; o.sourcilY=10*prie; o.sourcilTilt=-13*prie; o.sourcilArc=3*prie;
  o.lid=.55*fen(t,1.9,2.6,.05)*(Math.sin(t*26)>0?1:0);   // il bat de la paupière
  o.bouche=insiste>.4?'o':'petit'; o.ouvre=.25; o.k=-.9; o.joues=.8*prie;
  o.g=mel(R.g,{x:-15,y:-150+7*g2,plie:.9,devant:true,main:24},prie); o.d=mel(R.d,{x:15,y:-152+7*g2,plie:-.9,devant:true,main:24},prie);
  return ()=>{ const [ex,ey]=P.monde(16,-20,true);
    S.particules('brille',t,1.0,4.9,{n:6,forme:'etoile',x:ex,y:ey,dx:230,dy:210,vx:0,vy:0,ex:0,ey:0,vie:.5,taille:.8,rot:160,cadence:.2,couleur:'#fff'});
    S.texte('a',t,1.0,2.7,"S'il te plaît…",o.x-490,SOL-370,{taille:48,couleur:'#FF8FC1',monte:10});
    S.texte('b',t,3.0,4.7,'Allez… dis oui !',o.x+120,SOL-350,{taille:44,couleur:'#B58BFF',tremble:1});
    S.particules('coeurs',t,3.0,4.8,{n:5,forme:'coeur',x:o.x,y:SOL-380,dx:360,vx:0,vy:-70,ex:20,ey:20,vie:1.2,taille:.8,cadence:.34,onde:8}); };
});

scene('pardon',{titre:'Pardon',dit:'Pardon. Je suis désolé.',cat:'tendre',duree:6.0,vignette:2.7,cadre:[240,20,600],x:640},(t,o,S)=>{
  const gene=ease(seg(t,.3,.9)), baisse=ease(seg(t,1.3,1.9))*(1-ease(seg(t,3.9,4.4))), offre=ease(seg(t,4.3,4.8))*(1-ease(seg(t,5.75,6.0))), pied=Math.sin(t*5)*baisse;
  o.angle=-3*gene+5*baisse; o.tAngle=24*baisse+2*pied-4*offre; o.tdy=10*baisse; o.sy=1-.04*baisse;
  o.px=k(t,[[0,0],[.3,0],[.6,-.85],[1.2,-.85],[1.5,-.2],[3.9,-.2],[4.4,0]]); o.py=.75*baisse+.2*gene*(1-baisse)*(1-offre); o.lid=.2*gene*(1-offre)+.25*baisse; o.sourcilTilt=-13*gene*(1-.6*offre); o.sourcilY=2+6*offre;
  o.bouche=offre>.5?'sourire':baisse>.3?'triste':'petit'; o.k=offre>.5?.9:baisse>.3?.7:-.8; o.joues=.7*gene; o.teinte=.1*gene;
  o.g=mel(mel(R.g,BRAS.dosG,gene),{x:-24,y:-140,plie:.9,devant:true},offre); o.d=mel(mel(R.d,BRAS.dosD,gene),{x:24,y:-140,plie:-.9,devant:true},offre);
  S.objet('vase',`<ellipse cx="14" cy="2" rx="126" ry="14" fill="#BFE3FF" stroke="${N}" stroke-width="6"/>${T('M-70,0 L-84,-54 L-50,-30 L-36,-66 L-14,-22 L-8,0 Z','#7FB6FF')}${T('M30,0 L22,-34 L52,-48 L70,-14 L62,0 Z','#7FB6FF')}${T('M-22,-4 l16,-16 l14,14 Z','#A9D4FF',5)}${L('M14,-10 Q76,-28 124,-14','#4E9B3A',6)}${O(132,-14,14,'#FF8FC1',5)}${O(132,-14,5,'#FFC83D',3)}`,false);
  S.poser('vase',330,SOL,{});
  S.objet('offert',GROSCOEUR,true);
  return ()=>{ const [cx,cy]=P.monde(0,-186);
    S.poser('offert',cx,cy,{op:offre>.1?1:0,sx:.62*elast(seg(t,4.4,5.0))*(1+.07*Math.sin(t*9))});
    S.texte('z',t,.1,1.0,'crac…',250,SOL-110,{taille:38,couleur:'#7FB6FF',monte:8});
    S.texte('a',t,1.9,3.7,'Pardon…',o.x+120,SOL-300,{taille:54,couleur:'#9AA3B8',monte:10});
    S.texte('b',t,4.6,5.8,'Je suis désolé',o.x+110,SOL-370,{taille:40,couleur:'#FF8FC1'});
    S.particules('sueur',t,.7,3.6,{n:3,forme:'goutte',x:o.x+60,y:SOL-340,dx:60,vx:40,vy:30,ex:40,ey:20,g:400,vie:.6,taille:.8,cadence:.5}); };
});

scene('manque',{titre:'Tu me manques',dit:'Tu me manques.',cat:'tendre',duree:6.2,vignette:3.2,cadre:[362,20,600],x:500},(t,o,S)=>{
  const soupir=cl(t,.4,1.5), reve=ease(seg(t,1.4,2.0))*(1-ease(seg(t,5.6,6.1))), serre=fen(t,4.0,5.5,.25), resp=Math.sin(t*2.4);
  o.angle=-3*reve+2*resp*serre; o.tAngle=-12*reve*(1-serre)+10*serre; o.tdy=4*soupir; o.sy=1+.04*cl(t,.4,.95)-.03*cl(t,.95,1.5)-.02*reve+.01*resp;
  o.px=.55*reve*(1-serre); o.py=-.7*reve*(1-serre)+.3*serre; o.lid=.25*reve+.3*soupir; o.grand=1+.1*reve; o.brillant=reve; o.sourcilTilt=-12*Math.max(reve,soupir); o.sourcilY=4;
  o.oeil=serre>.7?'ferme':'normal'; o.bouche=soupir>.4?'o':'petit'; o.ouvre=.3; o.k=serre>.5?.8:-.6; o.joues=.4*reve;
  o.d=mel(mel(R.d,{x:74,y:-214,plie:-1,devant:true},reve*(1-serre)),BRAS.croiseD,serre); o.g=mel(R.g,BRAS.croiseG,serre);
  const app=elast(seg(t,1.75,2.35))*(1-ease(seg(t,5.6,6.0)));
  S.objet('p1',O(0,0,10,'#fff',5),false); S.poser('p1',o.x+118,SOL-352,{op:t>1.45&&t<5.7?1:0});
  S.objet('p2',O(0,0,16,'#fff',6),false); S.poser('p2',o.x+150,SOL-372,{op:t>1.6&&t<5.7?1:0});
  S.objet('bulle',`<path d="M-120,30 a52,52 0 0 1 12,-98 a70,70 0 0 1 128,-22 a58,58 0 0 1 96,60 a44,44 0 0 1 -14,72 a60,60 0 0 1 -110,10 a60,60 0 0 1 -112,-22 Z" fill="#fff" stroke="${N}" stroke-width="8" stroke-linejoin="round"/>`,false);
  S.poser('bulle',o.x+320,SOL-366+5*Math.sin(t*1.6),{sx:app*.8,op:t>1.75?1:0});
  S.objet('coeurB',GROSCOEUR,false); S.poser('coeurB',o.x+323,SOL-364+5*Math.sin(t*1.6),{sx:.85*app*(1+.12*Math.pow(Math.abs(Math.sin(t*4.2)),4)),op:t>1.75?1:0});
  return ()=>{ const [ex,ey]=P.monde(16,14,true);
    S.particules('larme',t,3.0,3.05,{n:1,forme:'goutte',x:ex-10,y:ey,vx:-10,vy:60,ex:0,ey:0,g:300,vie:1.0,taille:1,boucle:false});
    S.texte('a',t,.5,1.5,'pfff…',o.x-260,SOL-330,{taille:46,couleur:'#9AA3B8',monte:12});
    S.texte('b',t,2.4,3.9,'Tu me manques…',84,SOL-350,{taille:40,couleur:'#FF8FC1',monte:8});
    S.particules('petits',t,2.2,5.4,{n:4,forme:'coeur',x:o.x+140,y:SOL-330,vx:80,vy:-40,ex:20,ey:20,vie:1.2,taille:.6,cadence:.45,onde:6}); };
});

scene('bonjour',{titre:'Bonjour !',dit:'Bonjour !',cat:'tendre',duree:5.6,vignette:3.0,x:560},(t,o,S)=>{
  const etire=fen(t,.3,1.7,.3), salue=fen(t,2.0,5.0,.2), b=Math.abs(Math.sin(t*6.2))*salue;
  o.sy=1+.07*etire-.02*(1-b)*salue; o.saut=-16*b; o.angle=4*Math.sin(t*3.1)*salue; o.tAngle=-10*etire+6*Math.sin(t*6.2)*salue;
  o.lid=k(t,[[0,.7],[.4,.75],[1.2,.5],[1.8,.1],[2.0,0]]); o.oeil=etire>.6&&t<1.3?'ferme':(fen(t,3.6,4.0,.07)>.5?'heureux':'normal'); o.grand=1+.1*salue; o.brillant=salue;
  o.bouche=etire>.4?'o':salue>.4?'rire':'sourire'; o.ouvre=etire>.4?1.2*etire:.55; o.k=1.15; o.joues=.6*salue; o.sourcilY=8*etire+8*salue;
  o.g=mel(mel(R.g,{x:-124,y:-272,plie:.45,main:25},etire),BRAS.hancheG,salue); o.d=mel(mel(R.d,{x:124,y:-272,plie:-.45,main:25},etire),{x:136+34*Math.sin(t*12.4),y:-270+8*Math.cos(t*12.4),plie:-.35,main:28,pouce:-150},salue);
  const sx=lerp(1360,960,out(seg(t,.2,1.7)));
  S.objet('rayonsS',RAYONS,false); S.poser('rayonsS',sx,SOL-370,{rot:t*22,sx:1.05+.05*Math.sin(t*4)});
  S.objet('disque',DISQUE+`<circle cx="-22" cy="-6" r="7" fill="${N}"/><circle cx="20" cy="-6" r="7" fill="${N}"/><path d="M-24,16 q24,24 48,0" fill="none" stroke="${N}" stroke-width="7" stroke-linecap="round"/><ellipse cx="-36" cy="14" rx="9" ry="6" fill="#FF9A5A" opacity=".7"/><ellipse cx="36" cy="14" rx="9" ry="6" fill="#FF9A5A" opacity=".7"/>`,false); S.poser('disque',sx,SOL-370,{rot:4*Math.sin(t*2)});
  const oiseau=(aile)=>`${T('M34,-6 l24,6 l-24,8 Z','#FFB347',5)}<ellipse cx="0" cy="0" rx="36" ry="26" fill="#5CC8FF" stroke="${N}" stroke-width="6"/>${T('M-30,-4 l-30,-14 l4,22 Z','#5CC8FF',5)}<circle cx="18" cy="-8" r="5" fill="${N}"/>${T(aile,'#8FDCFF',5)}`;
  S.objet('oisA',oiseau('M-14,-6 Q-6,-56 22,-40 Q10,-20 10,-4 Z'),true); S.objet('oisB',oiseau('M-14,2 Q-8,44 20,30 Q8,16 10,0 Z'),true);
  const vol=seg(t,3.5,5.5), ox=lerp(-60,1320,vol), oy=SOL-420+26*Math.sin(vol*14), bat=Math.sin(t*26)>0;
  S.poser('oisA',ox,oy,{op:bat?1:0,rot:6*Math.cos(vol*14),sx:.9}); S.poser('oisB',ox,oy,{op:bat?0:1,rot:6*Math.cos(vol*14),sx:.9});
  S.texte('a',t,.5,1.6,'Ouaaah…',o.x-380,SOL-340,{taille:46,couleur:'#8FA3FF',monte:14});
  S.texte('b',t,2.1,3.7,'Bonjour !',o.x-440,SOL-360,{taille:64,couleur:'#FFB347',grossit:.18});
  S.texte('c',t,3.9,5.1,'cui cui !',ox-70,oy+74,{taille:34,couleur:'#5CC8FF'});
  S.particules('etoiles',t,2.1,5.0,{n:5,forme:'etoile',x:560,y:SOL-300,dx:560,dy:280,vx:0,vy:0,ex:0,ey:0,vie:.6,taille:.9,rot:120,cadence:.32});
});
