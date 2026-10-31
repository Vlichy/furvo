-- Ampliación bajo guardado explícito. Las configuraciones v1 conservan sus valores y lanzador antiguo.
ALTER TABLE tactics_schema_version RENAME TO tactics_schema_version_v1;
CREATE TABLE tactics_schema_version(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version IN(1,2)));
INSERT INTO tactics_schema_version VALUES(1,2);
DROP TABLE tactics_schema_version_v1;
ALTER TABLE career_set_pieces ADD COLUMN details_json TEXT CHECK(details_json IS NULL OR json_valid(details_json));
CREATE TABLE career_penalty_order(club_id TEXT NOT NULL REFERENCES career_tactics(club_id),ordinal INTEGER NOT NULL CHECK(ordinal BETWEEN 1 AND 5),
 player_id TEXT REFERENCES players(id),PRIMARY KEY(club_id,ordinal),UNIQUE(club_id,player_id));
CREATE TRIGGER shootout_member BEFORE INSERT ON career_penalty_order WHEN NEW.player_id IS NOT NULL AND NOT EXISTS(
 SELECT 1 FROM contracts c JOIN career_state s ON s.club_id=c.club_id WHERE s.employment=2 AND c.club_id=NEW.club_id AND c.player_id=NEW.player_id AND c.start_date<=s.current_date AND c.end_date>=s.current_date)
 BEGIN SELECT RAISE(ABORT,'Shootout taker not in squad'); END;
