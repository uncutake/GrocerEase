namespace GrocerEaseFinal;

public static class UserSession
{
    public static UserDto? CurrentUser { get; private set; }
    public static bool IsLoggedIn => CurrentUser != null;

    public static void SetUser(UserDto user) => CurrentUser = user;
    public static void Logout() => CurrentUser = null;
}