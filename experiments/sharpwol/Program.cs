using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;

namespace sharpwol
{
    class Program
    {
        public Program(string[] args)
        {
            Mac = MacToBytes(args[0]);
        }
        static int Main(string[] args)
        {
            try
            {
                TraceConfigure();
                if (!ValidArguments(args))
                {
                    Usage();
                    return 1;
                }
                var program = new Program(args);
                return program.Run();
            }
            catch (Exception e)
            {
                Trace.TraceError("running Wake-On-LAN failed:");
                Trace.TraceError(e.Message);
                Usage();
            }
            return 1;
        }

        private static bool ValidArguments(string[] args)
        {
            if (args.Length != 1)
            {
                return false;
            }
            return true;
        }

        private static void TraceConfigure()
        {
            System.Diagnostics.TraceSwitch general = new TraceSwitch("General", "General Application Messages");
        }

        private static void Usage()
        {
            const string msg =
@"usage: sharpwol MACADDRESS
Send a message to wake a network connected device. Wake-On-LAN operates on
a link level so it only knows about the device address usually referred to
as a Mac Address. It is formatted as 6 hexadecimal numbers separated by a 
colon.

    MACADDRESS  The network device to send a wake command to. Has a format of
                6 hexidecimal numbers separated by colons.
                12:34:56:78:90:AB
";
            Trace.WriteLine("always", msg);
        }

        public int Run()
        {
            try
            {
                var broadcastEndpoint = BroadcastEndPoint();
                var client = BroadcastClient();
                var wolPacket = MagicPacket();

                SendWolPacket(broadcastEndpoint, client, wolPacket);
            }
            catch (Exception e)
            {
                Trace.TraceError("encountered a problem");
                Trace.TraceError(e.Message);
            }
            return 0;
        }

        private static void SendWolPacket(IPEndPoint broadcastEndpoint, System.Net.Sockets.UdpClient client, byte[] wolPacket)
        {
            var result = client.Send(wolPacket, wolPacket.Length, broadcastEndpoint);

            if (result != wolPacket.Length)
            {
                var msg = String.Format(
                    "Was not able to send entire packet: expected to send {0} but only {1} done.",
                    wolPacket.Length, result);
                throw new Exception(msg);
            }
        }

        private static System.Net.Sockets.UdpClient BroadcastClient()
        {
            System.Net.Sockets.UdpClient client =
                new System.Net.Sockets.UdpClient();

            client.EnableBroadcast = true;
            return client;
        }

        private byte[] MagicPacket()
        {
            byte[] magicpacket = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

            var macRepeated = RepeatMac(Mac);
            macRepeated.InsertRange(0, magicpacket);
            return macRepeated.ToArray();
        }

        private static IPEndPoint BroadcastEndPoint()
        {
            byte[] subnetBroadcast = { 0xFF, 0xFF, 0xFF, 0xFF };
            var broadcastAddress = new IPAddress(subnetBroadcast);
            var broadcastEndpoint = new IPEndPoint(broadcastAddress, 0);
            return broadcastEndpoint;
        }

        private static List<byte> RepeatMac(List<byte> mac)
        {
            List<byte> results = new List<byte>();
            for (var i = 0; i < 16; ++i)
            {
                results.AddRange(mac);
            }

            return results;
        }

        private static List<byte> MacToBytes(string macString)
        {
            var parts = macString.Split(':');
            List<byte> macBytes = new List<byte>();

            foreach (var id in parts)
            {
                byte thisId;
                if (byte.TryParse(id, System.Globalization.NumberStyles.HexNumber, null, out thisId))
                {
                    macBytes.Add(thisId);
                }
            }

            if (macBytes.Count != 6)
            {
                throw new Exception(String.Format("incorrect mac address {0}", macString));
            }

            return macBytes;
        }

        public List<byte> Mac { get; set; }
    }
}
