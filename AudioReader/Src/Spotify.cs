using SpotifyAPI.Local;
using SpotifyAPI.Local.Enums;
using SpotifyAPI.Local.Models;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace AudioReader
{
    internal delegate void TrackChangedHandler(string title, string artist, string album, byte[] art);
    internal delegate void TimeChangedHandler(int time, int totalTime, double progress);
    internal delegate void PlayingChangedHandler(bool isPlaying);

    internal static class Spotify
    {
        private static SpotifyLocalAPI _spotify;
        private static StatusResponse _status;
        private static DateTime _lastStatusUpdate = DateTime.Now;
        private static int _lastPrintedTime = 0;

        internal static bool Ready { get; private set; } = false;
        #pragma warning disable 0649 // allow these so stay null in case they are never subscribed to
        internal static TrackChangedHandler OnTrackChanged;
        internal static TimeChangedHandler OnTimeChanged;
        internal static PlayingChangedHandler OnPlayingChanged;
        #pragma warning restore 0649

        internal static void Setup()
        {
            _spotify = new SpotifyLocalAPI();
            if (!SpotifyLocalAPI.IsSpotifyRunning())
            {
                Log.Error("Spotify", "Make sure Spotify is running in order to use the Spotify integration.");
                return;
            }

            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                Log.Error("Spotify", "The Spotify web helper isn't available.");
                return;
            }

            if (!_spotify.Connect())
            {
                Log.Error("Spotify", "Could not connect to Spotify.");
                return;
            }

            _spotify.ListenForEvents = true;

            _spotify.OnTrackChange += (object sender, TrackChangeEventArgs args) =>
            {
                // by updating the status, the OnTrackTimeChange listener is able to use the new track time - don't use the function to ensure it is actually updated
                _status = _spotify.GetStatus();
                _lastPrintedTime = -1;
                var title = args.NewTrack.TrackResource.Name;
                var artist = args.NewTrack.ArtistResource.Name;
                var album = args.NewTrack.AlbumResource.Name;
                var art = _jpgToRGB(args.NewTrack.GetAlbumArtAsByteArray(AlbumArtSize.Size640));
                Log.Info("Spotify", "Track changed. New track: " + title + " by " + artist + " on " + album + ".");
                OnTrackChanged?.Invoke(title, artist, album, art);
            };

            _spotify.OnTrackTimeChange += (object sender, TrackTimeChangeEventArgs args) =>
            {
                var time = (int)args.TrackTime;
                var totalTime = _status.Track.Length;
                var progress = args.TrackTime / _status.Track.Length;
                if (time > _lastPrintedTime)
                {
                    _lastPrintedTime = time;
                    Log.Verbose("Spotify", "Time changed. Currently at " + time / 60 + ":" + (time % 60).ToString("00", CultureInfo.InvariantCulture) + "/" + totalTime / 60 + ":" + (totalTime % 60).ToString("00", CultureInfo.InvariantCulture) + " which is " + progress.ToString("#0.00%", CultureInfo.InvariantCulture) + ".");
                }
                OnTimeChanged?.Invoke(time, totalTime, progress);
            };

            _spotify.OnPlayStateChange += (object sender, PlayStateEventArgs args) =>
            {
                Log.Info("Spotify", "Spotify is now " + (args.Playing ? "playing." : "paused."));
                OnPlayingChanged?.Invoke(args.Playing);
            };

            Ready = true;

            Log.Info("Spotify", "Spotify " + (IsPlaying() ? "is" : "isn't") + " currently playing.");

            if (!IsPlaying())
                return;

            Log.Info("Spotify",
                "The current track is " + GetTitle() + " by " + GetArtist() + " on " + GetAlbum() + ".");
        }

        private static void _updateStatus()
        {
            // limit status updating to once a second
            // in order to get more detailed info about the play progress, subscribe to the respective event
            if (_status == null || (DateTime.Now - _lastStatusUpdate).TotalMilliseconds > 1000)
            {
                _status = _spotify.GetStatus();
                _lastStatusUpdate = DateTime.Now;
            }
        }

        // a safe runner for getter functions
        private static bool _safeGet<T>(out T result, Func<T> getter)
        {
            result = default(T);
            try
            {
                if (!Ready)
                    return false;
                _updateStatus();
                result = getter.Invoke();
            }
            catch (Exception ex)
            {
                if (ex is NullReferenceException)
                    return false;
                throw;
            }
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static bool IsPlaying() => _safeGet(out var result, () => _status.Playing) && result;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static bool GetTitle(out string title) => _safeGet(out title, () => _status.Track.TrackResource.Name);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static string GetTitle() => GetTitle(out var title) ? title : "";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static bool GetArtist(out string artist) => _safeGet(out artist, () => _status.Track.ArtistResource.Name);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static string GetArtist() => GetArtist(out var artist) ? artist : "";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static bool GetAlbum(out string album) => _safeGet(out album, () => _status.Track.AlbumResource.Name);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static string GetAlbum() => GetAlbum(out var album) ? album : "";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static bool GetAlbumArt(out byte[] art) => _safeGet(out art, () => _jpgToRGB(_status.Track.GetAlbumArtAsByteArray(AlbumArtSize.Size640)));

        // no simple accessor for art - this should always be checked for success!

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static bool GetTrackLength(out int time) => _safeGet(out time, () => _status.Track.Length);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static int GetTrackLength() => GetTrackLength(out var time) ? time : 0;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static bool GetTrackLength(out int minutes, out int seconds) => GetTrackLength(out var time) & ((minutes = time / 60) > 0 | (seconds = time % 60) > 0);

        // no simple accessor for track length with minutes and seconds - would create ambiguity

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static bool GetTrackProgressRatio(out double progress) => _safeGet(out progress, () => _status.PlayingPosition / _status.Track.Length);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static double GetTrackProgressRatio() => GetTrackProgressRatio(out var progress) ? progress : 0;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static bool GetTrackProgress(out int time) => _safeGet(out time, () => (int)(_status.PlayingPosition / _status.Track.Length));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static int GetTrackProgress() => GetTrackProgress(out var time) ? time : 0;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static bool GetTrackProgress(out int minutes, out int seconds) => GetTrackProgress(out var time) & ((minutes = time / 60) > 0 | (seconds = time % 60) > 0);

        // no simple accessor for track progress with minutes and seconds - would create ambiguity

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static void Play() => _spotify.Play();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static void Pause() => _spotify.Pause();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static void Next() => _spotify.Skip();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static void Previous() => _spotify.Previous();

        private static byte[] _jpgToRGB(byte[] jpg)
        {
            using (var inStream = new MemoryStream(jpg))
            {
                var bitmap = (Bitmap)Image.FromStream(inStream);
                var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var bmpData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
                var ptr = bmpData.Scan0;
                var bytes = bmpData.Stride * bitmap.Height;
                var brgValues = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(ptr, brgValues, 0, bytes); bitmap.UnlockBits(bmpData);
                var rgbValues = new byte[bytes];
                for (var y = 0; y < bitmap.Height; y++)
                    for (var x = 0; x < bmpData.Stride; x++)
                        rgbValues[y * bmpData.Stride + x] = brgValues[(bmpData.Height - 1 - y) * bmpData.Stride + x + (x % 3 == 0 ? 2 : ((x + 1) % 3 == 0 ? -2 : 0))];
                return rgbValues;
            }
        }
    }
}
