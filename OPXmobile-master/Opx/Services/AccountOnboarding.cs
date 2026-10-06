using Opx.Model;
using Opx.Views;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Opx.Services
{

    public static class AccountOnboarding
    {
        private static bool _promptedThisSession;
        private static bool _running;

        private static string PromptKey(string email) => $"kyc_auto_prompted_{email.Trim().ToLowerInvariant()}";

        /// <summary>Call on logout / session clear so the next login is evaluated afresh.</summary>
        public static void ResetSession()
        {
            _promptedThisSession = false;
            _running = false;
        }

        /// <summary>Remember a linked virtual account (also stops any further automatic prompting).</summary>
        public static void ApplyVirtualAccount(string? bankName, string? accountNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(accountNumber)) return;

                LoginPage.accountNumber = accountNumber;
                if (!string.IsNullOrWhiteSpace(bankName)) LoginPage.bankName = bankName;
                if (string.IsNullOrWhiteSpace(LoginPage.accountName)) LoginPage.accountName = LoginPage.myfullname;

                _promptedThisSession = true;
                _ = SessionStore.SaveAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyVirtualAccount error: {ex.Message}");
            }
        }

        /// <summary>true = has a virtual account, false = confirmed none, null = could not tell (offline, 401, server error).</summary>
        public static async Task<bool?> HasVirtualAccountAsync()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(LoginPage.accountNumber)) return true;

                var email = LoginPage.myemail;
                if (string.IsNullOrWhiteSpace(email)) return null;

                var result = await SessionStore.WithAuthRetryAsync(() =>
                    OpxApi.GetAsync<WalletDetailsResponse>($"/agencies/wallet-details?email={Uri.EscapeDataString(email)}"));

                if (result.IsNetworkError) return null;

                if (result.IsHttpSuccess)
                {
                    var acct = result.Data?.WalletDetails?.Accounts;
                    if (!string.IsNullOrWhiteSpace(acct?.AccountNumber))
                    {
                        ApplyVirtualAccount(acct!.Bank, acct.AccountNumber);
                        return true;
                    }
                    return false;
                }

                return result.StatusCode == HttpStatusCode.NotFound ? false : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HasVirtualAccountAsync error: {ex.Message}");
                return null;
            }
        }

        /// <summary>Called by Home when it first loads. Shows the KYC form at most once.</summary>
        public static async Task CheckAndPromptAsync(Page host)
        {
            if (_running || _promptedThisSession) return;
            _running = true;
            try
            {
                await Task.Delay(1500);   // let the dashboard settle first

                var email = LoginPage.myemail;
                if (string.IsNullOrWhiteSpace(email)) return;
                if (Preferences.Get(PromptKey(email), false)) return;   // already shown once for this user

                if (await HasVirtualAccountAsync() != false) return;    // has one, or couldn't tell

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var nav = host.Navigation;
                    // Don't stack on top of a page the user has already opened.
                    if (nav.ModalStack.Count > 0 && nav.ModalStack[^1] is not Home) return;

                    _promptedThisSession = true;
                    Preferences.Set(PromptKey(email), true);
                    await nav.PushModalAsync(new Kycform());
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Onboarding check error: {ex.Message}");
            }
            finally
            {
                _running = false;
            }
        }
    }
}

