namespace Projeto_SEGUES.Models.Enums;

public enum UserAction : byte
{
    // Identity
    LogIn,
    LogOut,
    FailedLogin,
    PasswordChange,
    RoleAssigned,
    RoleRemoved,
    TwoFactorEnabled,
    TwoFactorDisabled,
    AccountLocked,
    AccountUnlocked,
    SessionTimedOut,
    
    // Business Actions
    SuccessPayment,
    FailedPayment,
    
    // Order
    OrderSubmitted,
    OrderCancelled,
    
    // Ticket
    TransferTicket,
    TicketPurchase,

    // Employee Actions
    ValidateTicket,
    ValidateOrder,
    UpdateStatus,

    // Database Actions
    Create,
    Update,
    Delete,
    
    // General
    Other,
}