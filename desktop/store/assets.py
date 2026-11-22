# Fabrique les images du paquet Microsoft Store (dossier Assets) à partir de l'icône d'Eyzi,
# redessinée en grand pour rester nette à toutes les tailles.
#   python assets.py        (il faut Pillow)
from PIL import Image, ImageDraw
import os

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Assets")
os.makedirs(OUT, exist_ok=True)
S = 1024  # taille de travail

def icone():
    k = S / 256.0
    # Le fond : carré arrondi, violet nuit, un peu plus clair en haut.
    fond = Image.new("RGBA", (S, S))
    d = ImageDraw.Draw(fond)
    haut, bas = (0x3B, 0x27, 0x63), (0x2A, 0x1C, 0x4E)
    for y in range(S):
        t = y / (S - 1)
        d.line([(0, y), (S, y)], fill=tuple(int(haut[i] + (bas[i] - haut[i]) * t) for i in range(3)) + (255,))
    masque = Image.new("L", (S, S), 0)
    ImageDraw.Draw(masque).rounded_rectangle([0, 0, S - 1, S - 1], radius=int(58 * k), fill=255)
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    im.paste(fond, (0, 0), masque)
    d = ImageDraw.Draw(im)
    jaune = (0xFF, 0xD2, 0x4A, 255)
    c = lambda x, y, r, col: d.ellipse([(x - r) * k, (y - r) * k, (x + r) * k, (y + r) * k], fill=col)
    c(128, 70, 29, jaune)                       # la tête
    c(138, 68, 12.5, (255, 255, 255, 255))      # l'œil
    c(140, 69, 6.5, (0x2A, 0x1C, 0x4E, 255))    # la pupille
    d.rounded_rectangle([104 * k, 108 * k, 152 * k, 212 * k], radius=24 * k, fill=jaune)  # le corps
    return im

ICONE = icone()

def carre(taille, part=1.0):
    """L'icône dans un carré transparent ; « part » = la place qu'elle y prend."""
    im = Image.new("RGBA", (taille, taille), (0, 0, 0, 0))
    n = max(1, int(round(taille * part)))
    ic = ICONE.resize((n, n), Image.LANCZOS)
    im.paste(ic, ((taille - n) // 2, (taille - n) // 2), ic)
    return im

def large(l, h, part=0.70):
    im = Image.new("RGBA", (l, h), (0, 0, 0, 0))
    n = int(round(h * part))
    ic = ICONE.resize((n, n), Image.LANCZOS)
    im.paste(ic, ((l - n) // 2, (h - n) // 2), ic)
    return im

def ecrire(im, nom):
    im.save(os.path.join(OUT, nom), optimize=True)

for e in (100, 200, 400):
    ecrire(carre(44 * e // 100), "Square44x44Logo.scale-%d.png" % e)
    ecrire(carre(150 * e // 100, 0.66), "Square150x150Logo.scale-%d.png" % e)
    ecrire(carre(50 * e // 100), "StoreLogo.scale-%d.png" % e)
for e in (100, 200):
    ecrire(large(310 * e // 100, 150 * e // 100), "Wide310x150Logo.scale-%d.png" % e)
for t in (16, 24, 32, 48, 256):
    ecrire(carre(t), "Square44x44Logo.targetsize-%d.png" % t)
    ecrire(carre(t), "Square44x44Logo.targetsize-%d_altform-unplated.png" % t)
# L'image pour la fiche du Store (300 × 300, demandée par l'espace partenaires).
ecrire(carre(300), "../fiche-300.png")
print(len(os.listdir(OUT)), "images dans", OUT)
