using NewTek;
using NewTek.NDI;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace AirDirector.Services
{
    /// <summary>
    /// Receiver NDI dedicato per la preview in OverviewControl
    /// </summary>
    public class NDIPreviewReceiver : IDisposable
    {
        private IntPtr _recvInstance = IntPtr.Zero;
        private Thread _receiveThread;
        private volatile bool _running = false;
        private volatile bool _disposed = false;
        private string _sourceName = "";

        private Bitmap _lastFrame = null;
        private readonly object _frameLock = new object();

        public event Action<Bitmap> FrameReceived;

        public bool IsConnected { get; private set; } = false;
        public string CurrentSource => _sourceName;

        /// <summary>
        /// Avvia la ricezione da una sorgente NDI
        /// </summary>
        public bool Start(string sourceName)
        {
            if (_running) Stop();

            if (string.IsNullOrEmpty(sourceName))
            {
                Console.WriteLine("[NDIPreview] ⚠️ Nome sorgente vuoto");
                return false;
            }

            try
            {
                if (!NDIlib.initialize())
                {
                    Console.WriteLine("[NDIPreview] ❌ NDI non inizializzato");
                    return false;
                }

                _sourceName = sourceName;

                // Crea il receiver
                string fullName = $"{Environment.MachineName} ({sourceName})";

                NDIlib.source_t source = new NDIlib.source_t
                {
                    p_ndi_name = Marshal.StringToHGlobalAnsi(fullName)
                };

                NDIlib.recv_create_v3_t recvSettings = new NDIlib.recv_create_v3_t
                {
                    source_to_connect_to = source,
                    color_format = NDIlib.recv_color_format_e.recv_color_format_BGRX_BGRA,
                    bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_lowest, // Bassa qualità per preview
                    allow_video_fields = false,
                    p_ndi_recv_name = Marshal.StringToHGlobalAnsi("AirDirector Preview")
                };

                _recvInstance = NDIlib.recv_create_v3(ref recvSettings);

                Marshal.FreeHGlobal(source.p_ndi_name);
                Marshal.FreeHGlobal(recvSettings.p_ndi_recv_name);

                if (_recvInstance == IntPtr.Zero)
                {
                    Console.WriteLine("[NDIPreview] ❌ Impossibile creare receiver");
                    return false;
                }

                _running = true;
                _receiveThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.BelowNormal,
                    Name = "NDI_Preview_Receiver"
                };
                _receiveThread.Start();

                Console.WriteLine($"[NDIPreview] ✅ Avviato per:  {fullName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NDIPreview] ❌ Errore avvio:  {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ferma la ricezione
        /// </summary>
        public void Stop()
        {
            _running = false;

            _receiveThread?.Join(1000);
            _receiveThread = null;

            if (_recvInstance != IntPtr.Zero)
            {
                NDIlib.recv_destroy(_recvInstance);
                _recvInstance = IntPtr.Zero;
            }

            IsConnected = false;
            Console.WriteLine("[NDIPreview] ⏹️ Fermato");
        }

        /// <summary>
        /// Riconnetti a una nuova sorgente
        /// </summary>
        public void Reconnect(string sourceName)
        {
            Stop();
            Start(sourceName);
        }

        /// <summary>
        /// Loop di ricezione frame
        /// </summary>
        private void ReceiveLoop()
        {
            NDIlib.video_frame_v2_t videoFrame = new NDIlib.video_frame_v2_t();
            NDIlib.audio_frame_v2_t audioFrame = new NDIlib.audio_frame_v2_t();
            NDIlib.metadata_frame_t metadataFrame = new NDIlib.metadata_frame_t();
            int frameSkip = 0;

            while (_running && !_disposed)
            {
                try
                {
                    // Ricevi frame (timeout 100ms) - passa ref per audio e metadata
                    NDIlib.frame_type_e frameType = NDIlib.recv_capture_v2(
                        _recvInstance,
                        ref videoFrame,
                        ref audioFrame,      // ✅ Aggiunto ref
                        ref metadataFrame,   // ✅ Aggiunto ref
                        100);

                    if (frameType == NDIlib.frame_type_e.frame_type_video)
                    {
                        IsConnected = true;

                        // Processa solo 1 frame ogni 3 (circa 8-10 fps per preview)
                        frameSkip++;
                        if (frameSkip >= 3)
                        {
                            frameSkip = 0;
                            ProcessVideoFrame(ref videoFrame);
                        }

                        // Libera il frame
                        NDIlib.recv_free_video_v2(_recvInstance, ref videoFrame);
                    }
                    else if (frameType == NDIlib.frame_type_e.frame_type_audio)
                    {
                        // Ignora audio per la preview, ma libera il frame
                        NDIlib.recv_free_audio_v2(_recvInstance, ref audioFrame);
                    }
                    else if (frameType == NDIlib.frame_type_e.frame_type_metadata)
                    {
                        // Ignora metadata, ma libera il frame
                        NDIlib.recv_free_metadata(_recvInstance, ref metadataFrame);
                    }
                    else if (frameType == NDIlib.frame_type_e.frame_type_none)
                    {
                        // Nessun frame disponibile
                        IsConnected = false;
                        Thread.Sleep(50);
                    }
                    else if (frameType == NDIlib.frame_type_e.frame_type_error)
                    {
                        IsConnected = false;
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NDIPreview] ⚠️ Receive error: {ex.Message}");
                    Thread.Sleep(100);
                }
            }
        }
        

        /// <summary>
        /// Processa un frame video e genera bitmap
        /// </summary>
        private void ProcessVideoFrame(ref NDIlib.video_frame_v2_t frame)
        {
            if (frame.p_data == IntPtr.Zero || frame.xres <= 0 || frame.yres <= 0)
                return;

            try
            {
                int width = frame.xres;
                int height = frame.yres;
                int stride = frame.line_stride_in_bytes;

                // Crea bitmap dal buffer NDI
                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                BitmapData bmpData = bmp.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);

                // Copia righe (gestendo stride diversi)
                if (stride == bmpData.Stride)
                {
                    // Stride uguale - copia diretta
                    int size = stride * height;
                    unsafe
                    {
                        Buffer.MemoryCopy(
                            (void*)frame.p_data,
                            (void*)bmpData.Scan0,
                            size,
                            size);
                    }
                }
                else
                {
                    // Stride diversi - copia riga per riga
                    int copyBytes = Math.Min(stride, bmpData.Stride);
                    for (int y = 0; y < height; y++)
                    {
                        IntPtr srcRow = IntPtr.Add(frame.p_data, y * stride);
                        IntPtr dstRow = IntPtr.Add(bmpData.Scan0, y * bmpData.Stride);

                        unsafe
                        {
                            Buffer.MemoryCopy(
                                (void*)srcRow,
                                (void*)dstRow,
                                copyBytes,
                                copyBytes);
                        }
                    }
                }

                bmp.UnlockBits(bmpData);

                // Aggiorna ultimo frame
                lock (_frameLock)
                {
                    _lastFrame?.Dispose();
                    _lastFrame = bmp;
                }

                // Notifica listeners
                FrameReceived?.Invoke(bmp);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NDIPreview] ⚠️ Errore processamento frame: {ex.Message}");
            }
        }

        /// <summary>
        /// Ottieni l'ultimo frame disponibile
        /// </summary>
        public Bitmap GetLastFrame()
        {
            lock (_frameLock)
            {
                return _lastFrame != null ? new Bitmap(_lastFrame) : null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Stop();

            lock (_frameLock)
            {
                _lastFrame?.Dispose();
                _lastFrame = null;
            }

            Console.WriteLine("[NDIPreview] ✅ Disposed");
        }
    }
}