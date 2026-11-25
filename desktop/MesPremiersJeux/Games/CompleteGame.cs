using System;
using System.Windows.Controls;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// « Le panier » (MS — maths : COMPLÉMENTS de 2 à 6, compléter une
    /// collection, droit du programme de l'école) : il faut 4 fruits dans le
    /// panier ; il y en a déjà quelques-uns — combien en manque-t-il ?
    /// Acte 1 : on complète librement (chaque fruit regardé se décroche de
    /// l'arbre et vole dans le panier, on compte à voix haute). Acte 2 : on
    /// DEVINE combien il en manque en choisissant une pancarte, puis on
    /// vérifie en comptant ensemble — l'erreur enseigne. Zéro lecture.
    ///
    /// L'activité choisit les nombres, pose les questions et parle ; le verger
    /// qu'on voit (l'arbre, le panier, les pancartes) est dans PanierMoteur.cs,
    /// ses dessins dans NombresArt.cs (fabriqué par desktop/jeux-dessines).
    /// </summary>
    public sealed class CompleteGame : GameControl
    {
        private int Target = 4; // le total du panier : change à chaque tour (2 à 6)
        private int _lastMissing, _missing;

        private static readonly (string Dessin, string Emoji, string One, string Many, bool Fem)[] Items =
        {
            ("fruit_pomme", "🍎", "pomme", "pommes", true), ("fruit_poire", "🍐", "poire", "poires", true),
            ("fruit_orange", "🍊", "orange", "oranges", true), ("fruit_citron", "🍋", "citron", "citrons", false),
        };

        private readonly Random _rng = new Random();
        private readonly PanierMoteur _moteur = new PanierMoteur();
        private readonly UserControl _scene = new UserControl();
        private readonly VecToile _toile;
        private int _act;
        private int _round;
        private int _inBasket;
        private (string Dessin, string Emoji, string One, string Many, bool Fem) _item;

        public CompleteGame(Action celebrate) : base(celebrate)
        {
            _toile = new VecToile(_scene, _moteur, null, nameof(CompleteGame), null, incruste: true);
            _moteur.Demande = SurDemande;
        }

        protected override void NewRound()
        {
            _act = 1;
            _round = 0;
            Next();
        }

        private void Next()
        {
            Locked = false;
            _round++;
            if (_act == 1 && _round > 2) { _act = 2; _round = 1; }
            if (_act == 2 && _round > 3)
            {
                Question.Text = "🧺 Champion du panier !";
                Speak("Bravo ! Tu sais compléter le panier ! Quel champion !");
                GameKit.Success();
                Celebrate();
                Schedule(3200, NewRound);
                return;
            }

            _item = Items[_rng.Next(Items.Length)];
            // Le total varie (pioche 2..6) et le nombre qui MANQUE varie aussi :
            // jamais le même deux fois de suite, et jamais plus de quatre (il y a quatre pancartes).
            Target = GameKit.NextNumber("complement-total", 2, Math.Min(6, Level.MaxNumber)); // le panier : 6 au plus
            int missing;
            int guard = 0;
            do { missing = 1 + _rng.Next(Math.Min(4, Target - 1)); }
            while (missing == _lastMissing && Target > 2 && ++guard < 20);
            _lastMissing = missing;
            _missing = missing;
            _inBasket = Target - missing; // au moins 1 déjà dans le panier

            Question.Text = "🧺 Le panier de " + Target;
            SetConsigne(Art.ForEmoji(_item.Emoji, 100),
                () => _act == 1
                    ? "Il faut " + Target + " " + _item.Many + " dans le panier ! Ajoute les " + _item.Many + " qui manquent !"
                    : "Il y a " + _inBasket + " " + (_inBasket > 1 ? _item.Many : _item.One) + "... Il en faut " + Target + " ! COMBIEN en manque-t-il ? Choisis le chiffre !");

            _moteur.Prepare(_item.Dessin, Target, _inBasket, _act == 1, _act == 1 ? 0 : 4);
            if (_scene.Parent == null) SetBodyFill(_scene);

            Speak(_act == 1
                ? "Il faut " + Target + " " + _item.Many + " dans le panier ! Regarde : il y en a déjà " + _inBasket +
                  " ! Ajoute " + (_item.Fem ? "celles" : "ceux") + " qui manquent !"
                : "Il y a " + _inBasket + " " + (_inBasket > 1 ? _item.Many : _item.One) + " dans le panier. Il en faut " + Target +
                  " ! Combien en manque-t-il ? Choisis le chiffre !");
        }

        /// <summary>Ce que le verger signale : le panier est plein, une pancarte est choisie, la vérification est finie.</summary>
        private void SurDemande(string quoi)
        {
            if (Closed || quoi == null) return;
            if (quoi == "plein")
            {
                // Acte 1 : chaque fruit regardé a volé dans le panier, la voix a compté.
                Locked = true;
                GameKit.Success();
                Speak(Target + " " + _item.Many + " ! Le panier est plein ! Bravo !");
                Celebrate();
                Schedule(2800, Next);
            }
            else if (quoi.StartsWith("choix:"))
            {
                // Acte 2 : combien en manque-t-il ? (puis vérification en comptant)
                if (Locked || !int.TryParse(quoi.Substring(6), out int choix)) return;
                if (choix == _missing)
                {
                    Locked = true;
                    GameKit.Success();
                    Speak("Oui ! Il en manque " + _missing + " ! On vérifie : on les ajoute !");
                    _moteur.Verifie(choix);   // les fruits manquants arrivent un par un
                }
                else
                {
                    _moteur.Refuse(choix);
                    Speak("Hmm... regarde bien ! Il y a " + _inBasket + " " + (_inBasket > 1 ? _item.Many : _item.One) +
                          ", et il en faut " + Target + " ! Compte avec moi ce qui manque !");
                    _moteur.Indice();         // les places vides clignotent
                }
            }
            else if (quoi == "verifie")
            {
                Speak(Target + " " + _item.Many + " ! Le compte est bon ! Bravo !");
                Celebrate();
                Schedule(2600, Next);
            }
        }
    }
}
