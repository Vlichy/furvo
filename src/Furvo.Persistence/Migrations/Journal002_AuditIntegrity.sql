-- Refuerzo de invariantes. Conserva los asientos, recibos y saldos existentes sin reescribirlos.
DROP TRIGGER journal_valid_entry;
CREATE TRIGGER journal_valid_entry BEFORE INSERT ON journal_entries BEGIN
 SELECT CASE WHEN NEW.id=(SELECT opening_entry_id FROM journal_scopes WHERE club_id=NEW.club_id) AND NEW.opening<>1
 THEN RAISE(ABORT,'Opening entry must be identified explicitly') END;
 SELECT CASE WHEN NEW.economic_date<(SELECT cutoff FROM journal_scopes WHERE club_id=NEW.club_id)
 THEN RAISE(ABORT,'Entry predates opening') END;
 SELECT CASE WHEN EXISTS(SELECT 1 FROM journal_entries WHERE club_id=NEW.club_id AND economic_date>NEW.economic_date)
 THEN RAISE(ABORT,'Backdated entry requires a separate correction policy') END;
 SELECT CASE WHEN EXISTS(SELECT 1 FROM json_each(NEW.lines_json) j WHERE
  json_type(j.value) IS NOT 'object' OR
  (SELECT count(*) FROM json_each(j.value))<>3 OR
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
 (SELECT opening_cents FROM journal_scopes WHERE club_id=NEW.club_id)<=0 OR
 NEW.id IS NOT (SELECT opening_entry_id FROM journal_scopes WHERE club_id=NEW.club_id) OR
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
-- Solo se sustituye la fila de metadatos cuyo CHECK anterior permitía únicamente versión 1.
DROP TABLE journal_schema;
CREATE TABLE journal_schema(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=2));
INSERT INTO journal_schema VALUES(1,2);
