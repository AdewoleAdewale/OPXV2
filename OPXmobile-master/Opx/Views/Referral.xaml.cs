using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;

namespace Opx.Views;

public partial class Referral : ContentPage, IDisposable
{
    #region Fields
    private bool _isDisposed = false;
    private bool _isLoading = false;
    private ReferralViewModel _viewModel;
    private CancellationTokenSource _cancellationTokenSource;
    private List<ReferralData> _originalReferrals = new();
    private readonly object _lockObject = new object();
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const int TIMEOUT_SECONDS = 30;
    #endregion

    #region Data Models
    public class ReferralData : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _fullName = string.Empty;
        private string _email = string.Empty;
        private string _phoneNumber = string.Empty;
        private DateTime _joinedDate = DateTime.Now;

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string FullName
        {
            get => _fullName;
            set
            {
                _fullName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(InitialLetter));
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                _phoneNumber = value;
                OnPropertyChanged();
            }
        }

        public DateTime JoinedDateValue
        {
            get => _joinedDate;
            set
            {
                _joinedDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(JoinedDate));
            }
        }

        public string JoinedDate => _joinedDate.ToString("MMM dd, yyyy");

        public string InitialLetter => string.IsNullOrEmpty(_fullName)
            ? "U"
            : _fullName.Substring(0, 1).ToUpper();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ReferralInfo : INotifyPropertyChanged
    {
        private string _referralCode = string.Empty;
        private string _referralLink = string.Empty;
        private int _totalReferrals = 0;

        public string ReferralCode
        {
            get => _referralCode;
            set
            {
                _referralCode = value;
                OnPropertyChanged();
            }
        }

        public string ReferralLink
        {
            get => _referralLink;
            set
            {
                _referralLink = value;
                OnPropertyChanged();
            }
        }

        public int TotalReferrals
        {
            get => _totalReferrals;
            set
            {
                _totalReferrals = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ReferralViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ReferralData> _referrals = new();
        private ObservableCollection<ReferralData> _filteredReferrals = new();
        private ReferralInfo _referralInfo = new();
        private readonly object _lockObject = new object();

        public ObservableCollection<ReferralData> Referrals
        {
            get
            {
                lock (_lockObject)
                {
                    return _referrals;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _referrals = value ?? new ObservableCollection<ReferralData>();
                    OnPropertyChanged();
                    UpdateFilteredReferrals();
                }
            }
        }

        public ObservableCollection<ReferralData> FilteredReferrals
        {
            get
            {
                lock (_lockObject)
                {
                    return _filteredReferrals;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _filteredReferrals = value ?? new ObservableCollection<ReferralData>();
                    OnPropertyChanged();
                }
            }
        }

        public ReferralInfo ReferralInfo
        {
            get => _referralInfo;
            set
            {
                _referralInfo = value ?? new ReferralInfo();
                OnPropertyChanged();
            }
        }

        private void UpdateFilteredReferrals()
        {
            try
            {
                lock (_lockObject)
                {
                    FilteredReferrals = new ObservableCollection<ReferralData>(Referrals ?? new ObservableCollection<ReferralData>());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateFilteredReferrals error: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnPropertyChanged error: {ex.Message}");
            }
        }
    }
    #endregion

    public Referral()
    {
        try
        {
            InitializeComponent();
            InitializeViewModel();
            InitializeCancellationToken();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Referral initialization error: {ex.Message}");
        }
    }

    private void InitializeViewModel()
    {
        try
        {
            _viewModel = new ReferralViewModel();
            BindingContext = _viewModel;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ViewModel initialization error: {ex.Message}");
            _viewModel = new ReferralViewModel();
            BindingContext = _viewModel;
        }
    }

    private void InitializeCancellationToken()
    {
        try
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CancellationToken initialization error: {ex.Message}");
            _cancellationTokenSource = new CancellationTokenSource();
        }
    }

    #region UI Event Handlers
    private void BackButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed) return;
            NavigateBack();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Back button error: {ex.Message}");
        }
    }

    private async void RefreshButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed) return;
            await LoadReferralsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Refresh button error: {ex.Message}");
            ShowErrorSnackbar("Failed to refresh referrals");
        }
    }

    private void SearchEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (_isDisposed) return;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
        }
    }

    private void ClearSearchButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            SearchEntry.Text = string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clear search error: {ex.Message}");
        }
    }

    private async void OnReferralCodeTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (_isDisposed || _viewModel?.ReferralInfo == null) return;

            var code = _viewModel.ReferralInfo.ReferralCode;
            if (!string.IsNullOrEmpty(code))
            {
                await Clipboard.SetTextAsync(code);
                ShowInfoSnackbar("Referral code copied to clipboard!");

                if (sender is Label label)
                {
                    var originalColor = label.TextColor;
                    label.TextColor = Colors.Red;
                    await Task.Delay(500);
                    label.TextColor = originalColor;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Copy referral code error: {ex.Message}");
            ShowErrorSnackbar("Failed to copy referral code");
        }
    }

    private async void OnCopyLinkClicked(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed || _viewModel?.ReferralInfo == null) return;

            var link = _viewModel.ReferralInfo.ReferralLink;
            if (!string.IsNullOrEmpty(link))
            {
                await Clipboard.SetTextAsync(link);
                ShowInfoSnackbar("Referral link copied to clipboard!");
            }
            else
            {
                ShowErrorSnackbar("No referral link available");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Copy link error: {ex.Message}");
            ShowErrorSnackbar("Failed to copy referral link");
        }
    }

    private async void OnCopyReferralInfo(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.CommandParameter is ReferralData referral)
            {
                var info = $"Name: {referral.FullName}\nEmail: {referral.Email}\nPhone: {referral.PhoneNumber}\nJoined: {referral.JoinedDate}";
                await Clipboard.SetTextAsync(info);
                ShowInfoSnackbar("Referral information copied!");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Copy referral info error: {ex.Message}");
            ShowErrorSnackbar("Failed to copy referral information");
        }
    }

    private async void RetryButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed) return;
            await LoadReferralsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Retry button error: {ex.Message}");
        }
    }
    #endregion

    #region API and Data Loading
    private async Task LoadReferralsAsync()
    {
        if (_isDisposed || _cancellationTokenSource?.Token.IsCancellationRequested == true)
            return;

        var retryCount = 0;
        while (retryCount < MAX_RETRY_ATTEMPTS)
        {
            try
            {
                _isLoading = true;
                ShowLoadingState(true);

                if (!CheckNetworkConnectivity())
                {
                    ShowErrorSnackbar("No internet connection available");
                    ShowErrorState(true);
                    return;
                }

                await FetchReferralsFromServer();
                break;
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Referral loading cancelled");
                break;
            }
            catch (HttpRequestException httpEx)
            {
                retryCount++;
                System.Diagnostics.Debug.WriteLine($"HTTP error (attempt {retryCount}): {httpEx.Message}");

                if (retryCount >= MAX_RETRY_ATTEMPTS)
                {
                    ShowErrorSnackbar($"Network error after {MAX_RETRY_ATTEMPTS} attempts. Please check your connection.");
                    ShowErrorState(true);
                    break;
                }

                await Task.Delay(1000 * retryCount, _cancellationTokenSource?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API loading error: {ex.Message}");
                ShowErrorSnackbar($"An error occurred: {ex.Message}");
                ShowErrorState(true);
                break;
            }
            finally
            {
                _isLoading = false;
                ShowLoadingState(false);
            }
        }
    }

    private bool CheckNetworkConnectivity()
    {
        try
        {
            var connectivity = Connectivity.Current;
            return connectivity?.NetworkAccess == NetworkAccess.Internet;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Connectivity check error: {ex.Message}");
            return true; // Assume connected if check fails
        }
    }

    private async Task FetchReferralsFromServer()
    {
        if (_isDisposed || _cancellationTokenSource?.Token.IsCancellationRequested == true)
            return;

        HttpClient client = null;

        try
        {
            var userEmail = LoginPage.myemail;
            if (string.IsNullOrEmpty(userEmail))
            {
                ShowErrorSnackbar("User email not available");
                ShowErrorState(true);
                return;
            }

            string url = $"https://opxng.com/api/referralsapi?email={Uri.EscapeDataString(userEmail)}";

            // ✅ Custom handler to bypass SSL errors (dev/test only)
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS)
            };

            // Ensure TLS 1.2+
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using var response = await client.GetAsync(url, _cancellationTokenSource?.Token ?? CancellationToken.None);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"API returned status code: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                ShowErrorSnackbar("Empty response from server");
                ShowErrorState(true);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"API Response: {json}");
            await ProcessApiResponse(json);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("Fetch operation cancelled");
        }
        catch (JsonException jsonEx)
        {
            System.Diagnostics.Debug.WriteLine($"JSON parsing error: {jsonEx.Message}");
            ShowErrorSnackbar("Invalid response format from server");
            ShowErrorState(true);
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP error: {httpEx.Message}");
            ShowErrorSnackbar("Network error. Please check your internet connection.");
            ShowErrorState(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fetch Error: {ex.Message}");
            ShowErrorSnackbar($"Failed to fetch referrals: {ex.Message}");
            ShowErrorState(true);
        }
        finally
        {
            client?.Dispose();
        }
    }

    private async Task ProcessApiResponse(string json)
    {
        try
        {
            var apiResponse = JsonConvert.DeserializeObject<ReferralApiResponse>(json);

            if (apiResponse == null || !apiResponse.Success)
            {
                ShowErrorSnackbar("Failed to retrieve referral data");
                ShowErrorState(true);
                return;
            }

            await UpdateUIWithReferralData(apiResponse);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Response processing error: {ex.Message}");
            throw;
        }
    }

    private async Task UpdateUIWithReferralData(ReferralApiResponse apiResponse)
    {
        if (_isDisposed || apiResponse == null || _viewModel == null)
            return;

        try
        {
            // Validate response data
            if (string.IsNullOrEmpty(apiResponse.ReferralCode))
            {
                ShowErrorSnackbar("Invalid referral data received");
                ShowErrorState(true);
                return;
            }

            // Process referrals list first
            await ProcessReferralsList(apiResponse.Referrals);

            // Update UI on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isDisposed || _viewModel == null)
                    return;

                try
                {
                    // Update referral info model
                    _viewModel.ReferralInfo = new ReferralInfo
                    {
                        ReferralCode = apiResponse.ReferralCode ?? string.Empty,
                        ReferralLink = apiResponse.ReferralLink ?? string.Empty,
                        TotalReferrals = apiResponse.TotalReferrals
                    };

                    // Update UI controls through binding or direct assignment
                    UpdateReferralCodeUI(apiResponse.ReferralCode);
                    UpdateReferralLinkUI(apiResponse.ReferralLink);
                    UpdateTotalReferralsUI(apiResponse.TotalReferrals);

                    // Apply filters and update state
                    ApplyFilters();
                    UpdateUIState();

                    // Show footer if there are referrals
                    if (FooterSection != null)
                        FooterSection.IsVisible = _originalReferrals.Count > 0;

                    System.Diagnostics.Debug.WriteLine($"Successfully loaded {_viewModel.Referrals.Count} referrals");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UI Update Error: {ex.Message}");
                    ShowErrorSnackbar("Failed to update referral interface");
                    ShowErrorState(true);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateUIWithReferralData Error: {ex.Message}");
            ShowErrorSnackbar("Failed to process referral data");
            ShowErrorState(true);
        }
    }

    private async Task ProcessReferralsList(List<ReferralItem> referralItems)
    {
        try
        {
            lock (_lockObject)
            {
                _originalReferrals.Clear();

                if (referralItems != null && referralItems.Count > 0)
                {
                    foreach (var item in referralItems)
                    {
                        if (_isDisposed || item == null)
                            continue;

                        try
                        {
                            _originalReferrals.Add(new ReferralData
                            {
                                Id = item.Id ?? string.Empty,
                                FullName = item.FullName ?? "Unknown",
                                Email = item.Email ?? string.Empty,
                                PhoneNumber = item.PhoneNumber ?? string.Empty,
                                JoinedDateValue = DateTime.Now
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing referral item: {ex.Message}");
                            continue;
                        }
                    }
                }
            }

            // Update ViewModel collection on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isDisposed || _viewModel == null)
                    return;

                try
                {
                    _viewModel.Referrals.Clear();
                    foreach (var referral in _originalReferrals)
                    {
                        if (_isDisposed)
                            break;
                        _viewModel.Referrals.Add(referral);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating referrals collection: {ex.Message}");
                }
            });

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessReferralsList Error: {ex.Message}");
        }
    }

    private void UpdateReferralCodeUI(string code)
    {
        try
        {
            if (ReferralCodeLabel != null)
            {
                ReferralCodeLabel.Text = !string.IsNullOrEmpty(code) ? code : "N/A";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateReferralCodeUI Error: {ex.Message}");
        }
    }

    private void UpdateReferralLinkUI(string link)
    {
        try
        {
            if (ReferralLinkEntry != null)
            {
                ReferralLinkEntry.Text = !string.IsNullOrEmpty(link) ? link : string.Empty;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateReferralLinkUI Error: {ex.Message}");
        }
    }

    private void UpdateTotalReferralsUI(int total)
    {
        try
        {
            if (TotalReferralsLabel != null)
            {
                TotalReferralsLabel.Text = total.ToString();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateTotalReferralsUI Error: {ex.Message}");
        }
    }

    private void ApplyFilters()
    {
        try
        {
            lock (_lockObject)
            {
                if (_originalReferrals == null || _originalReferrals.Count == 0)
                {
                    UpdateUIState();
                    return;
                }

                var filteredResults = _originalReferrals.AsEnumerable();

                // Search filter
                var searchText = SearchEntry?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var searchTerm = searchText.ToLower();
                    filteredResults = filteredResults.Where(r =>
                        r != null && (
                            (r.FullName?.ToLower().Contains(searchTerm) ?? false) ||
                            (r.Email?.ToLower().Contains(searchTerm) ?? false) ||
                            (r.PhoneNumber?.ToLower().Contains(searchTerm) ?? false)
                        ));
                }

                var filteredList = filteredResults.Where(r => r != null).OrderByDescending(r => r.JoinedDateValue).ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!_isDisposed && _viewModel != null)
                    {
                        try
                        {
                            _viewModel.FilteredReferrals.Clear();
                            foreach (var item in filteredList)
                            {
                                if (_isDisposed) break;
                                _viewModel.FilteredReferrals.Add(item);
                            }

                            UpdateUIState();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"UI update during filter error: {ex.Message}");
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Filter error: {ex.Message}");
        }
    }
    #endregion

    #region UI State Management
    private void ShowLoadingState(bool show)
    {
        if (_isDisposed) return;

        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isDisposed) return;

                try
                {
                    if (LoadingSection != null)
                        LoadingSection.IsVisible = show;
                    if (LoadingIndicator != null)
                    {
                        LoadingIndicator.IsVisible = show;
                        LoadingIndicator.IsRunning = show;
                    }
                    if (ReferralsCollectionView != null)
                        ReferralsCollectionView.IsVisible = !show;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Loading state UI update error: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Loading state error: {ex.Message}");
        }
    }

    private void ShowErrorState(bool show)
    {
        if (_isDisposed) return;

        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isDisposed) return;

                try
                {
                    if (ErrorStateContainer != null)
                        ErrorStateContainer.IsVisible = show;
                    if (ReferralsCollectionView != null)
                        ReferralsCollectionView.IsVisible = !show;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error state UI update error: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error state error: {ex.Message}");
        }
    }

    private void ShowEmptyState(bool show)
    {
        if (_isDisposed) return;

        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isDisposed) return;

                try
                {
                    if (EmptyStateContainer != null)
                        EmptyStateContainer.IsVisible = show;
                    if (ReferralsCollectionView != null)
                        ReferralsCollectionView.IsVisible = !show;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Empty state UI update error: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Empty state error: {ex.Message}");
        }
    }

    private void UpdateUIState()
    {
        try
        {
            if (_isDisposed) return;

            var hasReferrals = _viewModel?.FilteredReferrals?.Count > 0;

            ShowLoadingState(false);
            ShowErrorState(false);
            ShowEmptyState(!hasReferrals);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UI state update error: {ex.Message}");
        }
    }
    #endregion

    #region Navigation
    private bool NavigateBack()
    {
        try
        {
            if (_isDisposed) return false;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var navStack = Navigation?.NavigationStack;
                    if (navStack != null && navStack.Count > 1)
                    {
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        Application.Current.MainPage = new DashBoard();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                    try
                    {
                        Application.Current.MainPage = new DashBoard();
                    }
                    catch { }
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NavigateBack error: {ex.Message}");
            return false;
        }
    }

    protected override bool OnBackButtonPressed()
    {
        try
        {
            if (_isDisposed) return false;

            _ = Task.Run(async () =>
            {
                try
                {
                    NavigateBack();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Back button navigation error: {ex.Message}");
                }
            });
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Back button error: {ex.Message}");
            return false;
        }
    }

    private async void OnShareButtonClicked(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed || _viewModel?.ReferralInfo == null) return;

            await ShareReferralLink();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Share button error: {ex.Message}");
            ShowErrorSnackbar("Failed to open share options");
        }
    }

    private async void OnShareWhatsApp(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed || _viewModel?.ReferralInfo == null) return;

            var message = GetShareMessage();
            var encodedMessage = Uri.EscapeDataString(message);
            var whatsappUrl = $"https://wa.me/?text={encodedMessage}";

            await Launcher.OpenAsync(new Uri(whatsappUrl));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WhatsApp share error: {ex.Message}");
            ShowErrorSnackbar("Unable to open WhatsApp. Please ensure it's installed.");
        }
    }

    private async void OnShareSMS(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed || _viewModel?.ReferralInfo == null) return;

            var message = GetShareMessage();
            var sms = new SmsMessage(message, new List<string>());
            await Sms.ComposeAsync(sms);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SMS share error: {ex.Message}");
            ShowErrorSnackbar("Unable to open SMS application");
        }
    }

    private async void OnShareEmail(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed || _viewModel?.ReferralInfo == null) return;

            var message = GetShareMessage();
            var email = new EmailMessage
            {
                Subject = "Join me on OPX!",
                Body = message,
                To = new List<string>()
            };

            await Email.ComposeAsync(email);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Email share error: {ex.Message}");
            ShowErrorSnackbar("Unable to open email application");
        }
    }

    private async void OnShareMore(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed || _viewModel?.ReferralInfo == null) return;

            await ShareReferralLink();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Share more error: {ex.Message}");
            ShowErrorSnackbar("Failed to open share options");
        }
    }

    private async Task ShareReferralLink()
    {
        try
        {
            var message = GetShareMessage();

            await Share.RequestAsync(new ShareTextRequest
            {
                Text = message,
                Title = "Share Referral Link"
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Share error: {ex.Message}");
            ShowErrorSnackbar("Failed to share referral link");
        }
    }

    private string GetShareMessage()
    {
        var referralCode = _viewModel?.ReferralInfo?.ReferralCode ?? "";
        var referralLink = _viewModel?.ReferralInfo?.ReferralLink ?? "";

        return $"Hey! 👋\n\n" +
           $"I'm using OPX and thought you might be interested!\n\n" +
           $"Use my referral code: {referralCode}\n" +
           $"Or sign up directly: {referralLink}\n\n" +
           $"Looking forward to having you on board! ";
    }

    #endregion

    #region Snackbar Helpers
    private void ShowErrorSnackbar(string message)
    {
        if (_isDisposed) return;

        try
        {
            var snackbar = Snackbar.Make(
                message ?? "An error occurred",
                null,
                "OKAY",
                TimeSpan.FromSeconds(5),
                new SnackbarOptions
                {
                    BackgroundColor = Color.FromArgb("#E74C3C"),
                    TextColor = Colors.White,
                    ActionButtonTextColor = Colors.White,
                    CornerRadius = new CornerRadius(8),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                });

            snackbar.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Snackbar error: {ex.Message}");
        }
    }

    private void ShowInfoSnackbar(string message)
    {
        if (_isDisposed) return;

        try
        {
            var snackbar = Snackbar.Make(
                message ?? "Information",
                null,
                "OKAY",
                TimeSpan.FromSeconds(3),
                new SnackbarOptions
                {
                    BackgroundColor = Color.FromArgb("#00BCD4"),
                    TextColor = Colors.White,
                    ActionButtonTextColor = Colors.White,
                    CornerRadius = new CornerRadius(8),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(14)
                });

            snackbar.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Info snackbar error: {ex.Message}");
        }
    }
    #endregion

    #region Lifecycle Methods
    protected override void OnAppearing()
    {
        if (_isDisposed) return;

        try
        {
            base.OnAppearing();
            _ = Task.Run(LoadReferralsAsync);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnAppearing error: {ex.Message}");
            ShowErrorSnackbar("Failed to load page");
        }
    }

    protected override void OnDisappearing()
    {
        try
        {
            base.OnDisappearing();
            _cancellationTokenSource?.Cancel();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnDisappearing error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        try
        {
            _isDisposed = true;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            if (_viewModel != null)
            {
                _viewModel.Referrals?.Clear();
                _viewModel.FilteredReferrals?.Clear();
                _viewModel = null;
            }

            BindingContext = null;
            _originalReferrals?.Clear();

            System.Diagnostics.Debug.WriteLine("Referral page disposed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Disposal error: {ex.Message}");
        }
    }
    #endregion

    #region API Response Models
    public class ReferralApiResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("referralCode")]
        public string ReferralCode { get; set; }

        [JsonProperty("referralLink")]
        public string ReferralLink { get; set; }

        [JsonProperty("totalReferrals")]
        public int TotalReferrals { get; set; }

        [JsonProperty("referrals")]
        public List<ReferralItem> Referrals { get; set; }
    }

    public class ReferralItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; }
    }
    #endregion
}