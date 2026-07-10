/* =====================================================================
   PROYECTO FINAL DE PROGRAMACION
   Sistema de Facturacion y Cuentas por Cobrar / Cuentas por Pagar
   ---------------------------------------------------------------------
   Script 01: Creacion de la base de datos, tablas, relaciones y restricciones
   Motor: Microsoft SQL Server (compatible con LocalDB / Express)
   ---------------------------------------------------------------------
   Orden de ejecucion:
       1) 01_CrearBaseDatos.sql   <-- este archivo
       2) 02_DatosPrueba.sql
   ===================================================================== */

/* ---------------------------------------------------------------------
   Crear la base de datos si no existe
   --------------------------------------------------------------------- */
IF DB_ID('SistemaFacturacionDB') IS NULL
BEGIN
    CREATE DATABASE SistemaFacturacionDB;
END
GO

USE SistemaFacturacionDB;
GO

/* ---------------------------------------------------------------------
   Eliminar tablas si ya existen (para poder re-ejecutar el script).
   Se eliminan respetando el orden de las llaves foraneas (hijas primero).
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Pagos','U')          IS NOT NULL DROP TABLE dbo.Pagos;
IF OBJECT_ID('dbo.DetalleCompra','U')  IS NOT NULL DROP TABLE dbo.DetalleCompra;
IF OBJECT_ID('dbo.Compras','U')        IS NOT NULL DROP TABLE dbo.Compras;
IF OBJECT_ID('dbo.Cobros','U')         IS NOT NULL DROP TABLE dbo.Cobros;
IF OBJECT_ID('dbo.DetalleFactura','U') IS NOT NULL DROP TABLE dbo.DetalleFactura;
IF OBJECT_ID('dbo.Facturas','U')       IS NOT NULL DROP TABLE dbo.Facturas;
IF OBJECT_ID('dbo.Productos','U')      IS NOT NULL DROP TABLE dbo.Productos;
IF OBJECT_ID('dbo.Proveedores','U')    IS NOT NULL DROP TABLE dbo.Proveedores;
IF OBJECT_ID('dbo.Clientes','U')       IS NOT NULL DROP TABLE dbo.Clientes;
IF OBJECT_ID('dbo.Empleados','U')      IS NOT NULL DROP TABLE dbo.Empleados;
IF OBJECT_ID('dbo.Usuarios','U')       IS NOT NULL DROP TABLE dbo.Usuarios;
GO

/* =====================================================================
   TABLA: Usuarios  (control de acceso al sistema)
   ===================================================================== */
CREATE TABLE dbo.Usuarios (
    UsuarioID        INT IDENTITY(1,1)   NOT NULL,
    NombreUsuario    NVARCHAR(50)        NOT NULL,
    Contrasena       NVARCHAR(256)       NOT NULL,   -- almacenada como hash SHA-256
    NombreCompleto   NVARCHAR(100)       NULL,
    Rol              NVARCHAR(20)        NOT NULL CONSTRAINT CK_Usuarios_Rol CHECK (Rol IN ('Administrador','Usuario')),
    Activo           BIT                 NOT NULL CONSTRAINT DF_Usuarios_Act   DEFAULT (1),
    FechaCreacion    DATETIME            NOT NULL CONSTRAINT DF_Usuarios_Fecha DEFAULT (GETDATE()),
    CONSTRAINT PK_Usuarios     PRIMARY KEY (UsuarioID),
    CONSTRAINT UQ_Usuarios_Nom UNIQUE (NombreUsuario)
);
GO

/* =====================================================================
   TABLA: Empleados
   ===================================================================== */
CREATE TABLE dbo.Empleados (
    EmpleadoID    INT IDENTITY(1,1) NOT NULL,
    Nombre        NVARCHAR(50)      NOT NULL,
    Apellido      NVARCHAR(50)      NOT NULL,
    Cedula        NVARCHAR(20)      NULL,
    Cargo         NVARCHAR(50)      NULL,
    Telefono      NVARCHAR(20)      NULL,
    Email         NVARCHAR(100)     NULL,
    FechaIngreso  DATE              NULL,
    Activo        BIT               NOT NULL CONSTRAINT DF_Empleados_Act DEFAULT (1),
    CONSTRAINT PK_Empleados     PRIMARY KEY (EmpleadoID),
    CONSTRAINT UQ_Empleados_Ced UNIQUE (Cedula)
);
GO

/* =====================================================================
   TABLA: Clientes  (el campo Saldo acumula las Cuentas por Cobrar)
   ===================================================================== */
CREATE TABLE dbo.Clientes (
    ClienteID      INT IDENTITY(1,1) NOT NULL,
    Nombre         NVARCHAR(100)     NOT NULL,
    Identificacion NVARCHAR(20)      NULL,   -- Cedula o RNC
    Telefono       NVARCHAR(20)      NULL,
    Email          NVARCHAR(100)     NULL,
    Direccion      NVARCHAR(200)     NULL,
    LimiteCredito  DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Clientes_Lim DEFAULT (0),
    Saldo          DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Clientes_Sal DEFAULT (0),   -- total adeudado por el cliente
    Activo         BIT               NOT NULL CONSTRAINT DF_Clientes_Act DEFAULT (1),
    CONSTRAINT PK_Clientes     PRIMARY KEY (ClienteID),
    CONSTRAINT CK_Clientes_Lim CHECK (LimiteCredito >= 0)
);
GO

/* =====================================================================
   TABLA: Proveedores  (el campo Saldo acumula las Cuentas por Pagar)
   ===================================================================== */
CREATE TABLE dbo.Proveedores (
    ProveedorID  INT IDENTITY(1,1) NOT NULL,
    Nombre       NVARCHAR(100)     NOT NULL,
    RNC          NVARCHAR(20)      NULL,
    Telefono     NVARCHAR(20)      NULL,
    Email        NVARCHAR(100)     NULL,
    Direccion    NVARCHAR(200)     NULL,
    Saldo        DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Proveedores_Sal DEFAULT (0),   -- total que se le debe al proveedor
    Activo       BIT               NOT NULL CONSTRAINT DF_Proveedores_Act DEFAULT (1),
    CONSTRAINT PK_Proveedores PRIMARY KEY (ProveedorID)
);
GO

/* =====================================================================
   TABLA: Productos  (productos y servicios)
   ===================================================================== */
CREATE TABLE dbo.Productos (
    ProductoID   INT IDENTITY(1,1) NOT NULL,
    Codigo       NVARCHAR(30)      NULL,
    Nombre       NVARCHAR(100)     NOT NULL,
    Descripcion  NVARCHAR(200)     NULL,
    Tipo         NVARCHAR(20)      NOT NULL CONSTRAINT CK_Productos_Tipo CHECK (Tipo IN ('Producto','Servicio')),
    Precio       DECIMAL(18,2)     NOT NULL CONSTRAINT CK_Productos_Prec CHECK (Precio >= 0),
    Costo        DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Productos_Cost DEFAULT (0),
    Stock        INT               NOT NULL CONSTRAINT DF_Productos_Stk  DEFAULT (0),
    Activo       BIT               NOT NULL CONSTRAINT DF_Productos_Act  DEFAULT (1),
    CONSTRAINT PK_Productos     PRIMARY KEY (ProductoID),
    CONSTRAINT UQ_Productos_Cod UNIQUE (Codigo)
);
GO

/* =====================================================================
   TABLA: Facturas  (encabezado de venta - genera Cuentas por Cobrar)
   ===================================================================== */
CREATE TABLE dbo.Facturas (
    FacturaID      INT IDENTITY(1,1) NOT NULL,
    NumeroFactura  NVARCHAR(20)      NOT NULL,
    ClienteID      INT               NOT NULL,
    EmpleadoID     INT               NULL,
    Fecha          DATETIME          NOT NULL CONSTRAINT DF_Facturas_Fecha DEFAULT (GETDATE()),
    Subtotal       DECIMAL(18,2)     NOT NULL,
    Impuesto       DECIMAL(18,2)     NOT NULL,
    Total          DECIMAL(18,2)     NOT NULL,
    Saldo          DECIMAL(18,2)     NOT NULL,   -- pendiente de cobro
    TipoPago       NVARCHAR(20)      NOT NULL CONSTRAINT CK_Facturas_TPago CHECK (TipoPago IN ('Contado','Credito')),
    Estado         NVARCHAR(20)      NOT NULL CONSTRAINT DF_Facturas_Est DEFAULT ('Pendiente')
                                     CONSTRAINT CK_Facturas_Est CHECK (Estado IN ('Pendiente','Pagada','Anulada')),
    CONSTRAINT PK_Facturas     PRIMARY KEY (FacturaID),
    CONSTRAINT UQ_Facturas_Num UNIQUE (NumeroFactura),
    CONSTRAINT FK_Facturas_Cli FOREIGN KEY (ClienteID)  REFERENCES dbo.Clientes(ClienteID),
    CONSTRAINT FK_Facturas_Emp FOREIGN KEY (EmpleadoID) REFERENCES dbo.Empleados(EmpleadoID)
);
GO

/* =====================================================================
   TABLA: DetalleFactura  (lineas de la factura)
   ===================================================================== */
CREATE TABLE dbo.DetalleFactura (
    DetalleID       INT IDENTITY(1,1) NOT NULL,
    FacturaID       INT               NOT NULL,
    ProductoID      INT               NOT NULL,
    Cantidad        INT               NOT NULL CONSTRAINT CK_DetFact_Cant CHECK (Cantidad > 0),
    PrecioUnitario  DECIMAL(18,2)     NOT NULL,
    Importe         DECIMAL(18,2)     NOT NULL,   -- Cantidad * PrecioUnitario
    CONSTRAINT PK_DetalleFactura   PRIMARY KEY (DetalleID),
    CONSTRAINT FK_DetFact_Factura  FOREIGN KEY (FacturaID)  REFERENCES dbo.Facturas(FacturaID)  ON DELETE CASCADE,
    CONSTRAINT FK_DetFact_Producto FOREIGN KEY (ProductoID) REFERENCES dbo.Productos(ProductoID)
);
GO

/* =====================================================================
   TABLA: Cobros  (pagos recibidos del cliente - abonos a Cuentas por Cobrar)
   ===================================================================== */
CREATE TABLE dbo.Cobros (
    CobroID     INT IDENTITY(1,1) NOT NULL,
    FacturaID   INT               NOT NULL,
    ClienteID   INT               NOT NULL,
    Fecha       DATETIME          NOT NULL CONSTRAINT DF_Cobros_Fecha DEFAULT (GETDATE()),
    Monto       DECIMAL(18,2)     NOT NULL CONSTRAINT CK_Cobros_Monto CHECK (Monto > 0),
    FormaPago   NVARCHAR(30)      NOT NULL,   -- Efectivo / Transferencia / Tarjeta / Cheque
    Referencia  NVARCHAR(50)      NULL,
    EmpleadoID  INT               NULL,
    CONSTRAINT PK_Cobros         PRIMARY KEY (CobroID),
    CONSTRAINT FK_Cobros_Factura FOREIGN KEY (FacturaID)  REFERENCES dbo.Facturas(FacturaID),
    CONSTRAINT FK_Cobros_Cliente FOREIGN KEY (ClienteID)  REFERENCES dbo.Clientes(ClienteID),
    CONSTRAINT FK_Cobros_Emp     FOREIGN KEY (EmpleadoID) REFERENCES dbo.Empleados(EmpleadoID)
);
GO

/* =====================================================================
   TABLA: Compras  (facturas de proveedor - genera Cuentas por Pagar)
   ===================================================================== */
CREATE TABLE dbo.Compras (
    CompraID        INT IDENTITY(1,1) NOT NULL,
    NumeroDocumento NVARCHAR(30)      NOT NULL,
    ProveedorID     INT               NOT NULL,
    Fecha           DATETIME          NOT NULL CONSTRAINT DF_Compras_Fecha DEFAULT (GETDATE()),
    Subtotal        DECIMAL(18,2)     NOT NULL,
    Impuesto        DECIMAL(18,2)     NOT NULL,
    Total           DECIMAL(18,2)     NOT NULL,
    Saldo           DECIMAL(18,2)     NOT NULL,   -- pendiente de pago
    Estado          NVARCHAR(20)      NOT NULL CONSTRAINT DF_Compras_Est DEFAULT ('Pendiente')
                                      CONSTRAINT CK_Compras_Est CHECK (Estado IN ('Pendiente','Pagada','Anulada')),
    CONSTRAINT PK_Compras      PRIMARY KEY (CompraID),
    CONSTRAINT FK_Compras_Prov FOREIGN KEY (ProveedorID) REFERENCES dbo.Proveedores(ProveedorID)
);
GO

/* =====================================================================
   TABLA: DetalleCompra  (lineas de la compra)
   ===================================================================== */
CREATE TABLE dbo.DetalleCompra (
    DetalleID      INT IDENTITY(1,1) NOT NULL,
    CompraID       INT               NOT NULL,
    ProductoID     INT               NOT NULL,
    Cantidad       INT               NOT NULL CONSTRAINT CK_DetComp_Cant CHECK (Cantidad > 0),
    CostoUnitario  DECIMAL(18,2)     NOT NULL,
    Importe        DECIMAL(18,2)     NOT NULL,
    CONSTRAINT PK_DetalleCompra    PRIMARY KEY (DetalleID),
    CONSTRAINT FK_DetComp_Compra   FOREIGN KEY (CompraID)   REFERENCES dbo.Compras(CompraID) ON DELETE CASCADE,
    CONSTRAINT FK_DetComp_Producto FOREIGN KEY (ProductoID) REFERENCES dbo.Productos(ProductoID)
);
GO

/* =====================================================================
   TABLA: Pagos  (pagos realizados al proveedor - abonos a Cuentas por Pagar)
   ===================================================================== */
CREATE TABLE dbo.Pagos (
    PagoID       INT IDENTITY(1,1) NOT NULL,
    CompraID     INT               NOT NULL,
    ProveedorID  INT               NOT NULL,
    Fecha        DATETIME          NOT NULL CONSTRAINT DF_Pagos_Fecha DEFAULT (GETDATE()),
    Monto        DECIMAL(18,2)     NOT NULL CONSTRAINT CK_Pagos_Monto CHECK (Monto > 0),
    FormaPago    NVARCHAR(30)      NOT NULL,
    Referencia   NVARCHAR(50)      NULL,
    EmpleadoID   INT               NULL,
    CONSTRAINT PK_Pagos        PRIMARY KEY (PagoID),
    CONSTRAINT FK_Pagos_Compra FOREIGN KEY (CompraID)    REFERENCES dbo.Compras(CompraID),
    CONSTRAINT FK_Pagos_Prov   FOREIGN KEY (ProveedorID) REFERENCES dbo.Proveedores(ProveedorID),
    CONSTRAINT FK_Pagos_Emp    FOREIGN KEY (EmpleadoID)  REFERENCES dbo.Empleados(EmpleadoID)
);
GO

PRINT '===============================================================';
PRINT ' Base de datos SistemaFacturacionDB creada correctamente.';
PRINT ' Ejecute ahora el script 02_DatosPrueba.sql';
PRINT '===============================================================';
GO
