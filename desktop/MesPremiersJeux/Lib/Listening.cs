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

        public static string Status { get; private set; } = "off";
        public static string LastError { get; private set; } = "";
        public static bool Enabled => _rec != null;

        private static SpeechRecognizer _rec;
        private static KeywordRecognitionModel _kw;
        private static bool _keywordMode, _busy;
        private static string _key = "", _region = "";

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
            var mode = (s.ListenMode ?? "off").ToLowerInvariant();
            if (mode == "off" || string.IsNullOrWhiteSpace(s.AzureKey)) { SetStatus("off"); return; }
            _key = s.AzureKey.Trim(); _region = string.IsNullOrWhiteSpace(s.AzureRegion) ? "francecentral" : s.AzureRegion.Trim();
            try
            {
                var config = SpeechConfig.FromSubscription(_key, _region);
                config.SpeechRecognitionLanguage = "fr-FR";
                _rec = new SpeechRecognizer(config, AudioConfig.FromDefaultMicrophoneInput());
                _rec.Recognized += OnRecognized;
                _rec.Canceled += (o, e) =>
                {
                    if (e.Reason == CancellationReason.Error)
                    {
                        LastError = e.ErrorDetails ?? e.ErrorCode.ToString();
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
            if (e.Result.Reason == ResultReason.RecognizedKeyword) { SetStatus("ecoute"); return; }
            if (e.Result.Reason != ResultReason.RecognizedSpeech) return;
            var text = Clean(e.Result.Text);
            if (_keywordMode) SetStatus("veille");
            if (text.Length < 2) return;
            Log.Write("ecoute", "Entendu : " + text);
            try { Heard?.Invoke(text); } catch { }
        }

        /// <summary>Enlève le mot-clé (et ses variantes entendues) en tête de phrase.</summary>
        public static string Clean(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var t = Regex.Replace(text.Trim(), @"^\s*(dis|dit|dix|hé|hey|eh|ok|okay)?[\s,]*(eyzi|ezi|ezy|easy|izzy|aisie|eisie|lézi)[\s,!.?]*", "", RegexOptions.IgnoreCase);
            return t.Trim();
        }

        /// <summary>Le bouton 👂 : écoute UNE phrase maintenant (dans tous les modes).</summary>
        public static async Task ListenOnce()
        {
            if (_rec == null || _busy) return;
            _busy = true;
            try
            {
                if (_keywordMode) { try { await _rec.StopKeywordRecognitionAsync(); } catch { } }
                SetStatus("ecoute");
                var result = await _rec.RecognizeOnceAsync();
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    var text = Clean(result.Text);
                    if (text.Length >= 2) { Log.Write("ecoute", "Entendu (bouton) : " + text); try { Heard?.Invoke(text); } catch { } }
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var d = CancellationDetails.FromResult(result);
                    if (d.Reason == CancellationReason.Error) { LastError = d.ErrorDetails; Log.Write("ecoute", "Bouton : " + d.ErrorCode + " " + d.ErrorDetails); SetStatus("erreur"); _busy = false; return; }
                }
                if (_keywordMode && _kw != null) { try { await _rec.StartKeywordRecognitionAsync(_kw); } catch { } SetStatus("veille"); }
                else SetStatus("pret");
            }
            catch (Exception ex) { LastError = ex.Message; Log.Write("ecoute", "Bouton : " + ex.Message); SetStatus("erreur"); }
            finally { _busy = false; }
        }
    }
}
