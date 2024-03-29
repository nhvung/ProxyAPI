using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ProxyAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.Run(ProxyAPI.Controllers.Proxy64Controller.Process);

            try
            {
                WebConfig.ApiUrl = Configuration.GetValue<string>("API_URL");
                string sDefaultTimeout = Configuration.GetValue<string>("DEFAULT_TIMEOUT");
                int defaultTimeout = 0;
                int.TryParse(sDefaultTimeout, out defaultTimeout);
                if (defaultTimeout <= 0)
                {
                    defaultTimeout = -1;
                }
                else
                {
                    defaultTimeout *= 1000;
                }
                WebConfig.DefaultTimeout = defaultTimeout;

                WebConfig.TimeHeaders = new string[] { "Process(ms)", "Transfer(ms)", "Content Length(bytes)", "Speed", "URL" };
            }
            catch { }
        }
    }
}
