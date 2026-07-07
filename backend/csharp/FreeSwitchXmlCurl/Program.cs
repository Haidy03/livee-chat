
using MongoDB.Driver;
using VoiceFlow.FreeSwitchXmlCurl.Models;
using VoiceFlow.FreeSwitchXmlCurl.Services;
using VoiceFlow.FreeSwitchXmlCurl.Settings;

namespace FreeSwitch
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
            builder.Services.Configure<FreeSwitchSettings>(builder.Configuration.GetSection("FreeSwitch"));

            builder.Services.AddSingleton<IMongoClient>(sp =>
            {
                var s = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>().Value;
                return new MongoClient(s.ConnectionString);
            });

            builder.Services.AddSingleton(sp =>
            {
                var s = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>().Value;
                return sp.GetRequiredService<IMongoClient>().GetDatabase(s.DatabaseName);
            });

            builder.Services.AddSingleton<IMongoCollection<DialplanDocument>>(sp =>
            {
                var s = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>().Value;
                return sp.GetRequiredService<IMongoDatabase>().GetCollection<DialplanDocument>(s.DialplanCollection);
            });

            builder.Services.AddSingleton<IMongoCollection<VoicemailMessage>>(sp =>
            {
                var fs = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FreeSwitchSettings>>().Value;
                return sp.GetRequiredService<IMongoDatabase>().GetCollection<VoicemailMessage>(fs.VoicemailCollectionName);
            });

            builder.Services.AddSingleton<ITemplateResolver, TemplateResolver>();
            builder.Services.AddSingleton<IXmlDialplanRenderer, XmlDialplanRenderer>();
            builder.Services.AddScoped<IDialplanService, DialplanService>();
            builder.Services.AddScoped<IVoicemailService, VoicemailService>();

            builder.Services.AddControllers();
            builder.Services.AddLogging();

            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
            app.MapControllers();

            app.Run();
        }
    }
}
