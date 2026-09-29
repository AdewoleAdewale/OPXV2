using CommunityToolkit.Maui.Views;

namespace Opx.Views
{
    public partial class VerificationErrorSheet : Popup
    {
        private Frame mainFrame;
        private Label errorIcon;
        private Label titleLabel;
        private Label subtitleLabel;
        private Label messageLabel;
        private Label errorCodeLabel;
        private StackLayout solutionsContainer;
        private Label timestampLabel;
        private Button tryAgainButton;
        private Button contactSupportButton;
        private Button closeButton;

        private string _title;
        private string _message;
        private string _errorCode;

        public VerificationErrorSheet(string title, string message, string errorCode)
        {
            Size = new Size(350, 450);
            Color = Colors.Transparent;

            _title = title;
            _message = message;
            _errorCode = errorCode;

            mainFrame = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 24,
                Padding = 0,
                HasShadow = true,
                Content = CreateErrorContent()
            };

            Content = mainFrame;
            AnimateIn();
        }

        private VerticalStackLayout CreateErrorContent()
        {
            var contentLayout = new VerticalStackLayout
            {
                Spacing = 0
            };

            // Error Header
            var headerGrid = new Grid
            {
                BackgroundColor = Color.FromArgb("#E74C3C"),
                Padding = new Thickness(25, 30, 25, 30)
            };

            var headerStack = new VerticalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Center
            };

            errorIcon = new Label
            {
                Text = GetErrorIcon(),
                FontSize = 60,
                HorizontalOptions = LayoutOptions.Center,
                Opacity = 0
            };

            titleLabel = new Label
            {
                Text = _title.ToUpper(),
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Colors.White
            };

            subtitleLabel = new Label
            {
                Text = "An error occurred during verification",
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Color.FromArgb("#FFE7E7"),
                Opacity = 0.9
            };

            headerStack.Children.Add(errorIcon);
            headerStack.Children.Add(titleLabel);
            headerStack.Children.Add(subtitleLabel);
            headerGrid.Children.Add(headerStack);

            contentLayout.Children.Add(headerGrid);

            // Scrollable Content
            var scrollView = new ScrollView
            {
                Padding = new Thickness(25, 20, 25, 25),
                VerticalScrollBarVisibility = ScrollBarVisibility.Never
            };

            var scrollContent = new VerticalStackLayout
            {
                Spacing = 20
            };

            // Error Message Card
            var messageFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#FFE7E7"),
                BorderColor = Color.FromArgb("#E74C3C"),
                CornerRadius = 12,
                HasShadow = false,
                Padding = 20
            };

            var messageStack = new VerticalStackLayout
            {
                Spacing = 12
            };

            messageStack.Children.Add(new Label
            {
                Text = "Error Details",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2D3436")
            });

            messageLabel = new Label
            {
                Text = _message,
                FontSize = 14,
                TextColor = Color.FromArgb("#2D3436"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            messageStack.Children.Add(messageLabel);
            messageFrame.Content = messageStack;
            scrollContent.Children.Add(messageFrame);

            // Error Code Card
            var errorCodeFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#F8F9FA"),
                BorderColor = Color.FromArgb("#E0E0E0"),
                CornerRadius = 12,
                HasShadow = false,
                Padding = 15
            };

            var errorCodeGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 12
            };

            errorCodeGrid.Children.Add(new Label
            {
                Text = "🔍",
                FontSize = 24,
                VerticalOptions = LayoutOptions.Start
            });

            var errorCodeStack = new VerticalStackLayout
            {
                Spacing = 4
            };

            errorCodeStack.Children.Add(new Label
            {
                Text = "Error Code",
                FontSize = 12,
                TextColor = Color.FromArgb("#636E72"),
                FontAttributes = FontAttributes.Bold
            });

            errorCodeLabel = new Label
            {
                Text = _errorCode,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#E74C3C"),
                FontFamily = "Courier"
            };

            errorCodeStack.Children.Add(errorCodeLabel);
            errorCodeGrid.Children.Add(errorCodeStack);
            Grid.SetColumn(errorCodeStack, 1);

            errorCodeFrame.Content = errorCodeGrid;
            scrollContent.Children.Add(errorCodeFrame);

            // Common Solutions
            var solutionsFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#E3F2FD"),
                BorderColor = Color.FromArgb("#74B9FF"),
                CornerRadius = 12,
                HasShadow = false,
                Padding = 20
            };

            var solutionsMainStack = new VerticalStackLayout
            {
                Spacing = 12
            };

            var solutionsHeaderStack = new HorizontalStackLayout
            {
                Spacing = 8
            };

            solutionsHeaderStack.Children.Add(new Label
            {
                Text = "💡",
                FontSize = 20,
                VerticalOptions = LayoutOptions.Start
            });

            solutionsHeaderStack.Children.Add(new Label
            {
                Text = "What you can try",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2D3436"),
                VerticalOptions = LayoutOptions.Center
            });

            solutionsMainStack.Children.Add(solutionsHeaderStack);

            solutionsContainer = new StackLayout
            {
                Spacing = 10
            };

            AddSolutions();

            solutionsMainStack.Children.Add(solutionsContainer);
            solutionsFrame.Content = solutionsMainStack;
            scrollContent.Children.Add(solutionsFrame);

            // Timestamp
            timestampLabel = new Label
            {
                Text = $"Error occurred at: {DateTime.Now:MMM dd, yyyy hh:mm tt}",
                FontSize = 12,
                TextColor = Color.FromArgb("#636E72"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            scrollContent.Children.Add(timestampLabel);

            // Action Buttons
            var buttonsStack = new VerticalStackLayout
            {
                Spacing = 12,
                Margin = new Thickness(0, 10, 0, 0)
            };

            tryAgainButton = new Button
            {
                Text = "Try Again",
                BackgroundColor = Color.FromArgb("#E74C3C"),
                TextColor = Colors.White,
                CornerRadius = 12,
                HeightRequest = 50,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold
            };
            tryAgainButton.Clicked += OnTryAgainClicked;

            contactSupportButton = new Button
            {
                Text = "Contact Support",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#74B9FF"),
                BorderWidth = 2,
                BorderColor = Color.FromArgb("#74B9FF"),
                CornerRadius = 12,
                HeightRequest = 50,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold
            };
            contactSupportButton.Clicked += OnContactSupportClicked;

            closeButton = new Button
            {
                Text = "Close",
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#E74C3C"),
                BorderWidth = 2,
                BorderColor = Color.FromArgb("#E74C3C"),
                CornerRadius = 12,
                HeightRequest = 50,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold
            };
            closeButton.Clicked += OnCloseClicked;

            buttonsStack.Children.Add(tryAgainButton);
            buttonsStack.Children.Add(contactSupportButton);
            buttonsStack.Children.Add(closeButton);

            scrollContent.Children.Add(buttonsStack);

            scrollView.Content = scrollContent;
            contentLayout.Children.Add(scrollView);

            return contentLayout;
        }

        private string GetErrorIcon()
        {
            return _errorCode switch
            {
                "NETWORK_ERROR" or "TIMEOUT_ERROR" => "📡",
                "AUTHENTICATION_ERROR" => "🔒",
                "VALIDATION_ERROR" => "⚠️",
                "SERVER_ERROR" => "🔧",
                "DATA_ERROR" => "📋",
                _ => "❌"
            };
        }

        private void AddSolutions()
        {
            solutionsContainer.Clear();

            List<string> solutions = GetSolutionsForErrorCode(_errorCode);

            foreach (var solution in solutions)
            {
                var solutionLayout = new HorizontalStackLayout
                {
                    Spacing = 8
                };

                solutionLayout.Children.Add(new Label
                {
                    Text = "•",
                    FontSize = 16,
                    TextColor = Color.FromArgb("#2D3436"),
                    VerticalOptions = LayoutOptions.Start
                });

                solutionLayout.Children.Add(new Label
                {
                    Text = solution,
                    FontSize = 14,
                    TextColor = Color.FromArgb("#2D3436"),
                    LineBreakMode = LineBreakMode.WordWrap,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                });

                solutionsContainer.Children.Add(solutionLayout);
            }
        }

        private List<string> GetSolutionsForErrorCode(string errorCode)
        {
            return errorCode switch
            {
                "NETWORK_ERROR" or "TIMEOUT_ERROR" => new List<string>
                {
                    "Check your internet connection",
                    "Try switching between WiFi and mobile data",
                    "Move to an area with better network coverage",
                    "Wait a moment and try again"
                },
                "AUTHENTICATION_ERROR" => new List<string>
                {
                    "Log out and log back in",
                    "Verify your account credentials",
                    "Check if your session has expired"
                },
                "VALIDATION_ERROR" => new List<string>
                {
                    "Double-check your token format",
                    "Ensure the token hasn't expired",
                    "Request a new token if needed",
                    "Contact the sender to verify the token"
                },
                "SERVER_ERROR" => new List<string>
                {
                    "Wait a few minutes and try again",
                    "The server may be undergoing maintenance",
                    "Contact support if the issue persists"
                },
                "DATA_ERROR" => new List<string>
                {
                    "Clear app cache and try again",
                    "Update the app to the latest version",
                    "Reinstall the app if problem continues"
                },
                "CANCELLED_ERROR" => new List<string>
                {
                    "Simply try the verification again",
                    "Ensure you complete the process"
                },
                _ => new List<string>
                {
                    "Try the verification process again",
                    "Check your internet connection",
                    "Contact support if the problem persists"
                }
            };
        }

        private async void AnimateIn()
        {
            mainFrame.TranslationY = 600;
            mainFrame.Opacity = 0;

            await Task.WhenAll(
                mainFrame.TranslateTo(0, 0, 500, Easing.SpringOut),
                mainFrame.FadeTo(1, 500)
            );

            // Animate error icon with shake effect
            await errorIcon.FadeTo(1, 300, Easing.CubicOut);

            // Shake animation
            for (int i = 0; i < 3; i++)
            {
                await errorIcon.TranslateTo(-10, 0, 50);
                await errorIcon.TranslateTo(10, 0, 50);
            }
            await errorIcon.TranslateTo(0, 0, 50);
        }

        private async Task CloseSheet()
        {
            await Task.WhenAll(
                mainFrame.TranslateTo(0, 600, 300, Easing.CubicIn),
                mainFrame.FadeTo(0, 300)
            );
            await this.CloseAsync();
        }

        private async void OnTryAgainClicked(object sender, EventArgs e)
        {
            try
            {
                await CloseSheet();
                await Task.Delay(300);

                // Navigate back to verification page
                var verifyPage = new VerifyToken();
                await Application.Current.MainPage.Navigation.PushModalAsync(verifyPage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error on try again: {ex.Message}");
                await CloseSheet();
            }
        }

        private async void OnContactSupportClicked(object sender, EventArgs e)
        {
            try
            {
                // Create support message with error details
                string supportMessage = $"Error Code: {_errorCode}\n" +
                                      $"Error: {_title}\n" +
                                      $"Message: {_message}\n" +
                                      $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                // Option 1: Email support
                var emailSubject = $"Verification Error - {_errorCode}";
                var emailBody = Uri.EscapeDataString(supportMessage);
                var emailUri = $"mailto:support@opxng.com?subject={emailSubject}&body={emailBody}";

                try
                {
                    await Launcher.OpenAsync(new Uri(emailUri));
                }
                catch
                {
                    // Option 2: Copy to clipboard and show alert
                    await Clipboard.SetTextAsync(supportMessage);

                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Support",
                            "Error details copied to clipboard.\n\n" +
                            "Please contact support at:\n" +
                            "support@opxng.com",
                            "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error contacting support: {ex.Message}");
            }
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await CloseSheet();
        }
    }
}