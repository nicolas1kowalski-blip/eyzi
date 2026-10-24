using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// VOIX NATURELLE HORS LIGNE : le moteur Piper (libre, MIT) et la voix
    /// française « siwis » (CC BY 4.0), livrés dans Voix\piper\ par
    /// l'installeur. Un seul processus piper.exe reste ouvert (le modèle est
    /// chargé une fois) ; chaque phrase lui est envoyée en JSON, le WAV produit
    /// est mis en cache sur disque (comme pour Azure) puis joué. Sans ces
    /// fichiers (version compilée à la main), la voix Windows prend le relais.
    /// </summary>
    public static class PiperTts
    {
        public static string Dir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Voix", "piper");
        private static string Exe => Path.Combine(Dir, "piper.exe");
        private static string Model => Directory.Exists(Dir)
            ? Directory.GetFiles(Dir, "*.onnx").OrderBy(f => f).FirstOrDefault() ?? Path.Combine(Dir, "fr_FR-siwis-medium.onnx")
            : Path.Combine(Dir, "fr_FR-siwis-medium.onnx");

        /// <summary>Les fichiers du moteur et de la voix sont là.</summary>
        public static bool Available
        {
            get { try { return File.Exists(Exe) && File.Exists(Model) && File.Exists(Model + ".json"); } catch { return false; } }
        }

        /// <summary>Nom de la voix livrée (pour l'écran d'administration).</summary>
        public static string VoiceLabel => Available ? Path.GetFileNameWithoutExtension(Model) : "";

        private static string CacheDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MesPremiersJeux", "voix-piper");
        private static string WorkDir => Path.Combine(CacheDir, "tmp");

        private static readonly object Gate = new object();
        private static Process _proc;
        private static StreamWriter _in;
        private static StreamReader _out;
        private static string _lastError = "";
        private static SoundPlayer _player;
        private static DispatcherTimer _wordTimer;
        private static Stopwatch _playClock;

        public static string LastError => _lastError;

        // ------------------------------------------------------------------
        //  Processus
        // ------------------------------------------------------------------
        private static bool EnsureProcess()
        {
            lock (Gate)
            {
                if (_proc != null && !_proc.HasExited) return true;
                try
                {
                    Directory.CreateDirectory(WorkDir);
                    var psi = new ProcessStartInfo
                    {
                        FileName = Exe,
                        Arguments = "--model \"" + Model + "\" --config \"" + Model + ".json\" --output_dir \"" + WorkDir + "\"" +
                                    " --json-input --sentence_silence 0.25 --length_scale 1.05 --quiet",
                        WorkingDirectory = Dir,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8,
                    };
                    _proc = Process.Start(psi);
                    _in = new StreamWriter(_proc.StandardInput.BaseStream, new UTF8Encoding(false)) { AutoFlush = true };
                    _out = _proc.StandardOutput;
                    // stderr lu en continu (sinon le processus se bloque quand le tampon est plein)
                    var err = _proc.StandardError;
                    Task.Run(() => { try { string l; while ((l = err.ReadLine()) != null) { if (l.Length > 0) _lastError = l; } } catch { } });
                    Log.Write("voix", "Piper démarré : " + Path.GetFileName(Model));
                    return true;
                }
                catch (Exception ex)
                {
                    _lastError = ex.Message;
                    Log.Write("voix", "Piper impossible à démarrer : " + ex.Message);
                    _proc = null;
                    return false;
                }
            }
        }

        public static void Shutdown()
        {
            lock (Gate)
            {
                try { _in?.Close(); } catch { }
                try { if (_proc != null && !_proc.HasExited && !_proc.WaitForExit(800)) _proc.Kill(); } catch { }
                _proc = null; _in = null; _out = null;
            }
        }

        // ------------------------------------------------------------------
        //  Synthèse (cache disque, sinon le processus)
        // ------------------------------------------------------------------
        private static string CachePath(string text)
        {
            string hash;
            using (var sha = SHA1.Create())
                hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFileName(Model) + "|" + text))).Replace("-", "");
            return Path.Combine(CacheDir, hash + ".wav");
        }

        /// <summary>Le WAV de la phrase (depuis le cache, ou synthétisé). Null si impossible.</summary>
        public static Task<string> SynthesizeAsync(string text) => Task.Run(() => Synthesize(text));

        private static string Synthesize(string text)
        {
            try
            {
                Directory.CreateDirectory(CacheDir);
                var path = CachePath(text);
                if (File.Exists(path) && new FileInfo(path).Length > 100) return path;
                if (!Available || !EnsureProcess()) return null;
                lock (Gate)
                {
                    if (_proc == null || _proc.HasExited) return null;
                    var json = "{\"text\":\"" + Esc(text) + "\"}";
                    _in.WriteLine(json);
                    // piper écrit le WAV dans output_dir et affiche son chemin.
                    string line = null;
                    var reader = Task.Run(() => _out.ReadLine());
                    if (!reader.Wait(TimeSpan.FromSeconds(25))) { Log.Write("voix", "Piper : pas de réponse (25 s)"); Shutdown(); return null; }
                    line = reader.Result;
                    if (string.IsNullOrWhiteSpace(line)) { Shutdown(); return null; }
                    line = line.Trim();
                    if (!File.Exists(line)) return null;
                    try { if (File.Exists(path)) File.Delete(path); File.Move(line, path); } catch { path = line; }
                    return path;
                }
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                Log.Write("voix", "Piper : " + ex.Message);
                return null;
            }
        }

        private static string Esc(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");

        // ------------------------------------------------------------------
        //  Lecture
        // ------------------------------------------------------------------
        public static async void Speak(string text, Action onFail)
        {
            try
            {
                var file = await SynthesizeAsync(text);
                if (file == null) { onFail?.Invoke(); return; }
                StopPlayback();
                Speech.MarkBusyExact(WavDurationSeconds(file) + 0.2);
                _player = new SoundPlayer(file);
                _player.Play();
            }
            catch { onFail?.Invoke(); }
        }

        /// <summary>Lecture d'une page avec suivi des mots (temps estimés, pondérés par la longueur des mots).</summary>
        public static async void SpeakPage(string text, Action<int> onWord, Action onDone, Action onFail)
        {
            try
            {
                var file = await SynthesizeAsync(text);
                if (file == null) { onFail?.Invoke(); return; }
                double duration = WavDurationSeconds(file);
                var words = text.Split(' ').Where(t => t.Any(char.IsLetterOrDigit)).ToList();
                var times = new List<double>();
                double totalWeight = words.Sum(w => w.Length + 2.0), cum = 0;
                foreach (var w in words) { times.Add(duration * (cum / totalWeight)); cum += w.Length + 2.0; }

                StopPlayback();
                Speech.MarkBusyExact(duration + 0.2);
                _player = new SoundPlayer(file);
                _player.Play();
                _playClock = Stopwatch.StartNew();
                int next = 0;
                _wordTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
                _wordTimer.Tick += (s, e) =>
                {
                    double t = _playClock.Elapsed.TotalSeconds;
                    while (next < times.Count && t >= times[next]) onWord?.Invoke(next++);
                    if (t >= duration + 0.15) { _wordTimer.Stop(); onDone?.Invoke(); }
                };
                _wordTimer.Start();
            }
            catch { onFail?.Invoke(); }
        }

        public static void StopPlayback()
        {
            try { _wordTimer?.Stop(); } catch { }
            try { _player?.Stop(); } catch { }
        }

        /// <summary>Prépare en arrière-plan des phrases qui reviendront (accueil, bravos) : le cache les rend instantanées.</summary>
        public static void Prewarm(IEnumerable<string> phrases)
        {
            if (!Available) return;
            Task.Run(() => { foreach (var p in phrases) { try { Synthesize(p); } catch { } } });
        }

        // Durée d'un WAV d'après son en-tête (fréquence, canaux, bits).
        private static double WavDurationSeconds(string path)
        {
            try
            {
                using (var fs = File.OpenRead(path))
                using (var br = new BinaryReader(fs))
                {
                    fs.Seek(22, SeekOrigin.Begin);
                    int channels = br.ReadInt16();
                    int rate = br.ReadInt32();
                    fs.Seek(34, SeekOrigin.Begin);
                    int bits = br.ReadInt16();
                    double bytesPerSec = Math.Max(1, rate * channels * (bits / 8.0));
                    return Math.Max(0.1, (fs.Length - 44) / bytesPerSec);
                }
            }
            catch { return 1.0; }
        }
    }
}
