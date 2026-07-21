-- 009_ficha_created_by.sql
-- Agrega el campo created_by_email a la tabla fichas_departamentales para rastrear
-- qué usuario (gestor) creó o importó la ficha. Esto permite filtrar las fichas
-- por usuario para el rol Gestor Departamental.

ALTER TABLE fichas_departamentales
    ADD COLUMN created_by_email VARCHAR(200) NULL AFTER observaciones;
