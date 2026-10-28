// Création des licences côté serveur (clé service_role) : utilisée par le
// formulaire (mode automatique, si AUTO_LICENCE=1) et par la page admin
// (validation manuelle, le mode par défaut).
//
// Variables Vercel : BETA_PLACES (nombre total de licences prod, défaut 10),
// BETA_UNTIL (fin de la bêta, défaut 2027-06-30), AUTO_LICENCE ('1' = le
// formulaire attribue tout seul tant qu'il reste des places).
import { createClient } from '@supabase/supabase-js';

export function supabaseAdmin() {
  const url = String(process.env.SUPABASE_URL || '').trim().replace(/\/rest\/v1\/?$/i, '').replace(/\/+$/, '');
  const key = String(process.env.SUPABASE_SERVICE_ROLE_KEY || '').trim();
  if (!url || !key) return null;
  return createClient(url, key, { auth: { persistSession: false } });
}

export const betaPlaces = () => Math.max(0, parseInt(process.env.BETA_PLACES || '10', 10) || 0);
export const betaUntil = () => (process.env.BETA_UNTIL || '2027-06-30').trim();
export const autoLicence = () => process.env.AUTO_LICENCE === '1';

// Numéro de licence : EYZ-XXXX-XXXX-XXXX-XXXX (même alphabet que new_licence_key en SQL).
export function newKey() {
  const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  let s = '';
  for (const b of bytes) s += alphabet[b % alphabet.length];
  return 'EYZ-' + s.slice(0, 4) + '-' + s.slice(4, 8) + '-' + s.slice(8, 12) + '-' + s.slice(12, 16);
}

/// Nombre de licences prod existantes et places restantes.
export async function places(supabase) {
  const { count, error } = await supabase.from('licences').select('key', { count: 'exact', head: true }).eq('channel', 'prod');
  if (error) throw error;
  return { used: count || 0, total: betaPlaces(), left: Math.max(0, betaPlaces() - (count || 0)) };
}

/// Attribue une licence à une demande. r = { name, email, kind, tablets }.
/// Renseigne r.licenceKey, r.resent, r.placesLeft. Avec force=false (mode
/// automatique), rien n'est créé s'il ne reste pas de place.
export const ALL_MODULES = ['jouer', 'apprendre', 'parler'];
/** La liste des mondes d'une licence, nettoyée ; vide ou absente = tout. */
export function modulesText(list) {
  const m = Array.isArray(list) ? list.map((x) => String(x).toLowerCase()).filter((x) => ALL_MODULES.includes(x)) : [];
  return (m.length ? m : ALL_MODULES).join(',');
}

export async function issueLicence(supabase, r, requestId, { force = false } = {}) {
  const maxTablets = Math.min(Math.max(1, r.tablets || 1), r.kind === 'famille' ? 2 : 10);

  // Déjà servie ? (même e-mail, demande traitée avec un numéro) : on renvoie la même.
  if (r.email) {
    const { data: prev } = await supabase.from('licence_requests')
      .select('licence_key').eq('email', r.email).eq('topic', 'licence').not('licence_key', 'is', null)
      .order('created_at', { ascending: false }).limit(1);
    if (prev && prev.length && prev[0].licence_key) {
      r.licenceKey = prev[0].licence_key;
      r.resent = true;
      await supabase.from('licence_requests').update({ status: 'done', licence_key: r.licenceKey, note: 'numéro déjà attribué, renvoyé' }).eq('id', requestId);
      return r;
    }
  }

  const p = await places(supabase);
  r.placesLeft = p.left;
  if (!force && p.left <= 0) return r; // demande gardée (status 'new')

  let key = '';
  for (let i = 0; i < 5 && !key; i++) {
    const k = newKey();
    const { error } = await supabase.from('licences').insert({
      key: k, family: r.name, channel: 'prod', max_tablets: maxTablets, enabled: true,
      modules: modulesText(opts.modules),
      expires_at: betaUntil() || null, note: (force ? 'validée depuis admin' : 'créée par le site (bêta)') + ' pour ' + (r.email || '?'),
    });
    if (!error) key = k; else if (error.code !== '23505') throw error;
  }
  if (!key) throw new Error('impossible de générer un numéro unique');
  r.licenceKey = key;
  r.placesLeft = Math.max(0, p.left - 1);
  await supabase.from('licence_requests').update({ status: 'done', licence_key: key, note: force ? 'validée depuis la page admin' : 'licence créée automatiquement' }).eq('id', requestId);
  return r;
}
