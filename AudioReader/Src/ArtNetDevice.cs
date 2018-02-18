using ArtNet.Packets;
using ArtNet.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AudioReader
{
    class ArtNetDevice
    {
        private IPEndPoint _endPoint;

        private uint _width_px = 0;
        private uint _height_px = 0;

        private float _width_m = 0.0f;
        private float _height_m = 0.0f;

        private uint[] _ledIds; //which led is at this pixel

        uint pos = 0; //TODO: remove; just for testing

        private OutputTexture _textureByteArray;

        public ArtNetDevice(
            string ip,
            uint width_px, uint height_px,
            float width_m, float height_m,
            bool snake, string direction, string start_x, string start_y)
        {
            _width_px = width_px;
            _height_px = height_px;
            _width_m = width_m;
            _height_m = height_m;

            var destIPBytes = IPAddress.Parse(ip).GetAddressBytes();
            long ipLong = 0;

            long fac = 1;
            foreach (var b in destIPBytes)
            {
                ipLong += b * fac;
                fac *= 0x100;
            }
            _endPoint = new IPEndPoint(ipLong, 6454);

            _ledIds = new uint[width_px * height_px];

            _textureByteArray = GlslRenderer.Instance.RequestByteArray((int)height_px, (int)width_px);

            //Log.Debug("ArtNet", "===============");
            for (uint y = 0; y < height_px; y++)
            {
                string line = "";
                for(uint x = 0; x < width_px; x++)
                {
                    uint pixel_pos = x + y * width_px;

                    uint x_led = start_x == "left" ? x : width_px - x - 1;
                    uint y_led = start_y == "bottom" ? y : height_px - y - 1;

                    uint width = width_px;
                    uint height = height_px;

                    if (direction != "horizontal")
                    {
                        uint cache = x_led;
                        x_led = y_led;
                        y_led = cache;
                        width = height_px;
                        height = width_px;
                    }

                    if (snake)
                    {
                        if(direction == "horizontal")
                        {
                            if ((height - y) % 2 == 0)
                                x_led = width - x_led - 1;
                        }
                        else
                        {
                            if ((x) % 2 == 1)
                                x_led = width - x_led - 1;
                        }
                    }

                    _ledIds[pixel_pos] = x_led + y_led * width;
                    line += _ledIds[pixel_pos].ToString("D3") + " ";
                }
                //Log.Debug("ledIds", line);
            }
        }

        public void Send(ArtNetSocket artnet)
        {
            if (!_textureByteArray.Ready) return;

            ArtNetDmxPacket toSend = new ArtNetDmxPacket();
            long dataLength = _width_px * _height_px;
            toSend.DmxData = new byte[dataLength * 3];
            toSend.Universe = (short)1;

            /*for (uint i = 0; i < dataLength; i++) toSend.DmxData[i] = 0;
            toSend.DmxData[_ledIds[pos++] * 3] = 255;
            if (pos * 3 >= dataLength) pos = 0;/**/

            for (uint i = 0; i < dataLength; i++)
            {
                toSend.DmxData[_ledIds[i] * 3 + 0] = _textureByteArray.Data[i * 3 + 0];
                toSend.DmxData[_ledIds[i] * 3 + 1] = _textureByteArray.Data[i * 3 + 1];
                toSend.DmxData[_ledIds[i] * 3 + 2] = _textureByteArray.Data[i * 3 + 2];
            }

            artnet.SendTo(toSend.ToArray(), _endPoint);
        }

    }
}
