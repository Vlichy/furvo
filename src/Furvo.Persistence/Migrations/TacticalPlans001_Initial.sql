-- Una sola composición por plan y dos geometrías. No se duplican jugadores en cada fase.
CREATE TABLE tactical_plans_schema(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1));
INSERT INTO tactical_plans_schema VALUES(1,1);
CREATE TABLE career_plan_books(club_id TEXT PRIMARY KEY REFERENCES clubs(id),revision INTEGER NOT NULL CHECK(revision>=1));
CREATE TABLE career_tactical_plans(id TEXT PRIMARY KEY,club_id TEXT NOT NULL REFERENCES career_plan_books(club_id),
 kind INTEGER NOT NULL CHECK(kind BETWEEN 1 AND 3),seed TEXT NOT NULL CHECK(length(seed)=16),bench_limit INTEGER NOT NULL CHECK(bench_limit>=0),
 mentality INTEGER CHECK(mentality BETWEEN 1 AND 5),instructions_json TEXT NOT NULL CHECK(json_valid(instructions_json)),UNIQUE(club_id,kind));
CREATE TABLE career_plan_members(plan_id TEXT NOT NULL REFERENCES career_tactical_plans(id),player_id TEXT NOT NULL REFERENCES players(id),
 assignment INTEGER NOT NULL CHECK(assignment BETWEEN 1 AND 3),slot_id TEXT,PRIMARY KEY(plan_id,player_id),UNIQUE(plan_id,slot_id),
 CHECK((assignment=1 AND slot_id IS NOT NULL) OR (assignment IN(2,3) AND slot_id IS NULL)));
CREATE TABLE career_plan_dispositions(plan_id TEXT NOT NULL REFERENCES career_tactical_plans(id),phase INTEGER NOT NULL CHECK(phase IN(1,2)),
 reference_json TEXT NOT NULL CHECK(json_valid(reference_json)),positions_json TEXT NOT NULL CHECK(json_valid(positions_json)),
 customized INTEGER NOT NULL CHECK(customized IN(0,1)),PRIMARY KEY(plan_id,phase));
CREATE TRIGGER plan_owner BEFORE INSERT ON career_plan_books WHEN NOT EXISTS(SELECT 1 FROM career_state WHERE employment=2 AND club_id=NEW.club_id)
 BEGIN SELECT RAISE(ABORT,'Plan requires employed club'); END;
CREATE TRIGGER plan_member_club BEFORE INSERT ON career_plan_members WHEN NOT EXISTS(
 SELECT 1 FROM contracts c JOIN career_state s ON s.club_id=c.club_id JOIN career_tactical_plans p ON p.club_id=s.club_id
 WHERE p.id=NEW.plan_id AND c.player_id=NEW.player_id AND c.start_date<=s.current_date AND c.end_date>=s.current_date)
 BEGIN SELECT RAISE(ABORT,'Plan member not in squad'); END;
