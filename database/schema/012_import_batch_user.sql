-- 012_import_batch_user.sql
-- Registra quién cargó cada archivo en /imports. El historial de importaciones pasa a ser
-- personal: el Gestor Departamental ve solo sus cargas, mientras que el Líder de Gobernanza y el
-- Administrador siguen viendo las de todo el equipo.
--
-- Los lotes que ya existen quedan con el correo en NULL (cargas anteriores a este cambio): solo
-- son visibles para el Líder de Gobernanza y el Administrador.

ALTER TABLE import_batches
    ADD COLUMN IF NOT EXISTS created_by_email VARCHAR(256) NULL AFTER summary_json;

CREATE INDEX IF NOT EXISTS ix_import_batches_created_by
    ON import_batches (created_by_email);
