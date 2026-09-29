using Opx.Views;

namespace Opx.Renderers
{
    public static class DisputeHelper
    {

        public static async Task ShowDisputeSheetAsync(INavigation navigation, string prefillToken = null)
        {
            try
            {
                var disputePage = new Dispute();

                // Prefill token if provided
                if (!string.IsNullOrWhiteSpace(prefillToken))
                {
                    disputePage.TokenText = prefillToken;
                }

                // Add slide-up animation
                disputePage.TranslationY = 400;
                await navigation.PushAsync(disputePage);

                // Animate slide up
                await disputePage.TranslateTo(0, 0, 300, Easing.CubicOut);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing dispute sheet: {ex.Message}");

                // Fallback - show without animation
                try
                {
                    await navigation.PushAsync(new Dispute());
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback navigation also failed: {fallbackEx.Message}");
                }
            }
        }


        public static async Task ShowDisputeSheetModalAsync(INavigation navigation, string prefillToken = null)
        {
            try
            {
                var disputePage = new Dispute();

                // Prefill token if provided
                if (!string.IsNullOrWhiteSpace(prefillToken))
                {
                    disputePage.TokenText = prefillToken;
                }

                // Show as modal
                await navigation.PushModalAsync(disputePage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing dispute modal: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Custom exceptions for dispute handling
    /// </summary>
    public class DisputeException : Exception
    {
        public DisputeException(string message) : base(message) { }
        public DisputeException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class DisputeValidationException : DisputeException
    {
        public string Field { get; }

        public DisputeValidationException(string field, string message) : base(message)
        {
            Field = field;
        }
    }

    public class DisputeNetworkException : DisputeException
    {
        public int? StatusCode { get; }

        public DisputeNetworkException(string message, int? statusCode = null) : base(message)
        {
            StatusCode = statusCode;
        }

        public DisputeNetworkException(string message, Exception innerException, int? statusCode = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}