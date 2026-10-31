-- Extensión deportiva por carrera. No modifica publicaciones iniciales.
CREATE TABLE sports_schema(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1));
INSERT INTO sports_schema VALUES(1,1);
CREATE TABLE sport_assignments(player_id TEXT PRIMARY KEY,club_id TEXT NOT NULL REFERENCES clubs(id),team_id TEXT NOT NULL);
CREATE INDEX sport_assignments_team ON sport_assignments(team_id);
CREATE TABLE training_plans(team_id TEXT PRIMARY KEY,revision INTEGER NOT NULL CHECK(revision>=1),data_json TEXT NOT NULL CHECK(json_valid(data_json)));
CREATE TABLE training_development(player_id TEXT PRIMARY KEY,team_id TEXT NOT NULL,data_json TEXT NOT NULL CHECK(json_valid(data_json)));
CREATE TABLE movement_recommendations(id TEXT PRIMARY KEY,data_json TEXT NOT NULL CHECK(json_valid(data_json)));
CREATE TABLE movement_history(id INTEGER PRIMARY KEY AUTOINCREMENT,player_id TEXT NOT NULL,occurred_on TEXT NOT NULL,data_json TEXT NOT NULL CHECK(json_valid(data_json)),evidence TEXT NOT NULL);
CREATE TABLE sport_analysis_reports(id TEXT PRIMARY KEY,team_id TEXT NOT NULL,data_json TEXT NOT NULL CHECK(json_valid(data_json)));
CREATE TABLE sport_analysis_requests(id TEXT PRIMARY KEY,team_id TEXT NOT NULL,data_json TEXT NOT NULL CHECK(json_valid(data_json)));
CREATE TABLE sport_facilities(id TEXT PRIMARY KEY,club_id TEXT NOT NULL REFERENCES clubs(id),technology TEXT NOT NULL,data_json TEXT NOT NULL CHECK(json_valid(data_json)),UNIQUE(club_id,technology));
CREATE TABLE sport_acquisitions(id TEXT PRIMARY KEY,club_id TEXT NOT NULL REFERENCES clubs(id),data_json TEXT NOT NULL CHECK(json_valid(data_json)));

CREATE TABLE training_attendance(session_id TEXT PRIMARY KEY,team_id TEXT NOT NULL,occurred_on TEXT NOT NULL,evidence_json TEXT NOT NULL CHECK(json_valid(evidence_json)));
