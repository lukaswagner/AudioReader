using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Q42.HueApi;
using Q42.HueApi.Models.Bridge;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Groups;
using System.Threading;

namespace AudioReader
{
    class HueController
    {
        private string _key;
        private HttpBridgeLocator locator = new HttpBridgeLocator();
        private string ip;
        Random rnd = new Random();
        ILocalHueClient client;
        IEnumerable<Group> groups;
        LightCommand beatCommand;
        LightCommand defaultCommand;
        public HueController(Visualization vis)
        {
            vis.BeatDetected += new BeatEventHandler(BeatDetected);
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

            client = new LocalHueClient(ip);

            Dictionary<string, string> hueKeyConfig = IniParser.GetSectionParameter("philips_hue");
            if (!hueKeyConfig.TryGetValue("key", out _key))
            {
                _key = client.RegisterAsync("mypersonalappname", "mydevicename").GetAwaiter().GetResult();
                Console.WriteLine("new key = " + _key);
            }
            else
                Console.WriteLine("key = " + _key);
            _key = "BF2f0oKYa4yj0oheEf4KUQuSZTs5ASROmdTOE7nU";
            client.Initialize(_key);

            IEnumerable<Light> lights = client.GetLightsAsync().GetAwaiter().GetResult();
            foreach (var light in lights)
            {
                Console.WriteLine(light.Name);
            }

            beatCommand = new LightCommand();
            beatCommand.Brightness = 255;
            beatCommand.TransitionTime = new TimeSpan(0);
            beatCommand.Saturation = 200;

            defaultCommand = new LightCommand();
            defaultCommand.Brightness = 20;
            defaultCommand.TransitionTime = new TimeSpan(0);
            defaultCommand.Saturation = 255;

            groups = client.GetGroupsAsync().GetAwaiter().GetResult().Where((g) => g.Type == GroupType.Room);
            foreach (var group in groups)
            {
                int index = rnd.Next(group.Lights.Count());
                pulseLight(group.Lights[index]);
            }
        }

        private void BeatDetected(object sender, EventArgs e)
        {
            foreach (var group in groups)
            {
                int index = rnd.Next(group.Lights.Count());
                pulseLight(group.Lights[index]);
            }
        }

        private async void pulseLight(string light)
        {
            await client.SendCommandAsync(beatCommand, new List<string> { light });
            Thread.Sleep(5);
            await client.SendCommandAsync(defaultCommand, new List<string> { light });
        }
    }
}
