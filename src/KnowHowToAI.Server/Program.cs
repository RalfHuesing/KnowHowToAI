using KnowHowToAI.Server.Configuration;
using KnowHowToAI.Server.Hosting;
using Microsoft.Extensions.Hosting;

namespace KnowHowToAI.Server;

internal static class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((ctx, services) =>
            {
                services.AddKnowHowToAIOptions(ctx.Configuration);
                services.AddSqlStorage();
            })
            .Build();

        host.Run();
    }
}
