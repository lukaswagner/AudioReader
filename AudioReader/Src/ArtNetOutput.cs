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

        private static List<ArtNetDevice> devices = new List<ArtNetDevice>();

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

            int i = 1;
            while(Config.NodeExists("artnet/devices/device[" + i + "]"))
            {
                Config.Get<string>("artnet/devices/device[" + i + "]/ip", out var ip);
                Config.Get<uint>(  "artnet/devices/device[" + i + "]/width_px", out var width_px);
                Config.Get<uint>(  "artnet/devices/device[" + i + "]/height_px", out var height_px);
                Config.Get<float>( "artnet/devices/device[" + i + "]/width_m", out var width_m);
                Config.Get<float>( "artnet/devices/device[" + i + "]/height_m", out var height_m);
                Config.Get<bool>(  "artnet/devices/device[" + i + "]/patch_mode/snake", out var snake);
                Config.Get<string>("artnet/devices/device[" + i + "]/patch_mode/direction", out var direction);
                Config.Get<string>("artnet/devices/device[" + i + "]/patch_mode/start_x", out var start_x);
                Config.Get<string>("artnet/devices/device[" + i + "]/patch_mode/start_y", out var start_y);

                var device = new ArtNetDevice(ip, width_px, height_px, width_m, height_m, snake, direction, start_x, start_y);
                devices.Add(device);
                i++;
            }

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
                    foreach (var device in devices)
                        device.Send(s_artnet);
                }
            }
        }
    }
}
