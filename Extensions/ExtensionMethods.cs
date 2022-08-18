using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using VSSystem.Net;

namespace ProxyAPI
{
    public static class ExtensionMethods
    {
        const int BUFFER_SIZE = 16384;
        const int BUFFER_SIZE_1M = 1048576;

        #region Request
        public static async Task<byte[]> GetRequestBytesAsync(this object sender, HttpContext context)
        {
            byte[] result = new byte[0];

            try
            {
                using (var s = new MemoryStream(Convert.ToInt32(context.Request.ContentLength)))
                {
                    await context.Request.Body.CopyToAsync(s);

                    s.Close();
                    s.Dispose();

                    result = s.ToArray();
                }
            }
            catch { }
            return result;
        }

        public static async Task<string> GetRequestStringAsync(this object sender, HttpContext context, Encoding encoding = default)
        {
            if (encoding == null)
            {
                encoding = Encoding.ASCII;
            }
            string result = string.Empty;

            try
            {
                byte[] dataBytes = await GetRequestBytesAsync(sender, context);
                result = encoding.GetString(dataBytes);
            }
            catch { }
            return result;
        }
        public static async Task<TRequest> GetRequestObject<TRequest>(this object sender, HttpContext context, Encoding encoding = null)
        {

            try
            {
                string requestString = await GetRequestStringAsync(sender, context, encoding);
                var result = JsonConvert.DeserializeObject<TRequest>(requestString);
                return result;
            }
            catch { }
            return default;
        }

        #endregion

        #region Response

        #region Async
        static async Task ResponseEmptysAsync(this object sender, HttpContext context, string contentType, HttpStatusCode statusCode, List<KeyValuePair<string, string>> headers = null)
        {
            try
            {
                context.Response.ContentLength = 0;
                context.Response.StatusCode = (int)statusCode;
                if (headers?.Count > 0)
                {
                    foreach (var header in headers)
                    {
                        try
                        {
                            context.Response.Headers.Add(header.Key, header.Value);
                        }
                        catch { }
                    }
                }
                context.Response.ContentType = contentType;
                await context.Response.WriteAsync("", context.RequestAborted);
            }
            catch { }
            await context.Response.Body.DisposeAsync();
        }
        static async Task ResponseBytesAsync(this object sender, HttpContext context, byte[] data, string contentType, HttpStatusCode statusCode, List<KeyValuePair<string, string>> headers = null)
        {
            try
            {
                context.Response.ContentLength = data.Length;
                context.Response.StatusCode = (int)statusCode;
                if (headers?.Count > 0)
                {
                    foreach (var header in headers)
                    {
                        try
                        {
                            context.Response.Headers.Add(header.Key, header.Value);
                        }
                        catch { }
                    }
                }
                context.Response.ContentType = contentType;
                await context.Response.Body.WriteAsync(data, 0, data.Length, context.RequestAborted);
            }
            catch { }
            await context.Response.Body.DisposeAsync();
        }
        public static async Task ResponseJsonAsync(this object sender, HttpContext context, object obj, HttpStatusCode statusCode, List<KeyValuePair<string, string>> headers = null)
        {
            try
            {
                if (obj == null)
                {
                    await ResponseEmptysAsync(sender, context, VSSystem.Net.ContentType.Json, statusCode, headers);
                    return;
                }
                string jsonResponse = JsonConvert.SerializeObject(obj);
                var dataBytes = Encoding.UTF8.GetBytes(jsonResponse);
                await ResponseBytesAsync(sender, context, dataBytes, VSSystem.Net.ContentType.Json, statusCode, headers);
            }
            catch { }
            await context.Response.Body.DisposeAsync();
        }
        public static async Task ResponseZipAsync(this object sender, HttpContext context, byte[] zipBytes, HttpStatusCode statusCode, List<KeyValuePair<string, string>> headers = null)
        {
            try
            {
                await ResponseBytesAsync(sender, context, zipBytes, VSSystem.Net.ContentType.Zip, statusCode, headers);
            }
            catch { }
            await context.Response.Body.DisposeAsync();
        }
        public static async Task ResponseStreamAsync(this object sender, HttpContext context, byte[] streamBytes, HttpStatusCode statusCode, List<KeyValuePair<string, string>> headers = null)
        {
            try
            {
                await ResponseBytesAsync(sender, context, streamBytes, VSSystem.Net.ContentType.Stream, statusCode, headers);
            }
            catch { }
            await context.Response.Body.DisposeAsync();
        }
        public static async Task ResponseStreamAsync(this object sender, HttpContext context, Stream s, string contentType, HttpStatusCode statusCode, List<KeyValuePair<string, string>> headers = null)
        {
            try
            {
                context.Response.ContentLength = s.Length;
                context.Response.ContentType = contentType;
                context.Response.StatusCode = (int)statusCode;
                int ret = -1;
                if (headers?.Count > 0)
                {
                    foreach (var header in headers)
                    {
                        context.Response.Headers.Add(header.Key, header.Value);
                    }
                }
                do
                {
                    byte[] buff = new byte[BUFFER_SIZE];
                    ret = await s.ReadAsync(buff, context.RequestAborted);
                    if (ret > 0)
                    {
                        await context.Response.Body.WriteAsync(buff, 0, ret, context.RequestAborted);
                    }
                } while (ret > 0);

                await context.Response.Body.DisposeAsync();
            }
            catch { }
            await context.Response.Body.DisposeAsync();
        }
        public static async Task ResponseTextAsync(this object sender, HttpContext context, string text, string contentType, HttpStatusCode statusCode, Encoding encoding = default, List<KeyValuePair<string, string>> headers = null)
        {
            try
            {
                if (encoding == null)
                {
                    encoding = Encoding.UTF8;
                }
                context.Response.ContentLength = text.Length;
                if (headers?.Count > 0)
                {
                    foreach (var header in headers)
                    {
                        try
                        {
                            context.Response.Headers.Add(header.Key, header.Value);
                        }
                        catch { }
                    }
                }
                context.Response.ContentType = contentType;
                await context.Response.WriteAsync(text, encoding, context.RequestAborted);
                await context.Response.Body.DisposeAsync();
            }
            catch { }
            await context.Response.Body.DisposeAsync();
        }
        public static async Task ResponsePngImageAsync(this object sender, HttpContext context, byte[] imageBytes, HttpStatusCode statusCode, List<KeyValuePair<string, string>> headers = null)
        {
            try
            {
                await ResponseBytesAsync(sender, context, imageBytes, VSSystem.Net.ContentType.Png, statusCode, headers);
            }
            catch { }
            await context.Response.Body.DisposeAsync();
        }
        public static async Task ResponseJpgImageAsync(this object sender, HttpContext context, byte[] imageBytes, HttpStatusCode statusCode, List<KeyValuePair<string, string>> headers = null)
        {
            try
            {
                await ResponseBytesAsync(sender, context, imageBytes, VSSystem.Net.ContentType.Jpg, statusCode, headers);
            }
            catch { }
            await context.Response.Body.DisposeAsync();
        }
        public static async Task ResponseJpegImageAsync(this object sender, HttpContext context, byte[] imageBytes, HttpStatusCode statusCode, List<KeyValuePair<string, string>> headers = null)
        {
            try
            {
                await ResponseBytesAsync(sender, context, imageBytes, ContentType.Jpeg, statusCode, headers);
            }
            catch { }
            await context.Response.Body.DisposeAsync();
        }

        async public static Task ResponseXmlAsync(this object sender, HttpContext context, object src, HttpStatusCode statusCode, Encoding encoding = default, List<KeyValuePair<string, string>> headers = null)
        {

            try
            {
                if (src != null)
                {
                    if (encoding == null)
                        encoding = Encoding.UTF8;
                    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                    ns.Add("", "");
                    XmlWriterSettings writerSettings = new XmlWriterSettings();
                    //writerSettings.OmitXmlDeclaration = true;
                    byte[] xmlBytes = new byte[0];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        StreamWriter xmlStream;
                        xmlStream = new StreamWriter(ms, encoding);
                        var xmlWr = XmlWriter.Create(xmlStream, writerSettings);
                        XmlSerializer serializer = new XmlSerializer(src.GetType());
                        serializer.Serialize(xmlWr, src, ns);
                        ms.Close();
                        ms.Dispose();
                        xmlBytes = ms.ToArray();
                    }

                    await ResponseBytesAsync(sender, context, xmlBytes, VSSystem.Net.ContentType.Xml, statusCode, headers);
                }

            }
            catch { }
            await context.Response.Body.DisposeAsync();
        }

        #endregion

        #endregion


    }
}
