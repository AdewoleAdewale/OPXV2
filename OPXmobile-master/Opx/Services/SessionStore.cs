using Newtonsoft.Json;
using System.Net;
using Newtonsoft.Json.Linq;
using Opx.Model;
using Opx.Views;

namespace Opx.Services;

public enum SessionRestoreResult { None, Restored, Expired }

/// <summary>
/// Keeps the user signed in across app launches until they log out.
/// It stores the signed-in user's details plus the auth cookie (never the password) in SecureStorage.
/// </summary>
public static class SessionStore
{
    private const string Key = "opx_session_v1";
    private const string HasSessionFlag = "opx_has_session";
    private const string CredKey = "opx_cred_v1";
    private static readonly SemaphoreSlim ReloginLock = new(1, 1);

    private class SavedLogin
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    /// <summary>
    /// Keeps the login in Android Keystore-backed SecureStorage so the app can quietly sign in again when the
    /// server-side cookie lapses. Removed only by <see cref="Clear"/> (manual logout).
    /// </summary>
    public static async Task RememberCredentialsAsync(string? email, string? password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password)) return;
            await SecureStorage.Default.SetAsync(CredKey,
                JsonConvert.SerializeObject(new SavedLogin { Email = email, Password = password }));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SessionStore.RememberCredentials error: {ex.Message}");
        }
    }

    /// <summary>true = signed in again, false = server rejected the saved login (or none saved), null = could not reach the server.</summary>
    public static async Task<bool?> TryReloginAsync()
    {
        await ReloginLock.WaitAsync();
        try
        {
            SavedLogin? saved = null;
            try
            {
                var json = await SecureStorage.Default.GetAsync(CredKey);
                if (!string.IsNullOrWhiteSpace(json)) saved = JsonConvert.DeserializeObject<SavedLogin>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SessionStore.Relogin read error: {ex.Message}");
            }
            if (saved == null || string.IsNullOrWhiteSpace(saved.Email) || string.IsNullOrEmpty(saved.Password)) return false;

            var result = await OpxApi.PostAsync<JObject>("/AuthAccount/Login",
                new { email = saved.Email, password = saved.Password, rememberMe = true, isMobile = true });

            if (result.IsNetworkError || (int)result.StatusCode >= 500) return null;

            if (result.IsHttpSuccess && (result.Data?["success"]?.Value<bool>() ?? false))
            {
                var token = (string?)result.Data?["token"];
                if (!string.IsNullOrWhiteSpace(token)) LoginPage.mytoken = token;
                await SaveAsync();   // new cookie
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SessionStore.Relogin error: {ex.Message}");
            return null;
        }
        finally
        {
            ReloginLock.Release();
        }
    }

    /// <summary>Runs an API call; if the server says 401 it signs in again with the saved login and retries once.</summary>
    public static async Task<OpxResult<T>> WithAuthRetryAsync<T>(Func<Task<OpxResult<T>>> call) where T : class
    {
        var result = await call();
        if (result.StatusCode == HttpStatusCode.Unauthorized && await TryReloginAsync() == true)
            result = await call();
        return result;
    }

    /// <summary>Cheap synchronous check used at app start (SecureStorage itself is async-only).</summary>
    public static bool HasSavedSession => Preferences.Get(HasSessionFlag, false);

    private class Snapshot
    {
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Token { get; set; }
        public string? LedgerBalance { get; set; }
        public string? AvailableBalance { get; set; }
        public string? Completed { get; set; }
        public string? Total { get; set; }
        public string? Pending { get; set; }
        public string? Disputes { get; set; }
        public string? AccountName { get; set; }
        public string? AccountNumber { get; set; }
        public string? BankName { get; set; }
        public List<OpxApi.StoredCookie> Cookies { get; set; } = new();
    }

    /// <summary>Call after a successful login (and any time the details change) to persist the session.</summary>
    public static async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(LoginPage.myemail)) return;

            var snap = new Snapshot
            {
                Email = LoginPage.myemail,
                FullName = LoginPage.myfullname,
                Token = LoginPage.mytoken,
                LedgerBalance = LoginPage.ledgerBalance,
                AvailableBalance = LoginPage.availableBalance,
                Completed = LoginPage.completedTransactions,
                Total = LoginPage.totalTransactions,
                Pending = LoginPage.pendingTransactions,
                Disputes = LoginPage.disputes,
                AccountName = LoginPage.accountName,
                AccountNumber = LoginPage.accountNumber,
                BankName = LoginPage.bankName,
                Cookies = OpxApi.ExportCookies()
            };

            await SecureStorage.Default.SetAsync(Key, JsonConvert.SerializeObject(snap));
            Preferences.Set(HasSessionFlag, true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SessionStore.Save error: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads a saved session into memory and checks it with the server.
    /// Restored = go straight to the dashboard; Expired = the server no longer accepts it (user must log in);
    /// None = nothing saved.
    /// </summary>
    public static async Task<SessionRestoreResult> TryRestoreAsync()
    {
        Snapshot? snap = null;
        try
        {
            var json = await SecureStorage.Default.GetAsync(Key);
            if (string.IsNullOrWhiteSpace(json)) return SessionRestoreResult.None;
            snap = JsonConvert.DeserializeObject<Snapshot>(json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SessionStore.Restore read error: {ex.Message}");
        }

        if (snap == null || string.IsNullOrWhiteSpace(snap.Email))
        {
            Clear();
            return SessionRestoreResult.None;
        }

        LoginPage.myemail = snap.Email;
        LoginPage.myfullname = snap.FullName;
        LoginPage.mytoken = snap.Token;
        LoginPage.ledgerBalance = snap.LedgerBalance;
        LoginPage.availableBalance = snap.AvailableBalance;
        LoginPage.completedTransactions = snap.Completed;
        LoginPage.totalTransactions = snap.Total;
        LoginPage.pendingTransactions = snap.Pending;
        LoginPage.disputes = snap.Disputes;
        LoginPage.accountName = snap.AccountName;
        LoginPage.accountNumber = snap.AccountNumber;
        LoginPage.bankName = snap.BankName;
        OpxApi.ImportCookies(snap.Cookies);

        // Ask the server whether the saved cookie is still accepted (this also refreshes the balances).
        OpxResult<DashboardSummaryResponse> check;
        try
        {
            check = await OpxApi.GetAsync<DashboardSummaryResponse>(
                $"/dashboard/summary?email={Uri.EscapeDataString(snap.Email)}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SessionStore.Restore check error: {ex.Message}");
            return SessionRestoreResult.Restored;   // can't tell – never log the user out because of a glitch
        }

        // Offline or server trouble is not the same as an expired login – keep the user in.
        if (check.IsNetworkError || (int)check.StatusCode >= 500)
            return SessionRestoreResult.Restored;

        // The cookie lapsed but we hold the saved login: sign in again silently instead of asking the user.
        if (check.StatusCode == HttpStatusCode.Unauthorized || check.StatusCode == HttpStatusCode.Forbidden)
        {
            var relogin = await TryReloginAsync();
            if (relogin == true) return SessionRestoreResult.Restored;
            if (relogin == null) return SessionRestoreResult.Restored;   // couldn't reach the server – stay signed in
        }

        if (check.IsHttpSuccess && check.Data?.Summary != null)
        {
            var s = check.Data.Summary;
            LoginPage.availableBalance = s.AvailableBalance;
            LoginPage.ledgerBalance = s.LedgerBalance;
            LoginPage.completedTransactions = s.Completed.ToString();
            LoginPage.totalTransactions = s.Total.ToString();
            LoginPage.pendingTransactions = s.Pending.ToString();
            LoginPage.disputes = s.Disputed.ToString();
            await SaveAsync();
            return SessionRestoreResult.Restored;
        }

        // Only an explicit "not signed in" answer ends the session. Anything else (404, 400, odd payloads…) keeps the user in.
        if (check.StatusCode == HttpStatusCode.Unauthorized || check.StatusCode == HttpStatusCode.Forbidden)
        {
            Clear();
            return SessionRestoreResult.Expired;
        }

        return SessionRestoreResult.Restored;
    }

    /// <summary>Forget the saved session. Call on logout.</summary>
    public static void Clear()
    {
        try { SecureStorage.Default.Remove(Key); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"SessionStore.Clear error: {ex.Message}"); }
        try { SecureStorage.Default.Remove(CredKey); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"SessionStore.Clear creds error: {ex.Message}"); }
        try { Preferences.Remove(HasSessionFlag); } catch { }
        OpxApi.ClearSession();
        AccountOnboarding.ResetSession();   // a new login may need the setup prompt again
    }
}