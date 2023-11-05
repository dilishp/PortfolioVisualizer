using Microsoft.AspNetCore.Components.WebView.Maui;
using PortfolioVisualizer.Data;

namespace PortfolioVisualizer
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

            builder.Services.AddSingleton<StockTickListenerService>();
            builder.Services.AddSingleton<PortfolioManagerService>();

            return builder.Build();
        }
    }
}