-- Extensión explícita: se crea al guardar una configuración, nunca al consultar una carrera.
CREATE TABLE tactics_schema_version(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1));
INSERT INTO tactics_schema_version VALUES(1,1);
CREATE TABLE career_tactics(club_id TEXT PRIMARY KEY REFERENCES clubs(id),revision INTEGER NOT NULL CHECK(revision>=1),
 mentality INTEGER CHECK(mentality BETWEEN 1 AND 5),instructions_json TEXT NOT NULL CHECK(json_valid(instructions_json)),data_version INTEGER NOT NULL CHECK(data_version=1));
CREATE TABLE career_set_pieces(club_id TEXT NOT NULL REFERENCES career_tactics(club_id),kind INTEGER NOT NULL CHECK(kind BETWEEN 1 AND 7),
 routine TEXT,taker_id TEXT REFERENCES players(id),PRIMARY KEY(club_id,kind),CHECK(kind NOT IN(2,5) OR taker_id IS NULL));
CREATE TRIGGER tactics_owner BEFORE INSERT ON career_tactics WHEN NOT EXISTS(SELECT 1 FROM career_state WHERE employment=2 AND club_id=NEW.club_id)
 BEGIN SELECT RAISE(ABORT,'Tactics require employed club'); END;
CREATE TRIGGER tactics_taker BEFORE INSERT ON career_set_pieces WHEN NEW.taker_id IS NOT NULL AND NOT EXISTS(
 SELECT 1 FROM contracts c JOIN career_state s ON s.club_id=c.club_id WHERE s.employment=2 AND c.club_id=NEW.club_id AND c.player_id=NEW.taker_id AND c.start_date<=s.current_date AND c.end_date>=s.current_date)
 BEGIN SELECT RAISE(ABORT,'Taker not in current squad'); END;
CREATE TRIGGER tactics_taker_update BEFORE UPDATE ON career_set_pieces WHEN NEW.taker_id IS NOT NULL AND NOT EXISTS(
 SELECT 1 FROM contracts c JOIN career_state s ON s.club_id=c.club_id WHERE s.employment=2 AND c.club_id=NEW.club_id AND c.player_id=NEW.taker_id AND c.start_date<=s.current_date AND c.end_date>=s.current_date)
 BEGIN SELECT RAISE(ABORT,'Taker not in current squad'); END;
