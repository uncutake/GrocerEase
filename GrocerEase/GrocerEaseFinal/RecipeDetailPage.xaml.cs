using System;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using GrocerEaseFinal.Models;
using Microsoft.Maui.Controls;

namespace GrocerEaseFinal;

[QueryProperty(nameof(RecipeId), "RecipeId")]
[QueryProperty(nameof(From), "from")]
[QueryProperty(nameof(UserId), "userId")]
public partial class RecipeDetailPage : ContentPage
{
    public bool IsLoggedIn => UserSession.IsLoggedIn;
    public string StarText => _isFavorite ? "★" : "☆";

    public string? From { get; set; }
    public int UserId { get; set; }

    private readonly Services.IRecipesApi _api;
    private int _recipeId;

    public int RecipeId
    {
        get => _recipeId;
        set
        {
            if (_recipeId == value) return;
            _recipeId = value;
            _ = LoadAsync(_recipeId);
        }
    }

    private RecipeDto _recipe;
    public RecipeDto Recipe
    {
        get => _recipe;
        set { _recipe = value; OnPropertyChanged(); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public string ServingsCostText =>
        Recipe == null
            ? string.Empty
            : $"Servings: {Recipe.Servings}\tEstimated Cost: {(Recipe.EstimatedCost > 0 ? Recipe.EstimatedCost.ToString() : "-")}";

    private bool _isFavorite;
    private CancellationTokenSource _cts;

    private static T GetService<T>() where T : class =>
        Application.Current?.Handler?.MauiContext?.Services.GetService<T>()
        ?? throw new InvalidOperationException($"Service {typeof(T)} not found.");

    public RecipeDetailPage() : this(GetService<Services.IRecipesApi>()) { }

    public RecipeDetailPage(Services.IRecipesApi api)
    {
        InitializeComponent();
        _api = api;
        BindingContext = this;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
        _cts = null;
    }

    private async Task LoadAsync(int id)
    {
        if (id <= 0) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            IsBusy = true;

            Recipe = await _api.GetRecipeAsync(id);

            _isFavorite = false;

            if (UserSession.IsLoggedIn && Recipe != null)
            {
                var userId = UserSession.CurrentUser!.Id;
                _isFavorite = await _api.CheckFavorite(userId, Recipe.Id);
            }

            OnPropertyChanged(nameof(StarText));
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(ServingsCostText));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void OnToggleFavorite(object sender, EventArgs e)
    {
        if (Recipe == null) return;

        if (!UserSession.IsLoggedIn)
        {
            await DisplayAlert("Login Required", "Please log in to favorite recipes.", "OK");
            return;
        }

        try
        {
            var userId = UserSession.CurrentUser!.Id;

            if (_isFavorite)
            {
                await _api.RemoveFavorite(userId, Recipe.Id);
                _isFavorite = false;
            }
            else
            {
                await _api.AddFavorite(userId, Recipe.Id);
                _isFavorite = true;
            }

            OnPropertyChanged(nameof(StarText));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnBackTapped(object sender, EventArgs e)
    {
        if (From == "myrecipes" && UserId > 0)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(MyRecipesPage)}?userId={UserId}");
            return;
        }

        await Shell.Current.GoToAsync("..");
    }

    protected override bool OnBackButtonPressed()
    {
        if (From == "myrecipes" && UserId > 0)
        {
            _ = Shell.Current.GoToAsync(
                $"{nameof(MyRecipesPage)}?userId={UserId}");
        }
        else
        {
            _ = Shell.Current.GoToAsync("..");
        }

        return true;
    }
}
