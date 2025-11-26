using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace GrocerEaseFinal;

public static class RecipeFavoritesStore
{
    public sealed class FavoriteRecipe
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
        public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    private static string Key(string user) => $"favrecipes::{user}";

    public static List<FavoriteRecipe> Load(string user)
    {
        var json = Preferences.Get(Key(user), "[]");
        try { return JsonSerializer.Deserialize<List<FavoriteRecipe>>(json) ?? new(); }
        catch { return new(); }
    }

    public static void Save(string user, List<FavoriteRecipe> items)
    {
        var json = JsonSerializer.Serialize(items);
        Preferences.Set(Key(user), json);
    }

    public static bool Contains(string user, int id) =>
        Load(user).Any(f => f.Id == id);

    public static void Add(string user, FavoriteRecipe fav, bool dedupe = true)
    {
        var list = Load(user);
        if (dedupe && list.Any(f => f.Id == fav.Id)) return;
        list.Add(fav);
        Save(user, list);
    }

    public static void Remove(string user, int id)
    {
        var list = Load(user);
        list.RemoveAll(f => f.Id == id);
        Save(user, list);
    }

    public static void Clear(string user) => Preferences.Remove(Key(user));
}