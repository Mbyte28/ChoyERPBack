CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE TABLE audit_logs (
        id uuid NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        user_id uuid,
        user_email character varying(320),
        action character varying(100) NOT NULL,
        entity_type character varying(100),
        entity_id character varying(100),
        ip_address character varying(45),
        user_agent character varying(500),
        details character varying(4000),
        CONSTRAINT pk_audit_logs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE TABLE clients (
        id uuid NOT NULL,
        code character varying(30) NOT NULL,
        name character varying(200) NOT NULL,
        legal_name character varying(200),
        tax_id character varying(30),
        contact_name character varying(150),
        email character varying(320),
        phone character varying(30),
        notes character varying(2000),
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_clients PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE TABLE permissions (
        id uuid NOT NULL,
        code character varying(100) NOT NULL,
        name character varying(150) NOT NULL,
        module character varying(100) NOT NULL,
        description character varying(500),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_permissions PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE TABLE roles (
        id uuid NOT NULL,
        name character varying(100) NOT NULL,
        normalized_name character varying(100) NOT NULL,
        description character varying(500),
        is_system_role boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_roles PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE TABLE workers (
        id uuid NOT NULL,
        first_name character varying(100) NOT NULL,
        last_name character varying(100) NOT NULL,
        document_type character varying(20),
        document_number character varying(30) NOT NULL,
        position character varying(100),
        department character varying(100),
        phone character varying(30),
        email character varying(320),
        hire_date date,
        is_active boolean NOT NULL,
        notes character varying(2000),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_workers PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE TABLE client_addresses (
        id uuid NOT NULL,
        client_id uuid NOT NULL,
        label character varying(100) NOT NULL,
        line1 character varying(250) NOT NULL,
        line2 character varying(250),
        city character varying(100),
        state character varying(100),
        country character varying(100),
        postal_code character varying(20),
        is_primary boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_client_addresses PRIMARY KEY (id),
        CONSTRAINT fk_client_addresses_clients_client_id FOREIGN KEY (client_id) REFERENCES clients (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE TABLE role_permissions (
        role_id uuid NOT NULL,
        permission_id uuid NOT NULL,
        CONSTRAINT pk_role_permissions PRIMARY KEY (role_id, permission_id),
        CONSTRAINT fk_role_permissions_permissions_permission_id FOREIGN KEY (permission_id) REFERENCES permissions (id) ON DELETE CASCADE,
        CONSTRAINT fk_role_permissions_roles_role_id FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        email character varying(320) NOT NULL,
        normalized_email character varying(320) NOT NULL,
        password_hash character varying(255) NOT NULL,
        is_active boolean NOT NULL,
        worker_id uuid,
        last_login_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_users PRIMARY KEY (id),
        CONSTRAINT fk_users_workers_worker_id FOREIGN KEY (worker_id) REFERENCES workers (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE TABLE refresh_tokens (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        token_hash character varying(64) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        revoked_at timestamp with time zone,
        replaced_by_token_hash character varying(64),
        created_by_ip character varying(45),
        revoked_by_ip character varying(45),
        revoked_reason character varying(100),
        CONSTRAINT pk_refresh_tokens PRIMARY KEY (id),
        CONSTRAINT fk_refresh_tokens_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE TABLE user_roles (
        user_id uuid NOT NULL,
        role_id uuid NOT NULL,
        CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id),
        CONSTRAINT fk_user_roles_roles_role_id FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE CASCADE,
        CONSTRAINT fk_user_roles_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE INDEX ix_audit_logs_action ON audit_logs (action);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE INDEX ix_audit_logs_occurred_at ON audit_logs (occurred_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE INDEX ix_audit_logs_user_id ON audit_logs (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE INDEX ix_client_addresses_client_id ON client_addresses (client_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_clients_code ON clients (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE INDEX ix_clients_is_active ON clients (is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_clients_tax_id ON clients (tax_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_permissions_code ON permissions (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE INDEX ix_refresh_tokens_expires_at ON refresh_tokens (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_refresh_tokens_token_hash ON refresh_tokens (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE INDEX ix_role_permissions_permission_id ON role_permissions (permission_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_roles_normalized_name ON roles (normalized_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE INDEX ix_user_roles_role_id ON user_roles (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_normalized_email ON users (normalized_email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_worker_id ON users (worker_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_workers_document_number ON workers (document_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    CREATE INDEX ix_workers_is_active ON workers (is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261112195457_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261112195457_InitialCreate', '10.0.15');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE TABLE project_requests (
        id uuid NOT NULL,
        code character varying(30) NOT NULL,
        client_id uuid NOT NULL,
        client_address_id uuid,
        title character varying(200) NOT NULL,
        description character varying(2000),
        project_type character varying(30) NOT NULL,
        priority character varying(20) NOT NULL,
        status character varying(20) NOT NULL,
        requested_at date NOT NULL,
        responsible_worker_id uuid,
        notes character varying(2000),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_project_requests PRIMARY KEY (id),
        CONSTRAINT fk_project_requests_client_addresses_client_address_id FOREIGN KEY (client_address_id) REFERENCES client_addresses (id) ON DELETE SET NULL,
        CONSTRAINT fk_project_requests_clients_client_id FOREIGN KEY (client_id) REFERENCES clients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_project_requests_workers_responsible_worker_id FOREIGN KEY (responsible_worker_id) REFERENCES workers (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE TABLE quotes (
        id uuid NOT NULL,
        code character varying(30) NOT NULL,
        project_request_id uuid NOT NULL,
        version integer NOT NULL,
        status character varying(20) NOT NULL,
        currency character varying(3) NOT NULL,
        tax_rate numeric(5,4) NOT NULL,
        subtotal numeric(18,2) NOT NULL,
        tax_amount numeric(18,2) NOT NULL,
        total numeric(18,2) NOT NULL,
        valid_until date,
        notes character varying(2000),
        decision_notes character varying(1000),
        sent_at timestamp with time zone,
        accepted_at timestamp with time zone,
        rejected_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_quotes PRIMARY KEY (id),
        CONSTRAINT fk_quotes_project_requests_project_request_id FOREIGN KEY (project_request_id) REFERENCES project_requests (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE TABLE contracts (
        id uuid NOT NULL,
        code character varying(30) NOT NULL,
        quote_id uuid NOT NULL,
        client_id uuid NOT NULL,
        status character varying(20) NOT NULL,
        amount numeric(18,2) NOT NULL,
        currency character varying(3) NOT NULL,
        signed_at date,
        start_date date,
        end_date date,
        notes character varying(2000),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_contracts PRIMARY KEY (id),
        CONSTRAINT fk_contracts_clients_client_id FOREIGN KEY (client_id) REFERENCES clients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_contracts_quotes_quote_id FOREIGN KEY (quote_id) REFERENCES quotes (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE TABLE quote_items (
        id uuid NOT NULL,
        quote_id uuid NOT NULL,
        description character varying(500) NOT NULL,
        unit character varying(20),
        sort_order integer NOT NULL,
        quantity numeric(18,3) NOT NULL,
        unit_price numeric(18,2) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_quote_items PRIMARY KEY (id),
        CONSTRAINT ck_quote_items_quantity CHECK (quantity > 0),
        CONSTRAINT fk_quote_items_quotes_quote_id FOREIGN KEY (quote_id) REFERENCES quotes (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE TABLE projects (
        id uuid NOT NULL,
        code character varying(30) NOT NULL,
        contract_id uuid NOT NULL,
        client_id uuid NOT NULL,
        client_address_id uuid,
        location character varying(300),
        name character varying(200) NOT NULL,
        description character varying(2000),
        status character varying(20) NOT NULL,
        progress_percent integer NOT NULL,
        start_date date,
        expected_end_date date,
        actual_end_date date,
        responsible_worker_id uuid,
        notes character varying(2000),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_projects PRIMARY KEY (id),
        CONSTRAINT ck_projects_progress CHECK (progress_percent BETWEEN 0 AND 100),
        CONSTRAINT fk_projects_client_addresses_client_address_id FOREIGN KEY (client_address_id) REFERENCES client_addresses (id) ON DELETE SET NULL,
        CONSTRAINT fk_projects_clients_client_id FOREIGN KEY (client_id) REFERENCES clients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_projects_contracts_contract_id FOREIGN KEY (contract_id) REFERENCES contracts (id) ON DELETE RESTRICT,
        CONSTRAINT fk_projects_workers_responsible_worker_id FOREIGN KEY (responsible_worker_id) REFERENCES workers (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_contracts_client_id ON contracts (client_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE UNIQUE INDEX ix_contracts_code ON contracts (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE UNIQUE INDEX ix_contracts_quote_id ON contracts (quote_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_contracts_status ON contracts (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_project_requests_client_address_id ON project_requests (client_address_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_project_requests_client_id ON project_requests (client_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE UNIQUE INDEX ix_project_requests_code ON project_requests (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_project_requests_requested_at ON project_requests (requested_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_project_requests_responsible_worker_id ON project_requests (responsible_worker_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_project_requests_status ON project_requests (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_projects_client_address_id ON projects (client_address_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_projects_client_id ON projects (client_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE UNIQUE INDEX ix_projects_code ON projects (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE UNIQUE INDEX ix_projects_contract_id ON projects (contract_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_projects_responsible_worker_id ON projects (responsible_worker_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_projects_status ON projects (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_quote_items_quote_id ON quote_items (quote_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE UNIQUE INDEX ix_quotes_code ON quotes (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE UNIQUE INDEX ix_quotes_project_request_id_version ON quotes (project_request_id, version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_quotes_status ON quotes (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    CREATE INDEX ix_quotes_valid_until ON quotes (valid_until);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261117041944_AddContractingModule') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261117041944_AddContractingModule', '10.0.15');
    END IF;
END $EF$;
COMMIT;

