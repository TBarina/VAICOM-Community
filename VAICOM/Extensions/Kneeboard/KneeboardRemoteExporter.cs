using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VAICOM.Static;

namespace VAICOM.Extensions.Kneeboard
{
    public enum MessageType : byte
    {
        Init = 0x01,        // Inizializzazione (percorso, config)
        KneeboardData = 0x02, // Dati strutturati kneeboard
        Log = 0x03,         // Messaggi di log
        FileTransfer = 0x04, // Transfer file
        Command = 0x05,     // Comandi remoti
        Heartbeat = 0x06    // Keep-alive
    }

    /// <summary>
    /// Watches a kneeboard output folder and sends new/changed files to a remote receiver.
    /// Simple TCP protocol:
    /// [4 bytes length of filename][filename utf8][8 bytes file length][file bytes]
    /// </summary>
    public class KneeboardRemoteExporter : IDisposable
    {
        private readonly string _kneeboardPath;
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

            _kneeboardPath = this.watchFolder;

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

            Log.Write($"FileSystemWatcher created for: {watchFolder}", Colors.Text);
            Log.Write($"Watching for: {watcher.NotifyFilter}", Colors.Text);
            Log.Write($"Filter: {watcher.Filter}", Colors.Text);
            Log.Write($"Include subdirectories: {watcher.IncludeSubdirectories}", Colors.Text);

            // Test immediato del watcher
            if (Directory.Exists(watchFolder))
            {
                var files = Directory.GetFiles(watchFolder);
                Log.Write($"Found {files.Length} files in kneeboard folder", Colors.Text);
            }
            else
            {
                Log.Write($"Kneeboard folder does not exist: {watchFolder}", Colors.Warning);
            }
        }

        // In KneeboardRemoteExporter.cs (VAICOM)
        public async Task SendInitializationData()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(remoteIp, remotePort);

                    using (var ns = client.GetStream())
                    using (var writer = new BinaryWriter(ns, Encoding.UTF8, true))
                    {
                        //var initData = new
                        //{
                        //    Type = "INIT",
                        //    KneeboardPath = _kneeboardPath,
                        //    Timestamp = DateTime.UtcNow
                        //};

                        //var json = Newtonsoft.Json.JsonConvert.SerializeObject(initData);
                        //var dataBytes = Encoding.UTF8.GetBytes(json);
                        
                        // INVIA IL PERCORSO COME STRINGA SEMPLICE (non JSON)
                        var dataBytes = Encoding.UTF8.GetBytes(_kneeboardPath);

                        ns.WriteByte(((byte)MessageType.Init)); // 0x01 = Messaggio di inizializzazione
                        writer.Write(dataBytes.Length);
                        writer.Write(dataBytes);
                        await ns.FlushAsync();

                        Log.Write($"Sent initialization data to receiver: {_kneeboardPath}", Colors.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write($"SendInitializationData error: {ex.Message}", Colors.Text);
            }
        }

        public async void SendKneeboardMessage(KneeboardMessage message)
        {
            if (!Enabled) return;

            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(remoteIp, remotePort);
                    var timeoutTask = Task.Delay(5000);

                    if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                    {
                        Log.Write($"Connection timeout to {remoteIp}:{remotePort}", Colors.Warning);
                        return;
                    }

                    using (var ns = client.GetStream())
                    using (var writer = new BinaryWriter(ns, Encoding.UTF8, true))
                    {
                        // Invia tipo messaggio (0x02 = JSON)
                        ns.WriteByte(((byte)MessageType.KneeboardData));

                        // Serializza il messaggio in JSON
                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                        var dataBytes = Encoding.UTF8.GetBytes(json);

                        // Protocollo: [4 bytes length] + [json data]
                        writer.Write(dataBytes.Length);
                        writer.Write(dataBytes);
                        await ns.FlushAsync();

                        Log.Write($"Sent kneeboard message to remote: {message.eventid}", Colors.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write($"SendKneeboardMessage error: {ex.Message}", Colors.Warning);
            }
        }

        public async Task SendLogMessageAsync(string message, string logLevel = "INFO")
        {
            if (!Enabled) return;

            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(remoteIp, remotePort);
                    var timeoutTask = Task.Delay(3000);

                    if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                    {
                        return; // Timeout, esci silenziosamente
                    }

                    using (var ns = client.GetStream())
                    using (var writer = new BinaryWriter(ns, Encoding.UTF8, true))
                    {
                        // Crea un messaggio di log
                        var logData = new
                        {
                            Type = "LOG",
                            Level = logLevel,
                            Message = message,
                            Timestamp = DateTime.UtcNow,
                            Source = "VAICOM"
                        };

                        // Serializza in JSON
                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(logData);
                        var dataBytes = Encoding.UTF8.GetBytes(json);

                        // Protocollo: [tipo messaggio 0x03] + [lunghezza] + [dati]
                        ns.WriteByte(((byte)MessageType.Log)); // 0x03 = Messaggio di log
                        writer.Write(dataBytes.Length);
                        writer.Write(dataBytes);
                        await ns.FlushAsync();
                    }
                }
            }
            catch
            {
                // Ignora errori di logging per non creare loop
            }
        }

        void OnWatcherError(object sender, ErrorEventArgs e)
        {
            // Log if VAICOM has logging; for now just Console.
            Console.WriteLine("KneeboardRemoteExporter watcher error: " + e.GetException());
        }

        void OnRenamedDebounced(object sender, RenamedEventArgs e)
        {
            if (!Enabled)
            {
                Log.Write($"Exporter disabled, ignoring rename: {e.OldFullPath} -> {e.FullPath}", Colors.Text);
                return;
            }

            Log.Write($"File renamed: {e.OldFullPath} -> {e.FullPath}", Colors.Text);

            _ = Task.Run(async () =>
            {
                await Task.Delay(debounceDelay);
                await ProcessFileIfAllowedAsync(e.FullPath);
            });
        }

        void OnChangedDebounced(object sender, FileSystemEventArgs e)
        {
            if (!Enabled)
            {
                Log.Write($"Exporter disabled, ignoring file: {e.FullPath}", Colors.Text);
                return;
            }
            Log.Write($"File system event: {e.ChangeType} - {e.FullPath}", Colors.Text);

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
                        // 1. Invia tipo messaggio (0x04 = file)
                        ns.WriteByte((byte)MessageType.FileTransfer);

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


        public async Task ManualTestSendAsync(string testFilePath)
        {
            if (!File.Exists(testFilePath))
            {
                Log.Write($"Test file does not exist: {testFilePath}", Colors.Warning);
                return;
            }

            Log.Write($"Manual test: sending {testFilePath}", Colors.Text);
            await ProcessFileIfAllowedAsync(testFilePath);
        }

        public void StartTest()
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000); // Aspetta che l'inizializzazione sia completa

                await TestConnectionAsync();

                try
                {
                    string testFile = Path.Combine(watchFolder, "connection_test.txt");
                    File.WriteAllText(testFile, "Connection test file");
                    await ProcessFileIfAllowedAsync(testFile);
                    File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    Log.Write($"Auto-test failed: {ex.Message}", Colors.Warning);
                }
            });
        }

        public async Task TestConnectionAsync()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(remoteIp, remotePort);
                    var timeoutTask = Task.Delay(2000);

                    if (await Task.WhenAny(connectTask, timeoutTask) == connectTask)
                    {
                        Log.Write($"Connection test to {remoteIp}:{remotePort} SUCCESSFUL", Colors.Text);
                    }
                    else
                    {
                        Log.Write($"Connection test TIMEOUT to {remoteIp}:{remotePort}", Colors.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write($"Connection test FAILED: {ex.Message}", Colors.Warning);
            }
        }

        public void Dispose()
        {
            watcher?.Dispose();
        }
    }
}
