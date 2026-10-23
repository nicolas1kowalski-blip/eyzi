// Demande de licence (eyzi.fr/licence.html) ou retour d'utilisateur (eyzi.fr/retours.html).
// Enregistre la demande dans la table Supabase "licence_requests" avec la clé
// service_role (côté serveur uniquement). L'éditeur lit les demandes dans
// Supabase → Table Editor → licence_requests, puis crée la licence avec
// select public.create_licence(...) et répond à la famille par e-mail.
//
// Variables d'environnement (Vercel → Settings → Environment Variables) :
//   SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY   (déjà utilisées par api/books.js)
import { createClient } from '@supabase/supabase-js';
import { mailConfigured, notifyEditor, acknowledge, sendLicence } from '../lib/mail.js';
import { issueLicence, autoLicence } from '../lib/licences.js';

const MAX = { name: 120, email: 160, org: 160, message: 2000, child: 40, gear: 80 };

function clean(v, max) {
  return typeof v === 'string' ? v.trim().slice(0, max) : '';
}

export default async function handler(req, res) {
  res.setHeader('Cache-Control', 'no-store');
  if (req.method !== 'POST') {
    res.status(405).json({ ok: false, message: 'Méthode non autorisée.' });
    return;
  }
  const body = typeof req.body === 'string' ? JSON.parse(req.body || '{}') : (req.body || {});
  // Piège à robots : le champ "site" est caché ; un humain le laisse vide.
  if (clean(body.site, 50)) { res.status(200).json({ ok: true }); return; }

  const topic = body.topic === 'retour' ? 'retour' : 'licence';
  const name = clean(body.name, MAX.name);
  const email = clean(body.email, MAX.email);
  const kind = ['famille', 'etablissement', 'professionnel'].includes(body.kind) ? body.kind : 'famille';
  const org = clean(body.org, MAX.org);
  const child = clean(body.child, MAX.child);
  const gear = clean(body.gear, MAX.gear);
  const recontact = body.recontact === '1' || body.recontact === true;
  const tablets = Math.min(50, Math.max(1, parseInt(body.tablets, 10) || 1));
  const message = clean(body.message, MAX.message);
  const emailOk = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  if (topic === 'licence' && (name.length < 2 || !emailOk)) {
    res.status(400).json({ ok: false, message: 'Il manque un nom ou une adresse e-mail valide.' });
    return;
  }
  if (topic === 'retour' && (message.length < 5 || (email && !emailOk))) {
    res.status(400).json({ ok: false, message: 'Écrivez votre retour (et une adresse e-mail valide, si vous en donnez une).' });
    return;
  }

  // L'URL du projet, SANS le chemin /rest/v1/ (on tolère qu'il ait été collé :
  // supabase-js l'ajoute lui-même, sinon PostgREST répond PGRST125).
  const url = String(process.env.SUPABASE_URL || '').trim().replace(/\/rest\/v1\/?$/i, '').replace(/\/+$/, '');
  const key = String(process.env.SUPABASE_SERVICE_ROLE_KEY || '').trim();
  if (!url || !key) {
    res.status(503).json({ ok: false, message: "Le serveur n'est pas encore configuré. Écrivez-nous directement par e-mail." });
    return;
  }
  const supabase = createClient(url, key, { auth: { persistSession: false } });
  const { data: inserted, error } = await supabase.from('licence_requests').insert({
    topic, name: name || '(anonyme)', email: email || '', kind, org, child, gear, tablets, message, recontact,
    user_agent: clean(req.headers['user-agent'], 300),
  }).select('id').single();
  if (error) {
    console.error('licence_requests insert', error);
    res.status(500).json({ ok: false, message: explain(error), code: error.code || '', detail: String(error.message || '').slice(0, 200) });
    return;
  }

  // Par défaut la demande ATTEND la validation de l'éditeur (eyzi.fr/admin.html).
  // Avec AUTO_LICENCE=1 : tant qu'il reste des places, le numéro est créé tout
  // de suite et envoyé ; une même adresse e-mail reçoit toujours la même licence.
  const r = { topic, name, email, kind, org, child, gear, tablets, message, recontact, licenceKey: '', placesLeft: null, resent: false };
  if (topic === 'licence' && autoLicence()) {
    try { await issueLicence(supabase, r, inserted?.id); } catch (e) { console.error('licence auto', e?.message || e); }
  }

  // E-mails : l'éditeur est prévenu, la famille reçoit son numéro ou un
  // accusé de réception. Un échec d'envoi n'annule pas la demande.
  let mailed = false;
  if (mailConfigured()) {
    try { await notifyEditor(r); mailed = true; } catch (e) { console.error('mail éditeur', e?.message || e); }
    if (topic === 'licence') {
      try { if (r.licenceKey) await sendLicence(r); else await acknowledge(r); }
      catch (e) { console.error('mail famille', e?.message || e); mailed = false; }
    }
  }
  res.status(200).json({ ok: true, mailed, licence: Boolean(r.licenceKey) });
}

// Message précis selon la cause (affiché dans le formulaire, pour que la
// famille — ou l'éditeur — sache quoi faire).
function explain(error) {
  const code = String(error.code || '');
  const msg = String(error.message || '').toLowerCase();
  const mail = ' Écrivez-nous directement : contact@eyzi.fr';
  if (code === '42P01' || msg.includes('does not exist') && msg.includes('licence_requests'))
    return "Le serveur n'est pas encore prêt (table des demandes absente : exécuter supabase.sql)." + mail;
  if (code === '42703' || code === 'PGRST204' || msg.includes('column'))
    return "Le serveur n'est pas à jour (colonne manquante : relancer supabase.sql)." + mail;
  if (code === '42501' || msg.includes('row-level security') || msg.includes('permission denied'))
    return "Le serveur refuse l'écriture (clé serveur incorrecte : il faut la clé service_role, pas la clé publique)." + mail;
  if (code === 'PGRST125' || msg.includes('invalid path'))
    return "L'adresse du serveur est mal réglée (SUPABASE_URL doit être https://<projet>.supabase.co, sans /rest/v1/)." + mail;
  if (code === 'PGRST301' || msg.includes('jwt') || msg.includes('api key') || msg.includes('apikey'))
    return "Le serveur refuse la connexion (clé serveur invalide)." + mail;
  if (msg.includes('fetch failed') || msg.includes('enotfound') || msg.includes('network'))
    return "Le serveur de données est injoignable (adresse SUPABASE_URL incorrecte ?)." + mail;
  return "Impossible d'enregistrer la demande pour le moment (" + (code || 'erreur') + ")." + mail;
}
