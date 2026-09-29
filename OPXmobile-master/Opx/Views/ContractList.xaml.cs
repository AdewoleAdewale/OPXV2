using AiForms.Dialogs;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Opx.Views;
public partial class ContractList : ContentPage, IDisposable
{
    private VerifyToken _verifytokenPopup;
    private Dispute _disputePopup;
    #region Fields
    private readonly Dictionary<Microsoft.Maui.Controls.ViewCell, bool> _animatedCells = new();
    private bool _isLoading = false;
    private bool _isDisposed = false;
    private bool _isExpanded = false;
    public class Transaction : INotifyPropertyChanged
    {
        private bool _isExpanded = false;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }



        // Your other properties...

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    private HistoryDataHeaderFooter _viewModel;
    private CancellationTokenSource _cancellationTokenSource;
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const int TIMEOUT_SECONDS = 30;
    private List<HistoryData> _originalTransactions = new();
    private List<HistoryData> _filteredTransactions = new();
    private readonly object _lockObject = new object();
    #endregion

    #region Data Models
    public class HistoryData
    {

        public bool ShowPendingActions => GetTransactionStatus().ToLower() == "pending";

        public bool ShowVerifyButton => ShowPendingActions;
        public bool ShowDisputeButton => ShowPendingActions;
        public int Id { get; set; }
        public string SellerId { get; set; } = string.Empty;
        public string BuyerId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Token { get; set; } = string.Empty;
        public bool IsConfirmed { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public bool IsCancellationRequested { get; set; }
        public DateTime? CancelRequestedAt { get; set; }
        public bool? IsCancelled { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public string SellerName { get; set; } = string.Empty;
        public string SellerPhone { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerPhone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // UI Properties
        public string CategoryIcon => GetCategoryIcon();
        public string CategoryColor => GetCategoryColor();
        public string FormattedAmount => GetFormattedAmount();
        public string AmountColor => GetAmountColor();

        public bool IsExpanded { get; internal set; }

        private string GetCategoryIcon()
        {
            try
            {
                var description = Description?.ToLower() ?? string.Empty;
                var status = Status?.ToLower() ?? string.Empty;

                // Return text/emoji characters for display in Label
                return description switch
                {
                    var desc when desc.Contains("completed") => "✅",
                    var desc when desc.Contains("successful") || desc.Contains("sucessfull") => "✅",
                    var desc when desc.Contains("failed") => "❌",
                    var desc when desc.Contains("cancelled") => "⛔",
                    var desc when desc.Contains("pending") => "⏳",
                    _ => status switch
                    {
                        "completed" => "✅",
                        "successful" or "sucessfull" => "✅",
                        "failed" => "❌",
                        "cancelled" => "⛔",
                        "pending" => "⏳",
                        _ => "💼"
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCategoryIcon error: {ex.Message}");
                return "💼";
            }
        }

        private string GetCategoryColor()
        {
            try
            {
                var description = Description?.ToLower() ?? string.Empty;
                var status = Status?.ToLower() ?? string.Empty;

                return description switch
                {
                    var desc when desc.Contains("completed") => "#FFFFFF",
                    var desc when desc.Contains("successful") || desc.Contains("sucessfull") => "#FFFFFF",
                    var desc when desc.Contains("failed") => "#FFFFFF",
                    var desc when desc.Contains("cancelled") => "#FFFFFF",
                    var desc when desc.Contains("pending") => "#FFFFFF",
                    _ => status switch
                    {
                        "completed" => "#FFFFFF",
                        "successful" or "sucessfull" => "#FFFFFF",
                        "failed" => "#FFFFFF",
                        "cancelled" => "#FFFFFF",
                        "pending" => "#FFFFFF",
                        _ => "#FFFFFF"
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetCategoryColor error: {ex.Message}");
                return "#607D8B";
            }
        }

        private string GetFormattedAmount()
        {
            try
            {
                var symbol = Amount >= 0 ? "+" : "";
                return $"{symbol}₦{Math.Abs(Amount):N2}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetFormattedAmount error: {ex.Message}");
                return "₦0.00";
            }
        }

        private string GetAmountColor()
        {
            try
            {
                return Amount >= 0 ? "#4CAF50" : "#E74C3C";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAmountColor error: {ex.Message}");
                return "#607D8B";
            }
        }

        public string GetTransactionStatus()
        {
            try
            {
                if (Status == "cancelled")
                    return "cancelled";
                if (Status == "Confirmed")
                    return "Confirmed";
                if (Status == "pending")
                    return "pending";

                if (Status == "Cancellation Rejected")
                    return "Cancellation Rejected";
                if (IsCancellationRequested == true)
                    return "Cancellation Rejected";

                return "pending";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTransactionStatus error: {ex.Message}");
                return "pending";
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }




    }

    public class HistoryDataHeaderFooter : INotifyPropertyChanged
    {
        private ObservableCollection<HistoryData> _hd = new();
        private ObservableCollection<HistoryData> _filteredTransactions = new();
        private bool _isRefreshing = false;
        private string _currentMonth = DateTime.Now.ToString("MMMM");
        private decimal _totalIncome = 0;
        private decimal _totalOutcome = 0;
        private readonly object _lockObject = new object();

        public ObservableCollection<HistoryData> HD
        {
            get
            {
                lock (_lockObject)
                {
                    return _hd;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _hd = value ?? new ObservableCollection<HistoryData>();
                    OnPropertyChanged();
                    UpdateFilteredTransactions();
                    UpdateSummaryProperties();
                }
            }
        }

        public ObservableCollection<HistoryData> FilteredTransactions
        {
            get
            {
                lock (_lockObject)
                {
                    return _filteredTransactions;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _filteredTransactions = value ?? new ObservableCollection<HistoryData>();
                    OnPropertyChanged();
                    UpdateSummaryProperties();
                }
            }
        }

        public string CurrentMonth
        {
            get => _currentMonth;
            set
            {
                _currentMonth = value ?? DateTime.Now.ToString("MMMM");
                OnPropertyChanged();
            }
        }

        public decimal TotalIncome
        {
            get => _totalIncome;
            set
            {
                _totalIncome = value;
                OnPropertyChanged();
            }
        }

        public decimal TotalOutcome
        {
            get => _totalOutcome;
            set
            {
                _totalOutcome = value;
                OnPropertyChanged();
            }
        }

        public string Intro => $"You have performed a total of {FilteredTransactions?.Count ?? 0} transactions within your search dates";
        public string Summary => $"You have performed a total of {FilteredTransactions?.Count ?? 0} transactions";
        public decimal Size => FilteredTransactions?.Count ?? 0;
        public int TotalContracts => FilteredTransactions?.Count ?? 0;

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }

        private void UpdateFilteredTransactions()
        {
            try
            {
                lock (_lockObject)
                {
                    FilteredTransactions = new ObservableCollection<HistoryData>(HD ?? new ObservableCollection<HistoryData>());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateFilteredTransactions error: {ex.Message}");
                FilteredTransactions = new ObservableCollection<HistoryData>();
            }
        }

        private void UpdateSummaryProperties()
        {
            try
            {
                lock (_lockObject)
                {
                    var currentMonthTransactions = FilteredTransactions?.Where(t =>
                        t?.CreatedAt.Month == DateTime.Now.Month &&
                        t?.CreatedAt.Year == DateTime.Now.Year) ?? new List<HistoryData>();

                    TotalIncome = currentMonthTransactions.Where(t => t?.Amount > 0).Sum(t => t?.Amount ?? 0);
                    TotalOutcome = Math.Abs(currentMonthTransactions.Where(t => t?.Amount < 0).Sum(t => t?.Amount ?? 0));

                    OnPropertyChanged(nameof(Summary));
                    OnPropertyChanged(nameof(TotalContracts));
                    OnPropertyChanged(nameof(Intro));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateSummaryProperties error: {ex.Message}");
                TotalIncome = 0;
                TotalOutcome = 0;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
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

    public ContractList()
    {
        try
        {
            InitializeComponent();
            InitializeViewModel();
            InitializeCancellationToken();
            InitializeUIElements();
            _verifytokenPopup = new VerifyToken();
            _disputePopup = new Dispute();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ContractList initialization error: {ex.Message}");

        }
    }
    private void ClearSearchButton_Clicked(object sender, EventArgs e)
    {
        SearchEntry.Text = string.Empty;
        // This should trigger the TextChanged event automatically
    }

    private void InitializeViewModel()
    {
        try
        {
            _viewModel = new HistoryDataHeaderFooter();
            BindingContext = _viewModel;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ViewModel initialization error: {ex.Message}");
            _viewModel = new HistoryDataHeaderFooter();
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

    private void InitializeUIElements()
    {
        try
        {
            // Initialize date pickers with null checks
            if (FromDatePicker != null)
            {
                FromDatePicker.Date = DateTime.Now.AddDays(-30);
            }

            if (ToDatePicker != null)
            {
                ToDatePicker.Date = DateTime.Now;
            }

            // Initialize UI state
            ShowLoadingState(false);
            ShowErrorState(false);
            ShowEmptyState(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UI initialization error: {ex.Message}");
            // Continue with defaults even if initialization fails
        }
    }

    #region UI Event Handlers
    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed) return;
            NavigateToHomePage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Back button error: {ex.Message}");

        }
    }

    private async void DownloadButton_Clicked_Alternative(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed) return;

            // Show action sheet to choose between share and save
            var action = await DisplayActionSheet(
                "Screenshot Options",
                "Cancel",
                null,
                "Share Screenshot",
                "Save to Gallery"
            );

            if (action == "Share Screenshot")
            {
                await CaptureAndShareScreenshot();
            }
            else if (action == "Save to Gallery")
            {
                await CaptureAndSaveScreenshot();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Download button error: {ex.Message}");
            ShowErrorSnackbar($"Operation failed: {ex.Message}");
        }
    }

    private async Task CaptureAndShareScreenshot()
    {
        try
        {
            ShowLoadingState(true);
            ShowInfoSnackbar("Capturing screenshot...");

            var screenshotResult = await Screenshot.Default.CaptureAsync();

            if (screenshotResult != null)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"ContractList_{timestamp}.png";
                var filePath = Path.Combine(FileSystem.Current.CacheDirectory, filename);

                using (var fileStream = File.Create(filePath))
                {
                    await screenshotResult.CopyToAsync(fileStream);
                }

                screenshotResult = null; // Clear reference

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Contract List Screenshot",
                    File = new ShareFile(filePath)
                });

                ShowInfoSnackbar("Screenshot shared successfully!");
            }
            else
            {
                ShowErrorSnackbar("Failed to capture screenshot");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Share screenshot error: {ex.Message}");
            ShowErrorSnackbar($"Failed to share screenshot: {ex.Message}");
        }
        finally
        {
            ShowLoadingState(false);
        }
    }

    private async Task CaptureAndSaveScreenshot()
    {
        try
        {
            ShowLoadingState(true);
            ShowInfoSnackbar("Capturing and saving screenshot...");

            var screenshotResult = await Screenshot.Default.CaptureAsync();

            if (screenshotResult != null)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"ContractList_{timestamp}.png";

                // Save to device's Pictures/Screenshots folder
                var status = await Permissions.RequestAsync<Permissions.StorageWrite>();

                if (status == PermissionStatus.Granted)
                {
                    var filePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                        filename
                    );

                    using (var fileStream = File.Create(filePath))
                    {
                        await screenshotResult.CopyToAsync(fileStream);
                    }

                    ShowInfoSnackbar("Screenshot saved to Pictures folder!");
                }
                else
                {
                    ShowErrorSnackbar("Storage permission required to save screenshot");
                }

                screenshotResult = null; // Clear reference
            }
            else
            {
                ShowErrorSnackbar("Failed to capture screenshot");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save screenshot error: {ex.Message}");
            ShowErrorSnackbar($"Failed to save screenshot: {ex.Message}");
        }
        finally
        {
            ShowLoadingState(false);
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

    private void CategoryPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed) return;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Category filter error: {ex.Message}");

        }
    }

    private void StatusPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed) return;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Status filter error: {ex.Message}");

        }
    }

    private void FromDatePicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        try
        {
            if (_isDisposed) return;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"From date filter error: {ex.Message}");

        }
    }

    private void ToDatePicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        try
        {
            if (_isDisposed) return;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"To date filter error: {ex.Message}");

        }
    }

    private async void AnalysisButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed) return;
            ShowInfoSnackbar("Analysis feature coming soon!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Analysis button error: {ex.Message}");

        }
    }

    private async void RetryButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed) return;
            await LoadContractsFromApiAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Retry button error: {ex.Message}");

        }
    }



    private void OnTransactionTapped(object sender, EventArgs e)
    {
        if (sender is Grid grid && grid.BindingContext is HistoryData transaction)
        {
            transaction.IsExpanded = !transaction.IsExpanded;
        }
    }

    private void ApplyFilters()
    {
        try
        {
            lock (_lockObject)
            {
                if (_originalTransactions == null || _originalTransactions.Count == 0)
                {
                    UpdateUIState();
                    return;
                }

                var filteredResults = _originalTransactions.AsEnumerable();

                // Search filter with null checks
                var searchText = SearchEntry?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var searchTerm = searchText.ToLower();
                    filteredResults = filteredResults.Where(t =>
                        t != null && (
                            (t.Description?.ToLower().Contains(searchTerm) ?? false) ||
                            (t.SellerName?.ToLower().Contains(searchTerm) ?? false) ||
                            (t.BuyerName?.ToLower().Contains(searchTerm) ?? false) ||
                            t.Amount.ToString().Contains(searchTerm)
                        ));
                }



                // Status filter with null checks
                if (StatusPicker?.SelectedIndex > 0 && StatusPicker.Items != null && StatusPicker.SelectedIndex < StatusPicker.Items.Count)
                {
                    var selectedStatus = StatusPicker.Items[StatusPicker.SelectedIndex]?.ToLower();
                    if (!string.IsNullOrEmpty(selectedStatus) && selectedStatus != "all status")
                    {
                        filteredResults = filteredResults.Where(t =>
                            t != null && GetTransactionStatus(t).ToLower() == selectedStatus);
                    }
                }

                // Date range filter with null checks
                if (FromDatePicker != null && ToDatePicker != null)
                {
                    var fromDate = FromDatePicker.Date.Date;
                    var toDate = ToDatePicker.Date.Date.AddDays(1).AddTicks(-1);

                    filteredResults = filteredResults.Where(t =>
                        t != null && t.CreatedAt >= fromDate && t.CreatedAt <= toDate);
                }

                var filteredList = filteredResults.Where(t => t != null).OrderByDescending(t => t.CreatedAt).ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!_isDisposed && _viewModel != null)
                    {
                        try
                        {
                            _viewModel.FilteredTransactions.Clear();
                            foreach (var item in filteredList)
                            {
                                if (_isDisposed) break;
                                _viewModel.FilteredTransactions.Add(item);
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


    // Add this new event handler method to your ContractList class

    private async void StatusButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed) return;

            if (sender is Button button && button.CommandParameter is HistoryData transaction)
            {
                var status = transaction.GetTransactionStatus().ToLower();

                if (status == "pending")
                {
                    // Show action sheet for pending transactions
                    var action = await DisplayActionSheet(
                        "Pending Transaction Actions",
                        "Cancel",
                        null,
                        "Verify Token",
                        "Initiate Dispute",
                        "View Status"
                    );

                    switch (action)
                    {
                        case "Verify Token":
                            await ShowVerifyTokenPopup(transaction);
                            break;
                        case "Initiate Dispute":
                            await ShowDisputePopup(transaction);
                            break;
                        case "View Status":
                            await ShowStatusPopup(transaction);
                            break;
                    }
                }
                else
                {
                    // For non-pending transactions, show normal status popup
                    await ShowStatusPopup(transaction);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Status button error: {ex.Message}");
            ShowErrorSnackbar($"Failed to show status: {ex.Message}");
        }
    }

    private async void VerifyButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Get the transaction data if needed
            var button = sender as Button;
            var transaction = button?.CommandParameter as HistoryData; // Replace with your actual model

            // Create and present the verify token sheet
            var verifyTokenSheet = new VerifyToken();

            // If you need to pass data to the sheet, you can do it here
            // verifyTokenSheet.SetTransaction(transaction);

            // Present as modal page (this creates the sheet effect)
            await Navigation.PushModalAsync(verifyTokenSheet);
        }
        catch (Exception ex)
        {
            // Handle any errors
            await DisplayAlert("Error", "Unable to open verification sheet", "OK");
        }
    }

    private async void DisputeButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            var button = sender as Button;
            var transaction = button?.CommandParameter as HistoryData; // Replace with your actual model

            // Create and present the verify token sheet
            var DisputeSheet = new Dispute();

            // If you need to pass data to the sheet, you can do it here
            // verifyTokenSheet.SetTransaction(transaction);

            // Present as modal page (this creates the sheet effect)
            await Navigation.PushModalAsync(DisputeSheet);
            //await DisputeHelper.ShowDisputeSheetModalAsync(Navigation);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dispute button error: {ex.Message}");
            ShowErrorSnackbar($"Failed to open dispute popup: {ex.Message}");
        }
    }

    private async Task ShowVerifyTokenPopup(HistoryData transaction)
    {
        try
        {
            if (_isDisposed || _verifytokenPopup == null) return;

            // Use the CloseAsync method instead of ShowAsync, as ShowAsync is not defined for VerifyToken.  

            // Optional: Refresh the list after verification  
            // await LoadContractsFromApiAsync();  
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Verify token popup error: {ex.Message}");
            ShowErrorSnackbar("Failed to show verify token popup");
        }
    }

    private async Task ShowDisputePopup(HistoryData transaction)
    {
        try
        {


        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dispute popup error: {ex.Message}");
            ShowErrorSnackbar("Failed to show dispute popup");
        }
    }
    private async Task ShowStatusPopup(HistoryData transaction)
    {
        try
        {
            if (_isDisposed) return;

            var statusMessage = GetStatusMessage(transaction);
            var statusColor = GetStatusColor(transaction);

            var result = await DisplayAlert(
                "Transaction Status",
                statusMessage,
                "OK",
                "View Details"
            );

            // If user clicked "View Details", navigate to detail page
            if (!result)
            {
                await NavigateToDetailPage(transaction);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Status popup error: {ex.Message}");
            ShowErrorSnackbar("Failed to show status information");
        }
    }

    private string GetStatusMessage(HistoryData transaction)
    {
        try
        {
            if (transaction == null) return "Transaction information not available";

            var status = transaction.GetTransactionStatus().ToLower();

            return status switch
            {
                "pending" => $"Transaction is currently pending.\n\nAmount: {transaction.FormattedAmount}\nDate: {transaction.CreatedAt:MMM dd, yyyy HH:mm:ss}\n\nThis transaction is awaiting confirmation.",
                "successful" => $"Transaction completed successfully.\n\nAmount: {transaction.FormattedAmount}\nDate: {transaction.CreatedAt:MMM dd, yyyy HH:mm:ss}",
                "failed" => $"Transaction failed.\n\nAmount: {transaction.FormattedAmount}\nDate: {transaction.CreatedAt:MMM dd, yyyy HH:mm:ss}\n\nPlease contact support if needed.",
                "cancelled" => $"Transaction was cancelled.\n\nAmount: {transaction.FormattedAmount}\nDate: {transaction.CreatedAt:MMM dd, yyyy HH:mm:ss}",
                _ => $"Transaction status: {status}\n\nAmount: {transaction.FormattedAmount}\nDate: {transaction.CreatedAt:MMM dd, yyyy HH:mm:ss}"
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetStatusMessage error: {ex.Message}");
            return "Unable to retrieve transaction status information";
        }
    }

    private string GetStatusColor(HistoryData transaction)
    {
        try
        {
            if (transaction == null) return "#607D8B";

            var status = transaction.GetTransactionStatus().ToLower();

            return status switch
            {
                "pending" => "#FF9800",
                "successful" => "#4CAF50",
                "failed" => "#E74C3C",
                "cancelled" => "#9E9E9E",
                _ => "#607D8B"
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetStatusColor error: {ex.Message}");
            return "#607D8B";
        }
    }

    private string GetTransactionStatus(HistoryData transaction)
    {
        try
        {
            if (transaction == null) return "pending";
            return transaction.GetTransactionStatus();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTransactionStatus error: {ex.Message}");
            return "pending";
        }
    }
    #endregion
    private async void ViewDetailsButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_isDisposed) return;

            if (sender is Button button && button.CommandParameter is HistoryData transaction)
            {
                await NavigateToDetailPage(transaction);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"View details button error: {ex.Message}");
            ShowErrorSnackbar($"Failed to navigate to details: {ex.Message}");
        }
    }

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
                    if (TransactionsCollectionView != null)
                        TransactionsCollectionView.IsVisible = !show;
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
                    if (TransactionsCollectionView != null)
                        TransactionsCollectionView.IsVisible = !show;
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
                    if (TransactionsCollectionView != null)
                        TransactionsCollectionView.IsVisible = !show;
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

            var hasTransactions = _viewModel?.FilteredTransactions?.Count > 0;

            ShowLoadingState(false);
            ShowErrorState(false);
            ShowEmptyState(!hasTransactions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UI state update error: {ex.Message}");
        }
    }
    #endregion

    #region Phone Back Button Handler
    protected override bool OnBackButtonPressed()
    {
        try
        {
            if (_isDisposed) return false;

            _ = Task.Run(async () =>
            {
                try
                {
                    NavigateToHomePage();
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

    private bool NavigateToHomePage()
    {


        var navStack = Navigation.NavigationStack;
        if (navStack.Count > 1)
        {
            // Properly handle async operation
            Task.Run(async () => await Navigation.PopAsync());
        }
        else
        {
            // Handle case when there's nowhere to go back to
            // Navigate to the dashboard (TabPage)
            Application.Current.MainPage = new DashBoard();
        }

        return true; // Indicates we handled the back button press


    }
    #endregion

    #region API and Data Loading
    private async Task LoadContractsFromApiAsync()
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

                if (!await CheckNetworkConnectivity())
                {
                    ShowErrorSnackbar("No internet connection available");
                    ShowErrorState(true);
                    return;
                }

                await ConfigureAndStartLoading();
                break;
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("Contract loading cancelled");
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
                ShowErrorSnackbar($"An error occurred during contract loading: {ex.Message}");
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

    private async Task<bool> CheckNetworkConnectivity()
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

    private async Task ConfigureAndStartLoading()
    {
        try
        {
            Configurations.LoadingConfig = new LoadingConfig
            {
                Opacity = 0.4,
                DefaultMessage = "Loading Contracts Please Wait...",
                FontSize = 12,
            };

            await Loading.Instance.StartAsync(async progress =>
            {
                try
                {
                    for (var i = 0; i < 100; i++)
                    {
                        if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                            return;

                        await Task.Delay(20, _cancellationTokenSource?.Token ?? CancellationToken.None);
                        progress?.Report((i + 1) * 0.01d);
                    }

                    await FetchContractsFromServer();
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("Contract loading cancelled");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Progress loading error: {ex.Message}");
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Loading configuration error: {ex.Message}");
            throw;
        }
    }

    private async Task FetchContractsFromServer()
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

            string url = $"https://opxng.com/api/contractsapi/?email={Uri.EscapeDataString(userEmail)}";

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


            using var response = await client.GetAsync(url, _cancellationTokenSource?.Token ?? CancellationToken.None);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"API returned status code: {response.StatusCode}");
            }

            using var content = response.Content;
            var json = await content.ReadAsStringAsync();

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
            ShowErrorSnackbar("Network error. Please check your connection.");
            ShowErrorState(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fetch Error: {ex.Message}");
            ShowErrorSnackbar($"Failed to fetch contracts: {ex.Message}");
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
            var items = JsonConvert.DeserializeObject<List<HistoryData>>(json);

            if (items == null)
            {
                ShowErrorSnackbar("Invalid data format received");
                ShowErrorState(true);
                return;
            }

            if (items.Count == 0)
            {
                ShowNoContractsMessage();
                return;
            }

            var sortedItems = items.OrderByDescending(x => x.CreatedAt).Take(10).ToList();

            await UpdateUIWithContracts(sortedItems);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Response processing error: {ex.Message}");
            throw;
        }
    }

    private void ShowNoContractsMessage()
    {
        if (_isDisposed) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_isDisposed)
            {
                ShowEmptyState(true);
                System.Diagnostics.Debug.WriteLine("No contracts found");
            }
        });
    }

    private async Task UpdateUIWithContracts(List<HistoryData> contracts)
    {
        if (_isDisposed) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_isDisposed) return;

            try
            {
                _originalTransactions = contracts;

                _viewModel.HD.Clear();
                foreach (var item in contracts)
                {
                    if (_isDisposed) break;
                    _viewModel.HD.Add(item);
                }

                ApplyFilters(); // Apply any existing filters
                UpdateUIState();

                System.Diagnostics.Debug.WriteLine($"Loaded {_viewModel.HD.Count} contracts");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UI Update Error: {ex.Message}");
                ShowErrorSnackbar("Failed to update contract list");
                ShowErrorState(true);
            }
        });
    }
    #endregion

    private async void ExpandButton_Clicked_Enhanced(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            var currentParent = button.Parent;

            while (currentParent != null)
            {
                if (currentParent is StackLayout parentStack)
                {
                    var expandableContent = parentStack.Children
                        .OfType<StackLayout>()
                        .FirstOrDefault(child => child.BackgroundColor == Colors.Transparent &&
                                               child.Padding.Equals(new Thickness(15, 0, 15, 15)));

                    if (expandableContent != null)
                    {
                        if (expandableContent.IsVisible)
                        {
                            // Collapse with animation
                            await expandableContent.FadeTo(0, 200);
                            expandableContent.IsVisible = false;
                            button.Text = "▼";
                        }
                        else
                        {
                            // Expand with animation
                            expandableContent.IsVisible = true;
                            expandableContent.Opacity = 0;
                            await expandableContent.FadeTo(1, 200);
                            button.Text = "▲";
                        }

                        break;
                    }
                }
                currentParent = currentParent.Parent;
            }
        }
    }
    #region Navigation
    private async Task NavigateToDetailPage(HistoryData contract)
    {
        try
        {
            if (_isDisposed) return;

            if (Navigation == null)
            {
                ShowErrorSnackbar("Navigation not available");
                return;
            }

            await Navigation.PushModalAsync(new Views.ContractListDetail(contract));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            ShowErrorSnackbar("Failed to navigate to contract details");
        }
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
            _ = Task.Run(LoadContractsFromApiAsync);
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
                _viewModel.HD?.Clear();
                _viewModel.FilteredTransactions?.Clear();
                _viewModel = null;
            }

            BindingContext = null;
            _animatedCells?.Clear();
            _originalTransactions?.Clear();
            _filteredTransactions?.Clear();

            System.Diagnostics.Debug.WriteLine("ContractList disposed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Disposal error: {ex.Message}");
        }
    }

    #endregion

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        try
        {
            string tokennumber = string.Empty;

            // Get the token from the tapped element's binding context
            if (sender is View view && view.BindingContext is HistoryData transaction)
            {
                tokennumber = transaction.Token;
            }
            else if (sender is Label label)
            {
                // If the sender is a Label, get the text directly
                tokennumber = label.Text;
            }
            else if (sender is View senderView)
            {
                // Try to find the token from parent's binding context
                var parent = senderView.Parent;
                while (parent != null)
                {
                    if (parent.BindingContext is HistoryData parentTransaction)
                    {
                        tokennumber = parentTransaction.Token;
                        break;
                    }
                    parent = parent.Parent;
                }
            }

            if (!string.IsNullOrEmpty(tokennumber))
            {
                await Clipboard.SetTextAsync(tokennumber);
                ShowInfoSnackbar("Token number copied to clipboard");

                // Visual feedback - change color temporarily if sender is a Label
                if (sender is Label labelSender)
                {
                    var originalColor = labelSender.TextColor;
                    labelSender.TextColor = Colors.Red;
                    await Task.Delay(500);
                    labelSender.TextColor = originalColor;
                }
            }
            else
            {
                ShowErrorSnackbar("No token number found to copy");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Copy token error: {ex.Message}");
            ShowErrorSnackbar($"Failed to copy token number: {ex.Message}");
        }
    }
}