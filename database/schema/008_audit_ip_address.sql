-- 008_audit_ip_address.sql
-- Fase 7 (Workflow + auditoría de cambios): agrega la dirección IP del cliente al registro de
-- auditoría, completando el requisito: usuario, fecha/hora, IP, operación, valor anterior y nuevo.

ALTER TABLE audit_logs
    ADD COLUMN ip_address VARCHAR(64) NULL AFTER user_display_name;
