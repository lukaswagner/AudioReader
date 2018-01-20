using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Bridge;
using Q42.HueApi.Models.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AudioReader
{
    internal class HueController
    {
        private string _key;
        private Random _rnd = new Random();
        private ILocalHueClient _client;
        private IEnumerable<Group> _groups;

        private bool _preserveColor;

        private LightCommand _beatCommand;
        private LightCommand _defaultCommand;
        public HueController()
        {
            BeatDetection.BeatDetected += new BeatEventHandler(_beatDetected);

            HttpBridgeLocator locator = new HttpBridgeLocator();
            IEnumerable<LocatedBridge> bridgeIPs = locator.LocateBridgesAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            string ip;
            switch (bridgeIPs.Count())
            {
                case 0:
                    Log.Warn("Philips Hue", "No Philips Hue Bridge found.");
                    return;
                case 1:
                    ip = bridgeIPs.First().IpAddress;
                    Log.Info("Philips Hue", "Connecting to Philips Hue Bridge " + ip);
                    break;
                default:
                    ip = bridgeIPs.First().IpAddress;
                    Log.Info("Philips Hue", "Multiple Philips Hue Bridges found. Connecting to Philips Hue Bridge " + ip);
                    break;
            }

            _client = new LocalHueClient(ip);
            
            if (!Config.Get("philips_hue/key", out _key))
            {
                _key = _client.RegisterAsync("mypersonalappname", "mydevicename").GetAwaiter().GetResult();
                Config.Set("philips_hue/key", _key);
            }

            _client.Initialize(_key);

            _beatCommand = new LightCommand
            {
                Brightness = 255,
                TransitionTime = new TimeSpan(0),
                Saturation = 200
            };

            _defaultCommand = new LightCommand
            {
                Brightness = 20,
                TransitionTime = new TimeSpan(0),
                Saturation = 255
            };

            _preserveColor = Config.GetDefault("philips_hue/preserve_color", false);

            _groups = _client.GetGroupsAsync().GetAwaiter().GetResult().Where((g) => g.Type == GroupType.Room);
        }

        private void _beatDetected(object sender, EventArgs e)
        {
            if (_groups != null)
                foreach (var group in _groups)
                {
                    int index = _rnd.Next(group.Lights.Count());
                    _pulseLight(group.Lights[index]);
                }
        }

        private async void _pulseLight(string light)
        {
            if (!_preserveColor) _beatCommand.Hue = _rnd.Next(65535);
            await _client.SendCommandAsync(_beatCommand, new List<string> { light });
            Thread.Sleep(5);
            await _client.SendCommandAsync(_defaultCommand, new List<string> { light });
        }

        public void TurnAllTheLightsOff()
        {
            foreach (var group in _groups)
                foreach (var light in group.Lights)
                    _client.SendCommandAsync(_defaultCommand, new List<string> { light }).GetAwaiter().GetResult();
        }
    }
}
