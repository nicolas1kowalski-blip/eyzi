using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MesPremiersJeux.Learning;
using MesPremiersJeux.Lib;

namespace MesPremiersJeux.Games
{
    /// <summary>
    /// UNE LEÇON : enchaîne les pas d'une leçon (ou d'un défi) composée par
    /// LessonBuilder, avec les trois formes d'écran (choix, attribution, ordre),
    /// enregistre ce qui est réussi au premier regard, puis le récapitulatif.
    /// </summary>
    public sealed class LessonControl : GameControl
    {
        private readonly Lecon _lecon;
        private readonly Action<string> _onDone;   // « encore » ou « chemin »
        private int _index = -1;
        private bool _errored;
        private Pas Pas => _index >= 0 && _index < _lecon.Pas.Count ? _lecon.Pas[_index] : null;

        private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(0x3B, 0x2A, 0x5A));
        private static readonly Brush Gold = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));

        public LessonControl(Action celebrate, Lecon lecon, Action<string> onDone) : base(celebrate)
        {
            _lecon = lecon;
            _onDone = onDone;
        }

        protected override void NewRound() => Suivant();

        // ------------------------------------------------------------------
        //  Enchaînement
        // ------------------------------------------------------------------
        private void Suivant()
        {
            DisarmHint();
            Unlight();
            _index++;
            if (_index >= _lecon.Pas.Count) { Terminer(); return; }
            _errored = false;
            Locked = false;
            Question.Text = (_lecon.EstDefi ? "⚡ Défi  " : "🧭 Leçon  ") + Points();
            Emoji.Fix(Question);
            var ex = Pas.Exercice;
            try
            {
                if (ex is ExerciceChoix c) MontrerChoix(c);
                else if (ex is ExerciceAttribution a) MontrerAttribution(a);
                else if (ex is ExerciceOrdre o) MontrerOrdre(o);
                else Suivant();
            }
            catch (Exception err)
            {
                // Un exercice qui casse ne casse pas la leçon : on le saute, noté dans le journal.
                Log.Write("lecons", "Exercice " + Pas.Noeud.Key + " axe " + Pas.Axe + " : " + err);
                Pas.Reussi = false;
                Schedule(100, Suivant);
            }
        }

        private string Points()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _lecon.Pas.Count; i++) sb.Append(i < _index ? (_lecon.Pas[i].Reussi ? "●" : "◐") : i == _index ? "◉" : "○");
            return sb.ToString();
        }

        private void DireConsigne(string texte, int delay = 350) => Schedule(delay, () => Speak(texte));

        private FrameworkElement Modele(Exercice ex) => _lecon.EstDefi || ex.Modele == null ? null : ex.Modele();

        private void Reussite(Button btn, string bravo)
        {
            Locked = true;
            DisarmHint();
            Pas.Reussi = !_errored;
            GameKit.Success();
            if (btn != null) Highlight(btn);
            Speak(bravo ?? GameKit.Praise());
            RewardStar();
            Schedule(1600, Suivant);
        }

        private void Erreur(Button btn, string consigne, bool cacher)
        {
            _errored = true;
            Shake(btn);
            if (cacher) btn.Visibility = Visibility.Hidden; // une mauvaise cible en moins : on ne bloque jamais
            Speak(GameKit.Encourage());
            Schedule(1500, () => Speak(consigne));
        }

        private double TailleReponse(int n, double voulue) => voulue > 0 ? voulue : n <= 2 ? 300 : n == 3 ? 260 : n == 4 ? 230 : 195;

        private Button Reponse(Carte carte, double size)
        {
            var v = carte.Visuel();
            var btn = AnswerButton(new Viewbox { Child = v, Margin = new Thickness(12), Stretch = Stretch.Uniform }, size);
            return btn;
        }

        private Panel Rangee(IList<Button> boutons)
        {
            if (boutons.Count <= 4)
            {
                var row = Row();
                foreach (var b in boutons) { b.Margin = new Thickness(10); row.Children.Add(b); }
                return row;
            }
            var grid = new UniformGrid { Columns = 3, HorizontalAlignment = HorizontalAlignment.Center };
            foreach (var b in boutons) { b.Margin = new Thickness(8); grid.Children.Add(b); }
            return grid;
        }

        private static FrameworkElement Scene(FrameworkElement content, double height = 360)
            => new Viewbox { Child = content, MaxHeight = height, MaxWidth = 1300, Stretch = Stretch.Uniform, Margin = new Thickness(0, 6, 0, 6) };

        // ------------------------------------------------------------------
        //  CHOIX : une scène facultative, des réponses dont une bonne
        // ------------------------------------------------------------------
        private void MontrerChoix(ExerciceChoix c)
        {
            SetConsigne(Modele(c), () => c.Consigne, 72);
            var boutons = new List<Button>();
            Button bon = null;
            double size = TailleReponse(c.Reponses.Count, c.Taille);
            foreach (var carte in c.Reponses)
            {
                var cap = carte;
                var btn = Reponse(carte, size);
                btn.Click += (s, e) =>
                {
                    if (Locked) return;
                    if (cap.Bonne) Reussite(btn, c.Bravo ?? (string.IsNullOrEmpty(cap.Dit) ? null : "Oui ! " + cap.Dit + " !"));
                    else Erreur(btn, c.Consigne, c.Reponses.Count > 2);
                };
                if (carte.Bonne) bon = btn;
                boutons.Add(btn);
            }
            var answers = Rangee(boutons);
            if (c.Scene != null) SetBody(Scene(c.Scene()), answers);
            else SetBody(answers);
            if (!_lecon.EstDefi && bon != null) ArmHint(bon, () => c.Consigne);
            DireConsigne(c.Consigne);
        }

        // ------------------------------------------------------------------
        //  ATTRIBUTION : chaque objet, un par un, vers sa cible
        // ------------------------------------------------------------------
        private void MontrerAttribution(ExerciceAttribution a)
        {
            int index = 0;
            string ConsigneObjet(ExerciceAttribution ex) => index < ex.Objets.Count ? string.Format(ex.ConsigneObjet, ex.Objets[index].Dit) : ex.Consigne;
            SetConsigne(Modele(a), () => ConsigneObjet(a), 72);
            var compteurs = new List<TextBlock>();
            var boutons = new List<Button>();
            ContentControl stageHost = new ContentControl { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

            void Afficher()
            {
                if (index >= a.Objets.Count) return;
                var obj = a.Objets[index].Visuel();
                var row = Row();
                row.Children.Add(new Viewbox { Child = obj, Height = 300, Margin = new Thickness(0, 0, 30, 0) });
                row.Children.Add(new TextBlock { Text = "➜", FontSize = 110, Foreground = Ink, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.7 });
                row.Children.Add(new TextBlock { Text = "?", FontSize = 150, FontWeight = FontWeights.ExtraBold, Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x2C)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(24, 0, 0, 0) });
                var reste = new TextBlock { Text = "encore " + (a.Objets.Count - index), FontSize = 26, Foreground = Ink, Opacity = 0.6, HorizontalAlignment = HorizontalAlignment.Center };
                var st = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                st.Children.Add(row); st.Children.Add(reste);
                stageHost.Content = st;
                Locked = false;
            }

            for (int t = 0; t < a.Cibles.Count; t++)
            {
                int ti = t;
                var cible = a.Cibles[t];
                var inner = new Grid();
                inner.Children.Add(new Viewbox { Child = cible.Visuel(), Margin = new Thickness(10) });
                var badge = new TextBlock { Text = "", FontSize = 30, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Padding = new Thickness(10, 2, 10, 2), Background = new SolidColorBrush(Color.FromRgb(0x4C, 0xB8, 0x5E)), Visibility = Visibility.Collapsed };
                inner.Children.Add(badge);
                compteurs.Add(badge);
                var btn = AnswerButton(inner, 240);
                btn.Click += (s, e) =>
                {
                    if (Locked || index >= a.Objets.Count) return;
                    var obj = a.Objets[index];
                    if (obj.Cible == ti)
                    {
                        Locked = true;
                        DisarmHint();
                        GameKit.Success();
                        int n = int.TryParse(badge.Text, out var k) ? k + 1 : 1;
                        badge.Text = n.ToString(); badge.Visibility = Visibility.Visible;
                        index++;
                        if (index >= a.Objets.Count) { Reussite(btn, a.Bravo ?? "Tout est rangé ! Bravo !"); return; }
                        Speak("Oui !");
                        Schedule(900, () => { Afficher(); if (!_lecon.EstDefi) ArmHint(boutons[a.Objets[index].Cible], () => ConsigneObjet(a)); Speak(ConsigneObjet(a)); });
                    }
                    else Erreur(btn, ConsigneObjet(a), false);
                };
                boutons.Add(btn);
            }
            Afficher();
            SetBody(stageHost, Rangee(boutons));
            if (!_lecon.EstDefi) ArmHint(boutons[a.Objets[0].Cible], () => ConsigneObjet(a));
            DireConsigne(a.Consigne + " " + ConsigneObjet(a));
        }

        // ------------------------------------------------------------------
        //  ORDRE : remettre les cartes dans l'ordre
        // ------------------------------------------------------------------
        private void MontrerOrdre(ExerciceOrdre o)
        {
            SetConsigne(Modele(o), () => o.Consigne, 72);
            int next = 0;
            var slots = new List<Border>();
            var slotRow = Row();
            for (int i = 0; i < o.Cartes.Count; i++)
            {
                var slot = new Border
                {
                    Width = 170, Height = 170, Margin = new Thickness(8), CornerRadius = new CornerRadius(18),
                    Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0xC4)), BorderBrush = Ink, BorderThickness = new Thickness(3),
                    Child = new TextBlock { Text = (i + 1).ToString(), FontSize = 60, Foreground = Ink, Opacity = 0.35, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                };
                slots.Add(slot);
                slotRow.Children.Add(slot);
            }
            var boutons = new List<(Button Btn, int Index)>();
            var ordre = Enumerable.Range(0, o.Cartes.Count).OrderBy(_ => Visuels.Rng.Next()).ToList();
            foreach (var i in ordre)
            {
                int idx = i;
                var btn = Reponse(o.Cartes[i], TailleReponse(o.Cartes.Count, 0));
                btn.Click += (s, e) =>
                {
                    if (Locked) return;
                    if (idx == next)
                    {
                        Locked = true;
                        DisarmHint();
                        GameKit.Success();
                        btn.Visibility = Visibility.Hidden;
                        slots[next].Child = new Viewbox { Child = o.Cartes[idx].Visuel(), Margin = new Thickness(10) };
                        slots[next].Background = Brushes.White;
                        next++;
                        if (next >= o.Cartes.Count) { Reussite(null, o.Bravo ?? "Tout est dans l'ordre ! Bravo !"); return; }
                        Speak(o.ConsigneSuite);
                        Locked = false;
                        if (!_lecon.EstDefi) ArmHint(boutons.First(b => b.Index == next).Btn, () => o.ConsigneSuite);
                    }
                    else Erreur(btn, next == 0 ? o.Consigne : o.ConsigneSuite, false);
                };
                boutons.Add((btn, i));
            }
            SetBody(new Viewbox { Child = slotRow, MaxHeight = 220, Margin = new Thickness(0, 10, 0, 10) }, Rangee(boutons.Select(b => b.Btn).ToList()));
            if (!_lecon.EstDefi) ArmHint(boutons.First(b => b.Index == 0).Btn, () => o.Consigne);
            DireConsigne(o.Consigne);
        }

        // ------------------------------------------------------------------
        //  FIN : on enregistre, on fête, on propose la suite
        // ------------------------------------------------------------------
        private void Terminer()
        {
            Locked = true;
            SetConsigne(null, null);
            var noeud = _lecon.Noeud;
            var avant = Acquisition.EtatDe(noeud);
            var axesAvant = noeud.Def.Axes.Where(a => Acquisition.AxeValide(noeud, a.Id)).Select(a => a.Id).ToList();
            int reussis = _lecon.Pas.Count(p => p.Reussi);
            bool defiOk = false;

            if (_lecon.EstDefi)
            {
                defiOk = reussis >= Acquisition.DefiSeuil;
                if (defiOk) Acquisition.DefiReussi(noeud);
                else Acquisition.DefiRate(noeud, _lecon.Pas.Where(p => !p.Reussi).Select(p => p.Axe));
            }
            else
            {
                foreach (var p in _lecon.Pas.Where(p => p.Genre == Genre.Noeud)) Acquisition.Enregistrer(p.Noeud, p.Axe, p.Reussi);
                foreach (var g in _lecon.Pas.Where(p => p.Genre == Genre.Revision).GroupBy(p => p.Noeud.Key))
                    Acquisition.Revision(g.First().Noeud, g.All(p => p.Reussi));
            }
            var apres = Acquisition.EtatDe(noeud);
            var axesApres = noeud.Def.Axes.Where(a => Acquisition.AxeValide(noeud, a.Id)).Select(a => a.Id).ToList();
            int nouveauxAxes = axesApres.Except(axesAvant).Count();
            bool pretDefi = Acquisition.PretPourDefi(noeud);

            // L'écran de fin.
            var root = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            string titre = _lecon.EstDefi ? (defiOk ? "🏆 Défi réussi !" : "Presque !") : "Bravo !";
            root.Children.Add(new TextBlock { Text = titre, FontSize = 76, FontWeight = FontWeights.ExtraBold, Foreground = Ink, HorizontalAlignment = HorizontalAlignment.Center });
            var etoiles = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 10) };
            foreach (var p in _lecon.Pas) etoiles.Children.Add(Art.Visual("", p.Reussi ? "⭐" : "🌟", 64));
            root.Children.Add(etoiles);
            string ligne = reussis + " réussite" + (reussis > 1 ? "s" : "") + " sur " + _lecon.Pas.Count + "  ·  " + noeud.Titre;
            root.Children.Add(new TextBlock { Text = ligne, FontSize = 30, Foreground = Ink, Opacity = 0.75, HorizontalAlignment = HorizontalAlignment.Center });
            string etat = null, dit = null;
            if (_lecon.EstDefi && defiOk) { etat = "✅ " + noeud.Competence.Nom + " : acquis !"; dit = "Défi réussi ! " + noeud.Competence.Nom + ", c'est acquis !"; }
            else if (_lecon.EstDefi) { etat = "On réessaiera une autre fois."; dit = "Presque ! On réessaiera le défi une autre fois."; }
            else if (pretDefi) { etat = "⚡ Prêt pour le défi !"; dit = "Bravo ! Tu es prête pour le défi !"; }
            else if (nouveauxAxes > 0) { etat = "🎯 " + nouveauxAxes + (nouveauxAxes > 1 ? " nouveaux axes validés" : " nouvel axe validé"); dit = "Bravo ! Tu avances bien !"; }
            else if (apres != avant) { etat = "En route : " + Libelle(apres); dit = "Bravo, tu as bien travaillé !"; }
            else dit = reussis == _lecon.Pas.Count ? "Tout juste ! Bravo !" : "Bravo, tu as bien travaillé !";
            if (etat != null) root.Children.Add(new TextBlock { Text = etat, FontSize = 36, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0x9E, 0x4F)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 16, 0, 0) });

            var btns = Row();
            btns.Margin = new Thickness(0, 30, 0, 0);
            var encore = GrosBouton("▶", "Encore", Color.FromRgb(0x4C, 0xB8, 0x5E));
            encore.Click += (s, e) => { if (!Closed) _onDone?.Invoke("encore"); };
            var chemin = GrosBouton("🧭", "Le chemin", Color.FromRgb(0x3B, 0x9B, 0xFF));
            chemin.Click += (s, e) => { if (!Closed) _onDone?.Invoke("chemin"); };
            btns.Children.Add(encore); btns.Children.Add(chemin);
            root.Children.Add(btns);
            SetBody(root);
            Question.Text = _lecon.EstDefi ? "⚡ Défi terminé" : "🧭 Leçon terminée";
            Emoji.Fix(Question);

            Speak(dit);
            if (!_lecon.EstDefi || defiOk) Celebrate();
            Locked = false;
        }

        private Button GrosBouton(string icone, string label, Color c)
        {
            var st = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            st.Children.Add(Art.Visual("", icone, 90));
            st.Children.Add(new TextBlock { Text = label, FontSize = 30, FontWeight = FontWeights.Bold, Foreground = Ink, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) });
            var b = AnswerButton(st, 240);
            b.Margin = new Thickness(16);
            b.Background = new SolidColorBrush(Color.FromArgb(0x33, c.R, c.G, c.B));
            return b;
        }

        public static string Libelle(Etat e)
        {
            switch (e)
            {
                case Etat.NonCommence: return "pas commencé";
                case Etat.EnCours: return "en cours";
                case Etat.EnValidation: return "en validation";
                case Etat.Acquis: return "acquis";
                case Etat.AConsolider: return "à consolider";
                default: return "";
            }
        }
    }
}
