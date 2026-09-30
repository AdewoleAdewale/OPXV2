using AiForms.Dialogs;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Opx.Views
{
    public partial class ForgetPassword : Popup
    {
        // Event for when a password reset is requested
        public event EventHandler<string> PasswordResetRequested;

        public static string? Forgetemail { get; set; }

        public ForgetPassword()
        {
            InitializeComponent();
        }

        private async void ResetPassword_Clicked(object sender, EventArgs e)
        {
            string email = EmailEntry?.Text?.Trim() ?? "";

            // Basic validation
            if (string.IsNullOrEmpty(email))
            {
                if (ErrorLabel != null)
                {
                    ErrorLabel.Text = "Please enter your email address.";
                    ErrorLabel.IsVisible = true;
                }
                return;
            }

            // Simple regex for email validation
            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            if (!Regex.IsMatch(email, emailPattern))
            {
                if (ErrorLabel != null)
                {
                    ErrorLabel.Text = "Please enter a valid email address.";
                    ErrorLabel.IsVisible = true;
                }
                return;
            }

            // Hide error label
            if (ErrorLabel != null)
            {
                ErrorLabel.IsVisible = false;
            }

            // Trigger the event
            PasswordResetRequested?.Invoke(this, email);

            // Check internet connectivity first
            if (!await CheckInternetConnectionAsync())
            {
                return;
            }

            Configurations.LoadingConfig = new LoadingConfig
            {
                Opacity = 0.4,
                DefaultMessage = "Connecting OPX Please Wait...",
                FontSize = 12,
            };

            await Loading.Instance.StartAsync(async progress =>
            {
                bool shouldClosePopup = false;
                string resultMessage = "";
                bool isSuccess = false;
                string backgroundColor = "#A25AC4";

                try
                {
                    // Progress simulation
                    for (var i = 0; i < 30; i++)
                    {
                        await Task.Delay(30);
                        progress.Report((i + 1) * 0.01d);
                    }

                    // Make API call
                    var (success, message, bgColor) = await SendPasswordResetRequestAsync(email, progress);

                    isSuccess = success;
                    resultMessage = message;
                    backgroundColor = bgColor;
                    shouldClosePopup = success;

                    // Complete progress
                    progress.Report(1.0d);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Critical error in password reset: {ex.Message}\n{ex.StackTrace}");
                    resultMessage = $"A critical error occurred: {ex.Message}";
                    shouldClosePopup = false;
                }

                // Handle the result after all processing is complete
                await HandleResultAsync(resultMessage, backgroundColor, isSuccess, shouldClosePopup);
            });
        }

        private async Task<bool> CheckInternetConnectionAsync()
        {
            try
            {
                var current = Connectivity.Current.NetworkAccess;

                if (current != NetworkAccess.Internet)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            var snackbar = Snackbar.Make(
                                "No internet connection. Please check your network settings.",
                                null,
                                "OK",
                                TimeSpan.FromSeconds(5),
                                new SnackbarOptions
                                {
                                    BackgroundColor = Colors.OrangeRed,
                                    TextColor = Colors.White,
                                    ActionButtonTextColor = Colors.White,
                                    CornerRadius = new CornerRadius(8),
                                    Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                                });
                            await snackbar.Show();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error showing connectivity snackbar: {ex.Message}");
                        }
                    });
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking connectivity: {ex.Message}");
                return true; // Proceed anyway if we can't check
            }
        }

        private async Task<(bool success, string message, string backgroundColor)> SendPasswordResetRequestAsync(
            string email, IProgress<double> progress)
        {
            HttpClient client = null;
            try
            {
                string url = "https://opxng.com/api/AuthAccount/ForgotPassword";

                var requestPayload = new ForgetObject
                {
                    email = email,
                };

                string jsonPayload = JsonConvert.SerializeObject(requestPayload, Formatting.None);

                System.Diagnostics.Debug.WriteLine($"Sending forgot password request for: {email}");

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
                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                progress?.Report(0.4d);

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"Sending request to: {url}");

                HttpResponseMessage response = await client.PostAsync(url, content).ConfigureAwait(false);

                progress?.Report(0.7d);

                System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");

                // Read response content
                string resultString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                System.Diagnostics.Debug.WriteLine($"Response content: {resultString}");

                progress?.Report(0.9d);

                if (string.IsNullOrWhiteSpace(resultString))
                {
                    return (false, "Empty response from server", "Purple");
                }

                var forgetResponse = JsonConvert.DeserializeObject<ForgetResponse>(resultString);

                if (forgetResponse == null)
                {
                    return (false, "Failed to parse server response", "Purple");
                }

                // Check for success
                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = forgetResponse.message ?? $"Server returned status: {response.StatusCode}";
                    return (false, errorMsg, "Purple");
                }

                // Handle successful response
                if (!string.IsNullOrEmpty(forgetResponse.success) ||
                    (!string.IsNullOrEmpty(forgetResponse.message) && forgetResponse.message.Contains("success", StringComparison.OrdinalIgnoreCase)))
                {
                    Forgetemail = email;

                    string successMsg = !string.IsNullOrEmpty(forgetResponse.expires)
                        ? $"OTP sent successfully! It will expire: {forgetResponse.expires}"
                        : forgetResponse.message ?? "Password reset request sent successfully!";

                    return (true, successMsg, "ForestGreen");
                }
                else
                {
                    string msg = forgetResponse.message ?? "Request failed. Please try again.";
                    return (false, msg, "Purple");
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                System.Diagnostics.Debug.WriteLine($"Timeout error: {ex.Message}");
                return (false, "Request timed out. Please check your internet connection and try again.", "OrangeRed");
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cancelled error: {ex.Message}");
                return (false, "Request was cancelled. Please try again.", "OrangeRed");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Network error: {ex.Message}\n{ex.StackTrace}");
                return (false, $"Network error: {ex.Message}. Please check your internet connection.", "OrangeRed");
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON error: {ex.Message}");
                return (false, $"Data format error: {ex.Message}", "Purple");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General error: {ex.Message}\n{ex.StackTrace}");
                return (false, $"An error occurred: {ex.Message}", "OrangeRed");
            }
            finally
            {
                client?.Dispose();
            }
        }

        private async Task HandleResultAsync(string message, string backgroundColor, bool isSuccess, bool shouldClosePopup)
        {
            try
            {
                if (shouldClosePopup && isSuccess)
                {
                    // Close popup first for success case
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            await this.CloseAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error closing popup: {ex.Message}");
                        }
                    });

                    // Small delay to ensure popup is closed
                    await Task.Delay(200);

                    // Show success message and navigate
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            var snackbar = Snackbar.Make(
                                message,
                                null,
                                "OK",
                                TimeSpan.FromSeconds(10),
                                new SnackbarOptions
                                {
                                    BackgroundColor = Colors.ForestGreen,
                                    TextColor = Colors.White,
                                    ActionButtonTextColor = Colors.White,
                                    CornerRadius = new CornerRadius(10),
                                    Font = Microsoft.Maui.Font.SystemFontOfSize(14),
                                    ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14)
                                });
                            await snackbar.Show(CancellationToken.None);

                            // Small delay before navigation
                            await Task.Delay(500);

                            // Navigate to verification page
                            if (Application.Current?.MainPage?.Navigation != null)
                            {
                                await Application.Current.MainPage.Navigation.PushModalAsync(new Views.RPage());
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error showing success message or navigating: {ex.Message}");
                        }
                    });
                }
                else
                {
                    // Show error message while popup is still open
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            var color = backgroundColor switch
                            {
                                "Purple" => Colors.Purple,
                                "ForestGreen" => Colors.ForestGreen,
                                "OrangeRed" => Colors.OrangeRed,
                                _ => Color.FromArgb(backgroundColor)
                            };

                            var snackbar = Snackbar.Make(
                                message,
                                null,
                                "OK",
                                TimeSpan.FromSeconds(8),
                                new SnackbarOptions
                                {
                                    BackgroundColor = color,
                                    TextColor = Colors.White,
                                    ActionButtonTextColor = Colors.White,
                                    CornerRadius = new CornerRadius(8),
                                    Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                                });
                            await snackbar.Show(CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error showing error snackbar: {ex.Message}");

                            // Fallback to alert dialog
                            try
                            {
                                if (Application.Current?.MainPage != null)
                                {
                                    await Application.Current.MainPage.DisplayAlert("Notification", message, "OK");
                                }
                            }
                            catch (Exception alertEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error showing fallback alert: {alertEx.Message}");
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HandleResultAsync: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void Cancel_Clicked(object sender, EventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing popup: {ex.Message}");
            }
        }
    }

    internal class ForgetObject
    {
        public string email { get; set; } = "";
    }

    internal class ForgetResponse
    {
        public string? success { get; set; }
        public string? email { get; set; }
        public string? message { get; set; }
        public string? expires { get; set; }
        public string? requiresSetup { get; set; }
        public string? redirectTo { get; set; }
        public bool requiresVerification { get; set; }
    }
}