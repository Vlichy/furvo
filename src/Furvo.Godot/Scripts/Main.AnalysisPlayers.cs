using Furvo.Core.Domain;
using Furvo.Core.Development;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private Guid _analysisPlayerTeam;
    private PlayerId? _analysisSelectedPlayer,_analysisComparedPlayer;
    private AttributeCategory _analysisAttributeCategory=AttributeCategory.Technical;
    private AttributeAnalysisSort _analysisAttributeSort=AttributeAnalysisSort.Name;
    private bool _analysisAttributesDescending,_analysisPlayerCollapsed,_analysisPlayerHistory;
    private void RenderAnalysisPlayers(VBoxContainer content,Guid team,IReadOnlyList<PlayerDetails> players,TrainingPlan plan,SqliteSportsStore service)
    {
        if(_analysisPlayerTeam!=team){_analysisPlayerTeam=team;_analysisSelectedPlayer=null;_analysisComparedPlayer=null;_analysisPlayerCollapsed=false;}
        if(players.All(p=>p.Player.Id!=_analysisSelectedPlayer))_analysisSelectedPlayer=null;
        if(players.All(p=>p.Player.Id!=_analysisComparedPlayer))_analysisComparedPlayer=null;
        var table=AnalysisTable("AnalysisPlayers");content.AddChild(table);var date=service.CareerDate();
        table.SetRows(players.Select(p=>{
            var physical=new SqliteTrainingExecutionStore(SportsPath).ObservedPhysical(p.Player.Id);
            var load=plan.Sessions.Where(s=>s.Date>=date&&s.Date<date.AddDays(7)&&s.Participants.Contains(p.Player.Id)).Sum(TrainingCatalog.PlannedLoad);
            return new TableRow(p.Player.Id.ToString(),new[]{p.Player.Name,UiText.Get("Position_"+p.Player.Position),p.Player.Age?.ToString()??"—",AnalysisNumber(load),physical is null?"—":AnalysisNumber(physical.Fatigue)});
        }),_analysisSelectedPlayer?.ToString());
        var panel=SportsCard(content,"AnalysisPlayerDetail");panel.Name="AnalysisPlayerDetail";
        // Las instantáneas ya guardadas se consultan; seleccionar y comparar no crea informes ni escribe en SQLite.
        var records=service.Reports(team).Where(r=>r.Context is not null).SelectMany(r=>r.Context!.Players.Select(p=>new RecordedPlayerAttributes(r.Id,r.Date,p.Details.Player.Id,p.Details.AttributeGroups))).ToArray();
        table.RowSelected+=id=>{_analysisSelectedPlayer=new PlayerId(Guid.Parse(id));if(_analysisComparedPlayer==_analysisSelectedPlayer)_analysisComparedPlayer=null;_analysisPlayerCollapsed=false;_analysisPlayerHistory=false;RenderPanel();};
        table.RowActivated+=id=>{if(team==_workspace!.Career.ClubId!.Value.Value)ShowPlayer(new PlayerId(Guid.Parse(id)));else ShowAcademyPlayer(team,new PlayerId(Guid.Parse(id)));};
        RenderPanel();
        void RenderPanel()
        {
            foreach(var child in panel.GetChildren()){panel.RemoveChild(child);child.QueueFree();}
            if(_analysisSelectedPlayer is not {} selected){panel.AddChild(ClubUi.Text("AnalysisPlayerPrompt",UiText.Get("AnalysisSelectPlayer"),16));return;}
            var view=PlayerAttributeAnalysis.Select(players,selected,_analysisComparedPlayer,_analysisAttributeCategory,_analysisAttributeSort,_analysisAttributesDescending);
            panel.SetMeta("selected_player",selected.ToString());
            panel.AddChild(ClubUi.Text("AnalysisSelectedIdentity",view.Player.Name+" · "+UiText.Get("Position_"+view.Player.Position)+" · "+UiText.Format("AnalysisPlayerAge",view.Player.Age?.ToString()??"—"),18,"f1b96b"));
            panel.AddChild(WindowUi.Button(_analysisPlayerCollapsed?"AnalysisExpandPlayer":"AnalysisCollapsePlayer",()=>{_analysisPlayerCollapsed=!_analysisPlayerCollapsed;RenderPanel();}));
            if(_analysisPlayerCollapsed)return;
            var controls=new HFlowContainer{Name="AnalysisPlayerControls"};controls.AddThemeConstantOverride("h_separation",12);controls.AddThemeConstantOverride("v_separation",8);panel.AddChild(controls);
            var compareBox=new VBoxContainer{CustomMinimumSize=new Vector2(260,0)};controls.AddChild(compareBox);compareBox.AddChild(ClubUi.Text("AnalysisCompareLabel",UiText.Get("AnalysisCompareLabel"),14));
            var candidates=players.Where(p=>p.Player.Id!=selected).ToArray();var compare=new OptionButton{Name="AnalysisComparePlayer",SizeFlagsHorizontal=SizeFlags.ExpandFill,ClipText=true};compare.AddItem(UiText.Get("AnalysisNoComparison"));foreach(var p in candidates)compare.AddItem(p.Player.Name);compare.Selected=_analysisComparedPlayer is {} other?Math.Max(0,Array.FindIndex(candidates,p=>p.Player.Id==other)+1):0;compareBox.AddChild(compare);compare.ItemSelected+=i=>{_analysisComparedPlayer=i==0?null:candidates[(int)i-1].Player.Id;RenderPanel();};
            var sortBox=new VBoxContainer{CustomMinimumSize=new Vector2(200,0)};controls.AddChild(sortBox);sortBox.AddChild(ClubUi.Text("AnalysisSortLabel",UiText.Get("AnalysisSortLabel"),14));
            var sort=new OptionButton{Name="AnalysisAttributeSort"};foreach(var order in Enum.GetValues<AttributeAnalysisSort>())sort.AddItem(UiText.Get("AnalysisAttributeSort_"+order));sort.Selected=(int)_analysisAttributeSort;sortBox.AddChild(sort);sort.ItemSelected+=i=>{_analysisAttributeSort=(AttributeAnalysisSort)i;RenderPanel();};
            controls.AddChild(WindowUi.Button(_analysisAttributesDescending?"AnalysisAttributeAscending":"AnalysisAttributeDescending",()=>{_analysisAttributesDescending=!_analysisAttributesDescending;RenderPanel();}));
            var tabs=new HFlowContainer{Name="AnalysisAttributeCategories"};panel.AddChild(tabs);
            foreach(var category in new[]{AttributeCategory.Technical,AttributeCategory.Mental,AttributeCategory.Physical,AttributeCategory.Goalkeeper}){var button=WindowUi.Button("Category_"+category,()=>{_analysisAttributeCategory=category;RenderPanel();});button.Name="AnalysisCategory_"+category;if(category==_analysisAttributeCategory)button.AddThemeColorOverride("font_color",_accent);tabs.AddChild(button);}
            panel.AddChild(ClubUi.Text("AnalysisRelevanceNotice",UiText.Get("AnalysisRelevanceNotice"),13));
            var headings=new HBoxContainer();panel.AddChild(headings);headings.AddChild(ClubUi.Text("AnalysisAttributeColumn",UiText.Get("AnalysisAttributeColumn"),14,"f1b96b"));
            headings.AddChild(ClubUi.Text("AnalysisSelectedColumn",view.Player.Name,14,"f1b96b"));
            if(view.Comparison is {} compared)headings.AddChild(ClubUi.Text("AnalysisComparedColumn",compared.Name+" · "+UiText.Get("Position_"+compared.Position)+" · "+UiText.Format("AnalysisPlayerAge",compared.Age?.ToString()??"—"),14,"f1b96b"));
            // Un único bloque de filas con anchuras compartidas; nunca se renderiza el resto de la plantilla.
            var rows=new VBoxContainer{Name="AnalysisAttributeRows",SizeFlagsHorizontal=SizeFlags.ExpandFill};rows.AddThemeConstantOverride("separation",3);panel.AddChild(rows);
            foreach(var attribute in view.Attributes)rows.AddChild(new AnalysisAttributeRow(attribute));
            panel.AddChild(WindowUi.Button("AnalysisRecordedHistory",()=>{_analysisPlayerHistory=!_analysisPlayerHistory;RenderPanel();}));
            if(_analysisPlayerHistory){
                var history=PlayerAttributeAnalysis.History(selected,_analysisAttributeCategory,records);
                if(history.Count==0)panel.AddChild(ClubUi.Text("AnalysisNoRecordedHistory",UiText.Get("AnalysisNoRecordedHistory"),14));
                else{panel.AddChild(ClubUi.Text("AnalysisHistorySource",UiText.Get("AnalysisHistorySource"),14));foreach(var change in history)panel.AddChild(ClubUi.Text("AnalysisHistory_"+change.Code,UiText.Get("Attribute_"+change.Code)+" · "+change.From.ToString("dd/MM/yyyy")+" → "+change.To.ToString("dd/MM/yyyy")+" · "+change.Before+" → "+change.After,14));}
            }
        }
    }
}
