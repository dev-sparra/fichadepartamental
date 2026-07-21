ALTER TABLE audit_logs
    ADD COLUMN user_display_name VARCHAR(200) NOT NULL DEFAULT '' AFTER user_email;

ALTER TABLE approval_records
    ADD COLUMN reviewed_by_name VARCHAR(200) NULL AFTER reviewed_by_email;
