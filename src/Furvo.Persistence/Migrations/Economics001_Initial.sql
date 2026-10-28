-- Extensión independiente: mundos v1-v4 y carreras existentes siguen en lectura sin migración implícita.
CREATE TABLE economic_publication(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1),
 catalog_version TEXT NOT NULL, catalog_sha256 TEXT NOT NULL CHECK(length(catalog_sha256)=64));
CREATE TABLE club_economics(
 club_id TEXT NOT NULL REFERENCES clubs(id),season_id TEXT NOT NULL REFERENCES seasons(id),
 plan_json TEXT NOT NULL CHECK(json_valid(plan_json)),
 cash_cents INTEGER NOT NULL CHECK(cash_cents>=0),transfer_remaining_cents INTEGER NOT NULL CHECK(transfer_remaining_cents>=0),
 manager_cost_cents INTEGER CHECK(manager_cost_cents IS NULL OR manager_cost_cents>0),
 PRIMARY KEY(club_id,season_id));
CREATE TRIGGER immutable_economic_plan BEFORE UPDATE OF plan_json,club_id,season_id ON club_economics
 BEGIN SELECT RAISE(ABORT,'Initial economic plan is immutable'); END;
