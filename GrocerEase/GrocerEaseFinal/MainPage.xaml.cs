using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace GrocerEaseFinal;

[QueryProperty(nameof(RawFromBookmark), "raw")]
public partial class MainPage : ContentPage
{
    bool _menuOpen = false;
    double _drawerWidth = 360;
    private readonly Services.IRecipesApi _recipesApi;

    private string? _pendingBookmarkText;
    private bool _savingBookmark;
    private string? _rawFromBookmark;

    public string? RawFromBookmark
    {
        get => _rawFromBookmark;
        set
        {
            _rawFromBookmark = value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                _ = ShowResultsForTextAsync(value);
            }
        }
    }

    private static T GetService<T>() where T : class =>
        Application.Current?.Handler?.MauiContext?.Services.GetService<T>()
        ?? throw new InvalidOperationException($"Service {typeof(T)} not found.");

    public MainPage() : this(GetService<Services.IRecipesApi>()) { }

    public MainPage(Services.IRecipesApi recipesApi)
    {
        InitializeComponent();
        _recipesApi = recipesApi;
        UpdateMenuForAuth();

        HomeButton.Clicked -= OnMenuHome;
        HomeButton.Clicked += OnMenuHome;

        MenuOverlay.IsVisible = false;
        MenuOverlay.InputTransparent = true;

        OverlayTapTarget.IsVisible = false;
        OverlayTapTarget.InputTransparent = true;

        WireTap(HighProteinTile, "High-Protein");
        WireTap(LowCarbTile, "Low-Carb");
        WireTap(PlantBasedTile, "Plant-Based");
        WireTap(BalancedTile, "Balanced");

        ApplyRoundedClip(HighProteinTile, 20);
        ApplyRoundedClip(LowCarbTile, 20);
        ApplyRoundedClip(PlantBasedTile, 20);
        ApplyRoundedClip(BalancedTile, 20);

        SizeChanged += MainPage_SizeChanged;
    }

    void MainPage_SizeChanged(object sender, EventArgs e)
    {
        var target = Math.Clamp(Width * 0.36, 320, 440);
        _drawerWidth = target;

        if (RightDrawerHost != null)
        {
            RightDrawerHost.WidthRequest = _drawerWidth;
            RightDrawerHost.TranslationX = _menuOpen ? 0 : _drawerWidth;
        }
    }

    private void WireTap(View tile, string plan)
    {
        if (tile == null) return;

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, __) =>
        {
            await tile.ScaleTo(0.98, 80);
            await tile.ScaleTo(1, 80);

            if (!tile.IsEnabled) return;
            tile.IsEnabled = false;
            try
            {
                await Shell.Current.GoToAsync(nameof(DietPlansPage),
                    new Dictionary<string, object> { { "Plan", plan } });
            }
            finally
            {
                tile.IsEnabled = true;
            }
        };

        tile.GestureRecognizers.Clear();
        tile.GestureRecognizers.Add(tap);
    }

    private static void ApplyRoundedClip(Border border, float radius)
    {
        if (border == null) return;
        border.SizeChanged += (_, __) =>
        {
            border.Clip = new RoundRectangleGeometry
            {
                CornerRadius = radius,
                Rect = new Rect(0, 0, border.Width, border.Height)
            };
        };
    }

    private async void OnRecipeSearch(object sender, EventArgs e)
    {
        var query = RecipeSearchBar.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(query))
        {
            ShowHome();
            return;
        }

        try
        {
            var allRecipes = await _recipesApi.GetRecipesAsync();

            var filtered = allRecipes
                .Where(r => r.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filtered.Count == 0)
            {
                await DisplayAlert("No Results", $"No recipes found for \"{query}\".", "OK");
                return;
            }

            RightResultsCollectionView.ItemsSource = filtered;
            RightResultsCollectionView.IsVisible = true;

            CategoryTilesPanel.IsVisible = false;
            CategoryScroll.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Search Error", ex.Message, "OK");
        }
    }

    private void OnRecipeSearchBarCompleted(object sender, EventArgs e) => OnRecipeSearch(sender, e);

    private async void OnGenerateClick(object sender, EventArgs e)
    {
        var raw = GroceryEditor?.Text;
        if (string.IsNullOrWhiteSpace(raw))
        {
            await DisplayAlert("No input", "Enter ingredients in the box first.", "OK");
            return;
        }
        await ShowResultsForTextAsync(raw);
    }

    private async void OnBookmarkClick(object sender, EventArgs e)
    {
        var text = GroceryEditor?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            await DisplayAlert("Nothing to save", "Enter your grocery list first.", "OK");
            return;
        }

        if (!UserSession.IsLoggedIn)
        {
            _pendingBookmarkText = text;
            await DisplayAlert("Login required", "Please log in to save this list to your bookmarks.", "OK");
            await CloseMenuAsync();
            ShowLoginOverlay();
            return;
        }

        var ok = await SaveBookmarkNowAsync(text);
        if (ok)
            await DisplayAlert("Saved", "Grocery list added to your bookmarks.", "OK");
    }

    private async void OnMenuClick(object sender, EventArgs e)
    {
        if (_menuOpen) { await CloseMenuAsync(); return; }
        await OpenMenuAsync();
    }

    private async void OnOverlayTapped(object sender, EventArgs e)
    {
        await CloseMenuAsync();
    }

    private async void OnMenuHome(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopToRootAsync(animated: false);

        ShowHome();

        await CloseMenuAsync();
    }

    private void ShowHome()
    {
        RightResultsCollectionView.IsVisible = false;
        RightResultsCollectionView.ItemsSource = null;

        CategoryScroll.IsVisible = true;
        CategoryTilesPanel.IsVisible = true;
        CategoryTilesPanel.IsEnabled = true;
        CategoryTilesPanel.InputTransparent = false;

        RecipeSearchBar.Text = string.Empty;
    }

    async void OnMenuLogin(object sender, EventArgs e)
    {
        await CloseMenuAsync();
        ShowLoginOverlay();
    }

    private async Task OpenMenuAsync()
    {
        _menuOpen = true;
        CategoryTilesPanel.IsEnabled = false;
        RightResultsCollectionView.IsEnabled = false;

        MenuOverlay.IsVisible = true;
        MenuOverlay.InputTransparent = false;
        MenuOverlay.Opacity = 1;

        OverlayTapTarget.InputTransparent = false;
        OverlayTapTarget.IsVisible = true;
        OverlayTapTarget.Opacity = 1;

        RightDrawerHost.IsEnabled = true;
        RightDrawerHost.InputTransparent = false;

        await RightDrawerHost.TranslateTo(0, 0, 220, Easing.CubicOut);
    }

    private async Task CloseMenuAsync()
    {
        if (!_menuOpen) return;

        await RightDrawerHost.TranslateTo(_drawerWidth, 0, 200, Easing.CubicIn);

        OverlayTapTarget.IsVisible = false;
        OverlayTapTarget.InputTransparent = true;
        OverlayTapTarget.Opacity = 0;

        MenuOverlay.InputTransparent = true;
        MenuOverlay.IsVisible = false;
        MenuOverlay.Opacity = 0;

        CategoryTilesPanel.IsEnabled = true;
        RightResultsCollectionView.IsEnabled = true;

        _menuOpen = false;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ResetInteractivity();
        UpdateMenuForAuth();
    }

    private void ResetInteractivity()
    {
        CategoryTilesPanel.IsEnabled = true;
        CategoryTilesPanel.InputTransparent = false;

        RightResultsCollectionView.IsEnabled = true;
        RightResultsCollectionView.InputTransparent = false;

        MenuOverlay.IsVisible = false;
        MenuOverlay.InputTransparent = true;
        MenuOverlay.Opacity = 0;

        OverlayTapTarget.IsVisible = false;
        OverlayTapTarget.InputTransparent = true;
        OverlayTapTarget.Opacity = 0;

        RightDrawerHost.IsEnabled = true;
        RightDrawerHost.InputTransparent = false;
    }

    private void ShowLoginOverlay() => LoginOverlay.IsVisible = true;
    private void HideLoginOverlay() => LoginOverlay.IsVisible = false;
    private void OnLoginOverlayTapped(object sender, EventArgs e) => HideLoginOverlay();
    private void OnLoginCancel(object sender, EventArgs e) => HideLoginOverlay();

    private async void OnLoginConfirm(object sender, EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Login", "Enter username and password", "OK");
            return;
        }

        try
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password
            };

            using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5111/") };

            var response = await http.PostAsJsonAsync("auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Login Failed", "Invalid username or password.", "OK");
                return;
            }

            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            if (user == null)
            {
                await DisplayAlert("Login Failed", "Server returned no user data.", "OK");
                return;
            }

            UserSession.SetUser(user);
            UpdateMenuForAuth();

            ShowHome();

            if (!string.IsNullOrWhiteSpace(_pendingBookmarkText))
            {
                var ok = await SaveBookmarkNowAsync(_pendingBookmarkText);
                _pendingBookmarkText = null;

                if (ok)
                    await DisplayAlert("Saved", "Your grocery list was saved to bookmarks.", "OK");
            }

            await DisplayAlert("Welcome", $"Hello, {user.Username}!", "OK");
            HideLoginOverlay();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void UpdateMenuForAuth()
    {
        var loggedIn = UserSession.IsLoggedIn;

        LoginMenuItem.IsVisible = !loggedIn;
        GroceryListsButton.IsVisible = loggedIn;
        RecipesButton.IsVisible = loggedIn;
        LogoutButton.IsVisible = loggedIn;
    }

    private async void OnMenuGroceryLists(object? sender, EventArgs e)
    {
        if (!UserSession.IsLoggedIn)
        {
            await CloseMenuAsync();
            ShowLoginOverlay();
            return;
        }

        await CloseMenuAsync();

        var user = UserSession.CurrentUser!;
        var route = $"{nameof(GroceryListsPage)}?userId={user.Id}";
        await Shell.Current.GoToAsync(route);
    }

    private async void OnMenuRecipes(object sender, EventArgs e)
    {
        if (!UserSession.IsLoggedIn)
        {
            await DisplayAlert("Login Required",
                "Please log in to view your recipes.",
                "OK");
            return;
        }

        await CloseMenuAsync();

        var user = UserSession.CurrentUser!;
        await Shell.Current.GoToAsync(
            $"{nameof(MyRecipesPage)}?userId={user.Id}");
    }

    private async void OnMenuLogout(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Log Out", "Are you sure you want to log out?", "Yes", "No");
        if (!confirm) return;

        UserSession.Logout();
        UpdateMenuForAuth();

        ShowHome();

        await CloseMenuAsync();
    }

    private async Task<bool> SaveBookmarkNowAsync(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        if (_savingBookmark) return false;

        var user = UserSession.CurrentUser;
        if (user == null || user.Id <= 0) return false;

        _savingBookmark = true;

        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5111/") };

            var dto = new CreateBookmarkDto
            {
                UserId = user.Id,
                Text = raw
            };

            var response = await http.PostAsJsonAsync("bookmarks", dto);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
        finally
        {
            _savingBookmark = false;
        }
    }

    private async Task ShowResultsForTextAsync(string raw)
    {
        var ingredients = ParseIngredients(raw);

        if (ingredients.Count == 0)
        {
            RightResultsCollectionView.ItemsSource = null;
            RightResultsCollectionView.IsVisible = false;
            CategoryTilesPanel.IsVisible = true;
            CategoryScroll.IsVisible = true;
            return;
        }

        try
        {
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

            bool hasResults = results.Count > 0;

            RightResultsCollectionView.ItemsSource = results;
            RightResultsCollectionView.IsVisible = hasResults;
            CategoryTilesPanel.IsVisible = !hasResults;

            if (!hasResults)
                await DisplayAlert("No matches", "No recipes found.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnResultSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is Models.RecipeDto recipe)
        {
            ((CollectionView)sender).SelectedItem = null;

            await Shell.Current.GoToAsync(
                $"{nameof(RecipeDetailPage)}?RecipeId={recipe.Id}",
                true
            );
        }
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