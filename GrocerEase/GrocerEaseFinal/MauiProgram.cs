using CommunityToolkit.Maui;
using GrocerEaseFinal.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;

namespace GrocerEaseFinal;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() 
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddHttpClient<IRecipesApi, RecipesApi>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5111/");
        });

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<DietPlansPage>();
        builder.Services.AddTransient<RecipeDetailPage>();
        builder.Services.AddTransient<GroceryListsPage>();

        builder.ConfigureLifecycleEvents(events =>
        {
#if MACCATALYST
            events.AddiOS(ios =>
            {
                ios.SceneWillConnect((scene, session, options) =>
                {
                    if (scene is UIKit.UIWindowScene windowScene)
                    {
                        var size = new CoreGraphics.CGSize(1400, 1000);
                        windowScene.SizeRestrictions.MinimumSize = size;
                        windowScene.SizeRestrictions.MaximumSize = size;
                    }
                });
            });
#endif

#if WINDOWS
            events.AddWindows(w =>
            {
                w.OnWindowCreated(window =>
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                    appWindow.Resize(new Windows.Graphics.SizeInt32
                    {
                        Width = 1400,
                        Height = 1000
                    });

                    if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                    {
                        presenter.IsResizable = false;
                        presenter.IsMaximizable = false;
                    }
                });
            });
#endif
        });

        return builder.Build();
    }
}