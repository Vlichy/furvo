-- Formato inicial: entidades mínimas y cabecera de partida.
-- Solo se añaden comentarios; el SQL ejecutable de esta migración publicada se conserva.
CREATE TABLE save_metadata (
    singleton INTEGER PRIMARY KEY CHECK (singleton = 1),
    save_id TEXT NOT NULL UNIQUE
);
CREATE TABLE clubs (id TEXT PRIMARY KEY NOT NULL, name TEXT NOT NULL);
CREATE TABLE players (id TEXT PRIMARY KEY NOT NULL, name TEXT NOT NULL);
CREATE TABLE contracts (
    id TEXT PRIMARY KEY NOT NULL,
    club_id TEXT NOT NULL REFERENCES clubs(id),
    player_id TEXT NOT NULL REFERENCES players(id)
);
