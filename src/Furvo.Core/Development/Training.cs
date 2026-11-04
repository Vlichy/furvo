using Furvo.Core.Domain;
using Furvo.Core.Tactics;
namespace Furvo.Core.Development;

public enum TrainingArea { Field=1,Gym=2,Goalkeepers=3,SetPieces=4,Recovery=5,Technology=6 }
public enum TrainingLoad { Rest=0,VeryLow=1,Low=2,Medium=3,High=4,VeryHigh=5 }
public enum TrainingScope { Team=1,Individual=2,Position=3,Group=4 }
public enum TrainingResponsibility { Weekly=1,Collective=2,Individual=3,Position=4,Group=5,Gym=6,Goalkeepers=7,SetPieces=8,Recovery=9 }
public enum TrainingManagement { Manual=1,Delegated=2,Mixed=3 }
public sealed record TrainingStaff(Guid Id,string Name,Guid Team,IReadOnlyList<TrainingResponsibility> Responsibilities,string Role="HeadCoach");
/// <summary>Candidato público para planificar: identidad y demarcaciones, sin atributos internos.</summary>
public sealed record TrainingCandidate(PlayerId Id,string Name,DateOnly? BirthDate,PlayerPositions? Positions);
public sealed record TrainingGroup(Guid Id,string Name,IReadOnlyList<PlayerId> Members,string Objective,Guid? Responsible);
public enum TrainingDelegationMode { Delegated=1,Mixed=2 }
/// <summary>La ausencia de asignación expresa control manual; las partidas anteriores conservan delegación completa por defecto.</summary>
public sealed record TrainingDelegation(TrainingResponsibility Area,Guid? Responsible,TrainingDelegationMode Mode=TrainingDelegationMode.Delegated);
public sealed record TrainingTarget(TrainingScope Scope,PlayerId? Player=null,PlayerRole? Position=null,Guid? Group=null,IReadOnlyList<PlayerId>? Excluded=null);
public sealed record RoutineReference(SetPieceKind Kind,FreeKickLocation? Location=null);
public sealed record TrainingSession(Guid Id,Guid Team,DateOnly Date,TimeOnly Start,int Minutes,string Exercise,TrainingLoad Load,
    TrainingTarget Target,IReadOnlyList<PlayerId> Participants,Guid? Responsible=null,bool Opposition=false,RoutineReference? Routine=null,Guid? Facility=null,string? Objective=null,int FrequencyWeeks=1,TrainingTurn? Turn=null)
{
    public decimal MinutesPerPlayer=>TrainingCatalog.Get(Exercise).Area==TrainingArea.Technology?Minutes/(decimal)Math.Max(1,Participants.Count):Minutes;
    public DateTime Begins=>Date.ToDateTime(Start);
    public DateTime Ends=>Begins.AddMinutes(Minutes);
}
public sealed record TrainingPlan(Guid Team,int Revision,IReadOnlyList<TrainingSession> Sessions,IReadOnlyList<TrainingGroup> Groups,IReadOnlyList<TrainingDelegation> Delegations,bool RequiresReview=false,IReadOnlyList<TrainingDayPlan>? Days=null,IReadOnlyList<LegacyTrainingRest>? LegacyRests=null)
{
    public static TrainingPlan Empty(Guid team)=>new(team,0,Array.Empty<TrainingSession>(),Array.Empty<TrainingGroup>(),Array.Empty<TrainingDelegation>());
    public TrainingManagement Management=>Delegations.Count==0?TrainingManagement.Manual:Delegations.All(d=>d.Mode==TrainingDelegationMode.Delegated)&&Delegations.Select(d=>d.Area).Distinct().Count()==Enum.GetValues<TrainingResponsibility>().Length?TrainingManagement.Delegated:TrainingManagement.Mixed;
}
public sealed record TrainingExercise(string Code,TrainingArea Area,IReadOnlyList<string> Attributes,decimal LoadFactor,bool GoalkeepersOnly=false);
/// <summary>Catálogo de ejercicios; parámetros de juego v0 experimentales, sin pretensión médica o ganancias instantáneas.</summary>
public static class TrainingCatalog
{
    public const string Version="training-experimental-v0";
    public static IReadOnlyList<TrainingExercise> Exercises {get;}=new TrainingExercise[]{
        new("technical",TrainingArea.Field,new[]{"ball_control","first_touch","technique"},1m),new("passing",TrainingArea.Field,new[]{"short_passing","long_passing","vision"},.85m),new("finishing",TrainingArea.Field,new[]{"finishing","off_the_ball","composure"},1m),new("defending",TrainingArea.Field,new[]{"defensive_positioning","anticipation","marking"},1m),new("collective",TrainingArea.Field,new[]{"teamwork","tactical_discipline","tactical_understanding"},1m),
        new("general_strength",TrainingArea.Gym,new[]{"strength"},1.1m),new("upper_body",TrainingArea.Gym,new[]{"strength","balance"},.85m),new("lower_body",TrainingArea.Gym,new[]{"strength","jumping"},1.1m),new("power",TrainingArea.Gym,new[]{"jumping","acceleration"},1.2m),new("explosiveness",TrainingArea.Gym,new[]{"acceleration","agility"},1.2m),new("muscular_endurance",TrainingArea.Gym,new[]{"stamina","strength"},1.1m),new("mobility",TrainingArea.Gym,new[]{"agility","coordination"},.5m),new("stability",TrainingArea.Gym,new[]{"balance","coordination"},.6m),new("prevention",TrainingArea.Gym,new[]{"balance","coordination"},.5m),new("adapted_physical",TrainingArea.Gym,new[]{"stamina"},.5m),
        new("reflexes",TrainingArea.Goalkeepers,new[]{"reflexes"},.9m,true),new("handling",TrainingArea.Goalkeepers,new[]{"handling"},.8m,true),new("punching",TrainingArea.Goalkeepers,new[]{"punching"},.9m,true),new("keeper_exits",TrainingArea.Goalkeepers,new[]{"rushing_out"},1m,true),new("aerial",TrainingArea.Goalkeepers,new[]{"aerial_claims"},1m,true),new("one_on_one",TrainingArea.Goalkeepers,new[]{"one_on_ones"},1m,true),new("keeper_feet",TrainingArea.Goalkeepers,new[]{"first_touch","short_passing"},.8m,true),new("distribution",TrainingArea.Goalkeepers,new[]{"hand_distribution","long_passing"},.7m,true),new("keeper_penalties",TrainingArea.Goalkeepers,new[]{"reflexes","one_on_ones"},.9m,true),new("keeper_coordination",TrainingArea.Goalkeepers,new[]{"command_of_area","defensive_communication"},.7m,true),
        new("penalty_practice",TrainingArea.SetPieces,new[]{"penalties","composure","shot_power"},.7m),new("corner_practice",TrainingArea.SetPieces,new[]{"corners","heading","teamwork"},.8m),new("free_kick_practice",TrainingArea.SetPieces,new[]{"free_kicks","off_the_ball"},.8m),new("throw_practice",TrainingArea.SetPieces,new[]{"throw_ins","teamwork"},.6m),
        new("active_recovery",TrainingArea.Recovery,Array.Empty<string>(),.2m),new("regeneration",TrainingArea.Recovery,Array.Empty<string>(),.15m),new("recovery_mobility",TrainingArea.Recovery,Array.Empty<string>(),.2m),
        new("footbonaut_touch",TrainingArea.Technology,new[]{"first_touch","ball_control"},.7m),new("footbonaut_pass",TrainingArea.Technology,new[]{"short_passing","decisions"},.7m),new("footbonaut_response",TrainingArea.Technology,new[]{"anticipation","coordination"},.8m),new("footbonaut_directions",TrainingArea.Technology,new[]{"ball_control","first_touch"},.8m),new("footbonaut_weak_foot",TrainingArea.Technology,new[]{"technique","short_passing"},.7m),new("footbonaut_time_pressure",TrainingArea.Technology,new[]{"first_touch","composure"},.8m)
    };
    // Solo el decodificador histórico reconoce rest: no pertenece al catálogo activo y no puede guardarse como bloque.
    public static TrainingExercise Get(string code)=>code=="rest"?new("rest",TrainingArea.Recovery,Array.Empty<string>(),0):Exercises.SingleOrDefault(e=>e.Code==code)??throw new InvalidDataException("Ejercicio desconocido.");
    public static decimal PlannedLoad(TrainingSession s)=>s.MinutesPerPlayer*new[]{0m,.25m,.5m,1m,1.4m,1.8m}[(int)s.Load]*Get(s.Exercise).LoadFactor;
}
/// <summary>Conflicto identificado en la agenda completa; la presentación traduce los códigos de ejercicio.</summary>
public sealed record TrainingConflict(TrainingSession First,TrainingSession Second)
{
    public static InvalidDataException Error(TrainingSession first,TrainingSession second)
    {
        var error=new InvalidDataException($"Sesiones solapadas: {first.Exercise} ({first.Start:HH:mm}–{first.Ends:HH:mm}) y {second.Exercise} ({second.Start:HH:mm}–{second.Ends:HH:mm}); futbolistas, responsable o instalación no disponibles.");
        error.Data[nameof(TrainingConflict)]=new TrainingConflict(first,second);return error;
    }
}
public static class TrainingRules
{
    public static IReadOnlyList<PlayerId> Resolve(TrainingTarget target,IReadOnlyList<TrainingCandidate> squad,IReadOnlyList<TrainingGroup> groups)=>Resolve(target,squad.Select(p=>new Player(p.Id,p.Name,p.BirthDate,mainPosition:p.Positions is null?null:PlayerPositions.GroupOf(p.Positions.Primary),positions:p.Positions)).ToArray(),groups);
    public static IReadOnlyList<PlayerId> Resolve(TrainingTarget target,IReadOnlyList<Player> squad,IReadOnlyList<TrainingGroup> groups)
    {
        if(!Enum.IsDefined(target.Scope)||target.Excluded?.Distinct().Count()!=target.Excluded?.Count)throw new InvalidDataException("Selección de entrenamiento inválida.");
        var chosen=target.Scope switch{TrainingScope.Team=>squad.Select(p=>p.Id),TrainingScope.Individual=>target.Player is {} player?new[]{player}:throw new InvalidDataException("Selecciona un futbolista."),TrainingScope.Position=>target.Position is {} role?squad.Where(p=>p.Positions?.Primary==role||p.Positions?.Secondary.Contains(role)==true).Select(p=>p.Id):throw new InvalidDataException("Selecciona demarcación."),TrainingScope.Group=>groups.SingleOrDefault(g=>g.Id==target.Group)?.Members??throw new InvalidDataException("Grupo inexistente."),_=>throw new InvalidDataException("Selección desconocida.")};
        var excluded=target.Excluded??Array.Empty<PlayerId>();if(excluded.Any(id=>squad.All(p=>p.Id!=id)))throw new InvalidDataException("Excepción ajena a la plantilla.");var result=chosen.Except(excluded).Distinct().OrderBy(p=>p.ToString(),StringComparer.Ordinal).ToArray();if(result.Length==0||result.Any(id=>squad.All(p=>p.Id!=id)))throw new InvalidDataException("No hay participantes propios elegibles.");return result;
    }
    public static TrainingResponsibility Responsibility(TrainingSession s)=>TrainingCatalog.Get(s.Exercise).Area switch{TrainingArea.Gym=>TrainingResponsibility.Gym,TrainingArea.Goalkeepers=>TrainingResponsibility.Goalkeepers,TrainingArea.SetPieces=>TrainingResponsibility.SetPieces,TrainingArea.Recovery=>TrainingResponsibility.Recovery,_=>s.Target.Scope switch{TrainingScope.Individual=>TrainingResponsibility.Individual,TrainingScope.Position=>TrainingResponsibility.Position,TrainingScope.Group=>TrainingResponsibility.Group,_=>TrainingResponsibility.Collective}};
    public static void Validate(TrainingPlan plan,IReadOnlyList<Player> players,IReadOnlyList<TrainingStaff> staff,IReadOnlyList<TrainingSession>? otherTeams=null,bool youthTeam=false)
    {
        if(plan.Team==Guid.Empty||plan.Revision<0||plan.Groups.Select(g=>g.Id).Distinct().Count()!=plan.Groups.Count||plan.Sessions.Select(s=>s.Id).Distinct().Count()!=plan.Sessions.Count||plan.Delegations.Select(d=>d.Area).Distinct().Count()!=plan.Delegations.Count)throw new InvalidDataException("Identidades o configuración repetidas.");
        foreach(var g in plan.Groups)if(g.Id==Guid.Empty||string.IsNullOrWhiteSpace(g.Name)||string.IsNullOrWhiteSpace(g.Objective)||g.Members.Count==0||g.Members.Distinct().Count()!=g.Members.Count||g.Members.Any(id=>players.All(p=>p.Id!=id))||g.Responsible is {} actor&&!staff.Any(p=>p.Id==actor&&p.Responsibilities.Contains(TrainingResponsibility.Group)))throw new InvalidDataException("Grupo o responsable no válido.");
        foreach(var d in plan.Delegations)if(!Enum.IsDefined(d.Area)||!Enum.IsDefined(d.Mode)||d.Responsible is not {} id||!staff.Any(p=>p.Id==id&&p.Responsibilities.Contains(d.Area)))throw new InvalidDataException("No existe personal disponible y competente para delegar esa área.");
        foreach(var s in plan.Sessions)
        {
            if(s.Exercise=="rest")throw new InvalidDataException("El descanso se confirma como jornada completa, no como ejercicio.");
            if(s.Load==TrainingLoad.Rest)throw new InvalidDataException("Selecciona una intensidad válida para el ejercicio.");
            var exercise=TrainingCatalog.Get(s.Exercise);if(s.Id==Guid.Empty||s.Team!=plan.Team||s.Date==default||s.Minutes<1||s.Start.ToTimeSpan().TotalMinutes+s.Minutes>1440||!Enum.IsDefined(s.Load)||s.FrequencyWeeks is <1 or >52||string.IsNullOrWhiteSpace(s.Objective))throw new InvalidDataException("Fecha, duración, frecuencia u objetivo no válido.");
            var expected=Resolve(s.Target,players,plan.Groups);if(!expected.ToHashSet().SetEquals(s.Participants)||s.Participants.Distinct().Count()!=s.Participants.Count)throw new InvalidDataException("Participantes desactualizados o duplicados; vuelve a confirmar la sesión.");
            if(exercise.Area==TrainingArea.Technology&&s.MinutesPerPlayer<5)throw new InvalidDataException("La rotación secuencial de Footbonaut necesita al menos cinco minutos por participante.");
            if(s.Routine is {} routine&&(!TrainingRoutines.Compatible(s.Exercise).Contains(routine.Kind)||routine.Location is {} location&&(!Enum.IsDefined(location)||!TrainingRoutines.Located(routine.Kind))))throw new InvalidDataException("La jugada no corresponde al ejercicio seleccionado.");
            if(exercise.GoalkeepersOnly&&s.Participants.Any(id=>players.Single(p=>p.Id==id).Positions?.Primary!=PlayerRole.Goalkeeper))throw new InvalidDataException("El ejercicio específico requiere porteros.");
            if(exercise.Area==TrainingArea.Technology&&s.Facility is null||exercise.Area==TrainingArea.SetPieces&&s.Routine is null||exercise.Area!=TrainingArea.SetPieces&&s.Routine is not null)throw new InvalidDataException("Falta instalación o referencia a la jugada diseñada en Táctica.");
            if(s.Responsible is {} responsible&&!staff.Any(p=>p.Id==responsible&&p.Responsibilities.Contains(Responsibility(s))))throw new InvalidDataException("Responsable sin competencia disponible para esta sesión.");
        }
        var all=plan.Sessions.Concat(otherTeams??Array.Empty<TrainingSession>()).ToArray();
        for(var i=0;i<all.Length;i++)for(var j=i+1;j<all.Length;j++)if(all[i].Id!=all[j].Id&&all[i].Begins<all[j].Ends&&all[j].Begins<all[i].Ends&&
            (all[i].Participants.Intersect(all[j].Participants).Any()||all[i].Responsible is {} r&&r==all[j].Responsible||all[i].Facility is {} f&&f==all[j].Facility))throw TrainingConflict.Error(all[i],all[j]);
    }
}
