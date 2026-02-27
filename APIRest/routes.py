"""
Router de pedidos – endpoints de la API.
"""

from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from database import get_db
from schemas import OrderCreate, OrderResponse, MessageResponse
from crud import get_order_by_id_zoho, create_order, update_order, get_order_by_id

router = APIRouter(prefix="/orders", tags=["Pedidos"])


@router.post(
    "/",
    response_model=MessageResponse,
    summary="Crear o actualizar un pedido",
    description=(
        "Si el `id_zoho` no existe, crea un nuevo pedido (201).\n\n"
        "Si el `id_zoho` ya existe, actualiza únicamente `notes` y `details`, "
        "marca `is_updated = 1` para re-sincronización con SAP y retorna (200)."
    ),
)
def create_or_update_order(
    order_data: OrderCreate,
    db: Session = Depends(get_db),
):
    """
    Endpoint UPSERT: recibe el JSON completo del pedido.
    - Si `id_zoho` NO existe → crea el registro (201).
    - Si `id_zoho` YA existe → actualiza solo `notes` y `details` (200).
      Al actualizar se marca `is_updated = 1` y se limpia `is_failed`.
    """
    existing = get_order_by_id_zoho(db, order_data.id_zoho)

    if existing:
        # ── Actualizar pedido existente ──────────────────────────────────
        try:
            updated_order = update_order(db, existing, order_data)
        except Exception as e:
            db.rollback()
            raise HTTPException(
                status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                detail=f"Error al actualizar el pedido: {str(e)}",
            )

        from starlette.responses import JSONResponse
        return JSONResponse(
            status_code=status.HTTP_200_OK,
            content=MessageResponse(
                message="Pedido actualizado exitosamente.",
                order=OrderResponse.model_validate(updated_order),
            ).model_dump(mode="json"),
        )
    else:
        # ── Crear pedido nuevo ──────────────────────────────────────────
        try:
            new_order = create_order(db, order_data)
        except Exception as e:
            db.rollback()
            raise HTTPException(
                status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                detail=f"Error al crear el pedido: {str(e)}",
            )

        from starlette.responses import JSONResponse
        return JSONResponse(
            status_code=status.HTTP_201_CREATED,
            content=MessageResponse(
                message="Pedido creado exitosamente.",
                order=OrderResponse.model_validate(new_order),
            ).model_dump(mode="json"),
        )


@router.get(
    "/{order_id}",
    response_model=OrderResponse,
    summary="Obtener un pedido por ID",
    description="Retorna un pedido con sus líneas de detalle.",
)
def read_order(order_id: int, db: Session = Depends(get_db)):
    """Obtiene un pedido existente por su ID numérico."""
    order = get_order_by_id(db, order_id)
    if not order:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"No se encontró el pedido con id {order_id}.",
        )
    return OrderResponse.model_validate(order)


@router.get(
    "/zoho/{id_zoho}",
    response_model=OrderResponse,
    summary="Obtener un pedido por id_zoho",
    description="Retorna un pedido buscándolo por su identificador de Zoho.",
)
def read_order_by_zoho(id_zoho: str, db: Session = Depends(get_db)):
    """Obtiene un pedido existente por su id_zoho."""
    order = get_order_by_id_zoho(db, id_zoho)
    if not order:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"No se encontró el pedido con id_zoho '{id_zoho}'.",
        )
    # Cargar detalles
    from crud import get_order_by_id as _get
    full_order = _get(db, order.id)
    return OrderResponse.model_validate(full_order)
