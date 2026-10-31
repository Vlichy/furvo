using Furvo.Core.Domain;
using Furvo.Core.Careers;
using Furvo.Core.Development;
using Furvo.Core.Queries;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
/// <summary>Generación de instantáneas observables, sin estadísticas de partido ni capacidad de empleados inventadas.</summary>
public sealed class SqliteAnalysisReportService(string path)
{
    private readonly SqliteSportsStore _sports=new(path);
    public IReadOnlyList<AnalysisStaff> Staff(Guid team,AnalysisReportKind kind)
    {
        if(!Enum.IsDefined(kind))throw new InvalidDataException("Tipo de informe desconocido.");var known=_sports.Staff(team);var players=new SqliteClubSquadStore(path).Candidates(team).Select(p=>p.Id.Value).ToHashSet();
        using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);var result=new List<AnalysisStaff>();
        using(var q=WorldDatabase.Command(c,t,"SELECT c.id,c.profile_json FROM career_coaches c JOIN career_state s ON s.coach_id=c.id WHERE s.employment=2;"))using(var r=q.ExecuteReader())while(r.Read()){var id=Guid.Parse(r.GetString(0));if(known.Any(s=>s.Id==id)&&!players.Contains(id))result.Add(new(id,CareerJson.Read<CoachProfile>(r.GetString(1)).FullName,"HeadCoach"));}
        foreach(var person in known.Where(p=>result.All(s=>s.Id!=p.Id)&&!players.Contains(p.Id)))result.Add(new(person.Id,person.Name,"AcademyCoach"));return result.AsReadOnly();
    }
    public AnalysisReport Generate(Guid team,AnalysisSelection selection,AnalysisReportKind kind,Guid staffId)
    {
        var staff=Staff(team,kind).SingleOrDefault(s=>s.Id==staffId)??throw new InvalidDataException("Selecciona un miembro real y elegible del cuerpo técnico.");
        var squads=new SqliteClubSquadStore(path);var target=squads.Teams().Single(t=>t.Id==team);var plan=_sports.Plan(team);var selected=AnalysisReportRules.Select(selection,squads.Details(team),plan.Groups);var date=_sports.CareerDate();
        var players=selected.Select(p=>{var state=new SqliteTrainingExecutionStore(path).State(p.Player.Id);var sessions=plan.Sessions.Where(s=>s.Date>=date&&s.Date<date.AddDays(7)&&s.Participants.Contains(p.Player.Id)).ToArray();var description=PositionInsightCatalog.Describe(p.Player.Position,p.AttributeGroups);return new AnalysisPlayerSnapshot(p,sessions.Length,sessions.Sum(TrainingCatalog.PlannedLoad),state.Physical?.Fatigue,state.Physical?.Date,description.Strengths,description.Weaknesses,state.AttributeChanges.Where(v=>!PlayerAttributeCatalog.Get(v.Key).IsHidden).ToDictionary());}).ToArray();
        var findings=new List<AnalysisFinding>{new("selected-count",players.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),"career-roster",date),new("scheduled-sessions",plan.Sessions.Count(s=>s.Date>=date&&s.Date<date.AddDays(7)&&s.Participants.Intersect(players.Select(p=>p.Details.Player.Id)).Any()).ToString(System.Globalization.CultureInfo.InvariantCulture),"training-plan/revision/"+plan.Revision,date)};
        foreach(var player in players){var prefix="player/"+player.Details.Player.Id+"/";foreach(var item in player.Weaknesses) find("weakness/"+item.Code,item);foreach(var item in player.Strengths) find("strength/"+item.Code,item);if(player.ScheduledSessions>0)findings.Add(new(prefix+"planned-load",player.Details.Player.Name+" · "+player.PlannedLoad.ToString("0.0",System.Globalization.CultureInfo.GetCultureInfo("es-ES")),"planned-training-load",date));
            void find(string key,VisibleAttribute attribute)=>findings.Add(new(prefix+key,player.Details.Player.Name+" · "+PlayerAttributeCatalog.Get(attribute.Code).Name+": "+attribute.Value,"visible-player-attributes",date));}
        Guid career;using(var c=WorldDatabase.Open(path))using(var t=c.BeginTransaction(deferred:true))career=SqliteLineupStore.Owner(c,t).Save.Value;
        var context=new AnalysisReportContext(career,selection,staff,target.Name,selection.Group is {} group?plan.Groups.Single(g=>g.Id==group).Name:null,players);
        var report=new AnalysisReport(Guid.NewGuid(),team,kind,selection.Scope==AnalysisScope.Individual?selection.Player:null,null,date,staff.Id,findings,Array.Empty<string>(),context);_sports.SaveReport(report);return report;
    }
    public void SavePriorities(Guid team,Guid report,IReadOnlyList<string> priorities)
    {var current=_sports.Reports(team).Single(r=>r.Id==report);_sports.SaveReport(current with{Priorities=priorities.ToArray()});}
    /// <summary>El mismo grupo de Entrenamiento; modificarlo no reescribe informes ni sesiones. Marca revisión cuando procede.</summary>
    public TrainingPlan SaveGroup(Guid team,TrainingGroup group,int revision)
    {
        var current=_sports.Plan(team);if(current.Revision!=revision)throw new InvalidOperationException("Los grupos cambiaron. Vuelve a abrirlos.");var groups=current.Groups.Where(g=>g.Id!=group.Id).Append(group).ToArray();var proposed=current with{Groups=groups,RequiresReview=current.RequiresReview||current.Sessions.Any(s=>s.Target.Group==group.Id)};
        TrainingRules.Validate(proposed with{Sessions=Array.Empty<TrainingSession>()},new SqliteClubSquadStore(path).Players(team),_sports.Staff(team));
        using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();SqliteSportsStore.Ensure(c,t);using var q=WorldDatabase.Command(c,t,"INSERT INTO training_plans VALUES($team,$next,$json) ON CONFLICT(team_id) DO UPDATE SET revision=excluded.revision,data_json=excluded.data_json WHERE revision=$previous;",("$team",team.ToString("D")),("$next",revision+1),("$previous",revision),("$json",CareerJson.Write(proposed with{Revision=revision+1})));if(q.ExecuteNonQuery()!=1)throw new InvalidOperationException("Los grupos cambiaron en otra operación.");t.Commit();return proposed with{Revision=revision+1};
    }
}
