using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace VAICOM.KneeboardReceiver
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 55000;
            string outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReceivedKneeboard");
            Directory.CreateDirectory(outDir);

            Console.WriteLine($"Kneeboard Receiver listening on port {port}...");
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            while (true)
            {
                using (var client = listener.AcceptTcpClient())
                using (var ns = client.GetStream())
                {
                    // 1) read filename length (4 bytes)
                    var buf = new byte[4];
                    ns.Read(buf, 0, 4);
                    int fnLen = BitConverter.ToInt32(buf, 0);

                    // 2) read filename
                    var fnBytes = new byte[fnLen];
                    ns.Read(fnBytes, 0, fnLen);
                    string filename = Encoding.UTF8.GetString(fnBytes);

                    // 3) read file length (8 bytes)
                    var flBuf = new byte[8];
                    ns.Read(flBuf, 0, 8);
                    long fileLen = BitConverter.ToInt64(flBuf, 0);

                    // 4) read file content
                    var data = new byte[fileLen];
                    int offset = 0;
                    while (offset < fileLen)
                    {
                        int read = ns.Read(data, offset, (int)(fileLen - offset));
                        if (read <= 0) break;
                        offset += read;
                    }

                    string outPath = Path.Combine(outDir, filename);
                    File.WriteAllBytes(outPath, data);
                    Console.WriteLine($"Saved: {outPath} ({data.Length} bytes)");
                }
            }
        }
    }
}

