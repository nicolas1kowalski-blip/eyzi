// L'ÉCOUTE DU MONDE PARLER : la tablette envoie la phrase qu'un adulte vient de
// dire à l'enfant (texte seulement, jamais l'audio) ; on demande à Claude une
// reformulation très simple et trois à six réponses courtes à la première
// personne de l'enfant, que la tablette affiche en tuiles à regarder.
//
// Garde-fous : licence et tablette connues (table activations), 300 phrases par
// tablette et par jour (compteur en mémoire : par instance, indicatif), phrase
// limitée à 300 caractères. La clé Anthropic reste ici (ANTHROPIC_API_KEY).
//
// Trois demandes en plus de la question :
//  - « photo » + « vue » : une image JPEG (base64, 1024 px au plus) prise par la
//    tablette ; vue = "scene" (caméra arrière : ce que l'adulte montre) ou
//    "enfant" (caméra avant : l'enfant, pour lui proposer ce qu'il ressent
//    peut-être, à confirmer). L'image n'est ni gardée ni journalisée.
//  - « habitudes » : ce que l'enfant a déjà choisi pour des questions proches.
//  - « spontane » : « Je veux dire… », ce que l'enfant pourrait vouloir dire
//    maintenant, sans question de l'adulte.
//  - « rebond » + « reponse » : l'enfant vient de répondre ; que pourrait-il dire
//    ENSUITE (préciser, demander, poser une question à l'adulte) ? La
//    conversation se poursuit : le fil de la journée (dix échanges au moins,
//    avec qui parlait) est joint à chaque demande.
//
// RÉSERVÉ À L'ÉDITEUR pour l'instant : seules les licences du canal « test »
// (celles de l'éditeur) et les numéros listés dans PARLER_LICENCES (séparés par
// des virgules) obtiennent des réponses de Claude. Les familles (canal « prod »)
// reçoivent un refus poli et la tablette garde sa grille devinée, sans réseau.
import Anthropic from '@anthropic-ai/sdk';
import { supabaseAdmin } from '../lib/licences.js';

// Vercel : la fonction peut durer jusqu'à 30 s (l'appel à Claude prend 1 à 4 s).
export const maxDuration = 30;
export const config = { supportsResponseStreaming: true };

const MAX_PER_DAY = 300;
const counters = new Map(); // machine → { day, n }
const checked = new Map();  // key|machine → instant de la dernière vérification réussie (10 min)
const canal = new Map();    // key|machine → canal de la licence (pour le journal : qui consomme l'IA)

function clean(v, max) {
  return String(v ?? '').replace(/[\u0000-\u001f]/g, ' ').trim().slice(0, max);
}

// Le compteur d'IA par licence et par tablette, dans la table usage (activités « ia:… »), pour le tableau
// de bord de l'administration : qui consomme l'IA, combien, et qui a été refusé. Jamais le contenu.
const IA_LABELS = { 'ia:question': 'IA · questions', 'ia:photo': 'IA · photos', 'ia:spontane': 'IA · je veux dire', 'ia:rebond': 'IA · rebonds', 'ia:refus': 'IA · refus (licence prod)' };
async function compterIa(supabase, key, machine, activity, ms) {
  try {
    const day = new Date().toISOString().slice(0, 10);
    const { data: cur } = await supabase.from('usage').select('launches, seconds').eq('licence_key', key).eq('machine', machine).eq('day', day).eq('activity', activity).maybeSingle();
    await supabase.from('usage').upsert({
      licence_key: key, machine, day, activity, label: IA_LABELS[activity] || activity,
      launches: (cur?.launches || 0) + 1, seconds: (cur?.seconds || 0) + Math.round((ms || 0) / 1000), successes: 0, updated_at: new Date().toISOString(),
    }, { onConflict: 'licence_key,machine,day,activity' });
  } catch (e) { console.warn('parler compteur', String(e?.message || e).slice(0, 120)); }
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
{"type": "...", "simple": "...", "reponses": [{"texte": "...", "mot": "...", "picto": "...", "emoji": "...", "cle": "..."}]}

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
- ouverte : 6 à 8 réponses concrètes et probables pour cet enfant, en PRIORITÉ prises dans son vocabulaire (ci-dessous) et dans ce que la famille dit de lui, puis "Autre chose". Quand la question porte sur une chose précise (un parfum, une couleur, un plat, un jeu, un vêtement), les réponses sont les options les plus courantes et les plus évidentes pour un enfant, les plus probables d'abord : « quel parfum de yaourt ? » → chocolat, vanille, fraise, nature, abricot… ; « quelle couleur ? » → rouge, bleu, jaune, vert, rose…
- Un fragment qui prolonge la question d'avant (« ou du chocolat », « et de la vanille ? », « sinon dehors ») n'est JAMAIS une question fermée : c'est la question précédente complétée. Même type qu'elle, la nouvelle option en premier, puis les options précédentes utiles, puis "Autre chose".
- consigne : "D'accord", "Non", "Attends", puis 1 ou 2 réactions qui collent à ce qui est annoncé (« Pourquoi ? », « Après on joue ? », « Avec toi », « Pas tout de suite »). 5 au plus.
- autre (une remarque, un récit, une phrase adressée à l'enfant sans question) : 4 à 6 RÉACTIONS qu'un enfant pourrait avoir : « Super ! », « Encore », « Pourquoi ? », « Moi aussi », « Je t'aime », « Raconte encore », « Et après ? ». Si ce n'était vraiment pas pour l'enfant : 3 réponses, "D'accord", "Encore", "Je t'aime".
- UNE CASE INATTENDUE : pour ouverte, choix et spontane, l'avant-dernière réponse (juste avant "Autre chose") est une chose UTILE que la question n'appelle pas mais que l'enfant pourrait avoir besoin de dire maintenant : un besoin (« Je veux aller aux toilettes », « J'ai mal », « J'ai soif »), un ressenti (« Je suis fatiguée »), ou une question à l'adulte (« Et toi ? », « On y va quand ? »). Elle change selon le moment et le fil. Les cases proposées sont un point de départ, jamais un périmètre : l'enfant doit toujours pouvoir dire autre chose.
Jamais de réponse blessante ni dangereuse.

"mot" : le mot FRANÇAIS complet et sans ambiguïté de la chose à DESSINER, pour retrouver un pictogramme dans une banque française (ARASAAC) : souvent plus précis que "texte", parce que le texte est court et dépend de la question. « Nature » (parfum de yaourt) → "yaourt nature" ; « Chocolat » → "yaourt au chocolat" ou "chocolat" selon la question ; « Dehors » → "jardin" ou "parc" ; « Papa » → "papa" ; « Oui » → "oui". Un nom commun, au singulier, 1 à 3 mots, sans article.
"picto" : le NOM ANGLAIS simple de l'objet ou de l'action de la réponse, en minuscules, 1 ou 2 mots, au singulier, pour retrouver un pictogramme dans une banque anglaise (Mulberry Symbols) : "pasta", "yogurt", "toast", "hot chocolate", "hug", "sleep", "outside", "dad", "yes", "no". Pour "Autre chose" : "other".
"emoji" : un seul emoji courant qui illustre la réponse (secours si le picto manque).
"cle" : si la réponse correspond à un mot du vocabulaire de l'enfant (liste "cle: mot"), sa clé ; sinon "".

Le vocabulaire de l'enfant, le contexte (lieu, jour, ce qu'elle a fait, ce qu'elle choisit souvent) et, s'il y en a, les derniers échanges de la journée (qui a parlé : l'adulte, ou l'enfant de lui-même ; il y a combien de minutes ; et quand l'enfant est sorti des cases avec « Autre chose » ou « Pas ça », signe que les propositions ne lui convenaient pas), sont donnés dans le message. Suis le FIL : une question qui revient sur ce qui vient d'être dit (« Et après ? », « Lequel ? ») se comprend avec l'échange d'avant ; ce que l'enfant a dit il y a cinq minutes (« J'ai soif ») compte encore ; ce qui date de ce matin compte moins. Le lieu change tout : à l'école, les réponses parlent des copains, de la maîtresse, de la récré, de la cantine ; à la maison, de la famille, des jouets, des repas ; dehors ou en voiture, de ce qu'on voit et où l'on va. Les réponses qu'elle choisit souvent passent en premier quand elles conviennent à la question. Si le message donne ce qu'elle a DÉJÀ CHOISI pour des questions proches (« habitudes »), ces réponses viennent en PREMIER dès qu'elles conviennent à la question : ce sont ses goûts à elle.`;

const PHOTO_SCENE = `
UNE PHOTO EST JOINTE : ce que l'adulte montre, ou ce qui entoure l'enfant (caméra arrière de la tablette). Les réponses s'appuient D'ABORD sur ce qu'on y voit : les objets, les aliments, les jouets, les vêtements, la page du livre, le lieu. Si l'adulte tient ou montre plusieurs choses, CHAQUE chose est une réponse, nommée simplement (« Le yaourt », « La compote »), les plus visibles d'abord, puis « Les deux » si ça a un sens, puis « Autre chose ». Si la question est ouverte (« qu'est-ce que tu vois ? »), 6 à 8 réponses sur ce que l'enfant peut vouloir dire de la photo (« Un chien ! », « Il est gros », « J'ai peur », « Encore »). "simple" peut nommer ce qu'on voit (« Tu veux le yaourt ou la compote ? »). Ne nomme JAMAIS une personne, ne décris ni visage ni apparence ni âge : dis « maman », « papa », « quelqu'un », « la dame ». Si la photo ne montre rien d'utile, réponds comme sans photo.`;

const PHOTO_ENFANT = `
UNE PHOTO EST JOINTE : c'est L'ENFANT lui-même (caméra avant de la tablette). Il ne parle pas. Propose-lui, à la première personne, 5 ou 6 ressentis POSSIBLES qu'il confirmera ou non avec ses yeux : d'abord ce que son visage et sa posture PEUVENT suggérer (sourire → « Je suis contente », yeux qui se ferment ou tête penchée → « Je suis fatiguée », grimace ou larmes → « J'ai mal » ou « Je suis triste »), puis le contraire ou une nuance, puis « Ça va », puis « Autre chose ». Prudence absolue : tu PROPOSES, tu n'affirmes rien ; l'enfant a le dernier mot. "type" = "humeur". "simple" = une question douce à l'enfant (« Tu es fatiguée ? », « Comment tu te sens ? »), jamais une affirmation sur son état. Ne décris pas son visage, ne juge pas, n'invente aucun diagnostic. "mot" pour chaque ressenti : "content", "fatigue", "douleur", "triste", "câlin", "calme"…`;

const SYSTEM_SPONTANE = `Tu aides un enfant qui ne parle pas et qui s'exprime avec ses yeux sur une tablette, en regardant une case parmi quelques-unes. Personne ne lui a posé de question : c'est LUI qui veut dire quelque chose, maintenant. Tu renvoies UNIQUEMENT un objet JSON, sans texte autour, de la forme :
{"type": "spontane", "simple": "Je veux dire…", "reponses": [{"texte": "...", "mot": "...", "picto": "...", "emoji": "...", "cle": "..."}]}

"reponses" : 8 phrases courtes (2 à 6 mots) à la première personne, ce que cet enfant a le plus de chances de vouloir dire À CE MOMENT : d'après le lieu, l'heure et le moment de la journée, le jour, ce que la famille dit de lui, ce qu'il choisit souvent, ce qu'il a fait aujourd'hui et le fil des derniers échanges. Mélange : des besoins (« Je veux aller aux toilettes », « J'ai soif », « J'ai mal », « Je suis fatiguée »), des envies (« Je veux jouer dehors », « Je veux une histoire », « Encore de la musique »), du lien (« Je veux jouer avec Inès », « Je veux ma maman », « Je t'aime »), UNE OU DEUX QUESTIONS que l'enfant pose à l'adulte (« On fait quoi après ? », « Maman arrive quand ? », « Tu restes avec moi ? », « Et toi ? »), et une ou deux propositions qui collent précisément au moment (à 16 h : « Je veux mon goûter » ; le soir : « Je veux mon doudou » ; à l'école : « Je veux jouer avec un copain »). Les plus probables d'abord. La dernière est "Autre chose". Jamais de phrase blessante ni dangereuse.
"mot" : le mot FRANÇAIS complet de la chose à dessiner (« toilettes », « goûter », « câlin », « peluche »), 1 à 3 mots, sans article. "picto" : le mot ANGLAIS simple correspondant (« toilet », « snack », « hug »). "emoji" : un seul emoji. "cle" : la clé du vocabulaire de l'enfant si la phrase y correspond, sinon "".`;

const SYSTEM_REBOND = `Tu aides un enfant qui ne parle pas et qui s'exprime avec ses yeux sur une tablette, en regardant une case parmi quelques-unes. Un adulte lui a parlé, l'enfant VIENT DE RÉPONDRE en regardant une case. Tu proposes maintenant ce que l'enfant pourrait vouloir dire ENSUITE, pour que l'échange continue comme une vraie conversation. Tu renvoies UNIQUEMENT un objet JSON, sans texte autour, de la forme :
{"type": "rebond", "simple": "...", "reponses": [{"texte": "...", "mot": "...", "picto": "...", "emoji": "...", "cle": "..."}]}

"simple" : ce que l'enfant vient de dire, en une phrase courte à la première personne.
"reponses" : 5 ou 6 phrases courtes (1 à 6 mots) à la première personne, variées, dans l'ordre le plus utile :
- une ou deux PRÉCISIONS de sa réponse (« Du jus » → « Avec une paille », « Dans mon verre rose » ; « Dehors » → « Au parc », « Avec mon vélo ») ;
- une ENVIE ou un besoin qui va avec (« Et un gâteau », « Vite, j'ai soif ») ;
- une ou deux QUESTIONS À L'ADULTE (« Et toi ? », « On y va quand ? », « Tu viens avec moi ? », « Pourquoi ? ») ;
- un mot de LIEN ou de ressenti (« Merci », « Je suis contente », « Je t'aime »), quand ça a un sens ;
- une case INATTENDUE mais utile maintenant (un besoin, un ressenti, une question sur la suite), différente de ce que la question appelait.
Pas de "Oui" ni "Non", pas de "Autre chose" ni "Fini" (la tablette les ajoute). Jamais de phrase blessante ni dangereuse. Les réponses doivent être réalistes pour un enfant de cet âge, cohérentes avec le lieu, le moment et le fil de la journée, et tenir compte de ce que la famille dit de lui et de ce qu'il choisit souvent. Si l'enfant a déjà dit cette chose dans le fil, ne la repropose pas.
"mot" : le mot FRANÇAIS complet de la chose à dessiner (1 à 3 mots, sans article). "picto" : le mot ANGLAIS simple correspondant. "emoji" : un seul emoji. "cle" : la clé du vocabulaire de l'enfant si la phrase y correspond, sinon "".`;

export default async function handler(req, res) {
  const tStart = Date.now();
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
  const lieu = clean(body.lieu, 60);
  const jour = clean(body.jour, 60);
  const activites = Array.isArray(body.activites) ? body.activites.slice(0, 8).map((a) => clean(a, 40)).filter(Boolean) : [];
  const preferees = Array.isArray(body.preferees) ? body.preferees.slice(0, 15).map((a) => clean(a, 40)).filter(Boolean) : [];
  const vocab = Array.isArray(body.vocab) ? body.vocab.slice(0, 120).map((v) => ({ cle: clean(v?.cle, 40), mot: clean(v?.mot, 40) })).filter((v) => v.cle && v.mot) : [];
  const historique = Array.isArray(body.historique) ? body.historique.slice(-12).map((h) => ({
    q: clean(h?.q, 200), r: clean(h?.r, 60), t: clean(h?.t, 12), qui: h?.qui === 'enfant' ? 'enfant' : 'adulte', ecart: h?.ecart === 'autre' ? 'autre' : h?.ecart === 'pasca' ? 'pasca' : '',
    il: Math.max(0, Math.min(1440, parseInt(h?.il, 10) || 0)), p: Array.isArray(h?.p) ? h.p.slice(0, 8).map((x) => clean(x, 40)).filter(Boolean) : [],
  })).filter((h) => h.q) : [];
  const suite = body.suite === true && historique.length > 0;
  const spontane = body.spontane === true;
  const rebond = body.rebond === true && !spontane;
  const reponse = clean(body.reponse, 80);
  const habitudes = Array.isArray(body.habitudes) ? body.habitudes.slice(0, 10).map((h) => ({ q: clean(h?.q, 80), r: clean(h?.r, 60), n: Math.max(1, Math.min(999, parseInt(h?.n, 10) || 1)) })).filter((h) => h.q && h.r) : [];
  const vue = body.vue === 'enfant' ? 'enfant' : body.vue === 'scene' ? 'scene' : '';
  // La photo : JPEG en base64, 700 000 caractères au plus (≈ 500 Ko), jamais journalisée.
  let photo = typeof body.photo === 'string' ? body.photo.replace(/^data:image\/\w+;base64,/, '').replace(/\s+/g, '') : '';
  if (photo.length > 700000 || !/^[A-Za-z0-9+/]+=*$/.test(photo)) photo = '';
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
      if (lic.channel !== 'test' && !allowed.includes(key)) {
        console.log('parler refus', 'canal', lic.channel, 'licence …' + key.slice(-6), 'tablette', machine);
        await compterIa(supabase, key, machine, 'ia:refus', 0);
        res.status(403).json({ ok: false, message: "Les réponses proposées sont réservées à l'éditeur pendant la bêta." }); return;
      }
      canal.set(ck, lic.channel + (allowed.includes(key) ? '+liste' : ''));
      const { data: act } = await supabase.from('activations').select('machine, enabled').eq('licence_key', key).eq('machine', machine).maybeSingle();
      if (!act || act.enabled === false) { res.status(403).json({ ok: false, message: 'Tablette inconnue pour cette licence.' }); return; }
      checked.set(ck, Date.now());
    }
    if (tooMany(machine)) { res.status(429).json({ ok: false, message: 'Trop de demandes aujourd\'hui.' }); return; }

    // Une clé « toute organisation » doit nommer son espace de travail (ANTHROPIC_WORKSPACE_ID, wrkspc_…) ;
    // une clé créée dans un espace de travail n'en a pas besoin.
    const workspace = String(process.env.ANTHROPIC_WORKSPACE_ID || '').trim();
    const client = new Anthropic({ apiKey, ...(workspace ? { defaultHeaders: { 'anthropic-workspace-id': workspace } } : {}) });
    // La forme d'appel qui a fonctionné (journal du 30/10, 19 h 25) : point d'entrée bêta.
    const t0 = Date.now();
    const contexte = [];
    if (lieu) contexte.push(`Lieu : ${lieu}.`);
    if (jour) contexte.push(`Jour : ${jour}.`);
    if (activites.length) contexte.push(`Ce que l'enfant a fait aujourd'hui sur la tablette : ${activites.join(', ')}.`);
    if (preferees.length) contexte.push(`Réponses qu'elle choisit souvent : ${preferees.join(', ')}.`);
    if (habitudes.length) contexte.push(`Ce qu'elle a déjà choisi pour des questions proches (à mettre en premier si ça convient) : ${habitudes.map((h) => `« ${h.q} » → ${h.r} (${h.n} fois)`).join(' ; ')}.`);
    const userText =
      (vocab.length ? `Vocabulaire de l'enfant :\n${vocab.map((v) => `${v.cle}: ${v.mot}`).join('\n')}\n\n` : '') +
      (contexte.length ? `Contexte :\n${contexte.join('\n')}\n\n` : '') +
      (historique.length ? `Le fil de la journée (du plus ancien au plus récent) :\n${historique.map((h) => {
        const quand = h.il < 2 ? "à l'instant" : h.il < 60 ? `il y a ${h.il} min` : `il y a ${Math.round(h.il / 60)} h`;
        const ecart = h.ecart === 'autre' ? " (l'enfant a demandé « Autre chose » : aucune case ne convenait)" : h.ecart === 'pasca' ? " (l'enfant a dit « Pas ça » : aucune case ne convenait)" : '';
        if (h.qui === 'enfant' && h.t === 'rebond') return `[${quand}] L'enfant a ajouté de lui-même : « ${h.r || '(rien)'} »${ecart}`;
        if (h.qui === 'enfant') return `[${quand}] L'enfant a voulu dire (« Je veux dire… ») : « ${h.r || '(rien)'} »${h.p.length ? ` (cases proposées : ${h.p.join(', ')})` : ''}${ecart}`;
        return `[${quand}] Adulte : « ${h.q} »${h.p.length ? ` → cases proposées : ${h.p.join(', ')}` : ''} → l'enfant a répondu : « ${h.r || '(rien)'} »${ecart}`;
      }).join('\n')}\n\n` : '') +
      (suite ? `ATTENTION : ce que l'adulte vient de dire est la SUITE de sa question précédente « ${historique[historique.length - 1].q} » (il ajoute ou précise une option). Ce n'est PAS une question fermée. Garde le type de la question précédente (${historique[historique.length - 1].t || 'choix'}), mets d'abord la nouvelle option, puis les cases précédentes encore utiles, puis "Autre chose".\n\n` : '') +
      (spontane ? `L'enfant veut dire quelque chose maintenant. Propose ce qu'il a le plus de chances de vouloir dire.`
        : rebond ? `L'adulte avait dit : « ${question} ». L'enfant vient de répondre : « ${reponse} ». Que pourrait-il vouloir dire ensuite ?`
        : photo ? `L'adulte vient de dire : « ${question} » (la photo jointe montre ${vue === 'enfant' ? "l'enfant" : "ce qu'il montre ou ce qui entoure l'enfant"}).`
        : `L'adulte vient de dire : « ${question} »`);
    const userContent = photo
      ? [{ type: 'image', source: { type: 'base64', media_type: 'image/jpeg', data: photo } }, { type: 'text', text: userText }]
      : userText;
    const systemText = (spontane ? SYSTEM_SPONTANE : rebond ? SYSTEM_REBOND : SYSTEM + (photo ? (vue === 'enfant' ? PHOTO_ENFANT : PHOTO_SCENE) : ''))
      + `\nL'enfant s'appelle ${child} et a ${age} ans. Il est ${heure} h : c'est ${moment || 'la journée'}.` + (profil ? `\nCe que la famille dit de ${child} : ${profil}` : '');
    const request = {
      model: 'claude-haiku-5-5',
      max_tokens: 1500,
      betas: ['server-side-fallback-2026-07-01'],
      fallbacks: 'default',
      output_config: { effort: 'low' },
      system: systemText,
      messages: [{ role: 'user', content: userContent }],
    };

    // En FLUX (une ligne JSON par réponse) : la tablette affiche chaque case dès qu'elle est écrite,
    // sans attendre la fin. Les lignes : {"type","simple"} puis {"reponse":{…}}… puis {"fin":true,"ms":{…}}.
    res.writeHead(200, { 'Content-Type': 'application/x-ndjson; charset=utf-8', 'Cache-Control': 'no-store', 'X-Accel-Buffering': 'no' });
    let text = '', sent = 0, headerSent = false, nb = 0;
    const emit = (obj) => res.write(JSON.stringify(obj) + '\n');
    const objRegex = /\{[^{}]*\}/g;
    const getStr = (src, k) => { const x = src.match(new RegExp('"' + k + '"\\s*:\\s*"((?:[^"\\\\]|\\\\.)*)"')); return x ? x[1] : ''; };
    const flush = () => {
      if (!headerSent) {
        const ty = text.match(/"type"\s*:\s*"([a-z]+)"/), si = text.match(/"simple"\s*:\s*"((?:[^"\\]|\\.)*)"/);
        if (ty && si) { headerSent = true; emit({ type: ty[1], simple: si[1] }); }
      }
      if (!headerSent) return;
      const start = text.indexOf('"reponses"');
      if (start < 0) return;
      const objs = text.slice(start).match(objRegex) || [];
      for (; sent < objs.length && nb < 8; sent++) {
        const o = objs[sent];
        const texte = getStr(o, 'texte');
        if (!texte) continue;
        nb++;
        emit({ reponse: { texte, mot: getStr(o, 'mot'), picto: getStr(o, 'picto'), emoji: getStr(o, 'emoji'), cle: getStr(o, 'cle') } });
      }
    };
    const stream = client.beta.messages.stream(request);
    stream.on('text', (delta) => { text += delta; flush(); });
    const final = await stream.finalMessage();
    flush();
    const msIa = Date.now() - t0;
    console.log('parler', spontane ? 'spontane' : rebond ? 'rebond' : photo ? 'photo-' + vue : 'question', msIa, 'ms', final.stop_reason, text.length, nb, 'réponses', 'canal', canal.get(ck) || '?', 'licence …' + key.slice(-6));
    if (!headerSent) emit({ type: spontane ? 'spontane' : rebond ? 'rebond' : vue === 'enfant' ? 'humeur' : 'autre', simple: spontane ? 'Je veux dire…' : rebond ? reponse : question });
    await compterIa(supabase, key, machine, spontane ? 'ia:spontane' : rebond ? 'ia:rebond' : photo ? 'ia:photo' : 'ia:question', msIa);
    emit({ fin: true, ms: { ia: msIa, total: Date.now() - tStart }, stop: final.stop_reason, n: nb });
    res.end();
    return;
  } catch (e) {
    console.error('parler', e);
    const detail = String(e?.message || e).slice(0, 200);
    if (res.headersSent) { try { res.write(JSON.stringify({ erreur: 'Erreur du serveur : ' + detail }) + '\n'); res.end(); } catch {} return; }
    res.status(500).json({ ok: false, message: 'Erreur du serveur : ' + detail });
  }
}
