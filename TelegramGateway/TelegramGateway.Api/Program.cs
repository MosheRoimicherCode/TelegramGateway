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
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("StandaloneTestPage", policy =>
                {
                    policy.SetIsOriginAllowed(origin =>
                        string.Equals(origin, "null", StringComparison.OrdinalIgnoreCase) ||
                        origin.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(origin, "https://support.kav-medida.co.il", StringComparison.OrdinalIgnoreCase) ||
                        (Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
                         (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))))
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
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

            app.UseRouting();
            app.UseCors("StandaloneTestPage");
            //app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
