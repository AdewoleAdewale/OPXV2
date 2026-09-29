namespace Opx.Renderers
{
    public static class AccountSetupManager
    {
        private const string ACCOUNT_SETUP_KEY = "has_account_setup";
        private const string ACCOUNT_REMINDER_KEY = "account_reminder_count";
        private const string LAST_REMINDER_KEY = "last_reminder_date";
        private const string ACCOUNT_SETUP_SKIPPED_KEY = "account_setup_skipped";

        public static bool HasAccountSetup => Preferences.Get(ACCOUNT_SETUP_KEY, false);

        public static int ReminderCount => Preferences.Get(ACCOUNT_REMINDER_KEY, 0);

        public static DateTime LastReminderDate => Preferences.Get(LAST_REMINDER_KEY, DateTime.MinValue);

        public static bool HasSkippedSetup => Preferences.Get(ACCOUNT_SETUP_SKIPPED_KEY, false);

        public static void MarkAccountSetupComplete()
        {
            Preferences.Set(ACCOUNT_SETUP_KEY, true);
            Preferences.Remove(ACCOUNT_REMINDER_KEY);
            Preferences.Remove(LAST_REMINDER_KEY);
            Preferences.Remove(ACCOUNT_SETUP_SKIPPED_KEY);
        }

        public static void MarkAccountSetupSkipped()
        {
            Preferences.Set(ACCOUNT_SETUP_SKIPPED_KEY, true);
            IncrementReminderCount();
        }

        public static void IncrementReminderCount()
        {
            var count = ReminderCount + 1;
            Preferences.Set(ACCOUNT_REMINDER_KEY, count);
            Preferences.Set(LAST_REMINDER_KEY, DateTime.Now);
        }

        public static TimeSpan GetReminderInterval()
        {
            return ReminderCount switch
            {
                0 => TimeSpan.FromMinutes(5),   // First reminder after 5 minutes
                1 => TimeSpan.FromMinutes(15),  // Second reminder after 15 minutes
                2 => TimeSpan.FromHours(1),     // Third reminder after 1 hour
                3 => TimeSpan.FromHours(6),     // Fourth reminder after 6 hours
                4 => TimeSpan.FromHours(12),    // Fifth reminder after 12 hours
                _ => TimeSpan.FromDays(1)       // Daily reminders after that
            };
        }

        public static bool ShouldShowReminder()
        {
            if (HasAccountSetup) return false;

            var timeSinceLastReminder = DateTime.Now - LastReminderDate;
            return timeSinceLastReminder >= GetReminderInterval();
        }

        public static string GetReminderMessage()
        {
            return ReminderCount switch
            {
                0 => "Don't forget to set up your withdrawal account for seamless transactions!",
                1 => "Setting up your account takes just a minute and ensures faster withdrawals.",
                2 => "Your account setup is still pending. Complete it now to avoid delays.",
                3 => "Complete your account setup to enjoy all OPX features without interruption.",
                4 => "Last reminder: Set up your account now for the best experience.",
                _ => "Complete your account setup to unlock all features."
            };
        }

        public static void ResetForNewUser()
        {
            Preferences.Remove(ACCOUNT_SETUP_KEY);
            Preferences.Remove(ACCOUNT_REMINDER_KEY);
            Preferences.Remove(LAST_REMINDER_KEY);
            Preferences.Remove(ACCOUNT_SETUP_SKIPPED_KEY);
        }

        public static AccountSetupStatus GetSetupStatus()
        {
            if (HasAccountSetup)
                return AccountSetupStatus.Completed;

            if (HasSkippedSetup)
                return AccountSetupStatus.Skipped;

            return AccountSetupStatus.NotStarted;
        }
    }

    public enum AccountSetupStatus
    {
        NotStarted,
        Skipped,
        Completed
    }
}