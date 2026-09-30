namespace Opx.Views;

public partial class WelcomePage : ContentPage
{
    public WelcomePage()
    {
        InitializeComponent(); AnimatePageLoad();
    }

    private async void AnimatePageLoad()
    {
        // Initial state - hide all elements
        LogoFrame.Opacity = 0;
        LogoFrame.Scale = 0.8;
        HeaderSection.Opacity = 0;
        HeaderSection.TranslationY = 50;
        AuthOptionsSection.Opacity = 0;
        AuthOptionsSection.TranslationY = 50;
        ContinueButton.Opacity = 0;
        ContinueButton.TranslationY = 50;
        SkipLabel.Opacity = 0;

        // Animate logo first
        await Task.Delay(200);
        await Task.WhenAll(
            LogoFrame.FadeTo(1, 600),
            LogoFrame.ScaleTo(1, 600, Easing.BounceOut)
        );

        // Animate header
        await Task.Delay(200);
        await Task.WhenAll(
            HeaderSection.FadeTo(1, 600),
            HeaderSection.TranslateTo(0, 0, 600, Easing.CubicOut)
        );

        // Animate auth options with staggered effect
        await Task.Delay(200);
        await Task.WhenAll(
            AuthOptionsSection.FadeTo(1, 600),
            AuthOptionsSection.TranslateTo(0, 0, 600, Easing.CubicOut)
        );

        // Animate individual cards with delay
        await AnimateAuthCards();

        // Animate button and skip option
        await Task.Delay(200);
        await Task.WhenAll(
            ContinueButton.FadeTo(1, 600),
            ContinueButton.TranslateTo(0, 0, 600, Easing.CubicOut),
            SkipLabel.FadeTo(1, 600)
        );
    }

    private async Task AnimateAuthCards()
    {
        // Initially hide cards
        FingerprintCard.Opacity = 0;
        FingerprintCard.TranslationX = -50;
        FaceCard.Opacity = 0;
        FaceCard.TranslationX = -50;
        PasswordCard.Opacity = 0;
        PasswordCard.TranslationX = -50;

        // Animate cards with staggered timing
        var tasks = new List<Task>();

        // Fingerprint card
        tasks.Add(Task.Run(async () =>
        {
            await Task.Delay(100);
            await Task.WhenAll(
                FingerprintCard.FadeTo(1, 400),
                FingerprintCard.TranslateTo(0, 0, 400, Easing.CubicOut)
            );
        }));

        // Face card
        tasks.Add(Task.Run(async () =>
        {
            await Task.Delay(200);
            await Task.WhenAll(
                FaceCard.FadeTo(1, 400),
                FaceCard.TranslateTo(0, 0, 400, Easing.CubicOut)
            );
        }));

        // Password card
        tasks.Add(Task.Run(async () =>
        {
            await Task.Delay(300);
            await Task.WhenAll(
                PasswordCard.FadeTo(1, 400),
                PasswordCard.TranslateTo(0, 0, 400, Easing.CubicOut)
            );
        }));

        await Task.WhenAll(tasks);
    }

    private async void OnFingerprintTapped(object sender, EventArgs e)
    {
        await AnimateCardSelection(FingerprintCard);
        // Add your fingerprint authentication logic here
    }

    private async void OnFaceRecognitionTapped(object sender, EventArgs e)
    {
        await AnimateCardSelection(FaceCard);
        // Add your face recognition logic here
    }

    private async void OnPasswordTapped(object sender, EventArgs e)
    {
        await AnimateCardSelection(PasswordCard);
        // Add your password authentication logic here
    }

    private async Task AnimateCardSelection(Frame card)
    {
        // Scale animation for tap feedback
        await card.ScaleTo(0.95, 100);
        await card.ScaleTo(1, 100, Easing.BounceOut);

        // Add subtle glow effect
        await Task.WhenAll(
            card.FadeTo(0.8, 100),
            card.FadeTo(1, 100)
        );
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        // Animate button press
        await ContinueButton.ScaleTo(0.95, 100);
        await ContinueButton.ScaleTo(1, 100, Easing.BounceOut);

        // Add your continue logic here
        await AnimatePageExit();
        await Navigation.PushModalAsync(new Kycform());
    }

    private async void OnSkipTapped(object sender, EventArgs e)
    {
        await SkipLabel.FadeTo(0.5, 100);
        await SkipLabel.FadeTo(1, 100);

        // Add your skip logic here
        await AnimatePageExit();
        await Navigation.PushModalAsync(new Kycform());
    }

    private async Task AnimatePageExit()
    {
        // Animate all elements out
        var tasks = new List<Task>
        {
            LogoFrame.FadeTo(0, 300),
            HeaderSection.FadeTo(0, 300),
            AuthOptionsSection.FadeTo(0, 300),
            ContinueButton.FadeTo(0, 300),
            SkipLabel.FadeTo(0, 300)
        };

        await Task.WhenAll(tasks);
    }

    // Add hover effect for interactive elements
    private async void OnCardPointerEntered(object sender, EventArgs e)
    {
        if (sender is Frame card)
        {
            await card.ScaleTo(1.02, 100);
        }
    }

    private async void OnCardPointerExited(object sender, EventArgs e)
    {
        if (sender is Frame card)
        {
            await card.ScaleTo(1, 100);
        }
    }

    // Legacy method for backward compatibility
    private async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
    {
        // This maintains compatibility with your original code
        await AnimatePageExit();
        await Navigation.PushModalAsync(new Kycform());
    }


    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushModalAsync(new Kycform());

    }
}