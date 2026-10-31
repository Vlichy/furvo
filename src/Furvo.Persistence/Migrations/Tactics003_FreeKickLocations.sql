-- Solo en guardado explícito; se preservan las jugadas legadas.
ALTER TABLE tactics_schema_version RENAME TO tactics_schema_version_v2;
CREATE TABLE tactics_schema_version(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version BETWEEN 1 AND 3));
INSERT INTO tactics_schema_version VALUES(1,3);
DROP TABLE tactics_schema_version_v2;
CREATE TABLE career_free_kicks(club_id TEXT NOT NULL REFERENCES career_tactics(club_id),kind INTEGER NOT NULL CHECK(kind IN(3,4,5)),location INTEGER NOT NULL CHECK(location BETWEEN 1 AND 4),plan_json TEXT NOT NULL CHECK(json_valid(plan_json)),PRIMARY KEY(club_id,kind,location));
