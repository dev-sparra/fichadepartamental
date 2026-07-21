CREATE TABLE IF NOT EXISTS security_roles (
    id CHAR(36) NOT NULL,
    name VARCHAR(100) NOT NULL,
    normalized_name VARCHAR(100) NOT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_security_roles_normalized_name (normalized_name)
);

CREATE TABLE IF NOT EXISTS security_users (
    id CHAR(36) NOT NULL,
    email VARCHAR(200) NOT NULL,
    normalized_email VARCHAR(200) NOT NULL,
    display_name VARCHAR(200) NULL,
    password_hash VARCHAR(500) NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_security_users_normalized_email (normalized_email)
);

CREATE TABLE IF NOT EXISTS security_user_roles (
    user_account_id CHAR(36) NOT NULL,
    role_id CHAR(36) NOT NULL,
    PRIMARY KEY (user_account_id, role_id),
    CONSTRAINT fk_security_user_roles_user FOREIGN KEY (user_account_id) REFERENCES security_users (id),
    CONSTRAINT fk_security_user_roles_role FOREIGN KEY (role_id) REFERENCES security_roles (id)
);

CREATE TABLE IF NOT EXISTS catalog_departments (
    id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_departments_name (name)
);

CREATE TABLE IF NOT EXISTS catalog_municipalities (
    id INT NOT NULL,
    department_id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_municipalities_department_name (department_id, name),
    CONSTRAINT fk_catalog_municipalities_department FOREIGN KEY (department_id) REFERENCES catalog_departments (id)
);

CREATE TABLE IF NOT EXISTS catalog_region_ocad (
    id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_region_ocad_name (name)
);

CREATE TABLE IF NOT EXISTS catalog_committee_statuses (
    id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_committee_statuses_name (name)
);

CREATE TABLE IF NOT EXISTS catalog_plan_statuses (
    id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_plan_statuses_name (name)
);

CREATE TABLE IF NOT EXISTS catalog_priority_levels (
    id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_priority_levels_name (name)
);

CREATE TABLE IF NOT EXISTS catalog_pnmc_axes (
    id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_pnmc_axes_name (name)
);

CREATE TABLE IF NOT EXISTS catalog_pnmc_components (
    id INT NOT NULL,
    pnmc_axis_id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_pnmc_components_axis_name (pnmc_axis_id, name),
    CONSTRAINT fk_catalog_pnmc_components_axis FOREIGN KEY (pnmc_axis_id) REFERENCES catalog_pnmc_axes (id)
);

CREATE TABLE IF NOT EXISTS catalog_approach_options (
    id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_approach_options_name (name)
);

CREATE TABLE IF NOT EXISTS catalog_schedule_options (
    id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_schedule_options_name (name)
);

CREATE TABLE IF NOT EXISTS catalog_proposal_statuses (
    id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_proposal_statuses_name (name)
);

CREATE TABLE IF NOT EXISTS catalog_agent_types (
    id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_agent_types_name (name)
);

CREATE TABLE IF NOT EXISTS catalog_ecosystem_roles (
    id INT NOT NULL,
    agent_type_id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_ecosystem_roles_agent_type_name (agent_type_id, name),
    CONSTRAINT fk_catalog_ecosystem_roles_agent_type FOREIGN KEY (agent_type_id) REFERENCES catalog_agent_types (id)
);

CREATE TABLE IF NOT EXISTS catalog_territorial_levels (
    id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_territorial_levels_name (name)
);

CREATE TABLE IF NOT EXISTS catalog_information_sources (
    id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_information_sources_name (name)
);

CREATE TABLE IF NOT EXISTS catalog_months (
    id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_months_name (name)
);

CREATE TABLE IF NOT EXISTS catalog_years (
    id INT NOT NULL,
    value INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_years_value (value)
);

CREATE TABLE IF NOT EXISTS catalog_indicator_definitions (
    id INT NOT NULL,
    action_name LONGTEXT NOT NULL,
    indicator_name LONGTEXT NOT NULL,
    target_value DECIMAL(18,4) NOT NULL,
    display_order INT NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_indicator_definitions_display_order (display_order)
);

CREATE TABLE IF NOT EXISTS catalog_indicator_detail_templates (
    id INT NOT NULL,
    indicator_definition_id INT NOT NULL,
    sort_order INT NOT NULL,
    formula_label LONGTEXT NOT NULL,
    detail_description LONGTEXT NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_catalog_indicator_detail_templates_definition_order (indicator_definition_id, sort_order),
    CONSTRAINT fk_catalog_indicator_detail_templates_definition FOREIGN KEY (indicator_definition_id) REFERENCES catalog_indicator_definitions (id)
);

CREATE TABLE IF NOT EXISTS fichas_departamentales (
    id CHAR(36) NOT NULL,
    fecha_levantamiento DATE NOT NULL,
    department_id INT NOT NULL,
    municipality_id INT NULL,
    responsable_registro VARCHAR(200) NOT NULL,
    region_ocad_option_id INT NULL,
    observaciones LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_fichas_departamentales_department_date (department_id, fecha_levantamiento),
    CONSTRAINT fk_fichas_departamentales_department FOREIGN KEY (department_id) REFERENCES catalog_departments (id),
    CONSTRAINT fk_fichas_departamentales_municipality FOREIGN KEY (municipality_id) REFERENCES catalog_municipalities (id),
    CONSTRAINT fk_fichas_departamentales_region_ocad FOREIGN KEY (region_ocad_option_id) REFERENCES catalog_region_ocad (id)
);

CREATE TABLE IF NOT EXISTS ficha_fuentes_informacion (
    ficha_departamental_id CHAR(36) NOT NULL,
    information_source_option_id INT NOT NULL,
    PRIMARY KEY (ficha_departamental_id, information_source_option_id),
    CONSTRAINT fk_ficha_fuentes_informacion_ficha FOREIGN KEY (ficha_departamental_id) REFERENCES fichas_departamentales (id),
    CONSTRAINT fk_ficha_fuentes_informacion_source FOREIGN KEY (information_source_option_id) REFERENCES catalog_information_sources (id)
);

CREATE TABLE IF NOT EXISTS diagnosticos_ecosistema (
    id CHAR(36) NOT NULL,
    ficha_departamental_id CHAR(36) NOT NULL,
    caracterizacion_general LONGTEXT NULL,
    fortalezas_identificadas LONGTEXT NULL,
    politicas_priorizadas LONGTEXT NULL,
    debilidades_identificadas LONGTEXT NULL,
    tensiones_o_conflictos LONGTEXT NULL,
    committee_status_option_id INT NULL,
    plan_departamental_cultura_status_id INT NULL,
    consejo_departamental_cultura VARCHAR(100) NULL,
    plan_departamental_musica_status_id INT NULL,
    ordenanzas_culturales VARCHAR(100) NULL,
    consejo_departamental_musica LONGTEXT NULL,
    mesa_sectorial_territorial LONGTEXT NULL,
    observaciones LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_diagnosticos_ecosistema_ficha (ficha_departamental_id),
    CONSTRAINT fk_diagnosticos_ecosistema_ficha FOREIGN KEY (ficha_departamental_id) REFERENCES fichas_departamentales (id),
    CONSTRAINT fk_diagnosticos_ecosistema_committee_status FOREIGN KEY (committee_status_option_id) REFERENCES catalog_committee_statuses (id),
    CONSTRAINT fk_diagnosticos_ecosistema_plan_cultura_status FOREIGN KEY (plan_departamental_cultura_status_id) REFERENCES catalog_plan_statuses (id),
    CONSTRAINT fk_diagnosticos_ecosistema_plan_musica_status FOREIGN KEY (plan_departamental_musica_status_id) REFERENCES catalog_plan_statuses (id)
);

CREATE TABLE IF NOT EXISTS oportunidades_cambio (
    id CHAR(36) NOT NULL,
    ficha_departamental_id CHAR(36) NOT NULL,
    situacion_identificada LONGTEXT NULL,
    componente_otras_dependencias_entidades LONGTEXT NULL,
    aliados_y_creyentes LONGTEXT NULL,
    territorio_influencia LONGTEXT NULL,
    priority_level_option_id INT NULL,
    descripcion_adicional LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    CONSTRAINT fk_oportunidades_cambio_ficha FOREIGN KEY (ficha_departamental_id) REFERENCES fichas_departamentales (id),
    CONSTRAINT fk_oportunidades_cambio_priority FOREIGN KEY (priority_level_option_id) REFERENCES catalog_priority_levels (id)
);

CREATE TABLE IF NOT EXISTS ejes_pnmc_registros (
    id CHAR(36) NOT NULL,
    ficha_departamental_id CHAR(36) NOT NULL,
    descripcion_hallazgo LONGTEXT NULL,
    pnmc_axis_id INT NULL,
    pnmc_component_id INT NULL,
    accion_estrategica LONGTEXT NULL,
    politica_priorizada LONGTEXT NULL,
    armonizacion_pnc LONGTEXT NULL,
    armonizacion_pnd LONGTEXT NULL,
    armonizacion_internacional LONGTEXT NULL,
    priority_level_option_id INT NULL,
    aliados_responsables LONGTEXT NULL,
    fuentes_financiacion LONGTEXT NULL,
    valor_propuesta_cop DECIMAL(18,2) NULL,
    descripcion LONGTEXT NULL,
    schedule_option_id INT NULL,
    proposal_status_option_id INT NULL,
    observaciones LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    CONSTRAINT fk_ejes_pnmc_registros_ficha FOREIGN KEY (ficha_departamental_id) REFERENCES fichas_departamentales (id),
    CONSTRAINT fk_ejes_pnmc_registros_axis FOREIGN KEY (pnmc_axis_id) REFERENCES catalog_pnmc_axes (id),
    CONSTRAINT fk_ejes_pnmc_registros_component FOREIGN KEY (pnmc_component_id) REFERENCES catalog_pnmc_components (id),
    CONSTRAINT fk_ejes_pnmc_registros_priority FOREIGN KEY (priority_level_option_id) REFERENCES catalog_priority_levels (id),
    CONSTRAINT fk_ejes_pnmc_registros_schedule FOREIGN KEY (schedule_option_id) REFERENCES catalog_schedule_options (id),
    CONSTRAINT fk_ejes_pnmc_registros_status FOREIGN KEY (proposal_status_option_id) REFERENCES catalog_proposal_statuses (id)
);

CREATE TABLE IF NOT EXISTS ejes_pnmc_registro_enfoques (
    eje_pnmc_registro_id CHAR(36) NOT NULL,
    approach_option_id INT NOT NULL,
    PRIMARY KEY (eje_pnmc_registro_id, approach_option_id),
    CONSTRAINT fk_ejes_pnmc_registro_enfoques_registro FOREIGN KEY (eje_pnmc_registro_id) REFERENCES ejes_pnmc_registros (id),
    CONSTRAINT fk_ejes_pnmc_registro_enfoques_approach FOREIGN KEY (approach_option_id) REFERENCES catalog_approach_options (id)
);

CREATE TABLE IF NOT EXISTS actores (
    id CHAR(36) NOT NULL,
    ficha_departamental_id CHAR(36) NOT NULL,
    nombre_agente LONGTEXT NOT NULL,
    agent_type_id INT NULL,
    numero_contacto VARCHAR(50) NULL,
    correo_electronico VARCHAR(200) NULL,
    observaciones LONGTEXT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NULL,
    PRIMARY KEY (id),
    CONSTRAINT fk_actores_ficha FOREIGN KEY (ficha_departamental_id) REFERENCES fichas_departamentales (id),
    CONSTRAINT fk_actores_agent_type FOREIGN KEY (agent_type_id) REFERENCES catalog_agent_types (id)
);

CREATE TABLE IF NOT EXISTS actor_roles_ecosistema (
    actor_id CHAR(36) NOT NULL,
    ecosystem_role_id INT NOT NULL,
    PRIMARY KEY (actor_id, ecosystem_role_id),
    CONSTRAINT fk_actor_roles_ecosistema_actor FOREIGN KEY (actor_id) REFERENCES actores (id),
    CONSTRAINT fk_actor_roles_ecosistema_role FOREIGN KEY (ecosystem_role_id) REFERENCES catalog_ecosystem_roles (id)
);

CREATE TABLE IF NOT EXISTS actor_niveles_territoriales (
    actor_id CHAR(36) NOT NULL,
    territorial_level_option_id INT NOT NULL,
    PRIMARY KEY (actor_id, territorial_level_option_id),
    CONSTRAINT fk_actor_niveles_territoriales_actor FOREIGN KEY (actor_id) REFERENCES actores (id),
    CONSTRAINT fk_actor_niveles_territoriales_level FOREIGN KEY (territorial_level_option_id) REFERENCES catalog_territorial_levels (id)
);
