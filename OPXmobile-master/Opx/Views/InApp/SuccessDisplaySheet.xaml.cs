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
        var username = _response.AccountReference ?? _response.Agency ?? "User";

        UsernameLabel.Text = username;
        MessageLabel.Text = _response.Message ?? "Your BVN has been verified successfully!";

        if (!string.IsNullOrEmpty(_response.BankName))
        {
            BankInfoFrame.IsVisible = true;
            BankNameLabel.Text = _response.BankName;
            AccountNumberLabel.Text = _response.AccountNumber ?? "N/A";
        }
        else
        {
            BankInfoFrame.IsVisible = false;
        }

        if (!string.IsNullOrEmpty(_response.Token))
        {
            TokenFrame.IsVisible = true;
            TokenLabel.Text = $"Token: {_response.Token.Substring(0, Math.Min(20, _response.Token.Length))}...";
        }
        else
        {
            TokenFrame.IsVisible = false;
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
            TokenFrame,
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

    private async Task AnimateClose()
    {
        await ContentFrame.ScaleTo(0.95, 150);
        await ContentFrame.FadeTo(0, 150);
        Close();
    }
}