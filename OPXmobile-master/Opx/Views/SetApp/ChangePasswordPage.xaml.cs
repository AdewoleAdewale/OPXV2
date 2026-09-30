using Newtonsoft.Json;
using Opx.Model;
using Opx.Services;
using System.Text.RegularExpressions;

namespace Opx.Views;

/// <summary>Change password for the signed-in user — POST /api/AuthAccount/ChangePassword.</summary>
public partial class ChangePasswordPage : ContentPage
{
    private bool _busy;

    public ChangePasswordPage()
    {
        InitializeComponent();
    }

    // ── UI helpers ───────────────────────────────────────────
    private async void Back_Clicked(object sender, EventArgs e) => await CloseAsync();

    private async Task CloseAsync()
    {
        if (Navigation.ModalStack.Contains(this)) await Navigation.PopModalAsync();
        else if (Navigation.NavigationStack.Count > 1) await Navigation.PopAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_busy) return true;
        return base.OnBackButtonPressed();
    }

    private void ToggleCurrent_Clicked(object sender, EventArgs e)
    {
        CurrentEntry.IsPassword = !CurrentEntry.IsPassword;
        ToggleCurrentBtn.Text = CurrentEntry.IsPassword ? "Show" : "Hide";
    }

    private void ToggleNew_Clicked(object sender, EventArgs e)
    {
        NewEntry.IsPassword = !NewEntry.IsPassword;
        ToggleNewBtn.Text = NewEntry.IsPassword ? "Show" : "Hide";
    }

    private void Entry_TextChanged(object sender, TextChangedEventArgs e)
    {
        ErrorLabel.IsVisible = false;
        if (sender == NewEntry) UpdateStrength(NewEntry.Text ?? "");
    }

    private void UpdateStrength(string pwd)
    {
        if (string.IsNullOrEmpty(pwd)) { StrengthLabel.IsVisible = false; return; }

        int score = 0;
        if (pwd.Length >= 8) score++;
        if (Regex.IsMatch(pwd, "[a-z]") && Regex.IsMatch(pwd, "[A-Z]")) score++;
        if (Regex.IsMatch(pwd, @"\d")) score++;
        if (Regex.IsMatch(pwd, @"[^a-zA-Z0-9]")) score++;

        (StrengthLabel.Text, StrengthLabel.TextColor) = score switch
        {
            <= 1 => ("Strength: Weak", Colors.Red),
            2 => ("Strength: Fair", Colors.Orange),
            3 => ("Strength: Good", Colors.Goldenrod),
            _ => ("Strength: Strong", Colors.ForestGreen)
        };
        StrengthLabel.IsVisible = true;
    }

    private void ShowError(string msg)
    {
        SuccessLabel.IsVisible = false;
        ErrorLabel.Text = msg;
        ErrorLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        SubmitButton.IsEnabled = !busy;
        CurrentEntry.IsEnabled = NewEntry.IsEnabled = ConfirmEntry.IsEnabled = !busy;
        Spinner.IsVisible = Spinner.IsRunning = busy;
        SubmitButton.Text = busy ? "Updating..." : "Update Password";
    }

    // ── Validation ───────────────────────────────────────────
    private string? Validate(string current, string newPwd, string confirm)
    {
        if (string.IsNullOrEmpty(current)) return "Please enter your current password.";
        if (string.IsNullOrEmpty(newPwd)) return "Please enter a new password.";
        if (newPwd.Length < 8 || !Regex.IsMatch(newPwd, "[a-zA-Z]") || !Regex.IsMatch(newPwd, @"\d"))
            return "New password must be at least 8 characters and contain letters and numbers.";
        if (newPwd == current) return "New password must be different from your current password.";
        if (newPwd != confirm) return "New password and confirmation do not match.";
        return null;
    }

    // ── Submit ───────────────────────────────────────────────
    private async void Submit_Clicked(object sender, EventArgs e)
    {
        if (_busy) return;

        var current = CurrentEntry.Text ?? "";
        var newPwd = NewEntry.Text ?? "";
        var confirm = ConfirmEntry.Text ?? "";

        var validationError = Validate(current, newPwd, confirm);
        if (validationError != null) { ShowError(validationError); return; }

        if (Connectivity.NetworkAccess != NetworkAccess.Internet)
        {
            ShowError("No internet connection. Please check your network and try again.");
            return;
        }

        try
        {
            SetBusy(true);

            var result = await OpxApi.PostAsync<BasicResponse>("/AuthAccount/ChangePassword",
                new ChangePasswordRequest
                {
                    CurrentPassword = current,
                    NewPassword = newPwd,
                    ConfirmPassword = confirm
                });

            if (result.IsHttpSuccess && result.Data?.Success == true)
            {
                await OnSuccessAsync(newPwd, result.Data.Message);
                return;
            }

            switch (result.StatusCode)
            {
                case System.Net.HttpStatusCode.Unauthorized:
                    // Session cookie missing/expired – the user must sign in again.
                    ShowError("Your session has expired. Please log in again, then retry.");
                    await Task.Delay(1800);
                    await ReturnToLoginAsync();
                    break;

                default:
                    // 400 – mismatch or Identity password-policy errors (wrong current password included)
                    ShowError(result.Data?.Message is { Length: > 0 } m ? m : result.ErrorMessage);
                    break;
            }
        }
        catch (Exception ex)
        {
            ShowError($"Unexpected error: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task OnSuccessAsync(string newPassword, string? serverMessage)
    {
        // Keep the biometric / remembered login working with the new password.
        await UpdateSavedCredentialsAsync(newPassword);

        CurrentEntry.Text = NewEntry.Text = ConfirmEntry.Text = string.Empty;
        ErrorLabel.IsVisible = false;
        SuccessLabel.Text = serverMessage ?? "Password changed successfully.";
        SuccessLabel.IsVisible = true;

        await DisplayAlert("Password Updated", serverMessage ?? "Your password has been changed successfully.", "OK");
        await CloseAsync();
    }

    private static async Task UpdateSavedCredentialsAsync(string newPassword)
    {
        try
        {
            var json = await SecureStorage.GetAsync("saved_credentials");
            if (string.IsNullOrWhiteSpace(json)) return;

            var creds = JsonConvert.DeserializeObject<LoginPage.SavedCredentials>(json);
            if (creds == null) return;

            creds.Password = newPassword;
            await SecureStorage.SetAsync("saved_credentials", JsonConvert.SerializeObject(creds));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not refresh saved credentials: {ex.Message}");
        }
    }

    private async Task ReturnToLoginAsync()
    {
        OpxApi.ClearSession();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Application.Current!.MainPage = new LoginPage();
        });
    }
}