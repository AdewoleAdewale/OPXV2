using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace Opx.Views;

public partial class AddAccount : Popup
{
    private List<BankResponse> bankList;
    private HttpClient _httpClient;
    private bool _isLoading = false;

    public AddAccount()
    {
        InitializeComponent();
        InitializeHttpClient();
        LoadBankList();
    }

    private void InitializeHttpClient()
    {
        try
        {
            // Create HttpClient with proper configuration
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(90)
            };

            // Add default headers
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
            return true; // Proceed anyway if we can't check
        }
    }

    private async Task LoadBankList()
    {
        try
        {
            ShowLoading(true);
            HideError();

            // Check internet connection first
            if (!await CheckInternetConnectionAsync())
            {
                ShowLoading(false);
                return;
            }

            string url = "https://opxng.com/api/agencies/banks";

            System.Diagnostics.Debug.WriteLine($"Loading banks from: {url}");

            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

            System.Diagnostics.Debug.WriteLine($"Bank list response status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"API Response: {json}");

                if (!string.IsNullOrEmpty(json))
                {
                    // Deserialize directly as a list of banks
                    var banks = JsonConvert.DeserializeObject<List<BankResponse>>(json);

                    if (banks != null && banks.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully loaded {banks.Count} banks");

                        foreach (var bank in banks)
                        {
                            System.Diagnostics.Debug.WriteLine($"Bank: {bank.name}, Code: {bank.bankCode}");
                        }

                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            try
                            {
                                // Set the binding to the name property directly
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
                        ShowError("No banks available at the moment.");
                    }
                }
                else
                {
                    ShowError("Failed to load bank list. Please try again.");
                }
            }
            else
            {
                ShowError($"Failed to load banks. Status: {response.StatusCode}");
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
            // Validate inputs
            if (!ValidateInputs())
                return;

            // Check internet connection
            if (!await CheckInternetConnectionAsync())
            {
                return;
            }

            ShowLoading(true);
            HideError();

            // Get selected bank
            var selectedBank = Addbankname.SelectedItem as BankResponse;
            var accountNumber = accountnumber.Text?.Trim();

            System.Diagnostics.Debug.WriteLine($"Setting recipient - Bank: {selectedBank?.name}, Account: {accountNumber}");

            // Prepare request data
            var requestData = new BankDataObject
            {
                BankCode = selectedBank.bankCode,
                AccountNumber = accountNumber,
                Email = LoginPage.myemail,
            };

            // Send to API
            await SendToAPI(requestData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Set recipient error: {ex.Message}\n{ex.StackTrace}");
            ShowError("An error occurred while setting up account.");
            await ShowErrorSnackbarAsync("Failed to set recipient account. Please try again.");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private bool ValidateInputs()
    {
        // Validate bank selection
        if (Addbankname.SelectedItem == null)
        {
            ShowError("Please select a bank.");
            return false;
        }

        // Validate account number
        var accountNum = accountnumber.Text?.Trim();
        if (string.IsNullOrEmpty(accountNum))
        {
            ShowError("Please enter your account number.");
            return false;
        }

        // Basic account number validation
        if (accountNum.Length < 10 || accountNum.Length > 15)
        {
            ShowError("Account number must be between 10 and 15 digits.");
            return false;
        }

        if (!accountNum.All(char.IsDigit))
        {
            ShowError("Account number must contain only digits.");
            return false;
        }

        return true;
    }

    private async Task SendToAPI(BankDataObject requestData)
    {
        try
        {
            string url = "https://opxng.com/api/agencies/recipient-account";

            // Serialize request data
            var json = JsonConvert.SerializeObject(requestData);
            System.Diagnostics.Debug.WriteLine($"Sending request to: {url}");
            System.Diagnostics.Debug.WriteLine($"Request payload: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);

            System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");

            var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"Response content: {responseJson}");

            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrEmpty(responseJson))
                {
                    var apiResponse = JsonConvert.DeserializeObject<BankDataObjectResponse>(responseJson);

                    if (apiResponse != null)
                    {
                        // Check if API returned success
                        if (apiResponse.success == "true" || apiResponse.success == "1" ||
                            apiResponse.success?.ToLower() == "success")
                        {
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                try
                                {
                                    // Show success snackbar
                                    string accountName = apiResponse.AccountName ?? "Account";
                                    string successMessage = $"Account set successfully! {accountName}";

                                    await ShowSuccessSnackbarAsync(successMessage);

                                    // Small delay to ensure snackbar is visible
                                    await Task.Delay(500);

                                    // Close the popup
                                    Close();
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error showing success: {ex.Message}");
                                }
                            });
                        }
                        else
                        {
                            string errorMsg = apiResponse.message ?? "Failed to set account. Please try again.";
                            ShowError(errorMsg);
                            await ShowErrorSnackbarAsync(errorMsg);
                        }
                    }
                    else
                    {
                        ShowError("Invalid response from server.");
                        await ShowErrorSnackbarAsync("Invalid server response. Please try again.");
                    }
                }
                else
                {
                    ShowError("Empty response from server.");
                    await ShowErrorSnackbarAsync("Empty server response. Please try again.");
                }
            }
            else
            {
                // Handle different HTTP status codes
                string errorMessage = await HandleHttpErrorAsync(response.StatusCode, responseJson);
                ShowError(errorMessage);
                await ShowErrorSnackbarAsync(errorMessage);
            }
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error in SendToAPI: {httpEx.Message}\n{httpEx.StackTrace}");
            string errorMsg = "Network error. Please check your internet connection.";
            ShowError(errorMsg);
            await ShowErrorSnackbarAsync(errorMsg);
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Timeout in SendToAPI: {ex.Message}");
            string errorMsg = "Request timed out. Please try again.";
            ShowError(errorMsg);
            await ShowErrorSnackbarAsync(errorMsg);
        }
        catch (JsonException jsonEx)
        {
            System.Diagnostics.Debug.WriteLine($"JSON error in SendToAPI: {jsonEx.Message}");
            string errorMsg = "Error processing server response.";
            ShowError(errorMsg);
            await ShowErrorSnackbarAsync(errorMsg);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"General error in SendToAPI: {ex.Message}\n{ex.StackTrace}");
            string errorMsg = "An unexpected error occurred.";
            ShowError(errorMsg);
            await ShowErrorSnackbarAsync(errorMsg);
        }
    }

    private async Task<string> HandleHttpErrorAsync(HttpStatusCode statusCode, string responseJson)
    {
        try
        {
            // Try to extract error message from response
            if (!string.IsNullOrEmpty(responseJson))
            {
                var errorResponse = JsonConvert.DeserializeObject<BankDataObjectResponse>(responseJson);
                if (!string.IsNullOrEmpty(errorResponse?.message))
                {
                    return errorResponse.message;
                }
            }
        }
        catch
        {
            // Ignore deserialization errors
        }

        // Return default error messages based on status code
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Invalid request. Please check your details.",
            HttpStatusCode.Unauthorized => "Unauthorized. Please login again.",
            HttpStatusCode.Forbidden => "Access denied. Please contact support.",
            HttpStatusCode.NotFound => "Service not found. Please try again later.",
            HttpStatusCode.InternalServerError => "Server error. Please try again later.",
            _ => $"Request failed with status: {statusCode}"
        };
    }

    private async Task ShowSuccessSnackbarAsync(string message)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var snackbar = Snackbar.Make(
                    message,
                    null,
                    "OK",
                    TimeSpan.FromSeconds(5),
                    new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb("#4CAF50"),
                        TextColor = Colors.White,
                        ActionButtonTextColor = Colors.White,
                        CornerRadius = new CornerRadius(10),
                        Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                    });
                await snackbar.Show(CancellationToken.None);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing success snackbar: {ex.Message}");
        }
    }

    private async Task ShowErrorSnackbarAsync(string message)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var snackbar = Snackbar.Make(
                    message,
                    null,
                    "OK",
                    TimeSpan.FromSeconds(5),
                    new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb("#FF6B6B"),
                        TextColor = Colors.White,
                        ActionButtonTextColor = Colors.White,
                        CornerRadius = new CornerRadius(10),
                        Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                    });
                await snackbar.Show(CancellationToken.None);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing error snackbar: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (ErrorLabel != null)
                {
                    ErrorLabel.Text = message;
                    ErrorLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing error label: {ex.Message}");
            }
        });
    }

    private void HideError()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (ErrorLabel != null)
                {
                    ErrorLabel.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding error label: {ex.Message}");
            }
        });
    }

    private void ShowLoading(bool isLoading)
    {
        _isLoading = isLoading;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (setrecipient != null)
                {
                    setrecipient.IsEnabled = !isLoading;
                    setrecipient.Text = isLoading ? "Processing..." : "Set Recipient Account";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing loading: {ex.Message}");
            }
        });
    }

    private void Cancel_Clicked(object sender, EventArgs e)
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

    // Cleanup
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler == null)
        {
            // Popup is being disposed, cleanup resources
            _httpClient?.Dispose();
        }
    }

    // Data Models

    public class BankResponse
    {
        public string id { get; set; }
        public string name { get; set; }
        public string bankCode { get; set; }
    }

    internal class BankDataObject
    {
        public string BankCode { get; set; }
        public string AccountNumber { get; set; }
        public string Email { get; set; }
    }

    internal class BankDataObjectResponse
    {
        public string success { get; set; }
        public string bankCode { get; set; }
        public string message { get; set; }
        public string bankName { get; set; }
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
    }
}