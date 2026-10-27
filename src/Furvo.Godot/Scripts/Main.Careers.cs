using System.Globalization;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;

namespace Furvo.Presentation;

/// <summary>Composición del flujo de alta. La navegación previa no crea carreras; solo la confirmación invoca persistencia.</summary>
public partial class Main
{
    private string? _careerDataRoot;
    private string? _initialWorldPath;
    private ICoachProfileStore? _coachProfiles;
    private SqliteCareerStore? _careers;
    private CareerCreationFlow? _careerFlow;
    private bool _creatingCareer;

    /// <summary>Punto de composición para pruebas aisladas antes de entrar al árbol; no es un ajuste del jugador.</summary>
    public void ConfigureCareerStorage(string directory)
    {
        if(IsInsideTree() || !Path.IsPathFullyQualified(directory))throw new InvalidOperationException("Configura una ruta absoluta antes de iniciar la escena.");
        _careerDataRoot=Path.GetFullPath(directory);
    }
    private void EnsureCareerServices()
    {
        var root=_careerDataRoot??ProjectSettings.GlobalizePath("user://");
        _coachProfiles??=new SqliteCoachProfileStore(Path.Combine(root,"coach-profiles","profiles.db"));
        _careers??=new SqliteCareerStore(Path.Combine(root,"careers"),_initialWorldPath??throw new InvalidOperationException("Falta mundo inicial."));
        _careerFlow??=new CareerCreationFlow(_queries!,new InitialManagerOfferSource(_queries!),_careers);
    }
    private void BeginNewCareer()
    {
        _exploring=true;_careerFlow=null;
        if(_queries is null)LoadWorldThen(()=>ShowCoachProfiles());else ShowCoachProfiles();
    }
    private void ShowCoachProfiles(bool createFirst=true)
    {
        try
        {
            EnsureCareerServices();var profiles=_coachProfiles!.List();
            if(profiles.Count==0 && createFirst){ShowCoachForm();return;}
            var body=Frame("CareerSection",UiText.Get("CoachProfilesTitle"),UiText.Get("CoachProfilesDescription"));
            var list=ClubUi.ScrollBody(body,"CoachProfilesScroll");
            if(profiles.Count==0)list.AddChild(ClubUi.Text("NoCoachProfiles",UiText.Get("NoCoachProfiles"),16));
            foreach(var profile in profiles)
            {
                var row=new HBoxContainer{Name="Profile_"+profile.Id,SizeFlagsHorizontal=SizeFlags.ExpandFill};row.SetMeta("profile_id",profile.Id.ToString());list.AddChild(row);
                row.AddChild(new CoachAvatar(profile.AvatarKey));var text=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};row.AddChild(text);
                text.AddChild(ClubUi.Text("CoachProfileName",profile.FullName,20,"f1b96b"));
                text.AddChild(ClubUi.Text("CoachProfileIdentity",UiText.Format("CoachProfileIdentity",UiText.Get("Nation_"+profile.NationalityCode),profile.AgeOn(_queries!.ReferenceDate),profile.BirthDate.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture)),14,"a6b6ca"));
                var selected=profile;row.AddChild(WindowUi.Button("SelectCoachProfile",()=>{_careerFlow!.SelectProfile(selected);_club=null;ShowClubs();}));
            }
            var footer=new HFlowContainer();footer.AddThemeConstantOverride("h_separation",12);body.AddChild(footer);
            footer.AddChild(Button("Home",ShowHome));footer.AddChild(Button("CreateCoachProfile",ShowCoachForm));
        }
        catch(Exception error){GD.PushError(error.ToString());ShowError("CoachProfilesLoadError",()=>ShowCoachProfiles(false),ShowHome);}
    }
    private void ShowCoachForm()
    {
        EnsureCareerServices();var body=Frame("CareerSection",UiText.Get("CreateCoachProfile"),UiText.Get("CoachFormDescription"));
        body.AddChild(new CoachProfileForm(_queries!.ReferenceDate,p=>Task.Run(()=>_coachProfiles!.Create(p)),()=>ShowCoachProfiles(false),()=>ShowCoachProfiles(false)));
    }
    private void ShowManagerOffer()
    {
        try
        {
            EnsureCareerServices();if(_careerFlow!.Profile is null || _club is null)throw new InvalidOperationException("Falta entrenador o club.");
            var offer=_careerFlow.SelectClub(_club.Id);var body=Frame("CareerSection",UiText.Get("ManagerOfferTitle"),UiText.Get("ManagerOfferDescription"));
            body.AddChild(OfferDocument(offer));var actions=new HFlowContainer{Name="ManagerOfferActions"};actions.AddThemeConstantOverride("h_separation",12);body.AddChild(actions);
            var sign=Button("SignManagerContract",()=>ShowCareerConfirmation(CareerStartMode.SignOffer));sign.Disabled=offer.SigningBlocks().Count>0;sign.TooltipText=string.Join("\n",offer.SigningIssues().Select(ManagerOfferDocument.SigningIssueText));actions.AddChild(sign);
            actions.AddChild(Button("RejectAndStartUnemployed",()=>ShowCareerConfirmation(CareerStartMode.RejectOffer)));
            actions.AddChild(Button("BackChooseClub",()=>{_careerFlow.BackToClubs();ShowClubs();}));
        }
        catch(ContractCountryConfigurationException error){GD.PushError(error.ToString());ShowError("OfferCountryConfigurationError",ShowManagerOffer,ShowClubs);}
        catch(Exception error){GD.PushError(error.ToString());ShowError("ManagerOfferError",ShowManagerOffer,ShowClubs);}
    }
    private ManagerOfferDocument OfferDocument(ManagerOffer offer)=>new(offer,_careerFlow!.Profile!,_queries!.GetClubDetails(offer.ClubId),_queries.GetSeasons().Single(s=>s.Id==offer.SeasonId));
    private void ShowCareerConfirmation(CareerStartMode mode)
    {
        try
        {
            EnsureCareerServices();var request=_careerFlow!.Prepare(mode);
            var body=Frame("CareerSection",UiText.Get("CareerConfirmationTitle"),UiText.Get(mode==CareerStartMode.SignOffer?"CareerConfirmEmployed":mode==CareerStartMode.RejectOffer?"CareerConfirmRejection":"CareerConfirmUnemployed"));
            if(mode==CareerStartMode.SignOffer)body.AddChild(OfferDocument(request.Offer!));
            else
            {
                var content=ClubUi.ScrollBody(body,"CareerConfirmationScroll");content.AddChild(new CoachAvatar(request.ProfileSnapshot.AvatarKey));
                ClubUi.Field(content,"CoachFullName",request.ProfileSnapshot.FullName);ClubUi.Field(content,"CareerDate",_queries!.ReferenceDate.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture));
                ClubUi.Field(content,"CareerClub",UiText.Get("NoClub"));ClubUi.Field(content,"CareerEmployment",UiText.Get("CareerUnemployed"));
                if(request.Offer is {} rejected)ClubUi.Field(content,"DeclinedClub",_queries!.GetClubDetails(rejected.ClubId).Club.Name);
                content.AddChild(ClubUi.Text("CareerNoContractNotice",UiText.Get("CareerNoContractNotice"),14,"a6b6ca"));
            }
            var feedback=ClubUi.Text("CareerCreationFeedback","",14,"e0a0a0");body.AddChild(feedback);
            var actions=new HFlowContainer{Name="CareerConfirmationActions"};actions.AddThemeConstantOverride("h_separation",12);body.AddChild(actions);
            var cancel=Button("CancelCareerCreation",()=>{_careerFlow.CancelConfirmation();if(mode==CareerStartMode.Unemployed)ShowClubs();else ShowManagerOffer();});actions.AddChild(cancel);
            var confirm=Button("ConfirmCareerCreation",()=>{});actions.AddChild(confirm);
            confirm.Pressed+=async()=>
            {
                if(_creatingCareer)return;_creatingCareer=true;confirm.Disabled=true;cancel.Disabled=true;feedback.Text=UiText.Get("CareerCreating");var generation=_loadRequest;
                try
                {
                    var flow=_careerFlow;var career=await Task.Run(()=>flow.Confirm());
                    if(generation==_loadRequest && IsInsideTree())ShowCareerWelcome(career);
                }
                catch(Exception error){GD.PushError(error.ToString());if(generation==_loadRequest && GodotObject.IsInstanceValid(feedback) && feedback.IsInsideTree())feedback.Text=UiText.Get("CareerCreationError");}
                finally{_creatingCareer=false;if(GodotObject.IsInstanceValid(confirm) && confirm.IsInsideTree()){confirm.Disabled=false;cancel.Disabled=false;}}
            };
        }
        catch(Exception error){GD.PushError(error.ToString());ShowError("CareerPrepareError",ShowClubs,ShowClubs);}
    }
    private void ShowCareerWelcome(CareerSummary career)
    {
        var body=Frame("CareerSection",UiText.Get("CareerWelcomeTitle"),UiText.Get("CareerWelcomeDescription"));body.SetMeta("career_id",career.Id.ToString());
        var content=ClubUi.ScrollBody(body,"CareerWelcomeSummary");content.GetParent().SetMeta("career_id",career.Id.ToString());content.GetParent().SetMeta("coach_id",career.Coach.Id.ToString());
        content.AddChild(new CoachAvatar(career.Coach.ProfileSnapshot.AvatarKey));
        ClubUi.Field(content,"CoachFullName",career.Coach.ProfileSnapshot.FullName);
        ClubUi.Field(content,"EconomySeason",_queries!.GetSeasons().Single(s=>s.Id==career.SeasonId).Name);
        ClubUi.Field(content,"CareerDate",career.CurrentDate.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture));
        ClubUi.Field(content,"CareerEmployment",UiText.Get(career.Employment==CareerEmploymentState.Employed?"CareerEmployed":"CareerUnemployed"));
        ClubUi.Field(content,"CareerClub",career.ClubId is {} club?_queries.GetClubDetails(club).Club.Name:UiText.Get("NoClub"));
        if(career.Contract is {} contract)
        {
            ClubUi.Field(content,"OfferAnnualSalary",ManagerOfferDocument.Money(contract.AcceptedOffer.Terms.GrossAnnualSalary,contract.AcceptedOffer.Terms.Currency));
        }
        var footer=new HFlowContainer();footer.AddThemeConstantOverride("h_separation",12);body.AddChild(footer);footer.AddChild(Button("Home",ShowHome));footer.AddChild(Button("ExitGame",ExitGame));
    }
}
