let currentUser = null;
let products = [];

// API базовый URL
const API = '/api';

// Авторизация
async function login() {
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;

    try {
        const response = await fetch(`${API}/AuthController/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        if (response.ok) {
            currentUser = await response.json();
            document.getElementById('loginPanel').style.display = 'none';
            document.getElementById('appPanel').style.display = 'block';
            document.getElementById('userName').innerText = currentUser.fullName;
            document.getElementById('userRole').innerText = currentUser.role;

            loadDashboard();
            loadProducts();
            loadIncoming();
            loadOutgoing();
            loadCells();
        } else {
            alert('Неверный логин или пароль');
        }
    } catch (error) {
        alert('Ошибка подключения к серверу');
    }
}

function logout() {
    currentUser = null;
    document.getElementById('loginPanel').style.display = 'flex';
    document.getElementById('appPanel').style.display = 'none';
    document.getElementById('username').value = '';
    document.getElementById('password').value = '';
}

// Навигация
document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));

        btn.classList.add('active');
        const tab = btn.dataset.tab;
        document.getElementById(`${tab}Tab`).classList.add('active');

        if (tab === 'dashboard') loadDashboard();
        else if (tab === 'products') loadProducts();
        else if (tab === 'incoming') loadIncoming();
        else if (tab === 'outgoing') loadOutgoing();
        else if (tab === 'cells') loadCells();
    });
});

// Загрузка дашборда
async function loadDashboard() {
    try {
        const response = await fetch(`${API}/StatisticsController`);
        const stats = await response.json();

        document.getElementById('statsGrid').innerHTML = `
            <div class="stat-card"><h4>📦 Товаров</h4><div class="stat-value">${stats.totalProducts}</div></div>
            <div class="stat-card"><h4>📊 Общий остаток</h4><div class="stat-value">${stats.totalStock} шт</div></div>
            <div class="stat-card"><h4>💰 Общая стоимость</h4><div class="stat-value">${Math.round(stats.totalValue).toLocaleString()} ₽</div></div>
            <div class="stat-card"><h4>🏪 Загрузка склада</h4><div class="stat-value">${Math.round(stats.warehouseOccupancy / stats.warehouseCapacity * 100)}%</div>
            <div class="stat-sub">${stats.warehouseOccupancy} / ${stats.warehouseCapacity} шт</div></div>
            <div class="stat-card"><h4>📥 Приход (мес)</h4><div class="stat-value">${stats.incomingLastMonth}</div></div>
            <div class="stat-card"><h4>📤 Расход (мес)</h4><div class="stat-value">${stats.outgoingLastMonth}</div></div>
        `;

        const lowStockHtml = stats.lowStockProducts.map(p => `
            <div class="stat-card" style="margin-bottom:10px"><strong>${p.sku}</strong> - ${p.name}<br>
            Остаток: ${p.currentStock} / Мин: ${p.minStock}</div>
        `).join('');
        document.getElementById('lowStockList').innerHTML = lowStockHtml || '<div class="stat-card">✅ Все товары в норме</div>';
    } catch (error) {
        console.error('Error loading dashboard:', error);
    }
}

// Загрузка товаров
async function loadProducts() {
    try {
        const response = await fetch(`${API}/ProductsController`);
        products = await response.json();
        renderProducts();
    } catch (error) {
        console.error('Error loading products:', error);
    }
}

function renderProducts() {
    const search = document.getElementById('productSearch')?.value.toLowerCase() || '';
    const filtered = products.filter(p =>
        p.sku.toLowerCase().includes(search) || p.name.toLowerCase().includes(search)
    );

    document.getElementById('productsList').innerHTML = filtered.map(p => `
        <tr>
            <td>${p.sku}</td><td>${p.name}</td><td>${p.category || '-'}</td>
            <td>${p.price.toLocaleString()} ₽</td>
            <td style="${p.currentStock <= p.minStock ? 'color:#ff4757;font-weight:bold' : ''}">${p.currentStock}</td>
            <td>${p.minStock}</td>
        </tr>
    `).join('');
}

function filterProducts() { renderProducts(); }

// Загрузка приходов
async function loadIncoming() {
    try {
        const response = await fetch(`${API}/IncomingController`);
        const invoices = await response.json();

        document.getElementById('incomingList').innerHTML = invoices.map(i => `
            <tr><td>${i.number}</td><td>${new Date(i.date).toLocaleDateString()}</td>
            <td>${i.supplier}</td><td>${i.status}</td><td>${i.items?.length || 0}</td></tr>
        `).join('');
    } catch (error) {
        console.error('Error loading incoming:', error);
    }
}

// Загрузка отгрузок
async function loadOutgoing() {
    try {
        const response = await fetch(`${API}/OutgoingController`);
        const invoices = await response.json();

        document.getElementById('outgoingList').innerHTML = invoices.map(i => `
            <tr>
                <td>${i.number}</td><td>${new Date(i.date).toLocaleDateString()}</td>
                <td>${i.customer}</td>
                <td><span class="status-badge status-${getStatusClass(i.status)}">${i.status}</span></td>
                <td>${getActions(i)}</td>
            </tr>
        `).join('');
    } catch (error) {
        console.error('Error loading outgoing:', error);
    }
}

function getStatusClass(status) {
    const map = { 'Новый': 'new', 'К сборке': 'picking', 'Собран': 'picked', 'Отгружен': 'shipped' };
    return map[status] || 'new';
}

function getActions(invoice) {
    if (invoice.status === 'Новый') {
        return `<button class="btn-success" onclick="pickOrder(${invoice.id})">📦 Собрать</button>`;
    } else if (invoice.status === 'Собран') {
        return `<button class="btn-primary" onclick="shipOrder(${invoice.id})">🚚 Отгрузить</button>`;
    }
    return '-';
}

async function pickOrder(id) {
    if (confirm('Подтвердите сборку заказа?')) {
        const response = await fetch(`${API}/OutgoingController/${id}/pick`, { method: 'POST' });
        if (response.ok) { alert('Заказ собран'); loadOutgoing(); loadDashboard(); loadProducts(); }
        else { alert('Ошибка: недостаточно товара'); }
    }
}

async function shipOrder(id) {
    if (confirm('Подтвердите отгрузку?')) {
        await fetch(`${API}/OutgoingController/${id}/ship`, { method: 'POST' });
        alert('Заказ отгружен');
        loadOutgoing();
        loadDashboard();
        loadProducts();
    }
}

// Загрузка ячеек
async function loadCells() {
    try {
        const response = await fetch(`${API}/CellsController`);
        const cells = await response.json();

        document.getElementById('cellsList').innerHTML = cells.map(c => `
            <tr>
                <td>${c.code}</td><td>${c.zone}</td>
                <td>${c.maxCapacity}</td>
                <td><div class="progress-bar" style="width:${c.currentOccupancy / c.maxCapacity * 100}%"></div> ${c.currentOccupancy}</td>
                <td>${c.maxCapacity - c.currentOccupancy}</td>
            </tr>
        `).join('');
    } catch (error) {
        console.error('Error loading cells:', error);
    }
}

// Модальные окна
function showAddProductModal() { document.getElementById('productModal').style.display = 'block'; }
async function addProduct() {
    const product = {
        sku: document.getElementById('prodSku').value,
        name: document.getElementById('prodName').value,
        category: document.getElementById('prodCategory').value,
        price: parseFloat(document.getElementById('prodPrice').value),
        minStock: parseInt(document.getElementById('prodMinStock').value),
        currentStock: 0
    };

    await fetch(`${API}/ProductsController`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(product)
    });

    closeModal('productModal');
    loadProducts();
    alert('Товар добавлен');
}

let receiveItems = [];
function showReceiveModal() { receiveItems = []; document.getElementById('receiveModal').style.display = 'block'; renderReceiveItems(); }
function addReceiveItem() { receiveItems.push({ productId: 0, quantity: 0, price: 0 }); renderReceiveItems(); }
function renderReceiveItems() {
    document.getElementById('receiveItemsList').innerHTML = receiveItems.map((item, idx) => `
        <div class="receive-item">
            <select onchange="updateReceiveItem(${idx}, 'productId', this.value)">
                <option value="0">Выберите товар</option>
                ${products.map(p => `<option value="${p.id}">${p.sku} - ${p.name}</option>`).join('')}
            </select>
            <input type="number" placeholder="Кол-во" onchange="updateReceiveItem(${idx}, 'quantity', this.value)">
            <input type="number" placeholder="Цена" onchange="updateReceiveItem(${idx}, 'price', this.value)">
            <button onclick="removeReceiveItem(${idx})">❌</button>
        </div>
    `).join('');
}
function updateReceiveItem(idx, field, val) { receiveItems[idx][field] = field === 'productId' ? parseInt(val) : parseFloat(val); }
function removeReceiveItem(idx) { receiveItems.splice(idx, 1); renderReceiveItems(); }
async function submitReceive() {
    const supplier = document.getElementById('receiveSupplier').value;
    if (!supplier || receiveItems.length === 0) { alert('Заполните все поля'); return; }

    await fetch(`${API}/IncomingController/receive`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ supplier, userId: currentUser.id, items: receiveItems })
    });

    closeModal('receiveModal');
    loadIncoming();
    loadDashboard();
    loadProducts();
    alert('Товар принят');
}

let orderItems = [];
function showOrderModal() { orderItems = []; document.getElementById('orderModal').style.display = 'block'; renderOrderItems(); }
function addOrderItem() { orderItems.push({ productId: 0, quantity: 0 }); renderOrderItems(); }
function renderOrderItems() {
    document.getElementById('orderItemsList').innerHTML = orderItems.map((item, idx) => `
        <div class="order-item">
            <select onchange="updateOrderItem(${idx}, 'productId', this.value)">
                <option value="0">Выберите товар</option>
                ${products.map(p => `<option value="${p.id}">${p.sku} - ${p.name} (в наличии: ${p.currentStock})</option>`).join('')}
            </select>
            <input type="number" placeholder="Кол-во" onchange="updateOrderItem(${idx}, 'quantity', this.value)">
            <button onclick="removeOrderItem(${idx})">❌</button>
        </div>
    `).join('');
}
function updateOrderItem(idx, field, val) { orderItems[idx][field] = field === 'productId' ? parseInt(val) : parseInt(val); }
function removeOrderItem(idx) { orderItems.splice(idx, 1); renderOrderItems(); }
async function submitOrder() {
    const customer = document.getElementById('orderCustomer').value;
    if (!customer || orderItems.length === 0) { alert('Заполните все поля'); return; }

    await fetch(`${API}/OutgoingController/create`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ customer, userId: currentUser.id, items: orderItems })
    });

    closeModal('orderModal');
    loadOutgoing();
    alert('Заказ создан');
}

function closeModal(id) { document.getElementById(id).style.display = 'none'; }
window.onclick = function (event) { if (event.target.classList.contains('modal')) event.target.style.display = 'none'; }