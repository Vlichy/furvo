-- Extensión por carrera: no modifica tablas fundacionales ni tácticas del primer equipo.
CREATE TABLE academy_schema_version(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1),generator TEXT NOT NULL,catalog_hash TEXT NOT NULL);
CREATE TABLE academy_teams(id TEXT PRIMARY KEY,club_id TEXT NOT NULL REFERENCES clubs(id),name TEXT NOT NULL,category TEXT NOT NULL CHECK(category IN('Senior','Juvenil')),level INTEGER NOT NULL CHECK(level>0),size INTEGER NOT NULL CHECK(size BETWEEN 18 AND 22),competition TEXT,
 management INTEGER NOT NULL CHECK(management IN(1,2)),tactic_mode INTEGER NOT NULL CHECK(tactic_mode IN(1,2)),revision INTEGER NOT NULL CHECK(revision>=0),rosters_json TEXT NOT NULL CHECK(json_valid(rosters_json)),own_json TEXT CHECK(own_json IS NULL OR json_valid(own_json)),common_json TEXT NOT NULL CHECK(json_valid(common_json)),UNIQUE(id,club_id),UNIQUE(club_id,name),CHECK((tactic_mode=1 AND own_json IS NULL) OR (tactic_mode=2 AND own_json IS NOT NULL)));
CREATE TABLE academy_staff(id TEXT PRIMARY KEY,team_id TEXT NOT NULL UNIQUE REFERENCES academy_teams(id),name TEXT NOT NULL,role TEXT NOT NULL,link_state TEXT NOT NULL);
CREATE TABLE academy_players(id TEXT PRIMARY KEY,team_id TEXT NOT NULL,club_id TEXT NOT NULL REFERENCES clubs(id),data_json TEXT NOT NULL CHECK(json_valid(data_json) AND json_extract(data_json,'$.Id')=id),FOREIGN KEY(team_id,club_id) REFERENCES academy_teams(id,club_id));
CREATE INDEX academy_players_team ON academy_players(team_id);
CREATE TRIGGER academy_distinct_player BEFORE INSERT ON academy_players WHEN EXISTS(SELECT 1 FROM players WHERE id=NEW.id) BEGIN SELECT RAISE(ABORT,'Player already in first-team world'); END;

CREATE TRIGGER academy_distinct_player_update BEFORE UPDATE OF id ON academy_players WHEN EXISTS(SELECT 1 FROM players WHERE id=NEW.id) BEGIN SELECT RAISE(ABORT,'Player already in first-team world'); END;
CREATE TRIGGER academy_world_distinct_player BEFORE INSERT ON players WHEN EXISTS(SELECT 1 FROM academy_players WHERE id=NEW.id) BEGIN SELECT RAISE(ABORT,'Player permanently assigned to academy'); END;
CREATE TRIGGER academy_world_distinct_player_update BEFORE UPDATE OF id ON players WHEN EXISTS(SELECT 1 FROM academy_players WHERE id=NEW.id) BEGIN SELECT RAISE(ABORT,'Player permanently assigned to academy'); END;
