-- 011_notifications.sql
-- Bandeja de notificaciones del portal. Se usa para avisar al Gestor Departamental cuando el
-- Líder de Gobernanza cambia el estado de su ficha (aprobada o devuelta para ajustes).
-- Cada aviso pertenece a un destinatario (correo) y solo esa persona lo consulta o lo marca leído.

CREATE TABLE IF NOT EXISTS user_notifications (
    id CHAR(36) NOT NULL,
    recipient_email VARCHAR(200) NOT NULL,
    recipient_normalized_email VARCHAR(200) NOT NULL,
    category VARCHAR(50) NOT NULL,
    event_code VARCHAR(60) NOT NULL,
    title VARCHAR(200) NOT NULL,
    message LONGTEXT NOT NULL,
    tone VARCHAR(20) NOT NULL,
    action_route VARCHAR(300) NULL,
    related_entity_name VARCHAR(100) NULL,
    related_entity_id CHAR(36) NULL,
    triggered_by_email VARCHAR(200) NULL,
    triggered_by_name VARCHAR(200) NULL,
    is_read TINYINT(1) NOT NULL DEFAULT 0,
    read_at_utc DATETIME(6) NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    KEY ix_user_notifications_recipient (recipient_normalized_email, is_read),
    KEY ix_user_notifications_created (created_at_utc)
);
