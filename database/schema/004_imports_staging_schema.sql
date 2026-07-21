ALTER TABLE import_batches
    ADD COLUMN IF NOT EXISTS persisted_record_count INT NOT NULL DEFAULT 0 AFTER warning_count;

CREATE TABLE IF NOT EXISTS import_identification_staging_rows (
    id CHAR(36) NOT NULL,
    import_batch_id CHAR(36) NOT NULL,
    source_row_number INT NOT NULL,
    fecha_levantamiento DATE NULL,
    department_name VARCHAR(255) NULL,
    municipality_name VARCHAR(255) NULL,
    responsable_registro VARCHAR(200) NULL,
    region_ocad_name VARCHAR(255) NULL,
    information_sources_raw LONGTEXT NULL,
    observaciones LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    KEY ix_import_identification_staging_rows_batch (import_batch_id),
    CONSTRAINT fk_import_identification_staging_rows_batch FOREIGN KEY (import_batch_id) REFERENCES import_batches (id)
);

CREATE TABLE IF NOT EXISTS import_diagnostic_staging_rows (
    id CHAR(36) NOT NULL,
    import_batch_id CHAR(36) NOT NULL,
    source_row_number INT NOT NULL,
    caracterizacion_general LONGTEXT NULL,
    fortalezas_identificadas LONGTEXT NULL,
    politicas_priorizadas LONGTEXT NULL,
    debilidades_identificadas LONGTEXT NULL,
    tensiones_o_conflictos LONGTEXT NULL,
    committee_status_name VARCHAR(255) NULL,
    plan_departamental_cultura_status_name VARCHAR(255) NULL,
    consejo_departamental_cultura VARCHAR(100) NULL,
    plan_departamental_musica_status_name VARCHAR(255) NULL,
    ordenanzas_culturales VARCHAR(100) NULL,
    consejo_departamental_musica LONGTEXT NULL,
    mesa_sectorial_territorial LONGTEXT NULL,
    observaciones LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    KEY ix_import_diagnostic_staging_rows_batch (import_batch_id),
    CONSTRAINT fk_import_diagnostic_staging_rows_batch FOREIGN KEY (import_batch_id) REFERENCES import_batches (id)
);

CREATE TABLE IF NOT EXISTS import_opportunity_staging_rows (
    id CHAR(36) NOT NULL,
    import_batch_id CHAR(36) NOT NULL,
    source_row_number INT NOT NULL,
    situacion_identificada LONGTEXT NULL,
    componente_otras_dependencias_entidades LONGTEXT NULL,
    aliados_y_creyentes LONGTEXT NULL,
    territorio_influencia LONGTEXT NULL,
    priority_level_name VARCHAR(255) NULL,
    descripcion_adicional LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    KEY ix_import_opportunity_staging_rows_batch (import_batch_id),
    CONSTRAINT fk_import_opportunity_staging_rows_batch FOREIGN KEY (import_batch_id) REFERENCES import_batches (id)
);

CREATE TABLE IF NOT EXISTS import_pnmc_axis_staging_rows (
    id CHAR(36) NOT NULL,
    import_batch_id CHAR(36) NOT NULL,
    source_row_number INT NOT NULL,
    descripcion_hallazgo LONGTEXT NULL,
    pnmc_axis_name VARCHAR(255) NULL,
    pnmc_component_name VARCHAR(255) NULL,
    accion_estrategica LONGTEXT NULL,
    politica_priorizada LONGTEXT NULL,
    armonizacion_pnc LONGTEXT NULL,
    armonizacion_pnd LONGTEXT NULL,
    armonizacion_internacional LONGTEXT NULL,
    priority_level_name VARCHAR(255) NULL,
    aliados_responsables LONGTEXT NULL,
    fuentes_financiacion LONGTEXT NULL,
    valor_propuesta_cop DECIMAL(18,2) NULL,
    enfoques_raw LONGTEXT NULL,
    descripcion LONGTEXT NULL,
    schedule_name VARCHAR(255) NULL,
    proposal_status_name VARCHAR(255) NULL,
    observaciones LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    KEY ix_import_pnmc_axis_staging_rows_batch (import_batch_id),
    CONSTRAINT fk_import_pnmc_axis_staging_rows_batch FOREIGN KEY (import_batch_id) REFERENCES import_batches (id)
);

CREATE TABLE IF NOT EXISTS import_actor_staging_rows (
    id CHAR(36) NOT NULL,
    import_batch_id CHAR(36) NOT NULL,
    source_row_number INT NOT NULL,
    nombre_agente LONGTEXT NULL,
    agent_type_name VARCHAR(255) NULL,
    ecosystem_roles_raw LONGTEXT NULL,
    territorial_levels_raw LONGTEXT NULL,
    numero_contacto VARCHAR(50) NULL,
    correo_electronico VARCHAR(200) NULL,
    observaciones LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    KEY ix_import_actor_staging_rows_batch (import_batch_id),
    CONSTRAINT fk_import_actor_staging_rows_batch FOREIGN KEY (import_batch_id) REFERENCES import_batches (id)
);

CREATE TABLE IF NOT EXISTS import_indicator_staging_rows (
    id CHAR(36) NOT NULL,
    import_batch_id CHAR(36) NOT NULL,
    source_row_number INT NOT NULL,
    departments_raw LONGTEXT NULL,
    action_name LONGTEXT NULL,
    indicator_name LONGTEXT NULL,
    target_raw VARCHAR(50) NULL,
    monthly_quantitative_json LONGTEXT NULL,
    monthly_detail_json LONGTEXT NULL,
    fuente LONGTEXT NULL,
    year_raw VARCHAR(50) NULL,
    observaciones_generales LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    KEY ix_import_indicator_staging_rows_batch (import_batch_id),
    CONSTRAINT fk_import_indicator_staging_rows_batch FOREIGN KEY (import_batch_id) REFERENCES import_batches (id)
);

CREATE TABLE IF NOT EXISTS import_indicator_detail_staging_rows (
    id CHAR(36) NOT NULL,
    import_batch_id CHAR(36) NOT NULL,
    source_row_number INT NOT NULL,
    departments_raw LONGTEXT NULL,
    action_name LONGTEXT NULL,
    indicator_name LONGTEXT NULL,
    target_raw VARCHAR(50) NULL,
    formula_label LONGTEXT NULL,
    detail_description LONGTEXT NULL,
    months_raw LONGTEXT NULL,
    current_value_raw VARCHAR(50) NULL,
    fuente LONGTEXT NULL,
    year_raw VARCHAR(50) NULL,
    observaciones LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    KEY ix_import_indicator_detail_staging_rows_batch (import_batch_id),
    CONSTRAINT fk_import_indicator_detail_staging_rows_batch FOREIGN KEY (import_batch_id) REFERENCES import_batches (id)
);
