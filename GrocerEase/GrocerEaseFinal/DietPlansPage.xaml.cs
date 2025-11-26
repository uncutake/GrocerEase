using System.Collections.ObjectModel;
using GrocerEaseFinal.Models;
using GrocerEaseFinal.Services;

namespace GrocerEaseFinal;

public partial class DietPlansPage : ContentPage, IQueryAttributable
{
    private readonly IRecipesApi _api;

    public ObservableCollection<RecipeDto> Recipes { get; } = new();
    public string Plan { get; set; } = "";
    public string PageTitle => $"{Plan} Recipes";

    public DietPlansPage(IRecipesApi api)
    {
        InitializeComponent();
        _api = api;
        BindingContext = this;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Plan", out var p) && p is string plan)
        {
            Plan = plan;
            OnPropertyChanged(nameof(PageTitle));
            await LoadAsync(plan);
        }
    }

    private async Task LoadAsync(string category)
    {
        Recipes.Clear();
        var rows = await _api.GetRecipesAsync(category);
        Console.WriteLine($"DEBUG -> Loaded {rows.Count} recipes for category = {category}");

        foreach (var r in rows)
            Recipes.Add(r);
    }
    private async void OnRecipeSelected(object sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection?.FirstOrDefault() as RecipeDto;
        if (selected is null) return;

        await Shell.Current.GoToAsync(nameof(RecipeDetailPage), new Dictionary<string, object>
        {
            ["RecipeId"] = selected.Id
        });

        ((CollectionView)sender).SelectedItem = null;
    }
    private async void OnBackTapped(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopToRootAsync(animated: true);
    }
}