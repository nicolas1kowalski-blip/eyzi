#!/bin/bash
# Compile le banc d'essai (mono + le compilateur C# Roslyn) et le lance.
#   CSC=/chemin/vers/csc.exe ./banc.sh <dossier de sortie> <scène> <durée> <une image toutes les n> [graine]
ICI="$(cd "$(dirname "$0")" && pwd)"; JEU="$ICI/../../MesPremiersJeux/Games"
mono "${CSC:?indiquer CSC=/chemin/vers/csc.exe}" -nologo -langversion:9.0 -warn:0 -out:"$1/../banc.exe" "$ICI/Banc.cs" "$JEU/JetMoteur.cs" "$JEU/JetScenes.cs" "$JEU/JetArt.cs" || exit 1
mono "$1/../banc.exe" "$@"
