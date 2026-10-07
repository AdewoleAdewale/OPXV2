using AiForms.Dialogs;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using Opx.Model;
using Opx.Services;
using System.Net;
using Application = Microsoft.Maui.Controls.Application;

namespace Opx.Views;

public partial class Home : ContentPage
{
    private CreateContract createContract;
    private bool _isAnimating = false;
    private bool _hasShownAccountPopup = false;
    private static bool _transactionsLoaded = false; // Track if transactions are already loaded
    private static List<HistoryData> _cachedTransactions = new List<HistoryData>(); // Cache transactions
    private static DateTime _lastLoadTime = DateTime.MinValue; // Track when last loaded
    private bool _isRefreshing = false; // Track if currently refreshing

    private bool _isCheckingAccountSetup = false;
    private static bool _hasShownAccountPopupThisSession = false; // Static to persist across page loads


    // Add these constants for tracking account setup
    private const string ACCOUNT_SETUP_KEY = "has_account_setup";
    private const string ACCOUNT_REMINDER_KEY = "account_reminder_count";
    private const string LAST_REMINDER_KEY = "last_reminder_date";

    class HistoryData
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
        public string Initials
        {
            get
            {
                var words = (Description ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0) return "•";
                var s = words.Length == 1 ? words[0][..Math.Min(2, words[0].Length)] : $"{words[0][0]}{words[1][0]}";
                return s.ToUpperInvariant();
            }
        }
    }

    class HistoryDataHeaderFooter
    {
        public required List<HistoryData> HD { get; set; }
        public string Intro { get { return " You have Performed a total of " + HD.Count + " transactions within your search dates"; } }
        public string Summary { get { return " You have Performed a total of " + HD.Count + " transactions"; } }
        public decimal Size { get { return HD.Count; } }
    }

    public Home()
    {
        try
        {
            InitializeComponent();
            _current = new WeakReference<Home>(this);
            DashBoard.Attach(this, DashTab.Home);   // curved gradient tab bar

            dashbaordusername.Text = (LoginPage.myfullname ?? "").Substring(0, Math.Min(15, (LoginPage.myfullname ?? "").Length));
            //StartEllipseAnimation();
            BalanceLabel.Text = "₦" + LoginPage.availableBalance;
            AmountLabel.Text = "₦" + LoginPage.ledgerBalance;
            QuickFundsButton.Text = $"{LoginPage.completedTransactions}";
            if (!string.IsNullOrEmpty(LoginPage.pendingTransactions))
                System.Diagnostics.Debug.WriteLine($"Pending: {LoginPage.pendingTransactions}");
            TransferButton.Text = $"{LoginPage.totalTransactions}";
            PendingLabel.Text = string.IsNullOrEmpty(LoginPage.pendingTransactions) ? "0" : LoginPage.pendingTransactions;
            createContract = new CreateContract();
            cardaccountNumber.Text = LoginPage.accountNumber;
            cardAccountname.Text = LoginPage.myfullname;
            cardExiprydate.Text = LoginPage.bankName;
            UpdateHeaderExtras();
            if (!_transactionsLoaded)
            {
                LoadingRecentTransactions();
            }
            else
            {
                // Use cached data
                LoadCachedTransactions();
            }

            StartPageAnimations();
            AddPullToRefresh();
            // KYC prompt is triggered from OnAppearing (the page is attached to the navigation stack by then).
        }
        catch (Exception ex)
        {
            DisplayAlert("Initialization Error", $"Failed to initialize Dashboard: {ex.Message}", "OK");
        }
    }




    //private async void StartEllipseAnimation()
    //{
    //    // Animate first ellipse slowly
    //    while (true)
    //    {
    //        await Ellipse1.ScaleTo(1.2, 4000, Easing.SinInOut);
    //        await Ellipse1.ScaleTo(1.0, 4000, Easing.SinInOut);
    //    }
    //}

    //protected override async void OnAppearing()
    //{
    //    base.OnAppearing();
    //    // Animate second ellipse with slight delay
    //    await Task.Delay(2000);
    //    while (true)
    //    {
    //        await Ellipse2.ScaleTo(1.3, 5000, Easing.SinInOut);
    //        await Ellipse2.ScaleTo(1.0, 5000, Easing.SinInOut);
    //    }
    //}

    private async Task AnimateCardEntry(Frame frame)
    {
        await Task.WhenAll(
            frame.FadeTo(1, 600, Easing.CubicOut),
            frame.TranslateTo(0, 0, 600, Easing.CubicOut)
        );
    }

    private async Task AnimateCardPress(Frame frame)
    {
        await Task.WhenAll(
            frame.ScaleTo(0.95, 100, Easing.CubicIn),
            frame.FadeTo(0.8, 100, Easing.CubicIn)
        );

        await Task.WhenAll(
            frame.ScaleTo(1, 100, Easing.CubicOut),
            frame.FadeTo(1, 100, Easing.CubicOut)
        );
    }


    private async void OnNewContractTapped(object sender, EventArgs e)
    {
        var frame = sender as Frame;
        if (frame != null)
        {
            await frame.ScaleTo(0.95, 100);
            await frame.ScaleTo(1.0, 100);
        }

        var createContractPopup = new CreateContract();
        await this.ShowPopupAsync(createContractPopup);
    }

    private async void OnWithdrawalTapped(object sender, EventArgs e)
    {
        var frame = sender as Frame;
        if (frame != null)
        {
            await frame.ScaleTo(0.95, 100);
            await frame.ScaleTo(1.0, 100);
        }

        await Navigation.PushModalAsync(new CurrentBalance());
    }

    private async void OnRecentEscrowTapped(object sender, EventArgs e)
    {
        var frame = sender as Frame;
        if (frame != null)
        {
            await frame.ScaleTo(0.95, 100);
            await frame.ScaleTo(1.0, 100);
        }

        await Navigation.PushModalAsync(new ContractList());
    }


    private async void NavigateUsingShell(string route)
    {
        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", ex.Message, "OK");
        }
    }

    // Alternative method for dependency injection navigation
    private async void NavigateWithDI(Type pageType)
    {
        try
        {
            var page = (Page)Activator.CreateInstance(pageType);
            await Navigation.PushAsync(page);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", ex.Message, "OK");
        }
    }


    private static bool _kycPromptedThisRun = false;

    private void LoadCachedTransactions()
    {
        try
        {
            if (_cachedTransactions.Any())
            {
                SetRecent(_cachedTransactions);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading cached transactions: {ex.Message}");
        }
    }

    private void AddPullToRefresh()
    {
        try
        {
            // Add pull-to-refresh functionality to the ScrollView
            var refreshView = new RefreshView();
            refreshView.Command = new Command(async () => await RefreshTransactions());

            // You would need to wrap your ScrollView content in RefreshView
            // For now, we'll add a manual refresh method
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding pull-to-refresh: {ex.Message}");
        }
    }

    // Public method to manually refresh transactions
    private bool _manualRefreshRunning = false;

    public async Task RefreshTransactions()
    {
        if (_manualRefreshRunning) return;
        _manualRefreshRunning = true;
        try
        {
            _transactionsLoaded = false;   // force a reload
            await LoadingRecentTransactions();
        }
        finally
        {
            _manualRefreshRunning = false;
        }
    }

    // Pull down, or tap the refresh button in the header. These are the only ways the dashboard reloads.
    private async void OnPullRefresh(object? sender, EventArgs e)
    {
        try { await RefreshTransactions(); }
        finally { PullRefresh.IsRefreshing = false; }
    }

    private async void OnRefreshTapped(object? sender, TappedEventArgs e)
    {
        await AnimateButtonPress(RefreshButton);
        await RefreshTransactions();
    }

    private void ResetButton_Clicked(object sender, EventArgs e)
    {
        Preferences.Remove("temp_page_shown");
        // Restart the app or notify the user to restart
    }

    private async Task ShowKycFormPopup()
    {
        // Account number already exists: never show the KYC form.
        if (!string.IsNullOrWhiteSpace(LoginPage.accountNumber)) return;

        try
        {
            _hasShownAccountPopup = true;

            // Show KYC form as a modal popup
            await Navigation.PushModalAsync(new Kycform());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to show KYC verification: {ex.Message}", "OK");
        }
    }
    private async void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e)
    {
        await Navigation.PushModalAsync(new Views.CurrentBalance());
    }

    private async void TapGestureRecognizer_Tapped_2(object sender, TappedEventArgs e)
    {

        await Navigation.PushModalAsync(new Views.TransactionPage());
    }

    private async void TapGestureRecognizer_Tapped_3(object sender, TappedEventArgs e)
    {


        await Navigation.PushModalAsync(new Views.ContractList());
    }

    private async void kyc_Tapped(object sender, TappedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(LoginPage.accountNumber))
        {
            await DisplayAlert("Virtual account", "Your virtual account is already set up.", "OK");
            return;
        }
        await Navigation.PushModalAsync(new Views.Kycform());
    }

    private async void uPDATEREcient(object sender, TappedEventArgs e)
    {
        await Navigation.PushModalAsync(new Views.CurrentBalance());
    }



    private async void StartPageAnimations()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        try
        {
            // Set initial states for animations
            EscrowCard.Opacity = 0;
            EscrowCard.Scale = 0.8;
            EscrowCard.TranslationY = 50;

            QuickMenuStack.Opacity = 0;
            QuickMenuStack.TranslationX = -100;
            QuickMenuStack2.Opacity = 0;
            QuickMenuStack2.TranslationX = -100;
            TransactionLogStack.Opacity = 0;
            TransactionLogStack.TranslationY = 100;

            // Start animations with delays
            await Task.Delay(200);

            // Animate header
            await HeaderGrid.FadeTo(1, 500);

            // Animate escrow card
            var escrowAnimations = new Task[]
            {
                EscrowCard.FadeTo(1, 800),
                EscrowCard.ScaleTo(1, 600, Easing.BounceOut),
                EscrowCard.TranslateTo(0, 0, 700, Easing.CubicOut)
            };
            await Task.WhenAll(escrowAnimations);

            // Animate quick menu
            await Task.Delay(200);
            var quickMenuAnimations = new Task[]
            {
                QuickMenuStack.FadeTo(1, 600),
                QuickMenuStack.TranslateTo(0, 0, 500, Easing.CubicOut) ,
                   QuickMenuStack2.FadeTo(1, 600),
                QuickMenuStack2.TranslateTo(0, 0, 500, Easing.CubicOut)
            };
            await Task.WhenAll(quickMenuAnimations);


            // Animate transaction log
            await Task.Delay(200);
            var transactionAnimations = new Task[]
            {
                TransactionLogStack.FadeTo(1, 600),
                TransactionLogStack.TranslateTo(0, 0, 500, Easing.CubicOut)
            };
            await Task.WhenAll(transactionAnimations);

            // Animate balance number counting effect
            await AnimateBalanceCounter();
        }
        finally
        {
            _isAnimating = false;
        }
    }

    private async Task AnimateBalanceCounter()
    {
        var targetValue = 50000;
        var steps = 50;
        var increment = targetValue / steps;
        var delay = 30;

        for (int i = 0; i <= steps; i++)
        {
            var currentValue = (int)(i * increment);
            BalanceLabel.Text = "₦" + LoginPage.availableBalance;
            await Task.Delay(delay);
        }
    }

    private async void OnQuickFundsClicked(object sender, EventArgs e)
    {
        await AnimateButtonPress(QuickFundsButton);
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        try
        {
            //await AnimateButtonPress(PlusFrame);
            //var createContract = new CreateContract();
            //var result = await this.ShowPopupAsync(createContract);

            var createContractPopup = new CreateContract();
            await this.ShowPopupAsync(createContractPopup);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to show Create Contract popup: {ex.Message}", "OK");
        }
    }

    private async void OnTransferClicked(object sender, EventArgs e)
    {
        await AnimateButtonPress(TransferButton);
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await AnimateButtonPress(LogoutButton);

        bool result = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");

        if (result)
        {
            await PerformLogout();
        }
    }

    private async Task AnimateLogoutSequence()
    {
        var fadeOutTasks = new Task[]
        {
            EscrowCard.FadeTo(0, 300),
            QuickMenuStack.FadeTo(0, 300),
            QuickMenuStack2.FadeTo(0, 300),
            TransactionLogStack.FadeTo(0, 300)
        };

        await Task.WhenAll(fadeOutTasks);

        var scaleDownTasks = new Task[]
        {
            EscrowCard.ScaleTo(0.5, 200),
            QuickMenuStack.ScaleTo(0.5, 200),
            QuickMenuStack2.ScaleTo(0.5, 200),
            TransactionLogStack.ScaleTo(0.5, 200)
        };

        await Task.WhenAll(scaleDownTasks);
    }

    private void ClearUserSession()
    {
        try
        {
            Preferences.Clear();
            SessionStore.Clear();   // saved session is removed only on logout
            _kycPromptedThisRun = false;
            dashbaordusername.Text = string.Empty;

            // Clear cached transactions on logout
            _cachedTransactions.Clear();
            _transactionsLoaded = false;
            _lastLoadTime = DateTime.MinValue;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing session: {ex.Message}");
        }
    }

    private async Task NavigateToLogin()
    {
        try
        {
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            Application.Current.MainPage = new Home();
        }
    }

    private async Task AnimateButtonPress(VisualElement element)
    {
        try
        {
            await element.ScaleTo(0.95, 50, Easing.CubicInOut);
            await element.ScaleTo(1.0, 50, Easing.CubicInOut);
            await element.ScaleTo(1.05, 100, Easing.BounceOut);
            await element.ScaleTo(1.0, 100, Easing.BounceOut);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
        }
    }

    private async Task AnimateButtonPressWithGlow(VisualElement element)
    {
        try
        {
            var originalOpacity = element.Opacity;
            var originalScale = element.Scale;

            var scaleDown = element.ScaleTo(0.9, 50);
            var fadeDown = element.FadeTo(0.7, 50);

            await Task.WhenAll(scaleDown, fadeDown);

            var scaleUp = element.ScaleTo(originalScale, 150, Easing.BounceOut);
            var fadeUp = element.FadeTo(originalOpacity, 150);

            await Task.WhenAll(scaleUp, fadeUp);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Enhanced animation error: {ex.Message}");
        }
    }


    private async Task StartPulseAnimation(VisualElement element)
    {
        try
        {
            while (element.IsVisible)
            {
                await element.FadeTo(0.5, 1000);
                await element.FadeTo(1.0, 1000);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Pulse animation error: {ex.Message}");
        }
    }

    private async Task ShakeAnimation(VisualElement element)
    {
        try
        {
            for (int i = 0; i < 3; i++)
            {
                await element.TranslateTo(-10, 0, 50);
                await element.TranslateTo(10, 0, 50);
            }
            await element.TranslateTo(0, 0, 50);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Shake animation error: {ex.Message}");
        }
    }

    private async Task RefreshDashboardData()
    {
        try
        {
            await EscrowCard.FadeTo(0.5, 200);
            await Task.Delay(1000);
            BalanceLabel.Text = "₦" + LoginPage.availableBalance;
            await EscrowCard.FadeTo(1.0, 200);
            await DisplayAlert("Success", "Dashboard data refreshed successfully!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Failed to refresh data: " + ex.Message, "OK");
        }
    }

    private async void OnRefreshRequested(object sender, EventArgs e)
    {
        await RefreshTransactions();
    }

    private void AddSwipeGestures()
    {
        var swipeGesture = new SwipeGestureRecognizer
        {
            Direction = SwipeDirection.Down
        };
        swipeGesture.Swiped += OnRefreshRequested;
        Content.GestureRecognizers.Add(swipeGesture);
    }

    private void InitializeAdditionalFeatures()
    {
        AddSwipeGestures();
        Device.StartTimer(TimeSpan.FromSeconds(5), () =>
        {
            return true;
        });
    }

    protected override bool OnBackButtonPressed()
    {
        Device.BeginInvokeOnMainThread(async () =>
        {
            bool result = await DisplayAlert("NOTIFICATION", "Are you sure you want to exit the application?", "Yes", "No");
            if (result)
            {
                Application.Current?.Quit();   // the session stays saved; only the Logout button signs out
            }
        });
        return true;
    }

    // ═════════ Return-to-Home refresh ═════════
    // Pages close back onto the Home instance that is already underneath (see DashBoard.GoHomeAsync) –
    // Home is never rebuilt. On return only the virtual card and the recent-transactions list refresh,
    // silently (no full-screen loading overlay, so no black flash).
    private static WeakReference<Home>? _current;
    private bool _hasAppearedOnce;
    private bool _returnRefreshRunning;
    private DateTime _lastReturnRefreshUtc = DateTime.MinValue;
    private string _recentSignature = "";

    /// <summary>Called by DashBoard.GoHomeAsync after it has closed the pages above Home.</summary>
    public static Task NotifyReturnedAsync() =>
        _current != null && _current.TryGetTarget(out var home) ? home.RefreshOnReturnAsync() : Task.CompletedTask;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = PromptKycIfNoAccountAsync();   // no account number yet -> open the KYC form
        if (!_hasAppearedOnce) { _hasAppearedOnce = true; return; }   // first show: the constructor already loaded everything
        _ = RefreshOnReturnAsync();
    }

    // Opens the KYC form (which leads to BVN verification) when the user has no account number.
    // Shown once per app run (_kycPromptedThisRun is reset on logout); never shown when an account number exists.
    private async Task PromptKycIfNoAccountAsync()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(LoginPage.accountNumber)) return;
            if (_kycPromptedThisRun) return;
            _kycPromptedThisRun = true;

            await Task.Delay(600);   // let Home finish rendering before the modal opens
            if (!string.IsNullOrWhiteSpace(LoginPage.accountNumber)) return;

            await MainThread.InvokeOnMainThreadAsync(ShowKycFormPopup);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"KYC prompt error: {ex.Message}");
        }
    }

    public async Task RefreshOnReturnAsync()
    {
        // OnAppearing and GoHomeAsync can both fire for one return – run once.
        if (_returnRefreshRunning || (DateTime.UtcNow - _lastReturnRefreshUtc).TotalMilliseconds < 1500) return;
        _returnRefreshRunning = true;
        _lastReturnRefreshUtc = DateTime.UtcNow;
        try
        {
            await MainThread.InvokeOnMainThreadAsync(ApplyCardState);   // instant: latest in-memory account/balance state
            await FetchSummaryAndRecentAsync();                          // then quietly pull fresh balances + recent list
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Return refresh error: {ex.Message}");
        }
        finally
        {
            _returnRefreshRunning = false;
        }
    }

    /// <summary>Greeting, initials avatar and the two status pills in the new header layout.</summary>
    private void UpdateHeaderExtras()
    {
        try
        {
            var parts = (LoginPage.myfullname ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            AvatarInitials.Text = parts.Length switch
            {
                0 => "U",
                1 => char.ToUpperInvariant(parts[0][0]).ToString(),
                _ => $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}"
            };

            int hour = DateTime.Now.Hour;
            GreetingLabel.Text = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";

            bool hasAccount = !string.IsNullOrWhiteSpace(LoginPage.accountNumber);
            SetPill(AccountPill, AccountPillLabel, hasAccount ? "Account active" : "No account yet", hasAccount);

            bool online = Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet;
            SetPill(NetworkPill, NetworkPillLabel, online ? "Online" : "Offline", online);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Header extras error: {ex.Message}");
        }
    }

    private static void SetPill(Border pill, Label label, string text, bool ok)
    {
        label.Text = text;
        pill.BackgroundColor = Color.FromArgb(ok ? "#E3F6EC" : "#FFF0DC");
        label.TextColor = Color.FromArgb(ok ? "#1A8F5F" : "#B26A00");
    }

    private void ApplyCardState()
    {
        BalanceLabel.Text = "₦" + LoginPage.availableBalance;
        AmountLabel.Text = "₦" + LoginPage.ledgerBalance;
        QuickFundsButton.Text = $"{LoginPage.completedTransactions}";
        TransferButton.Text = $"{LoginPage.totalTransactions}";
        cardaccountNumber.Text = LoginPage.accountNumber;
        cardAccountname.Text = LoginPage.myfullname;
        cardExiprydate.Text = LoginPage.bankName;
        PendingLabel.Text = string.IsNullOrEmpty(LoginPage.pendingTransactions) ? "0" : LoginPage.pendingTransactions;
        UpdateHeaderExtras();
    }

    private async Task FetchSummaryAndRecentAsync()
    {
        var result = await SessionStore.WithAuthRetryAsync(() => OpxApi.GetAsync<DashboardSummaryResponse>(
            $"/dashboard/summary?email={Uri.EscapeDataString(LoginPage.myemail ?? "")}"));

        // Offline / server error: keep what is already on screen, no error spam on a background refresh.
        if (!result.IsHttpSuccess || result.Data == null) return;

        if (result.Data.Summary != null) ApplySummary(result.Data.Summary);

        var recent = result.Data.RecentOrders;
        if (recent != null && recent.Count > 0)
        {
            var items = MapRecentOrders(recent);
            _cachedTransactions = items;
            _transactionsLoaded = true;
            _lastLoadTime = DateTime.Now;
            await MainThread.InvokeOnMainThreadAsync(() => SetRecent(items, animate: false));
        }
    }

    /// <summary>Persist fresh balances into the static login state (other pages read it) and update the card.</summary>
    private void ApplySummary(DashboardSummary summary)
    {
        LoginPage.availableBalance = summary.AvailableBalance;
        LoginPage.ledgerBalance = summary.LedgerBalance;
        LoginPage.completedTransactions = summary.Completed.ToString();
        LoginPage.totalTransactions = summary.Total.ToString();
        LoginPage.pendingTransactions = summary.Pending.ToString();
        LoginPage.disputes = summary.Disputed.ToString();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            BalanceLabel.Text = "₦" + summary.AvailableBalance;
            AmountLabel.Text = "₦" + summary.LedgerBalance;
            QuickFundsButton.Text = $"{summary.Completed}";
            TransferButton.Text = $"{summary.Total}";
            PendingLabel.Text = $"{summary.Pending}";
            UpdateHeaderExtras();
        });
    }

    private List<HistoryData> MapRecentOrders(IEnumerable<DashboardRecentOrder> orders) =>
        orders
            .OrderByDescending(x => x.Date)
            .Take(5)
            .Select(o => new HistoryData
            {
                Id = int.TryParse(o.Id, out var id) ? id : 0,
                SellerId = o.SellerName ?? "",
                BuyerId = o.IsBuyer ? LoginPage.myemail ?? "" : "",
                Amount = o.Amount,
                Token = o.Id ?? "",
                IsConfirmed = o.IsConfirmed,
                ConfirmedAt = null,
                IsCancellationRequested = o.IsCancellationRequested,
                CancelRequestedAt = null,
                IsCancelled = o.IsCancelled,
                CancelledAt = null,
                CreatedAt = o.Date,
                Description = o.Names ?? o.SellerName ?? "",
                Status = o.Status ?? "",
                SellerName = o.SellerName ?? "",
                SellerPhone = "",
                BuyerName = "",
                BuyerPhone = "",
                Role = o.IsBuyer ? "Buyer" : "Seller"
            }).ToList();

    /// <summary>Shows the recent list. Skips the rebuild when nothing changed so a silent refresh never flickers.</summary>
    private void SetRecent(IEnumerable<HistoryData>? items, bool animate = true)
    {
        var list = items?.ToList() ?? new List<HistoryData>();
        var signature = string.Join("|", list.Select(x => $"{x.Token}:{x.Status}:{x.Amount}"));
        if (!animate && signature == _recentSignature) return;
        _recentSignature = signature;

        BindableLayout.SetItemsSource(listView, list);
        RecentEmptyLabel.IsVisible = list.Count == 0;
        if (animate && list.Count > 0) AnimateListItems();
    }

    // ═════════ Responsive layout (all Android widths / heights) ═════════
    // Layout units here are dp, so they already normalise pixel density.
    // Baseline: a 5.5" Android phone is ~360dp wide. At 360dp or wider the card renders at its full intended
    // size; on narrower phones every card metric (fonts, padding, gaps, logo, button) shrinks proportionally.
    private const double RefWidthDp = 360;          // width at which the design is 1:1
    private const double MaxContentWidthDp = 440;   // content column cap on big phones / tablets / landscape
    private const double MinScale = 0.78;           // never shrink the card below 78 %
    private double _lastW, _lastH;

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0 || height <= 0) return;
        if (Math.Abs(width - _lastW) < 1 && Math.Abs(height - _lastH) < 1) return;
        _lastW = width; _lastH = height;

        bool tiny = height < 580;        // split-screen, very short displays
        bool shortScreen = height < 680;

        // One centred column holds everything (card, tiles, summary, banner, list), so every block
        // shares the same left/right edge and the card is always centred.
        double contentW = Math.Min(width, MaxContentWidthDp);
        double s = Math.Clamp(contentW / RefWidthDp, MinScale, 1.0);
        double Sc(double v, double min = 0) => Math.Max(min, Math.Round(v * s, 1));

        var r = Resources;

        // ── Virtual card (scales proportionally) ──
        r["HeroFont"] = Sc(34, 24);
        r["AcctFont"] = Sc(22, 16);
        r["CardBody"] = Sc(13, 11);
        r["CardSmall"] = Sc(11, 9);
        r["CardCaption"] = Sc(10, 8);
        r["CardMicro"] = Sc(9, 8);
        r["CardIcon"] = Sc(18, 15);
        r["CardLogo"] = Sc(28, 22);
        r["CardBtnH"] = Sc(42, 36);
        r["CardGap"] = shortScreen ? Sc(12, 9) : Sc(16, 12);
        double padV = shortScreen ? 16 : 20;
        r["CardPad"] = new Thickness(Sc(22, 16), Sc(padV, 12), Sc(22, 16), Sc(padV + 2, 14));

        // ── Rest of the page ──
        r["ValueFont"] = Sc(16, 13);
        r["StatFont"] = Sc(20, 16);
        r["TileBox"] = Sc(44, 36);
        r["TileImg"] = Sc(22, 18);
        r["TilePad"] = new Thickness(0, shortScreen ? 10 : 14);
        r["BlockGap"] = tiny ? 10d : shortScreen ? 14d : 18d;
        r["BannerImg"] = Sc(64, 44);
        r["HeaderPad"] = shortScreen ? new Thickness(16, 18, 16, 20) : new Thickness(16, 28, 16, 22);

        // Big phones / foldables / tablets / landscape: cap and centre the column instead of stretching edge to edge.
        bool capped = width > MaxContentWidthDp;
        MainContent.WidthRequest = capped ? MaxContentWidthDp : -1;
        MainContent.HorizontalOptions = capped ? LayoutOptions.Center : LayoutOptions.Fill;
    }

    async Task LoadingRecentTransactions()
    {
        if (_isRefreshing) return;

        try
        {
            Configurations.LoadingConfig = new LoadingConfig
            {
                Opacity = 0.4,
                DefaultMessage = "Loading dashboard...",
                FontSize = 12,
            };

            await Loading.Instance.StartAsync(async progress =>
            {
                try
                {
                    for (var i = 0; i < 50; i++) { await Task.Delay(20); progress.Report((i + 1) * 0.01d); }

                    // --- Dashboard summary (balances + recent orders) ---
                    var summaryResult = await OpxApi.GetAsync<DashboardSummaryResponse>(
                        $"/dashboard/summary?email={Uri.EscapeDataString(LoginPage.myemail ?? "")}");

                    progress.Report(0.7d);

                    if (summaryResult.IsHttpSuccess && summaryResult.Data?.Summary != null)
                    {
                        ApplySummary(summaryResult.Data.Summary);
                    }
                    else if (!summaryResult.IsNetworkError)
                    {
                        System.Diagnostics.Debug.WriteLine($"Dashboard summary error: {summaryResult.ErrorMessage}");
                    }

                    // --- Recent orders from summary recentOrders ---
                    var recentOrders = summaryResult.Data?.RecentOrders;
                    if (recentOrders != null && recentOrders.Count > 0)
                    {
                        var historyItems = MapRecentOrders(recentOrders);

                        _cachedTransactions = historyItems;
                        _transactionsLoaded = true;
                        _lastLoadTime = DateTime.Now;

                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            SetRecent(_cachedTransactions);
                        });
                    }
                    else if (recentOrders != null)
                    {
                        // summary came back but no recent orders – fall back to contracts list
                        await LoadContractsListAsync(progress);
                    }
                    else
                    {
                        await LoadContractsListAsync(progress);
                    }

                    progress.Report(1.0d);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Dashboard loading error: {ex.Message}");
                    await ShowErrorSnackbar($"Error loading dashboard: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Loading initialization error: {ex.Message}");
            await ShowErrorSnackbar($"Failed to initialize loading: {ex.Message}");
        }
    }

    /// <summary>Fallback: load recent contracts directly if the dashboard summary gives no recent orders.</summary>
    private async Task LoadContractsListAsync(IProgress<double> progress)
    {
        try
        {
            var result = await OpxApi.GetAsync<List<HistoryData>>(
                $"/contractsapi/?email={Uri.EscapeDataString(LoginPage.myemail ?? "")}");

            progress?.Report(0.9d);

            if (result.IsHttpSuccess && result.Data != null)
            {
                var sorted = result.Data.OrderByDescending(x => x.CreatedAt).Take(5).ToList();
                _cachedTransactions = sorted;
                _transactionsLoaded = true;
                _lastLoadTime = DateTime.Now;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetRecent(_cachedTransactions);
                });
            }
            else
            {
                await ShowErrorSnackbar(result.ErrorMessage.Length > 0
                    ? result.ErrorMessage
                    : $"Server returned {result.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorSnackbar($"Network error: {ex.Message}");
        }
    }

    private async Task ShowErrorSnackbar(string message)
    {
        await Device.InvokeOnMainThreadAsync(async () =>
        {
            var snackbar = Snackbar.Make(message, null, "OKAY", TimeSpan.FromSeconds(5), new SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#F44336"),
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.White,
                CornerRadius = new CornerRadius(8),
                Font = Microsoft.Maui.Font.SystemFontOfSize(14)
            });
            await snackbar.Show();
        });
    }

    private async void AnimateListItems()
    {
        try
        {
            var rows = listView.Children.OfType<VisualElement>().Take(10).ToList();
            foreach (var row in rows) row.Opacity = 0;
            foreach (var row in rows)
            {
                _ = row.FadeTo(1, 250, Easing.CubicOut);
                await Task.Delay(60);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
        }
    }

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

    private async void TapGestureRecognizer_Tapped_4(object sender, TappedEventArgs e)
    {
        Configurations.LoadingConfig = new LoadingConfig
        {
            Opacity = 0.4,
            DefaultMessage = "Fetching Transaction History From OPX Please Wait...",
            FontSize = 12,
        };

        await Loading.Instance.StartAsync(async progress =>
        {
            for (var i = 0; i < 100; i++)
            {
                await Task.Delay(50);
                progress.Report((i + 1) * 0.01d);
            }
        });

        await Navigation.PushModalAsync(new Views.ContractList());
    }



    private async Task ShowAccountSetupPopup()
    {
        // Account number already exists: never show the BVN verification pop-up.
        if (!string.IsNullOrWhiteSpace(LoginPage.accountNumber)) return;

        try
        {
            _hasShownAccountPopup = true;

            // Check if account number exists to determine popup type/message
            bool hasAccountNumber = !string.IsNullOrEmpty(LoginPage.accountNumber);

            var addAccountPopup = new AddAccount();
            var result = await this.ShowPopupAsync(addAccountPopup);

            if (result is bool success && success)
            {
                OnAccountSetupCompleted();
            }
            else if (result is string resultString)
            {
                if (resultString == "completed" || resultString == "success")
                {
                    OnAccountSetupCompleted();
                }
                else
                {
                    OnAccountSetupDismissed();
                }
            }
            else
            {
                OnAccountSetupDismissed();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to show account setup: {ex.Message}", "OK");
            OnAccountSetupDismissed();
        }
    }


    private void OnAccountSetupCompleted()
    {
        // Clear both popup session keys since setup is complete
        Preferences.Remove("popup_shown_no_account");
        Preferences.Remove("popup_shown_has_account");

        // Set traditional account setup key
        Preferences.Set(ACCOUNT_SETUP_KEY, true);
        Preferences.Remove(ACCOUNT_REMINDER_KEY);
        Preferences.Remove(LAST_REMINDER_KEY);

        // Mark as completed to prevent future popups
        _hasShownAccountPopup = true;

        Device.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert("Success", "Account setup completed successfully!", "OK");
        });
    }

    // Update the OnAccountSetupDismissed method
    private void OnAccountSetupDismissed()
    {
        // The popup has been shown and dismissed for current account status
        // No need to show again in this session
        _hasShownAccountPopup = true;
    }

    private async Task PerformLogout()
    {
        try
        {
            await AnimateLogoutSequence();
            ClearUserSession();
            ResetAccountPopupState();

            // ✅ Clear ALL KYC session preferences on logout
            Preferences.Remove("kyc_shown_this_session");
            Preferences.Remove("kyc_popup_shown_no_account");

            await NavigateToLogin();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "An error occurred during logout: " + ex.Message, "OK");
        }
    }

    public static void ResetAccountPopupState()
    {
        _hasShownAccountPopupThisSession = false;

        // Clear session-specific popup keys so popup can show again in new session
        Preferences.Remove("kyc_popup_shown_no_account");
        Preferences.Remove("kyc_popup_shown_has_account");
    }
    // Simplify the StartAccountReminderSystem method since we only want to show once per session
    private void StartAccountReminderSystem()
    {
        // Since you only want popup to show once per session based on account status,
        // we can simplify this or remove it entirely
        // The popup logic is now handled in CheckAndShowAccountSetup
        return;
    }

    // Simplify the ShowAccountReminder method  
    private async Task ShowAccountReminder()
    {
        // Since you only want popup to show once per session based on account status,
        // we can simplify this or remove it entirely
        return;
    }



    public void MarkAccountSetupComplete()
    {
        Preferences.Set(ACCOUNT_SETUP_KEY, true);
        Preferences.Remove(ACCOUNT_REMINDER_KEY);
        Preferences.Remove(LAST_REMINDER_KEY);
    }



    private async void TapGestureRecognizer_Tapped_5(object sender, TappedEventArgs e)
    {
        try
        {
            string accountNumber = cardaccountNumber.Text;

            if (!string.IsNullOrEmpty(accountNumber))
            {
                await Clipboard.SetTextAsync(accountNumber);
                await DisplayAlert("Copied", "Account number copied to clipboard", "OK");

                var originalColor = cardaccountNumber.TextColor;
                cardaccountNumber.TextColor = Colors.LightGreen;
                await Task.Delay(500);
                cardaccountNumber.TextColor = originalColor;
            }
            else
            {
                await DisplayAlert("Error", "No account number to copy", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to copy account number: {ex.Message}", "OK");
        }
    }

    // Add this method to force refresh when needed (e.g., after creating a new transaction)
    public async Task ForceRefreshTransactions()
    {
        _transactionsLoaded = false;
        _cachedTransactions.Clear();
        await LoadingRecentTransactions();
    }



    // Existing event handlers (keep your current implementation)
    private async void Addmoney_Tapped(object sender, EventArgs e)
    {
        await AnimateCardTap(sender as Frame);
        var createContractPopup = new CreateContract();
        await this.ShowPopupAsync(createContractPopup);
    }

    private async void withdraw_Tapped(object sender, EventArgs e)
    {
        await AnimateCardTap(sender as Frame);
        await Navigation.PushModalAsync(new CurrentBalance());
    }

    // New event handlers for enhanced Quick Menu items
    private async void OnTransactionHistoryTapped(object sender, EventArgs e)
    {
        await AnimateCardTap(sender as Frame);
        try
        {
            await Navigation.PushModalAsync(new ContractList());

            // Or show a popup with recent transactions
            //await DisplayAlert("Transaction History", "Opening transaction history...", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Unable to open transaction history: {ex.Message}", "OK");
        }
    }

    private async void OnAnalyticsTapped(object sender, EventArgs e)
    {
        await AnimateCardTap(sender as Frame);
        try
        {
            // Navigate to analytics page
            // await Navigation.PushAsync(new AnalyticsPage());

            // Or show analytics dashboard
            await DisplayAlert("Analytics", "Opening analytics dashboard...", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Unable to open analytics: {ex.Message}", "OK");
        }
    }

    private async void OnSupportTapped(object sender, EventArgs e)
    {
        await AnimateCardTap(sender as Frame);
        try
        {
            // Show support options
            string action = await DisplayActionSheet("Customer Support", "Cancel", null,
                "Live Chat", "Call Support", "Email Support");

            switch (action)
            {
                case "Live Chat":
                    // Open live chat
                    await DisplayAlert("Live Chat", "Opening live chat...", "OK");
                    break;
                case "Call Support":
                    // Open phone dialer
                    await DisplayAlert("Call Support", "Opening phone dialer...", "OK");
                    break;
                case "Email Support":
                    // Open email client
                    await DisplayAlert("Email Support", "Opening email client...", "OK");
                    break;

            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Unable to open support: {ex.Message}", "OK");
        }
    }

    private async void OnSettingsTapped(object sender, EventArgs e)
    {
        await AnimateCardTap(sender as Frame);
        try
        {
            await Navigation.PushModalAsync(new ProfilePage());

            // Or show settings options
            //string action = await DisplayActionSheet("Account Settings", "Cancel", null,
            //    "Profile Settings", "Security Settings", "Notification Settings", "Privacy Settings");

            //switch (action)
            //{
            //    case "Profile Settings":
            //        await DisplayAlert("Profile Settings", "Opening profile settings...", "OK");
            //        break;
            //    case "Security Settings":
            //        await DisplayAlert("Security Settings", "Opening security settings...", "OK");
            //        break;
            //    case "Notification Settings":
            //        await DisplayAlert("Notification Settings", "Opening notification settings...", "OK");
            //        break;
            //    case "Privacy Settings":
            //        await DisplayAlert("Privacy Settings", "Opening privacy settings...", "OK");
            //        break;
            //}
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Unable to open settings: {ex.Message}", "OK");
        }
    }







    private async Task AnimateCardEntrance(Frame card, int delay)
    {
        await Task.Delay(delay);

        var translateAnimation = card.TranslateTo(0, 0, 400, Easing.CubicOut);
        var fadeAnimation = card.FadeTo(1, 400, Easing.CubicOut);

        await Task.WhenAll(translateAnimation, fadeAnimation);
    }



    private async Task AnimateIconPulse(Label icon)
    {
        await icon.ScaleTo(1.2, 150, Easing.CubicOut);
        await icon.ScaleTo(1.0, 150, Easing.CubicIn);
    }

    private async Task AnimateCardPress(Frame card, Label icon)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        // Scale down animation
        var scaleDownTask = card.ScaleTo(0.95, 100, Easing.CubicOut);
        var iconScaleTask = icon.ScaleTo(0.9, 100, Easing.CubicOut);

        await Task.WhenAll(scaleDownTask, iconScaleTask);

        // Scale back up animation
        var scaleUpTask = card.ScaleTo(1.0, 100, Easing.CubicOut);
        var iconScaleUpTask = icon.ScaleTo(1.0, 100, Easing.CubicOut);

        await Task.WhenAll(scaleUpTask, iconScaleUpTask);

        _isAnimating = false;
    }

    private async Task AnimateCardHover(Frame card, bool isHovered)
    {
        if (_isAnimating) return;

        if (isHovered)
        {
            await card.ScaleTo(1.05, 200, Easing.CubicOut);
        }
        else
        {
            await card.ScaleTo(1.0, 200, Easing.CubicOut);
        }
    }

    // Event handlers for card taps



    // Optional: Add touch effects for better user experience
    private void OnCardPointerEntered(object sender, EventArgs e)
    {
        if (sender is Frame card)
        {
            _ = AnimateCardHover(card, true);
        }
    }

    private void OnCardPointerExited(object sender, EventArgs e)
    {
        if (sender is Frame card)
        {
            _ = AnimateCardHover(card, false);
        }
    }

    // Method to add shimmer loading effect (optional)
    private async Task AnimateShimmerEffect(Frame card)
    {
        var shimmerFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#40FFFFFF"),
            CornerRadius = 15,
            HasShadow = false,
            Opacity = 0
        };

        // Add shimmer frame to card (you'd need to modify the XAML structure for this)
        // This is a simplified example

        await shimmerFrame.FadeTo(0.7, 800, Easing.SinInOut);
        await shimmerFrame.FadeTo(0, 800, Easing.SinInOut);
    }



    private async Task AnimateCardTap(Frame frame)
    {
        if (frame == null) return;

        try
        {
            // Create a bounce effect when tapped
            await frame.ScaleTo(0.95, 100, Easing.CubicIn);
            await frame.ScaleTo(1.05, 100, Easing.CubicOut);
            await frame.ScaleTo(1, 100, Easing.CubicInOut);
        }
        catch (Exception ex)
        {
            // Handle animation errors silently
            System.Diagnostics.Debug.WriteLine($"Tap animation error: {ex.Message}");
        }
    }


    private async Task ShowLoadingIndicator(string message = "Loading...")
    {

        await DisplayAlert("Loading", message, "OK");
    }

    private async Task NavigateToPage(Page page)
    {
        try
        {
            await Navigation.PushAsync(page);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", $"Unable to navigate: {ex.Message}", "OK");
        }
    }

    private async void TapGestureRecognizer_Tapped_6(object sender, TappedEventArgs e)
    {
        await Navigation.PushModalAsync(new Views.CurrentBalance());
    }

    private async void TapGestureRecognizer_Tapped_7(object sender, TappedEventArgs e)
    {
        var createContractPopup = new CreateContract();
        await this.ShowPopupAsync(createContractPopup);
    }

    private async void TapGestureRecognizer_Tapped_8(object sender, TappedEventArgs e)
    {
        await Navigation.PushModalAsync(new Views.ContractList());
    }

    private async void OnWaterTapped(object sender, EventArgs e)
    {

        var createContractPopup = new CreateContract();
        await this.ShowPopupAsync(createContractPopup);
    }
    private async void OnPowerTapped(object sender, EventArgs e)
    {

        await Navigation.PushModalAsync(new Views.CurrentBalance());

    }
    private async void OnGroceryTapped(object sender, EventArgs e)
    {

        await Navigation.PushModalAsync(new Views.ContractList());
    }

    private async void OnNewContractTapped_WithFeedback(object sender, EventArgs e)
    {
        // Add visual feedback
        var frame = sender as Frame;
        if (frame != null)
        {
            await frame.ScaleTo(0.95, 100);
            await frame.ScaleTo(1.0, 100);
        }

        var createContractPopup = new CreateContract();
        await this.ShowPopupAsync(createContractPopup);
    }



    private async void TapGestureRecognizer_Tapped_9(object sender, TappedEventArgs e)
    {
        var createContractPopup = new CreateContract();
        await this.ShowPopupAsync(createContractPopup);
    }

    private async void TapGestureRecognizer_Tapped_10(object sender, TappedEventArgs e)
    {
        await Navigation.PushModalAsync(new CurrentBalance());
    }

    private async void TapGestureRecognizer_Tapped_11(object sender, TappedEventArgs e)
    {
        await Navigation.PushModalAsync(new ContractList());
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {

        await Navigation.PushModalAsync(new Views.Referral());
    }

    private async void TapGestureRecognizer_Tapped_12(object sender, TappedEventArgs e)
    {
        await Navigation.PushModalAsync(new Views.Referral());
    }
}