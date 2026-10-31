using Furvo.Core.Domain;
using Furvo.Core.Generation;
using Furvo.Core.Lineups;
namespace Furvo.Core.Academy;
/// <summary>Datos ficticios reproducibles. Semilla por equipo y plaza; no usa identidades de personas reales.</summary>
public static class AcademyGenerator
{
    public const string Version="academy-2026-v1";
    public static Guid Identity(string key)=>DeterministicRandom.Identity("furvo/academy/identity/v1/"+key);
    public static IReadOnlyList<Player> Generate(AcademyTeam team,IReadOnlyList<NameCulture> cultures,ISet<string> usedNames)
    {
        var players=new List<Player>();var culture=cultures.Single(c=>c.Country=="España");
        PlayerRole[] roles=[PlayerRole.Goalkeeper,PlayerRole.Goalkeeper,PlayerRole.CentreBack,PlayerRole.CentreBack,PlayerRole.RightBack,PlayerRole.LeftBack,PlayerRole.CentreBack,PlayerRole.RightBack,PlayerRole.LeftBack,PlayerRole.DefensiveMidfielder,PlayerRole.CentralMidfielder,PlayerRole.CentralMidfielder,PlayerRole.AttackingMidfielder,PlayerRole.RightMidfielder,PlayerRole.LeftMidfielder,PlayerRole.RightWinger,PlayerRole.LeftWinger,PlayerRole.Striker,PlayerRole.Striker,PlayerRole.SecondStriker,PlayerRole.CentralMidfielder,PlayerRole.CentreBack];
        if(team.Size is <18 or >22)throw new InvalidDataException("Tamaño editorial no configurado.");
        for(var slot=0;slot<team.Size;slot++){
            var key=team.Id+"/player/"+slot;var rng=new DeterministicRandom(Version+"/"+key);string name;var attempts=0;
            do {name=culture.GivenNames[rng.Next(0,culture.GivenNames.Count)]+" "+culture.FamilyNames[rng.Next(0,culture.FamilyNames.Count)]+" "+culture.FamilyNames[rng.Next(0,culture.FamilyNames.Count)];if(++attempts>10000)throw new InvalidDataException("Repertorio de nombres agotado.");}while(!usedNames.Add(name));
            var role=roles[slot];var year=team.Category=="Juvenil"?rng.Next(2008,2011):rng.Next(2004,2008);var birth=new DateOnly(year,rng.Next(1,13),rng.Next(1,29));
            var secondary=role switch{PlayerRole.RightBack=>new[]{PlayerRole.RightMidfielder},PlayerRole.LeftBack=>new[]{PlayerRole.LeftMidfielder},PlayerRole.CentralMidfielder=>new[]{PlayerRole.DefensiveMidfielder},PlayerRole.AttackingMidfielder=>new[]{PlayerRole.CentralMidfielder},PlayerRole.RightWinger=>new[]{PlayerRole.RightMidfielder},PlayerRole.LeftWinger=>new[]{PlayerRole.LeftMidfielder},PlayerRole.Striker=>new[]{PlayerRole.SecondStriker},_=>Array.Empty<PlayerRole>()};
            var relevant=LineupSelection.Relevant(role);var values=PlayerAttributeCatalog.All.ToDictionary(d=>d.Code,d=>rng.Next(1,team.Category=="Juvenil"?13:16)+(relevant.Contains(d.Code)?2:0));
            players.Add(new(new PlayerId(Identity(key)),name,birth,culture.Country,PlayerPositions.GroupOf(role),new PlayerAttributes(values),new PlayerPositions(role,secondary)));
        }
        return players.AsReadOnly();
    }
    public static AcademyStaff Coach(AcademyTeam team,IReadOnlyList<NameCulture> cultures){var c=cultures.Single(c=>c.Country=="España");var r=new DeterministicRandom(Version+"/coach/"+team.Id);return new(Identity(team.Id+"/coach"),team.Id,c.GivenNames[r.Next(0,c.GivenNames.Count)]+" "+c.FamilyNames[r.Next(0,c.FamilyNames.Count)]+" "+c.FamilyNames[r.Next(0,c.FamilyNames.Count)],"HeadCoach","Institutional");}
}
