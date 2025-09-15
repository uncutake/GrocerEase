namespace GrocerEaseFinal;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(DietPlansPage), typeof(DietPlansPage));
    }
}

