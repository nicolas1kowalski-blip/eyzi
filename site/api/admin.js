// Page privée de l'éditeur (eyzi.fr/admin.html) : voir les demandes, valider
// (créer la licence et l'envoyer), refuser, renvoyer un numéro.
// Protégée par ADMIN_TOKEN (variable Vercel : une longue phrase secrète),
// envoyé par le navigateur dans l'en-tête x-admin-token.
import { supabaseAdmin, issueLicence, places } from '../lib/licences.js';
import { mailConfigured, sendLicence, refuse } from '../lib/mail.js';

function authorized(req) {
  const want = String(process.env.ADMIN_TOKEN || '');
  const got = String(req.headers['x-admin-token'] || '');
  if (want.length < 12 || got.length !== want.length) return false;
  let diff = 0;
  for (let i = 0; i < want.length; i++) diff |= want.charCodeAt(i) ^ got.charCodeAt(i);
  return diff === 0;
}

export default async function handler(req, res) {
  res.setHeader('Cache-Control', 'no-store');
  if (!process.env.ADMIN_TOKEN || String(process.env.ADMIN_TOKEN).length < 12) {
    res.status(503).json({ ok: false, message: "ADMIN_TOKEN n'est pas réglé dans Vercel (12 caractères minimum)." });
    return;
  }
  if (!authorized(req)) { res.status(401).json({ ok: false, message: 'Jeton incorrect.' }); return; }
  const supabase = supabaseAdmin();
  if (!supabase) { res.status(503).json({ ok: false, message: 'SUPABASE_URL / SUPABASE_SERVICE_ROLE_KEY manquants.' }); return; }

  try {
    if (req.method === 'GET') {
      const status = ['new', 'done', 'refused'].includes(req.query.status) ? req.query.status : 'new';
      const { data, error } = await supabase.from('licence_requests')
        .select('id, created_at, topic, name, email, kind, org, child, gear, tablets, message, recontact, status, licence_key, note')
        .eq('status', status).order('created_at', { ascending: false }).limit(200);
      if (error) throw error;
      const p = await places(supabase);
      res.status(200).json({ ok: true, requests: data || [], places: p, mail: mailConfigured() });
      return;
    }
    if (req.method !== 'POST') { res.status(405).json({ ok: false, message: 'Méthode non autorisée.' }); return; }

    const body = typeof req.body === 'string' ? JSON.parse(req.body || '{}') : (req.body || {});
    const id = parseInt(body.id, 10);
    const action = String(body.action || '');
    if (!id) { res.status(400).json({ ok: false, message: 'Demande inconnue.' }); return; }
    const { data: reqRow, error: rErr } = await supabase.from('licence_requests').select('*').eq('id', id).single();
    if (rErr || !reqRow) { res.status(404).json({ ok: false, message: 'Demande introuvable.' }); return; }
    const r = { ...reqRow, licenceKey: reqRow.licence_key || '', resent: false, placesLeft: null };

    if (action === 'approve') {
      if (r.topic !== 'licence') { res.status(400).json({ ok: false, message: "Ce n'est pas une demande de licence." }); return; }
      await issueLicence(supabase, r, id, { force: true });
      let mailed = false;
      if (mailConfigured() && r.email) { try { await sendLicence(r); mailed = true; } catch (e) { console.error('mail licence', e?.message || e); } }
      res.status(200).json({ ok: true, licence: r.licenceKey, mailed, resent: r.resent });
      return;
    }
    if (action === 'resend') {
      if (!r.licenceKey) { res.status(400).json({ ok: false, message: 'Pas de numéro à renvoyer.' }); return; }
      let mailed = false;
      if (mailConfigured() && r.email) { try { await sendLicence(r); mailed = true; } catch (e) { console.error('mail licence', e?.message || e); } }
      res.status(200).json({ ok: true, licence: r.licenceKey, mailed });
      return;
    }
    if (action === 'refuse') {
      const note = String(body.note || '').slice(0, 500);
      await supabase.from('licence_requests').update({ status: 'refused', note: note || 'refusée depuis la page admin' }).eq('id', id);
      let mailed = false;
      if (body.notify && mailConfigured() && r.email && r.topic === 'licence') {
        try { await refuse(r, note); mailed = true; } catch (e) { console.error('mail refus', e?.message || e); }
      }
      res.status(200).json({ ok: true, mailed });
      return;
    }
    if (action === 'reopen') {
      await supabase.from('licence_requests').update({ status: 'new' }).eq('id', id);
      res.status(200).json({ ok: true });
      return;
    }
    res.status(400).json({ ok: false, message: 'Action inconnue.' });
  } catch (e) {
    console.error('admin', e);
    res.status(500).json({ ok: false, message: String(e?.message || e).slice(0, 300) });
  }
}
