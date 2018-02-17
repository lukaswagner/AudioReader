using ArtNet.Packets;
using ArtNet.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AudioReader
{
    class ArtNetOutput
    {
        private const uint SENDER_UNIVERSES = 1; // number of universes to send
        private const uint LED_COUNT = 30; // number of bytes to send
        private const uint SENDER_LENGTH = LED_COUNT * 3; // number of bytes to send

        private static ArtNetSocket s_artnet = new ArtNetSocket();
        private static IPEndPoint endPoint;
        private static DateTime _lastTime = DateTime.Now; 
        private static Thread _artNetOutputLoopThread = new Thread(_artNetOutputLoop);
        private static bool _run = false;

        public static void Enable()
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress lanAdress = null;
            foreach (var ip in localIPs)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && ip.GetAddressBytes()[0] == 192)
                    lanAdress = ip;
            }

            s_artnet.EnableBroadcast = true;
            s_artnet.Open(lanAdress, IPAddress.Parse("255.255.255.0"));
            
            var destIPBytes = IPAddress.Parse("192.168.1.100").GetAddressBytes();
            long destIP = 0;

            long fac = 1;
            foreach (var b in destIPBytes)
            {
                destIP += b * fac;
                fac *= 0x100;
            }

            endPoint = new IPEndPoint(destIP, 6454);

            _run = true;
            _artNetOutputLoopThread.Start();
        }

        private static void _artNetOutputLoop()
        {
            while (_run)
            {
                if((DateTime.Now - _lastTime).Milliseconds > 16)
                {
                    _lastTime = DateTime.Now;
                    ArtNetDmxPacket toSend = new ArtNetDmxPacket();
                    Random rnd = new Random();

                    toSend.DmxData = new byte[SENDER_LENGTH];

                    toSend.Universe = (short)1;

                    for (uint i = 0; i < SENDER_LENGTH; i++) toSend.DmxData[i] = 0;
                    toSend.DmxData[(DateTime.Now.Second / 2) * 3] = 255;

                    s_artnet.SendTo(toSend.ToArray(), endPoint);
                }
            }
        }
    }
}
