// Page privée de l'éditeur (eyzi.fr/admin.html) : voir les demandes, valider
// (créer la licence et l'envoyer), refuser, renvoyer un numéro.
// Protégée par ADMIN_TOKEN (variable Vercel : une longue phrase secrète),
// envoyé par le navigateur dans l'en-tête x-admin-token.
import { supabaseAdmin, issueLicence, places } from '../lib/licences.js';
import { mailConfigured, sendLicence, refuse } from '../lib/mail.js';
import catalogue from '../lib/activities.js';

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
    if (req.method === 'GET' && req.query.view === 'stats') {
      // Statistiques d'usage (ce que les familles utilisent, pas plus) : les
      // vues SQL de supabase.sql, plus le catalogue des activités de l'app
      // pour repérer ce qui n'est jamais ouvert.
      const TRIAL = 'EYZ-ESSAI-0000-0000-0000'; // pseudo-licence des essais de 10 jours (usage anonyme)
      const [resume, total, trente, lic, act, trialUse] = await Promise.all([
        supabase.from('usage_resume').select('*'),
        supabase.from('usage_par_famille').select('*'),
        supabase.from('usage_30_jours').select('*'),
        supabase.from('licences').select('key, family, channel, max_tablets, enabled, expires_at, created_at'),
        supabase.from('activations').select('licence_key, machine, app_version, first_seen, last_seen, enabled'),
        supabase.from('usage').select('machine, activity, label, launches, seconds, day').eq('licence_key', TRIAL).order('day', { ascending: false }).limit(5000),
      ]);
      const err = resume.error || total.error || trente.error || lic.error || act.error || trialUse.error;
      if (err) throw err;
      // Essais sans licence : une ligne par tablette (identifiant haché), avec ses activités.
      const trialName = ((lic.data || []).find(l => l.key === TRIAL) || {}).family || 'Essais sans licence';
      const perMachine = {};
      (act.data || []).filter(a => a.licence_key === TRIAL).forEach(a => {
        perMachine[a.machine] = { machine: a.machine, version: a.app_version, depuis: a.first_seen, vue: a.last_seen, minutes: 0, jours: new Set(), activites: {} };
      });
      (trialUse.data || []).forEach(u => {
        const m = perMachine[u.machine] = perMachine[u.machine] || { machine: u.machine, version: null, depuis: null, vue: null, minutes: 0, jours: new Set(), activites: {} };
        m.minutes += (u.seconds || 0) / 60; m.jours.add(u.day);
        const a = m.activites[u.activity] = m.activites[u.activity] || { nom: u.label || u.activity, minutes: 0, ouvertures: 0 };
        a.minutes += (u.seconds || 0) / 60; a.ouvertures += u.launches || 0;
      });
      const essais = Object.values(perMachine).map(m => ({
        machine: m.machine, version: m.version, depuis: m.depuis, vue: m.vue, minutes: Math.round(m.minutes), jours: m.jours.size,
        top: Object.values(m.activites).sort((a, b) => b.minutes - a.minutes).slice(0, 5).map(a => ({ nom: a.nom, minutes: Math.round(a.minutes), ouvertures: a.ouvertures })),
      })).sort((a, b) => String(b.vue || '').localeCompare(String(a.vue || '')));
      lic.data = (lic.data || []).filter(l => l.key !== TRIAL);
      resume.data = (resume.data || []).filter(r => r.licence !== TRIAL);
      total.data = (total.data || []).filter(r => r.famille !== trialName);
      trente.data = (trente.data || []).filter(r => r.famille !== trialName);
      // Installée ou pas : une activation = l'application a été installée et la licence saisie.
      const tablets = {};
      (act.data || []).forEach(a => { (tablets[a.licence_key] = tablets[a.licence_key] || []).push(a); });
      const familles = (lic.data || []).map(l => {
        const t = (tablets[l.key] || []).sort((a, b) => String(b.last_seen).localeCompare(String(a.last_seen)));
        return {
          famille: l.family, licence: l.key, canal: l.channel, max_tablettes: l.max_tablets, active: l.enabled,
          expire: l.expires_at, creee: l.created_at,
          installee: t.length > 0,
          premiere_installation: t.length ? t.map(x => x.first_seen).sort()[0] : null,
          derniere_ouverture: t.length ? t[0].last_seen : null,
          version: t.length ? t[0].app_version : null,
          tablettes: t.map(x => ({ machine: x.machine, version: x.app_version, depuis: x.first_seen, vue: x.last_seen, active: x.enabled })),
        };
      });
      res.status(200).json({ ok: true, resume: resume.data || [], total: total.data || [], trente: trente.data || [], familles, essais, catalogue });
      return;
    }
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
