ahiora necesito qyue me ayudes con algo adiciona
luego de que un registro se ha insertado este es tomado por otro subsistema y es insertado en otro loguar
pero al hacerlo se actualizan cuiertos campos en la base de datos 
 is_integrated BIT NOT NULL DEFAULT 0, -- si se integro con SAP o no
    is_failed BIT NOT NULL DEFAULT 0, -- si la integracion fallo por algun motivo (ejemplo: error de validacion en SAP) se debe marcar este campo para no seguir intentando integrar hasta que se revise el error y se corrija
    is_updated BIT NOT NULL DEFAULT 0, -- si esta actulizada desde la ultima integracion si lo esta se debe actualizar en SAP y volver a poner este campo en 0
    is_mail_send BIT NOT NULL DEFAULT 0, -- se notico al usurio
    mail_send_date DATETIME NULL, -- fecha de envio del correo de notificacion
    salesperson VARCHAR(150) NULL,
    seler_email VARCHAR(150) NULL,
doc_num INT NULL,
    doc_entry INT NULL,
    error_message VARCHAR(500) NULL,

quiero uin endpint que cuando consulte por id de zoho se entrege esta infiormacion
