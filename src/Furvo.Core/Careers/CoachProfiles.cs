using Furvo.Core.Domain;

namespace Furvo.Core.Careers;

public readonly record struct CoachProfileId
{
    public Guid Value { get; }
    public CoachProfileId(Guid value) { if (value == Guid.Empty) throw new ArgumentException("Identidad vacía."); Value = value; }
    public static CoachProfileId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("D");
}
// Experienced=3 se conserva únicamente para lectura y reutilización de perfiles históricos.
public enum CoachExperience { Novice = 1, FormerPlayer = 2, Experienced = 3 }
public static class NewCoachBackgrounds
{
    public static IReadOnlyList<CoachExperience> Options { get; }=Array.AsReadOnly(new[]{CoachExperience.Novice,CoachExperience.FormerPlayer});
}

/// <summary>Catálogo inicial estable de códigos ISO, no condicionado por cultura del sistema ni nacionalidades de una plantilla.</summary>
public static class CoachNationalities
{
    public static IReadOnlyList<string> Codes { get; } = Array.AsReadOnly("ES PT FR DE IT GB IE NL BE CH AT DK NO SE FI HR RS GR PL UA TR US CA MX AR BR UY CL CO EC PE VE BO PY CR JP KR AU MA DZ SN CM NG GH ZA".Split(' '));
}

/// <summary>Identidad reutilizable y validada. ReferenceDate es fecha explícita de validación, nunca DateTime.Now.</summary>
public sealed record CoachProfile
{
    public CoachProfileId Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string FullName => FirstName + " " + LastName;
    public DateOnly BirthDate { get; }
    public string NationalityCode { get; }
    public string BirthCity { get; }
    public CoachExperience Experience { get; }
    public string AvatarKey { get; }
    public DateOnly ReferenceDate { get; }
    public CoachProfile(CoachProfileId id, string firstName, string lastName, DateOnly birthDate, string nationalityCode,
        string birthCity, CoachExperience experience, string avatarKey, DateOnly referenceDate)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("Identidad vacía."); Id = id;
        FirstName = Text(firstName,60); LastName = Text(lastName,100); BirthCity = Text(birthCity,100);
        if (!CoachNationalities.Codes.Contains(nationalityCode,StringComparer.Ordinal)) throw new ArgumentException("Nacionalidad no válida.");
        if (!Enum.IsDefined(experience)) throw new ArgumentException("Antecedente no válido.");
        if (avatarKey != "generic-v1") throw new ArgumentException("Avatar no disponible.");
        BirthDate = birthDate; NationalityCode = nationalityCode; Experience = experience; AvatarKey = avatarKey; ReferenceDate = referenceDate;
        ValidateAge(referenceDate);
    }
    private static string Text(string text,int max)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length > max || text.Any(char.IsControl)) throw new ArgumentException("Texto incompleto o demasiado largo.");
        return text.Trim();
    }
    public int AgeOn(DateOnly date)
    {
        if (date < BirthDate) throw new ArgumentException("Nacimiento posterior a la fecha de referencia.");
        var age=date.Year-BirthDate.Year; return BirthDate.AddYears(age)>date?age-1:age;
    }
    public void ValidateAge(DateOnly date) { var age=AgeOn(date); if(age is <18 or >100) throw new ArgumentException("La edad debe estar entre 18 y 100 años."); }
}
public interface ICoachProfileStore
{
    IReadOnlyList<CoachProfile> List();
    CoachProfile Create(CoachProfile profile);
}
