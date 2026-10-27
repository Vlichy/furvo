namespace Furvo.Core.Domain;

// Perfil descriptivo; no contiene calidad de plantilla ni reglas de simulación.
/// <summary>Perfil institucional descriptivo. Sus campos no calculan reputación ni capacidad deportiva de la plantilla.</summary>
public sealed record ClubProfile
{
    public string ShortName { get; }
    public string Region { get; }
    public int FoundationYear { get; }
    public string SportingIdentity { get; }
    public string PrimaryColor { get; }
    public string SecondaryColor { get; }
    public string StadiumName { get; }
    public int StadiumCapacity { get; }
    public string InitialCategory { get; }

    /// <summary>Valida un perfil completo; un año representable y una capacidad positiva evitan datos institucionales incoherentes.</summary>
    public ClubProfile(string shortName, string region, int foundationYear, string sportingIdentity,
        string primaryColor, string secondaryColor, string stadiumName, int stadiumCapacity, string initialCategory)
    {
        ShortName = DomainRules.Text(shortName);
        Region = DomainRules.Text(region);
        if (foundationYear is < 1 or > 9999) throw new ArgumentOutOfRangeException(nameof(foundationYear));
        FoundationYear = foundationYear;
        SportingIdentity = DomainRules.Text(sportingIdentity);
        PrimaryColor = DomainRules.Text(primaryColor);
        SecondaryColor = DomainRules.Text(secondaryColor);
        StadiumName = DomainRules.Text(stadiumName);
        if (stadiumCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(stadiumCapacity));
        StadiumCapacity = stadiumCapacity;
        InitialCategory = DomainRules.Text(initialCategory);
    }
}
