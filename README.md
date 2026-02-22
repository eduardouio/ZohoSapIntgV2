# ConsoleApp2 como servicio (.NET Framework)

Este proyecto se migró de consola a **servicio de Windows**, manteniendo **.NET Framework 4.6**.

## Desarrollo con VS Code + PowerShell

> Abre VS Code en la carpeta del proyecto y usa la terminal PowerShell integrada.

### 1) Compilar

Opción recomendada (si tienes SDK .NET instalado):

```powershell
dotnet build .\ConsoleApp2.csproj -c Debug
```

Opción alternativa (si tienes MSBuild en PATH):

```powershell
msbuild .\ConsoleApp2.csproj /t:Build /p:Configuration=Debug /p:Platform=AnyCPU
```

### 2) Probar en modo consola (sin instalar servicio)

```powershell
.\bin\Debug\ConsoleApp2.exe --console
```

Este modo permite hacer pruebas rápidas durante desarrollo.

## Instalar y ejecutar como servicio de Windows

Ejecuta PowerShell como **Administrador**.

### 1) Crear servicio

```powershell
$exePath = (Resolve-Path ".\bin\Debug\ConsoleApp2.exe").Path
sc.exe create ConsoleApp2Service binPath= "\"$exePath\"" start= auto
```

### 2) Iniciar servicio

```powershell
sc.exe start ConsoleApp2Service
```

### 3) Detener y eliminar servicio

```powershell
sc.exe stop ConsoleApp2Service
sc.exe delete ConsoleApp2Service
```

## Notas

- Se ignoraron archivos vacíos/no usados por ahora, como pediste.
- La lógica SAP se ejecuta al iniciar el servicio y luego cada 5 minutos.
- Los pedidos se leen desde `DB_INTG_SAPZOHO_PROD` (`SAP_Orders` + `SAP_Order_Details`) filtrando `is_integrated = 0`.
- Cuando SAP crea la orden correctamente, se actualiza en SQL: `is_integrated = 1`, `integration_date`, `doc_entry`, `doc_num`.
- Se genera log en: `C:\ProgramData\ConsoleApp2\logs\app.log`

Monitoreo en tiempo real desde PowerShell:

```powershell
Get-Content "C:\ProgramData\ConsoleApp2\logs\app.log" -Wait
```

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
    customer VARCHAR(150) NOT NULL,
    order_date DATETIME NOT NULL,
    integration_date DATETIME NULL,
    is_integrated BIT NOT NULL DEFAULT 0, -- si se integro con SAP o no
    is_updated BIT NOT NULL DEFAULT 0, -- si esta actulizada desde la ultima integracion si lo esta se debe actualizar en SAP y volver a poner este campo en 0
    salesperson VARCHAR(150) NULL,
    serie INT NULL,
    doc_num INT NULL,
    doc_entry INT NULL,
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
