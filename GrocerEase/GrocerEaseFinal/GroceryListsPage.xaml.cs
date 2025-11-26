using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Json;
using GrocerEaseFinal.Models;
using Microsoft.Maui.Controls;

namespace GrocerEaseFinal;

[QueryProperty(nameof(User), "user")]
public partial class GroceryListsPage : ContentPage
{
    private string? _user;
    private readonly Services.IRecipesApi _recipesApi;
    private BookmarkDto? _currentList;
    private readonly HttpClient _http =
        new() { BaseAddress = new Uri("http://localhost:5111/") };

    private readonly ObservableCollection<BookmarkDto> _items = new();

    public string? User
    {
        get => _user;
        set
        {
            _user = value;
            _ = RefreshItemsAsync();
        }
    }

    private static T GetService<T>() where T : class =>
        Application.Current?.Handler?.MauiContext?.Services.GetService<T>()
        ?? throw new InvalidOperationException($"Service {typeof(T)} not found.");

    public GroceryListsPage()
    {
        InitializeComponent();

        _recipesApi = GetService<Services.IRecipesApi>();

        ListsView.ItemsSource = _items;
        ShowLists();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!ResultsView.IsVisible)
        {
            _ = RefreshItemsAsync();
        }
    }

    private async Task RefreshItemsAsync()
    {
        if (!UserSession.IsLoggedIn)
        {
            _items.Clear();
            UpdateVisibility();
            return;
        }

        try
        {
            Busy.IsVisible = true;

            var userId = UserSession.CurrentUser!.Id;
            var list = await _http.GetFromJsonAsync<List<BookmarkDto>>(
                           $"bookmarks/{userId}") ?? new();

            _items.Clear();
            foreach (var b in list)
                _items.Add(b);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Unable to load saved lists:\n{ex.Message}", "OK");
        }
        finally
        {
            Busy.IsVisible = false;
            UpdateVisibility();
        }
    }

    private void UpdateVisibility()
    {
        ListsView.IsVisible = _items.Count > 0;
        EmptyState.IsVisible = _items.Count == 0 && !Busy.IsVisible;
        ResultsView.IsVisible = false;
        ResultsView.ItemsSource = null;
    }

    private void ShowLists()
    {
        _currentList = null;
        RemoveButton.IsEnabled = false;
        UpdateVisibility();
    }

    private void ShowResults(IEnumerable itemsSource)
    {
        ListsView.IsVisible = false;
        EmptyState.IsVisible = false;

        ResultsView.IsVisible = true;
        ResultsView.ItemsSource = itemsSource;
    }

    private async void OnBookmarkTapped(object sender, EventArgs e)
    {
        if (sender is not View v || v.BindingContext is not BookmarkDto item)
            return;

        var raw = item.Text?.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return;

        try
        {
            Busy.IsVisible = true;

            var ingredients = ParseIngredients(raw);

            var allRecipes = await _recipesApi.GetRecipesAsync();

            var terms = ingredients
                .Select(i => i.Trim().ToLowerInvariant())
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Distinct()
                .ToList();

            var results = allRecipes
                .Where(r => terms.Any(term =>
                    ContainsRealIngredient(r.Ingredients ?? string.Empty, term)))
                .ToList();

            _currentList = item; 
            RemoveButton.IsEnabled = true;

            ShowResults(results);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            Busy.IsVisible = false;
        }
    }
    private async void OnRemoveSelected(object sender, EventArgs e)
    {
        if (ResultsView.IsVisible && _currentList != null)
        {
            await RemoveBookmarkAsync(_currentList);
            _currentList = null;
            RemoveButton.IsEnabled = false;
            ShowLists();
            return;
        }
        if (ListsView.SelectedItem is not BookmarkDto selected)
        {
            await DisplayAlert("Remove",
                "Please select or open a saved list to remove.",
                "OK");
            return;
        }

        await RemoveBookmarkAsync(selected);
        ListsView.SelectedItem = null;
        RemoveButton.IsEnabled = false;
    }
    private async Task RemoveBookmarkAsync(BookmarkDto target)
    {
        var confirm = await DisplayAlert(
            "Remove list",
            "Are you sure you want to remove this saved list?",
            "Yes", "No");

        if (!confirm) return;

        try
        {
            Busy.IsVisible = true;

            var response = await _http.DeleteAsync($"bookmarks/{target.Id}");

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Error",
                    "Failed to remove list from server.",
                    "OK");
                return;
            }

            await RefreshItemsAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            Busy.IsVisible = false;
        }
    }
    private async void OnResultsSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is not RecipeDto recipe)
            return;

        await Shell.Current.GoToAsync(
            $"{nameof(RecipeDetailPage)}?RecipeId={recipe.Id}");

        if (sender is CollectionView cv)
            cv.SelectedItem = null;
    }
    private async void OnBackTapped(object sender, EventArgs e)
    {
        if (ResultsView.IsVisible)
        {
            ShowLists();
            return;
        }

        await Shell.Current.Navigation.PopAsync(animated: true);
    }
    private static List<string> ParseIngredients(string raw)
    {
        return raw
            .Split(new[] { '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(part => part
                .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    private static bool ContainsRealIngredient(string ingredientsText, string term)
    {
        if (string.IsNullOrWhiteSpace(ingredientsText)) return false;

        term = term.ToLowerInvariant();

        // words that should NOT count as the ingredient
        var bannedNextWords = new[] { "broth", "stock", "powder", "bouillon", "cube" };

        var lines = ingredientsText
            .Split('\n', '\r')
            .Select(l => l.Trim().ToLowerInvariant())
            .Where(l => !string.IsNullOrWhiteSpace(l));

        foreach (var line in lines)
        {
            if (!line.Contains(term))
                continue;

            var words = line
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim(',', '.', ';', ':'))
                .ToArray();

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i] != term)
                    continue;
                if (i + 1 < words.Length && bannedNextWords.Contains(words[i + 1]))
                    continue;
                return true;
            }
        }

        return false;
    }
}