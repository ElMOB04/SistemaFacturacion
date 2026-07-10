/* =====================================================================
   PROYECTO FINAL DE PROGRAMACION
   Sistema de Facturacion y Cuentas por Cobrar / Cuentas por Pagar
   ---------------------------------------------------------------------
   Script 02: Insercion de datos de prueba iniciales
   Ejecutar DESPUES de 01_CrearBaseDatos.sql
   ---------------------------------------------------------------------
   Credenciales de acceso (contrasena en texto plano -> guardada como hash SHA-256):
       Usuario: admin   Contrasena: admin123   (Administrador)
       Usuario: cajero  Contrasena: user123    (Usuario)
   ---------------------------------------------------------------------
   Nota: el detalle de facturas y compras se enlaza usando SCOPE_IDENTITY()
   para no depender de valores de ID fijos (mas robusto y re-ejecutable).
   ===================================================================== */

USE SistemaFacturacionDB;
GO

/* Limpiar datos previos (respetando el orden de llaves foraneas).
   Las tablas ya vienen recien creadas desde el script 01, por lo que las
   columnas IDENTITY inician correctamente en 1. */
DELETE FROM dbo.Pagos;
DELETE FROM dbo.DetalleCompra;
DELETE FROM dbo.Compras;
DELETE FROM dbo.Cobros;
DELETE FROM dbo.DetalleFactura;
DELETE FROM dbo.Facturas;
DELETE FROM dbo.Productos;
DELETE FROM dbo.Proveedores;
DELETE FROM dbo.Clientes;
DELETE FROM dbo.Empleados;
DELETE FROM dbo.Usuarios;
GO

/* ---------------------------------------------------------------------
   USUARIOS  (la contrasena se guarda como hash SHA-256 en hexadecimal)
   --------------------------------------------------------------------- */
INSERT INTO dbo.Usuarios (NombreUsuario, Contrasena, NombreCompleto, Rol, Activo) VALUES
('admin',  '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'Administrador del Sistema', 'Administrador', 1),
('cajero', 'e606e38b0d8c19b24cf0ee3808183162ea7cd63ff7912dbb22b5e803286b4446', 'Cajero Principal',          'Usuario',       1);
GO

/* ---------------------------------------------------------------------
   EMPLEADOS
   --------------------------------------------------------------------- */
INSERT INTO dbo.Empleados (Nombre, Apellido, Cedula, Cargo, Telefono, Email, FechaIngreso, Activo) VALUES
('Maria',   'Gonzalez', '001-1234567-8', 'Gerente de Ventas', '809-555-0101', 'maria.gonzalez@empresa.com',   '20220115', 1),
('Carlos',  'Perez',    '002-2345678-9', 'Vendedor',          '809-555-0102', 'carlos.perez@empresa.com',     '20220310', 1),
('Ana',     'Rodriguez','003-3456789-0', 'Cajera',            '809-555-0103', 'ana.rodriguez@empresa.com',    '20230520', 1),
('Jose',    'Martinez', '004-4567890-1', 'Contador',          '809-555-0104', 'jose.martinez@empresa.com',    '20211105', 1);
GO

/* ---------------------------------------------------------------------
   CLIENTES
   --------------------------------------------------------------------- */
INSERT INTO dbo.Clientes (Nombre, Identificacion, Telefono, Email, Direccion, LimiteCredito, Saldo, Activo) VALUES
('Ferreteria La Economica',    '101-0011223-4', '809-777-1001', 'ventas@laeconomica.com',   'Av. Independencia 45, Santo Domingo', 50000.00, 0, 1),
('Supermercado El Ahorro',     '102-0022334-5', '809-777-1002', 'compras@elahorro.com',     'Calle Duarte 120, Santiago',          80000.00, 0, 1),
('Juan Alberto Ramirez',       '001-9988776-5', '809-777-1003', 'juan.ramirez@correo.com',  'Res. Los Prados, Apt 3B',             15000.00, 0, 1),
('Distribuidora del Cibao',    '103-0033445-6', '809-777-1004', 'info@distcibao.com',       'Zona Industrial, La Vega',           120000.00, 0, 1),
('Consumidor Final',           NULL,            NULL,           NULL,                       NULL,                                       0.00, 0, 1);
GO

/* ---------------------------------------------------------------------
   PROVEEDORES
   --------------------------------------------------------------------- */
INSERT INTO dbo.Proveedores (Nombre, RNC, Telefono, Email, Direccion, Saldo, Activo) VALUES
('Importadora Nacional SRL',   '130112233', '809-333-2001', 'ventas@importanacional.com', 'Puerto de Haina, Santo Domingo', 0, 1),
('Suministros Industriales SA','130223344', '809-333-2002', 'pedidos@suminind.com',       'Autopista Duarte Km 12',         0, 1),
('Tecnologia y Equipos EIRL',  '130334455', '809-333-2003', 'info@tecnoequipos.com',      'Av. 27 de Febrero 300',          0, 1);
GO

/* ---------------------------------------------------------------------
   PRODUCTOS Y SERVICIOS
   --------------------------------------------------------------------- */
INSERT INTO dbo.Productos (Codigo, Nombre, Descripcion, Tipo, Precio, Costo, Stock, Activo) VALUES
('P001', 'Laptop HP 15"',            'Laptop Intel Core i5, 8GB RAM, 256GB SSD', 'Producto',  35000.00, 28000.00,  20, 1),
('P002', 'Mouse Inalambrico',        'Mouse optico USB inalambrico',             'Producto',    650.00,   400.00, 150, 1),
('P003', 'Teclado Mecanico',         'Teclado mecanico retroiluminado',          'Producto',   2200.00,  1500.00,  80, 1),
('P004', 'Monitor 24" LED',          'Monitor Full HD 1920x1080',                'Producto',   8500.00,  6200.00,  35, 1),
('P005', 'Impresora Multifuncional', 'Impresora, escaner y copiadora',           'Producto',   9800.00,  7500.00,  15, 1),
('P006', 'Cable HDMI 2m',            'Cable HDMI de alta velocidad',             'Producto',    350.00,   180.00, 300, 1),
('S001', 'Instalacion de Software',  'Servicio de instalacion y configuracion',  'Servicio',   1500.00,     0.00,   0, 1),
('S002', 'Soporte Tecnico (hora)',   'Hora de soporte tecnico especializado',    'Servicio',   1200.00,     0.00,   0, 1);
GO

/* ---------------------------------------------------------------------
   FACTURAS + DETALLE + COBROS  (Cuentas por Cobrar)
   Todo en un mismo lote para poder enlazar por SCOPE_IDENTITY().
   Los totales usan el impuesto ITBIS del 18%.
   Se referencian los productos y clientes por su nombre para no depender
   de IDs fijos.
   --------------------------------------------------------------------- */
DECLARE @f1 INT, @f2 INT, @f3 INT;
DECLARE @cliFerreteria INT = (SELECT ClienteID FROM dbo.Clientes WHERE Nombre = 'Ferreteria La Economica');
DECLARE @cliSuper      INT = (SELECT ClienteID FROM dbo.Clientes WHERE Nombre = 'Supermercado El Ahorro');
DECLARE @cliCibao      INT = (SELECT ClienteID FROM dbo.Clientes WHERE Nombre = 'Distribuidora del Cibao');
DECLARE @empCarlos INT = (SELECT EmpleadoID FROM dbo.Empleados WHERE Cedula = '002-2345678-9');
DECLARE @empAna    INT = (SELECT EmpleadoID FROM dbo.Empleados WHERE Cedula = '003-3456789-0');
DECLARE @pLaptop   INT = (SELECT ProductoID FROM dbo.Productos WHERE Codigo = 'P001');
DECLARE @pTeclado  INT = (SELECT ProductoID FROM dbo.Productos WHERE Codigo = 'P003');
DECLARE @pMonitor  INT = (SELECT ProductoID FROM dbo.Productos WHERE Codigo = 'P004');
DECLARE @pImpresora INT = (SELECT ProductoID FROM dbo.Productos WHERE Codigo = 'P005');

/* Factura FAC-000001 : a credito, con un abono parcial */
INSERT INTO dbo.Facturas (NumeroFactura, ClienteID, EmpleadoID, Fecha, Subtotal, Impuesto, Total, Saldo, TipoPago, Estado)
VALUES ('FAC-000001', @cliFerreteria, @empCarlos, '20260601', 37200.00, 6696.00, 43896.00, 23896.00, 'Credito', 'Pendiente');
SET @f1 = SCOPE_IDENTITY();
INSERT INTO dbo.DetalleFactura (FacturaID, ProductoID, Cantidad, PrecioUnitario, Importe) VALUES
(@f1, @pLaptop,  1, 35000.00, 35000.00),
(@f1, @pTeclado, 1,  2200.00,  2200.00);

/* Factura FAC-000002 : de contado, ya pagada */
INSERT INTO dbo.Facturas (NumeroFactura, ClienteID, EmpleadoID, Fecha, Subtotal, Impuesto, Total, Saldo, TipoPago, Estado)
VALUES ('FAC-000002', @cliSuper, @empAna, '20260605', 17000.00, 3060.00, 20060.00, 0.00, 'Contado', 'Pagada');
SET @f2 = SCOPE_IDENTITY();
INSERT INTO dbo.DetalleFactura (FacturaID, ProductoID, Cantidad, PrecioUnitario, Importe) VALUES
(@f2, @pMonitor, 2, 8500.00, 17000.00);

/* Factura FAC-000003 : a credito, sin abonos */
INSERT INTO dbo.Facturas (NumeroFactura, ClienteID, EmpleadoID, Fecha, Subtotal, Impuesto, Total, Saldo, TipoPago, Estado)
VALUES ('FAC-000003', @cliCibao, @empCarlos, '20260610', 9800.00, 1764.00, 11564.00, 11564.00, 'Credito', 'Pendiente');
SET @f3 = SCOPE_IDENTITY();
INSERT INTO dbo.DetalleFactura (FacturaID, ProductoID, Cantidad, PrecioUnitario, Importe) VALUES
(@f3, @pImpresora, 1, 9800.00, 9800.00);

/* Cobros: abono parcial a la factura 1 y pago total de la factura 2 */
INSERT INTO dbo.Cobros (FacturaID, ClienteID, Fecha, Monto, FormaPago, Referencia, EmpleadoID)
VALUES (@f1, @cliFerreteria, '20260615', 20000.00, 'Transferencia', 'TRF-88123', @empAna);
INSERT INTO dbo.Cobros (FacturaID, ClienteID, Fecha, Monto, FormaPago, Referencia, EmpleadoID)
VALUES (@f2, @cliSuper, '20260605', 20060.00, 'Efectivo', NULL, @empAna);
GO

/* Actualizar el Saldo (Cuentas por Cobrar) de cada cliente segun las facturas pendientes */
UPDATE c
SET c.Saldo = ISNULL((SELECT SUM(f.Saldo) FROM dbo.Facturas f
                      WHERE f.ClienteID = c.ClienteID AND f.Estado <> 'Anulada'), 0)
FROM dbo.Clientes c;
GO

/* ---------------------------------------------------------------------
   COMPRAS + DETALLE + PAGOS  (Cuentas por Pagar)
   --------------------------------------------------------------------- */
DECLARE @c1 INT, @c2 INT;
DECLARE @provImporta INT = (SELECT ProveedorID FROM dbo.Proveedores WHERE RNC = '130112233');
DECLARE @provSuminis INT = (SELECT ProveedorID FROM dbo.Proveedores WHERE RNC = '130223344');
DECLARE @empJose INT = (SELECT EmpleadoID FROM dbo.Empleados WHERE Cedula = '004-4567890-1');
DECLARE @pLaptop2   INT = (SELECT ProductoID FROM dbo.Productos WHERE Codigo = 'P001');
DECLARE @pMonitor2  INT = (SELECT ProductoID FROM dbo.Productos WHERE Codigo = 'P004');
DECLARE @pImpresora2 INT = (SELECT ProductoID FROM dbo.Productos WHERE Codigo = 'P005');

/* Compra a credito, con un pago parcial */
INSERT INTO dbo.Compras (NumeroDocumento, ProveedorID, Fecha, Subtotal, Impuesto, Total, Saldo, Estado)
VALUES ('NCF-A0100001', @provImporta, '20260602', 280000.00, 50400.00, 330400.00, 130400.00, 'Pendiente');
SET @c1 = SCOPE_IDENTITY();
INSERT INTO dbo.DetalleCompra (CompraID, ProductoID, Cantidad, CostoUnitario, Importe) VALUES
(@c1, @pLaptop2, 10, 28000.00, 280000.00);

/* Compra a credito, sin pagos */
INSERT INTO dbo.Compras (NumeroDocumento, ProveedorID, Fecha, Subtotal, Impuesto, Total, Saldo, Estado)
VALUES ('NCF-B0200015', @provSuminis, '20260608', 45000.00, 8100.00, 53100.00, 53100.00, 'Pendiente');
SET @c2 = SCOPE_IDENTITY();
INSERT INTO dbo.DetalleCompra (CompraID, ProductoID, Cantidad, CostoUnitario, Importe) VALUES
(@c2, @pMonitor2,   5, 6200.00, 31000.00),
(@c2, @pImpresora2, 2, 7000.00, 14000.00);

/* Pago parcial a la compra 1 */
INSERT INTO dbo.Pagos (CompraID, ProveedorID, Fecha, Monto, FormaPago, Referencia, EmpleadoID)
VALUES (@c1, @provImporta, '20260620', 200000.00, 'Transferencia', 'PAG-55021', @empJose);
GO

/* Actualizar el Saldo (Cuentas por Pagar) de cada proveedor segun las compras pendientes */
UPDATE p
SET p.Saldo = ISNULL((SELECT SUM(co.Saldo) FROM dbo.Compras co
                      WHERE co.ProveedorID = p.ProveedorID AND co.Estado <> 'Anulada'), 0)
FROM dbo.Proveedores p;
GO

PRINT '===============================================================';
PRINT ' Datos de prueba insertados correctamente.';
PRINT ' Usuario: admin / Contrasena: admin123';
PRINT '===============================================================';
GO
