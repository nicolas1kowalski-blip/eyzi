# Eyzi sur le Microsoft Store : ce qu'il faut saisir

Brouillon à relire avant de le copier dans l'espace partenaires Microsoft. Les lignes
marquées **À CONFIRMER** attendent une réponse de l'éditeur.

## 1. L'identité du paquet (réglée)

Produit « EYZI » dans l'espace partenaires Microsoft, type « Application MSIX ou PWA ».

| | |
|---|---|
| `Package/Identity/Name` | `EyziNicolas.EYZI` |
| `Package/Identity/Publisher` | `CN=B18DAB22-112D-434B-846E-A74DBE2B760E` |
| `Package/Properties/PublisherDisplayName` | `EyziNicolas` |
| Package Family Name | `EyziNicolas.EYZI_c1hrq7b9m0bs8` |
| ID Store | `9N0PL01VFJLG` |

Ces valeurs sont dans `.github/workflows/build.yml`. Une fois le produit en ligne, l'adresse de la
fiche sera `https://apps.microsoft.com/detail/9N0PL01VFJLG`.

## 2. La fiche (français)

**Nom** : Eyzi

**Description courte** (une phrase) :
Jouer, apprendre et parler avec les yeux : une application pour les enfants de 4 à 7 ans qui pilotent la tablette à la commande oculaire, au doigt ou à la souris.

**Description** :

Eyzi est une application pour les enfants de 4 à 7 ans qui ne peuvent pas se servir de leurs mains. Elle se pilote au regard avec une commande oculaire Tobii, et aussi au doigt ou à la souris. Toute autre commande oculaire qui déplace la souris fonctionne en mode « Curseur ».

Trois mondes, dans une seule application :

- Jouer : ballons, paires, cherche et trouve, fête foraine, coloriage, peinture, collage, musique.
- Apprendre : des histoires illustrées, et des leçons qui suivent le programme de l'école maternelle (les nombres, les formes et les grandeurs), avec une explication animée avant chaque exercice.
- Parler : un tableau de communication en pictogrammes ARASAAC, avec le code couleur des classeurs de communication, des pages de mots et de verbes, et seize thèmes.

Eyzi est en version bêta, gratuite pendant la bêta (jusqu'au 30 juin 2027). L'application s'essaie 10 jours sans licence ; la licence se demande sur eyzi.fr.

Les pictogrammes sont ceux d'ARASAAC (auteur : Sergio Palao, origine : ARASAAC, https://arasaac.org, licence CC BY-NC-SA, propriété du Gouvernement d'Aragon).

**Ce que fait l'application** (liste à puces du Store) :
- Se pilote au regard, au doigt ou à la souris
- Tableau de communication en pictogrammes ARASAAC
- Leçons alignées sur le programme de l'école maternelle
- Histoires, jeux, coloriage, peinture, musique
- Voix naturelle hors ligne, livrée avec l'application
- Suivi des progrès pour les parents et l'école

**Mots-clés** : commande oculaire, regard, CAA, communication alternative, handicap, pictogrammes, maternelle

**Catégorie** : Éducation. **À CONFIRMER** : ne pas cocher la catégorie « Enfants » sans avoir lu ses règles (elles sont plus strictes).

**Captures d'écran** : au moins une, 1366 × 768 ou plus. À prendre sur la tablette : l'accueil, un jeu, une leçon, le tableau Parler.

**Logo de la fiche** : `desktop/store/fiche-300.png` (300 × 300).

**Site web** : https://eyzi.fr — **Contact** : contact@eyzi.fr

## 3. Les questions de la soumission

**Capacité restreinte « runFullTrust »** (justification, en anglais dans l'espace partenaires) :

> Eyzi is a classic desktop (WPF, .NET Framework) application packaged with MSIX. It needs full trust to talk to eye-tracker drivers (Tobii SDK native libraries), to run its bundled offline text-to-speech engine as a child process, and to read the family's own photos and voice recordings from the user's AppData folder.

**Notes pour la certification** (en anglais) :

> Eyzi can be tested without any account or licence key: on first launch, choose "Essayer 10 jours sans licence" (10-day trial, all features). The app is designed for eye-gaze control but works fully with mouse and touch. The settings panel opens with a one-second long press on the gear button (top right). Listening / AI answer features are off by default and need the family's own Azure Speech key; they are not required to test the app.

**Microphone et caméra** : déclarés dans le paquet. Le micro sert à l'écoute (« Dis Eyzi ») et à l'enregistrement des voix de la famille ; la caméra à « Regarde » et au miroir. Les deux sont éteints tant que la famille ne les active pas.

**Classification d'âge** : questionnaire à remplir dans l'espace partenaires (pas de violence, pas d'achats, pas de publicité, pas d'échanges entre utilisateurs).

**Visibilité de la première publication** : cachée, accessible seulement par lien direct, le temps de l'essayer sur une tablette.

## 4. La politique de confidentialité

Publiée : **https://eyzi.fr/confidentialite.html** (c'est l'adresse à saisir dans l'espace partenaires).
Le responsable nommé est « Eyzi » avec contact@eyzi.fr ; les hébergeurs sont cités sans pays
(Supabase, Vercel, OVH) ; la durée est « tant que la licence est utilisée, suppression sur demande ».
À préciser plus tard si besoin : le pays des serveurs et une durée chiffrée.

## 5. Les mises à jour du Store, sans rien déposer à la main

À chaque promotion en prod (`.github/workflows/promote.yml`), le paquet MSIX de la version promue est
envoyé au Store et part en certification. Microsoft le valide (quelques heures à quelques jours), puis
le Store met à jour les tablettes installées par le Store.

**État au 24 novembre 2026 : pas actif.** Le compte développeur d'Eyzi est un compte individuel, et
l'espace partenaires n'y propose pas l'onglet « Applications Microsoft Entra » (seul l'onglet
« Users » apparaît, même avec un annuaire Entra relié). Sans cet onglet, pas d'identifiants, donc pas
d'envoi automatique : le paquet se dépose à la main à chaque promotion. L'étape reste dans
`promote.yml`, endormie, au cas où Microsoft ouvre cette possibilité ou si le compte devient un compte
d'entreprise.

Cela demande quatre secrets GitHub (dépôt → Settings → Secrets and variables → Actions). Sans eux,
l'envoi ne se fait pas et la promotion reste ce qu'elle était.

| Secret GitHub | Où le trouver |
|---|---|
| `STORE_TENANT_ID` | Espace partenaires → Paramètres du compte → Utilisateurs → Applications Microsoft Entra : l'ID du locataire |
| `STORE_CLIENT_ID` | Même page : l'ID de l'application créée pour GitHub (rôle « Gestionnaire ») |
| `STORE_CLIENT_SECRET` | Même page : « Ajouter une clé » sur cette application (affichée une seule fois) |
| `STORE_SELLER_ID` | Espace partenaires → Paramètres du compte → Informations légales : l'ID vendeur |

Ces valeurs ne vont que dans les secrets GitHub : jamais dans le dépôt, ni dans une conversation.

Limites : la fiche (textes, images) se modifie toujours dans l'espace partenaires ; si une soumission
est déjà en cours de validation, l'envoi suivant est refusé (relancer la promotion une fois la
première validée).

## 6. Point bloquant : le regard dans la version du Store (24 novembre 2026)

Premier essai de la version du Store (2026.1123.261.0) sur une tablette à commande oculaire : aucun
moteur Tobii ne voit le capteur. Le journal montre « Appareils trouvés : aucun » pour le Stream Engine
de l'application comme pour celui de la tablette (4.23.0.3), et « Trackers trouvés : 0 » pour le Pro SDK.
La cause n'est pas établie ; l'hypothèse est que l'emballage MSIX coupe Eyzi du logiciel Tobii.

Tant que ce n'est pas réglé : pas de lien vers le Store sur le site ni dans le guide, pas de mail aux
familles. À vérifier d'abord : la version de l'installeur, sur la même tablette, voit-elle le capteur ?

