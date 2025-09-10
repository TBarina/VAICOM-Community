using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VAICOM.Extensions.Kneeboard
{
    /// <summary>
    /// Watches a kneeboard output folder and sends new/changed files to a remote receiver.
    /// Simple TCP protocol:
    /// [4 bytes length of filename][filename utf8][8 bytes file length][file bytes]
    /// </summary>
    public class KneeboardRemoteExporter : IDisposable
    {
        readonly string watchFolder;
        readonly string remoteIp;
        readonly int remotePort;
        readonly FileSystemWatcher watcher;
        readonly TimeSpan debounceDelay = TimeSpan.FromMilliseconds(300);
        readonly SynchronizationContext context;

        // If you want to toggle behavior from UI, expose a public property.
        public bool Enabled { get; set; } = true;

        // Optional: only send certain extensions
        readonly string[] allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".html", ".htm", ".txt" };

        public KneeboardRemoteExporter(string watchFolder, string remoteIp, int remotePort)
        {
            this.watchFolder = watchFolder ?? throw new ArgumentNullException(nameof(watchFolder));
            this.remoteIp = remoteIp ?? throw new ArgumentNullException(nameof(remoteIp));
            this.remotePort = remotePort;
            context = SynchronizationContext.Current;

            watcher = new FileSystemWatcher(watchFolder)
            {
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
            };

            watcher.Created += OnChangedDebounced;
            watcher.Changed += OnChangedDebounced;
            watcher.Renamed += OnRenamedDebounced;
            watcher.Error += OnWatcherError;
        }

        void OnWatcherError(object sender, ErrorEventArgs e)
        {
            // Log if VAICOM has logging; for now just Console.
            Console.WriteLine("KneeboardRemoteExporter watcher error: " + e.GetException());
        }

        void OnRenamedDebounced(object sender, RenamedEventArgs e)
        {
            if (!Enabled) return;
            _ = Task.Run(async () =>
            {
                await Task.Delay(debounceDelay);
                await ProcessFileIfAllowedAsync(e.FullPath);
            });
        }

        void OnChangedDebounced(object sender, FileSystemEventArgs e)
        {
            if (!Enabled) return;
            _ = Task.Run(async () =>
            {
                await Task.Delay(debounceDelay);
                await ProcessFileIfAllowedAsync(e.FullPath);
            });
        }

        bool IsAllowed(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            foreach (var a in allowedExtensions) if (ext == a) return true;
            return false;
        }

        async Task ProcessFileIfAllowedAsync(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                if (!IsAllowed(path)) return;

                // Wait a bit to ensure write finished; use small retries.
                const int maxAttempts = 6;
                int attempt = 0;
                while (attempt < maxAttempts)
                {
                    attempt++;
                    try
                    {
                        using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            // We can read, so proceed
                            byte[] data;
                            using (var ms = new MemoryStream())
                            {
                                await stream.CopyToAsync(ms);
                                data = ms.ToArray();
                            }

                            await SendFileAsync(Path.GetFileName(path), data);
                            return;
                        }
                    }
                    catch (IOException)
                    {
                        // file is locked; wait and retry
                        await Task.Delay(150 * attempt);
                    }
                }
                Console.WriteLine($"KneeboardRemoteExporter: Couldn't read file after retries: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("KneeboardRemoteExporter ProcessFile error: " + ex);
            }
        }

        async Task SendFileAsync(string filename, byte[] data)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    // small timeout
                    var connectTask = client.ConnectAsync(remoteIp, remotePort);
                    var timeoutTask = Task.Delay(3000);
                    var completed = await Task.WhenAny(connectTask, timeoutTask);
                    if (completed == timeoutTask)
                    {
                        Console.WriteLine($"KneeboardRemoteExporter: connect timeout to {remoteIp}:{remotePort}");
                        return;
                    }

                    using (var ns = client.GetStream())
                    {
                        // protocol: [4 bytes filename length][filename utf8][8 bytes file length][file bytes]
                        var filenameBytes = Encoding.UTF8.GetBytes(filename);
                        var filenameLengthBytes = BitConverter.GetBytes((Int32)filenameBytes.Length);
                        var fileLengthBytes = BitConverter.GetBytes((Int64)data.LongLength);

                        await ns.WriteAsync(filenameLengthBytes, 0, filenameLengthBytes.Length);
                        await ns.WriteAsync(filenameBytes, 0, filenameBytes.Length);
                        await ns.WriteAsync(fileLengthBytes, 0, fileLengthBytes.Length);
                        await ns.WriteAsync(data, 0, data.Length);
                        await ns.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("KneeboardRemoteExporter SendFile error: " + ex);
            }
        }

        public void Dispose()
        {
            watcher?.Dispose();
        }
    }
}
