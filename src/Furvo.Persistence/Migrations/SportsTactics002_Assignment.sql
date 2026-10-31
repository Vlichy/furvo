-- La asignación deportiva no reemplaza el contrato. Admite futbolistas del club cuyo equipo cambió explícitamente.
DROP TRIGGER IF EXISTS plan_member_club;
CREATE TRIGGER plan_member_club BEFORE INSERT ON career_plan_members WHEN NOT EXISTS(
 SELECT 1 FROM career_tactical_plans p JOIN career_state s ON s.club_id=p.club_id
 WHERE p.id=NEW.plan_id AND (
 EXISTS(SELECT 1 FROM sport_assignments a WHERE a.player_id=NEW.player_id AND a.club_id=p.club_id AND a.team_id=p.club_id)
 OR (NOT EXISTS(SELECT 1 FROM sport_assignments a WHERE a.player_id=NEW.player_id) AND EXISTS(SELECT 1 FROM contracts c WHERE c.player_id=NEW.player_id AND c.club_id=p.club_id AND c.start_date<=s.current_date AND c.end_date>=s.current_date))))
 BEGIN SELECT RAISE(ABORT,'Plan member not assigned to first team'); END;
