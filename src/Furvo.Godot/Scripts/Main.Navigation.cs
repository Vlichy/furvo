using System.Globalization;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;

/// <summary>Espacio de trabajo de carrera: consulta su copia SQLite, nunca el catálogo ni el último club explorado.</summary>
public partial class Main
{
    private CareerWorkspace? _workspace;
    private bool CareerActive=>_workspace is not null;
    private void EnterCareer(CareerSummary career)
    {
        if(TrainingGroupDirty){TrainingGroupGuard(()=>EnterCareer(career));return;}
        var loaded=_careers!.Load(career.Id);
        var data=new SqliteWorldStore(_careers.PathFor(loaded.Id)).Load();
        _queries=new WorldQueries(data,loaded.CurrentDate,SqliteEconomyStore.ReadResources(_careers.PathFor(loaded.Id)));
        _marketContext=new(loaded.Id.Value);_marketSnapshot=null;_academyContexts.Clear();_academyListSelection=null;_academyAnalysisReturn=null;_trainingGroupDrafts.Clear();_trainingDelegationDrafts.Clear();_trainingDrafts.Clear();_trainingSelectionContext=null;_trainingSelectedDay=null;_trainingChooseArea=false;
        _trainingWeek=null;_trainingTab=0;_analysisTab=0;_trainingTeam=null;_academyTeam=null;_academyTab=0;_planKind=Furvo.Core.Tactics.TacticalPlanKind.Initial;_layoutPhase=Furvo.Core.Tactics.PossessionPhase.WithoutBall;_lineupSlot=null;_lineupPinned=false;_tacticTab=0;_workspace=new CareerWorkspace(loaded);_careerSquadSort=SquadTrackingSort.Position;_careerSquadDescending=false;_club=null;_selectedPlayer=null;_sort=SquadSort.Position;_descending=false;
        _exploring=true;NavigateCareer(CareerPage.Desktop);
    }
    private void NavigateCareer(CareerPage page)
    {
        try
        {
            if(TrainingGroupDirty&&page!=_workspace!.Page){TrainingGroupGuard(()=>NavigateCareer(page));return;}
            if(_workspace?.Page==CareerPage.Training&&page!=CareerPage.Training&&((_trainingDrafts.TryGetValue(SportsPath+"|"+SportsTeam,out var draft)&&draft.Dirty)||TrainingDelegationDirty)){TrainingNavigate(()=>NavigateCareer(page),true);return;}
            if(page!=CareerPage.Analysis)_academyAnalysisReturn=null;
            if(page!=CareerPage.Academy){_academyTeam=null;_academyTab=0;}
            if(page==CareerPage.Training&&_workspace!.Page!=CareerPage.Training)_trainingSelectedDay=null;
            _workspace!.Open(page);
            if(page is CareerPage.MarketOffers or CareerPage.MarketNegotiations){ShowTransferList(page);return;}
            if(page==CareerPage.MarketRights){ShowTransferRights();return;}
            if(page==CareerPage.MarketSearch){ShowMarket();return;}
            if(page==CareerPage.Training){ShowTraining();return;}
            if(page==CareerPage.Analysis){ShowSportsAnalysis();return;}
            if(page==CareerPage.ClubPersonnel){ShowClubDelegations();return;}
            if(page==CareerPage.Facilities){ShowSportsTechnology();return;}
            if(page==CareerPage.Academy){ShowAcademy();return;}
            if(page==CareerPage.Lineup){_tacticTab=0;ShowTactics();return;}
            if(page==CareerPage.OwnSquad){ShowCareerSquad();return;}
            if(page==CareerPage.NoClub){var b=Frame("CareerSection",UiText.Get("NavPage_NoClub"),"");b.AddChild(ClubUi.Text("NoClubInformation",UiText.Get("CareerNoClubManagement"),17));return;}
            if(page is CareerPage.OwnInformation or CareerPage.OwnEconomy or CareerPage.OwnHistory or CareerPage.OwnStadium)
            {
                var club=_workspace.Career.ClubId!.Value;_workspace.RequireManagement(club);
                if(_club?.Id!=club){_selectedPlayer=null;_sort=SquadSort.Position;_descending=false;}
                _club=_queries!.GetClubs().Single(c=>c.Id==club);
                _clubTab=page switch{CareerPage.OwnSquad=>1,CareerPage.OwnEconomy=>3,CareerPage.OwnHistory=>2,CareerPage.OwnStadium=>4,_=>0};ShowClub();return;
            }
            if(page==CareerPage.Desktop){ShowCareerDesktop();return;}
            if(page==CareerPage.Inbox){var b=Frame("CareerSection",UiText.Get("NavInbox"),"");b.AddChild(new CareerEmptyList("CareerInbox","CareerInboxEmpty",Array.Empty<CareerListEntry>()));b.AddChild(Button("BackCareerDashboard",()=>NavigateCareer(CareerPage.Desktop)));return;}
            if(page==CareerPage.Profile){ShowCareerProfile();return;}
            ShowCareerContract();
        }
        catch(Exception error){GD.PushError(error.ToString());ShowError("CareerWorkspaceError",()=>NavigateCareer(CareerPage.Desktop),ShowHome);}
    }
    private VBoxContainer CareerFrame(string title,string description,bool showHeading)
    {
        foreach(var node in GetChildren()){RemoveChild(node);node.QueueFree();}
        var background=new ColorRect{Color=new Color("0d192b"),MouseFilter=MouseFilterEnum.Ignore};background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);AddChild(background);
        var margin=new MarginContainer{Name="ScreenMargins"};margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach(var side in new[]{"left","right","top","bottom"})margin.AddThemeConstantOverride("margin_"+side,16);AddChild(margin);
        var layout=new VBoxContainer{Name="CareerShell",SizeFlagsHorizontal=SizeFlags.ExpandFill};layout.AddThemeConstantOverride("separation",7);margin.AddChild(layout);
        var career=_workspace!.Career;layout.SetMeta("career_id",career.Id.ToString());layout.SetMeta("coach_id",career.Coach.Id.ToString());
        var header=ClubUi.ResponsiveRow(layout,"CareerHeader");
        var brand=ClubUi.Text("CareerBrand",UiText.Get("Brand"),23,"f1b96b");brand.AutowrapMode=TextServer.AutowrapMode.Off;brand.CustomMinimumSize=new Vector2(80,0);brand.SizeFlagsHorizontal=SizeFlags.Fill;brand.SizeFlagsVertical=SizeFlags.ShrinkCenter;header.AddChild(brand);
        var identityLine=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill,SizeFlagsVertical=SizeFlags.ShrinkCenter,CustomMinimumSize=new Vector2(0,52)};identityLine.AddThemeConstantOverride("separation",2);header.AddChild(identityLine);
        identityLine.AddChild(ClubUi.Text("CareerCoachName",career.Coach.ProfileSnapshot.FullName,18));
        var employer=_workspace.HasClub?_queries!.GetClubDetails(career.ClubId!.Value).Club.Name:UiText.Get("CareerFreeCoach");
        identityLine.AddChild(ClubUi.Text("CareerEmployer",employer,15,"f1b96b"));
        var period=new VBoxContainer{CustomMinimumSize=new Vector2(110,0),SizeFlagsVertical=SizeFlags.ShrinkCenter};header.AddChild(period);
        period.AddChild(ClubUi.Text("CareerSeason",_queries!.GetSeasons().Single(s=>s.Id==career.SeasonId).Name??UiText.Get("Unknown"),15));
        period.AddChild(ClubUi.Text("CareerHeaderDate",career.CurrentDate.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture),15));
        var actions=new HBoxContainer{SizeFlagsVertical=SizeFlags.ShrinkCenter};actions.AddThemeConstantOverride("separation",5);header.AddChild(actions);
        actions.AddChild(Button("CareerHeaderInbox",()=>NavigateCareer(CareerPage.Inbox)));
        var advance=Button("CareerContinue",()=>{});advance.Disabled=true;advance.TooltipText=UiText.Get("CareerContinueUnavailable");actions.AddChild(advance);
        actions.AddChild(Button("CareerGameMenu",ToggleCareerMenu));

        var primary=new HBoxContainer{Name="CareerPrimaryNavigation"};primary.AddThemeConstantOverride("separation",5);layout.AddChild(primary);
        foreach(var area in Enum.GetValues<CareerArea>())
        {
            var current=area;var button=Button("NavArea_"+area,()=>NavigateCareer(DefaultPage(current)));button.SizeFlagsHorizontal=SizeFlags.ExpandFill;
            button.Disabled=area==CareerArea.Competition || (area is CareerArea.Team or CareerArea.Market)&&!_workspace.HasClub;
            if(button.Disabled)button.TooltipText=UiText.Get(area==CareerArea.Team?"CareerNoTeam":"CareerFeatureUnavailable");
            var active=_workspace.Area==area;button.SetMeta("active",active);if(active)button.AddThemeColorOverride("font_color",_accent);primary.AddChild(button);
        }
        if(_workspace.Area!=CareerArea.Home)
        {
            var context=new HFlowContainer{Name="CareerContextNavigation"};context.AddThemeConstantOverride("h_separation",6);layout.AddChild(context);
            foreach(var page in ContextPages(_workspace.Area).Where(_workspace.CanOpen))
            {
                var target=page;var button=Button("NavPage_"+page,()=>NavigateCareer(target));var active=page==_workspace.Page;button.SetMeta("active",active);
                if(active)button.AddThemeColorOverride("font_color",_accent);context.AddChild(button);
            }
        }
        layout.AddChild(new HSeparator());
        var content=new VBoxContainer{Name="CareerContent",SizeFlagsVertical=SizeFlags.ExpandFill,SizeFlagsHorizontal=SizeFlags.ExpandFill};layout.AddChild(content);
        if(showHeading&&!string.IsNullOrWhiteSpace(title)){content.AddChild(ClubUi.Text("CareerPageTitle",title,23,"f1b96b"));if(!string.IsNullOrWhiteSpace(description))content.AddChild(ClubUi.Text("CareerPageDescription",description,14,"a6b6ca"));}
        return content;
    }
    private CareerPage DefaultPage(CareerArea area)=>area switch{CareerArea.Home=>CareerPage.Desktop,CareerArea.Team=>CareerPage.OwnSquad,CareerArea.Market=>CareerPage.MarketSearch,CareerArea.Career=>CareerPage.Profile,_=>_workspace!.HasClub?CareerPage.OwnInformation:CareerPage.NoClub};
    private CareerPage[] ContextPages(CareerArea area)=>area switch{
        CareerArea.Home=>[],CareerArea.Team=>[CareerPage.OwnSquad,CareerPage.Lineup,CareerPage.Training,CareerPage.Analysis,CareerPage.Academy],
        CareerArea.Club=>_workspace!.HasClub?[CareerPage.OwnInformation,CareerPage.OwnStadium,CareerPage.OwnEconomy,CareerPage.OwnHistory,CareerPage.Facilities,CareerPage.ClubPersonnel]:[CareerPage.NoClub],
        CareerArea.Career=>[CareerPage.Profile,CareerPage.Contract],CareerArea.Market=>[CareerPage.MarketSearch,CareerPage.MarketOffers,CareerPage.MarketNegotiations,CareerPage.MarketRights],_=>[]};
    private void ToggleCareerMenu()
    {
        var menu=GetNodeOrNull<ExplorationMenu>("ExplorationMenu");
        if(menu is null){menu=new ExplorationMenu(Theme,ShowHome,()=>ShowDisplayOptions(()=>GetNodeOrNull<ExplorationMenu>("ExplorationMenu")?.Open()),ExitGame,true);AddChild(menu);}
        if(menu.Visible)menu.Hide();else menu.Open();
    }
    private void ShowCareerDesktop()
    {
        var career=_workspace!.Career;var body=Frame("CareerSection",UiText.Get("NavPage_Desktop"),UiText.Get("CareerContinueUnavailable"));
        var content=ClubUi.ScrollBody(body,"CareerDashboard");content.GetParent().SetMeta("career_id",career.Id.ToString());content.GetParent().SetMeta("coach_id",career.Coach.Id.ToString());
        var cards=ClubUi.ResponsiveColumns(content,1000);cards.Name="CareerDashboardCards";
        if(_workspace.HasClub)
        {
            var id=career.ClubId!.Value;var details=_queries!.GetClubDetails(id);var identity=ClubUi.Card(cards,"CareerIdentity");var emblemLine=new HBoxContainer();identity.AddChild(emblemLine);
            emblemLine.AddChild(new ClubEmblem(ClubVisualCatalog.Get(id.ToString())));var clubText=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};emblemLine.AddChild(clubText);
            clubText.AddChild(ClubUi.Text("CareerClubName",details.Club.Name,20,"f1b96b"));clubText.AddChild(ClubUi.Text("CareerClubCity",details.Club.City??UiText.Get("Unknown"),15,"a6b6ca"));
            var next=ClubUi.Card(cards,"CareerNextMatch");next.AddChild(ClubUi.Text("NoNextMatch",UiText.Get("CareerNoCalendar"),15,"a6b6ca"));
            var sport=ClubUi.Card(cards,"CareerSportingSituation");sport.AddChild(ClubUi.Text("NoStandings",UiText.Get("CareerNoStandings"),15,"a6b6ca"));
            var goals=career.Contract!.AcceptedOffer.Commitments.Objectives.Where(o=>o.Mandatory==true).ToArray();
            if(goals.Length==0)sport.AddChild(ClubUi.Text("NoAgreedObjectives",UiText.Get("CareerNoAgreedObjectives"),14,"a6b6ca"));
            foreach(var goal in goals)sport.AddChild(ClubUi.Text("AcceptedObjective",goal.Description,14));
            var squad=ClubUi.Card(cards,"CareerSquadCard");squad.AddChild(ClubUi.Text("CareerSquadCount",UiText.Format("CareerSquadCount",_queries.GetSquad(id).Count),18));squad.AddChild(Button("OpenCareerSquad",()=>NavigateCareer(CareerPage.OwnSquad)));
            var finance=ClubUi.Card(cards,"CareerEconomyCard");var resources=_queries.GetFinancialResources(id,career.SeasonId);
            foreach(var field in new[]{("EconomyTransferBudget",resources.TransferBudget),("EconomyWageBudget",resources.PlayerWageBudget),("EconomyRemainingWages",resources.PlayerWageRemaining)})
                ClubUi.Field(finance,field.Item1,ManagerOfferDocument.Money(field.Item2,resources.Currency));
            ClubUi.Field(finance,"EconomySituation",UiText.Get("NotEvaluated"));finance.AddChild(Button("OpenCareerEconomy",()=>NavigateCareer(CareerPage.OwnEconomy)));
        }
        else
        {
            var identity=ClubUi.Card(cards,"CareerIdentity");var identityRow=new HBoxContainer();identity.AddChild(identityRow);identityRow.AddChild(new CoachAvatar(career.Coach.ProfileSnapshot.AvatarKey));
            var person=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};identityRow.AddChild(person);
            person.AddChild(ClubUi.Text("CareerFreeIdentity",career.Coach.ProfileSnapshot.FullName,20,"f1b96b"));
            person.AddChild(ClubUi.Text("CareerEmploymentValue",UiText.Get("CareerUnemployed"),16));person.AddChild(ClubUi.Text("CareerFreeStatus",UiText.Get("CareerFreeCoach"),16));
            var offers=ClubUi.Card(cards,"CareerOffers");offers.AddChild(new CareerEmptyList("CareerOffersList","CareerOffersEmpty",Array.Empty<CareerListEntry>()));
            var vacancies=ClubUi.Card(cards,"CareerVacancies");vacancies.AddChild(new CareerEmptyList("CareerVacanciesList","CareerVacanciesEmpty",Array.Empty<CareerListEntry>()));
            var history=ClubUi.Card(cards,"CareerProfessionalHistory");history.AddChild(ClubUi.Text("CareerHistoryEmpty",UiText.Get("CareerHistoryEmpty"),15,"a6b6ca"));

        }
        var inbox=ClubUi.Card(cards,"NavInbox");inbox.AddChild(new CareerEmptyList("CareerInbox","CareerInboxEmpty",Array.Empty<CareerListEntry>()));
    }
    private void ShowCareerProfile()
    {
        var c=_workspace!.Career;var p=c.Coach.ProfileSnapshot;var body=Frame("CareerSection",UiText.Get("NavPage_Profile"),"");var content=ClubUi.ScrollBody(body,"CareerProfile");
        content.AddChild(new CoachAvatar(p.AvatarKey));ClubUi.Field(content,"CoachFullName",p.FullName);ClubUi.Field(content,"CoachBirthDate",p.BirthDate.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture));
        ClubUi.Field(content,"CoachNationality",UiText.Get("Nation_"+p.NationalityCode));ClubUi.Field(content,"CoachExperience",UiText.Get("CoachExperience_"+p.Experience));
        ClubUi.Field(content,"CareerEmployment",UiText.Get(_workspace.HasClub?"CareerEmployed":"CareerUnemployed"));
        ClubUi.Field(content,"CareerClub",_workspace.HasClub?_queries!.GetClubDetails(c.ClubId!.Value).Club.Name:UiText.Get("NoClub"));
        content.AddChild(ClubUi.Text("CareerHistoryEmpty",UiText.Get("CareerHistoryEmpty"),15,"a6b6ca"));
    }
    private void ShowCareerContract()
    {
        var c=_workspace!.Career;var body=Frame("CareerSection",UiText.Get("NavPage_Contract"),"");
        if(c.Contract is null){body.AddChild(ClubUi.Text("CareerNoContract",UiText.Get("CareerNoContractNotice"),17));return;}
        body.AddChild(new ManagerOfferDocument(c.Contract.AcceptedOffer,c.Coach.ProfileSnapshot,_queries!.GetClubDetails(c.Contract.AcceptedOffer.ClubId),_queries.GetSeasons().Single(s=>s.Id==c.SeasonId)));
    }
}
