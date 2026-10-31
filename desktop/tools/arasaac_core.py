#!/usr/bin/env python3
"""
Constitue le NOYAU de pictogrammes ARASAAC livré avec Eyzi (exécuté par la
compilation automatique, sur le serveur de compilation) :
  - index.csv   : TOUS les pictos ARASAAC avec leurs mots-clés français
                  (id;téléchargements;mots|clés;catégories) — quelques Mo ;
  - <id>.png    : les pictos du noyau (vocabulaire d'un enfant de 3 à 6 ans +
                  les plus téléchargés), en 300 px ;
  - LISEZ-MOI.txt : l'attribution (CC BY-NC-SA).
Le reste est téléchargé à la demande par l'application et gardé en cache.
Usage : python arasaac_core.py <dossier_sortie> [max_noyau]
"""
import json, os, sys, time, unicodedata, urllib.request

OUT = sys.argv[1] if len(sys.argv) > 1 else "arasaac-cache"
MAX = int(sys.argv[2]) if len(sys.argv) > 2 else 1500
API = "https://api.arasaac.org/api/pictograms/all/fr"
IMG = "https://static.arasaac.org/pictograms/{id}/{id}_300.png"

# Le vocabulaire d'un enfant de maternelle : ce que l'IA propose le plus souvent, ce que dit le tableau.
NOYAU = """
oui non encore fini aide aider attendre stop autre chose je ne sais pas d'accord peut-être
bonjour au revoir merci s'il te plaît pardon bravo coucou bonne nuit
moi toi papa maman mamie papi frère sœur bébé famille copain copine ami amie maîtresse maître aesh enfant fille garçon docteur nounou
manger boire dormir jouer lire chanter danser dessiner colorier peindre courir sauter marcher nager tomber pleurer rire crier parler écouter regarder voir entendre toucher sentir
vouloir aimer donner prendre mettre enlever ouvrir fermer laver ranger casser attraper lancer pousser tirer monter descendre entrer sortir venir aller partir revenir rester finir commencer
faim soif fatigué malade mal douleur froid chaud content triste fâché peur surprise calme énervé jaloux timide fier
tête cheveux yeux œil nez bouche oreille dent dents cou bras main doigt ventre dos jambe pied genou cœur
pain tartine beurre confiture céréales lait chocolat chocolat chaud jus eau yaourt fromage compote gâteau biscuit bonbon glace fruit pomme banane fraise orange poire raisin cerise pêche abricot kiwi melon pastèque
légume carotte tomate salade haricot petit pois pomme de terre purée frites riz pâtes soupe œuf poulet viande poisson jambon pizza crêpe sandwich goûter petit déjeuner déjeuner dîner repas assiette verre cuillère fourchette couteau serviette bavoir
maison chambre lit cuisine salon salle de bain toilettes douche bain baignoire porte fenêtre table chaise canapé télévision télé lampe escalier jardin balcon garage voiture bus train vélo poussette fauteuil roulant tablette ordinateur téléphone
école classe cour récréation cantine sieste cartable trousse crayon feutre ciseaux colle feuille cahier livre histoire puzzle jeu jouet ballon balle poupée peluche doudou nounours cubes lego dinette voiture jouet toboggan balançoire bac à sable
chien chat lapin oiseau poisson cheval vache cochon mouton chèvre canard poule coq souris ours lion tigre éléphant girafe singe grenouille papillon abeille coccinelle escargot tortue
dehors parc forêt plage mer piscine montagne ville magasin marché boulangerie médecin hôpital pharmacie
pantalon tee-shirt pull manteau chaussures chaussettes bonnet écharpe gants pyjama robe jupe culotte couche
matin midi après-midi soir nuit aujourd'hui demain hier lundi mardi mercredi jeudi vendredi samedi dimanche semaine week-end anniversaire fête noël vacances
soleil pluie nuage neige vent arc-en-ciel fleur arbre herbe feuille
rouge bleu jaune vert orange violet rose noir blanc marron gris
grand petit gros long court lourd léger plein vide propre sale beau joli nouveau vieux rapide lent fort doux
un deux trois quatre cinq six sept huit neuf dix
musique chanson instrument tambour guitare piano xylophone danse film dessin animé photo câlin bisou caresse chatouille
pipi caca couche change propre lavage des mains brosse à dents dentifrice savon shampoing serviette peigne
médicament sirop piqûre pansement fièvre rhume toux mal au ventre mal à la tête
encore plus un peu beaucoup assez trop ici là-bas dedans dehors dessus dessous devant derrière à côté
quoi qui où quand comment pourquoi
"""

def norm(s):
    s = unicodedata.normalize("NFD", (s or "").lower().strip())
    s = "".join(c for c in s if unicodedata.category(c) != "Mn")
    return " ".join(s.replace("’", "'").split())

def fetch(url, binary=False, tries=4):
    for i in range(tries):
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": "Eyzi-build"}), timeout=60) as r:
                data = r.read()
            return data if binary else json.loads(data.decode("utf-8"))
        except Exception as e:
            if i == tries - 1: raise
            time.sleep(2 * (i + 1))

os.makedirs(OUT, exist_ok=True)
print("index…", flush=True)
tous = fetch(API)
print(len(tous), "pictos")

entrees = []
for p in tous:
    if p.get("sex") or p.get("violence"): continue
    kws = []
    for k in p.get("keywords") or []:
        for w in (k.get("keyword"), k.get("plural")):
            if w and norm(w) not in kws: kws.append(norm(w))
    if not kws: continue
    entrees.append({"id": p["_id"], "dl": int(p.get("downloads") or 0), "kw": kws, "cat": [norm(c) for c in (p.get("categories") or [])], "schematic": bool(p.get("schematic")), "aac": bool(p.get("aac"))})

with open(os.path.join(OUT, "index.csv"), "w", encoding="utf-8", newline="") as f:
    f.write("id;telechargements;mots;categories\n")
    for e in sorted(entrees, key=lambda e: -e["dl"]):
        f.write(f'{e["id"]};{e["dl"]};{"|".join(e["kw"])};{"|".join(e["cat"])}\n')
print("index.csv écrit")

par_mot = {}
for e in entrees:
    for w in e["kw"]:
        par_mot.setdefault(w, []).append(e)
for l in par_mot.values(): l.sort(key=lambda e: (e["schematic"], -e["dl"]))

noyau, manquants = {}, []
for w in NOYAU.split():
    pass
# les mots du noyau sont séparés par des espaces mais certains sont composés : on essaie d'abord les groupes de 3, 2, 1 mots
mots = NOYAU.split()
i = 0
while i < len(mots):
    pris = False
    for n in (3, 2, 1):
        w = norm(" ".join(mots[i:i + n]))
        if w in par_mot and (n == 1 or len(par_mot[w]) > 0):
            noyau[par_mot[w][0]["id"]] = w
            i += n; pris = True; break
    if not pris:
        manquants.append(mots[i]); i += 1
print(len(noyau), "pictos du vocabulaire ;", len(manquants), "mots sans picto :", " ".join(manquants[:60]))
for e in sorted([e for e in entrees if e["aac"]], key=lambda e: -e["dl"]):
    if len(noyau) >= MAX: break
    noyau.setdefault(e["id"], e["kw"][0])
print(len(noyau), "pictos dans le noyau")

ok = 0
for n, (pid, w) in enumerate(noyau.items(), 1):
    dest = os.path.join(OUT, f"{pid}.png")
    if os.path.exists(dest) and os.path.getsize(dest) > 500: ok += 1; continue
    try:
        data = fetch(IMG.format(id=pid), binary=True)
        with open(dest, "wb") as f: f.write(data)
        ok += 1
    except Exception as e:
        print("raté", pid, w, e)
    if n % 200 == 0: print(n, "…", flush=True)
print(ok, "images")

with open(os.path.join(OUT, "LISEZ-MOI.txt"), "w", encoding="utf-8") as f:
    f.write("Pictogrammes ARASAAC\n\nAuteur des pictogrammes : Sergio Palao. Origine : ARASAAC (https://arasaac.org).\n"
            "Licence : Creative Commons BY-NC-SA 4.0. Propriété : Gouvernement d'Aragon (Espagne).\n"
            "Ce dossier contient un noyau de pictogrammes choisi pour Eyzi, en 300 px, et l'index (index.csv) de toute la banque en français ;\n"
            "les autres pictogrammes sont téléchargés à la demande depuis static.arasaac.org et gardés sur la tablette.\n"
            "Usage non commercial : Eyzi est gratuite.\n")
with open(os.path.join(OUT, "RESUME.txt"), "w", encoding="utf-8") as f:
    f.write(f"ARASAAC : {len(tous)} pictos dans l'index, {len(entrees)} gardés (sans sexe ni violence)\n")
    f.write(f"Noyau : {len(noyau)} pictos demandés, {ok} images téléchargées\n")
    f.write(f"Mots du vocabulaire sans picto ({len(manquants)}) : {' '.join(manquants)}\n")
if ok < 800:
    print("TROP PEU D'IMAGES"); sys.exit(1)
