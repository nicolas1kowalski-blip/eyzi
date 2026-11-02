using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 👂 L'ÉCOUTE (Azure Speech, même ressource que la voix naturelle) :
    ///  • mode « bouton » : l'accompagnant ou l'enfant appuie sur 👂, la tablette
    ///    écoute une phrase et la transcrit ;
    ///  • mode « mot-clé » : la tablette veille EN LOCAL sur « Dis Eyzi » (fichier
    ///    Voix\eyzi.table, fabriqué dans Speech Studio) ; rien ne part de la
    ///    tablette avant le mot-clé ; la phrase qui suit est transcrite.
    /// Les événements arrivent sur des fils du SDK : à marshaler vers l'interface.
    /// </summary>
    public static class Listening
    {
        /// <summary>Une phrase entendue (sans le mot-clé).</summary>
        public static event Action<string> Heard;
        /// <summary>« off », « veille » (mot-clé), « ecoute » (phrase en cours), « erreur ».</summary>
        public static event Action<string> StatusChanged;
        /// <summary>Le texte PARTIEL pendant que la personne parle encore (pour préparer les réponses avant la fin).</summary>
        public static event Action<string> Partial;

        public static string Status { get; private set; } = "off";
        public static string LastError { get; private set; } = "";
        public static bool Enabled => _rec != null;

        private static SpeechRecognizer _rec;
        private static KeywordRecognitionModel _kw;
        private static bool _keywordMode, _busy;
        private static string _key = "", _region = "";
        private static Settings _last;

        public static string KeywordFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Voix", "eyzi.table");
        public static bool KeywordAvailable => File.Exists(KeywordFile);

        private static void SetStatus(string s)
        {
            Status = s;
            try { StatusChanged?.Invoke(s); } catch { }
        }

        /// <summary>(Re)démarre selon les réglages : clé Azure, mode (off | bouton | motcle).</summary>
        public static void Start(Settings s)
        {
            Stop();
            if (s == null) return;
            _last = s;
            var mode = (s.ListenMode ?? "off").ToLowerInvariant();
            if (mode == "off" || string.IsNullOrWhiteSpace(s.AzureKey)) { SetStatus("off"); return; }
            _key = s.AzureKey.Trim(); _region = string.IsNullOrWhiteSpace(s.AzureRegion) ? "francecentral" : s.AzureRegion.Trim();
            try
            {
                var config = SpeechConfig.FromSubscription(_key, _region);
                config.SpeechRecognitionLanguage = "fr-FR";
                // La phrase est close après 450 ms de silence au lieu d'une seconde : la grille arrive plus tôt.
                config.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "600");
                config.SetProperty(PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "600");
                _rec = new SpeechRecognizer(config, AudioConfig.FromDefaultMicrophoneInput());
                _rec.Recognized += OnRecognized;
                _rec.Recognizing += (o, e) => { try { if (e.Result.Reason == ResultReason.RecognizingSpeech) Partial?.Invoke(Clean(e.Result.Text)); } catch { } };
                _rec.Canceled += (o, e) =>
                {
                    if (e.Reason == CancellationReason.Error)
                    {
                        LastError = Humain(e.ErrorDetails, e.ErrorCode.ToString());
                        Log.Write("ecoute", "Annulé : " + e.ErrorCode + " " + e.ErrorDetails);
                        SetStatus("erreur");
                    }
                };
                _keywordMode = mode == "motcle" && KeywordAvailable;
                if (_keywordMode)
                {
                    _kw = KeywordRecognitionModel.FromFile(KeywordFile);
                    _rec.StartKeywordRecognitionAsync(_kw).ContinueWith(t =>
                    {
                        if (t.IsFaulted) { LastError = t.Exception?.GetBaseException().Message; Log.Write("ecoute", "Mot-clé : " + LastError); SetStatus("erreur"); }
                        else SetStatus("veille");
                    });
                }
                else SetStatus("pret");
                Log.Write("ecoute", "Écoute démarrée : mode " + mode + (_keywordMode ? " (mot-clé)" : ""));
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Log.Write("ecoute", "Démarrage : " + ex);
                Stop();
                SetStatus("erreur");
            }
        }

        public static void Stop()
        {
            var r = _rec; _rec = null;
            if (r != null)
            {
                try { if (_keywordMode) r.StopKeywordRecognitionAsync().Wait(2000); } catch { }
                try { r.Dispose(); } catch { }
            }
            try { _kw?.Dispose(); } catch { }
            _kw = null; _keywordMode = false; _busy = false;
            SetStatus("off");
        }

        private static void OnRecognized(object sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedKeyword)
            {
                // Le mot-clé est entendu : on n'enchaîne pas dans le même souffle (la pause après « Dis Eyzi »
                // fermait la phrase, vide) ; on lance une vraie écoute de phrase, puis on revient à la veille.
                Log.Write("ecoute", "Mot-clé entendu");
                SetStatus("ecoute");
                _ = Task.Run(() => ListenOnce());
                return;
            }
            if (e.Result.Reason != ResultReason.RecognizedSpeech) return;
            var text = Clean(e.Result.Text);
            if (_keywordMode) SetStatus("veille");
            if (text.Length < 2) return;
            Log.Write("ecoute", "Entendu : " + text);
            try { Heard?.Invoke(text); } catch { }
        }

        /// <summary>Un message court et compréhensible à la place du détail technique du SDK.</summary>
        public static string Humain(string details, string code)
        {
            var d = details ?? "";
            if (d.Contains("401") || d.IndexOf("Authentication", StringComparison.OrdinalIgnoreCase) >= 0 || code == "AuthenticationFailure") return "clé Azure ou région incorrecte (401). Vérifiez la clé (32 caractères) et la région (francecentral) dans la page Voix.";
            if (d.Contains("403") || code == "Forbidden") return "la clé Azure n'a pas le droit d'écouter (403) : quota du niveau gratuit dépassé, ou ressource désactivée.";
            if (d.Contains("1006") || d.IndexOf("Connection", StringComparison.OrdinalIgnoreCase) >= 0 || code == "ConnectionFailure") return "pas de connexion à Azure : vérifiez l'Internet de la tablette.";
            if (d.Contains("INVALID_STATE_TRANSITION") || d.Contains("0x13")) return "l'écoute a dû redémarrer, réessayez.";
            if (d.IndexOf("microphone", StringComparison.OrdinalIgnoreCase) >= 0 || d.Contains("0x15") || d.Contains("MIC")) return "pas de micro : vérifiez le micro dans les réglages Windows (confidentialité → microphone).";
            int nl = d.IndexOf('\n'); if (nl > 0) d = d.Substring(0, nl);
            if (d.Length > 140) d = d.Substring(0, 140) + "…";
            return d.Length > 0 ? d : code;
        }

        /// <summary>Enlève le mot-clé (et ses variantes entendues) en tête de phrase.</summary>
        public static string Clean(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var t = Regex.Replace(text.Trim(), @"^\s*(dis|dit|dix|hé|hey|eh|ok|okay)?[\s,]*(eyzi|ezi|ezy|easy|izzy|aisie|eisie|lézi)[\s,!.?]*", "", RegexOptions.IgnoreCase);
            return t.Trim();
        }

        /// <summary>Le bouton 👂, ou le mot-clé : écoute UNE phrase maintenant, puis revient TOUJOURS à la veille (ou recrée le moteur).</summary>
        public static async Task ListenOnce()
        {
            if (_busy) return;
            // Après une erreur (mauvaise clé, réseau), le moteur reste bloqué dans son état : on le recrée avant de réessayer.
            if (_rec == null || Status == "erreur") { if (_last == null) return; Start(_last); if (_rec == null) return; }
            _busy = true;
            bool erreur = false;
            try
            {
                if (_keywordMode) { try { await _rec.StopKeywordRecognitionAsync(); } catch (Exception ex) { Log.Write("ecoute", "Arrêt du mot-clé : " + ex.Message); } }
                SetStatus("ecoute");
                var result = await _rec.RecognizeOnceAsync();
                if (result.Reason == ResultReason.Canceled)
                {
                    var d0 = CancellationDetails.FromResult(result);
                    if (d0.Reason == CancellationReason.Error && (d0.ErrorDetails ?? "").Contains("INVALID_STATE_TRANSITION"))
                    {
                        Log.Write("ecoute", "État invalide : on recrée le moteur et on réessaie");
                        Start(_last); if (_rec == null) return;
                        if (_keywordMode) { try { await _rec.StopKeywordRecognitionAsync(); } catch { } }
                        SetStatus("ecoute");
                        result = await _rec.RecognizeOnceAsync();
                    }
                }
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    var text = Clean(result.Text);
                    if (text.Length >= 2) { Log.Write("ecoute", "Entendu : " + text); try { Heard?.Invoke(text); } catch { } }
                    else Log.Write("ecoute", "Phrase vide (« " + result.Text + " »)");
                }
                else if (result.Reason == ResultReason.NoMatch) Log.Write("ecoute", "Rien entendu");
                else if (result.Reason == ResultReason.Canceled)
                {
                    var d = CancellationDetails.FromResult(result);
                    if (d.Reason == CancellationReason.Error) { LastError = Humain(d.ErrorDetails, d.ErrorCode.ToString()); Log.Write("ecoute", "Écoute : " + d.ErrorCode + " " + d.ErrorDetails); erreur = true; }
                }
            }
            catch (Exception ex) { LastError = Humain(ex.Message, ""); Log.Write("ecoute", "Écoute : " + ex.Message); erreur = true; }
            finally
            {
                _busy = false;
                await RetourVeille(erreur);
            }
        }

        /// <summary>Après une phrase : la veille sur le mot-clé reprend ; après une erreur, le moteur est recréé (ce qui relance la veille).</summary>
        private static async Task RetourVeille(bool erreur)
        {
            try
            {
                if (erreur || _rec == null) { SetStatus("erreur"); if (_last != null) { Log.Write("ecoute", "Moteur recréé après erreur"); Start(_last); } return; }
                if (_keywordMode && _kw != null)
                {
                    await _rec.StartKeywordRecognitionAsync(_kw);
                    SetStatus("veille");
                }
                else SetStatus("pret");
            }
            catch (Exception ex)
            {
                Log.Write("ecoute", "Reprise de la veille impossible (" + ex.Message + ") : moteur recréé");
                if (_last != null) Start(_last);
            }
        }

        // Le chien de garde : en mode mot-clé, si la veille n'est plus active (après une erreur, une coupure réseau…),
        // le moteur est recréé. Toutes les 20 secondes, quand rien n'est en cours ; après trois échecs de suite
        // (pas de clé, pas de micro, pas de réseau), on n'insiste plus que toutes les cinq minutes.
        private static int _echecsGarde;
        private static DateTime _prochainEssai = DateTime.MinValue;
        private static readonly System.Threading.Timer _garde = new System.Threading.Timer(_ =>
        {
            try
            {
                if (_last == null || _busy) return;
                var mode = (_last.ListenMode ?? "off").ToLowerInvariant();
                if (mode == "off" || string.IsNullOrWhiteSpace(_last.AzureKey)) return;
                bool attendu = mode == "motcle" && KeywordAvailable ? (Status == "veille" || Status == "ecoute") : (Status == "pret" || Status == "ecoute");
                if (attendu) { _echecsGarde = 0; return; }
                if (DateTime.Now < _prochainEssai) return;
                _echecsGarde++;
                if (_echecsGarde >= 3) { _prochainEssai = DateTime.Now.AddMinutes(5); Log.Write("ecoute", "Veille perdue (état « " + Status + " ») : " + _echecsGarde + " échecs, prochain essai dans 5 min"); }
                else Log.Write("ecoute", "Veille perdue (état « " + Status + " ») : moteur recréé");
                Start(_last);
            }
            catch (Exception ex) { Log.Write("ecoute", "Chien de garde : " + ex.Message); }
        }, null, 20000, 20000);
    }
}
