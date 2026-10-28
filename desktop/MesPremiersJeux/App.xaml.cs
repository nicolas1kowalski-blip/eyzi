using System;
using System.Threading;
using System.Windows;

namespace MesPremiersJeux
{
    public partial class App : Application
    {
        private Mutex _single;

        protected override void OnStartup(StartupEventArgs e)
        {
            // ⏳ Mode « fenêtre d'attente de mise à jour » : une copie de l'exécutable,
            // lancée par l'ancienne version juste avant de se fermer. Pas de verrou,
            // pas de fenêtre principale : juste la petite fenêtre, qui se ferme toute
            // seule quand la nouvelle version est à l'écran.
            var args = e.Args ?? new string[0];
            int wait = Array.IndexOf(args, "--attente-maj");
            if (wait >= 0)
            {
                StartupUri = null;
                int oldPid = wait + 1 < args.Length && int.TryParse(args[wait + 1], out var pid) ? pid : -1;
                var w = new Views.UpdateWaitWindow(oldPid);
                MainWindow = w;
                w.Show();
                base.OnStartup(e);
                return;
            }

            // INSTANCE UNIQUE : plusieurs exemplaires simultanés = plusieurs moteurs
            // de regard qui injectent chacun leurs clics (chaos observé au journal).
            // Si l'application tourne déjà, ce nouvel exemplaire se ferme sans bruit.
            _single = new Mutex(true, @"Local\MesPremiersJeux-instance-unique", out bool isNew);
            if (!isNew)
            {
                Lib.Log.Write("app", "Instance déjà en cours : ce double se ferme.");
                Shutdown();
                return;
            }

            Lib.Log.Write("app", $"PID {System.Diagnostics.Process.GetCurrentProcess().Id} — instance unique acquise");

            // ⏳ Quelque chose à l'écran tout de suite : la fenêtre principale met
            // plusieurs secondes à se construire (activités, regard, voix).
            bool afterUpdate = e.Args != null && System.Linq.Enumerable.Contains(e.Args, "--apres-maj");
            Views.Splash.Show(afterUpdate);
            if (afterUpdate) Lib.Log.Write("maj", "Redémarrage après mise à jour");

            // 🛟 JAMAIS DE FERMETURE MUETTE : toute erreur imprévue est écrite dans
            // le journal (avec l'endroit exact dans le code) et dans un fichier
            // plantage-<tablette>.txt du dossier Contenu. Une erreur sur le fil
            // d'affichage est AVALÉE : l'application reste ouverte pour l'enfant.
            DispatcherUnhandledException += (s, ex) =>
            {
                Crash("fil d'affichage", ex.Exception);
                ex.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
                Crash("fil de fond" + (ex.IsTerminating ? " — FATAL, l'application se ferme" : ""), ex.ExceptionObject as Exception);
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                Crash("tâche", ex.Exception);
                ex.SetObserved();
            };

            base.OnStartup(e);
        }

        private static int _crashes;

        private static void Crash(string where, Exception ex)
        {
            try
            {
                _crashes++;
                var text = "ERREUR IMPRÉVUE (" + where + ") n°" + _crashes + " : " + (ex?.ToString() ?? "(inconnue)");
                Lib.Log.Write("plantage", text);
                var dir = Lib.UserContent.RootDir;
                var path = System.IO.Path.Combine(dir, "plantage-" + Environment.MachineName + ".txt");
                System.IO.File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + text + "\r\n\r\n");
            }
            catch { }
            // Trop d'erreurs d'affilée : quelque chose est vraiment cassé, on le dit.
            if (_crashes == 1 || _crashes % 10 == 0)
            {
                try { Lib.Speech.Say("Oups, petit souci, mais on continue !"); } catch { }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { Lib.Listening.Stop(); } catch { }
            try { _single?.ReleaseMutex(); _single?.Dispose(); } catch { }
            // Mise à jour en attente : l'installeur part maintenant que tout est
            // fermé et que le verrou « instance unique » est relâché (sinon la
            // nouvelle version, relancée par l'installeur, se croyait en double).
            try { Lib.Updater.RunPendingSetup(); } catch { }
            base.OnExit(e);
        }
    }
}
