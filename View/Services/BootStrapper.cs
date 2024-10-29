using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Geography;
using Shared.Interfaces.Model;
using Shared.Interfaces.View;
using Shared.Interfaces.ViewModel;
using System.Windows;

namespace View.Services;

public class BootStrapper(App mainApp, ILogger<BootStrapper> logger) : IBootStrapperService
{
    private readonly App _mainApp = mainApp;
    private readonly ILogger _logger = logger;

    public string SaveFileName { get; set; } = string.Empty;

    public void InitializeGame()
    {
        var viewModel = _mainApp.AppHost.Services.GetRequiredService<IMainVM>();
        MainWindow mainWindow = new();
        mainWindow.Initialize(viewModel);
        _mainApp.MainWindow = mainWindow;
        _mainApp.MainWindow.Show();
    }
    public void InitializeGame(string fileName)
    {
        if (!string.IsNullOrEmpty(fileName)) {
            SaveFileName = fileName;

            MainWindow mainWindow = new();
            _mainApp.MainWindow = mainWindow;

            _logger.LogInformation($"Closing old Windows...");
            foreach (Window window in Application.Current.Windows) {
                if (window != mainWindow) {
                    if (window is MainWindow oldWindow)
                        oldWindow.SetShutDown(false);

                    window.Close();
                }
            }

            var viewModel = _mainApp.AppHost.Services.GetRequiredService<IMainVM>();
            _logger.LogInformation("Initializing game from source: {FileName}.", fileName);
            viewModel.Initialize([], [], fileName);
            if (viewModel.CurrentGame is IGame currentGame)
                WorldGeography.Initialize(currentGame.AssetFetcher.FetchGeography());
            ((MainWindow)_mainApp.MainWindow).Initialize(viewModel);
            _mainApp.MainWindow.Show();
        }
    }
    public void InitializeGame((string Name, string Color)[] namesAndColors)
    {
        var playerNames = namesAndColors.Select(item => item.Name).ToArray();
        var playerColors = namesAndColors.Select(item => item.Color).ToArray();
        SaveFileName = string.Empty;

        MainWindow mainWindow = new();
        _mainApp.MainWindow = mainWindow;
        foreach (Window window in Application.Current.Windows) {
            if (window != mainWindow) {
                if (window is MainWindow oldMain)
                    oldMain.SetShutDown(false);

                window.Close();
            }
        }

        var viewModel = _mainApp.AppHost.Services.GetRequiredService<IMainVM>();
        viewModel.Initialize(playerNames, playerColors, null);
        if (viewModel.CurrentGame is IGame currentGame)
            WorldGeography.Initialize(currentGame.AssetFetcher.FetchGeography());
        ((MainWindow)(_mainApp.MainWindow)).Initialize(viewModel);
        _mainApp.MainWindow.Show();
    }
}
