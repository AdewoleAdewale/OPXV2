using AiForms.Dialogs;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using Opx.Model;
using Opx.Services;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Opx.Views;

public partial class CreateContract : Popup
{
    private bool _isDragging = false;
    private double _initialY;
    private double _dragThreshold = 100;
    private Timer _timeoutTimer;
    private readonly int _timeoutMinutes = 5;
    private bool _isDisposed = false;
    private bool _isProcessing = false; // Add this to prevent multiple clicks

    public CreateContract()
    {
        InitializeComponent();
        CREATECONTRACT.IsVisible = false;
        ResponseLabel.IsVisible = false;
        InitializeSheet();
        InitializePopupEvents();
        StartTimeoutTimer();
    }

    private void InitializeSheet()
    {
        // Set initial position for slide-up animation
        MainFrame.TranslationY = 500;

        // Add pan gesture recognizer for dragging
        var panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        //DragHandle.GestureRecognizers.Add(panGesture);

        // Animate sheet entrance
        AnimateSheetIn();
    }


    private async void AnimateSheetIn()
    {
        try
        {
            await Task.Delay(50);

            // Slide up animation
            var slideAnimation = MainFrame.TranslateTo(0, 0, 300, Easing.CubicOut);

            // Fade in animation
            MainFrame.Opacity = 0;
            var fadeAnimation = MainFrame.FadeTo(1, 300, Easing.Linear);

            await Task.WhenAll(slideAnimation, fadeAnimation);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
        }
    }

    private async Task AnimateSheetOut()
    {
        try
        {
            // Slide down animation
            var slideAnimation = MainFrame.TranslateTo(0, 500, 250, Easing.CubicIn);

            // Fade out animation
            var fadeAnimation = MainFrame.FadeTo(0, 250, Easing.Linear);

            await Task.WhenAll(slideAnimation, fadeAnimation);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
        }
    }

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        try
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _isDragging = true;
                    _initialY = MainFrame.TranslationY;
                    break;

                case GestureStatus.Running:
                    if (_isDragging)
                    {
                        // Only allow downward dragging
                        double newY = Math.Max(0, _initialY + e.TotalY);
                        MainFrame.TranslationY = newY;

                        // Adjust opacity based on drag distance
                        double opacity = Math.Max(0.3, 1 - (newY / 300));
                        MainFrame.Opacity = opacity;
                    }
                    break;

                case GestureStatus.Completed:
                    _isDragging = false;
                    HandleDragComplete(e.TotalY);
                    break;

                case GestureStatus.Canceled:
                    _isDragging = false;
                    SnapBack();
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Pan gesture error: {ex.Message}");
        }
    }

    private async void HandleDragComplete(double totalY)
    {
        try
        {
            if (totalY > _dragThreshold)
            {
                // Dismiss the sheet
                await AnimateSheetOut();
                await ClosePopupAsync();
            }
            else
            {
                // Snap back to original position
                await SnapBack();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Drag complete error: {ex.Message}");
        }
    }

    private async Task SnapBack()
    {
        try
        {
            var slideAnimation = MainFrame.TranslateTo(0, 0, 200, Easing.SpringOut);
            var fadeAnimation = MainFrame.FadeTo(1, 200, Easing.Linear);
            await Task.WhenAll(slideAnimation, fadeAnimation);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Snap back error: {ex.Message}");
        }
    }

    private void StartTimeoutTimer()
    {
        try
        {
            _timeoutTimer = new Timer(OnTimeout, null, TimeSpan.FromMinutes(_timeoutMinutes), Timeout.InfiniteTimeSpan);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Timer initialization error: {ex.Message}");
        }
    }

    private async void OnTimeout(object state)
    {
        try
        {
            if (_isDisposed) return;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ShowTimeoutSnackbar();
                await AnimateSheetOut();
                await CloseAsync();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Timeout error: {ex.Message}");
        }
    }


    private void ResetTimeout()
    {
        try
        {
            _timeoutTimer?.Dispose();
            StartTimeoutTimer();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Reset timeout error: {ex.Message}");
        }
    }

    private async void OnEntryFocused(object sender, FocusEventArgs e)
    {
        try
        {
            ResetTimeout();

            // Animate entry focus
            if (sender is VisualElement element)
            {
                await element.ScaleTo(1.02, 150, Easing.CubicOut);
                await element.ScaleTo(1, 150, Easing.CubicIn);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Entry focus error: {ex.Message}");
        }
    }

    private async void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            ResetTimeout();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Text changed error: {ex.Message}");
        }
    }


    private async Task AnimateButtonPress(Button button)
    {
        try
        {
            await button.ScaleTo(0.95, 100, Easing.CubicOut);
            await button.ScaleTo(1, 100, Easing.CubicIn);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Button animation error: {ex.Message}");
        }
    }

    private async Task AnimateFieldError(Frame frame)
    {
        try
        {
            var originalColor = frame.BorderColor;
            frame.BorderColor = Colors.Red;

            // Shake animation
            await frame.TranslateTo(-10, 0, 50);
            await frame.TranslateTo(10, 0, 50);
            await frame.TranslateTo(-5, 0, 50);
            await frame.TranslateTo(5, 0, 50);
            await frame.TranslateTo(0, 0, 50);

            // Restore original color after delay
            await Task.Delay(2000);
            frame.BorderColor = originalColor;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Field error animation error: {ex.Message}");
        }
    }



    private async Task CreateContractAsync(decimal contractAmount)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(LoginPage.myemail))
            {
                await ShowErrorSheet("USER EMAIL NOT FOUND", "Please log in again to continue.");
                return;
            }

            var request = new ContractInitiateRequest
            {
                SellerEmail = LoginPage.myemail,
                BuyerPhone = UserPhone.Text?.Trim() ?? "",
                Amount = contractAmount,
                Description = Description.Text?.Trim() ?? ""
            };

            var result = await OpxApi.PostAsync<ContractInitiateResponse>("/ContractsApi/initiate", request);

            if (result.IsNetworkError)
            {
                await ShowErrorSheet("Network Error", result.ErrorMessage);
                return;
            }

            var r = result.Data;

            // 400 / 401 / 500 – the API still returns a JSON body for the business errors
            if (!result.IsHttpSuccess)
            {
                if (r != null && r.AvailableBalance.HasValue)
                {
                    // "Buyer has insufficient available balance." – show the numbers the API gives us
                    var detail = $"{r.Message}\n\nAvailable balance: ₦{r.AvailableBalance:N2}" +
                                 (r.RequiredAmount.HasValue ? $"\nRequired: ₦{r.RequiredAmount:N2}" : "") +
                                 (r.ProcessingFee.HasValue ? $"\nProcessing fee: ₦{r.ProcessingFee:N2}" : "");
                    await ShowErrorSheet("Insufficient Balance", detail);
                }
                else
                {
                    await ShowErrorSheet($"Contract Failed ({(int)result.StatusCode})",
                        r?.Message ?? result.ErrorMessage);
                }
                return;
            }

            if (r == null)
            {
                await ShowErrorSheet("Invalid Response", "Failed to parse server response. Please try again.");
                return;
            }

            // 200 – seller has not set up a wallet yet
            if (r.RequiresSetup)
            {
                await Snackbar.Make(r.Message ?? "Please complete your account setup first.", null, "OK",
                    TimeSpan.FromSeconds(3), new SnackbarOptions
                    {
                        BackgroundColor = Color.FromArgb("#4CAF50"),
                        TextColor = Colors.White,
                        ActionButtonTextColor = Colors.White,
                        CornerRadius = new CornerRadius(10),
                        Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                    }).Show();

                await Task.Delay(2500);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Application.Current.MainPage.Navigation.PushModalAsync(new Kycform()));
                return;
            }

            // 200 – contract created
            if (r.Success)
            {
                await ShowSuccessSheet(r);
                await Task.Delay(500);
                await AnimateSheetOut();
                await ClosePopupAsync();
                return;
            }

            // 200 – buyer phone is not registered on OPX
            if (r.Exists == false)
            {
                var msg = r.Message ?? "This buyer is not registered on OPX.";
                if (!string.IsNullOrWhiteSpace(r.RegistrationLink))
                    msg += $"\n\nAsk the buyer to register here:\n{r.RegistrationLink}";
                await ShowErrorSheet("Buyer Not Registered", msg);
                return;
            }

            await ShowErrorSheet("Contract Creation Failed", r.Message ?? "Unknown contract error");
        }
        catch (Exception ex)
        {
            await ShowErrorSheet("Failed to Create Contract", $"Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"CreateContract error: {ex.Message}");
        }
    }


    private async Task ShowSuccessSheet(ContractInitiateResponse response)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var successSheet = new ContractSuccessSheet(response);
                await Application.Current.MainPage.ShowPopupAsync(successSheet);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing success sheet: {ex.Message}");
            // Fallback to old method
            await ShowSuccessMessage($"Contract created! Token: {response.Token}");
        }
    }

    private async Task ShowErrorSheet(string title, string details, Action retryAction = null)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var errorSheet = new ContractErrorSheet(title, details, retryAction);
                await Application.Current.MainPage.ShowPopupAsync(errorSheet);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing error sheet: {ex.Message}");
            // Fallback to old method
            await ShowErrorMessage($"{title}: {details}");
        }
    }


    private async Task AnimateSuccess()
    {
        try
        {
            // Success pulse animation
            await MainFrame.ScaleTo(1.05, 200, Easing.CubicOut);
            await MainFrame.ScaleTo(1, 200, Easing.CubicIn);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Success animation error: {ex.Message}");
        }
    }

    // NEW METHOD: Show messages directly on the popup instead of snackbars
    private async Task ShowSuccessMessage(string message)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ResponseLabel.Text = $"✓ {message}";
                ResponseLabel.TextColor = Colors.ForestGreen;
                ResponseLabel.IsVisible = true;
                ResponseLabel.FontSize = 12;

                // Animate the success message
                ResponseLabel.Opacity = 0;
                await ResponseLabel.FadeTo(1, 300);

                // Optional: Also show a subtle snackbar that appears above the popup
                try
                {
                    var snackbar = Snackbar.Make(message, null, "OK", TimeSpan.FromSeconds(3), new SnackbarOptions
                    {
                        BackgroundColor = Colors.ForestGreen,
                        TextColor = Colors.White,
                        ActionButtonTextColor = Colors.White,
                        CornerRadius = new CornerRadius(10),
                        Font = Microsoft.Maui.Font.SystemFontOfSize(12)
                    });

                    // Show snackbar with a slight delay to ensure it appears above popup
                    await Task.Delay(100);
                    await snackbar.Show(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error showing success snackbar: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing success message: {ex.Message}");
        }
    }

    private async Task ShowErrorMessage(string message)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ResponseLabel.Text = $"✗ {message}";
                ResponseLabel.TextColor = Colors.Red;
                ResponseLabel.IsVisible = true;
                ResponseLabel.FontSize = 12;

                // Animate the error message
                ResponseLabel.Opacity = 0;
                await ResponseLabel.FadeTo(1, 300);
            });

            // Also show snackbar for error
            try
            {
                var snackbar = Snackbar.Make(message, null, "TRY AGAIN", TimeSpan.FromSeconds(5), new SnackbarOptions
                {
                    BackgroundColor = Color.FromArgb("#A25AC4"),
                    TextColor = Colors.White,
                    ActionButtonTextColor = Colors.White,
                    CornerRadius = new CornerRadius(8),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(12)
                });
                await snackbar.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing error snackbar: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing error message: {ex.Message}");
        }
    }

    private async Task ShowInfoMessage(string message)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ResponseLabel.Text = $"ℹ️ {message}";
                ResponseLabel.TextColor = Colors.Orange;
                ResponseLabel.IsVisible = true;
                ResponseLabel.FontSize = 12;

                // Animate the info message
                ResponseLabel.Opacity = 0;
                await ResponseLabel.FadeTo(1, 300);

                // Auto-hide after 2 seconds
                await Task.Delay(2000);
                await ResponseLabel.FadeTo(0, 300);
                ResponseLabel.IsVisible = false;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing info message: {ex.Message}");
        }
    }

    private async Task ShowTimeoutSnackbar()
    {
        try
        {
            var snackbar = Snackbar.Make($"Session timed out after {_timeoutMinutes} minutes of inactivity", null, "OK", TimeSpan.FromSeconds(3), new SnackbarOptions
            {
                BackgroundColor = Colors.Orange,
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.White,
                CornerRadius = new CornerRadius(8),
                Font = Microsoft.Maui.Font.SystemFontOfSize(14)
            });
            await snackbar.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing timeout snackbar: {ex.Message}");
        }
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Prevent closing while processing
            if (_isProcessing)
            {
                await ShowInfoMessage("Please wait for the current operation to complete...");
                return;
            }

            await AnimateButtonPress(sender as Button);
            await AnimateSheetOut();
            await ClosePopupAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cancel button error: {ex.Message}");
            await ClosePopupAsync(); // Fallback close
        }
    }

    private async void UserPhone_Unfocused(object sender, FocusEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(UserPhone?.Text))
            {
                ResponseLabel.Text = "";
                ResponseLabel.IsVisible = false;
                CREATECONTRACT.IsVisible = false;
                CREATECONTRACT.IsEnabled = false;
                return;
            }

            // 🔸 Show and animate loading state
            ResponseLabel.IsVisible = true;
            ResponseLabel.Text = "Verifying phone number...";
            ResponseLabel.TextColor = Colors.Orange;
            await ResponseLabel.FadeTo(0.5, 200);

            var result = await OpxApi.GetAsync<CheckBuyerResponse>(
                "/ContractsApi/check-buyer?phoneNumber=" + Uri.EscapeDataString(UserPhone.Text.Trim()));

            if (result.IsNetworkError)
            {
                CREATECONTRACT.IsVisible = false;
                CREATECONTRACT.IsEnabled = false;
                await HandlePhoneVerificationError(result.ErrorMessage);
                return;
            }

            var buyer = result.Data;

            if (result.IsHttpSuccess && buyer is { Success: true, Exists: true })
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    ResponseLabel.Text = $"✓ Name: {buyer.BuyerName ?? "N/A"}\n{buyer.Message ?? "Buyer found"}";
                    ResponseLabel.TextColor = Colors.ForestGreen;
                    ResponseLabel.IsVisible = true;
                    await ResponseLabel.FadeTo(1, 200);

                    CREATECONTRACT.IsVisible = true;
                    CREATECONTRACT.IsEnabled = true;
                    CREATECONTRACT.Opacity = 0;
                    await CREATECONTRACT.FadeTo(1, 300);
                });
            }
            else
            {
                CREATECONTRACT.IsVisible = false;
                CREATECONTRACT.IsEnabled = false;

                // 200 + exists:false → buyer isn't on OPX; the API supplies message + registrationLink
                var msg = result.IsHttpSuccess
                    ? (buyer?.Message ?? "Buyer not registered on OPX")
                    : result.ErrorMessage;
                if (result.IsHttpSuccess && !string.IsNullOrWhiteSpace(buyer?.RegistrationLink))
                    msg += $"\nRegister: {buyer!.RegistrationLink}";
                await HandlePhoneVerificationError(msg);
            }
        }
        catch (TaskCanceledException)
        {
            await HandlePhoneVerificationError("Phone verification timed out");
        }
        catch (HttpRequestException ex)
        {
            await HandlePhoneVerificationError("Network error during verification");
            System.Diagnostics.Debug.WriteLine($"Phone verification network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            await HandlePhoneVerificationError("Unexpected error during verification");
            System.Diagnostics.Debug.WriteLine($"UserPhone_Unfocused Error: {ex}");
        }
    }

    private async Task HandlePhoneVerificationError(string message)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                ResponseLabel.Text = $"✗ {message}";
                ResponseLabel.TextColor = Colors.Red;
                ResponseLabel.IsVisible = true;
                await ResponseLabel.FadeTo(1, 200);

                CREATECONTRACT.IsVisible = false;
                CREATECONTRACT.IsEnabled = false;
            });

            var snackbar = Snackbar.Make($"NOTIFICATION: {message}", null, "TRY AGAIN", TimeSpan.FromSeconds(5), new SnackbarOptions
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
            System.Diagnostics.Debug.WriteLine($"Error handling phone verification error: {ex.Message}");
        }
    }

    private void InitializePopupEvents()
    {
        // Subscribe to popup events for proper cleanup
        this.Closed += OnPopupClosed;
    }

    private void OnPopupClosed(object sender, PopupClosedEventArgs e)
    {
        try
        {
            CleanupResources();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnPopupClosed error: {ex.Message}");
        }
    }

    private async Task ClosePopupAsync()
    {
        try
        {
            CleanupResources();
            await this.CloseAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ClosePopupAsync error: {ex.Message}");
            // Ensure cleanup even if CloseAsync fails
            CleanupResources();
        }
    }

    private void CleanupResources()
    {
        try
        {
            if (_isDisposed) return;

            _isDisposed = true;
            _isProcessing = false; // Reset processing flag on cleanup
            _timeoutTimer?.Dispose();
            _timeoutTimer = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CleanupResources error: {ex.Message}");
        }
    }

    private async void CREATECONTRACT_Clicked_1(object sender, EventArgs e)
    {
        try
        {
            // Prevent multiple clicks - CHECK THIS FIRST
            if (_isProcessing)
            {
                await ShowInfoMessage("Contract creation is already in progress...");
                return;
            }

            _isProcessing = true;

            // Animate button press
            await AnimateButtonPress(CREATECONTRACT);

            // Validate input fields
            if (string.IsNullOrWhiteSpace(UserPhone?.Text))
            {
                await ShowErrorMessage("Please enter Receiver Phone");
                await AnimateFieldError(UserPhoneFrame);
                _isProcessing = false; // Reset processing flag
                return;
            }

            if (string.IsNullOrWhiteSpace(Description?.Text))
            {
                await ShowErrorMessage("Please Enter Contract Description");
                await AnimateFieldError(DescriptionFrame);
                _isProcessing = false; // Reset processing flag
                return;
            }

            if (string.IsNullOrWhiteSpace(ContractAmount?.Text))
            {
                await ShowErrorMessage("Please enter Amount");
                await AnimateFieldError(AmountFrame);
                _isProcessing = false; // Reset processing flag
                return;
            }

            // Validate and convert amount to decimal
            if (!decimal.TryParse(ContractAmount.Text.Trim(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out decimal contractAmount))
            {
                await ShowErrorMessage("Please enter a valid amount (numbers only)");
                await AnimateFieldError(AmountFrame);
                _isProcessing = false; // Reset processing flag
                return;
            }

            if (contractAmount <= 0)
            {
                await ShowErrorMessage("Amount must be greater than zero");
                await AnimateFieldError(AmountFrame);
                _isProcessing = false; // Reset processing flag
                return;
            }

            // Disable button to prevent multiple submissions
            CREATECONTRACT.IsEnabled = false;
            CREATECONTRACT.Text = "CREATING..."; // Visual feedback

            try
            {
                Configurations.LoadingConfig = new LoadingConfig
                {
                    Opacity = 0.4,
                    DefaultMessage = "Creating Contract, please wait...",
                    FontSize = 12,
                };

                await Loading.Instance.StartAsync(async progress =>
                {
                    try
                    {
                        // Progress simulation
                        for (var i = 0; i < 50; i++)
                        {
                            await Task.Delay(20);
                            progress.Report((i + 1) * 0.02d);
                        }

                        await CreateContractAsync(contractAmount);
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorMessage($"Error creating contract: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorMessage($"Failed to start contract creation: {ex.Message}");
            }
            finally
            {
                CREATECONTRACT.IsEnabled = true;
                CREATECONTRACT.Text = "CREATE CONTRACT";
                _isProcessing = false;
            }

        }
        catch (Exception ex)
        {
            await ShowErrorMessage($"Unexpected error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Button click error: {ex.Message}");
            _isProcessing = false; // Reset processing flag
        }
    }
}

public class ContractSuccessSheet : Popup
{
    private Frame mainFrame;
    private Label titleLabel;
    private Label messageLabel;
    private Label tokenLabel;
    private Label detailsLabel;
    private Button closeButton;

    public ContractSuccessSheet(ContractInitiateResponse response)
    {
        Size = new Size(360, 450);
        Color = Colors.Transparent;

        mainFrame = new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 24,
            Padding = 0,
            HasShadow = true,
            Content = CreateSuccessContent(response)
        };

        Content = mainFrame;
        AnimateIn();
    }

    private VerticalStackLayout CreateSuccessContent(ContractInitiateResponse response)
    {
        // Success Icon
        var iconLabel = new Label
        {
            Text = "✓",
            FontSize = 64,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.ForestGreen,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 30, 0, 10)
        };

        // Title
        titleLabel = new Label
        {
            Text = "Contract Created!",
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Black,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };

        // Message
        messageLabel = new Label
        {
            Text = response.Message ?? "Your contract has been created successfully.",
            FontSize = 14,
            TextColor = Colors.Gray,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(20, 0, 20, 15)
        };

        // Token Display
        tokenLabel = new Label
        {
            Text = $"Token: {response.Token ?? "N/A"}",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#6B46C1"),
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(20, 0, 20, 15)
        };

        // Contract Details
        var detailsText = $"Amount: ₦{response.Amount:N2}\n" +
                         $"Processing fee: ₦{response.ProcessingFee ?? 0:N2}\n" +
                         $"Status: {response.Status ?? "Pending"}\n" +
                         $"Buyer: {response.BuyerName ?? "N/A"}\n" +
                         $"Email: {response.BuyerEmail ?? "N/A"}";

        detailsLabel = new Label
        {
            Text = detailsText,
            FontSize = 13,
            TextColor = Colors.DarkGray,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(20, 0, 20, 20)
        };

        // Close Button
        closeButton = new Button
        {
            Text = "DONE",
            BackgroundColor = Colors.ForestGreen,
            TextColor = Colors.White,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 50,
            Margin = new Thickness(20, 10, 20, 20)
        };
        closeButton.Clicked += async (s, e) => await CloseSheet();

        return new VerticalStackLayout
        {
            Children = { iconLabel, titleLabel, messageLabel, tokenLabel, detailsLabel, closeButton }
        };
    }

    private async void AnimateIn()
    {
        mainFrame.TranslationY = 400;
        mainFrame.Opacity = 0;

        await Task.WhenAll(
            mainFrame.TranslateTo(0, 0, 350, Easing.CubicOut),
            mainFrame.FadeTo(1, 350)
        );
    }

    private async Task CloseSheet()
    {
        await Task.WhenAll(
            mainFrame.TranslateTo(0, 400, 300, Easing.CubicIn),
            mainFrame.FadeTo(0, 300)
        );
        await this.CloseAsync();
    }
}

public class ContractErrorSheet : Popup
{
    private Frame mainFrame;
    private Label titleLabel;
    private Label errorMessageLabel;
    private Label errorDetailsLabel;
    private Button retryButton;
    private Button closeButton;
    private Action onRetry;

    public ContractErrorSheet(string errorMessage, string errorDetails = null, Action retryAction = null)
    {
        Size = new Size(360, 450);
        Color = Colors.Transparent;
        onRetry = retryAction;

        mainFrame = new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 24,
            Padding = 0,
            HasShadow = true,
            Content = CreateErrorContent(errorMessage, errorDetails)
        };

        Content = mainFrame;
        AnimateIn();
    }

    private VerticalStackLayout CreateErrorContent(string errorMessage, string errorDetails)
    {
        // Error Icon
        var iconLabel = new Label
        {
            Text = "✗",
            FontSize = 64,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Red,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 30, 0, 10)
        };

        // Title
        titleLabel = new Label
        {
            Text = "Contract Creation Failed",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Black,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };

        // Error Message
        errorMessageLabel = new Label
        {
            Text = errorMessage ?? "Failed to create contract. Please try again.",
            FontSize = 14,
            TextColor = Colors.Red,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(20, 0, 20, 15)
        };

        // Error Details (if available)
        errorDetailsLabel = new Label
        {
            Text = errorDetails,
            FontSize = 12,
            TextColor = Colors.Gray,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(20, 0, 20, 20),
            IsVisible = !string.IsNullOrWhiteSpace(errorDetails)
        };

        // Network Tips
        var tipsLabel = new Label
        {
            Text = "Possible issues:\n• Check your internet connection\n• Verify entered information\n• Try again in a moment",
            FontSize = 12,
            TextColor = Colors.DarkGray,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Start,
            Margin = new Thickness(30, 0, 30, 20)
        };

        // Buttons Container
        var buttonsLayout = new HorizontalStackLayout
        {
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(20, 10, 20, 20)
        };

        // Close Button
        closeButton = new Button
        {
            Text = "CLOSE",
            BackgroundColor = Colors.LightGray,
            TextColor = Colors.Black,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            WidthRequest = 140,
            HeightRequest = 50
        };
        closeButton.Clicked += async (s, e) => await CloseSheet();

        // Retry Button (if retry action provided)
        if (onRetry != null)
        {
            retryButton = new Button
            {
                Text = "RETRY",
                BackgroundColor = Color.FromArgb("#A25AC4"),
                TextColor = Colors.White,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 12,
                WidthRequest = 140,
                HeightRequest = 50
            };
            retryButton.Clicked += async (s, e) => await RetryAction();
            buttonsLayout.Children.Add(retryButton);
        }

        buttonsLayout.Children.Add(closeButton);

        return new VerticalStackLayout
        {
            Children = { iconLabel, titleLabel, errorMessageLabel, errorDetailsLabel, tipsLabel, buttonsLayout }
        };
    }

    private async void AnimateIn()
    {
        mainFrame.TranslationY = 400;
        mainFrame.Opacity = 0;

        await Task.WhenAll(
            mainFrame.TranslateTo(0, 0, 350, Easing.CubicOut),
            mainFrame.FadeTo(1, 350)
        );

        // Shake animation for error
        await Task.Delay(100);
        await mainFrame.TranslateTo(-10, 0, 50);
        await mainFrame.TranslateTo(10, 0, 50);
        await mainFrame.TranslateTo(-5, 0, 50);
        await mainFrame.TranslateTo(5, 0, 50);
        await mainFrame.TranslateTo(0, 0, 50);
    }

    private HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private async Task CloseSheet()
    {
        await Task.WhenAll(
            mainFrame.TranslateTo(0, 400, 300, Easing.CubicIn),
            mainFrame.FadeTo(0, 300)
        );
        await this.CloseAsync();
    }

    private async Task RetryAction()
    {
        await CloseSheet();
        onRetry?.Invoke();
    }
}