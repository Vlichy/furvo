using Furvo.Core.Domain;
using Furvo.Core.Development;
using Furvo.Core.Tactics;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
/// <summary>Planificación y observaciones separadas: consultar no crea tablas ni ejecuta sesiones.</summary>
public sealed class SqliteSportsStore(string path,TrainingSchedulePolicy? schedulePolicy=null)
{
    internal static bool HasAssignments(string path){using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);if(!HasSchema(c,t))return false;using var q=WorldDatabase.Command(c,t,"SELECT (SELECT count(*) FROM sport_assignments)+(SELECT count(*) FROM training_development);");return Convert.ToInt32(q.ExecuteScalar())>0;}
    internal static bool HasSchema(SqliteConnection c,SqliteTransaction t)
    {using var q=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE name='sports_schema';");if(Convert.ToInt32(q.ExecuteScalar())==0)return false;q.CommandText="SELECT version FROM sports_schema WHERE singleton=1";if(Convert.ToInt32(q.ExecuteScalar())!=1)throw new NotSupportedException("Versión deportiva desconocida.");return true;}
    internal static void Ensure(SqliteConnection c,SqliteTransaction t){_=SqliteLineupStore.Owner(c,t);if(!HasSchema(c,t))CareerJson.Migrate(c,t,"Sports001_Initial.sql");}
    private ClubTeam Team(Guid id)=>new SqliteClubSquadStore(path).Teams().SingleOrDefault(t=>t.Id==id)??throw new InvalidOperationException("Equipo ajeno al club dirigido.");
    public IReadOnlyList<TrainingStaff> Staff(Guid team)
    {
        var target=Team(team);using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);var result=new List<TrainingStaff>();
        using(var q=WorldDatabase.Command(c,t,"SELECT s.coach_id,c.profile_json FROM career_state s JOIN career_coaches c ON c.id=s.coach_id WHERE s.singleton=1;"))
        using(var r=q.ExecuteReader())if(r.Read())result.Add(new(Guid.Parse(r.GetString(0)),CareerJson.Read<Furvo.Core.Careers.CoachProfile>(r.GetString(1)).FullName,team,new[]{TrainingResponsibility.Weekly,TrainingResponsibility.Collective,TrainingResponsibility.Individual,TrainingResponsibility.Position,TrainingResponsibility.Group,TrainingResponsibility.SetPieces}));
        if(target.Category!="First"){var coach=new SqliteAcademyStore(path).Get(team).Coach;if(coach.LinkState=="Institutional")result.Add(new(coach.Id,coach.Name,team,new[]{TrainingResponsibility.Weekly,TrainingResponsibility.Collective,TrainingResponsibility.Individual,TrainingResponsibility.Position,TrainingResponsibility.Group,TrainingResponsibility.SetPieces},"AcademyCoach"));}return result;
    }
    public void SaveDelegation(Guid team, int revision, TrainingResponsibility area, TrainingDelegation? assignment)
    {
        if (!Enum.IsDefined(area) || assignment is not null &&
            (assignment.Area != area || !Enum.IsDefined(assignment.Mode) || assignment.Responsible is not { } id ||
             !Staff(team).Any(p => p.Id == id && p.Role != "HeadCoach" && p.Responsibilities.Contains(area))))
            throw new InvalidDataException("El responsable no está disponible para esta tarea.");
        Team(team);
        using var c = WorldDatabase.Open(path, SqliteOpenMode.ReadWrite);
        using var t = c.BeginTransaction();
        Ensure(c, t);
        using var read = WorldDatabase.Command(c, t, "SELECT data_json FROM training_plans WHERE team_id=$team", ("$team", team.ToString("D")));
        var plan = read.ExecuteScalar() is string json ? CareerJson.Read<TrainingPlan>(json) : TrainingPlan.Empty(team);
        if (plan.Revision != revision) throw new InvalidOperationException("La planificación cambió; vuelve a abrirla.");
        var assignments = plan.Delegations.Where(d => d.Area != area).ToList();
        if (assignment is not null) assignments.Add(assignment);
        var next = plan with { Revision = checked(plan.Revision + 1), Delegations = assignments.OrderBy(d => d.Area).ToArray() };
        using var write = WorldDatabase.Command(c, t,
            "INSERT INTO training_plans VALUES($team,$revision,$json) ON CONFLICT(team_id) DO UPDATE SET revision=excluded.revision,data_json=excluded.data_json",
            ("$team", team.ToString("D")), ("$revision", next.Revision), ("$json", CareerJson.Write(next)));
        write.ExecuteNonQuery();
        t.Commit();
    }
    public TrainingPlan Plan(Guid team)
    {Team(team);using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);if(!HasSchema(c,t))return TrainingPlan.Empty(team);using var q=WorldDatabase.Command(c,t,"SELECT data_json FROM training_plans WHERE team_id=$team;",("$team",team.ToString("D")));var plan=q.ExecuteScalar() is string json?CareerJson.Read<TrainingPlan>(json):TrainingPlan.Empty(team);if(!HasDaySchema(c,t))return ReadLegacyRests(c,t,plan);var days=new List<TrainingDayPlan>();using var read=WorldDatabase.Command(c,t,"SELECT date,kind,single_turn FROM training_day_plans WHERE team_id=$team ORDER BY date;",("$team",team.ToString("D")));using var rows=read.ExecuteReader();while(rows.Read())days.Add(new(DateOnly.ParseExact(rows.GetString(0),"yyyy-MM-dd"),(TrainingDayKind)rows.GetInt32(1),rows.IsDBNull(2)?null:(TrainingTurn)rows.GetInt32(2)));rows.Close();return ReadLegacyRests(c,t,plan with{Days=days});}
    private static bool HasDaySchema(SqliteConnection c,SqliteTransaction t){using var q=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE name='training_day_schema';");if(Convert.ToInt32(q.ExecuteScalar())==0)return false;q.CommandText="SELECT version FROM training_day_schema WHERE singleton=1;";if(Convert.ToInt32(q.ExecuteScalar())!=1)throw new NotSupportedException("Versión de jornadas desconocida.");return true;}
    private static bool HasRestSchema(SqliteConnection c,SqliteTransaction t)
    {using var q=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE name='training_rest_schema';");if(Convert.ToInt32(q.ExecuteScalar())==0)return false;q.CommandText="SELECT version FROM training_rest_schema WHERE singleton=1;";if(Convert.ToInt32(q.ExecuteScalar())!=1)throw new NotSupportedException("Versión de descansos desconocida.");return true;}
    private static TrainingPlan ReadLegacyRests(SqliteConnection c,SqliteTransaction t,TrainingPlan plan)
    {
        var records=new List<LegacyTrainingRest>();
        if(HasRestSchema(c,t)){using var read=WorldDatabase.Command(c,t,"SELECT data_json FROM training_rest_archive WHERE team_id=$team ORDER BY occurred_on,block_id;",("$team",plan.Team.ToString("D")));using var r=read.ExecuteReader();while(r.Read())records.Add(CareerJson.Read<LegacyTrainingRest>(r.GetString(0)));}
        var executed=new HashSet<Guid>();using(var read=WorldDatabase.Command(c,t,"SELECT session_id FROM training_attendance WHERE team_id=$team;",("$team",plan.Team.ToString("D"))))using(var r=read.ExecuteReader())while(r.Read())executed.Add(Guid.Parse(r.GetString(0)));
        return TrainingRestCompatibility.Project(plan with{LegacyRests=records},executed);
    }
    internal static void WriteLegacyRests(SqliteConnection c,SqliteTransaction t,Guid team,IReadOnlyList<LegacyTrainingRest>? records)
    {
        if(!HasRestSchema(c,t))CareerJson.Migrate(c,t,"SportsTraining004_RestArchive.sql");
        foreach(var record in records??Array.Empty<LegacyTrainingRest>()){
            using var q=WorldDatabase.Command(c,t,"INSERT INTO training_rest_archive VALUES($team,$id,$date,$json) ON CONFLICT(team_id,block_id) DO UPDATE SET data_json=excluded.data_json;",("$team",team.ToString("D")),("$id",record.Original.Id.ToString("D")),("$date",record.Original.Date.ToString("yyyy-MM-dd")),("$json",CareerJson.Write(record)));q.ExecuteNonQuery();
        }
    }
    public TrainingPlan SavePlan(TrainingPlan plan)
    {
        var target=Team(plan.Team);var squad=new SqliteClubSquadStore(path).Players(plan.Team);var others=new SqliteClubSquadStore(path).Teams().Where(t=>t.Id!=plan.Team).SelectMany(t=>Plan(t.Id).Sessions).ToArray();var previous=Plan(plan.Team);if(plan.Sessions.Any(s=>s.Exercise=="rest"))throw new InvalidDataException("El descanso debe confirmarse en la jornada; no se crean bloques de descanso.");if(plan.LegacyRests is not null&&CareerJson.Write(plan.LegacyRests)!=CareerJson.Write(previous.LegacyRests))throw new InvalidOperationException("El archivo histórico de descansos no es editable.");plan=TrainingRestCompatibility.Project(plan with{LegacyRests=previous.LegacyRests});foreach(var day in plan.Days??Array.Empty<TrainingDayPlan>())if(day.Date<CareerDate()&&!(previous.Days??Array.Empty<TrainingDayPlan>()).Contains(day))throw new InvalidDataException("No se modifica el tipo de una jornada pasada.");foreach(var removed in previous.Groups.Where(g=>plan.Groups.All(next=>next.Id!=g.Id))){var used=GroupReferences(plan.Team,removed.Id);if(used.Count>0)throw new TrainingGroupInUseException(used);}
        var completed=new SqliteTrainingExecutionStore(path).CompletedSessions(plan.Team);TrainingSchedule.ValidateChanges(plan,previous,schedulePolicy);TrainingRules.Validate(plan with{Sessions=plan.Sessions.Where(s=>s.Date>=CareerDate()&&!completed.Contains(s.Id)).ToArray()},squad,Staff(plan.Team),others,target.Category=="Juvenil");
        foreach(var session in plan.Sessions){if(completed.Contains(session.Id))continue;if(session.Date<CareerDate()){if(!previous.Sessions.Any(s=>s.Id==session.Id&&CareerJson.Write(s)==CareerJson.Write(session)))throw new InvalidDataException("No se crean ni modifican sesiones de una fecha anterior a la carrera.");continue;}var exercise=TrainingCatalog.Get(session.Exercise);if(exercise.Area==TrainingArea.Technology){var facility=Facility();if(session.Facility!=facility.Id||!facility.Available(session.Date))throw new InvalidOperationException("Footbonaut no está instalado o no tiene disponibilidad verificada.");}if(session.Routine is {} routine){var store=target.Category=="First"?(ITacticalPlanStore)new SqliteTacticalPlanStore(path):new SqliteAcademyPlanStore(path,plan.Team);var b=store.Load()??throw new InvalidOperationException("Falta táctica.");var settings=store.Settings(b,TacticalPlanKind.Initial);TrainingRoutines.Validate(session.Exercise,settings,routine);}}
        using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();Ensure(c,t);
        // Las restricciones posteriores señalan conflictos sin reescribir sesiones; solo se exige resolverlas al confirmar cambios de esa actividad.
        var restrictions=SqliteTrainingAvailabilityStore.Read(c,t);
        foreach(var session in plan.Sessions.Where(s=>s.Date>=CareerDate()&&!completed.Contains(s.Id)))
            if(!previous.Sessions.Any(old=>old.Id==session.Id&&CareerJson.Write(old)==CareerJson.Write(session)))TrainingAvailability.Validate(session,restrictions);
        using(var locked=WorldDatabase.Command(c,t,"SELECT session_id,evidence_json FROM training_attendance WHERE team_id=$team;",("$team",plan.Team.ToString("D"))))using(var reader=locked.ExecuteReader())while(reader.Read()){using var snapshot=System.Text.Json.JsonDocument.Parse(reader.GetString(1));var original=CareerJson.Read<TrainingSession>(snapshot.RootElement.GetProperty("Session").GetRawText());if(original.Exercise=="rest"&&(plan.LegacyRests??Array.Empty<LegacyTrainingRest>()).Any(r=>r.Original.Id==original.Id&&CareerJson.Write(r.Original)==CareerJson.Write(original)))continue;if(!plan.Sessions.Any(s=>s.Id==original.Id&&CareerJson.Write(s)==CareerJson.Write(original)))throw new InvalidOperationException("Una sesión realizada conserva su historial y no puede editarse ni eliminarse.");}using var old=WorldDatabase.Command(c,t,"SELECT revision FROM training_plans WHERE team_id=$team;",("$team",plan.Team.ToString("D")));if(Convert.ToInt32(old.ExecuteScalar()??0)!=plan.Revision)throw new InvalidOperationException("La planificación cambió; vuelve a abrirla.");var next=plan with{Revision=plan.Revision+1};using var q=WorldDatabase.Command(c,t,"INSERT INTO training_plans VALUES($team,$revision,$json) ON CONFLICT(team_id) DO UPDATE SET revision=excluded.revision,data_json=excluded.data_json;",("$team",plan.Team.ToString("D")),("$revision",next.Revision),("$json",CareerJson.Write(next with{Days=null,LegacyRests=null})));q.ExecuteNonQuery();if(!HasDaySchema(c,t))CareerJson.Migrate(c,t,"SportsTraining003_Days.sql");using(var delete=WorldDatabase.Command(c,t,"DELETE FROM training_day_plans WHERE team_id=$team;",("$team",plan.Team.ToString("D"))))delete.ExecuteNonQuery();foreach(var day in next.Days??Array.Empty<TrainingDayPlan>()){using var insert=WorldDatabase.Command(c,t,"INSERT INTO training_day_plans VALUES($team,$date,$kind,$turn);",("$team",plan.Team.ToString("D")),("$date",day.Date.ToString("yyyy-MM-dd")),("$kind",(int)day.Kind),("$turn",day.SingleTurn is {} turn?(object)(int)turn:DBNull.Value));insert.ExecuteNonQuery();}WriteLegacyRests(c,t,next.Team,next.LegacyRests);t.Commit();return next;
    }
    /// <summary>Operaciones explícitas sobre el mismo plan; el historial y los informes no se eliminan en cascada.</summary>
    public TrainingPlan SaveGroup(Guid team,TrainingGroup group,int revision)
    {var current=Plan(team);if(current.Revision!=revision)throw new InvalidOperationException("Los grupos cambiaron. Vuelve a abrirlos.");return SavePlan(TrainingGroups.Update(current,group,new SqliteClubSquadStore(path).Candidates(team),CareerDate(),new SqliteTrainingExecutionStore(path).CompletedSessions(team)));}
    public IReadOnlyList<TrainingGroupReference> GroupReferences(Guid team,Guid group)
    {
        var current=Plan(team);var result=TrainingGroups.References(current,group,Reports(team)).ToList();
        using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);if(HasSchema(c,t)){using var q=WorldDatabase.Command(c,t,"SELECT evidence_json FROM training_attendance WHERE team_id=$team",("$team",team.ToString("D")));using var rows=q.ExecuteReader();while(rows.Read()){using var json=System.Text.Json.JsonDocument.Parse(rows.GetString(0));if(json.RootElement.TryGetProperty("Session",out var data)){var block=CareerJson.Read<TrainingSession>(data.GetRawText());if(block.Target.Group==group)result.Add(new("Activity",block.Id,block.Date,block.Objective??block.Exercise));}}}
        return result.DistinctBy(r=>(r.Kind,r.Id)).OrderBy(r=>r.Date).ThenBy(r=>r.Id).ToArray();
    }
    public TrainingPlan DeleteGroup(Guid team,Guid group,int revision)
    {var current=Plan(team);if(current.Revision!=revision)throw new InvalidOperationException("Los grupos cambiaron. Vuelve a abrirlos.");var used=GroupReferences(team,group);if(used.Count>0)throw new TrainingGroupInUseException(used);return SavePlan(TrainingGroups.Delete(current,group,Reports(team)));}
    public TacticalSettings? TrainingTactics(Guid team)
    {var target=Team(team);var store=target.Category=="First"?(ITacticalPlanStore)new SqliteTacticalPlanStore(path):new SqliteAcademyPlanStore(path,team);var book=store.Load();return book is null?null:store.Settings(book,TacticalPlanKind.Initial);}
    public DateOnly CareerDate(){using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);return SqliteLineupStore.Owner(c,t).Date;}
    public SportsFacility Facility()
    {
        var club=new SqliteClubSquadStore(path).Teams()[0].Club;using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);if(HasSchema(c,t)){using var q=WorldDatabase.Command(c,t,"SELECT data_json FROM sport_facilities WHERE club_id=$club AND technology='footbonaut';",("$club",club.ToString()));if(q.ExecuteScalar() is string json)return CareerJson.Read<SportsFacility>(json);}
        return new(Furvo.Core.Academy.AcademyGenerator.Identity(club+"/facility/footbonaut"),club,"footbonaut",FacilityOwnership.Unknown,1,null,null);
    }
    public void RegisterFacility(SportsFacility facility)
    {facility.Validate();if(facility.Club!=new SqliteClubSquadStore(path).Teams()[0].Club)throw new InvalidOperationException("Instalación de otro club.");using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();Ensure(c,t);using var q=WorldDatabase.Command(c,t,"INSERT INTO sport_facilities VALUES($id,$club,$tech,$json) ON CONFLICT(club_id,technology) DO UPDATE SET data_json=excluded.data_json;",("$id",facility.Id.ToString("D")),("$club",facility.Club.ToString()),("$tech",facility.Technology),("$json",CareerJson.Write(facility)));q.ExecuteNonQuery();t.Commit();}
    public IReadOnlyList<AnalysisReport> Reports(Guid team)=>ReadMany<AnalysisReport>("sport_analysis_reports",team);
    public IReadOnlyList<AnalysisRequest> AnalysisRequests(Guid team)=>ReadMany<AnalysisRequest>("sport_analysis_requests",team);
    private IReadOnlyList<T> ReadMany<T>(string table,Guid team)
    {Team(team);using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);if(!HasSchema(c,t))return Array.Empty<T>();using var q=WorldDatabase.Command(c,t,"SELECT data_json FROM "+table+" WHERE team_id=$team ORDER BY id;",("$team",team.ToString("D")));using var r=q.ExecuteReader();var values=new List<T>();while(r.Read())values.Add(CareerJson.Read<T>(r.GetString(0)));return values;}
    public void SaveReport(AnalysisReport report)
    {
        Team(report.Team);var previous=Reports(report.Team).SingleOrDefault(r=>r.Id==report.Id);
        if(previous is not null){if(CareerJson.Write(previous with{Priorities=report.Priorities})!=CareerJson.Write(report))throw new InvalidOperationException("El informe histórico es inmutable; solo se pueden guardar sus prioridades.");}
        else {AnalysisReportRules.ValidateSnapshot(report);using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);if(report.Context!.Career!=SqliteLineupStore.Owner(c,t).Save.Value)throw new InvalidDataException("El informe pertenece a otra carrera.");var allowed=new SqliteAnalysisReportService(path).Staff(report.Team,report.Kind);if(!allowed.Any(s=>s.Id==report.Analyst))throw new InvalidDataException("Técnico no elegible para este informe.");var members=new SqliteClubSquadStore(path).Candidates(report.Team).Select(p=>p.Id).ToHashSet();if(report.Context.Players.Any(p=>!members.Contains(p.Details.Player.Id)))throw new InvalidDataException("Futbolista ajeno a la plantilla.");}
        if(report.Priorities.Distinct().Count()!=report.Priorities.Count||report.Priorities.Any(p=>!report.Findings.Any(f=>f.Topic==p)))throw new InvalidDataException("Prioridades ajenas al informe o duplicadas.");
        if(report.Id==Guid.Empty||report.Date==default||report.Findings.Count==0||!Enum.IsDefined(report.Kind)||report.Date>CareerDate()||report.Findings.Select(f=>f.Topic).Distinct().Count()!=report.Findings.Count||report.Findings.Any(f=>string.IsNullOrWhiteSpace(f.Topic)||string.IsNullOrWhiteSpace(f.Content)||string.IsNullOrWhiteSpace(f.Source)||f.ObservedAt>report.Date))throw new InvalidDataException("Informe sin datos observables válidos.");
        // Comprobar de nuevo bajo bloqueo evita sobrescribir una instantánea creada concurrentemente.
        using var connection=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var transaction=connection.BeginTransaction();Ensure(connection,transaction);
        using(var read=WorldDatabase.Command(connection,transaction,"SELECT team_id,data_json FROM sport_analysis_reports WHERE id=$id;",("$id",report.Id.ToString("D"))))using(var reader=read.ExecuteReader())if(reader.Read()){
            var stored=CareerJson.Read<AnalysisReport>(reader.GetString(1));if(reader.GetString(0)!=report.Team.ToString("D")||CareerJson.Write(stored with{Priorities=report.Priorities})!=CareerJson.Write(report))throw new InvalidOperationException("El informe histórico es inmutable.");
        }
        using var write=WorldDatabase.Command(connection,transaction,"INSERT INTO sport_analysis_reports VALUES($id,$team,$json) ON CONFLICT(id) DO UPDATE SET data_json=excluded.data_json;",("$id",report.Id.ToString("D")),("$team",report.Team.ToString("D")),("$json",CareerJson.Write(report)));write.ExecuteNonQuery();transaction.Commit();
    }
    public void RequestAnalysis(AnalysisRequest request)
    {if(request.Id==Guid.Empty||string.IsNullOrWhiteSpace(request.Subject)||request.Date!=CareerDate()||request.Status!="PendingData"||request.Responsible is not null)throw new InvalidDataException("No existe analista contratado; la solicitud debe quedar pendiente de datos y responsable.");WriteDocument("sport_analysis_requests",request.Id,request.Team,request);}
    private void WriteDocument<T>(string table,Guid id,Guid team,T value)
    {Team(team);using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();Ensure(c,t);using var q=WorldDatabase.Command(c,t,"INSERT INTO "+table+" VALUES($id,$team,$json) ON CONFLICT(id) DO UPDATE SET data_json=excluded.data_json WHERE team_id=excluded.team_id;",("$id",id.ToString("D")),("$team",team.ToString("D")),("$json",CareerJson.Write(value)));if(q.ExecuteNonQuery()!=1)throw new InvalidOperationException("Identidad de otro equipo.");t.Commit();}
    public void RequestFacility(FacilityAcquisition request)
    {var club=new SqliteClubSquadStore(path).Teams()[0].Club;if(request.Club!=club||request.Id==Guid.Empty||request.Technology!="footbonaut"||request.RequestedAt!=CareerDate()||request.Status!="PendingQuote"||request.Authorization is not null)throw new InvalidDataException("Solicitud de adquisición inválida.");using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();Ensure(c,t);using var q=WorldDatabase.Command(c,t,"INSERT INTO sport_acquisitions VALUES($id,$club,$json);",("$id",request.Id.ToString("D")),("$club",club.ToString()),("$json",CareerJson.Write(request)));q.ExecuteNonQuery();t.Commit();}
    public IReadOnlyList<FacilityAcquisition> Acquisitions(){var club=new SqliteClubSquadStore(path).Teams()[0].Club;using var c=WorldDatabase.Open(path);using var t=c.BeginTransaction(deferred:true);if(!HasSchema(c,t))return Array.Empty<FacilityAcquisition>();using var q=WorldDatabase.Command(c,t,"SELECT data_json FROM sport_acquisitions WHERE club_id=$club;",("$club",club.ToString()));using var r=q.ExecuteReader();var list=new List<FacilityAcquisition>();while(r.Read())list.Add(CareerJson.Read<FacilityAcquisition>(r.GetString(0)));return list;}
    /// <summary>Integración futura con autorización presupuestaria real; no hay un botón de compra que invente precios o aprobación.</summary>
    public void CommitAcquisition(FacilityAcquisition quote,IFacilityPurchaseAuthorization authorization)
    {
        var facility=Facility();if(quote.Club!=facility.Club||quote.Technology!="footbonaut"||quote.Status!="Approved"||quote.Purchase is null or <=0||quote.Installation is null or <0||quote.AnnualMaintenance is null or <0||quote.Operating is null or <0||quote.LeadDays is null or <1||quote.SpaceConfirmed!=true||string.IsNullOrWhiteSpace(quote.Authorization)||facility.Ownership is FacilityOwnership.Installed or FacilityOwnership.Ordered)throw new InvalidDataException("Falta presupuesto completo, espacio, plazo o autorización de la adquisición.");
        using var c=WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();Ensure(c,t);using(var existing=WorldDatabase.Command(c,t,"SELECT data_json FROM sport_acquisitions WHERE id=$id AND club_id=$club",("$id",quote.Id.ToString("D")),("$club",quote.Club.ToString())))if(existing.ExecuteScalar() is not string json||CareerJson.Read<FacilityAcquisition>(json).Status!="PendingQuote")throw new InvalidOperationException("Solicitud inexistente o ya comprometida.");
        var owner=SqliteLineupStore.Owner(c,t);var resources=SqliteEconomyStore.ReadResources(c,t).SingleOrDefault(r=>r.ClubId==quote.Club);var cost=quote.Purchase.Value+quote.Installation.Value;if(resources?.Cash is not {} cash||resources.Currency is not {} currency||currency!=quote.Currency||cash<cost||!authorization.CanCommit(quote,cash,currency))throw new InvalidOperationException("Compra no autorizada o sin presupuesto disponible en la moneda del club.");
        using(var payment=WorldDatabase.Command(c,t,"UPDATE club_economics SET cash_cents=cash_cents-$cost WHERE club_id=$club AND cash_cents>=$cost;",("$cost",SqliteEconomyStore.Cents(cost)),("$club",quote.Club.ToString())))if(payment.ExecuteNonQuery()!=1)throw new InvalidOperationException("La tesorería cambió.");var ordered=quote with{Status="Ordered"};using(var update=WorldDatabase.Command(c,t,"UPDATE sport_acquisitions SET data_json=$json WHERE id=$id",("$json",CareerJson.Write(ordered)),("$id",quote.Id.ToString("D"))))update.ExecuteNonQuery();var installation=facility with{Ownership=FacilityOwnership.Ordered,AvailableFrom=owner.Date.AddDays(quote.LeadDays.Value),Evidence=quote.Authorization};using(var insert=WorldDatabase.Command(c,t,"INSERT INTO sport_facilities VALUES($id,$club,$technology,$json) ON CONFLICT(club_id,technology) DO UPDATE SET data_json=excluded.data_json;",("$id",installation.Id.ToString("D")),("$club",installation.Club.ToString()),("$technology",installation.Technology),("$json",CareerJson.Write(installation))))insert.ExecuteNonQuery();t.Commit();
    }
}
