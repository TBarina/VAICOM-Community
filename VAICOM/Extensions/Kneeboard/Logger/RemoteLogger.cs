using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VAICOM.Static;
using static System.Windows.Forms.AxHost;

namespace VAICOM.Extensions.Kneeboard.Logger
{
    public static class RemoteLogger
    {
        public static void Write(string message)
        {
            Log.Write(message, Colors.Text);
            SendToRemoteReceiver(message, "INFO");
        }

        public static void WriteWarning(string message)
        {
            Log.Write(message, Colors.Warning);
            SendToRemoteReceiver(message, "WARNING");
        }

        public static void WriteError(string message)
        {
            Log.Write(message, Colors.Critical);
            SendToRemoteReceiver(message, "ERROR");
        }

        private static void SendToRemoteReceiver(string message, string level)
        {
            if (State.KneeboardExporter != null && State.KneeboardExporter.Enabled)
            {
                _ = State.KneeboardExporter.SendLogMessageAsync(message, level);
            }
            else
            {
                Log.Write("KneeboardExporter not available for " + message, Colors.Warning);
            }
        }
    }
}