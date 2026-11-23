// Transforme les images SVG du banc d'essai en PNG, puis en planche (--planche) ou en vidéo (--video).
//   node images.js <dossier> [--planche colonnes] [--video sortie.mp4] [--ips 15]
const { chromium } = require('playwright'); const fs = require('fs'), path = require('path'), { execFileSync } = require('child_process');
(async () => {
  const d = process.argv[2], a = process.argv.slice(3); const svgs = fs.readdirSync(d).filter(f => f.endsWith('.svg')).sort();
  const b = await chromium.launch(), p = await b.newPage({ viewport: { width: 1600, height: 900 } });
  for (const f of svgs) { await p.goto('file://' + path.resolve(d, f)); await p.screenshot({ path: path.join(d, f.replace('.svg', '.png')) }); }
  await b.close();
  if (a.includes('--planche')) { const c = +a[a.indexOf('--planche') + 1] || 3; execFileSync('ffmpeg', ['-v', 'error', '-y', '-i', path.join(d, '%04d.png'), '-vf', `scale=800:450,tile=${c}x${Math.ceil(svgs.length / c)}`, '-frames:v', '1', '-q:v', '3', path.join(d, 'planche.jpg')]); }
  if (a.includes('--video')) execFileSync('ffmpeg', ['-v', 'error', '-y', '-framerate', String(a.includes('--ips') ? a[a.indexOf('--ips') + 1] : 30), '-r', '30', '-i', path.join(d, '%04d.png'), '-vf', 'scale=1280:720:flags=lanczos', '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '22', a[a.indexOf('--video') + 1]]);
  console.log(svgs.length, 'images');
})();
