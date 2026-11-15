using Furvo.Core.Development;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private AnalysisScope _reportScope=AnalysisScope.Collective;
    private AnalysisReportKind _reportKind=AnalysisReportKind.Technical;
    private Guid? _reportStaff,_reportSelected;
    private Guid _reportTeam;
    private PlayerId? _reportPlayer;
    private PlayerRole _reportPosition=PlayerRole.Goalkeeper;
    private Guid? _reportGroup;
    private bool _reportSecondary,_reportGroups;
    private static string AnalysisNumber(decimal value)=>value.ToString("0.0",System.Globalization.CultureInfo.GetCultureInfo("es-ES"));
    private static DataTable AnalysisTable(string name)=>new([
        new(UiText.Get("Name"),210,36,false,HorizontalAlignment.Left,HorizontalAlignment.Left),
        new(UiText.Get("Position"),150,24,false,HorizontalAlignment.Left,HorizontalAlignment.Left),
        new(UiText.Get("Age"),50,8,false,HorizontalAlignment.Center,HorizontalAlignment.Center),
        new(UiText.Get("TrainingPlannedLoad"),110,16,false,HorizontalAlignment.Right,HorizontalAlignment.Right),
        new(UiText.Get("TrainingObservedFatigue"),110,16,false,HorizontalAlignment.Center,HorizontalAlignment.Center)],
        rowHeight:36,rowSpacing:2,constrainColumns:true){Name=name,CustomMinimumSize=new Vector2(0,260)};
    private void RenderAnalysisReports(VBoxContainer content,Label status,Guid team)
    {
        var service=new SqliteAnalysisReportService(SportsPath);var sports=Sports();var squads=new SqliteClubSquadStore(SportsPath);var players=squads.Details(team);var plan=sports.Plan(team);
        if(_reportTeam!=team){_reportTeam=team;_reportStaff=null;_reportPlayer=null;_reportGroup=null;_reportSelected=null;_reportGroups=false;}
        void Heading(string key)=>content.AddChild(ClubUi.Text(key,UiText.Get(key),15,"f1b96b"));
        Heading("AnalysisScopeLabel");var scope=new OptionButton{Name="AnalysisReportScope"};foreach(var value in Enum.GetValues<AnalysisScope>())scope.AddItem(UiText.Get("AnalysisScope_"+value));scope.Selected=(int)_reportScope-1;content.AddChild(scope);scope.ItemSelected+=i=>{_reportScope=(AnalysisScope)(i+1);ShowSportsAnalysis();};
        Heading("AnalysisContentLabel");var kind=new OptionButton{Name="AnalysisReportKind"};foreach(var value in Enum.GetValues<AnalysisReportKind>())kind.AddItem(UiText.Get("AnalysisContent_"+value));kind.Selected=(int)_reportKind-1;content.AddChild(kind);kind.ItemSelected+=i=>{_reportKind=(AnalysisReportKind)(i+1);ShowSportsAnalysis();};
        Heading("AnalysisStaffLabel");var staff=service.Staff(team,_reportKind);var owner=new OptionButton{Name="AnalysisReportStaff"};owner.AddItem(UiText.Get("AnalysisChooseStaff"));foreach(var p in staff)owner.AddItem(p.Name+" · "+UiText.Get("AnalysisStaff_"+p.Role));owner.Selected=_reportStaff is {} id?Math.Max(0,staff.ToList().FindIndex(p=>p.Id==id)+1):0;content.AddChild(owner);owner.ItemSelected+=i=>_reportStaff=i==0?null:staff[(int)i-1].Id;
        if(staff.Count==0)content.AddChild(ClubUi.Text("AnalysisNoStaff",UiText.Get("AnalysisNoStaff"),14));
        if(_reportScope==AnalysisScope.Individual){Heading("AnalysisPlayerLabel");var select=new OptionButton{Name="AnalysisReportPlayer"};foreach(var p in players)select.AddItem(p.Player.Name);select.Selected=Math.Max(0,players.ToList().FindIndex(p=>p.Player.Id==_reportPlayer));_reportPlayer=players.Count>0?players[select.Selected].Player.Id:null;content.AddChild(select);select.ItemSelected+=i=>_reportPlayer=players[(int)i].Player.Id;}
        if(_reportScope==AnalysisScope.Position){Heading("AnalysisPositionLabel");var roles=Enum.GetValues<PlayerRole>();var select=new OptionButton{Name="AnalysisReportPosition"};foreach(var role in roles)select.AddItem(UiText.Get("Position_"+role));select.Selected=Array.IndexOf(roles,_reportPosition);content.AddChild(select);select.ItemSelected+=i=>_reportPosition=roles[(int)i];var secondary=new PriorityChoice(UiText.Get("AnalysisIncludeSecondary"),_reportSecondary){Name="AnalysisIncludeSecondary"};content.AddChild(secondary);secondary.Indicator.Toggled+=v=>_reportSecondary=v;}
        if(_reportScope==AnalysisScope.Group){Heading("AnalysisGroupLabel");var select=new OptionButton{Name="AnalysisReportGroup"};foreach(var group in plan.Groups)select.AddItem(group.Name);select.Selected=Math.Max(0,plan.Groups.ToList().FindIndex(g=>g.Id==_reportGroup));_reportGroup=plan.Groups.Count>0?plan.Groups[select.Selected].Id:null;content.AddChild(select);select.ItemSelected+=i=>_reportGroup=plan.Groups[(int)i].Id;
            content.AddChild(WindowUi.Button("AnalysisManageGroups",()=>{_reportGroups=!_reportGroups;ShowSportsAnalysis();}));
            if(_reportGroups){status.Name="TrainingStatus";RenderTrainingGroups(content,plan,squads.Candidates(team),sports.Staff(team),ShowSportsAnalysis,id=>_reportGroup=id);}
        }
        content.AddChild(WindowUi.Button("AnalysisCreateSnapshot",()=>{try{if(_reportStaff is not {} responsible)throw new InvalidDataException(UiText.Get("AnalysisChooseStaff"));var selection=new AnalysisSelection(_reportScope,_reportScope==AnalysisScope.Individual?_reportPlayer:null,_reportScope==AnalysisScope.Position?_reportPosition:null,_reportScope==AnalysisScope.Position&&_reportSecondary,_reportScope==AnalysisScope.Group?_reportGroup:null);_reportSelected=service.Generate(team,selection,_reportKind,responsible).Id;ShowSportsAnalysis();}catch(Exception e){SportsError(e,status);}}));
        var reports=sports.Reports(team).OrderByDescending(r=>r.Date).ThenBy(r=>r.Id).ToArray();if(reports.Length==0)return;
        Heading("AnalysisHistory");var history=new OptionButton{Name="AnalysisReportHistory"};foreach(var r in reports)history.AddItem(r.Date.ToString("dd/MM/yyyy")+" · "+UiText.Get("AnalysisContent_"+r.Kind)+" · "+(r.Context is {} ctx?UiText.Get("AnalysisScope_"+ctx.Selection.Scope):UiText.Get("AnalysisLegacy")));history.Selected=Math.Max(0,Array.FindIndex(reports,r=>r.Id==_reportSelected));content.AddChild(history);history.ItemSelected+=i=>{_reportSelected=reports[(int)i].Id;ShowSportsAnalysis();};
        RenderStoredAnalysisReport(content,reports[history.Selected],priorities=>{try{service.SavePriorities(team,reports[history.Selected].Id,priorities);ShowSportsAnalysis();}catch(Exception e){SportsError(e,status);}});
    }
    /// <summary>La misma instantánea se presenta desde Análisis y Cantera; consultar en Cantera no modifica prioridades.</summary>
    private void RenderStoredAnalysisReport(VBoxContainer content,AnalysisReport report,Action<IReadOnlyList<string>>? save=null,PlayerId? focus=null)
    {
        var card=SportsCard(content,"AnalysisReport_"+report.Id);
        if(report.Context is {} context){
            card.AddChild(ClubUi.Text("AnalysisReportTitle",context.TeamName+" · "+UiText.Get("AnalysisScope_"+context.Selection.Scope)+" · "+report.Date.ToString("dd/MM/yyyy"),18,"f1b96b"));
            card.AddChild(ClubUi.Text("AnalysisAssignedStaff",context.Staff.Name+" · "+UiText.Get("AnalysisStaff_"+context.Staff.Role),15));
            if(context.GroupName is {} groupName)card.AddChild(ClubUi.Text("AnalysisFrozenGroup",groupName,16));
            if(context.Selection.Position is {} position)card.AddChild(ClubUi.Text("AnalysisFrozenPosition",UiText.Get("Position_"+position)+" · "+UiText.Get(context.Selection.IncludeSecondary?"AnalysisSecondaryIncluded":"AnalysisPrimaryOnly"),15));
            var table=AnalysisTable("AnalysisReportPlayers");card.AddChild(table);table.SetRows(context.Players.Where(p=>focus is null||p.Details.Player.Id==focus).Select(p=>new TableRow(p.Details.Player.Id.ToString(),new[]{p.Details.Player.Name,UiText.Get("Position_"+p.Details.Player.Position),p.Details.Player.Age?.ToString()??"—",AnalysisNumber(p.PlannedLoad),p.ObservedFatigue is {} fatigue?AnalysisNumber(fatigue):"—"})));
            if(context.Selection.Scope==AnalysisScope.Position)foreach(var player in context.Players.Where(p=>focus is null||p.Details.Player.Id==focus)){var relevant=PositionInsightCatalog.RelevantFor(context.Selection.Position!.Value).ToHashSet();card.AddChild(ClubUi.Text("AnalysisPositionAttributes",player.Details.Player.Name+" · "+string.Join(" · ",player.Details.AttributeGroups.SelectMany(g=>g.Values).Where(a=>relevant.Contains(a.Code)).Select(a=>UiText.Get("Attribute_"+a.Code)+": "+a.Value)),14));}
            if(report.Kind==AnalysisReportKind.Individual||context.Selection.Scope==AnalysisScope.Individual)
                foreach(var player in context.Players.Where(p=>focus is null||p.Details.Player.Id==focus)){card.AddChild(ClubUi.Text("AnalysisPlayerName",player.Details.Player.Name,17,"f1b96b"));foreach(var group in player.Details.AttributeGroups){card.AddChild(ClubUi.Text("AnalysisCategory",UiText.Get("Category_"+group.Category),15,"f1b96b"));card.AddChild(ClubUi.Text("AnalysisAttributes",string.Join(" · ",group.Values.Select(a=>UiText.Get("Attribute_"+a.Code)+": "+a.Value)),14));}}
        }else card.AddChild(ClubUi.Text("AnalysisLegacyNotice",UiText.Get("AnalysisLegacyNotice"),15));
        card.AddChild(ClubUi.Text("AnalysisPriorityHeading",UiText.Get("AnalysisPriorities"),17,"f1b96b"));var choices=new List<PriorityChoice>();
        var findings=report.Findings.Where(f=>focus is null||!f.Topic.StartsWith("player/",StringComparison.Ordinal)||f.Topic.StartsWith("player/"+focus+"/",StringComparison.Ordinal)).ToArray();
        foreach(var finding in findings){var text=finding.Topic is "selected-count" or "squad-count"?UiText.Get("TrainingPlayers")+": "+finding.Content:finding.Topic=="scheduled-sessions"?UiText.Get("TrainingSessionsPlanned")+": "+finding.Content:finding.Content;if(finding.Topic.Contains("/weakness/"))text=UiText.Get("WeaknessesHeading")+" · "+text;else if(finding.Topic.Contains("/strength/"))text=UiText.Get("StrengthsHeading")+" · "+text;else if(finding.Topic.EndsWith("/planned-load",StringComparison.Ordinal))text=UiText.Get("TrainingPlannedLoad")+" · "+text;var choice=new PriorityChoice(text,report.Priorities.Contains(finding.Topic)){TooltipText=finding.Source};choice.Indicator.Disabled=save is null;card.AddChild(choice);choices.Add(choice);}
        if(save is not null)card.AddChild(WindowUi.Button("AnalysisSavePriorities",()=>save(findings.Where((_,i)=>choices[i].Checked).Select(f=>f.Topic).ToArray())));
    }
}
