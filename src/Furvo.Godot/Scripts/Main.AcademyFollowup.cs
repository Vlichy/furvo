using Furvo.Core.Academy;
using Furvo.Core.Domain;
using Furvo.Core.Development;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private Action? _academyAnalysisReturn;
    private Guid? _academyNoteSelected,_academyReportSelected;
    private bool _academyNotesDescending=true;
    private void ShowAcademyFollowup(Guid team,PlayerId player,int tab=0)
    {
        try{
            var details=Academy().Player(team,player);_academyAnalysisReturn=null;_academyTeam=team;_academyListSelection=team;_workspace!.Open(Furvo.Core.Careers.CareerPage.Academy);AcademyContext(team).Selected=player;
            var body=Frame("CareerSection",UiText.Get("AcademyFollowupTitle"),details.Player.Name+" · "+(details.Club?.Name??UiText.Get("NotAvailable")));body.Name="AcademyFollowupPage";body.SetMeta("followup_player",player.ToString());body.SetMeta("followup_team",team.ToString());
            var tabs=new HFlowContainer{Name="AcademyFollowupTabs"};body.AddChild(tabs);for(var i=0;i<3;i++){var index=i;var button=WindowUi.Button("AcademyFollowupTab_"+i,()=>ShowAcademyFollowup(team,player,index));TrainingUi.Tab(button,i==tab);tabs.AddChild(button);}
            var content=ClubUi.ScrollBody(body,"AcademyFollowupScroll");var store=new SqliteAcademyFollowupStore(SportsPath);
            if(tab==0)RenderAcademyObservations(content,store,team,player);else if(tab==1)RenderAcademyReports(content,store,team,player);else RenderAcademyMovementHistory(content,player);
            body.AddChild(WindowUi.Button("AcademyFollowupBackPlayer",()=>ShowAcademyPlayer(team,player)));body.AddChild(WindowUi.Button("AcademyFollowupBackRoster",()=>OpenAcademyTeam(team,AcademyContext(team).Section)));
        }catch(Exception error){GD.PushError(error.ToString());ShowError("AcademyLoadError",()=>OpenAcademyTeam(team,AcademyContext(team).Section),ShowAcademy);}
    }
    private void RenderAcademyObservations(VBoxContainer content,SqliteAcademyFollowupStore store,Guid team,PlayerId player)
    {
        var notes=store.Observations(player);var status=ClubUi.Text("AcademyObservationStatus","",14,"f1b96b");content.AddChild(status);
        var actions=new HFlowContainer();content.AddChild(actions);actions.AddChild(WindowUi.Button("AcademyObservationNew",()=>EditAcademyObservation(store,team,player,null)));
        actions.AddChild(WindowUi.Button("AcademyObservationSort",()=>{_academyNotesDescending=!_academyNotesDescending;ShowAcademyFollowup(team,player);}));
        if(notes.Count==0){content.AddChild(ClubUi.Text("AcademyObservationsEmpty",UiText.Get("AcademyObservationsEmpty"),16));return;}
        var ordered=(_academyNotesDescending?notes.OrderByDescending(n=>n.RegisteredOn):notes.OrderBy(n=>n.RegisteredOn)).ThenBy(n=>n.Id).ToArray();
        if(!notes.Any(n=>n.Id==_academyNoteSelected))_academyNoteSelected=ordered[0].Id;
        var table=new DataTable([new(UiText.Get("AcademyObservationDate"),100,15,true),new(UiText.Get("AcademyObservationAuthor"),180,25),new(UiText.Get("AcademyObservationText"),220,60,Wrap:true)]){Name="AcademyObservationsTable",CustomMinimumSize=new Vector2(0,190)};content.AddChild(table);
        table.SetRows(ordered.Select(n=>new TableRow(n.Id.ToString(),new[]{n.RegisteredOn.ToString("dd/MM/yyyy"),n.AuthorName,n.Text})),_academyNoteSelected?.ToString());table.MarkSort(0,_academyNotesDescending);
        void Choose(string id){_academyNoteSelected=Guid.Parse(id);ShowAcademyFollowup(team,player);}table.RowSelected+=Choose;table.SortRequested+=_=>{_academyNotesDescending=!_academyNotesDescending;ShowAcademyFollowup(team,player);};
        var selected=notes.Single(n=>n.Id==_academyNoteSelected);content.AddChild(ClubUi.Text("AcademyObservationSelected",selected.Text,15));
        if(selected.ModifiedOn is {} changed)content.AddChild(ClubUi.Text("AcademyObservationModified",UiText.Get("AcademyObservationModified")+": "+changed.ToString("dd/MM/yyyy"),13));
        var controls=new HFlowContainer();content.AddChild(controls);controls.AddChild(WindowUi.Button("AcademyObservationEdit",()=>EditAcademyObservation(store,team,player,selected)));
        controls.AddChild(WindowUi.Button("AcademyObservationDelete",()=>{
            var dialog=new ConfirmationDialog{Name="AcademyObservationDeleteDialog",Title=UiText.Get("AcademyFollowupTitle"),DialogText=UiText.Get("AcademyObservationDeleteConfirm"),OkButtonText=UiText.Get("PlanConfirm"),CancelButtonText=UiText.Get("PlanCancel")};AddChild(dialog);
            dialog.Confirmed+=()=>{try{store.Delete(selected.Id,_workspace!.Career.Coach.Id.Value,selected.Revision);dialog.QueueFree();ShowAcademyFollowup(team,player);}catch(Exception error){dialog.QueueFree();status.Text=error.Message;}};dialog.Canceled+=()=>dialog.QueueFree();dialog.PopupCentered(new Vector2I(520,180));
        }));
    }
    private void EditAcademyObservation(SqliteAcademyFollowupStore store,Guid team,PlayerId player,AcademyObservation? note)
    {
        var dialog=new ConfirmationDialog{Name="AcademyObservationEditor",Title=UiText.Get(note is null?"AcademyObservationNew":"AcademyObservationEdit"),DialogHideOnOk=false,Exclusive=true,OkButtonText=UiText.Get("AcademyObservationSave"),CancelButtonText=UiText.Get("PlanCancel")};AddChild(dialog);
        var body=new VBoxContainer();dialog.AddChild(body);body.AddChild(ClubUi.Text("AcademyObservationAuthor",UiText.Get("AcademyObservationAuthor")+": "+(note?.AuthorName??_workspace!.Career.Coach.ProfileSnapshot.FullName),14));
        body.AddChild(ClubUi.Text("AcademyObservationDate",UiText.Get("AcademyObservationDate")+": "+(note?.RegisteredOn??Sports().CareerDate()).ToString("dd/MM/yyyy"),14));
        var text=new TextEdit{Name="AcademyObservationInput",Text=note?.Text??"",WrapMode=TextEdit.LineWrappingMode.Boundary,CustomMinimumSize=new Vector2(0,170)};body.AddChild(text);var status=ClubUi.Text("AcademyObservationEditorStatus","",14,"f1b96b");body.AddChild(status);
        dialog.Confirmed+=()=>{try{var actor=_workspace!.Career.Coach.Id.Value;var saved=note is null?store.Create(Guid.NewGuid(),team,player,text.Text,actor):store.Edit(note.Id,text.Text,actor,note.Revision);_academyNoteSelected=saved.Id;dialog.QueueFree();ShowAcademyFollowup(team,player);}catch(Exception error){status.Text=error.Message;}};
        dialog.Canceled+=()=>dialog.QueueFree();dialog.PopupCentered(new Vector2I(Math.Min(680,(int)GetViewportRect().Size.X-60),300));text.GrabFocus();
    }
    private void RenderAcademyReports(VBoxContainer content,SqliteAcademyFollowupStore store,Guid team,PlayerId player)
    {
        var reports=store.Reports(player);if(reports.Count==0){content.AddChild(ClubUi.Text("AcademyReportsEmpty",UiText.Get("AcademyReportsEmpty"),16));return;}
        string Caption(AnalysisReport report)=>report.Date.ToString("dd/MM/yyyy")+" · "+UiText.Get("AnalysisContent_"+report.Kind)+" · "+(report.Context?.Staff.Name??UiText.Get("AcademyNotRecorded"))+" · "+report.Id.ToString("N")[..8];
        var select=new OptionButton{Name="AcademyReportSelect",FitToLongestItem=false,ClipText=true};foreach(var report in reports){select.AddItem(Caption(report));select.SetItemMetadata(select.ItemCount-1,report.Id.ToString());}select.Selected=Math.Max(0,reports.ToList().FindIndex(r=>r.Id==_academyReportSelected));var selected=reports[select.Selected];_academyReportSelected=selected.Id;content.AddChild(select);select.ItemSelected+=i=>{_academyReportSelected=reports[(int)i].Id;ShowAcademyFollowup(team,player,1);};TrainingUi.LimitPopup(select);
        content.AddChild(WindowUi.Button("AcademyOpenAnalysisReport",()=>{
            _trainingTeam=selected.Team;_analysisTab=3;_reportTeam=selected.Team;_reportSelected=selected.Id;_reportScope=AnalysisScope.Individual;_reportPlayer=player;
            _academyAnalysisReturn=()=>ShowAcademyFollowup(team,player,1);NavigateCareer(Furvo.Core.Careers.CareerPage.Analysis);
        }));
        var compare=new OptionButton{Name="AcademyCompareReport",FitToLongestItem=false,ClipText=true};compare.AddItem(UiText.Get("AcademyReportCompareNone"));var others=reports.Where(r=>r.Id!=selected.Id&&r.Context is not null&&selected.Context is not null).ToArray();foreach(var other in others)compare.AddItem(Caption(other));content.AddChild(compare);
        var comparison=new VBoxContainer{Name="AcademyReportComparison"};content.AddChild(comparison);compare.ItemSelected+=index=>{
            foreach(var child in comparison.GetChildren()){comparison.RemoveChild(child);child.QueueFree();}if(index==0)return;
            try{var rows=AcademyFollowup.Compare(selected,others[(int)index-1],player);comparison.AddChild(ClubUi.Text("AcademyReportCompareNotice",UiText.Get("AcademyReportCompareNotice"),13));
                var table=new DataTable([new(UiText.Get("AcademyReportAttribute"),180,60),new(selected.Date.ToString("dd/MM/yyyy"),90,20),new(others[(int)index-1].Date.ToString("dd/MM/yyyy"),90,20)],selectable:false){Name="AcademyReportComparisonTable",CustomMinimumSize=new Vector2(0,220)};comparison.AddChild(table);table.SetRows(rows.Select(r=>new TableRow(r.Code,new[]{UiText.Get("Attribute_"+r.Code),r.First.ToString(),r.Second.ToString()})));
            }catch(Exception error){comparison.AddChild(ClubUi.Text("AcademyReportComparisonError",error.Message,14));}
        };RenderStoredAnalysisReport(content,selected,focus:player);
    }
    private void RenderAcademyMovementHistory(VBoxContainer content,PlayerId player)
    {
        var store=new SqliteAcademyMobilityStore(SportsPath);var teams=SportsTeams().ToDictionary(t=>t.Id,t=>t.Name);var staff=Academy().List().ToDictionary(t=>t.Coach.Id,t=>t.Coach.Name);
        string Team(Guid id)=>teams.GetValueOrDefault(id)??UiText.Get("AcademyNotRecorded")+" ("+id+")";
        var entries=new List<(DateOnly Date,string Id,string Type,string State,string Summary,string Details)>();
        foreach(var proposal in store.Recommendations().Where(p=>p.Player==player))entries.Add((proposal.Date,"proposal:"+proposal.Id,UiText.Get("AcademyHistoryRecommendation"),UiText.Get("RecommendationStatus_"+proposal.Status),Team(proposal.Source)+" → "+Team(proposal.Destination),
            UiText.Get("MovementKind_"+proposal.Kind)+"\n"+UiText.Get("AcademyObservationAuthor")+": "+(staff.GetValueOrDefault(proposal.Coach)??UiText.Get("AcademyNotRecorded"))+"\n"+proposal.Reason+"\n"+(proposal.Decision??UiText.Get("AcademyHistoryNoDecision"))+"\n"+UiText.Get("AcademyHistoryDecisionAuthorUnknown")+"\n"+(proposal.RegulatorySummary??"")+"\n"+UiText.Get("AcademyHistoryReference")+": "+proposal.Id));
        foreach(var movement in store.History(player))entries.Add((movement.Date,"movement:"+movement.Id,UiText.Get("AcademyHistoryMovement"),UiText.Get("AcademyHistoryExecuted"),Team(movement.Request.Source)+" → "+Team(movement.Request.Destination),
            UiText.Get("MovementKind_"+movement.Request.Kind)+"\n"+UiText.Get("AcademyHistoryDecisionAuthorUnknown")+"\n"+UiText.Get("AcademyHistoryReference")+": movement_history/"+movement.Id+"\n"+movement.Evidence));
        content.AddChild(ClubUi.Text("AcademyHistoryReadOnly",UiText.Get("AcademyHistoryReadOnly"),13,"f1b96b"));
        if(entries.Count==0){content.AddChild(ClubUi.Text("AcademyHistoryEmpty",UiText.Get("AcademyHistoryEmpty"),16));return;}
        var table=new DataTable([new(UiText.Get("AcademyObservationDate"),90,12),new(UiText.Get("AcademyHistoryType"),130,18),new(UiText.Get("AcademyHistoryState"),110,16),new(UiText.Get("AcademyHistoryRoute"),230,54,Wrap:true)]){Name="AcademyMovementHistoryTable",CustomMinimumSize=new Vector2(0,210)};content.AddChild(table);
        var detail=ClubUi.Text("AcademyMovementHistoryDetail","",14);content.AddChild(detail);var ordered=entries.OrderByDescending(e=>e.Date).ThenBy(e=>e.Id,StringComparer.Ordinal).ToArray();table.SetRows(ordered.Select(e=>new TableRow(e.Id,new[]{e.Date.ToString("dd/MM/yyyy"),e.Type,e.State,e.Summary})),ordered[0].Id);detail.Text=ordered[0].Details;table.RowSelected+=id=>detail.Text=ordered.Single(e=>e.Id==id).Details;
    }
}
