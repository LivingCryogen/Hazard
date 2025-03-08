using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Interfaces.View;
using Shared.Interfaces.ViewModel;
using Shared.Services.Options;
using System.Windows;
using View;

namespace Bootstrap;

public class BootStrapper(ILogger<BootStrapper> logger) : IBootStrapperService
{
    private readonly ILogger _logger = logger;

    public App? MainApp { get; set; } = null;
    public string SaveFileName { get; set; } = string.Empty;

    public void InitializeGame()
    {
        if (MainApp == null)
            return;
        var viewModel = MainApp.Host.Services.GetRequiredService<IMainVM>();
        MainWindow mainWindow = new() {
            AppOptions = MainApp.Host.Services.GetRequiredService<IOptions<AppConfig>>()
        };
        mainWindow.Initialize(viewModel);
        MainApp.MainWindow = mainWindow;
        MainApp.MainWindow.Show();
    }
    public void InitializeGame(string fileName)
    {
        if (MainApp == null)
            return;
        if (string.IsNullOrEmpty(fileName))
            return;
        SaveFileName = fileName;
        MainWindow mainWindow = new() {
            AppOptions = MainApp.Host.Services.GetRequiredService<IOptions<AppConfig>>()
        };
        MainApp.MainWindow = mainWindow;

        _logger.LogInformation($"Closing old Windows...");
        foreach (Window window in Application.Current.Windows) {
            if (window == mainWindow)
                continue;
            if (window is MainWindow oldWindow)
                oldWindow.SetShutDown(false);
            window.Close();
        }

        var viewModel = MainApp.Host.Services.GetRequiredService<IMainVM>();
        _logger.LogInformation("Initializing game from source: {FileName}.", fileName);
        viewModel.Initialize([], [], fileName);
        ((MainWindow)MainApp.MainWindow).Initialize(viewModel);
        MainApp.MainWindow.Show();
    }
    public void InitializeGame((string Name, string Color)[] namesAndColors)
    {
        if (MainApp == null)
            return;
        var playerNames = namesAndColors.Select(item => item.Name).ToArray();
        var playerColors = namesAndColors.Select(item => item.Color).ToArray();
        SaveFileName = string.Empty;

        MainWindow mainWindow = new() {
            AppOptions = MainApp.Host.Services.GetRequiredService<IOptions<AppConfig>>()
        };
        MainApp.MainWindow = mainWindow;
        foreach (Window window in Application.Current.Windows) {
            if (window == mainWindow)
                continue;
            if (window is MainWindow oldWindow)
                oldWindow.SetShutDown(false);
            window.Close();
        }

        var viewModel = MainApp.Host.Services.GetRequiredService<IMainVM>();
        viewModel.Initialize(playerNames, playerColors, null);
        ((MainWindow)(MainApp.MainWindow)).Initialize(viewModel);
        MainApp.MainWindow.Show();
    }
}
