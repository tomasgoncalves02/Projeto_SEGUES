/*
 * Events registration
 */
document.addEventListener("DOMContentLoaded", addEvents);

function addEvents() {
    // Swal
    const swalContainer = document.getElementById("swal-data");
    if (swalContainer && swalContainer.dataset.json && swalContainer.dataset.json.length > 0) {
        try {
            showSwal(JSON.parse(swalContainer.dataset.json));
        } catch (e) {
            console.error("Invalid JSON data for Swal:", e);
        }
    }
    
    // Password Visibility Toggle
    function setupPasswordToggle(buttonId, inputId) {
        const toggleBtn = document.getElementById( buttonId );
        const passwordInput = document.getElementById( inputId );
        if (toggleBtn && passwordInput) {
            toggleBtn.addEventListener('click', () => togglePasswordVisibility(toggleBtn, passwordInput));
        }
    }
    setupPasswordToggle('togglePassword', 'passwordInput');
    setupPasswordToggle('toggleConfirmPassword', 'confirmPasswordInput');
    
    // Verify Email Validation Code and Auto-Submit
    setupVerifyCode('verificationCodeInput');
    
    // Show Qr
    const qrBtnList = document.getElementsByClassName("showQr");
    for (let btn of qrBtnList)
        btn.addEventListener("click", () => showQr(btn.dataset.code));
    
    // Focus ticket code validation
    setupTicketCodeValidation('TicketCodeInput');
}

/*
 * Swal and Notifications Wrapper (matches C# TempDataExtensions
 */
function showSwal(options) {
    if (!options || typeof options !== "object") {
        console.error("showSwal requires a valid options object.");
        return;
    }

    const config = {
        icon: options.icon !== undefined ? options.icon : 'info',
        title: options.title || '',
        text: options.text || '',
        html: options.html || undefined,
        footer: options.footer || undefined,
        timer: options.timer || undefined,
        // Behavior
        allowOutsideClick: options.allowOutsideClick ?? true,
        allowEscapeKey: options.allowEscapeKey ?? true,
        showCloseButton: options.showCloseButton ?? false,
        backdrop: options.backdrop || "var(--ips-shadow-soft)",
        // Confirm Button
        showConfirmButton: options.showConfirmButton ?? true,
        confirmButtonText: options.confirmButtonText || 'OK',
        confirmButtonColor: options.confirmButtonColor || 'var(--ips)',
        confirmButtonAriaLabel: options.confirmButtonAriaLabel || 'OK',
        // Deny Button
        showDenyButton: options.showDenyButton ?? false,
        denyButtonText: options.denyButtonText || 'Não',
        denyButtonColor: options.denyButtonColor || 'var(--deny)',
        denyButtonAriaLabel: options.denyButtonAriaLabel || 'Não',
        // Cancel Button
        showCancelButton: options.showCancelButton ?? false,
        cancelButtonText: options.cancelButtonText || 'Cancelar',
        cancelButtonColor: options.cancelButtonColor || 'var(--cancel)',
        cancelButtonAriaLabel: options.cancelButtonAriaLabel || 'Cancelar'
    };
    return Swal.fire(config); // Return the promise
}

const Notifications = {
    // Success (Auto-close 3s, no button)
    success: function(message, html = undefined) {
        if (html) message = ''; // If HTML is provided, ignore text to avoid redundancy
        return showSwal({
            icon: 'success',
            title: 'Operação Concluída',
            text: message,
            html: html,
            timer: 3000,
            showConfirmButton: false,
            showCloseButton: true
        });
    },

    // Error (Sticky, footer link)
    error: function(message, html = undefined) {
        if (html) message = ''; // If HTML is provided, ignore text to avoid redundancy
        return showSwal({
            icon: 'error',
            title: 'Erro',
            text: message,
            html: html,
            allowOutsideClick: false,
            allowEscapeKey: false,
            footer: "Se o erro persistir, contacte o <a href='mailto:segues2026@gmail.com'>suporte</a>."
        });
    },

    // Warning (Sticky, requires click)
    warning: function(message, html = undefined) {
        if (html) message = ''; // If HTML is provided, ignore text to avoid redundancy
        return showSwal({
            icon: 'warning',
            title: 'Aviso',
            text: message,
            html: html,
            allowOutsideClick: false,
            allowEscapeKey: false
        });
    },

    // Info (Auto-close 4s)
    info: function(message, html = undefined) {
        if (html) message = ''; // If HTML is provided, ignore text to avoid redundancy
        return showSwal({
            icon: 'info',
            title: 'Informação',
            text: message,
            html: html,
            timer: 4000,
            showConfirmButton: false,
            showCloseButton: true
        });
    },

    // Confirmation (Returns Promise for logic)
    confirm: function(message, html = undefined) {
        if (html) message = ''; // If HTML is provided, ignore text to avoid redundancy
        return showSwal({
            icon: 'question',
            title: 'Confirma Operação?',
            text: message,
            html: html,
            showCancelButton: true,
            confirmButtonText: 'Sim',
            confirmButtomAriaLabel: 'Sim',
            cancelButtonText: 'Não',
            cancelButtonAriaLabel: 'Não',
            allowOutsideClick: false,
            allowEscapeKey: false
        });
    }
};

/*
 * Password Visibility Toggle
 */
function togglePasswordVisibility(toggleBtn, passwordInput) {
    const icon = toggleBtn.getElementsByTagName('i')[0];
    const isPassword = passwordInput.type === 'password';
    passwordInput.type = isPassword ? 'text' : 'password';
    if (icon) {
        icon.classList.toggle('bi-eye');
        icon.classList.toggle('bi-eye-slash');
    }
}

/*
 * Input Validation and Formatting
 */
function calculateTotal(quantityString, priceString) {
    if (!quantityString || !priceString) return "0,00 €";
    const quantity = Number(quantityString) || 0;
    const price = Number(priceString) || 0;
    return (quantity * price).toFixed(2).toString().replace('.', ',') + ' €';
}

/*
 * Code generation and verification
 */
// Verify Email Validation Code and Auto-Submit
function setupVerifyCode(inputId) {
    const codeInput = document.getElementById(inputId);
    if (!codeInput) return;
    
    const validationForm = codeInput.closest("form");
    codeInput.addEventListener("input", function () {
        // Remove any non-numeric characters
        this.value = this.value.replace(/[^0-9]/g, '');
        // Auto-submit when 6 digits are reached
        if (this.value.length === 6 && validationForm) {
            validationForm.submit();
        }
    });
}

// Display QR Code
function showQr(code) {
    showSwal({
        title: 'Senha de Refeição', 
        html: `<div class="p-3 text-color-ips">
                 <img src="https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${code}" class="mb-3 border rounded p-2 shadow-sm" alt="QR">
                 <h2 class="fw-bold text-color-ips" style="letter-spacing: 6px;">${code}</h2>
                 <p class="text-muted small mt-2">
                    Apresente este código no refeitório para validação.<br />
                    Mantém o brilho do telemóvel alto para facilitar a leitura.
                 </p>
              </div>`,
        backdrop: 'var(--ips-shadow-soft)'
    });
}

// Ticket Code Validation
function setupTicketCodeValidation(inputId) {
    const codeInput = document.getElementById(inputId);
    if (!codeInput) return;
    document.addEventListener("DOMContentLoaded", function() {
        codeInput.focus();
        codeInput.addEventListener('blur', function() { 
            setTimeout(() => this.focus(), 100); 
        });
    });
}

//Edit personal data
function showEditName(typeName,currentName) {
    showSwal({
        icon: null,
        showConfirmButton: false,
        showCloseButton: false,
        html: `
            <div class="p-4 d-flex flex-column align-items-center" style="font-family: Arial, sans-serif; color: #000;">
                
                <h2 class="fw-bold mb-4" style="font-size: 2.2rem; margin-top: 10px;">${typeName}</h2>
                
                <p class="fw-bold mb-3" style="font-size: 1.1rem;">Insira o ${typeName} pretendido</p>
                
                <input type="text" 
                       id="${typeName}" 
                       class="form-control text-center mb-4 p-2" 
                       style="max-width: 320px; width: 100%; border: 1px solid #a9a9a9; font-size: 1.4rem; color: #6c757d; border-radius: 4px;" 
                       value="${currentName}">
                
                <div class="d-flex gap-3 w-100" style="max-width: 320px;">
                    
                    <button class="btn text-white w-50 p-2" 
                            style="background-color: #009A93; font-size: 1.1rem; border-radius: 4px; transition: background-color 0.3s;" 
                            onclick="handleEditNameSubmit()">
                        Editar
                    </button>
                    
                    <button class="btn text-white w-50 p-2" 
                            style="background-color: #A6A6A6; font-size: 1.1rem; border-radius: 4px; transition: background-color 0.3s;" 
                            onclick="Swal.close()">
                        Fechar
                    </button>

                </div>
                
            </div>
        `,
        backdrop: 'var(--ips-shadow-soft)'
    });
}

// Função para processar o clique no botão
function handleEditNameSubmit() {
    const newName = document.getElementById('inputEditName').value;

    // Aqui adicionas a tua lógica para atualizar o nome na base de dados/interface
    console.log("A atualizar o nome para:", newName);

    // Fecha o modal após clicar em Editar
    Swal.close();
}

/* ==========================================
   Novas Funcionalidades: Inventário e Bar
   ========================================== */

/**
 * Filtra tabelas genericamente por texto e estado.
 * Usado no Inventário, Histórico e Pedidos.
 */
function filterTableGeneric(tableId, searchInputId, statusFilterId, rowClass) {
    const searchTerm = document.getElementById(searchInputId).value.toLowerCase();
    const status = statusFilterId ? document.getElementById(statusFilterId).value : "todos";
    const rows = document.querySelectorAll(`.${rowClass}`);

    rows.forEach(row => {
        const textContent = row.innerText.toLowerCase();
        const rowStatus = row.getAttribute('data-estado') || row.getAttribute('data-stock');

        const matchesSearch = textContent.includes(searchTerm);
        const matchesStatus = (status === "todos" || rowStatus === status);

        row.style.display = (matchesSearch && matchesStatus) ? "" : "none";
    });
}

/**
 * Lógica do Carrinho de Compras (Efetuar Pedido)
 */
let cartCount = 0;
function updateCartBadge(quantity) {
    cartCount += parseInt(quantity);
    const badge = document.getElementById('cart-count');
    if (badge) {
        badge.innerText = cartCount;
        badge.style.display = cartCount > 0 ? "block" : "none";
    }
}

/**
 * Formatação de Moeda para Modais (Portugal)
 */
function formatCurrency(value) {
    return new Intl.NumberFormat('pt-PT', {
        style: 'currency',
        currency: 'EUR'
    }).format(value);
}

/**
 * Exibe Detalhes do Produto no Inventário (BarProductViewModel)
 */
function showProductDetails(id, name, description, price, stock) {
    const nameElem = document.getElementById('view-name');
    const descElem = document.getElementById('view-description');
    const priceElem = document.getElementById('view-price');
    const stockElem = document.getElementById('view-stock');

    if (nameElem) nameElem.innerText = name;
    if (descElem) descElem.innerText = description || "Sem descrição disponível.";
    if (priceElem) priceElem.innerText = formatCurrency(price.replace(',', '.'));

    if (stockElem) {
        stockElem.innerText = stock;
        stockElem.className = "fw-bold fs-4 " +
            (stock <= 0 ? "text-danger" : (stock < 5 ? "text-warning" : "text-success"));
    }

    const modalElem = document.getElementById('productModal');
    if (modalElem) {
        new bootstrap.Modal(modalElem).show();
    }
}

/**
 * Confirmação de Eliminação Customizada usando o teu objeto Notifications
 */
function confirmDelete(id, name, formPrefix) {
    Notifications.confirm(
        '',
        `Tem a certeza que deseja eliminar <b>${name}</b>?<br>` +
        `<small class='text-muted'>Esta ação não pode ser revertida.</small>`
    ).then((result) => {
        if (result.isConfirmed) {
            document.getElementById(`${formPrefix}${id}`).submit();
        }
    });
}

// Adicionar ao carrinho (BD)
function processAddToCart(id, name) {
    const qty = document.getElementById('qty-' + id).value;

    fetch(`/Bar/OrderManagement/AddToCart?id=${id}&qty=${qty}`, { method: 'POST' })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                updateCartBadgeManual(data.count); // Usa a função de badge
                Notifications.success(`${qty}x ${name} adicionado!`);
            }
        });
}

// Remover do carrinho (BD)
function removeFromCart(id, name) {
    Notifications.confirm(`Desejas remover ${name}?`).then(res => {
        if (res.isConfirmed) {
            fetch(`/Bar/OrderManagement/RemoveFromCart?id=${id}`, { method: 'POST' })
                .then(() => location.reload()); // Refresh apenas no checkout para atualizar totais
        }
    });
}

// Helper para atualizar a badge com valor exato da BD
function updateCartBadgeManual(count) {
    const badge = document.getElementById('cart-count');
    if (badge) {
        badge.innerText = count;
        badge.style.display = count > 0 ? "block" : "none";
    }
}
// Adiciona isto no início do site.js
function confirmarCompraBar(formId) {
    // Verifica se o objeto de notificações existe
    if (typeof Notifications !== 'undefined' && Notifications.confirm) {
        Notifications.confirm("Tem a certeza que deseja efetuar este pedido?")
            .then((result) => {
                if (result.isConfirmed) {
                    const form = document.getElementById(formId);
                    if (form) form.submit();
                }
            });
    } else {
        // Fallback de segurança
        if (confirm("Tem a certeza que deseja efetuar este pedido?")) {
            document.getElementById(formId).submit();
        }
    }
}

function verDetalhesProdutos(id) {
    // 1. Mostrar spinner ou limpar dados antigos
    document.getElementById('modalTabelaCorpo').innerHTML = '<tr><td colspan="4">A carregar...</td></tr>';

    // 2. Chamada ao Controller
    fetch(`/Bar/OrderManagement/GetOrderDetails/${id}`)
        .then(response => response.json())
        .then(data => {
            // Preencher Código
            document.getElementById('modalCodigo').innerText = data.codigo;

            // Preencher Tabela
            let html = '';
            data.produtos.forEach(p => {
                html += `
                <tr>
                    <td class="fw-bold text-start">${p.nome}</td>
                    <td><button class="btn btn-sm btn-ips"><i class="bi bi-eye text-white"></i></button></td>
                    <td class="text-color-ips fw-bold">${p.preco.toFixed(2)}€</td>
                    <td>${p.quantidade}</td>
                </tr>`;
            });
            document.getElementById('modalTabelaCorpo').innerHTML = html;

            // 3. Abrir o Modal
            new bootstrap.Modal(document.getElementById('modalDetalhes')).show();
        })
        .catch(error => {
            console.error('Erro:', error);
            Notifications.error("Erro ao carregar detalhes.");
        });
}

