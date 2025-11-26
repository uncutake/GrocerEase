using System.Text.Json.Serialization;
namespace Recipes.Api.Models;

public class Recipe
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Servings { get; set; }
    public int EstimatedCost { get; set; }
    public string Category { get; set; }
    public string Ingredients { get; set; }
    public string Instructions { get; set; }
    public string ImageUrl { get; set; }
}