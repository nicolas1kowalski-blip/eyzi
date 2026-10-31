# Eyzi — notes pour l'éditeur

Ce document n'est pas publié sur le site. Il reprend la section « Pour l'éditeur » retirée du guide utilisateur.

## Donner l'application à une autre famille : donner l'application à une autre famille

1. **Créer une licence** dans le serveur (Supabase → SQL) :
   `select public.create_licence('Famille Martin', 'prod', 2);` — le nom, le canal (`test` ou `prod`), le nombre de tablettes, une date de fin facultative en 4ᵉ paramètre, et en 5ᵉ les mondes compris (`'jouer,apprendre,parler'` par défaut ; par exemple `'jouer,apprendre'` pour une licence sans le monde Parler). La fonction renvoie le numéro `EYZ-…` à transmettre. Pour changer les mondes d'une licence existante : `update public.licences set modules = 'jouer,parler' where key = 'EYZ-…';` ou, plus simple, la page admin du site (cases à cocher puis « Enregistrer les mondes »).
2. **Donner** à la famille : l'adresse eyzi.fr, le numéro de licence, ce guide (eyzi.fr/guide.html). Elle choisit elle-même son mot de passe famille.
3. **Publier une version** : chaque poussée de code compile une version *test* (pré-version) sur GitHub. Pour la passer en *prod* : GitHub → Releases → Edit → décocher « Set as a pre-release » → cocher « Set as the latest release ». Les tablettes prod la reçoivent au prochain démarrage.
4. **Révoquer** une licence : dans la table `licences`, passer `enabled` à `false` ; la tablette perd l'accès au plus tard 30 jours après (ou à sa prochaine vérification en ligne).
5. **Désactiver une tablette** (perdue, remplacée, prêtée) : `select public.disable_tablet('EYZ-…', 'NOM-DE-LA-TABLETTE');` (même fonction avec `true` en 3ᵉ paramètre pour la réactiver). La vue `tablettes` liste toutes les tablettes par famille, avec leur version et leur dernière connexion. La limite est stricte : une tablette de plus que `max_tablets` est refusée tant qu'une place n'est pas libérée.
6. **Voir ce qui est utilisé**, par famille : la vue `usage_par_famille` (du plus utilisé au moins utilisé : minutes, ouvertures, réussites, jours), `usage_30_jours` (les 30 derniers jours) et `usage_resume` (une ligne par famille : minutes, jours, tablettes, dernière fois). Dans Supabase : Table Editor → choisir la vue, ou SQL : `select * from public.usage_30_jours where famille = 'Famille Martin';`

Les fichiers utiles : `desktop/docs/supabase.sql` (le serveur), `desktop/installer/MesPremiersJeux.iss` (l'installeur), `.github/workflows/build.yml` (la compilation automatique).
