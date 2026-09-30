using AiForms.Dialogs;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Opx.Views;

public partial class ResetPassword : Popup
{
    public event EventHandler<string> PasswordResetRequested;
    public event EventHandler LogoutRequested; // Add logout event

    private string otp; // Make sure this is properly set

    public ResetPassword()
    {
        InitializeComponent();
    }

    public ResetPassword(string otpValue) : this()
    {
        otp = otpValue;
    }

    private async void ResetPassword_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Prevent multiple clicks
            if (sender is Button button)
            {
                button.IsEnabled = false;
            }

            string newPassword = enterpass?.Text?.Trim() ?? string.Empty;

            // Enhanced validation
            if (string.IsNullOrEmpty(newPassword))
            {
                await ShowSnackbar("Please enter a new password.", Colors.Red);
                return;
            }

            // Password strength validation instead of email validation
            if (!IsValidPassword(newPassword))
            {
                await ShowSnackbar("Password must be at least 8 characters long and contain letters and numbers.", Colors.Red);
                return;
            }

            // Validate required fields
            if (string.IsNullOrEmpty(ForgetPassword.Forgetemail))
            {
                await ShowSnackbar("Email is missing. Please restart the reset process.", Colors.Red);
                return;
            }

            if (string.IsNullOrEmpty(otp))
            {
                await ShowSnackbar("OTP is missing. Please restart the reset process.", Colors.Red);
                return;
            }

            // Trigger the event
            PasswordResetRequested?.Invoke(this, newPassword);

            await ProcessPasswordReset(newPassword);
        }
        catch (Exception ex)
        {
            await ShowSnackbar($"An unexpected error occurred: {ex.Message}", Colors.Red);
        }
        finally
        {
            // Re-enable button
            if (sender is Button button)
            {
                button.IsEnabled = true;
            }
        }
    }

    private async Task ProcessPasswordReset(string newPassword)
    {
        try
        {
            Configurations.LoadingConfig = new LoadingConfig
            {
                Opacity = 0.4,
                DefaultMessage = "Resetting password, please wait...",
                FontSize = 12,
            };

            await Loading.Instance.StartAsync(async progress =>
            {
                try
                {
                    // Progress simulation
                    for (var i = 0; i < 50; i++)
                    {
                        await Task.Delay(20);
                        progress.Report((i + 1) * 0.02d);
                    }

                    bool resetSuccess = await CallResetPasswordAPI(newPassword);

                    // Complete progress
                    progress.Report(1.0d);

                    if (resetSuccess)
                    {
                        await HandleSuccessfulReset();
                    }
                }
                catch (Exception ex)
                {
                    await ShowSnackbar($"Reset process failed: {ex.Message}", Colors.Red);
                }
            });
        }
        catch (Exception ex)
        {
            await ShowSnackbar($"Failed to start password reset: {ex.Message}", Colors.Red);
        }
    }



    private async Task<bool> CallResetPasswordAPI(string newPassword)
    {
        try
        {
            string url = "https://opxng.com/api/AuthAccount/ResetPassword";

            var requestPayload = new ResetObject
            {
                email = ForgetPassword.Forgetemail,
                resetToken = otp, // Assuming OTP is used as reset token
                newPassword = newPassword,
                confirmPassword = newPassword
            };

            string jsonPayload = JsonConvert.SerializeObject(requestPayload, Formatting.None);

            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            {


                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    await ShowSnackbar($"Server error ({response.StatusCode}): {errorContent}", Colors.Red);
                    return false;
                }

                string resultString = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(resultString))
                {
                    await ShowSnackbar("Empty response from server", Colors.Red);
                    return false;
                }

                var resetResponse = JsonConvert.DeserializeObject<ResetResponse>(resultString);

                if (resetResponse == null)
                {
                    await ShowSnackbar("Failed to parse server response", Colors.Red);
                    return false;
                }

                if (!string.IsNullOrEmpty(resetResponse.success))
                {
                    return true;
                }
                else
                {
                    string errorMessage = resetResponse.message ?? "Password reset failed";
                    await ShowSnackbar(errorMessage, Colors.Red);
                    return false;
                }
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            await ShowSnackbar("Request timed out. Please check your internet connection.", Colors.Orange);
            return false;
        }
        catch (HttpRequestException ex)
        {
            await ShowSnackbar($"Network error: {ex.Message}", Colors.Orange);
            return false;
        }
        catch (JsonException ex)
        {
            await ShowSnackbar($"Data parsing error: {ex.Message}", Colors.Red);
            return false;
        }
        catch (Exception ex)
        {
            await ShowSnackbar($"Unexpected error: {ex.Message}", Colors.Red);
            return false;
        }
    }

    private async Task HandleSuccessfulReset()
    {
        try
        {
            // Show success message
            await ShowSnackbar("Password reset successful!", Colors.Green);

            // Wait a moment for user to see the success message
            await Task.Delay(1500);

            // Show logout notification
            await ShowSnackbar("You will be logged out and redirected to login.", Colors.Blue);

            // Wait a moment
            await Task.Delay(1500);

            // Close the popup first
            Close();

            // Trigger logout
            await PerformLogout();
        }
        catch (Exception ex)
        {
            await ShowSnackbar($"Error during logout process: {ex.Message}", Colors.Red);
        }
    }

    private async Task PerformLogout()
    {
        try
        {
            // Clear any stored user data, tokens, etc.
            ClearUserSession();

            // Trigger logout event for parent to handle navigation
            LogoutRequested?.Invoke(this, EventArgs.Empty);

            // Alternative: Direct navigation if you have access to navigation service
            // await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            await ShowSnackbar($"Logout error: {ex.Message}", Colors.Red);
        }
    }

    private void ClearUserSession()
    {
        try
        {
            // Clear stored preferences, tokens, user data, etc.
            Preferences.Clear();

            // Clear any static user data
            // UserSession.Clear(); // If you have a user session class

            // Clear secure storage if you're using it
            SecureStorage.RemoveAll();
        }
        catch (Exception ex)
        {
            // Log the error but don't show to user as it's not critical
            System.Diagnostics.Debug.WriteLine($"Error clearing session: {ex.Message}");
        }
    }

    private bool IsValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return false;

        // Check for at least one letter and one number
        bool hasLetter = Regex.IsMatch(password, @"[a-zA-Z]");
        bool hasNumber = Regex.IsMatch(password, @"\d");

        return hasLetter && hasNumber;
    }

    private async Task ShowSnackbar(string message, Color backgroundColor)
    {
        try
        {
            var snackbar = Snackbar.Make(message, null, "OK", TimeSpan.FromSeconds(5), new SnackbarOptions
            {
                BackgroundColor = backgroundColor,
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.White,
                CornerRadius = new CornerRadius(8),
                Font = Microsoft.Maui.Font.SystemFontOfSize(14)
            });

            await snackbar.Show(CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Fallback: just show an alert if snackbar fails
            await Application.Current?.MainPage?.DisplayAlert("Error", message, "OK");
        }
    }

    private void Cancel_Clicked(object sender, EventArgs e)
    {
        try
        {
            Close();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error closing popup: {ex.Message}");
        }
    }

    internal class ResetObject
    {
        public string email { get; set; } = "";
        public string resetToken { get; set; } = "";
        public string newPassword { get; set; } = "";
        public string confirmPassword { get; set; } = "";
    }

    internal class ResetResponse
    {
        public string? success { get; set; }
        public string? email { get; set; }
        public string? message { get; set; }
        public string? expires { get; set; }
        public string? requiresSetup { get; set; }
        public string? redirectTo { get; set; }
        public bool requiresVerification { get; set; }
    }
}