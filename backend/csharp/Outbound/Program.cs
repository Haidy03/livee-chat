using HelperLib;
using HelperLib.Services;
using Outbound.Extensions;

namespace Outbound
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
                builder.Services.AddHelperServices(builder.Configuration);
                Infrastructure.Ami.AmiTrace.Configure(builder.Configuration.GetValue<bool>("AmiTrace:Enabled"));
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