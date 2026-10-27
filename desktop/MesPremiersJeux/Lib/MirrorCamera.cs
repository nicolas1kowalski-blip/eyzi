using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace MesPremiersJeux.Lib
{
    /// <summary>
    /// 📷 La caméra avant de la tablette, pour le miroir magique : les images
    /// (BGRA, une quinzaine par seconde) et la position des visages, grâce au
    /// détecteur de visages fourni par Windows (hors ligne, rien ne sort de la
    /// tablette). Tout vient d'un fil de fond : l'appelant repasse sur le fil
    /// d'affichage. Si la caméra manque ou est interdite (réglages de
    /// confidentialité Windows), « Failed » le dit, et le jeu continue sans image.
    /// </summary>
    public sealed class MirrorCamera
    {
        public struct Face { public double X, Y, W, H; }

        public event Action<int, int, byte[]> Frame;   // largeur, hauteur, pixels BGRA
        public event Action<List<Face>> Faces;          // visages, en pixels de l'image
        public event Action<string> Failed;

        private MediaCapture _mc;
        private MediaFrameReader _reader;
        private FaceDetector _det;
        private volatile bool _stopped;
        private int _detecting;
        private int _frameNo;
        private DateTime _lastFrame = DateTime.MinValue;
        private TypedEventHandler<MediaFrameReader, MediaFrameArrivedEventArgs> _onFrame;

        // Les opérations Windows sont asynchrones ; on enchaîne par rappels.
        private static void Then<TR>(IAsyncOperation<TR> op, Action<TR> ok, Action<Exception> fail)
        {
            op.Completed = (o, st) =>
            {
                try { if (st == AsyncStatus.Completed) ok(o.GetResults()); else fail(o.ErrorCode ?? new Exception("opération annulée")); }
                catch (Exception ex) { fail(ex); }
            };
        }

        private static void Then(IAsyncAction op, Action ok, Action<Exception> fail)
        {
            op.Completed = (o, st) =>
            {
                try { if (st == AsyncStatus.Completed) ok(); else fail(o.ErrorCode ?? new Exception("opération annulée")); }
                catch (Exception ex) { fail(ex); }
            };
        }

        private void Fail(Exception ex)
        {
            if (_stopped) return;
            string msg = ex is UnauthorizedAccessException
                ? "Windows interdit la caméra aux applications. Réglages Windows → Confidentialité → Caméra → autoriser les applications de bureau."
                : "Caméra indisponible : " + ex.Message;
            Log.Write("miroir", "Caméra : " + ex);
            Failed?.Invoke(msg);
        }

        public void Start()
        {
            try
            {
                Then(DeviceInformation.FindAllAsync(DeviceClass.VideoCapture), devices =>
                {
                    if (devices.Count == 0) { Failed?.Invoke("Aucune caméra sur cette tablette."); return; }
                    // La caméra AVANT (celle qui voit l'enfant) ; sinon la première.
                    var front = devices.FirstOrDefault(d => d.EnclosureLocation != null && d.EnclosureLocation.Panel == Panel.Front)
                                ?? devices.FirstOrDefault(d => d.Name.IndexOf("front", StringComparison.OrdinalIgnoreCase) >= 0)
                                ?? devices[0];
                    Log.Write("miroir", "Caméra : " + front.Name + " (" + devices.Count + " caméra(s))");
                    _mc = new MediaCapture();
                    var settings = new MediaCaptureInitializationSettings
                    {
                        VideoDeviceId = front.Id,
                        StreamingCaptureMode = StreamingCaptureMode.Video,
                        MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                    };
                    Then(_mc.InitializeAsync(settings), () =>
                    {
                        var src = _mc.FrameSources.Values.FirstOrDefault(s => s.Info.SourceKind == MediaFrameSourceKind.Color && s.Info.MediaStreamType == MediaStreamType.VideoPreview)
                                  ?? _mc.FrameSources.Values.FirstOrDefault(s => s.Info.SourceKind == MediaFrameSourceKind.Color)
                                  ?? _mc.FrameSources.Values.FirstOrDefault();
                        if (src == null) { Failed?.Invoke("La caméra ne fournit pas d'image."); return; }
                        // Une image modeste suffit (et ménage la tablette) : la plus proche de 640 de large.
                        var fmt = src.SupportedFormats
                            .Where(f => f.VideoFormat != null && f.VideoFormat.Width >= 320)
                            .OrderBy(f => Math.Abs((int)f.VideoFormat.Width - 640))
                            .ThenByDescending(f => f.FrameRate.Numerator / Math.Max(1.0, f.FrameRate.Denominator))
                            .FirstOrDefault();
                        Action afterFormat = () =>
                        {
                            Then(_mc.CreateFrameReaderAsync(src, MediaEncodingSubtypes.Bgra8), reader =>
                            {
                                _reader = reader;
                                Then(FaceDetector.CreateAsync(), det =>
                                {
                                    _det = det;
                                    _onFrame = OnFrameArrived;
                                    _reader.FrameArrived += _onFrame;
                                    Then(_reader.StartAsync(), status =>
                                    {
                                        if (status != MediaFrameReaderStartStatus.Success) Failed?.Invoke("La caméra n'a pas démarré (" + status + ").");
                                    }, Fail);
                                }, Fail);
                            }, Fail);
                        };
                        if (fmt != null) Then(src.SetFormatAsync(fmt), afterFormat, ex => afterFormat());
                        else afterFormat();
                    }, Fail);
                }, Fail);
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs e)
        {
            if (_stopped) return;
            var now = DateTime.UtcNow;
            if ((now - _lastFrame).TotalMilliseconds < 60) return; // ~15 images par seconde suffisent
            _lastFrame = now;
            try
            {
                using (var f = sender.TryAcquireLatestFrame())
                {
                    var bmp = f?.VideoMediaFrame?.SoftwareBitmap;
                    if (bmp == null) return;
                    SoftwareBitmap bgra = bmp;
                    bool converted = false;
                    if (bmp.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
                    {
                        bgra = SoftwareBitmap.Convert(bmp, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
                        converted = true;
                    }
                    int w = bgra.PixelWidth, h = bgra.PixelHeight;
                    var pixels = new byte[w * h * 4];
                    var buf = new Windows.Storage.Streams.Buffer((uint)pixels.Length);
                    bgra.CopyToBuffer(buf);
                    DataReader.FromBuffer(buf).ReadBytes(pixels);
                    if (converted) bgra.Dispose();
                    Frame?.Invoke(w, h, pixels);

                    // Les visages : une image sur trois, jamais deux détections à la fois.
                    if (++_frameNo % 3 == 0 && _det != null && Interlocked.CompareExchange(ref _detecting, 1, 0) == 0)
                    {
                        var gray = SoftwareBitmap.Convert(bmp, BitmapPixelFormat.Gray8);
                        Then(_det.DetectFacesAsync(gray), faces =>
                        {
                            var list = faces.Select(fc => new Face { X = fc.FaceBox.X, Y = fc.FaceBox.Y, W = fc.FaceBox.Width, H = fc.FaceBox.Height }).ToList();
                            gray.Dispose();
                            _detecting = 0;
                            if (!_stopped) Faces?.Invoke(list);
                        }, ex => { gray.Dispose(); _detecting = 0; });
                    }
                }
            }
            catch (Exception ex) { Log.Write("miroir", "Image : " + ex.Message); }
        }

        public void Stop()
        {
            _stopped = true;
            try
            {
                var r = _reader; var mc = _mc;
                _reader = null; _mc = null;
                if (r != null)
                {
                    if (_onFrame != null) r.FrameArrived -= _onFrame;
                    Then(r.StopAsync(), () => { try { r.Dispose(); mc?.Dispose(); } catch { } }, ex => { try { r.Dispose(); mc?.Dispose(); } catch { } });
                }
                else mc?.Dispose();
            }
            catch (Exception ex) { Log.Write("miroir", "Arrêt caméra : " + ex.Message); }
        }
    }
}
