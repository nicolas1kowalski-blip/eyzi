# Les jeux « dessinés » et leur banc d'essai

« Le jet d'eau », « La pêche » et « Patatou » sont faits des mêmes morceaux :

- **Les dessins** : `../jet.js`, `../peche.js`, `../patate.js` (et `../communs.js` pour ce qu'ils partagent, `../outils.js` pour les outils de dessin).
  `node ../genere.js` en tire `MesPremiersJeux/Games/JetArt.cs`, `PecheArt.cs` et `PatateArt.cs`.
- **Le moteur** de chaque jeu : `MesPremiersJeux/Games/JetMoteur.cs` + `JetScenes.cs`, `PecheMoteur.cs`, `PatateMoteur.cs`, sur la base commune `VecMoteur.cs`.
  Un moteur ne connaît ni l'écran ni Windows : il tient une liste d'objets à dessiner et les fait bouger image par image.
- **L'affichage** dans l'application : `VecToile.cs` (WPF, regard, voix, bruits), utilisé par `WaterJetGame.cs`, `FishingGame.cs` et `PotatoGame.cs`.
  Quand un moteur a des « zones » (les cartes et les boutons de Patatou), `VecToile` pose dessus des boutons invisibles : le regard les choisit comme partout ailleurs.

Le banc d'essai fait tourner le **même** moteur sous Linux, avec un regard automatique qui va d'une cible à la suivante,
et en tire de vraies images. Il sert à voir un jeu sans tablette avant de pousser une version.

Il faut `mono`, le compilateur C# Roslyn (`csc.exe`), Node avec Playwright, et `ffmpeg`.

```
CSC=/chemin/vers/csc.exe ./banc.sh /tmp/essai/voiture voiture 24 60     # 24 s de la scène « voiture », une image toutes les 60
node images.js /tmp/essai/voiture --planche 2                           # les images en PNG, plus une planche
CSC=… ./banc.sh /tmp/essai/peche peche 40 2 && node images.js /tmp/essai/peche --video peche.mp4 --ips 15   # une vidéo
```

Scènes du jet d'eau : `voiture`, `feu`, `remplir`, `jardin`, `roue`.
La pêche : `peche` (niveau 0), `peche1`, `peche2` (niveaux suivants), `peche10` (la manche du soir).
Patatou : `patate` (les choix de l'enfant sont écrits à l'avance dans `Banc.cs`).
Le corps humain (`../corps.js`, `CorpsMoteur.cs`, `InsideBodyGame.cs`) : `corps` (niveau 0), `corps1`, `corps2`.

Chamboule-tout (`../chamboule.js`, `ChambouleMoteur.cs`, `ChambouleGame.cs`) : `chamboule` (niveau 0), `chamboule1`, `chamboule2`.

Les décors des activités de nombres (`../nombres.js`, `NombresArt.cs`) : `panier` (`PanierMoteur.cs`, pour `CompleteGame.cs`), `fil`, `gouter`, `train` (`FilMoteur.cs`, `GouterMoteur.cs`, `TrainMoteur.cs`, pour `MathGame.cs`). Ici c'est l'activité qui choisit les nombres et juge les réponses : le banc joue son rôle avec quelques questions écrites à l'avance. Les peluches partagées sont dans `../peluches.js`.

Range la maison (`../maison.js`, `MaisonArt.cs`, `MaisonMoteur.cs`, pour `TidyHouseGame.cs`) : `maison`. Le même moteur montre les trois lieux de vie de « Où habite-t-il ? » (`HabitatGame.cs`) : `habitats`. Les objets à ranger sont des images de la banque d'images de l'application (`MesPremiersJeux/Images`), que `banc.sh` indique au banc par `EYZI_IMAGES`. Le paysage partagé est dans `../paysage.js`.

Sur, sous, devant, derrière (`../reperes.js`, `ReperesArt.cs`, `LitMoteur.cs`, pour `SpatialGame.cs`) : `lit` (on découvre, puis « où est-il ? », puis « mets-le… »).

La valise du voyageur (`../valise.js`, `ValiseArt.cs`, `ValiseMoteur.cs`, pour `SuitcaseGame.cs`) : `valise` (la neige, le désert, la pluie à la suite).
Le journal (`journal.txt`) note la voix, les bruits et les réussites.
Ce que le banc ne vérifie pas : l'affichage WPF lui-même, le son, et le vrai regard.
