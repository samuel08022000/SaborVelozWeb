// -----------------------------------------------------
// --- VARIABLES GLOBALES Y CONFIGURACIÓN ---
// -----------------------------------------------------
const API_URL = "/api";
let currentUser = null;
let productos = [];
let pedidoActual = [];
let intervaloCocina = null;

// -----------------------------------------------------
// --- INICIALIZACIÓN (AL CARGAR LA PÁGINA) ---
// -----------------------------------------------------
document.addEventListener('DOMContentLoaded', () => {
    // 1. Restaurar sesión si existe
    const savedUser = localStorage.getItem('saborVelozUser');
    if (savedUser) {
        currentUser = JSON.parse(savedUser);
        restaurarVistaUsuario();
    } else {
        mostrarVista('login-view');
    }

    // 2. Inicializar eventos
    inicializarLogin();
    inicializarAdminUI();
});

// -----------------------------------------------------
// MODIFICAR ESTA FUNCIÓN (restaurarVistaUsuario)
// -----------------------------------------------------
function restaurarVistaUsuario() {
    if (currentUser.rol === 'Administrador' || currentUser.rol === 'Admin') {
        mostrarVista('dashboard-view', currentUser.rol);
        mostrarSubVistaAdmin('reportes-view'); // El admin va directo a los reportes/resumen
    }
    else if (currentUser.rol === 'Cajero') {
        mostrarVista('cajero-view', currentUser.rol);
        document.getElementById('cajero-name').textContent = currentUser.nombre;

        // LÓGICA NUEVA: Verificar si la caja está abierta antes de dejarlo trabajar
        verificarEstadoCajaParaCajero();
    }
    else if (currentUser.rol === 'Cocina') {
        mostrarVista('cocina-view', currentUser.rol);
        iniciarMonitorCocina();
    }
}

// -----------------------------------------------------
// --- 1. LOGIN Y SESIÓN ---
// -----------------------------------------------------
function inicializarLogin() {
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            document.getElementById('mensaje-error').style.display = 'none';

            const usuario = document.getElementById('usuario').value.trim();
            const password = document.getElementById('contrasena').value.trim();

            try {
                const response = await fetch(`${API_URL}/Auth/login`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ usuario, password })
                });

                if (!response.ok) throw new Error("Usuario o contraseña incorrectos");

                const data = await response.json();

                // Guardamos datos reales del usuario
                currentUser = {
                    usuario: data.usuario,
                    nombre: data.nombre,
                    rol: data.rol
                };

                localStorage.setItem('saborVelozUser', JSON.stringify(currentUser));
                restaurarVistaUsuario();

            } catch (error) {
                const msg = document.getElementById('mensaje-error');
                msg.textContent = error.message;
                msg.style.display = 'block';
            }
        });
    }
}

function mostrarVista(viewId, rol) {
    document.querySelectorAll('.view').forEach(v => v.style.display = 'none');
    const target = document.getElementById(viewId);
    if (target) {
        target.style.display = 'block';
        const rolDisplay = document.getElementById('rolDisplay');
        if (rolDisplay && rol) rolDisplay.textContent = rol.toUpperCase();
    }

    // Detener consumo de recursos si no estamos en cocina
    if (viewId !== 'cocina-view' && intervaloCocina) {
        clearInterval(intervaloCocina);
    }
}

function logout() {
    if (confirm("¿Cerrar sesión?")) {
        localStorage.removeItem('saborVelozUser');
        location.reload();
    }
}

// -----------------------------------------------------
// --- 2. PANEL DE ADMINISTRADOR (TODO REAL) ---
// -----------------------------------------------------
function inicializarAdminUI() {
    // Navegación del Dashboard
    const navButtons = document.querySelectorAll('#admin-nav .nav-button');
    navButtons.forEach(btn => {
        btn.addEventListener('click', (e) => {
            navButtons.forEach(b => b.classList.remove('active-nav'));
            e.target.classList.add('active-nav');
            mostrarSubVistaAdmin(e.target.dataset.view);
        });
    });

    // Eventos Botones
    const btnAbrirCaja = document.querySelector('.caja-confirm-btn');
    if (btnAbrirCaja) btnAbrirCaja.addEventListener('click', abrirCaja);

    const btnCerrarCaja = document.querySelector('.btn-cerrar-caja');
    if (btnCerrarCaja) btnCerrarCaja.addEventListener('click', cerrarCaja);

    const btnNuevoProd = document.querySelector('.btn-nuevo-producto');
    if (btnNuevoProd) btnNuevoProd.addEventListener('click', abrirModalProducto);

    const btnExcel = document.querySelector('.btn-excel');
    if (btnExcel) btnExcel.addEventListener('click', descargarExcelDiario);
}

function mostrarSubVistaAdmin(subViewId) {
    document.querySelectorAll('.sub-view').forEach(v => v.style.display = 'none');
    const target = document.getElementById(subViewId);
    if (target) {
        target.style.display = 'block';
        // Cargar datos frescos cada vez que entra a la vista
        if (subViewId === 'caja-view') cargarDatosCaja();
        if (subViewId === 'gestion-productos-view') cargarProductosAdmin();
        if (subViewId === 'reportes-view') cargarReportesReales();
    }
}

// --- A. CAJA (SHIFT) ---
async function cargarDatosCaja() {
    try {
        const res = await fetch(`${API_URL}/Caja/estado`);
        if (res.ok) {
            const data = await res.json();
            const formApertura = document.querySelector('.apertura-caja-form');
            const resumenCaja = document.querySelector('.resumen-diario');
            const btnCerrar = document.querySelector('.btn-cerrar-caja');

            if (data.abierta) {
                formApertura.style.display = 'none';
                resumenCaja.style.display = 'flex';
                btnCerrar.style.display = 'block';

                document.querySelector('.resumen-card.vendido span').textContent = `$${data.totalVendido.toFixed(2)}`;
                document.querySelector('.resumen-card.caja span').textContent = `$${data.totalCaja.toFixed(2)}`;
            } else {
                formApertura.style.display = 'block';
                resumenCaja.style.display = 'none';
                btnCerrar.style.display = 'none';
            }
        }
    } catch (e) { console.error("Error caja", e); }
}

async function abrirCaja() {
    const inputMonto = document.querySelector('.apertura-caja-form input[type="number"]');
    const monto = parseFloat(inputMonto.value);

    if (isNaN(monto) || monto < 0) return alert("Monto inválido");

    try {
        const res = await fetch(`${API_URL}/Caja/abrir`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ montoInicial: monto, usuario: currentUser.usuario })
        });

        if (res.ok) {
            alert("Caja Abierta");
            cargarDatosCaja();
        } else {
            alert("Error: " + await res.text());
        }
    } catch (e) { alert("Error de red"); }
}

async function cerrarCaja() {
    if (!confirm("¿Cerrar caja y finalizar turno?")) return;
    try {
        const res = await fetch(`${API_URL}/Caja/cerrar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({})
        });
        if (res.ok) {
            const data = await res.json();
            alert(data.message);
            cargarDatosCaja();
        }
    } catch (e) { console.error(e); }
}
function mostrarSubVistaAdmin(subViewId) {
    document.querySelectorAll('.sub-view').forEach(v => v.style.display = 'none');
    const target = document.getElementById(subViewId);
    if (target) {
        target.style.display = 'block';

        // Si entra a CAJA, ve el estado pero NO el botón de abrir (eso es del cajero)
        if (subViewId === 'caja-view') {
            cargarDatosCaja(); // Esto llena los labels de lblVentasHoy, lblTotalCaja
        }
        // Si entra a REPORTES, ve los KPIs (Tu resumen diario)
        if (subViewId === 'reportes-view') {
            cargarReportesReales();
        }
        if (subViewId === 'gestion-productos-view') {
            cargarProductosAdmin();
        }
    }
}
// --- B. PRODUCTOS (CRUD) ---
async function cargarProductosAdmin() {
    try {
        const response = await fetch(`${API_URL}/Productos/lista`);
        if (response.ok) {
            const data = await response.json();
            const tbody = document.querySelector('.productos-table tbody');
            tbody.innerHTML = '';

            data.forEach(p => {
                // MIRA ESTA LÍNEA CON CUIDADO 👇
                // Aquí le pasamos p.nombreProducto y p.precio a la función editarProducto
                const botonEditar = `editarProducto(${p.idProducto}, '${p.nombreProducto}', ${p.precio})`;

                tbody.innerHTML += `
                    <tr>
                        <td>${p.idProducto}</td>
                        <td>${p.nombreProducto}</td>
                        <td>${p.categoria}</td>
                        <td>$${p.precio.toFixed(2)}</td>
                        <td>${p.disponible ? '<b style="color:green">Activo</b>' : '<b style="color:red">Inactivo</b>'}</td>
                        <td>
                            <button class="btn-accion btn-editar" onclick="${botonEditar}">Editar</button>
                            <button class="btn-accion btn-eliminar" onclick="eliminarProducto(${p.idProducto})">Eliminar</button>
                        </td>
                    </tr>`;
            });
        }
    } catch (e) { console.error(e); }
}

async function abrirModalProducto() {
    const nombre = prompt("Nombre del producto:");
    if (!nombre) return;
    const precio = parseFloat(prompt("Precio:"));
    if (isNaN(precio)) return;

    try {
        const res = await fetch(`${API_URL}/Productos/crear`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ nombreProducto: nombre, precio: precio, categoria: "General", disponible: true })
        });
        if (res.ok) { cargarProductosAdmin(); }
    } catch (e) { alert("Error"); }
}

// Recibe los datos (id, nombre, precio) y los pone en la pantalla
function editarProducto(id, nombre, precio) {
    // 1. Llenar los campos con lo que ya existe en la BD
    document.getElementById('editId').value = id;
    document.getElementById('editNombre').value = nombre;   // <--- Aquí aparece el nombre
    document.getElementById('editPrecio').value = precio;   // <--- Aquí aparece el precio

    // 2. Mostrar la ventana
    document.getElementById('modalEditar').style.display = 'flex';
}

function cerrarModal() {
    document.getElementById('modalEditar').style.display = 'none';
}

async function guardarCambiosProducto() {
    const id = document.getElementById('editId').value;
    const nombre = document.getElementById('editNombre').value;
    const precio = parseFloat(document.getElementById('editPrecio').value);

    // Validación
    if (!nombre || isNaN(precio) || precio <= 0) {
        return alert("Datos inválidos");
    }

    try {
        const res = await fetch(`${API_URL}/Productos/editar/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                nombreProducto: nombre,
                precio: precio,
                disponible: true
            })
        });

        if (res.ok) {
            alert("Producto actualizado");
            cerrarModal();
            cargarProductosAdmin(); // Recargar tabla para ver cambios
        } else {
            alert("Error: " + await res.text());
        }
    } catch (e) { alert("Error de conexión"); }
}

async function eliminarProducto(id) {
    if (!confirm("¿Eliminar producto?")) return;
    await fetch(`${API_URL}/Productos/eliminar/${id}`, { method: 'DELETE' });
    cargarProductosAdmin();
}

// --- C. REPORTES REALES ---
async function cargarReportesReales() {
    console.log("Obteniendo métricas del backend...");
    const cards = document.querySelectorAll('.kpi-card span'); // Día, Semana, Mes, Año

    try {
        // Llamadas paralelas a tus endpoints de ReportesController
        const [diario, semanal, mensual, anual] = await Promise.all([
            fetch(`${API_URL}/Reportes/diario`).then(r => r.json()),
            fetch(`${API_URL}/Reportes/semanal`).then(r => r.json()),
            fetch(`${API_URL}/Reportes/mensual`).then(r => r.json()),
            fetch(`${API_URL}/Reportes/anual`).then(r => r.json())
        ]);

        // Renderizar en las tarjetas (ordenadas según tu HTML)
        if (cards[0]) cards[0].textContent = `$${diario.totalVentas.toFixed(2)}`;
        if (cards[1]) cards[1].textContent = `$${semanal.totalVentas.toFixed(2)}`;
        if (cards[2]) cards[2].textContent = `$${mensual.totalVentas.toFixed(2)}`;
        if (cards[3]) cards[3].textContent = `$${anual.totalVentas.toFixed(2)}`;

    } catch (error) {
        console.error("Error cargando reportes:", error);
        cards.forEach(c => c.textContent = "Error");
    }
}

function descargarExcelDiario() {
    // Descarga directa usando el endpoint del backend
    window.open(`${API_URL}/Reportes/exportar/diario`, '_blank');
}

// -----------------------------------------------------
// --- 3. PUNTO DE VENTA (CAJERO) ---
// -----------------------------------------------------
// -----------------------------------------------------
// NUEVA FUNCIÓN: VERIFICAR CAJA (Solo Cajero)
// -----------------------------------------------------
async function verificarEstadoCajaParaCajero() {
    try {
        const res = await fetch(`${API_URL}/Caja/estado`);
        if (res.ok) {
            const data = await res.json();

            if (!data.abierta) {
                // SI LA CAJA ESTÁ CERRADA -> MOSTRAR MODAL BLOQUEANTE
                document.getElementById('lblCajeroApertura').textContent = currentUser.nombre;
                document.getElementById('modalAperturaCaja').style.display = 'flex';

                // Ocultar el grid de productos para que no toque nada
                document.querySelector('.cajero-layout').style.filter = "blur(5px)";
                document.querySelector('.cajero-layout').style.pointerEvents = "none";
            } else {
                // SI ESTÁ ABIERTA -> CARGAR PRODUCTOS NORMALMENTE
                document.getElementById('modalAperturaCaja').style.display = 'none';
                document.querySelector('.cajero-layout').style.filter = "none";
                document.querySelector('.cajero-layout').style.pointerEvents = "all";
                cargarProductosCajero();
            }
        }
    } catch (e) {
        console.error("Error verificando caja", e);
        alert("Error de conexión al verificar la caja.");
    }
}

// -----------------------------------------------------
// NUEVA FUNCIÓN: CONFIRMAR APERTURA (Botón del Modal)
// -----------------------------------------------------
async function confirmarAperturaCaja() {
    const inputMonto = document.getElementById('montoAperturaInput');
    const monto = parseFloat(inputMonto.value);

    if (isNaN(monto) || monto < 0) {
        return alert("⚠️ Por favor ingresa un monto inicial válido.");
    }

    try {
        const res = await fetch(`${API_URL}/Caja/abrir`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                montoInicial: monto,
                usuario: currentUser.usuario // Enviamos el usuario actual
            })
        });

        if (res.ok) {
            alert("✅ ¡Caja abierta exitosamente! Buen turno.");

            // Restaurar la interfaz
            document.getElementById('modalAperturaCaja').style.display = 'none';
            document.querySelector('.cajero-layout').style.filter = "none";
            document.querySelector('.cajero-layout').style.pointerEvents = "all";

            cargarProductosCajero(); // Cargar menú
        } else {
            const errorText = await res.text();
            alert("❌ Error: " + errorText);
        }
    } catch (e) {
        console.error(e);
        alert("Error de red al intentar abrir la caja.");
    }
}
async function cargarProductosCajero() {
    try {
        const response = await fetch(`${API_URL}/Productos/lista`);
        if (response.ok) {
            productos = await response.json();
            renderizarGridCajero('general');

            document.querySelectorAll('.tab-button').forEach(btn => {
                btn.onclick = () => {
                    document.querySelectorAll('.tab-button').forEach(b => b.classList.remove('active'));
                    btn.classList.add('active');
                    renderizarGridCajero(btn.dataset.categoria);
                };
            });
        }
    } catch (e) { console.error(e); }
}

function renderizarGridCajero(filtro) {
    const grid = document.getElementById('productGrid');
    grid.innerHTML = '';

    // Filtro simple (puedes ajustar 'categoria' según tu BD)
    const lista = (filtro === 'general') ? productos : productos; // Si tu BD tiene categorias, usa: productos.filter(p => p.categoria === filtro)

    lista.forEach(p => {
        const card = document.createElement('div');
        card.className = 'product-card';
        card.innerHTML = `
            <div style="pointer-events:none">
                <div>${p.nombreProducto}</div>
                <div style="color:var(--color-secondary); font-weight:bold;">$${p.precio.toFixed(2)}</div>
            </div>`;
        card.onclick = () => agregarAlCarrito(p);
        grid.appendChild(card);
    });
}

function agregarAlCarrito(p) {
    const item = pedidoActual.find(i => i.idProducto === p.idProducto);
    if (item) item.cantidad++;
    else pedidoActual.push({ idProducto: p.idProducto, nombre: p.nombreProducto, precio: p.precio, cantidad: 1 });
    actualizarUIcarrito();
}

function actualizarUIcarrito() {
    const lista = document.getElementById('pedidoLista');
    const totalLbl = document.getElementById('totalPedido');
    lista.innerHTML = '';
    let total = 0;

    pedidoActual.forEach((item, idx) => {
        total += item.cantidad * item.precio;
        const div = document.createElement('div');
        div.className = 'pedido-item';
        div.innerHTML = `
            <span class="pedido-item-qty">${item.cantidad}x ${item.nombre}</span> 
            <span>$${(item.cantidad * item.precio).toFixed(2)} <span style="cursor:pointer;color:red;margin-left:5px" onclick="borrarItem(${idx})">×</span></span>`;
        lista.appendChild(div);
    });
    totalLbl.textContent = total.toFixed(2);
}

function borrarItem(idx) {
    pedidoActual.splice(idx, 1);
    actualizarUIcarrito();
}

async function registrarVenta() {
    // 1. Validación: ¿Hay algo que cobrar?
    if (pedidoActual.length === 0) {
        alert("⚠️ El carrito está vacío. Agrega productos antes de cobrar.");
        return;
    }

    // 2. Obtener Datos: Cajero y Método de Pago
    const metodoPagoInput = document.querySelector('input[name="pago"]:checked');
    const metodoPago = metodoPagoInput ? metodoPagoInput.value : "Efectivo"; // Valor por defecto

    // 3. Preparar el paquete (DTO) para el Backend
    // En script.js dentro de registrarVenta()
    const ventaDto = {
        // CAMBIO AQUI: Enviamos el usuario (ej: "samuel08") que es único, no el nombre
        Usuario: currentUser.usuario,
        metodoPago: metodoPago,
        productos: pedidoActual.map(p => ({
            idProducto: p.idProducto,
            cantidad: p.cantidad
        }))
    };

    // 4. Enviar al Servidor
    try {
        const response = await fetch(`${API_URL}/Ventas/registrar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(ventaDto)
        });

        if (response.ok) {
            const data = await response.json();
            // Mensaje de éxito confirmando ambas acciones
            alert(`✅ ¡Venta #${data.idVenta} Registrada!\n💰 Total: $${data.total}\n👨‍🍳 ¡La orden se envió a Cocina automáticamente!`);

            // Limpiar carrito
            pedidoActual = [];
            actualizarUIcarrito();
        } else {
            const errorMsg = await response.text();
            alert("❌ Error al registrar: " + errorMsg);
        }
    } catch (error) {
        console.error(error);
        alert("❌ Error de conexión con el servidor.");
    }
}

// Hacemos que el botón "Enviar a cocina" haga lo mismo que registrar,
// ya que el backend une ambos procesos.
function enviarACocina() {
    registrarVenta();
}

function enviarACocina() { registrarVenta(); }

// -----------------------------------------------------
// --- 4. MONITOR DE COCINA ---
// -----------------------------------------------------
function iniciarMonitorCocina() {
    cargarComandas();
    intervaloCocina = setInterval(cargarComandas, 5000);
}

async function cargarComandas() {
    try {
        const res = await fetch(`${API_URL}/Cocina/pendientes`);
        if (res.ok) {
            const data = await res.json();
            renderizarCocina(data);
        }
    } catch (e) { console.error(e); }
}

function renderizarCocina(comandas) {
    const contenedor = document.getElementById('pendientes-list');
    contenedor.innerHTML = '';

    if (comandas.length === 0) {
        contenedor.innerHTML = '<p style="text-align:center;color:#999">Sin pedidos pendientes</p>';
        return;
    }

    comandas.forEach(c => {
        let itemsHtml = c.venta?.detalles?.map(d =>
            `<li><strong>${d.cantidad}x</strong> ${d.producto?.nombreProducto || '??'}</li>`
        ).join('') || '<li>Sin detalles</li>';

        const card = document.createElement('div');
        card.className = 'orden-card';
        card.innerHTML = `
            <div class="orden-header">
                <span>Mesa/Pedido #${c.idComanda}</span>
                <span style="color:#ffc107; font-weight:bold">${c.estado}</span>
            </div>
            <ul class="orden-items">${itemsHtml}</ul>
            <div class="orden-footer">
                <button class="btn-iniciar-prep" onclick="avanzarEstado(${c.idComanda}, 'En Preparacion')">
                    Empezar
                </button>
            </div>`;
        contenedor.appendChild(card);
    });
}

async function avanzarEstado(id, estado) {
    await fetch(`${API_URL}/Cocina/actualizar-estado/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(estado)
    });
    cargarComandas();
}