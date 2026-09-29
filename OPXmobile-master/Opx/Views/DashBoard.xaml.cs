using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using Application = Microsoft.Maui.Controls.Application;
using Plat = Microsoft.Maui.Controls.PlatformConfiguration;

namespace Opx.Views;


public partial class DashBoard : Microsoft.Maui.Controls.TabbedPage
{

    public DashBoard()
    {
        InitializeComponent();
        On<Plat.Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);

        // Set initial state for entrance animation
        this.Opacity = 0;
        this.Scale = 0.9;

        // Subscribe to page events
        this.Appearing += OnDashBoardAppearing;
        this.CurrentPageChanged += OnCurrentPageChanged;
    }

    protected override bool OnBackButtonPressed()
    {
        var navStack = Navigation.NavigationStack;
        if (navStack.Count > 1)
        {
            // Properly handle async operation
            Task.Run(async () => await Navigation.PopAsync());
        }
        else
        {
            // Handle case when there's nowhere to go back to
            // Navigate to the dashboard (TabPage)
            Application.Current.MainPage = new DashBoard();
        }

        return true; // Indicates we handled the back button press
    }
    private void ResetButton_Clicked(object sender, EventArgs e)
    {
        Preferences.Remove("temp_page_shown");
        // Restart the app or notify the user to restart
    }
    private async void OnDashBoardAppearing(object sender, EventArgs e)
    {
        // Entrance animation for the entire tabbed page
        await Task.WhenAll(
            this.FadeTo(1, 500, Easing.CubicOut),
            this.ScaleTo(1, 500, Easing.CubicOut)
        );

        // Check if KYC verification is needed
        await CheckAndShowKycForm();
    }
    private bool _kycModalAlreadyShown = false; // prevent multiple triggers in same session

    private async Task CheckAndShowKycForm()
    {
        try
        {
            // ✅ Delay execution to let UI stabilize fully
            await Task.Delay(2000);

            // ✅ ONLY show KYC form if accountNumber is null or empty
            bool needsKycVerification = string.IsNullOrEmpty(LoginPage.accountNumber);

            // If account number exists, do NOT show KYC form at all
            if (!needsKycVerification)
            {
                System.Diagnostics.Debug.WriteLine("Account number exists, skipping KYC form");
                return;
            }

            // Only check session flag if KYC is actually needed
            bool hasShownKycInSession = Preferences.Get("kyc_shown_this_session", false);

            // ✅ Show KYC form only once per session when account number is null
            if (!hasShownKycInSession && !_kycModalAlreadyShown)
            {
                _kycModalAlreadyShown = true;
                Preferences.Set("kyc_shown_this_session", true);

                // ✅ Use Dispatcher to avoid blocking Appearing thread
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Navigation.PushModalAsync(new Kycform(), true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"KYC Modal Error: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"KYC check error: {ex.Message}");
        }
    }
    private async void OnCurrentPageChanged(object sender, EventArgs e)
    {
        if (CurrentPage != null)
        {
            // Reset page state
            CurrentPage.Opacity = 0;
            CurrentPage.Scale = 0.95;
            CurrentPage.TranslationY = 20;

            // Animate page transition
            await Task.WhenAll(
                CurrentPage.FadeTo(1, 300, Easing.CubicOut),
                CurrentPage.ScaleTo(1, 300, Easing.CubicOut),
                CurrentPage.TranslateTo(0, 0, 300, Easing.CubicOut)
            );

            // Add a subtle bounce effect for tab icons
            await AnimateTabSelection();
        }
    }

    private async Task AnimateTabSelection()
    {
        // This creates a subtle bounce effect
        await Task.WhenAll(
            this.ScaleTo(1.02, 100, Easing.CubicOut),
            this.ScaleTo(1, 100, Easing.CubicIn)
        );
    }

    // Optional: Add method to animate individual elements within pages
    public async Task AnimateContent(View content)
    {
        if (content != null)
        {
            content.Opacity = 0;
            content.TranslationY = 30;

            await Task.WhenAll(
                content.FadeTo(1, 400, Easing.CubicOut),
                content.TranslateTo(0, 0, 400, Easing.CubicOut)
            );
        }
    }

    // Add this method to animate multiple elements in sequence
    public async Task AnimateContentSequentially(params View[] views)
    {
        foreach (var view in views)
        {
            if (view != null)
            {
                view.Opacity = 0;
                view.TranslationY = 20;

                var fadeTask = view.FadeTo(1, 250, Easing.CubicOut);
                var slideTask = view.TranslateTo(0, 0, 250, Easing.CubicOut);

                await Task.WhenAll(fadeTask, slideTask);
                await Task.Delay(50); // Small delay between animations
            }
        }
    }
}