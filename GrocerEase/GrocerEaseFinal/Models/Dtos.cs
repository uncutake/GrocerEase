namespace GrocerEaseFinal;

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

public class FavoriteStatusDto
{
    public bool isFavorite { get; set; }
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