-- Conserva íntegramente los bloques de descanso obsoletos. No elimina evidencias ni observaciones.
CREATE TABLE training_rest_schema(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1));
INSERT INTO training_rest_schema VALUES(1,1);
CREATE TABLE training_rest_archive(
 team_id TEXT NOT NULL REFERENCES training_plans(team_id),
 block_id TEXT NOT NULL,
 occurred_on TEXT NOT NULL,
 data_json TEXT NOT NULL CHECK(json_valid(data_json)),
 PRIMARY KEY(team_id,block_id)
);
