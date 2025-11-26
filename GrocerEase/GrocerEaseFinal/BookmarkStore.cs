using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace GrocerEaseFinal;

public static class BookmarkStore
{
    public sealed class BookmarkItem
    {
        public string Text { get; set; } = "";
        public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    private static string Key(string user) => $"bookmarks::{user}";

    public static List<BookmarkItem> Load(string user)
    {
        var json = Preferences.Get(Key(user), "[]");
        try { return JsonSerializer.Deserialize<List<BookmarkItem>>(json) ?? new(); }
        catch { return new(); }
    }

    public static void Save(string user, List<BookmarkItem> items)
    {
        var json = JsonSerializer.Serialize(items);
        Preferences.Set(Key(user), json);
    }

    public static void Add(string user, string text, bool dedupe = true)
    {
        var list = Load(user);

        var trimmed = (text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return;

        if (dedupe && list.Any(b => string.Equals(b.Text, trimmed, StringComparison.Ordinal)))
            return;

        list.Add(new BookmarkItem { Text = trimmed, SavedAt = DateTimeOffset.UtcNow });
        Save(user, list);
    }
    public static  void Remove(string user, BookmarkItem item)
    {
        if (string.IsNullOrWhiteSpace(user) || item == null)
            return;

        var list = Load(user);

        var idx = list.FindIndex(x =>
        x.Text == item.Text &&
        x.SavedAt == item.SavedAt);

        if (idx >= 0)
        {
            list.RemoveAt(idx);
            Save(user, list);
        }
    }

    public static void Clear(string user) => Preferences.Remove(Key(user));

    public static Task<List<BookmarkItem>> LoadAsync(string user) =>
        Task.FromResult(Load(user));

    public static Task AddAsync(string user, string text, bool dedupe = true)
    {
        Add(user, text, dedupe);
        return Task.CompletedTask;
    }

    public static Task ClearAsync(string user)
    {
        Clear(user);
        return Task.CompletedTask;
    }
}