-- Esquema exclusivo de perfiles; no se ejecuta en mundos ni partidas.
CREATE TABLE profile_nationalities(code TEXT PRIMARY KEY NOT NULL CHECK(length(code)=2));
CREATE TABLE coach_profiles(
 id TEXT PRIMARY KEY NOT NULL CHECK(length(id)=36),
 first_name TEXT NOT NULL CHECK(length(trim(first_name)) BETWEEN 1 AND 60),
 last_name TEXT NOT NULL CHECK(length(trim(last_name)) BETWEEN 1 AND 100),
 birth_date TEXT NOT NULL CHECK(length(birth_date)=10 AND date(birth_date,'+0 days') IS birth_date),
 nationality_code TEXT NOT NULL REFERENCES profile_nationalities(code),
 birth_city TEXT NOT NULL CHECK(length(trim(birth_city)) BETWEEN 1 AND 100),
 experience INTEGER NOT NULL CHECK(experience BETWEEN 1 AND 3),
 avatar_key TEXT NOT NULL CHECK(avatar_key='generic-v1'),
 reference_date TEXT NOT NULL CHECK(length(reference_date)=10 AND date(reference_date,'+0 days') IS reference_date),
 -- La edad usa aniversario ajustado a 28/02 en años no bisiestos, igual que DateOnly.AddYears.
 CHECK(CAST(substr(reference_date,1,4) AS INTEGER)-CAST(substr(birth_date,1,4) AS INTEGER)-
 CASE WHEN substr(reference_date,6)<CASE
 WHEN substr(birth_date,6)='02-29' AND date(substr(reference_date,1,4)||'-02-29','+0 days') IS NOT substr(reference_date,1,4)||'-02-29'
 THEN '02-28' ELSE substr(birth_date,6) END THEN 1 ELSE 0 END BETWEEN 18 AND 100)
);
