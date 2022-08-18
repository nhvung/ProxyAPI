using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Net;
using VSSystem.Logger;

namespace ProxyAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            CreateHostBuilder(args).Build().Run();
        }

        #region Log

        static void _InitLogger(VSSystem.Configuration.IniConfiguration _ini, IWebHostBuilder webHostBuilder)
        {
            try
            {
                string logSection = "logger";
                if (_ini != null)
                {
                    ELogMode logMode = ELogMode.None;

                    if (_ini.ReadValue<string>(logSection, "mode", "Console")?.Equals("Console", StringComparison.InvariantCultureIgnoreCase) ?? false)
                    {
                        logMode = ELogMode.Console;
                    }
                    else if (_ini.ReadValue<string>(logSection, "mode", "Console")?.Equals("File", StringComparison.InvariantCultureIgnoreCase) ?? false)
                    {
                        logMode = ELogMode.File;
                    }
                    else if (_ini.ReadValue<string>(logSection, "mode", "Console")?.Equals("Api", StringComparison.InvariantCultureIgnoreCase) ?? false)
                    {
                        logMode = ELogMode.Api;
                    }

                    ELogLevel logLevel = ELogLevel.None;
                    if (_ini.ReadValue<string>(logSection, "info", "1")?.Equals("1") ?? false)
                    {
                        logLevel = logLevel | ELogLevel.Info;
                    }
                    if (_ini.ReadValue<string>(logSection, "debug", "1")?.Equals("1") ?? false)
                    {
                        logLevel = logLevel | ELogLevel.Debug;
                    }
                    if (_ini.ReadValue<string>(logSection, "warning", "1")?.Equals("1") ?? false)
                    {
                        logLevel = logLevel | ELogLevel.Warning;
                    }
                    if (_ini.ReadValue<string>(logSection, "error", "1")?.Equals("1") ?? false)
                    {
                        logLevel = logLevel | ELogLevel.Error;
                    }
                    if (_ini.ReadValue<string>(logSection, "csv", "1")?.Equals("1") ?? false)
                    {
                        logLevel = logLevel | ELogLevel.Csv;
                    }
                    if (logMode == ELogMode.Api)
                    {
                        string apiUrl = _ini.ReadValue<string>(logSection, "url");
                        WebConfig.Logger = ALogger.Create(logMode, logLevel, apiUrl);
                    }
                    else
                    {
                        WebConfig.Logger = ALogger.Create(logMode, logLevel, Directory.GetCurrentDirectory());
                    }

                    WebConfig.Logger?.Clear();
                }
            }
            catch { }
        }

        #endregion

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    try
                    {
                        VSSystem.Configuration.IniConfiguration ini = new VSSystem.Configuration.IniConfiguration(Directory.GetCurrentDirectory() + "/config.ini");

                        _InitLogger(ini, webBuilder);

                        ini.ReadAllStaticConfigs<WebConfig>("web");
                    }
                    catch { }

                    webBuilder.ConfigureKestrel(opts =>
                    {
                        opts.Limits.MaxConcurrentConnections = WebConfig.web_max_concurrent_connections;
                        opts.Limits.MaxConcurrentUpgradedConnections = WebConfig.web_max_concurrent_connections;
                        opts.Limits.MaxRequestBodySize = long.MaxValue;

                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
