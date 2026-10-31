# Eyzi

**Communication alternative et jeux inclusifs, pilotés par le regard.** Application Windows pour les enfants de 4 à 7 ans qui ne peuvent pas utiliser leurs mains : avec la commande oculaire (Tobii), ils jouent, choisissent, parlent et apprennent. Site : https://eyzi.fr

Ce dépôt est public : il contient le code, et ses [versions publiées](https://github.com/nicolas1kowalski-blip/eyzi/releases) (l'installeur), que l'application interroge pour se mettre à jour. Aucune clé ni aucun secret n'y est stocké.

## Contenu

- `desktop/MesPremiersJeux/` : l'application (C# / WPF, .NET Framework 4.7.2, x64). L'espace de noms du code garde l'ancien nom ; le produit s'appelle Eyzi.
- `desktop/installer/Eyzi.iss` : l'installeur Inno Setup (par utilisateur, sans droits administrateur).
- `desktop/docs/` : `GUIDE-UTILISATEUR.md`, `LANCEMENT.md` (toutes les étapes de mise en ligne), `ETUDE-DE-MARCHE.md`, `supabase.sql` (le serveur : licences, activations, usage, demandes).
- `desktop/brand/eyzi/` : logo, icône, favicon.
- `site/` : eyzi.fr (pages statiques, `api/licence-request.js` pour le formulaire, `vercel.json`). Dans Vercel, le « Root Directory » du projet est `site`.
- `.github/workflows/build.yml` : à chaque poussée sur `main` touchant `desktop/`, compilation Windows, installeur, et publication d'une **version test** (pré-version) dans les Releases de ce dépôt. Promouvoir en **prod** : sur la version, décocher « Set as a pre-release ».

## Variables Vercel (projet `site`)

- `SUPABASE_URL`, `SUPABASE_SERVICE_ROLE_KEY` : le projet Supabase (tables `licence_requests`, `licences`…).

## Licence

Code source visible, tous droits réservés : voir `LICENSE`. Lire, vérifier et proposer des corrections est bienvenu ; copier, redistribuer ou dériver ne l'est pas sans accord écrit (contact@eyzi.fr).
