using AiForms.Dialogs;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Opx.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Opx.Views;

public partial class BvnToken : Popup, IDisposable, INotifyPropertyChanged
{
    private readonly ILogger<BvnToken>? _logger;
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private CancellationTokenSource _cancellationTokenSource = new();
    private bool _isProcessing = false;
    private bool _isDisposed = false;
    private string _tokenText = string.Empty;
    private string _referenceText = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError = false;
    private bool _isValidating = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    // Bindable properties for better UI experience
    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            _isProcessing = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanValidate));
        }
    }

    public bool IsValidating
    {
        get => _isValidating;
        set
        {
            _isValidating = value;
            OnPropertyChanged();
        }
    }

    public string TokenText
    {
        get => _tokenText;
        set
        {
            _tokenText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanValidate));
            ValidateInput();
        }
    }

    public string ReferenceText
    {
        get => _referenceText;
        set
        {
            _referenceText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanValidate));
            ValidateInput();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError
    {
        get => _hasError;
        set
        {
            _hasError = value;
            OnPropertyChanged();
        }
    }

    public bool CanValidate => !IsProcessing && !string.IsNullOrWhiteSpace(TokenText) && !string.IsNullOrWhiteSpace(ReferenceText);

    public BvnToken(string token, string accountRef) : this()
    {
        TokenText = token;
        ReferenceText = accountRef;
    }

    public BvnToken(ILogger<BvnToken>? logger = null)
    {
        _logger = logger;

        try
        {
            InitializeComponent();
            SetupBindings();
            SetupValidation();
            LogInfo("BvnToken popup initialized successfully");
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to initialize BvnToken popup");
            _ = ShowErrorMessage("Failed to initialize the validation form. Please try again.");
        }
    }

    private void SetupBindings()
    {
        try
        {
            // Bind properties to UI elements
            bvntoken.SetBinding(Entry.TextProperty, new Binding(nameof(TokenText), source: this));
            refbvn.SetBinding(Entry.TextProperty, new Binding(nameof(ReferenceText), source: this));
            ErrorLabel.SetBinding(Label.TextProperty, new Binding(nameof(ErrorMessage), source: this));
            ErrorLabel.SetBinding(Label.IsVisibleProperty, new Binding(nameof(HasError), source: this));
            ResetButton.SetBinding(Button.IsEnabledProperty, new Binding(nameof(CanValidate), source: this));
            LoadingIndicator.SetBinding(ActivityIndicator.IsRunningProperty, new Binding(nameof(IsValidating), source: this));
            LoadingIndicator.SetBinding(ActivityIndicator.IsVisibleProperty, new Binding(nameof(IsValidating), source: this));
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to setup data bindings");
        }
    }

    private void SetupValidation()
    {
        try
        {
            // Real-time validation
            bvntoken.TextChanged += (s, e) => ValidateInput();
            refbvn.TextChanged += (s, e) => ValidateInput();

            // Input formatting
            bvntoken.TextChanged += (s, e) =>
            {
                if (s is Entry entry)
                {
                    // Remove non-alphanumeric characters for token
                    var cleanText = Regex.Replace(entry.Text ?? "", @"[^a-zA-Z0-9]", "");
                    if (cleanText != entry.Text)
                    {
                        entry.Text = cleanText;
                    }
                }
            };

            refbvn.TextChanged += (s, e) =>
            {
                if (s is Entry entry)
                {
                    // Format reference text (remove special characters except dashes and underscores)
                    var cleanText = Regex.Replace(entry.Text ?? "", @"[^a-zA-Z0-9\-_]", "");
                    if (cleanText != entry.Text)
                    {
                        entry.Text = cleanText;
                    }
                }
            };
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to setup input validation");
        }
    }

    private void ValidateInput()
    {
        try
        {
            ClearError();

            var token = TokenText?.Trim();
            var reference = ReferenceText?.Trim();

            if (string.IsNullOrEmpty(token) && string.IsNullOrEmpty(reference))
            {
                return; // No validation needed for empty fields initially
            }

            if (!string.IsNullOrEmpty(token) && token.Length < 4)
            {
                SetError("Token must be at least 4 characters long");
                return;
            }

            if (!string.IsNullOrEmpty(token) && token.Length > 200)
            {
                SetError("Token cannot exceed 200 characters");
                return;
            }

            if (!string.IsNullOrEmpty(reference) && reference.Length < 3)
            {
                SetError("Reference must be at least 3 characters long");
                return;
            }

            if (!string.IsNullOrEmpty(reference) && reference.Length > 300)
            {
                SetError("Reference cannot exceed 300 characters");
                return;
            }

            // Validate token format (alphanumeric)
            if (!string.IsNullOrEmpty(token) && !Regex.IsMatch(token, @"^[a-zA-Z0-9]+$"))
            {
                SetError("Token can only contain letters and numbers");
                return;
            }

            // Validate reference format
            if (!string.IsNullOrEmpty(reference) && !Regex.IsMatch(reference, @"^[a-zA-Z0-9\-_]+$"))
            {
                SetError("Reference can only contain letters, numbers, hyphens, and underscores");
                return;
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error during input validation");
        }
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    private async void ResetPassword_Clicked(object sender, EventArgs e)
    {
        if (_isDisposed || !await _processingLock.WaitAsync(100))
        {
            await ShowWarningMessage("Please wait, validation is already in progress...");
            return;
        }

        try
        {
            if (IsProcessing)
            {
                await ShowWarningMessage("Validation is already in progress, please wait...");
                return;
            }

            // Final validation before processing
            if (!CanValidate)
            {
                await ShowErrorMessage("Please fill in all required fields correctly.");
                return;
            }

            if (HasError)
            {
                await ShowErrorMessage("Please fix the validation errors before proceeding.");
                return;
            }

            // Check connectivity first
            if (!await CheckInternetConnectivity())
            {
                await ShowErrorMessage("No internet connection detected. Please check your network and try again.");
                return;
            }

            IsProcessing = true;
            ClearError();

            LogInfo("Starting BVN validation process");
            await ProcessBvnValidation();
        }
        catch (OperationCanceledException)
        {
            LogInfo("BVN validation was cancelled by user");
            await ShowInfoMessage("Validation was cancelled.");
        }
        catch (Exception ex)
        {
            LogError(ex, "Unexpected error during BVN validation");
            await ShowErrorMessage("An unexpected error occurred. Please try again.");
        }
        finally
        {
            IsProcessing = false;
            _processingLock.Release();
        }
    }

    private async Task ProcessBvnValidation()
    {
        try
        {
            IsValidating = true;

            // Configure loading dialog
            ConfigureLoadingDialog();

            await Loading.Instance.StartAsync(async progress =>
            {
                try
                {
                    // Simulate progress with meaningful steps
                    var steps = new[]
                    {
                        ("Validating input data...", 0.1),
                        ("Connecting to server...", 0.3),
                        ("Verifying BVN token...", 0.6),
                        ("Processing response...", 0.9),
                        ("Finalizing validation...", 1.0)
                    };

                    foreach (var (message, progressValue) in steps)
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                            return;

                        Configurations.LoadingConfig.DefaultMessage = message;
                        progress?.Report(progressValue);
                        await Task.Delay(800, _cancellationTokenSource.Token);
                    }

                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        return;

                    var result = await CallBvnValidationAPI();

                    if (result.IsSuccess && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await HandleSuccessfulValidation(result);
                    }
                    else if (!result.IsSuccess)
                    {
                        await ShowErrorMessage(result.ErrorMessage ?? "BVN validation failed. Please try again.");
                    }
                }
                catch (OperationCanceledException)
                {
                    await ShowInfoMessage("Validation was cancelled.");
                }
                catch (Exception ex)
                {
                    LogError(ex, "Error during BVN validation process");
                    await ShowErrorMessage("Validation process failed. Please try again.");
                }
            });
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to start BVN validation process");
            await ShowErrorMessage("Failed to start validation. Please try again.");
        }
        finally
        {
            IsValidating = false;
        }
    }

    private void ConfigureLoadingDialog()
    {
        try
        {
            Configurations.LoadingConfig ??= new LoadingConfig();
            Configurations.LoadingConfig.Opacity = 0.6;
            Configurations.LoadingConfig.OverlayColor = Colors.Black; // Corrected property name  
            Configurations.LoadingConfig.IndicatorColor = Colors.Purple;
            Configurations.LoadingConfig.DefaultMessage = "Initializing validation...";
            Configurations.LoadingConfig.FontSize = 14;
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to configure loading dialog");
        }
    }

    private async Task<ValidationResult> CallBvnValidationAPI()
    {
        try
        {
            var email = LoginPage.myemail;
            if (string.IsNullOrWhiteSpace(email))
                return ValidationResult.Failure("Email not found. Please log in again.");

            var body = new BvnValidationRequest
            {
                token = TokenText.Trim(),
                accountRef = ReferenceText.Trim(),
                email = email
            };

            LogInfo("Sending link-virtual-account request");
            var result = await SessionStore.WithAuthRetryAsync(() =>
                OpxApi.PostAsync<BvnValidationResponse>("/agencies/link-virtual-account", body, _cancellationTokenSource.Token));

            if (result.IsNetworkError || !result.IsHttpSuccess)
            {
                LogWarning($"link-virtual-account failed ({(int)result.StatusCode}): {result.ErrorMessage}");
                return ValidationResult.Failure(string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Could not link the virtual account. Please try again."
                    : result.ErrorMessage);
            }

            var data = result.Data;
            if (data == null)
                return ValidationResult.Failure("Invalid response from server");

            if (data.success || data.alreadyLinked)
            {
                AccountOnboarding.ApplyVirtualAccount(data.bankName, data.accountNumber);
                return ValidationResult.Success(data);
            }

            return ValidationResult.Failure(data.message ?? "Virtual account link failed");
        }
        catch (OperationCanceledException)
        {
            return ValidationResult.Cancelled();
        }
        catch (Exception ex)
        {
            LogError(ex, "Unexpected error while linking the virtual account");
            return ValidationResult.Failure("An unexpected error occurred. Please try again.");
        }
    }

    private static string GetFriendlyErrorMessage(System.Net.HttpStatusCode statusCode, string errorContent)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => "Invalid request data. Please check your input and try again.",
            System.Net.HttpStatusCode.Unauthorized => "Authentication failed. Please verify your credentials.",
            System.Net.HttpStatusCode.Forbidden => "Access denied. Please contact support.",
            System.Net.HttpStatusCode.NotFound => "Service endpoint not found. Please contact support.",
            System.Net.HttpStatusCode.RequestTimeout => "Request timeout. Please try again.",
            System.Net.HttpStatusCode.TooManyRequests => "Too many requests. Please wait a moment and try again.",
            System.Net.HttpStatusCode.InternalServerError => "Server error. Please try again later.",
            System.Net.HttpStatusCode.BadGateway => "Gateway error. Please try again later.",
            System.Net.HttpStatusCode.ServiceUnavailable => "Service temporarily unavailable. Please try again later.",
            System.Net.HttpStatusCode.GatewayTimeout => "Gateway timeout. Please try again later.",
            _ => $"Server error (HTTP {(int)statusCode}). Please try again later."
        };
    }

    private async Task HandleSuccessfulValidation(ValidationResult result)
    {
        try
        {
            await ShowSuccessMessage("Virtual account linked successfully!");
            await Task.Delay(1200, _cancellationTokenSource.Token);

            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            // Hand control back to the KYC flow, which continues with the payout (recipient) account.
            await MainThread.InvokeOnMainThreadAsync(() => Close("linked"));
        }
        catch (OperationCanceledException)
        {
            LogInfo("Success handling was cancelled");
        }
        catch (Exception ex)
        {
            LogError(ex, "Error handling successful validation");
            await ShowErrorMessage("Linked, but the next step could not open. You can continue from the dashboard.");
        }
    }

    private async Task NavigateToLoginPage()
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (Application.Current?.MainPage?.Navigation == null)
                    {
                        throw new InvalidOperationException("Navigation service is not available");
                    }

                    await Application.Current.MainPage.Navigation.PushModalAsync(new LoginPage());
                    LogInfo("Successfully navigated to login page");
                }
                catch (Exception ex)
                {
                    LogError(ex, "Navigation to login page failed");
                    await ShowErrorMessage("Unable to navigate to login page. Please restart the application.");
                }
            });
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to invoke navigation on main thread");
            await ShowErrorMessage("Navigation failed. Please restart the application.");
        }
    }

    private async Task<bool> CheckInternetConnectivity()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await client.GetAsync("https://www.google.com", _cancellationTokenSource.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogError(ex, "Internet connectivity check failed");
            return false;
        }
    }

    private async Task ShowErrorMessage(string message)
    {
        await ShowSnackbar(message, Colors.Red);
    }

    private async Task ShowSuccessMessage(string message)
    {
        await ShowSnackbar(message, Colors.Green);
    }

    private async Task ShowWarningMessage(string message)
    {
        await ShowSnackbar(message, Colors.Orange);
    }

    private async Task ShowInfoMessage(string message)
    {
        await ShowSnackbar(message, Colors.Blue);
    }

    private async Task ShowSnackbar(string message, Color backgroundColor)
    {
        try
        {
            if (string.IsNullOrEmpty(message) || _isDisposed)
                return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var snackbar = Snackbar.Make(
                        message,
                        null,
                        "OK",
                        TimeSpan.FromSeconds(4),
                        new SnackbarOptions
                        {
                            BackgroundColor = backgroundColor,
                            TextColor = Colors.White,
                            ActionButtonTextColor = Colors.White,
                            CornerRadius = new CornerRadius(12),
                            Font = Microsoft.Maui.Font.SystemFontOfSize(15, FontWeight.Medium)
                        });

                    await snackbar.Show(_cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    LogError(ex, "Failed to show snackbar");

                    // Fallback to alert dialog
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Notification", message, "OK");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            LogError(ex, "Error showing message to user");
        }
    }

    private void Cancel_Clicked(object sender, EventArgs e)
    {
        try
        {
            LogInfo("User cancelled BVN validation");

            // Cancel any ongoing operations
            _cancellationTokenSource?.Cancel();

            // Close the popup
            Close();
        }
        catch (Exception ex)
        {
            LogError(ex, "Error handling cancel operation");
        }
        finally
        {
            // Ensure cleanup happens
            Dispose();
        }
    }

    protected override void OnHandlerChanged()
    {
        try
        {
            base.OnHandlerChanged();

            if (Handler == null && !_isDisposed)
            {
                // Popup is being disposed
                _cancellationTokenSource?.Cancel();
                Dispose();
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error in OnHandlerChanged");
        }
    }
    private async Task<bool> CheckInternetConnectionAsync()
    {
        try
        {
            var current = Connectivity.Current.NetworkAccess;

            if (current != NetworkAccess.Internet)
            {
                await ShowWarningMessage("No internet connection. Please check your network settings.");
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

    public void Dispose()
    {
        if (_isDisposed)
            return;

        try
        {
            _isDisposed = true;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _processingLock?.Dispose();

            LogInfo("BvnToken popup disposed successfully");
        }
        catch (Exception ex)
        {
            LogError(ex, "Error disposing BvnToken popup");
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void LogInfo(string message)
    {
        _logger?.LogInformation(message);
        System.Diagnostics.Debug.WriteLine($"[BvnToken] INFO: {message}");
    }

    private void LogWarning(string message)
    {
        _logger?.LogWarning(message);
        System.Diagnostics.Debug.WriteLine($"[BvnToken] WARNING: {message}");
    }

    private void LogError(string message)
    {
        _logger?.LogError(message);
        System.Diagnostics.Debug.WriteLine($"[BvnToken] ERROR: {message}");
    }

    private void LogError(Exception exception, string message)
    {
        _logger?.LogError(exception, message);
        System.Diagnostics.Debug.WriteLine($"[BvnToken] ERROR: {message} - {exception}");
    }

    // Data classes
    internal class BvnValidationRequest
    {
        public string token { get; set; } = "";
        public string accountRef { get; set; } = "";
        public string email { get; set; } = "";

    }

    internal class BvnValidationResponse
    {
        [Newtonsoft.Json.JsonProperty("success")]
        public bool success { get; set; }

        [Newtonsoft.Json.JsonProperty("message")]
        public string? message { get; set; }

        [Newtonsoft.Json.JsonProperty("bankName")]
        public string? bankName { get; set; }

        [Newtonsoft.Json.JsonProperty("accountNumber")]
        public string? accountNumber { get; set; }

        [Newtonsoft.Json.JsonProperty("alreadyLinked")]
        public bool alreadyLinked { get; set; }

        [Newtonsoft.Json.JsonProperty("nextStep")]
        public string? nextStep { get; set; }
    }

    internal class ValidationResult
    {
        public bool IsSuccess { get; set; }
        public bool IsCancelled { get; set; }
        public string? ErrorMessage { get; set; }
        public BvnValidationResponse? Response { get; set; }

        public static ValidationResult Success(BvnValidationResponse response)
        {
            return new ValidationResult
            {
                IsSuccess = true,
                Response = response
            };
        }

        public static ValidationResult Failure(string errorMessage)
        {
            return new ValidationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }

        public static ValidationResult Cancelled()
        {
            return new ValidationResult
            {
                IsSuccess = false,
                IsCancelled = true,
                ErrorMessage = "Operation was cancelled"
            };
        }
    }
}