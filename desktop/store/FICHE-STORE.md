# Eyzi sur le Microsoft Store : ce qu'il faut saisir

Brouillon à relire avant de le copier dans l'espace partenaires Microsoft. Les lignes
marquées **À CONFIRMER** attendent une réponse de l'éditeur.

## 1. L'identité du paquet (à me transmettre)

Espace partenaires → l'application → *Gestion du produit* → *Identité du produit*. Trois valeurs :

| Dans l'espace partenaires | Variable GitHub |
|---|---|
| `Package/Identity/Name` | `STORE_NAME` |
| `Package/Identity/Publisher` (commence par `CN=`) | `STORE_PUBLISHER` |
| `Package/Properties/PublisherDisplayName` | `STORE_PUBLISHER_DISPLAY` |

Elles ne sont pas secrètes. Elles se règlent dans GitHub → Settings → Secrets and variables →
Actions → onglet *Variables*. Sans elles, le paquet porte une identité provisoire que le Store refuse.

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

## 4. La politique de confidentialité (obligatoire : il faut une adresse publique)

À publier sur https://eyzi.fr/confidentialite.html une fois les trois points ci-dessous confirmés.
Tout le reste vient du guide utilisateur (partie « Les données et la sécurité »).

**À CONFIRMER avant publication :**
1. Qui est nommé comme responsable des données : votre nom, ou seulement « Eyzi » avec contact@eyzi.fr ?
2. Dans quel pays sont les serveurs (région du projet Supabase) ?
3. Combien de temps sont gardés les compteurs d'usage et les demandes de licence ?

**Texte proposé :**

### Ce qui reste sur la tablette
Les réglages, les photos de famille, les voix enregistrées, les livres, le journal et les progrès de l'enfant restent sur la tablette.

### Ce qui part, et vers qui
- **La licence** : le numéro de licence, le nom de la tablette et la version de l'application sont envoyés au serveur d'Eyzi pour vérifier la licence.
- **Les compteurs d'usage** : par jour et par activité, le nombre d'ouvertures, de réussites et de minutes, avec le nom de la tablette. Ni prénom, ni photo, ni voix, ni ce qui a été dit. Pendant l'essai de 10 jours sans licence, ces compteurs partent sous un identifiant de tablette haché, sans nom de machine ni de famille.
- **Le nuage de la famille** (facultatif) : les contenus partagés entre les tablettes d'une même famille sont chiffrés (AES-256) avec le mot de passe famille avant de partir. Le serveur et l'éditeur ne voient qu'un fichier illisible.
- **L'écoute** (facultative, éteinte au départ) : la voix est transcrite par le service Azure Speech de Microsoft, avec la clé que la famille a créée elle-même ; l'éditeur ne connaît pas cette clé. Le son ne va jamais à eyzi.fr.
- **Les réponses proposées par l'intelligence artificielle** (facultatives) : pour proposer des réponses, la tablette envoie à eyzi.fr le texte de la question, le prénom, l'âge et le profil de l'enfant saisis par la famille, son vocabulaire, le contexte (lieu, heure, jour, activités, habitudes) et les derniers échanges de la journée. eyzi.fr les transmet à un prestataire d'intelligence artificielle (Anthropic) pour obtenir les réponses. Ces éléments ne sont pas enregistrés par eyzi.fr ; seul un compteur d'usage est tenu. **À CONFIRMER** : la durée de conservation chez le prestataire, d'après votre contrat avec lui.
- **La caméra** (facultative, éteinte au départ) : une photo peut être envoyée à eyzi.fr avec la question, et transmise au même prestataire. Elle n'est jamais enregistrée, ni sur la tablette, ni sur le site, ni dans les journaux.
- **Les demandes de licence et les retours** envoyés depuis eyzi.fr : le nom, l'adresse e-mail et le message saisis dans le formulaire.

### Ce que l'application ne fait pas
Pas de publicité, pas de revente de données, pas de suivi publicitaire.

### Vos droits
Pour consulter, corriger ou faire supprimer vos données : contact@eyzi.fr.
