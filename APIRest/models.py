"""
Modelos ORM de SQLAlchemy que mapean las tablas SAP_Orders y SAP_Order_Details.
"""

from sqlalchemy import (
    Column,
    Integer,
    SmallInteger,
    String,
    DateTime,
    Boolean,
    Numeric,
    ForeignKey,
    func,
)
from sqlalchemy.orm import relationship, DeclarativeBase


class Base(DeclarativeBase):
    """Clase base declarativa para los modelos ORM."""
    pass


class SAPOrder(Base):
    """Representa la tabla SAP_Orders."""

    __tablename__ = "SAP_Orders"

    id = Column(Integer, primary_key=True, autoincrement=True)
    id_zoho = Column(String(50), nullable=False)
    enterprise = Column(String(50), nullable=False, default="VINESA")
    id_warehouse = Column(Integer, nullable=False, default=1)
    customer = Column(String(150), nullable=False)
    order_date = Column(DateTime, nullable=False)
    integration_date = Column(DateTime, nullable=True)
    is_integrated = Column(Boolean, nullable=False, default=False)
    is_failed = Column(Boolean, nullable=False, default=False)
    is_updated = Column(Boolean, nullable=False, default=False)
    is_mail_send = Column(Boolean, nullable=False, default=False)
    mail_send_date = Column(DateTime, nullable=True)
    salesperson = Column(String(150), nullable=True)
    seler_email = Column(String(150), nullable=True)
    seler_id = Column(SmallInteger, default=0)
    serie = Column(Integer, nullable=True)
    doc_num = Column(Integer, nullable=True)
    doc_entry = Column(Integer, nullable=True)
    error_message = Column(String(500), nullable=True)
    notes = Column(String(500), nullable=True)
    created_at = Column(DateTime, nullable=False, server_default=func.now())

    # Relación con los detalles
    details = relationship("SAPOrderDetail", back_populates="order", cascade="all, delete-orphan")


class SAPOrderDetail(Base):
    """Representa la tabla SAP_Order_Details."""

    __tablename__ = "SAP_Order_Details"

    id = Column(Integer, primary_key=True, autoincrement=True)
    order_id = Column(Integer, ForeignKey("SAP_Orders.id"), nullable=False)
    product = Column(String(150), nullable=False)
    quantity = Column(Numeric(18, 4), nullable=False)
    unit_price = Column(Numeric(18, 4), nullable=False)
    discount = Column(Numeric(18, 4), nullable=False, default=0)
    total = Column(Numeric(18, 4), nullable=False)
    tax = Column(Numeric(18, 4), nullable=False, default=0)
    cost_center = Column(String(50), nullable=True)
    account = Column(String(50), nullable=True)
    created_at = Column(DateTime, nullable=False, server_default=func.now())

    # Relación inversa
    order = relationship("SAPOrder", back_populates="details")
