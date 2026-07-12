using TelegramGateway.Api.Options;
using TelegramGateway.Api.Services;
using TelegramGateway.Api.Services.DatabaseService;
using TelegramGateway.Api.Services.Orchestration;
using TelegramGateway.Api.Services.Telegram;

namespace TelegramGateway.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.Sources.Clear();
            builder.Configuration
                .SetBasePath(builder.Environment.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

            builder.Services.AddControllers();
            builder.Services.AddScoped<ISupportMessageService, SupportMessageService>();
            builder.Services.AddScoped<ISupportMessageOrchestrator, SupportMessageOrchestrator>();

            builder.Services.Configure<DatabaseServiceOptions>(builder.Configuration.GetSection("DatabaseService"));
            builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));

            builder.Services.AddSingleton<IDatabaseServicePacketFactory, DatabaseServicePacketFactory>();
            builder.Services.AddScoped<IDatabaseServiceTcpClient, DatabaseServiceTcpClient>();
            builder.Services.AddHttpClient<ITelegramBotService, TelegramBotService>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
