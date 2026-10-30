// L'ÉCOUTE DU MONDE PARLER : la tablette envoie la phrase qu'un adulte vient de
// dire à l'enfant (texte seulement, jamais l'audio) ; on demande à Claude une
// reformulation très simple et trois à six réponses courtes à la première
// personne de l'enfant, que la tablette affiche en tuiles à regarder.
//
// Garde-fous : licence et tablette connues (table activations), 300 phrases par
// tablette et par jour (compteur en mémoire : par instance, indicatif), phrase
// limitée à 300 caractères. La clé Anthropic reste ici (ANTHROPIC_API_KEY).
//
// RÉSERVÉ À L'ÉDITEUR pour l'instant : seules les licences du canal « test »
// (celles de l'éditeur) et les numéros listés dans PARLER_LICENCES (séparés par
// des virgules) obtiennent des réponses de Claude. Les familles (canal « prod »)
// reçoivent un refus poli et la tablette garde sa grille devinée, sans réseau.
import Anthropic from '@anthropic-ai/sdk';
import { supabaseAdmin } from '../lib/licences.js';

// Vercel : la fonction peut durer jusqu'à 30 s (l'appel à Claude prend 1 à 4 s).
export const maxDuration = 30;

const MAX_PER_DAY = 300;
const counters = new Map(); // machine → { day, n }
const checked = new Map();  // key|machine → instant de la dernière vérification réussie (10 min)

function clean(v, max) {
  return String(v ?? '').replace(/[\u0000-\u001f]/g, ' ').trim().slice(0, max);
}

function tooMany(machine) {
  const day = new Date().toISOString().slice(0, 10);
  const c = counters.get(machine) || { day, n: 0 };
  if (c.day !== day) { c.day = day; c.n = 0; }
  c.n += 1;
  counters.set(machine, c);
  return c.n > MAX_PER_DAY;
}

const SYSTEM = `Tu aides un enfant qui ne parle pas et qui répond avec ses yeux sur une tablette, en regardant une case parmi quelques-unes. Un adulte vient de lui parler. Tu renvoies UNIQUEMENT un objet JSON, sans texte autour, de la forme :
{"type": "...", "simple": "...", "reponses": [{"texte": "...", "emoji": "...", "cle": "..."}]}

"type", la nature de ce qui a été dit :
- "fermee" : une question à laquelle on répond oui ou non (« Tu as faim ? », « On va au parc ? »).
- "choix" : l'adulte propose des options (« du jus ou de l'eau ? », « le rouge, le bleu ou le vert ? »).
- "ouverte" : une question qui appelle un contenu (« Qu'est-ce que tu veux manger ? », « Où as-tu mal ? », « Avec qui tu veux jouer ? »).
- "consigne" : l'adulte annonce ou demande quelque chose (« On va prendre le bain », « Donne-moi la main »).
- "autre" : ce n'est pas adressé à l'enfant, ou ça n'appelle pas de réponse.

"simple" : la phrase reformulée très simplement, courte, telle que l'enfant doit la comprendre, sans changer le sens.

"reponses" : les réponses possibles, à la première personne de l'enfant, chacune de 1 à 4 mots, dans l'ordre le plus utile.
Elles doivent être RÉALISTES pour un enfant de cet âge, dans sa vie de tous les jours, et COHÉRENTES avec le moment de la journée : au petit-déjeuner on propose une tartine, des céréales, du lait, un chocolat chaud, pas des pâtes ; au goûter une compote, un gâteau, un fruit ; un enfant de 4 ans ne demande ni café ni choses d'adulte. Si la famille a décrit ce que l'enfant aime, ses goûts et ses habitudes passent en premier.
Les règles par type :
- fermee : exactement 3 réponses : "Oui", "Non", puis une nuance utile ("Un peu", "Plus tard", "Je ne sais pas").
- choix : chaque option proposée (dans l'ordre où l'adulte les a dites), puis "Les deux" si ça a un sens, puis "Autre chose". 6 au plus.
- ouverte : 4 à 6 réponses concrètes et probables pour cet enfant, en PRIORITÉ prises dans son vocabulaire (ci-dessous) et dans ce que la famille dit de lui, puis "Autre chose".
- consigne : 3 réponses : "D'accord", "Non", "Attends".
- autre : 3 réponses : "D'accord", "Encore", "Je t'aime".
Jamais de réponse blessante ni dangereuse.

"emoji" : un seul emoji courant qui illustre la réponse.
"cle" : si la réponse correspond à un mot du vocabulaire de l'enfant (liste "cle: mot"), sa clé ; sinon "".

Le vocabulaire de l'enfant et, s'il y en a, les derniers échanges, sont donnés dans le message.`;

export default async function handler(req, res) {
  res.setHeader('Cache-Control', 'no-store');
  if (req.method !== 'POST') { res.status(405).json({ ok: false, message: 'Méthode non autorisée.' }); return; }
  const body = typeof req.body === 'string' ? JSON.parse(req.body || '{}') : (req.body || {});
  // Réveil : la tablette appelle ici en entrant dans Parler, pour que la fonction soit chaude à la première vraie phrase.
  if (body.ping) { res.status(200).json({ ok: true, pong: true }); return; }
  const key = clean(body.key, 40).toUpperCase();
  const machine = clean(body.machine, 80);
  const child = clean(body.child, 40) || "l'enfant";
  const question = clean(body.question, 300);
  const age = Math.max(1, Math.min(29, parseInt(body.age, 10) || 4));
  const profil = clean(body.profil, 1200);
  const moment = clean(body.moment, 60);
  const heure = Math.max(0, Math.min(23, parseInt(body.heure, 10) || 12));
  const vocab = Array.isArray(body.vocab) ? body.vocab.slice(0, 120).map((v) => ({ cle: clean(v?.cle, 40), mot: clean(v?.mot, 40) })).filter((v) => v.cle && v.mot) : [];
  const historique = Array.isArray(body.historique) ? body.historique.slice(-3).map((h) => ({ q: clean(h?.q, 200), r: clean(h?.r, 60) })).filter((h) => h.q) : [];
  if (!/^EYZ-[A-Z0-9-]{10,}$/.test(key) || !machine || question.length < 2) { res.status(400).json({ ok: false, message: 'Demande incomplète.' }); return; }

  const apiKey = String(process.env.ANTHROPIC_API_KEY || '').trim();
  if (!apiKey) { res.status(503).json({ ok: false, message: "Les réponses proposées ne sont pas configurées (ANTHROPIC_API_KEY)." }); return; }
  const supabase = supabaseAdmin();
  if (!supabase) { res.status(503).json({ ok: false, message: 'Serveur non configuré.' }); return; }

  try {
    // La licence existe, est active, et cette tablette y est enregistrée (vérifié au plus une fois par 10 minutes).
    const ck = key + '|' + machine;
    if (!(checked.get(ck) > Date.now() - 10 * 60 * 1000)) {
      const { data: lic } = await supabase.from('licences').select('key, enabled, expires_at, modules, channel').eq('key', key).maybeSingle();
      if (!lic || !lic.enabled || (lic.expires_at && new Date(lic.expires_at) < new Date())) { res.status(403).json({ ok: false, message: 'Licence inconnue ou terminée.' }); return; }
      if (lic.modules && !String(lic.modules).split(',').map((m) => m.trim()).includes('parler')) { res.status(403).json({ ok: false, message: 'Le monde Parler ne fait pas partie de cette licence.' }); return; }
      const allowed = String(process.env.PARLER_LICENCES || '').toUpperCase().split(',').map((k) => k.trim()).filter(Boolean);
      if (lic.channel !== 'test' && !allowed.includes(key)) { res.status(403).json({ ok: false, message: "Les réponses proposées sont réservées à l'éditeur pendant la bêta." }); return; }
      const { data: act } = await supabase.from('activations').select('machine, enabled').eq('licence_key', key).eq('machine', machine).maybeSingle();
      if (!act || act.enabled === false) { res.status(403).json({ ok: false, message: 'Tablette inconnue pour cette licence.' }); return; }
      checked.set(ck, Date.now());
    }
    if (tooMany(machine)) { res.status(429).json({ ok: false, message: 'Trop de demandes aujourd\'hui.' }); return; }

    const client = new Anthropic({ apiKey });
    // Haiku 5.5 : pas de repli serveur (réservé aux modèles Opus / Sonnet / Fable).
    const t0 = Date.now();
    const response = await client.messages.create({
      model: 'claude-haiku-5-5',
      max_tokens: 600,
      thinking: { type: 'disabled' },   // des réponses courtes, tout de suite
      output_config: { effort: 'low' },
      system: SYSTEM + `\nL'enfant s'appelle ${child} et a ${age} ans. Il est ${heure} h : c'est ${moment || 'la journée'}.` + (profil ? `\nCe que la famille dit de ${child} : ${profil}` : ''),
      messages: [{
        role: 'user',
        content:
          (vocab.length ? `Vocabulaire de l'enfant :\n${vocab.map((v) => `${v.cle}: ${v.mot}`).join('\n')}\n\n` : '') +
          (historique.length ? `Derniers échanges :\n${historique.map((h) => `Adulte : « ${h.q} » → l'enfant a répondu : « ${h.r || '(rien)'} »`).join('\n')}\n\n` : '') +
          `L'adulte vient de dire : « ${question} »`,
      }],
    });
    if (response.stop_reason === 'refusal') { res.status(200).json({ ok: true, type: 'autre', simple: question, reponses: [] }); return; }
    let text = '';
    for (const block of response.content) if (block.type === 'text') text += block.text;
    console.log('parler', Date.now() - t0, 'ms', response.stop_reason, text.length);
    const m = text.match(/\{[\s\S]*\}/);
    let parsed = null;
    try { parsed = m ? JSON.parse(m[0]) : null; } catch { parsed = null; }
    if (!parsed) { console.error('parler: réponse non JSON', text.slice(0, 300)); res.status(502).json({ ok: false, message: "Réponse de l'IA illisible : " + text.slice(0, 120) }); return; }
    const reponses = Array.isArray(parsed?.reponses)
      ? parsed.reponses.map((r) => ({ texte: clean(r?.texte, 60), emoji: clean(r?.emoji, 8), cle: clean(r?.cle, 40) })).filter((r) => r.texte).slice(0, 6)
      : [];
    const types = ['fermee', 'choix', 'ouverte', 'consigne', 'autre'];
    const type = types.includes(parsed?.type) ? parsed.type : 'autre';
    res.status(200).json({ ok: true, type, simple: clean(parsed?.simple, 200) || question, reponses });
  } catch (e) {
    console.error('parler', e);
    const detail = String(e?.message || e).slice(0, 200);
    res.status(500).json({ ok: false, message: 'Erreur du serveur : ' + detail });
  }
}
