using AiForms.Dialogs;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Opx.Views
{
    public partial class VerifyToken : ContentPage
    {
        private bool _isAnimating = false;
        private bool _isVerifying = false;
        private bool _isDragging = false;
        private double _initialTranslationY = 0;
        private double _dragStartY = 0;
        private const int TOKEN_MIN_LENGTH = 6;
        private const int TOKEN_MAX_LENGTH = 20;
        private const string TOKEN_PATTERN = @"^[A-Za-z0-9\-]+$";
        private const double DRAG_THRESHOLD = 100;
        private const double AUTO_CLOSE_TIMEOUT = 5 * 60 * 1000;

        private System.Timers.Timer _autoCloseTimer;
        private bool _isSheetClosed = false;

        public VerifyToken()
        {
            try
            {
                InitializeComponent();
                InitializeSheet();
                SetupAutoCloseTimer();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to initialize verify token sheet");
            }
        }

        private void SetupAutoCloseTimer()
        {
            try
            {
                _autoCloseTimer = new System.Timers.Timer(AUTO_CLOSE_TIMEOUT);
                _autoCloseTimer.Elapsed += OnAutoCloseTimerElapsed;
                _autoCloseTimer.AutoReset = false;
                _autoCloseTimer.Start();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to setup auto-close timer");
            }
        }

        private async void OnAutoCloseTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (!_isSheetClosed && string.IsNullOrWhiteSpace(inputtoken.Text))
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await ShowErrorSnackbar("Session expired. Please try again.");
                        await DismissSheet();
                    });
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error during auto-close timer elapsed");
            }
        }

        private void StopAutoCloseTimer()
        {
            try
            {
                _autoCloseTimer?.Stop();
                _autoCloseTimer?.Dispose();
                _autoCloseTimer = null;
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error stopping auto-close timer");
            }
        }

        private void ResetAutoCloseTimer()
        {
            try
            {
                if (_autoCloseTimer != null && !_isSheetClosed)
                {
                    _autoCloseTimer.Stop();
                    _autoCloseTimer.Start();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error resetting auto-close timer");
            }
        }

        private async void InitializeSheet()
        {
            try
            {
                this.Opacity = 0;
                await this.FadeTo(1, 300, Easing.CubicOut);

                await Task.Delay(100);
                await AnimateSheetIn();

                SetupDragGesture();

                await Task.Delay(300);
                inputtoken.Focus();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to initialize sheet animation");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                ResetFormState();
                _isSheetClosed = false;
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error during page appearance");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                _isSheetClosed = true;
                StopAutoCloseTimer();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error during page disappearing");
            }
        }

        private void ResetFormState()
        {
            try
            {
                inputtoken.Text = string.Empty;
                VerifyButton.IsEnabled = false;
                HideMessage();
                ResetInputFieldStyle();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to reset form state");
            }
        }

        private async Task AnimateSheetIn()
        {
            try
            {
                if (_isAnimating) return;
                _isAnimating = true;

                await SheetFrame.TranslateTo(0, 0, 400, Easing.SpringOut);

                _isAnimating = false;
            }
            catch (Exception ex)
            {
                _isAnimating = false;
                HandleException(ex, "Failed to animate sheet in");
            }
        }

        private async Task AnimateSheetOut()
        {
            try
            {
                if (_isAnimating) return;
                _isAnimating = true;

                await SheetFrame.TranslateTo(0, 400, 200, Easing.CubicIn);

                _isAnimating = false;
            }
            catch (Exception ex)
            {
                _isAnimating = false;
                HandleException(ex, "Failed to animate sheet out");
            }
        }

        private async void OnBackgroundTapped(object sender, EventArgs e)
        {
            try
            {
                if (!_isDragging && !_isAnimating)
                {
                    await DismissSheet();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error dismissing sheet on background tap");
            }
        }

        private void OnSheetTapped(object sender, EventArgs e)
        {
            ResetAutoCloseTimer();
        }

        private void OnTokenTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var token = e.NewTextValue ?? string.Empty;

                bool isValid = ValidateTokenFormat(token);
                VerifyButton.IsEnabled = isValid && !_isVerifying;

                UpdateInputFieldStyle(isValid, token.Length > 0);

                if (token.Length > 0)
                {
                    HideMessage();
                    ResetAutoCloseTimer();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error during token text validation");
            }
        }

        private void OnTokenCompleted(object sender, EventArgs e)
        {
            try
            {
                if (VerifyButton.IsEnabled)
                {
                    OnVerifyTokenClicked(sender, e);
                }
                ResetAutoCloseTimer();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error on token completion");
            }
        }

        private bool ValidateTokenFormat(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return false;

                if (token.Length < TOKEN_MIN_LENGTH || token.Length > TOKEN_MAX_LENGTH)
                    return false;

                return Regex.IsMatch(token, TOKEN_PATTERN);
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error validating token format");
                return false;
            }
        }

        private void UpdateInputFieldStyle(bool isValid, bool hasContent)
        {
            try
            {
                if (!hasContent)
                {
                    TokenInputFrame.BorderColor = Color.FromArgb("#E0E0E0");
                    TokenInputFrame.BackgroundColor = Color.FromArgb("#F8F9FA");
                }
                else if (isValid)
                {
                    TokenInputFrame.BorderColor = Color.FromArgb("#00B894");
                    TokenInputFrame.BackgroundColor = Color.FromArgb("#F0FFF4");
                }
                else
                {
                    TokenInputFrame.BorderColor = Color.FromArgb("#E74C3C");
                    TokenInputFrame.BackgroundColor = Color.FromArgb("#FFF5F5");
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error updating input field style");
            }
        }

        private void ResetInputFieldStyle()
        {
            try
            {
                TokenInputFrame.BorderColor = Color.FromArgb("#E0E0E0");
                TokenInputFrame.BackgroundColor = Color.FromArgb("#F8F9FA");
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error resetting input field style");
            }
        }

        private async void OnVerifyTokenClicked(object sender, EventArgs e)
        {
            ResetAutoCloseTimer();
            await setrecipient_Clicked(sender, e);
        }

        private async Task setrecipient_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (_isVerifying) return;

                var token = inputtoken.Text?.Trim();

                if (string.IsNullOrWhiteSpace(token))
                {
                    ShowErrorMessage("Please enter a token number");
                    return;
                }

                if (!ValidateTokenFormat(token))
                {
                    ShowErrorMessage("Invalid token format. Please check and try again.");
                    return;
                }

                _isVerifying = true;
                SetLoadingState(true);

                try
                {
                    Configurations.LoadingConfig = new LoadingConfig
                    {
                        Opacity = 0.4,
                        DefaultMessage = "Verifying token, please wait...",
                        FontSize = 12,
                    };

                    await Loading.Instance.StartAsync(async progress =>
                    {
                        try
                        {
                            for (var i = 0; i < 50; i++)
                            {
                                await Task.Delay(20);
                                progress.Report((i + 1) * 0.02d);
                            }

                            await CreateContractAsync();
                        }
                        catch (Exception ex)
                        {
                            await ShowErrorSnackbar($"Error verifying token: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    await ShowErrorSnackbar($"Failed to start token verification: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error during token verification");
            }
            finally
            {
                _isVerifying = false;
                SetLoadingState(false);
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Button_Clicked_1(sender, e);
        }

        private async Task Button_Clicked_1(object sender, EventArgs e)
        {
            try
            {
                await DismissSheet();
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error on cancel button click");
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            try
            {
                LoadingIndicator.IsVisible = isLoading;
                LoadingIndicator.IsRunning = isLoading;

                VerifyButton.IsEnabled = !isLoading && ValidateTokenFormat(inputtoken.Text ?? string.Empty);
                CancelButton.IsEnabled = !isLoading;
                inputtoken.IsEnabled = !isLoading;

                if (isLoading)
                {
                    VerifyButton.Text = "Verifying...";
                }
                else
                {
                    VerifyButton.Text = "Verify Token";
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error setting loading state");
            }
        }

        private void ShowErrorMessage(string message)
        {
            try
            {
                ShowMessage(message, "❌", Color.FromArgb("#FFE7E7"), Color.FromArgb("#E74C3C"));
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error showing error message");
            }
        }

        private void ShowSuccessMessage(string message)
        {
            try
            {
                ShowMessage(message, "✅", Color.FromArgb("#E8F5E8"), Color.FromArgb("#00B894"));
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error showing success message");
            }
        }

        private void ShowMessage(string message, string icon, Color backgroundColor, Color textColor)
        {
            try
            {
                MessageContainer.IsVisible = true;
                MessageFrame.BackgroundColor = backgroundColor;
                MessageIcon.Text = icon;
                MessageIcon.TextColor = textColor;
                MessageLabel.Text = message;
                MessageLabel.TextColor = textColor;
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error showing message");
            }
        }

        private void HideMessage()
        {
            try
            {
                MessageContainer.IsVisible = false;
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error hiding message");
            }
        }

        private async Task DismissSheet()
        {
            try
            {
                if (_isAnimating || _isSheetClosed) return;

                _isSheetClosed = true;
                StopAutoCloseTimer();

                await AnimateSheetOut();
                await this.FadeTo(0, 200, Easing.CubicIn);

                if (Navigation.ModalStack.Count > 0)
                {
                    await Navigation.PopModalAsync();
                }
                else
                {
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error dismissing sheet");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DismissSheet();
                });
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error handling back button press");
                return base.OnBackButtonPressed();
            }
        }

        private async void OnResendTokenTapped(object sender, EventArgs e)
        {
            try
            {
                ShowMessage("Resending token...", "📤", Color.FromArgb("#E3F2FD"), Color.FromArgb("#2196F3"));

                ResetAutoCloseTimer();

                await Task.Delay(1500);

                ShowSuccessMessage("Token resent successfully!");

                await Task.Delay(2000);
                HideMessage();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Failed to resend token. Please try again.");
                HandleException(ex, "Error resending token");
            }
        }

        private async Task CreateContractAsync()
        {
            try
            {
                StopAutoCloseTimer();

                if (string.IsNullOrWhiteSpace(LoginPage.myemail))
                {
                    await ShowCustomErrorSheet("User Email Not Found", "Please log in again to continue.", "AUTHENTICATION_ERROR");
                    return;
                }

                string url = "https://opxng.com/api/contractsApi/confirm-delivery";

                var requestPayload = new VerifyTokenObject
                {
                    Token = inputtoken.Text?.Trim() ?? "",
                    Email = LoginPage.myemail,
                };

                string jsonPayload = JsonConvert.SerializeObject(requestPayload, Formatting.None);

                // ✅ Fix SSL issues here with HttpClientHandler
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) })
                {
                    // Force TLS 1.2 or 1.3
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                    try
                    {
                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.PostAsync(url, content);
                        string resultString = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            string errorMsg = string.IsNullOrWhiteSpace(resultString)
                                ? $"Server returned {response.StatusCode}"
                                : resultString;

                            await ShowCustomErrorSheet(
                                $"Verification Failed ({response.StatusCode})",
                                errorMsg,
                                "SERVER_ERROR"
                            );
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(resultString))
                        {
                            await ShowCustomErrorSheet(
                                "Empty Response",
                                "The server returned an empty response.",
                                "NETWORK_ERROR"
                            );
                            return;
                        }

                        var contractResponse = JsonConvert.DeserializeObject<VerifyTokenResponse>(resultString);

                        if (contractResponse == null)
                        {
                            await ShowCustomErrorSheet(
                                "Parse Error",
                                "Failed to parse server response.",
                                "DATA_ERROR"
                            );
                            return;
                        }

                        // Inside CreateContractAsync
                        if (!string.IsNullOrEmpty(contractResponse.success))
                        {
                            await DismissSheet();
                            await Task.Delay(100);
                            await ShowCustomSuccessSheet(contractResponse);
                        }
                        else
                        {
                            string errorMessage = contractResponse.message ?? "Token verification failed";
                            await DismissSheet();
                            await Task.Delay(100);
                            await ShowCustomErrorSheet(
                                "Verification Failed",
                                errorMessage,
                                "VALIDATION_ERROR"
                            );
                        }

                    }
                    catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                    {
                        await ShowCustomErrorSheet(
                            "Connection Timeout",
                            "The request took too long to complete. Please check your internet connection.",
                            "TIMEOUT_ERROR"
                        );
                    }
                    catch (TaskCanceledException)
                    {
                        await ShowCustomErrorSheet(
                            "Request Cancelled",
                            "The request was cancelled. Please try again.",
                            "CANCELLED_ERROR"
                        );
                    }
                    catch (HttpRequestException ex)
                    {
                        await ShowCustomErrorSheet(
                            "Network Error",
                            $"Unable to connect to server: {ex.Message}",
                            "NETWORK_ERROR"
                        );
                    }
                    catch (JsonException ex)
                    {
                        await ShowCustomErrorSheet(
                            "Data Format Error",
                            $"Invalid response format: {ex.Message}",
                            "DATA_ERROR"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowCustomErrorSheet(
                    "Unexpected Error",
                    $"An unexpected error occurred: {ex.Message}",
                    "UNKNOWN_ERROR"
                );
            }
        }

        // NEW: Show custom success sheet with full response details
        private async Task ShowCustomSuccessSheet(VerifyTokenResponse response)
        {
            try
            {
                await DismissSheet();
                await Task.Delay(100);

                var successPopup = new VerificationSuccessSheet(response);
                await Application.Current.MainPage.ShowPopupAsync(successPopup);
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error showing success sheet");
                await ShowSuccessSnackbar("Verification successful!");
            }
        }

        private async Task ShowCustomErrorSheet(string title, string message, string errorCode)
        {
            try
            {
                var errorPopup = new VerificationErrorSheet(title, message, errorCode);
                await Application.Current.MainPage.ShowPopupAsync(errorPopup);
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error showing error sheet");
                await ShowErrorSnackbar(message);
            }
        }


        private async Task ShowErrorSnackbar(string message)
        {
            try
            {
                var snackbar = Snackbar.Make(message, null, "TRY AGAIN", TimeSpan.FromSeconds(5), new SnackbarOptions
                {
                    BackgroundColor = Color.FromArgb("#A25AC4"),
                    TextColor = Colors.White,
                    ActionButtonTextColor = Colors.White,
                    CornerRadius = new CornerRadius(8),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                });
                await snackbar.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing snackbar: {ex.Message}. Original message: {message}");
            }
        }

        private async Task ShowSuccessSnackbar(string message)
        {
            try
            {
                var snackbar = Snackbar.Make(message, null, "OK", TimeSpan.FromSeconds(10), new SnackbarOptions
                {
                    BackgroundColor = Colors.ForestGreen,
                    TextColor = Colors.White,
                    ActionButtonTextColor = Colors.White,
                    CornerRadius = new CornerRadius(10),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(14),
                    ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14)
                });
                await snackbar.Show(CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing success snackbar: {ex.Message}. Original message: {message}");
            }
        }

        private async void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            try
            {
                if (_isAnimating || _isVerifying) return;

                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        _isDragging = true;
                        _initialTranslationY = SheetFrame.TranslationY;
                        _dragStartY = e.TotalY;
                        break;

                    case GestureStatus.Running:
                        if (_isDragging)
                        {
                            var newTranslationY = _initialTranslationY + (e.TotalY - _dragStartY);

                            if (newTranslationY >= 0)
                            {
                                SheetFrame.TranslationY = newTranslationY;

                                var opacity = Math.Max(0.3, 1 - (newTranslationY / 400));
                                this.Opacity = opacity;

                                if (newTranslationY > 20)
                                {
                                    DragHandle.BackgroundColor = Color.FromArgb("#6C5CE7");
                                    DragHandle.WidthRequest = 60;
                                }
                                else
                                {
                                    DragHandle.BackgroundColor = Color.FromArgb("#E0E0E0");
                                    DragHandle.WidthRequest = 50;
                                }
                            }
                        }
                        break;

                    case GestureStatus.Completed:
                    case GestureStatus.Canceled:
                        if (_isDragging)
                        {
                            _isDragging = false;
                            var finalTranslationY = SheetFrame.TranslationY;

                            DragHandle.BackgroundColor = Color.FromArgb("#E0E0E0");
                            DragHandle.WidthRequest = 50;

                            if (finalTranslationY > DRAG_THRESHOLD)
                            {
                                await DismissSheet();
                            }
                            else
                            {
                                await Task.WhenAll(
                                    SheetFrame.TranslateTo(0, 0, 300, Easing.SpringOut),
                                    this.FadeTo(1, 200, Easing.CubicOut)
                                );
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Error handling drag gesture");
            }
        }

        private void SetupDragGesture()
        {
            try
            {
                var panGesture = new PanGestureRecognizer();
                panGesture.PanUpdated += OnPanUpdated;

                SheetFrame.GestureRecognizers.Add(panGesture);
                DragHandleArea.GestureRecognizers.Add(panGesture);
            }
            catch (Exception ex)
            {
                HandleException(ex, "Failed to setup drag gesture");
            }
        }

        private void HandleException(Exception ex, string context)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Error in {context}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        ShowErrorMessage("An unexpected error occurred. Please try again.");
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to show error message to user");
                    }
                });
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Failed to handle exception properly");
            }
        }

        internal class VerifyTokenObject
        {
            public string Token { get; set; } = "";
            public string Email { get; set; } = "";
        }

        public class VerifyTokenResponse
        {
            public string? success { get; set; }
            public string? message { get; set; }
            public decimal? amount { get; set; }
            public string? SellerName { get; set; }
            public string? buyerName { get; set; }
            public string? confirmedAt { get; set; }
            public string? status { get; set; }
            public int? contractId { get; set; }
        }
    }
}