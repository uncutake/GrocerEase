using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using GrocerEaseFinal;

namespace GrocerEaseFinal
{
    public partial class MainPage : ContentPage
    {
        bool _drawerOpen;
        const uint DrawerAnim = 220;
        double _drawerWidth = 360;

        public MainPage()
        {
            InitializeComponent();

            // Each of these names must exist in MainPage.xaml
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
                RightDrawerHost.TranslationX = _drawerOpen ? 0 : _drawerWidth;
            }
        }
        private void WireTap(View tile, string plan)
        {
            if (tile == null) return;

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, __) =>
                await Shell.Current.GoToAsync(nameof(DietPlansPage),
                    new Dictionary<string, object> { { "Plan", plan } });
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
            var q = RecipeSearchBar.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(q))
                await DisplayAlert("Search", $"Searching for: {q}", "OK");
        }

        private void OnRecipeSearchBarCompleted(object sender, EventArgs e) => OnRecipeSearch(sender, e);
        private async void OnRecipeTextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private async void OnGenerateClick(object sender, EventArgs e)
        {
            var text = GroceryEditor?.Text?.Trim();
            var msg = string.IsNullOrWhiteSpace(text)
                ? "Enter ingredients in the box first."
                : $"Would generate recipes using\n\n{text}";
            await DisplayAlert("Generate", msg, "OK");
        }

        private async void OnBookmarkClick(object sender, EventArgs e)
            => await DisplayAlert("Bookmarks", "Open saved recipes", "OK");

        async void OnMenuClick(object sender, EventArgs e)
        {
            await ToggleDrawer(!_drawerOpen);
        }

        async void OnOverlayTapped(object sender, EventArgs e)
        {
            await ToggleDrawer(false);
        }

        async void OnMenuHome(object sender, EventArgs e)
        {
            await ToggleDrawer(false);
        }

        async void OnMenuLogin(object sender, EventArgs e)
        {
            await ToggleDrawer(false);
        }

        private async System.Threading.Tasks.Task ToggleDrawer(bool open)
        {
            if (MenuOverlay == null || RightDrawerHost == null)
                return;

            if (open)
            {
                MenuOverlay.IsVisible = true;
                RightDrawerHost.TranslationX = _drawerWidth;

                await RightDrawerHost.TranslateTo(0, 0, DrawerAnim, Easing.CubicOut);
                _drawerOpen = true;
            }
            else
            {
                await RightDrawerHost.TranslateTo(_drawerWidth, 0, DrawerAnim, Easing.CubicIn);
                MenuOverlay.IsVisible = false;
                _drawerOpen = false;
            }
        }
    }
}