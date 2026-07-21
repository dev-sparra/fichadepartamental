CREATE TABLE IF NOT EXISTS approval_records (
    id CHAR(36) NOT NULL,
    ficha_departamental_id CHAR(36) NOT NULL,
    status VARCHAR(50) NOT NULL,
    reviewed_by_email VARCHAR(200) NULL,
    reviewed_at_utc DATETIME(6) NULL,
    comment LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    KEY ix_approval_records_ficha (ficha_departamental_id),
    CONSTRAINT fk_approval_records_ficha FOREIGN KEY (ficha_departamental_id) REFERENCES fichas_departamentales (id)
);

CREATE TABLE IF NOT EXISTS audit_logs (
    id CHAR(36) NOT NULL,
    user_email VARCHAR(200) NOT NULL,
    entity_name VARCHAR(100) NOT NULL,
    entity_id CHAR(36) NOT NULL,
    operation VARCHAR(50) NOT NULL,
    old_values_json LONGTEXT NULL,
    new_values_json LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    KEY ix_audit_logs_entity (entity_name, entity_id)
);
