-- Solo extensiones recién creadas y vacías dentro del temporal de una carrera nueva.
-- Las carreras existentes v1 se leen sin ejecutar esta migración.
CREATE TABLE career002_empty_guard(value INTEGER NOT NULL CHECK(value=0));
INSERT INTO career002_empty_guard SELECT (SELECT count(*) FROM career_state)+(SELECT count(*) FROM manager_contracts)+(SELECT count(*) FROM declined_manager_offers)+(SELECT count(*) FROM career_coaches);
DROP TABLE career002_empty_guard;
DROP TABLE manager_contracts;
CREATE TABLE manager_contracts(
 id TEXT PRIMARY KEY NOT NULL CHECK(length(id)=36),career_singleton INTEGER NOT NULL UNIQUE CHECK(career_singleton=1)
 REFERENCES career_state(singleton) DEFERRABLE INITIALLY DEFERRED,
 coach_id TEXT NOT NULL REFERENCES career_coaches(id),club_id TEXT NOT NULL REFERENCES clubs(id),
 start_date TEXT NOT NULL CHECK(length(start_date)=10 AND date(start_date,'+0 days') IS start_date),
 end_date TEXT NOT NULL CHECK(length(end_date)=10 AND date(end_date,'+0 days') IS end_date AND end_date>start_date),
 gross_annual_salary TEXT NOT NULL CHECK(CAST(gross_annual_salary AS REAL)>0),
 currency TEXT NOT NULL CHECK(currency IN('EUR','USD','GBP','CHF','JPY','CAD','AUD')),
 termination_kind INTEGER NOT NULL CHECK(termination_kind IN(0,1,2)),termination_conditions TEXT,
 offer_json TEXT NOT NULL CHECK(json_valid(offer_json)),
 CHECK((termination_kind=1 AND termination_conditions IS NOT NULL AND length(trim(termination_conditions))>0) OR
       (termination_kind IN(0,2) AND termination_conditions IS NULL))
);
CREATE TRIGGER manager_contract_integrity BEFORE INSERT ON manager_contracts
WHEN EXISTS(SELECT 1 FROM career_state s WHERE s.employment<>2 OR s.contract_id<>NEW.id OR s.club_id<>NEW.club_id OR s.coach_id<>NEW.coach_id)
BEGIN SELECT RAISE(ABORT,'Contract incompatible with career'); END;
CREATE TRIGGER frozen_manager_contract BEFORE UPDATE ON manager_contracts BEGIN SELECT RAISE(ABORT,'Accepted conditions are immutable'); END;
DROP TABLE career_schema_version;
CREATE TABLE career_schema_version(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=2));
INSERT INTO career_schema_version VALUES(1,2);
