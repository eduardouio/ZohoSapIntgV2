# ZhohoSapIntg como servicio (.NET Framework)

Este proyecto se migró de consola a **servicio de Windows**, manteniendo **.NET Framework 4.6**.

## Desarrollo con VS Code + PowerShell

> Abre VS Code en la carpeta del proyecto y usa la terminal PowerShell integrada.

### 1) Compilar

Opción recomendada (si tienes SDK .NET instalado):

```powershell
dotnet build .\ZhohoSapIntg.csproj -c Debug
```

Opción alternativa (si tienes MSBuild en PATH):

```powershell
msbuild .\ZhohoSapIntg.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU
```

### 2) Probar en modo consola (sin instalar servicio)

```powershell
.\bin\Debug\ZhohoSapIntg.exe --console
```

Este modo permite hacer pruebas rápidas durante desarrollo.

## Configuración multiempresa (`App.config`)

Ahora la integración procesa pedidos por cada empresa configurada, filtrando en SQL por:

- `enterprise`
- `id_warehouse`

La configuración se define en `App.config` con estas claves:

- `SqlServerName`, `SqlUserName`, `SqlPassword`, `IntegrationDatabase`
- `SapServer`, `SapLicenseServer`, `SapDbServerType`, `SapUserName`, `SapPassword`
- `EnterpriseMappings`

Formato de `EnterpriseMappings`:

```txt
ENTERPRISE|SAP_COMPANY_DB|WAREHOUSE_ID;ENTERPRISE2|SAP_COMPANY_DB2|WAREHOUSE_ID2
```

Ejemplo:

```txt
VINESA|TEST_VINESA|1;PLUSBRABD|TEST_PLUSBRABD|1;SERVMULTIMARC|TEST_SERVMULTIMARC|1;VINLITORAL|TEST_VINLITORAL|1
```

El mapeo de bases SAP queda externalizado para que en producción solo cambies `App.config`.

## Instalar y ejecutar como servicio de Windows

Ejecuta PowerShell como **Administrador**.

### 1) Crear servicio

```powershell
$exePath = (Resolve-Path ".\bin\Debug\ZhohoSapIntg.exe").Path
sc.exe create ZhohoSapIntgService binPath= "\"$exePath\"" start= auto
```

### 2) Iniciar servicio

```powershell
sc.exe start ZhohoSapIntgService
```

### 3) Detener y eliminar servicio

```powershell
sc.exe stop ZhohoSapIntgService
sc.exe delete ZhohoSapIntgService
```

## Notas

- Se ignoraron archivos vacíos/no usados por ahora, como pediste.
- La lógica SAP se ejecuta al iniciar el servicio y luego cada 5 minutos.
- Los pedidos se leen desde `DB_INTG_SAPZOHO_PROD` (`SAP_Orders` + `SAP_Order_Details`) filtrando `is_integrated = 0`.
- Cuando SAP crea la orden correctamente, se actualiza en SQL: `is_integrated = 1`, `integration_date`, `doc_entry`, `doc_num`.
- También se procesan pedidos ya integrados con `is_updated = 1` (y `doc_entry` con valor) para **actualizarlos** en SAP; al finalizar se vuelve `is_updated = 0`.
- Si falla la creación o actualización en SAP, se marca `is_failed = 1` y se guarda el detalle en `error_message` (máx. 500 chars).
- Los procesos de lectura para creación/actualización solo toman registros con `is_failed = 0`.
- Se genera log en: `C:\ProgramData\ZhohoSapIntg\logs\app.log`

Monitoreo en tiempo real desde PowerShell:

```powershell
Get-Content "C:\ProgramData\ZhohoSapIntg\logs\app.log" -Wait
```

## Mapeo de Vendedores, Empresas y Bodegas

| NOMBRE VENDEDOR | SELER ID | CORREO | EMPRESA | BODEGA | SAP ID |
|---|---:|---|---|---|---:|
| Alejandro Padovan | 35 | apadovan@vinesa.com.ec | VINESA | ALMACÉN GENERAL UIO | 1 |
| Edwin Ortega | 29 | eortega@vinesa.com.ec | VINESA | ALMACÉN GENERAL UIO | 1 |
| José Luis Rivera | 28 | jrivera@vinesa.com.ec | VINESA | ALMACÉN GENERAL UIO | 1 |
| Jessica López | 25 | jlopez@vinesa.com.ec | VINESA | ALMACÉN GENERAL UIO | 1 |
| Cristhian Arguello | 37 | carguello@vinesa.com.ec | VINESA | BODEGA MANTA | 15 |
| Masaki Hakamada | 36 | mhakamada@vinesa.com.ec | VINESA | BODEGA MANTA | 15 |
| Fernando Lituma | 31 | flituma@vinesa.com.ec | VINESA | BODEGA CUENCA | 8 |
| Henry Rojas | 33 | hrojas@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 05 |
| Vanessa Cevallos | 39 | vcevallos@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 05 |
| Paola Rossero | 28 | prossero@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 05 |
| Marjorie Cedeño | 32 | mcedeno@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 05 |
| Marjorie Cano | 42 | mcano@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 05 |
| Miguel Mora | 48 | mmora@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 05 |
| Roberto Roldos | 36 | rroldos@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 05 |
| Erick Pihuave | 45 | epihuave@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 05 |
| Jaime Paredes | 124 | jparedes@plusbrand.com.ec | SERVMULTIMARC | MATRIZ PLUSBRAND GYE | 1 |
| Guillermo Neira | 99 | gneira@plusbrand.com.ec | SERVMULTIMARC | MATRIZ PLUSBRAND GYE | 1 |
| Jennifer Quintana | 52 | jquintana@plusbrand.com.ec | SERVMULTIMARC | MATRIZ PLUSBRAND GYE | 1 |
| Mathias Moral | 132 | mmoral@plusbrand.com.ec | SERVMULTIMARC | MATRIZ PLUSBRAND GYE | 1 |
| Alejandro Mendoza | 102 | amendoza@plusbrand.com.ec | SERVMULTIMARC | MATRIZ PLUSBRAND MANTA | 11 |
| Byron Cordova | 108 | bcordova@plusbrand.com.ec | SERVMULTIMARC | MATRIZ PLUSBRAND MANTA | 11 |
| Juan Pablo Cordova | 91 | jcordova@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND CUENCA | 17 |
| Marco Venegas | 98 | mvenegas@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND CUENCA | 17 |
| Nicolás Abril | 93 | nabril@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND \| ALMACEN 10 DE AGOSTO | 1 \| 4 |
| Fernanda Alvarez | 52 | falvarez@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND \| ALMACEN 10 DE AGOSTO | 1 \| 4 |
| Ricardo Cabeza de Vaca | 34 | rcabezadevaca@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND \| ALMACEN 10 DE AGOSTO | 1 \| 4 |
| Isabel Jacome | 117 | ijacome@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND \| ALMACEN 10 DE AGOSTO | 1 \| 4 |
| Sandra Orquera | 47 | sorquera@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND \| ALMACEN 10 DE AGOSTO | 1 \| 4 |
| Stalin Tapia | 34 | stapia@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND \| ALMACEN 10 DE AGOSTO | 1 \| 4 |

---

sql de la consulta
```sql
CREATE DATABASE DB_INTG_SAPZOHO_PROD;
GO

USE DB_INTG_SAPZOHO_PROD;
GO

CREATE TABLE SAP_Orders (
    id INT IDENTITY(1,1) PRIMARY KEY, 
    id_zoho VARCHAR(50) NOT NULL,
    enterprise VARCHAR(50) NOT NULL DEFAULT 'VINESA','PLUSBRABD','SERVMULTIMARC','VINLITORAL', 
    id_warehouse INT NOT NULL DEFAULT 1, -- para futuras integraciones con otras bodegas
    customer VARCHAR(150) NOT NULL,
    order_date DATETIME NOT NULL,
    integration_date DATETIME NULL,
    is_integrated BIT NOT NULL DEFAULT 0, -- si se integro con SAP o no
    is_failed BIT NOT NULL DEFAULT 0, -- si la integracion fallo por algun motivo (ejemplo: error de validacion en SAP) se debe marcar este campo para no seguir intentando integrar hasta que se revise el error y se corrija
    is_updated BIT NOT NULL DEFAULT 0, -- si esta actulizada desde la ultima integracion si lo esta se debe actualizar en SAP y volver a poner este campo en 0
    is_mail_send BIT NOT NULL DEFAULT 0, -- se notico al usurio
    mail_send_date DATETIME NULL, -- fecha de envio del correo de notificacion
    salesperson VARCHAR(150) NULL,
    seler_email VARCHAR(150) NULL,
    seler_id SMALLINT DEFAULT 0,
    serie INT NULL,
    doc_num INT NULL,
    doc_entry INT NULL,
    error_message VARCHAR(500) NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE SAP_Order_Details (
    id INT IDENTITY(1,1) PRIMARY KEY,
    order_id INT NOT NULL,
    product VARCHAR(150) NOT NULL,
    quantity DECIMAL(18,4) NOT NULL,
    unit_price DECIMAL(18,4) NOT NULL,
    discount DECIMAL(18,4) NOT NULL DEFAULT 0,
    total DECIMAL(18,4) NOT NULL,
    tax DECIMAL(18,4) NOT NULL DEFAULT 0,
    cost_center VARCHAR(50) NULL,
    account VARCHAR(50) NULL,
    notes VARCHAR(500) NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_SAP_Order_Details_Order
        FOREIGN KEY (order_id)
        REFERENCES SAP_Orders(id)
);
GO
```

## SQL de datos de prueba

```sql
USE DB_INTG_SAPZOHO_PROD;
GO

-- Limpieza opcional de datos de prueba previos
DELETE d
FROM SAP_Order_Details d
INNER JOIN SAP_Orders o ON o.id = d.order_id
WHERE o.id_zoho LIKE 'ZOHO-TEST-%';

DELETE FROM SAP_Orders
WHERE id_zoho LIKE 'ZOHO-TEST-%';
GO

-- Inserta cabeceras (quedan pendientes con is_integrated = 0)
INSERT INTO SAP_Orders (id_zoho, customer, order_date, is_integrated, is_updated, salesperson, seler_email, seler_id, enterprise, id_warehouse)
VALUES
('ZOHO-TEST-0001','C0102434438', DATEADD(MINUTE,-30,GETDATE()), 0, 0, 'Eduardo Villota', 'evillota@vinesa.com.ec', 1),
('ZOHO-TEST-0002','C0102493269001', DATEADD(MINUTE,-25,GETDATE()), 0, 0, 'Eduardo Villota', 'evillota@vinesa.com.ec', 1),
('ZOHO-TEST-0003','C0102534328001', DATEADD(MINUTE,-20,GETDATE()), 0, 0, 'Eduardo Villota', 'evillota@vinesa.com.ec', 1),
('ZOHO-TEST-0004','C0102578507001', DATEADD(MINUTE,-15,GETDATE()), 0, 0, 'Eduardo Villota', 'evillota@vinesa.com.ec', 1),
('ZOHO-TEST-0005','C0102622289001', DATEADD(MINUTE,-10,GETDATE()), 0, 0, 'Eduardo Villota', 'evillota@vinesa.com.ec', 1);
GO

-- Inserta detalles usando artículos válidos
INSERT INTO SAP_Order_Details (order_id, product, quantity, unit_price, discount, total, tax, notes)
SELECT o.id, x.product, x.quantity, x.unit_price, x.discount,
       ROUND((x.quantity * x.unit_price) * (1 - (x.discount / 100.0)), 4) AS total,
       0,
       'Seed de prueba para integración SQL -> SAP'
FROM SAP_Orders o
INNER JOIN (
    VALUES
    ('ZOHO-TEST-0001','01022094490106020750', 2.0000, 18.5000, 0.0000),
    ('ZOHO-TEST-0001','01022094490111010750', 1.0000, 17.2500, 0.0000),
    ('ZOHO-TEST-0002','01022094490205010750', 3.0000, 9.8000, 2.0000),
    ('ZOHO-TEST-0002','01022094490205020750', 2.0000, 10.4000, 0.0000),
    ('ZOHO-TEST-0003','01022110330102020750', 4.0000, 11.9000, 3.0000),
    ('ZOHO-TEST-0003','01022110330103020750', 2.0000, 12.3000, 0.0000),
    ('ZOHO-TEST-0004','01022113620901010750', 1.0000, 22.5000, 0.0000),
    ('ZOHO-TEST-0004','01022113621001010750', 2.0000, 20.7500, 1.5000),
    ('ZOHO-TEST-0005','01022190080206010200', 5.0000, 6.9500, 0.0000),
    ('ZOHO-TEST-0005','01022190080213010750', 2.0000, 14.2000, 0.0000)
) x(id_zoho, product, quantity, unit_price, discount)
    ON o.id_zoho = x.id_zoho;
GO

-- 20 órdenes adicionales de prueba
DECLARE @Customers TABLE (idx INT IDENTITY(1,1), code VARCHAR(50));
INSERT INTO @Customers(code)
VALUES
('C0102434438'),
('C0102493269001'),
('C0102534328001'),
('C0102578507001'),
('C0102622289001'),
('C0102704871001'),
('C0102734456001'),
('C0102833654001'),
('C0102962545001'),
('C0102983996001'),
('C0103030128001'),
('C0103051108'),
('C0103106159001'),
('C0103116174001'),
('C0103175246001');

DECLARE @Products TABLE (idx INT IDENTITY(1,1), product VARCHAR(150), price DECIMAL(18,4));
INSERT INTO @Products(product, price)
VALUES
('01022094490106020750',18.5000),
('01022094490111010750',17.2500),
('01022094490205010750',9.8000),
('01022094490205020750',10.4000),
('01022094490205040750',11.1500),
('01022094490206010750',9.9500),
('01022110290210010750',48.0000),
('01022110330101020750',12.1000),
('01022110330102020750',11.9000),
('01022110330103020750',12.3000),
('01022113620901010750',22.5000),
('01022113621001010750',20.7500),
('01022114900101010750',75.9000),
('01022114900103010750',79.9000),
('01022190080206010200',6.9500),
('01022190080213010750',14.2000);

DECLARE @ProductCount INT = (SELECT COUNT(1) FROM @Products);
DECLARE @CustomerCount INT = (SELECT COUNT(1) FROM @Customers);

;WITH N AS (
    SELECT TOP (20) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects
)
INSERT INTO SAP_Orders (id_zoho, customer, order_date, is_integrated, is_updated, salesperson, seler_email, seler_id)
SELECT
    CONCAT('ZOHO-TEST-X', RIGHT('0000' + CAST(n AS VARCHAR(4)), 4)),
    c.code,
    DATEADD(MINUTE, -(90 + n), GETDATE()),
    0,
    0,
    'Eduardo Villota',
    'evillota@vinesa.com.ec',
    1
FROM N
CROSS APPLY (
    SELECT code
    FROM @Customers
    WHERE idx = ((N.n - 1) % @CustomerCount) + 1
) c;

;WITH O AS (
    SELECT id, TRY_CONVERT(INT, RIGHT(id_zoho, 4)) AS n
    FROM SAP_Orders
    WHERE id_zoho LIKE 'ZOHO-TEST-X%'
)
INSERT INTO SAP_Order_Details (order_id, product, quantity, unit_price, discount, total, tax, notes)
SELECT
    O.id,
    p.product,
    q.qty,
    p.price,
    q.discount,
    ROUND((q.qty * p.price) * (1 - (q.discount / 100.0)), 4),
    0,
    'Seed de prueba masivo (20 órdenes)'
FROM O
CROSS APPLY (
    VALUES
    ((((O.n - 1) % @ProductCount) + 1), CAST(1 + (O.n % 3) AS DECIMAL(18,4)), CAST(0.0000 AS DECIMAL(18,4))),
    ((((O.n + 6 - 1) % @ProductCount) + 1), CAST(1 + ((O.n + 1) % 2) AS DECIMAL(18,4)), CAST(2.0000 AS DECIMAL(18,4)))
) q(product_idx, qty, discount)
INNER JOIN @Products p ON p.idx = q.product_idx;
GO

-- Verificación
SELECT o.id, o.id_zoho, o.customer, o.is_integrated, o.order_date,
       d.product, d.quantity, d.unit_price, d.discount, d.total
FROM SAP_Orders o
INNER JOIN SAP_Order_Details d ON d.order_id = o.id
WHERE o.id_zoho LIKE 'ZOHO-TEST-%'
ORDER BY o.id, d.id;
GO
```

## Documentos SAP B1 (BoObjectTypes)

La **orden de venta** en SAP Business One corresponde a:

- `oOrders` = **17**

Tabla de referencia en español latino:

| Objeto (`BoObjectTypes`) | Código | Nombre en español (LatAm) |
|---|---:|---|
| `oQuotations` | 23 | Cotización de venta |
| `oOrders` | 17 | Orden de venta / Pedido de cliente |
| `oDeliveryNotes` | 15 | Entrega (nota de entrega) |
| `oReturns` | 16 | Devolución de cliente |
| `oInvoices` | 13 | Factura de clientes |
| `oCreditNotes` | 14 | Nota de crédito de clientes |
| `oDownPayments` | 203 | Anticipo de clientes |
| `oIncomingPayments` | 24 | Pago recibido (cobro) |
| `oPurchaseQuotations` | 540000006 | Solicitud de cotización de compra |
| `oPurchaseOrders` | 22 | Orden de compra |
| `oPurchaseDeliveryNotes` | 20 | Entrada de mercancías (compra) |
| `oPurchaseReturns` | 21 | Devolución a proveedor |
| `oPurchaseInvoices` | 18 | Factura de proveedor |
| `oPurchaseCreditNotes` | 19 | Nota de crédito de proveedor |
| `oPurchaseDownPayments` | 204 | Anticipo a proveedor |
| `oVendorPayments` | 46 | Pago efectuado a proveedor |
| `oInventoryGenEntry` | 59 | Entrada de mercancías (inventario) |
| `oInventoryGenExit` | 60 | Salida de mercancías (inventario) |
| `oStockTransfer` | 67 | Transferencia de inventario |
| `oInventoryTransferRequest` | 1250000001 | Solicitud de traslado de inventario |
| `oDrafts` | 112 | Borradores de documentos |




```txt
Errores recibidos en SAP
Error al crear la orden de venta: (1009) Codigo : 01011010010206010750 con saldo Negativo

## JSON de muestra para endpoint (recepción de pedidos)

```json
{
    "id_zoho": "ZOHO-API-0001",
    "customer": "C0102434438",
    "order_date": "2026-02-21T10:30:00",
    "salesperson": "Eduardo Villota",
    "seler_email": "evillota@vinesa.com.ec",
    "enterprise" : "VINESA",
    "id_warehouse": 1,
    "seler_id": 1,
    "serie": 1,
    "notes": "Pedido generado desde endpoint API",
    "details": [
        {
            "product": "01022094490106020750",
            "quantity": 2.0,
            "unit_price": 18.5,
            "discount": 0.0,
            "total": 37.0,
            "cost_center": "CC-01",
            "account": "4-01-001",
        },
        {
            "product": "01022094490111010750",
            "quantity": 1.0,
            "unit_price": 17.25,
            "discount": 0.0,
            "total": 17.25,
            "cost_center": "CC-01",
            "account": "4-01-001",
        }
    ]
}
```


```sql
USE master;
GO

-- Cerrar todas las conexiones activas a la base de datos
ALTER DATABASE DB_INTG_SAPZOHO_PROD SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

-- Eliminar la base de datos
DROP DATABASE DB_INTG_SAPZOHO_PROD;
GO