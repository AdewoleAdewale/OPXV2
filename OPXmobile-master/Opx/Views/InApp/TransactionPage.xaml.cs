using AiForms.Dialogs;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Newtonsoft.Json;
using System.Net;

namespace Opx.Views;

public partial class TransactionPage : ContentPage
{
    public class HistoryData
    {
        public int Id { get; set; }
        public required string SellerId { get; set; }
        public required string BuyerId { get; set; }
        public required decimal Amount { get; set; }
        public required string Token { get; set; }
        public required bool IsConfirmed { get; set; }
        public required DateTime? ConfirmedAt { get; set; }
        public required bool IsCancellationRequested { get; set; }
        public required DateTime? CancelRequestedAt { get; set; }
        public required bool? IsCancelled { get; set; }
        public required DateTime? CancelledAt { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required string Description { get; set; }
        public required string Status { get; set; }
        public required string SellerName { get; set; }
        public required string SellerPhone { get; set; }
        public required string BuyerName { get; set; }
        public required string BuyerPhone { get; set; }
        public required string Role { get; set; }




    }


    class HistoryDataHeaderFooter
    {
        public required List<HistoryData> HD { get; set; }
        public string Intro { get { return " You have Performed a total of " + HD.Count + " transactions within your search dates"; } }
        public string Summary { get { return " You have Performed a total of " + HD.Count + " transactions"; } }
        public decimal Size { get { return HD.Count; } }

    }

    public TransactionPage()
    {
        InitializeComponent();

    }


    private async void TapGestureRecognizer_Tapped5(object sender, TappedEventArgs e)
    {

        await Navigation.PushModalAsync(new Views.Home());
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


    async void TapGestureRecognizer_Tapped()
    {
        try
        {
            Configurations.LoadingConfig = new LoadingConfig
            {
                Opacity = 0.4,
                DefaultMessage = "Connecting OPX Please Wait...",
                FontSize = 12,
            };

            await Loading.Instance.StartAsync(async progress =>
            {
                try
                {
                    // Simulate loading progress
                    for (var i = 0; i < 100; i++)
                    {
                        await Task.Delay(50);
                        progress.Report((i + 1) * 0.01d);
                    }

                    string SearchStringFrom = DateTime.Today.AddDays(-3).ToString("MM/dd/yyyy");
                    string SearchStringTo = DateTime.Today.AddDays(+1).ToString("MM/dd/yyyy");

                    string url = "https://opxng.com/api/contractsapi/?email=" + LoginPage.myemail;

                    // 🧰 Fix SSL validation issues
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };

                    using (HttpClient client = new HttpClient(handler))
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                        HttpResponseMessage response = await client.GetAsync(url);

                        if (!response.IsSuccessStatusCode)
                        {
                            await ShowSnackbarAsync($"Server returned: {response.StatusCode}");
                            return;
                        }

                        string json = await response.Content.ReadAsStringAsync();

                        if (string.IsNullOrWhiteSpace(json))
                        {
                            await ShowSnackbarAsync("Empty server response. Please try again.");
                            return;
                        }

                        List<HistoryData> items = JsonConvert.DeserializeObject<List<HistoryData>>(json);

                        var SortedGazette = items
                            .OrderByDescending(x => x.CreatedAt)
                            .Take(15)
                            .ToList();

                        listView.ItemsSource = SortedGazette;
                        AnimateListItems();
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Network error: {httpEx.Message}");
                    await ShowSnackbarAsync($"Network error: {httpEx.Message}");
                }
                catch (JsonException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine($"JSON parsing error: {jsonEx.Message}");
                    await ShowSnackbarAsync("Error reading server data.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ContractHistory loading error: {ex.Message}");
                    await ShowSnackbarAsync($"An error occurred: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            await ShowSnackbarAsync($"Failed to Get ContractHistory: {ex.Message}");
        }
    }

    // ✅ Reusable Snackbar helper to avoid repeating code
    private async Task ShowSnackbarAsync(string message)
    {
        var snackbar = Snackbar.Make(
            message ?? "An error occurred",
            null,
            "OKAY",
            TimeSpan.FromSeconds(5),
            new SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#A25AC4"),
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.White,
                CornerRadius = new CornerRadius(8),
                Font = Microsoft.Maui.Font.SystemFontOfSize(14)
            });
        await snackbar.Show();
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();

        TapGestureRecognizer_Tapped();

    }

    private async void AnimateListItems()
    {
        try
        {
            if (listView.ItemsSource is System.Collections.IEnumerable items)
            {
                var itemList = items.Cast<object>().ToList();

                for (int i = 0; i < itemList.Count && i < 10; i++) // Limit to prevent too many animations
                {
                    await Task.Delay(i * 150); // Stagger the animations

                    // Alternative approach: Animate the entire ListView with a subtle effect
                    await listView.FadeTo(0.7, 50);
                    await listView.FadeTo(1, 100);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
        }
    }

    // Even better approach - animate individual items as they appear
    private async void AnimateItemOnAppearing(object sender, EventArgs e)
    {
        if (sender is Frame frame)
        {
            frame.Opacity = 0;
            frame.TranslationX = 50;

            await Task.WhenAll(
                frame.FadeTo(1, 400, Easing.CubicOut),
                frame.TranslateTo(0, 0, 400, Easing.CubicOut)
            );
        }
    }



}