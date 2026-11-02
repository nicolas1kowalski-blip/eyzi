using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 📷 UNE PHOTO, pour « Dis Eyzi, regarde » : la caméra ARRIÈRE voit ce
    /// que l'adulte montre (deux objets, l'assiette, une page de livre), la
    /// caméra AVANT voit l'enfant. Une seule image, réduite (1024 px au plus,
    /// JPEG), qui part vers eyzi.fr avec la question et n'est JAMAIS enregistrée
    /// sur la tablette ni ailleurs. Si la caméra manque ou est interdite par
    /// Windows, on le dit et la question part sans image.
    /// </summary>
    public static class Photo
    {
        public sealed class Resultat { public byte[] Jpeg; public string Erreur; public string Camera; }

        // Les opérations Windows sont asynchrones ; on les attend par rappels (pas de System.Runtime.WindowsRuntime ici).
        private static Task<TR> Wait<TR>(IAsyncOperation<TR> op)
        {
            var tcs = new TaskCompletionSource<TR>();
            op.Completed = (o, st) =>
            {
                try { if (st == AsyncStatus.Completed) tcs.TrySetResult(o.GetResults()); else tcs.TrySetException(o.ErrorCode ?? new Exception("opération annulée")); }
                catch (Exception ex) { tcs.TrySetException(ex); }
            };
            return tcs.Task;
        }

        private static Task Wait(IAsyncAction op)
        {
            var tcs = new TaskCompletionSource<bool>();
            op.Completed = (o, st) =>
            {
                try { if (st == AsyncStatus.Completed) tcs.TrySetResult(true); else tcs.TrySetException(o.ErrorCode ?? new Exception("opération annulée")); }
                catch (Exception ex) { tcs.TrySetException(ex); }
            };
            return tcs.Task;
        }

        /// <summary>Prend une photo (caméra avant si <paramref name="avant"/>, sinon arrière), réduite à <paramref name="maxPx"/> de côté.</summary>
        public static async Task<Resultat> Prendre(bool avant, int maxPx = 1024)
        {
            var res = new Resultat();
            MediaCapture mc = null;
            try
            {
                var travail = PrendreInterne(avant, maxPx, res, m => mc = m);
                var fini = await Task.WhenAny(travail, Task.Delay(9000));
                if (fini != travail) { res.Erreur = "La caméra n'a pas répondu."; res.Jpeg = null; }
                else await travail;
            }
            catch (UnauthorizedAccessException)
            {
                res.Erreur = "Windows interdit la caméra aux applications : Réglages Windows → Confidentialité → Caméra → autoriser les applications de bureau.";
            }
            catch (Exception ex) { res.Erreur = "Caméra : " + ex.Message; }
            finally { try { mc?.Dispose(); } catch { } }
            if (res.Erreur != null) Log.Write("photo", res.Erreur);
            return res;
        }

        private static async Task PrendreInterne(bool avant, int maxPx, Resultat res, Action<MediaCapture> garde)
        {
            var toutes = await Wait(DeviceInformation.FindAllAsync(DeviceClass.VideoCapture));
            // Jamais la caméra du capteur oculaire (Tobii IS5…) : on ne touche qu'aux vraies caméras de la tablette.
            var devices = toutes.Where(d => { var n = (d.Name ?? "").ToLowerInvariant(); return !(n.Contains("tobii") || n.Contains("is5") || n.Contains("is4") || n.Contains("eye track") || n.Contains("eyetrack")); }).ToList();
            if (devices.Count == 0) { res.Erreur = "Aucune caméra sur cette tablette."; return; }
            var voulu = avant ? Panel.Front : Panel.Back;
            var cam = devices.FirstOrDefault(d => d.EnclosureLocation != null && d.EnclosureLocation.Panel == voulu)
                      ?? devices.FirstOrDefault(d => d.Name.IndexOf(avant ? "front" : "back", StringComparison.OrdinalIgnoreCase) >= 0 || d.Name.IndexOf(avant ? "avant" : "arrière", StringComparison.OrdinalIgnoreCase) >= 0)
                      ?? (avant ? devices[0] : devices[devices.Count - 1]);
            res.Camera = cam.Name;
            var mc = new MediaCapture();
            garde(mc);
            await Wait(mc.InitializeAsync(new MediaCaptureInitializationSettings
            {
                VideoDeviceId = cam.Id,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                PhotoCaptureSource = PhotoCaptureSource.Auto,
            }));
            using (var brut = new InMemoryRandomAccessStream())
            {
                await Wait(mc.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), brut));
                brut.Seek(0);
                var decoder = await Wait(BitmapDecoder.CreateAsync(brut));
                uint w = decoder.PixelWidth, h = decoder.PixelHeight;
                double k = Math.Min(1.0, maxPx / (double)Math.Max(w, h));
                using (var sortie = new InMemoryRandomAccessStream())
                {
                    var encoder = await Wait(BitmapEncoder.CreateForTranscodingAsync(sortie, decoder));
                    if (k < 1.0)
                    {
                        encoder.BitmapTransform.ScaledWidth = (uint)Math.Max(1, Math.Round(w * k));
                        encoder.BitmapTransform.ScaledHeight = (uint)Math.Max(1, Math.Round(h * k));
                        encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                    }
                    await Wait(encoder.FlushAsync());
                    sortie.Seek(0);
                    var bytes = new byte[sortie.Size];
                    var reader = new DataReader(sortie.GetInputStreamAt(0));
                    await Wait(reader.LoadAsync((uint)bytes.Length));
                    reader.ReadBytes(bytes);
                    res.Jpeg = bytes;
                }
            }
            Log.Write("photo", (avant ? "Avant" : "Arrière") + " : " + cam.Name + ", " + (res.Jpeg?.Length ?? 0) / 1024 + " Ko");
        }
    }
}
