using Opx.Services;
using Opx.Views;
namespace Opx
{
    public partial class App : Application
    {
        public static bool IsUserLoggedIn { get; set; }
        private Timer _inactivityTimer;
        private readonly TimeSpan _inactivityTimeout = TimeSpan.FromMinutes(5);
        private DateTime _lastActivityTime;
        private bool _isAppSleeping = false;
        private bool _sessionExpired = false;

        private BvnToken createContract;
        public App()
        {
            InitializeComponent();

            // Check if this is the first time the app is launched
            bool isFirstLaunch = !Preferences.Get("has_launched_before", false);

            if (isFirstLaunch)
            {
                // First time launch - show TempPage
                MainPage = new SecureSystemPage();
                // Set the flag to indicate the app has been launched before
                Preferences.Set("has_launched_before", true);
            }
            else
            {
                // Subsequent launches - show LoginPage
                MainPage = new LoginPage();
            }

            // Initialize activity tracking
            _lastActivityTime = DateTime.Now;
        }

        public void ResetInactivityTimer() { }

        public void NavigateToLogin() => MainPage = new LoginPage();

        public void OnUserLoggedIn() => IsUserLoggedIn = true;

        public void OnUserLoggedOut()
        {
            IsUserLoggedIn = false;
            MainPage = new LoginPage();
        }

        protected override void OnSleep()
        {
            // Minimised / sent to background: keep the latest details and cookie so the next launch signs in automatically.
            _ = SessionStore.SaveAsync();
        }

        protected override void OnResume()
        {
            // Back in the foreground: quietly refresh the card and recent transactions. No logout, no login screen.
            if (!string.IsNullOrWhiteSpace(LoginPage.myemail))
                _ = Home.NotifyReturnedAsync();
        }
    }

    public class SessionSplashPage : ContentPage
    {
        public SessionSplashPage()
        {
            BackgroundColor = Color.FromArgb("#F4F5FB");
            Content = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 14,
                Children =
                {
                    new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#6B4CE6"), HeightRequest = 40, WidthRequest = 40 },
                    new Label { Text = "Signing you in...", TextColor = Color.FromArgb("#7A8099"), FontSize = 14, HorizontalOptions = LayoutOptions.Center }
                }
            };
            Loaded += async (_, _) => await RestoreAsync();
        }

        private async Task RestoreAsync()
        {
            Page next;
            try
            {
                var restore = SessionStore.TryRestoreAsync();
                // On a slow network don't hold the user on the splash: the saved details are already loaded in memory.
                var finished = await Task.WhenAny(restore, Task.Delay(800));
                var result = finished == restore ? await restore : SessionRestoreResult.Restored;

                next = result == SessionRestoreResult.Restored ? new Home() : new LoginPage();
                if (IsUserLoggedInResult(result)) App.IsUserLoggedIn = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Splash restore error: {ex.Message}");
                next = new LoginPage();
            }

            await MainThread.InvokeOnMainThreadAsync(() => Application.Current!.MainPage = next);
        }

        private static bool IsUserLoggedInResult(SessionRestoreResult r) => r == SessionRestoreResult.Restored;
    }
}