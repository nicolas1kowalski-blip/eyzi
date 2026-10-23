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

  // LICENCE AUTOMATIQUE (bêta) : tant qu'il reste des places, le numéro est
  // créé tout de suite et envoyé à la famille ; sinon la demande est gardée
  // (status 'new') pour la vague suivante. Une même adresse e-mail ne reçoit
  // qu'une licence : on lui renvoie la sienne.
  const r = { topic, name, email, kind, org, child, gear, tablets, message, recontact, licenceKey: '', placesLeft: null, resent: false };
  if (topic === 'licence') {
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

// Numéro de licence : EYZ-XXXX-XXXX-XXXX-XXXX (même alphabet que new_licence_key en SQL).
function newKey() {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  let s = '';
  for (const b of bytes) s += alphabet[b % alphabet.length];
  return 'EYZ-' + s.slice(0, 4) + '-' + s.slice(4, 8) + '-' + s.slice(8, 12) + '-' + s.slice(12, 16);
}

// Variables Vercel : BETA_PLACES (nombre total de licences prod, défaut 10),
// BETA_UNTIL (fin de la bêta, défaut 2027-06-30).
async function issueLicence(supabase, r, requestId) {
  const places = Math.max(0, parseInt(process.env.BETA_PLACES || '10', 10) || 0);
  const until = (process.env.BETA_UNTIL || '2027-06-30').trim();
  const maxTablets = Math.min(r.tablets, r.kind === 'famille' ? 2 : 10);

  // Déjà servie ? (même e-mail, demande traitée avec un numéro)
  const { data: prev } = await supabase.from('licence_requests')
    .select('licence_key').eq('email', r.email).eq('topic', 'licence').not('licence_key', 'is', null)
    .order('created_at', { ascending: false }).limit(1);
  if (prev && prev.length && prev[0].licence_key) {
    r.licenceKey = prev[0].licence_key;
    r.resent = true;
    await supabase.from('licence_requests').update({ status: 'done', licence_key: r.licenceKey, note: 'numéro déjà attribué, renvoyé' }).eq('id', requestId);
    return;
  }

  // Reste-t-il des places ? (licences prod existantes, créées par le site ou à la main)
  const { count, error: cErr } = await supabase.from('licences').select('key', { count: 'exact', head: true }).eq('channel', 'prod');
  if (cErr) throw cErr;
  r.placesLeft = Math.max(0, places - (count || 0));
  if (r.placesLeft <= 0) return; // demande gardée (status 'new')

  // Création de la licence (nouvel essai si le numéro existe déjà : improbable).
  let key = '';
  for (let i = 0; i < 5 && !key; i++) {
    const k = newKey();
    const { error } = await supabase.from('licences').insert({
      key: k, family: r.name, channel: 'prod', max_tablets: maxTablets, enabled: true,
      expires_at: until || null, note: 'créée par le site (bêta) pour ' + r.email,
    });
    if (!error) key = k; else if (error.code !== '23505') throw error;
  }
  if (!key) throw new Error('impossible de générer un numéro unique');
  r.licenceKey = key;
  r.placesLeft -= 1;
  await supabase.from('licence_requests').update({ status: 'done', licence_key: key, note: 'licence créée automatiquement' }).eq('id', requestId);
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
