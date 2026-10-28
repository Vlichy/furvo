using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using Furvo.Core.Domain;
namespace Furvo.Core.Lineups;
/// <summary>Criterio provisional visible, separado de calidad oficial. Sin atributos ocultos ni aleatoriedad global.</summary>
public static class LineupSelection
{
    public const string PolicyVersion="lineup-selection-v1";
    public static string SeedFor(Guid career)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("FURVO/lineup/v1/"+career.ToString("D")))).ToLowerInvariant()[..16];
    public static IReadOnlyList<string> Relevant(PlayerRole role)=>(role switch{
        PlayerRole.Goalkeeper=>"reflexes one_on_ones goalkeeping_positioning",PlayerRole.CentreBack=>"marking tackling defensive_positioning",
        PlayerRole.RightBack or PlayerRole.LeftBack=>"tackling pace crossing",PlayerRole.DefensiveMidfielder=>"anticipation defensive_positioning short_passing",
        PlayerRole.CentralMidfielder=>"short_passing vision decisions",PlayerRole.AttackingMidfielder=>"vision technique creativity",
        PlayerRole.RightMidfielder or PlayerRole.LeftMidfielder=>"crossing short_passing stamina",PlayerRole.RightWinger or PlayerRole.LeftWinger=>"dribbling acceleration off_the_ball",
        PlayerRole.Striker=>"finishing off_the_ball composure",PlayerRole.SecondStriker=>"first_touch short_passing finishing",_=>throw new ArgumentOutOfRangeException(nameof(role))}).Split(' ');
    public static int Compatibility(LineupPlayer p,PlayerRole role)=>p.Primary==role?0:p.Secondary.Contains(role)?1:
        p.Primary==PlayerRole.Goalkeeper||role==PlayerRole.Goalkeeper?99:2;
    public static int ProvisionalScore(LineupPlayer player,PlayerRole role)=>Relevant(role).Sum(code=>player.Visible.TryGetValue(code,out var value)&&value is >=1 and <=20?value:throw new InvalidDataException("Atributo público incompleto: "+code));
    public static IReadOnlyList<LineupPlayer> Candidates(IEnumerable<LineupPlayer> players,PlayerRole role)=>Array.AsReadOnly(players.Where(p=>Compatibility(p,role)<2)
        .OrderBy(p=>Compatibility(p,role)).ThenByDescending(p=>ProvisionalScore(p,role)).ThenBy(p=>p.Name,StringComparer.OrdinalIgnoreCase).ThenBy(p=>p.Id.ToString(),StringComparer.Ordinal).ToArray());
    private struct SplitMix64(ulong seed)
    {
        private ulong _state=seed;
        public ulong Next(){unchecked{var z=(_state+=0x9E3779B97F4A7C15UL);z=(z^(z>>30))*0xBF58476D1CE4E5B9UL;z=(z^(z>>27))*0x94D049BB133111EBUL;return z^(z>>31);}}
    }
    public static CareerLineup Generate(ClubId club,string seed,FormationDefinition formation,IReadOnlyList<LineupPlayer> squad,int benchLimit=7,CareerLineup? previous=null)
    {
        FormationCatalog.Validate(formation);if(squad.Count<11||squad.Select(p=>p.Id).Distinct().Count()!=squad.Count||benchLimit<0||benchLimit>squad.Count-11)throw new InvalidDataException("Plantilla insuficiente o repetida.");
        if(previous is not null&&previous.Formation.Id==formation.Id&&previous.Formation.Slots.SequenceEqual(formation.Slots)){Validate(previous,squad);return previous;}
        var random=new SplitMix64(ulong.Parse(seed,NumberStyles.HexNumber,CultureInfo.InvariantCulture));
        var assignments=new List<LineupMember>();var used=new HashSet<PlayerId>();
        // Al cambiar esquema se conservan primero titulares en la misma demarcación compatible.
        if(previous is not null)
            foreach(var slot in formation.Slots)
            {
                var old=previous.Members.Where(m=>m.Role==LineupRole.Starter&&!used.Contains(m.PlayerId)&&previous.Formation.Slots.Single(s=>s.Id==m.SlotId).Role==slot.Role)
                    .OrderByDescending(m=>m.SlotId==slot.Id).ThenBy(m=>m.SlotId,StringComparer.Ordinal).FirstOrDefault(m=>squad.Any(p=>p.Id==m.PlayerId&&Compatibility(p,slot.Role)<2));
                if(old is not null){assignments.Add(old with{SlotId=slot.Id});used.Add(old.PlayerId);}
            }
        var slots=formation.Slots.Where(s=>!assignments.Any(m=>m.SlotId==s.Id)).ToArray();var players=squad.Where(p=>!used.Contains(p.Id)).OrderBy(p=>p.Id.ToString(),StringComparer.Ordinal).ToArray();
        var costs=new long[slots.Length,players.Length];
        for(var i=0;i<slots.Length;i++)for(var j=0;j<players.Length;j++)
        {var rank=Compatibility(players[j],slots[i].Role);var penalty=rank switch{0=>0L,1=>1L,2=>100L,_=>10000L};costs[i,j]=penalty*1_000_000_000L-ProvisionalScore(players[j],slots[i].Role)*100_000L+(long)(random.Next()&65535);}
        var matches=MinimumAssignment(costs);
        for(var i=0;i<slots.Length;i++){var p=players[matches[i]];if(Compatibility(p,slots[i].Role)>=99)throw new InvalidDataException("No hay portero elegible.");assignments.Add(new(p.Id,LineupRole.Starter,slots[i].Id));used.Add(p.Id);}
        var remaining=squad.Where(p=>!used.Contains(p.Id)).OrderBy(p=>p.Id.ToString(),StringComparer.Ordinal).ToArray();
        var priorBench=previous?.Members.Where(m=>m.Role==LineupRole.Substitute).Select(m=>m.PlayerId).ToHashSet()??new();
        var bench=new HashSet<PlayerId>();
        if(benchLimit>0&&remaining.Any(p=>p.Primary==PlayerRole.Goalkeeper))bench.Add(remaining.Where(p=>p.Primary==PlayerRole.Goalkeeper).OrderByDescending(p=>ProvisionalScore(p,p.Primary)).ThenBy(p=>p.Id.ToString(),StringComparer.Ordinal).First().Id);
        foreach(var p in remaining.OrderByDescending(p=>priorBench.Contains(p.Id)).ThenByDescending(p=>ProvisionalScore(p,p.Primary)).ThenBy(p=>p.Id.ToString(),StringComparer.Ordinal))if(bench.Count<benchLimit)bench.Add(p.Id);
        assignments.AddRange(remaining.Select(p=>new LineupMember(p.Id,bench.Contains(p.Id)?LineupRole.Substitute:LineupRole.Reserve,null)));
        var result=new CareerLineup(club,seed,formation,benchLimit,previous?.Revision??0,Array.AsReadOnly(assignments.OrderBy(m=>m.PlayerId.ToString(),StringComparer.Ordinal).ToArray()));Validate(result,squad);return result;
    }
    // Asignación mínima rectangular (húngaro): evita que una selección codiciosa bloquee puestos elegibles.
    private static int[] MinimumAssignment(long[,] costs)
    {
        var n=costs.GetLength(0);var m=costs.GetLength(1);var u=new long[n+1];var v=new long[m+1];var p=new int[m+1];var way=new int[m+1];
        for(var i=1;i<=n;i++)
        {
            p[0]=i;var j0=0;var min=Enumerable.Repeat(long.MaxValue/4,m+1).ToArray();var used=new bool[m+1];
            do{used[j0]=true;var i0=p[j0];var delta=long.MaxValue/4;var j1=0;
                for(var j=1;j<=m;j++)if(!used[j]){var cur=costs[i0-1,j-1]-u[i0]-v[j];if(cur<min[j]){min[j]=cur;way[j]=j0;}if(min[j]<delta){delta=min[j];j1=j;}}
                for(var j=0;j<=m;j++)if(used[j]){u[p[j]]+=delta;v[j]-=delta;}else min[j]-=delta;j0=j1;
            }while(p[j0]!=0);
            do{var j1=way[j0];p[j0]=p[j1];j0=j1;}while(j0!=0);
        }
        var result=new int[n];for(var j=1;j<=m;j++)if(p[j]>0)result[p[j]-1]=j-1;return result;
    }
    public static void Validate(CareerLineup lineup,IReadOnlyList<LineupPlayer> squad)
    {
        FormationCatalog.Validate(lineup.Formation);
        if(lineup.Seed.Length!=16||!ulong.TryParse(lineup.Seed,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out _)||lineup.Revision<0||lineup.Members.Count!=squad.Count||lineup.Members.Select(m=>m.PlayerId).Distinct().Count()!=squad.Count||lineup.Members.Any(m=>!squad.Any(p=>p.Id==m.PlayerId)||!Enum.IsDefined(m.Role)))throw new InvalidDataException("Miembros o semilla de alineación no válidos.");
        var starters=lineup.Members.Where(m=>m.Role==LineupRole.Starter).ToArray();
        if(starters.Length!=11||starters.Select(m=>m.SlotId).Distinct().Count()!=11||lineup.Members.Count(m=>m.Role==LineupRole.Substitute)!=lineup.BenchLimit||lineup.Members.Any(m=>(m.Role==LineupRole.Starter)!=(m.SlotId is not null)))throw new InvalidDataException("Once o banquillo no válidos.");
        foreach(var m in starters){var slot=lineup.Formation.Slots.SingleOrDefault(s=>s.Id==m.SlotId)??throw new InvalidDataException("Puesto desconocido.");if(Compatibility(squad.Single(p=>p.Id==m.PlayerId),slot.Role)>=99)throw new InvalidDataException("Portería incompatible.");}
    }
    public static CareerLineup Replace(CareerLineup current,string slotId,PlayerId candidate,IReadOnlyList<LineupPlayer> squad)
    {
        Validate(current,squad);var slot=current.Formation.Slots.Single(s=>s.Id==slotId);var selected=squad.Single(p=>p.Id==candidate);
        if(Compatibility(selected,slot.Role)>1)throw new InvalidOperationException("El candidato no tiene esa demarcación natural ni secundaria.");
        var old=current.Members.Single(m=>m.SlotId==slotId);var other=current.Members.Single(m=>m.PlayerId==candidate);if(old.PlayerId==candidate)return current;
        if(other.Role==LineupRole.Starter)
        {
            var otherSlot=current.Formation.Slots.Single(s=>s.Id==other.SlotId);
            if(Compatibility(squad.Single(p=>p.Id==old.PlayerId),otherSlot.Role)>1)throw new InvalidOperationException("Intercambio incompatible: el candidato ya ocupa otro puesto.");
            return current with{Members=Array.AsReadOnly(current.Members.Select(m=>m.PlayerId==candidate?m with{SlotId=slotId}:m.PlayerId==old.PlayerId?m with{SlotId=other.SlotId}:m).ToArray())};
        }
        var members=current.Members.Select(m=>m.PlayerId==candidate?new LineupMember(candidate,LineupRole.Starter,slotId):m.PlayerId==old.PlayerId?new LineupMember(old.PlayerId,other.Role,null):m).ToArray();
        var result=current with{Members=Array.AsReadOnly(members)};Validate(result,squad);return result;
    }
}
