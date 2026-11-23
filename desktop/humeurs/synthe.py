# Le petit synthétiseur des humeurs animées : tous les bruits sont calculés (aucun fichier son à côté).
# Chaque fonction rend un son (un tableau numpy, à 44,1 kHz, dont la plus forte valeur vaut 1).
# sons.py s'en sert pour écrire la bande-son de chaque scène.
import numpy as np
import scipy.signal as sg

SR = 44100
_alea = np.random.default_rng(54)


def graine(n):
    """Repart d'un hasard connu : la même scène donne toujours la même bande-son."""
    global _alea
    _alea = np.random.default_rng(n)


def nb(d): return max(1, int(round(d * SR)))
def temps(n): return np.arange(n) / SR
def norm(x, p=1.0):
    m = np.max(np.abs(x)) if len(x) else 0
    return x * (p / m) if m > 0 else x
def bruit(d): return _alea.uniform(-1, 1, nb(d))
def hasard(a=0.0, b=1.0): return float(_alea.uniform(a, b))


def freq(f, n):
    """Une fréquence fixe, ou une suite de fréquences étirée sur n échantillons."""
    f = np.asarray(f, float)
    return np.full(n, float(f)) if f.ndim == 0 else np.interp(np.linspace(0, 1, n), np.linspace(0, 1, len(f)), f)


def glisse(*morceaux):
    """Des fréquences qui glissent : glisse((f0, f1, durée), (f1, f2, durée)…)."""
    return np.concatenate([np.linspace(a, b, nb(d)) for a, b, d in morceaux])


def env(n, a=.005, r=.05, forme=1.5):
    """Une enveloppe : montée en a secondes, descente en r secondes."""
    e = np.ones(n); na = min(n, int(a * SR)); nr = min(n - na, int(r * SR))
    if na > 0: e[:na] = np.linspace(0, 1, na)
    if nr > 0: e[n - nr:] *= np.linspace(1, 0, nr) ** forme
    return e


def chute(n, tau): return np.exp(-temps(n) / tau)
def cloche_env(n): return np.sin(np.pi * np.linspace(0, 1, n)) ** 2


def filtre(x, genre, f, ordre=2):
    return sg.sosfilt(sg.butter(ordre, np.array(f, float) / (SR / 2), genre, output='sos'), x)
def bas(x, f, o=2): return filtre(x, 'low', f, o)
def haut(x, f, o=2): return filtre(x, 'high', f, o)
def bande(x, lo, hi, o=2): return filtre(x, 'band', [lo, hi], o)
def pic(x, f, q):
    b, a = sg.iirpeak(f / (SR / 2), q)
    return sg.lfilter(b, a, x)


SCIE = [1 / k for k in range(1, 25)]
CARRE = [(1 / k if k % 2 else 0) for k in range(1, 25)]
GLOTTE = [1 / k ** 1.3 for k in range(1, 31)]


def onde(f, d, harmoniques=(1,)):
    """Une onde faite d'harmoniques (sans repliement) ; f fixe ou glissante."""
    n = nb(d); f = freq(f, n); ph = 2 * np.pi * np.cumsum(f) / SR; y = np.zeros(n)
    for k, a in enumerate(harmoniques, 1):
        if a: y += a * np.sin(k * ph) * (k * f < SR * .45)
    return y


NOTES = {'C': 0, 'D': 2, 'E': 4, 'F': 5, 'G': 7, 'A': 9, 'B': 11}
def note(nom):
    """« C5 », « F#4 », « Bb3 » → hertz."""
    demi = NOTES[nom[0]] + (1 if '#' in nom else -1 if 'b' in nom[1:] else 0)
    return 440 * 2 ** ((demi - 9) / 12 + int(nom[-1]) - 4)


# ───────────── La petite voix du personnage ─────────────
FORMANTS = {'a': (900, 1500, 2900), 'è': (650, 2000, 2900), 'e': (480, 2100, 2900), 'i': (340, 2600, 3400), 'o': (520, 950, 2800),
            'ou': (360, 820, 2600), 'eu': (450, 1450, 2700), 'm': (270, 1150, 2500)}


def _formants(src, v):
    y = np.zeros_like(src)
    # des résonances larges, par-dessus le son de la gorge : la note reste pleine même quand elle est aiguë
    for f, g, largeur in zip(FORMANTS[v], (1, .6, .3), (220, 280, 380)):
        y += g * pic(src, f, f / largeur)
    return y + .4 * bas(src, 1100, 1)


def voix(f0, d, v='a', v2=None, souffle=.04, vib=0, rauque=0, nez=0, a=.02, r=.06):
    """Une voyelle chantée par une petite voix : f0 fixe ou glissante, v → v2 d'un bout à l'autre."""
    n = nb(d); f = freq(f0, n); t = temps(n)
    if vib: f = f * (1 + vib * np.sin(2 * np.pi * 5.5 * t))
    src = onde(f, d, GLOTTE)
    if rauque: src = src * (1 + .7 * rauque * np.sin(np.pi * np.cumsum(f) / SR)) + .25 * rauque * bas(bruit(d), 2500)
    src = src + souffle * 3 * bruit(d)
    y = _formants(src, v)
    if v2: w = np.linspace(0, 1, n); y = (1 - w) * y + w * _formants(src, v2)
    if nez: y = (1 - nez) * y + nez * 1.6 * _formants(src, 'm')
    return norm(y * env(n, a, r))


def rire(n=4, f0=560, v='a', pas=.17, baisse=.94, d=.11):
    """« Ha ha ha ! » : des voyelles brèves, chacune un peu plus bas et un peu moins fort."""
    y = np.zeros(nb(pas * n + .1))
    for i in range(n):
        f = f0 * baisse ** i * hasard(.98, 1.02)
        s = voix(glisse((f * 1.1, f * .88, d)), d, v, souffle=.12, a=.012, r=.05)
        h = norm(_formants(bruit(.04), v)) * env(nb(.04), .01, .02) * .25      # le « h » soufflé
        k = nb(i * pas)
        y[k:k + len(h)] += h * .9 ** i
        k += nb(.03)
        y[k:k + len(s)] += s * .92 ** i
    return norm(y)


def sanglots(n=3, f0=470, pas=.36):
    """« Bouhouhou… »"""
    y = np.zeros(nb(pas * n + .2))
    for i in range(n):
        f = f0 * .95 ** i
        s = voix(glisse((f * 1.12, f, .08), (f, f * .82, pas - .12)), pas - .04, 'ou', souffle=.08, vib=.02, a=.03, r=.1)
        s *= 1 - .35 * np.sin(2 * np.pi * 9 * temps(len(s))) ** 2
        k = nb(i * pas); y[k:k + len(s)] += s
    return norm(y)


def baille(d=1.2, f0=360):
    """« Ouaaah… » : la voix monte, puis retombe longuement."""
    return voix(glisse((f0 * .8, f0 * 1.25, d * .35), (f0 * 1.25, f0 * .6, d * .65)), d, 'o', 'a', souffle=.12, a=.15, r=.4)


def aah(d=.9, f0=520):
    """« Aaah ! » de contentement."""
    return voix(glisse((f0, f0 * 1.06, d * .2), (f0 * 1.06, f0 * .7, d * .8)), d, 'a', souffle=.14, a=.05, r=.4)


def oh(d=.45, f0=430, monte=True):
    """« Oh ? » (qui monte) ou « Oooh ! » (qui monte puis redescend)."""
    f = glisse((f0, f0 * 1.45, d)) if monte else glisse((f0, f0 * 1.3, d * .4), (f0 * 1.3, f0 * .95, d * .6))
    return voix(f, d, 'o', souffle=.06, a=.03, r=.12)


def hum(f, d, vib=.012, a=.06, r=.15):
    """« Mmmmh » : bouche fermée."""
    return voix(f, d, 'm', souffle=.02, vib=vib, a=a, r=r)


def aie(d=.38, f0=620):
    """« Aïe ! »"""
    return voix(glisse((f0, f0 * 1.35, d * .35), (f0 * 1.35, f0 * .8, d * .65)), d, 'a', 'i', souffle=.06, a=.01, r=.1)


def cri(d=.75, f0=760):
    """« AAAAH ! » : aigu, qui tremble."""
    s = voix(glisse((f0 * .8, f0, .08), (f0, f0 * 1.08, d - .08)), d, 'a', souffle=.08, vib=.035, a=.02, r=.15)
    return norm(s * (1 - .3 * np.sin(2 * np.pi * 13 * temps(len(s))) ** 2))


def hic(fort=1.0):
    """« Hic ! » : un petit cri qui grimpe et s'arrête net."""
    d = .085 * (1 + .5 * (fort - 1))
    s = voix(np.linspace(430, 1050 * fort ** .3, nb(d)) , d, 'i', souffle=.02, a=.006, r=.012)
    return norm(np.concatenate([s, np.zeros(nb(.02))]))


def grogne(d=1.3, f0=120):
    """« GRRRR ! » : grave, rauque, de plus en plus fort."""
    s = voix(glisse((f0, f0 * 1.25, d)), d, 'eu', souffle=.1, rauque=1, a=.08, r=.12)
    t = temps(len(s))
    return norm(s * (1 - .55 * np.sin(2 * np.pi * 27 * t) ** 2) * np.linspace(.5, 1, len(s)))


def hmpf(fort=1.0):
    """« Hmpf ! » : un grognement bref, puis l'air qui sort par le nez."""
    g = voix(glisse((230, 180, .13)), .13, 'm', souffle=.05, rauque=.4, a=.01, r=.04)
    p = norm(bande(bruit(.22), 900, 4000)) * env(nb(.22), .01, .18) * .7
    y = np.zeros(nb(.36)); y[:len(g)] += g; y[nb(.1):nb(.1) + len(p)] += p
    return norm(y) * min(1, fort)


def atchoum():
    """« ATCHOUM ! » : l'explosion, puis « oum »."""
    tch = norm(haut(bruit(.11), 1800)) * env(nb(.11), .004, .08)
    oum = voix(glisse((520, 300, .28)), .28, 'ou', 'm', souffle=.08, a=.01, r=.12)
    y = np.zeros(nb(.42)); y[:len(tch)] += tch; y[nb(.07):nb(.07) + len(oum)] += oum * .9
    return norm(y)


def chante(notes, v='a', lie=.9, vib=.012):
    """Une mélodie chantée : notes = [(instant, hertz, durée)…] ; chaque note s'attaque comme un « la »."""
    fin = max(t + d for t, f, d in notes)
    y = np.zeros(nb(fin + .1))
    for t, f, d in notes:
        s = voix(glisse((f * .94, f, .04), (f, f, d * lie - .04)), d * lie, v, souffle=.03, vib=vib if d > .3 else 0, a=.035, r=.07)
        k = nb(t); y[k:k + len(s)] += s
    return norm(y)


# ───────────── Les instruments ─────────────
def ting(f=2100, d=.9):
    """Une clochette claire (l'éclat d'une étoile, une idée)."""
    n = nb(d); t = temps(n); y = np.zeros(n)
    for r, a, tau in ((1, 1, .32), (2.76, .5, .2), (5.4, .25, .12), (8.93, .12, .07)):
        if f * r < SR * .45: y += a * np.sin(2 * np.pi * f * r * t) * np.exp(-t / tau)
    return norm(y * env(n, .001, .05))


def boite(f, d=1.1):
    """Une note de boîte à musique."""
    n = nb(d); t = temps(n)
    y = np.sin(2 * np.pi * f * t) * np.exp(-t / .38) + .35 * np.sin(2 * np.pi * 2 * f * t) * np.exp(-t / .16) + .16 * np.sin(2 * np.pi * 3.01 * f * t) * np.exp(-t / .07)
    return norm(y * env(n, .002, .08))


def xylo(f, d=.4):
    n = nb(d); t = temps(n)
    y = np.sin(2 * np.pi * f * t) * np.exp(-t / .11) + .45 * np.sin(2 * np.pi * 4 * f * t) * np.exp(-t / .028) + .2 * np.sin(2 * np.pi * 9.2 * f * t) * np.exp(-t / .012)
    return norm(y * env(n, .001, .03))


def corde(f, d=.9, eclat=.5):
    """Une corde pincée (harpe, contrebasse) — Karplus-Strong."""
    n = nb(d); N = max(2, int(round(SR / f)))
    a = np.zeros(N + 2); a[0] = 1; a[N] = -.5 * .996; a[N + 1] = -.5 * .996
    x = np.zeros(n); x[:N] = bas(_alea.uniform(-1, 1, N), 900 + 6000 * eclat) if N > 12 else _alea.uniform(-1, 1, N)
    return norm(sg.lfilter([1], a, x) * env(n, .001, .1))


def cuivre(f, d, a=.035, vib=.006):
    """Une note de trompette."""
    n = nb(d); t = temps(n); fr = freq(f, n) * (1 + vib * np.sin(2 * np.pi * 5.5 * t) * np.clip(t / .3, 0, 1))
    y = onde(fr, d, [1 / k ** .9 for k in range(1, 15)])
    clair = bas(y, 2400); sombre = bas(y, 700); w = np.clip(t / .06, 0, 1)      # elle s'éclaircit en s'attaquant
    return norm(((1 - w) * sombre + w * clair) * env(n, a, .08))


def synthe(f, d, a=.01):
    """Un petit clavier électronique."""
    return norm(bas(onde(f, d, CARRE), 2600) * env(nb(d), a, .06) * chute(nb(d), .5))


def basse(f, d):
    n = nb(d)
    return norm((bas(onde(f, d, SCIE), 500) + .6 * onde(f, d)) * env(n, .008, .05) * chute(n, .35))


def sifflet(f, d, vib=.012, a=.03, r=.06):
    """Un sifflement, une flûte à coulisse : f fixe ou glissante."""
    n = nb(d); t = temps(n); fr = freq(f, n) * (1 + vib * np.sin(2 * np.pi * 6 * t))
    y = onde(fr, d, (1, .12, .04)) + .1 * norm(bande(bruit(d), 1500, 6000))
    return norm(y * env(n, a, r))


def pouet(f=440, d=.8):
    """La langue de belle-mère : « Pouêêêt ! »"""
    n = nb(d); t = temps(n)
    fr = np.concatenate([np.linspace(f * .72, f, nb(.09)), np.full(n - nb(.09), f)])[:n] * (1 + .01 * np.sin(2 * np.pi * 7 * t))
    y = bande(onde(fr, d, [1 / k ** .6 for k in range(1, 20)]), 500, 3800)
    return norm(y * (1 - .22 * np.sin(2 * np.pi * 41 * t) ** 2) * env(n, .012, .05))


def klaxon(f=430, d=.2):
    """La poire du clown : « Pouet ! »"""
    n = nb(d); fr = np.linspace(f * 1.04, f * .93, n)
    y = onde(fr, d, [1 / k ** .7 for k in range(1, 18)]) + .7 * onde(fr * 1.26, d, [1 / k ** .7 for k in range(1, 14)])
    return norm(bande(y, 350, 3000) * env(n, .006, .05))


def boing(f=170, d=.55):
    """Le ressort."""
    n = nb(d); t = temps(n)
    fr = f * (1 + .7 * t / d) * (1 + .3 * np.sin(2 * np.pi * 11 * t) * np.exp(-t * 3))
    return norm(bas(onde(fr, d, SCIE), 2600) * np.exp(-t / (.3 * d / .55)) * env(n, .003, .05))


def pop(f=950):
    """« Poc » : une bulle qui éclate, un confetti."""
    d = .07; n = nb(d); t = temps(n)
    y = np.sin(2 * np.pi * np.cumsum(np.linspace(f, f * .3, n)) / SR) * np.exp(-t / .018)
    y[:nb(.003)] += .5 * _alea.uniform(-1, 1, nb(.003))
    return norm(y)


def bouchon():
    """« POP ! » : un gros bouchon qui saute."""
    n = nb(.22); t = temps(n)
    y = np.sin(2 * np.pi * np.cumsum(np.linspace(520, 110, n)) / SR) * np.exp(-t / .05) + .8 * norm(bande(bruit(.22), 900, 5000)) * np.exp(-t / .025)
    return norm(y)


def clap():
    n = nb(.13); t = temps(n); e = np.zeros(n)
    for d0 in (0, .009, .02): k = nb(d0); e[k:] += np.exp(-t[:n - k] / .011)
    e += .35 * np.exp(-t / .05)
    return norm(bande(bruit(.13), 700, 3800) * e)


def claquement():
    """Un claquement de doigts."""
    n = nb(.07); t = temps(n)
    return norm(bande(bruit(.07), 1800, 5200) * np.exp(-t / .012) + .4 * np.sin(2 * np.pi * 2400 * t) * np.exp(-t / .006))


def applaudit(d, force=16):
    """Une salle qui applaudit."""
    n = nb(d); y = np.zeros(n + nb(.2)); t = 0.0
    while t < d:
        c = clap() * hasard(.3, 1); k = nb(t); y[k:k + len(c)] += c
        t += _alea.exponential(1 / force)
    y = y[:n] + .25 * norm(bande(bruit(d), 1000, 5000))
    return norm(y * env(n, .25, .7))


def grosse_caisse(f=150):
    n = nb(.3); t = temps(n)
    y = np.sin(2 * np.pi * np.cumsum(45 + (f - 45) * np.exp(-t / .03)) / SR) * np.exp(-t / .11)
    y[:nb(.004)] += .3 * _alea.uniform(-1, 1, nb(.004))
    return norm(y)


def caisse_claire(d=.16):
    n = nb(d); t = temps(n)
    return norm(bande(bruit(d), 1400, 7500) * np.exp(-t / .06) + .5 * np.sin(2 * np.pi * 190 * t) * np.exp(-t / .045))


def charley(d=.05):
    n = nb(d)
    return norm(haut(bruit(d), 7000) * np.exp(-temps(n) / .016))


def cymbale(d=1.1):
    n = nb(d); t = temps(n)
    y = haut(bruit(d), 4500) * np.exp(-t / .32)
    for f in (3150, 4270, 5830, 7410): y += .12 * np.sin(2 * np.pi * f * t) * np.exp(-t / .25)
    return norm(y * env(n, .002, .2))


def roulement(d=1.0):
    """Un roulement de tambour qui enfle."""
    n = nb(d); y = np.zeros(n + nb(.2)); t = 0.0
    while t < d:
        c = caisse_claire(.09) * (.25 + .75 * (t / d) ** 1.5) * hasard(.8, 1); k = nb(t); y[k:k + len(c)] += c
        t += 1 / 27
    return norm(y[:n])


def boum(grave=1.0):
    """Un coup sourd : un pied qui tape, une chute."""
    n = nb(.35); t = temps(n)
    y = np.sin(2 * np.pi * np.cumsum(38 + 70 * grave * np.exp(-t / .035)) / SR) * np.exp(-t / .1) + .6 * norm(bas(bruit(.35), 350)) * np.exp(-t / .05)
    return norm(y)


def poc(f=700):
    """Un coup sec et creux : la tête contre le ballon, un bloc de bois."""
    n = nb(.12); t = temps(n)
    return norm(np.sin(2 * np.pi * f * t) * np.exp(-t / .028) + .5 * np.sin(2 * np.pi * f * 1.62 * t) * np.exp(-t / .016))


def pas():
    n = nb(.1); t = temps(n)
    return norm(.8 * norm(bas(bruit(.1), 600)) * np.exp(-t / .022) + np.sin(2 * np.pi * 105 * t) * np.exp(-t / .035))


def tic(f=2300):
    n = nb(.06); t = temps(n)
    return norm(np.sin(2 * np.pi * f * t) * np.exp(-t / .007) + .5 * np.sin(2 * np.pi * f * 1.5 * t) * np.exp(-t / .004))


def tada(f=523.25, long=.75):
    """« Ta-daa ! »"""
    y = np.zeros(nb(.2 + long + .1))
    for r in (1, 1.26, 1.5):
        a = cuivre(f * r, .13); y[:len(a)] += a
        b = cuivre(f * r * 1.335, long); y[nb(.17):nb(.17) + len(b)] += b
    return norm(y)


def fanfare():
    """La fanfare de la victoire."""
    airs = [(0, 'G4', .12), (.13, 'C5', .12), (.26, 'E5', .12), (.39, 'G5', .3), (.72, 'E5', .12), (.86, 'G5', .8)]
    y = np.zeros(nb(1.8))
    for t, nom, d in airs:
        for r, g in ((1, 1), (.5, .5), (1.26 if nom == 'G5' and d > .5 else 1, .4)):
            s = cuivre(note(nom) * r, d) * g; k = nb(t); y[k:k + len(s)] += s
    return norm(y)


def trombone_triste(n=4):
    """« Ouin ouin ouin ouiiin… »"""
    hauteurs = [233.1, 220, 207.7, 196][:n]; y = np.zeros(nb(.3 * n + .7)); t = 0.0
    for i, f in enumerate(hauteurs):
        d = .85 if i == len(hauteurs) - 1 else .27
        m = nb(d); tt = temps(m)
        s = onde(np.linspace(f, f * (.94 if d > .5 else .985), m) * (1 + (.02 if d > .5 else 0) * np.sin(2 * np.pi * 5.5 * tt)), d, SCIE)
        ouvert = np.sin(np.pi * np.linspace(0, 1, m)) ** .7             # la sourdine s'ouvre et se referme : « oua »
        s = norm(((1 - ouvert) * bas(s, 450) + ouvert * bas(s, 1500)) * env(m, .03, .08))
        k = nb(t); y[k:k + m] += s; t += d + .03
    return norm(y)


def harpe(notes, ecart=.045, d=1.2):
    """Un glissando de harpe."""
    y = np.zeros(nb(ecart * len(notes) + d))
    for i, f in enumerate(notes):
        s = corde(f, d, .7); k = nb(i * ecart); y[k:k + len(s)] += s
    return norm(y)


def melodie(notes, instrument=boite, **o):
    """Une mélodie : notes = [(instant, hertz ou nom, durée)…]."""
    fin = max(t + d for t, f, d in notes); y = np.zeros(nb(fin + 1.5))
    for t, f, d in notes:
        s = instrument(note(f) if isinstance(f, str) else f, d, **o); k = nb(t); y[k:k + len(s)] += s
    return norm(y[:nb(fin + 1.2)])


def scintille(d, par_seconde=7, grave=1800, aigu=4200):
    """Une pluie de petites clochettes : la magie, les étoiles."""
    n = nb(d); y = np.zeros(n + nb(.5)); t = 0.0
    while t < d:
        s = ting(hasard(grave, aigu), .35) * hasard(.3, 1); k = nb(t); y[k:k + len(s)] += s
        t += _alea.exponential(1 / par_seconde)
    return norm(y[:n] * env(n, .05, .3))


# ───────────── L'eau, l'air, le corps ─────────────
def bulle(f=500, d=.07):
    n = nb(d)
    return norm(np.sin(2 * np.pi * np.cumsum(np.linspace(f, f * 1.9, n)) / SR) * cloche_env(n))


def bulles(d, par_seconde=9, grave=350, aigu=1100, pousse=1.0):
    n = nb(d); y = np.zeros(n + nb(.2)); t = 0.0
    while t < d:
        s = bulle(hasard(grave, aigu), hasard(.04, .09)) * hasard(.3, 1); k = nb(t); y[k:k + len(s)] += s
        t += _alea.exponential(1 / par_seconde) / pousse
    return norm(y[:n])


def glou(f=250):
    """Une gorgée."""
    n1, n2 = nb(.05), nb(.07)
    fr = np.concatenate([np.linspace(f * 1.25, f * .72, n1), np.linspace(f * .72, f * 1.7, n2)])
    y = np.sin(2 * np.pi * np.cumsum(fr) / SR) * cloche_env(n1 + n2) ** .6
    return norm(bas(y, 1400))


def gloups():
    """La gorge qui se serre, le ventre qui gargouille."""
    v = voix(glisse((330, 215, .08), (215, 520, .08)), .16, 'ou', souffle=.02, a=.012, r=.03)
    b = bulle(160, .11)
    y = np.zeros(nb(.2)); y[:len(v)] += v; y[nb(.03):nb(.03) + len(b)] += .7 * b
    return norm(y)


def gargouille(d=.8):
    """Le ventre qui gargouille : « grrl… grl »."""
    n = nb(d); y = np.zeros(n + nb(.2)); t = 0.0
    while t < d:
        s = bulle(hasard(110, 260), hasard(.05, .1)) * hasard(.4, 1); k = nb(t); y[k:k + len(s)] += s
        t += hasard(.035, .09)
    y = y[:n] + .3 * norm(bas(bruit(d), 300)) * (.5 + .5 * np.sin(2 * np.pi * 9 * temps(n)))
    return norm(bas(y, 900) * cloche_env(n) ** .5)


def prout(d=.7, f0=98, f1=68, couine=True):
    """Le pet : un train de petites explosions, qui tremble, et finit en couinant."""
    n = nb(d); t = temps(n)
    fr = np.linspace(f0, f1, n) * (1 + .5 * bas(bruit(d), 14))
    if couine: fr[int(n * .78):] *= np.linspace(1, 1.9, n - int(n * .78))
    ph = (np.cumsum(fr) / SR) % 1
    coups = np.exp(-ph * 5.5)
    x = bande(coups - coups.mean(), 70, 1100) * 3 + .5 * norm(bande(bruit(d), 250, 1500)) * coups
    e = env(n, .012, .06) * (.75 + .25 * norm(bas(bruit(d), 7)))
    return norm(np.tanh(2.2 * norm(x)) * e)


def langue(d=.9):
    """« Prrrrt ! » : la langue tirée qui vibre."""
    n = nb(d); t = temps(n)
    ph = (np.cumsum(np.linspace(34, 27, n) * (1 + .3 * bas(bruit(d), 10))) / SR) % 1
    coups = np.exp(-ph * 3.2)
    x = norm(bande(bruit(d), 500, 4500)) * coups + .5 * bande(coups - coups.mean(), 100, 900) * 4
    return norm(np.tanh(1.8 * norm(x)) * env(n, .02, .08))


def souffle(d=.8, grave=500, aigu=3000, descend=True):
    """« Pfff… » : de l'air qui sort."""
    n = nb(d); w = np.linspace(0, 1, n)
    if not descend: w = 1 - w
    y = (1 - w) * norm(bande(bruit(d), aigu * .5, aigu)) + w * norm(bande(bruit(d), grave, grave * 2.5))
    return norm(y * env(n, .05, d * .6, 1.2))


def chut(d=1.1):
    """« Chuuut… »"""
    n = nb(d)
    return norm(bande(bruit(d), 2400, 7500) * env(n, .09, d * .5, 1.3))


def snif():
    """On renifle."""
    n = nb(.2); e = np.linspace(0, 1, n) ** 1.6; e[int(n * .85):] *= np.linspace(1, 0, n - int(n * .85))
    return norm(bande(bruit(.2), 1400, 5200) * e)


def ronfle(d=.9):
    """Un ronflement : « ron… »"""
    n = nb(d); t = temps(n)
    ph = (np.cumsum(np.linspace(30, 36, n)) / SR) % 1
    x = norm(bas(bruit(d), 700)) * np.exp(-ph * 3) + .4 * onde(np.linspace(62, 70, n), d, SCIE[:8]) * np.exp(-ph * 2)
    return norm(x * cloche_env(n) ** .7)


def pschh(d=.8):
    """… « pschhh » : l'air qui ressort en sifflant un peu."""
    n = nb(d)
    y = norm(haut(bruit(d), 2800)) + .25 * sifflet(np.linspace(1500, 1100, n), d, a=.2, r=.3)
    return norm(y * cloche_env(n) ** .8)


def vent(d, force=1.0):
    """Le vent qui siffle."""
    n = nb(d); t = temps(n)
    centre = 650 + 350 * np.sin(2 * np.pi * .23 * t + 1) + 200 * np.sin(2 * np.pi * .61 * t)
    y = .6 * norm(bande(bruit(d), 250, 1400)) * (.6 + .4 * np.sin(2 * np.pi * .31 * t))
    y += .35 * sifflet(centre, d, vib=.004, a=.4, r=.6) * (.5 + .5 * np.sin(2 * np.pi * .19 * t + 2)) ** 2
    return norm(y * env(n, .5, .8)) * force


def pluie(d):
    n = nb(d); y = .5 * norm(haut(bruit(d), 3500))
    for _ in range(int(d * 26)):
        k = int(hasard(0, n - 400)); s = tic(hasard(2500, 5200)) * hasard(.1, .5); y[k:k + len(s)] += s[:n - k]
    return norm(y * env(n, .5, .7))


def crac():
    """Un craquement sec, puis ça gronde."""
    n = nb(.9); t = temps(n)
    y = norm(haut(bruit(.9), 900)) * np.exp(-t / .035)
    for d0 in (.03, .07): k = nb(d0); y[k:] += .6 * norm(haut(_alea.uniform(-1, 1, n - k), 1200)) * np.exp(-t[:n - k] / .02)
    y += .8 * norm(bas(bruit(.9), 160)) * np.exp(-t / .3)
    return norm(y)


def casse():
    """Quelque chose qui se casse : un craquement et des petits éclats."""
    n = nb(.7); y = np.zeros(n); c = croque(.14); y[:len(c)] += c
    for _ in range(6):
        k = nb(hasard(.05, .4)); s = ting(hasard(2600, 5200), .25) * hasard(.2, .6); y[k:k + len(s)] += s[:n - k]
    return norm(y)


def croque(d=.25):
    """« CROUNCH ! »"""
    n = nb(d); e = np.zeros(n); t = temps(n)
    for _ in range(int(d * 90)):
        k = int(hasard(0, n * .8)); e[k:] += hasard(.3, 1) * np.exp(-t[:n - k] / .004)
    return norm(bande(bruit(d), 900, 6500) * e * env(n, .001, d * .5))


def mache(n=4, pas_=.3):
    """On mâche : « miam miam »."""
    y = np.zeros(nb(pas_ * n + .2))
    for i in range(n):
        s = bas(croque(.09), 2200) * hasard(.6, 1); k = nb(i * pas_ + hasard(0, .03)); y[k:k + len(s)] += s
    return norm(y)


def plip(f=1300):
    """Une goutte qui tombe."""
    n = nb(.25); t = temps(n); m = nb(.035)
    g = np.sin(2 * np.pi * np.cumsum(np.linspace(f, f * .5, m)) / SR) * np.exp(-temps(m) / .012)
    y = np.zeros(n); y[:m] += g; k = nb(.1); y[k:k + m] += .3 * g
    return norm(y)


def plouf(d=1.0):
    """Un grand plouf."""
    n = nb(d); t = temps(n)
    y = norm(bande(bruit(d), 500, 6500)) * (np.exp(-t / .22) + .25 * np.exp(-t / .6)) * env(n, .008, .2)
    b0 = boum(.6)[:n]; y[:len(b0)] += .9 * b0
    b = bulles(d - .15, 16, 250, 900) * np.exp(-temps(nb(d - .15)) / .4)
    y[nb(.15):nb(.15) + len(b)] += .6 * b[:n - nb(.15)]
    return norm(y)


def splat():
    """« SPLAT ! » : quelque chose de mou qui s'écrase."""
    n = nb(.45); t = temps(n)
    y = norm(bas(bruit(.45), 2600)) * np.exp(-t / .07) * (1 + .5 * np.sin(2 * np.pi * 38 * t))
    y[:nb(.35)] += .8 * boum(.5)
    for d0 in (.06, .12, .2): s = bulle(hasard(180, 420), .06); k = nb(d0); y[k:k + len(s)] += .5 * s
    return norm(y)


def whoosh(d=.3, monte=True):
    """Un geste vif qui fend l'air."""
    n = nb(d); y = np.zeros(n); bandes = [(300, 900), (700, 2000), (1500, 4000), (3000, 7500)]
    if not monte: bandes = bandes[::-1]
    for i, (lo, hi) in enumerate(bandes):
        c = (i + .5) / len(bandes); e = np.exp(-((np.linspace(0, 1, n) - c) / .22) ** 2)
        y += norm(bande(bruit(d), lo, hi)) * e
    return norm(y * cloche_env(n) ** .4)


def bisou(fort=1.0):
    """« Smack ! » : les lèvres qui aspirent, puis qui claquent."""
    n = nb(.09); e = np.linspace(0, 1, n) ** 2; e[int(n * .9):] *= np.linspace(1, 0, n - int(n * .9))
    asp = np.sin(2 * np.pi * np.cumsum(np.linspace(1000, 3400, n)) / SR) * e + .3 * norm(bande(bruit(.09), 2500, 6000)) * e
    y = np.zeros(nb(.2)); y[:n] += norm(asp) * .8; p = pop(700); y[n:n + len(p)] += p
    return norm(y) * min(1, fort)


def coeur_bat(f=62):
    """« Boum-boum » : un battement de cœur."""
    def coup(f, tau):
        n = nb(.14); t = temps(n)
        return np.sin(2 * np.pi * np.cumsum(f * (1 + .6 * np.exp(-t / .02))) / SR) * np.exp(-t / tau)
    y = np.zeros(nb(.36)); a = coup(f, .045); b = coup(f * .85, .035)
    y[:len(a)] += a; y[nb(.16):nb(.16) + len(b)] += .75 * b
    return norm(y)


def mouche(d, f=215):
    """Une mouche qui tourne."""
    n = nb(d); t = temps(n)
    fr = f * (1 + .08 * norm(bas(bruit(d), 3)) + .03 * np.sin(2 * np.pi * 1.3 * t))
    y = bande(onde(fr, d, SCIE[:14]), 180, 3200) * (1 - .4 * np.sin(2 * np.pi * 29 * t) ** 2)
    return norm(y * (.55 + .45 * np.sin(2 * np.pi * .43 * t + 1) ** 2) * env(n, .15, .25))


def couic():
    """Un petit couinement (la souris, le jouet du bain)."""
    fr = glisse((1700, 2700, .05), (2700, 1500, .09))
    n = len(fr)
    return norm(onde(fr, n / SR, (1, .3, .15)) * env(n, .008, .04))


def coincoin():
    """Le canard du bain."""
    def coin():
        fr = glisse((520, 470, .03), (470, 360, .1)); d = len(fr) / SR
        return norm(bande(onde(fr, d, [1 / k ** .5 for k in range(1, 20)]), 800, 3400) * env(len(fr), .006, .04))
    y = np.zeros(nb(.42)); a = coin(); y[:len(a)] += a; y[nb(.2):nb(.2) + len(a)] += a
    return norm(y)


def cuicui(n=2, f=3600):
    """Un oiseau."""
    y = np.zeros(nb(.16 * n + .1))
    for i in range(n):
        fr = glisse((f * hasard(.9, 1), f * 1.35, .05), (f * 1.35, f * 1.05, .05))
        s = np.sin(2 * np.pi * np.cumsum(fr) / SR) * cloche_env(len(fr)); k = nb(i * .16); y[k:k + len(s)] += s
    return norm(y)


def frotte(d, par_seconde=4.0, grave=1200, aigu=4500):
    """On frotte, on brosse : un va-et-vient."""
    n = nb(d); t = temps(n)
    return norm(bande(bruit(d), grave, aigu) * (.15 + .85 * np.abs(np.sin(np.pi * par_seconde * t)) ** 1.5) * env(n, .05, .1))


def claque_des_dents(d, par_seconde=15):
    n = nb(d); y = np.zeros(n + nb(.1)); t = 0.0; i = 0
    while t < d:
        s = tic(1900 if i % 2 else 2500) * hasard(.5, 1); k = nb(t); y[k:k + len(s)] += s
        t += 1 / par_seconde * hasard(.85, 1.15); i += 1
    return norm(y[:n])


def cigales(d):
    """La chaleur : les cigales."""
    n = nb(d); t = temps(n)
    y = haut(bruit(d), 4800) * (.35 + .65 * (np.sin(2 * np.pi * 23 * t) > 0)) * (.6 + .4 * np.sin(2 * np.pi * .7 * t) ** 2)
    return norm(y * env(n, .3, .5))


def page():
    """Une page qu'on tourne."""
    n = nb(.26); w = np.linspace(0, 1, n)
    y = (1 - w) * norm(bande(bruit(.26), 1500, 4000)) + w * norm(bande(bruit(.26), 3500, 8500))
    return norm(y * cloche_env(n) ** .6)


def bips(n=3, f=2700):
    """Le thermomètre."""
    y = np.zeros(nb(.16 * n))
    for i in range(n):
        s = np.sin(2 * np.pi * f * temps(nb(.08))) * env(nb(.08), .004, .01); k = nb(i * .16); y[k:k + len(s)] += s
    return norm(y)


def tout_s_arrete(d=.4):
    """Le disque qu'on arrête d'un coup."""
    n = nb(d)
    y = bas(onde(np.linspace(520, 45, n), d, SCIE), 2500) + .3 * norm(bande(bruit(d), 600, 3000)) * np.linspace(1, 0, n)
    return norm(y * env(n, .005, .1))


def trompe(f=311, d=.8):
    """« TUUUT ! » : un gros klaxon."""
    n = nb(d)
    y = onde(f, d, [1 / k ** .7 for k in range(1, 22)]) + .8 * onde(f * 1.19, d, [1 / k ** .7 for k in range(1, 18)])
    return norm(bande(y, 250, 3500) * env(n, .02, .06))


def blam():
    """Une porte qui claque."""
    y = np.zeros(nb(.4)); b = boum(1.3); y[:len(b)] += b
    c = .8 * norm(bande(bruit(.2), 300, 2500)) * np.exp(-temps(nb(.2)) / .03); y[:len(c)] += c
    return norm(y)


def dzing():
    """Une cymbale qu'on cogne, du métal."""
    n = nb(.9); t = temps(n); y = np.zeros(n)
    for f, a in ((1320, 1), (2110, .7), (2930, .6), (3470, .5), (4780, .4), (6230, .3)): y += a * np.sin(2 * np.pi * f * t) * np.exp(-t / .22)
    return norm(y + .5 * norm(haut(bruit(.9), 5000)) * np.exp(-t / .12))


def batterie(d, tempo=124, motif='danse'):
    """Un rythme : grosse caisse, caisse claire, charleston."""
    temps_fort = 60 / tempo; y = np.zeros(nb(d + .5)); t = 0.0; i = 0
    while t < d:
        demi = i % 8                                  # huit croches par mesure
        def joue(s, g): k = nb(t); y[k:k + len(s)] += s * g
        if motif == 'danse':
            if demi % 2 == 0: joue(grosse_caisse(), 1)
            if demi in (2, 6): joue(caisse_claire(), .7)
            joue(charley(.05 if demi % 2 == 0 else .09), .35 if demi % 2 == 0 else .5)
        else:                                         # « tchic tchic boum »
            if demi in (0, 1, 4, 5): joue(charley(.07), .6)
            if demi in (2, 6): joue(grosse_caisse(), 1)
            if demi in (3, 7): joue(caisse_claire(.1), .35)
        t += temps_fort / 2; i += 1
    return norm(y[:nb(d)])
