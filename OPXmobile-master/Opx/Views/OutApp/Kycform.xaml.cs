using AiForms.Dialogs;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Opx.Model;
using Opx.Services;
using Snackbar = CommunityToolkit.Maui.Alerts.Snackbar;

namespace Opx.Views;

public partial class Kycform : ContentPage, INotifyPropertyChanged
{
    #region Private Fields
    private bool _isDisposed = false;
    private bool _isProcessing = false;
    private CancellationTokenSource _cancellationTokenSource;
    public static string mymail { get; set; }
    private const int MAX_RETRY_ATTEMPTS = 3;

    // TODO: set to the VAPlatform value given in the updated OPX endpoint doc. Until then the field is omitted
    // and the API will keep answering "The VAPlatform field is required."
    private const string? VaPlatform = null;
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
        await Navigation.PushModalAsync(new Views.Home());
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

            // If /agencies/create did not auto-link the virtual account, link it now with the
            // token + accountReference it returned. This runs silently in the background while the
            // success sheet is showing, so the user never sees (or types) the token.
            var autoLinked = response.NextStep == "set-recipient-account";
            var needsLink = !autoLinked
                            && !string.IsNullOrWhiteSpace(response.Token)
                            && !string.IsNullOrWhiteSpace(response.AccountReference);

            Task<LinkVirtualAccountResponse?>? linkTask = needsLink
                ? Task.Run(() => LinkVirtualAccountSilentlyAsync(response.Token!, response.AccountReference!))
                : null;

            // Show sophisticated success display sheet
            await ShowSuccessDisplaySheet(response);

            await Task.Delay(2000);

            if (linkTask != null)
            {
                var link = await linkTask;
                autoLinked = link != null && (link.Success || link.AlreadyLinked);
            }

            if (autoLinked)
            {
                // Virtual account is linked: go straight to payout account setup.
                await Application.Current.MainPage.ShowPopupAsync(new AddAccount());
            }
            else if (needsLink)
            {
                // Silent link failed after all retries: only now fall back to the manual token form.
                await Application.Current.MainPage.ShowPopupAsync(
                    new BvnToken(response.Token!, response.AccountReference!));
            }
            else
            {
                // Fallback – refresh dashboard.
                await NavigateToLoginPage();
            }
        });
    }

    /// <summary>
    /// POST /api/agencies/link-virtual-account with the token + accountRef returned by /agencies/create.
    /// Fully silent: retries network/5xx failures (3 attempts), never throws, returns null if it could not link.
    /// 200 with success=true OR alreadyLinked=true both count as linked.
    /// </summary>
    private async Task<LinkVirtualAccountResponse?> LinkVirtualAccountSilentlyAsync(string token, string accountRef)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var result = await OpxApi.PostAsync<LinkVirtualAccountResponse>(
                    "/agencies/link-virtual-account",
                    new LinkVirtualAccountRequest
                    {
                        Email = LoginPage.myemail ?? "",
                        Token = token.Trim(),
                        AccountRef = accountRef.Trim()
                    });

                if (result.IsHttpSuccess && result.Data is { } data && (data.Success || data.AlreadyLinked))
                    return data;

                // 400 / 401 will not fix themselves on retry.
                if (!result.IsNetworkError && (int)result.StatusCode is >= 400 and < 500)
                {
                    System.Diagnostics.Debug.WriteLine($"[LINK] rejected {(int)result.StatusCode}: {result.ErrorMessage}");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"[LINK] attempt {attempt}/{maxAttempts} failed: {result.ErrorMessage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LINK] attempt {attempt}/{maxAttempts} error: {ex.Message}");
            }

            if (attempt < maxAttempts)
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
        }

        return null;
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
                OpxApiException apiEx => apiEx.Message,
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
                Application.Current.MainPage = new NavigationPage(new Home())
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
        if (string.IsNullOrWhiteSpace(bvn))
            throw new ArgumentException("BVN cannot be empty.", nameof(bvn));

        if (string.IsNullOrWhiteSpace(LoginPage.myemail))
            throw new InvalidOperationException("User email not found. Please log in again.");

        var result = await OpxApi.PostAsync<BvnApiResponse>(
            "/agencies/create",
            new BvnRequest { bvn = bvn, Email = LoginPage.myemail, VAPlatform = VaPlatform },
            cancellationToken);

        if (result.IsNetworkError)
            throw new HttpRequestException(result.ErrorMessage);

        if (!result.IsHttpSuccess || result.Data == null)
            throw new OpxApiException(result.StatusCode, result.ErrorMessage);

        System.Diagnostics.Debug.WriteLine($"[PARSED RESPONSE] Success: {result.Data.Success}, NextStep: {result.Data.NextStep}");
        return result.Data;
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

            if (response.Success)
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

        // Required by the updated /agencies/create endpoint ("The VAPlatform field is required.")
        [JsonProperty("vaPlatform", NullValueHandling = NullValueHandling.Ignore)]
        public string? VAPlatform { get; set; }
    }

    /// <summary>The API answered, but with an error (as opposed to a network failure). Message is already user-readable.</summary>
    internal class OpxApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public OpxApiException(HttpStatusCode statusCode, string message) : base(message) => StatusCode = statusCode;
    }

    public class BvnApiResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("token")]
        public string? Token { get; set; }

        [JsonProperty("accountReference")]
        public string? AccountReference { get; set; }

        [JsonProperty("bankName")]
        public string? BankName { get; set; }

        [JsonProperty("accountNumber")]
        public string? AccountNumber { get; set; }

        [JsonProperty("nextStep")]
        public string? NextStep { get; set; }
        public string? Agency { get; internal set; }
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
        }
    }
    #endregion
}