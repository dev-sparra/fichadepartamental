CREATE TABLE IF NOT EXISTS import_batches (
    id CHAR(36) NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    content_type VARCHAR(150) NOT NULL,
    file_size_bytes BIGINT NOT NULL,
    status VARCHAR(50) NOT NULL,
    source_type VARCHAR(50) NOT NULL,
    started_at_utc DATETIME(6) NOT NULL,
    completed_at_utc DATETIME(6) NULL,
    valid_row_count INT NOT NULL DEFAULT 0,
    invalid_row_count INT NOT NULL DEFAULT 0,
    warning_count INT NOT NULL DEFAULT 0,
    summary_json LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS import_validation_issues (
    id CHAR(36) NOT NULL,
    import_batch_id CHAR(36) NOT NULL,
    severity VARCHAR(20) NOT NULL,
    sheet_name VARCHAR(100) NOT NULL,
    row_number INT NULL,
    cell_reference VARCHAR(20) NULL,
    error_code VARCHAR(100) NOT NULL,
    message LONGTEXT NOT NULL,
    raw_value LONGTEXT NULL,
    context_json LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    KEY ix_import_validation_issues_batch (import_batch_id),
    CONSTRAINT fk_import_validation_issues_batch FOREIGN KEY (import_batch_id) REFERENCES import_batches (id)
);
