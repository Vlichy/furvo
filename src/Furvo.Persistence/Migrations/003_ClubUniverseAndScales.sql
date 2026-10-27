-- La prevalidación C# enumera todos los conflictos antes de ejecutar este script en una copia.
-- Un error revierte la transacción completa; el archivo original nunca recibe estas escrituras.
-- No se reescriben 001/002. Los valores antiguos se validan antes de ejecutar esta migración.
-- El CHECK histórico de clubs se conserva; estos triggers imponen la escala definitiva.
CREATE TRIGGER clubs_reputation_v3_insert BEFORE INSERT ON clubs
WHEN NEW.reputation IS NOT NULL AND (typeof(NEW.reputation) <> 'integer' OR NEW.reputation NOT BETWEEN 1 AND 5)
BEGIN SELECT RAISE(ABORT, 'Club reputation must be 1..5'); END;
CREATE TRIGGER clubs_reputation_v3_update BEFORE UPDATE ON clubs
WHEN NEW.reputation IS NOT NULL AND (typeof(NEW.reputation) <> 'integer' OR NEW.reputation NOT BETWEEN 1 AND 5)
BEGIN SELECT RAISE(ABORT, 'Club reputation must be 1..5'); END;

-- Reconstruir solo la tabla de valores aplica el CHECK nuevo sin reconstruir entidades referenciadas.
CREATE TABLE player_attributes_v3 (
    player_id TEXT NOT NULL REFERENCES players(id),
    attribute_code TEXT NOT NULL REFERENCES attribute_definitions(code),
    value INTEGER NOT NULL CHECK (typeof(value) = 'integer' AND value BETWEEN 1 AND 20),
    PRIMARY KEY (player_id, attribute_code)
);
INSERT INTO player_attributes_v3 SELECT player_id, attribute_code, value FROM player_attributes;
DROP TABLE player_attributes;
ALTER TABLE player_attributes_v3 RENAME TO player_attributes;

ALTER TABLE clubs ADD COLUMN short_name TEXT CHECK (short_name IS NULL OR length(trim(short_name)) > 0);
ALTER TABLE clubs ADD COLUMN region TEXT CHECK (region IS NULL OR length(trim(region)) > 0);
ALTER TABLE clubs ADD COLUMN foundation_year INTEGER CHECK (foundation_year IS NULL OR
    (typeof(foundation_year) = 'integer' AND foundation_year BETWEEN 1 AND 9999));
ALTER TABLE clubs ADD COLUMN sporting_identity TEXT CHECK (sporting_identity IS NULL OR length(trim(sporting_identity)) > 0);
ALTER TABLE clubs ADD COLUMN primary_color TEXT CHECK (primary_color IS NULL OR length(trim(primary_color)) > 0);
ALTER TABLE clubs ADD COLUMN secondary_color TEXT CHECK (secondary_color IS NULL OR length(trim(secondary_color)) > 0);
ALTER TABLE clubs ADD COLUMN stadium_name TEXT CHECK (stadium_name IS NULL OR length(trim(stadium_name)) > 0);
ALTER TABLE clubs ADD COLUMN stadium_capacity INTEGER CHECK (stadium_capacity IS NULL OR
    (typeof(stadium_capacity) = 'integer' AND stadium_capacity > 0));
ALTER TABLE clubs ADD COLUMN initial_category TEXT CHECK (initial_category IS NULL OR length(trim(initial_category)) > 0);
ALTER TABLE seasons ADD COLUMN name TEXT CHECK (name IS NULL OR length(trim(name)) > 0);

CREATE TRIGGER clubs_profile_complete_insert BEFORE INSERT ON clubs
WHEN ((NEW.short_name IS NOT NULL OR NEW.region IS NOT NULL OR NEW.foundation_year IS NOT NULL OR NEW.sporting_identity IS NOT NULL OR NEW.primary_color IS NOT NULL OR NEW.secondary_color IS NOT NULL OR NEW.stadium_name IS NOT NULL OR NEW.stadium_capacity IS NOT NULL OR NEW.initial_category IS NOT NULL)
  AND (NEW.short_name IS NULL OR NEW.region IS NULL OR NEW.foundation_year IS NULL OR NEW.sporting_identity IS NULL OR NEW.primary_color IS NULL OR NEW.secondary_color IS NULL OR NEW.stadium_name IS NULL OR NEW.stadium_capacity IS NULL OR NEW.initial_category IS NULL))
BEGIN SELECT RAISE(ABORT, 'Club profile must be complete or unknown'); END;

CREATE TRIGGER clubs_profile_complete_update BEFORE UPDATE ON clubs
WHEN ((NEW.short_name IS NOT NULL OR NEW.region IS NOT NULL OR NEW.foundation_year IS NOT NULL OR NEW.sporting_identity IS NOT NULL OR NEW.primary_color IS NOT NULL OR NEW.secondary_color IS NOT NULL OR NEW.stadium_name IS NOT NULL OR NEW.stadium_capacity IS NOT NULL OR NEW.initial_category IS NOT NULL)
  AND (NEW.short_name IS NULL OR NEW.region IS NULL OR NEW.foundation_year IS NULL OR NEW.sporting_identity IS NULL OR NEW.primary_color IS NULL OR NEW.secondary_color IS NULL OR NEW.stadium_name IS NULL OR NEW.stadium_capacity IS NULL OR NEW.initial_category IS NULL))
BEGIN SELECT RAISE(ABORT, 'Club profile must be complete or unknown'); END;
