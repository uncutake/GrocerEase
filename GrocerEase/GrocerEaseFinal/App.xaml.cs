namespace GrocerEaseFinal;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);

#if MACCATALYST
        window.Created += (s, e) =>
        {
            try
            {
                var nativeWindow = window?.Handler?.PlatformView as UIKit.UIWindow;
                if (nativeWindow is not null)
                {
                    // Keep current top-left, change size only
                    var frame = nativeWindow.Frame;
                    nativeWindow.Frame = new CoreGraphics.CGRect(
                        frame.X, frame.Y, 1400, 1000);
                }

                // Also ensure size restrictions are applied (defensive)
                var scene = (nativeWindow?.WindowScene);
                if (scene is not null)
                {
                    var fixedSize = new CoreGraphics.CGSize(1400, 1000);
                    scene.SizeRestrictions.MinimumSize = fixedSize;
                    scene.SizeRestrictions.MaximumSize = fixedSize;
                }
            }
            catch
            {
                // Ignore if APIs vary; window will still be locked by size restrictions
            }
        };
#endif

        return window;
    }
}
