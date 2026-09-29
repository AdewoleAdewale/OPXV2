using AiForms.Dialogs;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using static Opx.Views.LoginPage;

namespace Opx.Views;

public partial class Verification : ContentPage
{
    #region Private Fields
    private int _secondsRemaining = 300; // 5 minutes
    private bool _isTimerRunning;
    private Entry[] _otpEntries;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly HttpClient _httpClient;
    private const int OTP_LENGTH = 5;
    private const int MAX_RESEND_ATTEMPTS = 3;
    private int _resendAttempts = 0;
    private bool _isProcessing = false;
    #endregion

    #region Constructor
    public Verification()
    {
        InitializeComponent();
        InitializeComponents();
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _cancellationTokenSource = new CancellationTokenSource();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        _otpEntries = new Entry[] { otp1, otp2, otp3, otp4, otp5 };
        SetupOtpEntries();
        StartTimer();

        // Focus on first entry
        Device.BeginInvokeOnMainThread(() => otp1.Focus());
    }

    private void SetupOtpEntries()
    {
        for (int i = 0; i < _otpEntries.Length; i++)
        {
            var entry = _otpEntries[i];
            entry.TextChanged += OnOtpTextChanged;
            entry.Focused += OnOtpEntryFocused;
            entry.Unfocused += OnOtpEntryUnfocused;

            // Add keyboard handling for better UX
            entry.Behaviors.Add(new NumericValidationBehavior());
        }
    }
    #endregion

    #region Timer Management
    private void StartTimer()
    {
        if (_isTimerRunning) return;

        _isTimerRunning = true;
        Device.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return false;

            Device.BeginInvokeOnMainThread(() =>
            {
                _secondsRemaining--;

                if (_secondsRemaining <= 0)
                {
                    timerLabel.Text = "00:00";
                    timerLabel.TextColor = Colors.Red;
                    _isTimerRunning = false;
                    ShowTimerExpiredMessage();
                }
                else
                {
                    var timeSpan = TimeSpan.FromSeconds(_secondsRemaining);
                    timerLabel.Text = timeSpan.ToString(@"mm\:ss");

                    // Change color when time is running out
                    if (_secondsRemaining <= 30)
                        timerLabel.TextColor = Colors.Red;
                    else if (_secondsRemaining <= 60)
                        timerLabel.TextColor = Colors.Orange;
                    else
                        timerLabel.TextColor = Colors.Black;
                }
            });
            return _secondsRemaining > 0;
        });
    }

    private void ResetTimer()
    {
        _secondsRemaining = 300;
        _isTimerRunning = false;
        timerLabel.TextColor = Colors.Black;
        StartTimer();
    }

    private async void ShowTimerExpiredMessage()
    {
        await DisplayAlert("Timer Expired",
            "The OTP has expired. Please request a new code.",
            "OK");
    }
    #endregion

    #region OTP Input Handling
    private void OnOtpTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isProcessing) return;

        // Hide error message when user starts typing
        if (errorLabel != null)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                errorLabel.IsVisible = false;
            });
        }

        var entry = (Entry)sender;
        var newText = e.NewTextValue;

        // Validate input
        if (!string.IsNullOrEmpty(newText) && !Regex.IsMatch(newText, @"^\d$"))
        {
            entry.Text = e.OldTextValue;
            return;
        }

        // Handle navigation
        if (!string.IsNullOrWhiteSpace(newText) && newText.Length == 1)
        {
            MoveToNextEntry(entry);
        }
        else if (string.IsNullOrWhiteSpace(newText))
        {
            MoveToPreviousEntry(entry);
        }

        // Auto-submit when all fields are filled
        if (IsOtpComplete())
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(500); // Small delay for better UX
                await SubmitOtp();
            });
        }
    }

    private void OnOtpEntryFocused(object sender, FocusEventArgs e)
    {
        var entry = (Entry)sender;
        entry.CursorPosition = entry.Text?.Length ?? 0;
    }

    private void OnOtpEntryUnfocused(object sender, FocusEventArgs e)
    {
        // Additional validation if needed
    }

    private void MoveToNextEntry(Entry currentEntry)
    {
        int currentIndex = Array.IndexOf(_otpEntries, currentEntry);
        if (currentIndex < _otpEntries.Length - 1)
        {
            _otpEntries[currentIndex + 1].Focus();
        }
        else
        {
            // Hide keyboard when reaching the last entry
            currentEntry.Unfocus();
        }
    }

    private void MoveToPreviousEntry(Entry currentEntry)
    {
        int currentIndex = Array.IndexOf(_otpEntries, currentEntry);
        if (currentIndex > 0)
        {
            _otpEntries[currentIndex - 1].Focus();
        }
    }

    private bool IsOtpComplete()
    {
        return _otpEntries.All(entry => !string.IsNullOrWhiteSpace(entry.Text));
    }

    private string GetOtpValue()
    {
        return string.Join("", _otpEntries.Select(x => x.Text ?? ""));
    }

    private void ClearOtpEntries()
    {
        foreach (var entry in _otpEntries)
        {
            entry.Text = "";
        }
        _otpEntries[0].Focus();
    }
    #endregion

    #region OTP Submission
    private async void SubmitButton_Clicked(object sender, EventArgs e)
    {
        await SubmitOtp();
    }

    private async Task SubmitOtp()
    {
        if (_isProcessing) return;

        string otp = GetOtpValue();

        // Validate OTP length
        if (otp.Length != OTP_LENGTH)
        {
            await ShowValidationError($"Please enter a complete {OTP_LENGTH}-digit OTP code");
            return;
        }

        // Check if timer expired
        if (_secondsRemaining <= 0)
        {
            await ShowValidationError("OTP has expired. Please request a new code.");
            return;
        }

        await ProcessOtpVerification(otp);
    }

    private async Task ProcessOtpVerification(string otp)
    {
        _isProcessing = true;

        try
        {
            await ShowLoadingDialog("Verifying OTP...");

            var result = await VerifyOtpWithServer(otp);

            if (result.IsSuccess)
            {
                await HandleSuccessfulVerification(result.Response);
            }
            else
            {
                await HandleVerificationError(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            await HandleUnexpectedError(ex);
        }
        finally
        {
            _isProcessing = false;
            await HideLoadingDialog();
        }
    }

    private async Task<ApiResult<OTPResponse>> VerifyOtpWithServer(string otp)
    {
        try
        {
            var requestPayload = new
            {
                email = GetUserEmail(),
                otp = otp
            };

            var jsonPayload = JsonConvert.SerializeObject(requestPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            System.Diagnostics.Debug.WriteLine($"Request URL: https://opxng.com/api/AuthAccount/VerifyEmail");
            System.Diagnostics.Debug.WriteLine($"Request Payload: {jsonPayload}");

            var response = await _httpClient.PostAsync(
                "https://opxng.com/api/AuthAccount/VerifyEmail",
                content,
                _cancellationTokenSource.Token);

            System.Diagnostics.Debug.WriteLine($"Response Status: {response.StatusCode}");

            var responseContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Response Content: {responseContent}");

            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return ApiResult<OTPResponse>.Failure("Empty response from server");
            }

            // FIXED: Check if response is a plain string (error message)
            if (responseContent.StartsWith("\"") && responseContent.EndsWith("\""))
            {
                // It's a plain string error message
                var errorMessage = JsonConvert.DeserializeObject<string>(responseContent);
                return ApiResult<OTPResponse>.Failure(errorMessage ?? "Verification failed");
            }

            // FIXED: Try to parse as JSON object
            try
            {
                var otpResponse = JsonConvert.DeserializeObject<OTPResponse>(responseContent);

                if (otpResponse == null)
                {
                    return ApiResult<OTPResponse>.Failure("Failed to parse server response");
                }

                // Check if verification was successful
                if (!otpResponse.success)
                {
                    return ApiResult<OTPResponse>.Failure(otpResponse.message ?? "Verification failed");
                }

                return ApiResult<OTPResponse>.Success(otpResponse);
            }
            catch (JsonException)
            {
                // If JSON parsing fails, treat the response as an error message
                return ApiResult<OTPResponse>.Failure(responseContent);
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return ApiResult<OTPResponse>.Failure("Request timed out. Please check your internet connection.");
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<OTPResponse>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResult<OTPResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    private async Task HandleSuccessfulVerification(OTPResponse response)
    {
        // Update user information
        if (!string.IsNullOrEmpty(response.email))
            myemail = response.email;

        if (!string.IsNullOrEmpty(response.fullName))
            myfullname = response.fullName;

        await ShowSuccessMessage("Email verified successfully!");

        // Navigate based on redirectTo response
        await Task.Delay(1000); // Give user time to see success message

        if (response.redirectTo?.Equals("Setup", StringComparison.OrdinalIgnoreCase) == true || response.requiresSetup)
        {
            // Navigate to setup page
            await Navigation.PushAsync(new Views.WelcomePage());
        }
        else if (response.redirectTo?.Equals("Login", StringComparison.OrdinalIgnoreCase) == true)
        {
            await Navigation.PushModalAsync(new Views.LoginPage());
        }
        else
        {
            // Default navigation to login
            await Navigation.PushAsync(new Views.LoginPage());
        }
    }

    private async Task HandleVerificationError(string errorMessage)
    {
        ClearOtpEntries();
        await ShowError(errorMessage ?? "Verification failed");

        // Also show error on XAML if errorLabel exists
        if (errorLabel != null)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                errorLabel.Text = errorMessage ?? "Verification failed";
                errorLabel.IsVisible = true;
            });
        }
    }

    private async Task HandleUnexpectedError(Exception ex)
    {
        await ShowError($"An unexpected error occurred: {ex.Message}");
    }
    #endregion

    #region Resend OTP
    private async void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e)
    {
        await ResendOtp();
    }

    private async void PasteButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (Clipboard.HasText)
            {
                var clipboardText = await Clipboard.GetTextAsync();

                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    await ShowValidationError("Clipboard is empty");
                    return;
                }

                // Remove any whitespace or special characters
                var cleanedText = Regex.Replace(clipboardText.Trim(), @"[^\d]", "");

                // Validate the OTP length
                if (cleanedText.Length != OTP_LENGTH)
                {
                    await ShowValidationError($"Invalid OTP format. Expected {OTP_LENGTH} digits, got {cleanedText.Length}");
                    return;
                }

                // Distribute the digits to the OTP entries
                for (int i = 0; i < OTP_LENGTH && i < cleanedText.Length; i++)
                {
                    _otpEntries[i].Text = cleanedText[i].ToString();
                }

                // Focus on the last entry and then unfocus to hide keyboard
                _otpEntries[OTP_LENGTH - 1].Focus();
                await Task.Delay(100);
                _otpEntries[OTP_LENGTH - 1].Unfocus();

                // Show success feedback
                await ShowSnackbar("OTP pasted successfully!", Colors.Green, "OK");

                // Auto-submit after a short delay
                await Task.Delay(500);
                await SubmitOtp();
            }
            else
            {
                await ShowValidationError("No text found in clipboard");
            }
        }
        catch (Exception ex)
        {
            await ShowError($"Failed to paste OTP: {ex.Message}");
        }
    }

    private async Task ResendOtp()
    {
        if (_isProcessing) return;

        if (_resendAttempts >= MAX_RESEND_ATTEMPTS)
        {
            await ShowError("Maximum resend attempts reached. Please try again later.");
            return;
        }

        _isProcessing = true;

        try
        {
            await ShowLoadingDialog("Resending OTP...");

            var result = await ResendOtpFromServer();

            if (result.IsSuccess)
            {
                _resendAttempts++;
                ResetTimer();
                ClearOtpEntries();
                await ShowSuccessMessage($"New OTP sent successfully! (Attempt {_resendAttempts}/{MAX_RESEND_ATTEMPTS})");
            }
            else
            {
                await ShowError(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            await HandleUnexpectedError(ex);
        }
        finally
        {
            _isProcessing = false;
            await HideLoadingDialog();
        }
    }

    private async Task<ApiResult<OTPResponse>> ResendOtpFromServer()
    {
        try
        {
            // Create anonymous object with only email
            var requestPayload = new { email = GetUserEmail() };

            var jsonPayload = JsonConvert.SerializeObject(requestPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Add debug logging
            System.Diagnostics.Debug.WriteLine($"ResendOtp Request: {jsonPayload}");

            var response = await _httpClient.PostAsync(
                "https://opxng.com/api/AuthAccount/ResendOtp",
                content,
                _cancellationTokenSource.Token);

            System.Diagnostics.Debug.WriteLine($"ResendOtp Response Status: {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"ResendOtp Response: {responseContent}");

            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return ApiResult<OTPResponse>.Failure("Empty response from server");
            }

            // Handle string response
            if (responseContent.StartsWith("\"") && responseContent.EndsWith("\""))
            {
                var message = JsonConvert.DeserializeObject<string>(responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<OTPResponse>.Failure(message ?? "Failed to resend OTP");
                }

                // Success message as string
                return ApiResult<OTPResponse>.Success(new OTPResponse { success = true, message = message });
            }

            try
            {
                var otpResponse = JsonConvert.DeserializeObject<OTPResponse>(responseContent);

                if (otpResponse == null)
                {
                    return ApiResult<OTPResponse>.Failure("Failed to parse server response");
                }

                if (!otpResponse.success)
                {
                    return ApiResult<OTPResponse>.Failure(otpResponse.message ?? "Failed to resend OTP");
                }

                return ApiResult<OTPResponse>.Success(otpResponse);
            }
            catch (JsonException)
            {
                return ApiResult<OTPResponse>.Failure(responseContent);
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return ApiResult<OTPResponse>.Failure("Request timed out. Please check your internet connection.");
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<OTPResponse>.Failure($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResult<OTPResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }
    #endregion

    #region Navigation
    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        await GoBack();
    }

    private async Task GoBack()
    {
        if (_isProcessing)
        {
            var result = await DisplayAlert("Cancel Operation",
                "An operation is in progress. Are you sure you want to go back?",
                "Yes", "No");

            if (!result) return;

            _cancellationTokenSource?.Cancel();
        }

        var navStack = Navigation.NavigationStack;
        if (navStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
        else
        {
            Application.Current.MainPage = new NavigationPage(new MainPage());
        }
    }
    #endregion

    #region UI Helpers
    private async Task ShowLoadingDialog(string message)
    {
        Configurations.LoadingConfig = new LoadingConfig
        {
            Opacity = 0.4,
            DefaultMessage = message,
            FontSize = 12,
        };

        await Loading.Instance.StartAsync(async progress =>
        {
            for (var i = 0; i < 100; i++)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;

                await Task.Delay(30);
                progress.Report((i + 1) * 0.01d);
            }
        });
    }

    private async Task HideLoadingDialog()
    {
        Loading.Instance.Hide();
    }

    private async Task ShowValidationError(string message)
    {
        await ShowSnackbar(message, Colors.Orange, "OK");

        // Show on XAML if errorLabel exists
        if (errorLabel != null)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                errorLabel.Text = message;
                errorLabel.IsVisible = true;
            });
        }
    }

    private async Task ShowError(string message)
    {
        await ShowSnackbar(message, Color.FromArgb("#A25AC4"), "TRY AGAIN");

        // Show on XAML if errorLabel exists
        if (errorLabel != null && errorFrame != null)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                errorLabel.Text = message;
                errorFrame.IsVisible = true;
            });
        }
    }

    private async Task ShowSuccessMessage(string message)
    {
        await ShowSnackbar(message, Colors.ForestGreen, "OK");
    }

    private async Task ShowSnackbar(string message, Color backgroundColor, string actionText)
    {
        var snackbar = Snackbar.Make(message, null, actionText, TimeSpan.FromSeconds(5), new SnackbarOptions
        {
            BackgroundColor = backgroundColor,
            TextColor = Colors.White,
            ActionButtonTextColor = Colors.White,
            CornerRadius = new CornerRadius(8),
            Font = Microsoft.Maui.Font.SystemFontOfSize(14)
        });

        await snackbar.Show(_cancellationTokenSource.Token);
    }
    #endregion

    #region Utility Methods
    private string GetUserEmail()
    {
        return MainPage.mymail ?? LoginPage.myemail ?? "";
    }
    #endregion

    #region Cleanup
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cancellationTokenSource?.Cancel();
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _httpClient?.Dispose();
    }
    #endregion

    #region Data Models
    internal class OtpRequest
    {
        [JsonProperty("email")]
        public string email { get; set; } = "";

        [JsonProperty("otp")]
        public string otp { get; set; } = "";
    }

    internal class OTPResponse
    {
        [JsonProperty("success")]
        public bool success { get; set; }

        [JsonProperty("email")]
        public string? email { get; set; }

        [JsonProperty("fullName")]
        public string? fullName { get; set; }

        [JsonProperty("message")]
        public string? message { get; set; }

        [JsonProperty("roles")]
        public string? Roles { get; set; }

        [JsonProperty("requiresSetup")]
        public bool requiresSetup { get; set; }

        [JsonProperty("redirectTo")]
        public string? redirectTo { get; set; }

        [JsonProperty("totalTransactions")]
        public int totalTransactions { get; set; }

        [JsonProperty("completedTransactions")]
        public int completedTransactions { get; set; }

        [JsonProperty("pendingTransactions")]
        public int pendingTransactions { get; set; }

        [JsonProperty("disputes")]
        public int disputes { get; set; }

        [JsonProperty("ledgerBalance")]
        public string? ledgerBalance { get; set; }

        [JsonProperty("availableBalance")]
        public string? availableBalance { get; set; }
    }

    internal class ApiResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T Response { get; private set; }
        public string ErrorMessage { get; private set; }

        private ApiResult(bool isSuccess, T response, string errorMessage)
        {
            IsSuccess = isSuccess;
            Response = response;
            ErrorMessage = errorMessage;
        }

        public static ApiResult<T> Success(T response)
        {
            return new ApiResult<T>(true, response, null);
        }

        public static ApiResult<T> Failure(string errorMessage)
        {
            return new ApiResult<T>(false, default(T), errorMessage);
        }
    }
    #endregion
}

#region Behaviors
public class NumericValidationBehavior : Behavior<Entry>
{
    protected override void OnAttachedTo(Entry entry)
    {
        entry.TextChanged += OnEntryTextChanged;
        base.OnAttachedTo(entry);
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.TextChanged -= OnEntryTextChanged;
        base.OnDetachingFrom(entry);
    }

    private void OnEntryTextChanged(object sender, TextChangedEventArgs args)
    {
        if (sender is Entry entry)
        {
            var newText = args.NewTextValue;

            if (!string.IsNullOrEmpty(newText))
            {
                if (!Regex.IsMatch(newText, @"^\d$"))
                {
                    entry.Text = args.OldTextValue;
                }
            }
        }
    }
}
#endregion