-- Registro técnico independiente. No modifica caja, contratos, presupuestos ni el esquema del mundo.
CREATE TABLE financial_operation_schema(
 singleton INTEGER PRIMARY KEY CHECK(singleton=1),
 version INTEGER NOT NULL CHECK(version=1)
);
INSERT INTO financial_operation_schema VALUES(1,1);
CREATE TABLE financial_operations(
 id TEXT PRIMARY KEY NOT NULL CHECK(length(id)=36),
 kind TEXT NOT NULL CHECK(length(trim(kind))>0),
 origin TEXT NOT NULL CHECK(length(trim(origin))>0),
 request_hash TEXT NOT NULL CHECK(length(request_hash)=64),
 result_json TEXT NOT NULL CHECK(json_valid(result_json)),
 UNIQUE(kind,origin)
);
CREATE TRIGGER financial_operations_no_update BEFORE UPDATE ON financial_operations
 BEGIN SELECT RAISE(ABORT,'Confirmed operation is immutable'); END;
CREATE TRIGGER financial_operations_no_delete BEFORE DELETE ON financial_operations
 BEGIN SELECT RAISE(ABORT,'Confirmed operation is immutable'); END;
