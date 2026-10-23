// Envoi d'e-mails depuis contact@eyzi.fr via le serveur SMTP de la boîte (OVH).
// Variables d'environnement (Vercel → Settings → Environment Variables) :
//   SMTP_USER  = contact@eyzi.fr            (identifiant de la boîte)
//   SMTP_PASS  = mot de passe de la boîte   (secret, uniquement dans Vercel)
//   SMTP_HOST  = ssl0.ovh.net (défaut)      SMTP_PORT = 465 (défaut)
//   NOTIFY_TO  = adresse qui reçoit les demandes (défaut : SMTP_USER)
// Sans SMTP_USER/SMTP_PASS, aucun e-mail n'est envoyé (la demande reste
// enregistrée dans Supabase, et la réponse du formulaire le signale).
import nodemailer from 'nodemailer';

export function mailConfigured() {
  return Boolean(process.env.SMTP_USER && process.env.SMTP_PASS);
}

function transport() {
  const port = parseInt(process.env.SMTP_PORT || '465', 10);
  return nodemailer.createTransport({
    host: process.env.SMTP_HOST || 'ssl0.ovh.net',
    port,
    secure: port === 465,
    auth: { user: process.env.SMTP_USER, pass: process.env.SMTP_PASS },
    connectionTimeout: 8000,
    greetingTimeout: 8000,
    socketTimeout: 12000,
  });
}

const esc = (s) => String(s || '').replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

/// Prévient l'éditeur d'une nouvelle demande (licence ou retour).
export async function notifyEditor(r) {
  const to = process.env.NOTIFY_TO || process.env.SMTP_USER;
  const isLic = r.topic === 'licence';
  const subject = isLic
    ? `[Eyzi] Demande de licence — ${r.name} (${r.kind}, ${r.tablets} tablette${r.tablets > 1 ? 's' : ''})`
    : `[Eyzi] Retour d'utilisateur — ${r.name || 'anonyme'}`;
  const rows = [
    ['Type', isLic ? 'Demande de licence' : "Retour d'utilisateur"],
    ['Nom', r.name], ['E-mail', r.email || '—'], ['Profil', r.kind], ['Établissement', r.org || '—'],
    ['Enfant', r.child || '—'], ['Commande oculaire', r.gear || '—'], ['Tablettes', String(r.tablets)],
    ['Recontact pour avis', r.recontact ? 'oui' : 'non'], ['Message', r.message || '—'],
  ];
  const admin = 'https://eyzi.fr/admin.html';
  if (isLic) {
    rows.push(['Licence', r.licenceKey
      ? (r.resent ? `${r.licenceKey} (déjà attribuée à cette adresse, renvoyée)` : `${r.licenceKey} — créée et envoyée automatiquement`)
      : 'EN ATTENTE de votre validation']);
    if (r.placesLeft != null) rows.push(['Places bêta restantes', String(r.placesLeft)]);
  }
  const text = rows.map(([k, v]) => `${k} : ${v}`).join('\n') +
    (isLic && !r.licenceKey ? `\n\nValider ou refuser : ${admin}\n(« Valider » crée la licence et l'envoie à la famille.)\n` : '') +
    `\nPour répondre à ${r.email || '(pas d\'adresse)'} : répondez simplement à cet e-mail.`;
  const html = `<h2 style="font-family:sans-serif">${esc(subject)}</h2>` +
    `<table style="font-family:sans-serif;border-collapse:collapse">` +
    rows.map(([k, v]) => `<tr><td style="padding:4px 10px 4px 0;color:#555"><b>${esc(k)}</b></td><td style="padding:4px 0;white-space:pre-wrap">${esc(v)}</td></tr>`).join('') +
    `</table>` +
    (isLic && !r.licenceKey ? `<p style="font-family:sans-serif"><a href="${admin}" style="display:inline-block;background:#1F5EFF;color:#fff;padding:10px 18px;border-radius:10px;text-decoration:none;font-weight:bold">Valider ou refuser la demande</a><br><small>« Valider » crée la licence et l'envoie à la famille.</small></p>` : '') +
    (r.email ? `<p style="font-family:sans-serif">Répondre : <a href="mailto:${esc(r.email)}">${esc(r.email)}</a></p>` : '');
  await transport().sendMail({
    from: `"Eyzi — site" <${process.env.SMTP_USER}>`, to, subject, text, html,
    replyTo: r.email || undefined,
  });
}

/// Accusé de réception à la famille (demande de licence seulement).
export async function acknowledge(r) {
  if (!r.email) return;
  const first = (r.name || '').trim().split(/\s+/)[0] || '';
  const text =
`Bonjour${first ? ' ' + first : ''},

Nous avons bien reçu votre demande de licence pour Eyzi${r.child ? ' (pour ' + r.child + ')' : ''}.

Eyzi est en bêta gratuite et limitée : nous ouvrons les places par petits groupes pour accompagner chaque famille. Vous recevrez votre numéro de licence (EYZ-…) par e-mail, en général sous quelques jours. S'il n'y a plus de place pour le moment, nous vous le dirons et garderons votre demande pour la vague suivante.

En attendant, vous pouvez déjà installer l'application et l'essayer 10 jours sans licence : https://eyzi.fr
Le guide : https://eyzi.fr/guide.html

Une question ? Répondez simplement à ce message.

L'équipe Eyzi
contact@eyzi.fr`;
  await transport().sendMail({
    from: `"Eyzi" <${process.env.SMTP_USER}>`, to: r.email, subject: 'Votre demande de licence Eyzi est bien reçue', text,
  });
}

/// La famille reçoit son numéro de licence et la marche à suivre.
export async function sendLicence(r) {
  if (!r.email || !r.licenceKey) return;
  const first = (r.name || '').trim().split(/\s+/)[0] || '';
  const text =
`Bonjour${first ? ' ' + first : ''},

Bienvenue dans la bêta d'Eyzi ! Voici votre numéro de licence :

    ${r.licenceKey}

Pour l'installer :
1. Téléchargez l'application sur https://eyzi.fr (bouton « Télécharger ») et installez-la sur la tablette de l'enfant.
2. Au premier démarrage, entrez ce numéro, puis choisissez un mot de passe famille : il protège vos photos et vos voix, et il doit être le même sur chaque tablette (${r.tablets > 1 ? r.tablets + ' tablettes' : '1 tablette'} avec cette licence).
3. Le guide pas à pas : https://eyzi.fr/guide.html

La licence est gratuite pendant toute la bêta (jusqu'au 30 juin 2027). Si Eyzi devenait payant un jour, les familles de la bêta garderaient leur licence gratuitement.

Un problème, une idée ? Répondez simplement à ce message, ou passez par https://eyzi.fr/retours.html. Vos retours font l'application.

L'équipe Eyzi
contact@eyzi.fr`;
  await transport().sendMail({
    from: `"Eyzi" <${process.env.SMTP_USER}>`, to: r.email, subject: 'Votre licence Eyzi : ' + r.licenceKey, text,
  });
}

/// Réponse polie quand l'éditeur refuse (ou n'a pas de place) — texte libre ajouté.
export async function refuse(r, note) {
  if (!r.email) return;
  const first = (r.name || '').trim().split(/\s+/)[0] || '';
  const text =
`Bonjour${first ? ' ' + first : ''},

Merci pour votre demande de licence Eyzi. Nous ne pouvons malheureusement pas vous attribuer de licence pour le moment : la bêta est limitée à un petit nombre de familles, pour pouvoir accompagner chacune.
${note ? '\n' + note.trim() + '\n' : ''}
Vous pouvez toujours installer l'application et l'essayer 10 jours sans licence : https://eyzi.fr
Si une place se libère ou si la bêta s'élargit, nous vous recontacterons à cette adresse.

L'équipe Eyzi
contact@eyzi.fr`;
  await transport().sendMail({
    from: `"Eyzi" <${process.env.SMTP_USER}>`, to: r.email, subject: 'Votre demande de licence Eyzi', text,
  });
}
