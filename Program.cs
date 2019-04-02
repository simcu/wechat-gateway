using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Threading;

namespace Wechat
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(500, 1000);
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
