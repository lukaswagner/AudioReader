using System;
using System.IO;
using System.Net;

namespace AudioReader
{
    internal static class HttpInterface
    {
        private static string _tag = "HttpInterface";

        public static void Enable()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://*:" + Config.GetDefault("httpInterface/port", 8080) + "/");

            listener.Start();
            listener.BeginGetContext(_callback, listener);
        }

        private static void _callback(IAsyncResult result)
        {
            var listener = (HttpListener)result.AsyncState;
            var context = listener.EndGetContext(result);
            var request = context.Request;
            var response = context.Response;

            void Done(HttpStatusCode status)
            {
                response.StatusCode = (int)status;
                response.Close();
                // start listening again
                listener.BeginGetContext(_callback, listener);
            }

            var method = request.HttpMethod;
            Log.Info(_tag, "Received " + method + " request");

            if (method != "PUT")
            {
                Done(HttpStatusCode.MethodNotAllowed);
                return;
            }

            var target = request.Url.LocalPath;
            Log.Info(_tag, "Target: " + target);

            if (target != "/shader")
            {
                Done(HttpStatusCode.NotImplemented);
                return;
            }

            string content;
            using (var sr = new StreamReader(request.InputStream))
                content = sr.ReadToEnd().Trim();
            Log.Info(_tag, "Content: " + content);

            var file = "Shader/GlslSandbox/" + content + ".frag";
            if (File.Exists(file))
            {
                GlslRenderer.Instance.UpdateShader(content + ".frag");
                Done(HttpStatusCode.OK);
            }
            else
            {
                Log.Info(_tag, "Could not find file " + file);
                Done(HttpStatusCode.NotFound);
            }
        }
    }
}
