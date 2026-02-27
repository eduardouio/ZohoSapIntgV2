"""
Operaciones CRUD para pedidos SAP.
"""

from datetime import datetime
from decimal import Decimal

from sqlalchemy.orm import Session, joinedload
from sqlalchemy import select

from models import SAPOrder, SAPOrderDetail
from schemas import OrderCreate


def get_order_by_id_zoho(db: Session, id_zoho: str) -> SAPOrder | None:
    """Busca un pedido por su id_zoho."""
    stmt = select(SAPOrder).where(SAPOrder.id_zoho == id_zoho)
    return db.execute(stmt).scalar_one_or_none()


def get_order_by_id(db: Session, order_id: int) -> SAPOrder | None:
    """Busca un pedido por su id incluyendo detalles."""
    stmt = (
        select(SAPOrder)
        .options(joinedload(SAPOrder.details))
        .where(SAPOrder.id == order_id)
    )
    return db.execute(stmt).unique().scalar_one_or_none()


def create_order(db: Session, order_data: OrderCreate) -> SAPOrder:
    """
    Crea un nuevo pedido y sus líneas de detalle.
    La fecha de creación (created_at) se asigna automáticamente como NOW.
    """
    now = datetime.now()

    # Crear la orden
    db_order = SAPOrder(
        id_zoho=order_data.id_zoho,
        enterprise=order_data.enterprise,
        id_warehouse=order_data.id_warehouse,
        customer=order_data.customer,
        order_date=datetime.strptime(order_data.order_date, "%Y-%m-%d"),
        salesperson=order_data.salesperson,
        seler_email=order_data.seler_email,
        seler_id=order_data.seler_id,
        serie=order_data.serie,
        notes=order_data.notes,
        is_integrated=False,
        is_failed=False,
        is_updated=False,
        is_mail_send=False,
        created_at=now,
    )

    # Crear las líneas de detalle
    for detail in order_data.details:
        db_detail = SAPOrderDetail(
            product=detail.product,
            quantity=detail.quantity,
            unit_price=detail.unit_price,
            discount=detail.discount,
            total=detail.total,
            tax=Decimal("0"),
            cost_center=detail.cost_center or None,
            account=detail.account or None,
            created_at=now,
        )
        db_order.details.append(db_detail)

    db.add(db_order)
    db.commit()
    db.refresh(db_order)

    # Cargar detalles para la respuesta
    return get_order_by_id(db, db_order.id)


def update_order(db: Session, existing_order: SAPOrder, order_data: OrderCreate) -> SAPOrder:
    """
    Actualiza un pedido existente.
    Solo se pueden modificar: notes y details.
    Se marca is_updated = 1 para que el servicio de Windows lo re-sincronice con SAP.
    Se limpia is_failed = 0 y error_message = None para permitir re-integración.
    """
    now = datetime.now()

    # Actualizar solo los campos permitidos
    existing_order.notes = order_data.notes
    existing_order.is_updated = True
    existing_order.is_failed = False
    existing_order.error_message = None

    # Eliminar los detalles anteriores
    for old_detail in existing_order.details:
        db.delete(old_detail)
    db.flush()

    # Crear los nuevos detalles
    for detail in order_data.details:
        db_detail = SAPOrderDetail(
            order_id=existing_order.id,
            product=detail.product,
            quantity=detail.quantity,
            unit_price=detail.unit_price,
            discount=detail.discount,
            total=detail.total,
            tax=Decimal("0"),
            cost_center=detail.cost_center or None,
            account=detail.account or None,
            created_at=now,
        )
        db.add(db_detail)

    db.commit()
    db.refresh(existing_order)

    # Cargar detalles para la respuesta
    return get_order_by_id(db, existing_order.id)
