"""
Configuración de la conexión a base de datos MSSQL y parámetros generales de la API.
"""

# ── Conexión a Base de Datos ────────────────────────────────────────────────
DB_SERVER = "192.168.0.189"
DB_USER = "intg"
DB_PASSWORD = "Horiz0nt3s"
DB_NAME = "DB_INTG_SAPZOHO_PROD"
DB_DRIVER = "ODBC Driver 18 for SQL Server"

# Cadena de conexión para pyodbc / SQLAlchemy
CONNECTION_STRING = (
    f"mssql+pyodbc://{DB_USER}:{DB_PASSWORD}@{DB_SERVER}/{DB_NAME}"
    f"?driver={DB_DRIVER.replace(' ', '+')}&TrustServerCertificate=yes"
)

# ── Empresas válidas ────────────────────────────────────────────────────────
VALID_ENTERPRISES = {"VINESA", "PLUSBRAND", "SERVMULTIMARC", "VINLITORAL"}

# ── Autenticación ───────────────────────────────────────────────────────────
API_SECRET_TOKEN = "ZoHo$aP!ntG_2026#Pr0d"

# ── API ─────────────────────────────────────────────────────────────────────
API_TITLE = "Zhoho‑SAP Integration API"
API_VERSION = "1.0.0"
API_DESCRIPTION = "API REST para integración de pedidos de Zoho a SAP."
