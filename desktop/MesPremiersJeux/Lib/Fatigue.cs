using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 😴 LA VEILLE FATIGUE, sans caméra ni IA : le regard lui-même le dit.
    /// Toutes les 5 s on relève l'agitation du regard (le tremblement mesuré par
    /// le moteur de fixation) et les erreurs du jour. Quand l'agitation dépasse
    /// nettement et durablement celle du début de la séance, ou que les erreurs
    /// s'enchaînent sans réussite, « Signal » part : la fenêtre principale ouvre
    /// l'écoute avec une question douce (« Tu es fatiguée ? »). Au plus une fois
    /// toutes les dix minutes, jamais dans les trois premières minutes de jeu.
    /// </summary>
    public static class Fatigue
    {
        public static event Action<string> Signal; // la raison, pour le journal
        public static bool Actif => _timer != null;

        private static DispatcherTimer _timer;
        private static Func<bool> _enActivite;
        private static readonly List<double> _base = new List<double>();   // agitation des 2 premières minutes de jeu
        private static double _agiteDepuis;                                  // secondes consécutives d'agitation forte
        private static double _joueDepuis;                                   // secondes de jeu dans la séance
        private static DateTime _dernierSignal = DateTime.MinValue;
        private static double _wrong0 = -1, _succ0, _tWrong;

        public static void Demarrer(Func<bool> enActivite)
        {
            _enActivite = enActivite;
            if (_timer != null) return;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _timer.Tick += (s, e) => Tic();
            _timer.Start();
        }

        public static void Arreter() { _timer?.Stop(); _timer = null; }

        private static void Tic()
        {
            try
            {
                if (_enActivite == null || !_enActivite()) { _agiteDepuis = 0; return; }
                _joueDepuis += 5;
                var dc = Gaze.DwellController.Instance;
                double jitter = dc?.JitterPx ?? 0;
                if (jitter <= 0) return;
                if (_joueDepuis <= 120) { _base.Add(jitter); return; }
                if (_base.Count < 6) return;
                double ref0 = _base.OrderBy(x => x).ElementAt(_base.Count / 2); // médiane du début
                bool agite = jitter > ref0 * 1.8 + 4;
                _agiteDepuis = agite ? _agiteDepuis + 5 : 0;

                // Les erreurs qui s'enchaînent : au moins 4 erreurs en 2 minutes avec au plus 1 réussite.
                var jour = GazeStats.Day(DateTime.Today);
                double wrong = GazeStats.Get(jour, "wrong"), succ = GazeStats.Get(jour, "succ");
                if (_wrong0 < 0 || _joueDepuis - _tWrong > 120) { _wrong0 = wrong; _succ0 = succ; _tWrong = _joueDepuis; }
                bool erreurs = wrong - _wrong0 >= 4 && succ - _succ0 <= 1;

                string raison = _agiteDepuis >= 90 ? "regard agité (" + jitter.ToString("0") + " px, repère " + ref0.ToString("0") + ")" : erreurs ? "erreurs en série" : null;
                if (raison == null) return;
                if ((DateTime.Now - _dernierSignal).TotalMinutes < 10) return;
                _dernierSignal = DateTime.Now;
                _agiteDepuis = 0; _wrong0 = wrong; _succ0 = succ; _tWrong = _joueDepuis;
                Log.Write("fatigue", "Signal : " + raison);
                Signal?.Invoke(raison);
            }
            catch (Exception ex) { Log.Write("fatigue", ex.Message); }
        }

        /// <summary>Nouvelle séance (l'application démarre, ou après une pause) : on reprend le repère.</summary>
        public static void Reprise() { _base.Clear(); _joueDepuis = 0; _agiteDepuis = 0; _wrong0 = -1; }
    }
}
