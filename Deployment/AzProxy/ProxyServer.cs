namespace AzProxy
{
    public class ProxyServer
    {
        public static void Main(string[] args)
        {
            var app = GetBuiltApp(args);
            app.UseHttpsRedirection();
            app.UseCors("FromGitHubPages");

            app.MapGet("/", () => "Proxy is up.");

            app.Run();
        }

        private static WebApplication GetBuiltApp(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddEnvironmentVariables();
            builder.Services.AddCors(options => {
                options.AddPolicy("FromGitHubPages", policy =>
                {
                    policy.WithOrigins("https://livingcryogen.github.io/Hazard")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IBanCache, BanListCache>();
            builder.Services.AddHostedService<BanListTableManager>();
            builder.Services.AddSingleton<BanService>();
            builder.Services.AddSingleton<RequestHandler>();
            return builder.Build();
        }
    }
}
