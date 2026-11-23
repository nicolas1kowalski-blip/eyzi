#!/usr/bin/env python3
# Les bruitages des humeurs animées : une bande-son par scène, calée sur ce que fait le personnage
# (les instants viennent des scènes : scenes.js, scenes2.js, scenes3.js, beurk.html, prout.js, jetaime.js).
# Tous les sons sont calculés par synthe.py ; ffmpeg les glisse dans les vidéos de ../MesPremiersJeux/Humeurs.
#   python3 sons.py                tout refaire
#   python3 sons.py prout miam     seulement ces scènes
#   python3 sons.py --wav dossier  écrire aussi les .wav dans ce dossier (pour écouter ou relire)
# Il faut Python 3 avec numpy et scipy, et ffmpeg. fabrique.js appelle ce script après avoir fait les vidéos.
import os, subprocess, sys, tempfile, wave
import numpy as np
from synthe import *

ICI = os.path.dirname(os.path.abspath(__file__))
SORTIE = os.path.join(ICI, '..', 'MesPremiersJeux', 'Humeurs')


class Piste:
    """La bande-son d'une scène : on y pose des sons à des instants donnés."""
    def __init__(self, duree): self.y = np.zeros(nb(duree)); self.niveau = 1.0
    def met(self, t, son, force=1.0):
        k = max(0, nb(t) if t > 0 else 0)
        if k >= len(self.y): return
        son = son[:len(self.y) - k]
        self.y[k:k + len(son)] += force * son
    def air(self, t, notes, instrument=boite, force=.5, **o):
        """Une mélodie : notes = [(instant, note, durée)…], à partir de t."""
        self.met(t, melodie(notes, instrument, **o), force)
    def rendu(self):
        # Toutes les scènes au même niveau : on règle sur le passage le plus fort (0,4 s), sans jamais saturer.
        y = self.y.copy(); f = nb(.4)
        fort = np.sqrt(np.max(np.convolve(y ** 2, np.ones(f) / f, 'same')))
        if fort > 0: y *= .3 * self.niveau / fort
        m = np.max(np.abs(y))
        if m > .92: y *= .92 / m
        n = nb(.03); y[:n] *= np.linspace(0, 1, n); y[-n:] *= np.linspace(1, 0, n)
        return y


def suite(noms, pas_, d=None, depart=0.0):
    """['C5', 'E5'…] → [(instant, note, durée)…], une note tous les « pas_ »."""
    return [(depart + i * pas_, n, d or pas_ * 1.6) for i, n in enumerate(noms) if n]


def nasillard(notes):
    """Une mélodie chantée du nez : « Nananère ! »"""
    fin = max(t + d for t, f, d in notes); y = np.zeros(nb(fin + .1))
    for t, f, d in notes:
        s = voix(glisse((f * .93, f, .03), (f, f, d - .03)), d, 'è', nez=.55, souffle=.03, a=.03, r=.05); k = nb(t); y[k:k + len(s)] += s
    return norm(y)


def tremble(son, par_seconde=18, creux=.6):
    return norm(son * (1 - creux * np.sin(np.pi * par_seconde * temps(len(son))) ** 2))


def coule(d):
    """Un liquide qui se déverse."""
    return norm(norm(bande(bruit(d), 250, 3200)) * (.45 + .55 * norm(bas(bruit(d), 14)) ** 2) * env(nb(d), .03, .15) + .7 * bulles(d, 24, 180, 700))


S = {}
def scene(f): S[f.__name__[2:]] = f; return f       # s_prout → la scène « prout »

# ═════════════ La joie ═════════════
@scene
def s_tropcool(p):
    # une contrebasse qui se promène, des claquements de doigts ; l'éclat des lunettes, le clin d'œil
    for i, n in enumerate(['E3', 'G3', 'A3', 'B3', 'E3', 'G3', 'A3', 'B3', 'D4', 'B3', 'A3', 'G3', 'E3', 'G3', 'A3', 'B3']): p.met(.2 + i * .29, corde(note(n), .5, .35), .7)
    for i in range(8): p.met(.2 + .29 * (2 * i + 1), claquement(), .5)
    p.met(1.15, ting(2400), .8); p.met(3.3, ting(2800), .8); p.met(4.6, ting(3300, .6), .7)
    p.met(1.8, scintille(3.0, 3), .22)
    p.met(4.9, basse(glisse((165, 82, .5)), .5), .7)

@scene
def s_youpi(p):
    p.met(.5, whoosh(.2), .4); p.met(.62, sifflet(glisse((500, 1500, .32)), .32), .8); p.met(.95, tada(587, .5), .7)
    for i in range(12): p.met(.8 + i * .29 + hasard(0, .1), pop(hasard(700, 1400)), hasard(.15, .35))
    p.met(.7, scintille(3.6, 5), .28)
    p.met(3.15, sifflet(glisse((500, 1700, .5), (1700, 1700, .25)), .75), .8); p.met(3.9, tada(659, .6), .7)

@scene
def s_rigole(p):
    p.met(1.2, rire(6, 580, 'a', .19), 1); p.met(3.0, rire(4, 700, 'i', .15), .9); p.met(3.65, rire(5, 600, 'a', .17), 1)
    p.met(5.1, souffle(.7, 500, 2500), .5)

@scene
def s_bravo(p):
    for t in (.544, .785, 1.027, 1.269, 1.510, 1.752, 1.994, 2.235, 2.477): p.met(t, clap(), .9)      # ses mains se rejoignent à ces instants
    p.met(2.9, tada(523, .7), .9); p.met(2.95, applaudit(1.9), .55)
    p.met(3.7, sifflet(glisse((900, 2200, .18), (2200, 1300, .25)), .43), .4)
    for i in range(10): p.met(3.0 + i * .15 + hasard(0, .05), pop(hasard(800, 1500)), hasard(.1, .25))

@scene
def s_super(p):
    p.met(1.25, ting(2300), .9); p.met(1.3, tada(523, .6), .8); p.met(2.6, ting(2900), .8)
    p.met(3.2, pop(1100), .6); p.air(3.22, suite(['C6', 'E6', 'G6'], .08, .5), xylo, .6)
    p.met(1.3, scintille(3.2, 5), .25)

@scene
def s_danse(p):
    T = 60 / 126; t0 = .9
    p.met(t0, batterie(4.5, 126), .8)
    for i, n in enumerate(['C3', 'C3', 'Eb3', 'F3', 'C3', 'C3', 'G3', 'F3', 'C3']): p.met(t0 + i * T, basse(note(n) * 2, T * .9), .6)
    p.air(t0, suite(['C5', 'Eb5', 'F5', 'G5', 'Bb5', 'G5', 'F5', 'Eb5', 'C5', 'Eb5', 'G5', 'Bb5', 'C6', 'Bb5', 'G5', 'F5', 'Eb5', 'C5'], T / 2, T / 2 * .9), synthe, .45)
    p.met(3.0, whoosh(.25), .5); p.met(3.2, boing(220, .4), .5); p.met(4.1, cymbale(.9), .45)
    p.met(5.45, grosse_caisse(), .9); p.met(5.45, cymbale(.5), .4)

@scene
def s_chante(p):
    p.met(1.1, chante_([(0, 'C5', .33), (.36, 'D5', .33), (.72, 'E5', .36)]), .8)
    p.met(2.2, chante_([(0, 'E5', .3), (.33, 'F5', .3), (.66, 'G5', .5)]), .85)
    p.met(3.5, chante_([(0, 'C6', 1.45)], vib=.022), 1)
    for t, n in ((1.1, 'C4'), (2.2, 'F4'), (3.5, 'C4')): p.met(t, harpe([note(n), note(n) * 1.26, note(n) * 1.5, note(n) * 2], .06, 1.0), .35)
    p.met(3.5, scintille(1.5, 7), .3)

def chante_(notes, vib=.012): return chante([(t, note(n), d) for t, n, d in notes], vib=vib)

@scene
def s_fete(p):
    p.met(.4, batterie(5.0, 132), .4)
    p.air(.4, suite(['G5', 'G5', 'A5', 'G5', 'C6', 'B5', None, 'G5', 'G5', 'A5', 'G5', 'D6', 'C6'], .34, .3), xylo, .45)
    p.met(1.55, pouet(440, 1.0), .95); p.met(2.8, cymbale(.8), .4)
    p.met(3.65, pouet(494, .3), .9); p.met(4.05, pouet(494, .5), .9)
    for i in range(14): p.met(1.2 + i * .3 + hasard(0, .12), pop(hasard(700, 1500)), hasard(.15, .35))

@scene
def s_fier(p):
    p.met(1.3, ting(2300), .8); p.met(1.9, tada(392, .95), .9)
    p.met(3.6, rire(2, 340, 'è', .22, .9, .13), .6); p.met(4.5, ting(2900), .8)
    p.met(1.8, scintille(3.2, 4), .22)

@scene
def s_gagne(p):
    for t in (0, .3, .6, .9): p.met(t, pas(), .5)                    # il court
    p.met(.95, whoosh(.3), .4); p.met(1.45, boum(.7), .6)             # il saute sur la marche
    p.met(2.2, ting(2500), .7); p.met(2.25, fanfare(), 1)
    p.met(2.6, applaudit(2.6), .45)
    p.met(3.95, sifflet(glisse((900, 1900, .2), (1900, 1300, .28)), .48), .45)

@scene
def s_surprise(p):
    p.met(.7, oh(.4), .7); p.met(1.2, roulement(1.0), .7)             # il tire sur le ruban…
    p.met(2.2, bouchon(), 1); p.met(2.22, cymbale(.9), .45)
    for i in range(12): p.met(2.3 + i * .17 + hasard(0, .06), pop(hasard(800, 1600)), hasard(.12, .3))
    p.met(3.0, tada(523, .8), .8); p.met(2.3, scintille(2.2, 6), .28)

# ═════════════ La tendresse ═════════════
@scene
def s_jetaime(p):
    for t in (.35, .95, 1.55): p.met(t, coeur_bat(), .9)
    for i in range(4): p.met(1.75 + i * .16, pop(900 + 150 * i), .3)   # ses yeux deviennent des cœurs
    p.met(2.95, harpe([note(n) for n in ('C4', 'E4', 'G4', 'C5', 'E5', 'G5', 'C6')], .05, 1.4), .7)
    p.met(3.3, scintille(.9, 9), .35)
    p.met(4.3, bisou(), 1); p.met(4.45, whoosh(.35), .3)
    p.met(5.0, hum(glisse((520, 480, .8)), .8), .5); p.air(5.05, suite(['E5', 'G5', 'C6'], .2, .9), boite, .45)

@scene
def s_coucou(p):
    for i, t in enumerate((.16, .49, .81, 1.14)): p.met(t, xylo(note(['C5', 'D5', 'E5', 'G5'][i]), .2), .4)      # il arrive en sautillant
    for t in (.325, .65, .975, 1.3): p.met(t, pas(), .4)
    appel = lambda: chante([(0, 784, .22), (.26, 659, .36)], v='ou')
    p.met(1.6, appel(), 1); p.air(1.6, [(0, 'G5', .3), (.26, 'E5', .5)], xylo, .5)
    p.met(3.35, ting(2600), .6)
    p.met(3.5, appel(), .9); p.met(4.0, appel(), .9)
    for i, t in enumerate((4.7, 4.9, 5.1)): p.met(t, xylo(note(['G5', 'E5', 'C5'][i]), .2), .35)

@scene
def s_merci(p):
    p.met(1.5, ting(2500), .7); p.met(1.5, scintille(.6, 9), .3)
    p.met(2.3, hum(glisse((440, 540, .5)), .5), .4)
    p.air(2.35, suite(['C5', 'E5', 'G5', 'C6'], .12, 1.2), boite, .8)
    p.air(3.0, suite(['E5', 'G5', 'A5', 'G5', 'C6', 'A5'], .4, 1.0), boite, .4)

@scene
def s_bisou(p):
    p.met(1.25, bisou(), .9); p.met(1.6, scintille(.9, 6), .3)
    p.met(3.45, bisou(), 1); p.met(3.62, harpe([note(n) for n in ('G4', 'B4', 'D5', 'G5', 'B5', 'D6')], .045, 1.2), .5)
    p.met(3.8, scintille(.9, 7), .3)

@scene
def s_calin(p):
    p.met(.6, hum(glisse((420, 420, .25), (420, 570, .3)), .55), .6)          # « Un câlin ? »
    p.met(3.0, hum(glisse((500, 470, 1.3)), 1.3), .7)
    p.met(3.0, harpe([note(n) for n in ('C4', 'E4', 'G4', 'C5')], .09, 1.6), .35)
    for t in (3.2, 4.0, 4.8, 5.6): p.met(t, coeur_bat(), .45)
    p.air(4.6, [(0, 'E5', .9), (.35, 'E5', .9), (.7, 'G5', 1.2)], boite, .4)

@scene
def s_aurevoir(p):
    p.air(.6, [(0, 'G5', .5), (.3, 'E5', .8)], boite, .7); p.air(1.5, [(0, 'G5', .5), (.3, 'E5', .8)], boite, .55)
    for i in range(12): p.met(3.0 + i * .2, pas(), .5 * (1 - i / 14))          # il s'éloigne
    p.air(3.6, [(0, 'E5', .6), (.3, 'D5', .6), (.6, 'C5', 1.2)], boite, .6)

@scene
def s_silteplait(p):
    p.met(1.0, hum(glisse((480, 620, .5), (620, 560, .3)), .8, vib=.03), .7)
    p.air(1.1, suite(['C5', 'D5', 'E5'], .4, 1.0), boite, .35)
    p.met(1.0, scintille(3.8, 2.5), .18)
    p.met(3.0, hum(glisse((500, 680, .35)), .35, vib=.02), .7); p.met(3.5, hum(glisse((520, 700, .35)), .35, vib=.02), .7)
    p.met(4.0, hum(glisse((540, 760, .6)), .6, vib=.03), .75)
    p.air(3.05, suite(['E5', 'G5', 'C6'], .5, 1.0), boite, .35)

@scene
def s_pardon(p):
    p.met(.1, casse(), .7)
    p.met(2.0, hum(glisse((400, 330, .9)), .9), .45)
    p.air(1.9, [(0, 'E5', .9), (.5, 'D5', .9), (1.0, 'C5', .9), (1.5, 'A4', 1.3)], boite, .6)
    p.met(4.6, hum(glisse((380, 300, 1.0)), 1.0), .45); p.air(4.7, [(0, 'C5', .9), (.5, 'A4', 1.2)], boite, .4)

@scene
def s_manque(p):
    p.met(.5, souffle(.9, 400, 2200), .5); p.met(.6, hum(glisse((420, 330, .7)), .7), .35)
    p.air(2.4, [(0, 'A4', .9), (.5, 'C5', .9), (1.0, 'E5', 1.1), (1.7, 'D5', .9), (2.2, 'C5', .9), (2.7, 'A4', 1.4)], boite, .6)
    p.met(3.05, plip(), .6)

@scene
def s_bonjour(p):
    p.met(.5, baille(1.0), .7)
    p.air(2.1, suite(['C5', 'E5', 'G5', 'C6'], .14, .5), xylo, .8); p.met(2.66, ting(2100, .7), .4)
    p.met(3.9, cuicui(3), .7); p.met(4.55, cuicui(2, 4100), .5)
    p.met(2.1, scintille(2.8, 3), .18)

# ═════════════ Pour rire ═════════════
@scene
def s_beurk(p):
    p.met(.75, gloups(), .6); p.met(1.25, gloups(), 1)
    p.met(1.58, souffle(.3, 600, 3000, descend=False), .5)                    # il prend son élan
    vomi = lambda d: voix(glisse((240, 165, d)), d, 'eu', 'a', souffle=.15, rauque=1, a=.03, r=.1)
    p.met(2.12, vomi(.9), .9); p.met(2.15, coule(.9), .7); p.met(2.5, splat(), .5)
    p.met(3.07, souffle(.12, 800, 3000), .4); p.met(3.2, souffle(.12, 800, 3000), .4)      # il reprend son souffle
    p.met(3.35, vomi(.65), .8); p.met(3.38, coule(.65), .6); p.met(3.65, splat(), .4)
    p.met(4.2, mouche(1.7), .3)
    p.met(5.0, sifflet(glisse((1100, 600, .5)), .5), .45); p.met(5.0, souffle(.6, 500, 2500), .4)       # « pfiou… »

@scene
def s_prout(p):
    p.air(0, [(0, 'E6', .2), (.22, 'G6', .2), (.44, 'E6', .2), (.66, 'C6', .26)], sifflet, .45)       # il sifflote, l'air de rien
    p.met(.95, gargouille(.8), .7)
    p.met(1.6, hum(glisse((300, 390, .9)), .9, vib=.035), .35)                # il se retient…
    p.met(2.55, prout(.7), 1)
    p.met(3.3, sifflet(glisse((900, 600, .25)), .25), .4)                     # « oups… »
    p.met(3.3, mouche(.7, 260), .3)                                           # une mouche arrive… et tombe
    p.met(4.05, sifflet(glisse((1400, 500, .5)), .5, vib=0), .35); p.met(4.58, poc(520), .4)
    p.met(3.75, rire(5, 700, 'i', .2), .9)
    for t in (4.9, 5.12, 5.34, 5.56): p.met(t, whoosh(.18), .3)               # il s'évente

@scene
def s_nananere(p):
    g, e, a = 588, 494, 660
    p.met(.7, nasillard([(0, g, .28), (.3, e, .28), (.6, a, .14), (.75, g, .14), (.9, e, .45)]), .9)
    p.met(2.4, langue(.95), 1)
    p.met(3.4, nasillard([(0, g, .15), (.16, g, .15), (.32, e, .15), (.48, a, .15), (.64, g, .15), (.8, e, .4)]), .9)

@scene
def s_chut(p):
    p.niveau = .7
    for t in (.1, 2.5, 4.5): p.met(t, ronfle(.8), .22); p.met(t + .85, pschh(.6), .15)       # quelqu'un dort
    p.met(1.3, chut(1.1), .9); p.met(3.0, chut(.7), .6); p.met(3.8, chut(.5), .4)
    p.air(.3, [(0, 'E5', .9), (.6, 'E5', .9), (1.2, 'G5', 1.4), (2.6, 'E5', .9), (3.2, 'E5', .9), (3.8, 'G5', 1.4)], boite, .18)

@scene
def s_clown(p):
    for i in range(16): p.met(.3 + i * .25, corde(note('C3' if i % 2 == 0 else 'G3'), .3, .4), .45)              # « poum-tchac » du cirque
    for i in range(8): p.met(.425 + i * .5, charley(.08), .3)
    p.air(1.2, suite(['C6', 'B5', 'Bb5', 'B5', 'Bb5', 'A5', 'Ab5', 'A5', 'Ab5', 'G5', 'F#5', 'G5', 'E5', 'G5', 'C6', 'E6', 'D6', 'C6', 'B5', 'C6', 'G5', 'E5', 'C5'], .125, .2), xylo, .5)
    p.met(1.4, sifflet(glisse((700, 1500, .15)), .15), .5); p.met(2.0, sifflet(glisse((800, 1700, .15)), .15), .5)
    p.met(2.8, boing(200, .6), .6)
    p.met(4.72, klaxon(), 1); p.met(5.17, klaxon(), 1); p.met(5.62, klaxon(400, .15), .5)       # il presse son nez
    p.met(5.75, cymbale(.4), .35)

@scene
def s_chatouilles(p):
    p.met(1.2, frotte(1.1, 9, 3000, 8000), .22)                               # la plume
    p.met(1.6, rire(8, 760, 'i', .15, .985), 1)
    p.met(3.1, rire(3, 720, 'i', .15), .9); p.met(3.6, rire(4, 640, 'a', .17), .95)
    p.met(4.9, souffle(.7, 500, 2500), .5)

@scene
def s_oups(p):
    p.air(.1, [(0, 'G5', .18), (.2, 'C6', .18), (.4, 'E6', .18), (.6, 'G6', .3), (.95, 'E6', .18), (1.15, 'G6', .25)], sifflet, .45)     # il se promène en sifflotant
    for t in (.375, .75, 1.125, 1.5): p.met(t, pas(), .35)
    p.met(1.62, sifflet(glisse((1500, 500, .5)), .5, vib=0), .6)              # ça lui échappe…
    p.met(2.14, splat(), 1)
    p.met(2.8, trombone_triste(3), .7)
    p.met(4.5, rire(2, 400, 'è', .24, .9, .13), .5)                           # « hé hé… »

@scene
def s_cachecache(p):
    p.met(.4, whoosh(.3, monte=False), .4)
    for i, n in enumerate(['C4', 'Eb4', 'C4', 'Gb4', 'C4', 'Eb4', 'B3']): p.met(.9 + i * .3, corde(note(n), .3, .3), .45)       # à pas de loup
    p.met(1.5, sifflet(glisse((700, 1000, .15)), .15), .3); p.met(2.3, sifflet(glisse((800, 1100, .15)), .15), .3)             # un œil dépasse
    p.met(3.0, boing(260, .5), .7); p.met(3.05, chante([(0, 784, .22), (.26, 659, .4)], v='ou'), 1); p.met(3.05, ting(2400), .5)
    p.met(4.0, rire(3, 720, 'i', .16), .8); p.met(3.3, scintille(1.7, 5), .25)

@scene
def s_hoquet(p):
    for t in (.8, 1.9): p.met(t, hic(), .9)
    p.met(2.8, hic(1.2), 1)
    p.met(3.4, hum(glisse((300, 430, 1.3)), 1.3, vib=.04), .35)               # il se retient…
    p.met(4.9, hic(1.7), 1); p.met(4.96, boing(200, .6), .6); p.met(4.96, pop(600), .5)

@scene
def s_grimace(p):
    p.met(.7, tremble(voix(glisse((420, 300, 1.1)), 1.1, 'eu', souffle=.1), 14, .5), .9)       # « Bleuuuh ! », la langue tirée
    p.met(2.2, boing(210, .3), .4)
    p.met(2.4, voix(glisse((500, 830, 1.0)), 1.0, 'i', nez=.6, vib=.02), .8)                   # « Gniiii ! »
    p.met(3.9, boing(240, .3), .4)
    p.met(4.1, nasillard([(i * .2, f, .18) for i, f in enumerate((588, 494, 660, 588, 494, 588))]), .9)     # « Gnagnagna ! »

# ═════════════ Quand ça ne va pas ═════════════
@scene
def s_triste(p):
    p.met(1.5, snif(), .7); p.met(1.9, pluie(3.6), .3)
    p.air(1.9, [(0, 'A4', 1.1), (.7, 'G4', 1.1), (1.4, 'F4', 1.1), (2.1, 'E4', 1.5)], boite, .4)
    p.met(3.0, sanglots(3), .9)
    p.met(4.6, snif(), .7); p.met(4.95, snif(), .7)

@scene
def s_colere(p):
    p.met(1.0, norm(bande(bruit(3.4), 3000, 8000) * (.5 + .5 * np.sin(2 * np.pi * 4.5 * temps(nb(3.4))) ** 2) * env(nb(3.4), .3, .3)), .2)      # la vapeur
    for t in (1.3, 2.1, 2.45): p.met(t, boum(), 1)                            # il tape du pied
    p.met(3.0, grogne(1.4), 1); p.met(3.05, sifflet(glisse((1800, 3300, 1.3)), 1.3, vib=.004), .28)      # la bouilloire monte
    p.met(4.7, souffle(.9, 400, 2500), .7)

@scene
def s_peur(p):
    p.met(0, vent(4.2), .25)
    p.met(.55, crac(), 1)
    p.met(1.5, tremble(voix(glisse((520, 570, .9)), .9, 'ou', vib=.05), 11, .5), .4); p.met(1.5, claque_des_dents(1.0), .35)
    for t in (2.4, 2.85, 3.25, 3.6): p.met(t, coeur_bat(70), .6)              # caché, le cœur qui bat
    p.met(4.0, couic(), .7)                                                   # une souris !
    p.met(4.2, cri(.7), 1); p.met(4.2, boing(240, .5), .4)
    p.met(5.0, claque_des_dents(.5), .3)

@scene
def s_boude(p):
    p.met(.9, hmpf(), .9); p.met(1.35, cuivre(131, .45), .45)
    p.met(2.3, trombone_triste(2), .45)
    p.met(3.5, hmpf(), 1); p.met(3.62, souffle(.4, 700, 3000), .5); p.met(3.95, cuivre(110, .6), .5)

@scene
def s_malade(p):
    p.met(.5, claque_des_dents(1.3), .35); p.met(.5, tremble(voix(glisse((300, 280, 1.2)), 1.2, 'eu', souffle=.08), 18), .5)
    p.met(1.6, bips(3), .5)                                                   # le thermomètre
    p.met(2.4, voix(glisse((420, 520, .35)), .35, 'a', souffle=.3, a=.08), .6); p.met(2.85, voix(glisse((460, 620, .45)), .45, 'a', souffle=.3, a=.1), .75)
    p.met(3.4, atchoum(), 1); p.met(3.42, souffle(.25, 1500, 5000), .5)
    p.met(4.6, snif(), .6); p.met(5.0, hum(glisse((420, 350, .6)), .6), .4)

@scene
def s_dodo(p):
    p.niveau = .85
    p.met(.45, baille(1.3), .8)
    p.air(2.0, [(0, 'E5', .8), (.3, 'E5', .8), (.6, 'G5', 1.3), (1.5, 'E5', .8), (1.8, 'E5', .8), (2.1, 'G5', 1.3), (3.0, 'E5', .8), (3.3, 'G5', .8), (3.6, 'C6', 1.0)], boite, .3)     # une berceuse
    p.met(3.2, ronfle(.9), .6)
    p.met(4.25, hmpf(), .5); p.met(4.4, oh(.3, 420), .6)                      # « hein ?! »
    p.met(5.0, ronfle(.9), .7); p.met(5.9, pschh(.6), .5)

@scene
def s_mal(p):
    for t in (.33, .67, 1.0): p.met(t, pas(), .35)
    p.met(1.1, boum(), 1); p.met(1.1, poc(430), .7)
    p.met(1.5, aie(), 1); p.met(1.6, scintille(1.1, 6), .18)                  # il en voit des étoiles
    for i in range(4): p.met(1.79 + i * .393, pas(), .3)                      # il sautille de douleur
    for i, f in enumerate((600, 560, 520)): p.met(2.8 + i * .34, aie(.26, f), .7)
    p.met(4.2, snif(), .6); p.met(4.55, sanglots(1, 430), .5)
    p.air(5.0, [(0, 'E5', .8), (.5, 'C5', 1.1)], boite, .4)

@scene
def s_chaud(p):
    p.met(.2, cigales(5.6), .2)
    p.met(.6, souffle(.8, 500, 2500), .6)
    for i in range(6): p.met(1.8 + i * .4, whoosh(.22, monte=i % 2 == 0), .35)            # il s'évente
    p.met(4.3, sifflet(glisse((900, 350, 1.0)), 1.0, vib=.03), .45)           # « je fonds… »
    for t in (4.6, 5.0, 5.3, 5.55): p.met(t, plip(hasard(1000, 1500)), .5)

@scene
def s_froid(p):
    p.met(.1, vent(4.6), .45)
    p.met(.8, tremble(voix(glisse((260, 240, 1.2)), 1.2, 'eu', souffle=.08, rauque=.3), 20), .7)       # « Brrrr… »
    p.met(2.3, claque_des_dents(1.0), .7)
    p.met(4.6, aah(1.0, 480), .7); p.met(4.65, harpe([note(n) for n in ('C4', 'E4', 'G4', 'C5')], .09, 1.3), .4)
    p.met(4.9, scintille(.9, 4), .15)

@scene
def s_ennui(p):
    p.met(0, mouche(6.3), .28)
    for i, t in enumerate((.5, 1.5, 2.5, 3.5, 4.5, 5.5)): p.met(t, tic(2300 if i % 2 == 0 else 1700), .6)       # tic… tac…
    p.met(3.0, souffle(1.2, 400, 2500), .7)
    p.met(4.8, baille(1.1, 330), .7)

@scene
def s_bruit(p):
    vacarme = np.tanh(3 * batterie(3.0, 152)) * .6
    vacarme[:len(vacarme)] += .4 * melodie(suite(['C5', 'F#5', 'D5', 'G#5', 'C#5', 'A5', 'Eb5', 'B5', 'C5', 'F#5', 'D5', 'G#5', 'C#5', 'A5'], .2, .18), synthe)[:len(vacarme)]
    p.met(.5, norm(vacarme) * env(len(vacarme), .2, .02), .5)
    p.met(.6, boum(), 1); p.met(1.3, trompe(311, .8), .9); p.met(2.1, blam(), 1); p.met(2.7, dzing(), .9)
    p.met(3.5, tout_s_arrete(.4), .8)                                         # « STOP ! » : tout se tait
    p.met(4.75, sifflet(glisse((800, 600, .3)), .3), .25)
    p.met(5.2, cuicui(2), .3); p.air(5.3, [(0, 'E5', 1.0)], boite, .25)

@scene
def s_non(p):
    for i, t in enumerate((1.15, 1.55, 1.95)):
        p.met(t, voix(glisse((330 - 12 * i, 268 - 12 * i, .22)), .22, 'o', nez=.4, a=.015, r=.05), .9); p.met(t - .05, whoosh(.14, monte=i % 2 == 0), .2)
    p.met(2.9, boum(), 1); p.met(2.92, voix(glisse((300, 215, .55)), .55, 'o', nez=.3, rauque=.3, a=.015, r=.1), 1)
    p.met(3.71, boum(), .8)

# ═════════════ Ce que je fais ═════════════
@scene
def s_miam(p):
    p.met(.2, oh(.7, 430, monte=False), .7)
    p.met(1.5, croque(.28), 1); p.met(1.85, mache(4, .3), .6)
    p.met(3.2, croque(.22), .9); p.met(3.5, mache(3, .3), .6)
    p.met(4.6, hum(glisse((480, 620, .25), (620, 440, .6)), .85), .8); p.met(4.7, scintille(1.0, 4), .22)

@scene
def s_soif(p):
    p.met(.3, souffle(.35, 600, 2800), .5); p.met(.8, souffle(.35, 600, 2800), .5)
    for i in range(8): p.met(2.1 + i * .24, glou(220 * (1 + .06 * i)), .9)
    p.met(4.3, aah(1.0, 540), .9); p.met(4.4, scintille(1.0, 6, 2500, 5000), .28)

@scene
def s_musique(p):
    T = 60 / 112; t0 = 1.0
    p.met(t0, batterie(4.1, 112, 'tchic'), .8)
    for i, n in enumerate(['E3', 'E3', 'G3', 'A3', 'E3', 'E3', 'B3', 'A3']): p.met(t0 + i * T, basse(note(n) * 2, T * .8), .6)
    p.air(t0 + T, suite(['E5', 'G5', 'A5', 'B5', None, 'A5', 'G5', 'E5', 'G5', 'A5', 'B5', 'D6', 'E6'], T / 2, T / 2 * .9), synthe, .4)
    p.met(3.9, cymbale(.9), .45); p.met(3.9, tada(659, .5), .4)

@scene
def s_nesaispas(p):
    p.met(.5, hum(glisse((380, 400, .8), (400, 480, .6)), 1.4, vib=.02), .6)
    for i, n in enumerate(['E4', 'G4', 'E4', 'A4']): p.met(.7 + i * .4, corde(note(n), .35, .3), .3)
    p.met(2.7, sifflet(glisse((500, 720, .18), (720, 440, .3)), .48), .6)     # il hausse les épaules
    p.met(4.0, sifflet(glisse((460, 660, .15), (660, 400, .35)), .5), .6)

@scene
def s_ballon(p):
    p.met(0, whoosh(.4, monte=False), .3)
    p.met(.45, boing(150, .35), .9); p.met(.85, boing(175, .3), .7)
    for i, t in enumerate((1.3, 1.9, 2.5, 3.1)): p.met(t, poc(700 + 60 * i), .8)          # il jongle de la tête
    p.met(3.7, poc(380), .6); p.met(3.78, sifflet(glisse((700, 1500, .18)), .18), .4)
    p.met(4.6, tada(523, .5), .6); p.met(4.1, scintille(1.5, 4), .22)

@scene
def s_lis(p):
    p.met(.45, page(), .6)
    p.air(1.1, suite(['C4', 'E4', 'G4', 'C5', 'E5', 'G5', 'E5', 'C5'], .25, 1.0), lambda f, d: corde(f, d, .6), .45)
    p.met(3.3, page(), .5); p.met(3.5, oh(.6, 460, monte=False), .7)
    p.met(3.4, scintille(2.1, 9), .38); p.met(3.45, harpe([note(n) for n in ('C5', 'D5', 'E5', 'G5', 'A5', 'C6', 'D6', 'E6')], .04, 1.0), .4)
    p.met(4.8, page(), .6); p.air(5.0, [(0, 'G5', .8), (.3, 'C6', 1.0)], boite, .4)

@scene
def s_bain(p):
    p.met(.3, sifflet(glisse((1200, 400, .55)), .55, vib=0), .45)             # il plonge
    p.met(.9, plouf(1.0), 1)
    p.met(1.2, bulles(4.8, 7), .28)
    p.met(1.7, frotte(1.8, 4), .5)
    p.met(3.9, chante_([(0, 'G5', .25), (.28, 'A5', .25), (.56, 'G5', .25), (.84, 'E5', .4)]), .7)
    p.met(4.5, coincoin(), .8)
    for t in (5.4, 5.75): p.met(t, plip(), .4)

@scene
def s_dents(p):
    p.met(1.2, frotte(1.25, 6.5, 2500, 7000), .7); p.met(2.5, frotte(1.2, 6.5, 3300, 8500), .7)       # en haut… en bas…
    p.met(1.2, bulles(2.6, 6, 600, 1500), .15)
    p.met(3.8, bulles(.3, 30, 300, 800), .4)                                  # on rince
    p.met(4.2, ting(2600), 1); p.met(4.25, scintille(1.2, 7), .4)
    p.air(4.3, [(0, 'E6', .25), (.15, 'G6', .25), (.3, 'C7', .5)], xylo, .5)

@scene
def s_idee(p):
    p.met(.5, hum(glisse((380, 400, 1.0), (400, 455, .6)), 1.6, vib=.02), .5)
    for i, n in enumerate(['E4', 'G4', 'E4', 'A4']): p.met(.8 + i * .4, corde(note(n), .35, .3), .3)
    p.met(2.6, ting(1760, 1.2), 1); p.met(2.6, ting(2640, .9), .6)
    p.air(2.66, suite(['C6', 'E6', 'G6', 'C7'], .07, .5), xylo, .7)
    p.met(3.3, tada(587, .5), .5); p.met(2.7, scintille(1.8, 5), .28)

@scene
def s_encore(p):
    for k in range(1, 11): p.met(.419 * k, pas(), .3)                         # il saute sur place
    p.air(.6, [(0, 'G5', .3), (.18, 'C6', .5)], xylo, .8); p.met(.6, clap(), .6); p.met(.78, clap(), .6)
    p.air(2.4, [(0, 'A5', .3), (.18, 'D6', .5)], xylo, .85); p.met(2.4, clap(), .6); p.met(2.58, clap(), .6)
    p.air(3.4, [(0, 'C6', .3), (.18, 'E6', .3), (.36, 'G6', .6)], xylo, .9)
    for t in (3.4, 3.58, 3.76): p.met(t, clap(), .7)
    p.met(3.85, tada(659, .5), .5)

@scene
def s_oui(p):
    p.air(1.3, [(0, 'C6', .3), (.16, 'G6', .7)], xylo, 1); p.met(1.3, hum(glisse((440, 440, .12), (440, 660, .2)), .32, vib=0), .6)
    p.met(1.47, ting(2800, .6), .5)                                           # le pouce levé
    p.air(3.0, [(0, 'C6', .3), (.22, 'E6', .3), (.44, 'G6', .7)], xylo, .9)
    for i in range(3): p.met(3.0 + i * .22, hum(glisse((460 + 40 * i, 620 + 60 * i, .16)), .16, vib=0), .5)
    p.met(1.3, scintille(2.9, 4), .22)


def durees():
    """Les durées des scènes, lues dans humeurs.txt (fabriqué par fabrique.js)."""
    d = {}
    for ligne in open(os.path.join(SORTIE, 'humeurs.txt'), encoding='utf-8'):
        c = ligne.strip().split('|')
        if len(c) >= 5 and not ligne.startswith('#'): d[c[0]] = float(c[4])
    return d


def ecrit_wav(chemin, y):
    with wave.open(chemin, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((np.clip(y, -1, 1) * 32767).astype('<i2').tobytes())


def main():
    args = sys.argv[1:]; dossier_wav = None
    if '--wav' in args: i = args.index('--wav'); dossier_wav = args[i + 1]; del args[i:i + 2]; os.makedirs(dossier_wav, exist_ok=True)
    D = durees()
    manquent = [c for c in D if c not in S]
    if manquent: print('⚠ scènes sans bande-son :', ' '.join(manquent))
    with tempfile.TemporaryDirectory() as tmp:
        for i, cle in enumerate(D):
            if cle not in S or (args and cle not in args): continue
            graine(1000 + i)
            p = Piste(D[cle]); S[cle](p); y = p.rendu()
            wav = os.path.join(dossier_wav or tmp, cle + '.wav'); ecrit_wav(wav, y)
            film = os.path.join(SORTIE, cle + '.mp4'); neuf = os.path.join(tmp, cle + '.mp4')
            if os.path.exists(film):
                subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', film, '-i', wav, '-map', '0:v', '-map', '1:a', '-c:v', 'copy', '-c:a', 'aac', '-b:a', '64k', '-movflags', '+faststart', neuf], check=True)
                os.replace(neuf, film)
            print('%-12s %.1f s  efficace %.3f' % (cle, D[cle], float(np.sqrt(np.mean(y ** 2)))))


if __name__ == '__main__':
    main()
