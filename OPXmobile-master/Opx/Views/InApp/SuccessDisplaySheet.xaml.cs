using CommunityToolkit.Maui.Views;

namespace Opx.Views;

public partial class SuccessDisplaySheet : Popup
{
    private readonly Kycform.BvnApiResponse _response;

    public SuccessDisplaySheet(Kycform.BvnApiResponse response)
    {
        _response = response;
        InitializeComponent();
        LoadData();
        StartAnimations();
    }

    private void LoadData()
    {
        // Greet by the account name returned by /agencies/create (falls back to the signed-in user's name).
        var name = !string.IsNullOrWhiteSpace(_response.AccountName) ? _response.AccountName
                 : !string.IsNullOrWhiteSpace(LoginPage.myfullname) ? LoginPage.myfullname
                 : "User";

        UsernameLabel.Text = name;
        MessageLabel.Text = _response.Message ?? "Your BVN has been verified successfully!";
        ReferenceLabel.Text = string.IsNullOrWhiteSpace(_response.AccountReference) ? "-" : _response.AccountReference;

        var hasAccount = !string.IsNullOrWhiteSpace(_response.AccountNumber) || !string.IsNullOrWhiteSpace(_response.BankName);
        BankInfoFrame.IsVisible = hasAccount;
        if (hasAccount)
        {
            AccountNameLabel.Text = name;
            BankNameLabel.Text = string.IsNullOrWhiteSpace(_response.BankName) ? "N/A" : _response.BankName;
            AccountNumberLabel.Text = string.IsNullOrWhiteSpace(_response.AccountNumber) ? "N/A" : _response.AccountNumber;
        }

        TimestampLabel.Text = $"Verified on {DateTime.Now:MMM dd, yyyy • hh:mm tt}";
    }

    private async void StartAnimations()
    {
        await Task.Delay(100);

        // Animate checkmark icon
        await CheckmarkIcon.ScaleTo(0, 0);
        await CheckmarkIcon.ScaleTo(1.2, 400, Easing.SpringOut);
        await CheckmarkIcon.ScaleTo(1, 200, Easing.SpringIn);

        // Animate content with stagger effect
        await Task.Delay(200);
        await AnimateContent();
    }

    private async Task AnimateContent()
    {
        var elements = new List<View>
        {
            TitleLabel,
            UsernameLabel,
            MessageLabel,
            DetailsFrame,
            BankInfoFrame,
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

    private async void OnCloseButtonClicked(object sender, EventArgs e)
    {
        await AnimateClose();
    }

    /// <summary>Closes the sheet with its fade-out; used by the caller to move on to the dashboard automatically.</summary>
    public Task DismissAsync() => MainThread.InvokeOnMainThreadAsync(AnimateClose);

    private async Task AnimateClose()
    {
        await ContentFrame.ScaleTo(0.95, 150);
        await ContentFrame.FadeTo(0, 150);
        Close();
    }
}