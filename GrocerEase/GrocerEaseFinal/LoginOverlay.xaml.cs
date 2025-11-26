using System;
using System.Net.Http.Json;
using Microsoft.Maui.Controls;

namespace GrocerEaseFinal
{
    public partial class LoginOverlay : ContentPage
    {
        private const string ApiBaseUrl = "http://localhost:5111/";
        private readonly HttpClient _httpClient;

        public LoginOverlay()
        {
            InitializeComponent();
            _httpClient = new HttpClient { BaseAddress = new Uri(ApiBaseUrl) };
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            MessageLabel.IsVisible = false;

            var username = UsernameEntry.Text?.Trim() ?? string.Empty;
            var password = PasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageLabel.Text = "Please enter both username and password.";
                MessageLabel.IsVisible = true;
                return;
            }

            try
            {
                var request = new LoginRequest
                {
                    Username = username,
                    Password = password
                };

                var response = await _httpClient.PostAsJsonAsync("auth/login", request);

                if (!response.IsSuccessStatusCode)
                {
                    MessageLabel.Text = "Invalid username or password.";
                    MessageLabel.IsVisible = true;
                    return;
                }

                var user = await response.Content.ReadFromJsonAsync<UserDto>();
                if (user == null)
                {
                    MessageLabel.Text = "Unexpected server response.";
                    MessageLabel.IsVisible = true;
                    return;
                }

                UserSession.SetUser(user);

                await DisplayAlert("Welcome", $"Hello, {user.Username}!", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                MessageLabel.Text = $"Error: {ex.Message}";
                MessageLabel.IsVisible = true;
            }
        }
    }
}