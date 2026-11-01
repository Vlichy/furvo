-- Metadatos de jornada, sin reescribir los bloques históricos ni su delegación.
CREATE TABLE training_day_schema(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1));
INSERT INTO training_day_schema VALUES(1,1);
CREATE TABLE training_day_plans(
 team_id TEXT NOT NULL REFERENCES training_plans(team_id),
 date TEXT NOT NULL,
 kind INTEGER NOT NULL CHECK(kind BETWEEN 1 AND 3),
 single_turn INTEGER CHECK(single_turn IN(1,2)),
 PRIMARY KEY(team_id,date),
 CHECK((kind=2 AND single_turn IS NOT NULL) OR (kind<>2 AND single_turn IS NULL))
);
