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

function showCode(code) {
    showSwal({
        title: 'Codigo de Pedido',
        html: `<div class="p-3 text-color-ips">
                 <h2 class="fw-bold text-color-ips" style="letter-spacing: 6px;">${code}</h2>
                 <p class="text-muted small mt-2">
                    Apresente este código no bar para validação.<br />
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
function showEdit(typeName,currentName,key) {
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
                             onclick="confirmarEdicao('${typeName}', '${key}')">
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

function showEditData(typeName, currentName, key) {
    let inputValue = '';
    if (currentName && currentName !== 'Não definido') {
        
        const parts = currentName.split('/');
        if (parts.length === 3) {
            inputValue = `${parts[2]}-${parts[1]}-${parts[0]}`;
        }
    }

    showSwal({
        icon: null,
        showConfirmButton: false,
        showCloseButton: false,
        html: `
            <div class="p-4 d-flex flex-column align-items-center" style="font-family: Arial, sans-serif; color: #000;">
                <h2 class="fw-bold mb-4" style="font-size: 2.2rem; margin-top: 10px;">${typeName}</h2>
                <p class="fw-bold mb-3" style="font-size: 1.1rem;">Insira o ${typeName} pretendido</p>
                <input type="date" 
                       id="${typeName}" 
                       class="form-control text-center mb-4 p-2" 
                       style="max-width: 320px; width: 100%;" 
                       value="${inputValue}">
                <div class="d-flex gap-3 w-100" style="max-width: 320px;">
                    <button class="btn text-white w-50 p-2"
                            style="background-color: #009A93; font-size: 1.1rem; border-radius: 4px; transition: background-color 0.3s;"
                             onclick="confirmarEdicao('${typeName}', '${key}')">
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



function showEditGenre(typeName, currentName, key) {
    fetch('/User/User/GetGenders')
        .then(r => r.json())
        .then(genders => {
            const options = genders.map(g =>
                `<option value="${g.value}" ${currentName === g.text ? 'selected' : ''}>${g.text}</option>`
            ).join('');

            showSwal({
                icon: null,
                showConfirmButton: false,
                showCloseButton: false,
                html: `
                    <div class="p-4 d-flex flex-column align-items-center" style="font-family: Arial, sans-serif; color: #000;">
                        <h2 class="fw-bold mb-4" style="font-size: 2.2rem; margin-top: 10px;">${typeName}</h2>
                        <p class="fw-bold mb-3" style="font-size: 1.1rem;">Insira o ${typeName} pretendido</p>
                       <select id="genreSelect" class="form-select mb-4">
                        <option value="">Selecione...</option>
                        ${options}
                        </select>
                        <div class="d-flex gap-3 w-100" style="max-width: 320px;">
                             <button class="btn text-white w-50 p-2"
                            style="background-color: #009A93; font-size: 1.1rem; border-radius: 4px; transition: background-color 0.3s;"
                             onclick="confirmarEdicao('${typeName}', '${key}')">
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
        }); 
}


function showEditPassword(typeName, key) {
    showSwal({
        icon: null,
        showConfirmButton: false,
        showCloseButton: false,
        html: `
            <div class="p-4 d-flex flex-column align-items-center" style="font-family: Arial, sans-serif; color: #000;">
                
                <h2 class="fw-bold mb-4" style="font-size: 2.2rem; margin-top: 10px;">${typeName}</h2>
                
                <p class="fw-bold mb-3" style="font-size: 1.1rem;">Altere a sua o ${typeName}</p>
                
                <input type="password" 
                       placeholder="Password Antiga"
                       id="oldpassword" 
                       class="form-control text-center mb-4 p-2" 
                       style="max-width: 320px; width: 100%; border: 1px solid #a9a9a9; font-size: 1.4rem; color: #6c757d; border-radius: 4px;" 
                       value="">

                <input type="password"
                       placeholder="Nova Password"
                       id="newpassword" 
                       class="form-control text-center mb-4 p-2" 
                       style="max-width: 320px; width: 100%; border: 1px solid #a9a9a9; font-size: 1.4rem; color: #6c757d; border-radius: 4px;" 
                       value="">

                <input type="password"
                       id="confirmnewpassword"
                       placeholder="Confirme a Nova Password"
                       class="form-control text-center mb-4 p-2" 
                       style="max-width: 320px; width: 100%; border: 1px solid #a9a9a9; font-size: 1.4rem; color: #6c757d; border-radius: 4px;" 
                       value="">
                
                <div class="d-flex gap-3 w-100" style="max-width: 320px;">
                    
                    <button class="btn text-white w-50 p-2" 
                            style="background-color: #009A93; font-size: 1.1rem; border-radius: 4px; transition: background-color 0.3s;" 
                             onclick="confirmarEdicao('${typeName}', '${key}')">
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


function confirmarEdicao(typeName, key) {
    let value;

    if (key === 'genre') {
        value = document.getElementById('genreSelect').value;
    } else {
        value = document.getElementById(typeName)?.value;
    }


    Notifications.confirm(`Tens a certeza que queres alterar ${typeName}?`)
        .then(r => {
            if (r.isConfirmed) {

                if (typeName == 'password') {
                    handlePasswordSubmit(typeName, key, value);
                } else {
                    handleEditSubmit(typeName, key, value);
                }

                
            }
        });
}



function handlePasswordSubmit() {
    const currentPassword = document.getElementById('oldpassword').value;
    const newPassword = document.getElementById('newpassword').value;
    const confirmPassword = document.getElementById('confirmnewpassword').value;

    if (newPassword === currentPassword) {
        Notifications.error("A nova password não pode ser igual à password atual.");
        return;
    }


    if (newPassword !== confirmPassword) {
        Notifications.error("As passwords não coincidem.");
        return;
    }



    fetch('/User/User/UpdatePassword', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        },
        body: `currentPassword=${encodeURIComponent(currentPassword)}&newPassword=${encodeURIComponent(newPassword)}`
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                Swal.close(); 
                setTimeout(() => {
                    Notifications.success("Password atualizada com sucesso!");
                    setTimeout(() => location.reload(), 1500);
                }, 300);
            } else {
                Notifications.error(data.message || "Erro ao atualizar password.");
            }
        });
}

function handleEditSubmit(typeName, key, value) {
    fetch('/User/User/UpdateType', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        },
        body: `key=${key}&value=${encodeURIComponent(value)}`
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                Swal.close();
                setTimeout(() => {
                    Notifications.success(typeName + " atualizado com sucesso!");
                    setTimeout(() => location.reload(), 1500);
                }, 300);
            } else {
                Notifications.error(data.message || "Erro ao atualizar.");
            }
        })
        .catch(err => {
            console.error(err);
            Notifications.error("Erro de comunicação com o servidor.");
        });
}

//-----------------------------------------------------------------------------------------

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

// REMOVER DO CARRINHO (Corrigido para UserOrders)
function removeFromCart(id, name) {
    Notifications.confirm(`Desejas remover ${name}?`).then(res => {
        if (res.isConfirmed) {
            // Alterado para UserOrders e corrigido o formato do ID no URL
            fetch(`/Bar/UserOrders/RemoveFromCart/${id}`, {
                method: 'POST',
                headers: {
                    // Importante para não dar erro 400 Bad Request
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                }
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        location.reload(); // Refresh para atualizar os totais no Checkout
                    } else {
                        Notifications.error("Não foi possível remover o item.");
                    }
                });
        }
    });
}

// DETALHES DO PEDIDO (Unificada e corrigida para UserOrders)
function verDetalhesProdutos(id) {
    const modalElement = document.getElementById('modalDetalhes');
    const contentElement = document.getElementById('modalContentBody');

    if (!modalElement || !contentElement) {
        console.error("Elementos do modal não encontrados no HTML.");
        return;
    }

    contentElement.innerHTML = '<div class="p-5 text-center"><div class="spinner-border text-color-ips"></div></div>';

    // Alterado para UserOrders
    fetch(`/Bar/UserOrders/GetOrderDetails/${id}`)
        .then(res => res.json())
        .then(data => {
            let pRows = data.produtos.map(p => `
                <tr>
                    <td class="text-start fw-bold">${p.nome}</td>
                    <td class="text-color-ips fw-bold">${p.preco.toFixed(2)}€</td>
                    <td class="fw-bold">${p.quantidade}</td>
                </tr>
            `).join('');

            contentElement.innerHTML = `
                <div class="modal-body p-4 text-center">
                    <div class="mb-3"><i class="bi bi-info-circle text-info" style="font-size: 4rem;"></i></div>
                    <h2 class="fw-bold mb-4">Detalhes do Pedido</h2>
                    <div class="text-start mb-3">
                        <p class="mb-0 text-muted small fw-bold">Código</p>
                        <h4 class="text-color-ips fw-bold">${data.codigo}</h4>
                    </div>
                    <div class="table-responsive border rounded-3">
                        <table class="table table-hover mb-0">
                            <thead class="bg-color-ips text-white small">
                                <tr><th>Nome</th><th>Preço</th><th>Qtd</th></tr>
                            </thead>
                            <tbody>${pRows}</tbody>
                        </table>
                    </div>
                    <button type="button" class="btn btn-ips mt-4 px-5 py-2 fw-bold" data-bs-dismiss="modal">Fechar</button>
                </div>`;

            new bootstrap.Modal(modalElement).show();
        })
        .catch(err => {
            console.error(err);
            Notifications.error("Erro ao carregar os detalhes.");
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
    const modalElement = document.getElementById('modalDetalhes');
    const contentElement = document.getElementById('modalContentBody');

    if (!modalElement || !contentElement) return;

    contentElement.innerHTML = '<div class="p-5 text-center"><div class="spinner-border text-color-ips"></div></div>';

    fetch(`/Bar/UserOrders/GetOrderDetails/${id}`)
        .then(res => res.json())
        .then(data => {
            // Validação de segurança: se não houver produtos, mostra mensagem amigável
            if (!data.produtos || data.produtos.length === 0) {
                contentElement.innerHTML = '<div class="p-4">Nenhum detalhe encontrado.</div>';
                return;
            }

            let pRows = data.produtos.map(p => `
                <tr>
                    <td class="text-start fw-bold">${p.nome}</td>
                    <td class="text-color-ips fw-bold">${p.preco.toFixed(2)}€</td>
                    <td class="fw-bold text-center">${p.quantidade}</td>
                </tr>
            `).join('');

            contentElement.innerHTML = `
                <div class="modal-body p-4 text-center">
                    <div class="mb-3"><i class="bi bi-info-circle text-info" style="font-size: 4rem;"></i></div>
                    <h2 class="fw-bold mb-4">Detalhes do Pedido</h2>
                    <div class="text-start mb-3">
                        <p class="mb-0 text-muted small fw-bold text-uppercase">Código de Recolha</p>
                        <h4 class="text-color-ips fw-bold" style="letter-spacing: 2px;">${data.codigo}</h4>
                    </div>
                    <div class="table-responsive border rounded-3 shadow-sm">
                        <table class="table table-hover mb-0">
                            <thead class="bg-color-ips text-white small">
                                <tr>
                                    <th class="text-start">Produto</th>
                                    <th>Preço</th>
                                    <th>Qtd</th>
                                </tr>
                            </thead>
                            <tbody>${pRows}</tbody>
                        </table>
                    </div>
                    <button type="button" class="btn btn-ips mt-4 px-5 py-2 fw-bold w-100" data-bs-dismiss="modal">FECHAR</button>
                </div>`;

            new bootstrap.Modal(modalElement).show();
        })
        .catch(err => {
            console.error("Erro ao carregar detalhes:", err);
            Notifications.error("Não foi possível carregar os detalhes do pedido.");
        });
}

/**
 * Lógica de atualização de estado com validação de código para entrega
 */
function handleUpdate() {
    const statusSelect = document.getElementById('statusSelect');
    const orderIdInput = document.getElementById('orderId');

    const status = statusSelect.value;
    const orderId = orderIdInput.value;

    // CASO 1: SE FOR ENTREGUE (STATUS 3) -> ABRE POPUP
    if (status === "3") {
        Swal.fire({
            title: 'Validar Entrega',
            text: 'Introduza o código do cliente:',
            input: 'text',
            inputAttributes: { autocapitalize: 'characters' },
            showCancelButton: true,
            confirmButtonText: 'Validar',
            confirmButtonColor: 'var(--ips)',
            showLoaderOnConfirm: true,
            preConfirm: (code) => {
                return fetch(`/Bar/OrderManagement/ValidateOrderCode?id=${orderId}&codeEntered=${code}`, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value }
                })
                    .then(response => {
                        if (!response.ok) return response.json().then(data => { throw new Error(data.message) });
                        return response.json();
                    })
                    .catch(error => { Swal.showValidationMessage(error.message) });
            }
        }).then((result) => {
            if (result.isConfirmed) {
                Notifications.success("Pedido entregue!");
                // Atualiza o badge para Entregue em vez de remover
                const row = document.querySelector(`tr[data-order-id="${orderId}"]`);
                if (row) {
                    const badge = row.querySelector('.badge');
                    if (badge) {
                        badge.className = 'badge bg-success';
                        badge.textContent = 'Entregue';
                    }
                }
                // Remove após 5 segundos
                setTimeout(() => {
                    row?.remove();
                }, 5000);
            }
        });
    }
    // CASO 2: OUTROS ESTADOS (PENDENTE, PREPARAÇÃO, PRONTO) -> UPDATE DIRETO
    else {
        // Fazemos um fetch para a Action UpdateStatus que já tinhas
        fetch(`/Bar/OrderManagement/UpdateStatus`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: `id=${orderId}&newStatus=${status}`
        })
            .then(response => {
                if (response.ok) {
                    Notifications.success("Estado atualizado com sucesso!");
                    // Se usas HTMX para o painel lateral, podes recarregar apenas o painel
                    // Para simplificar agora, vamos recarregar a página:
                    setTimeout(() => window.location.reload(), 1000);
                } else {
                    Notifications.error("Erro ao atualizar o estado.");
                }
            });

    }
}

function showProductDetails(name, description, price) {
    Swal.fire({
        title: '<strong>Detalhes do Produto</strong>',
        icon: 'info',
        html: `
                    <div class="text-start mt-3">
                        <small class="text-muted fw-bold text-uppercase d-block mb-1" style="font-size: 0.7rem;">Nome do Produto</small>
                        <h5 class="fw-bold mb-4" style="color: #009697;">${name}</h5>

                        <small class="text-muted fw-bold text-uppercase d-block mb-1" style="font-size: 0.7rem;">Descrição</small>
                        <p class="text-muted small mb-4">${description || 'Nenhuma descrição disponível para este produto.'}</p>

                        <div class="card bg-light border-0 shadow-sm rounded-3">
                            <div class="card-body text-center py-3">
                                <small class="text-muted fw-bold text-uppercase d-block mb-1" style="font-size: 0.7rem;">Preço</small>
                                <h3 class="fw-bold text-dark mb-0">${price}</h3>
                            </div>
                        </div>
                    </div>
                `,
        showCloseButton: false,
        focusConfirm: false,
        confirmButtonText: 'Fechar',
        confirmButtonColor: '#009697',
        customClass: {
            popup: 'rounded-4 shadow-lg',
            confirmButton: 'px-4 py-2 fw-bold rounded-3 shadow-sm'
        }
    });
}

function confirmarCancelamento(form) {
    Notifications.confirm("Tens a certeza que queres cancelar este pedido?")
        .then(result => {
            if (result.isConfirmed) form.submit();
        });
}

function confirmarEdição(type,form) {
    Notifications.confirm("Tens a certeza que queres alterar o teu " + type + " ?")
        .then(result => {
            if (result.isConfirmed) form.submit();
        });
}



