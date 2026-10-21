var API_BASE = 'http://localhost:5012/api';

function getToken() { return localStorage.getItem('token'); }
function setToken(t) { localStorage.setItem('token', t); }
function getCurrentUser() { var u = localStorage.getItem('currentUser'); return u ? JSON.parse(u) : null; }
function setCurrentUser(u) { localStorage.setItem('currentUser', JSON.stringify(u)); }

function requireAuth() {
    if (!getToken()) { window.location.href = 'login.html'; return null; }
    return getCurrentUser();
}

function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('currentUser');
    window.location.href = 'login.html';
}

function statusBadge(s) {
    var labels = {
        'New': '<span class="status-badge status-new">Новый</span>',
        'InProgress': '<span class="status-badge status-inprogress">В работе</span>',
        'Ready': '<span class="status-badge status-ready">На проверке</span>',
        'Completed': '<span class="status-badge status-completed">Завершён</span>',
        'Revision': '<span class="status-badge status-revision">Доработка</span>'
    };
    return labels[s] || s;
}

function escapeHtml(s) {
    if (!s) return '';
    return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

function api(method, url, data, auth) {
    var headers = { 'Content-Type': 'application/json' };
    if (auth !== false) {
        var t = getToken();
        if (t) headers['Authorization'] = 'Bearer ' + t;
    }
    var body = data ? JSON.stringify(data) : undefined;
    return fetch(API_BASE + url, { method: method, headers: headers, body: body })
        .then(function (r) {
            if (!r.ok) return r.text().then(function (t) { throw new Error(t || 'Ошибка ' + r.status); });
            return r.text().then(function (t) { return t ? JSON.parse(t) : null; });
        });
}

/* ============ BURGER ============ */
function initBurger() {
    var b = document.querySelector('.burger');
    var n = document.querySelector('.site-header nav');
    if (b && n) b.addEventListener('click', function () { n.classList.toggle('open'); });
}

/* ============ REGISTER ============ */
function initRegister() {
    var f = document.getElementById('registerForm');
    if (!f) return;
    f.addEventListener('submit', function (e) {
        e.preventDefault();
        var name = document.getElementById('regName').value.trim();
        var email = document.getElementById('regEmail').value.trim();
        var pass = document.getElementById('regPassword').value;
        var err = document.getElementById('regError');
        if (!name || !email || !pass) { err.textContent = 'Заполните все поля'; err.style.display = 'block'; return; }
        err.style.display = 'none';
        api('POST', '/auth/register', { fullName: name, email: email, password: pass }, false)
            .then(function (r) {
                setToken(r.token);
                setCurrentUser({ name: r.fullName, email: r.email, role: r.role });
                window.location.href = 'dashboard.html';
            })
            .catch(function (e) { err.textContent = e.message; err.style.display = 'block'; });
    });
}

/* ============ LOGIN ============ */
function initLogin() {
    var f = document.getElementById('loginForm');
    if (!f) return;
    f.addEventListener('submit', function (e) {
        e.preventDefault();
        var email = document.getElementById('loginEmail').value.trim();
        var pass = document.getElementById('loginPassword').value;
        var err = document.getElementById('loginError');
        if (!email || !pass) { err.textContent = 'Заполните все поля'; err.style.display = 'block'; return; }
        err.style.display = 'none';
        api('POST', '/auth/login', { email: email, password: pass }, false)
            .then(function (r) {
                setToken(r.token);
                setCurrentUser({ name: r.fullName, email: r.email, role: r.role });
                window.location.href = 'dashboard.html';
            })
            .catch(function (e) { err.textContent = e.message; err.style.display = 'block'; });
    });
}

/* ============ DASHBOARD ============ */
function initDashboard() {
    var c = document.getElementById('ordersContainer');
    if (!c) return;
    if (!requireAuth()) return;
    var nameEl = document.getElementById('userName');
    if (nameEl) nameEl.textContent = getCurrentUser().name;
    api('GET', '/orders').then(function (orders) {
        if (!orders || !orders.length) {
            c.innerHTML = '<p style="color:var(--text-muted)">У вас пока нет заказов.</p>';
            return;
        }
        orders.sort(function (a, b) { return new Date(b.createdAt) - new Date(a.createdAt); });
        var h = '';
        orders.forEach(function (o) {
            h += '<div class="glass-card order-card"><div class="order-info"><h3>' + escapeHtml(o.title) +
                '</h3><span class="meta">' + escapeHtml(o.type) + ' • от ' + o.createdAt.split('T')[0] +
                '</span></div><div style="display:flex;align-items:center;gap:12px;">' + statusBadge(o.status) +
                '<a href="order-details.html?id=' + o.id + '" class="btn btn-ghost btn-sm">Подробнее</a></div></div>';
        });
        c.innerHTML = h;
    }).catch(function (e) { c.innerHTML = '<p style="color:var(--red)">Ошибка: ' + e.message + '</p>'; });
}

/* ============ CREATE ORDER ============ */
function initCreateOrder() {
    var f = document.getElementById('createOrderForm');
    if (!f) return;
    if (!requireAuth()) return;

    var nameEl = document.getElementById('userName');
    if (nameEl) nameEl.textContent = getCurrentUser().name;

    // Находим f.addEventListener('submit', function (e) { ... })
    // Добавляем ключевое слово async перед функцией:
    f.addEventListener('submit', async function (e) {
        e.preventDefault(); // Намертво блокируем стандартную перезагрузку браузера

        var titleEl = document.getElementById('orderTitle');
        var typeEl = document.getElementById('orderType');
        var descEl = document.getElementById('orderDesc');
        var deadlineEl = document.getElementById('orderDeadline');

        var title = titleEl ? titleEl.value.trim() : '';
        var type = typeEl ? typeEl.value : 'Разработка';
        var desc = descEl ? descEl.value.trim() : '';
        var deadline = deadlineEl ? deadlineEl.value : null;

        var err = document.getElementById('orderError');
        if (!title || !desc) {
            if (err) { err.textContent = 'Заполните название и описание'; err.style.display = 'block'; }
            return;
        }
        if (err) err.style.display = 'none';

        var body = {
            Title: title,
            Description: desc,
            Type: type,
            Deadline: deadline ? new Date(deadline).toISOString() : null
        };

        try {
            console.log('[JS DEBUG] Пробуем отправить запрос на бэкенд...');

            // ЖДЕМ, пока fetch реально дойдет до сервера и вернет ответ
            await api('POST', '/orders', body);

            console.log('[JS DEBUG] Успешно сохранено! Перенаправляем...');
            window.location.href = 'dashboard.html'; // Перенаправляем ТОЛЬКО после успеха
        } catch (e) {
            if (err) {
                err.textContent = 'Ошибка создания заказа: ' + e.message;
                err.style.display = 'block';
            }
            console.error('[JS ERROR] Запрос отклонен бэкендом:', e);
        }
    });
}

/* ============ ORDER DETAILS ============ */
function initOrderDetails() {
    var c = document.getElementById('detailContainer');
    if (!c) return;
    if (!requireAuth()) return;

    var orderId = new URLSearchParams(window.location.search).get('id');

    // Запрашиваем ОДНОВРЕМЕННО данные заказа и сообщения по нему
    Promise.all([
        api('GET', '/orders/' + orderId),
        api('GET', '/orders/' + orderId + '/messages')
    ]).then(function (results) {
        var order = results[0];
        var messages = results[1] || [];

        // 1. Формируем список задач
        var tasksHtml = '';
        if (order.tasks && order.tasks.length) {
            order.tasks.forEach(function (t) {
                tasksHtml += '<div class="glass-card task-card"><div class="task-info"><h4>' + escapeHtml(t.title) +
                    '</h4><div class="task-meta">' + (t.developerName || 'Не назначен') + ' • до ' + (t.deadline ? t.deadline.split('T')[0] : '—') +
                    '</div></div>' + statusBadge(t.status) + '</div>';
            });
        } else {
            tasksHtml = '<p class="subtitle">Задачи пока не назначены.</p>';
        }

        // 2. Ищем последнее замечание менеджера (если оно есть)
        var revisionBlockHtml = '';
        var lastManagerMessage = messages.filter(function (m) {
            return m.direction === 'ManagerToClient';
        }).pop(); // Берем самое последнее сообщение

        // Если статус "Revision", выводим красивый блок с замечанием менеджера
        if (order.status === 'Revision' && lastManagerMessage) {
            revisionBlockHtml =
                '<div class="glass-card" style="padding:20px; border-left: 4px solid #E53E3E; margin-bottom:20px; background: rgba(229, 62, 62, 0.1);">' +
                '<h4 style="color:#E53E3E; margin-bottom:6px; font-weight:600;">Замечание менеджера по доработке:</h4>' +
                '<p style="color:var(--text-white); font-style:italic;">"' + escapeHtml(lastManagerMessage.text) + '"</p>' +
                '</div>';
        }

        // Подстраиваем текст кнопки проблемы под контекст доработки
        var problemBtnText = (order.status === 'Revision') ? 'Ответить на доработку' : 'Сообщить о проблеме';
        var problemBtnColor = (order.status === 'Revision') ? '#3182CE' : '#E53E3E'; // Синяя если ответ, красная если жалоба

        // 3. Собираем весь интерфейс страницы воедино
        var html =
            revisionBlockHtml + // Вставляем блок доработки в самый верх, если он активен
            '<div class="glass-card" style="padding:30px;margin-bottom:20px;">' +
            '<h2 style="color:var(--text-white);margin-bottom:16px;">' + escapeHtml(order.title) + '</h2>' +
            '<div class="detail-grid">' +
            '<div><div class="label">Статус</div><div class="value">' + statusBadge(order.status) + '</div></div>' +
            '<div><div class="label">Тип</div><div class="value">' + escapeHtml(order.type) + '</div></div>' +
            '<div><div class="label">Создан</div><div class="value">' + order.createdAt.split('T')[0] + '</div></div>' +
            '<div><div class="label">Срок</div><div class="value">' + (order.deadline ? order.deadline.split('T')[0] : 'Не указан') + '</div></div>' +
            '</div>' +
            '<div class="label" style="margin-top:12px;">Описание</div>' +
            '<p style="color:var(--text-main);margin-top:4px;">' + escapeHtml(order.description) + '</p>' +
            '</div>' +
            '<h3 style="color:var(--text-white);margin-bottom:12px;">Задачи по заказу</h3>' + tasksHtml +
            '<div style="display:flex;gap:12px;margin-top:20px;flex-wrap:wrap;">' +
            '<a href="dashboard.html" class="btn btn-ghost">← Назад к списку</a>' +
            '<button id="btnReportProblem" class="btn btn-ghost" style="color:' + problemBtnColor + ';border-color:' + problemBtnColor + ';">' + problemBtnText + '</button>' +
            '</div>';

        c.innerHTML = html;

        // Привязываем открытие модального окна к динамически созданной кнопке
        var btn = document.getElementById('btnReportProblem');
        var modal = document.getElementById('reportModal');
        var modalTitle = modal ? modal.querySelector('h3') : null;

        if (btn && modal) {
            btn.addEventListener('click', function () {
                if (modalTitle) {
                    modalTitle.textContent = (order.status === 'Revision') ? 'Ответ на доработку' : 'Сообщить о проблеме';
                }
                modal.style.display = 'flex';
            });
        }
    }).catch(function (e) {
        c.innerHTML = '<p style="color:var(--red)">Ошибка загрузки деталей заказа: ' + e.message + '</p>';
    });
}
function closeReport() {
    document.getElementById('reportModal').style.display = 'none';
    document.getElementById('reportText').value = '';
}

function sendReport() {
    var text = document.getElementById('reportText').value.trim();
    if (!text) { alert('Введите текст'); return; }
    var orderId = new URLSearchParams(window.location.search).get('id');
    api('POST', '/orders/' + orderId + '/messages', { text: text, direction: 'ClientToManager' })
        .then(function () { closeReport(); alert('Сообщение отправлено менеджеру'); location.reload(); })
        .catch(function (e) { alert('Ошибка: ' + e.message); });
}

document.addEventListener('DOMContentLoaded', function () {
    initBurger();
    initRegister();
    initLogin();
    initDashboard();
    initCreateOrder();
    initOrderDetails();
});