-- 013_auditoria_detallada.sql
-- Amplía el historial de auditoría para que cada registro explique por sí solo qué pasó:
-- en qué módulo, sobre qué objeto, qué cambió campo a campo, con qué resultado y desde dónde.
--
-- Los registros anteriores a este cambio se quedan con el módulo deducido de la entidad y sin
-- descripción ni detalle de cambios: son las acciones que se guardaron cuando aún no existían
-- esas columnas.

ALTER TABLE audit_logs
    ADD COLUMN IF NOT EXISTS user_roles VARCHAR(300) NULL AFTER user_display_name;

ALTER TABLE audit_logs
    ADD COLUMN IF NOT EXISTS module VARCHAR(60) NOT NULL DEFAULT '' AFTER ip_address;

ALTER TABLE audit_logs
    ADD COLUMN IF NOT EXISTS entity_key VARCHAR(100) NULL AFTER entity_id;

ALTER TABLE audit_logs
    ADD COLUMN IF NOT EXISTS entity_label VARCHAR(400) NULL AFTER entity_key;

ALTER TABLE audit_logs
    ADD COLUMN IF NOT EXISTS description LONGTEXT NULL AFTER operation;

ALTER TABLE audit_logs
    ADD COLUMN IF NOT EXISTS result VARCHAR(20) NOT NULL DEFAULT 'Exitoso' AFTER description;

ALTER TABLE audit_logs
    ADD COLUMN IF NOT EXISTS changes_json LONGTEXT NULL AFTER result;

ALTER TABLE audit_logs
    ADD COLUMN IF NOT EXISTS request_method VARCHAR(10) NULL AFTER changes_json;

ALTER TABLE audit_logs
    ADD COLUMN IF NOT EXISTS request_path VARCHAR(300) NULL AFTER request_method;

-- entity_id deja de ser obligatorio: hay acciones sobre objetos que no se identifican con un GUID
-- (los catálogos usan un entero) y otras que no recaen sobre un registro concreto, como un intento
-- de ingreso fallido.
ALTER TABLE audit_logs
    MODIFY COLUMN entity_id CHAR(36) NULL;

-- La operación pasa de 50 a 80 caracteres: los nombres de acción ahora son frases cortas
-- ("Actualizar oportunidades de cambio").
ALTER TABLE audit_logs
    MODIFY COLUMN operation VARCHAR(80) NOT NULL;

-- El historial se ordena por fecha y se filtra por módulo y por usuario.
CREATE INDEX IF NOT EXISTS ix_audit_logs_created_at ON audit_logs (created_at_utc);
CREATE INDEX IF NOT EXISTS ix_audit_logs_module ON audit_logs (module);
CREATE INDEX IF NOT EXISTS ix_audit_logs_user_email ON audit_logs (user_email);

-- Módulo de los registros que ya existían, deducido del tipo de objeto al que se refieren.
UPDATE audit_logs SET module = 'Gobernanza' WHERE module = '' AND entity_name = 'FichaDepartamental';
UPDATE audit_logs SET module = 'Seguridad' WHERE module = '' AND entity_name = 'UserAccount';
UPDATE audit_logs SET module = 'Sin clasificar' WHERE module = '';
