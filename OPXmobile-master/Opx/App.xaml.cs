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
            StartInactivityTimer();
        }

        private void StartInactivityTimer()
        {
            // Check every 30 seconds, but use a more reasonable interval
            _inactivityTimer = new Timer(CheckInactivity, null, TimeSpan.FromSeconds(1000), TimeSpan.FromSeconds(1000));
        }

        private void CheckInactivity(object state)
        {
            if (_isAppSleeping || !IsUserLoggedIn) return;

            var timeSinceLastActivity = DateTime.Now - _lastActivityTime;
            if (timeSinceLastActivity >= _inactivityTimeout && !_sessionExpired)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    PutAppToSleep();
                });
            }
        }

        private void PutAppToSleep()
        {
            if (_isAppSleeping || _sessionExpired) return;

            _isAppSleeping = true;
            _sessionExpired = true;
            IsUserLoggedIn = false;

            // Stop the timer to prevent multiple triggers
            _inactivityTimer?.Dispose();

            // Navigate back to login page
            MainPage = new LoginPage();

            // Show session expired message
            Device.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Session Expired",
                        "Your session has expired due to inactivity. Please log in again.",
                        "OK");
                }
                catch (Exception ex)
                {
                    // Log the error but don't crash the app
                    System.Diagnostics.Debug.WriteLine($"Error showing session expired alert: {ex.Message}");
                }
            });
        }

        public void ResetInactivityTimer()
        {
            _lastActivityTime = DateTime.Now;
            if (_isAppSleeping)
            {
                _isAppSleeping = false;
            }
        }

        // Call this method to navigate from TempPage to LoginPage
        public void NavigateToLogin()
        {
            MainPage = new LoginPage();
        }

        // Call this method when user successfully logs in
        public void OnUserLoggedIn()
        {
            IsUserLoggedIn = true;
            _sessionExpired = false;
            _isAppSleeping = false;
            ResetInactivityTimer();

            // Restart timer if it was disposed
            if (_inactivityTimer == null)
            {
                StartInactivityTimer();
            }
        }

        // Call this method when user logs out
        public void OnUserLoggedOut()
        {
            IsUserLoggedIn = false;
            _sessionExpired = false;
            _isAppSleeping = false;
            _inactivityTimer?.Dispose();
            _inactivityTimer = null;
            MainPage = new LoginPage();
        }

        protected override void OnStart()
        {
            ResetInactivityTimer();
        }

        protected override void OnSleep()
        {
            // App is going to background
            _inactivityTimer?.Dispose();
            _inactivityTimer = null;
        }



        protected override void OnResume()
        {
            // App is coming back to foreground
            if (_sessionExpired)
            {
                // Only force login if session actually expired due to inactivity
                MainPage = new LoginPage();
                _sessionExpired = false;
            }
            else if (IsUserLoggedIn)
            {
                // User is still logged in, just restart the inactivity timer
                ResetInactivityTimer();
                StartInactivityTimer();
            }
            // If user is not logged in but session hasn't expired, 
            // they're probably already on the login page, so do nothing
        }

        // Clean up resources when app is terminating
        ~App()
        {
            _inactivityTimer?.Dispose();
        }
    }
}