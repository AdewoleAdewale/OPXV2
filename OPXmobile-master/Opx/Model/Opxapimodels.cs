using Newtonsoft.Json;

namespace Opx.Model;

// ───────────────────────── Generic ─────────────────────────
public class BasicResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

// ───────────────────────── Contracts ─────────────────────────
// POST /api/ContractsApi/initiate
public class ContractInitiateRequest
{
    public string? SellerEmail { get; set; }
    public string BuyerPhone { get; set; } = "";
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
}

public class ContractInitiateResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    // success payload
    public int? Id { get; set; }
    public string? Token { get; set; }
    public decimal? Amount { get; set; }
    public decimal? ProcessingFee { get; set; }
    public string? BuyerName { get; set; }
    public string? BuyerEmail { get; set; }
    public string? SellerName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? Status { get; set; }

    // seller wallet not set up
    public bool RequiresSetup { get; set; }
    public string? RedirectTo { get; set; }

    // buyer not registered
    public bool? Exists { get; set; }
    public string? RegistrationLink { get; set; }
    public string? BuyerPhone { get; set; }
    public string? Description { get; set; }

    // 400: insufficient balance
    public decimal? AvailableBalance { get; set; }
    public decimal? RequiredAmount { get; set; }
}

// GET /api/ContractsApi/check-buyer?phoneNumber=
public class CheckBuyerResponse
{
    public bool Success { get; set; }
    public bool Exists { get; set; }
    public string? BuyerName { get; set; }
    public string? BuyerEmail { get; set; }
    public string? BuyerPhone { get; set; }
    public string? Message { get; set; }
    public string? RegistrationLink { get; set; }
}

// POST /api/ContractsApi/confirm-delivery
public class ConfirmDeliveryRequest
{
    public string Token { get; set; } = "";
    public string Email { get; set; } = "";
}

public class ConfirmDeliveryResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? ContractId { get; set; }
    public decimal? Amount { get; set; }
    public decimal? ProcessingFee { get; set; }
    public string? SellerName { get; set; }
    public string? BuyerName { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? Reference { get; set; }
    public string? Status { get; set; }
}

// POST /api/ContractsApi/request-cancellation | approve-cancellation | reject-cancellation
public class CancellationResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ContractToken { get; set; }
    public string? Status { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}

// ───────────────────────── Auth ─────────────────────────
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}

// ───────────────────────── Agencies / Virtual account ─────────────────────────
// POST /api/agencies/create
public class CreateAgencyRequest
{
    public string Bvn { get; set; } = "";
    public string Email { get; set; } = "";
}

public class CreateAgencyResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public string? AccountReference { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? NextStep { get; set; }
}

// POST /api/agencies/link-virtual-account
public class LinkVirtualAccountRequest
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
    public string AccountRef { get; set; } = "";
}

public class LinkVirtualAccountResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public bool AlreadyLinked { get; set; }
    public string? NextStep { get; set; }
}

// ───────────────────────── Dashboard ─────────────────────────
// GET /api/dashboard/summary?email=&fromDate=&toDate=
public class DashboardSummaryResponse
{
    public DashboardSummary Summary { get; set; } = new();
    public DashboardChartData ChartData { get; set; } = new();
    public List<DashboardRecentOrder> RecentOrders { get; set; } = new();
}

public class DashboardSummary
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Pending { get; set; }
    public int Disputed { get; set; }
    // The API returns these as formatted strings ("1,250.00" style) – keep as strings for display.
    public string LedgerBalance { get; set; } = "0.00";
    public string PendingAmount { get; set; } = "0.00";
    public string AvailableBalance { get; set; } = "0.00";
}

public class DashboardChartData
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Data { get; set; } = new();
}

public class DashboardRecentOrder
{
    public string? Id { get; set; }
    public string? Names { get; set; }
    public DateTime Date { get; set; }
    public string? Status { get; set; }
    public decimal Amount { get; set; }
    public string? SellerName { get; set; }
    public string? UserImage { get; set; }
    public bool IsBuyer { get; set; }
    public bool IsConfirmed { get; set; }
    public bool IsCancellationRequested { get; set; }
    public bool? IsCancelled { get; set; }
}