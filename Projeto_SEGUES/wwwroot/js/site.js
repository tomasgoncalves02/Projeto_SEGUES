

const Notifications = {
    // Success (Auto-close 3s, no button)
    success: function (message, html = undefined) {
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
    error: function (message, html = undefined) {
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
    warning: function (message, html = undefined) {
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
    info: function (message, html = undefined, showConfirmButton = false, timer = 4000) {
        if (html) message = ''; // If HTML is provided, ignore text to avoid redundancy
        return showSwal({
            icon: 'info',
            title: 'Informação',
            text: message,
            html: html,
            timer: timer,
            showConfirmButton: showConfirmButton,
            showCloseButton: true
        });
    },

    // Confirmation (Returns Promise for logic)
    confirm: function (message, html = undefined) {
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
 * Events registration
 */
document.addEventListener("DOMContentLoaded", addEvents);




function addEvents() {

    const periodSelectR = document.getElementById('periodSelectR');
    if (periodSelectR) {
        loadMealsSummary(); 
        periodSelectR.addEventListener('change', loadMealsSummary); 
    }
    const periodSelectB = document.getElementById('periodSelectB');
    if (periodSelectB) {
        loadBarSummary(); 
        periodSelectB.addEventListener('change', loadBarSummary); 
    }


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

function escolherEmenta() {
    Swal.fire({
        title: 'Qual ementa deseja visualizar?',
        icon: 'question',
        showCancelButton: true,
        showDenyButton: true,
        confirmButtonText: 'Refeitório',
        denyButtonText: 'Bar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#009697', 
        denyButtonColor: '#009697',    
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {
            window.open("https://software.movelife.net/pt-PT/Menus/PublicCC/Tj6o3O_vCFB2LmCmm9VUjw%3d%3d", "_blank");
        } else if (result.isDenied) {
            window.open(" https://software.movelife.net/pt-PT/Menus/PublicCC/Tj6o3O_vCFDXvHU0nbgTmg%3d%3d?DaySelected=13%2F03%2F2026&capit=5&idzone=1616", "_blank");
        }
    });
}

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
function showEdit(typeName, currentName, key) {
    Swal.fire({
        title: `<h2 class="fw-bold mb-4">${typeName}</h2>`,
        html: `
            <div class="p-2 d-flex flex-column align-items-center">
                <input type="text" id="editInput" class="form-control" placeholder="${typeName}" value="${currentName}">
            </div>`,
        showCancelButton: true,
        confirmButtonText: 'Editar',
        confirmButtonColor: '#009A93',
        cancelButtonText: 'Fechar',
        preConfirm: () => {
            const value = Swal.getPopup().querySelector('#editInput').value;
            if (!value) {
                Swal.showValidationMessage(`Por favor, preencha o campo`);
                return false;
            }
            return { value };
        }
    }).then((result) => {
        if (result.isConfirmed) {
            handleEditSubmit(typeName, key, result.value.value);
        }
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

    Swal.fire({
        title: `<h2 class="fw-bold mb-4">${typeName}</h2>`,
        html: `
            <div class="p-2 d-flex flex-column align-items-center">
                <input type="date" id="editInput" class="form-control" value="${inputValue}">
            </div>`,
        showCancelButton: true,
        confirmButtonText: 'Editar',
        confirmButtonColor: '#009A93',
        cancelButtonText: 'Fechar',
        preConfirm: () => {
            const value = Swal.getPopup().querySelector('#editInput').value;
            if (!value) {
                Swal.showValidationMessage(`Por favor, selecione uma data`);
                return false;
            }
            return { value };
        }
    }).then((result) => {
        if (result.isConfirmed) {
            handleEditSubmit(typeName, key, result.value.value);
        }
    });
}

function showEditGenre(typeName, currentName, key) {
    fetch('/User/User/GetGenders')
        .then(r => r.json())
        .then(genders => {
            const options = genders.map(g =>
                `<option value="${g.value}" ${currentName === g.text ? 'selected' : ''}>${g.text}</option>`
            ).join('');

            Swal.fire({
                title: `<h2 class="fw-bold mb-4">${typeName}</h2>`,
                html: `
                    <div class="p-2 d-flex flex-column align-items-center">
                        <select id="genreSelect" class="form-select">
                            <option value="">Selecione...</option>
                            ${options}
                        </select>
                    </div>`,
                showCancelButton: true,
                confirmButtonText: 'Editar',
                confirmButtonColor: '#009A93',
                cancelButtonText: 'Fechar',
                preConfirm: () => {
                    const value = Swal.getPopup().querySelector('#genreSelect').value;
                    if (!value) {
                        Swal.showValidationMessage(`Por favor, selecione um género`);
                        return false;
                    }
                    return { value };
                }
            }).then((result) => {
                if (result.isConfirmed) {
                    handleEditSubmit(typeName, key, result.value.value);
                }
            });
        });
}

function showEditPassword(typeName, key) {
    Swal.fire({
        title: `<h2 class="fw-bold mb-4">${typeName}</h2>`,
        html: `
            <div class="p-2 d-flex flex-column align-items-center">

                    <div class="input-group mb-3">
            <input type="password" id="oldpassword" class="form-control" placeholder="Password Atual">
            <button class="btn btn-ips-outline-secondary px-3" type="button" onclick="togglePasswordVisibility(this, document.getElementById('oldpassword'))">
                <i class="bi fs-5 bi-eye"></i>
            </button>
        </div>
        <div class="input-group mb-3">
            <input type="password" id="newpassword" class="form-control" placeholder="Nova Password">
            <button class="btn btn-ips-outline-secondary px-3" type="button" onclick="togglePasswordVisibility(this, document.getElementById('newpassword'))">
                <i class="bi fs-5 bi-eye"></i>
            </button>
        </div>
        <div class="input-group mb-3">
            <input type="password" id="confirmnewpassword" class="form-control" placeholder="Confirme a Nova Password">
            <button class="btn btn-ips-outline-secondary px-3" type="button" onclick="togglePasswordVisibility(this, document.getElementById('confirmnewpassword'))">
                <i class="bi fs-5 bi-eye"></i>
            </button>
        </div>
  
            </div>`,
        showCancelButton: true,
        confirmButtonText: 'Editar',
        confirmButtonColor: '#009A93',
        cancelButtonText: 'Fechar',
        preConfirm: () => {
            const currentPassword = Swal.getPopup().querySelector('#oldpassword').value;
            const newPassword = Swal.getPopup().querySelector('#newpassword').value;
            const confirmPassword = Swal.getPopup().querySelector('#confirmnewpassword').value;
            const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$/;

            if (!currentPassword || !newPassword || !confirmPassword) {
                Swal.showValidationMessage(`Por favor, preencha todos os campos`);
                return false;
            }
            if (newPassword !== confirmPassword) {
                Swal.showValidationMessage(`As passwords novas não coincidem`);
                return false;
            }
            if (!passwordRegex.test(newPassword)) {
                Swal.showValidationMessage(`A password deve ter mínimo 12 caracteres, uma maiúscula, uma minúscula, um número e um símbolo (@$!%*?&)`);
                return false;
            }

          
                

            return { currentPassword, newPassword };
        }
    }).then((result) => {
        if (result.isConfirmed) {
            enviarNovaPassword(result.value.currentPassword, result.value.newPassword);
        }
    });
}

function enviarNovaPassword(currentPassword, newPassword) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const params = new URLSearchParams();
    params.append('currentPassword', currentPassword);
    params.append('newPassword', newPassword);

    fetch('/User/User/UpdatePassword', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token 
        },
        body: params.toString()
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                Notifications.success("Password alterada!");
                setTimeout(() => location.reload(), 1500);
            } else {
                Notifications.error(data.message || "Erro ao atualizar password.");
            }
        })
        .catch(() => Notifications.error("Erro de comunicação."));
}


function confirmarEdicao(typeName, key) {
    let value = "";

    // Evita tentar ler valor de um campo de password que não existe como input único
    if (key === 'genre') {
        value = document.getElementById('genreSelect')?.value;
    } else if (key !== 'password') {
        value = document.getElementById(typeName)?.value;
    }

    Notifications.confirm(`Tens a certeza que queres alterar ${typeName}?`)
        .then(r => {
            if (r.isConfirmed) {
                if (key === 'password') {
                    handlePasswordSubmit(); // Agora sem parâmetros
                } else {
                    handleEditSubmit(typeName, key, value);
                }
            }
        });
}



function handlePasswordSubmit() {
    // Adicionei verificações de existência para evitar o erro "null reading value"
    const oldInput = document.getElementById('oldpassword');
    const newInput = document.getElementById('newpassword');
    const confirmInput = document.getElementById('confirmnewpassword');

    if (!oldInput || !newInput || !confirmInput) {
        console.error("Erro: Inputs de password não encontrados no DOM.");
        return;
    }

    const currentPassword = oldInput.value;
    const newPassword = newInput.value;
    const confirmPassword = confirmInput.value;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    if (newPassword !== confirmPassword) {
        Notifications.error("As passwords não coincidem.");
        return;
    }

    // Usar URLSearchParams garante que o formato enviado não causa erro 400
    const bodyParams = new URLSearchParams();
    bodyParams.append('currentPassword', currentPassword);
    bodyParams.append('newPassword', newPassword);

    fetch('/User/User/UpdatePassword', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        },
        body: bodyParams.toString()
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                Swal.close();
                Notifications.success("Password atualizada!");
                setTimeout(() => location.reload(), 1500);
            } else {
                Notifications.error(data.message || "Erro ao atualizar.");
            }
        })
        .catch(() => Notifications.error("Erro de comunicação com o servidor."));
}

function handleEditSubmit(typeName, key, value) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const bodyParams = new URLSearchParams();
    bodyParams.append('key', key);
    bodyParams.append('value', value);

    fetch('/User/User/UpdateType', { 
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token 
        },
        body: bodyParams.toString()
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                Swal.close();
                Notifications.success(typeName + " atualizado!");
                setTimeout(() => location.reload(), 1500);
            } else {
                Notifications.error(data.message || "Erro na atualização.");
            }
        })
        .catch(() => Notifications.error("Erro de rede."));
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

// REMOVER DO CARRINHO (Ajustado para CreateOrderController)
function removeFromCart(id, name) {
    Notifications.confirm(`Desejas remover ${name} do carrinho?`).then(res => {
        if (res.isConfirmed) {
            fetch(`/Order/CreateOrder/RemoveFromCart?id=${id}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                }
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        location.reload();
                    } else {
                        Notifications.error(data.message || "Não foi possível remover o item.");
                    }
                })
                .catch(err => {
                    console.error("Erro na remoção:", err);
                    Notifications.error("Erro de comunicação com o servidor.");
                });
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

function verDetalhesProdutos(id, urlBase = '/Report/ReportOrder/GetOrderDetails') {
    // 1. Mostrar Spinner de carregamento
    Notifications.info(null, '<div id="detailsModal" class="p-5 text-center"><div class="spinner-border text-color-ips"></div></div>', true, 0);
    let detailsModal = document.getElementById('detailsModal');

    // 2. Fazer o fetch para a URL fornecida
    fetch(`${urlBase}/${id}`)
        .then(res => {
            if (!res.ok) throw new Error("Erro ao aceder aos detalhes");
            return res.json();
        })
        .then(data => {
            // Validação: se não houver produtos ou dados
            if (!data || !data.produtos || data.produtos.length === 0) {
                detailsModal.innerHTML = '<div class="p-4 text-center text-muted">Nenhum detalhe encontrado para este pedido.</div>';
                return;
            }

            // Gerar linhas da tabela
            let pRows = data.produtos.map(p => `
                <tr>
                    <td class="text-start fw-bold">${p.nome}</td>
                    <td class="text-color-ips fw-bold">${p.preco.toFixed(2)}€</td>
                    <td class="fw-bold text-center">${p.quantidade}</td>
                </tr>
            `).join('');

            // Injetar o conteúdo final no modal
            detailsModal.innerHTML = `
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
                        <tbody class="text-center">${pRows}</tbody>
                    </table>
                </div>`;
        })
        .catch(err => {
            console.error("Erro ao carregar detalhes:", err);
            detailsModal.innerHTML = '<div class="p-4 text-danger text-center">Erro ao carregar detalhes. Verifique as permissões.</div>';
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

    // CASO 1: SE FOR ENTREGUE (STATUS 4) -> ABRE POPUP
    if (status === "4") {
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
                return fetch(`/Order/OrderManagement/ValidateOrderCode?id=${orderId}&codeEntered=${code}`, {
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
        fetch(`/Order/OrderManagement/UpdateStatus`, {
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


// ── Estatísticas: Resumo de Refeições ────────────────────────────────────────

let mealsChart;
let barChartB;
let productCategoryChart;

function renderChart(data, period) {
    const config = {
        '1': { sub: 'Refeições por hora hoje', x: 'Horas' },
        '2': { sub: 'Refeições por dia esta semana', x: 'Dias da Semana' },
        '3': { sub: 'Refeições por dia este mês', x: 'Dias do Mês' },
        '4': { sub: 'Refeições por mês este ano', x: 'Meses' },
        '5': { sub: 'Refeições por mês (Ano Atual)', x: 'Meses do Ano' }
    };

    const currentConfig = config[period] || { sub: '', x: 'Tempo' };

    const canvas = document.getElementById('mealsChart');
    if (!canvas) return; 

    const ctx = canvas.getContext('2d');

    if (mealsChart) {
        mealsChart.destroy();
    }

    const subtitleElem = document.getElementById('chartSubtitle');
    if (subtitleElem) subtitleElem.textContent = currentConfig.sub;
    
    const chartLabels = data.map(d => d.label);
    const chartValues = data.map(d => d.count);

    mealsChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: chartLabels,
            datasets: [{
                label: 'Refeições',
                data: chartValues,
                borderColor: 'darkcyan',
                backgroundColor: 'rgba(0,139,139,0.15)', 
                borderWidth: 3,
                pointRadius: 5,
                pointHoverRadius: 7,
                pointBackgroundColor: 'darkcyan',
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    title: {
                        display: true,
                        text: currentConfig.x, 
                        color: '#6c757d',
                        font: {
                            size: 12,
                            weight: 'bold'
                        },
                        padding: { top: 10 }
                    },
                    grid: { display: false }
                },
                y: {
                    beginAtZero: true,
                    grace: '10%',
                    title: {
                        display: true,
                        text: 'Nº Senhas',
                        color: '#6c757d',
                        font: {
                            size: 12,
                            weight: 'bold'
                        },
                        padding: { bottom: 10 }
                    },
                    ticks: {
                        stepSize: 1,
                        precision: 0,
                        color: '#6c757d'
                    },
                    grid: {
                        color: 'rgba(0,0,0,0.04)'
                    }
                }
            }
        }
    });
}


function renderChartB(data, period) {
    const config = {
        '1': { sub: 'Pedidos por hora hoje', x: 'Horas' },
        '2': { sub: 'Pedidos por dia esta semana', x: 'Dias da Semana' },
        '3': { sub: 'Pedidos por dia este mês', x: 'Dias do Mês' },
        '4': { sub: 'Pedidos por mês este ano', x: 'Meses' },
        '5': { sub: 'Pedidos por mês (Ano Atual)', x: 'Meses do Ano' }
    };

    const currentConfig = config[period] || { sub: '', x: 'Tempo' };

    const canvas = document.getElementById('categoryChartB');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');

    if (barChartB) {
        barChartB.destroy();
    }

    const subtitleElem = document.getElementById('chartSubtitleB');
    if (subtitleElem) subtitleElem.textContent = currentConfig.sub;

    const chartLabels = data.map(d => d.label);
    const chartValues = data.map(d => d.count);

    barChartB = new Chart(ctx, {
        type: 'line',
        data: {
            labels: chartLabels,
            datasets: [{
                label: 'Pedidos',
                data: chartValues,
                borderColor: 'darkcyan',
                backgroundColor: 'rgba(0,139,139,0.15)',
                borderWidth: 3,
                pointRadius: 5,
                pointHoverRadius: 7,
                pointBackgroundColor: 'darkcyan',
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    title: {
                        display: true,
                        text: currentConfig.x,
                        color: '#6c757d',
                        font: { size: 12, weight: 'bold' },
                        padding: { top: 10 }
                    },
                    grid: { display: false }
                },
                y: {
                    beginAtZero: true,
                    grace: '10%',
                    title: {
                        display: true,
                        text: 'Nº Pedidos',
                        color: '#6c757d',
                        font: { size: 12, weight: 'bold' },
                        padding: { bottom: 10 }
                    },
                    ticks: { stepSize: 1, precision: 0, color: '#6c757d' },
                    grid: { color: 'rgba(0,0,0,0.04)' }
                }
            }
        }
    });
}
function renderDoughnutChart(data) {
    const canvas = document.getElementById('productCategoryChart');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');

    if (productCategoryChart) {
        productCategoryChart.destroy();
    }

    const safeData = data || [];
    const labels = safeData.map(d => d.category);
    const values = safeData.map(d => d.count);

    
    const bgColors = ['#009697', '#007f80', 'rgba(0, 150, 151, 0.35)', '#dc3545', '#6c757d'];

    productCategoryChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: values,
                backgroundColor: bgColors,
                borderWidth: 2, 
                borderColor: '#ffffff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '65%', 
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        boxWidth: 40, 
                        padding: 15,
                        font: { size: 12 }
                    }
                }
            }
        }
    });
}

function clearDataB() {
    document.getElementById('idConsumptionB').textContent = '...';
    document.getElementById('idMoneyB').textContent = '...';
    document.getElementById('idAverageB').textContent = '...';
    document.getElementById('idBuyersB').textContent = '...';

    const tbody = document.getElementById('topProductsTableBody');
    if (tbody) {
        tbody.innerHTML = '<tr><td colspan="2" class="text-center text-muted py-3">A carregar...</td></tr>';
    }

    if (productCategoryChart) {
        productCategoryChart.data.labels = [];
        productCategoryChart.data.datasets.forEach((dataset) => {
            dataset.data = [];
        });
        productCategoryChart.update();
    }

    if (barChartB) {
        barChartB.data.labels = [];
        barChartB.options.scales.x.title.text = '';
        barChartB.data.datasets.forEach((dataset) => {
            dataset.data = [];
        });
        barChartB.update();
    }
}





function clearDataR() {
    document.getElementById('idMeals').textContent = '...';
    document.getElementById('idMoney').textContent = '...';
    document.getElementById('idAverage').textContent = '...';
    document.getElementById('idBuyers').textContent = '...';
    document.getElementById('idStudent').textContent = '...';
    document.getElementById('idExternal').textContent = '...';
    document.getElementById('idWorker').textContent = '...';



    if (mealsChart) {

        mealsChart.data.labels = [];
        mealsChart.options.scales.x.title.text = '';
        mealsChart.data.datasets.forEach((dataset) => {
            dataset.data = [];
        });


        mealsChart.update();
    }

        
}




async function loadMealsSummary() {

    clearDataR();

    const periodSelect = document.getElementById('periodSelectR');
    const period = periodSelect?.value;

    

    try {

        const d = await fetch(`/Statistics/StatisticsTicket/GetTicketsStats?period=${encodeURIComponent(period)}`)
            .then(r => r.json());
        const cat = d.byCategory ?? [];
        const find = name => (cat.find(c => c.category === name)?.count ?? 0);



        document.getElementById('idMeals').textContent = d.totalMeals;
        document.getElementById('idMoney').textContent =
            new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' }).format(d.totalRevenue);
        document.getElementById('idAverage').textContent =
            new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' }).format(d.averageRevenue);
        document.getElementById('idBuyers').textContent = d.newBuyers;

        document.getElementById('idStudent').textContent = find('Estudante');
        document.getElementById('idExternal').textContent = find('Externo');
        document.getElementById('idWorker').textContent = find('Trabalhador IPS');

        renderChart(d.chart, period);
    } catch (error) {
        console.error("Erro ao carregar estatísticas:", error);
    }
}


async function loadBarSummary() {

    clearDataB();

    const periodSelect = document.getElementById('periodSelectB');
    const period = periodSelect?.value;



    try {

        const d = await fetch(`/Statistics/StatisticsBar/GetBarStats?period=${encodeURIComponent(period)}`)
            .then(r => r.json());
        const cat = d.byCategory ?? [];
        const find = name => (cat.find(c => c.category === name)?.count ?? 0);



        document.getElementById('idConsumptionB').textContent = d.totalConsumptions ?? 0;
        document.getElementById('idMoneyB').textContent =
            new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' }).format(d.totalRevenue ?? 0);
        document.getElementById('idAverageB').textContent =
            new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' }).format(d.averageRevenue ?? 0);
        document.getElementById('idBuyersB').textContent = d.newBuyers ?? 0;

        renderChartB(d.chart, period);
        renderDoughnutChart(d.productCategories);

        if (typeof renderDoughnutChart === 'function') renderDoughnutChart(d.productCategories);
        renderTopProductsTable(d.topProducts);

    } catch (error) {
        console.error("Erro ao carregar estatísticas do bar:", error);
    }
}

function renderTopProductsTable(data) {
    const tbody = document.getElementById('topProductsTableBody');
    if (!tbody) return;

    tbody.innerHTML = ''; 

    const safeData = data || [];

   
    if (safeData.length === 0) {
        tbody.innerHTML = '<tr><td colspan="2" class="text-center text-muted py-3">Sem vendas registadas neste período.</td></tr>';
        return;
    }

    
    safeData.forEach(item => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td class="fw-semibold text-dark">${item.name}</td>
            <td class="text-center fw-bold" style="color: var(--ips); font-size: 1.1rem;">${item.quantity}</td>
        `;
        tbody.appendChild(tr);
    });
}












