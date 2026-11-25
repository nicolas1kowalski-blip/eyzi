#!/bin/bash
# Compile le banc d'essai (mono + le compilateur C# Roslyn) et le lance.
#   CSC=/chemin/vers/csc.exe ./banc.sh <dossier de sortie> <scène> <durée> <une image toutes les n> [graine]
ICI="$(cd "$(dirname "$0")" && pwd)"; JEU="$ICI/../../MesPremiersJeux/Games"
mono "${CSC:?indiquer CSC=/chemin/vers/csc.exe}" -nologo -langversion:9.0 -warn:0 -out:"$1/../banc.exe" "$ICI/Banc.cs" "$JEU"/Vec*Moteur.cs "$JEU"/Jet*.cs $(ls "$JEU"/Peche*.cs "$JEU"/Patate*.cs "$JEU"/Corps*.cs "$JEU"/ChambouleMoteur.cs "$JEU"/ChambouleArt.cs "$JEU"/NombresArt.cs "$JEU"/PanierMoteur.cs "$JEU"/VecPancartes.cs "$JEU"/FilMoteur.cs "$JEU"/GouterMoteur.cs "$JEU"/TrainMoteur.cs "$JEU"/MaisonArt.cs "$JEU"/MaisonMoteur.cs "$JEU"/ValiseArt.cs "$JEU"/ValiseMoteur.cs "$JEU"/ReperesArt.cs "$JEU"/LitMoteur.cs 2>/dev/null) || exit 1
EYZI_IMAGES="$(cd "$JEU/../Images" && pwd)" mono "$1/../banc.exe" "$@"
