-- Ámbito explícito de observación. No modifica club_economics ni sus escritores anteriores.
CREATE TABLE journal_schema(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1));
INSERT INTO journal_schema VALUES(1,1);
CREATE TABLE journal_accounts(id INTEGER PRIMARY KEY CHECK(id BETWEEN 1 AND 9),name TEXT NOT NULL UNIQUE);
INSERT INTO journal_accounts VALUES(1,'Cash'),(2,'OpeningBalance'),(3,'Receivables'),(4,'Payables'),
 (5,'Income'),(6,'Expense'),(7,'Assets'),(8,'FinancialDebt'),(9,'Equity');
CREATE TABLE journal_scopes(
 club_id TEXT PRIMARY KEY REFERENCES clubs(id),
 cutoff TEXT NOT NULL CHECK(length(cutoff)=10 AND date(cutoff,'+0 days') IS cutoff),
 source_season_id TEXT NOT NULL REFERENCES seasons(id),
 opening_cents INTEGER NOT NULL CHECK(typeof(opening_cents)='integer' AND opening_cents>=0),
 cash_cents INTEGER NOT NULL CHECK(typeof(cash_cents)='integer' AND cash_cents>=0),
 opening_operation_id TEXT NOT NULL UNIQUE REFERENCES financial_operations(id) DEFERRABLE INITIALLY DEFERRED,
 opening_entry_id TEXT,
 CHECK((opening_cents=0 AND opening_entry_id IS NULL) OR (opening_cents>0 AND opening_entry_id IS NOT NULL)),
 FOREIGN KEY(opening_entry_id,club_id) REFERENCES journal_entries(id,club_id) DEFERRABLE INITIALLY DEFERRED
);
CREATE TABLE journal_entries(
 sequence INTEGER PRIMARY KEY AUTOINCREMENT,
 id TEXT NOT NULL UNIQUE CHECK(length(id)=36),
 club_id TEXT NOT NULL REFERENCES journal_scopes(club_id),
 economic_date TEXT NOT NULL CHECK(length(economic_date)=10 AND date(economic_date,'+0 days') IS economic_date),
 operation_id TEXT NOT NULL UNIQUE REFERENCES financial_operations(id) DEFERRABLE INITIALLY DEFERRED,
 reference TEXT NOT NULL CHECK(length(trim(reference))>0),
 opening INTEGER NOT NULL CHECK(opening IN(0,1)),
 reverses TEXT,
 lines_json TEXT NOT NULL CHECK(json_valid(lines_json) AND json_type(lines_json)='array' AND json_array_length(lines_json)>=2),
 UNIQUE(id,club_id),FOREIGN KEY(reverses,club_id) REFERENCES journal_entries(id,club_id)
);
CREATE UNIQUE INDEX journal_one_opening ON journal_entries(club_id) WHERE opening=1;
CREATE UNIQUE INDEX journal_one_reversal ON journal_entries(reverses) WHERE reverses IS NOT NULL;
CREATE VIEW journal_lines AS SELECT e.id AS entry_id,e.club_id,e.economic_date,
 CAST(j.key AS INTEGER) AS line_number,json_extract(j.value,'$.Account') AS account_id,
 json_extract(j.value,'$.Debit') AS debit_cents,json_extract(j.value,'$.Credit') AS credit_cents
 FROM journal_entries e,json_each(e.lines_json) j;
CREATE TRIGGER journal_valid_entry BEFORE INSERT ON journal_entries BEGIN
 SELECT CASE WHEN NEW.id=(SELECT opening_entry_id FROM journal_scopes WHERE club_id=NEW.club_id) AND NEW.opening<>1
 THEN RAISE(ABORT,'Opening entry must be identified explicitly') END;
 SELECT CASE WHEN NEW.economic_date<(SELECT cutoff FROM journal_scopes WHERE club_id=NEW.club_id)
 THEN RAISE(ABORT,'Entry predates opening') END;
 SELECT CASE WHEN EXISTS(SELECT 1 FROM journal_entries WHERE club_id=NEW.club_id AND economic_date>NEW.economic_date)
 THEN RAISE(ABORT,'Backdated entry requires a separate correction policy') END;
 SELECT CASE WHEN EXISTS(SELECT 1 FROM json_each(NEW.lines_json) j WHERE
  json_type(j.value,'$.Account') IS NOT 'integer' OR
  json_extract(j.value,'$.Account') NOT IN (SELECT id FROM journal_accounts) OR
  json_type(j.value,'$.Debit') IS NOT 'integer' OR json_type(j.value,'$.Credit') IS NOT 'integer' OR
  json_extract(j.value,'$.Debit')<0 OR json_extract(j.value,'$.Credit')<0 OR
  (json_extract(j.value,'$.Debit')=0)=(json_extract(j.value,'$.Credit')=0))
 THEN RAISE(ABORT,'Invalid entry line') END;
 SELECT CASE WHEN (SELECT sum(json_extract(value,'$.Debit')) FROM json_each(NEW.lines_json))
 <> (SELECT sum(json_extract(value,'$.Credit')) FROM json_each(NEW.lines_json))
 THEN RAISE(ABORT,'Unbalanced entry') END;
 SELECT CASE WHEN NEW.opening=0 AND EXISTS(SELECT 1 FROM json_each(NEW.lines_json) WHERE json_extract(value,'$.Account')=2)
 THEN RAISE(ABORT,'Opening account reserved for initial balance') END;
 SELECT CASE WHEN NEW.opening=1 AND (NEW.reverses IS NOT NULL OR
 NEW.id<>(SELECT opening_entry_id FROM journal_scopes WHERE club_id=NEW.club_id) OR
 NEW.operation_id<>(SELECT opening_operation_id FROM journal_scopes WHERE club_id=NEW.club_id) OR
 NEW.economic_date<>(SELECT cutoff FROM journal_scopes WHERE club_id=NEW.club_id) OR
 json_array_length(NEW.lines_json)<>2 OR
 json_extract(NEW.lines_json,'$[0].Account')<>1 OR json_extract(NEW.lines_json,'$[1].Account')<>2 OR
 json_extract(NEW.lines_json,'$[0].Debit')<>(SELECT opening_cents FROM journal_scopes WHERE club_id=NEW.club_id) OR
 json_extract(NEW.lines_json,'$[1].Credit')<>(SELECT opening_cents FROM journal_scopes WHERE club_id=NEW.club_id))
 THEN RAISE(ABORT,'Invalid opening') END;
 SELECT CASE WHEN NEW.reverses IS NOT NULL AND NOT EXISTS(SELECT 1 FROM journal_entries original
 WHERE original.id=NEW.reverses AND original.club_id=NEW.club_id AND original.opening=0
 AND original.economic_date<=NEW.economic_date AND json_array_length(original.lines_json)=json_array_length(NEW.lines_json)
 AND NOT EXISTS(SELECT 1 FROM json_each(NEW.lines_json) line WHERE
 json_extract(line.value,'$.Account')<>json_extract(original.lines_json,'$['||line.key||'].Account') OR
 json_extract(line.value,'$.Debit')<>json_extract(original.lines_json,'$['||line.key||'].Credit') OR
 json_extract(line.value,'$.Credit')<>json_extract(original.lines_json,'$['||line.key||'].Debit')))
 THEN RAISE(ABORT,'Invalid reversal') END;
END;
CREATE TRIGGER journal_update_cash AFTER INSERT ON journal_entries BEGIN
 UPDATE journal_scopes SET cash_cents=cash_cents+COALESCE((SELECT sum(json_extract(value,'$.Debit')-json_extract(value,'$.Credit'))
 FROM json_each(NEW.lines_json) WHERE json_extract(value,'$.Account')=1),0) WHERE club_id=NEW.club_id;
END;
CREATE TRIGGER journal_entries_no_update BEFORE UPDATE ON journal_entries BEGIN SELECT RAISE(ABORT,'Posted entry is immutable'); END;
CREATE TRIGGER journal_entries_no_delete BEFORE DELETE ON journal_entries BEGIN SELECT RAISE(ABORT,'Posted entry is immutable'); END;
CREATE TRIGGER journal_scope_identity BEFORE UPDATE OF club_id,cutoff,source_season_id,opening_cents,opening_operation_id,opening_entry_id ON journal_scopes
 BEGIN SELECT RAISE(ABORT,'Opening scope is immutable'); END;
CREATE TRIGGER journal_scope_no_delete BEFORE DELETE ON journal_scopes BEGIN SELECT RAISE(ABORT,'Opening scope is immutable'); END;
CREATE TRIGGER journal_accounts_no_update BEFORE UPDATE ON journal_accounts BEGIN SELECT RAISE(ABORT,'Account catalog is immutable'); END;
CREATE TRIGGER journal_accounts_no_delete BEFORE DELETE ON journal_accounts BEGIN SELECT RAISE(ABORT,'Account catalog is immutable'); END;
