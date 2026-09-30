using AiForms.Dialogs;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using Opx.Views;
using System.Net;
using System.Text;
using Toast = CommunityToolkit.Maui.Alerts.Toast;

namespace Opx.Views;

public partial class AddAccount : Popup
{
    private List<BankResponse> bankList = new();
    private HttpClient _httpClient;
    private bool _isLoading = false;

    public AddAccount()
    {
        InitializeComponent();
        InitializeHttpClient();
        _ = LoadBankList();
    }

    private void InitializeHttpClient()
    {
        try
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(90)
            };

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HttpClient initialization error: {ex.Message}");
        }
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

            ShowLoading(true);
            HideError();

            var selectedBank = Addbankname.SelectedItem as BankResponse;
            var accountNumber = accountnumber.Text?.Trim();

            System.Diagnostics.Debug.WriteLine($"Setting recipient - Bank: {selectedBank?.name}, Account: {accountNumber}");

            var requestData = new BankDataObject
            {
                BankCode = selectedBank?.bankCode,
                AccountNumber = accountNumber,
                Email = "" // populate as needed, original code truncated so kept blank
            };

            // Attempt to post the recipient (adjust endpoint as needed)
            var postResult = await Opx.Services.OpxApi.PostAsync<object>("/recipients", requestData);

            if (postResult.IsNetworkError)
            {
                ShowError(postResult.ErrorMessage ?? "Network error while creating recipient.");
                return;
            }

            if (postResult.IsHttpSuccess)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ShowErrorSnackbarAsync("Recipient added successfully.");
                });
            }
            else
            {
                ShowError("Failed to add recipient. Please try again.");
            }
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

        // Additional basic validation can be added here (length, digits, etc.)
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
}