namespace GrocerEaseFinal;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(DietPlansPage), typeof(DietPlansPage));
        Routing.RegisterRoute(nameof(RecipeDetailPage), typeof(RecipeDetailPage));
        Routing.RegisterRoute(nameof(GroceryListsPage), typeof(GroceryListsPage));
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(MyRecipesPage), typeof(MyRecipesPage));
    }
}

