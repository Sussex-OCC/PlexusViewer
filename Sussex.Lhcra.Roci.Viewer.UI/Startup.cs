using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Sussex.Lchra.AzureServiceBusMessageBroker.Publisher.PublisherTypes;
using Sussex.Lchra.MessageBroker.Common.Configurations;
using Sussex.Lhcra.Common.AzureADServices;
using Sussex.Lhcra.Common.AzureADServices.Interfaces;
using Sussex.Lhcra.Common.ClientServices;
using Sussex.Lhcra.Common.ClientServices.Audit;
using Sussex.Lhcra.Common.ClientServices.Interfaces;
using Sussex.Lhcra.Common.ClientServices.Logging;
using Sussex.Lhcra.Common.Domain.Audit.Services;
using Sussex.Lhcra.Common.Domain.Logging.Services;
using Sussex.Lhcra.Roci.Viewer.DataServices;
using Sussex.Lhcra.Roci.Viewer.DataServices.Models;
using Sussex.Lhcra.Roci.Viewer.Domain.Interfaces;
using Sussex.Lhcra.Roci.Viewer.Services;
using Sussex.Lhcra.Roci.Viewer.Services.Configurations;
using Sussex.Lhcra.Roci.Viewer.Services.Core;
using Sussex.Lhcra.Roci.Viewer.UI.Configurations;
using Sussex.Lhcra.Roci.Viewer.UI.EmbeddedMode;
using Sussex.Lhcra.Roci.Viewer.UI.Helpers;
using Sussex.Lhcra.Roci.Viewer.UI.Helpers.Core;
using System;
using System.Net.Http;

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
            var config = Configuration.GetSection("ViewerAppSettings").Get<ViewerAppSettingsConfiguration>();


            if (Configuration.GetValue<bool>("EmbeddedMode") == false)
            {
                services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAD"))
                    .EnableTokenAcquisitionToCallDownstreamApi()
                    .AddInMemoryTokenCaches();

                services.AddAzureADServices();              

            }
            else
            {
                services.AddScoped<ITokenService, EmbeddedTokenService>();

                services.AddHttpClient<IDownStreamAuthorisation, ADDownStreamAuthorisation>();

                //services.AddSingleton<ICertificateProvider, LocalCertificateProvider>();

                services.AddSingleton<ICertificateProvider>(provider =>
                new AzureCertificateProvider(config.KeyVault.KeyVaultUrl, config.KeyVault.KeyVaultclientId, config.KeyVault.KeyVaultclientSecret));
            }

            services.AddTransient<CertificateHttpClientHandler>();

            services.Configure<ViewerAppSettingsConfiguration>(Configuration.GetSection("ViewerAppSettings"));
            services.Configure<LoggingDataServiceConfig>(Configuration.GetSection("AppLogDataServiceConfig"));
            services.Configure<RociGatewayADSetting>(Configuration.GetSection(nameof(RociGatewayADSetting)));
            services.Configure<EmbeddedTokenConfig>(Configuration.GetSection(nameof(EmbeddedTokenConfig)));



            services.AddHttpClient<IAuditDataService, AuditDataService>();

            services.AddHttpClient<IAppLogDataService, AppLogDataService>();

            services.AddHttpClient<IRociGatewayDataService, RociGatewayDataService>()
                     .AddHttpMessageHandler<CertificateHttpClientHandler>();

            services.AddScoped<IIpAddressProvider, IpAddressProvider>();


            services.Configure<ClientCertificateConfig>(Configuration.GetSection(nameof(ClientCertificateConfig)));

            //services.AddSingleton<ICacheService>(provider => new CacheService(config.DatabaseConnectionStrings.RedisCacheConnectionString));
            var loggingConfig = new MessageBrokerTopicConfig();
            var loggingSection = Configuration.GetSection("LogRecordTopicServiceBusConfig");
            loggingSection.Bind(loggingConfig);
            var logMessageBrokerTopicPublisher = new TopicPublisher(loggingConfig);
            services.AddScoped<ILoggingTopicPublisher>(x => new LoggingTopicPublisher(logMessageBrokerTopicPublisher));



            services.AddHttpClient<ISmspProxyDataService, SmspProxyDataService>(client =>
            {
                client.BaseAddress = new Uri(config.ProxyEndpoints.SpineMiniServicesEndpoint);
            }).AddHttpMessageHandler<CertificateHttpClientHandler>();

            services.AddScoped<SessionTimeout>();

            services.AddDistributedMemoryCache();

            var to = config.SessionTimeout;

            services.AddSession(options =>
            {
                options.Cookie.Name = ".Sussex.LHCRA.Roci.Session";
                options.IdleTimeout = TimeSpan.FromSeconds(config.SessionTimeout);
                options.Cookie.IsEssential = true;
            });

            //services.AddDistributedRedisCache(o =>
            //{
            //    o.Configuration = Configuration.GetConnectionString(redisConn);
            //});

            services.AddControllersWithViews();

            services.AddRazorPages()
             .AddMicrosoftIdentityUI();
            services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_CONNECTIONSTRING"]);

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();

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
                var mappController = endpoints.MapControllerRoute(
                     name: "default",
                     pattern: "{controller=Home}/{action=Index}/{id?}");

                if (Configuration.GetValue<bool>("EmbeddedMode") == false)
                {
                    mappController.RequireAuthorization();
                }
            });
        }
    }
}
