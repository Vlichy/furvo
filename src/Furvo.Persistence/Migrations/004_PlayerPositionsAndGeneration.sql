-- Se añaden datos opcionales: las partidas anteriores no reciben posiciones ni puntuaciones inventadas.
CREATE TABLE player_positions (
    player_id TEXT NOT NULL REFERENCES players(id),
    role INTEGER NOT NULL CHECK (typeof(role) = 'integer' AND role BETWEEN 1 AND 13),
    is_primary INTEGER NOT NULL CHECK (typeof(is_primary) = 'integer' AND is_primary IN (0, 1)),
    PRIMARY KEY (player_id, role)
);
CREATE UNIQUE INDEX player_positions_one_primary ON player_positions(player_id) WHERE is_primary = 1;

-- Las filas detalladas deben corresponder al grupo principal; portería no se mezcla con posiciones de campo.
CREATE TRIGGER player_positions_valid_insert BEFORE INSERT ON player_positions
WHEN (NEW.is_primary = 1 AND (SELECT main_position FROM players WHERE id = NEW.player_id) IS NOT
    CASE WHEN NEW.role = 1 THEN 1 WHEN NEW.role BETWEEN 2 AND 4 THEN 2 WHEN NEW.role BETWEEN 5 AND 9 THEN 3 ELSE 4 END)
 OR ((SELECT main_position FROM players WHERE id = NEW.player_id) = 1 AND NEW.role <> 1)
 OR ((SELECT main_position FROM players WHERE id = NEW.player_id) <> 1 AND NEW.role = 1)
BEGIN SELECT RAISE(ABORT, 'Position group mismatch'); END;
CREATE TRIGGER player_positions_valid_update BEFORE UPDATE ON player_positions
WHEN (NEW.is_primary = 1 AND (SELECT main_position FROM players WHERE id = NEW.player_id) IS NOT
    CASE WHEN NEW.role = 1 THEN 1 WHEN NEW.role BETWEEN 2 AND 4 THEN 2 WHEN NEW.role BETWEEN 5 AND 9 THEN 3 ELSE 4 END)
 OR ((SELECT main_position FROM players WHERE id = NEW.player_id) = 1 AND NEW.role <> 1)
 OR ((SELECT main_position FROM players WHERE id = NEW.player_id) <> 1 AND NEW.role = 1)
BEGIN SELECT RAISE(ABORT, 'Position group mismatch'); END;
CREATE TRIGGER players_keep_position_group BEFORE UPDATE OF main_position ON players
WHEN EXISTS (SELECT 1 FROM player_positions p WHERE p.player_id = OLD.id AND p.is_primary = 1
    AND NEW.main_position IS NOT CASE WHEN p.role = 1 THEN 1 WHEN p.role BETWEEN 2 AND 4 THEN 2
        WHEN p.role BETWEEN 5 AND 9 THEN 3 ELSE 4 END)
BEGIN SELECT RAISE(ABORT, 'Provide the complete new position set'); END;

-- Trazabilidad de la publicación: parámetros de generación, nunca valoraciones oficiales por jugador.
CREATE TABLE generation_metadata (
    singleton INTEGER PRIMARY KEY CHECK (singleton = 1),
    generator_version TEXT NOT NULL CHECK (length(trim(generator_version)) > 0),
    seed TEXT NOT NULL CHECK (length(trim(seed)) > 0),
    parameter_hash TEXT NOT NULL CHECK (length(parameter_hash) = 64),
    catalog_hash TEXT NOT NULL CHECK (length(catalog_hash) = 64),
    source_world_id TEXT NOT NULL CHECK (length(source_world_id) = 36)
);

-- Una secundaria exige una principal; al reemplazar un conjunto, el repositorio retira primero las secundarias.
CREATE TRIGGER player_positions_secondary_insert BEFORE INSERT ON player_positions
WHEN NEW.is_primary = 0 AND NOT EXISTS (
    SELECT 1 FROM player_positions p WHERE p.player_id = NEW.player_id AND p.is_primary = 1)
BEGIN SELECT RAISE(ABORT, 'Secondary position needs a primary position'); END;
CREATE TRIGGER player_positions_secondary_update BEFORE UPDATE ON player_positions
WHEN NEW.is_primary = 0 AND NOT EXISTS (
    SELECT 1 FROM player_positions p WHERE p.player_id = NEW.player_id AND p.is_primary = 1
        AND NOT (p.player_id = OLD.player_id AND p.role = OLD.role))
BEGIN SELECT RAISE(ABORT, 'Secondary position needs a primary position'); END;
CREATE TRIGGER player_positions_keep_primary_delete BEFORE DELETE ON player_positions
WHEN OLD.is_primary = 1 AND EXISTS (
    SELECT 1 FROM player_positions p WHERE p.player_id = OLD.player_id AND p.is_primary = 0)
BEGIN SELECT RAISE(ABORT, 'Remove secondary positions first'); END;
CREATE TRIGGER player_positions_keep_primary_update BEFORE UPDATE ON player_positions
WHEN OLD.is_primary = 1 AND (NEW.player_id <> OLD.player_id OR NEW.is_primary <> 1) AND EXISTS (
    SELECT 1 FROM player_positions p WHERE p.player_id = OLD.player_id AND p.is_primary = 0)
BEGIN SELECT RAISE(ABORT, 'Secondary positions need their original primary'); END;
