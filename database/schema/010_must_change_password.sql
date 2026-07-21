-- 010_must_change_password.sql
-- Agrega el campo must_change_password a security_users para forzar el cambio de
-- contraseña en el primer inicio de sesión de usuarios creados por el administrador.

ALTER TABLE security_users
    ADD COLUMN must_change_password TINYINT(1) NOT NULL DEFAULT 0 AFTER is_active;

-- Marcar el usuario administrador sembrado para que cambie su contraseña temporal.
UPDATE security_users
SET must_change_password = 1
WHERE normalized_email = 'PLANDEMUSICA@MINCULTURA.GOV.CO' AND must_change_password = 0;