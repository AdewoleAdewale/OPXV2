namespace Opx.Views;

public partial class Congratulations : ContentPage
{
    public Congratulations()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Mark that this page has been shown
        Preferences.Set("temp_page_shown", true);

        // Wait for 3 seconds
        await Task.Delay(3000);

        // Navigate to the main page
        Application.Current.MainPage = new DashBoard();
    }
}