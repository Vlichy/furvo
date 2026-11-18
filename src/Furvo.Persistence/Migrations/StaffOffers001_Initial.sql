CREATE TABLE staff_offer_versions(
 id TEXT NOT NULL,
 revision INTEGER NOT NULL CHECK(revision>0),
 employee_id TEXT NOT NULL REFERENCES academy_staff(id),
 buyer_id TEXT NOT NULL REFERENCES clubs(id),
 payload TEXT NOT NULL CHECK(json_valid(payload)),
 PRIMARY KEY(id,revision)
);
