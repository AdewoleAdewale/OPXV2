using AiForms.Dialogs;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Opx.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RPage : ContentPage
    {
        #region Fields and Properties
        private readonly ILogger<RPage> _logger;
        private ResetPassword _resetPasswordPopup;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public static string Resetemail { get; set; } = string.Empty;
        public static string OtPtoken { get; set; } = string.Empty;

        private int _secondsRemaining = 120; // Increased to 2 minutes
        private bool _isTimerRunning;
        private bool _isDisposed;
        private bool _isProcessing;
        private Entry[] _otpEntries = Array.Empty<Entry>();

        // Constants
        private const int OTP_LENGTH = 5;
        private const int TIMER_DURATION = 120;
        private const int REQUEST_TIMEOUT = 30;
        private const string RESEND_OTP_URL = "https://opxng.com/api/AuthAccount/ResendPasswordResetOtp";
        private const string VERIFY_OTP_URL = "https://opxng.com/api/AuthAccount/VerifyPasswordResetOtp";
        #endregion

        #region Constructor and Initialization
        public RPage()
        {
            try
            {
                InitializeComponent();
                _cancellationTokenSource = new CancellationTokenSource();

                // Use dependency injection if available, otherwise create a simple logger
                _logger = CreateLogger();

                InitializePageAsync();
            }
            catch (Exception ex)
            {
                HandleCriticalError("Failed to initialize OTP page", ex);
            }
        }

        private async void InitializePageAsync()
        {
            try
            {
                await InitializeOtpEntriesAsync();
                await StartTimerAsync();

                // Validate required data
                if (string.IsNullOrWhiteSpace(ForgetPassword.Forgetemail))
                {
                    await ShowErrorAsync("Email address is required. Please go back and try again.");
                    await NavigateBackAsync();
                    return;
                }

                _resetPasswordPopup = new ResetPassword();
                _logger?.LogInformation("OTP page initialized successfully");
            }
            catch (Exception ex)
            {
                HandleCriticalError("Failed to initialize OTP page components", ex);
            }
        }

        private async Task InitializeOtpEntriesAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var entries = new List<Entry>();

                    // Safely add entries with null checks
                    if (otp1 != null) entries.Add(otp1);
                    if (otp2 != null) entries.Add(otp2);
                    if (otp3 != null) entries.Add(otp3);
                    if (otp4 != null) entries.Add(otp4);
                    if (otp5 != null) entries.Add(otp5);

                    _otpEntries = entries.ToArray();

                    // Setup additional properties for better UX
                    foreach (var entry in _otpEntries)
                    {
                        if (entry != null)
                        {
                            entry.ReturnType = ReturnType.Next;
                            entry.ClearButtonVisibility = ClearButtonVisibility.WhileEditing;
                        }
                    }
                });

                if (_otpEntries.Length != 5)
                {
                    _logger?.LogWarning($"Expected 5 OTP entries, found {_otpEntries.Length}");
                    throw new InvalidOperationException("Not all OTP entry fields were found in the UI");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize OTP entries");
                throw;
            }
        }
        #endregion

        #region Timer Management
        private async Task StartTimerAsync()
        {
            try
            {
                if (_isTimerRunning || _isDisposed)
                    return;

                _isTimerRunning = true;
                _secondsRemaining = TIMER_DURATION;

                await Task.Run(async () =>
                {
                    while (_secondsRemaining > 0 && !_isDisposed && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(1000, _cancellationTokenSource.Token);

                        if (_isDisposed) break;

                        _secondsRemaining--;

                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            try
                            {
                                if (timerLabel != null && !_isDisposed)
                                {
                                    if (_secondsRemaining <= 0)
                                    {
                                        timerLabel.Text = "00:00";
                                        timerLabel.TextColor = Colors.Red;
                                    }
                                    else
                                    {
                                        timerLabel.Text = TimeSpan.FromSeconds(_secondsRemaining).ToString(@"mm\:ss");
                                        timerLabel.TextColor = _secondsRemaining <= 30 ? Colors.Orange : Colors.Black;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "Timer UI update failed");
                            }
                        });
                    }

                    _isTimerRunning = false;
                }, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("Timer cancelled");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Timer failed");
                _isTimerRunning = false;
            }
        }

        private void ResetTimer()
        {
            try
            {
                _secondsRemaining = TIMER_DURATION;
                if (!_isTimerRunning)
                {
                    _ = StartTimerAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to reset timer");
            }
        }
        #endregion

        #region OTP Input Handling
        private async void OnOtpTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_isDisposed || _otpEntries == null || sender is not Entry entry)
                    return;

                // Validate input - only allow digits
                if (!string.IsNullOrEmpty(e.NewTextValue) && !Regex.IsMatch(e.NewTextValue, @"^\d$"))
                {
                    entry.Text = e.OldTextValue;
                    return;
                }

                int currentIndex = Array.IndexOf(_otpEntries, entry);
                if (currentIndex < 0) return;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Move to next entry when a digit is entered
                    if (!string.IsNullOrWhiteSpace(e.NewTextValue) && e.NewTextValue.Length == 1)
                    {
                        if (currentIndex < _otpEntries.Length - 1)
                        {
                            _otpEntries[currentIndex + 1]?.Focus();
                        }
                        else
                        {
                            // Auto-verify when last digit is entered
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(500); // Small delay for better UX
                                if (ValidateOtp(GetOtpValue()))
                                {
                                    await VerifyOtpAsync();
                                }
                            });
                        }
                    }
                    // Move to previous entry when backspace is pressed
                    else if (string.IsNullOrWhiteSpace(e.NewTextValue) && currentIndex > 0)
                    {
                        _otpEntries[currentIndex - 1]?.Focus();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "OTP text changed handler failed");
            }
        }

        private string GetOtpValue()
        {
            try
            {
                if (_otpEntries == null || _otpEntries.Length == 0)
                    return string.Empty;

                return string.Join("", _otpEntries.Where(x => x != null).Select(x => x.Text ?? ""));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get OTP value");
                return string.Empty;
            }
        }

        private bool ValidateOtp(string otp)
        {
            return !string.IsNullOrWhiteSpace(otp) &&
                   otp.Length == OTP_LENGTH &&
                   otp.All(char.IsDigit);
        }

        private async Task ClearOtpAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var entry in _otpEntries)
                    {
                        if (entry != null)
                        {
                            entry.Text = string.Empty;
                        }
                    }
                    _otpEntries.FirstOrDefault()?.Focus();
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to clear OTP");
            }
        }
        #endregion

        #region Event Handlers
        private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (_isDisposed || _isProcessing)
                    return;

                if (_secondsRemaining > 0)
                {
                    await ShowInfoAsync("Please wait for the timer to expire before requesting a new OTP.");
                    return;
                }

                await ResendOtpAsync();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Failed to resend OTP", ex);
            }
        }

        private async void ResetButton_Clicked(object sender, EventArgs e)
        {
            await VerifyOtpAsync();
        }

        private async void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e)
        {
            await NavigateBackAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            Task.Run(async () => await NavigateBackAsync());
            return true;
        }
        #endregion

        #region API Operations
        private async Task ResendOtpAsync()
        {
            if (!await _semaphore.WaitAsync(100))
            {
                await ShowInfoAsync("Another operation is in progress. Please wait.");
                return;
            }

            try
            {
                _isProcessing = true;

                if (string.IsNullOrWhiteSpace(ForgetPassword.Forgetemail))
                {
                    await ShowErrorAsync("Email address is required");
                    return;
                }

                await ShowLoadingAsync("Resending OTP...", async () =>
                {
                    var requestPayload = new OtPObject
                    {
                        email = ForgetPassword.Forgetemail
                    };

                    using var client = CreateHttpClient();
                    var jsonPayload = JsonConvert.SerializeObject(requestPayload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(RESEND_OTP_URL, content);
                    await HandleResendOtpResponseAsync(response);
                });
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Failed to resend OTP", ex);
            }
            finally
            {
                _isProcessing = false;
                _semaphore.Release();
            }
        }

        private async Task VerifyOtpAsync()
        {
            if (!await _semaphore.WaitAsync(100))
            {
                await ShowInfoAsync("Another operation is in progress. Please wait.");
                return;
            }

            try
            {
                _isProcessing = true;

                string otp = GetOtpValue();

                if (!ValidateOtp(otp))
                {
                    await ShowErrorAsync($"Please enter a complete {OTP_LENGTH}-digit OTP code");
                    await ClearOtpAsync();
                    return;
                }

                if (string.IsNullOrWhiteSpace(ForgetPassword.Forgetemail))
                {
                    await ShowErrorAsync("Email address is required");
                    return;
                }

                await ShowLoadingAsync("Verifying OTP...", async () =>
                {
                    var requestPayload = new ResteOtpObject
                    {
                        email = ForgetPassword.Forgetemail,
                        otp = otp
                    };

                    using var client = CreateHttpClient();
                    var jsonPayload = JsonConvert.SerializeObject(requestPayload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(VERIFY_OTP_URL, content);
                    await HandleVerifyOtpResponseAsync(response);
                });
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("Verification failed", ex);
                await ClearOtpAsync();
            }
            finally
            {
                _isProcessing = false;
                _semaphore.Release();
            }
        }

        private async Task HandleResendOtpResponseAsync(HttpResponseMessage response)
        {
            try
            {
                if (!response.IsSuccessStatusCode)
                {
                    await ShowErrorAsync($"Server error: {response.StatusCode}");
                    return;
                }

                string resultString = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(resultString))
                {
                    await ShowErrorAsync("Empty response from server");
                    return;
                }

                var resetOtpResponse = JsonConvert.DeserializeObject<ForgetResponse>(resultString);

                if (resetOtpResponse == null)
                {
                    await ShowErrorAsync("Failed to parse server response");
                    return;
                }

                if (!string.IsNullOrEmpty(resetOtpResponse.success))
                {
                    await ShowSuccessAsync($"New OTP sent successfully!");
                    await ClearOtpAsync();
                    ResetTimer();
                }
                else
                {
                    await ShowErrorAsync($"Failed to send OTP: {resetOtpResponse.message}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to handle resend OTP response");
                await ShowErrorAsync("Failed to process server response");
            }
        }

        private async Task HandleVerifyOtpResponseAsync(HttpResponseMessage response)
        {
            try
            {
                if (!response.IsSuccessStatusCode)
                {
                    await ShowErrorAsync($"Server error: {response.StatusCode}");
                    return;
                }

                string resultString = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(resultString))
                {
                    await ShowErrorAsync("Empty response from server");
                    return;
                }

                var resetOtpResponse = JsonConvert.DeserializeObject<ResetotpResponse>(resultString);

                if (resetOtpResponse == null)
                {
                    await ShowErrorAsync("Failed to parse server response");
                    return;
                }

                if (!string.IsNullOrEmpty(resetOtpResponse.success))
                {
                    await HandleSuccessfulVerificationAsync(resetOtpResponse);
                }
                else
                {
                    await ShowErrorAsync($"Verification failed: {resetOtpResponse.message}");
                    await ClearOtpAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to handle verify OTP response");
                await ShowErrorAsync("Failed to process server response");
            }
        }

        private async Task HandleSuccessfulVerificationAsync(ResetotpResponse response)
        {
            try
            {
                Resetemail = ForgetPassword.Forgetemail;
                OtPtoken = response.resetToken ?? string.Empty;

                await ShowSuccessAsync("OTP verified successfully!");

                // Navigate to reset password screen
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        if (Application.Current?.MainPage != null)
                        {
                            var resetPasswordPopup = new ResetPassword();
                            var result = await Application.Current.MainPage.ShowPopupAsync(resetPasswordPopup);
                            _logger?.LogInformation($"Reset password popup result: {result}");
                        }
                        else
                        {
                            await ShowErrorAsync("Unable to navigate to password reset screen");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Navigation to reset password failed");
                        await ShowErrorAsync("Failed to open password reset screen");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to handle successful verification");
                await ShowErrorAsync("Navigation failed");
            }
        }
        #endregion

        #region UI Helper Methods
        private async Task ShowLoadingAsync(string message, Func<Task> action)
        {
            try
            {
                Configurations.LoadingConfig = new LoadingConfig
                {
                    Opacity = 0.6,
                    DefaultMessage = message,
                    FontSize = 14,
                    OverlayColor = Color.FromArgb("#A25AC4")
                };

                await Loading.Instance.StartAsync(async progress =>
                {
                    var progressTask = Task.Run(async () =>
                    {
                        for (int i = 0; i < 100 && !_isDisposed; i++)
                        {
                            await Task.Delay(30);
                            progress.Report((i + 1) * 0.01d);
                        }
                    });

                    await Task.WhenAll(progressTask, action());
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Loading dialog failed");
                await action(); // Execute action anyway
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            try
            {
                if (_isDisposed) return;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var snackbar = Snackbar.Make(
                        message,
                        null,
                        "OK",
                        TimeSpan.FromSeconds(6),
                        new SnackbarOptions
                        {
                            BackgroundColor = Color.FromArgb("#DC3545"),
                            TextColor = Colors.White,
                            ActionButtonTextColor = Colors.White,
                            CornerRadius = new CornerRadius(10),
                            Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                        });

                    await snackbar.Show(_cancellationTokenSource.Token);
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to show error message");
            }
        }

        private async Task ShowSuccessAsync(string message)
        {
            try
            {
                if (_isDisposed) return;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var snackbar = Snackbar.Make(
                        message,
                        null,
                        "OK",
                        TimeSpan.FromSeconds(4),
                        new SnackbarOptions
                        {
                            BackgroundColor = Color.FromArgb("#28A745"),
                            TextColor = Colors.White,
                            ActionButtonTextColor = Colors.White,
                            CornerRadius = new CornerRadius(10),
                            Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                        });

                    await snackbar.Show(_cancellationTokenSource.Token);
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to show success message");
            }
        }

        private async Task ShowInfoAsync(string message)
        {
            try
            {
                if (_isDisposed) return;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var snackbar = Snackbar.Make(
                        message,
                        null,
                        "OK",
                        TimeSpan.FromSeconds(4),
                        new SnackbarOptions
                        {
                            BackgroundColor = Color.FromArgb("#17A2B8"),
                            TextColor = Colors.White,
                            ActionButtonTextColor = Colors.White,
                            CornerRadius = new CornerRadius(10),
                            Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                        });

                    await snackbar.Show(_cancellationTokenSource.Token);
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to show info message");
            }
        }
        #endregion

        #region Navigation and Disposal
        private async Task NavigateBackAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var navStack = Navigation.NavigationStack;
                    if (navStack.Count > 1)
                    {
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        Application.Current.MainPage = new NavigationPage(new MainPage());
                    }
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Navigation failed");
                // Last resort - try to set main page directly
                try
                {
                    Application.Current.MainPage = new MainPage();
                }
                catch (Exception criticalEx)
                {
                    _logger?.LogCritical(criticalEx, "Critical navigation failure");
                }
            }
        }

        protected override void OnDisappearing()
        {
            try
            {
                base.OnDisappearing();
                DisposeResources();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during page disappearing");
            }
        }

        private void DisposeResources()
        {
            try
            {
                _isDisposed = true;
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _semaphore?.Dispose();
                _resetPasswordPopup = null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during resource disposal");
            }
        }
        #endregion

        #region Utility Methods
        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(REQUEST_TIMEOUT)
            };

            return client;
        }

        private ILogger<RPage> CreateLogger()
        {
            // Simple logger implementation - replace with your preferred logging framework
            return new SimpleLogger<RPage>();
        }

        private async Task HandleErrorAsync(string message, Exception ex)
        {
            _logger?.LogError(ex, message);
            await ShowErrorAsync($"{message}. Please try again.");
        }

        private void HandleCriticalError(string message, Exception ex)
        {
            _logger?.LogCritical(ex, message);

            // Show a simple alert for critical errors
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await DisplayAlert("Critical Error",
                        "An unexpected error occurred. The application may need to be restarted.",
                        "OK");
                }
                catch
                {
                    // If even displaying an alert fails, log and continue
                    System.Diagnostics.Debug.WriteLine($"Critical error: {message} - {ex.Message}");
                }
            });
        }
        #endregion
    }

    #region Data Models
    internal class ResteOtpObject
    {
        public string otp { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
    }

    internal class ResetotpResponse
    {
        public string? success { get; set; }
        public string? email { get; set; }
        public string? message { get; set; }
        public string? resetToken { get; set; }
        public string? requiresSetup { get; set; }
        public string? redirectTo { get; set; }
        public bool requiresVerification { get; set; }
    }

    internal class OtPObject
    {
        public string email { get; set; } = string.Empty;
    }

    internal class ForgetResponses
    {
        public string? success { get; set; }
        public string? message { get; set; }
        public string? expires { get; set; }
    }
    #endregion

    #region Simple Logger Implementation
    public class SimpleLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {typeof(T).Name}: {message}";

            if (exception != null)
            {
                logMessage += $"\nException: {exception}";
            }

            System.Diagnostics.Debug.WriteLine(logMessage);
            Console.WriteLine(logMessage);
        }
    }
    #endregion
}