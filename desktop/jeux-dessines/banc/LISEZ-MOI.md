# Les jeux « dessinés » et leur banc d'essai

« Le jet d'eau » et « La pêche » sont faits des mêmes morceaux :

- **Les dessins** : `../jet.js`, `../peche.js` (et `../communs.js` pour ce qu'ils partagent, `../outils.js` pour les outils de dessin).
  `node ../genere.js` en tire `MesPremiersJeux/Games/JetArt.cs` et `PecheArt.cs`.
- **Le moteur** de chaque jeu : `MesPremiersJeux/Games/JetMoteur.cs` + `JetScenes.cs`, `PecheMoteur.cs`, sur la base commune `VecMoteur.cs`.
  Un moteur ne connaît ni l'écran ni Windows : il tient une liste d'objets à dessiner et les fait bouger image par image.
- **L'affichage** dans l'application : `VecToile.cs` (WPF, regard, voix, bruits), utilisé par `WaterJetGame.cs` et `FishingGame.cs`.

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
Le journal (`journal.txt`) note la voix, les bruits et les réussites.
Ce que le banc ne vérifie pas : l'affichage WPF lui-même, le son, et le vrai regard.
