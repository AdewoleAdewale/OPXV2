using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Opx.Renderers
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue ? !boolValue : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue ? !boolValue : false;
        }
    }

    // Logging Service
    public interface ILoggingService
    {
        void LogError(string message, Exception exception = null);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogDebug(string message);
    }

    public class LoggingService : ILoggingService
    {
        private readonly ILogger<LoggingService> _logger;

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
        }

        public void LogError(string message, Exception exception = null)
        {
            if (exception != null)
                _logger.LogError(exception, message);
            else
                _logger.LogError(message);
        }

        public void LogInfo(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        public void LogDebug(string message)
        {
            _logger.LogDebug(message);
        }
    }

    // Network Service
    public interface INetworkService
    {
        Task<bool> IsConnectedAsync();
        Task<NetworkStatus> GetNetworkStatusAsync();
    }

    public class NetworkService : INetworkService
    {
        public async Task<bool> IsConnectedAsync()
        {
            try
            {
                var networkAccess = Connectivity.NetworkAccess;
                return networkAccess == NetworkAccess.Internet;
            }
            catch
            {
                return false;
            }
        }

        public async Task<NetworkStatus> GetNetworkStatusAsync()
        {
            try
            {
                var networkAccess = Connectivity.NetworkAccess;
                var connectionProfiles = Connectivity.ConnectionProfiles;

                return new NetworkStatus
                {
                    IsConnected = networkAccess == NetworkAccess.Internet,
                    ConnectionType = GetConnectionType(connectionProfiles),
                    IsRoaming = false // You might want to implement this
                };
            }
            catch (Exception ex)
            {
                return new NetworkStatus
                {
                    IsConnected = false,
                    ConnectionType = ConnectionType.Unknown,
                    Error = ex.Message
                };
            }
        }

        private ConnectionType GetConnectionType(IEnumerable<ConnectionProfile> profiles)
        {
            if (profiles.Contains(ConnectionProfile.WiFi))
                return ConnectionType.WiFi;
            if (profiles.Contains(ConnectionProfile.Cellular))
                return ConnectionType.Cellular;
            if (profiles.Contains(ConnectionProfile.Ethernet))
                return ConnectionType.Ethernet;

            return ConnectionType.Unknown;
        }
    }

    public class NetworkStatus
    {
        public bool IsConnected { get; set; }
        public ConnectionType ConnectionType { get; set; }
        public bool IsRoaming { get; set; }
        public string Error { get; set; }
    }

    public enum ConnectionType
    {
        Unknown,
        WiFi,
        Cellular,
        Ethernet
    }

    // BVN Validation Service
    public interface IBvnValidationService
    {
        ValidationResult ValidateBvn(string bvn);
        bool IsValidBvnFormat(string bvn);
        string SanitizeBvn(string bvn);
    }

    public class BvnValidationService : IBvnValidationService
    {
        private const string BVN_PATTERN = @"^\d{11}$";
        private readonly Regex _bvnRegex = new Regex(BVN_PATTERN, RegexOptions.Compiled);

        public ValidationResult ValidateBvn(string bvn)
        {
            if (string.IsNullOrWhiteSpace(bvn))
                return new ValidationResult(false, "BVN is required");

            var sanitizedBvn = SanitizeBvn(bvn);

            if (sanitizedBvn.Length != 11)
                return new ValidationResult(false, "BVN must be exactly 11 digits");

            if (!_bvnRegex.IsMatch(sanitizedBvn))
                return new ValidationResult(false, "BVN must contain only numbers");

            if (IsSequentialNumbers(sanitizedBvn))
                return new ValidationResult(false, "BVN cannot be sequential numbers");

            if (IsRepeatingNumbers(sanitizedBvn))
                return new ValidationResult(false, "BVN cannot be all the same number");

            return new ValidationResult(true, "Valid BVN");
        }

        public bool IsValidBvnFormat(string bvn)
        {
            return ValidateBvn(bvn).IsValid;
        }

        public string SanitizeBvn(string bvn)
        {
            if (string.IsNullOrWhiteSpace(bvn))
                return string.Empty;

            return new string(bvn.Where(char.IsDigit).ToArray());
        }

        private bool IsSequentialNumbers(string bvn)
        {
            // Check for sequential ascending/descending numbers
            var ascending = true;
            var descending = true;

            for (int i = 1; i < bvn.Length; i++)
            {
                if (bvn[i] != bvn[i - 1] + 1)
                    ascending = false;
                if (bvn[i] != bvn[i - 1] - 1)
                    descending = false;
            }

            return ascending || descending;
        }

        private bool IsRepeatingNumbers(string bvn)
        {
            return bvn.All(c => c == bvn[0]);
        }
    }

    // Secure Storage Service
    public interface ISecureStorageService
    {
        Task<string> GetAsync(string key);
        Task SetAsync(string key, string value);
        Task RemoveAsync(string key);
        Task<bool> ContainsKeyAsync(string key);
    }

    public class SecureStorageService : ISecureStorageService
    {
        public async Task<string> GetAsync(string key)
        {
            try
            {
                return await SecureStorage.GetAsync(key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecureStorage Get Error: {ex.Message}");
                return null;
            }
        }

        public async Task SetAsync(string key, string value)
        {
            try
            {
                await SecureStorage.SetAsync(key, value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecureStorage Set Error: {ex.Message}");
                throw;
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                SecureStorage.Remove(key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SecureStorage Remove Error: {ex.Message}");
            }
        }

        public async Task<bool> ContainsKeyAsync(string key)
        {
            try
            {
                var value = await SecureStorage.GetAsync(key);
                return !string.IsNullOrEmpty(value);
            }
            catch
            {
                return false;
            }
        }
    }

    // Analytics Service
    public interface IAnalyticsService
    {
        void TrackEvent(string eventName, Dictionary<string, string> properties = null);
        void TrackError(string errorName, Exception exception, Dictionary<string, string> properties = null);
        void TrackScreenView(string screenName);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly ILoggingService _logger;

        public AnalyticsService(ILoggingService logger)
        {
            _logger = logger;
        }

        public void TrackEvent(string eventName, Dictionary<string, string> properties = null)
        {
            try
            {
                var eventData = new
                {
                    EventName = eventName,
                    Properties = properties ?? new Dictionary<string, string>(),
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonConvert.SerializeObject(eventData);
                _logger.LogInfo($"Analytics Event: {json}");

                // Here you would integrate with your analytics provider
                // e.g., AppCenter, Firebase, Application Insights, etc.
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to track analytics event", ex);
            }
        }

        public void TrackError(string errorName, Exception exception, Dictionary<string, string> properties = null)
        {
            try
            {
                var errorData = new
                {
                    ErrorName = errorName,
                    Exception = exception.ToString(),
                    Properties = properties ?? new Dictionary<string, string>(),
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonConvert.SerializeObject(errorData);
                _logger.LogError($"Analytics Error: {json}", exception);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to track analytics error", ex);
            }
        }

        public void TrackScreenView(string screenName)
        {
            TrackEvent("ScreenView", new Dictionary<string, string> { { "ScreenName", screenName } });
        }
    }

    // Configuration Service
    public interface IConfigurationService
    {
        T GetValue<T>(string key, T defaultValue = default);
        void SetValue<T>(string key, T value);
        string ApiBaseUrl { get; }
        int RequestTimeoutSeconds { get; }
        int MaxRetryAttempts { get; }
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly Dictionary<string, object> _configuration;

        public ConfigurationService()
        {
            _configuration = new Dictionary<string, object>
            {
                { "ApiBaseUrl", "https://opx.osoftpay.net/api/" },
                { "RequestTimeoutSeconds", 30 },
                { "MaxRetryAttempts", 3 },
                { "EnableLogging", true },
                { "EnableAnalytics", true }
            };
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (_configuration.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            _configuration[key] = value;
        }

        public string ApiBaseUrl => GetValue<string>("ApiBaseUrl");
        public int RequestTimeoutSeconds => GetValue<int>("RequestTimeoutSeconds");
        public int MaxRetryAttempts => GetValue<int>("MaxRetryAttempts");
    }

    // Error Handler Service
    public interface IErrorHandlerService
    {
        Task<bool> HandleErrorAsync(Exception exception, string context = null);
        Task ShowUserFriendlyErrorAsync(Exception exception, string fallbackMessage = null);
    }

    public class ErrorHandlerService : IErrorHandlerService
    {
        private readonly ILoggingService _logger;
        private readonly IAnalyticsService _analytics;

        public ErrorHandlerService(ILoggingService logger, IAnalyticsService analytics)
        {
            _logger = logger;
            _analytics = analytics;
        }

        public async Task<bool> HandleErrorAsync(Exception exception, string context = null)
        {
            try
            {
                _logger.LogError($"Error in {context ?? "Unknown"}: {exception.Message}", exception);

                _analytics.TrackError("UnhandledException", exception, new Dictionary<string, string>
                {
                    { "Context", context ?? "Unknown" },
                    { "ExceptionType", exception.GetType().Name }
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task ShowUserFriendlyErrorAsync(Exception exception, string fallbackMessage = null)
        {
            var userMessage = GetUserFriendlyMessage(exception) ?? fallbackMessage ?? "An unexpected error occurred";

            try
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", userMessage, "OK");
                }
            }
            catch
            {
                // Fallback to debug output if UI is not available
                System.Diagnostics.Debug.WriteLine($"Error Alert: {userMessage}");
            }
        }

        private string GetUserFriendlyMessage(Exception exception)
        {
            return exception switch
            {
                HttpRequestException => "Please check your internet connection and try again.",
                TaskCanceledException when exception.InnerException is TimeoutException =>
                    "The request timed out. Please try again.",
                JsonException => "There was a problem processing the server response.",
                UnauthorizedAccessException => "You don't have permission to perform this action.",
                ArgumentException => "Invalid input provided. Please check your data.",
                _ => null
            };
        }
    }

    // Validation Result Class
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string Message { get; }
        public List<string> Errors { get; }

        public ValidationResult(bool isValid, string message, List<string> errors = null)
        {
            IsValid = isValid;
            Message = message;
            Errors = errors ?? new List<string>();
        }

        public static ValidationResult Success(string message = "Valid") => new ValidationResult(true, message);
        public static ValidationResult Failure(string message, List<string> errors = null) => new ValidationResult(false, message, errors);
    }

    // Base ViewModel for MVVM support
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        protected readonly ILoggingService _logger;
        protected readonly IAnalyticsService _analytics;
        protected readonly IErrorHandlerService _errorHandler;

        protected BaseViewModel(ILoggingService logger, IAnalyticsService analytics, IErrorHandlerService errorHandler)
        {
            _logger = logger;
            _analytics = analytics;
            _errorHandler = errorHandler;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        protected async Task ExecuteAsync(Func<Task> operation, [CallerMemberName] string operationName = null)
        {
            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleErrorAsync(ex, operationName);
                throw;
            }
        }
    }


    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOpxServices(this IServiceCollection services)
        {
            // Core Services
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IAnalyticsService, AnalyticsService>();
            services.AddSingleton<INetworkService, NetworkService>();
            services.AddSingleton<ISecureStorageService, SecureStorageService>();

            // Validation Services
            services.AddSingleton<IBvnValidationService, BvnValidationService>();

            // Error Handling
            services.AddSingleton<IErrorHandlerService, ErrorHandlerService>();

            // HTTP Client Configuration
            services.AddHttpClient<BvnApiService>(client =>
            {
                client.BaseAddress = new Uri("https://opx.osoftpay.net/api/");

                client.Timeout = TimeSpan.FromSeconds(30);
            });



            return services;
        }
    }

    // Enhanced BVN API Service
    public interface IBvnApiService
    {
        Task<BvnApiResponse> VerifyBvnAsync(string bvn, CancellationToken cancellationToken = default);
        Task<bool> CheckBvnStatusAsync(string bvn, CancellationToken cancellationToken = default);
    }

    public class BvnApiService : IBvnApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoggingService _logger;
        private readonly IConfigurationService _config;
        private readonly IAnalyticsService _analytics;

        public BvnApiService(
            HttpClient httpClient,
            ILoggingService logger,
            IConfigurationService config,
            IAnalyticsService analytics)
        {
            _httpClient = httpClient;
            _logger = logger;
            _config = config;
            _analytics = analytics;
        }

        public async Task<BvnApiResponse> VerifyBvnAsync(string bvn, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInfo($"Starting BVN verification for BVN: {MaskBvn(bvn)}");

                var request = new BvnRequest { Bvn = bvn };
                var json = JsonConvert.SerializeObject(request);

                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("agencies/create", content, cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"BVN API returned error: {response.StatusCode} - {responseContent}");
                    throw new HttpRequestException($"API returned {response.StatusCode}: {responseContent}");
                }

                var result = JsonConvert.DeserializeObject<BvnApiResponse>(responseContent);

                var duration = DateTime.UtcNow - startTime;
                _analytics.TrackEvent("BvnVerificationCompleted", new Dictionary<string, string>
                {
                    { "Success", (!string.IsNullOrEmpty(result?.Success)).ToString() },
                    { "Duration", duration.TotalMilliseconds.ToString() }
                });

                _logger.LogInfo($"BVN verification completed in {duration.TotalMilliseconds}ms");

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _analytics.TrackError("BvnVerificationFailed", ex, new Dictionary<string, string>
                {
                    { "Duration", duration.TotalMilliseconds.ToString() },
                    { "ExceptionType", ex.GetType().Name }
                });

                _logger.LogError($"BVN verification failed: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> CheckBvnStatusAsync(string bvn, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInfo($"Checking BVN status for BVN: {MaskBvn(bvn)}");

                var response = await _httpClient.GetAsync($"agencies/status/{bvn}", cancellationToken);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"BVN status check failed: {ex.Message}", ex);
                return false;
            }
        }

        private string MaskBvn(string bvn)
        {
            if (string.IsNullOrEmpty(bvn) || bvn.Length < 4)
                return "****";

            return bvn.Substring(0, 2) + new string('*', bvn.Length - 4) + bvn.Substring(bvn.Length - 2);
        }
    }

    // Enhanced BVN Request/Response Models
    public class BvnRequest
    {
        [JsonProperty("bvn")]
        public string Bvn { get; set; } = string.Empty;

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; } = DeviceInfo.Current.Model;

        [JsonProperty("platform")]
        public string Platform { get; set; } = DeviceInfo.Current.Platform.ToString();

        [JsonProperty("appVersion")]
        public string AppVersion { get; set; } = AppInfo.Current.VersionString;

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class BvnApiResponse
    {
        [JsonProperty("success")]
        public string Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("agency")]
        public string Agency { get; set; }

        [JsonProperty("agencyToken")]
        public string AgencyToken { get; set; }

        [JsonProperty("nextStep")]
        public string NextStep { get; set; }

        [JsonProperty("timestamp")]
        public DateTime? Timestamp { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("expiresAt")]
        public DateTime? ExpiresAt { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; }
    }

    // Application State Manager
    public interface IApplicationStateManager
    {
        Task SaveBvnVerificationStateAsync(BvnVerificationState state);
        Task<BvnVerificationState> GetBvnVerificationStateAsync();
        Task ClearBvnVerificationStateAsync();
    }

    public class ApplicationStateManager : IApplicationStateManager
    {
        private readonly ISecureStorageService _secureStorage;
        private readonly ILoggingService _logger;
        private const string BVN_STATE_KEY = "bvn_verification_state";

        public ApplicationStateManager(ISecureStorageService secureStorage, ILoggingService logger)
        {
            _secureStorage = secureStorage;
            _logger = logger;
        }

        public async Task SaveBvnVerificationStateAsync(BvnVerificationState state)
        {
            try
            {
                var json = JsonConvert.SerializeObject(state);
                await _secureStorage.SetAsync(BVN_STATE_KEY, json);
                _logger.LogInfo("BVN verification state saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save BVN verification state", ex);
                throw;
            }
        }

        public async Task<BvnVerificationState> GetBvnVerificationStateAsync()
        {
            try
            {
                var json = await _secureStorage.GetAsync(BVN_STATE_KEY);
                if (string.IsNullOrEmpty(json))
                    return new BvnVerificationState();

                var state = JsonConvert.DeserializeObject<BvnVerificationState>(json);
                return state ?? new BvnVerificationState();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve BVN verification state", ex);
                return new BvnVerificationState();
            }
        }

        public async Task ClearBvnVerificationStateAsync()
        {
            try
            {
                await _secureStorage.RemoveAsync(BVN_STATE_KEY);
                _logger.LogInfo("BVN verification state cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to clear BVN verification state", ex);
            }
        }
    }

    public class BvnVerificationState
    {
        public string LastVerifiedBvn { get; set; }
        public DateTime? LastVerificationTime { get; set; }
        public bool IsVerified { get; set; }
        public string Agency { get; set; }
        public string AgencyToken { get; set; }
        public DateTime? TokenExpiresAt { get; set; }
        public int AttemptCount { get; set; }
        public DateTime? LastAttemptTime { get; set; }
        public List<string> ErrorHistory { get; set; } = new List<string>();
    }

    // Usage in MauiProgram.cs

}

