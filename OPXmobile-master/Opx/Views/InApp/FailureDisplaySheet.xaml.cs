using CommunityToolkit.Maui.Views;
using System.Timers;

namespace Opx.Views;

public partial class FailureDisplaySheet : Popup
{
    private readonly Kycform.BvnApiResponse _response;
    private System.Timers.Timer _inactivityTimer;
    private readonly TimeSpan _timeoutDuration = TimeSpan.FromMinutes(2);

    public FailureDisplaySheet(Kycform.BvnApiResponse response)
    {
        _response = response;
        InitializeComponent();
        LoadData();
        StartAnimations();
        InitializeInactivityTimer();
    }

    private void InitializeInactivityTimer()
    {
        _inactivityTimer = new System.Timers.Timer(_timeoutDuration.TotalMilliseconds);
        _inactivityTimer.Elapsed += OnInactivityTimeout;
        _inactivityTimer.AutoReset = false;
        _inactivityTimer.Start();
    }

    private async void OnInactivityTimeout(object sender, ElapsedEventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await AnimateClose();
            await NavigateToDashboard();
        });
    }

    private void ResetInactivityTimer()
    {
        _inactivityTimer?.Stop();
        _inactivityTimer?.Start();
    }

    private void OnUserInteraction(object sender, EventArgs e)
    {
        ResetInactivityTimer();
    }

    private async Task NavigateToDashboard()
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

    private void LoadData()
    {
        var errorMessage = _response.Message ?? "BVN verification failed. Please try again.";
        ErrorMessageLabel.Text = errorMessage;

        // Determine error type and provide appropriate guidance
        var (errorType, guidance, actionText) = DetermineErrorDetails(errorMessage);

        ErrorTypeLabel.Text = errorType;
        GuidanceLabel.Text = guidance;
        ActionButton.Text = actionText;

        TimestampLabel.Text = $"Failed on {DateTime.Now:MMM dd, yyyy • hh:mm tt}";

        // Show additional details if available
        if (!string.IsNullOrEmpty(_response.NextStep))
        {
            NextStepsFrame.IsVisible = true;
            NextStepLabel.Text = _response.NextStep;
        }
        else
        {
            NextStepsFrame.IsVisible = false;
        }
    }

    private (string errorType, string guidance, string actionText) DetermineErrorDetails(string errorMessage)
    {
        var lowerError = errorMessage.ToLower();

        if (lowerError.Contains("invalid") || lowerError.Contains("incorrect"))
        {
            return (
                "Invalid BVN",
                "The BVN you entered is not valid. Please check and ensure you've entered the correct 11-digit BVN number.",
                "Try Again"
            );
        }
        else if (lowerError.Contains("network") || lowerError.Contains("connection") || lowerError.Contains("timeout"))
        {
            return (
                "Network Error",
                "We couldn't connect to our servers. Please check your internet connection and try again.",
                "Retry Verification"
            );
        }
        else if (lowerError.Contains("not found") || lowerError.Contains("does not exist"))
        {
            return (
                "BVN Not Found",
                "The BVN you entered could not be found in our records. Please verify your BVN or contact support.",
                "Contact Support"
            );
        }
        else if (lowerError.Contains("already") || lowerError.Contains("exist"))
        {
            return (
                "BVN Already Registered",
                "This BVN has already been registered. If this is your BVN, please contact support for assistance.",
                "Contact Support"
            );
        }
        else
        {
            return (
                "Verification Failed",
                "We encountered an issue while verifying your BVN. Please try again or contact our support team.",
                "Try Again"
            );
        }
    }

    private async void StartAnimations()
    {
        await Task.Delay(100);

        // Animate error icon with shake effect
        await ErrorIcon.ScaleTo(0, 0);
        await ErrorIcon.ScaleTo(1.2, 400, Easing.SpringOut);
        await ErrorIcon.ScaleTo(1, 200, Easing.SpringIn);

        // Shake animation
        await ShakeAnimation(ErrorIcon);

        // Animate content with stagger effect
        await Task.Delay(200);
        await AnimateContent();
    }

    private async Task ShakeAnimation(View view)
    {
        await view.TranslateTo(-10, 0, 50);
        await view.TranslateTo(10, 0, 50);
        await view.TranslateTo(-10, 0, 50);
        await view.TranslateTo(10, 0, 50);
        await view.TranslateTo(0, 0, 50);
    }

    private async Task AnimateContent()
    {
        var elements = new List<View>
        {
            TitleLabel,
            ErrorTypeLabel,
            ErrorMessageLabel,
            GuidanceFrame,
            NextStepsFrame,
            SupportFrame,
            FooterStack
        };

        foreach (var element in elements)
        {
            if (element != null && element.IsVisible)
            {
                element.Opacity = 0;
                element.TranslationY = 20;
            }
        }

        foreach (var element in elements)
        {
            if (element != null && element.IsVisible)
            {
                await Task.WhenAll(
                    element.FadeTo(1, 300),
                    element.TranslateTo(0, 0, 300, Easing.CubicOut)
                );
                await Task.Delay(80);
            }
        }
    }

    private async void OnActionButtonClicked(object sender, EventArgs e)
    {
        ResetInactivityTimer();
        StopInactivityTimer();
        await AnimateClose();
    }

    private async void OnSupportButtonClicked(object sender, EventArgs e)
    {
        ResetInactivityTimer();

        try
        {
            await Launcher.OpenAsync("mailto:support@opxng.com?subject=BVN Verification Support");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening support: {ex.Message}");
        }

        StopInactivityTimer();
        await AnimateClose();
    }

    private async Task AnimateClose()
    {
        await ContentFrame.ScaleTo(0.95, 150);
        await ContentFrame.FadeTo(0, 150);
        Close();
    }

    private void StopInactivityTimer()
    {
        _inactivityTimer?.Stop();
        _inactivityTimer?.Dispose();
        _inactivityTimer = null;
    }

    protected override async Task OnClosed(object result, bool wasDismissedByTappingOutsideOfPopup, CancellationToken token = default)
    {
        StopInactivityTimer();
        await base.OnClosed(result, wasDismissedByTappingOutsideOfPopup, token);
    }
}