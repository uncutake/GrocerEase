using System.Text.Json;
using GrocerEaseFinal.Models;

namespace GrocerEaseFinal.Services;

public interface IRecipesApi
{
    Task<List<RecipeDto>> GetRecipesAsync(string? category = null);
    Task<RecipeDto?> GetRecipeAsync(int id);
    Task<List<RecipeDto>> SearchByIngredientsAsync(List<string> ingredients);
    Task<bool> CheckFavorite(int userId, int recipeId);
    Task AddFavorite(int userId, int recipeId);
    Task RemoveFavorite(int userId, int recipeId);
    Task<List<RecipeDto>> GetFavoriteRecipesAsync(int userId);

}