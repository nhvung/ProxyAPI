using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using ProxyAPI.Response;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using VSSystem.Logger.Extensions;

namespace ProxyAPI.Controllers
{
    public static class Proxy64Controller
    {
        const int _BUFFER_SIZE = 16384;
        const string _LOGNAME = "Proxy64Controller";
        public async static Task Process(HttpContext context)
        {
            DateTime requestTime = DateTime.UtcNow;
            bool xmlContentType = context.Request.ContentType?.IndexOf("json", StringComparison.InvariantCultureIgnoreCase) < 0;
            string path = context.Request.Path;
            string originalUrl = string.Format("{0}{1}", path, context.Request.QueryString);
            double processTime = 0;
            long contentLength = 0; // in Bytes
            try
            {
                while (path.StartsWith("/"))
                {
                    path = path.Substring(1);
                }
                if (path.IndexOf("favicon.ico", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {

                }
                else
                {
                    string baseUrl = context.Request.Headers["ProxyBaseUrl"];
                    if (string.IsNullOrWhiteSpace(baseUrl))
                    {
                        baseUrl = context.Request.GetEncodedUrl();
                    }


                    string newUrl = string.Format("{0}/{1}{2}", WebConfig.ApiUrl, path, context.Request.QueryString);

                    HttpClientHandler handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = delegate { return true; };

                    var client = new HttpClient(handler);
                    client.Timeout = new TimeSpan(0, 0, WebConfig.DefaultTimeout);

                    HttpRequestMessage rMess = new HttpRequestMessage(new HttpMethod(context.Request.Method), newUrl);
                    rMess.Headers.Add("ProxyBaseUrl", baseUrl);
                    if (context.Request.Headers?.Count > 0)
                    {
                        foreach (var header in context.Request.Headers)
                        {
                            foreach (var value in header.Value)
                            {
                                try
                                {
                                    rMess.Headers.Add(header.Key, HttpUtility.HtmlEncode(value));
                                }
                                catch { }
                            }
                        }
                    }
                    if (context.Request.ContentLength > 0)
                    {
                        rMess.Content = new StreamContent(context.Request.Body);
                        rMess.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(context.Request.ContentType);
                    }

                    var response = await client.SendAsync(rMess);
                    try
                    {
                        context.Response.Headers.Clear();
                        if (response.Content?.Headers?.Count() > 0)
                        {
                            foreach (var header in response.Content.Headers)
                            {
                                foreach (var value in header.Value)
                                {
                                    context.Response.Headers.Add(header.Key, HttpUtility.HtmlEncode(value));
                                }
                                if (header.Key.Equals("ResponseTimeInMiliseconds", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    double.TryParse(header.Value.ToString(), out processTime);
                                }
                            }
                        }

                        context.Response.StatusCode = (int)response.StatusCode;
                        context.Response.ContentLength = response.Content.Headers.ContentLength;
                        context.Response.ContentType = response.Content.Headers.ContentType.ToString();

                        await response.Content.CopyToAsync(context.Response.Body);
                    }
                    catch (Exception ex)
                    {
                        xmlContentType.LogError(WebConfig.Logger, _LOGNAME, ex, WebConfig.web_component_name);
                        if (xmlContentType)
                        {
                            await ex.ResponseXmlAsync(context, new DefaultResponse(HttpStatusCode.InternalServerError, ex.Message), System.Net.HttpStatusCode.InternalServerError);
                        }
                        else
                        {
                            await ex.ResponseJsonAsync(context, new DefaultResponse(HttpStatusCode.InternalServerError, ex.Message), System.Net.HttpStatusCode.InternalServerError);
                        }
                    }

                    client.Dispose();

                }
            }
            catch (WebException ex)
            {
                try
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)ex.Response;
                    if (httpResponse != null)
                    {
                        context.Response.Headers.Clear();
                        foreach (var headerKey in httpResponse.Headers.AllKeys)
                        {
                            context.Response.Headers.Add(headerKey, HttpUtility.HtmlEncode(httpResponse.Headers[headerKey]));
                        }
                        context.Response.ContentType = httpResponse.ContentType;
                        context.Response.ContentLength = httpResponse.ContentLength;

                        using (var resStream = httpResponse.GetResponseStream())
                        {
                            await resStream.CopyToAsync(context.Response.Body, context.RequestAborted);

                            resStream.Close();
                            resStream.Dispose();
                        }
                        httpResponse.Close();
                        httpResponse.Dispose();
                    }
                    else
                    {
                        xmlContentType.LogError(WebConfig.Logger, _LOGNAME, ex, WebConfig.web_component_name);
                        if (xmlContentType)
                        {
                            await ex.ResponseXmlAsync(context, new DefaultResponse(HttpStatusCode.InternalServerError, ex.Message), System.Net.HttpStatusCode.InternalServerError);
                        }
                        else
                        {
                            await ex.ResponseJsonAsync(context, new DefaultResponse(HttpStatusCode.InternalServerError, ex.Message), System.Net.HttpStatusCode.InternalServerError);
                        }
                    }
                }
                catch (Exception ex2)
                {
                    xmlContentType.LogError(WebConfig.Logger, _LOGNAME, ex2, WebConfig.web_component_name);
                    if (xmlContentType)
                    {
                        await ex2.ResponseXmlAsync(context, new DefaultResponse(HttpStatusCode.InternalServerError, ex2.Message), System.Net.HttpStatusCode.InternalServerError);
                    }
                    else
                    {
                        await ex2.ResponseJsonAsync(context, new DefaultResponse(HttpStatusCode.InternalServerError, ex2.Message), System.Net.HttpStatusCode.InternalServerError);
                    }
                }

            }
            catch (Exception ex)
            {
                xmlContentType.LogError(WebConfig.Logger, _LOGNAME, ex, WebConfig.web_component_name);
                if (xmlContentType)
                {
                    await ex.ResponseXmlAsync(context, new DefaultResponse(HttpStatusCode.InternalServerError, ex.Message), System.Net.HttpStatusCode.InternalServerError);
                }
                else
                {
                    await ex.ResponseJsonAsync(context, new DefaultResponse(HttpStatusCode.InternalServerError, ex.Message), System.Net.HttpStatusCode.InternalServerError);
                }
            }

            DateTime finishTime = DateTime.UtcNow;

            TimeSpan ts = finishTime - requestTime;
            double totalTime = ts.TotalMilliseconds;

            double speed = -1;
            string[] speedUnit = new string[] { "bps", "Kbps", "Mbps", "Gbps" };
            int unitIdx = 0;
            if (totalTime > 0 && contentLength > 0)
            {
                speed = contentLength * 8.0 / ts.TotalSeconds; // Bis/s
                while (speed > 1024)
                {
                    speed /= 1024;
                    unitIdx++;
                }
            }

            try
            {
                //context.LogDebug(WebConfig.Logger, _LOGNAME, $"Total transfer time: {totalTime.ToString("#.###")}ms. Total process time: {processTime.ToString("#.###")}ms. Speed: {speed.ToString("#.###")}{speedUnit[unitIdx]}. Path: {originalUrl}", WebConfig.web_component_name);
                context.LogCsv(WebConfig.Logger, _LOGNAME, WebConfig.TimeHeaders, new string[] { $"\"{processTime.ToString("#.###")}\"", $"\"{totalTime.ToString("#.###")}\"", $"{contentLength}", $"\"{speed.ToString("#.###")}{speedUnit[unitIdx]}\"", $"\"{originalUrl}\"" }, WebConfig.web_component_name);
            }
            catch { }
        }
    }
}
