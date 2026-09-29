using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using Opx.Services;
using System.Text;

namespace Opx.Views;

public partial class LoginPage : ContentPage
{
    private ForgetPassword _forgetPasswordPopup;
    private bool _isPasswordVisible = false;
    private const string CREDENTIALS_KEY = "saved_credentials";
    private readonly IBiometricAuthenticationService? _biometricService;

    public static string? myemail { get; set; }
    public static string? myfullname { get; set; }
    public static string? mytoken { get; set; }
    public static string? completedTransactions { get; set; }
    public static string? totalTransactions { get; set; }
    public static string? ledgerBalance { get; set; }
    public static string? availableBalance { get; set; }
    public static string? accountName { get; set; }
    public static string? accountNumber { get; set; }
    public static string? bankName { get; set; }

    public LoginPage()
    {
        try
        {
            InitializeComponent();
            _forgetPasswordPopup = new ForgetPassword();

            // Get biometric service from dependency injection
            _biometricService = Handler?.MauiContext?.Services.GetService<IBiometricAuthenticationService>();

            // Start entrance animations after the page loads
            Loaded += OnPageLoaded;

            // Add focus/unfocus animations to entry fields
            Email.Focused += OnEntryFocused;
            Email.Unfocused += OnEntryUnfocused;
            password.Focused += OnEntryFocused;
            password.Unfocused += OnEntryUnfocused;

            // Load saved credentials and check biometric availability
            LoadSavedCredentials();
            CheckBiometricAvailability();
        }
        catch (Exception ex)
        {
            DisplayAlert("Initialization Error", $"Failed to initialize login page: {ex.Message}", "OK");
        }
    }

    private async void CheckBiometricAvailability()
    {
        try
        {
            // Check if we have saved credentials
            bool hasCredentials = await HasSavedCredentialsAsync();

            // Show biometric buttons only if credentials exist
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                BiometricButtonsContainer.IsVisible = hasCredentials;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Biometric check error: {ex.Message}");
        }
    }

    private async void LoadSavedCredentials()
    {
        try
        {
            var savedCreds = await GetSavedCredentialsAsync();
            if (savedCreds != null && !string.IsNullOrEmpty(savedCreds.Email))
            {
                Email.Text = savedCreds.Email;
                // Don't populate password for security
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load credentials error: {ex.Message}");
        }
    }

    private async Task<bool> HasSavedCredentialsAsync()
    {
        try
        {
            var creds = await GetSavedCredentialsAsync();
            return creds != null && !string.IsNullOrEmpty(creds.Email) && !string.IsNullOrEmpty(creds.Password);
        }
        catch
        {
            return false;
        }
    }

    private async Task<SavedCredentials?> GetSavedCredentialsAsync()
    {
        try
        {
            var credentialsJson = await SecureStorage.GetAsync(CREDENTIALS_KEY);
            if (!string.IsNullOrEmpty(credentialsJson))
            {
                return JsonConvert.DeserializeObject<SavedCredentials>(credentialsJson);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get credentials error: {ex.Message}");
        }
        return null;
    }

    private async Task SaveCredentialsAsync(string email, string password)
    {
        try
        {
            var credentials = new SavedCredentials
            {
                Email = email,
                Password = password
            };

            var credentialsJson = JsonConvert.SerializeObject(credentials);
            await SecureStorage.SetAsync(CREDENTIALS_KEY, credentialsJson);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save credentials error: {ex.Message}");
        }
    }

    private async Task<bool> AuthenticateWithBiometricsAsync()
    {
        try
        {
            // Check if biometric authentication is available
            var authResult = await SecureStorage.GetAsync("biometric_check");

            // For actual implementation, you would use platform-specific biometric APIs
            // For now, we'll simulate the authentication
            bool isAuthenticated = await DisplayAlert(
                "Biometric Authentication",
                "Use your fingerprint or face to login",
                "Authenticate",
                "Cancel"
            );

            return isAuthenticated;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Biometric auth error: {ex.Message}");
            return false;
        }
    }

    private async void BiometricLoginButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Determine which button was clicked (Face ID or Fingerprint)
            var button = sender as Button;
            string authType = button?.Text.Contains("👤") == true ? "Face ID" : "Fingerprint";

            await AnimateButtonPress(button);

            // Check if credentials are saved
            var savedCreds = await GetSavedCredentialsAsync();
            if (savedCreds == null || string.IsNullOrEmpty(savedCreds.Email) || string.IsNullOrEmpty(savedCreds.Password))
            {
                await ShowErrorSnackbar("No saved credentials found. Please login with email and password first.");
                return;
            }

            await ShowLoadingOverlay($"Authenticating with {authType}...");

            // Authenticate with biometrics
            bool isAuthenticated = await AuthenticateWithBiometricsAsync();

            if (isAuthenticated)
            {
                // Use saved credentials to login
                await DoSomeDataAccessAsync(savedCreds.Email, savedCreds.Password, true);
            }
            else
            {
                await HideLoadingOverlay();
                await ShowErrorSnackbar($"{authType} authentication failed or cancelled");
            }
        }
        catch (Exception ex)
        {
            await HideLoadingOverlay();
            await ShowErrorSnackbar($"Biometric login error: {ex.Message}");
        }
    }

    private async void EnableBiometricSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        // This method has been removed - no longer using toggle switch
    }

    private void FaceIdButton_Clicked(object sender, EventArgs e)
    {
        BiometricLoginButton_Clicked(sender, e);
    }

    private void FingerprintButton_Clicked(object sender, EventArgs e)
    {
        BiometricLoginButton_Clicked(sender, e);
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        try
        {
            // Animate floating background elements
            _ = Task.Run(async () =>
            {
                await AnimateFloatingElements();
            });

            // Staggered entrance animations
            await Task.Delay(200);
            await AnimateLogoSection();

            await Task.Delay(300);
            await AnimateLoginForm();

            await Task.Delay(200);
            await AnimateRegisterSection();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
        }
    }

    private async Task AnimateFloatingElements()
    {
        try
        {
            while (true)
            {
                await FloatingElements.RotateTo(360, 30000, Easing.Linear);
                FloatingElements.Rotation = 0;
            }
        }
        catch
        {
            // Silent fail for background animation
        }
    }

    private async Task AnimateLogoSection()
    {
        try
        {
            LogoSection.TranslationY = -50;
            await Task.WhenAll(
                LogoSection.FadeTo(1, 800, Easing.CubicOut),
                LogoSection.TranslateTo(0, 0, 800, Easing.CubicOut)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Logo animation error: {ex.Message}");
        }
    }

    private async Task AnimateLoginForm()
    {
        try
        {
            LoginForm.TranslationY = 30;
            await Task.WhenAll(
                LoginForm.FadeTo(1, 600, Easing.CubicOut),
                LoginForm.TranslateTo(0, 0, 600, Easing.CubicOut)
            );

            // Animate form fields sequentially
            await AnimateFormField(EmailFrame, 0);
            await AnimateFormField(PasswordFrame, 100);
            await AnimateFormField(LoginButton, 200);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Form animation error: {ex.Message}");
        }
    }

    private async Task AnimateFormField(View element, int delay)
    {
        try
        {
            element.Scale = 0.9;
            element.Opacity = 0.7;

            await Task.Delay(delay);
            await Task.WhenAll(
                element.ScaleTo(1, 300, Easing.CubicOut),
                element.FadeTo(1, 300, Easing.CubicOut)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Field animation error: {ex.Message}");
        }
    }

    private async Task AnimateRegisterSection()
    {
        try
        {
            RegisterSection.TranslationY = 20;
            await Task.WhenAll(
                RegisterSection.FadeTo(1, 500, Easing.CubicOut),
                RegisterSection.TranslateTo(0, 0, 500, Easing.CubicOut)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Register animation error: {ex.Message}");
        }
    }

    private async void OnEntryFocused(object? sender, FocusEventArgs e)
    {
        try
        {
            if (sender is View view)
            {
                var parent = view.Parent as Frame;
                if (parent != null)
                {
                    await Task.WhenAll(
                        parent.ScaleTo(1.05, 200, Easing.CubicOut),
                        parent.FadeTo(1, 200, Easing.CubicOut)
                    );
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Focus animation error: {ex.Message}");
        }
    }

    private async void OnEntryUnfocused(object? sender, FocusEventArgs e)
    {
        try
        {
            if (sender is View view)
            {
                var parent = view.Parent as Frame;
                if (parent != null)
                {
                    await Task.WhenAll(
                        parent.ScaleTo(1, 200, Easing.CubicOut),
                        parent.FadeTo(0.95, 200, Easing.CubicOut)
                    );
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unfocus animation error: {ex.Message}");
        }
    }

    private async void ShowPasswordButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            _isPasswordVisible = !_isPasswordVisible;
            password.IsPassword = !_isPasswordVisible;
            ShowPasswordButton.Text = _isPasswordVisible ? "🙈" : "👁️";

            await ShowPasswordButton.ScaleTo(0.8, 100, Easing.CubicOut);
            await ShowPasswordButton.ScaleTo(1, 100, Easing.CubicOut);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Show password error: {ex.Message}");
        }
    }

    private async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
    {
        try
        {
            if (sender is Label label)
            {
                await label.ScaleTo(0.95, 100, Easing.CubicOut);
                await label.ScaleTo(1, 100, Easing.CubicOut);
            }

            var forgotPasswordPopup = new ForgetPassword();
            var result = await this.ShowPopupAsync(forgotPasswordPopup);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to show forgot password popup: {ex.Message}", "OK");
        }
    }

    private async void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e)
    {
        try
        {
            await AnimateButtonPress(sender as View);
            await ShowLoadingOverlay("Connecting to OPX...");
            await Task.Delay(200);
            await HideLoadingOverlay();
            await Navigation.PushModalAsync(new MainPage());
        }
        catch (Exception ex)
        {
            await HideLoadingOverlay();
            await DisplayAlert("Navigation Error", $"Failed to navigate: {ex.Message}", "OK");
        }
    }

    private async void LoginButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            await AnimateButtonPress(LoginButton);

            if (string.IsNullOrWhiteSpace(Email?.Text))
            {
                await ShowValidationError("Please enter your email address");
                return;
            }

            if (string.IsNullOrWhiteSpace(password?.Text))
            {
                await ShowValidationError("Please enter your password");
                return;
            }

            if (!await CheckInternetConnectionAsync())
            {
                return;
            }

            await ShowLoadingOverlay("Logging in...");

            try
            {
                string MyEmail = Email.Text.Trim();
                string MyPassword = password.Text;
                bool rememberMeToggle = RememberMeSwitch?.IsToggled ?? false;

                // Save credentials securely for future biometric login
                await SaveCredentialsAsync(MyEmail, MyPassword);

                await DoSomeDataAccessAsync(MyEmail, MyPassword, rememberMeToggle);
            }
            finally
            {
                await HideLoadingOverlay();
            }
        }
        catch (Exception ex)
        {
            await HideLoadingOverlay();
            await ShowErrorSnackbar($"Failed to start login process: {ex.Message}");
        }
    }

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        try
        {
            await AnimateButtonPress(sender as View);
            await ShowLoadingOverlay("Opening registration...");
            await Task.Delay(500);
            await HideLoadingOverlay();
            await Navigation.PushModalAsync(new MainPage());
        }
        catch (Exception ex)
        {
            await HideLoadingOverlay();
            await ShowErrorSnackbar($"Failed to navigate to registration: {ex.Message}");
        }
    }

    private async Task<bool> CheckInternetConnectionAsync()
    {
        try
        {
            var current = Connectivity.Current.NetworkAccess;

            if (current != NetworkAccess.Internet)
            {
                await ShowErrorSnackbar("No internet connection. Please check your network settings.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking connectivity: {ex.Message}");
            return true;
        }
    }

    private async Task AnimateButtonPress(View? button)
    {
        try
        {
            if (button != null)
            {
                await Task.WhenAll(
                    button.ScaleTo(0.95, 100, Easing.CubicOut),
                    button.FadeTo(0.8, 100, Easing.CubicOut)
                );
                await Task.WhenAll(
                    button.ScaleTo(1, 100, Easing.CubicOut),
                    button.FadeTo(1, 100, Easing.CubicOut)
                );
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Button animation error: {ex.Message}");
        }
    }

    private async Task ShowLoadingOverlay(string message)
    {
        try
        {
            LoadingOverlay.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            await LoadingOverlay.FadeTo(1, 300, Easing.CubicOut);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Loading overlay error: {ex.Message}");
        }
    }

    private async Task HideLoadingOverlay()
    {
        try
        {
            await LoadingOverlay.FadeTo(0, 300, Easing.CubicOut);
            LoadingOverlay.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Hide loading overlay error: {ex.Message}");
        }
    }

    private async Task ShowValidationError(string message)
    {
        try
        {
            await EmailFrame.TranslateTo(-10, 0, 50);
            await EmailFrame.TranslateTo(10, 0, 50);
            await EmailFrame.TranslateTo(-5, 0, 50);
            await EmailFrame.TranslateTo(5, 0, 50);
            await EmailFrame.TranslateTo(0, 0, 50);

            var snackbar = Snackbar.Make(message, null, "OK", TimeSpan.FromSeconds(4), new SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#FF6B6B"),
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.White,
                CornerRadius = new CornerRadius(10),
                Font = Microsoft.Maui.Font.SystemFontOfSize(14)
            });
            await snackbar.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Validation error display: {ex.Message}");
        }
    }

    private async Task ShowErrorSnackbar(string message)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var snackbar = Snackbar.Make(message, null, "OK", TimeSpan.FromSeconds(5), new SnackbarOptions
                {
                    BackgroundColor = Color.FromArgb("#FF6B6B"),
                    TextColor = Colors.White,
                    ActionButtonTextColor = Colors.White,
                    CornerRadius = new CornerRadius(10),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                });
                await snackbar.Show();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error snackbar: {ex.Message}");
        }
    }

    private async Task ShowSuccessSnackbar(string message)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var snackbar = Snackbar.Make(message, null, "OK", TimeSpan.FromSeconds(3), new SnackbarOptions
                {
                    BackgroundColor = Color.FromArgb("#4CAF50"),
                    TextColor = Colors.White,
                    ActionButtonTextColor = Colors.White,
                    CornerRadius = new CornerRadius(10),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                });
                await snackbar.Show();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Success snackbar: {ex.Message}");
        }
    }

    private async Task DoSomeDataAccessAsync(string MyEmail, string MyPassword, bool rememberme)
    {
        HttpClient client = null;
        try
        {
            string url = "https://opxng.com/api/AuthAccount/Login";

            var requestPayload = new LoginObject
            {
                email = MyEmail,
                password = MyPassword,
                isMobile = rememberme,
            };

            string jsonPayload = JsonConvert.SerializeObject(requestPayload, Formatting.None);

            System.Diagnostics.Debug.WriteLine($"Login request for: {MyEmail}");

            client = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            })
            {
                Timeout = TimeSpan.FromSeconds(90)
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content).ConfigureAwait(false);

            string resultString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(resultString))
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ShowErrorSnackbar("Empty response from server");
                });
                return;
            }

            var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(resultString);

            if (loginResponse == null)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ShowErrorSnackbar("Failed to parse server response");
                });
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                string errorMsg = loginResponse.message ?? $"Server returned status: {response.StatusCode}";
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ShowErrorSnackbar(errorMsg);
                });
                return;
            }

            await HandleLoginResponseAsync(loginResponse, MyEmail);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ShowErrorSnackbar("Request timed out. Please check your internet connection and try again.");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"General error: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ShowErrorSnackbar($"An error occurred: {ex.Message}");
            });
        }
        finally
        {
            client?.Dispose();
        }
    }

    private async Task HandleLoginResponseAsync(LoginResponse loginResponse, string email)
    {
        try
        {
            if (!string.IsNullOrEmpty(loginResponse.success))
            {
                if (loginResponse.redirectTo == "Dashboard")
                {
                    mytoken = loginResponse.token;
                    myemail = loginResponse.email ?? email;
                    myfullname = loginResponse.fullname ?? "";
                    ledgerBalance = loginResponse.ledgerBalance ?? "";
                    availableBalance = loginResponse.availableBalance ?? "";
                    totalTransactions = loginResponse.totalTransactions ?? "";
                    completedTransactions = loginResponse.completedTransactions ?? "";
                    accountName = loginResponse.accountName;
                    accountNumber = loginResponse.accountNumber;
                    bankName = loginResponse.bankName;

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        // Show biometric icons after successful first login
                        BiometricButtonsContainer.IsVisible = true;

                        await ShowSuccessSnackbar($"Welcome back, {loginResponse.fullname}!");
                        await Task.Delay(500);
                        await Navigation.PushModalAsync(new Views.DashBoard());
                    });
                }
                else if (loginResponse.redirectTo == "Setup")
                {
                    myemail = loginResponse.email ?? email;
                    myfullname = loginResponse.fullname ?? "";

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await ShowSuccessSnackbar("Please complete your profile setup");
                        await Task.Delay(500);
                        await Navigation.PushModalAsync(new Views.DashBoard());
                    });
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await ShowErrorSnackbar($"Unexpected redirect: {loginResponse.redirectTo}");
                    });
                }
            }
            else if (loginResponse.requiresVerification == true)
            {
                myemail = email;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    string message = loginResponse.message ?? "Email verification required";
                    await ShowErrorSnackbar(message);
                    await Task.Delay(500);
                    await Navigation.PushModalAsync(new Views.DashBoard());
                });
            }
            else
            {
                string errorMessage = loginResponse.message ?? "Login failed. Please check your credentials.";
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ShowErrorSnackbar(errorMessage);
                    if (password != null)
                    {
                        password.Text = "";
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HandleLoginResponse error: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ShowErrorSnackbar($"Error processing login response: {ex.Message}");
            });
        }
    }

    internal class SavedCredentials
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    internal class LoginObject
    {
        public string email { get; set; } = "";
        public string password { get; set; } = "";
        public bool isMobile { get; set; } = false;
        public string model { get; set; } = "";
    }

    internal class LoginResponse
    {
        public string? success { get; set; }
        public string? email { get; set; }
        public string? token { get; set; }
        public string? fullname { get; set; }
        public string? message { get; set; }
        public string? requiresSetup { get; set; }
        public string? redirectTo { get; set; }
        public string? completedTransactions { get; set; }
        public string? totalTransactions { get; set; }
        public string? ledgerBalance { get; set; }
        public string? availableBalance { get; set; }
        public string? accountName { get; set; }
        public string? accountNumber { get; set; }
        public string? bankName { get; set; }
        public string? bankCode { get; set; }
        public bool requiresVerification { get; set; }
    }
}