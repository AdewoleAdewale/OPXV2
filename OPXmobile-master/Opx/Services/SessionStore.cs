using Newtonsoft.Json;
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
        var check = await OpxApi.GetAsync<DashboardSummaryResponse>(
            $"/dashboard/summary?email={Uri.EscapeDataString(snap.Email)}");

        // Offline or server trouble is not the same as an expired login – keep the user in.
        if (check.IsNetworkError || (int)check.StatusCode >= 500)
            return SessionRestoreResult.Restored;

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

        Clear();
        return SessionRestoreResult.Expired;
    }

    /// <summary>Forget the saved session. Call on logout.</summary>
    public static void Clear()
    {
        try { SecureStorage.Default.Remove(Key); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"SessionStore.Clear error: {ex.Message}"); }
        OpxApi.ClearSession();
    }
}