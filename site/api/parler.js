// L'ÉCOUTE DU MONDE PARLER : la tablette envoie la phrase qu'un adulte vient de
// dire à l'enfant (texte seulement, jamais l'audio) ; on demande à Claude une
// reformulation très simple et trois à six réponses courtes à la première
// personne de l'enfant, que la tablette affiche en tuiles à regarder.
//
// Garde-fous : licence et tablette connues (table activations), 300 phrases par
// tablette et par jour (compteur en mémoire : par instance, indicatif), phrase
// limitée à 300 caractères. La clé Anthropic reste ici (ANTHROPIC_API_KEY).
import Anthropic from '@anthropic-ai/sdk';
import { supabaseAdmin } from '../lib/licences.js';

const MAX_PER_DAY = 300;
const counters = new Map(); // machine → { day, n }

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

const SYSTEM = `Tu aides un enfant qui ne parle pas et qui répond avec ses yeux sur une tablette. Un adulte vient de lui dire une phrase. Tu renvoies UNIQUEMENT un objet JSON, sans texte autour, de la forme :
{"simple": "...", "reponses": [{"texte": "...", "emoji": "..."}]}
- "simple" : la phrase reformulée en mots très simples, courte, telle que l'enfant doit la comprendre (sans changer le sens).
- "reponses" : 3 à 6 réponses possibles, à la première personne de l'enfant, chacune de 1 à 4 mots, dans l'ordre le plus utile. Si l'adulte propose un choix (« du jus ou de l'eau »), chaque option est une réponse. Si c'est une question fermée, "Oui" et "Non" sont des réponses. Termine toujours par une réponse qui ouvre ("Autre chose", "Je ne sais pas", "Plus tard"…). Jamais de réponse blessante ni dangereuse.
- "emoji" : un seul emoji courant qui illustre la réponse.
Si la phrase n'est pas adressée à l'enfant ou n'appelle pas de réponse, renvoie quand même des réponses utiles ("D'accord", "Encore", "Je t'aime", "Autre chose").`;

export default async function handler(req, res) {
  res.setHeader('Cache-Control', 'no-store');
  if (req.method !== 'POST') { res.status(405).json({ ok: false, message: 'Méthode non autorisée.' }); return; }
  const body = typeof req.body === 'string' ? JSON.parse(req.body || '{}') : (req.body || {});
  const key = clean(body.key, 40).toUpperCase();
  const machine = clean(body.machine, 80);
  const child = clean(body.child, 40) || "l'enfant";
  const question = clean(body.question, 300);
  const vocab = Array.isArray(body.vocab) ? body.vocab.slice(0, 120).map((v) => ({ cle: clean(v?.cle, 40), mot: clean(v?.mot, 40) })).filter((v) => v.cle && v.mot) : [];
  const historique = Array.isArray(body.historique) ? body.historique.slice(-3).map((h) => ({ q: clean(h?.q, 200), r: clean(h?.r, 60) })).filter((h) => h.q) : [];
  if (!/^EYZ-[A-Z0-9-]{10,}$/.test(key) || !machine || question.length < 2) { res.status(400).json({ ok: false, message: 'Demande incomplète.' }); return; }

  const apiKey = String(process.env.ANTHROPIC_API_KEY || '').trim();
  if (!apiKey) { res.status(503).json({ ok: false, message: "Les réponses proposées ne sont pas configurées (ANTHROPIC_API_KEY)." }); return; }
  const supabase = supabaseAdmin();
  if (!supabase) { res.status(503).json({ ok: false, message: 'Serveur non configuré.' }); return; }

  try {
    // La licence existe, est active, et cette tablette y est enregistrée.
    const { data: lic } = await supabase.from('licences').select('key, enabled, expires_at, modules').eq('key', key).maybeSingle();
    if (!lic || !lic.enabled || (lic.expires_at && new Date(lic.expires_at) < new Date())) { res.status(403).json({ ok: false, message: 'Licence inconnue ou terminée.' }); return; }
    if (lic.modules && !String(lic.modules).split(',').map((m) => m.trim()).includes('parler')) { res.status(403).json({ ok: false, message: 'Le monde Parler ne fait pas partie de cette licence.' }); return; }
    const { data: act } = await supabase.from('activations').select('machine, enabled').eq('licence_key', key).eq('machine', machine).maybeSingle();
    if (!act || act.enabled === false) { res.status(403).json({ ok: false, message: 'Tablette inconnue pour cette licence.' }); return; }
    if (tooMany(machine)) { res.status(429).json({ ok: false, message: 'Trop de demandes aujourd\'hui.' }); return; }

    const client = new Anthropic({ apiKey });
    const response = await client.beta.messages.create({
      model: 'claude-haiku-5-5',
      max_tokens: 600,
      betas: ['server-side-fallback-2026-07-01'],
      fallbacks: 'default',
      output_config: { effort: 'low' },
      system: SYSTEM + `\nL'enfant s'appelle ${child}.`,
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
    const m = text.match(/\{[\s\S]*\}/);
    let parsed = null;
    try { parsed = m ? JSON.parse(m[0]) : null; } catch { parsed = null; }
    const reponses = Array.isArray(parsed?.reponses)
      ? parsed.reponses.map((r) => ({ texte: clean(r?.texte, 60), emoji: clean(r?.emoji, 8), cle: clean(r?.cle, 40) })).filter((r) => r.texte).slice(0, 6)
      : [];
    const types = ['fermee', 'choix', 'ouverte', 'consigne', 'autre'];
    const type = types.includes(parsed?.type) ? parsed.type : 'autre';
    res.status(200).json({ ok: true, type, simple: clean(parsed?.simple, 200) || question, reponses });
  } catch (e) {
    console.error('parler', e);
    res.status(500).json({ ok: false, message: 'Erreur du serveur.' });
  }
}
