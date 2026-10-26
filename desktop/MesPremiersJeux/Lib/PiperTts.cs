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

        /// <summary>Les voix livrées : nom affiché → (fichier du modèle, numéro de locuteur dans ce modèle).</summary>
        public static readonly (string Id, string Label, string File, int? Speaker)[] Voices =
        {
            ("siwis",   "Siwis — claire et posée (voix des sons et des syllabes)", "fr_FR-siwis-medium.onnx", null),
            ("jessica", "Jessica — plus ronde, plus chaleureuse",                  "fr_FR-upmc-medium.onnx",  0),
        };

        private static string _voice = "siwis";
        /// <summary>La voix choisie (siwis par défaut). Changer de voix redémarre le moteur.</summary>
        public static string Voice
        {
            get => _voice;
            set { var v = Voices.Any(x => x.Id == value) ? value : "siwis"; if (v != _voice) { _voice = v; Shutdown(); } }
        }

        private static (string File, int? Speaker) Current()
        {
            var v = Voices.First(x => x.Id == _voice);
            var path = Path.Combine(Dir, v.File);
            if (File.Exists(path)) return (path, v.Speaker);
            // Voix demandée absente : la première voix disponible.
            foreach (var o in Voices) { var p = Path.Combine(Dir, o.File); if (File.Exists(p)) return (p, o.Speaker); }
            return (path, v.Speaker);
        }
        private static string Model => Current().File;

        /// <summary>Les fichiers du moteur et d'au moins une voix sont là.</summary>
        public static bool Available
        {
            get { try { return File.Exists(Exe) && File.Exists(Model) && File.Exists(Model + ".json"); } catch { return false; } }
        }

        /// <summary>Cette voix précise est livrée.</summary>
        public static bool HasVoice(string id)
        {
            try { var v = Voices.FirstOrDefault(x => x.Id == id); return v.File != null && File.Exists(Path.Combine(Dir, v.File)); } catch { return false; }
        }

        /// <summary>Nom de la voix en service (pour l'écran d'administration).</summary>
        public static string VoiceLabel => Available ? Voices.First(x => Path.Combine(Dir, x.File) == Model).Id : "";

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
                hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFileName(Model) + "|" + (Current().Speaker?.ToString() ?? "") + "|" + text))).Replace("-", "");
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
                    var spk = Current().Speaker;
                    var json = "{\"text\":\"" + Esc(text) + "\"" + (spk.HasValue ? ",\"speaker_id\":" + spk.Value : "") + "}";
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

        // ------------------------------------------------------------------
        //  Page racontée : phrase par phrase, avec de vraies pauses
        // ------------------------------------------------------------------
        // La voix synthétique enchaîne les phrases d'un bloc, ce qui sonne
        // monotone. Ici chaque phrase est synthétisée à part (et mise en cache),
        // puis les phrases sont recollées avec une pause qui dépend de la
        // ponctuation : courte après un point, plus longue après une question,
        // encore plus après des points de suspension, un souffle avant qu'un
        // personnage parle (« …). Le suivi des mots gagne aussi en précision,
        // puisque l'on connaît le début et la durée de chaque phrase.

        public sealed class PageAudio
        {
            public string File;
            public double Duration;
            public readonly List<(double Start, double Dur, int Words)> Segments = new List<(double, double, int)>();
        }

        /// <summary>Découpe un texte en phrases : après . ! ? … (et le » qui suit), devant une majuscule, un chiffre, « ou un tiret.</summary>
        public static List<string> SplitSentences(string text)
        {
            var list = new List<string>();
            var sb = new StringBuilder();
            text = (text ?? "").Trim();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                sb.Append(c);
                if (c != '.' && c != '!' && c != '?' && c != '…') continue;
                int j = i + 1;
                while (j < text.Length && (text[j] == '.' || text[j] == '!' || text[j] == '?' || text[j] == '…')) { sb.Append(text[j]); j++; }
                int k = j;
                while (k < text.Length && text[k] == ' ') k++;
                if (k < text.Length && (text[k] == '»' || text[k] == '"' || text[k] == '”')) { sb.Append(' ').Append(text[k]); j = k + 1; }
                int m = j;
                while (m < text.Length && char.IsWhiteSpace(text[m])) m++;
                bool cut = m >= text.Length || char.IsUpper(text[m]) || char.IsDigit(text[m]) || text[m] == '«' || text[m] == '"' || text[m] == '-' || text[m] == '—';
                if (cut)
                {
                    var s = sb.ToString().Trim();
                    if (s.Length > 0) list.Add(s);
                    sb.Clear();
                    i = m - 1;
                }
                else i = j - 1;
            }
            var rest = sb.ToString().Trim();
            if (rest.Length > 0) list.Add(rest);
            return list;
        }

        private static int CountWords(string s) => s.Split(' ').Count(t => t.Any(char.IsLetterOrDigit));

        /// <summary>La pause (en secondes) après une phrase, d'après sa ponctuation et ce qui suit.</summary>
        private static double PauseAfter(string sentence, string next)
        {
            string s = sentence.TrimEnd('»', '"', '”', ' ');
            char last = s.Length > 0 ? s[s.Length - 1] : '.';
            double p = last == '?' ? 0.65 : last == '!' ? 0.55 : last == '…' ? 0.85 : 0.5;
            if (sentence.EndsWith("»")) p += 0.1;                 // fin d'une réplique
            if (next != null && next.StartsWith("«")) p += 0.15;  // un personnage va parler
            return p;
        }

        public static Task<PageAudio> SynthesizePageAsync(string text) => Task.Run(() => SynthesizePage(text));

        private static PageAudio SynthesizePage(string text)
        {
            try
            {
                var sentences = SplitSentences(text);
                if (sentences.Count == 0 || !Available) return null;
                var outPath = CachePath("§page§" + text);
                var segPath = outPath + ".seg";
                // Déjà assemblée ? Le fichier des repères donne le début et la durée de chaque phrase.
                if (File.Exists(outPath) && File.Exists(segPath))
                {
                    try
                    {
                        var pa = new PageAudio { File = outPath };
                        foreach (var line in File.ReadAllLines(segPath))
                        {
                            var f = line.Split(';');
                            if (f.Length < 3) continue;
                            pa.Segments.Add((double.Parse(f[0], System.Globalization.CultureInfo.InvariantCulture),
                                             double.Parse(f[1], System.Globalization.CultureInfo.InvariantCulture), int.Parse(f[2])));
                        }
                        pa.Duration = WavDurationSeconds(outPath);
                        if (pa.Segments.Count == sentences.Count) return pa;
                    }
                    catch { }
                }

                int rate = 0, ch = 0, bits = 0;
                var all = new MemoryStream();
                var result = new PageAudio { File = outPath };
                double t = 0;
                for (int i = 0; i < sentences.Count; i++)
                {
                    var file = Synthesize(sentences[i]);
                    if (file == null) return null;
                    var data = ReadWav(file, out int r, out int c, out int b);
                    if (data == null) return null;
                    if (rate == 0) { rate = r; ch = c; bits = b; }
                    else if (r != rate || c != ch || b != bits) return null;
                    int frame = Math.Max(1, ch * bits / 8);
                    int len = TrimSilence(data, frame, rate, bits);
                    double dur = (double)len / (rate * frame);
                    result.Segments.Add((t, dur, CountWords(sentences[i])));
                    all.Write(data, 0, len);
                    double pause = PauseAfter(sentences[i], i + 1 < sentences.Count ? sentences[i + 1] : null);
                    int silence = (int)(pause * rate) * frame;
                    all.Write(new byte[silence], 0, silence);
                    t += dur + pause;
                }
                WriteWav(outPath, rate, ch, bits, all.ToArray());
                result.Duration = t;
                File.WriteAllLines(segPath, result.Segments.Select(s =>
                    s.Start.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ";" +
                    s.Dur.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ";" + s.Words));
                return result;
            }
            catch (Exception ex)
            {
                Log.Write("voix", "Piper (page) : " + ex.Message);
                return null;
            }
        }

        // Lit un WAV PCM (chunks fmt / data) ; null si le format n'est pas du PCM simple.
        private static byte[] ReadWav(string path, out int rate, out int channels, out int bits)
        {
            rate = channels = bits = 0;
            using (var fs = File.OpenRead(path))
            using (var br = new BinaryReader(fs))
            {
                if (fs.Length < 44) return null;
                fs.Seek(12, SeekOrigin.Begin);
                byte[] data = null;
                while (fs.Position + 8 <= fs.Length)
                {
                    var id = Encoding.ASCII.GetString(br.ReadBytes(4));
                    int size = br.ReadInt32();
                    long next = fs.Position + size + (size % 2);
                    if (id == "fmt ")
                    {
                        int format = br.ReadInt16();
                        channels = br.ReadInt16();
                        rate = br.ReadInt32();
                        fs.Seek(6, SeekOrigin.Current);
                        bits = br.ReadInt16();
                        if (format != 1 || bits != 16) return null;
                    }
                    else if (id == "data")
                    {
                        data = br.ReadBytes((int)Math.Min(size, fs.Length - fs.Position));
                    }
                    if (next > fs.Length) break;
                    fs.Seek(next, SeekOrigin.Begin);
                }
                return rate > 0 ? data : null;
            }
        }

        private static void WriteWav(string path, int rate, int channels, int bits, byte[] pcm)
        {
            using (var fs = File.Create(path))
            using (var bw = new BinaryWriter(fs))
            {
                int frame = channels * bits / 8;
                bw.Write(Encoding.ASCII.GetBytes("RIFF")); bw.Write(36 + pcm.Length); bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(Encoding.ASCII.GetBytes("fmt ")); bw.Write(16); bw.Write((short)1); bw.Write((short)channels);
                bw.Write(rate); bw.Write(rate * frame); bw.Write((short)frame); bw.Write((short)bits);
                bw.Write(Encoding.ASCII.GetBytes("data")); bw.Write(pcm.Length); bw.Write(pcm);
            }
        }

        // Longueur utile (en octets) une fois le silence de fin enlevé (au plus 1 s, seuil bas).
        private static int TrimSilence(byte[] pcm, int frame, int rate, int bits)
        {
            int len = pcm.Length - pcm.Length % frame;
            if (bits != 16) return len;
            int minLen = Math.Max(0, len - rate * frame);
            int pos = len;
            while (pos - frame >= minLen)
            {
                short s = (short)(pcm[pos - frame] | (pcm[pos - frame + 1] << 8));
                if (Math.Abs((int)s) > 250) break;
                pos -= frame;
            }
            return Math.Max(pos, Math.Min(len, frame * 10));
        }

        /// <summary>Lecture d'une page avec suivi des mots : phrase par phrase quand c'est possible (repères précis), sinon d'un bloc.</summary>
        public static async void SpeakPage(string text, Action<int> onWord, Action onDone, Action onFail)
        {
            try
            {
                var words = text.Split(' ').Where(t => t.Any(char.IsLetterOrDigit)).ToList();
                var times = new List<double>();
                string file;
                double duration;
                var page = await SynthesizePageAsync(text);
                if (page != null && page.Segments.Sum(s => s.Words) == words.Count)
                {
                    file = page.File;
                    duration = page.Duration;
                    int wi = 0;
                    foreach (var seg in page.Segments)
                    {
                        var ws = words.Skip(wi).Take(seg.Words).ToList();
                        wi += seg.Words;
                        double tw = Math.Max(1, ws.Sum(w => w.Length + 2.0)), cum = 0;
                        foreach (var w in ws) { times.Add(seg.Start + seg.Dur * (cum / tw)); cum += w.Length + 2.0; }
                    }
                }
                else
                {
                    file = await SynthesizeAsync(text);
                    if (file == null) { onFail?.Invoke(); return; }
                    duration = WavDurationSeconds(file);
                    double totalWeight = words.Sum(w => w.Length + 2.0), cum = 0;
                    foreach (var w in words) { times.Add(duration * (cum / totalWeight)); cum += w.Length + 2.0; }
                }

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
