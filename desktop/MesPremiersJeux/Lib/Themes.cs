using System;
using System.Collections.Generic;
using System.Linq;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 💭 « AUTRE CHOSE » : les grilles par thème. Quand aucune case proposée ne
    /// convient, l'enfant choisit un thème (manger, boire, les gens, aller où,
    /// ce que je ressens, faire, les jouets, mon corps, l'école, les habits, les
    /// animaux, dehors, quand, je demande, les fêtes, les couleurs) puis un mot
    /// parmi beaucoup : il peut dire ce qu'il veut, et aller tout à fait ailleurs
    /// que là où la question l'emmenait. Les proches du tableau de communication
    /// (photos) viennent en tête du thème « Les gens ». Les mêmes mots pour
    /// toutes les familles ; les mots de l'enfant sont dans « Mes mots ».
    /// </summary>
    public static class Themes
    {
        public sealed class Theme
        {
            public string Cle, Nom, Emoji, Mot; // Mot : le mot ARASAAC du thème lui-même, pour la case
            public List<Suggest.Reponse> Mots = new List<Suggest.Reponse>();
        }

        private static Suggest.Reponse R(string texte, string mot, string emoji) => new Suggest.Reponse { Texte = texte, Mot = mot, Emoji = emoji, Cle = "", Picto = "" };

        private static Theme T(string cle, string nom, string emoji, string mot, params Suggest.Reponse[] mots) => new Theme { Cle = cle, Nom = nom, Emoji = emoji, Mot = mot, Mots = mots.ToList() };

        private static readonly List<Theme> Tous = new List<Theme>
        {
            T("manger", "Manger", "🍽️", "manger",
                R("Du pain", "pain", "🍞"), R("Une tartine", "tartine", "🍞"), R("Des céréales", "céréales", "🥣"), R("Un yaourt", "yaourt", "🥛"), R("Une compote", "compote", "🍎"),
                R("Une banane", "banane", "🍌"), R("Une pomme", "pomme", "🍎"), R("Des fraises", "fraise", "🍓"), R("Du raisin", "raisin", "🍇"), R("Une clémentine", "mandarine", "🍊"),
                R("Un gâteau", "gâteau", "🍰"), R("Un biscuit", "biscuit", "🍪"), R("Du chocolat", "chocolat", "🍫"), R("Une glace", "glace", "🍦"), R("Des bonbons", "bonbon", "🍬"),
                R("Des pâtes", "pâtes", "🍝"), R("Du riz", "riz", "🍚"), R("Des frites", "frites", "🍟"), R("De la purée", "purée", "🥔"), R("Du poulet", "poulet", "🍗"),
                R("Du poisson", "poisson", "🐟"), R("Du jambon", "jambon", "🥓"), R("Du fromage", "fromage", "🧀"), R("Un œuf", "œuf", "🥚"), R("Une pizza", "pizza", "🍕"),
                R("De la soupe", "soupe", "🍲"), R("Des légumes", "légumes", "🥕"), R("Des carottes", "carotte", "🥕"), R("Des petits pois", "petit pois", "🟢"), R("Une crêpe", "crêpe", "🥞"),
                R("J'ai faim", "faim", "😋"), R("Je n'ai plus faim", "fini", "🙅"), R("Encore", "encore", "🔁"), R("C'est bon", "bon", "😋"), R("Je n'aime pas", "ne pas aimer", "🙁")),
            T("boire", "Boire", "🥤", "boire",
                R("De l'eau", "eau", "💧"), R("Du lait", "lait", "🥛"), R("Un jus de pomme", "jus de pomme", "🧃"), R("Un jus d'orange", "jus d'orange", "🍊"), R("Un chocolat chaud", "chocolat chaud", "☕"),
                R("Un sirop", "sirop", "🧃"), R("Une tisane", "tisane", "🍵"), R("Un smoothie", "smoothie", "🥤"), R("Avec une paille", "paille", "🥤"), R("Dans mon verre", "verre", "🥛"),
                R("Dans mon biberon", "biberon", "🍼"), R("Chaud", "chaud", "🔥"), R("Froid", "froid", "🧊"), R("J'ai soif", "soif", "💧"), R("Encore", "encore", "🔁"), R("Je n'ai plus soif", "fini", "🙅")),
            T("gens", "Les gens", "👪", "famille",
                R("Maman", "maman", "👩"), R("Papa", "papa", "👨"), R("Mamie", "grand-mère", "👵"), R("Papi", "grand-père", "👴"), R("Mon frère", "frère", "👦"), R("Ma sœur", "sœur", "👧"),
                R("Le bébé", "bébé", "👶"), R("Tonton", "oncle", "🧔"), R("Tata", "tante", "👩‍🦰"), R("Mon cousin", "cousin", "🧒"), R("Ma cousine", "cousine", "👧"), R("La nounou", "nounou", "👩‍🍼"),
                R("La maîtresse", "maîtresse", "👩‍🏫"), R("Mon AESH", "accompagnante", "🧑‍🤝‍🧑"), R("Un copain", "ami", "🧒"), R("Une copine", "amie", "👧"), R("Le docteur", "médecin", "🩺"), R("La kiné", "kinésithérapeute", "💪"),
                R("L'orthophoniste", "orthophoniste", "🗣️"), R("Le chien", "chien", "🐶"), R("Le chat", "chat", "🐱"), R("Tout le monde", "tous", "👥"), R("Personne", "personne", "🚫"), R("Moi", "moi", "🙋")),
            T("lieux", "Aller où ?", "🏠", "lieu",
                R("À la maison", "maison", "🏠"), R("Dans ma chambre", "chambre", "🛏️"), R("Au salon", "salon", "🛋️"), R("À la cuisine", "cuisine", "🍳"), R("Au lit", "lit", "🛏️"), R("Aux toilettes", "toilettes", "🚽"),
                R("Dans le bain", "bain", "🛁"), R("Au jardin", "jardin", "🌳"), R("Dehors", "dehors", "🌤️"), R("Au parc", "parc", "🛝"), R("À l'école", "école", "🏫"), R("Chez mamie", "grand-mère", "👵"),
                R("Chez papi", "grand-père", "👴"), R("À la piscine", "piscine", "🏊"), R("Au magasin", "magasin", "🛒"), R("En voiture", "voiture", "🚗"), R("À la plage", "plage", "🏖️"), R("Dans la forêt", "forêt", "🌲"),
                R("Au manège", "manège", "🎠"), R("À la bibliothèque", "bibliothèque", "📚"), R("Au restaurant", "restaurant", "🍽️"), R("Chez le docteur", "médecin", "🩺"), R("Au cinéma", "cinéma", "🎬"), R("Je veux rester ici", "rester", "🧍")),
            T("ressens", "Je ressens", "😊", "émotion",
                R("Je suis contente", "content", "😄"), R("Je suis triste", "triste", "😢"), R("Je suis en colère", "colère", "😠"), R("J'ai peur", "peur", "😨"), R("Je suis fatiguée", "fatigue", "😴"), R("J'ai sommeil", "dormir", "🌙"),
                R("Je m'ennuie", "ennui", "😐"), R("Je suis fière", "fier", "😎"), R("C'est rigolo", "rire", "😂"), R("Je suis surprise", "surprise", "😲"), R("J'ai honte", "honte", "😳"), R("Je suis calme", "calme", "😌"),
                R("J'ai mal", "douleur", "🤕"), R("J'ai chaud", "chaud", "🥵"), R("J'ai froid", "froid", "🥶"), R("Je suis malade", "malade", "🤒"), R("J'ai envie de pleurer", "pleurer", "😭"), R("Je t'aime", "amour", "❤️"),
                R("Je veux un câlin", "câlin", "🤗"), R("Je veux être tranquille", "tranquille", "🤫"), R("C'est trop fort", "bruit", "🙉"), R("Je ne sais pas", "ne pas savoir", "🤷"), R("Ça va", "bien", "🙂"), R("Ça ne va pas", "mal", "🙁")),
            T("faire", "Faire", "🎮", "jouer",
                R("Je veux jouer", "jouer", "🎮"), R("Je veux dessiner", "dessiner", "✏️"), R("Je veux peindre", "peindre", "🎨"), R("Une histoire", "livre", "📖"), R("Regarder la télé", "télévision", "📺"), R("De la musique", "musique", "🎵"),
                R("Danser", "danser", "💃"), R("Chanter", "chanter", "🎤"), R("Des bulles", "bulles de savon", "🫧"), R("Jouer dehors", "jardin", "🌳"), R("Faire du vélo", "vélo", "🚲"), R("Me balancer", "balançoire", "🛝"),
                R("Dormir", "dormir", "😴"), R("Prendre le bain", "bain", "🛁"), R("Cuisiner", "cuisiner", "👩‍🍳"), R("Un puzzle", "puzzle", "🧩"), R("La tablette", "tablette", "📱"), R("Regarder des photos", "photo", "🖼️"),
                R("Jouer à l'eau", "eau", "💦"), R("Un jeu sur Eyzi", "jeu", "🎲"), R("Me promener", "promenade", "🚶"), R("Me reposer", "repos", "🛋️"), R("Rien", "rien", "🚫"), R("Autre chose", "", "💭")),
            T("jouets", "Les jouets", "🧸", "jouet",
                R("Mon doudou", "peluche", "🧸"), R("Ma poupée", "poupée", "🪆"), R("Le ballon", "ballon", "⚽"), R("Les cubes", "cubes", "🧱"), R("Les Lego", "briques", "🧱"), R("La voiture", "petite voiture", "🚗"),
                R("Le train", "train", "🚂"), R("La dînette", "dînette", "🍽️"), R("Les crayons", "crayon", "🖍️"), R("La pâte à modeler", "pâte à modeler", "🟣"), R("Les bulles", "bulles de savon", "🫧"), R("Le tambour", "tambour", "🥁"),
                R("Le xylophone", "xylophone", "🎹"), R("Le livre", "livre", "📖"), R("Le puzzle", "puzzle", "🧩"), R("La trottinette", "trottinette", "🛴"), R("Le toboggan", "toboggan", "🛝"), R("Le trampoline", "trampoline", "🤸"),
                R("Les animaux", "animaux", "🐘"), R("Le déguisement", "déguisement", "🎭"), R("Les gommettes", "gommettes", "⭐"), R("Le tricycle", "tricycle", "🚲"), R("Les perles", "perles", "📿"), R("Mon jouet préféré", "jouet", "💖")),
            T("corps", "Mon corps", "🤕", "corps",
                R("J'ai mal à la tête", "tête", "🤕"), R("J'ai mal au ventre", "ventre", "🤢"), R("J'ai mal aux dents", "dent", "🦷"), R("J'ai mal à l'oreille", "oreille", "👂"), R("J'ai mal à la gorge", "gorge", "😷"), R("J'ai mal au bras", "bras", "💪"),
                R("J'ai mal à la jambe", "jambe", "🦵"), R("J'ai mal à la main", "main", "✋"), R("J'ai mal au pied", "pied", "🦶"), R("J'ai mal au dos", "dos", "🧍"), R("J'ai mal au genou", "genou", "🦵"), R("J'ai mal aux yeux", "œil", "👁️"),
                R("J'ai mal au nez", "nez", "👃"), R("J'ai mal à la bouche", "bouche", "👄"), R("J'ai mal aux fesses", "fesses", "🍑"), R("Je veux aller aux toilettes", "toilettes", "🚽"), R("Je suis mouillée", "mouillé", "💧"), R("Ça gratte", "gratter", "🖐️"),
                R("J'ai envie de vomir", "vomir", "🤮"), R("J'ai le hoquet", "hoquet", "😮"), R("J'ai le nez bouché", "rhume", "🤧"), R("Je veux un pansement", "pansement", "🩹"), R("Je veux un médicament", "médicament", "💊"), R("Ça va mieux", "mieux", "🙂")),
            T("ecole", "L'école", "🏫", "école",
                R("La maîtresse", "maîtresse", "👩‍🏫"), R("Les copains", "ami", "🧒"), R("La récré", "récréation", "🛝"), R("La cantine", "cantine", "🍽️"), R("La sieste", "sieste", "😴"), R("La peinture", "peinture", "🎨"),
                R("Les chansons", "chanson", "🎵"), R("Le dessin", "dessin", "✏️"), R("La cour", "cour", "🏫"), R("Le cartable", "cartable", "🎒"), R("La motricité", "gymnastique", "🤸"), R("Les livres", "livre", "📚"),
                R("La pâte à modeler", "pâte à modeler", "🟣"), R("Les comptines", "comptine", "🎶"), R("Le bus", "bus", "🚌"), R("Mon AESH", "accompagnante", "🧑‍🤝‍🧑"), R("Les ateliers", "atelier", "✂️"), R("Le goûter", "goûter", "🍪"),
                R("La bibliothèque", "bibliothèque", "📖"), R("Un copain m'a embêtée", "dispute", "😠"), R("J'ai bien travaillé", "bravo", "⭐"), R("Je veux y aller", "école", "🏫"), R("Je ne veux pas y aller", "non", "🙅"), R("Je veux rentrer", "maison", "🏠")),
            T("habits", "Les habits", "👕", "vêtement",
                R("Le pull", "pull", "🧥"), R("Le tee-shirt", "tee-shirt", "👕"), R("Le pantalon", "pantalon", "👖"), R("La robe", "robe", "👗"), R("La jupe", "jupe", "👗"), R("Le manteau", "manteau", "🧥"),
                R("Les chaussures", "chaussures", "👟"), R("Les bottes", "bottes", "👢"), R("Les chaussettes", "chaussettes", "🧦"), R("Le bonnet", "bonnet", "🧢"), R("L'écharpe", "écharpe", "🧣"), R("Les gants", "gants", "🧤"),
                R("Le pyjama", "pyjama", "🛏️"), R("La culotte", "culotte", "🩲"), R("Le gilet", "gilet", "🧥"), R("La casquette", "casquette", "🧢"), R("Les lunettes", "lunettes", "👓"), R("Le maillot de bain", "maillot de bain", "🩱"),
                R("Le bavoir", "bavoir", "🍼"), R("La couche", "couche", "👶"), R("J'ai trop chaud", "chaud", "🥵"), R("J'ai froid", "froid", "🥶"), R("Ça me serre", "serré", "😣"), R("Je veux me changer", "changer", "🔁")),
            T("animaux", "Les animaux", "🐱", "animal",
                R("Le chat", "chat", "🐱"), R("Le chien", "chien", "🐶"), R("Le lapin", "lapin", "🐰"), R("Le cheval", "cheval", "🐴"), R("La poule", "poule", "🐔"), R("La vache", "vache", "🐮"),
                R("Le mouton", "mouton", "🐑"), R("Le cochon", "cochon", "🐷"), R("Le canard", "canard", "🦆"), R("L'oiseau", "oiseau", "🐦"), R("Le poisson", "poisson", "🐟"), R("Le papillon", "papillon", "🦋"),
                R("La coccinelle", "coccinelle", "🐞"), R("Le lion", "lion", "🦁"), R("L'éléphant", "éléphant", "🐘"), R("La girafe", "girafe", "🦒"), R("Le singe", "singe", "🐵"), R("L'ours", "ours", "🐻"),
                R("La tortue", "tortue", "🐢"), R("La grenouille", "grenouille", "🐸"), R("L'escargot", "escargot", "🐌"), R("Le dinosaure", "dinosaure", "🦕"), R("La licorne", "licorne", "🦄"), R("Le loup", "loup", "🐺")),
            T("dehors", "Dehors", "🌳", "nature",
                R("Le soleil", "soleil", "☀️"), R("La pluie", "pluie", "🌧️"), R("La neige", "neige", "❄️"), R("Le vent", "vent", "🌬️"), R("Les nuages", "nuage", "☁️"), R("L'arc-en-ciel", "arc-en-ciel", "🌈"),
                R("Les arbres", "arbre", "🌳"), R("Les fleurs", "fleur", "🌸"), R("L'herbe", "herbe", "🌿"), R("Les feuilles", "feuille", "🍂"), R("Les cailloux", "caillou", "🪨"), R("Le sable", "sable", "🏖️"),
                R("La mer", "mer", "🌊"), R("La rivière", "rivière", "🏞️"), R("La montagne", "montagne", "⛰️"), R("La lune", "lune", "🌙"), R("Les étoiles", "étoile", "⭐"), R("Le bateau", "bateau", "⛵"),
                R("Les flaques", "flaque", "💦"), R("La balançoire", "balançoire", "🛝"), R("Le toboggan", "toboggan", "🛝"), R("Le vélo", "vélo", "🚲"), R("La poussette", "poussette", "👶"), R("Je veux sortir", "sortir", "🚪")),
            T("quand", "Quand ?", "⏰", "temps",
                R("Maintenant", "maintenant", "👉"), R("Tout de suite", "vite", "⚡"), R("Après", "après", "⏭️"), R("Plus tard", "plus tard", "⏰"), R("Encore un peu", "un peu", "🤏"), R("Ce soir", "soir", "🌆"),
                R("Demain", "demain", "📅"), R("Après le dodo", "dormir", "😴"), R("Après manger", "manger", "🍽️"), R("Après l'école", "école", "🏫"), R("Ce week-end", "week-end", "🎉"), R("À midi", "midi", "🕛"),
                R("Jamais", "jamais", "🚫"), R("Tout le temps", "toujours", "♾️"), R("Pas encore", "attendre", "✋"), R("Vite", "vite", "🏃"), R("Doucement", "doucement", "🐢"), R("C'est fini", "fini", "🏁")),
            T("demande", "Je demande", "❓", "question",
                R("Et toi ?", "toi", "👉"), R("Pourquoi ?", "pourquoi", "❓"), R("C'est quand ?", "quand", "⏰"), R("On va où ?", "où", "🗺️"), R("Qui vient ?", "qui", "👥"), R("C'est quoi ?", "quoi", "❔"),
                R("Tu restes avec moi ?", "rester", "🤝"), R("On y va ?", "aller", "🚶"), R("Tu m'aides ?", "aide", "🙋"), R("Je peux ?", "pouvoir", "🙏"), R("Raconte-moi", "raconter", "📣"), R("Montre-moi", "montrer", "👀"),
                R("On fait quoi après ?", "après", "⏭️"), R("Maman arrive quand ?", "maman", "👩"), R("Où est papa ?", "papa", "👨"), R("C'est bientôt fini ?", "fini", "🏁"), R("Encore une fois ?", "encore", "🔁"), R("Tu viens jouer ?", "jouer", "🎮"),
                R("Qu'est-ce qu'on mange ?", "repas", "🍽️"), R("Tu es fâchée ?", "colère", "😠"), R("Tu m'aimes ?", "amour", "❤️"), R("C'est pour moi ?", "cadeau", "🎁"), R("On peut sortir ?", "dehors", "🌳"), R("Je t'écoute", "écouter", "👂")),
            T("fetes", "Les fêtes", "🎉", "fête",
                R("Noël", "Noël", "🎄"), R("Le sapin", "sapin de Noël", "🎄"), R("Le Père Noël", "Père Noël", "🎅"), R("Les cadeaux", "cadeau", "🎁"), R("Mon anniversaire", "anniversaire", "🎂"), R("Les bougies", "bougie", "🕯️"),
                R("Le gâteau", "gâteau", "🍰"), R("Les ballons", "ballon de baudruche", "🎈"), R("Un déguisement", "déguisement", "🎭"), R("La musique", "musique", "🎵"), R("Danser", "danser", "💃"), R("Les copains", "ami", "🧒"),
                R("Le cinéma", "cinéma", "🎬"), R("Le zoo", "zoo", "🦁"), R("La ferme", "ferme", "🐄"), R("Le manège", "manège", "🎠"), R("Le marché", "marché", "🛒"), R("Le train", "train", "🚂"),
                R("La piscine", "piscine", "🏊"), R("Les vacances", "vacances", "🏖️"), R("Un pique-nique", "pique-nique", "🧺"), R("Les feux d'artifice", "feu d'artifice", "🎆"), R("Pâques", "Pâques", "🐣"), R("Halloween", "Halloween", "🎃")),
            T("couleurs", "Les couleurs", "🎨", "couleur",
                R("Rouge", "rouge", "🔴"), R("Bleu", "bleu", "🔵"), R("Jaune", "jaune", "🟡"), R("Vert", "vert", "🟢"), R("Rose", "rose", "🩷"), R("Violet", "violet", "🟣"),
                R("Orange", "orange", "🟠"), R("Noir", "noir", "⚫"), R("Blanc", "blanc", "⚪"), R("Marron", "marron", "🟤"), R("Gris", "gris", "🩶"), R("Doré", "doré", "⭐"),
                R("Un rond", "cercle", "⭕"), R("Un carré", "carré", "🟥"), R("Un triangle", "triangle", "🔺"), R("Une étoile", "étoile", "⭐"), R("Un cœur", "cœur", "❤️"), R("Grand", "grand", "🐘"),
                R("Petit", "petit", "🐜"), R("Beaucoup", "beaucoup", "🔢"), R("Un peu", "un peu", "🤏"), R("Pareil", "pareil", "🟰"), R("Pas pareil", "différent", "↔️"), R("Celui-là", "celui-là", "👉")),
        };

        /// <summary>Les thèmes, dans l'ordre des grilles.</summary>
        public static IReadOnlyList<Theme> Liste => Tous;

        public static Theme Trouver(string cle) => Tous.FirstOrDefault(t => t.Cle == cle);

        /// <summary>Les mots d'un thème ; pour « Les gens », les proches du tableau (photos) d'abord, puis les mots sans doublon.</summary>
        public static List<Suggest.Reponse> MotsDe(Theme t)
        {
            if (t == null) return new List<Suggest.Reponse>();
            if (t.Cle != "gens") return t.Mots;
            var res = new List<Suggest.Reponse>();
            var vus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var m in Views.CommBoard.Vocabulaire().Where(m => (m.Cle ?? "").StartsWith("pers-")))
                    if (vus.Add(m.Label)) res.Add(new Suggest.Reponse { Texte = m.Label, Cle = m.Cle, Emoji = m.Emoji, Mot = "", Picto = "" });
            }
            catch { }
            foreach (var r in t.Mots) if (vus.Add(r.Texte)) res.Add(r);
            return res;
        }
    }
}
