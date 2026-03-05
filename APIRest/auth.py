"""
Autenticación por Bearer Token estático.

El token se define en config.py (API_SECRET_TOKEN).
Todas las rutas protegidas requieren el header:
    Authorization: Bearer <token>
"""

from fastapi import Depends, HTTPException, status
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials

from config import API_SECRET_TOKEN

_bearer_scheme = HTTPBearer()


def verify_token(
    credentials: HTTPAuthorizationCredentials = Depends(_bearer_scheme),
) -> str:
    """
    Dependencia de FastAPI que valida el Bearer token.
    Lanza 401 si el token es inválido o no se proporcionó.
    """
    if credentials.credentials != API_SECRET_TOKEN:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Token inválido o no autorizado.",
            headers={"WWW-Authenticate": "Bearer"},
        )
    return credentials.credentials
