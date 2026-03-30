namespace Projeto_SEGUES.Models.Enums;

/// <summary>
/// A comprehensive enumeration of specific user-driven events and business actions.
/// </summary>
/// <remarks>
/// This enum categorizes activities into Identity, Business/Payments, Orders, 
/// Ticketing, and Administrative actions. It is essential for reconstructing 
/// user sessions and auditing high-value transactions.
/// Inherits from <see cref="byte"/> to ensure high-performance logging.
/// </remarks>
public enum UserAction : byte
{
    // ==========================================
    // Identity & Security Management
    // ==========================================

    /// <summary>Successful authentication into the system.</summary>
    LogIn,
    /// <summary>Intentional termination of the user session.</summary>
    LogOut,
    /// <summary>Failed authentication attempt (critical for security monitoring).</summary>
    FailedLogin,
    /// <summary>Successful update of the user's password.</summary>
    PasswordChange,
    /// <summary>Elevating or changing a user's permission set.</summary>
    RoleAssigned,
    /// <summary>Revoking a user's permission set.</summary>
    RoleRemoved,
    /// <summary>Activation of Multi-Factor Authentication.</summary>
    TwoFactorEnabled,
    /// <summary>Deactivation of Multi-Factor Authentication.</summary>
    TwoFactorDisabled,
    /// <summary>Automatic or manual lockout due to failed attempts.</summary>
    AccountLocked,
    /// <summary>Manual administrative restoration of account access.</summary>
    AccountUnlocked,
    /// <summary>Passive session termination due to inactivity.</summary>
    SessionTimedOut,

    // ==========================================
    // Business & Payment Actions
    // ==========================================

    /// <summary>Confirmation of a successful balance reload or external payment.</summary>
    SuccessPayment,
    /// <summary>Unsuccessful attempt at a financial transaction.</summary>
    FailedPayment,

    // ==========================================
    // Order Management
    // ==========================================

    /// <summary>Finalization and submission of a shopping cart.</summary>
    OrderSubmitted,
    /// <summary>Revocation of an order by the user or admin.</summary>
    OrderCancelled,

    // ==========================================
    // Ticket Operations
    // ==========================================

    /// <summary>P2P movement of a digital ticket between users.</summary>
    TransferTicket,
    /// <summary>Direct purchase of digital tickets from the platform.</summary>
    TicketPurchase,

    // ==========================================
    // Staff & Employee Operations
    // ==========================================

    /// <summary>Physical scanning/redeeming of a student's ticket.</summary>
    ValidateTicket,
    /// <summary>Handover of food or products to a student.</summary>
    ValidateOrder,
    /// <summary>Manual progression of an order through the fulfillment states.</summary>
    UpdateStatus,

    // ==========================================
    // Generic Database/CRUD Actions
    // ==========================================

    Create,
    Update,
    Delete,

    // ==========================================
    // Miscellaneous
    // ==========================================

    Other,
}