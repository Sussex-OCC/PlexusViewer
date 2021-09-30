using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;

namespace Sussex.Lhcra.Roci.Viewer.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
                //.ConfigureServices((hostContext, services) =>
                //{
                //    services.AddTransient<IAuthenticationProvider, Services.GraphAuthenticationProvider>();
                //    //services.AddTransient<IGraphServiceClient, GraphServiceClient>();
                //   // services.AddTransient<IGraphProvider, MicrosoftGraphProvider>();
                //});
    }
}
