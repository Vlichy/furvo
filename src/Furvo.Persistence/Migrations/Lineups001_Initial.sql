-- Extensión independiente, creada al confirmar carreras nuevas o por inicialización explícita de una carrera antigua.
CREATE TABLE lineup_schema_version(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1));
INSERT INTO lineup_schema_version VALUES(1,1);
CREATE TABLE career_lineup_seed(singleton INTEGER PRIMARY KEY CHECK(singleton=1),seed TEXT NOT NULL CHECK(length(seed)=16));
CREATE TABLE career_lineups(club_id TEXT PRIMARY KEY REFERENCES clubs(id),formation_json TEXT NOT NULL CHECK(json_valid(formation_json)),
 bench_limit INTEGER NOT NULL CHECK(bench_limit>=0),revision INTEGER NOT NULL CHECK(revision>=0),policy_version TEXT NOT NULL);
CREATE TABLE career_lineup_members(club_id TEXT NOT NULL REFERENCES career_lineups(club_id),player_id TEXT NOT NULL REFERENCES players(id),
 assignment INTEGER NOT NULL CHECK(assignment IN(1,2,3)),slot_id TEXT,
 PRIMARY KEY(club_id,player_id),UNIQUE(club_id,slot_id),CHECK((assignment=1 AND slot_id IS NOT NULL) OR (assignment IN(2,3) AND slot_id IS NULL)));
CREATE TRIGGER lineup_owner BEFORE INSERT ON career_lineups WHEN NOT EXISTS(SELECT 1 FROM career_state WHERE employment=2 AND club_id=NEW.club_id)
 BEGIN SELECT RAISE(ABORT,'Lineup requires current employed club'); END;
CREATE TRIGGER lineup_member_club BEFORE INSERT ON career_lineup_members WHEN NOT EXISTS(
 SELECT 1 FROM contracts c JOIN career_state s ON s.club_id=c.club_id WHERE s.employment=2 AND c.club_id=NEW.club_id AND c.player_id=NEW.player_id AND c.start_date<=s.current_date AND c.end_date>=s.current_date)
 BEGIN SELECT RAISE(ABORT,'Player not in current squad'); END;
