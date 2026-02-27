# Zhoho‑SAP Integration API

API REST construida con **FastAPI** (Python) para la integración de pedidos desde Zoho hacia SAP.  
Permite crear pedidos en la base de datos intermedia (`SAP_Orders` / `SAP_Order_Details`) que luego el servicio de Windows **ZhohoSapIntg** procesa e integra con SAP Business One.

---

## Requisitos previos

| Componente | Versión mínima | Notas |
|---|---|---|
| Python | 3.10+ | Se recomienda 3.12 o superior |
| ODBC Driver for SQL Server | 17 | Necesario para conectarse a MSSQL |
| SQL Server | 2016+ | Base de datos `db_zhohoSAP` en `192.168.0.189` |
| Nginx *(opcional)* | 1.24+ | Solo si se desea proxy reverso en producción |

---

## Instalación en un ordenador nuevo

### 1. Instalar Python

Descargar desde [python.org](https://www.python.org/downloads/) e instalar.  
Marcar la casilla **"Add Python to PATH"** durante la instalación.

Verificar:

```powershell
python --version
```

### 2. Instalar ODBC Driver 17

Descargar e instalar desde:  
<https://learn.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server>

Verificar que esté disponible:

```powershell
Get-OdbcDriver | Where-Object Name -like "*SQL Server*"
```

### 3. Clonar / copiar el proyecto

Copiar la carpeta `APIRest` al servidor. Por ejemplo:

```
C:\Apps\ZhohoSapIntgAPI\
```

### 4. Crear un entorno virtual (recomendado)

```powershell
cd C:\Apps\ZhohoSapIntgAPI
python -m venv venv
.\venv\Scripts\Activate.ps1
```

### 5. Instalar dependencias

```powershell
pip install -r requirements.txt
```

### 6. Configurar la conexión a la base de datos

Editar el archivo `config.py` con los datos del servidor MSSQL:

```python
DB_SERVER   = "192.168.0.189"
DB_USER     = "intg"
DB_PASSWORD = "H0riz0ntes"
DB_NAME     = "db_zhohoSAP"
DB_DRIVER   = "ODBC Driver 17 for SQL Server"
```

### 7. Iniciar la API

**Modo desarrollo** (con recarga automática):

```powershell
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
```

**Modo producción** (múltiples workers):

```powershell
uvicorn main:app --host 127.0.0.1 --port 8000 --workers 4
```

### 8. Verificar que funciona

Abrir en el navegador:

- Health check: `http://localhost:8000/`
- Documentación Swagger: `http://localhost:8000/docs`
- Documentación ReDoc: `http://localhost:8000/redoc`

---

## Estructura de archivos

```
APIRest/
├── config.py          # Configuración de BD y parámetros de la API
├── database.py        # Motor SQLAlchemy y dependencia get_db
├── models.py          # Modelos ORM (SAP_Orders, SAP_Order_Details)
├── schemas.py         # Esquemas Pydantic de validación (entrada/salida)
├── crud.py            # Operaciones de creación y consulta
├── routes.py          # Definición de endpoints
├── main.py            # Punto de entrada FastAPI
├── requirements.txt   # Dependencias Python
└── README.md          # Este archivo
```

---

## Endpoints de la API

### Health Check

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/` | Verificación de estado de la API |

**Respuesta:**

```json
{
  "status": "ok",
  "message": "Zhoho-SAP Integration API is running."
}
```

---

### Crear o actualizar un pedido (UPSERT)

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/orders/` | Crea un nuevo pedido o actualiza uno existente |

**Comportamiento:**

- Si el `id_zoho` **no existe** en la base de datos → **crea** el pedido (retorna `201`).
- Si el `id_zoho` **ya existe** → **actualiza** únicamente `notes` y `details` (retorna `200`).
  - Se marca `is_updated = 1` para que el servicio de Windows re-sincronice con SAP.
  - Se limpia `is_failed = 0` y `error_message = null` para permitir re-integración.
  - Los detalles anteriores se eliminan y se reemplazan por los nuevos.

**Headers:**

```
Content-Type: application/json
```

#### Campos modificables en actualización

Cuando el pedido ya existe, solo se actualizan estos campos del JSON:

| Campo | Descripción |
|---|---|
| `notes` | Notas generales del pedido |
| `details` | Lista completa de productos (se reemplazan todos) |

> Los demás campos (`customer`, `enterprise`, `salesperson`, etc.) se **ignoran** en la actualización.

#### Estructura del JSON de entrada

##### Objeto principal (Orden)

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| `id_zoho` | `string` | Sí | ID único del pedido en Zoho. Debe iniciar con `"ZOHO-"`. |
| `customer` | `string` | Sí | Código del cliente en SAP. |
| `order_date` | `string` | Sí | Fecha del pedido en formato `yyyy-MM-dd`. |
| `salesperson` | `string` | Sí | Nombre del vendedor. |
| `seler_email` | `string` | Sí | Correo del vendedor. |
| `enterprise` | `string` | Sí | Empresa. Valores válidos: `VINESA`, `PLUSBRAND`, `SERVMULTIMARC`, `VINLITORAL`. |
| `id_warehouse` | `int` | Sí | ID de la bodega en SAP. |
| `seler_id` | `int` | Sí | ID del vendedor en SAP. |
| `serie` | `int` | Sí | Serie del documento (por defecto `1`). |
| `notes` | `string` | No | Notas generales del pedido. |
| `details` | `array` | Sí | Lista de productos. Debe contener al menos un producto. |

##### Cada objeto en `details` (Detalle de línea)

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| `product` | `string` | Sí | Código del producto en SAP. |
| `quantity` | `decimal` | Sí | Cantidad del producto. Mayor a `0`. |
| `unit_price` | `decimal` | Sí | Precio unitario. Mayor a `0`. |
| `discount` | `decimal` | Sí | Descuento en valor monetario. |
| `total` | `decimal` | Sí | Total de la línea: `(quantity × unit_price) - discount`. Mayor a `0`. |
| `cost_center` | `string` | No | Centro de costo. |
| `account` | `string` | No | Cuenta contable. |

#### Ejemplo de petición

```bash
curl -X POST http://localhost:8000/orders/ \
  -H "Content-Type: application/json" \
  -d '{
    "id_zoho": "ZOHO-API-0001",
    "customer": "C0102434438",
    "order_date": "2026-02-21",
    "salesperson": "Eduardo Villota",
    "seler_email": "evillota@vinesa.com.ec",
    "enterprise": "VINESA",
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
            "cost_center": "",
            "account": ""
        },
        {
            "product": "01022094490111010750",
            "quantity": 1.0,
            "unit_price": 17.25,
            "discount": 0.0,
            "total": 17.25,
            "cost_center": "CC-01",
            "account": "4-01-001"
        }
    ]
}'
```

#### Ejemplo con PowerShell (Invoke-RestMethod)

```powershell
$body = @{
    id_zoho      = "ZOHO-API-0001"
    customer     = "C0102434438"
    order_date   = "2026-02-21"
    salesperson  = "Eduardo Villota"
    seler_email  = "evillota@vinesa.com.ec"
    enterprise   = "VINESA"
    id_warehouse = 1
    seler_id     = 1
    serie        = 1
    notes        = "Pedido generado desde endpoint API"
    details      = @(
        @{
            product    = "01022094490106020750"
            quantity   = 2.0
            unit_price = 18.5
            discount   = 0.0
            total      = 37.0
            cost_center = ""
            account     = ""
        },
        @{
            product    = "01022094490111010750"
            quantity   = 1.0
            unit_price = 17.25
            discount   = 0.0
            total      = 17.25
            cost_center = "CC-01"
            account     = "4-01-001"
        }
    )
} | ConvertTo-Json -Depth 3

Invoke-RestMethod -Uri "http://localhost:8000/orders/" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body
```

#### Respuestas

| Código | Descripción |
|---|---|
| `201 Created` | Pedido **creado** exitosamente. Retorna el pedido completo. |
| `200 OK` | Pedido **actualizado** exitosamente. Solo se modificaron `notes` y `details`. |
| `422 Unprocessable Entity` | Error de validación (campos faltantes, formato incorrecto, etc.). |
| `500 Internal Server Error` | Error inesperado al insertar/actualizar en la base de datos. |

**Respuesta exitosa al crear (201):**

```json
{
  "message": "Pedido creado exitosamente.",
  "order": {
    "id": 1,
    "id_zoho": "ZOHO-API-0001",
    "enterprise": "VINESA",
    "id_warehouse": 1,
    "customer": "C0102434438",
    "order_date": "2026-02-21T00:00:00",
    "is_integrated": false,
    "is_failed": false,
    "is_updated": false,
    "is_mail_send": false,
    "salesperson": "Eduardo Villota",
    "seler_email": "evillota@vinesa.com.ec",
    "seler_id": 1,
    "serie": 1,
    "notes": "Pedido generado desde endpoint API",
    "created_at": "2026-02-27T10:30:00",
    "details": [...]
  }
}
```

**Respuesta exitosa al actualizar (200):**

```json
{
  "message": "Pedido actualizado exitosamente.",
  "order": {
    "id": 1,
    "id_zoho": "ZOHO-API-0001",
    "is_updated": true,
    "is_failed": false,
    "notes": "Notas actualizadas",
    "details": [...]
  }
}
```

---

### Obtener un pedido por ID numérico

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/orders/{order_id}` | Retorna un pedido con sus líneas de detalle |

**Ejemplo:**

```bash
curl http://localhost:8000/orders/1
```

| Código | Descripción |
|---|---|
| `200 OK` | Pedido encontrado. |
| `404 Not Found` | No se encontró el pedido con ese ID. |

---

### Obtener un pedido por id_zoho

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/orders/zoho/{id_zoho}` | Retorna un pedido buscándolo por su identificador de Zoho |

**Ejemplo:**

```bash
curl http://localhost:8000/orders/zoho/ZOHO-API-0001
```

| Código | Descripción |
|---|---|
| `200 OK` | Pedido encontrado. |
| `404 Not Found` | No se encontró el pedido con ese `id_zoho`. |

---

## Validaciones automáticas

La API valida automáticamente al recibir el JSON:

- `id_zoho` debe comenzar con el prefijo `ZOHO-`.
- `enterprise` debe ser una de: `VINESA`, `PLUSBRAND`, `SERVMULTIMARC`, `VINLITORAL`.
- `order_date` debe tener formato `yyyy-MM-dd` y ser una fecha válida.
- `details` debe tener al menos 1 elemento.
- `quantity`, `unit_price` y `total` deben ser mayores a `0`.
- `discount` debe ser mayor o igual a `0`.
- No se permite duplicar `id_zoho` (retorna `409 Conflict`).
- `created_at` se asigna automáticamente como la fecha/hora actual al momento de crear el registro.

---

## Configuración con Nginx (proxy reverso)

### 1. Instalar Nginx en Windows

Descargar desde <https://nginx.org/en/download.html> la versión estable para Windows.  
Extraer en `C:\nginx`.

### 2. Configurar Nginx

Editar `C:\nginx\conf\nginx.conf` y reemplazar el bloque `server` por:

```nginx
worker_processes  1;

events {
    worker_connections  1024;
}

http {
    include       mime.types;
    default_type  application/octet-stream;

    sendfile        on;
    keepalive_timeout  65;

    # ── Upstream: API FastAPI ──────────────────────────────────────────
    upstream zhoho_api {
        server 127.0.0.1:8000;
    }

    # ── Servidor principal ────────────────────────────────────────────
    server {
        listen       80;
        server_name  localhost;
        # Para usar un dominio: server_name  api.vinesa.com.ec;

        # ── Proxy hacia FastAPI ───────────────────────────────────────
        location / {
            proxy_pass         http://zhoho_api;
            proxy_http_version 1.1;
            proxy_set_header   Host              $host;
            proxy_set_header   X-Real-IP         $remote_addr;
            proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
            proxy_set_header   X-Forwarded-Proto $scheme;
            proxy_set_header   Connection        "";
            proxy_read_timeout 300s;
            proxy_send_timeout 300s;
        }

        # ── Manejo de errores ─────────────────────────────────────────
        error_page  500 502 503 504  /50x.html;
        location = /50x.html {
            root   html;
        }
    }
}
```

### 3. Iniciar Nginx

```powershell
cd C:\nginx
.\nginx.exe
```

### 4. Verificar

Abrir en el navegador:

```
http://localhost/docs
```

La documentación Swagger debe mostrarse igual que en el puerto 8000 pero ahora a través de Nginx en el puerto 80.

### 5. Comandos útiles de Nginx

```powershell
# Recargar configuración sin reiniciar
cd C:\nginx
.\nginx.exe -s reload

# Detener Nginx
.\nginx.exe -s stop

# Verificar configuración antes de aplicar
.\nginx.exe -t
```

---

## Ejecutar la API como servicio de Windows (producción)

Para que la API se inicie automáticamente al arrancar el servidor, se puede registrar como servicio con **NSSM** (Non-Sucking Service Manager):

### 1. Descargar NSSM

<https://nssm.cc/download>

### 2. Instalar el servicio

```powershell
nssm install ZhohoSapAPI "C:\Apps\ZhohoSapIntgAPI\venv\Scripts\python.exe" "-m uvicorn main:app --host 127.0.0.1 --port 8000 --workers 4"
nssm set ZhohoSapAPI AppDirectory "C:\Apps\ZhohoSapIntgAPI"
nssm set ZhohoSapAPI Description "Zhoho-SAP Integration API (FastAPI)"
nssm set ZhohoSapAPI Start SERVICE_AUTO_START
```

### 3. Iniciar / Detener / Eliminar

```powershell
nssm start ZhohoSapAPI
nssm stop ZhohoSapAPI
nssm remove ZhohoSapAPI confirm
```

---

## Mapeo de Vendedores, Empresas y Bodegas

| Vendedor | Seler ID | Correo | Empresa | Bodega | Warehouse ID |
|---|---:|---|---|---|---:|
| Alejandro Padovan | 35 | apadovan@vinesa.com.ec | VINESA | ALMACÉN GENERAL UIO | 1 |
| Edwin Ortega | 29 | eortega@vinesa.com.ec | VINESA | ALMACÉN GENERAL UIO | 1 |
| José Luis Rivera | 28 | jrivera@vinesa.com.ec | VINESA | ALMACÉN GENERAL UIO | 1 |
| Jessica López | 25 | jlopez@vinesa.com.ec | VINESA | ALMACÉN GENERAL UIO | 1 |
| Cristhian Arguello | 37 | carguello@vinesa.com.ec | VINESA | BODEGA MANTA | 15 |
| Masaki Hakamada | 36 | mhakamada@vinesa.com.ec | VINESA | BODEGA MANTA | 15 |
| Fernando Lituma | 31 | flituma@vinesa.com.ec | VINESA | BODEGA CUENCA | 8 |
| Henry Rojas | 33 | hrojas@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 5 |
| Vanessa Cevallos | 39 | vcevallos@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 5 |
| Paola Rossero | 28 | prossero@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 5 |
| Marjorie Cedeño | 32 | mcedeno@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 5 |
| Marjorie Cano | 42 | mcano@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 5 |
| Miguel Mora | 48 | mmora@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 5 |
| Roberto Roldos | 36 | rroldos@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 5 |
| Erick Pihuave | 45 | epihuave@vinlitoral.com.ec | VINLITORAL | BODEGA GENERAL G8 | 5 |
| Jaime Paredes | 124 | jparedes@plusbrand.com.ec | SERVMULTIMARC | MATRIZ PLUSBRAND GYE | 1 |
| Guillermo Neira | 99 | gneira@plusbrand.com.ec | SERVMULTIMARC | MATRIZ PLUSBRAND GYE | 1 |
| Jennifer Quintana | 52 | jquintana@plusbrand.com.ec | SERVMULTIMARC | MATRIZ PLUSBRAND GYE | 1 |
| Mathias Moral | 132 | mmoral@plusbrand.com.ec | SERVMULTIMARC | MATRIZ PLUSBRAND GYE | 1 |
| Alejandro Mendoza | 102 | amendoza@plusbrand.com.ec | SERVMULTIMARC | MATRIZ PLUSBRAND MANTA | 11 |
| Byron Cordova | 108 | bcordova@plusbrand.com.ec | SERVMULTIMARC | MATRIZ PLUSBRAND MANTA | 11 |
| Juan Pablo Cordova | 91 | jcordova@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND CUENCA | 17 |
| Marco Venegas | 98 | mvenegas@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND CUENCA | 17 |
| Nicolás Abril | 93 | nabril@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND / ALMACEN 10 DE AGOSTO | 1 / 4 |
| Fernanda Alvarez | 52 | falvarez@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND / ALMACEN 10 DE AGOSTO | 1 / 4 |
| Ricardo Cabeza de Vaca | 34 | rcabezadevaca@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND / ALMACEN 10 DE AGOSTO | 1 / 4 |
| Isabel Jacome | 117 | ijacome@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND / ALMACEN 10 DE AGOSTO | 1 / 4 |
| Sandra Orquera | 47 | sorquera@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND / ALMACEN 10 DE AGOSTO | 1 / 4 |
| Stalin Tapia | 34 | stapia@plusbrand.com.ec | PLUSBRAND | MATRIZ PLUSBRAND / ALMACEN 10 DE AGOSTO | 1 / 4 |
