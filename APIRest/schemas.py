"""
Modelos Pydantic para la validación de los datos de entrada y salida de la API.
"""

from __future__ import annotations

import re
from datetime import datetime
from decimal import Decimal
from typing import List, Optional

from pydantic import BaseModel, Field, field_validator, model_validator


# ── Valores válidos ─────────────────────────────────────────────────────────
VALID_ENTERPRISES = {"VINESA", "PLUSBRAND", "SERVMULTIMARC", "VINLITORAL"}


# ── Detalle de línea ────────────────────────────────────────────────────────
class OrderDetailCreate(BaseModel):
    """Esquema de entrada para cada línea de detalle del pedido."""

    product: str = Field(..., min_length=1, description="Código del producto en SAP.")
    quantity: Decimal = Field(..., gt=0, description="Cantidad del producto.")
    unit_price: Decimal = Field(..., gt=0, description="Precio unitario del producto.")
    discount: Decimal = Field(Decimal("0"), ge=0, description="Descuento en valor monetario.")
    total: Decimal = Field(..., gt=0, description="Total de la línea.")
    cost_center: Optional[str] = Field(None, description="Centro de costo.")
    account: Optional[str] = Field(None, description="Cuenta contable.")


class OrderDetailResponse(BaseModel):
    """Esquema de salida para cada línea de detalle del pedido."""

    id: int
    order_id: int
    product: str
    quantity: Decimal
    unit_price: Decimal
    discount: Decimal
    total: Decimal
    tax: Decimal
    cost_center: Optional[str] = None
    account: Optional[str] = None
    created_at: datetime

    model_config = {"from_attributes": True}


# ── Orden ───────────────────────────────────────────────────────────────────
class OrderCreate(BaseModel):
    """Esquema de entrada para crear un pedido."""

    id_zoho: str = Field(..., min_length=1, description="ID único del pedido en Zoho.  Debe iniciar con 'ZOHO-'.")
    customer: str = Field(..., min_length=1, description="Código del cliente en SAP.")
    order_date: str = Field(..., description="Fecha del pedido en formato yyyy-MM-dd.")
    salesperson: str = Field(..., min_length=1, description="Nombre del vendedor.")
    seler_email: str = Field(..., min_length=1, description="Correo del vendedor.")
    enterprise: str = Field(..., description="Nombre de la empresa.")
    id_warehouse: int = Field(1, ge=1, description="ID de la bodega en SAP.")
    seler_id: int = Field(..., ge=0, description="ID del vendedor en SAP.")
    serie: int = Field(1, description="Serie del documento.")
    notes: Optional[str] = Field(None, description="Notas generales del pedido.")
    details: List[OrderDetailCreate] = Field(..., min_length=1, description="Lista de productos del pedido.")

    # ── Validadores ─────────────────────────────────────────────────────────
    @field_validator("id_zoho")
    @classmethod
    def id_zoho_must_start_with_prefix(cls, v: str) -> str:
        if not v.startswith("ZOHO-"):
            raise ValueError("id_zoho debe iniciar con el prefijo 'ZOHO-'.")
        return v

    @field_validator("enterprise")
    @classmethod
    def enterprise_must_be_valid(cls, v: str) -> str:
        upper = v.upper()
        if upper not in VALID_ENTERPRISES:
            raise ValueError(
                f"enterprise inválida. Valores permitidos: {', '.join(sorted(VALID_ENTERPRISES))}."
            )
        return upper

    @field_validator("order_date")
    @classmethod
    def order_date_format(cls, v: str) -> str:
        if not re.match(r"^\d{4}-\d{2}-\d{2}$", v):
            raise ValueError("order_date debe estar en formato yyyy-MM-dd.")
        # Verificar que sea una fecha válida
        try:
            datetime.strptime(v, "%Y-%m-%d")
        except ValueError:
            raise ValueError("order_date no es una fecha válida.")
        return v


class OrderResponse(BaseModel):
    """Esquema de salida de un pedido creado."""

    id: int
    id_zoho: str
    enterprise: str
    id_warehouse: int
    customer: str
    order_date: datetime
    integration_date: Optional[datetime] = None
    is_integrated: bool
    is_failed: bool
    is_updated: bool
    is_mail_send: bool
    mail_send_date: Optional[datetime] = None
    salesperson: Optional[str] = None
    seler_email: Optional[str] = None
    seler_id: Optional[int] = None
    serie: Optional[int] = None
    doc_num: Optional[int] = None
    doc_entry: Optional[int] = None
    error_message: Optional[str] = None
    notes: Optional[str] = None
    created_at: datetime
    details: List[OrderDetailResponse] = []

    model_config = {"from_attributes": True}


class MessageResponse(BaseModel):
    """Respuesta genérica con mensaje."""

    message: str
    order: Optional[OrderResponse] = None
