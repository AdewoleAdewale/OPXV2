using CommunityToolkit.Maui.Views;
using System.Collections.Concurrent;
using static Android.App.ActivityManager;

namespace Opx.Views;

public partial class ProfilePage : ContentPage
{
    #region Fields
    private ResetPassword _resetPasswordPopup;
    private readonly ConcurrentDictionary<string, bool> _operationStates = new();
    private const string PROFILE_IMAGE_KEY = "profile_image_cache";
    private const string LAST_REFRESH_KEY = "last_profile_refresh";
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private CancellationTokenSource _cancellationTokenSource;
    #endregion

    #region Properties
    public bool IsRefreshing { get; set; }
    private bool IsLoading
    {
        get => _operationStates.GetValueOrDefault("is_loading", false);
        set => _operationStates.AddOrUpdate("is_loading", value, (key, oldValue) => value);
    }
    #endregion

    #region Constructor
    public ProfilePage()
    {
        try
        {
            InitializeComponent();
            DashBoard.Attach(this, DashTab.Profile);   // curved gradient tab bar
            _cancellationTokenSource = new CancellationTokenSource();
            InitializeProfileAsync();
        }
        catch (Exception ex)
        {
            HandleCriticalError("Initialization Error", ex);
        }
    }
    #endregion

    #region Initialization Methods
    private async void InitializeProfileAsync()
    {
        try
        {
            await SetLoadingState(true, "Initializing profile...");

            // Initialize profile data
            await InitializeProfileData();

            // Load cached profile image
            await LoadCachedProfileImage();

            // Update progress bar
            await UpdateProfileProgress();

            // Initialize popup
            _resetPasswordPopup = new ResetPassword();
        }
        catch (Exception ex)
        {
            await HandleError("Profile Initialization", ex);
        }
        finally
        {
            await SetLoadingState(false);
        }
    }

    private async Task InitializeProfileData()
    {
        try
        {
            await Task.Run(() =>
            {
                // Set user data with null checks and validation
                var fullName = !string.IsNullOrWhiteSpace(LoginPage.myfullname)
                    ? LoginPage.myfullname.Trim()
                    : "User Name";

                var email = !string.IsNullOrWhiteSpace(LoginPage.myemail)
                    ? LoginPage.myemail.Trim().ToLowerInvariant()
                    : "user@example.com";

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Fullname.Text = fullName;
                    loggedemail.Text = email;
                });
            });
        }
        catch (Exception ex)
        {
            // Set default values if error occurs
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Fullname.Text = "User Name";
                loggedemail.Text = "user@example.com";
            });
            throw new InvalidOperationException("Failed to initialize profile data", ex);
        }
    }

    private async Task LoadCachedProfileImage()
    {
        try
        {
            var cachedImagePath = await SecureStorage.GetAsync(PROFILE_IMAGE_KEY);

            if (!string.IsNullOrEmpty(cachedImagePath) && File.Exists(cachedImagePath))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ProfileImage.Source = ImageSource.FromFile(cachedImagePath);
                });
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleError("Image Access Denied", ex, "Unable to access cached profile image.");
        }
        catch (Exception ex)
        {
            await HandleError("Image Loading Error", ex);
        }
    }

    private async Task UpdateProfileProgress()
    {
        try
        {
            await Task.Run(async () =>
            {
                var progress = await CalculateProfileCompleteness();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ProgressText.Text = $"{progress}% Complete";

                    // Animate progress bar
                    var targetWidth = (progress / 100.0) * 200;
                    ProgressBar.WidthRequest = targetWidth;
                });
            });
        }
        catch (Exception ex)
        {
            await HandleError("Progress Update Error", ex);
        }
    }

    private async Task<int> CalculateProfileCompleteness()
    {
        try
        {
            int progress = 0;

            // Check profile data completion
            if (!string.IsNullOrWhiteSpace(LoginPage.myfullname)) progress += 30;
            if (!string.IsNullOrWhiteSpace(LoginPage.myemail)) progress += 30;

            // Check profile image
            var cachedImagePath = await SecureStorage.GetAsync(PROFILE_IMAGE_KEY);
            if (!string.IsNullOrEmpty(cachedImagePath) && File.Exists(cachedImagePath)) progress += 25;

            // Base verification points
            progress += 15;

            return Math.Min(progress, 100);
        }
        catch
        {
            return 50; // Default progress
        }
    }
    #endregion

    #region Image Upload Methods
    private async void UploadProfileImage_Tapped(object sender, TappedEventArgs e)
    {
        if (!await PreventMultipleClicks("upload_image")) return;

        try
        {
            await SetLoadingState(true, "Preparing image upload...");

            // Request permissions first
            if (!await RequestCameraPermissions())
            {
                return;
            }

            var action = await DisplayActionSheet(
                "Select Profile Picture",
                "Cancel",
                null,
                "Take Photo",
                "Choose from Gallery"
            );

            switch (action)
            {
                case "Take Photo":
                    await CapturePhoto();
                    break;
                case "Choose from Gallery":
                    await PickPhoto();
                    break;
            }
        }
        catch (Exception ex)
        {
            await HandleError("Image Upload Error", ex);
        }
        finally
        {
            await SetLoadingState(false);
            CompleteOperation("upload_image");
        }
    }

    private async Task<bool> RequestCameraPermissions()
    {
        try
        {
            var cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
            var storageStatus = await Permissions.RequestAsync<Permissions.StorageRead>();

            if (cameraStatus != PermissionStatus.Granted)
            {
                await DisplayAlert(
                    "Permission Required",
                    "Camera permission is required to take photos. Please enable it in settings.",
                    "OK"
                );
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            await HandleError("Permission Error", ex, "Unable to request permissions.");
            return false;
        }
    }

    private async Task CapturePhoto()
    {
        try
        {
            await SetLoadingState(true, "Opening camera...");

            var photo = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Profile Photo"
            });

            if (photo != null)
            {
                await ProcessSelectedImage(photo);
            }
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("Not Supported", "Camera is not supported on this device.", "OK");
        }
        catch (Exception ex)
        {
            await HandleError("Photo Capture Error", ex, "Failed to capture photo.");
        }
    }

    private async Task PickPhoto()
    {
        try
        {
            await SetLoadingState(true, "Opening gallery...");

            var photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select Profile Photo"
            });

            if (photo != null)
            {
                await ProcessSelectedImage(photo);
            }
        }
        catch (Exception ex)
        {
            await HandleError("Photo Selection Error", ex, "Failed to select photo.");
        }
    }

    private async Task ProcessSelectedImage(FileResult photo)
    {
        if (photo == null) return;

        try
        {
            await SetLoadingState(true, "Processing image...");

            // Validate file size (max 5MB)
            using var sourceStream = await photo.OpenReadAsync();
            if (sourceStream.Length > 5 * 1024 * 1024)
            {
                await DisplayAlert("File Too Large", "Please select an image smaller than 5MB.", "OK");
                return;
            }

            // Create cache directory and file path
            var fileName = $"profile_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            var cacheDir = Path.Combine(FileSystem.CacheDirectory, "ProfileImages");

            // Ensure directory exists
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            var cachedFilePath = Path.Combine(cacheDir, fileName);

            // Reset stream position
            sourceStream.Position = 0;

            // Copy and resize image
            await CopyAndResizeImage(sourceStream, cachedFilePath);

            // Update UI on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ProfileImage.Source = ImageSource.FromFile(cachedFilePath);
            });

            // Save to secure storage
            await SecureStorage.SetAsync(PROFILE_IMAGE_KEY, cachedFilePath);

            // Clean up old images
            await CleanupOldCachedImages(cacheDir, fileName);

            // Update progress
            await UpdateProfileProgress();

            await DisplayAlert("Success", "Profile picture updated successfully!", "OK");
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleError("Access Denied", ex, "Unable to save image. Check storage permissions.");
        }
        catch (DirectoryNotFoundException ex)
        {
            await HandleError("Storage Error", ex, "Unable to create image cache directory.");
        }
        catch (Exception ex)
        {
            await HandleError("Image Processing Error", ex, "Failed to process selected image.");
        }
    }

    private async Task CopyAndResizeImage(Stream sourceStream, string targetPath)
    {
        try
        {
            using var targetStream = File.Create(targetPath);
            await sourceStream.CopyToAsync(targetStream);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to copy image", ex);
        }
    }

    private async Task CleanupOldCachedImages(string cacheDir, string currentFileName)
    {
        try
        {
            await Task.Run(() =>
            {
                var files = Directory.GetFiles(cacheDir, "profile_*.jpg");
                foreach (var file in files)
                {
                    if (Path.GetFileName(file) != currentFileName)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Ignore individual file deletion errors
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            // Log but don't show error for cleanup failures
            System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
        }
    }
    #endregion

    #region Navigation and Button Handlers
    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        if (!await PreventMultipleClicks("back_button")) return;

        try
        {
            await SafeNavigateBack();
        }
        catch (Exception ex)
        {
            await HandleError("Navigation Error", ex);
        }
        finally
        {
            CompleteOperation("back_button");
        }
    }



    private async void UpdatePassword_Tapped(object sender, TappedEventArgs e)
    {
        if (!await PreventMultipleClicks("update_password")) return;

        try
        {
            await SetLoadingState(true, "Opening change password...");

            // Signed-in users change their password with the current one (POST /AuthAccount/ChangePassword).
            // The OTP-based ResetPassword popup is only for the forgot-password flow.
            await Navigation.PushModalAsync(new ChangePasswordPage());
        }
        catch (InvalidOperationException ex)
        {
            await HandleError("Password Update Error", ex, "Unable to open password reset.");
        }
        catch (Exception ex)
        {
            await HandleError("Password Update Error", ex);
        }
        finally
        {
            await SetLoadingState(false);
            CompleteOperation("update_password");
        }
    }

    private async void Settings_Clicked(object sender, EventArgs e)
    {
        if (!await PreventMultipleClicks("settings")) return;

        try
        {
            await DisplayAlert("Settings", "Settings functionality coming soon!", "OK");
        }
        catch (Exception ex)
        {
            await HandleError("Settings Error", ex);
        }
        finally
        {
            CompleteOperation("settings");
        }
    }

    private async void Referral_Tapped(object sender, TappedEventArgs e)
    {
        try
        {
            await Navigation.PushModalAsync(new Views.Referral());
        }
        catch (Exception ex)
        {
            await HandleError("Navigation Error", ex, "Unable to open the referral page.");
        }
    }

    private async void RecipientAccount_Tapped(object sender, TappedEventArgs e)
    {
        if (!await PreventMultipleClicks("recipient_account")) return;

        try
        {
            await SetLoadingState(true, "Opening recipient account...");
            await Navigation.PushModalAsync(new Views.CurrentBalance());
        }
        catch (InvalidOperationException ex)
        {
            await HandleError("Navigation Error", ex, "Unable to open recipient account.");
        }
        catch (Exception ex)
        {
            await HandleError("Navigation Error", ex);
        }
        finally
        {
            await SetLoadingState(false);
            CompleteOperation("recipient_account");
        }
    }

    private async void Logout_Tapped(object sender, TappedEventArgs e)
    {
        if (!await PreventMultipleClicks("logout")) return;

        try
        {
            bool answer = await DisplayAlert(
                "Confirm Logout",
                "Are you sure you want to logout? You will need to login again to access your account.",
                "Yes, Logout",
                "Cancel"
            );

            if (answer)
            {
                await PerformLogout();
            }
        }
        catch (Exception ex)
        {
            await HandleError("Logout Error", ex);
        }
        finally
        {
            CompleteOperation("logout");
        }
    }

    private async void RefreshView_Refreshing(object sender, EventArgs e)
    {
        try
        {
            IsRefreshing = true;
            await RefreshProfileData();
        }
        catch (Exception ex)
        {
            await HandleError("Refresh Error", ex);
        }
        finally
        {
            IsRefreshing = false;
            RefreshView.IsRefreshing = false;
        }
    }
    #endregion

    #region Core Operations
    private async Task RefreshProfileData()
    {
        try
        {
            // Refresh profile data
            await InitializeProfileData();

            // Reload cached image
            await LoadCachedProfileImage();

            // Update progress
            await UpdateProfileProgress();

            // Save last refresh time
            await SecureStorage.SetAsync(LAST_REFRESH_KEY, DateTime.Now.ToString("O"));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to refresh profile data", ex);
        }
    }

    private async Task PerformLogout()
    {
        try
        {
            await SetLoadingState(true, "Logging out...");

            // Cancel any ongoing operations
            _cancellationTokenSource?.Cancel();

            // Clear the shared HttpClient session cookie so /ChangePassword can't be replayed.
            Opx.Services.SessionStore.Clear();   // removes the saved session (and the auth cookie)

            // Clear authentication data
            SecureStorage.Remove("auth_token");
            Preferences.Remove("user_id");
            SecureStorage.Remove("user_session");

            // Clear cached profile image
            SecureStorage.Remove(PROFILE_IMAGE_KEY);
            SecureStorage.Remove(LAST_REFRESH_KEY);

            // Clear profile image cache directory
            await ClearImageCache();

            // Clear operation states
            _operationStates.Clear();

            // Clear in-memory login state
            LoginPage.myemail = LoginPage.myfullname = LoginPage.mytoken = null;
            LoginPage.availableBalance = LoginPage.ledgerBalance = null;
            LoginPage.completedTransactions = LoginPage.totalTransactions = null;
            LoginPage.pendingTransactions = LoginPage.disputes = null;
            LoginPage.accountName = LoginPage.accountNumber = LoginPage.bankName = null;

            // Navigate to login page
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new NavigationPage(new LoginPage());
            });
        }
        catch (Exception ex)
        {
            await HandleError("Logout Process Error", ex, "An error occurred during logout. Some data may not be cleared.");
        }
        finally
        {
            await SetLoadingState(false);
        }
    }

    private async Task ClearImageCache()
    {
        try
        {
            await Task.Run(() =>
            {
                var cacheDir = Path.Combine(FileSystem.CacheDirectory, "ProfileImages");
                if (Directory.Exists(cacheDir))
                {
                    Directory.Delete(cacheDir, true);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cache cleanup error: {ex.Message}");
        }
    }

    private async Task SafeNavigateBack()
    {
        try
        {
            await _semaphore.WaitAsync();


            // Navigate to the dashboard
            await MainThread.InvokeOnMainThreadAsync(() => DashBoard.GoHomeAsync());

        }
        catch (InvalidOperationException)
        {
            // Fallback navigation
            await MainThread.InvokeOnMainThreadAsync(() => DashBoard.GoHomeAsync());
        }
        finally
        {
            _semaphore.Release();
        }
    }
    #endregion

    #region State Management
    private async Task SetLoadingState(bool isLoading, string message = "Loading...")
    {
        try
        {
            IsLoading = isLoading;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingOverlay.IsVisible = isLoading;
                LoadingIndicator.IsRunning = isLoading;
                LoadingText.Text = message;
                MainScrollView.IsEnabled = !isLoading;

                // Disable buttons during loading
                BackButton.IsEnabled = !isLoading;
            });

            if (isLoading)
            {
                await Task.Delay(100); // Brief delay for UI update
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Loading state error: {ex.Message}");
        }
    }

    private async Task<bool> PreventMultipleClicks(string operationKey)
    {
        try
        {
            if (_operationStates.GetValueOrDefault(operationKey, false))
            {
                return false; // Operation already in progress
            }

            _operationStates.AddOrUpdate(operationKey, true, (key, oldValue) => true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void CompleteOperation(string operationKey)
    {
        try
        {
            _operationStates.AddOrUpdate(operationKey, false, (key, oldValue) => false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Complete operation error: {ex.Message}");
        }
    }
    #endregion

    #region Error Handling
    private async Task HandleError(string title, Exception ex, string customMessage = null)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"{title}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

            var message = customMessage ?? GetUserFriendlyErrorMessage(ex);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await DisplayAlert(title, message, "OK");
                }
                catch
                {
                    // If DisplayAlert fails, just debug log
                    System.Diagnostics.Debug.WriteLine($"Failed to show error alert: {title} - {message}");
                }
            });
        }
        catch (Exception criticalEx)
        {
            System.Diagnostics.Debug.WriteLine($"Critical error in HandleError: {criticalEx.Message}");
        }
    }

    private void HandleCriticalError(string title, Exception ex)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"CRITICAL - {title}: {ex.Message}");

            // Set basic fallback values
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    Fullname.Text = "User Name";
                    loggedemail.Text = "user@example.com";
                    LoadingOverlay.IsVisible = false;
                }
                catch
                {
                    // Even fallback failed - log only
                    System.Diagnostics.Debug.WriteLine("Critical fallback failed");
                }
            });
        }
        catch (Exception criticalEx)
        {
            System.Diagnostics.Debug.WriteLine($"Critical error handling failed: {criticalEx.Message}");
        }
    }

    private string GetUserFriendlyErrorMessage(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => "Access denied. Please check your permissions.",
            FileNotFoundException => "Required file not found. Please try again.",
            DirectoryNotFoundException => "Storage location not found. Please try again.",
            InvalidOperationException => "This operation cannot be completed right now. Please try again.",
            TimeoutException => "The operation timed out. Please check your connection and try again.",
            _ => "An unexpected error occurred. Please try again."
        };
    }
    #endregion

    #region Lifecycle Methods
    protected override bool OnBackButtonPressed()
    {
        try
        {
            if (IsLoading) return true; // Prevent back navigation during loading

            Task.Run(async () => await SafeNavigateBack());
            return true;
        }
        catch (Exception ex)
        {
            HandleCriticalError("Back Button Error", ex);
            return false;
        }
    }

    protected override void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            // Check if refresh is needed (every 5 minutes)
            CheckAndRefreshIfNeeded();
        }
        catch (Exception ex)
        {
            HandleCriticalError("Page Appearance Error", ex);
        }
    }

    protected override void OnDisappearing()
    {
        try
        {
            base.OnDisappearing();
            SetLoadingState(false);

            // Clear any pending operations
            foreach (var key in _operationStates.Keys.ToList())
            {
                CompleteOperation(key);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Page disappearing error: {ex.Message}");
        }
    }

    private async void CheckAndRefreshIfNeeded()
    {
        try
        {
            var lastRefreshStr = await SecureStorage.GetAsync(LAST_REFRESH_KEY);
            if (DateTime.TryParse(lastRefreshStr, out var lastRefresh))
            {
                if (DateTime.Now - lastRefresh > TimeSpan.FromMinutes(5))
                {
                    await RefreshProfileData();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Auto-refresh check error: {ex.Message}");
        }
    }

    protected override void OnBindingContextChanged()
    {
        try
        {
            base.OnBindingContextChanged();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Binding context error: {ex.Message}");
        }
    }
    #endregion

    #region Disposal
    public void Dispose()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _semaphore?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Disposal error: {ex.Message}");
        }
    }
    #endregion

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        if (!await PreventMultipleClicks("refferalpage")) return;

        try
        {
            await SetLoadingState(true, "Opening referral Page...");
            await Navigation.PushModalAsync(new Views.Referral());
        }
        catch (InvalidOperationException ex)
        {
            await HandleError("Navigation Error", ex, "Unable to open referral Page.");
        }
        catch (Exception ex)
        {
            await HandleError("Navigation Error", ex);
        }
        finally
        {
            await SetLoadingState(false);
            CompleteOperation("refferalpage");
        }
    }
}