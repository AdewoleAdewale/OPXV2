using AiForms.Dialogs;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Snackbar = CommunityToolkit.Maui.Alerts.Snackbar;

namespace Opx.Views;

public partial class Kycform : ContentPage, INotifyPropertyChanged
{
    #region Private Fields
    private bool _isDisposed = false;
    private bool _isProcessing = false;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly HttpClient _httpClient;
    public static string mymail { get; set; }
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const int REQUEST_TIMEOUT_SECONDS = 30;
    private const string BVN_PATTERN = @"^\d{11}$";
    #endregion

    #region Properties
    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            if (_isProcessing != value)
            {
                _isProcessing = value;
                OnPropertyChanged();
                UpdateProcessingUI(value);
            }
        }
    }
    #endregion

    #region UI Update Methods
    private void UpdateProcessingUI(bool isProcessing)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (buttonverify != null)
            {
                buttonverify.IsVisible = !isProcessing;
            }

            if (ProcessingStack != null)
            {
                ProcessingStack.IsVisible = isProcessing;
            }

            if (ProcessingIndicator != null)
            {
                ProcessingIndicator.IsRunning = isProcessing;
            }
        });
    }
    #endregion

    #region Constructor
    public Kycform()
    {
        InitializeComponent();
        BindingContext = this;

        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS)
        };

        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "OpxMobileApp/1.0");

        InitializeComponents();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        buttonverify.Text = "VERIFY BVN";
        buttonverify.TextColor = Colors.White;
        _cancellationTokenSource = new CancellationTokenSource();
    }
    #endregion

    #region Navigation Handling
    protected override bool OnBackButtonPressed()
    {
        if (IsProcessing)
        {
            _cancellationTokenSource?.Cancel();
            return true;
        }

        HandleBackNavigation();
        return true;
    }

    private bool HandleBackNavigation()
    {
        Device.BeginInvokeOnMainThread(async () =>
        {
            bool result = await DisplayAlert("NOTIFICATION", "Are you sure you want to exit the application?", "Yes", "No");
            if (result)
            {
                Application.Current.MainPage = new NavigationPage(new Home());
            }
        });
        return true;
    }
    #endregion

    #region Event Handlers
    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        if (IsProcessing)
        {
            await ShowWarningSnackbar("Please wait", "A verification process is already in progress.");
            return;
        }

        await ProcessBvnVerificationAsync();
    }

    private async void OnBackButtonTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushModalAsync(new Views.DashBoard());
    }

    private void OnBvnTextChanged(object sender, TextChangedEventArgs e)
    {
        ValidateInputRealTime();
    }

    private async void OnSupportTapped(object sender, TappedEventArgs e)
    {
        try
        {
            var action = await DisplayActionSheet(
                "Need Help?",
                "Cancel",
                null,
                "Call Support",
                "Email Support",
                "Live Chat",
                "FAQ"
            );

            switch (action)
            {
                case "Call Support":
                    await OpenDialerAsync("+2341234567890");
                    break;
                case "Email Support":
                    await OpenEmailAsync("support@opxng.com");
                    break;
                case "Live Chat":
                    await OpenChatAsync();
                    break;
                case "FAQ":
                    await OpenFaqAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            await ShowErrorSnackbar("Unable to open support options");
        }
    }

    private void ValidateInputRealTime()
    {
        try
        {
            var bvnText = BVN?.Text?.Trim();

            if (string.IsNullOrEmpty(bvnText))
            {
                HideValidationMessage();
                return;
            }

            if (bvnText.Length > 11)
            {
                BVN.Text = bvnText.Substring(0, 11);
                return;
            }

            if (!bvnText.All(char.IsDigit))
            {
                ShowValidationMessage("BVN must contain only numbers", false);
            }
            else if (bvnText.Length < 11)
            {
                ShowValidationMessage($"BVN must be 11 digits ({bvnText.Length}/11)", false);
            }
            else
            {
                ShowValidationMessage("✓ Valid BVN format", true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Validation error: {ex.Message}");
        }
    }

    private void ShowValidationMessage(string message, bool isValid)
    {
        if (ValidationLabel != null)
        {
            ValidationLabel.Text = message;
            ValidationLabel.TextColor = isValid ? Color.FromArgb("#28A745") : Color.FromArgb("#DC3545");
            ValidationLabel.IsVisible = true;
        }
    }

    private void HideValidationMessage()
    {
        if (ValidationLabel != null)
        {
            ValidationLabel.IsVisible = false;
        }
    }

    private async Task OpenDialerAsync(string phoneNumber)
    {
        try
        {
            if (PhoneDialer.Default.IsSupported)
            {
                PhoneDialer.Default.Open(phoneNumber);
            }
            else
            {
                await Launcher.OpenAsync($"tel:{phoneNumber}");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorSnackbar("Unable to open dialer");
        }
    }

    private async Task OpenEmailAsync(string email)
    {
        try
        {
            var emailMessage = new EmailMessage
            {
                Subject = "BVN Verification Support",
                Body = "I need help with BVN verification process.",
                To = new List<string> { email }
            };

            await Email.Default.ComposeAsync(emailMessage);
        }
        catch (Exception ex)
        {
            await ShowErrorSnackbar("Unable to open email client");
        }
    }

    private async Task OpenChatAsync()
    {
        try
        {
            await Launcher.OpenAsync("https://opxng.com/support/chat");
        }
        catch (Exception ex)
        {
            await ShowErrorSnackbar("Unable to open chat");
        }
    }

    private async Task OpenFaqAsync()
    {
        try
        {
            await Launcher.OpenAsync("https://opxng.com/support/faq");
        }
        catch (Exception ex)
        {
            await ShowErrorSnackbar("Unable to open FAQ");
        }
    }
    #endregion

    #region BVN Processing
    private async Task ProcessBvnVerificationAsync()
    {
        try
        {
            IsProcessing = true;
            _cancellationTokenSource = new CancellationTokenSource();

            mymail = LoginPage.myemail;

            if (string.IsNullOrWhiteSpace(mymail))
            {
                await ShowErrorSnackbar("Email not found. Please login again.");
                return;
            }

            var validationResult = ValidateBvnInput();
            if (!validationResult.IsValid)
            {
                await ShowErrorSnackbar(validationResult.ErrorMessage);
                return;
            }

            ConfigureLoadingDialog();

            await Loading.Instance.StartAsync(async progress =>
            {
                try
                {
                    await ProcessBvnWithRetryAsync(BVN.Text.Trim(), progress, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    await ShowInfoSnackbar("Operation cancelled", "The verification process was cancelled.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"BVN processing error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    await ShowErrorSnackbar("Verification failed. Please try again.");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Critical error in ProcessBvnVerificationAsync: {ex}");
            await ShowErrorSnackbar("Critical error occurred. Please restart and try again.");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task ProcessBvnWithRetryAsync(string bvn, IProgress<double> progress, CancellationToken cancellationToken)
    {
        Exception lastException = null;

        for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                progress?.Report(0.1d + (attempt - 1) * 0.2d);

                System.Diagnostics.Debug.WriteLine($"=== Attempt {attempt} ===");
                var response = await CallBvnApiAsync(bvn, cancellationToken);

                progress?.Report(0.8d);

                await HandleBvnResponseAsync(response);

                progress?.Report(1.0d);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex) when (attempt < MAX_RETRY_ATTEMPTS)
            {
                lastException = ex;
                System.Diagnostics.Debug.WriteLine($"Retry attempt {attempt} failed: {ex.Message}");

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                lastException = ex;
                System.Diagnostics.Debug.WriteLine($"Error on attempt {attempt}: {ex.Message}");
                break;
            }
        }

        await HandleApiFailureAsync(lastException);
    }

    private async Task HandleSuccessfulVerificationAsync(BvnApiResponse response)
    {
        await Dispatcher.DispatchAsync(async () =>
        {
            var username = response.AccountReference ?? response.Agency ?? "User";

            System.Diagnostics.Debug.WriteLine($"SUCCESS: BVN verified successfully for {username}");

            // Show sophisticated success display sheet
            await ShowSuccessDisplaySheet(response);

            // Wait for display sheet animation
            await Task.Delay(3500);

            // Navigate to login page
            await NavigateToLoginPage();
        });
    }

    private async Task HandleFailedVerificationAsync(BvnApiResponse response)
    {
        await Dispatcher.DispatchAsync(async () =>
        {
            var message = !string.IsNullOrEmpty(response.Message)
                ? response.Message
                : "BVN verification failed. Please check your BVN and try again.";

            System.Diagnostics.Debug.WriteLine($"FAILED: {message}");

            // Show sophisticated failure display sheet
            await ShowFailureDisplaySheet(response);

            // Wait for display sheet animation
            await Task.Delay(3500);

            // Navigate to login page
            await NavigateToLoginPage();
        });
    }

    private async Task HandleApiFailureAsync(Exception exception)
    {
        await Dispatcher.DispatchAsync(async () =>
        {
            var message = exception switch
            {
                TaskCanceledException when exception.InnerException is TimeoutException =>
                    "Request timed out. Please check your internet connection.",
                HttpRequestException httpEx =>
                    $"Network error: {httpEx.Message}. Please check your connection.",
                JsonException =>
                    "Invalid response from server. Please try again.",
                _ =>
                    $"An error occurred: {exception.Message}"
            };

            System.Diagnostics.Debug.WriteLine($"API FAILURE: {message}");
            System.Diagnostics.Debug.WriteLine($"Exception: {exception}");

            // Show error display sheet
            var errorResponse = new BvnApiResponse
            {
                Success = false,
                Message = message
            };

            await ShowFailureDisplaySheet(errorResponse);

            // Wait for display sheet animation
            await Task.Delay(3500);

            // Navigate to login page
            await NavigateToLoginPage();
        });
    }

    private async Task NavigateToLoginPage()
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                System.Diagnostics.Debug.WriteLine("Navigating to DashBoard...");

                // Clear navigation stack and set DashBoard as main page
                Application.Current.MainPage = new NavigationPage(new DashBoard())
                {
                    BarBackgroundColor = Color.FromArgb("#A25AC4"),
                    BarTextColor = Colors.White
                };
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
        }
    }
    #endregion

    #region Display Sheets
    private async Task ShowSuccessDisplaySheet(BvnApiResponse response)
    {
        try
        {
            var displaySheet = new SuccessDisplaySheet(response);
            await Application.Current.MainPage.ShowPopupAsync(displaySheet);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing success sheet: {ex.Message}");
        }
    }

    private async Task ShowFailureDisplaySheet(BvnApiResponse response)
    {
        try
        {
            var displaySheet = new FailureDisplaySheet(response);
            await Application.Current.MainPage.ShowPopupAsync(displaySheet);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing failure sheet: {ex.Message}");
        }
    }
    #endregion

    #region Validation
    private ValidationResult ValidateBvnInput()
    {
        var bvnText = BVN?.Text?.Trim();

        if (string.IsNullOrWhiteSpace(bvnText))
        {
            return new ValidationResult(false, "Please enter your BVN");
        }

        if (!Regex.IsMatch(bvnText, BVN_PATTERN))
        {
            return new ValidationResult(false, "BVN must be exactly 11 digits");
        }

        return new ValidationResult(true, string.Empty);
    }

    private class ValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }

        public ValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }
    #endregion

    #region UI Helpers
    private void ConfigureLoadingDialog()
    {
        Configurations.LoadingConfig = new LoadingConfig
        {
            Opacity = 0.7,
            DefaultMessage = "Verifying your BVN securely...",
            FontSize = 15,
            IndicatorColor = Color.FromArgb("#A25AC4"),
            OverlayColor = Color.FromArgb("#90000000"),
            FontColor = Colors.White
        };
    }

    private async Task ShowSuccessSnackbar(string message, string actionText = "OK")
    {
        if (_isDisposed) return;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var snackbar = Snackbar.Make(
                    message,
                    null,
                    actionText,
                    TimeSpan.FromSeconds(5),
                    new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb("#28A745"),
                        TextColor = Colors.White,
                        ActionButtonTextColor = Colors.White,
                        CornerRadius = new CornerRadius(12),
                        Font = Microsoft.Maui.Font.SystemFontOfSize(15, FontWeight.Medium),
                        ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14, FontWeight.Bold)
                    });

                await snackbar.Show(_cancellationTokenSource.Token);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Snackbar error: {ex.Message}");
        }
    }

    private async Task ShowInfoSnackbar(string title, string message, string actionText = "OK")
    {
        if (_isDisposed) return;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var snackbar = Snackbar.Make(
                    $"{title}: {message}",
                    null,
                    actionText,
                    TimeSpan.FromSeconds(4),
                    new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb("#17A2B8"),
                        TextColor = Colors.White,
                        ActionButtonTextColor = Colors.White,
                        CornerRadius = new CornerRadius(12),
                        Font = Microsoft.Maui.Font.SystemFontOfSize(15, FontWeight.Medium),
                        ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14, FontWeight.Bold)
                    });

                await snackbar.Show(_cancellationTokenSource.Token);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Snackbar error: {ex.Message}");
        }
    }

    private async Task ShowWarningSnackbar(string title, string message, string actionText = "OK")
    {
        if (_isDisposed) return;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var snackbar = Snackbar.Make(
                    $"{title}: {message}",
                    null,
                    actionText,
                    TimeSpan.FromSeconds(4),
                    new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb("#FFC107"),
                        TextColor = Color.FromArgb("#1A1A1A"),
                        ActionButtonTextColor = Color.FromArgb("#1A1A1A"),
                        CornerRadius = new CornerRadius(12),
                        Font = Microsoft.Maui.Font.SystemFontOfSize(15, FontWeight.Medium),
                        ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14, FontWeight.Bold)
                    });

                await snackbar.Show(_cancellationTokenSource.Token);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Snackbar error: {ex.Message}");
        }
    }

    private async Task ShowErrorSnackbar(string message, string actionText = "DISMISS")
    {
        if (_isDisposed) return;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var snackbar = Snackbar.Make(
                    message,
                    null,
                    actionText,
                    TimeSpan.FromSeconds(6),
                    new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb("#DC3545"),
                        TextColor = Colors.White,
                        ActionButtonTextColor = Colors.White,
                        CornerRadius = new CornerRadius(12),
                        Font = Microsoft.Maui.Font.SystemFontOfSize(15, FontWeight.Medium),
                        ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14, FontWeight.Bold)
                    });

                await snackbar.Show(_cancellationTokenSource.Token);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Snackbar error: {ex.Message}");
        }
    }
    #endregion

    #region API Communication

    private async Task<BvnApiResponse> CallBvnApiAsync(string bvn, CancellationToken cancellationToken)
    {
        const string apiUrl = "https://opxng.com/api/agencies/create";
        try
        {
            if (string.IsNullOrWhiteSpace(bvn))
                throw new ArgumentException("BVN cannot be empty.", nameof(bvn));

            if (string.IsNullOrWhiteSpace(LoginPage.myemail))
                throw new InvalidOperationException("User Email Not Found. Please log in again.");

            var requestPayload = new BvnRequest
            {
                bvn = bvn,
                Email = LoginPage.myemail
            };

            var jsonPayload = JsonConvert.SerializeObject(requestPayload, Formatting.None);

            // ✅ Fix SSL issues with HttpClientHandler
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) })
            {
                // Force TLS 1.2 or 1.3
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                // Log request details
                System.Diagnostics.Debug.WriteLine($"[API REQUEST] {apiUrl}");
                System.Diagnostics.Debug.WriteLine(jsonPayload);

                HttpResponseMessage response = null;

                // Retry policy (3 attempts for transient network errors)
                const int maxRetries = 3;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                        response = await client.PostAsync(apiUrl, content, cancellationToken);
                        break; // Success → exit retry loop
                    }
                    catch (HttpRequestException ex) when (attempt < maxRetries)
                    {
                        System.Diagnostics.Debug.WriteLine($"Network error (attempt {attempt}): {ex.Message}. Retrying...");
                        await Task.Delay(1000 * attempt, cancellationToken); // incremental backoff
                    }
                }

                if (response == null)
                    throw new HttpRequestException("No response received from server after multiple attempts.");

                // Log response
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[API RESPONSE] {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine(responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    try
                    {
                        var errorResponse = JsonConvert.DeserializeObject<BvnApiResponse>(responseContent);
                        var message = errorResponse?.Message ?? "Unknown API error.";
                        throw new HttpRequestException($"API Error ({response.StatusCode}): {message}");
                    }
                    catch (JsonException)
                    {
                        throw new HttpRequestException($"API returned {response.StatusCode}: {responseContent}");
                    }
                }

                if (string.IsNullOrWhiteSpace(responseContent))
                    throw new InvalidOperationException("Empty response received from the server.");

                var bvnResponse = DeserializeResponse(responseContent)
                    ?? throw new JsonException("Failed to parse server response (null).");

                System.Diagnostics.Debug.WriteLine($"[PARSED RESPONSE] Success: {bvnResponse.Success}, Message: {bvnResponse.Message}");

                return bvnResponse;
            }
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            System.Diagnostics.Debug.WriteLine($"Request Timeout: {ex.Message}");
            throw new HttpRequestException("Request timed out. Please check your internet connection.", ex);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP Error: {ex.Message}");
            throw new HttpRequestException($"Network error: {ex.Message}. Please try again.", ex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected Error: {ex.GetType().Name} - {ex.Message}");
            throw new Exception("An unexpected error occurred while contacting the server.", ex);
        }
    }
    private BvnApiResponse DeserializeResponse(string responseContent)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<BvnApiResponse>(responseContent);
            if (response != null)
            {
                return response;
            }

            var dynamicResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
            if (dynamicResponse != null)
            {
                return new BvnApiResponse
                {
                    Success = dynamicResponse.success?.ToString() ??
                             dynamicResponse.status?.ToString() ??
                             string.Empty,
                    Message = dynamicResponse.message?.ToString() ??
                             dynamicResponse.msg?.ToString() ??
                             string.Empty,
                    Agency = dynamicResponse.agency?.ToString() ??
                            dynamicResponse.username?.ToString() ??
                            string.Empty,
                    AgencyToken = dynamicResponse.agencyToken?.ToString() ??
                                 dynamicResponse.token?.ToString() ??
                                 string.Empty,
                    NextStep = dynamicResponse.nextStep?.ToString() ??
                              string.Empty,
                    AccountReference = dynamicResponse.accountReference?.ToString() ??
                                      string.Empty,
                    bankName = dynamicResponse.BankName?.ToString() ??
                              string.Empty,
                    accountNumber = dynamicResponse.AccountNumber?.ToString() ??
                                   string.Empty
                };
            }

            throw new JsonException("Unable to deserialize response");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Deserialization error: {ex.Message}");
            throw;
        }
    }

    private async Task HandleBvnResponseAsync(BvnApiResponse response)
    {
        await Dispatcher.DispatchAsync(async () =>
        {
            System.Diagnostics.Debug.WriteLine($"=== HANDLING RESPONSE ===");
            System.Diagnostics.Debug.WriteLine($"Success: {response?.Success}");

            if (response == null)
            {
                await ShowErrorSnackbar("Invalid response from server");
                return;
            }

            bool isSuccess = response.Success ||
                           (!string.IsNullOrEmpty(response.Success.ToString()) &&
                            (response.Success.ToString().Equals("true", StringComparison.OrdinalIgnoreCase) ||
                             response.Success.ToString() == "1" ||
                             response.Success.ToString().Equals("success", StringComparison.OrdinalIgnoreCase)));

            if (isSuccess)
            {
                await HandleSuccessfulVerificationAsync(response);
            }
            else
            {
                await HandleFailedVerificationAsync(response);
            }
        });
    }
    #endregion

    #region Data Models
    internal class BvnRequest
    {
        [JsonProperty("bvn")]
        public string bvn { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;
    }

    public class BvnApiResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("agency")]
        public string? Agency { get; set; }

        [JsonProperty("agencyToken")]
        public string? AgencyToken { get; set; }

        [JsonProperty("nextStep")]
        public string? NextStep { get; set; }

        [JsonProperty("token")]
        public string? Token { get; set; }

        [JsonProperty("accountReference")]
        public string? AccountReference { get; set; }

        [JsonProperty("BankName")]
        public string? bankName { get; set; }

        [JsonProperty("AccountNumber")]
        public string? accountNumber { get; set; }
    }
    #endregion

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    #region Disposal
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isDisposed = true;
        _cancellationTokenSource?.Cancel();
    }

    ~Kycform()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            _isDisposed = true;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _httpClient?.Dispose();
        }
    }
    #endregion
}