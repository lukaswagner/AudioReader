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
        private static string _tag = "ArtNetDevice";

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

            _textureByteArray = GlslRenderer.Instance.RequestByteArray((int)width_px, (int)height_px);

            //Log.Debug("ArtNet", "===============");

            _ledIds = _generateLedIDs(width_px, height_px, start_x == "right", start_y == "bottom", direction == "vertical", snake);
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
                toSend.DmxData[_ledIds[i] * 3 + 0] = _textureByteArray.Data[i * 4 + 0];
                toSend.DmxData[_ledIds[i] * 3 + 1] = _textureByteArray.Data[i * 4 + 1];
                toSend.DmxData[_ledIds[i] * 3 + 2] = _textureByteArray.Data[i * 4 + 2];
            }

            artnet.SendTo(toSend.ToArray(), _endPoint);
        }

        private static uint[] _generateLedIDs(uint width, uint height, bool startRight, bool startBottom, bool vertical, bool snake)
        {
            var ledIds = new uint[width * height];

            if (vertical)
            {
                bool mirrored = false;
                for (var x = 0u; x < width; x++)
                {
                    for (var y = 0u; y < height; y++)
                    {
                        ledIds[y * width + x] = (mirrored ? height - y -1 : y) + x * height;
                    }
                    if(snake)
                        mirrored = !mirrored;
                }
            }
            else
            {
                bool mirrored = false;
                for (var y = 0u; y < height; y++)
                {
                    for (var x = 0u; x < width; x++)
                    {
                        ledIds[y * width + x] = (mirrored ? width - x - 1 : x) + y * width;
                    }
                    if (snake)
                        mirrored = !mirrored;
                }
            }

            if (startRight)
            {
                var temp = new List<uint>();
                for (var y = 0; y < height; y++)
                    temp.AddRange(ledIds.Skip(y * (int)width).Take((int)width).Reverse());
                ledIds = temp.ToArray();
            }

            if (!startBottom) // OpenGL textures are inverted vertically
            {
                uint temp;
                for (var y = 0; y < height / 2; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        temp = ledIds[y * width + x];
                        ledIds[y * width + x] = ledIds[(height - y - 1) * width + x];
                        ledIds[(height - y - 1) * width + x] = temp;
                    }
                }
            }

            return ledIds;
        }

    }
}
