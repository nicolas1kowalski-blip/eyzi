# Le banc d'essai du jeu « Le jet d'eau »

Le jeu est fait de trois morceaux :

- `../art.js` : les dessins. `node ../genere.js` en tire `MesPremiersJeux/Games/JetArt.cs`.
- `MesPremiersJeux/Games/JetMoteur.cs` et `JetScenes.cs` : le moteur et les cinq scènes. Ils ne connaissent ni l'écran ni Windows.
- `MesPremiersJeux/Games/WaterJetGame.cs` : l'affichage dans l'application (WPF), le regard, la voix et les bruits.

Le banc d'essai fait tourner le **même** moteur sous Linux, avec un regard automatique qui va d'une cible à la suivante,
et en tire de vraies images. Il sert à voir le jeu sans tablette avant de pousser une version.

Il faut `mono`, le compilateur C# Roslyn (`csc.exe`), Node avec Playwright, et `ffmpeg`.

```
CSC=/chemin/vers/csc.exe ./banc.sh /tmp/essai/voiture voiture 24 60     # 24 s de la scène « voiture », une image toutes les 60
node images.js /tmp/essai/voiture --planche 2                           # les images en PNG, plus une planche
CSC=… ./banc.sh /tmp/essai/tout voiture 100 2 && node images.js /tmp/essai/tout --video jeu.mp4 --ips 15   # tout le jeu en vidéo
```

Scènes : `voiture`, `feu`, `remplir`, `jardin`, `roue`. Le journal (`journal.txt`) note la voix, les bruits et les réussites.
Ce que le banc ne vérifie pas : l'affichage WPF lui-même, le son, et le vrai regard.
