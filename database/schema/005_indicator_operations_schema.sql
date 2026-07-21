CREATE TABLE IF NOT EXISTS indicator_records (
    id CHAR(36) NOT NULL,
    department_id INT NOT NULL,
    indicator_definition_id INT NOT NULL,
    year INT NOT NULL,
    source LONGTEXT NULL,
    general_observations LONGTEXT NULL,
    current_value_calculated DECIMAL(18,4) NOT NULL DEFAULT 0,
    compliance_percentage_calculated DECIMAL(18,6) NOT NULL DEFAULT 0,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_indicator_records_department_definition_year (department_id, indicator_definition_id, year),
    CONSTRAINT fk_indicator_records_department FOREIGN KEY (department_id) REFERENCES catalog_departments (id),
    CONSTRAINT fk_indicator_records_definition FOREIGN KEY (indicator_definition_id) REFERENCES catalog_indicator_definitions (id)
);

CREATE TABLE IF NOT EXISTS indicator_monthly_progresses (
    id CHAR(36) NOT NULL,
    indicator_record_id CHAR(36) NOT NULL,
    month_option_id INT NOT NULL,
    quantitative_advance DECIMAL(18,4) NULL,
    detail LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_indicator_monthly_progresses_record_month (indicator_record_id, month_option_id),
    CONSTRAINT fk_indicator_monthly_progresses_record FOREIGN KEY (indicator_record_id) REFERENCES indicator_records (id),
    CONSTRAINT fk_indicator_monthly_progresses_month FOREIGN KEY (month_option_id) REFERENCES catalog_months (id)
);

CREATE TABLE IF NOT EXISTS indicator_detail_records (
    id CHAR(36) NOT NULL,
    department_id INT NOT NULL,
    indicator_definition_id INT NOT NULL,
    indicator_detail_template_id INT NOT NULL,
    year INT NOT NULL,
    source LONGTEXT NULL,
    observations LONGTEXT NULL,
    current_value_calculated DECIMAL(18,4) NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_indicator_detail_records_department_template_year (department_id, indicator_definition_id, indicator_detail_template_id, year),
    CONSTRAINT fk_indicator_detail_records_department FOREIGN KEY (department_id) REFERENCES catalog_departments (id),
    CONSTRAINT fk_indicator_detail_records_definition FOREIGN KEY (indicator_definition_id) REFERENCES catalog_indicator_definitions (id),
    CONSTRAINT fk_indicator_detail_records_template FOREIGN KEY (indicator_detail_template_id) REFERENCES catalog_indicator_detail_templates (id)
);

CREATE TABLE IF NOT EXISTS indicator_detail_record_months (
    id CHAR(36) NOT NULL,
    indicator_detail_record_id CHAR(36) NOT NULL,
    month_option_id INT NOT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_indicator_detail_record_months_record_month (indicator_detail_record_id, month_option_id),
    CONSTRAINT fk_indicator_detail_record_months_record FOREIGN KEY (indicator_detail_record_id) REFERENCES indicator_detail_records (id),
    CONSTRAINT fk_indicator_detail_record_months_month FOREIGN KEY (month_option_id) REFERENCES catalog_months (id)
);
