using Microsoft.Azure.Functions.Worker.Builder;
using Azure.Identity;
using Azure.Core;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddSingleton<TokenCredential, DefaultAzureCredential>();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
