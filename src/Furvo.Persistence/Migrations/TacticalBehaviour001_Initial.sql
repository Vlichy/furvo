-- Extensión por carrera. Leer formatos anteriores no provoca migraciones.
CREATE TABLE IF NOT EXISTS tactical_behaviour_schema(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1));
INSERT OR IGNORE INTO tactical_behaviour_schema VALUES(1,1);
CREATE TABLE IF NOT EXISTS career_slot_behaviours(plan_id TEXT NOT NULL REFERENCES career_tactical_plans(id),phase INTEGER NOT NULL CHECK(phase IN(1,2)),data_json TEXT NOT NULL CHECK(json_valid(data_json)),PRIMARY KEY(plan_id,phase));
