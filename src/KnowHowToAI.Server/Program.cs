using KnowHowToAI.Server.Configuration;
using Microsoft.Extensions.Hosting;

namespace KnowHowToAI.Server;

internal static class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((ctx, services) =>
                services.AddKnowHowToAIOptions(ctx.Configuration))
            .Build();

        host.Run();
    }
}

