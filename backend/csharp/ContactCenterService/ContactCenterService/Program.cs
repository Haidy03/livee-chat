using Contact_Center.Worker.Extensions;
using HelperLib.Services;
using HelperLib;
using VoiceFlow.Infrastructure;
namespace Contact_Center.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var builder = Host.CreateApplicationBuilder(args);

                builder.Services.ConfigureOptions(builder.Configuration);
                builder.Services.AddServices();
                builder.Services.AddHostedService();
                builder.Services.AddInfrastructure(builder.Configuration);
                builder.Services.AddHelperServices(builder.Configuration);                
                var host = builder.Build();
                LoggingHelper.LogStartup(host.Services.GetRequiredService<IConfiguration>());
                host.Run();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogStartupFailure(ex);
            }
            finally
            {
                LoggingHelper.EnsureFlush();
            }
        }
    }
}