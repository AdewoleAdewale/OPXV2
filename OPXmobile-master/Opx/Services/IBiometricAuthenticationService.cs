using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Opx.Services
{
    public interface IBiometricAuthenticationService
    {
        Task<BiometricAuthResult> AuthenticateAsync(string reason = "Authenticate to login");
        Task<bool> IsBiometricAvailableAsync();
        Task<BiometricType> GetAvailableBiometricTypeAsync();
    }

    public enum BiometricAuthResult
    {
        Success,
        Failed,
        Cancelled,
        NotAvailable,
        NotEnrolled,
        Error
    }

    public enum BiometricType
    {
        None,
        Fingerprint,
        Face,
        Iris,
        Multiple
    }
}
