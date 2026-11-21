-- Extensión explícita por carrera. No modifica tesorería ni instala el diario.
CREATE TABLE budget_schema(singleton INTEGER PRIMARY KEY CHECK(singleton=1), version INTEGER NOT NULL CHECK(version=1));
INSERT INTO budget_schema VALUES(1,1);
CREATE TABLE budgets(
 id TEXT PRIMARY KEY, club_id TEXT NOT NULL REFERENCES clubs(id), category TEXT NOT NULL CHECK(length(trim(category))>0),
 basis TEXT NOT NULL CHECK(length(trim(basis))>0), start_date TEXT NOT NULL, end_date TEXT NOT NULL CHECK(end_date>=start_date),
 currency TEXT NOT NULL CHECK(currency='EUR'), definition_json TEXT NOT NULL CHECK(json_valid(definition_json)));
CREATE TRIGGER budgets_no_overlap BEFORE INSERT ON budgets BEGIN
 SELECT CASE WHEN EXISTS(SELECT 1 FROM budgets WHERE club_id=NEW.club_id AND category=NEW.category AND basis=NEW.basis
 AND start_date<=NEW.end_date AND end_date>=NEW.start_date) THEN RAISE(ABORT,'Overlapping budget periods') END;
END;
CREATE TABLE budget_revisions(
 budget_id TEXT NOT NULL REFERENCES budgets(id), revision INTEGER NOT NULL CHECK(revision>0),
 operation_id TEXT NOT NULL REFERENCES financial_operations(id) DEFERRABLE INITIALLY DEFERRED,
 effective_date TEXT NOT NULL, actor TEXT NOT NULL CHECK(length(trim(actor))>0),
 authority TEXT NOT NULL CHECK(length(trim(authority))>0), reason TEXT NOT NULL CHECK(length(trim(reason))>0),
 action INTEGER NOT NULL CHECK(action BETWEEN 0 AND 6),
 authorized_cents INTEGER NOT NULL CHECK(typeof(authorized_cents)='integer' AND authorized_cents>=0),
 reserved_cents INTEGER NOT NULL CHECK(typeof(reserved_cents)='integer' AND reserved_cents>=0),
 committed_cents INTEGER NOT NULL CHECK(typeof(committed_cents)='integer' AND committed_cents>=0),
 executed_cents INTEGER NOT NULL CHECK(typeof(executed_cents)='integer' AND executed_cents>=0 AND executed_cents<=committed_cents),
 snapshot_json TEXT NOT NULL CHECK(json_valid(snapshot_json)),
 PRIMARY KEY(budget_id,revision), UNIQUE(budget_id,operation_id));
CREATE TRIGGER budget_revision_order BEFORE INSERT ON budget_revisions BEGIN
 SELECT CASE WHEN NEW.revision<>coalesce((SELECT max(revision)+1 FROM budget_revisions WHERE budget_id=NEW.budget_id),1)
 THEN RAISE(ABORT,'Budget revision conflict') END;
 SELECT CASE WHEN EXISTS(SELECT 1 FROM budget_revisions WHERE budget_id=NEW.budget_id AND effective_date>NEW.effective_date)
 THEN RAISE(ABORT,'Budget cannot rewrite earlier history') END;
END;
CREATE TRIGGER budgets_no_update BEFORE UPDATE ON budgets BEGIN SELECT RAISE(ABORT,'Immutable budget definition'); END;
CREATE TRIGGER budgets_no_delete BEFORE DELETE ON budgets BEGIN SELECT RAISE(ABORT,'Immutable budget definition'); END;
CREATE TRIGGER budget_revisions_no_update BEFORE UPDATE ON budget_revisions BEGIN SELECT RAISE(ABORT,'Immutable budget history'); END;
CREATE TRIGGER budget_revisions_no_delete BEFORE DELETE ON budget_revisions BEGIN SELECT RAISE(ABORT,'Immutable budget history'); END;
