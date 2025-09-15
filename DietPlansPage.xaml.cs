using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace GrocerEaseFinal;

public partial class DietPlansPage : ContentPage, IQueryAttributable
{
    public string Plan { get; set; } = "";
    public string PageTitle => $"{Plan} Recipes";
    public ObservableCollection<string> Recipes { get; } = new();

    public DietPlansPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    // Receives query params from Shell navigation
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Plan", out var planObj) && planObj is string plan)
        {
            Plan = plan;
            LoadRecipes(plan);
            OnPropertyChanged(nameof(PageTitle));
        }
    }

    private void LoadRecipes(string plan)
    {
        // TODO: replace with DB call later, e.g. await _repo.GetRecipesByDietAsync(plan)
        var sample = plan switch
        {
            "High-Protein" => new[] { "Grilled Chicken Bowl", "Tofu Scramble", "Tuna Steak Salad" },
            "Low-Carb" => new[] { "Zucchini Noodles Pesto", "Cauli Rice Stir-Fry", "Avocado Egg Cups" },
            "Balanced" => new[] { "Salmon + Quinoa Plate", "Turkey Wrap", "Veggie Omelet + Toast" },
            "Plant-Based" => new[] { "Chickpea Curry", "Lentil Bolognese", "Miso Tofu Bowl" },
            _ => Array.Empty<string>()
        };
        Recipes.Clear();
        foreach (var r in sample) Recipes.Add(r);
    }
}