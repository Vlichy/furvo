using System.Globalization;
using Furvo.Core.Careers;
using Microsoft.Data.Sqlite;

namespace Furvo.Persistence;

/// <summary>Almacén independiente de perfiles. Solo las altas explícitas crean archivo; no existe upsert ni edición silenciosa.</summary>
public sealed class SqliteCoachProfileStore : ICoachProfileStore
{
    private const int ApplicationId=1179996752;
    private readonly string _path;
    public SqliteCoachProfileStore(string path)=>_path=WorldDatabase.PathFor(path);
    public IReadOnlyList<CoachProfile> List()
    {
        if(!File.Exists(_path))return Array.Empty<CoachProfile>();
        using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);Require(c,t);
        return ReadAll(c,t);
    }
    public CoachProfile Create(CoachProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);profile.ValidateAge(profile.ReferenceDate);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        using var c=WorldDatabase.Open(_path,SqliteOpenMode.ReadWriteCreate);using var t=c.BeginTransaction();
        using(var command=WorldDatabase.Command(c,t,"PRAGMA user_version;"))
        {
            if(Convert.ToInt32(command.ExecuteScalar())==0)
            {
                command.CommandText="SELECT count(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
                if(Convert.ToInt32(command.ExecuteScalar())!=0)throw new InvalidDataException("La base no está vacía ni pertenece a perfiles.");
                command.CommandText="PRAGMA application_id;"; if(Convert.ToInt32(command.ExecuteScalar())!=0)throw new InvalidDataException("Base ajena.");
                CareerJson.Migrate(c,t,"Profiles001_Initial.sql");
                foreach(var code in CoachNationalities.Codes)
                {using var insert=WorldDatabase.Command(c,t,"INSERT INTO profile_nationalities VALUES($code);",("$code",code));insert.ExecuteNonQuery();}
                command.CommandText=$"PRAGMA application_id={ApplicationId}; PRAGMA user_version=1;";command.ExecuteNonQuery();
            }
        }
        Require(c,t);var existing=ReadAll(c,t).SingleOrDefault(p=>p.Id==profile.Id);
        if(existing is not null)
        {
            if(existing!=profile)throw new InvalidOperationException("Ese identificador pertenece a otro perfil; no se sobrescribe.");
            t.Commit();return existing;
        }
        if(!NewCoachBackgrounds.Options.Contains(profile.Experience))throw new ArgumentException("Antecedente no disponible para nuevos perfiles.");
        using(var insert=WorldDatabase.Command(c,t,"INSERT INTO coach_profiles VALUES($id,$first,$last,$birth,$nation,$city,$experience,$avatar,$reference);",
            ("$id",profile.Id.ToString()),("$first",profile.FirstName),("$last",profile.LastName),("$birth",profile.BirthDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),
            ("$nation",profile.NationalityCode),("$city",profile.BirthCity),("$experience",(int)profile.Experience),("$avatar",profile.AvatarKey),("$reference",profile.ReferenceDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture))))insert.ExecuteNonQuery();
        t.Commit();return profile;
    }
    private static void Require(SqliteConnection c,SqliteTransaction t)
    {
        using var cmd=WorldDatabase.Command(c,t,"PRAGMA application_id;");if(Convert.ToInt32(cmd.ExecuteScalar())!=ApplicationId)throw new InvalidDataException("No es un almacén de perfiles Furvo.");
        cmd.CommandText="PRAGMA user_version;";if(Convert.ToInt32(cmd.ExecuteScalar())!=1)throw new NotSupportedException("Versión de perfiles no compatible.");
    }
    private static IReadOnlyList<CoachProfile> ReadAll(SqliteConnection c,SqliteTransaction t)
    {
        using var cmd=WorldDatabase.Command(c,t,"SELECT id,first_name,last_name,birth_date,nationality_code,birth_city,experience,avatar_key,reference_date FROM coach_profiles ORDER BY first_name,last_name,id;");
        using var r=cmd.ExecuteReader();var rows=new List<CoachProfile>();
        while(r.Read())rows.Add(new CoachProfile(new CoachProfileId(Guid.Parse(r.GetString(0))),r.GetString(1),r.GetString(2),DateOnly.ParseExact(r.GetString(3),"yyyy-MM-dd",CultureInfo.InvariantCulture),r.GetString(4),r.GetString(5),(CoachExperience)r.GetInt32(6),r.GetString(7),DateOnly.ParseExact(r.GetString(8),"yyyy-MM-dd",CultureInfo.InvariantCulture)));
        return rows.AsReadOnly();
    }
}
