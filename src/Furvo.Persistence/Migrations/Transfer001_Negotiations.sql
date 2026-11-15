-- Extensión por carrera. Nunca se instala al consultar ni modifica entidades deportivas o económicas.
CREATE TABLE transfer_schema(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1));
INSERT INTO transfer_schema VALUES(1,1);
CREATE TABLE transfer_negotiations(id TEXT PRIMARY KEY,career_id TEXT NOT NULL,buyer_id TEXT NOT NULL REFERENCES clubs(id),seller_id TEXT NOT NULL REFERENCES clubs(id),player_id TEXT NOT NULL,CHECK(buyer_id<>seller_id));
CREATE TABLE transfer_versions(negotiation_id TEXT NOT NULL REFERENCES transfer_negotiations(id),revision INTEGER NOT NULL CHECK(revision>0),operation_id TEXT NOT NULL UNIQUE,data_json TEXT NOT NULL CHECK(json_valid(data_json) AND json_extract(data_json,'$.Id') IS NOT NULL AND json_extract(data_json,'$.Id')=negotiation_id AND json_extract(data_json,'$.Revision') IS NOT NULL AND json_extract(data_json,'$.Revision')=revision),PRIMARY KEY(negotiation_id,revision));
CREATE TRIGGER transfer_versions_no_update BEFORE UPDATE ON transfer_versions BEGIN SELECT RAISE(ABORT,'Negotiation history is immutable'); END;
CREATE TRIGGER transfer_versions_no_delete BEFORE DELETE ON transfer_versions BEGIN SELECT RAISE(ABORT,'Negotiation history is immutable'); END;
CREATE TRIGGER transfer_headers_no_update BEFORE UPDATE ON transfer_negotiations BEGIN SELECT RAISE(ABORT,'Negotiation identity is immutable'); END;
CREATE TRIGGER transfer_headers_no_delete BEFORE DELETE ON transfer_negotiations BEGIN SELECT RAISE(ABORT,'Negotiation identity is immutable'); END;

CREATE INDEX transfer_buyer_career ON transfer_negotiations(career_id,buyer_id);
CREATE TRIGGER transfer_version_identity BEFORE INSERT ON transfer_versions
WHEN json_extract(NEW.data_json,'$.Career') IS NOT (SELECT career_id FROM transfer_negotiations WHERE id=NEW.negotiation_id)
 OR json_extract(NEW.data_json,'$.Buyer') IS NOT (SELECT buyer_id FROM transfer_negotiations WHERE id=NEW.negotiation_id)
 OR json_extract(NEW.data_json,'$.Seller') IS NOT (SELECT seller_id FROM transfer_negotiations WHERE id=NEW.negotiation_id)
 OR json_extract(NEW.data_json,'$.Target') IS NOT (SELECT player_id FROM transfer_negotiations WHERE id=NEW.negotiation_id)
BEGIN SELECT RAISE(ABORT,'Negotiation version identity mismatch'); END;
