vamos con algo necesito que en mismo endpoint se puieda editar el pedido, pero se deben tomasr algunas concidertaciones en cuanta

como es el mismo endopint debe buscar que existe el registro en la base para modificar, sino se crea
cuando se modifica solo se puede moficiar los siguientes campos

{
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


    este es el json completo 

    