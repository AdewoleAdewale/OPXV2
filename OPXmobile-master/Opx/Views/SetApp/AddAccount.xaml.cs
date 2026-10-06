using AiForms.Dialogs;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using Opx.Model;
using Opx.Renderers;
using Opx.Services;
using Opx.Views;
using System.Net;
using System.Text;
using Toast = CommunityToolkit.Maui.Alerts.Toast;

namespace Opx.Views;

public partial class AddAccount : Popup
{
    private List<BankResponse> bankList = new();
    private bool _isLoading = false;

    public AddAccount()
    {
        InitializeComponent();
        _ = LoadBankList();
        accountnumber.TextChanged += (_, _) => _ = LookupAccountNameAsync();
        Addbankname.SelectedIndexChanged += (_, _) => _ = LookupAccountNameAsync();
    }

    private async Task<bool> CheckInternetConnectionAsync()
    {
        try
        {
            var current = Connectivity.Current.NetworkAccess;
            if (current != NetworkAccess.Internet)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ShowErrorSnackbarAsync("No internet connection. Please check your network settings.");
                });
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking connectivity: {ex.Message}");
            // Allow proceeding if connectivity check fails
            return true;
        }
    }

    private async Task LoadBankList()
    {
        try
        {
            ShowLoading(true);
            HideError();

            if (!await CheckInternetConnectionAsync())
            {
                ShowLoading(false);
                return;
            }

            string url = "/agencies/banks";
            System.Diagnostics.Debug.WriteLine($"Loading banks from: {Opx.Services.OpxApi.BaseUrl}{url}");

            var result = await Opx.Services.OpxApi.GetAsync<List<BankResponse>>(url);

            if (result.IsNetworkError)
            {
                ShowError(result.ErrorMessage ?? "Network error while loading banks.");
                return;
            }

            if (result.IsHttpSuccess && result.Data != null)
            {
                var banks = result.Data;
                System.Diagnostics.Debug.WriteLine($"Successfully loaded {banks.Count} banks");

                foreach (var bank in banks)
                {
                    System.Diagnostics.Debug.WriteLine($"Bank: {bank.name}, Code: {bank.bankCode}");
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        Addbankname.ItemDisplayBinding = new Binding("name");
                        Addbankname.ItemsSource = banks;
                        bankList = banks;
                        System.Diagnostics.Debug.WriteLine($"Bank picker updated with {banks.Count} banks");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating bank picker: {ex.Message}");
                        ShowError("Error updating bank list.");
                    }
                });
            }
            else
            {
                ShowError("Failed to load bank list. Please try again.");
            }
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error loading banks: {httpEx.Message}\n{httpEx.StackTrace}");
            ShowError("Network error. Please check your internet connection.");
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Timeout loading banks: {ex.Message}");
            ShowError("Request timed out. Please try again.");
        }
        catch (JsonException jsonEx)
        {
            System.Diagnostics.Debug.WriteLine($"JSON error loading banks: {jsonEx.Message}");
            ShowError("Error processing bank data. Please try again.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"General error loading banks: {ex.Message}\n{ex.StackTrace}");
            ShowError("An unexpected error occurred. Please try again.");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private string _lookupKey = "";

    private async Task LookupAccountNameAsync()
    {
        try
        {
            var bank = Addbankname.SelectedItem as BankResponse;
            var number = accountnumber.Text?.Trim() ?? "";
            if (bank == null || number.Length != 10 || !number.All(char.IsDigit))
            {
                _lookupKey = "";
                accountHolderName.Text = "";
                return;
            }

            var key = bank.bankCode + "|" + number;
            _lookupKey = key;

            var result = await OpxApi.GetAsync<ValidateAccountResponse>(
                $"/agencies/validate-account?bankCode={Uri.EscapeDataString(bank.bankCode)}&accountNumber={Uri.EscapeDataString(number)}");

            if (_lookupKey != key) return;   // the user already changed the bank / number

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (result.IsHttpSuccess && result.Data is { IsValid: true })
                    accountHolderName.Text = result.Data.AccountName ?? "";
                else
                {
                    accountHolderName.Text = "";
                    if (!result.IsNetworkError) ShowError(result.ErrorMessage);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Account lookup error: {ex.Message}");
        }
    }

    private async void setrecipient_Clicked(object sender, EventArgs e)
    {
        if (_isLoading)
            return;

        try
        {
            if (!ValidateInputs())
                return;

            if (!await CheckInternetConnectionAsync())
                return;

            var email = LoginPage.myemail;
            if (string.IsNullOrWhiteSpace(email))
            {
                ShowError("Email not found. Please log in again.");
                return;
            }

            ShowLoading(true);
            HideError();

            var selectedBank = (BankResponse)Addbankname.SelectedItem!;
            var request = new RecipientAccountRequest
            {
                Email = email,
                BankCode = selectedBank.bankCode,
                AccountNumber = accountnumber.Text!.Trim()
            };

            // POST /api/agencies/recipient-account – the server validates the bank and account number itself.
            var result = await SessionStore.WithAuthRetryAsync(() =>
                OpxApi.PostAsync<RecipientAccountResponse>("/agencies/recipient-account", request));

            if (!result.IsHttpSuccess || result.Data == null || !result.Data.Success)
            {
                ShowError(string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? (result.Data?.Message ?? "Could not save the payout account. Please try again.")
                    : result.ErrorMessage);
                return;
            }

            AccountSetupManager.MarkAccountSetupComplete();
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                accountHolderName.Text = result.Data.AccountName ?? accountHolderName.Text;
                await ShowSuccessSnackbarAsync("Payout account saved successfully.");
                Close("saved");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting recipient: {ex.Message}\n{ex.StackTrace}");
            ShowError("An unexpected error occurred. Please try again.");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private bool ValidateInputs()
    {
        var selectedBank = Addbankname.SelectedItem as BankResponse;
        var acct = accountnumber.Text?.Trim();

        if (selectedBank == null)
        {
            ShowError("Please select a bank.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(acct))
        {
            ShowError("Please enter an account number.");
            return false;
        }

        if (acct.Length != 10 || !acct.All(char.IsDigit))
        {
            ShowError("Account number must be 10 digits.");
            return false;
        }

        return true;
    }

    private void ShowLoading(bool show)
    {
        _isLoading = show;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // Assume there's an activity indicator named LoadingIndicator in XAML
                if (this.FindByName("LoadingIndicator") is View v)
                    v.IsVisible = show;

                // Optionally disable controls while loading
                Addbankname.IsEnabled = !show;
                accountnumber.IsEnabled = !show;
                setrecipient.IsEnabled = !show;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowLoading error: {ex.Message}");
            }
        });
    }

    private void ShowError(string message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Show a simple alert/dialog or a label in UI
                await ShowErrorSnackbarAsync(message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowError error: {ex.Message}");
            }
        });
    }

    private void HideError()
    {
        // Implement hiding any inline error UI if present.
        // Kept intentionally minimal.
    }

    private async Task ShowErrorSnackbarAsync(string message)
    {
        try
        {
            var snackbarOptions = new SnackbarOptions
            {
                BackgroundColor = Colors.DarkRed,
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.White
            };

            var snackbar = Snackbar.Make(message, null, "OK", TimeSpan.FromSeconds(3), snackbarOptions);
            await snackbar.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowErrorSnackbarAsync error: {ex.Message}");
            // Fallback to toast
            try
            {
                var toast = Toast.Make(message, CommunityToolkit.Maui.Core.ToastDuration.Short);
                await toast.Show();
            }
            catch { }
        }
    }

    private async Task ShowSuccessSnackbarAsync(string message)
    {
        try
        {
            var snackbar = Snackbar.Make(message, null, "OK", TimeSpan.FromSeconds(3), new SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#1FA971"),
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.White
            });
            await snackbar.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowSuccessSnackbarAsync error: {ex.Message}");
        }
    }

    private void Button_Clicked(object sender, EventArgs e)
    {

        try
        {
            Close();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cancel error: {ex.Message}");
        }
    }
}