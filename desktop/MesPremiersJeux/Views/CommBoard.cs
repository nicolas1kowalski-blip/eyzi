using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Views
{
    /// <summary>
    /// 💬 LA GRILLE DE COMMUNICATION : ouverte à tout moment par le bouton
    /// « Parler » (toujours visible, même en plein jeu), refermée d'un regard
    /// pour reprendre le jeu là où il était. Cinq pages de 12 grandes cases :
    /// l'essentiel (oui, non, encore, aide, j'ai mal...), les émotions, les
    /// envies, où j'ai mal, les gens et les mots gentils. Chaque case est dite
    /// à voix haute (ou avec la voix de papa/maman : Contenu\Voix\comm-clé-1.wav)
    /// et reste affichée en grand en haut, pour l'adulte qui arrive.
    /// CODE COULEUR par nature du mot, celui des classeurs de communication
    /// (Fitzgerald modifié, comme les pictogrammes ARASAAC) : les personnes en
    /// jaune, les verbes en vert, les noms en orange, les mots qui décrivent
    /// (émotions, états) en bleu, les mots sociaux en rose, les questions en
    /// violet ; « oui » en vert franc, « non » en rouge franc, la douleur en
    /// rouge clair. La couleur suit le mot, quelle que soit la page.
    /// </summary>
    public sealed class CommBoard : UserControl
    {
        public static bool IsOpen { get; private set; }

        /// <summary>Demande de fermeture (bouton « Retour au jeu »).</summary>
        public event Action CloseRequested;

        private sealed class Item
        {
            public string Key, Label, Say, Img, Emoji;
            public string Mot;              // le mot français à chercher dans ARASAAC, quand le libellé est une phrase
            public char Nat;                // la nature du mot, pour le code couleur (voir Couleur) ; 0 = d'après la clé
            public bool Alert;              // fait sonner avant de parler (appel, douleur)
            public Func<FrameworkElement> Photo; // photo de famille
        }

        private sealed class Category
        {
            public string Key, Icon, Name;
            public Color Tint;
            public Func<List<Item>> Items;
            public int MinLevel = 1; // la grille GRANDIT avec le niveau de l'enfant
        }

        /// <summary>Un mot du vocabulaire de l'enfant, vu de l'extérieur (écoute, prédiction).</summary>
        public sealed class Mot
        {
            public string Cle, Label, Dit, Img, Emoji;
            public Func<FrameworkElement> Photo;
        }

        /// <summary>Tout le vocabulaire du tableau (photos de la famille comprises), sans doublon de clé.</summary>
        public static List<Mot> Vocabulaire()
        {
            var res = new List<Mot>(); var vus = new HashSet<string>();
            IEnumerable<Item> all = Essentials.Concat(Feelings).Concat(Wants).Concat(Verbs).Concat(Body).Concat(Social).Concat(DayItems);
            try { all = all.Concat(People()); } catch { }
            foreach (var it in all)
            {
                if (it == null || string.IsNullOrEmpty(it.Key) || !vus.Add(it.Key)) continue;
                res.Add(new Mot { Cle = it.Key, Label = it.Label, Dit = it.Say, Img = it.Img, Emoji = it.Emoji, Photo = it.Photo });
            }
            return res;
        }

        public static Mot Trouver(string cle)
        {
            if (string.IsNullOrEmpty(cle)) return null;
            foreach (var m in Vocabulaire()) if (m.Cle == cle) return m;
            return null;
        }

        private static Item I(string key, string label, string say, string img, string emoji, bool alert = false) =>
            new Item { Key = key, Label = label, Say = say, Img = img, Emoji = emoji, Alert = alert };

        /// <summary>Un mot des pages suivantes : sa nature (v verbe, n nom, d mot qui décrit, s social, q question, p personne, o oui, x non, m douleur), sans image maison (picto ARASAAC, sinon l'emoji).</summary>
        private static Item P(char nat, string key, string label, string say, string emoji, string mot = null, bool alert = false) =>
            new Item { Key = key, Label = label, Say = say, Img = "", Emoji = emoji, Mot = mot, Nat = nat, Alert = alert };

        /// <summary>Un verbe seul (« Chercher. »).</summary>
        private static Item V(string cle, string label, string emoji) =>
            new Item { Key = "v-" + cle, Label = label, Say = label + ".", Img = "", Emoji = emoji, Mot = label.ToLowerInvariant(), Nat = 'v' };

        private static readonly Color Orange = Color.FromRgb(0xFF, 0xD8, 0xA8);
        private static readonly Color Blue = Color.FromRgb(0xC8, 0xE4, 0xFF);
        private static readonly Color Green = Color.FromRgb(0xCF, 0xF2, 0xC8);
        private static readonly Color Yellow = Color.FromRgb(0xFF, 0xF0, 0xA8);
        private static readonly Color Pink = Color.FromRgb(0xFF, 0xD3, 0xE6);
        private static readonly Color Red = Color.FromRgb(0xFF, 0xC4, 0xC4);
        private static readonly Color Violet = Color.FromRgb(0xE3, 0xD4, 0xFF);
        private static readonly Color Oui = Color.FromRgb(0x8F, 0xE0, 0x9B);
        private static readonly Color Non = Color.FromRgb(0xFF, 0xA3, 0xA3);

        // La nature de chaque mot : v = verbe, n = nom, d = mot qui décrit, s = social, q = question, o = oui, x = non.
        // (Les personnes « pers-… », les actions « v-… », les mots de phrase « w-… » et la douleur « mal… » se reconnaissent à leur clé.)
        private static readonly Dictionary<string, char> Nature = new Dictionary<string, char>
        {
            ["oui"] = 'o', ["non"] = 'x',
            ["aide"] = 'v', ["viens"] = 'v', ["manger"] = 'v', ["boire"] = 'v', ["dormir"] = 'v', ["jouer"] = 'v', ["bouger"] = 'v', ["pause"] = 'v', ["raconte"] = 'v',
            ["s-veux"] = 'v', ["s-aime"] = 'v', ["s-pasaime"] = 'v', ["s-donne"] = 'v', ["s-vois"] = 'v', ["s-fini"] = 'v', ["w-dodo"] = 'v',
            ["toilettes"] = 'n', ["calin"] = 'n', ["musique"] = 'n', ["histoire"] = 'n', ["dehors"] = 'n', ["bain"] = 'n', ["doudou"] = 'n', ["tele"] = 'n', ["ballon"] = 'n',
            ["ecole"] = 'n', ["cantine"] = 'n', ["copains"] = 'n', ["promenade"] = 'n', ["piscine"] = 'n', ["bain2"] = 'n', ["tele2"] = 'n', ["musique2"] = 'n', ["jeux"] = 'n', ["docteur"] = 'n',
            ["encore"] = 'd', ["fini"] = 'd', ["faim"] = 'd', ["soif"] = 'd', ["bien"] = 'd', ["rigolo"] = 'd', ["triste"] = 'd', ["colere"] = 'd', ["peur"] = 'd', ["pasbien"] = 'd',
            ["sommeil"] = 'd', ["chaud"] = 'd', ["froid"] = 'd', ["ennui"] = 'd', ["bruit"] = 'd', ["s-encore"] = 'd',
            ["jetaime"] = 's', ["bonjour"] = 's', ["aurevoir"] = 's', ["merci"] = 's', ["stp"] = 's', ["bisou"] = 's', ["bravo"] = 's',
            ["pardon"] = 's', ["coucou"] = 's', ["abientot"] = 's', ["bonnenuit"] = 's', ["bonappetit"] = 's',
            ["s-ou"] = 'q', ["cava"] = 'q',
        };

        /// <summary>La couleur du mot selon sa nature (null : la couleur de la page).</summary>
        private static Color? Couleur(Item it)
        {
            string k = it?.Key ?? "";
            char t = it?.Nat ?? (char)0;
            if (t == 0)
            {
                if (k.StartsWith("pers-")) return Yellow;
                if (k.StartsWith("v-")) return Green;
                if (k.StartsWith("mal")) return Red;
                if (!Nature.TryGetValue(k, out t)) return k.StartsWith("w-") ? Orange : (Color?)null;
            }
            switch (t)
            {
                case 'p': return Yellow;
                case 'm': return Red;
                case 'o': return Oui;
                case 'x': return Non;
                case 'v': return Green;
                case 'n': return Orange;
                case 'd': return Blue;
                case 's': return Pink;
                case 'q': return Violet;
                default: return null;
            }
        }

        private static readonly List<Item> Essentials = new List<Item>
        {
            I("oui", "Oui", "Oui !", "comm-oui", "✅"),
            I("non", "Non", "Non !", "comm-non", "❌"),
            I("encore", "Encore", "Encore ! J'en veux encore !", "comm-encore", "🔁"),
            I("fini", "Fini", "C'est fini ! J'arrête.", "comm-fini", "✋"),
            I("aide", "Aide-moi", "Aide-moi, s'il te plaît !", "comm-aide", "🙋"),
            I("viens", "Viens !", "Viens me voir, s'il te plaît !", "comm-viens", "🔔", alert: true),
            I("mal", "J'ai mal", "J'ai mal !", "comm-mal", "🤕", alert: true),
            I("faim", "J'ai faim", "J'ai faim !", "assiette", "🍽"),
            I("soif", "J'ai soif", "J'ai soif ! Je voudrais boire.", "verre", "🥛"),
            I("toilettes", "Toilettes", "J'ai besoin des toilettes, ou d'être changé.", "toilettes", "🚽"),
            I("calin", "Câlin", "Je veux un câlin !", "comm-calin", "🤗"),
            I("jetaime", "Je t'aime", "Je t'aime !", "coeur", "❤"),
        };

        // La suite de « L'essentiel » : les petits mots qui servent partout, puis les questions.
        private static readonly List<Item> EssentialsPlus = new List<Item>
        {
            P('v', "jenesaispas", "Je ne sais pas", "Je ne sais pas.", "🤷", "ne pas savoir"),
            P('v', "attends", "Attends", "Attends, s'il te plaît !", "✋", "attendre"),
            P('v', "regarde", "Regarde !", "Regarde !", "👀", "regarder"),
            P('v', "jaime", "J'aime", "J'aime ça !", "💖", "aimer"),
            P('v', "jaimepas", "Je n'aime pas", "Je n'aime pas ça.", "🙁", "ne pas aimer"),
            P('o', "daccord", "D'accord", "D'accord !", "👍", "d'accord"),
            P('x', "stop", "Stop", "Stop ! Arrête !", "🛑", "stop"),
            P('d', "unpeu", "Un peu", "Un peu.", "🤏", "un peu"),
            P('d', "beaucoup", "Beaucoup", "Beaucoup !", "🔢", "beaucoup"),
            P('d', "plustard", "Plus tard", "Plus tard.", "⏰", "plus tard"),
            P('d', "autrechose", "Autre chose", "Je veux autre chose.", "💭", "autre"),
            P('p', "amoi", "À moi", "C'est à moi !", "🙋", "moi"),

            P('p', "atoi", "À toi", "C'est à toi !", "👉", "toi"),
            P('q', "cestquoi", "C'est quoi ?", "C'est quoi ?", "❔", "quoi"),
            P('q', "cestqui", "C'est qui ?", "C'est qui ?", "👥", "qui"),
            P('q', "cestou", "C'est où ?", "C'est où ?", "🗺", "où"),
            P('q', "cestquand", "C'est quand ?", "C'est quand ?", "⏰", "quand"),
            P('q', "pourquoi", "Pourquoi ?", "Pourquoi ?", "❓", "pourquoi"),
            P('d', "ici", "Ici", "Ici !", "📍", "ici"),
            P('d', "labas", "Là-bas", "Là-bas !", "👉", "là-bas"),
            P('d', "pareil", "Pareil", "C'est pareil.", "👯", "pareil"),
            P('d', "paspareil", "Pas pareil", "Ce n'est pas pareil.", "↔", "différent"),
            P('d', "vite", "Vite", "Vite !", "🏃", "vite"),
            P('d', "doucement", "Doucement", "Doucement.", "🐢", "doucement"),
        };

        private static readonly List<Item> Feelings = new List<Item>
        {
            I("bien", "Ça va bien", "Ça va bien ! Je suis de bonne humeur !", "comm-content", "😄"),
            I("rigolo", "C'est rigolo", "C'est trop rigolo !", "comm-rigolo", "😂"),
            I("triste", "Triste", "Je suis triste.", "comm-triste", "😢"),
            I("colere", "En colère", "Je suis en colère !", "comm-colere", "😠"),
            I("peur", "J'ai peur", "J'ai peur !", "comm-peur", "😨", alert: true),
            I("pasbien", "Ça ne va pas", "Ça ne va pas, je ne me sens pas bien.", "comm-malade", "🤒", alert: true),
            I("sommeil", "Sommeil", "J'ai sommeil.", "comm-sommeil", "😴"),
            I("chaud", "J'ai chaud", "J'ai chaud !", "soleil", "🥵"),
            I("froid", "J'ai froid", "J'ai froid !", "bonnet", "🥶"),
            I("ennui", "Je m'ennuie", "Je m'ennuie... on fait autre chose ?", "comm-ennui", "😐"),
            I("bruit", "Trop de bruit", "Il y a trop de bruit !", "comm-bruit", "🔊"),
            I("pause", "Pause", "Je veux faire une pause.", "comm-pause", "⏸"),
        };

        private static readonly List<Item> FeelingsPlus = new List<Item>
        {
            P('d', "fatigue", "Fatigue", "Je suis fatiguée.", "😴", "fatigue"),
            P('d', "calme", "Calme", "Je suis calme.", "😌", "calme"),
            P('d', "malade", "Malade", "Je suis malade.", "🤒", "malade"),
            P('d', "timide", "Timide", "Je suis timide.", "😳", "timide"),
            P('d', "honte", "J'ai honte", "J'ai honte.", "😳", "honte"),
            P('d', "pleurer", "Envie de pleurer", "J'ai envie de pleurer.", "😭", "pleurer"),
            P('d', "enerve", "Ça m'énerve", "Ça m'énerve !", "😤", "énervé"),
            P('d', "inquiet", "Je m'inquiète", "Je m'inquiète.", "😟", "inquiet"),
            P('d', "reussi", "J'ai réussi", "J'ai réussi !", "😎", "fier"),
            P('d', "hate", "J'ai hâte", "J'ai hâte !", "🤩", "impatient"),
            P('d', "manques", "Tu me manques", "Tu me manques.", "💗", "manquer"),
            P('d', "tranquille", "Tranquille", "Je veux être tranquille.", "🤫", "tranquille"),
        };

        private static readonly List<Item> Wants = new List<Item>
        {
            I("manger", "Manger", "Je veux manger.", "assiette", "🍽"),
            I("boire", "Boire", "Je veux boire.", "verre", "🥛"),
            I("dormir", "Dormir", "Je veux dormir.", "comm-dormir", "🛏"),
            I("jouer", "Jouer", "Je veux jouer !", "cubes", "🧸"),
            I("musique", "Musique", "Je veux écouter de la musique !", "tambour", "🎵"),
            I("histoire", "Une histoire", "Je veux une histoire !", "livre", "📖"),
            I("dehors", "Dehors", "Je veux aller dehors !", "arbre", "🌳"),
            I("bain", "Le bain", "Je veux prendre un bain !", "baignoire", "🛁"),
            I("doudou", "Mon doudou", "Je veux mon doudou !", "ours-peluche", "🧸"),
            I("tele", "La télé", "Je veux regarder la télé.", "comm-tele", "📺"),
            I("bouger", "Changer de position", "Je veux changer de position.", "comm-position", "🔄"),
            I("ballon", "Le ballon", "Je veux jouer au ballon !", "ballon", "⚽"),
        };

        private static readonly List<Item> WantsPlus = new List<Item>
        {
            P('n', "veux-gouter", "Mon goûter", "Je veux mon goûter !", "🍪", "goûter"),
            P('n', "veux-eau", "De l'eau", "Je veux de l'eau.", "💧", "eau"),
            P('n', "veux-gateau", "Un gâteau", "Je veux un gâteau !", "🍰", "gâteau"),
            P('n', "veux-compote", "Une compote", "Je veux une compote.", "🍎", "compote"),
            P('n', "veux-livre", "Un livre", "Je veux un livre.", "📖", "livre"),
            P('v', "veux-dessiner", "Dessiner", "Je veux dessiner.", "✏", "dessiner"),
            P('v', "veux-chanter", "Chanter", "Je veux chanter !", "🎤", "chanter"),
            P('v', "veux-danser", "Danser", "Je veux danser !", "💃", "danser"),
            P('n', "veux-bulles", "Des bulles", "Je veux des bulles !", "🎈", "bulles de savon"),
            P('n', "veux-puzzle", "Un puzzle", "Je veux faire un puzzle.", "🧩", "puzzle"),
            P('n', "veux-tablette", "La tablette", "Je veux la tablette.", "📱", "tablette"),
            P('n', "veux-photos", "Des photos", "Je veux regarder des photos.", "🖼", "photo"),

            P('n', "veux-promenade", "Me promener", "Je veux me promener.", "🚶", "promenade"),
            P('n', "veux-parc", "Le parc", "Je veux aller au parc !", "🌳", "parc"),
            P('n', "veux-voiture", "La voiture", "Je veux aller en voiture.", "🚗", "voiture"),
            P('n', "veux-velo", "Le vélo", "Je veux faire du vélo.", "🚲", "vélo"),
            P('n', "veux-balancoire", "La balançoire", "Je veux faire de la balançoire !", "🎠", "balançoire"),
            P('n', "veux-piscine", "La piscine", "Je veux aller à la piscine !", "🏊", "piscine"),
            P('n', "veux-maison", "La maison", "Je veux rentrer à la maison.", "🏠", "maison"),
            P('n', "veux-lit", "Mon lit", "Je veux aller au lit.", "🛏", "lit"),
            P('v', "veux-bras", "Les bras", "Je veux aller dans les bras.", "🤗", "porter"),
            P('n', "veux-couverture", "Ma couverture", "Je veux ma couverture.", "🛌", "couverture"),
            P('n', "veux-lumiere", "La lumière", "Je veux de la lumière.", "💡", "lumière"),
            P('v', "veux-repos", "Me reposer", "Je veux me reposer.", "🛋", "se reposer"),
        };

        private static readonly List<Item> Body = new List<Item>
        {
            I("mal-tete", "La tête", "J'ai mal à la tête.", "comm-tete", "🤕", alert: true),
            I("mal-ventre", "Le ventre", "J'ai mal au ventre.", "comm-ventre", "🤢", alert: true),
            I("mal-dos", "Le dos", "J'ai mal au dos.", "comm-dos", "🔙", alert: true),
            I("mal-gorge", "La gorge", "J'ai mal à la gorge.", "comm-gorge", "😷", alert: true),
            I("mal-dents", "Les dents", "J'ai mal aux dents.", "comm-dents", "🦷", alert: true),
            I("mal-oreille", "L'oreille", "J'ai mal à l'oreille.", "comm-oreille", "👂", alert: true),
            I("mal-yeux", "Les yeux", "J'ai mal aux yeux.", "comm-yeux", "👁", alert: true),
            I("mal-bras", "Le bras", "J'ai mal au bras.", "comm-bras", "💪", alert: true),
            I("mal-main", "La main", "J'ai mal à la main.", "comm-main", "✋", alert: true),
            I("mal-jambe", "La jambe", "J'ai mal à la jambe.", "comm-jambe", "🦵", alert: true),
            I("mal-pied", "Le pied", "J'ai mal au pied.", "comm-pied", "🦶", alert: true),
            I("mal-partout", "Je ne sais pas", "J'ai mal, mais je ne sais pas où.", "comm-mal", "❓", alert: true),
        };

        private static readonly List<Item> BodyPlus = new List<Item>
        {
            P('m', "mal-genou", "Le genou", "J'ai mal au genou.", "🦵", "genou", alert: true),
            P('m', "mal-hanche", "La hanche", "J'ai mal à la hanche.", "🦵", "hanche", alert: true),
            P('m', "mal-cou", "Le cou", "J'ai mal au cou.", "🧣", "cou", alert: true),
            P('m', "mal-epaule", "L'épaule", "J'ai mal à l'épaule.", "💪", "épaule", alert: true),
            P('m', "mal-doigt", "Le doigt", "J'ai mal au doigt.", "☝", "doigt", alert: true),
            P('m', "mal-nez", "Le nez", "J'ai mal au nez.", "👃", "nez", alert: true),
            P('m', "mal-bouche", "La bouche", "J'ai mal à la bouche.", "👄", "bouche", alert: true),
            P('m', "mal-fesses", "Les fesses", "J'ai mal aux fesses.", "🍑", "fesses", alert: true),
            P('m', "mal-poitrine", "La poitrine", "J'ai mal à la poitrine.", "🧍", "poitrine", alert: true),
            P('m', "mal-gratte", "Ça gratte", "Ça me gratte.", "🖐", "gratter"),
            P('m', "mal-vomir", "Envie de vomir", "J'ai envie de vomir.", "🤮", "vomir", alert: true),
            P('d', "mieux", "Ça va mieux", "Ça va mieux.", "🙂", "mieux"),
        };

        private static readonly List<Item> Social = new List<Item>
        {
            I("bonjour", "Bonjour", "Bonjour !", "comm-bonjour", "👋"),
            I("aurevoir", "Au revoir", "Au revoir !", "comm-aurevoir", "👋"),
            I("merci", "Merci", "Merci !", "comm-merci", "🙏"),
            I("stp", "S'il te plaît", "S'il te plaît !", "comm-stp", "🙏"),
            I("bisou", "Bisou", "Un bisou !", "comm-bisou", "😘"),
            I("bravo", "Bravo", "Bravo !", "comm-bravo", "👏"),
            I("coucou", "Coucou", "Coucou !", "comm-bonjour", "🤗"),
            I("cava", "Ça va ?", "Ça va ?", "comm-content", "🙂"),
            I("pardon", "Pardon", "Pardon !", "comm-stp", "🙇"),
            I("abientot", "À bientôt", "À bientôt !", "comm-aurevoir", "👋"),
            I("bonappetit", "Bon appétit", "Bon appétit !", "assiette", "🍽"),
            I("bonnenuit", "Bonne nuit", "Bonne nuit !", "lune", "🌙"),
        };

        private static readonly List<Item> SocialPlus = new List<Item>
        {
            P('s', "salut", "Salut", "Salut !", "👋", "salut"),
            P('s', "bonnejournee", "Bonne journée", "Bonne journée !", "☀", "bonne journée"),
            P('s', "ademain", "À demain", "À demain !", "👋", "à demain"),
            P('s', "atoutalheure", "À tout à l'heure", "À tout à l'heure !", "⏰", "à tout à l'heure"),
            P('s', "derien", "De rien", "De rien !", "🙂", "de rien"),
            P('s', "excuse", "Excuse-moi", "Excuse-moi.", "🙇", "excuser"),
            P('s', "bienjoue", "Bien joué", "Bien joué !", "👍", "bien"),
            P('s', "bonanniversaire", "Bon anniversaire", "Bon anniversaire !", "🎂", "anniversaire"),
            P('s', "joyeuxnoel", "Joyeux Noël", "Joyeux Noël !", "🎄", "Noël"),
            P('s', "bonneannee", "Bonne année", "Bonne année !", "🎉", "bonne année"),
            P('s', "bonnesvacances", "Bonnes vacances", "Bonnes vacances !", "🏖", "vacances"),
            P('q', "ettoi", "Et toi ?", "Et toi ?", "👉", "toi"),
        };

        // Les ACTIONS : des verbes seuls, pour répondre quand on lui demande ce qu'elle veut faire
        // (« Tu veux regarder ? écouter ? »), et pour que l'adulte les montre en parlant.
        private static readonly List<Item> Verbs = new List<Item>
        {
            I("v-regarder", "Regarder", "Regarder.", "comm-vois", "👀"),
            I("v-ecouter", "Écouter", "Écouter.", "comm-oreille", "👂"),
            I("v-manger", "Manger", "Manger.", "assiette", "🍽"),
            I("v-boire", "Boire", "Boire.", "verre", "🥛"),
            I("v-jouer", "Jouer", "Jouer.", "cubes", "🧸"),
            I("v-dormir", "Dormir", "Dormir.", "comm-dormir", "🛏"),
            I("v-venir", "Venir", "Venir.", "comm-viens", "👉"),
            I("v-aller", "Aller", "Aller.", "velo", "🚶"),
            I("v-donner", "Donner", "Donner.", "comm-donne", "🤲"),
            I("v-prendre", "Prendre", "Prendre.", "comm-main", "✊"),
            I("v-ouvrir", "Ouvrir", "Ouvrir.", "cadeau", "📂"),
            I("v-arreter", "Arrêter", "Arrêter.", "comm-fini", "✋"),
        };

        // La suite des actions : les verbes de tous les jours, puis les activités, puis bouger.
        private static readonly List<Item> VerbsPlus = new List<Item>
        {
            V("aider", "Aider", "🙋"), V("vouloir", "Vouloir", "🙌"), V("aimer", "Aimer", "💖"), V("faire", "Faire", "🛠"),
            V("mettre", "Mettre", "📥"), V("enlever", "Enlever", "📤"), V("fermer", "Fermer", "🚪"), V("chercher", "Chercher", "🔍"),
            V("attendre", "Attendre", "⏳"), V("finir", "Finir", "🏁"), V("montrer", "Montrer", "👉"), V("parler", "Parler", "🗣"),

            V("chanter", "Chanter", "🎤"), V("danser", "Danser", "💃"), V("lire", "Lire", "📖"), V("dessiner", "Dessiner", "✏"),
            V("peindre", "Peindre", "🎨"), V("couper", "Couper", "✂"), V("coller", "Coller", "📎"), V("laver", "Laver", "🧼"),
            V("habiller", "S'habiller", "👕"), V("promener", "Se promener", "🚶"), V("caliner", "Câliner", "🤗"), V("rire", "Rire", "😂"),

            V("sortir", "Sortir", "🚪"), V("monter", "Monter", "⬆"), V("descendre", "Descendre", "⬇"), V("courir", "Courir", "🏃"),
            V("sauter", "Sauter", "🤸"), V("tomber", "Tomber", "💥"), V("pousser", "Pousser", "🤜"), V("tirer", "Tirer", "🤛"),
            V("lancer", "Lancer", "🤾"), V("attraper", "Attraper", "🙌"), V("asseoir", "S'asseoir", "💺"), V("lever", "Se lever", "🧍"),
        };

        // Les gens : les PHOTOS de la famille d'abord (Contenu\Famille), puis
        // les personnes habituelles qui manquent ; et les mots gentils.
        private static List<Item> People()
        {
            var list = new List<Item>();
            try
            {
                foreach (var (name, path) in UserContent.LoadFamily().Take(6))
                {
                    var p = path;
                    var nice = name.Length > 0 ? char.ToUpperInvariant(name[0]) + name.Substring(1) : name;
                    list.Add(new Item
                    {
                        Key = "pers-" + name.ToLowerInvariant(),
                        Label = nice,
                        Say = nice + " !",
                        Photo = () => new Border
                        {
                            CornerRadius = new CornerRadius(20),
                            ClipToBounds = true,
                            Width = 170,
                            Height = 170,
                            Child = new Image { Source = UserContent.LoadBitmap(p, 400), Stretch = Stretch.UniformToFill },
                        },
                    });
                }
            }
            catch { }
            var defaults = new[]
            {
                I("pers-maman", "Maman", "Maman !", "comm-maman", "👩"),
                I("pers-papa", "Papa", "Papa !", "comm-papa", "👨"),
                I("pers-mamie", "Mamie", "Mamie !", "comm-mamie", "👵"),
                I("pers-papi", "Papi", "Papi !", "comm-papi", "👴"),
                I("pers-copain", "Mon copain", "Mon copain !", "comm-copain", "🧒"),
                I("pers-docteur", "Le docteur", "Le docteur.", "comm-docteur", "🩺"),
            };
            foreach (var d in defaults)
            {
                if (list.Count >= 6) break;
                if (list.Any(x => string.Equals(x.Label, d.Label, StringComparison.OrdinalIgnoreCase))) continue;
                list.Add(d);
            }
            return list.Take(6).Concat(Social.Take(6)).ToList();
        }

        private static readonly Category[] Cats =
        {
            new Category { Icon = "⭐", Name = "L'essentiel", Tint = Orange, Items = () => Essentials.Concat(EssentialsPlus).ToList() },
            new Category { Icon = "😊", Name = "Je ressens", Tint = Blue, Items = () => Feelings.Concat(FeelingsPlus).ToList() },
            new Category { Icon = "🙌", Name = "Je veux", Tint = Green, Items = () => Wants.Concat(WantsPlus).ToList() },
            new Category { Icon = "🏃", Name = "Les actions", Tint = Green, Items = () => Verbs.Concat(VerbsPlus).ToList() },
            new Category { Icon = "🤕", Name = "J'ai mal où ?", Tint = Red, Items = () => Body.Concat(BodyPlus).ToList() },
            new Category { Icon = "👪", Name = "Les gens", Tint = Yellow, Items = People },
            new Category { Icon = "👋", Name = "Bonjour, merci", Tint = Pink, Items = () => Social.Concat(SocialPlus).ToList() },
            new Category { Key = "themes", Icon = "📚", Name = "Les thèmes", Tint = Color.FromRgb(0xEC, 0xEC, 0xF6), Items = () => new List<Item>() },
            new Category { Key = "jour", Icon = "📅", Name = "Ma journée", Tint = Pink, Items = () => DayItems },
            new Category { Key = "phrase", Icon = "🧩", Name = "Je fais une phrase", Tint = Color.FromRgb(0xE0, 0xE8, 0xFF), Items = () => Starters, MinLevel = 2 },
        };

        // --- 📅 MA JOURNÉE : raconter ce qu'on a fait (les pictos s'ajoutent, puis on raconte). ---
        private static readonly List<Item> DayItems = new List<Item>
        {
            I("raconte", "Raconter", "Aujourd'hui...", "livre", "📣"),
            I("ecole", "L'école", "l'école", "bus", "🏫"), I("cantine", "La cantine", "la cantine", "assiette", "🍽"),
            I("copains", "Les copains", "les copains", "ballon-foot", "🧒"), I("promenade", "La promenade", "la promenade", "velo", "🚶"),
            I("piscine", "La piscine", "la piscine", "maillot", "🏊"), I("bain2", "Le bain", "le bain", "baignoire", "🛁"),
            I("tele2", "La télé", "la télé", "radio", "📺"), I("musique2", "La musique", "la musique", "note-musique", "🎵"),
            I("jeux", "Les jeux", "les jeux", "cubes", "🧸"), I("calin", "Un câlin", "un câlin", "coeur", "🤗"),
            I("docteur", "Le docteur", "le docteur", "ambulance", "🩺"),
        };
        private static readonly List<Item> _day = new List<Item>();
        private static DateTime _dayDate = DateTime.MinValue;

        // --- 🧩 JE FAIS UNE PHRASE : un début, puis un mot → la phrase entière. ---
        private static readonly List<Item> Starters = new List<Item>
        {
            I("s-veux", "Je veux", "Je veux", "comm-veux", "🙌"), I("s-aime", "J'aime", "J'aime", "coeur", "💖"),
            I("s-pasaime", "Je n'aime pas", "Je n'aime pas", "comm-non", "🙅"), I("s-encore", "Encore", "Encore", "comm-encore", "🔁"),
            I("s-donne", "Donne-moi", "Donne-moi", "comm-donne", "🤲"), I("s-vois", "Je vois", "Je vois", "comm-vois", "👀"),
            I("s-fini", "J'ai fini", "J'ai fini", "comm-fini", "✅"), I("s-ou", "Où est", "Où est", "comm-ou", "❓"),
        };
        private static readonly List<Item> SentenceWords = new List<Item>
        {
            I("w-jus", "Le jus", "le jus", "verre", "🧃"), I("w-gateau", "Le gâteau", "le gâteau", "gateau", "🎂"),
            I("w-pomme", "La pomme", "la pomme", "pomme", "🍎"), I("w-ballon", "Le ballon", "le ballon", "ballon", "🎈"),
            I("w-livre", "Le livre", "le livre", "livre", "📖"), I("w-doudou", "Mon doudou", "mon doudou", "ours-peluche", "🧸"),
            I("w-musique", "La musique", "la musique", "note-musique", "🎵"), I("w-dehors", "Dehors", "dehors", "arbre", "🌳"),
            I("w-dodo", "Dodo", "dormir", "lune", "🌙"), I("w-bain", "Le bain", "le bain", "baignoire", "🛁"),
            I("w-calin", "Un câlin", "un câlin", "coeur", "🤗"), I("w-chat", "Le chat", "le chat", "chat", "🐱"),
        };
        private Item _starter; // le début de phrase choisi (null = on choisit le début)

        private readonly UniformGrid _grid = new UniformGrid { Columns = 4, Rows = 3, Margin = new Thickness(2, 0, 0, 0) };
        private readonly List<Button> _tabs = new List<Button>();
        private readonly UniformGrid _tabsPanel = new UniformGrid { Columns = 1, Margin = new Thickness(0, 0, 8, 0) };
        private int _tabCount;
        private ColumnDefinition _colTabs;

        private void BuildTabs()
        {
            var visible = Cats.Where(c => c.MinLevel <= Level.Current).ToList();
            if (visible.Count == _tabCount && _tabs.Count > 0) return;
            _tabCount = visible.Count;
            _tabsPanel.Children.Clear();
            _tabs.Clear();
            // Plus de sept pages : deux colonnes, pour que chaque onglet reste une grande cible.
            bool deux = visible.Count > 7;
            _tabsPanel.Columns = deux ? 2 : 1;
            _tabsPanel.Rows = deux ? (visible.Count + 1) / 2 : visible.Count;
            if (_colTabs != null) _colTabs.Width = new GridLength(deux ? 310 : 240);
            for (int i = 0; i < visible.Count; i++)
            {
                int idx = Array.IndexOf(Cats, visible[i]);
                var t = deux ? TabButton(Cats[idx].Icon, Cats[idx].Name, Cats[idx].Tint) : BigButton(Cats[idx].Icon, Cats[idx].Name, Cats[idx].Tint);
                t.Width = double.NaN;
                t.Height = double.NaN;
                t.Click += (s, e) => { ShowCategory(idx); Speech.SayAlways(Cats[idx].Name + " !"); };
                _tabsPanel.Children.Add(t);
                _tabs.Add(t);
            }
        }
        private readonly ContentControl _msgPic = new ContentControl();
        private readonly TextBlock _msgText = new TextBlock
        {
            FontSize = 40,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(18, 0, 0, 0),
            Text = "Je veux dire...",
        };
        private Item _last;

        public CommBoard()
        {
            Background = new LinearGradientBrush(Color.FromRgb(0xF6, 0xF0, 0xFF), Color.FromRgb(0xFF, 0xF6, 0xEC), 90);

            // Trois zones, pas plus : en haut la bande du message ; à gauche les pages ; les douze cases,
            // avec la flèche « Suivant » sur leur bord droit quand la page a une suite.
            var root = new Grid { Margin = new Thickness(10) };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(104) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _colTabs = new ColumnDefinition { Width = new GridLength(250) };
            root.ColumnDefinitions.Add(_colTabs);
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // --- En haut : ce qui vient d'être dit, sur toute la largeur. Un appui (doigt ou souris) le redit ;
            //     ce n'est pas une cible du regard, pour que l'enfant puisse le relire sans le relancer.
            var top = new Grid();
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var msg = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(28),
                Padding = new Thickness(18, 6, 18, 6),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Appuyer pour redire",
                Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 2, Opacity = 0.18 },
                // L'image du mot, ramenée à 80 quelle que soit sa taille (une photo de famille en fait 170).
                Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new Viewbox { Width = 80, Height = 80, Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center, Child = _msgPic }, _msgText } },
            };
            msg.MouseLeftButtonUp += (s, e) => { if (_last != null) SayItem(_last, record: false); };
            top.Children.Add(msg);

            // « Retour au jeu » : seulement quand le tableau est ouvert par-dessus une activité (voir AvecRetour).
            _back = BigButton("🎮", "Retour au jeu", Color.FromRgb(0xB8, 0xF0, 0xC0));
            _back.Margin = new Thickness(12, 0, 0, 8);
            _back.Click += (s, e) => CloseRequested?.Invoke();
            Grid.SetColumn(_back, 1);
            top.Children.Add(_back);
            Grid.SetColumnSpan(top, 3);
            root.Children.Add(top);

            // --- À droite des cases : ➡ Suivant, comme le bord d'une page qu'on tourne. Toujours au même
            //     endroit, hors des cases (aucun mot ne perd sa place) ; après la dernière page, retour à la première.
            _next = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"],
                Width = 104, Margin = new Thickness(12, 6, 0, 6),
                Background = new SolidColorBrush(Color.FromRgb(0xF6, 0xF2, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x7E, 0x3F, 0xF2)),
                BorderThickness = new Thickness(3),
                Visibility = Visibility.Collapsed,
                Content = new Viewbox
                {
                    Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly,
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = "➜", FontSize = 58, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(0x7E, 0x3F, 0xF2)) },
                            new TextBlock { Text = "Suivant", FontSize = 22, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)), Margin = new Thickness(0, 4, 0, 4) },
                            _nextInfo,
                        },
                    },
                },
            };
            _next.Click += (s, e) => _suivant?.Invoke();
            Grid.SetRow(_next, 1);
            Grid.SetColumn(_next, 2);
            root.Children.Add(_next);

            // --- À gauche : les pages (celles du niveau de l'enfant) ---
            Grid.SetRow(_tabsPanel, 1);
            root.Children.Add(_tabsPanel);
            _tabCount = 0;
            BuildTabs();

            // --- Au centre : les 12 cases ---
            Grid.SetRow(_grid, 1);
            Grid.SetColumn(_grid, 1);
            root.Children.Add(_grid);

            Content = root;
            ShowCategory(0);
        }

        private Button _back;

        /// <summary>Le bouton « Retour au jeu » : utile quand le tableau s'ouvre par-dessus une activité,
        /// inutile dans le monde Parler (la barre du haut est là).</summary>
        public bool AvecRetour
        {
            get => _back != null && _back.Visibility == Visibility.Visible;
            set { if (_back != null) _back.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }
        private Button _next;
        private readonly TextBlock _nextInfo = new TextBlock { FontSize = 22, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x65, 0x87)), HorizontalAlignment = HorizontalAlignment.Center };
        private Action _suivant;
        private const int ParPage = 12;

        /// <summary>Règle le bouton « Suivant » pour la grille affichée : page « page » sur « n ».</summary>
        private void Pages(int page, int n, Action<int> aller)
        {
            if (_next == null) return;
            bool suite = n > 1 && aller != null;
            _next.Visibility = suite ? Visibility.Visible : Visibility.Collapsed;
            _nextInfo.Text = suite ? (page + 1) + " / " + n : "";
            _suivant = !suite ? (Action)null : () =>
            {
                bool fin = page + 1 >= n;
                Speech.SayAlways(fin ? "Le début !" : "La suite !");
                aller(fin ? 0 : page + 1);
            };
        }

        /// <summary>Remplit les douze cases avec la page demandée de « items » et règle « Suivant ».</summary>
        private void Remplir(IList<Item> items, Color tint, int page, Action<int> aller, Action<Item, Button> clic)
        {
            _grid.Children.Clear();
            int n = Math.Max(1, (items.Count + ParPage - 1) / ParPage);
            page = Math.Max(0, Math.Min(n - 1, page));
            foreach (var it in items.Skip(page * ParPage).Take(ParPage))
            {
                var item = it;
                var cell = Cell(item, Couleur(item) ?? tint);
                cell.Click += (s, e) => clic(item, cell);
                _grid.Children.Add(cell);
            }
            Pages(page, n, aller);
        }

        // ------------------------------------------------------------------
        //  📚 Les thèmes : les mêmes seize grilles que « Autre chose » dans l'écoute
        // ------------------------------------------------------------------
        private static char NatureTheme(string cle)
        {
            switch (cle)
            {
                case "gens": return 'p';
                case "ressens": case "quand": case "couleurs": return 'd';
                case "faire": return 'v';
                case "corps": return 'm';
                case "demande": return 'q';
                default: return 'n';
            }
        }

        private static readonly string[] Articles = { "Un ", "Une ", "Des ", "Du ", "De la ", "De l'", "Le ", "La ", "Les ", "L'", "Mon ", "Ma ", "Mes " };

        /// <summary>La nature d'un mot de thème, pour sa couleur : celle du thème, sauf ce que le mot dit de lui-même.</summary>
        private static char NatureMot(Themes.Theme t, Suggest.Reponse r)
        {
            string x = (r.Texte ?? "").Trim();
            char defaut = NatureTheme(t.Cle);
            if (x.EndsWith("?")) return 'q';
            if (x == "Je t'aime") return 's';
            if (t.Cle == "corps") return x.StartsWith("Je veux") ? 'v' : x == "Ça va mieux" ? 'd' : 'm';
            if (t.Cle == "gens") return r.Mot == "chien" || r.Mot == "chat" ? 'n' : 'p';
            if (x.StartsWith("Je veux") || x.StartsWith("Je ne veux") || x.StartsWith("Je n'aime") || x.StartsWith("Je t'") || x.EndsWith("-moi")) return 'v';
            if (x.StartsWith("J'ai ") || x.StartsWith("Je suis") || x.StartsWith("Je n'ai plus") || x.StartsWith("C'est") || x.StartsWith("Ça ") || x.StartsWith("Je m'ennuie") || x.StartsWith("Je ne sais")) return 'd';
            if (x == "Encore" || x == "Chaud" || x == "Froid" || x == "Rien") return 'd';
            if (x.IndexOf(' ') < 0 && (x.EndsWith("er") || x == "Dormir")) return 'v';
            if (defaut != 'n' && Articles.Any(a => x.StartsWith(a))) return 'n';
            return defaut;
        }

        private static Item CaseTheme(Themes.Theme t) =>
            new Item { Key = "theme-" + t.Cle, Label = t.Nom, Say = t.Nom + " !", Img = "", Emoji = t.Emoji, Mot = t.Mot, Nat = NatureTheme(t.Cle) };

        private static List<Item> MotsDuTheme(Themes.Theme t)
        {
            var res = new List<Item>();
            List<Item> gens = null;
            foreach (var r in Themes.MotsDe(t))
            {
                if (r == null || string.IsNullOrWhiteSpace(r.Texte) || r.Texte == "Autre chose") continue;
                if (!string.IsNullOrEmpty(r.Cle))
                {
                    // Un proche du tableau : sa case à lui, avec sa photo.
                    if (gens == null) { try { gens = People(); } catch { gens = new List<Item>(); } }
                    var proche = gens.FirstOrDefault(g => g.Key == r.Cle);
                    if (proche != null) { res.Add(proche); continue; }
                }
                string x = r.Texte.Trim();
                bool ponctue = x.EndsWith("?") || x.EndsWith("!") || x.EndsWith(".");
                res.Add(new Item
                {
                    Key = "t-" + Pictos.Norm(x).Replace(' ', '-'), Label = x, Say = ponctue ? x : x + ".", Img = "", Emoji = r.Emoji,
                    Mot = string.IsNullOrWhiteSpace(r.Mot) ? null : r.Mot, Nat = NatureMot(t, r),
                });
            }
            return res;
        }

        /// <summary>Les seize thèmes ; on en regarde un pour ouvrir ses mots. L'onglet « Les thèmes » y ramène.</summary>
        private void ShowThemes(Category cat, int page)
        {
            _msgText.Text = "De quoi je veux parler ?";
            _msgPic.Content = null;
            Remplir(Themes.Liste.Select(CaseTheme).ToList(), cat.Tint, page, p => ShowThemes(cat, p), (item, cell) =>
            {
                var t = Themes.Trouver(item.Key.Substring(6));
                if (t == null) return;
                Speech.SayAlways(t.Nom + " !");
                ShowTheme(cat, t, 0);
            });
        }

        private void ShowTheme(Category cat, Themes.Theme t, int page)
        {
            _msgText.Text = t.Nom + "...";
            _msgPic.Content = Pic(CaseTheme(t), 80);
            Remplir(MotsDuTheme(t), cat.Tint, page, p => ShowTheme(cat, t, p), (item, cell) => SayItem(item, record: true, btn: cell));
        }

        /// <summary>Le bouton « Parler », toujours accessible (posé par la fenêtre).</summary>
        public static Button MakeOpenButton()
        {
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"],
                Width = 128,
                Height = 128,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 8, 8), // tout au coin, en bas à droite
                Background = new LinearGradientBrush(Color.FromRgb(0xFF, 0x8F, 0xC8), Color.FromRgb(0xA0, 0x6C, 0xD5), 90),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(5),
                ToolTip = "Parler (grille de communication)",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "💬", FontSize = 50, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center },
                        new TextBlock { Text = "Parler", FontSize = 22, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center },
                    },
                },
            };
            return b;
        }

        /// <summary>✅ ❌ OUI / NON toujours à portée de regard (coin bas gauche) :
        /// pour répondre à une question qu'on lui pose, sans ouvrir la grille.</summary>
        public static FrameworkElement MakeYesNoBar()
        {
            var bar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(8, 0, 0, 8),
            };
            bar.Children.Add(YesNoButton("✓", "Oui", "Oui !", "oui", Color.FromRgb(0x4C, 0xC2, 0x6B), Color.FromRgb(0x2E, 0x9E, 0x4F)));
            bar.Children.Add(YesNoButton("✗", "Non", "Non !", "non", Color.FromRgb(0xFF, 0x7A, 0x7A), Color.FromRgb(0xE0, 0x4A, 0x4A)));
            return bar;
        }

        private static Button YesNoButton(string glyph, string label, string say, string key, Color c1, Color c2)
        {
            var b = new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"],
                Width = 118,
                Height = 118,
                Margin = new Thickness(0, 0, 8, 0),
                Background = new LinearGradientBrush(c1, c2, 90),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(5),
                ToolTip = label,
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = glyph, FontSize = 48, FontWeight = FontWeights.ExtraBold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, -4, 0, -6) },
                        new TextBlock { Text = label, FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center },
                    },
                },
            };
            b.Click += (o, ev) =>
            {
                if (!VoiceBank.Play("comm-" + key)) Speech.SayAlways(say);
                GazeStats.RecordComm(key);
                var sc = new ScaleTransform(1, 1);
                b.RenderTransformOrigin = new Point(0.5, 0.5);
                b.RenderTransform = sc;
                var a = new DoubleAnimation(1, 1.18, TimeSpan.FromMilliseconds(150)) { AutoReverse = true };
                sc.BeginAnimation(ScaleTransform.ScaleXProperty, a);
                sc.BeginAnimation(ScaleTransform.ScaleYProperty, a);
            };
            return b;
        }

        /// <summary>Ouverture : la grille s'annonce (« J'ai quelque chose à dire ! »).</summary>
        public void Open()
        {
            IsOpen = true;
            BuildTabs();
            _starter = null;
            ShowCategory(0);
            Speech.SayAlways("J'ai quelque chose à dire !");
        }

        public void MarkClosed() => IsOpen = false;

        /// <summary>Le visuel d'une tuile : la photo du proche, sinon le picto ARASAAC du mot (comme dans les classeurs de CAA), sinon l'image maison.</summary>
        private static FrameworkElement Pic(Item it, double size)
        {
            if (it.Photo != null) return it.Photo();
            var maison = Art.Visual(it.Img, it.Emoji, size);
            if (it.Key == "autre" || it.Key == "phrase" || (it.Key ?? "").StartsWith("jour-") || it.Key == "raconte") return maison;
            try { return Arasaac.Picture(string.IsNullOrEmpty(it.Mot) ? it.Label : it.Mot, size, maison) ?? (it.Mot != null ? Arasaac.Picture(it.Label, size, maison) : null) ?? maison; } catch { return maison; }
        }

        private Category _courante; // la page affichée

        private void ShowCategory(int i, int page = 0)
        {
            var visible = Cats.Where(c => c.MinLevel <= Level.Current).ToList();
            int vi = visible.IndexOf(Cats[i]);
            for (int k = 0; k < _tabs.Count; k++)
            {
                _tabs[k].BorderBrush = k == vi ? new SolidColorBrush(Color.FromRgb(0x7E, 0x3F, 0xF2)) : Brushes.Transparent;
                _tabs[k].BorderThickness = new Thickness(k == vi ? 7 : 0);
            }
            _grid.Children.Clear();
            var cat = Cats[i];
            _courante = cat;
            if (cat.Key == "phrase") { ShowSentence(cat); return; }
            if (cat.Key == "jour") { Pages(0, 1, null); ShowDay(cat); return; }
            if (cat.Key == "themes") { ShowThemes(cat, 0); return; }
            Remplir(cat.Items(), cat.Tint, page, p => ShowCategory(i, p), (item, cell) => SayItem(item, record: true, btn: cell));
        }

        // 🧩 La phrase : d'abord le début (« Je veux »), puis le mot (« le gâteau »).
        private void ShowSentence(Category cat, int page = 0)
        {
            if (_starter == null)
            {
                _grid.Children.Clear();
                Pages(0, 1, null);
                _msgText.Text = "Je fais une phrase...";
                foreach (var it in Starters.Take(12))
                {
                    var item = it;
                    var cell = Cell(item, Couleur(item) ?? cat.Tint);
                    cell.Click += (s, e) =>
                    {
                        _starter = item;
                        _msgText.Text = item.Say + "...";
                        _msgPic.Content = Pic(item, 80);
                        Speech.SayAlways(item.Say + "...");
                        ShowSentence(cat);
                    };
                    _grid.Children.Add(cell);
                }
                return;
            }
            var words = SentenceWords.ToList();
            // Les gens de la famille font aussi des mots (« Je veux papa »).
            try
            {
                foreach (var p in People().Where(x => x.Key.StartsWith("pers-")).Take(3))
                    words.Insert(0, p);
            }
            catch { }
            Remplir(words, Orange, page, p => ShowSentence(cat, p), (item, cell) =>
            {
                if (_starter == null) return; // la phrase vient d'être dite : on attend le retour aux débuts
                string obj = item.Key.StartsWith("pers-") ? item.Label : item.Say;
                var sentence = new Item { Key = "phrase", Label = _starter.Say + " " + obj, Say = _starter.Say + " " + obj + " !", Img = item.Img, Emoji = item.Emoji, Photo = item.Photo };
                SayItem(sentence, record: true, btn: cell);
                GazeStats.RecordComm("phrase-" + _starter.Key + "-" + item.Key);
                _starter = null;
                var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2500) };
                t.Tick += (s2, e2) => { t.Stop(); if (IsOpen && _starter == null && ReferenceEquals(_courante, cat)) ShowSentence(cat); };
                t.Start();
            });
        }

        // 📅 Ma journée : chaque picto s'ajoute à l'histoire du jour ; « Raconter » la dit.
        private void ShowDay(Category cat)
        {
            if (_dayDate != DateTime.Today) { _day.Clear(); _dayDate = DateTime.Today; }
            foreach (var it in DayItems.Take(12))
            {
                var item = it;
                bool done = _day.Contains(item);
                var cell = Cell(item, Couleur(item) ?? cat.Tint);
                if (done)
                {
                    // Déjà raconté : la case garde sa couleur (c'est toujours un nom) et s'entoure de vert.
                    cell.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x9E, 0x4F));
                    cell.BorderThickness = new Thickness(9);
                    cell.Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 0, Color = Color.FromRgb(0x4C, 0xC2, 0x6B), Opacity = 0.9 };
                }
                cell.Click += (s, e) =>
                {
                    if (item.Key == "raconte") { TellDay(cell); return; }
                    if (!_day.Contains(item)) { _day.Add(item); GazeStats.RecordComm("jour-" + item.Key); }
                    var said = new Item { Key = "jour-" + item.Key, Label = item.Label, Say = "J'ai fait " + item.Say + " !", Img = item.Img, Emoji = item.Emoji };
                    SayItem(said, record: false, btn: cell);
                    ShowDay(cat);
                };
                _grid.Children.Add(cell);
            }
        }

        private void TellDay(Button btn)
        {
            string story = _day.Count == 0 ? "Aujourd'hui... je n'ai encore rien raconté !"
                : "Aujourd'hui, j'ai fait " + (_day.Count == 1 ? _day[0].Say : string.Join(", ", _day.Take(_day.Count - 1).Select(d => d.Say)) + " et " + _day.Last().Say) + " !";
            SayItem(new Item { Key = "raconte", Label = "Ma journée", Say = story, Img = "livre", Emoji = "📣" }, record: true, btn: btn);
        }

        private static Color Fonce(Color c) => Color.FromRgb((byte)(c.R * 0.72), (byte)(c.G * 0.72), (byte)(c.B * 0.72));

        private static Button Cell(Item it, Color tint)
        {
            FrameworkElement pic = Pic(it, 170);
            var content = new StackPanel
            {
                Children =
                {
                    new ContentControl { Content = pic, HorizontalAlignment = HorizontalAlignment.Center },
                    new TextBlock
                    {
                        Text = it.Label,
                        FontSize = 34,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,   // « Je veux aller aux toilettes » : deux lignes plutôt qu'une image rétrécie
                        MaxWidth = 340,
                        Margin = new Thickness(0, 6, 0, 0),
                    },
                },
            };
            return new Button
            {
                Style = (Style)Application.Current.Resources["AnswerButton"],
                Margin = new Thickness(6),
                Background = new SolidColorBrush(tint),
                BorderBrush = new SolidColorBrush(Fonce(tint)),     // le bord reprend la couleur, en plus soutenu : le code se lit de loin
                BorderThickness = new Thickness(5),
                RenderTransformOrigin = new Point(0.5, 0.5),
                Content = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, Child = content },
            };
        }

        /// <summary>Un onglet de page pour l'affichage sur deux colonnes : l'icône au-dessus du nom.</summary>
        private static Button TabButton(string icon, string label, Color bg) => new Button
        {
            Style = (Style)Application.Current.Resources["AnswerButton"],
            Margin = new Thickness(5, 0, 5, 10),
            Background = new SolidColorBrush(bg),
            Content = new Viewbox
            {
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly,
                Margin = new Thickness(4),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = icon, FontSize = 44, HorizontalAlignment = HorizontalAlignment.Center },
                        new TextBlock
                        {
                            Text = label,
                            FontSize = 24,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 150,
                        },
                    },
                },
            },
        };

        private static Button BigButton(string icon, string label, Color bg) => new Button
        {
            Style = (Style)Application.Current.Resources["AnswerButton"],
            Margin = new Thickness(6, 0, 6, 10),
            MinWidth = 190,
            Background = new SolidColorBrush(bg),
            Content = new Viewbox
            {
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly,
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new TextBlock { Text = icon, FontSize = 44, VerticalAlignment = VerticalAlignment.Center },
                        new TextBlock
                        {
                            Text = label,
                            FontSize = 30,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A)),
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(10, 0, 0, 0),
                        },
                    },
                },
            },
        };

        private void SayItem(Item it, bool record, Button btn = null)
        {
            _last = it;
            _msgText.Text = it.Say;
            _msgPic.Content = Pic(it, 80);
            if (record) GazeStats.RecordComm(it.Key);

            // Appel, douleur, peur : une petite sonnerie d'abord, pour que
            // l'adulte tourne la tête.
            if (it.Alert)
            {
                SoundFx.Note(4);
                var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
                t.Tick += (s, e) => { t.Stop(); SoundFx.Note(2); };
                t.Start();
            }
            // La voix de papa/maman pour cette case (comm-<clé>-1.wav), sinon la synthèse.
            var delay = it.Alert ? 800 : 0;
            Action speak = () => { if (!VoiceBank.Play("comm-" + it.Key)) Speech.SayAlways(it.Say); };
            if (delay == 0) speak();
            else
            {
                var t2 = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(delay) };
                t2.Tick += (s, e) => { t2.Stop(); speak(); };
                t2.Start();
            }

            if (btn != null)
            {
                var sc = new ScaleTransform(1, 1);
                btn.RenderTransform = sc;
                var pop = new DoubleAnimation(1, 1.08, TimeSpan.FromMilliseconds(200))
                { AutoReverse = true, EasingFunction = new SineEase() };
                sc.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
                sc.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
            }
        }
    }
}
