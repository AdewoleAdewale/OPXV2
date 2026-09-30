namespace Opx.Views;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

public partial class SecureSystemPage : ContentPage
{
    private bool _isAnimating = false;
    private Random _random = new Random();
    private bool _hasNavigated = false; // Prevent multiple navigation attempts

    public SecureSystemPage()
    {
        InitializeComponent();
        StartAnimations();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartPageAnimations();

        // Start the automatic navigation process
        _ = StartAutoNavigationAsync();
    }

    private async Task StartAutoNavigationAsync()
    {
        // Wait for page animations to complete and show the page for a reasonable time
        await Task.Delay(5000); // 5 seconds total display time

        // Navigate to MainPage automatically
        await NavigateToMainPageAsync();
    }

    private async Task NavigateToMainPageAsync()
    {
        if (_hasNavigated) return; // Prevent multiple navigation attempts

        _hasNavigated = true;
        _isAnimating = true; // Stop continuous animations

        // Create the new MainPage first
        var mainPage = new MainPage();

        // Option 1: Immediate replacement (no black background)
        //Application.Current.MainPage = mainPage;

        // Option 2: If you want a cross-fade effect, uncomment below and comment above
        mainPage.Opacity = 0.3;
        Application.Current.MainPage = mainPage;
        await mainPage.FadeTo(1, 300, Easing.SinInOut);

        // Mark that this page has been shown
        Preferences.Set("temp_page_shown", true);
    }

    private async void StartPageAnimations()
    {
        // Reset all elements to initial state
        TopBar.Opacity = 0;
        TopBar.TranslationY = -50;
        LogoSection.Opacity = 0;
        LogoSection.Scale = 0.8;
        FormCard.Opacity = 0;
        FormCard.TranslationY = 50;
        LoadingSection.Opacity = 0;
        LoadingSection.Scale = 0.9;

        // Animate top bar
        await TopBar.FadeTo(1, 300);
        await TopBar.TranslateTo(0, 0, 400, Easing.SpringOut);

        // Animate logo section
        var logoTask = LogoSection.FadeTo(1, 500);
        var logoScaleTask = LogoSection.ScaleTo(1, 600, Easing.SpringOut);
        await Task.WhenAll(logoTask, logoScaleTask);

        // Animate form card
        var formTask = FormCard.FadeTo(1, 600);
        var formTranslateTask = FormCard.TranslateTo(0, 0, 700, Easing.SpringOut);
        await Task.WhenAll(formTask, formTranslateTask);

        // Animate loading section
        var loadingTask = LoadingSection.FadeTo(1, 400);
        var loadingScaleTask = LoadingSection.ScaleTo(1, 500, Easing.SpringOut);
        await Task.WhenAll(loadingTask, loadingScaleTask);

        // Start continuous animations
        StartContinuousAnimations();
    }

    private async void StartAnimations()
    {
        await Task.Delay(1000); // Wait for UI to load

        // Animate progress bar
        _ = AnimateProgressBar();

        // Animate security badge
        _ = AnimateSecurityBadge();

        // Animate features grid
        _ = AnimateFeaturesGrid();

        // Start system initialization simulation
        _ = SimulateSystemInitialization();
    }

    private async void StartContinuousAnimations()
    {
        // Start pulsing animation for logo
        _ = StartLogoPulse();

        // Start dots animation
        _ = StartDotsAnimation();

        // Start status indicator animation
        _ = StartStatusIndicatorAnimation();
    }

    private async Task AnimateProgressBar()
    {
        while (!_isAnimating)
        {
            await ProgressBar.ScaleTo(1.1, 1000, Easing.SinInOut);
            await ProgressBar.ScaleTo(1.0, 1000, Easing.SinInOut);
            await Task.Delay(500);
        }
    }

    private async Task AnimateSecurityBadge()
    {
        while (!_isAnimating)
        {
            await SecurityBadge.ScaleTo(1.05, 2000, Easing.SinInOut);
            await SecurityBadge.ScaleTo(1.0, 2000, Easing.SinInOut);
        }
    }

    private async Task AnimateFeaturesGrid()
    {
        await Task.Delay(1500);

        // Animate each feature card with staggered timing
        if (FeaturesGrid.Children.Count >= 4)
        {
            var features = new[] {
                FeaturesGrid.Children[0] as VisualElement,
                FeaturesGrid.Children[1] as VisualElement,
                FeaturesGrid.Children[2] as VisualElement,
                FeaturesGrid.Children[3] as VisualElement
            };

            foreach (var feature in features)
            {
                if (feature != null && !_isAnimating)
                {
                    _ = feature.ScaleTo(1.05, 300, Easing.SinOut);
                    await Task.Delay(200);
                    _ = feature.ScaleTo(1.0, 300, Easing.SinIn);
                }
            }
        }
    }

    private async Task StartLogoPulse()
    {
        while (!_isAnimating)
        {
            await LogoFrame.ScaleTo(1.02, 3000, Easing.SinInOut);
            await LogoFrame.ScaleTo(1.0, 3000, Easing.SinInOut);
        }
    }

    private async Task StartDotsAnimation()
    {
        var dots = new[] { Dot1, Dot2, Dot3 };

        while (!_isAnimating)
        {
            foreach (var dot in dots)
            {
                if (_isAnimating) break;

                var scaleTask = dot.ScaleTo(1.5, 300, Easing.SinOut);
                var fadeTask = dot.FadeTo(0.5, 300);
                await Task.WhenAll(scaleTask, fadeTask);

                var resetScaleTask = dot.ScaleTo(1.0, 300, Easing.SinIn);
                var resetFadeTask = dot.FadeTo(1.0, 300);
                await Task.WhenAll(resetScaleTask, resetFadeTask);

                await Task.Delay(200);
            }
            await Task.Delay(1000);
        }
    }

    private async Task StartStatusIndicatorAnimation()
    {
        while (!_isAnimating)
        {
            await StatusIndicator.FadeTo(0.7, 1500, Easing.SinInOut);
            await StatusIndicator.FadeTo(1.0, 1500, Easing.SinInOut);
        }
    }

    // Remove the tap gesture handler since we want automatic navigation
    private async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
    {
        // Optionally, you can remove this entirely or keep it for testing
        // For now, let's keep it but make it not navigate
        var image = sender as Image;
        if (image != null)
        {
            // Just animate but don't navigate
            await image.ScaleTo(0.9, 100, Easing.SinOut);
            await image.ScaleTo(1.1, 200, Easing.BounceOut);
            await image.ScaleTo(1.0, 300, Easing.SpringOut);

            await image.RotateTo(360, 1000, Easing.SinInOut);
            image.Rotation = 0;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isAnimating = true; // Stop all animations
    }

    // Method to simulate system initialization with progress updates
    private async Task SimulateSystemInitialization()
    {
        var statusMessages = new[]
        {
            "Initializing security protocols...",
            "Verifying user credentials...",
            "Establishing secure connection...",
            "Loading system modules...",
            "System ready!"
        };

        foreach (var message in statusMessages)
        {
            if (_isAnimating) break;

            // You can update a status label here if you add one to your XAML
            // StatusLabel.Text = message;

            await Task.Delay(1000); // Wait 1 second between status updates
        }
    }
}