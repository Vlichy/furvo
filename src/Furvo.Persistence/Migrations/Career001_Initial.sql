-- Extensión de carrera v1 aplicada solo a copias nuevas; user_version del mundo sigue siendo 4.
CREATE TABLE career_schema_version(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1));
INSERT INTO career_schema_version VALUES(1,1);
CREATE TABLE career_coaches(
 id TEXT PRIMARY KEY NOT NULL CHECK(length(id)=36), profile_id TEXT NOT NULL CHECK(length(profile_id)=36 AND profile_id<>id),
 profile_json TEXT NOT NULL CHECK(json_valid(profile_json))
);
CREATE TABLE career_state(
 singleton INTEGER PRIMARY KEY CHECK(singleton=1), save_id TEXT NOT NULL UNIQUE,
 coach_id TEXT NOT NULL UNIQUE REFERENCES career_coaches(id),season_id TEXT NOT NULL REFERENCES seasons(id),
 current_date TEXT NOT NULL CHECK(length(current_date)=10 AND date(current_date,'+0 days') IS current_date),
 start_mode INTEGER NOT NULL CHECK(start_mode BETWEEN 1 AND 3),employment INTEGER NOT NULL CHECK(employment IN(1,2)),
 club_id TEXT REFERENCES clubs(id),contract_id TEXT UNIQUE REFERENCES manager_contracts(id) DEFERRABLE INITIALLY DEFERRED,
 request_hash TEXT NOT NULL CHECK(length(request_hash)=64),
 CHECK((employment=1 AND start_mode IN(1,2) AND club_id IS NULL AND contract_id IS NULL) OR
       (employment=2 AND start_mode=3 AND club_id IS NOT NULL AND contract_id IS NOT NULL))
);
CREATE TABLE manager_contracts(
 id TEXT PRIMARY KEY NOT NULL CHECK(length(id)=36),career_singleton INTEGER NOT NULL UNIQUE CHECK(career_singleton=1)
 REFERENCES career_state(singleton) DEFERRABLE INITIALLY DEFERRED,
 coach_id TEXT NOT NULL REFERENCES career_coaches(id),club_id TEXT NOT NULL REFERENCES clubs(id),
 start_date TEXT NOT NULL CHECK(length(start_date)=10 AND date(start_date,'+0 days') IS start_date),
 end_date TEXT NOT NULL CHECK(length(end_date)=10 AND date(end_date,'+0 days') IS end_date AND end_date>=start_date),
 duration_seasons INTEGER NOT NULL CHECK(duration_seasons>=1),gross_annual_salary TEXT NOT NULL CHECK(CAST(gross_annual_salary AS REAL)>0),
 currency TEXT NOT NULL CHECK(length(trim(currency))=3),termination_conditions TEXT NOT NULL CHECK(length(trim(termination_conditions))>0),
 offer_json TEXT NOT NULL CHECK(json_valid(offer_json))
);
CREATE TABLE declined_manager_offers(
 offer_id TEXT PRIMARY KEY NOT NULL CHECK(length(offer_id)=36),career_singleton INTEGER NOT NULL UNIQUE CHECK(career_singleton=1)
 REFERENCES career_state(singleton) DEFERRABLE INITIALLY DEFERRED,
 club_id TEXT NOT NULL REFERENCES clubs(id),offer_json TEXT NOT NULL CHECK(json_valid(offer_json))
);
CREATE TRIGGER career_state_integrity BEFORE INSERT ON career_state BEGIN
 SELECT CASE WHEN NEW.save_id<>(SELECT save_id FROM save_metadata WHERE singleton=1) THEN RAISE(ABORT,'Save identity mismatch') END;
 SELECT CASE WHEN NEW.employment=2 AND NOT EXISTS(SELECT 1 FROM manager_contracts c WHERE c.id=NEW.contract_id AND c.coach_id=NEW.coach_id AND c.club_id=NEW.club_id AND c.career_singleton=1) THEN RAISE(ABORT,'Employed career requires matching contract') END;
 SELECT CASE WHEN NEW.employment=1 AND EXISTS(SELECT 1 FROM manager_contracts) THEN RAISE(ABORT,'Unemployed career cannot have contract') END;
 SELECT CASE WHEN (NEW.start_mode=2)<>EXISTS(SELECT 1 FROM declined_manager_offers) THEN RAISE(ABORT,'Declined offer mismatch') END;
END;
CREATE TRIGGER manager_contract_integrity BEFORE INSERT ON manager_contracts
WHEN EXISTS(SELECT 1 FROM career_state s WHERE s.employment<>2 OR s.contract_id<>NEW.id OR s.club_id<>NEW.club_id OR s.coach_id<>NEW.coach_id)
BEGIN SELECT RAISE(ABORT,'Contract incompatible with career'); END;
CREATE TRIGGER declined_offer_integrity BEFORE INSERT ON declined_manager_offers
WHEN EXISTS(SELECT 1 FROM career_state s WHERE s.start_mode<>2)
BEGIN SELECT RAISE(ABORT,'Declined offer incompatible with career'); END;
CREATE TRIGGER frozen_manager_contract BEFORE UPDATE ON manager_contracts BEGIN SELECT RAISE(ABORT,'Accepted conditions are immutable'); END;
CREATE TRIGGER frozen_career_coach BEFORE UPDATE ON career_coaches BEGIN SELECT RAISE(ABORT,'Initial identity snapshot is immutable'); END;
CREATE TRIGGER frozen_career_start BEFORE UPDATE ON career_state BEGIN SELECT RAISE(ABORT,'Initial career state is immutable in this version'); END;
