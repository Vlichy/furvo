-- Extensión por carrera aplicada solo al guardar la primera observación.
CREATE TABLE academy_followup_schema(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1));
INSERT INTO academy_followup_schema VALUES(1,1);
CREATE TABLE academy_observations(id TEXT PRIMARY KEY,player_id TEXT NOT NULL,team_id TEXT NOT NULL REFERENCES academy_teams(id),registered_on TEXT NOT NULL,revision INTEGER NOT NULL CHECK(revision>=1),data_json TEXT NOT NULL CHECK(json_valid(data_json)));
CREATE INDEX academy_observations_player_date ON academy_observations(player_id,registered_on,id);
