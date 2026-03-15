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
    Other,

        // --- Ações de Negócio SEGUES (ADICIONAR ESTES) ---
    ValidateTicket,   // Quando o funcionário valida uma senha de refeição
    ValidateOrder,    // Quando o funcionário entrega um pedido do bar (Redemption Code)
    UpdateStatus,     // Quando o funcionário muda o estado de um pedido (ex: Pendente -> Pronto)

    // --- Gestão de Dados ---
    Create,           // Criar novos registos 
    Update,           // Editar registos existentes
    Delete,           // Apagar registos

}