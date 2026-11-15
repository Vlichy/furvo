-- Extensión optativa por partida. No rellena contratos antiguos ni cambia el esquema del mundo publicado.
CREATE TABLE contract_release_schema(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1));
INSERT INTO contract_release_schema VALUES(1,1);
CREATE TABLE contract_release_terms(
 contract_id TEXT PRIMARY KEY REFERENCES contracts(id) ON DELETE RESTRICT,
 presence INTEGER NOT NULL CHECK(presence IN(0,1,2)),
 amount_minor INTEGER CHECK(amount_minor IS NULL OR (typeof(amount_minor)='integer' AND amount_minor>0 AND amount_minor<=100000000000)),
 currency TEXT CHECK(currency IS NULL OR (length(currency)=3 AND currency GLOB '[A-Z][A-Z][A-Z]')),
 valid_from TEXT CHECK(valid_from IS NULL OR (length(valid_from)=10 AND date(valid_from,'+0 days') IS valid_from)),
 valid_until TEXT CHECK(valid_until IS NULL OR (length(valid_until)=10 AND date(valid_until,'+0 days') IS valid_until)),
 scope INTEGER NOT NULL CHECK(scope IN(0,1,2)),
 conditions_json TEXT NOT NULL CHECK(json_valid(conditions_json) AND json_type(conditions_json)='array'),
 disclosure INTEGER NOT NULL CHECK(disclosure IN(0,1)),
 evidence TEXT,
 confirmed_on TEXT CHECK(confirmed_on IS NULL OR (length(confirmed_on)=10 AND date(confirmed_on,'+0 days') IS confirmed_on)),
 CHECK(presence=2 AND amount_minor IS NOT NULL AND currency IS NOT NULL OR presence IN(0,1) AND amount_minor IS NULL AND currency IS NULL AND valid_from IS NULL AND valid_until IS NULL AND scope=0 AND json_array_length(conditions_json)=0),
 CHECK(valid_from IS NULL OR valid_until IS NULL OR valid_until>=valid_from),
 CHECK(disclosure=0 OR presence<>0 AND evidence IS NOT NULL AND length(trim(evidence))>0 AND confirmed_on IS NOT NULL)
);
CREATE TRIGGER release_interval_insert BEFORE INSERT ON contract_release_terms WHEN
 NEW.valid_from<(SELECT start_date FROM contracts WHERE id=NEW.contract_id) OR NEW.valid_until>(SELECT end_date FROM contracts WHERE id=NEW.contract_id) OR
 NEW.valid_from>(SELECT end_date FROM contracts WHERE id=NEW.contract_id) OR NEW.valid_until<(SELECT start_date FROM contracts WHERE id=NEW.contract_id)
BEGIN SELECT RAISE(ABORT,'Release interval outside contract'); END;
CREATE TRIGGER release_interval_update BEFORE UPDATE ON contract_release_terms WHEN
 NEW.valid_from<(SELECT start_date FROM contracts WHERE id=NEW.contract_id) OR NEW.valid_until>(SELECT end_date FROM contracts WHERE id=NEW.contract_id) OR
 NEW.valid_from>(SELECT end_date FROM contracts WHERE id=NEW.contract_id) OR NEW.valid_until<(SELECT start_date FROM contracts WHERE id=NEW.contract_id)
BEGIN SELECT RAISE(ABORT,'Release interval outside contract'); END;
CREATE TRIGGER release_contract_interval BEFORE UPDATE OF start_date,end_date ON contracts WHEN EXISTS(
 SELECT 1 FROM contract_release_terms r WHERE r.contract_id=OLD.id AND
 (r.valid_from<NEW.start_date OR r.valid_until>NEW.end_date OR r.valid_from>NEW.end_date OR r.valid_until<NEW.start_date))
BEGIN SELECT RAISE(ABORT,'Contract change invalidates release interval'); END;

CREATE TRIGGER release_conditions_insert BEFORE INSERT ON contract_release_terms WHEN EXISTS(
 SELECT 1 FROM json_each(NEW.conditions_json) c WHERE json_extract(c.value,'$.Kind') IS NULL OR json_extract(c.value,'$.Kind') NOT IN(1,2) OR
 (json_extract(c.value,'$.Kind')=1 AND NOT EXISTS(SELECT 1 FROM clubs WHERE id=json_extract(c.value,'$.Reference'))) OR
 (json_extract(c.value,'$.Kind')=2 AND NOT EXISTS(SELECT 1 FROM competitions WHERE id=json_extract(c.value,'$.Reference'))))
BEGIN SELECT RAISE(ABORT,'Release condition reference invalid'); END;
CREATE TRIGGER release_conditions_update BEFORE UPDATE OF conditions_json ON contract_release_terms WHEN EXISTS(
 SELECT 1 FROM json_each(NEW.conditions_json) c WHERE json_extract(c.value,'$.Kind') IS NULL OR json_extract(c.value,'$.Kind') NOT IN(1,2) OR
 (json_extract(c.value,'$.Kind')=1 AND NOT EXISTS(SELECT 1 FROM clubs WHERE id=json_extract(c.value,'$.Reference'))) OR
 (json_extract(c.value,'$.Kind')=2 AND NOT EXISTS(SELECT 1 FROM competitions WHERE id=json_extract(c.value,'$.Reference'))))
BEGIN SELECT RAISE(ABORT,'Release condition reference invalid'); END;
CREATE TRIGGER release_club_reference BEFORE DELETE ON clubs WHEN EXISTS(
 SELECT 1 FROM contract_release_terms r,json_each(r.conditions_json) c WHERE json_extract(c.value,'$.Kind')=1 AND json_extract(c.value,'$.Reference')=OLD.id)
BEGIN SELECT RAISE(ABORT,'Club referenced by release condition'); END;
CREATE TRIGGER release_competition_reference BEFORE DELETE ON competitions WHEN EXISTS(
 SELECT 1 FROM contract_release_terms r,json_each(r.conditions_json) c WHERE json_extract(c.value,'$.Kind')=2 AND json_extract(c.value,'$.Reference')=OLD.id)
BEGIN SELECT RAISE(ABORT,'Competition referenced by release condition'); END;
