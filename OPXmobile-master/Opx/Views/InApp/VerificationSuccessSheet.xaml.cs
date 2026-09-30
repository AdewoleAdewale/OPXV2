using CommunityToolkit.Maui.Views;
using static Opx.Views.VerifyToken;

namespace Opx.Views
{
    public partial class VerificationSuccessSheet : Popup
    {
        private bool _isAnimating = false;
        private VerifyTokenResponse _response;

        public VerificationSuccessSheet(VerifyTokenResponse response)
        {
            InitializeComponent();
            _response = response;
            InitializeSheet();
        }

        private async void InitializeSheet()
        {
            try
            {
                PopulateData();
                await AnimatePopupIn();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing success sheet: {ex.Message}");
            }
        }

        private void PopulateData()
        {
            try
            {
                ContractIdLabel.Text = _response.contractId?.ToString() ?? "N/A";

                if (_response.amount.HasValue && _response.amount.Value > 0)
                {
                    AmountLabel.Text = $"₦{_response.amount.Value:N2}";
                    AmountGrid.IsVisible = true;
                }

                if (!string.IsNullOrEmpty(_response.status))
                {
                    StatusLabel.Text = _response.status;
                    UpdateStatusColor(_response.status);
                }

                if (!string.IsNullOrEmpty(_response.confirmedAt))
                {
                    ConfirmedAtLabel.Text = FormatDateTime(_response.confirmedAt);
                    ConfirmedAtGrid.IsVisible = true;
                }

                if (!string.IsNullOrEmpty(_response.sellerName))
                {
                    SellerNameLabel.Text = _response.sellerName;
                    SellerGrid.IsVisible = true;
                }

                if (!string.IsNullOrEmpty(_response.buyerName))
                {
                    BuyerNameLabel.Text = _response.buyerName;
                    BuyerGrid.IsVisible = true;
                }

                if (!string.IsNullOrEmpty(_response.message))
                {
                    MessageLabel.Text = _response.message;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error populating data: {ex.Message}");
            }
        }

        private void UpdateStatusColor(string status)
        {
            try
            {
                var normalizedStatus = status.ToLower();

                if (normalizedStatus.Contains("confirmed") || normalizedStatus.Contains("success"))
                {
                    StatusFrame.BackgroundColor = Color.FromArgb("#00B894");
                }
                else if (normalizedStatus.Contains("pending"))
                {
                    StatusFrame.BackgroundColor = Color.FromArgb("#F59E0B");
                }
                else if (normalizedStatus.Contains("failed") || normalizedStatus.Contains("rejected"))
                {
                    StatusFrame.BackgroundColor = Color.FromArgb("#EF4444");
                }
                else
                {
                    StatusFrame.BackgroundColor = Color.FromArgb("#3B82F6");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating status color: {ex.Message}");
            }
        }

        private string FormatDateTime(string dateTimeString)
        {
            try
            {
                if (DateTime.TryParse(dateTimeString, out DateTime dateTime))
                {
                    return dateTime.ToString("MMM dd, yyyy hh:mm tt");
                }
                return dateTimeString;
            }
            catch
            {
                return dateTimeString;
            }
        }

        private async Task AnimatePopupIn()
        {
            try
            {
                if (_isAnimating) return;
                _isAnimating = true;

                // Smooth popup entrance with spring effect
                var scaleTask = SheetFrame.ScaleTo(1, 400, Easing.SpringOut);
                var translateTask = SheetFrame.TranslateTo(0, 0, 400, Easing.SpringOut);
                var fadeTask = SheetFrame.FadeTo(1, 300, Easing.CubicOut);

                await Task.WhenAll(scaleTask, translateTask, fadeTask);

                // Animate success circles with ripple effect
                await Task.WhenAll(
                    CircleBackground.ScaleTo(1.1, 300, Easing.CubicOut),
                    CircleMiddle.ScaleTo(1.1, 250, Easing.CubicOut),
                    CircleInner.ScaleTo(1.1, 200, Easing.CubicOut)
                );

                await Task.WhenAll(
                    CircleBackground.ScaleTo(1, 200, Easing.CubicIn),
                    CircleMiddle.ScaleTo(1, 180, Easing.CubicIn),
                    CircleInner.ScaleTo(1, 150, Easing.CubicIn)
                );

                // Animate checkmark with bounce
                await SuccessIcon.FadeTo(1, 200, Easing.CubicOut);
                await SuccessIcon.ScaleTo(1.3, 200, Easing.CubicOut);
                await SuccessIcon.ScaleTo(1, 200, Easing.BounceOut);

                _isAnimating = false;
            }
            catch (Exception ex)
            {
                _isAnimating = false;
                System.Diagnostics.Debug.WriteLine($"Error animating popup in: {ex.Message}");
            }
        }

        private async Task AnimatePopupOut()
        {
            try
            {
                if (_isAnimating) return;
                _isAnimating = true;

                var scaleTask = SheetFrame.ScaleTo(0.85, 200, Easing.CubicIn);
                var fadeTask = SheetFrame.FadeTo(0, 200, Easing.CubicIn);

                await Task.WhenAll(scaleTask, fadeTask);

                _isAnimating = false;
            }
            catch (Exception ex)
            {
                _isAnimating = false;
                System.Diagnostics.Debug.WriteLine($"Error animating popup out: {ex.Message}");
            }
        }

        private async void OnDoneClicked(object sender, EventArgs e)
        {
            await CloseSheet();
        }

        private async void OnViewDetailsClicked(object sender, EventArgs e)
        {
            try
            {
                string details = $"Contract ID: {_response.contractId}\n" +
                               $"Status: {_response.status}\n" +
                               $"Amount: ₦{_response.amount:N2}\n" +
                               $"Processing fee: ₦{_response.processingFee ?? 0:N2}\n" +
                               $"Reference: {_response.reference}\n" +
                               $"Seller: {_response.sellerName}\n" +
                               $"Buyer: {_response.buyerName}\n" +
                               $"Confirmed: {_response.confirmedAt}";

                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Full Details", details, "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing details: {ex.Message}");
            }
        }

        private async Task CloseSheet()
        {
            try
            {
                if (_isAnimating) return;

                await AnimatePopupOut();
                await this.CloseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing sheet: {ex.Message}");
            }
        }
    }
}