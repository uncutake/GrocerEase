using GrocerEaseFinal.Models;
using Microsoft.Maui.Controls;
using System.Net.Http.Json;

namespace GrocerEaseFinal;

[QueryProperty(nameof(UserId), "userId")]
public partial class MyRecipesPage : ContentPage
{
    private int _userId;

    public int UserId
    {
        get => _userId;
        set
        {
            _userId = value;
            _ = LoadFavoritesAsync();
        }
    }

    public List<RecipeDto> Recipes { get; set; } = new();

    public MyRecipesPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private async Task LoadFavoritesAsync()
    {
        if (_userId <= 0)
            return;

        try
        {
            var url = $"http://localhost:5111/favorites/{_userId}";
            using var http = new HttpClient();
            var result = await http.GetFromJsonAsync<List<RecipeDto>>(url);

            Recipes = result ?? new List<RecipeDto>();
            OnPropertyChanged(nameof(Recipes));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Unable to load favorites:\n{ex.Message}", "OK");
        }
    }

    private async void OnRecipeSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is not RecipeDto selected)
            return;

        ((CollectionView)sender).SelectedItem = null;

        var route =
            $"{nameof(RecipeDetailPage)}" +
            $"?RecipeId={selected.Id}" +
            $"&from=myrecipes" +
            $"&userId={_userId}";

        await Shell.Current.GoToAsync(route);
    }

    private async void OnBackTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage", true);
    }

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("//MainPage", true);
        return true;
    }
}