using Dapper;
using Microsoft.OpenApi.Models;
using Recipes.Api.Models;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Recipes API", Version = "v1" });
});

builder.Services.AddScoped<MySqlConnection>(_ =>
    new MySqlConnection(builder.Configuration.GetConnectionString("MySql"))
);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "Recipes API is running!");

app.MapGet("/recipes", async (string? category, MySqlConnection db) =>
{
    Console.WriteLine($"DEBUG: category = '{category}'");

    if (!string.IsNullOrWhiteSpace(category))
    {
        var rows = await db.QueryAsync(
            "SELECT * FROM Recipes WHERE LOWER(Category) = LOWER(@Category)",
            new { Category = category.Trim() }
        );

        return Results.Ok(rows);
    }

    var all = await db.QueryAsync("SELECT * FROM Recipes");
    return Results.Ok(all);
});

app.MapPost("/recipes/search-ingredients", async (SearchIngredientsDto req, MySqlConnection db) =>
{
    if (req.Ingredients is null || req.Ingredients.Count == 0)
        return Results.BadRequest("No ingredients provided.");

    var clauses = new List<string>();
    var p = new Dapper.DynamicParameters();

    for (int i = 0; i < req.Ingredients.Count; i++)
    {
        var key = $"i{i}";
        clauses.Add($"LOWER(Ingredients) LIKE LOWER(@{key})");
        p.Add(key, $"%{req.Ingredients[i].Trim()}%");
    }

    var sql = $"SELECT * FROM Recipes WHERE {string.Join(" AND ", clauses)}";
    var rows = await db.QueryAsync(sql, p);
    return Results.Ok(rows);
});

app.MapGet("/recipes/{id:int}", async (int id, MySqlConnection db) =>
{
    var one = await db.QueryFirstOrDefaultAsync("SELECT * FROM Recipes WHERE Id = @Id", new { Id = id });
    return one is not null ? Results.Ok(one) : Results.NotFound();
});

app.MapPost("/recipes", async (Recipe r, MySqlConnection db) =>
{
    var sql = @"INSERT INTO Recipes (Name, Servings, Estimated Cost, Category, Ingredients, Instructions, ImageUrl)
                VALUES (@Name, @Servings, @EstimatedCost, @Category, @Ingredients, @Instructions, @ImageUrl)";
    var rows = await db.ExecuteAsync(sql, r);
    return rows > 0 ? Results.Ok("Recipe added") : Results.BadRequest("Insert failed");
});

app.MapPost("/auth/login", async (LoginRequest req, MySqlConnection db) =>
{
    const string sql = "SELECT Id, Username, Password FROM Users WHERE Username = @Username";

    var user = await db.QueryFirstOrDefaultAsync<dynamic>(sql, new { req.Username });

    if (user == null)
        return Results.Unauthorized();

    string storedPassword = user.Password;

    if (storedPassword != req.Password)
        return Results.Unauthorized();

    return Results.Ok(new UserDto
    {
        Id = user.Id,
        Username = user.Username
    });
});

// ------------------- BOOKMARKS -------------------

app.MapPost("/bookmarks", async (CreateBookmarkDto dto, MySqlConnection db) =>
{
    if (dto.UserId <= 0 || string.IsNullOrWhiteSpace(dto.Text))
        return Results.BadRequest("UserId and Text are required.");

    const string sql = @"INSERT INTO Bookmarks (UserId, Text)
                         VALUES (@UserId, @Text);";

    var rows = await db.ExecuteAsync(sql, new { dto.UserId, dto.Text });

    return rows > 0 ? Results.Ok() : Results.BadRequest("Insert failed.");
});

app.MapGet("/bookmarks/{userId:int}", async (int userId, MySqlConnection db) =>
{
    const string sql = @"SELECT Id, UserId, Text, DateCreated
                         FROM Bookmarks
                         WHERE UserId = @UserId
                         ORDER BY DateCreated DESC";

    var rows = await db.QueryAsync<BookmarkDto>(sql, new { UserId = userId });
    return Results.Ok(rows);
});
app.MapDelete("/bookmarks/{id:int}", async (int id, MySqlConnection db) =>
{
    const string sql = @"DELETE FROM Bookmarks WHERE Id = @Id";

    var rows = await db.ExecuteAsync(sql, new { Id = id });

    return rows > 0
        ? Results.Ok()
        : Results.NotFound($"Bookmark with Id {id} not found.");
});

// ------------------- FAVORITES -------------------

app.MapPost("/favorites/toggle", async (ToggleFavoriteDto dto, MySqlConnection db) =>
{
    if (dto.UserId <= 0 || dto.RecipeId <= 0)
        return Results.BadRequest("UserId and RecipeId are required.");

    const string findSql = @"SELECT Id FROM Favorites
                             WHERE UserId = @UserId AND RecipeId = @RecipeId";

    var existingId = await db.QueryFirstOrDefaultAsync<int?>(
        findSql, new { dto.UserId, dto.RecipeId });

    if (existingId.HasValue)
    {
        const string deleteSql = "DELETE FROM Favorites WHERE Id = @Id";
        await db.ExecuteAsync(deleteSql, new { Id = existingId.Value });
        return Results.Ok(new { isFavorite = false });
    }
    else
    {
        const string insertSql = @"INSERT INTO Favorites (UserId, RecipeId)
                                   VALUES (@UserId, @RecipeId)";
        await db.ExecuteAsync(insertSql, new { dto.UserId, dto.RecipeId });
        return Results.Ok(new { isFavorite = true });
    }
});

app.MapGet("/favorites/{userId:int}/{recipeId:int}", async (int userId, int recipeId, MySqlConnection db) =>
{
    const string sql = @"SELECT Id FROM Favorites
                         WHERE UserId = @UserId AND RecipeId = @RecipeId";

    var existingId = await db.QueryFirstOrDefaultAsync<int?>(
        sql, new { UserId = userId, RecipeId = recipeId });

    bool isFavorite = existingId.HasValue;
    return Results.Ok(new { isFavorite });
});
app.MapGet("/favorites/{userId:int}", async (int userId, MySqlConnection db) =>
{
    const string sql = @"
        SELECT r.*
        FROM Favorites f
        JOIN Recipes r ON r.Id = f.RecipeId
        WHERE f.UserId = @UserId";

    var rows = await db.QueryAsync<Recipe>(sql, new { UserId = userId });
    return Results.Ok(rows);
});

app.Run();

public class SearchIngredientsDto
{
    public List<string> Ingredients { get; set; } = new();
}
public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
}
public class CreateBookmarkDto
{
    public int UserId { get; set; }
    public string Text { get; set; } = "";
}

public class BookmarkDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Text { get; set; } = "";
    public DateTime DateCreated { get; set; }
}

public class ToggleFavoriteDto
{
    public int UserId { get; set; }
    public int RecipeId { get; set; }
}