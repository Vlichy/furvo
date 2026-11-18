CREATE TABLE staff_professions(
 employee_id TEXT PRIMARY KEY REFERENCES academy_staff(id),
 version INTEGER NOT NULL CHECK(version=1),
 profile_json TEXT NOT NULL CHECK(json_valid(profile_json))
);
CREATE TABLE staff_employments(
 id TEXT PRIMARY KEY,
 employee_id TEXT NOT NULL UNIQUE REFERENCES staff_professions(employee_id),
 club_id TEXT NOT NULL REFERENCES clubs(id),
 contract_json TEXT NOT NULL CHECK(json_valid(contract_json))
);
