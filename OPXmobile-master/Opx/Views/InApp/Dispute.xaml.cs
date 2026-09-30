using AiForms.Dialogs;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;

namespace Opx.Views;

public partial class Dispute : ContentPage, INotifyPropertyChanged
{
    private bool _isLoading;
    private bool _isFormValid;
    private string _tokenText = "";
    private string _descriptionText = "";
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private System.Timers.Timer _timeoutTimer;
    private readonly TimeSpan _timeoutDuration = TimeSpan.FromMinutes(5);
    private DateTime _lastInteractionTime;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFormEnabled));
            }
        }
    }

    public bool IsFormEnabled => !IsLoading;

    public bool IsFormValid
    {
        get => _isFormValid;
        set
        {
            if (_isFormValid != value)
            {
                _isFormValid = value;
                OnPropertyChanged();
            }
        }
    }

    public string TokenText
    {
        get => _tokenText;
        set
        {
            if (_tokenText != value)
            {
                _tokenText = value;
                OnPropertyChanged();
                ValidateForm();
                UpdateLastInteraction();
            }
        }
    }

    public string DescriptionText
    {
        get => _descriptionText;
        set
        {
            if (_descriptionText != value)
            {
                _descriptionText = value;
                OnPropertyChanged();
                ValidateForm();
                UpdateLastInteraction();
            }
        }
    }

    public Dispute()
    {
        InitializeComponent();
        BindingContext = this;
        SetupTimeoutTimer();
        SetupEventHandlers();
        UpdateLastInteraction();
    }

    private void SetupEventHandlers()
    {
        try
        {
            InputtToken.TextChanged += OnTokenTextChanged;
            Description.TextChanged += OnDescriptionTextChanged;
            InputtToken.Focused += OnInputFocused;
            Description.Focused += OnInputFocused;
            InputtToken.Unfocused += OnTokenUnfocused;
            Description.Unfocused += OnDescriptionUnfocused;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting up event handlers: {ex.Message}");
        }
    }

    private void SetupTimeoutTimer()
    {
        try
        {
            _timeoutTimer = new System.Timers.Timer(_timeoutDuration.TotalMilliseconds);
            _timeoutTimer.Elapsed += OnTimeoutElapsed;
            _timeoutTimer.AutoReset = false;
            _timeoutTimer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting up timeout timer: {ex.Message}");
        }
    }

    private void UpdateLastInteraction()
    {
        try
        {
            _lastInteractionTime = DateTime.Now;
            _timeoutTimer?.Stop();
            _timeoutTimer?.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating last interaction: {ex.Message}");
        }
    }

    private async void OnTimeoutElapsed(object sender, ElapsedEventArgs e)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ShowTimeoutWarning();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error handling timeout: {ex.Message}");
        }
    }

    private async Task ShowTimeoutWarning()
    {
        try
        {
            var result = await DisplayAlert(
                "Session Timeout",
                "This form will close due to inactivity. Do you want to continue?",
                "Continue",
                "Close"
            );

            if (result)
            {
                UpdateLastInteraction();
            }
            else
            {
                await Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing timeout warning: {ex.Message}");
            await Navigation.PopAsync();
        }
    }

    private void OnTokenTextChanged(object sender, TextChangedEventArgs e)
    {
        TokenText = e.NewTextValue ?? "";
        ClearFieldError(TokenErrorLabel);
        AnimateFieldFocus(sender as View);
    }

    private void OnDescriptionTextChanged(object sender, TextChangedEventArgs e)
    {
        DescriptionText = e.NewTextValue ?? "";
        ClearFieldError(DescriptionErrorLabel);
        AnimateFieldFocus(sender as View);
    }

    private void OnInputFocused(object sender, FocusEventArgs e)
    {
        UpdateLastInteraction();
        AnimateFieldFocus(sender as View);
    }

    private void OnTokenUnfocused(object sender, FocusEventArgs e)
    {
        ValidateTokenField();
        AnimateFieldUnfocus(sender as View);
    }

    private void OnDescriptionUnfocused(object sender, FocusEventArgs e)
    {
        ValidateDescriptionField();
        AnimateFieldUnfocus(sender as View);
    }

    private async void AnimateFieldFocus(View field)
    {
        try
        {
            if (field?.Parent is Frame frame)
            {
                await frame.ScaleTo(1.02, 150, Easing.CubicOut);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error animating field focus: {ex.Message}");
        }
    }

    private async void AnimateFieldUnfocus(View field)
    {
        try
        {
            if (field?.Parent is Frame frame)
            {
                await frame.ScaleTo(1.0, 150, Easing.CubicOut);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error animating field unfocus: {ex.Message}");
        }
    }

    private void ValidateForm()
    {
        try
        {
            bool tokenValid = !string.IsNullOrWhiteSpace(TokenText) && TokenText.Length >= 3;
            bool descriptionValid = !string.IsNullOrWhiteSpace(DescriptionText) && DescriptionText.Length >= 10;

            IsFormValid = tokenValid && descriptionValid;

            if (IsFormValid)
            {
                AnimateButtonEnabled(CREATECONTRACT);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error validating form: {ex.Message}");
            IsFormValid = false;
        }
    }

    private async void AnimateButtonEnabled(Button button)
    {
        try
        {
            if (button != null)
            {
                await button.ScaleTo(1.05, 100, Easing.CubicOut);
                await button.ScaleTo(1.0, 100, Easing.CubicOut);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error animating button: {ex.Message}");
        }
    }

    private void ValidateTokenField()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(TokenText))
            {
                ShowFieldError(TokenErrorLabel, "Token is required");
                return;
            }

            if (TokenText.Length < 3)
            {
                ShowFieldError(TokenErrorLabel, "Token must be at least 3 characters");
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(TokenText, @"^[a-zA-Z0-9]+$"))
            {
                ShowFieldError(TokenErrorLabel, "Token can only contain letters and numbers");
                return;
            }

            ClearFieldError(TokenErrorLabel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error validating token field: {ex.Message}");
        }
    }

    private void ValidateDescriptionField()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(DescriptionText))
            {
                ShowFieldError(DescriptionErrorLabel, "Description is required");
                return;
            }

            if (DescriptionText.Length < 10)
            {
                ShowFieldError(DescriptionErrorLabel, "Description must be at least 10 characters");
                return;
            }

            if (DescriptionText.Length > 500)
            {
                ShowFieldError(DescriptionErrorLabel, "Description cannot exceed 500 characters");
                return;
            }

            ClearFieldError(DescriptionErrorLabel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error validating description field: {ex.Message}");
        }
    }

    private async void ShowFieldError(Label errorLabel, string message)
    {
        try
        {
            if (errorLabel != null)
            {
                errorLabel.Text = message;
                errorLabel.Opacity = 0;
                errorLabel.IsVisible = true;
                await errorLabel.FadeTo(1, 200, Easing.CubicOut);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing field error: {ex.Message}");
        }
    }

    private async void ClearFieldError(Label errorLabel)
    {
        try
        {
            if (errorLabel != null && errorLabel.IsVisible)
            {
                await errorLabel.FadeTo(0, 200, Easing.CubicIn);
                errorLabel.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing field error: {ex.Message}");
        }
    }

    private async void CREATECONTRACT_Clicked(object sender, EventArgs e)
    {
        if (IsLoading || !IsFormValid)
            return;

        UpdateLastInteraction();

        if (!await _semaphore.WaitAsync(100))
        {
            await ShowErrorSnackbar("Please wait for the current operation to complete");
            return;
        }

        try
        {
            await ProcessDisputeCreation();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ProcessDisputeCreation()
    {
        try
        {
            IsLoading = true;
            await AnimateButtonLoading(CREATECONTRACT);

            Configurations.LoadingConfig = new LoadingConfig
            {
                Opacity = 0.5,
                DefaultMessage = "Creating dispute contract...",
                FontSize = 14,
                IndicatorColor = Color.FromArgb("#7B68EE")
            };

            await Loading.Instance.StartAsync(async progress =>
            {
                try
                {
                    await UpdateProgress(progress, 0.1, "Validating request...");
                    await Task.Delay(300);

                    await UpdateProgress(progress, 0.3, "Preparing contract data...");
                    await Task.Delay(200);

                    await UpdateProgress(progress, 0.5, "Contacting server...");
                    await CreateContractAsync();

                    await UpdateProgress(progress, 0.9, "Finalizing...");
                    await Task.Delay(200);

                    await UpdateProgress(progress, 1.0, "Complete!");
                }
                catch (Exception ex)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await ShowNetworkErrorSheet(ex);
                    });
                }
            });
        }
        catch (Exception ex)
        {
            await ShowNetworkErrorSheet(ex);
        }
        finally
        {
            IsLoading = false;
            await AnimateButtonNormal(CREATECONTRACT);
        }
    }

    private async Task AnimateButtonLoading(Button button)
    {
        try
        {
            if (button != null)
            {
                await button.ScaleTo(0.95, 100, Easing.CubicOut);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error animating button loading: {ex.Message}");
        }
    }

    private async Task AnimateButtonNormal(Button button)
    {
        try
        {
            if (button != null)
            {
                await button.ScaleTo(1.0, 100, Easing.CubicOut);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error animating button normal: {ex.Message}");
        }
    }

    private async Task UpdateProgress(IProgress<double> progress, double value, string message)
    {
        try
        {
            progress.Report(value);
            if (Loading.Instance != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Configurations.LoadingConfig.DefaultMessage = message;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating progress: {ex.Message}");
        }
    }

    private async Task CreateContractAsync()
    {
        const int maxRetries = 3;
        int retryCount = 0;
        Exception lastException = null;

        while (retryCount < maxRetries)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(LoginPage.myemail))
                {
                    throw new InvalidOperationException("User email not found. Please log in again.");
                }

                // Check network connectivity first
                var current = Connectivity.NetworkAccess;
                if (current != NetworkAccess.Internet)
                {
                    throw new NetworkException("No internet connection available. Please check your network settings.");
                }

                string url = "https://opxng.com/api/contractsApi/request-cancellation";

                var requestPayload = new DisputeObject
                {
                    cancellationComment = DescriptionText.Trim(),
                    token = TokenText.Trim(),
                    email = LoginPage.myemail,
                };

                string jsonPayload = JsonConvert.SerializeObject(requestPayload, Formatting.None);

                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(45)
                };

                // Add headers for better reliability
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "OpxApp/1.0");

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"Attempt {retryCount + 1}: Sending request to {url}");

                using var response = await client.PostAsync(url, content);
                string resultString = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response body: {resultString}");

                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = Opx.Services.OpxApi.ExtractError(resultString, response.StatusCode);

                    // Retry for specific status codes
                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                        response.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                        response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                        response.StatusCode == System.Net.HttpStatusCode.BadGateway ||
                        response.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                    {
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            int delayMs = 2000 * retryCount; // Progressive delay
                            System.Diagnostics.Debug.WriteLine($"Retrying in {delayMs}ms...");
                            await Task.Delay(delayMs);
                            continue;
                        }
                    }

                    throw new HttpRequestException($"Server error ({response.StatusCode}): {errorMsg}");
                }

                if (string.IsNullOrWhiteSpace(resultString))
                {
                    throw new InvalidOperationException("Empty response from server");
                }

                var contractResponse = JsonConvert.DeserializeObject<DisputeResponse>(resultString);

                if (contractResponse == null)
                {
                    throw new JsonException("Failed to parse server response");
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await HandleContractResponse(contractResponse);
                });

                return;
            }
            catch (TaskCanceledException ex)
            {
                lastException = ex;
                retryCount++;
                System.Diagnostics.Debug.WriteLine($"Request timeout (attempt {retryCount}/{maxRetries}): {ex.Message}");

                if (retryCount >= maxRetries)
                {
                    throw new NetworkException("Request timed out after multiple attempts. Please check your internet connection and try again.", ex);
                }
                await Task.Delay(2000 * retryCount);
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                retryCount++;
                System.Diagnostics.Debug.WriteLine($"Network error (attempt {retryCount}/{maxRetries}): {ex.Message}");

                if (retryCount >= maxRetries)
                {
                    throw new NetworkException($"Network error after {maxRetries} attempts. Please check your internet connection.", ex);
                }
                await Task.Delay(2000 * retryCount);
            }
            catch (NetworkException)
            {
                throw; // Re-throw NetworkException as-is
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex.Message}");
                throw;
            }
        }

        if (lastException != null)
        {
            throw new NetworkException("Failed to create dispute after multiple attempts.", lastException);
        }
    }

    private async Task HandleContractResponse(DisputeResponse contractResponse)
    {
        try
        {
            if (contractResponse.success)
            {
                await ShowSuccessSheet(contractResponse);
            }
            else
            {
                await ShowFailureSheet(contractResponse);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorSnackbar($"Error processing response: {ex.Message}");
        }
    }

    private async Task ShowSuccessSheet(DisputeResponse response)
    {
        try
        {
            var sheet = new ContentPage
            {
                BackgroundColor = Colors.Transparent
            };

            var overlay = new Grid
            {
                BackgroundColor = Color.FromArgb("#80000000")
            };

            var sheetContent = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 30,
                Padding = 0,
                HasShadow = true,
                VerticalOptions = LayoutOptions.End,
                Margin = 0
            };

            var stackLayout = new StackLayout
            {
                Padding = 30,
                Spacing = 20
            };

            // Success Icon
            var successIcon = new Label
            {
                Text = "✓",
                FontSize = 60,
                TextColor = Color.FromArgb("#28A745"),
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold
            };

            // Title
            var title = new Label
            {
                Text = "Dispute Initiated Successfully",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2C3E50"),
                HorizontalTextAlignment = TextAlignment.Center
            };

            // Details Frame
            var detailsFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#F8F9FA"),
                BorderColor = Color.FromArgb("#E1E8ED"),
                CornerRadius = 15,
                Padding = 20,
                HasShadow = false
            };

            var detailsStack = new StackLayout { Spacing = 15 };

            if (!string.IsNullOrWhiteSpace(response.contractToken))
            {
                detailsStack.Children.Add(CreateDetailRow("Token:", response.contractToken));
            }

            if (!string.IsNullOrWhiteSpace(response.status))
            {
                detailsStack.Children.Add(CreateDetailRow("Status:", response.status));
            }

            if (!string.IsNullOrWhiteSpace(response.message))
            {
                detailsStack.Children.Add(CreateDetailRow("Message:", response.message));
            }

            if (!string.IsNullOrWhiteSpace(response.requestedAt))
            {
                detailsStack.Children.Add(CreateDetailRow("Requested At:", response.requestedAt));
            }

            detailsFrame.Content = detailsStack;

            // Close Button
            var closeButton = new Button
            {
                Text = "Done",
                BackgroundColor = Color.FromArgb("#28A745"),
                TextColor = Colors.White,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 12,
                HeightRequest = 50
            };

            closeButton.Clicked += async (s, e) =>
            {
                await sheetContent.TranslateTo(0, 600, 250, Easing.CubicIn);
                await Navigation.PopModalAsync();
                ClearForm();
                await Navigation.PopAsync();
            };

            stackLayout.Children.Add(successIcon);
            stackLayout.Children.Add(title);
            stackLayout.Children.Add(detailsFrame);
            stackLayout.Children.Add(closeButton);

            sheetContent.Content = stackLayout;
            overlay.Children.Add(sheetContent);
            sheet.Content = overlay;

            await Navigation.PushModalAsync(sheet);

            // Animate sheet in
            sheetContent.TranslationY = 600;
            await sheetContent.TranslateTo(0, 0, 300, Easing.CubicOut);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing success sheet: {ex.Message}");
            await ShowSuccessSnackbar("Dispute initiated successfully!");
            ClearForm();
            await Task.Delay(2000);
            await Navigation.PopAsync();
        }
    }
    private async Task ShowNetworkErrorSheet(Exception ex)
    {
        try
        {
            string errorTitle = "Connection Error";
            string errorMessage = "Unable to connect to the server.";
            string errorDetails = "";

            if (ex is NetworkException networkEx)
            {
                errorMessage = networkEx.Message;
                if (networkEx.InnerException != null)
                {
                    errorDetails = networkEx.InnerException.Message;
                }
            }
            else if (ex is TaskCanceledException)
            {
                errorMessage = "Request timed out. Please check your internet connection.";
            }
            else if (ex is HttpRequestException)
            {
                errorMessage = "Network error. Please check your internet connection.";
                errorDetails = ex.Message;
            }
            else
            {
                errorMessage = ex.Message;
            }

            var sheet = new ContentPage
            {
                BackgroundColor = Colors.Transparent
            };

            var overlay = new Grid
            {
                BackgroundColor = Color.FromArgb("#80000000")
            };

            var sheetContent = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 30,
                Padding = 0,
                HasShadow = true,
                VerticalOptions = LayoutOptions.End,
                Margin = 0
            };

            var stackLayout = new StackLayout
            {
                Padding = 30,
                Spacing = 20
            };

            // Error Icon
            var errorIcon = new Label
            {
                Text = "📡",
                FontSize = 60,
                HorizontalOptions = LayoutOptions.Center
            };

            // Title
            var title = new Label
            {
                Text = errorTitle,
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2C3E50"),
                HorizontalTextAlignment = TextAlignment.Center
            };

            // Message
            var messageLabel = new Label
            {
                Text = errorMessage,
                FontSize = 15,
                TextColor = Color.FromArgb("#7F8C8D"),
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap
            };

            stackLayout.Children.Add(errorIcon);
            stackLayout.Children.Add(title);
            stackLayout.Children.Add(messageLabel);

            // Details if available
            Frame detailsFrame = null;
            if (!string.IsNullOrWhiteSpace(errorDetails))
            {
                detailsFrame = new Frame
                {
                    BackgroundColor = Color.FromArgb("#F8F9FA"),
                    BorderColor = Color.FromArgb("#E1E8ED"),
                    CornerRadius = 15,
                    Padding = 15,
                    HasShadow = false
                };

                var detailsLabel = new Label
                {
                    Text = errorDetails,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#95A5A6"),
                    LineBreakMode = LineBreakMode.WordWrap
                };

                detailsFrame.Content = detailsLabel;
                stackLayout.Children.Add(detailsFrame);
            }

            // Buttons
            var buttonsStack = new StackLayout { Spacing = 10 };
            var retryButton = new Button
            {
                Text = "Retry",
                BackgroundColor = Color.FromArgb("#7B68EE"),
                TextColor = Colors.White,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 12,
                HeightRequest = 50
            };

            var closeButton = new Button
            {
                Text = "Close",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#7B68EE"),
                BorderColor = Color.FromArgb("#7B68EE"),
                BorderWidth = 2,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 12,
                HeightRequest = 50
            };

            closeButton.Clicked += async (s, e) =>
            {
                await sheetContent.TranslateTo(0, 600, 250, Easing.CubicIn);
                await Navigation.PopModalAsync();
                ClearForm();
                await Navigation.PopAsync();
            };

            buttonsStack.Children.Add(retryButton);
            buttonsStack.Children.Add(closeButton);

            stackLayout.Children.Add(buttonsStack);

            sheetContent.Content = stackLayout;
            overlay.Children.Add(sheetContent);
            sheet.Content = overlay;

            await Navigation.PushModalAsync(sheet);

            // Animate sheet in
            sheetContent.TranslationY = 600;
            await sheetContent.TranslateTo(0, 0, 300, Easing.CubicOut);
        }
        catch (Exception sheetEx)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing failure sheet: {sheetEx.Message}");
            await ShowErrorSnackbar($"Failed: {sheetEx.Message}");
            await Task.Delay(2000);
        }
    }

    //private async Task ShowNetworkErrorSheet(Exception ex)
    //{
    //    try
    //    {
    //        string errorTitle = "Connection Error";
    //        string errorMessage = "Unable to connect to the server.";
    //        string errorDetails = "";

    //        if (ex is NetworkException networkEx)
    //        {
    //            errorMessage = networkEx.Message;
    //            if (networkEx.InnerException != null)
    //            {
    //                errorDetails = networkEx.InnerException.Message;
    //            }
    //        }
    //        else if (ex is TaskCanceledException)
    //        {
    //            errorMessage = "Request timed out. Please check your internet connection.";
    //        }
    //        else if (ex is HttpRequestException)
    //        {
    //            errorMessage = "Network error. Please check your internet connection.";
    //            errorDetails = ex.Message;
    //        }
    //        else
    //        {
    //            errorMessage = ex.Message;
    //        }

    //        var sheet = new ContentPage
    //        {
    //            BackgroundColor = Colors.Transparent
    //        };

    //        var overlay = new Grid
    //        {
    //            BackgroundColor = Color.FromArgb("#80000000")
    //        };

    //        var sheetContent = new Frame
    //        {
    //            BackgroundColor = Colors.White,
    //            CornerRadius = 30,
    //            Padding = 0,
    //            HasShadow = true,
    //            VerticalOptions = LayoutOptions.End,
    //            Margin = 0
    //        };

    //        var stackLayout = new StackLayout
    //        {
    //            Padding = 30,
    //            Spacing = 20
    //        };

    //        // Error Icon
    //        var errorIcon = new Label
    //        {
    //            Text = "📡",
    //            FontSize = 60,
    //            HorizontalOptions = LayoutOptions.Center
    //        };

    //        // Title
    //        var title = new Label
    //        {
    //            Text = errorTitle,
    //            FontSize = 22,
    //            FontAttributes = FontAttributes.Bold,
    //            TextColor = Color.FromArgb("#2C3E50"),
    //            HorizontalTextAlignment = TextAlignment.Center
    //        };

    //        // Message
    //        var messageLabel = new Label
    //        {
    //            Text = errorMessage,
    //            FontSize = 15,
    //            TextColor = Color.FromArgb("#7F8C8D"),
    //            HorizontalTextAlignment = TextAlignment.Center,
    //            LineBreakMode = LineBreakMode.WordWrap
    //        };

    //        stackLayout.Children.Add(errorIcon);
    //        stackLayout.Children.Add(title);
    //        stackLayout.Children.Add(messageLabel);

    //        // Details if available
    //        if (!string.IsNullOrWhiteSpace(errorDetails))
    //        {
    //            var detailsFrame = new Frame
    //            {
    //                BackgroundColor = Color.FromArgb("#F8F9FA"),
    //                BorderColor = Color.FromArgb("#E1E8ED"),
    //                CornerRadius = 15,
    //                Padding = 15,
    //                HasShadow = false
    //            };

    //            var detailsLabel = new Label
    //            {
    //                Text = errorDetails,
    //                FontSize = 12,
    //                TextColor = Color.FromArgb("#95A5A6"),
    //                LineBreakMode = LineBreakMode.WordWrap
    //            };

    //            detailsFrame.Content = detailsLabel;
    //            stackLayout.Children.Add(detailsFrame);
    //        }

    //        // Buttons
    //        var buttonsStack = new StackLayout { Spacing = 10 };
    //        var retryButton = new Button
    //        {
    //            Text = "Retry",
    //            BackgroundColor = Color.FromArgb("#7B68EE"),
    //            TextColor = Colors.White,
    //            FontSize = 16,
    //            FontAttributes = FontAttributes.Bold,
    //            CornerRadius = 12,
    //            HeightRequest = 50
    //        };

    //        var closeButton = new Button
    //        {
    //            Text = "Close",
    //            BackgroundColor = Colors.Transparent,
    //            TextColor = Color.FromArgb("#7B68EE"),
    //            BorderColor = Color.FromArgb("#7B68EE"),
    //            BorderWidth = 2,
    //            FontSize = 16,
    //            FontAttributes = FontAttributes.Bold,
    //            CornerRadius = 12,
    //            HeightRequest = 50
    //        };

    //        closeButton.Clicked += async (s, e) =>
    //        {
    //            await sheetContent.TranslateTo(0, 600, 250, Easing.CubicIn);
    //            await Navigation.PopModalAsync();
    //            ClearForm();
    //            await Navigation.PopAsync();
    //        };

    //        buttonsStack.Children.Add(retryButton);
    //        buttonsStack.Children.Add(closeButton);
    //        stackLayout.Children.Add(buttonsStack);

    //        sheetContent.Content = stackLayout;
    //        overlay.Children.Add(sheetContent);
    //        sheet.Content = overlay;

    //        await Navigation.PushModalAsync(sheet);

    //        // Animate sheet in
    //        sheetContent.TranslationY = 600;
    //        await sheetContent.TranslateTo(0, 0, 300, Easing.CubicOut);
    //    }
    //    catch (Exception error) // Changed from 'ex' to 'error'
    //    {
    //        System.Diagnostics.Debug.WriteLine($"Error showing failure sheet: {error.Message}");
    //        await ShowErrorSnackbar($"Failed: {error.Message}");
    //        await Task.Delay(2000);
    //    }
    //}
    private async Task ShowFailureSheet(DisputeResponse response)
    {
        try
        {
            var sheet = new ContentPage
            {
                BackgroundColor = Colors.Transparent
            };

            var overlay = new Grid
            {
                BackgroundColor = Color.FromArgb("#80000000")
            };

            var sheetContent = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 30,
                Padding = 0,
                HasShadow = true,
                VerticalOptions = LayoutOptions.End,
                Margin = 0
            };

            var stackLayout = new StackLayout
            {
                Padding = 30,
                Spacing = 20
            };

            // Error Icon
            var errorIcon = new Label
            {
                Text = "✕",
                FontSize = 60,
                TextColor = Color.FromArgb("#DC3545"),
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold
            };

            // Title
            var title = new Label
            {
                Text = "Dispute Failed",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2C3E50"),
                HorizontalTextAlignment = TextAlignment.Center
            };

            // Details Frame
            var detailsFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#FFF3CD"),
                BorderColor = Color.FromArgb("#FFEAA7"),
                CornerRadius = 15,
                Padding = 20,
                HasShadow = false
            };

            var detailsStack = new StackLayout { Spacing = 15 };

            if (!string.IsNullOrWhiteSpace(response.message))
            {
                var messageLabel = new Label
                {
                    Text = response.message,
                    FontSize = 15,
                    TextColor = Color.FromArgb("#856404"),
                    LineBreakMode = LineBreakMode.WordWrap,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                detailsStack.Children.Add(messageLabel);
            }

            if (!string.IsNullOrWhiteSpace(response.status))
            {
                detailsStack.Children.Add(CreateDetailRow("Status:", response.status));
            }

            detailsFrame.Content = detailsStack;

            // Buttons
            var buttonsStack = new StackLayout { Spacing = 10 };

            var retryButton = new Button
            {
                Text = "Try Again",
                BackgroundColor = Color.FromArgb("#7B68EE"),
                TextColor = Colors.White,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 12,
                HeightRequest = 50
            };

            retryButton.Clicked += async (s, e) =>
            {
                await Navigation.PopModalAsync();
                await ProcessDisputeCreation();
            };

            var closeButton = new Button
            {
                Text = "Close",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#7B68EE"),
                BorderColor = Color.FromArgb("#7B68EE"),
                BorderWidth = 2,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 12,
                HeightRequest = 50
            };

            closeButton.Clicked += async (s, e) =>
            {
                await sheetContent.TranslateTo(0, 600, 250, Easing.CubicIn);
                await Navigation.PopModalAsync();
            };

            buttonsStack.Children.Add(retryButton);
            buttonsStack.Children.Add(closeButton);

            stackLayout.Children.Add(buttonsStack);

            sheetContent.Content = stackLayout;
            overlay.Children.Add(sheetContent);
            sheet.Content = overlay;

            await Navigation.PushModalAsync(sheet);

            // Animate sheet in
            sheetContent.TranslationY = 600;
            await sheetContent.TranslateTo(0, 0, 300, Easing.CubicOut);
        }
        catch (Exception sheetEx)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing network error sheet: {sheetEx.Message}");
            await ShowErrorSnackbar($"Network error: {sheetEx.Message}");
        }
    }

    private StackLayout CreateDetailRow(string label, string value)
    {
        var row = new StackLayout
        {
            Orientation = StackOrientation.Vertical,
            Spacing = 5
        };

        var labelView = new Label
        {
            Text = label,
            FontSize = 13,
            TextColor = Color.FromArgb("#7F8C8D"),
            FontAttributes = FontAttributes.Bold
        };

        var valueView = new Label
        {
            Text = value,
            FontSize = 15,
            TextColor = Color.FromArgb("#2C3E50"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        row.Children.Add(labelView);
        row.Children.Add(valueView);

        return row;
    }

    private void ClearForm()
    {
        try
        {
            TokenText = "";
            DescriptionText = "";
            InputtToken.Text = "";
            Description.Text = "";
            ClearFieldError(TokenErrorLabel);
            ClearFieldError(DescriptionErrorLabel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing form: {ex.Message}");
        }
    }

    private async Task ShowErrorSnackbar(string message)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var snackbar = Snackbar.Make(
                    message,
                    null,
                    "DISMISS",
                    TimeSpan.FromSeconds(5),
                    new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb("#DC3545"),
                        TextColor = Colors.White,
                        ActionButtonTextColor = Colors.White,
                        CornerRadius = new CornerRadius(8),
                        Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                    });

                await snackbar.Show();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing error snackbar: {ex.Message}. Original: {message}");
        }
    }

    private async Task ShowSuccessSnackbar(string message)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var snackbar = Snackbar.Make(
                    message,
                    null,
                    "OK",
                    TimeSpan.FromSeconds(8),
                    new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb("#28A745"),
                        TextColor = Colors.White,
                        ActionButtonTextColor = Colors.White,
                        CornerRadius = new CornerRadius(10),
                        Font = Microsoft.Maui.Font.SystemFontOfSize(14),
                        ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14)
                    });

                await snackbar.Show(CancellationToken.None);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing success snackbar: {ex.Message}. Original: {message}");
        }
    }

    private async void CancelButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            UpdateLastInteraction();
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during cancel: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        try
        {
            base.OnDisappearing();

            // Clean up event handlers
            if (InputtToken != null)
            {
                InputtToken.TextChanged -= OnTokenTextChanged;
                InputtToken.Focused -= OnInputFocused;
                InputtToken.Unfocused -= OnTokenUnfocused;
            }

            if (Description != null)
            {
                Description.TextChanged -= OnDescriptionTextChanged;
                Description.Focused -= OnInputFocused;
                Description.Unfocused -= OnDescriptionUnfocused;
            }

            // Clean up timer
            _timeoutTimer?.Stop();
            _timeoutTimer?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
        }
    }

    protected override void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            // Focus on first input
            MainThread.BeginInvokeOnMainThread(() =>
            {
                InputtToken?.Focus();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during OnAppearing: {ex.Message}");
        }
    }

    // Handle hardware back button on Android
    protected override bool OnBackButtonPressed()
    {
        try
        {
            if (IsLoading)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var result = await DisplayAlert(
                        "Cancel Operation",
                        "Are you sure you want to go back? The dispute creation is in progress.",
                        "Yes, Go Back",
                        "No, Stay"
                    );

                    if (result)
                    {
                        await Navigation.PopAsync();
                    }
                });
                return true; // Prevent default back action
            }

            return base.OnBackButtonPressed();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error handling back button: {ex.Message}");
            return base.OnBackButtonPressed();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Custom exception for network errors
public class NetworkException : Exception
{
    public NetworkException(string message) : base(message) { }
    public NetworkException(string message, Exception innerException) : base(message, innerException) { }
}

// Enhanced data classes with better validation
internal class DisputeObject
{
    public string token { get; set; } = "";
    public string email { get; set; } = "";
    public string cancellationComment { get; set; } = "";
}

internal class DisputeResponse
{
    public bool success { get; set; }
    public string message { get; set; }
    public string status { get; set; }
    public string contractToken { get; set; }
    public string requestedAt { get; set; }
    public int? contractId { get; set; }
}