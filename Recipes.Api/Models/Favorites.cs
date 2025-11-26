namespace Recipes.Api.Models;

public class Favorite
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RecipeId { get; set; }
    public DateTime DateCreated { get; set; }
}