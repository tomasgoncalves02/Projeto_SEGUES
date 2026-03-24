/**
 * @description Opens the log details modal and populates fields with activity data.
 * @param {string} action - The type of user action (e.g., ValidateTicket).
 * @param {string} message - The activity message with formatted user data.
 * @param {string} path - The technical URL path from the request.
 * @param {string} time - The formatted timestamp of the event.
 */
function openLogModal(action, message, path, time) {
    const modalAction = document.getElementById('modalUserAction');
    const modalMessage = document.getElementById('modalMessage');
    const modalPath = document.getElementById('modalRequestPath');
    const modalTime = document.getElementById('modalTimeStamp');

    let friendlyPath = path || "N/A";

    if (path) {
        if (path.includes("/AdminTicketManagement/Validate")) {
            friendlyPath = "Validação de Senhas";
        } else if (path.includes("/Order/UpdateStatus")) {
            friendlyPath = "Atualização do pedido Bar";
        } else if (path.includes("/Order/ValidateCode")) {
            friendlyPath = "Entrega do pedido Bar";
        }
    }
    // Mapping data to elements
    if (modalAction) modalAction.innerText = action;
    if (modalMessage) modalMessage.innerText = message || "Sem descrição disponível.";
    if (modalPath) modalPath.innerText = friendlyPath;
    if (modalTime) modalTime.innerText = time;

    // Show the modal
    const modalElement = document.getElementById('modalLogDetails');
    if (modalElement) {
        const myModal = new bootstrap.Modal(modalElement);
        myModal.show();
    }
}