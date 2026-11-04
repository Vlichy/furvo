-- Extensión incremental por carrera; consultar carreras antiguas no migra ni inventa restricciones.
CREATE TABLE training_availability_schema(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1));
INSERT INTO training_availability_schema VALUES(1,1);
-- Los futbolistas de cantera comparten identidad de dominio pero viven en academy_players: la pertenencia se valida en el almacén.
CREATE TABLE training_availability(
 id TEXT PRIMARY KEY,player_id TEXT NOT NULL,starts_on TEXT NOT NULL,ends_on TEXT,
 revision INTEGER NOT NULL CHECK(revision>=1),data_json TEXT NOT NULL CHECK(json_valid(data_json)),
 CHECK(length(starts_on)=10),CHECK(ends_on IS NULL OR length(ends_on)=10 AND ends_on>=starts_on));
CREATE INDEX training_availability_player_dates ON training_availability(player_id,starts_on,ends_on);
