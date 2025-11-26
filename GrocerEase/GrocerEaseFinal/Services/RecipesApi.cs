using System.Net.Http.Json;
using GrocerEaseFinal.Models;

namespace GrocerEaseFinal.Services;

public class RecipesApi : IRecipesApi
{
    private readonly HttpClient _http;

    public RecipesApi(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<RecipeDto>> GetRecipesAsync(string? category = null)
    {
        var url = "recipes";
        if (!string.IsNullOrWhiteSpace(category))
            url += $"?category={Uri.EscapeDataString(category)}";

        return await _http.GetFromJsonAsync<List<RecipeDto>>(url) ?? new();
    }

    public async Task<RecipeDto?> GetRecipeAsync(int id)
    {
        return await _http.GetFromJsonAsync<RecipeDto>($"recipes/{id}");
    }

    public async Task<List<RecipeDto>> SearchByIngredientsAsync(List<string> ingredients)
    {
        var payload = new { ingredients };

        var response = await _http.PostAsJsonAsync("recipes/search-ingredients", payload);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<RecipeDto>>() ?? new();
    }

    public async Task<bool> CheckFavorite(int userId, int recipeId)
    {
        var result = await _http.GetFromJsonAsync<FavoriteCheckResponse>(
            $"favorites/{userId}/{recipeId}"
        );

        return result?.isFavorite ?? false;
    }

    public async Task AddFavorite(int userId, int recipeId)
    {
        var dto = new { UserId = userId, RecipeId = recipeId };

        var response = await _http.PostAsJsonAsync("favorites/toggle", dto);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveFavorite(int userId, int recipeId)
    {
        var dto = new { UserId = userId, RecipeId = recipeId };

        var response = await _http.PostAsJsonAsync("favorites/toggle", dto);
        response.EnsureSuccessStatusCode();
    }

    public class FavoriteCheckResponse
    {
        public bool isFavorite { get; set; }
    }
    public async Task<List<RecipeDto>> GetFavoriteRecipesAsync(int userId)
    {
        return await _http.GetFromJsonAsync<List<RecipeDto>>(
                   $"favorites/user/{userId}"
               ) ?? new();
    }
}