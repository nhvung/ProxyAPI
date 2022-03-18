using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using VSSystem.Hosting;
using VSSystem.Hosting.Webs.Response;
using VSSystem.Logger.Extensions;

namespace ProxyAPI.Controllers
{
    public static class ProxyController
    {
        const int _BUFFER_SIZE = 16384;
        const string _LOGNAME = "ProxyController";
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
                while(path.StartsWith("/"))
                {
                    path = path.Substring(1);
                }
                if(path.IndexOf("favicon.ico", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {

                }
                else
                {


                    string newUrl = string.Format("{0}/{1}{2}", WebConfig.ApiUrl, path, context.Request.QueryString);

                    HttpWebRequest httpRequest = WebRequest.CreateHttp(newUrl);
                    
                    httpRequest.ServerCertificateValidationCallback = delegate { return true; };
                    httpRequest.Method = context.Request.Method;
                    httpRequest.Headers.Clear();
                    if(WebConfig.DefaultTimeout > 0)
                    {
                        httpRequest.Timeout = WebConfig.DefaultTimeout;
                    }
                    
                    if (context.Request.Headers?.Count > 0)
                    {
                        foreach (var header in context.Request.Headers)
                        {
                            httpRequest.Headers.Add(header.Key, HttpUtility.HtmlEncode(header.Value));
                        }
                    }

                    httpRequest.ContentType = context.Request.ContentType;
                    if(string.IsNullOrEmpty(httpRequest.ContentType))
                    {
                        httpRequest.ContentType = "application/soap+xml";
                    }
                    httpRequest.ContentLength = context.Request.ContentLength ?? 0;
                    if(context.Request.ContentLength > 0)
                    {
                        using (var toStream = httpRequest.GetRequestStream())
                        {
                            await context.Request.Body.CopyToAsync(toStream, context.RequestAborted);
                        }
                    }
                    

                    using (var httpResponse = await httpRequest.GetResponseAsync())
                    {
                        context.Response.Headers.Clear();
                        foreach (var headerKey in httpResponse.Headers.AllKeys)
                        {
                            context.Response.Headers.Add(headerKey, HttpUtility.HtmlEncode(httpResponse.Headers[headerKey]));
                            if(headerKey.Equals("ResponseTimeInMiliseconds", StringComparison.InvariantCultureIgnoreCase))
                            {
                                double.TryParse(httpResponse.Headers[headerKey], out processTime);
                            }
                        }
                        context.Response.ContentType = httpResponse.ContentType;


                        try
                        {
                            context.Response.ContentLength = httpResponse.ContentLength;
                            contentLength = httpResponse.ContentLength;
                        }
                        catch { }

                        using (var resStream = httpResponse.GetResponseStream())
                        {
                            await resStream.CopyToAsync(context.Response.Body, context.RequestAborted);

                            resStream.Close();
                            resStream.Dispose();
                        }

                        httpResponse.Close();
                        httpResponse.Dispose();
                    }
                }
            }
            catch(WebException ex)
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
                            //int ret = -1;
                            //do
                            //{
                            //    byte[] buff = new byte[_BUFFER_SIZE];
                            //    ret = await resStream.ReadAsync(buff, 0, buff.Length, context.RequestAborted);
                            //    if (ret > 0)
                            //    {
                            //        await context.Response.Body.WriteAsync(buff, 0, ret, context.RequestAborted);
                            //    }
                            //} while (ret > 0);

                            resStream.Close();
                            resStream.Dispose();
                        }
                        httpResponse.Close();
                        httpResponse.Dispose();
                    }
                    else
                    {
                        xmlContentType.LogError(WebConfig.Logger, _LOGNAME, ex, WebConfig.web_component_name);
                        if(xmlContentType)
                        {
                            await ex.ResponseXmlAsync(context, new DefaultResponse(HttpStatusCode.InternalServerError, ex.Message), System.Net.HttpStatusCode.InternalServerError);
                        }
                        else
                        {
                            await ex.ResponseJsonAsync(context, new DefaultResponse(HttpStatusCode.InternalServerError, ex.Message), System.Net.HttpStatusCode.InternalServerError);
                        }
                    }
                }
                catch(Exception ex2)
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

            DateTime finishTime = DateTime.UtcNow;

            TimeSpan ts = finishTime - requestTime;
            double totalTime = ts.TotalMilliseconds;

            double speed = -1;
            string[] speedUnit = new string[] { "bps", "Kbps", "Mbps", "Gbps" };
            int unitIdx = 0;
            if(totalTime > 0 && contentLength > 0)
            {
                speed = contentLength * 8.0 / ts.TotalSeconds; // Bis/s
                while(speed > 1024)
                {
                    speed /= 1024;
                    unitIdx++;
                }
            }

            try
            {
                //context.LogDebug(WebConfig.Logger, _LOGNAME, $"Total transfer time: {totalTime.ToString("#.###")}ms. Total process time: {processTime.ToString("#.###")}ms. Speed: {speed.ToString("#.###")}{speedUnit[unitIdx]}. Path: {originalUrl}", WebConfig.web_component_name);
                context.LogCsv(WebConfig.Logger, _LOGNAME, WebConfig.TimeHeaders, new string[] { $"\"{processTime.ToString("#.###")}\"", $"\"{totalTime.ToString("#.###")}\"",$"{contentLength}", $"\"{speed.ToString("#.###")}{speedUnit[unitIdx]}\"", $"\"{originalUrl}\"" }, WebConfig.web_component_name);
            }
            catch { }
        }
    }
}
