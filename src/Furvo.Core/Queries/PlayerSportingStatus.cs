namespace Furvo.Core.Queries;
public enum SportingTone { Neutral,Positive,Intermediate,Negative }
public enum PlayerMorale { VeryHigh,High,Normal,Low,VeryLow }
/// <summary>Dato de presentación recibido de un futuro modelo real. Sin umbrales deportivos ni conversión de atributos.</summary>
public sealed record SegmentedStatus
{
    public int? Segments {get;}
    public int? Percentage {get;}
    public SportingTone Tone {get;}
    public SegmentedStatus(int? segments=null,int? percentage=null,SportingTone tone=SportingTone.Neutral)
    {
        if(segments is <0 or >5 || percentage is <0 or >100 || !Enum.IsDefined(tone))throw new ArgumentOutOfRangeException(nameof(segments));
        Segments=segments;Percentage=percentage;Tone=tone;
    }
}
public sealed record PlayerAppearance(Guid MatchId,DateOnly Date,int MinutesPlayed,decimal? Rating,SportingTone? Performance);
public sealed record PlayerSeasonStatistics(int Played,int Goals,int Assists,decimal? Average);
/// <summary>Contrato público para futuras fuentes persistidas. No contiene atributos, medias de capacidad ni reglas de fatiga.</summary>
public sealed record PlayerSportingStatus
{
    public SegmentedStatus Quality {get;init;}=new();
    public SegmentedStatus Physical {get;init;}=new();
    public SegmentedStatus Rhythm {get;init;}=new();
    public PlayerMorale? Morale {get;init;}
    public PlayerSeasonStatistics? Season {get;init;}
    public IReadOnlyList<PlayerAppearance> Appearances {get;init;}=Array.Empty<PlayerAppearance>();
    /// <summary>Solo para el estado inicial confirmado sin resultados; una carrera de fecha posterior conserva desconocidos.</summary>
    public static PlayerSportingStatus Initial(bool verifiedNoAppearances)=>new(){Season=verifiedNoAppearances?new(0,0,0,null):null};
    public IReadOnlyList<PlayerAppearance?> LastFive()
    {
        if(Appearances.Any(a=>a.MatchId==Guid.Empty || a.MinutesPlayed<0 || a.Rating is <0 || a.Performance is {} tone&&!Enum.IsDefined(tone)) ||
           Appearances.Select(a=>a.MatchId).Distinct().Count()!=Appearances.Count)throw new InvalidDataException("Actuaciones no válidas.");
        var played=Appearances.Where(a=>a.MinutesPlayed>0).OrderBy(a=>a.Date).ThenBy(a=>a.MatchId).TakeLast(5).Cast<PlayerAppearance?>().ToList();
        while(played.Count<5)played.Add(null);return played.AsReadOnly();
    }
}
