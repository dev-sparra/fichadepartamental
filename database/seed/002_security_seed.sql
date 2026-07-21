INSERT INTO security_roles (id, name, normalized_name, created_at_utc, updated_at_utc)
VALUES
    ('11111111-1111-1111-1111-111111111111', 'Administrador', 'ADMINISTRADOR', UTC_TIMESTAMP(6), NULL),
    ('22222222-2222-2222-2222-222222222222', 'Líder de Gobernanza', 'LIDER DE GOBERNANZA', UTC_TIMESTAMP(6), NULL),
    ('33333333-3333-3333-3333-333333333333', 'Gestor Departamental', 'GESTOR DEPARTAMENTAL', UTC_TIMESTAMP(6), NULL)
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    normalized_name = VALUES(normalized_name),
    updated_at_utc = NULL;

INSERT INTO security_users (id, email, normalized_email, display_name, password_hash, is_active, created_at_utc, updated_at_utc)
VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'plandemusica@mincultura.gov.co', 'PLANDEMUSICA@MINCULTURA.GOV.CO', 'Administrador Portal Gobernanza Musical', NULL, 1, UTC_TIMESTAMP(6), NULL)
ON DUPLICATE KEY UPDATE
    email = VALUES(email),
    normalized_email = VALUES(normalized_email),
    display_name = VALUES(display_name),
    is_active = VALUES(is_active),
    updated_at_utc = NULL;

INSERT INTO security_user_roles (user_account_id, role_id)
VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111')
ON DUPLICATE KEY UPDATE
    role_id = VALUES(role_id);
