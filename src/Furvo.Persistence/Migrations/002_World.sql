-- Amplía el mundo sin inventar datos de filas históricas; los campos desconocidos permanecen NULL.
-- Las restricciones y el catálogo reflejan su versión histórica; las escalas definitivas llegan en 003.
-- Las columnas añadidas admiten NULL para conservar filas de esquema 1 sin inventar datos.
ALTER TABLE clubs ADD COLUMN city TEXT CHECK (city IS NULL OR length(trim(city)) > 0);
ALTER TABLE clubs ADD COLUMN country TEXT CHECK (country IS NULL OR length(trim(country)) > 0);
ALTER TABLE clubs ADD COLUMN reputation INTEGER CHECK (reputation IS NULL OR (typeof(reputation) = 'integer' AND reputation BETWEEN 1 AND 100));
ALTER TABLE players ADD COLUMN birth_date TEXT CHECK (birth_date IS NULL OR (length(birth_date) = 10 AND birth_date GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]' AND substr(birth_date, 1, 4) BETWEEN '0001' AND '9999' AND date(birth_date, '+0 days') IS birth_date));
ALTER TABLE players ADD COLUMN nationality TEXT CHECK (nationality IS NULL OR length(trim(nationality)) > 0);
ALTER TABLE players ADD COLUMN main_position INTEGER CHECK (main_position IS NULL OR (typeof(main_position) = 'integer' AND main_position BETWEEN 1 AND 4));
ALTER TABLE contracts ADD COLUMN start_date TEXT CHECK (start_date IS NULL OR (length(start_date) = 10 AND start_date GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]' AND substr(start_date, 1, 4) BETWEEN '0001' AND '9999' AND date(start_date, '+0 days') IS start_date));
ALTER TABLE contracts ADD COLUMN end_date TEXT CHECK (end_date IS NULL OR (length(end_date) = 10 AND end_date GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]' AND substr(end_date, 1, 4) BETWEEN '0001' AND '9999' AND date(end_date, '+0 days') IS end_date));
CREATE TABLE world_info (
    singleton INTEGER PRIMARY KEY CHECK (singleton = 1),
    kind TEXT NOT NULL CHECK (kind IN ('initial', 'save')),
    world_id TEXT NOT NULL CHECK (length(world_id) = 36 AND lower(world_id) = world_id AND world_id <> '00000000-0000-0000-0000-000000000000' AND substr(world_id,9,1) = '-' AND substr(world_id,14,1) = '-' AND substr(world_id,19,1) = '-' AND substr(world_id,24,1) = '-' AND length(replace(world_id,'-','')) = 32 AND replace(world_id,'-','') NOT GLOB '*[^0-9a-f]*')
);
CREATE TABLE coaches (
    id TEXT PRIMARY KEY NOT NULL CHECK (length(id) = 36 AND lower(id) = id AND id <> '00000000-0000-0000-0000-000000000000' AND substr(id,9,1) = '-' AND substr(id,14,1) = '-' AND substr(id,19,1) = '-' AND substr(id,24,1) = '-' AND length(replace(id,'-','')) = 32 AND replace(id,'-','') NOT GLOB '*[^0-9a-f]*'),
    name TEXT NOT NULL CHECK (length(trim(name)) > 0),
    club_id TEXT NOT NULL REFERENCES clubs(id)
);
CREATE TABLE competitions (
    id TEXT PRIMARY KEY NOT NULL CHECK (length(id) = 36 AND lower(id) = id AND id <> '00000000-0000-0000-0000-000000000000' AND substr(id,9,1) = '-' AND substr(id,14,1) = '-' AND substr(id,19,1) = '-' AND substr(id,24,1) = '-' AND length(replace(id,'-','')) = 32 AND replace(id,'-','') NOT GLOB '*[^0-9a-f]*'),
    name TEXT NOT NULL CHECK (length(trim(name)) > 0),
    country TEXT NOT NULL CHECK (length(trim(country)) > 0)
);
CREATE TABLE seasons (
    id TEXT PRIMARY KEY NOT NULL CHECK (length(id) = 36 AND lower(id) = id AND id <> '00000000-0000-0000-0000-000000000000' AND substr(id,9,1) = '-' AND substr(id,14,1) = '-' AND substr(id,19,1) = '-' AND substr(id,24,1) = '-' AND length(replace(id,'-','')) = 32 AND replace(id,'-','') NOT GLOB '*[^0-9a-f]*'),
    start_date TEXT NOT NULL CHECK (length(start_date) = 10 AND start_date GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]' AND substr(start_date, 1, 4) BETWEEN '0001' AND '9999' AND date(start_date, '+0 days') IS start_date),
    end_date TEXT NOT NULL CHECK (length(end_date) = 10 AND end_date GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]' AND substr(end_date, 1, 4) BETWEEN '0001' AND '9999' AND date(end_date, '+0 days') IS end_date),
    CHECK (start_date <= end_date)
);
CREATE TABLE registrations (
    club_id TEXT NOT NULL REFERENCES clubs(id),
    competition_id TEXT NOT NULL REFERENCES competitions(id),
    season_id TEXT NOT NULL REFERENCES seasons(id),
    PRIMARY KEY (club_id, competition_id, season_id)
);
CREATE INDEX contracts_player_dates ON contracts(player_id, start_date, end_date);
CREATE INDEX contracts_club ON contracts(club_id);
CREATE INDEX coaches_club ON coaches(club_id);
CREATE INDEX registrations_competition_season ON registrations(competition_id, season_id);

CREATE TRIGGER clubs_complete_insert BEFORE INSERT ON clubs
WHEN NEW.city IS NULL OR NEW.country IS NULL OR NEW.reputation IS NULL OR length(trim(NEW.name)) = 0 OR NOT (length(NEW.id) = 36 AND lower(NEW.id) = NEW.id AND NEW.id <> '00000000-0000-0000-0000-000000000000' AND substr(NEW.id,9,1) = '-' AND substr(NEW.id,14,1) = '-' AND substr(NEW.id,19,1) = '-' AND substr(NEW.id,24,1) = '-' AND length(replace(NEW.id,'-','')) = 32 AND replace(NEW.id,'-','') NOT GLOB '*[^0-9a-f]*')
BEGIN SELECT RAISE(ABORT, 'Incomplete or invalid entity'); END;

CREATE TRIGGER clubs_complete_update BEFORE UPDATE ON clubs
WHEN NEW.city IS NULL OR NEW.country IS NULL OR NEW.reputation IS NULL OR length(trim(NEW.name)) = 0 OR NOT (length(NEW.id) = 36 AND lower(NEW.id) = NEW.id AND NEW.id <> '00000000-0000-0000-0000-000000000000' AND substr(NEW.id,9,1) = '-' AND substr(NEW.id,14,1) = '-' AND substr(NEW.id,19,1) = '-' AND substr(NEW.id,24,1) = '-' AND length(replace(NEW.id,'-','')) = 32 AND replace(NEW.id,'-','') NOT GLOB '*[^0-9a-f]*')
BEGIN SELECT RAISE(ABORT, 'Incomplete or invalid entity'); END;

CREATE TRIGGER players_complete_insert BEFORE INSERT ON players
WHEN NEW.birth_date IS NULL OR NEW.nationality IS NULL OR NEW.main_position IS NULL OR length(trim(NEW.name)) = 0 OR NOT (length(NEW.id) = 36 AND lower(NEW.id) = NEW.id AND NEW.id <> '00000000-0000-0000-0000-000000000000' AND substr(NEW.id,9,1) = '-' AND substr(NEW.id,14,1) = '-' AND substr(NEW.id,19,1) = '-' AND substr(NEW.id,24,1) = '-' AND length(replace(NEW.id,'-','')) = 32 AND replace(NEW.id,'-','') NOT GLOB '*[^0-9a-f]*')
BEGIN SELECT RAISE(ABORT, 'Incomplete or invalid entity'); END;

CREATE TRIGGER players_complete_update BEFORE UPDATE ON players
WHEN NEW.birth_date IS NULL OR NEW.nationality IS NULL OR NEW.main_position IS NULL OR length(trim(NEW.name)) = 0 OR NOT (length(NEW.id) = 36 AND lower(NEW.id) = NEW.id AND NEW.id <> '00000000-0000-0000-0000-000000000000' AND substr(NEW.id,9,1) = '-' AND substr(NEW.id,14,1) = '-' AND substr(NEW.id,19,1) = '-' AND substr(NEW.id,24,1) = '-' AND length(replace(NEW.id,'-','')) = 32 AND replace(NEW.id,'-','') NOT GLOB '*[^0-9a-f]*')
BEGIN SELECT RAISE(ABORT, 'Incomplete or invalid entity'); END;

CREATE TRIGGER contracts_complete_insert BEFORE INSERT ON contracts
WHEN NEW.start_date IS NULL OR NEW.end_date IS NULL OR NEW.start_date > NEW.end_date OR NOT (length(NEW.id) = 36 AND lower(NEW.id) = NEW.id AND NEW.id <> '00000000-0000-0000-0000-000000000000' AND substr(NEW.id,9,1) = '-' AND substr(NEW.id,14,1) = '-' AND substr(NEW.id,19,1) = '-' AND substr(NEW.id,24,1) = '-' AND length(replace(NEW.id,'-','')) = 32 AND replace(NEW.id,'-','') NOT GLOB '*[^0-9a-f]*')
BEGIN SELECT RAISE(ABORT, 'Incomplete or invalid entity'); END;

CREATE TRIGGER contracts_complete_update BEFORE UPDATE ON contracts
WHEN NEW.start_date IS NULL OR NEW.end_date IS NULL OR NEW.start_date > NEW.end_date OR NOT (length(NEW.id) = 36 AND lower(NEW.id) = NEW.id AND NEW.id <> '00000000-0000-0000-0000-000000000000' AND substr(NEW.id,9,1) = '-' AND substr(NEW.id,14,1) = '-' AND substr(NEW.id,19,1) = '-' AND substr(NEW.id,24,1) = '-' AND length(replace(NEW.id,'-','')) = 32 AND replace(NEW.id,'-','') NOT GLOB '*[^0-9a-f]*')
BEGIN SELECT RAISE(ABORT, 'Incomplete or invalid entity'); END;

-- Las fechas son inclusivas: cualquier día compartido impide dos contratos del mismo jugador.
CREATE TRIGGER contracts_no_overlap_insert BEFORE INSERT ON contracts
WHEN EXISTS (
    SELECT 1 FROM contracts c
    WHERE c.player_id = NEW.player_id AND c.id <> NEW.id
      AND (c.start_date IS NULL OR c.end_date IS NULL
           OR (c.start_date <= NEW.end_date AND NEW.start_date <= c.end_date))
)
BEGIN SELECT RAISE(ABORT, 'Incompatible player contracts'); END;

CREATE TRIGGER contracts_no_overlap_update BEFORE UPDATE ON contracts
WHEN EXISTS (
    SELECT 1 FROM contracts c
    WHERE c.player_id = NEW.player_id AND c.id <> NEW.id
      AND (c.start_date IS NULL OR c.end_date IS NULL
           OR (c.start_date <= NEW.end_date AND NEW.start_date <= c.end_date))
)
BEGIN SELECT RAISE(ABORT, 'Incompatible player contracts'); END;

CREATE TABLE attribute_definitions (
    code TEXT PRIMARY KEY NOT NULL CHECK (length(trim(code)) > 0),
    name TEXT NOT NULL CHECK (length(trim(name)) > 0),
    category INTEGER NOT NULL CHECK (typeof(category) = 'integer' AND category BETWEEN 1 AND 5)
);
-- Clave compuesta y FK preservan el detalle individual, sin columnas ni medias por categoría.
CREATE TABLE player_attributes (
    player_id TEXT NOT NULL REFERENCES players(id),
    attribute_code TEXT NOT NULL REFERENCES attribute_definitions(code),
    value INTEGER NOT NULL CHECK (typeof(value) = 'integer' AND value BETWEEN 1 AND 100),
    PRIMARY KEY (player_id, attribute_code)
);

-- Catálogo inicial congelado para que la migración 2 sea reproducible.
INSERT INTO attribute_definitions(code, name, category) VALUES
    ('ball_control', 'Control del balón', 1),
    ('first_touch', 'Primer toque', 1),
    ('technique', 'Técnica', 1),
    ('dribbling', 'Regate', 1),
    ('short_passing', 'Pase corto', 1),
    ('long_passing', 'Pase largo', 1),
    ('crossing', 'Centros', 1),
    ('finishing', 'Remate', 1),
    ('long_shots', 'Disparo lejano', 1),
    ('shot_power', 'Potencia de disparo', 1),
    ('heading', 'Cabeceo', 1),
    ('tackling', 'Entradas', 1),
    ('marking', 'Marcaje', 1),
    ('penalties', 'Penaltis', 1),
    ('free_kicks', 'Faltas', 1),
    ('corners', 'Córneres', 1),
    ('throw_ins', 'Saques de banda', 1),
    ('anticipation', 'Anticipación', 2),
    ('concentration', 'Concentración', 2),
    ('decisions', 'Decisiones', 2),
    ('vision', 'Visión de juego', 2),
    ('defensive_positioning', 'Colocación defensiva', 2),
    ('off_the_ball', 'Desmarques', 2),
    ('teamwork', 'Trabajo en equipo', 2),
    ('work_rate', 'Esfuerzo', 2),
    ('determination', 'Determinación', 2),
    ('composure', 'Serenidad', 2),
    ('bravery', 'Valentía', 2),
    ('aggression', 'Agresividad', 2),
    ('leadership', 'Liderazgo', 2),
    ('creativity', 'Talento / creatividad', 2),
    ('tactical_discipline', 'Disciplina táctica', 2),
    ('tactical_understanding', 'Comprensión táctica', 2),
    ('pace', 'Velocidad', 3),
    ('acceleration', 'Aceleración', 3),
    ('stamina', 'Resistencia', 3),
    ('strength', 'Fuerza', 3),
    ('agility', 'Agilidad', 3),
    ('balance', 'Equilibrio', 3),
    ('jumping', 'Salto', 3),
    ('coordination', 'Coordinación', 3),
    ('reflexes', 'Reflejos', 4),
    ('handling', 'Blocaje', 4),
    ('punching', 'Despejes', 4),
    ('one_on_ones', 'Uno contra uno', 4),
    ('aerial_claims', 'Salidas aéreas', 4),
    ('rushing_out', 'Salidas a los pies', 4),
    ('goalkeeping_positioning', 'Colocación en portería', 4),
    ('command_of_area', 'Mando del área', 4),
    ('hand_distribution', 'Saque con la mano', 4),
    ('defensive_communication', 'Comunicación defensiva', 4),
    ('professionalism', 'Profesionalidad', 5),
    ('ambition', 'Ambición', 5),
    ('consistency', 'Regularidad', 5),
    ('big_matches', 'Rendimiento en grandes partidos', 5),
    ('adaptability', 'Adaptabilidad', 5),
    ('injury_proneness', 'Propensión a lesiones', 5),
    ('pressure_resistance', 'Resistencia a la presión', 5),
    ('temperament', 'Temperamento', 5),
    ('loyalty', 'Lealtad', 5),
    ('learning_ability', 'Capacidad de aprendizaje', 5);
