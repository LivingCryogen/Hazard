﻿using Hazard_Share.Interfaces.View;
using Hazard_Share.Interfaces.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace Hazard_View.Services;

public class BootStrapper(App mainApp, ILogger<BootStrapper> logger) : IBootStrapperService
{
    private readonly App _mainApp = mainApp;
    private readonly ILogger _logger = logger;

    public string SaveFileName { get; set; } = string.Empty;

    public void InitializeGame()
    {
        var viewModel = _mainApp.AppHost.Services.GetRequiredService<IMainVM>();
        MainWindow mainWindow = new();
        viewModel.Initialize([(string.Empty, string.Empty)]);
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
            viewModel.Initialize(fileName);
            ((MainWindow)(_mainApp.MainWindow)).Initialize(viewModel);
            _mainApp.MainWindow.Show();
        }
    }
    public void InitializeGame((string Name, string Color)[] namesAndColors)
    {
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
        viewModel.Initialize(namesAndColors);
        ((MainWindow)(_mainApp.MainWindow)).Initialize(viewModel);
        _mainApp.MainWindow.Show();
    }
}