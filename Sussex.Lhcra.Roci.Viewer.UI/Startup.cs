using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Sussex.Lhcra.Common.ClientServices.Audit;
using Sussex.Lhcra.Common.ClientServices.Logging;
using Sussex.Lhcra.Common.Domain.Audit.Services;
using Sussex.Lhcra.Common.Domain.Logging.Services;
using Sussex.Lhcra.Roci.Viewer.DataServices;
using Sussex.Lhcra.Roci.Viewer.DataServices.Models;
using Sussex.Lhcra.Roci.Viewer.UI.Configurations;
using Sussex.Lhcra.Roci.Viewer.UI.Helpers;
using System;

namespace Sussex.Lhcra.Roci.Viewer.UI
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
            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAD"))
                    .EnableTokenAcquisitionToCallDownstreamApi()
                    .AddInMemoryTokenCaches();


            services.Configure<ViewerAppSettingsConfiguration>(Configuration.GetSection("ViewerAppSettings"));

            services.Configure<AuditDataServiceConfig>(Configuration.GetSection(nameof(AuditDataServiceConfig)));

            services.Configure<LoggingDataServiceConfig>(Configuration.GetSection("AppLogDataServiceConfig"));

            services.Configure<LoggingServiceADSetting>(Configuration.GetSection(nameof(LoggingServiceADSetting)));

            services.Configure<AuditServiceADSetting>(Configuration.GetSection(nameof(AuditServiceADSetting)));

            services.Configure<RociGatewayADSetting>(Configuration.GetSection(nameof(RociGatewayADSetting)));

            services.AddHttpClient<IAuditDataService, AuditDataService>();
            services.AddHttpClient<IAppLogDataService, AppLogDataService>();
            services.AddScoped<IRociGatewayDataService, RociGatewayDataService>();
            services.AddScoped<IIpAddressProvider, IpAddressProvider>();
            services.AddScoped<ITokenService, TokenService>();

            var config = Configuration.GetSection("ViewerAppSettings").Get<ViewerAppSettingsConfiguration>();

            services.AddHttpClient<ISmspProxyDataService, SmspProxyDataService>(client =>
            {
                client.BaseAddress = new Uri(config.ProxyEndpoints.SpineMiniServicesEndpoint);
            });

            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.Cookie.Name = ".Sussex.LHCRA.Roci.Session";
                options.IdleTimeout = TimeSpan.FromSeconds(config.SessionTimeout);
                options.Cookie.IsEssential = true;
            });

            services.AddControllersWithViews();

            services.AddRazorPages()
             .AddMicrosoftIdentityUI();
            services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_CONNECTIONSTRING"]);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
