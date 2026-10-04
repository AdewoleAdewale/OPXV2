
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Application = Microsoft.Maui.Controls.Application;
using Entry = Microsoft.Maui.Controls.Entry;

namespace Opx.Views;

public partial class CurrentBalance : ContentPage
{
    private HttpClient _httpClient;
    private bool _isLoading = false;
    private bool _isEditMode = false;
    private List<BankResponse> bankList;
    private RecipientData _currentRecipient = null;

    public static string bankname { get; set; }
    public static string cardnumber { get; set; }

    public CurrentBalance()
    {
        InitializeComponent();
        InitializeHttpClient();

        // Initialize UI state
        InitializeUI();

        // Load data
        LoadInitialData();

        // Set card details safely
        SetCardDetails();
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
                await ShowErrorSnackbarAsync("No internet connection. Please check your network settings.");
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

    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            await DashBoard.GoHomeAsync();   // Home is already underneath – don't rebuild it
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BackButton error: {ex.Message}");
        }
    }

    private void SetCardDetails()
    {
        try
        {
            if (LoginPage.accountNumber != null)
                cardaccountNumber.Text = LoginPage.accountNumber;
            else
                cardaccountNumber.Text = "****";

            if (LoginPage.accountName != null)
                cardAccountname.Text = LoginPage.accountName;
            else
                cardAccountname.Text = "Account Holder";

            if (LoginPage.bankName != null)
                cardExiprydate.Text = LoginPage.bankName;
            else
                cardExiprydate.Text = "Bank Name";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SetCardDetails Error: {ex.Message}");
        }
    }

    private void InitializeUI()
    {
        try
        {
            // Set initial UI state
            ShowEditMode(false);
            HideError();
            HideSuccess();

            // Initialize recipient card display
            UpdateRecipientCardDisplay();

            // Animate the main frame on load
            AnimateCardOnLoad();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InitializeUI Error: {ex.Message}");
        }
    }

    private async void AnimateCardOnLoad()
    {
        try
        {
            await Task.Delay(100);

            if (PaymentCardsCollection != null)
            {
                PaymentCardsCollection.Opacity = 0;
                PaymentCardsCollection.Scale = 0.9;
                await Task.WhenAll(
                    PaymentCardsCollection.FadeTo(1, 600, Easing.CubicOut),
                    PaymentCardsCollection.ScaleTo(1, 600, Easing.CubicOut)
                );
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AnimateCardOnLoad Error: {ex.Message}");
        }
    }

    private void UpdateRecipientCardDisplay()
    {
        try
        {
            if (_currentRecipient != null && !string.IsNullOrEmpty(_currentRecipient.AccountNumber))
            {
                // Update card display with recipient info
                RecipientBankName.Text = _currentRecipient.bankName ?? "N/A";
                RecipientAccountNumber.Text = _currentRecipient.AccountNumber ?? "N/A";
                RecipientAccountName.Text = _currentRecipient.AccountName ?? "N/A";

                // Update status indicator
                StatusIndicator.Fill = Colors.Green;
                StatusLabel.Text = "Recipient configured";
                StatusLabel.TextColor = Color.FromArgb("#E0FFE0");
            }
            else
            {
                // Show empty state
                RecipientBankName.Text = "Not Set";
                RecipientAccountNumber.Text = "Not Set";
                RecipientAccountName.Text = "Not Set";

                // Update status indicator
                StatusIndicator.Fill = Colors.Red;
                StatusLabel.Text = "No recipient set";
                StatusLabel.TextColor = Color.FromArgb("#FFE0E0");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateRecipientCardDisplay Error: {ex.Message}");
        }
    }

    private async void OnRecipientCardTapped(object sender, EventArgs e)
    {
        try
        {
            // Animate card tap
            var frame = sender as Frame;
            if (frame != null)
            {
                await frame.ScaleTo(0.98, 100, Easing.CubicIn);
                await frame.ScaleTo(1, 100, Easing.CubicOut);
            }

            // Show edit form if recipient is not set, otherwise show options
            if (_currentRecipient == null || string.IsNullOrEmpty(_currentRecipient.AccountNumber))
            {
                ShowEditMode(true);
            }
            else
            {
                await ShowEditRecipientFormAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnRecipientCardTapped Error: {ex.Message}");
        }
    }

    private async Task ShowEditRecipientFormAsync()
    {
        try
        {
            if (_currentRecipient != null)
            {
                // Pre-populate fields
                if (bankList != null)
                {
                    var bank = bankList.FirstOrDefault(b => b.name == _currentRecipient.bankName || b.bankCode == _currentRecipient.bankCode);
                    if (bank != null)
                    {
                        Addbankname.SelectedItem = bank;
                    }
                }

                accountnumber.Text = _currentRecipient.AccountNumber;
                accountHolderName.Text = _currentRecipient.AccountName;
            }

            ShowEditMode(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowEditRecipientFormAsync Error: {ex.Message}");
        }
    }

    private async void LoadInitialData()
    {
        try
        {
            // Check internet first
            if (!await CheckInternetConnectionAsync())
            {
                return;
            }

            await LoadBankList();
            await LoadCurrentRecipient();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadInitialData Error: {ex.Message}");
            ShowError("Failed to load initial data. Please refresh the page.");
        }
    }

    private async Task LoadBankList()
    {
        try
        {
            ShowLoading(true);
            HideError();

            string url = "https://opxng.com/api/agencies/banks";

            System.Diagnostics.Debug.WriteLine($"Loading banks from: {url}");

            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

            System.Diagnostics.Debug.WriteLine($"Bank list response status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"Bank API Response: {json}");

                if (!string.IsNullOrEmpty(json))
                {
                    var banks = JsonConvert.DeserializeObject<List<BankResponse>>(json);

                    if (banks != null && banks.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully loaded {banks.Count} banks");

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
                                System.Diagnostics.Debug.WriteLine($"UI Update Error: {ex.Message}");
                                ShowError("Error updating bank list display.");
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
            await ShowErrorSnackbarAsync("Network error loading banks. Please try again.");
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Timeout loading banks: {ex.Message}");
            ShowError("Request timed out. Please try again.");
            await ShowErrorSnackbarAsync("Request timed out. Please try again.");
        }
        catch (JsonException jsonEx)
        {
            System.Diagnostics.Debug.WriteLine($"JSON error loading banks: {jsonEx.Message}");
            ShowError("Error processing bank data.");
            await ShowErrorSnackbarAsync("Error processing bank data. Please try again.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"General error loading banks: {ex.Message}\n{ex.StackTrace}");
            ShowError("An unexpected error occurred.");
            await ShowErrorSnackbarAsync("Failed to load banks. Please try again.");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private async void ContentPage_Loaded(object sender, EventArgs e)
    {
        try
        {
            // Initial payment card animation
            if (PaymentCardsCollection != null)
            {
                PaymentCardsCollection.Opacity = 0;
                PaymentCardsCollection.Scale = 0.8;
                await Task.WhenAll(
                    PaymentCardsCollection.FadeTo(1, 800, Easing.CubicOut),
                    PaymentCardsCollection.ScaleTo(1, 800, Easing.CubicOut)
                );
            }

            // Recipient card animation
            if (RecipientDetailsCard != null)
            {
                RecipientDetailsCard.Opacity = 0;
                RecipientDetailsCard.TranslationY = 20;
                await Task.WhenAll(
                    RecipientDetailsCard.FadeTo(1, 600, Easing.CubicOut),
                    RecipientDetailsCard.TranslateTo(0, 0, 600, Easing.CubicOut)
                );
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ContentPage_Loaded Error: {ex.Message}");
        }
    }

    private async void OnCardTapped(object sender, EventArgs e)
    {
        try
        {
            var frame = sender as Frame;
            if (frame != null)
            {
                await frame.ScaleTo(1.05, 100, Easing.CubicIn);
                await frame.ScaleTo(1, 100, Easing.CubicOut);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnCardTapped Error: {ex.Message}");
        }
    }

    private async Task LoadCurrentRecipient()
    {
        try
        {
            ShowLoading(true);
            HideError();
            HideSuccess();

            // Check if LoginPage.myemail is available
            if (string.IsNullOrEmpty(LoginPage.myemail))
            {
                ShowError("User email not found. Please log in again.");
                return;
            }

            string url = $"https://opxng.com/api/agencies/recipient-account?email={Uri.EscapeDataString(LoginPage.myemail)}";

            System.Diagnostics.Debug.WriteLine($"Loading recipient from: {url}");

            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

            System.Diagnostics.Debug.WriteLine($"Recipient response status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"Recipient API Response: {json}");

                if (!string.IsNullOrEmpty(json))
                {
                    var recipient = JsonConvert.DeserializeObject<RecipientData>(json);

                    if (recipient != null && !string.IsNullOrEmpty(recipient.AccountNumber))
                    {
                        _currentRecipient = recipient;
                        bankname = recipient.AccountNumber;
                        cardnumber = recipient.AccountName;

                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            UpdateRecipientCardDisplay();
                            ShowEditMode(false);
                        });
                    }
                    else
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            UpdateRecipientCardDisplay();
                        });
                    }
                }
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    UpdateRecipientCardDisplay();
                });
            }
            else
            {
                ShowError($"Failed to load recipient details. Status: {response.StatusCode}");
            }
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error loading recipient: {httpEx.Message}\n{httpEx.StackTrace}");
            ShowError("Network error loading recipient.");
            await ShowErrorSnackbarAsync("Network error. Please check your connection.");
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Timeout loading recipient: {ex.Message}");
            ShowError("Request timed out.");
            await ShowErrorSnackbarAsync("Request timed out. Please try again.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading recipient: {ex.Message}\n{ex.StackTrace}");
            ShowError("An unexpected error occurred.");
            await ShowErrorSnackbarAsync("Failed to load recipient details.");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private void ShowEditMode(bool isEdit)
    {
        try
        {
            _isEditMode = isEdit;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (isEdit)
                    {
                        // Animate edit form appearance
                        EditRecipientForm.Opacity = 0;
                        EditRecipientForm.TranslationY = 20;
                        EditRecipientForm.IsVisible = true;

                        await Task.WhenAll(
                            EditRecipientForm.FadeTo(1, 400, Easing.CubicOut),
                            EditRecipientForm.TranslateTo(0, 0, 400, Easing.CubicOut)
                        );
                    }
                    else
                    {
                        // Animate edit form disappearance
                        if (EditRecipientForm.IsVisible)
                        {
                            await Task.WhenAll(
                                EditRecipientForm.FadeTo(0, 300, Easing.CubicIn),
                                EditRecipientForm.TranslateTo(0, 20, 300, Easing.CubicIn)
                            );
                        }
                        EditRecipientForm.IsVisible = false;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ShowEditMode Animation Error: {ex.Message}");
                    EditRecipientForm.IsVisible = isEdit;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowEditMode Error: {ex.Message}");
        }
    }

    private async void EditRecipientBtn_Clicked(object sender, EventArgs e)
    {
        try
        {
            await ShowEditRecipientFormAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EditRecipientBtn_Clicked Error: {ex.Message}");
            ShowError("An error occurred while editing recipient details.");
        }
    }

    private async void CancelEditBtn_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Clear fields
            Addbankname.SelectedItem = null;
            accountnumber.Text = string.Empty;
            accountHolderName.Text = string.Empty;

            HideError();
            HideSuccess();
            ShowEditMode(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CancelEditBtn_Clicked Error: {ex.Message}");
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

            // Check internet connection
            if (!await CheckInternetConnectionAsync())
            {
                return;
            }

            ShowLoading(true);
            HideError();
            HideSuccess();

            var selectedBank = Addbankname.SelectedItem as BankResponse;
            var accountNumber = accountnumber.Text?.Trim();
            var accountName = accountHolderName.Text?.Trim();

            System.Diagnostics.Debug.WriteLine($"Setting recipient - Bank: {selectedBank?.name}, Account: {accountNumber}");

            var requestData = new BankDataObject
            {
                BankCode = selectedBank.bankCode,
                AccountNumber = accountNumber,
                Email = LoginPage.myemail
            };

            await SendToAPI(requestData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"setrecipient_Clicked Error: {ex.Message}\n{ex.StackTrace}");
            ShowError("An error occurred while setting up the account.");
            await ShowErrorSnackbarAsync("Failed to set recipient account. Please try again.");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private bool ValidateInputs()
    {
        try
        {
            HideError();
            HideSuccess();

            if (Addbankname.SelectedItem == null)
            {
                ShowError("Please select a bank.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(accountnumber.Text))
            {
                ShowError("Please enter account number.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(accountHolderName.Text))
            {
                ShowError("Please enter account holder name.");
                return false;
            }

            if (accountnumber.Text.Length < 10)
            {
                ShowError("Account number must be at least 10 digits.");
                return false;
            }

            if (string.IsNullOrEmpty(LoginPage.myemail))
            {
                ShowError("User email not found. Please log in again.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ValidateInputs Error: {ex.Message}");
            ShowError("Validation error occurred.");
            return false;
        }
    }

    private async Task SendToAPI(BankDataObject requestData)
    {
        try
        {
            string url = "https://opxng.com/api/agencies/recipient-account";
            string json = JsonConvert.SerializeObject(requestData);

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
                    var result = JsonConvert.DeserializeObject<ApiResponse>(responseJson);

                    if (result != null && result.success)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            ShowSuccess("Recipient account updated successfully!");
                            await ShowSuccessSnackbarAsync("Recipient account updated successfully!");

                            // Small delay
                            await Task.Delay(500);

                            // Refresh recipient data
                            await LoadCurrentRecipient();

                            // Hide edit form after successful update
                            ShowEditMode(false);
                        });
                    }
                    else
                    {
                        string errorMsg = result?.message ?? "Failed to update recipient account.";
                        ShowError(errorMsg);
                        await ShowErrorSnackbarAsync(errorMsg);
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
                string errorMsg = await HandleHttpErrorAsync(response.StatusCode, responseJson);
                ShowError(errorMsg);
                await ShowErrorSnackbarAsync(errorMsg);
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
            if (!string.IsNullOrEmpty(responseJson))
            {
                var errorResponse = JsonConvert.DeserializeObject<ApiResponse>(responseJson);
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

    private void ShowLoading(bool show)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingIndicator.IsVisible = show;
                LoadingIndicator.IsRunning = show;
                _isLoading = show;

                // Disable form controls during loading
                if (setrecipient != null) setrecipient.IsEnabled = !show;
                if (CancelEditBtn != null) CancelEditBtn.IsEnabled = !show;
                if (Addbankname != null) Addbankname.IsEnabled = !show;
                if (accountnumber != null) accountnumber.IsEnabled = !show;
                if (accountHolderName != null) accountHolderName.IsEnabled = !show;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowLoading Error: {ex.Message}");
        }
    }

    private void ShowError(string message)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (ErrorLabel != null)
                {
                    ErrorLabel.Text = message;
                    ErrorLabel.IsVisible = true;
                }
                if (SuccessLabel != null)
                {
                    SuccessLabel.IsVisible = false;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowError Error: {ex.Message}");
        }
    }

    private void ShowSuccess(string message)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (SuccessLabel != null)
                {
                    SuccessLabel.Text = message;
                    SuccessLabel.IsVisible = true;
                }
                if (ErrorLabel != null)
                {
                    ErrorLabel.IsVisible = false;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowSuccess Error: {ex.Message}");
        }
    }

    private void HideError()
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (ErrorLabel != null)
                {
                    ErrorLabel.IsVisible = false;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HideError Error: {ex.Message}");
        }
    }

    private void HideSuccess()
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (SuccessLabel != null)
                {
                    SuccessLabel.IsVisible = false;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HideSuccess Error: {ex.Message}");
        }
    }
    private async void Addbankname_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            var picker = sender as Picker;
            if (picker?.SelectedItem is BankResponse selectedBank)
            {
                System.Diagnostics.Debug.WriteLine($"Selected Bank: {selectedBank.name}, Code: {selectedBank.bankCode}");

                // Reset verification state when bank changes
                AccountnameHolder.IsVisible = false;
                accountHolderName.Text = string.Empty;
                Addbanksaccounts.IsVisible = false;
                Addbanksaccounts.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Addbankname_SelectedIndexChanged Error: {ex.Message}");
        }
    }

    private async void accountnumber_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            var entry = sender as Entry;
            if (entry != null)
            {
                // Remove any non-numeric characters
                var numericOnly = new string(e.NewTextValue.Where(char.IsDigit).ToArray());

                if (numericOnly != e.NewTextValue)
                {
                    entry.Text = numericOnly;
                    return; // Exit to avoid double processing
                }

                // Reset verification state when user modifies the account number
                if (e.OldTextValue != e.NewTextValue)
                {
                    AccountnameHolder.IsVisible = false;
                    accountHolderName.Text = string.Empty;
                    Addbanksaccounts.IsVisible = false;
                    Addbanksaccounts.IsEnabled = false;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"accountnumber_TextChanged Error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        try
        {
            base.OnDisappearing();
            // Clean up any ongoing operations
            _httpClient?.CancelPendingRequests();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnDisappearing Error: {ex.Message}");
        }
    }

    protected override void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            // Refresh data when page appears
            if (_currentRecipient == null)
            {
                LoadInitialData();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnAppearing Error: {ex.Message}");
        }
    }

    private async void accountnumber_Unfocused(object sender, FocusEventArgs e)
    {
        try
        {
            var selectedBank = Addbankname.SelectedItem as BankResponse;

            // Validate bank selection first
            if (selectedBank == null)
            {
                await ShowErrorSnackbarAsync("Please select a bank first");
                return;
            }

            if (string.IsNullOrWhiteSpace(accountnumber?.Text))
            {
                // Hide all verification-related UI elements
                AccountnameHolder.IsVisible = false;
                accountHolderName.Text = string.Empty;
                Addbanksaccounts.IsVisible = false;
                Addbanksaccounts.IsEnabled = false;
                return;
            }

            // Validate account number length
            if (accountnumber.Text.Length < 10)
            {
                await ShowErrorSnackbarAsync("Account number must be at least 10 digits");
                AccountnameHolder.IsVisible = false;
                Addbanksaccounts.IsVisible = false;
                Addbanksaccounts.IsEnabled = false;
                return;
            }

            // Show loading state
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                AccountnameHolder.IsVisible = true;
                accountHolderName.Text = "Verifying account number...";
                accountHolderName.TextColor = Colors.Orange;
                accountHolderName.FontAttributes = FontAttributes.Italic;

                // Ensure buttons are hidden during verification
                Addbanksaccounts.IsVisible = false;
                Addbanksaccounts.IsEnabled = false;

                await accountHolderName.FadeTo(0.5, 200);
            });

            // Bypass SSL validation (DEV / TEST ONLY)
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            string url = $"https://opxng.com/api/agencies/validate-account?bankCode={selectedBank.bankCode}&AccountNumber={accountnumber.Text}";

            System.Diagnostics.Debug.WriteLine($"Verifying account: {url}");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) })
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    System.Diagnostics.Debug.WriteLine($"Verification response status: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Verification response: {json}");

                        accountnumberverify result = JsonConvert.DeserializeObject<accountnumberverify>(json);

                        if (result?.isValid == true && !string.IsNullOrEmpty(result.accountName))
                        {
                            // SUCCESS - Show verified account name and enable buttons
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                accountHolderName.Text = result.accountName;
                                accountHolderName.TextColor = Colors.ForestGreen;
                                accountHolderName.FontAttributes = FontAttributes.Bold;
                                await accountHolderName.FadeTo(1, 200);

                                // Show success indicator
                                await ShowSuccessSnackbarAsync($"Account verified: {result.accountName}");

                                // Enable and show the Save/Cancel buttons
                                Addbanksaccounts.IsVisible = true;
                                Addbanksaccounts.IsEnabled = true;
                                Addbanksaccounts.Opacity = 0;
                                await Addbanksaccounts.FadeTo(1, 300);
                            });
                        }
                        else
                        {
                            // Invalid account
                            await HandleAccountVerificationError("Invalid account number. Please check and try again.");
                        }
                    }
                    else
                    {
                        // Server error
                        await HandleAccountVerificationError($"Verification failed: {response.StatusCode}");
                    }
                }
            }
        }
        catch (TaskCanceledException)
        {
            await HandleAccountVerificationError("Verification timed out. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            await HandleAccountVerificationError("Network error during verification. Please check your connection.");
            System.Diagnostics.Debug.WriteLine($"Account verification network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            await HandleAccountVerificationError("Unexpected error during verification.");
            System.Diagnostics.Debug.WriteLine($"accountnumber_Unfocused Error: {ex.Message}\n{ex.StackTrace}");
        }
    }
    private async Task HandleAccountVerificationError(string message)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Show error message
                accountHolderName.Text = $"✗ {message}";
                accountHolderName.TextColor = Colors.Red;
                accountHolderName.FontAttributes = FontAttributes.Bold;
                await accountHolderName.FadeTo(1, 200);
                // Hide and disable buttons
                Addbanksaccounts.IsVisible = false;
                Addbanksaccounts.IsEnabled = false;
            });

            // Show error snackbar
            var snackbar = Snackbar.Make(
                message,
                null,
                "OK",
                TimeSpan.FromSeconds(5),
                new SnackbarOptions
                {
                    BackgroundColor = Colors.DarkRed,
                    TextColor = Colors.White,
                    ActionButtonTextColor = Colors.White,
                    CornerRadius = new CornerRadius(8),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                });
            await snackbar.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error handling verification error: {ex.Message}");
        }
    }


}

// Data Models

public class BankResponse
{
    public string id { get; set; }
    public string name { get; set; }
    public string bankCode { get; set; }
}

public class accountnumberverify
{

    public bool isValid { get; set; }
    public string accountName { get; set; }
}

public class RecipientData
{
    public string AccountNumber { get; set; }
    public string AccountName { get; set; }
    public string bankName { get; set; }
    public string bankCode { get; set; }
    public string Email { get; set; }
}

public class BankDataObject
{
    public string BankCode { get; set; }
    public string AccountNumber { get; set; }
    public string Email { get; set; }
}

public class ApiResponse
{
    public bool success { get; set; }
    public string message { get; set; }
    public object data { get; set; }
}