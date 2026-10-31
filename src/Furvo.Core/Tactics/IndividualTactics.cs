using Furvo.Core.Domain;
namespace Furvo.Core.Tactics;

public sealed record IndividualInstruction(string Code,string Group,IReadOnlyList<string> Options,bool Goalkeeper=false);
public sealed record TacticalRoleDefinition(string Code,IReadOnlyList<PlayerRole> Positions,IReadOnlyDictionary<string,string> Defaults);
public sealed record SlotBehaviour(string Role,IReadOnlyDictionary<string,string> Overrides);
/// <summary>El puesto y la fase son el ancla, no la identidad de un titular. No calcula rendimiento.</summary>
public static class IndividualTactics
{
    public static IReadOnlyList<IndividualInstruction> Instructions {get;}=new IndividualInstruction[]{
        new("forward_runs","attack",new[]{"few","balanced","frequent"}),new("roaming","attack",new[]{"hold","spaces","free"}),
        new("runs","attack",new[]{"feet","mixed","space"}),new("width","attack",new[]{"inside","hold","outside"}),
        new("involvement","attack",new[]{"conservative","balanced","aggressive"}),
        new("risk","creation",new[]{"low","medium","high"},true),new("distance","creation",new[]{"short","mixed","long"},true),
        new("creativity","creation",new[]{"restricted","normal","high"}),new("dribbling","creation",new[]{"less","normal","more"}),
        new("shooting","finish",new[]{"less","normal","more"}),new("box_runs","finish",new[]{"few","balanced","frequent"}),
        new("crossing","finish",new[]{"less","normal","more"}),new("cross_target","finish",new[]{"near","centre","far","mixed"}),
        new("pressing","defend",new[]{"low","medium","high"}),new("marking","defend",new[]{"zonal","individual"}),
        new("tackling","defend",new[]{"cautious","normal","aggressive"}),new("tracking","defend",new[]{"hold","follow"}),
        new("recovery","defend",new[]{"positions","structure","repress"}),
        new("keeper_sweeping","defend",new[]{"hold","balanced","advance"},true)
    };
    public static IReadOnlyList<IndividualInstruction> Available(PlayerRole position,PossessionPhase phase)=>Instructions.Where(i=>
        (position==PlayerRole.Goalkeeper?i.Goalkeeper:i.Code!="keeper_sweeping")&&
        (phase==PossessionPhase.WithBall?i.Group!="defend":i.Group=="defend"||i.Code=="risk"||i.Code=="distance")&&
        (i.Code!="cross_target"||IsWide(position))).ToArray();
    private static bool IsWide(PlayerRole p)=>p is PlayerRole.LeftBack or PlayerRole.RightBack or PlayerRole.LeftMidfielder or PlayerRole.RightMidfielder or PlayerRole.LeftWinger or PlayerRole.RightWinger;
    public static IReadOnlyList<TacticalRoleDefinition> Roles {get;}=Build();
    private static IReadOnlyList<TacticalRoleDefinition> Build()
    {
        var result=new List<TacticalRoleDefinition>();
        void Add(string code,PlayerRole[] positions,params (string,string)[] values)=>result.Add(new(code,positions,values.ToDictionary(v=>v.Item1,v=>v.Item2)));
        var g=new[]{PlayerRole.Goalkeeper};var c=new[]{PlayerRole.CentreBack};var b=new[]{PlayerRole.LeftBack,PlayerRole.RightBack};
        var d=new[]{PlayerRole.DefensiveMidfielder};var m=new[]{PlayerRole.CentralMidfielder};var a=new[]{PlayerRole.AttackingMidfielder};
        var w=new[]{PlayerRole.LeftMidfielder,PlayerRole.RightMidfielder,PlayerRole.LeftWinger,PlayerRole.RightWinger};var s=new[]{PlayerRole.Striker,PlayerRole.SecondStriker};
        Add("keeper_classic",g,("keeper_sweeping","hold"),("distance","mixed"));Add("keeper_sweeper",g,("keeper_sweeping","advance"),("risk","medium"));Add("keeper_distributor",g,("keeper_sweeping","balanced"),("distance","short"),("risk","high"));
        Add("centre_defensive",c,("forward_runs","few"),("risk","low"),("tracking","hold"));Add("centre_playmaker",c,("distance","long"),("risk","high"));Add("centre_stopper",c,("pressing","high"),("tracking","follow"));Add("centre_cover",c,("pressing","low"),("tracking","hold"),("recovery","positions"));
        Add("back_defensive",b,("forward_runs","few"),("involvement","conservative"));Add("back_balanced",b,("forward_runs","balanced"),("width","hold"));Add("back_attacking",b,("forward_runs","frequent"),("width","outside"),("crossing","more"));Add("back_inverted",b,("width","inside"),("distance","short"),("risk","medium"));Add("wingback",b,("forward_runs","frequent"),("roaming","spaces"),("dribbling","more"),("crossing","more"));
        Add("pivot_defensive",d,("forward_runs","few"),("tracking","hold"),("risk","low"));Add("pivot_playmaker",d,("risk","high"),("distance","mixed"),("roaming","hold"));Add("ball_winner",d.Concat(m).ToArray(),("pressing","high"),("tackling","aggressive"),("tracking","follow"));Add("pivot_support",d,("distance","short"),("forward_runs","balanced"));
        Add("playmaker",m,("risk","high"),("creativity","high"));Add("box_to_box",m,("forward_runs","frequent"),("box_runs","frequent"),("recovery","positions"));Add("interior_support",m,("roaming","spaces"),("distance","short"));Add("midfield_runner",m,("box_runs","frequent"),("shooting","more"),("runs","space"));
        Add("advanced_playmaker",a,("risk","high"),("creativity","high"),("shooting","less"));Add("attacking_runner",a,("box_runs","frequent"),("runs","space"));Add("second_forward",a.Concat(new[]{PlayerRole.SecondStriker}).ToArray(),("shooting","more"),("involvement","aggressive"));Add("free_ten",a,("roaming","free"),("creativity","high"));
        Add("winger_classic",w,("width","outside"),("crossing","more"));Add("winger_inverted",w,("width","inside"),("dribbling","more"),("shooting","more"));Add("wide_midfielder",w,("forward_runs","few"),("distance","short"),("recovery","positions"));Add("winger_support",w,("runs","feet"),("risk","medium"),("shooting","less"));Add("winger_scorer",w,("box_runs","frequent"),("runs","space"),("crossing","less"));
        Add("poacher",s,("shooting","more"),("box_runs","frequent"),("runs","space"));Add("forward_support",s,("runs","feet"),("distance","short"),("shooting","less"));Add("pressing_forward",s,("pressing","high"),("tracking","follow"));Add("target_forward",s,("runs","feet"),("roaming","hold"),("risk","low"));Add("mobile_forward",s,("roaming","spaces"),("dribbling","more"));Add("false_nine",s,("roaming","free"),("creativity","high"),("risk","high"),("box_runs","few"));
        return result.AsReadOnly();
    }
    public static TacticalRoleDefinition Default(PlayerRole position)=>Roles.First(r=>r.Positions.Contains(position));
    public static SlotBehaviour Initial(PlayerRole position)=>new(Default(position).Code,new Dictionary<string,string>());
    public static IReadOnlyDictionary<string,string> Effective(SlotBehaviour behaviour,PlayerRole position,PossessionPhase phase)
    {
        Validate(behaviour,position,phase);var role=Roles.Single(r=>r.Code==behaviour.Role);
        return Available(position,phase).ToDictionary(i=>i.Code,i=>behaviour.Overrides.GetValueOrDefault(i.Code)??role.Defaults.GetValueOrDefault(i.Code)??Neutral(i));
    }
    public static string Neutral(IndividualInstruction instruction)=>instruction.Code switch{"roaming"=>"hold","cross_target"=>"mixed","marking"=>"zonal","tracking"=>"hold",_=>instruction.Options[instruction.Options.Count/2]};
    public static void Validate(SlotBehaviour value,PlayerRole position,PossessionPhase phase)
    {
        if(!Roles.Any(r=>r.Code==value.Role&&r.Positions.Contains(position)))throw new InvalidDataException("Rol incompatible con la demarcación.");
        var available=Available(position,phase);
        if(value.Overrides is null||value.Overrides.Any(v=>!available.Any(i=>i.Code==v.Key&&i.Options.Contains(v.Value))))throw new InvalidDataException("Instrucción individual incompatible con la posición o fase.");
    }
    public static SlotBehaviour SelectRole(SlotBehaviour previous,string code,PlayerRole position,PossessionPhase phase,bool replacePersonalization)
    {
        if(previous.Overrides.Count>0&&!replacePersonalization)throw new InvalidOperationException("Confirma el reemplazo o conserva la personalización actual.");
        var result=new SlotBehaviour(code,new Dictionary<string,string>());Validate(result,position,phase);return result;
    }
    public static SlotBehaviour Customize(SlotBehaviour value,string code,string option,PlayerRole position,PossessionPhase phase)
    {var overrides=value.Overrides.ToDictionary();overrides[code]=option;var result=value with{Overrides=overrides};Validate(result,position,phase);return result;}
    /// <summary>Una preferencia individual no desplaza las anclas geométricas ni sobrepasa las restricciones colectivas.</summary>
    public static IReadOnlyList<string> Warnings(SlotBehaviour value,PlayerRole position,PossessionPhase phase,IReadOnlyDictionary<string,string?> collective)
    {var effective=Effective(value,position,phase);var result=new List<string>();if(collective.GetValueOrDefault("defensive_line")=="low"&&effective.GetValueOrDefault("pressing")=="high")result.Add("IndividualLowLineWarning");if(collective.GetValueOrDefault("freedom")=="disciplined"&&effective.GetValueOrDefault("roaming")=="free")result.Add("IndividualFreedomWarning");return result;}
}
