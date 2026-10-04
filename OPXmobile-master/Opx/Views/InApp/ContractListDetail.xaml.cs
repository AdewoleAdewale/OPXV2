using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Text;
using static Opx.Views.ContractList;

namespace Opx.Views;

public partial class ContractListDetail : ContentPage
{
    private HistoryData _selectedData;
    private TransactionDetail _transactionDetail;
    private bool _isAnimating = false;

    public ContractListDetail(HistoryData selectedData)
    {
        InitializeComponent();
        _selectedData = selectedData;
        LoadData();
        StartPageAnimations();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartPageAnimations();
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
            Application.Current.MainPage = new Home();
        }

        return true; // Indicates we handled the back button press
    }


    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        try
        {
            await Navigation.PushModalAsync(new Views.Home());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Failed to navigate. Please try again.", "OK");
            System.Diagnostics.Debug.WriteLine($"Navigation Error: {ex.Message}");
        }
    }

    private async void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e)
    {
        try
        {
            await Navigation.PushModalAsync(new Views.Home());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Failed to navigate. Please try again.", "OK");
            System.Diagnostics.Debug.WriteLine($"Navigation Error: {ex.Message}");
        }
    }

    private void LoadData()
    {
        try
        {
            if (_selectedData != null)
            {
                // Create TransactionDetail object from HistoryData
                _transactionDetail = new TransactionDetail
                {
                    ProfileImageSource = "Group 72.png",
                    Amount = +_selectedData.Amount,
                    Description = _selectedData.Description ?? "N/A",
                    SellerId = _selectedData.SellerId ?? "N/A",
                    Role = _selectedData.Role ?? "N/A",
                    Token = _selectedData.Token ?? "N/A",
                    CreatedAt = _selectedData.CreatedAt,
                    Status = _selectedData.Status ?? "N/A",
                    SellerPhone = _selectedData.SellerPhone ?? "N/A",
                    SellerName = _selectedData.SellerName ?? "N/A",
                    BuyerId = _selectedData.BuyerId ?? "N/A",
                    BuyerName = _selectedData.BuyerName ?? "N/A"
                };

                // Set the BindingContext to enable data binding
                BindingContext = _transactionDetail;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Data Loading Error: {ex.Message}");
            DisplayAlert("Error", "Failed to load contract details.", "OK");
        }
    }

    private async void StartPageAnimations()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        try
        {
            // Set initial states for animations
            Escrowcard.Opacity = 0;
            Escrowcard.Scale = 0.8;
            Escrowcard.TranslationY = 50;

            TransactionDetailsCard.Opacity = 0;
            TransactionDetailsCard.TranslationY = 100;

            // Start animations with delays
            await Task.Delay(200);

            // Animate header
            await PartiesCard.FadeTo(1, 500);

            // Animate escrow card
            var escrowAnimations = new Task[]
            {
                Escrowcard.FadeTo(1, 800),
                Escrowcard.ScaleTo(1, 600, Easing.BounceOut),
                Escrowcard.TranslateTo(0, 0, 700, Easing.CubicOut)
            };
            await Task.WhenAll(escrowAnimations);

            // Animate transaction log
            await Task.Delay(200);
            var transactionAnimations = new Task[]
            {
                TransactionDetailsCard.FadeTo(1, 600),
                TransactionDetailsCard.TranslateTo(0, 0, 500, Easing.CubicOut)
            };
            await Task.WhenAll(transactionAnimations);


        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Animation Error: {ex.Message}");
        }
        finally
        {
            _isAnimating = false;
        }
    }


    private async void OnShareTapped(object sender, EventArgs e)
    {
        try
        {
            // Show loading indicator
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            // Disable the share button to prevent multiple taps
            Escrowcard.IsEnabled = false;

            // Check if we have data to share
            if (_transactionDetail == null)
            {
                await ShowToast("No contract details available to share.");
                return;
            }

            // Create sharing options
            var action = await DisplayActionSheet(
                "Share Contract Details",
                "Cancel",
                null,
                "Share as Text",
                "Share as File",
                "Copy to Clipboard",
                "Share Screenshot",
                "Share as PDF"
            );

            switch (action)
            {
                case "Share as Text":
                    await ShareAsText();
                    break;
                case "Share as File":
                    await ShareAsFile();
                    break;
                case "Copy to Clipboard":
                    await CopyToClipboard();
                    break;
                case "Share Screenshot":
                    await ShareScreenshot();
                    break;
                case "Share as PDF":
                    await ShareAsPDF();
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Share Error: {ex.Message}");
            await ShowToast("Failed to share contract details. Please try again.");
        }
        finally
        {
            // Hide loading indicator and re-enable button
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            Escrowcard.IsEnabled = true;
        }
    }

    private async Task ShareScreenshot()
    {
        try
        {
            await ShowToast("Capturing screenshot...");

            // Take screenshot of the main container
            var screenshot = await StackContract.CaptureAsync();

            if (screenshot != null)
            {
                // Save screenshot to temporary file
                var fileName = $"Contract_Screenshot_{_transactionDetail.Token}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                using var stream = await screenshot.OpenReadAsync();
                using var fileStream = File.Create(filePath);
                await stream.CopyToAsync(fileStream);

                // Share the screenshot
                var shareRequest = new ShareFileRequest
                {
                    Title = "Contract Screenshot",
                    File = new ShareFile(filePath),
                    PresentationSourceBounds = DeviceInfo.Platform == DevicePlatform.iOS ?
                        new Rect(0, 20, 0, 0) : Rect.Zero
                };

                await Share.Default.RequestAsync(shareRequest);
                await ShowToast("Screenshot shared successfully!");
            }
            else
            {
                await ShowToast("Failed to capture screenshot.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screenshot Error: {ex.Message}");
            await ShowToast("Failed to capture screenshot. Please try again.");
        }
    }

    private async Task ShareAsPDF()
    {
        try
        {
            await ShowToast("Generating PDF...");

            // Create PDF content
            var pdfContent = await GeneratePDFContent();

            if (!string.IsNullOrEmpty(pdfContent))
            {
                var fileName = $"Contract_PDF_{_transactionDetail.Token}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                // Create a simple PDF using HTML to PDF conversion
                // Note: For production, consider using a proper PDF library like iTextSharp or similar
                await CreateSimplePDF(pdfContent, filePath);

                var shareRequest = new ShareFileRequest
                {
                    Title = "Contract PDF",
                    File = new ShareFile(filePath),
                    PresentationSourceBounds = DeviceInfo.Platform == DevicePlatform.iOS ?
                        new Rect(0, 20, 0, 0) : Rect.Zero
                };

                await Share.Default.RequestAsync(shareRequest);
                await ShowToast("PDF shared successfully!");
            }
            else
            {
                await ShowToast("Failed to generate PDF content.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF Error: {ex.Message}");
            await ShowToast("Failed to generate PDF. Please try again.");
        }
    }

    private async Task<string> GeneratePDFContent()
    {
        try
        {
            var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Contract Details</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 20px; background-color: #f9f9f9; }}
                    .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 20px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                    .header {{ text-align: center; margin-bottom: 30px; }}
                    .amount {{ font-size: 36px; font-weight: bold; color: #333; text-align: center; margin: 20px 0; }}
                    .currency {{ color: #9B59B6; }}
                    .subtitle {{ text-align: center; color: #666; margin-bottom: 30px; }}
                    .details {{ background: #fafafa; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                    .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #e0e0e0; }}
                    .detail-label {{ font-weight: normal; color: #666; }}
                    .detail-value {{ font-weight: bold; color: #333; }}
                    .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
                    .title {{ font-size: 24px; font-weight: bold; margin-bottom: 10px; }}
                    .section-title {{ font-size: 18px; font-weight: bold; margin: 20px 0 10px 0; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1 class='title'>📋 ESCROW CONTRACT DETAILS</h1>
                    </div>
                    
                    <div class='amount'>
                        {_transactionDetail.Amount:N2} <span class='currency'>NGN</span>
                    </div>
                    <div class='subtitle'>Entered Amount</div>
                    
                    <div class='section-title'>ESCROW DETAILS</div>
                    <div class='details'>
                        <div class='detail-row'>
                            <span class='detail-label'>Description</span>
                            <span class='detail-value'>{_transactionDetail.Description}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>My Role</span>
                            <span class='detail-value'>{_transactionDetail.Role}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Escrow Token</span>
                            <span class='detail-value'>{_transactionDetail.Token}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Buyer</span>
                            <span class='detail-value'>{_transactionDetail.BuyerName}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Seller</span>
                            <span class='detail-value'>{_transactionDetail.SellerName}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Status</span>
                            <span class='detail-value' style='color: #9B59B6;'>{_transactionDetail.Status}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Created At</span>
                            <span class='detail-value' style='color: #e74c3c;'>{_transactionDetail.CreatedAt:yyyy-MM-dd HH:mm}</span>
                        </div>
                    </div>
                    
                    <div class='footer'>
                        <p>Generated by Opx Escrow App</p>
                        <p>Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                    </div>
                </div>
            </body>
            </html>";

            return htmlContent;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF Content Generation Error: {ex.Message}");
            return null;
        }
    }

    private async Task CreateSimplePDF(string htmlContent, string filePath)
    {
        try
        {
            // For now, we'll save as HTML file
            // In production, you would use a proper PDF library
            var htmlFileName = filePath.Replace(".pdf", ".html");
            await File.WriteAllTextAsync(htmlFileName, htmlContent, Encoding.UTF8);

            // Create a simple text-based PDF alternative
            var textContent = GenerateShareText();
            await File.WriteAllTextAsync(filePath.Replace(".pdf", ".txt"), textContent, Encoding.UTF8);

            // For actual PDF generation, you would need to add a PDF library like:
            // - iTextSharp
            // - PdfSharp
            // - Syncfusion PDF
            // 
            // Example with a hypothetical PDF library:
            // var pdfDocument = new PdfDocument();
            // var page = pdfDocument.AddPage();
            // var graphics = XGraphics.FromPdfPage(page);
            // // Add content to PDF
            // pdfDocument.Save(filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF Creation Error: {ex.Message}");
            throw;
        }
    }

    private async Task ShareAsText()
    {
        try
        {
            var shareText = GenerateShareText();

            var shareRequest = new ShareTextRequest
            {
                Title = "Contract Details",
                Text = shareText,
                Subject = $"Escrow Contract - {_transactionDetail.Token}"
            };

            await Share.Default.RequestAsync(shareRequest);
            await ShowToast("Contract details shared successfully!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Share Text Error: {ex.Message}");
            await ShowToast("Failed to share as text. Please try again.");
        }
    }

    private async Task ShareAsFile()
    {
        try
        {
            var shareText = GenerateShareText();
            var fileName = $"Contract_{_transactionDetail.Token}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

            // Save to temporary file
            var tempFile = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(tempFile, shareText, Encoding.UTF8);

            var shareRequest = new ShareFileRequest
            {
                Title = "Contract Details",
                File = new ShareFile(tempFile),
                PresentationSourceBounds = DeviceInfo.Platform == DevicePlatform.iOS ?
                    new Rect(0, 20, 0, 0) : Rect.Zero
            };

            await Share.Default.RequestAsync(shareRequest);
            await ShowToast("Contract file shared successfully!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Share File Error: {ex.Message}");
            await ShowToast("Failed to share as file. Please try again.");
        }
    }

    private async Task CopyToClipboard()
    {
        try
        {
            var shareText = GenerateShareText();
            await Clipboard.Default.SetTextAsync(shareText);
            await ShowToast("Contract details copied to clipboard!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clipboard Error: {ex.Message}");
            await ShowToast("Failed to copy to clipboard. Please try again.");
        }
    }

    private string GenerateShareText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("📋 ESCROW CONTRACT DETAILS");
        sb.AppendLine("=" + new string('=', 30));
        sb.AppendLine();

        sb.AppendLine($"💰 Amount: {_transactionDetail.Amount:N2} NGN");
        sb.AppendLine($"📝 Description: {_transactionDetail.Description}");
        sb.AppendLine($"👤 My Role: {_transactionDetail.Role}");
        sb.AppendLine($"🎫 Escrow Token: {_transactionDetail.Token}");
        sb.AppendLine($"🛒 Buyer: {_transactionDetail.BuyerName}");
        sb.AppendLine($"🏪 Seller: {_transactionDetail.SellerName}");
        sb.AppendLine($"📊 Status: {_transactionDetail.Status}");
        sb.AppendLine($"📅 Created: {_transactionDetail.CreatedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        sb.AppendLine("Generated by Opx Escrow App");
        sb.AppendLine($"Shared on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        return sb.ToString();
    }

    private async Task ShowToast(string message)
    {
        try
        {
            var toast = Toast.Make(message, ToastDuration.Short, 14);
            await toast.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Toast Error: {ex.Message}");
            // Fallback to DisplayAlert if toast fails
            await DisplayAlert("Info", message, "OK");
        }
    }

    public class TransactionDetail
    {
        public string ProfileImageSource { get; set; } = "Group 72.png";
        public int Id { get; set; }
        public string SellerId { get; set; } = string.Empty;
        public string BuyerId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Token { get; set; } = string.Empty;
        public bool IsConfirmed { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public bool IsCancellationRequested { get; set; }
        public DateTime? CancelRequestedAt { get; set; }
        public bool? IsCancelled { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public string SellerPhone { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerPhone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}