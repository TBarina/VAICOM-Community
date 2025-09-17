using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VAICOM.KneeboardReceiver
{
    internal static class Program
    {
        public static bool IsTestMode { get; private set; } = false;
        
        public static AdvancedKneeboardManager KneeboardManager { get; private set; } = new AdvancedKneeboardManager();

        public static string outDir { get; private set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReceivedKneeboard");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread] // IMPORTANTE per Windows Forms
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Directory.CreateDirectory(outDir);

            // Controlla se è modalità test
            if (args.Length > 0 && args[0] == "--test")
            {
                IsTestMode = true;
                RunTestMode(args);
                return;
            }

            // Modalità normale receiver con UI
            IsTestMode = false;
            RunReceiverMode();
        }

        static void RunTestMode(string[] args)
        {
            if (args.Length > 1 && args[1] == "--interactive")
            {
                // Modalità console interattiva (se ancora la vuoi)
                //KneeboardTester.InteractiveTest();
            }
            else
            {
                // Modalità UI grafica per test
                var mainForm = new MainForm();
                var formsDisplay = new FormsDisplay(mainForm);
                Program.KneeboardManager.SetDisplay(formsDisplay);

                KneeboardTester.SetupTestManager();

                Application.Run(mainForm);
            }
        }

        static void RunReceiverMode()
        {
            var mainForm = new MainForm();
            var formsDisplay = new FormsDisplay(mainForm);
            Program.KneeboardManager.SetDisplay(formsDisplay);

            // Avvia il server TCP in background
            Task.Run(() => StartTcpServer());

            Application.Run(mainForm);
        }

        static async Task StartTcpServer()
        {
            int port = 55000;

            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            // Usa il logger della UI invece di KneeboardManager.LogMessage
            KneeboardManager.LogMessage($"Kneeboard Receiver listening on port {port}...");
            KneeboardManager.LogMessage($"Output directory: {outDir}");

            try
            {
                while (true)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    KneeboardManager.LogMessage($"Client connected: {client.Client.RemoteEndPoint}");

                    _ = Task.Run(() => HandleClient(client, outDir));
                }
            }
            catch (Exception ex)
            {
                KneeboardManager.LogMessage($"Server error: {ex.Message}");
            }
            finally
            {
                listener.Stop();
            }
        }

        static void HandleClient(TcpClient client, string outDir)
        {
            try
            {        
                KneeboardManager.LogMessage($"Handling client from: {client.Client.RemoteEndPoint}");
                
                using (client)
                using (var ns = client.GetStream())
                {
                    // Leggi il tipo di messaggio (1 byte)
                    int messageType = ns.ReadByte();
                    if (messageType == -1) return; // Connessione chiusa

                    KneeboardManager.LogMessage($"Received message type: {messageType} (0x{messageType:X2})");

                    switch (messageType)
                    {
                        case 0x01: // Init
                            KneeboardManager.LogMessage("Handling initialization message");
                            HandleInitMessage(ns);
                            break;

                        case 0x02: // Dati JSON
                            KneeboardManager.LogMessage("Handling JSON data message");
                            HandleJsonMessage(ns);
                            break;

                        case 0x03: // Messaggi di log
                            KneeboardManager.LogMessage("Handling log message");
                            HandleLogMessage(ns);
                            break;

                        case 0x04: // FILE - Transfer file
                            KneeboardManager.LogMessage("Handling file transfer message");
                            HandleFileMessage(ns, outDir);
                            break;

                        default:
                            KneeboardManager.LogMessage($"Unknown message type: {messageType} (0x{messageType:X2})");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                KneeboardManager.LogMessage($"Error handling client: {ex.Message}");
            }
            finally
            {
                client?.Close();
            }
        }

        static void HandleInitMessage(NetworkStream ns)
        {
            try
            {
                using (var reader = new BinaryReader(ns, Encoding.UTF8, true))
                {
                    int dataLength = reader.ReadInt32();

                    if (dataLength <= 0 || dataLength > 1000000)
                    {
                        Program.KneeboardManager.LogMessage($"Invalid data length: {dataLength}");
                        return;
                    }

                    byte[] dataBytes = reader.ReadBytes(dataLength);
                    string messageContent = Encoding.UTF8.GetString(dataBytes);

                    Program.KneeboardManager.LogMessage($"Received init message: {messageContent}");

                    // Prima prova come percorso diretto
                    if (Directory.Exists(messageContent))
                    {
                        Program.KneeboardManager.LogMessage($"Using direct path: {messageContent}");
                        Program.KneeboardManager.Initialize(messageContent);
                        return;
                    }

                    // Poi prova come JSON
                    try
                    {
                        if (messageContent.Trim().StartsWith("{"))
                        {
                            var initData = JsonConvert.DeserializeObject<dynamic>(messageContent);
                            string kneeboardPath = initData.KneeboardPath;

                            if (Directory.Exists(kneeboardPath))
                            {
                                Program.KneeboardManager.LogMessage($"Using JSON path: {kneeboardPath}");
                                Program.KneeboardManager.Initialize(kneeboardPath);
                                return;
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignora e prova altri formati
                    }

                    // Fallback: cerca un path nella stringa
                    string extractedPath = ExtractPathFromString(messageContent);
                    if (!string.IsNullOrEmpty(extractedPath) && Directory.Exists(extractedPath))
                    {
                        Program.KneeboardManager.LogMessage($"Using extracted path: {extractedPath}");
                        Program.KneeboardManager.Initialize(extractedPath);
                    }
                    else
                    {
                        Program.KneeboardManager.LogMessage("No valid path found in init message");
                    }
                }
            }
            catch (Exception ex)
            {
                Program.KneeboardManager.LogMessage($"Error handling init message: {ex.Message}");
                Program.KneeboardManager.LogMessage($"Stack trace: {ex.StackTrace}");
            }
        }
        // Metodo di fallback per estrarre il path
        private static string ExtractPathFromString(string input)
        {
            try
            {
                // Cerca pattern comuni di path
                var pathPatterns = new[]
                {
                    @"[A-Za-z]:\\[\\\S|*\S]?.*",
                    @"/.*",
                    @".*Saved Games.*",
                    @".*DCS.*",
                    @".*Kneeboard.*"
                };

                foreach (var pattern in pathPatterns)
                {
                    var match = Regex.Match(input, pattern);
                    if (match.Success && match.Value.Length > 10) // Lunghezza minima ragionevole
                    {
                        return match.Value;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        static void HandleJsonMessage(NetworkStream ns)
        {
            using (var reader = new BinaryReader(ns, Encoding.UTF8, true))
            {
                // Leggi la lunghezza del JSON
                int dataLength = reader.ReadInt32();
                byte[] jsonBytes = reader.ReadBytes(dataLength);
                string json = Encoding.UTF8.GetString(jsonBytes);

                // Deserializza il KneeboardMessage
                var message = JsonConvert.DeserializeObject<KneeboardServerData>(json);

                // Visualizza i dati
                if (message != null)
                {
                    DisplayServerData(message);
                }
            }
        }

        static void HandleLogMessage(NetworkStream ns)
        {
            try
            {
                using (var reader = new BinaryReader(ns, Encoding.UTF8, true))
                {
                    int dataLength = reader.ReadInt32();
                    byte[] jsonBytes = reader.ReadBytes(dataLength);
                    string json = Encoding.UTF8.GetString(jsonBytes);

                    var logData = JsonConvert.DeserializeObject<dynamic>(json);

                    // Visualizza il log con colori in base al livello
                    string level = logData.Level;
                    string message = logData.Message;
                    DateTime timestamp = logData.Timestamp;

                    ConsoleColor color = ConsoleColor.Gray;
                    switch (level.ToUpper())
                    {
                        case "WARNING":
                            color = ConsoleColor.Yellow;
                            break;
                        case "ERROR":
                            color = ConsoleColor.Red;
                            break;
                        case "INFO":
                            color = ConsoleColor.Cyan;
                            break;
                        case "TEXT":
                            color = ConsoleColor.White;
                            break;
                    }

                    Console.ForegroundColor = color;
                    KneeboardManager.LogMessage($"[{timestamp:HH:mm:ss}] {message}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                KneeboardManager.LogMessage($"Error handling log message: {ex.Message}");
            }
        }

        static void HandleFileMessage(NetworkStream ns, string outDir)
        {
            // 1) Read filename length (4 bytes)
            var buf = new byte[4];
            int bytesRead = ns.Read(buf, 0, 4);
            if (bytesRead != 4)
            {
                KneeboardManager.LogMessage("Invalid filename length received");
                return;
            }
            int fnLen = BitConverter.ToInt32(buf, 0);

            // 2) Read filename
            var fnBytes = new byte[fnLen];
            bytesRead = ns.Read(fnBytes, 0, fnLen);
            if (bytesRead != fnLen)
            {
                KneeboardManager.LogMessage("Invalid filename received");
                return;
            }
            string filename = Encoding.UTF8.GetString(fnBytes);

            // 3) Read file length (8 bytes)
            var flBuf = new byte[8];
            bytesRead = ns.Read(flBuf, 0, 8);
            if (bytesRead != 8)
            {
                KneeboardManager.LogMessage("Invalid file length received");
                return;
            }
            long fileLen = BitConverter.ToInt64(flBuf, 0);

            // 4) Read file content
            var data = new byte[fileLen];
            int offset = 0;
            while (offset < fileLen)
            {
                int read = ns.Read(data, offset, (int)(fileLen - offset));
                if (read <= 0) break;
                offset += read;
            }

            // Verifica di aver ricevuto tutti i dati
            if (offset != fileLen)
            {
                KneeboardManager.LogMessage($"Incomplete file received: {offset}/{fileLen} bytes");
                return;
            }

            string outPath = Path.Combine(outDir, filename);
            File.WriteAllBytes(outPath, data);
            KneeboardManager.LogMessage($"Saved: {outPath} ({data.Length} bytes)");
        }


        static void DisplayServerData(KneeboardServerData data)
        {
            Console.Clear();

            Console.WriteLine("=== VAICOM KNEELBOARD SERVER DATA ===");
            Console.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("=====================================\n");

            Console.WriteLine("SERVER INFORMATION:");
            Console.WriteLine($"Theater: {data.theater ?? "N/A"}");
            Console.WriteLine($"DCS Version: {data.dcsversion ?? "N/A"}");
            Console.WriteLine($"Aircraft: {data.aircraft ?? "N/A"}");
            Console.WriteLine($"Flight Size: {data.flightsize}");
            Console.WriteLine($"Player: {data.playerusername ?? "N/A"}");
            Console.WriteLine($"Callsign: {data.playercallsign ?? "N/A"}");
            Console.WriteLine($"Coalition: {data.coalition ?? "N/A"}");
            Console.WriteLine($"Sortie: {data.sortie ?? "N/A"}");
            Console.WriteLine($"Task: {data.task ?? "N/A"}");
            Console.WriteLine($"Country: {data.country ?? "N/A"}");
            Console.WriteLine($"Multiplayer: {data.multiplayer}");
            Console.WriteLine();

            if (!string.IsNullOrEmpty(data.missiontitle))
            {
                Console.WriteLine("MISSION:");
                Console.WriteLine($"Title: {data.missiontitle}");

                if (!string.IsNullOrEmpty(data.missionbriefing))
                {
                    Console.WriteLine($"Briefing: {data.missionbriefing}");
                }

                if (!string.IsNullOrEmpty(data.missiondetails))
                {
                    Console.WriteLine($"Details: {data.missiondetails}");
                }
                Console.WriteLine();
            }

            Console.WriteLine("=====================================");
            Console.WriteLine("Waiting for updates...\n");
        }

    }
}

