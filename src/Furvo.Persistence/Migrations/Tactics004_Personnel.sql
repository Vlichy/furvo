CREATE TABLE career_tactical_personnel(club_id TEXT PRIMARY KEY REFERENCES clubs(id),data_json TEXT NOT NULL CHECK(json_valid(data_json)));
CREATE TABLE career_tactical_familiarity(team_id TEXT NOT NULL,player_id TEXT NOT NULL,aspect INTEGER NOT NULL CHECK(aspect BETWEEN 1 AND 4),configuration TEXT NOT NULL,observation_json TEXT NOT NULL CHECK(json_valid(observation_json)),PRIMARY KEY(team_id,player_id,aspect,configuration));
CREATE TABLE career_opponent_orders(team_id TEXT NOT NULL,match_id TEXT NOT NULL,player_id TEXT NOT NULL,order_json TEXT NOT NULL CHECK(json_valid(order_json)),PRIMARY KEY(team_id,match_id,player_id));
ALTER TABLE tactics_schema_version RENAME TO tactics_schema_version_v3;
CREATE TABLE tactics_schema_version(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version BETWEEN 1 AND 4));
INSERT INTO tactics_schema_version VALUES(1,4);
DROP TABLE tactics_schema_version_v3;
