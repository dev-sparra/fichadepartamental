-- Usuario administrador objetivo:
-- plandemusica@mincultura.gov.co
-- Contrasena temporal generada:
-- MusiGov!2026#LunaR3d

UPDATE security_users
SET
    password_hash = 'AQAAAAIAAYagAAAAEBCHpduAuNRb/jEUYTDQtM7irOwTt5V8ULWZY5u4cubkQOAhs6E8MW695hO2/1TbAw==',
    must_change_password = 1,
    updated_at_utc = UTC_TIMESTAMP(6)
WHERE normalized_email = 'PLANDEMUSICA@MINCULTURA.GOV.CO';
