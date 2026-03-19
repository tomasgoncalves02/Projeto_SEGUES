// js/payment/payment.js
var PaymentModule = (function () {

    // Configurações e Seletores
    const selectors = {
        amountInput: '#amountInput',
        errorDiv: '#amountError',
        paymentForm: '#paymentForm'
    };

    // Função para validar o limite de 1000€
    const validateAmount = function () {
        const input = document.querySelector(selectors.amountInput);
        const error = document.querySelector(selectors.errorDiv);
        const value = parseFloat(input.value);

        if (value > 1000) {
            error.style.display = 'block';
            error.innerText = "Só pode depositar no máximo 1000 euros de cada vez.";
            input.setCustomValidity("Limite excedido");
            return false;
        } else {
            error.style.display = 'none';
            input.setCustomValidity("");
            return true;
        }
    };

    // Inicialização dos Eventos
    return {
        init: function () {
            const input = document.querySelector(selectors.amountInput);
            if (input) {
                input.addEventListener('input', validateAmount);
            }

            console.log("Módulo de Pagamento inicializado.");
        }
    };
})();

// Iniciar quando o DOM estiver pronto
document.addEventListener('DOMContentLoaded', function () {
    PaymentModule.init();
});