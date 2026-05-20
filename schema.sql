-- ============================================================
-- Restaurant Management System — SQLite Schema
-- Generated from Java JPA models (models.rar)
-- ============================================================

PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

-- ────────────────────────────────────────────
-- Lookup / reference tables
-- ────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS roles (
    id          TEXT PRIMARY KEY,
    token       TEXT NOT NULL UNIQUE,
    name        TEXT NOT NULL UNIQUE,
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ban_statuses (
    id          TEXT PRIMARY KEY,
    token       TEXT NOT NULL UNIQUE,
    name        TEXT NOT NULL UNIQUE,
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS dishes_categories (
    id          TEXT PRIMARY KEY,
    token       TEXT NOT NULL UNIQUE,
    name        TEXT NOT NULL UNIQUE,
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS guest_report_statuses (
    id          TEXT PRIMARY KEY,
    token       TEXT NOT NULL UNIQUE,
    name        TEXT NOT NULL UNIQUE,
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS order_items_statuses (
    id          TEXT PRIMARY KEY,
    token       TEXT NOT NULL UNIQUE,
    name        TEXT NOT NULL UNIQUE,
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS order_statuses (
    id          TEXT PRIMARY KEY,
    token       TEXT NOT NULL UNIQUE,
    name        TEXT NOT NULL UNIQUE,
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS reservation_statuses (
    id          TEXT PRIMARY KEY,
    token       TEXT NOT NULL UNIQUE,
    name        TEXT NOT NULL UNIQUE,
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS table_statuses (
    id          TEXT PRIMARY KEY,
    token       TEXT NOT NULL UNIQUE,
    name        TEXT NOT NULL UNIQUE,
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS allergens (
    id          TEXT PRIMARY KEY,
    token       TEXT NOT NULL UNIQUE,
    name        TEXT NOT NULL UNIQUE,
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL
);

-- ────────────────────────────────────────────
-- Core entities
-- ────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS users (
    id            TEXT PRIMARY KEY,
    token         TEXT NOT NULL UNIQUE,
    username      TEXT NOT NULL UNIQUE,
    email         TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    is_enabled    INTEGER NOT NULL DEFAULT 0,   -- 0=false, 1=true
    banned_until  TEXT,
    created_at    TEXT NOT NULL,
    updated_at    TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS users_roles (
    user_id  TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id  TEXT NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE IF NOT EXISTS verification_tokens (
    id         TEXT PRIMARY KEY,
    token      TEXT NOT NULL UNIQUE,
    token_type TEXT NOT NULL,               -- 'EmailVerification' | 'PasswordReset'
    expires_at TEXT NOT NULL,
    user_id    TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS bans (
    id           TEXT PRIMARY KEY,
    token        TEXT NOT NULL UNIQUE,
    reason       TEXT NOT NULL,
    expires_at   TEXT,
    is_permanent INTEGER,                   -- 0/1/NULL
    user_id      TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at   TEXT NOT NULL,
    updated_at   TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS bans_statuses (
    ban_id    TEXT NOT NULL REFERENCES bans(id) ON DELETE CASCADE,
    status_id TEXT NOT NULL REFERENCES ban_statuses(id) ON DELETE CASCADE,
    PRIMARY KEY (ban_id, status_id)
);

CREATE TABLE IF NOT EXISTS restaurant_tables (
    id           TEXT PRIMARY KEY,
    token        TEXT NOT NULL UNIQUE,
    table_number INTEGER NOT NULL,
    capacity     INTEGER NOT NULL DEFAULT 4,
    created_at   TEXT NOT NULL,
    updated_at   TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS tables_statuses (
    table_id  TEXT NOT NULL REFERENCES restaurant_tables(id) ON DELETE CASCADE,
    status_id TEXT NOT NULL REFERENCES table_statuses(id) ON DELETE CASCADE,
    PRIMARY KEY (table_id, status_id)
);

CREATE TABLE IF NOT EXISTS reservations (
    id              TEXT PRIMARY KEY,
    token           TEXT NOT NULL UNIQUE,
    reserved_from   TEXT NOT NULL,
    reserved_until  TEXT,
    table_id        TEXT NOT NULL REFERENCES restaurant_tables(id) ON DELETE RESTRICT,
    user_id         TEXT NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at      TEXT NOT NULL,
    updated_at      TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS reservations_statuses (
    reservation_id TEXT NOT NULL REFERENCES reservations(id) ON DELETE CASCADE,
    status_id      TEXT NOT NULL REFERENCES reservation_statuses(id) ON DELETE CASCADE,
    PRIMARY KEY (reservation_id, status_id)
);

CREATE TABLE IF NOT EXISTS orders (
    id         TEXT PRIMARY KEY,
    token      TEXT NOT NULL UNIQUE,
    table_id   TEXT NOT NULL REFERENCES restaurant_tables(id) ON DELETE RESTRICT,
    user_id    TEXT NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS orders_statuses (
    order_id  TEXT NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    status_id TEXT NOT NULL REFERENCES order_statuses(id) ON DELETE CASCADE,
    PRIMARY KEY (order_id, status_id)
);

CREATE TABLE IF NOT EXISTS ingredients (
    id         TEXT PRIMARY KEY,
    token      TEXT NOT NULL UNIQUE,
    name       TEXT NOT NULL UNIQUE,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ingredients_allergens (
    ingredient_id TEXT NOT NULL REFERENCES ingredients(id) ON DELETE CASCADE,
    allergen_id   TEXT NOT NULL REFERENCES allergens(id) ON DELETE CASCADE,
    PRIMARY KEY (ingredient_id, allergen_id)
);

CREATE TABLE IF NOT EXISTS dishes (
    id            TEXT PRIMARY KEY,
    token         TEXT NOT NULL UNIQUE,
    name          TEXT NOT NULL,
    description   TEXT,
    price         TEXT NOT NULL,            -- stored as TEXT for precision
    available_from TEXT,
    category_id   TEXT NOT NULL REFERENCES dishes_categories(id) ON DELETE RESTRICT,
    created_at    TEXT NOT NULL,
    updated_at    TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS dishes_ingredients (
    dish_id       TEXT NOT NULL REFERENCES dishes(id) ON DELETE CASCADE,
    ingredient_id TEXT NOT NULL REFERENCES ingredients(id) ON DELETE CASCADE,
    PRIMARY KEY (dish_id, ingredient_id)
);

CREATE TABLE IF NOT EXISTS order_items (
    id         TEXT PRIMARY KEY,
    token      TEXT NOT NULL UNIQUE,
    quantity   INTEGER NOT NULL DEFAULT 1,
    note       TEXT,
    order_id   TEXT NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    dish_id    TEXT NOT NULL REFERENCES dishes(id) ON DELETE RESTRICT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS order_items_statuses_join (
    order_item_id TEXT NOT NULL REFERENCES order_items(id) ON DELETE CASCADE,
    status_id     TEXT NOT NULL REFERENCES order_items_statuses(id) ON DELETE CASCADE,
    PRIMARY KEY (order_item_id, status_id)
);

CREATE TABLE IF NOT EXISTS guest_reports (
    id               TEXT PRIMARY KEY,
    token            TEXT NOT NULL UNIQUE,
    description      TEXT NOT NULL,
    reporter_id      TEXT NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    reported_user_id TEXT NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at       TEXT NOT NULL,
    updated_at       TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS guest_reports_statuses (
    report_id TEXT NOT NULL REFERENCES guest_reports(id) ON DELETE CASCADE,
    status_id TEXT NOT NULL REFERENCES guest_report_statuses(id) ON DELETE CASCADE,
    PRIMARY KEY (report_id, status_id)
);

CREATE TABLE IF NOT EXISTS audit_logs (
    id         TEXT PRIMARY KEY,
    token      TEXT NOT NULL UNIQUE,
    action     TEXT NOT NULL,
    details    TEXT,                        -- JSON blob
    user_id    TEXT NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

-- ────────────────────────────────────────────
-- Indexes
-- ────────────────────────────────────────────

CREATE INDEX IF NOT EXISTS idx_users_email      ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_username   ON users(username);
CREATE INDEX IF NOT EXISTS idx_vt_token         ON verification_tokens(token);
CREATE INDEX IF NOT EXISTS idx_vt_user          ON verification_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_bans_user        ON bans(user_id);
CREATE INDEX IF NOT EXISTS idx_orders_table     ON orders(table_id);
CREATE INDEX IF NOT EXISTS idx_orders_user      ON orders(user_id);
CREATE INDEX IF NOT EXISTS idx_oi_order         ON order_items(order_id);
CREATE INDEX IF NOT EXISTS idx_res_table        ON reservations(table_id);
CREATE INDEX IF NOT EXISTS idx_res_user         ON reservations(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_user       ON audit_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_gr_reporter      ON guest_reports(reporter_id);
CREATE INDEX IF NOT EXISTS idx_gr_reported      ON guest_reports(reported_user_id);
