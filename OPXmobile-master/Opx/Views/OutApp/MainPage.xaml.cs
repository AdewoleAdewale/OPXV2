using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;
using Newtonsoft.Json;
using System.Text;

namespace Opx
{
    public partial class MainPage : ContentPage
    {
        public static string mymail { get; set; }
        private int currentStep = 1;
        public static string otpCode { get; set; } // Store OTP for verification page
        private bool isPasswordVisible = false;
        private bool isConfirmPasswordVisible = false;
        private CancellationTokenSource? animationCancellationToken;

        public MainPage()
        {
            try
            {
                InitializeComponent();
                animationCancellationToken = new CancellationTokenSource();
                InitializeCountryCodePicker();
                InitializeAnimations();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainPage constructor error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void InitializeCountryCodePicker()
        {
            try
            {
                // Set Nigeria as default
                if (CountryCodePicker != null && CountryCodePicker.Items.Count > 0)
                {
                    CountryCodePicker.SelectedIndex = 0; // +234
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeCountryCodePicker error: {ex.Message}");
            }
        }

        private async void NextButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (NextButton == null) return;

                NextButton.IsEnabled = false;
                await AnimateButtonPressAsync(NextButton);

                // Validate Step 1 fields
                if (!await ValidateStep1Async())
                {
                    NextButton.IsEnabled = true;
                    return;
                }

                // Proceed to Step 2
                await TransitionToStep2Async();
                NextButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                await ShowErrorSnackbarAsync($"Navigation error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"NextButton_Clicked error: {ex.Message}");
                if (NextButton != null) NextButton.IsEnabled = true;
            }
        }

        private async Task<bool> ValidateStep1Async()
        {
            // Hide only Step 1 errors
            if (FirstNameError != null) FirstNameError.IsVisible = false;
            if (MiddleNameError != null) MiddleNameError.IsVisible = false;
            if (OtherNamesError != null) OtherNamesError.IsVisible = false;
            if (StateError != null) StateError.IsVisible = false;
            if (AddressError != null) AddressError.IsVisible = false;

            bool isValid = true;

            // Validate First Name
            if (string.IsNullOrWhiteSpace(firstname?.Text))
            {
                if (FirstNameError != null) FirstNameError.IsVisible = true;
                isValid = false;
            }

            // Validate Middle Name
            if (string.IsNullOrWhiteSpace(lastname?.Text))
            {
                if (MiddleNameError != null) MiddleNameError.IsVisible = true;
                isValid = false;
            }

            // Validate Other Names
            if (string.IsNullOrWhiteSpace(Othernamesinfo?.Text))
            {
                if (OtherNamesError != null) OtherNamesError.IsVisible = true;
                isValid = false;
            }

            // Validate State
            if (Addstatename?.SelectedIndex == -1)
            {
                if (StateError != null) StateError.IsVisible = true;
                isValid = false;
            }

            // Validate Address
            if (string.IsNullOrWhiteSpace(Addressinfo?.Text))
            {
                if (AddressError != null) AddressError.IsVisible = true;
                isValid = false;
            }

            if (!isValid)
            {
                await ShowErrorSnackbarAsync("Please fill all required fields in Step 1");
                if (MainScrollView != null)
                {
                    await MainScrollView.ScrollToAsync(0, 0, true);
                }
            }

            return isValid;
        }

        private async Task<bool> ValidateStep2Async()
        {
            // Hide only Step 2 errors
            if (PhoneError != null) PhoneError.IsVisible = false;
            if (EmailError != null) EmailError.IsVisible = false;
            if (PasswordError != null) PasswordError.IsVisible = false;
            if (ConfirmPasswordError != null) ConfirmPasswordError.IsVisible = false;

            bool isValid = true;

            // Validate Country Code
            if (CountryCodePicker?.SelectedIndex == -1)
            {
                if (PhoneError != null)
                {
                    PhoneError.Text = "Please select country code";
                    PhoneError.IsVisible = true;
                }
                isValid = false;
            }

            // Validate Phone Number
            if (string.IsNullOrWhiteSpace(UserPhone?.Text))
            {
                if (PhoneError != null)
                {
                    PhoneError.Text = "Phone number is required";
                    PhoneError.IsVisible = true;
                }
                isValid = false;
            }
            else if (UserPhone.Text.Length < 7)
            {
                if (PhoneError != null)
                {
                    PhoneError.Text = "Phone number is too short";
                    PhoneError.IsVisible = true;
                }
                isValid = false;
            }

            // Validate Email
            if (string.IsNullOrWhiteSpace(Email?.Text))
            {
                if (EmailError != null)
                {
                    EmailError.Text = "Email is required";
                    EmailError.IsVisible = true;
                }
                isValid = false;
            }
            else if (!IsValidEmail(Email.Text))
            {
                if (EmailError != null)
                {
                    EmailError.Text = "Invalid email format";
                    EmailError.IsVisible = true;
                }
                isValid = false;
            }

            // Validate Password
            if (string.IsNullOrWhiteSpace(Password?.Text))
            {
                if (PasswordError != null)
                {
                    PasswordError.Text = "Password is required";
                    PasswordError.IsVisible = true;
                }
                isValid = false;
            }
            else if (!IsPasswordStrong(Password.Text))
            {
                if (PasswordError != null) PasswordError.IsVisible = true;
                isValid = false;
            }

            // Validate Confirm Password
            if (string.IsNullOrWhiteSpace(ConfirmPassword?.Text))
            {
                if (ConfirmPasswordError != null)
                {
                    ConfirmPasswordError.Text = "Confirm password is required";
                    ConfirmPasswordError.IsVisible = true;
                }
                isValid = false;
            }
            else if (Password?.Text != ConfirmPassword?.Text)
            {
                if (ConfirmPasswordError != null)
                {
                    ConfirmPasswordError.Text = "Passwords do not match";
                    ConfirmPasswordError.IsVisible = true;
                }
                isValid = false;
            }

            if (!isValid)
            {
                await ShowErrorSnackbarAsync("Please fill all required fields correctly");
                if (MainScrollView != null)
                {
                    await MainScrollView.ScrollToAsync(Step2Container, ScrollToPosition.Start, true);
                }
            }

            return isValid;
        }
        private async void BackButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (BackButton == null) return;

                BackButton.IsEnabled = false;
                await AnimateButtonPressAsync(BackButton);

                // Go back to Step 1
                await TransitionToStep1Async();
                BackButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                await ShowErrorSnackbarAsync($"Navigation error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"BackButton_Clicked error: {ex.Message}");
                if (BackButton != null) BackButton.IsEnabled = true;
            }
        }

        private async Task TransitionToStep2Async()
        {
            try
            {
                currentStep = 2;

                // Update step indicators
                if (Step1Indicator != null)
                {
                    Step1Indicator.BackgroundColor = Colors.Transparent;
                    Step1Indicator.BorderColor = Colors.White;
                    Step1Indicator.HasShadow = false;
                    var step1Label = Step1Indicator.Content as Label;
                    if (step1Label != null) step1Label.TextColor = Colors.White;
                }

                if (Step2Indicator != null)
                {
                    Step2Indicator.BackgroundColor = Colors.White;
                    Step2Indicator.BorderColor = Colors.Transparent;
                    Step2Indicator.HasShadow = true;
                    var step2Label = Step2Indicator.Content as Label;
                    if (step2Label != null) step2Label.TextColor = Color.FromArgb("#A25AC4");
                }

                // Animate transition
                if (Step1Container != null)
                {
                    await Step1Container.FadeTo(0, 200);
                    Step1Container.IsVisible = false;
                }

                if (Step2Container != null)
                {
                    Step2Container.Opacity = 0;
                    Step2Container.TranslationX = 50;
                    Step2Container.IsVisible = true;

                    await Task.WhenAll(
                        Step2Container.FadeTo(1, 400, Easing.CubicOut),
                        Step2Container.TranslateTo(0, 0, 400, Easing.CubicOut)
                    );
                }

                // Scroll to top
                if (MainScrollView != null)
                {
                    await MainScrollView.ScrollToAsync(0, 0, true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TransitionToStep2 error: {ex.Message}");
            }
        }

        private async Task TransitionToStep1Async()
        {
            try
            {
                currentStep = 1;

                // Update step indicators
                if (Step1Indicator != null)
                {
                    Step1Indicator.BackgroundColor = Colors.White;
                    Step1Indicator.BorderColor = Colors.Transparent;
                    Step1Indicator.HasShadow = true;
                    var step1Label = Step1Indicator.Content as Label;
                    if (step1Label != null) step1Label.TextColor = Color.FromArgb("#A25AC4");
                }

                if (Step2Indicator != null)
                {
                    Step2Indicator.BackgroundColor = Colors.Transparent;
                    Step2Indicator.BorderColor = Colors.White;
                    Step2Indicator.HasShadow = false;
                    var step2Label = Step2Indicator.Content as Label;
                    if (step2Label != null) step2Label.TextColor = Colors.White;
                }

                // Animate transition
                if (Step2Container != null)
                {
                    await Step2Container.FadeTo(0, 200);
                    Step2Container.IsVisible = false;
                }

                if (Step1Container != null)
                {
                    Step1Container.Opacity = 0;
                    Step1Container.TranslationX = -50;
                    Step1Container.IsVisible = true;

                    await Task.WhenAll(
                        Step1Container.FadeTo(1, 400, Easing.CubicOut),
                        Step1Container.TranslateTo(0, 0, 400, Easing.CubicOut)
                    );
                }

                // Scroll to top
                if (MainScrollView != null)
                {
                    await MainScrollView.ScrollToAsync(0, 0, true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TransitionToStep1 error: {ex.Message}");
            }
        }
        protected override async void OnAppearing()
        {
            try
            {
                base.OnAppearing();
                await StartPageAnimationsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnAppearing error: {ex.Message}");
            }
        }

        protected override void OnDisappearing()
        {
            try
            {
                base.OnDisappearing();
                animationCancellationToken?.Cancel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnDisappearing error: {ex.Message}");
            }
        }

        #region Animation Methods

        private void InitializeAnimations()
        {
            try
            {
                // Check if elements exist before animating
                if (LogoSection != null)
                {
                    LogoSection.Opacity = 0;
                    LogoSection.TranslationY = -50;
                    LogoSection.Scale = 0.8;
                }

                if (RegistrationForm != null)
                {
                    RegistrationForm.Opacity = 0;
                    RegistrationForm.TranslationY = 50;
                }

                if (FooterSection != null)
                {
                    FooterSection.Opacity = 0;
                    FooterSection.TranslationY = 30;
                }

                var fields = new[] { FirstNameFrame, LastNameFrame, PhoneFrame, EmailFrame, PasswordFrame, ConfirmPasswordFrame }
                    .Where(f => f != null).ToArray();

                foreach (var field in fields)
                {
                    field.Scale = 0.8;
                    field.Opacity = 0.5;
                    field.TranslationY = 20;
                }

                // Start floating animation in background
                Dispatcher.Dispatch(async () =>
                {
                    await AnimateFloatingElementsAsync();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Animation initialization error: {ex.Message}");
            }
        }

        private async Task StartPageAnimationsAsync()
        {
            try
            {
                if (LogoSection != null)
                {
                    await Task.Delay(200);
                    await Task.WhenAll(
                        LogoSection.FadeTo(1, 800, Easing.CubicOut),
                        LogoSection.TranslateTo(0, 0, 600, Easing.CubicOut),
                        LogoSection.ScaleTo(1, 700, Easing.BounceOut)
                    );
                }

                if (RegistrationForm != null)
                {
                    await Task.Delay(200);
                    await Task.WhenAll(
                        RegistrationForm.FadeTo(1, 800, Easing.CubicOut),
                        RegistrationForm.TranslateTo(0, 0, 600, Easing.CubicOut)
                    );
                }

                if (FooterSection != null)
                {
                    await Task.Delay(200);
                    await Task.WhenAll(
                        FooterSection.FadeTo(1, 800, Easing.CubicOut),
                        FooterSection.TranslateTo(0, 0, 600, Easing.CubicOut)
                    );
                }

                await AnimateFormFieldsAsync();
                await AnimateRegisterButtonAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Page animation error: {ex.Message}");
            }
        }

        private async Task AnimateFormFieldsAsync()
        {
            try
            {
                var fields = new[] { FirstNameFrame, LastNameFrame, PhoneFrame, EmailFrame, PasswordFrame, ConfirmPasswordFrame }
                    .Where(f => f != null).ToArray();

                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    await Task.Delay(100);

                    await Task.WhenAll(
                        field.ScaleTo(1, 400, Easing.CubicOut),
                        field.FadeTo(1, 400, Easing.CubicOut),
                        field.TranslateTo(0, 0, 400, Easing.CubicOut)
                    );

                    await field.ScaleTo(1.02, 100, Easing.CubicOut);
                    await field.ScaleTo(1, 100, Easing.CubicOut);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Form field animation error: {ex.Message}");
            }
        }

        private async Task AnimateRegisterButtonAsync()
        {
            try
            {
                if (RegisterButton == null) return;

                RegisterButton.Scale = 0.8;
                RegisterButton.Opacity = 0.5;

                await Task.WhenAll(
                    RegisterButton.ScaleTo(1, 500, Easing.BounceOut),
                    RegisterButton.FadeTo(1, 500, Easing.CubicOut)
                );

                // Start pulse animation in background
                Dispatcher.Dispatch(async () =>
                {
                    await PulseButtonAsync();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Button animation error: {ex.Message}");
            }
        }

        private async Task PulseButtonAsync()
        {
            try
            {
                while (!animationCancellationToken.Token.IsCancellationRequested && RegisterButton != null)
                {
                    await Task.Delay(3000, animationCancellationToken.Token);
                    if (RegisterButton != null)
                    {
                        await RegisterButton.ScaleTo(1.05, 300, Easing.CubicInOut);
                        await RegisterButton.ScaleTo(1, 300, Easing.CubicInOut);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Button pulse animation error: {ex.Message}");
            }
        }

        private async Task AnimateFloatingElementsAsync()
        {
            try
            {
                if (FloatingElements == null) return;

                var random = new Random();

                while (!animationCancellationToken.Token.IsCancellationRequested)
                {
                    var ellipses = FloatingElements.Children.OfType<Ellipse>().ToList();

                    foreach (var child in ellipses)
                    {
                        try
                        {
                            var duration = (uint)(3000 + random.Next(2000));
                            var translateX = (random.NextDouble() - 0.5) * 20;
                            var translateY = (random.NextDouble() - 0.5) * 20;
                            var scale = 0.8 + (random.NextDouble() * 0.4);

                            await Task.WhenAll(
                                child.TranslateTo(translateX, translateY, duration, Easing.SinInOut),
                                child.ScaleTo(scale, duration, Easing.SinInOut)
                            );
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Individual element animation error: {ex.Message}");
                        }
                    }

                    await Task.Delay(1000, animationCancellationToken.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Floating elements animation error: {ex.Message}");
            }
        }

        private async Task AnimateButtonPressAsync(Button button)
        {
            try
            {
                if (button != null)
                {
                    await button.ScaleTo(0.95, 100, Easing.CubicOut);
                    await button.ScaleTo(1, 100, Easing.CubicOut);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Button press animation error: {ex.Message}");
            }
        }

        #endregion

        #region Password Visibility Toggle

        private async void ShowPasswordButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                await AnimateButtonPressAsync(ShowPasswordButton);

                isPasswordVisible = !isPasswordVisible;
                if (Password != null)
                {
                    Password.IsPassword = !isPasswordVisible;
                }
                if (ShowPasswordButton != null)
                {
                    ShowPasswordButton.Text = isPasswordVisible ? "🙈" : "👁️";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Password toggle error: {ex.Message}");
            }
        }

        private async void ShowConfirmPasswordButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                await AnimateButtonPressAsync(ShowConfirmPasswordButton);

                isConfirmPasswordVisible = !isConfirmPasswordVisible;
                if (ConfirmPassword != null)
                {
                    ConfirmPassword.IsPassword = !isConfirmPasswordVisible;
                }
                if (ShowConfirmPasswordButton != null)
                {
                    ShowConfirmPasswordButton.Text = isConfirmPasswordVisible ? "🙈" : "👁️";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Confirm password toggle error: {ex.Message}");
            }
        }

        #endregion

        #region Validation Methods

        private void HideAllErrors()
        {
            try
            {
                if (FirstNameError != null) FirstNameError.IsVisible = false;
                if (MiddleNameError != null) MiddleNameError.IsVisible = false;
                if (OtherNamesError != null) OtherNamesError.IsVisible = false;
                if (StateError != null) StateError.IsVisible = false;
                if (PhoneError != null) PhoneError.IsVisible = false;
                if (EmailError != null) EmailError.IsVisible = false;
                if (PasswordError != null) PasswordError.IsVisible = false;
                if (ConfirmPasswordError != null) ConfirmPasswordError.IsVisible = false;
                if (AddressError != null) AddressError.IsVisible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HideAllErrors error: {ex.Message}");
            }
        }

        private async Task<bool> ValidateAllFieldsAsync()
        {
            HideAllErrors();
            bool isValid = true;

            // Validate First Name
            if (string.IsNullOrWhiteSpace(firstname?.Text))
            {
                if (FirstNameError != null) FirstNameError.IsVisible = true;
                isValid = false;
            }

            // Validate Middle Name
            if (string.IsNullOrWhiteSpace(lastname?.Text))
            {
                if (MiddleNameError != null) MiddleNameError.IsVisible = true;
                isValid = false;
            }

            // Validate Other Names
            if (string.IsNullOrWhiteSpace(Othernamesinfo?.Text))
            {
                if (OtherNamesError != null) OtherNamesError.IsVisible = true;
                isValid = false;
            }

            // Validate State
            if (Addstatename?.SelectedIndex == -1)
            {
                if (StateError != null) StateError.IsVisible = true;
                isValid = false;
            }

            // Validate Country Code
            if (CountryCodePicker?.SelectedIndex == -1)
            {
                if (PhoneError != null)
                {
                    PhoneError.Text = "Please select country code";
                    PhoneError.IsVisible = true;
                }
                isValid = false;
            }

            // Validate Phone Number
            if (string.IsNullOrWhiteSpace(UserPhone?.Text))
            {
                if (PhoneError != null)
                {
                    PhoneError.Text = "Phone number is required";
                    PhoneError.IsVisible = true;
                }
                isValid = false;
            }
            else if (UserPhone.Text.Length < 7)
            {
                if (PhoneError != null)
                {
                    PhoneError.Text = "Phone number is too short";
                    PhoneError.IsVisible = true;
                }
                isValid = false;
            }

            // Validate Email
            if (string.IsNullOrWhiteSpace(Email?.Text))
            {
                if (EmailError != null)
                {
                    EmailError.Text = "Email is required";
                    EmailError.IsVisible = true;
                }
                isValid = false;
            }
            else if (!IsValidEmail(Email.Text))
            {
                if (EmailError != null)
                {
                    EmailError.Text = "Invalid email format";
                    EmailError.IsVisible = true;
                }
                isValid = false;
            }

            // Validate Password
            if (string.IsNullOrWhiteSpace(Password?.Text))
            {
                if (PasswordError != null)
                {
                    PasswordError.Text = "Password is required";
                    PasswordError.IsVisible = true;
                }
                isValid = false;
            }
            else if (!IsPasswordStrong(Password.Text))
            {
                if (PasswordError != null) PasswordError.IsVisible = true;
                isValid = false;
            }

            // Validate Confirm Password
            if (string.IsNullOrWhiteSpace(ConfirmPassword?.Text))
            {
                if (ConfirmPasswordError != null)
                {
                    ConfirmPasswordError.Text = "Confirm password is required";
                    ConfirmPasswordError.IsVisible = true;
                }
                isValid = false;
            }
            else if (Password?.Text != ConfirmPassword?.Text)
            {
                if (ConfirmPasswordError != null)
                {
                    ConfirmPasswordError.Text = "Passwords do not match";
                    ConfirmPasswordError.IsVisible = true;
                }
                isValid = false;
            }

            // Validate Address
            if (string.IsNullOrWhiteSpace(Addressinfo?.Text))
            {
                if (AddressError != null) AddressError.IsVisible = true;
                isValid = false;
            }

            if (!isValid)
            {
                await ShowErrorSnackbarAsync($"Please fill all required fields correctly");
                if (MainScrollView != null)
                {
                    await MainScrollView.ScrollToAsync(0, 0, true);
                }
            }

            return isValid;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email && email.Contains("@") && email.Contains(".");
            }
            catch
            {
                return false;
            }
        }

        private bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            return password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit);
        }

        #endregion

        #region Registration Logic

        async Task RegisterClick()
        {
            try
            {
                // Validate Step 2 fields (Step 1 was already validated)
                if (!await ValidateStep2Async())
                {
                    return;
                }

                string countryCode = CountryCodePicker?.SelectedItem?.ToString() ?? "+234";
                string phoneNumber = UserPhone?.Text?.Trim() ?? "";
                phoneNumber = phoneNumber.TrimStart('0');
                string fullPhoneNumber = $"{countryCode}{phoneNumber}";
                string MyEmail = Email?.Text?.Trim()?.ToLower() ?? "";
                string MyOtherNames = Othernamesinfo?.Text?.Trim() ?? "";
                string MyAddress = Addressinfo?.Text?.Trim() ?? "";
                string MyStateNames = Addstatename?.SelectedItem?.ToString() ?? "";
                string MyPassword = Password?.Text ?? "";
                string MyFirstName = firstname?.Text?.Trim() ?? "";
                string MyLastName = lastname?.Text?.Trim() ?? "";
                string Myref = refcode?.Text?.Trim() ?? ""; // Made optional - empty string if not provided
                string MyConfirmPassword = ConfirmPassword?.Text ?? "";

                await DoSomeDataAccessAsync(MyEmail, MyPassword, MyConfirmPassword, fullPhoneNumber,
                    MyAddress, MyOtherNames, MyStateNames, MyFirstName, MyLastName, Myref);
            }
            catch (Exception ex)
            {
                await ShowErrorSnackbarAsync($"Registration error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"RegisterClick error: {ex.Message}\n{ex.StackTrace}");
            }
        }



        // Rest of the method remains the same...
        private async Task DoSomeDataAccessAsync(string MyEmail, string MyPassword, string MyConfirmPassword, string Myphone,
          string MyAddress, string MyOtherNames, string MyStateNames, string MyFirstName, string MyLastName, string Myref)
        {
            HttpClient client = null;
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(MyEmail) || string.IsNullOrWhiteSpace(MyPassword))
                {
                    await ShowErrorSnackbarAsync("Email and password are required");
                    return;
                }

                string url = "https://opxng.com/api/AuthAccount/register";

                // Build the request payload - referral code is optional
                var requestPayload = new RegisterObject
                {
                    fullName = MyFirstName,
                    otherName = MyOtherNames,
                    middleName = MyLastName,
                    phone = Myphone,
                    state = MyStateNames,
                    address = MyAddress,
                    email = MyEmail,
                    password = MyPassword,
                    referralCode = string.IsNullOrWhiteSpace(Myref) ? "" : Myref, // Optional - send empty string if not provided
                    confirmPassword = MyConfirmPassword,
                    isMobile = true
                };

                // Serialize to JSON
                string jsonPayload = JsonConvert.SerializeObject(requestPayload, Formatting.None);

                System.Diagnostics.Debug.WriteLine($"Sending payload: {jsonPayload}");

                // Create HttpClient with proper configuration
                client = new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                })
                {
                    Timeout = TimeSpan.FromSeconds(90)
                };

                // Add headers
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (LoadingText != null) LoadingText.Text = "Sending registration data...";
                });

                // Send POST request
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"Sending request to: {url}");

                HttpResponseMessage response = await client.PostAsync(url, content).ConfigureAwait(false);

                System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");

                // Read response content
                string resultString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                System.Diagnostics.Debug.WriteLine($"Response content: {resultString}");

                if (string.IsNullOrWhiteSpace(resultString))
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await ShowErrorSnackbarAsync("Empty response from server");
                    });
                    return;
                }

                // Parse response
                var RegisResponse = JsonConvert.DeserializeObject<RegisterResponse>(resultString);

                if (RegisResponse == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await ShowErrorSnackbarAsync("Failed to parse server response");
                    });
                    return;
                }

                // Handle the response based on success or error
                if (!response.IsSuccessStatusCode || RegisResponse.success == false)
                {
                    string errorMsg = RegisResponse.error ?? RegisResponse.message ?? $"Registration failed: {response.StatusCode}";
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await ShowErrorSnackbarAsync(errorMsg);
                    });
                    return;
                }

                // Handle successful registration on main thread
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await HandleRegistrationResponseAsync(RegisResponse, MyEmail);
                });
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                System.Diagnostics.Debug.WriteLine($"Timeout error: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ShowErrorSnackbarAsync("Request timed out. Please check your internet connection and try again.");
                });
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cancelled error: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ShowErrorSnackbarAsync("Request was cancelled. Please try again.");
                });
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Network error: {ex.Message}\n{ex.StackTrace}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ShowErrorSnackbarAsync($"Network error: {ex.Message}. Please check your internet connection.");
                });
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON error: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ShowErrorSnackbarAsync($"Data format error: {ex.Message}");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General error: {ex.Message}\n{ex.StackTrace}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ShowErrorSnackbarAsync($"Unexpected error: {ex.Message}");
                });
            }
            finally
            {
                client?.Dispose();
            }
        }

        private async Task<bool> CheckInternetConnectionAsync()
        {
            var current = Connectivity.Current.NetworkAccess;

            if (current != NetworkAccess.Internet)
            {
                await ShowErrorSnackbarAsync("No internet connection. Please check your network settings.");
                return false;
            }

            return true;
        }
        private async Task HandleRegistrationResponseAsync(RegisterResponse response, string email)
        {
            try
            {
                // Check if registration was successful
                if (response.success == true)
                {
                    // Store email
                    mymail = email.ToLower();

                    // Show success message
                    string successMessage = response.message ?? "Registration successful!";
                    await ShowSuccessSnackbarAsync(successMessage);

                    // Wait for snackbar to be visible before navigating
                    await Task.Delay(100);

                    // Navigate based on redirectTo value
                    if (!string.IsNullOrEmpty(response.redirectTo))
                    {
                        if (response.redirectTo.Equals("Login", StringComparison.OrdinalIgnoreCase))
                        {
                            // Navigate to Login
                            try
                            {

                                await Navigation.PushModalAsync(new Views.LoginPage());
                            }
                            catch (Exception navEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Login navigation error: {navEx.Message}");
                                await ShowErrorSnackbarAsync($"Navigation error: {navEx.Message}");
                            }
                        }
                        else if (response.redirectTo.Equals("VerifyEmail", StringComparison.OrdinalIgnoreCase))
                        {
                            // Navigate to verification page if OTP is provided
                            try
                            {
                                if (!string.IsNullOrEmpty(response.otp))
                                {
                                    otpCode = response.otp;
                                    await Navigation.PushModalAsync(new Views.Verification());
                                }
                                else
                                {
                                    await Navigation.PushModalAsync(new Views.Verification());
                                }
                            }
                            catch (Exception navEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Verification navigation error: {navEx.Message}");
                                await ShowErrorSnackbarAsync($"Navigation error: {navEx.Message}");
                            }
                        }
                        else if (response.redirectTo.Equals("Login", StringComparison.OrdinalIgnoreCase))
                        {
                            // Navigate to Login page
                            try
                            {
                                await Navigation.PushModalAsync(new Views.LoginPage());
                            }
                            catch (Exception navEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Login navigation error: {navEx.Message}");
                                await ShowErrorSnackbarAsync($"Navigation error: {navEx.Message}");
                            }
                        }
                        else
                        {
                            // Default navigation to Dashboard
                            try
                            {

                                await Navigation.PushModalAsync(new Views.LoginPage());
                            }
                            catch (Exception navEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Default Dashboard navigation error: {navEx.Message}");
                                await ShowErrorSnackbarAsync($"Navigation error: {navEx.Message}");
                            }
                        }
                    }
                    else
                    {
                        // No redirectTo specified, default to Dashboard
                        try
                        {
                            await Navigation.PushModalAsync(new Views.LoginPage());
                        }
                        catch (Exception navEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"No redirectTo navigation error: {navEx.Message}");
                            await ShowErrorSnackbarAsync($"Navigation error: {navEx.Message}");
                        }
                    }
                }
                else
                {
                    // Show error message
                    string errorMessage = response.error ?? response.message ?? "Registration failed. Please try again.";
                    await ShowErrorSnackbarAsync(errorMessage);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorSnackbarAsync($"Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"HandleRegistrationResponseAsync error: {ex.Message}\n{ex.StackTrace}");
            }
        }
        #endregion

        #region UI Helper Methods

        private async Task ShowErrorSnackbarAsync(string message)
        {
            try
            {
                var snackbar = Snackbar.Make(message, null, "OK", TimeSpan.FromSeconds(5), new SnackbarOptions
                {
                    BackgroundColor = Color.FromArgb("#A25AC4"),
                    TextColor = Colors.White,
                    ActionButtonTextColor = Colors.White,
                    CornerRadius = new CornerRadius(8),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                });
                await snackbar.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Snackbar error: {ex.Message}");
            }
        }

        private async Task ShowSuccessSnackbarAsync(string message)
        {
            try
            {
                // Show success snackbar
                var snackbar = Snackbar.Make(message, null, "OK", TimeSpan.FromSeconds(5), new SnackbarOptions
                {
                    BackgroundColor = Colors.ForestGreen,
                    TextColor = Colors.White,
                    ActionButtonTextColor = Colors.White,
                    CornerRadius = new CornerRadius(8),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                });
                await snackbar.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Snackbar error: {ex.Message}");
            }
        }

        private async Task ShowOtpSnackbarAsync(string message)
        {
            try
            {
                // Show snackbar for 60 seconds with OTP
                var snackbar = Snackbar.Make(message, null, "OK", TimeSpan.FromSeconds(60), new SnackbarOptions
                {
                    BackgroundColor = Colors.ForestGreen,
                    TextColor = Colors.White,
                    ActionButtonTextColor = Colors.White,
                    CornerRadius = new CornerRadius(8),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                });
                await snackbar.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Snackbar error: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private async void Button_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (RegisterButton == null) return;

                RegisterButton.IsEnabled = false;

                await AnimateButtonPressAsync(RegisterButton);

                if (LoadingOverlay != null)
                {
                    LoadingOverlay.IsVisible = true;
                }
                if (LoadingIndicator != null)
                {
                    LoadingIndicator.IsRunning = true;
                }
                if (LoadingText != null)
                {
                    LoadingText.Text = "Validating information...";
                }

                try
                {
                    await Task.Delay(300);
                    await RegisterClick();
                }
                finally
                {
                    if (LoadingOverlay != null)
                    {
                        LoadingOverlay.IsVisible = false;
                    }
                    if (LoadingIndicator != null)
                    {
                        LoadingIndicator.IsRunning = false;
                    }
                    if (RegisterButton != null)
                    {
                        RegisterButton.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorSnackbarAsync($"Failed to start registration: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Button_Clicked error: {ex.Message}");

                if (LoadingOverlay != null) LoadingOverlay.IsVisible = false;
                if (LoadingIndicator != null) LoadingIndicator.IsRunning = false;
                if (RegisterButton != null) RegisterButton.IsEnabled = true;
            }
        }

        private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            try
            {
                await Navigation.PushModalAsync(new Views.LoginPage());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                await ShowErrorSnackbarAsync("Navigation error occurred");
            }
        }

        #endregion
    }

    #region Data Models

    internal class RegisterObject
    {
        public string fullName { get; set; } = "";
        public string otherName { get; set; } = "";
        public string middleName { get; set; } = "";
        public string phone { get; set; } = "";
        public string state { get; set; } = "";
        public string address { get; set; } = "";
        public string email { get; set; } = "";
        public string password { get; set; } = "";

        public string referralCode { get; set; } = "";
        public string confirmPassword { get; set; } = "";
        public bool isMobile { get; set; } = true;
    }

    internal class RegisterResponse
    {
        public bool success { get; set; }
        public string? message { get; set; }
        public string? error { get; set; }
        public string? email { get; set; }
        public string? fullName { get; set; }
        public bool requiresSetup { get; set; }
        public string? redirectTo { get; set; }
        public string? otp { get; set; }
        public bool requiresVerification { get; set; }
    }

    #endregion
}