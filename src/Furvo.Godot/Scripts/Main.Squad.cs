using Furvo.Persistence;
using System.Globalization;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Core.Lineups;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private SquadTrackingSort _careerSquadSort=SquadTrackingSort.Position;
    private bool _careerSquadDescending;
    /// <summary>Una tabla de seguimiento. No consulta atributos para inventar calidad ni estado físico.</summary>
    private void ShowCareerSquad()
    {
        try
        {
            var career=_workspace!.Career;var id=career.ClubId??throw new InvalidOperationException("Sin club.");_workspace.RequireManagement(id);_workspace.Open(CareerPage.OwnSquad);
            _club=_queries!.GetClubs().Single(c=>c.Id==id);var season=_queries.GetSeasons().Single(s=>s.Id==career.SeasonId);
            // No existe aún fuente de actuaciones; solo el estado inicial confirmado permite afirmar ceros.
            var initialNoMatches=career.CurrentDate==season.StartDate;
            var lineup=Lineups().Load();
            var assignments=lineup?.Members.ToDictionary(m=>m.PlayerId,m=>(SquadAssignment)(int)m.Role);
            var rows=SquadTrackingRows.Sort(new SqliteClubSquadStore(SportsPath).Details(id.Value).Select(d=>d.Player).Select(p=>new SquadTrackingRow(p,assignments?.GetValueOrDefault(p.Id)??SquadAssignment.Unassigned,PlayerSportingStatus.Initial(initialNoMatches))),_careerSquadSort,_careerSquadDescending);
            if(_selectedPlayer is not null&&!rows.Any(r=>r.Player.Id.ToString()==_selectedPlayer))_selectedPlayer=null;
            var body=Frame("CareerSection",UiText.Get("NavPage_OwnSquad"),UiText.Format("CareerSquadHeading",_club.Name,rows.Count,season.Name));
            var table=new DataTable([
                new(UiText.Get("SquadAssignment"),90,8,true,Wrap:true),new(UiText.Get("Position"),60,5,true),new(UiText.Get("SquadPlayer"),210,21,true,HeaderAlignment:HorizontalAlignment.Left,Wrap:true),
                new(UiText.Get("SquadQuality"),88,8),new(UiText.Get("SquadPhysical"),128,12,rows.Any(r=>r.Status.Physical.Percentage.HasValue)),new(UiText.Get("SquadRhythm"),86,8,rows.Any(r=>r.Status.Rhythm.Percentage.HasValue)),
                new(UiText.Get("SquadMorale"),100,9,rows.Any(r=>r.Status.Morale.HasValue)),new(UiText.Get("SquadForm"),140,10),
                new(UiText.Get("SquadPlayed"),40,4,initialNoMatches,CellAlignment:HorizontalAlignment.Center),new(UiText.Get("SquadGoals"),32,4,initialNoMatches,CellAlignment:HorizontalAlignment.Center),
                new(UiText.Get("SquadAssists"),32,4,initialNoMatches,CellAlignment:HorizontalAlignment.Center),new(UiText.Get("SquadAverage"),55,7,rows.Any(r=>r.Status.Season?.Average is not null),CellAlignment:HorizontalAlignment.Center)],rowHeight:48,rowSpacing:4){Name="SquadTable"};
            body.AddChild(table);body.AddChild(ClubUi.Text("SquadDataNotice",UiText.Get("SquadDataNotice"),13,"a6b6ca"));
            var open=Button("ViewPlayer",()=>{if(_selectedPlayer is not null)ShowPlayer(new PlayerId(Guid.Parse(_selectedPlayer)));});open.Disabled=_selectedPlayer is null;body.AddChild(open);
            table.RowSelected+=selected=>{_selectedPlayer=selected;open.Disabled=false;};table.RowActivated+=selected=>ShowPlayer(new PlayerId(Guid.Parse(selected)));
            table.SortRequested+=column=>{var sort=column switch{0=>SquadTrackingSort.Assignment,1=>SquadTrackingSort.Position,2=>SquadTrackingSort.Player,4=>SquadTrackingSort.Physical,5=>SquadTrackingSort.Rhythm,6=>SquadTrackingSort.Morale,8=>SquadTrackingSort.Played,9=>SquadTrackingSort.Goals,10=>SquadTrackingSort.Assists,_=>SquadTrackingSort.Average};
                _careerSquadDescending=sort==_careerSquadSort&&!_careerSquadDescending;_careerSquadSort=sort;ShowCareerSquad();};
            table.SetRows(rows.Select(r=>
            {
                string Number(int? value)=>value?.ToString(CultureInfo.InvariantCulture)??"—";
                var visuals=new Dictionary<int,TableCellVisual>();
                foreach(var pair in new[]{(3,SportingIndicatorKind.Quality),(4,SportingIndicatorKind.Physical),(5,SportingIndicatorKind.Rhythm),(6,SportingIndicatorKind.Morale),(7,SportingIndicatorKind.Form)})
                {
                    var model=new SportingIndicatorModel(pair.Item2,r.Status);var description=SportingIndicator.Describe(model);
                    visuals[pair.Item1]=new(pair.Item2.ToString(),pair.Item2==SportingIndicatorKind.Morale?1:5,(canvas,rect)=>SportingIndicator.Paint(canvas,rect,model),description);
                }
                return new TableRow(r.Player.Id.ToString(),new[]{UiText.Get("SquadAssignment_"+r.Assignment),r.Player.Position is {} position?UiText.Get("PositionCode_"+position):UiText.Get("Unknown"),r.Player.Name,
                    "—","—","—","—","—",Number(r.Status.Season?.Played),Number(r.Status.Season?.Goals),Number(r.Status.Season?.Assists),r.Status.Season?.Average?.ToString("0.00",CultureInfo.GetCultureInfo("es-ES"))??"—"},Visuals:visuals);
            }),_selectedPlayer);
            table.MarkSort(_careerSquadSort switch{SquadTrackingSort.Assignment=>0,SquadTrackingSort.Position=>1,SquadTrackingSort.Player=>2,SquadTrackingSort.Physical=>4,SquadTrackingSort.Rhythm=>5,SquadTrackingSort.Morale=>6,SquadTrackingSort.Played=>8,SquadTrackingSort.Goals=>9,SquadTrackingSort.Assists=>10,_=>11},_careerSquadDescending);table.FocusTable();
        }
        catch(Exception error){GD.PushError(error.ToString());ShowError("CareerSquadError",ShowCareerSquad,()=>NavigateCareer(CareerPage.Desktop));}
    }
}
