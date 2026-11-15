using System.Text.Json;
using Furvo.Core.Academy;
using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Queries;
using Furvo.Core.Tactics;
using Furvo.Core.Worlds;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
/// <summary>Estado aislado de cantera. Leer nunca inicializa ni migra una carrera antigua.</summary>
public sealed class SqliteAcademyStore(string path)
{
    private readonly string _path=WorldDatabase.PathFor(path);
    private sealed record PlayerData(Guid Id,string Name,DateOnly BirthDate,string Nationality,PlayerRole Primary,PlayerRole[] Secondary,Dictionary<string,int> Attributes)
    {
        public static PlayerData From(Player p)=>new(p.Id.Value,p.Name,p.BirthDate!.Value,p.Nationality!,p.Positions!.Primary,p.Positions.Secondary.ToArray(),p.Attributes.AllValues.ToDictionary());
        public Player Player()=>new(new PlayerId(Id),Name,BirthDate,Nationality,PlayerPositions.GroupOf(Primary),new PlayerAttributes(Attributes),new PlayerPositions(Primary,Secondary));
    }
    internal static Player DecodePlayer(string json)=>CareerJson.Read<PlayerData>(json).Player();
    private sealed record Seed(AcademyTeam Team,AcademyStaff Coach,Player[] Players);
    private static readonly Lazy<Seed[]> Seeds=new(()=>{
        var settings=InitialSquads2026.Settings();var used=InitialSquads2026.Generate().Players.Select(p=>p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return AcademyCatalog.Teams.OrderBy(t=>t.Id).Select(t=>new Seed(t,AcademyGenerator.Coach(t,settings.Cultures),AcademyGenerator.Generate(t,settings.Cultures,used).ToArray())).ToArray();
    });
    internal static bool HasSchema(SqliteConnection c,SqliteTransaction t){using var q=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE type='table' AND name='academy_schema_version'");if(Convert.ToInt32(q.ExecuteScalar())==0)return false;q.CommandText="SELECT version FROM academy_schema_version WHERE singleton=1";if(Convert.ToInt32(q.ExecuteScalar())!=1)throw new NotSupportedException("Versión de cantera no compatible.");return true;}
    public bool IsInitialized{get{using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);return HasSchema(c,t);}}
    internal static void Initialize(SqliteConnection c,SqliteTransaction t,WorldData world)
    {
        if(HasSchema(c,t))return;if(WorldDatabase.Info(c,t).Kind!=WorldFileKind.Save)throw new InvalidOperationException("La cantera solo se inicializa en una carrera.");
        if(!AcademyCatalog.Teams.Select(x=>x.ClubId).Distinct().ToHashSet().SetEquals(world.Clubs.Select(x=>x.Id)))throw new InvalidDataException("Catálogo de cantera ajeno al mundo.");
        CareerJson.Migrate(c,t,"Academy001_Initial.sql");using(var meta=WorldDatabase.Command(c,t,"INSERT INTO academy_schema_version VALUES(1,1,$version,$hash)",("$version",AcademyGenerator.Version),("$hash",AcademyCatalog.Hash)))meta.ExecuteNonQuery();
        foreach(var seed in Seeds.Value){var team=seed.Team;var publicPlayers=seed.Players.Select(p=>LineupPlayer.From(WorldQueries.ProjectPlayer(p,new(team.ClubId,team.Name,null,null),world.Seasons.Single().StartDate))).ToArray();
            var lineup=LineupSelection.Generate(team.ClubId,LineupSelection.SeedFor(team.Id),FormationCatalog.Initial(team.ClubId),publicPlayers,7);
            var rosters=Enum.GetValues<TacticalPlanKind>().ToDictionary(k=>k,_=>new AcademyAssignments(new PlanRoster(lineup.Seed,lineup.BenchLimit,lineup.Members),lineup.Formation.Slots.ToDictionary(s=>s.Id,s=>s.Role)));
            using(var row=WorldDatabase.Command(c,t,"INSERT INTO academy_teams VALUES($id,$club,$name,$category,$level,$size,$competition,2,1,0,$rosters,NULL,$common)",("$id",team.Id.ToString("D")),("$club",team.ClubId.ToString()),("$name",team.Name),("$category",team.Category),("$level",team.Level),("$size",team.Size),("$competition",team.Competition),("$rosters",CareerJson.Write(rosters)),("$common",CareerJson.Write(TacticalSettings.Undefined(team.ClubId)))))row.ExecuteNonQuery();
            using(var staff=WorldDatabase.Command(c,t,"INSERT INTO academy_staff VALUES($id,$team,$name,$role,$link)",("$id",seed.Coach.Id.ToString("D")),("$team",team.Id.ToString("D")),("$name",seed.Coach.Name),("$role",seed.Coach.Role),("$link",seed.Coach.LinkState)))staff.ExecuteNonQuery();
            foreach(var player in seed.Players){using var q=WorldDatabase.Command(c,t,"INSERT INTO academy_players VALUES($id,$team,$club,$json)",("$id",player.Id.ToString()),("$team",team.Id.ToString("D")),("$club",team.ClubId.ToString()),("$json",CareerJson.Write(PlayerData.From(player))));q.ExecuteNonQuery();}
        }
    }
    public void InitializeMissing(){var world=new SqliteWorldStore(_path).Load();using var c=WorldDatabase.Open(_path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();Initialize(c,t,world);t.Commit();}
    private static IReadOnlyDictionary<TacticalPlanKind,AcademyAssignments> ReadRosters(string json,ClubId club)
    {
        using var doc=JsonDocument.Parse(json);var first=doc.RootElement.EnumerateObject().FirstOrDefault();
        if(first.Value.ValueKind==JsonValueKind.Object&&first.Value.TryGetProperty("Roster",out _))return CareerJson.Read<Dictionary<TacticalPlanKind,AcademyAssignments>>(json);
        // Compatibilidad con instantáneas de previsualización: la lectura no escribe ni cambia miembros.
        var roles=FormationCatalog.Initial(club).Slots.ToDictionary(s=>s.Id,s=>s.Role);return CareerJson.Read<Dictionary<TacticalPlanKind,PlanRoster>>(json).ToDictionary(x=>x.Key,x=>new AcademyAssignments(x.Value,roles));
    }
    private static AcademyTeamState Read(SqliteConnection c,SqliteTransaction t,Guid id)
    {
        var owner=SqliteLineupStore.Owner(c,t);if(owner.Club is null)throw new InvalidOperationException("El entrenador no tiene club.");if(!HasSchema(c,t))throw new InvalidOperationException("La cantera de esta carrera aún no está inicializada.");
        using var q=WorldDatabase.Command(c,t,"SELECT a.club_id,a.name,a.category,a.level,a.size,a.competition,a.management,a.tactic_mode,a.revision,a.rosters_json,a.own_json,a.common_json,s.id,s.name,s.role,s.link_state FROM academy_teams a JOIN academy_staff s ON s.team_id=a.id WHERE a.id=$id",("$id",id.ToString("D")));using var r=q.ExecuteReader();if(!r.Read())throw new KeyNotFoundException("Equipo de cantera inexistente.");var club=new ClubId(Guid.Parse(r.GetString(0)));if(club!=owner.Club)throw new InvalidOperationException("No dirige el club matriz de este equipo.");
        var team=new AcademyTeam(id,club,r.GetString(1),r.GetString(2),r.GetInt32(3),r.GetInt32(4),r.IsDBNull(5)?null:r.GetString(5));
        return new(team,new(Guid.Parse(r.GetString(12)),id,r.GetString(13),r.GetString(14),r.GetString(15)),(AcademyManagement)r.GetInt32(6),(AcademyTacticMode)r.GetInt32(7),r.GetInt32(8),ReadRosters(r.GetString(9),club),r.IsDBNull(10)?null:CareerJson.Read<TacticalPlanBook>(r.GetString(10)),UpgradeCommon(CareerJson.Read<TacticalSettings>(r.GetString(11))));
    }
    public AcademyTeamState Get(Guid id){using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);return Read(c,t,id);}
    public IReadOnlyList<AcademyTeamState> List(){using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);if(!HasSchema(c,t))return Array.Empty<AcademyTeamState>();var owner=SqliteLineupStore.Owner(c,t);if(owner.Club is null)return Array.Empty<AcademyTeamState>();var ids=new List<Guid>();using(var q=WorldDatabase.Command(c,t,"SELECT id FROM academy_teams WHERE club_id=$club ORDER BY category DESC,level,id",("$club",owner.Club.Value.ToString())))using(var r=q.ExecuteReader())while(r.Read())ids.Add(Guid.Parse(r.GetString(0)));return ids.Select(id=>Read(c,t,id)).ToArray();}
    public int PlayerCount(Guid id){if(SqliteSportsStore.HasAssignments(_path))return new SqliteClubSquadStore(_path).Players(id).Count;using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);_=Read(c,t,id);using var q=WorldDatabase.Command(c,t,"SELECT count(*) FROM academy_players WHERE team_id=$id",("$id",id.ToString("D")));return Convert.ToInt32(q.ExecuteScalar());}
    public IReadOnlyList<Player> Players(Guid id){if(SqliteSportsStore.HasAssignments(_path))return new SqliteClubSquadStore(_path).Players(id);using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);_=Read(c,t,id);var result=new List<Player>();using var q=WorldDatabase.Command(c,t,"SELECT data_json FROM academy_players WHERE team_id=$id ORDER BY id",("$id",id.ToString("D")));using var r=q.ExecuteReader();while(r.Read())result.Add(CareerJson.Read<PlayerData>(r.GetString(0)).Player());return result.AsReadOnly();}
    public PlayerDetails Player(Guid team,PlayerId player){var state=Get(team);using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);var date=SqliteLineupStore.Owner(c,t).Date;var p=Players(team).SingleOrDefault(p=>p.Id==player)??throw new KeyNotFoundException("El futbolista no pertenece a este equipo.");return WorldQueries.ProjectPlayer(p,new(state.Team.ClubId,state.Team.Name,null,null),date);}
    public IReadOnlyList<PlayerDetails> Details(Guid id){var state=Get(id);using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);var date=SqliteLineupStore.Owner(c,t).Date;return Players(id).Select(p=>WorldQueries.ProjectPlayer(p,new(state.Team.ClubId,state.Team.Name,null,null),date)).ToArray();}
    public IReadOnlyList<LineupPlayer> LineupPlayers(Guid id)=>Details(id).Select(LineupPlayer.From).ToArray();
    private static TacticalPlanBook Source(SqliteConnection c,SqliteTransaction t){if(SqliteTacticalPlanStore.ReadPersisted(c,t) is {} book)return book;var owner=SqliteLineupStore.Owner(c,t);var lineup=SqliteLineupStore.ReadLegacy(c,t)??throw new InvalidOperationException("Inicializa primero la alineación del primer equipo.");var settings=SqliteTacticsStore.ReadRaw(c,t)??TacticalSettings.Undefined(lineup.ClubId);return TacticalPlanRules.FromLegacy(owner.Save.Value,lineup,settings) with{Revision=checked(lineup.Revision+settings.Revision)};}
    internal static void Write(SqliteConnection c,SqliteTransaction t,AcademyTeamState state,int previous){using var q=WorldDatabase.Command(c,t,"UPDATE academy_teams SET management=$management,tactic_mode=$mode,revision=$revision,rosters_json=$rosters,own_json=$own,common_json=$common WHERE id=$id AND revision=$previous",("$management",(int)state.Management),("$mode",(int)state.TacticMode),("$revision",state.Revision),("$rosters",CareerJson.Write(state.Rosters)),("$own",state.OwnBook is null?null:CareerJson.Write(state.OwnBook)),("$common",CareerJson.Write(state.Common)),("$id",state.Team.Id.ToString("D")),("$previous",previous));if(q.ExecuteNonQuery()!=1)throw new InvalidOperationException("El equipo cambió en otra operación.");}
    public AcademyTeamState SetManagement(Guid id,AcademyManagement management,int revision){if(!Enum.IsDefined(management))throw new InvalidDataException("Gestión desconocida.");using var c=WorldDatabase.Open(_path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var old=Read(c,t,id);if(old.Revision!=revision)throw new InvalidOperationException("La configuración cambió.");if(management==AcademyManagement.Delegated&&old.Coach.LinkState!="Institutional")throw new InvalidOperationException("No hay entrenador de cantera disponible.");var next=old with{Management=management,Revision=old.Revision+1};Write(c,t,next,old.Revision);t.Commit();return next;}
    public AcademyTeamState SetTacticMode(Guid id,AcademyTacticMode mode,int revision,bool confirmed=false)
    {
        if(!Enum.IsDefined(mode))throw new InvalidDataException("Modelo táctico desconocido.");var players=LineupPlayers(id);using var c=WorldDatabase.Open(_path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var old=Read(c,t,id);if(old.Revision!=revision)throw new InvalidOperationException("La configuración cambió.");if(old.TacticMode==mode)return old;
        if(mode==AcademyTacticMode.Synchronized&&!confirmed)throw new InvalidOperationException("Confirma la sustitución de la táctica propia.");var source=Source(c,t);var current=AcademyTactics.Resolve(old,source,players);
        var next=old with{TacticMode=mode,Revision=old.Revision+1,Rosters=current.Plans.ToDictionary(p=>p.Kind,p=>new AcademyAssignments(p.Roster,p.WithoutBall.Positions.ToDictionary(s=>s.Id,s=>s.Role))),OwnBook=mode==AcademyTacticMode.Own?current with{Revision=old.Revision+1}:null};Write(c,t,next,old.Revision);t.Commit();return next;
    }
    private static TacticalSettings UpgradeCommon(TacticalSettings settings)=>settings with{Instructions=TacticalCatalog.UpgradeInstructions(settings.Instructions)};
    public TacticalPlanBook LoadPlans(Guid id){var players=LineupPlayers(id);using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);var state=Read(c,t,id);return AcademyTactics.Resolve(state,Source(c,t),players);}
    public TacticalSettings Settings(Guid id,TacticalPlanBook book,TacticalPlanKind kind){var state=Get(id);var p=book.Get(kind);return state.Common with{Revision=book.Revision,Mentality=p.Mentality,Instructions=p.Instructions};}
    public TacticalSettings SaveSynchronizedPersonnel(Guid id,TacticalPlanKind kind,TacticalSettings proposed)
    {
        var players=LineupPlayers(id);TacticalCatalog.Validate(proposed,players.Select(p=>p.Id).ToHashSet());using var c=WorldDatabase.Open(_path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var old=Read(c,t,id);var book=AcademyTactics.Resolve(old,Source(c,t),players);var p=book.Get(kind);
        if(old.TacticMode!=AcademyTacticMode.Synchronized||book.Revision!=proposed.Revision||proposed.ClubId!=book.ClubId||proposed.Mentality!=p.Mentality||!proposed.Instructions.OrderBy(v=>v.Key).SequenceEqual(p.Instructions.OrderBy(v=>v.Key)))throw new InvalidOperationException("No se modifican instrucciones heredadas.");
        var next=old with{Revision=old.Revision+1,Common=proposed with{Revision=0,Mentality=null,Instructions=TacticalSettings.Undefined(book.ClubId).Instructions}};Write(c,t,next,old.Revision);t.Commit();return next.Common with{Revision=book.Revision+1,Mentality=p.Mentality,Instructions=p.Instructions};
    }
    public TacticalPlanBook SavePlans(Guid id,TacticalPlanBook proposed,TacticalSettings? common=null)
    {
        var players=LineupPlayers(id);TacticalPlanRules.Validate(proposed,players);using var c=WorldDatabase.Open(_path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var old=Read(c,t,id);if(old.TacticMode!=AcademyTacticMode.Own)throw new InvalidOperationException("La táctica sincronizada es de solo consulta. Cambia a Propia para editar el esquema.");
        if(proposed.ClubId!=old.Team.ClubId||proposed.Revision!=old.Revision||proposed.Plans.Any(p=>p.Id!=old.OwnBook!.Get(p.Kind).Id))throw new InvalidOperationException("Identidad o revisión de cantera incompatible.");
        if(proposed.Plans.Any(p=>p.Roster.Seed!=old.OwnBook!.Get(p.Kind).Roster.Seed||p.Roster.BenchLimit!=old.OwnBook.Get(p.Kind).Roster.BenchLimit))throw new InvalidDataException("Semilla o banquillo de cantera incompatible.");
        if(old.EffectiveManagement==AcademyManagement.Delegated&&proposed.Plans.Any(p=>!p.Roster.Members.OrderBy(m=>m.PlayerId.ToString(),StringComparer.Ordinal).SequenceEqual(old.OwnBook!.Get(p.Kind).Roster.Members.OrderBy(m=>m.PlayerId.ToString(),StringComparer.Ordinal))))throw new InvalidOperationException("Las asignaciones de futbolistas están delegadas.");
        var nextCommon=common??old.Common;if(nextCommon.ClubId!=old.Team.ClubId)throw new InvalidDataException("Configuración de otro club.");TacticalCatalog.Validate(nextCommon,players.Select(p=>p.Id).ToHashSet());nextCommon=nextCommon with{Mentality=null,Instructions=TacticalSettings.Undefined(old.Team.ClubId).Instructions,Revision=0};var next=old with{Revision=old.Revision+1,OwnBook=proposed with{Revision=old.Revision+1},Rosters=proposed.Plans.ToDictionary(p=>p.Kind,p=>new AcademyAssignments(p.Roster,p.WithoutBall.Positions.ToDictionary(s=>s.Id,s=>s.Role))),Common=nextCommon};Write(c,t,next,old.Revision);t.Commit();return next.OwnBook!;
    }
    public CareerLineup SaveLineup(Guid id,TacticalPlanKind kind,PossessionPhase phase,CareerLineup proposed)
    {
        var players=LineupPlayers(id);LineupSelection.Validate(proposed,players);var state=Get(id);if(state.EffectiveManagement!=AcademyManagement.Direct)throw new InvalidOperationException("Las asignaciones de jugadores están delegadas al entrenador responsable.");
        var book=LoadPlans(id);if(book.Revision!=proposed.Revision||proposed.ClubId!=book.ClubId||proposed.Seed!=book.Get(kind).Roster.Seed||proposed.BenchLimit!=book.Get(kind).Roster.BenchLimit)throw new InvalidOperationException("La disposición cambió o no pertenece al equipo.");if(state.TacticMode==AcademyTacticMode.Own){var plan=TacticalPlanRules.ApplyLineup(book.Get(kind),phase,proposed);var saved=SavePlans(id,book.Replace(plan));return saved.Get(kind).View(saved.ClubId,saved.Revision,phase);}
        if(!book.Get(kind).Disposition(phase).Positions.SequenceEqual(proposed.Formation.Slots))throw new InvalidOperationException("No se editan coordenadas heredadas.");
        using var c=WorldDatabase.Open(_path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var old=Read(c,t,id);var source=Source(c,t);if(checked(old.Revision+source.Revision)!=book.Revision||old.EffectiveManagement!=AcademyManagement.Direct||old.TacticMode!=AcademyTacticMode.Synchronized)throw new InvalidOperationException("La sincronización cambió.");var rosters=old.Rosters.ToDictionary();rosters[kind]=new(new PlanRoster(proposed.Seed,proposed.BenchLimit,proposed.Members),book.Get(kind).WithoutBall.Positions.ToDictionary(s=>s.Id,s=>s.Role));var next=old with{Revision=old.Revision+1,Rosters=rosters};Write(c,t,next,old.Revision);t.Commit();return proposed with{Revision=book.Revision+1};
    }
}
public sealed class SqliteAcademyPlanStore(string path,Guid team):ITacticalPlanStore
{
    private readonly SqliteAcademyStore _store=new(path);
    public TacticalPlanBook? Load()=>_store.LoadPlans(team);
    public TacticalPlanBook Save(TacticalPlanBook proposed,TacticalSettings? common=null)=>_store.SavePlans(team,proposed,common);
    public TacticalSettings Settings(TacticalPlanBook book,TacticalPlanKind kind)=>_store.Settings(team,book,kind);
    public TacticalSettings SaveSettings(TacticalPlanKind kind,TacticalSettings proposed){var book=Load()!;if(book.Revision!=proposed.Revision)throw new InvalidOperationException("La táctica de cantera cambió.");if(_store.Get(team).TacticMode==AcademyTacticMode.Synchronized)return _store.SaveSynchronizedPersonnel(team,kind,proposed);var plan=book.Get(kind) with{Mentality=proposed.Mentality,Instructions=proposed.Instructions};return Settings(Save(book.Replace(plan),proposed),kind);}
}
public sealed class SqliteAcademyLineupStore(string path,Guid team,TacticalPlanKind kind,PossessionPhase phase):ILineupEditorStore
{
    private readonly SqliteAcademyStore _store=new(path);
    public CareerLineup? Load(){var b=_store.LoadPlans(team);return b.Get(kind).View(b.ClubId,b.Revision,phase);}
    public IReadOnlyList<LineupPlayer> Players()=>_store.LineupPlayers(team);
    public CareerLineup Save(CareerLineup proposed)=>_store.SaveLineup(team,kind,phase,proposed);
    public CareerLineup InitializeMissing()=>Load()!;
}
