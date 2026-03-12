namespace Projeto_SEGUES.Models.Enums;

public enum UserAction : byte
{
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
    Other
}