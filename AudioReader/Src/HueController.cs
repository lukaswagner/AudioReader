using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Q42.HueApi;
using Q42.HueApi.Models.Bridge;
using Q42.HueApi.Interfaces;

namespace AudioReader
{
    class HueController
    {
        private string key;
        private HttpBridgeLocator locator = new HttpBridgeLocator();
        private string ip;
        ILocalHueClient client;
        public HueController()
        {
            IEnumerable<LocatedBridge> bridgeIPs = locator.LocateBridgesAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            switch (bridgeIPs.Count())
            {
                case 0:
                    Console.WriteLine("No Philips Hue Bridge found.");
                    return;
                    //break;
                case 1:
                    ip = bridgeIPs.First().IpAddress;
                    Console.WriteLine("Connecting to Philips Hue Bridge " + ip);
                    break;
                default:
                    ip = bridgeIPs.First().IpAddress;
                    Console.Write("Multiple Philips Hue Bridges found. Connecting to Philips Hue Bridge " + ip);
                    break;
            }

            ILocalHueClient client = new LocalHueClient(ip);

            Dictionary<string, string> hueKeyConfig = IniParser.GetSectionParameter("philips_hue");
            if (!hueKeyConfig.TryGetValue("key", out string key))
            {
                key = client.RegisterAsync("mypersonalappname", "mydevicename").GetAwaiter().GetResult();
                Console.WriteLine("new key = " + key);
            }
            else
                Console.WriteLine("key = " + key);
            key = "BF2f0oKYa4yj0oheEf4KUQuSZTs5ASROmdTOE7nU";
            client.Initialize(key);

            IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();
            Console.WriteLine(string.Join("\n", lights));
        }
        void init()
        {
            Bridge b;
        }
    }
}
