using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// 🎴 LE MÊME : une carte modèle en haut, des cartes en bas, toutes visibles —
    /// retrouver la même. C'est l'étape d'avant « Les paires » : rien à retenir,
    /// seulement comparer. Une carte fausse s'efface (il reste moins de choix) ;
    /// après trois réussites d'affilée au premier regard, les cartes proposées
    /// viennent de la même famille que le modèle, donc se ressemblent davantage.
    /// </summary>
    public sealed class SameGame : GameControl
    {
        private int _serie;      // réussites d'affilée au premier regard
        private bool _erreur;
        private ScaleTransform _modele;

        public SameGame(Action celebrate) : base(celebrate) { }

        private static UIElement Draw(CartoonItem item, double size)
            => new Viewbox { Child = item.Build(), Width = size, Height = size };

        protected override void NewRound()
        {
            Locked = false;
            _erreur = false;
            var target = GameKit.Rand(CartoonArt.Items);

            // Au début, les autres cartes sont d'une autre famille (très différentes) ; ensuite, de la même.
            IList<CartoonItem> vivier = CartoonArt.Items;
            if (!string.IsNullOrEmpty(target.Cat))
            {
                var choix = _serie >= 3
                    ? CartoonArt.Items.Where(i => i.Cat == target.Cat).ToList()
                    : CartoonArt.Items.Where(i => i.Cat != target.Cat || i.Name == target.Name).ToList();
                if (choix.Count >= Level.Choices) vivier = choix;
            }
            var options = Level.Pick(vivier, target);

            Question.Text = "Où est la même carte ?";
            SetConsigne(null, () => "Regarde bien la carte du haut. Où est la même ?");

            // Le modèle, dans un cadre doré, bien séparé des réponses.
            _modele = new ScaleTransform(1, 1);
            var cadre = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0x00)),
                BorderThickness = new Thickness(8),
                CornerRadius = new CornerRadius(34),
                Padding = new Thickness(22),
                Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 3, Opacity = 0.25 },
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = _modele,
                Child = Draw(target, 250),
            };

            var row = Row();
            Button bon = null;
            foreach (var s in options)
            {
                var btn = AnswerButton(Draw(s, 175));
                btn.Margin = new Thickness(12);
                var chosen = s;
                btn.Click += (ev, e) => Answer(chosen, target, btn);
                if (s.Name == target.Name) bon = btn;
                row.Children.Add(btn);
            }

            SetBody(cadre, row);
            if (bon != null) ArmHint(bon, () => "Regarde la carte du haut. Où est la même ?");
            Schedule(350, () => SpeakConsigne("Regarde bien cette carte. Où est la même ?"));
        }

        private void Battre()
        {
            if (_modele == null) return;
            var a = new DoubleAnimation(1, 1.12, TimeSpan.FromMilliseconds(260)) { AutoReverse = true, RepeatBehavior = new RepeatBehavior(2), EasingFunction = new SineEase() };
            _modele.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            _modele.BeginAnimation(ScaleTransform.ScaleYProperty, a);
        }

        private void Answer(CartoonItem s, CartoonItem target, Button btn)
        {
            if (Locked) return;
            if (s.Name == target.Name)
            {
                Locked = true;
                DisarmHint();
                _serie = _erreur ? 0 : _serie + 1;
                Highlight(btn);
                Battre();
                GameKit.Success();
                Celebrate();
                Speak("Oui ! C'est pareil : " + target.Fr + " !");
                ScheduleNext(2600);
            }
            else
            {
                _erreur = true;
                _serie = 0;
                GameKit.Wrong();
                Shake(btn);
                // La carte fausse s'efface : il reste moins de choix, jamais d'impasse.
                btn.IsEnabled = false;
                btn.Opacity = 0.3;
                Battre();
                Speak("Ce n'est pas pareil. Regarde encore la carte du haut.");
            }
        }
    }
}
