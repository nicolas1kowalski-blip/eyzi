// Fabrique les humeurs animées d'Eyzi : pour chaque scène, une vidéo (.mp4), une vignette (.png) et une ligne dans humeurs.txt,
// rangées dans ../MesPremiersJeux/Humeurs. Il faut Node avec Playwright (Chromium) et ffmpeg.
//   node fabrique.js              tout fabriquer
//   node fabrique.js miam soif    seulement ces scènes
//   node fabrique.js --vignettes  seulement les vignettes (rapide)
//   node fabrique.js --apercu     une planche d'images par scène dans ./apercu (pour relire avant de fabriquer)
const {chromium}=require('playwright'); const fs=require('fs'), path=require('path'), {execFileSync}=require('child_process');
const ICI=__dirname, SORTIE=path.join(ICI,'..','MesPremiersJeux','Humeurs'), TMP=path.join(process.env.HUMEURS_TMP||require('os').tmpdir(),'humeurs-images');
const lire=f=>fs.readFileSync(path.join(ICI,f),'utf8');
const PAGE=(corps)=>`<!doctype html><html lang="fr"><head><meta charset="utf-8"><style>
html,body{margin:0;width:1280px;height:720px;overflow:hidden;background:linear-gradient(#F6F0FF,#FFF3E4);font-family:"Nunito","Segoe UI",system-ui,sans-serif}
svg{position:absolute;left:0;top:0}#titre{position:absolute;left:0;right:0;bottom:26px;text-align:center;font-size:58px;font-weight:900;color:#2A1C4E}
</style></head><body><div id="titre"></div><svg id="s" width="1280" height="720" viewBox="64 108 1152 648">
<line x1="60" y1="610" x2="1220" y2="610" stroke="#E3D6F7" stroke-width="6" stroke-linecap="round"/><g id="fond"></g><g id="persoG"></g><g id="devant"></g></svg>
<script>${lire('rig.js')}</script><script>${corps}</script></body></html>`;
// Les scènes écrites à la main (elles ont leur propre code) : clé, fichier, et ce que l'application doit en savoir.
const LIBRES=[
  {cle:'beurk',fichier:'beurk.html',titre:'Beurk !',dit:'Beurk !',cat:'rigolo',duree:6.4,vignette:2.6,x:560,cadre:[330,40,580]},
  {cle:'prout',fichier:'prout.js',titre:'Prout !',dit:'Prout !',cat:'rigolo',duree:6.0,vignette:2.85,x:560},
  {cle:'jetaime',fichier:'jetaime.js',titre:"Je t'aime",dit:"Je t'aime !",cat:'tendre',duree:6.4,vignette:3.6,x:600},
];
const ORDRE_CAT=['joie','tendre','rigolo','pasbien','actions'];
(async()=>{
  const args=process.argv.slice(2), apercu=args.includes('--apercu'), vigSeules=args.includes('--vignettes'), choix=args.filter(a=>!a.startsWith('--'));
  const b=await chromium.launch(); const p=await b.newPage({viewport:{width:1280,height:720}}); let err=0; p.on('pageerror',e=>{ if(err++<8) console.log('  ERREUR JS :',e.message); });
  await p.addInitScript(()=>{window.__capture=true});
  fs.mkdirSync(TMP,{recursive:true}); const charger=async html=>{ const f=path.join(TMP,'page.html'); fs.writeFileSync(f,html); await p.goto('file://'+f); };   // une page neuve à chaque scène
  const moteur=['scene.js','scenes.js','scenes2.js','scenes3.js'].filter(f=>fs.existsSync(path.join(ICI,f))).map(lire).join('\n');
  await charger(PAGE(moteur)); const infos=await p.evaluate(()=>Object.values(SCENES).map(s=>({cle:s.cle,titre:s.titre,dit:s.dit,cat:s.cat,duree:s.duree,vignette:s.vignette,x:s.x??600,cadre:s.cadre})));
  const toutes=[...LIBRES,...infos].sort((a,b)=>ORDRE_CAT.indexOf(a.cat)-ORDRE_CAT.indexOf(b.cat));
  fs.mkdirSync(SORTIE,{recursive:true}); if(apercu) fs.mkdirSync(path.join(ICI,'apercu'),{recursive:true});
  for(const s of toutes){ if(choix.length&&!choix.includes(s.cle)) continue;
    if(s.fichier&&s.fichier.endsWith('.html')) await p.goto('file://'+path.join(ICI,s.fichier));
    else if(s.fichier) await charger(PAGE(lire(s.fichier)+'\nwindow.rendu=rendu; document.getElementById("titre").textContent='+JSON.stringify(s.titre)+';'));
    else await charger(PAGE(moteur+`\ndemarrer(${JSON.stringify(s.cle)}); window.rendu=rendu;`));
    const fps=30, n=Math.round(s.duree*fps), dos=path.join(TMP,s.cle); fs.rmSync(dos,{recursive:true,force:true}); fs.mkdirSync(dos,{recursive:true});
    if(apercu){ for(let i=0;i<6;i++){ const t=s.duree*(i+.5)/6; await p.evaluate(t=>window.rendu(t),t); await p.screenshot({path:path.join(dos,`a${i}.png`)}); }
      execFileSync('ffmpeg',['-v','error','-y','-i',path.join(dos,'a%d.png'),'-vf','scale=640:360,tile=3x2','-frames:v','1',path.join(ICI,'apercu',s.cle+'.jpg')]); console.log('aperçu',s.cle); continue; }
    if(!vigSeules){ for(let i=0;i<n;i++){ await p.evaluate(t=>window.rendu(t),i/fps); await p.screenshot({path:path.join(dos,String(i).padStart(4,'0')+'.png')}); }
      execFileSync('ffmpeg',['-v','error','-y','-framerate','30','-i',path.join(dos,'%04d.png'),'-vf','scale=960:540:flags=lanczos','-c:v','libx264','-profile:v','main','-pix_fmt','yuv420p','-crf','23','-movflags','+faststart',path.join(SORTIE,s.cle+'.mp4')]); }
    // La vignette : le carré autour du personnage, à l'instant choisi. Un texte coupé par le bord du carré est retiré (le titre est écrit sous la case).
    const cx=Math.round((s.x-64)*1280/1152), c=s.cadre||[Math.max(0,Math.min(1280-520,cx-260)),70,520], img=path.join(dos,'vignette.png');
    await p.evaluate(([t,c])=>{ window.rendu(t); for(const e of document.querySelectorAll('svg text')){ const r=e.getBoundingClientRect(); if(r.left<c[0]+4||r.right>c[0]+c[2]-4||r.top<c[1]+4||r.bottom>c[1]+c[2]-4) e.style.visibility='hidden'; } },[s.vignette,c]);
    await p.screenshot({path:img});
    execFileSync('ffmpeg',['-v','error','-y','-i',img,'-vf',`crop=${c[2]}:${c[2]}:${c[0]}:${c[1]},scale=320:320:flags=lanczos`,path.join(SORTIE,s.cle+'.png')]);
    fs.rmSync(dos,{recursive:true,force:true}); console.log('fabriqué',s.cle,(fs.statSync(path.join(SORTIE,s.cle+'.mp4')).size/1024|0)+' Ko'); }
  if(!apercu&&!choix.length) fs.writeFileSync(path.join(SORTIE,'humeurs.txt'),'# clé|page|titre|ce que la tablette dit|durée (s) — fabriqué par desktop/humeurs/fabrique.js\n'+toutes.map(s=>[s.cle,s.cat,s.titre,s.dit,s.duree].join('|')).join('\n')+'\n');
  await b.close(); console.log(err?('⚠ '+err+' erreur(s) JavaScript'):'sans erreur JavaScript');
})();
