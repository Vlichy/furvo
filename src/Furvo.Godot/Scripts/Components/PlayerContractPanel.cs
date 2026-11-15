using Furvo.Core.Queries;
using Godot;
namespace Furvo.Presentation.Components;
/// <summary>Panel informativo compartido por Mercado, oferta y ficha propia; sin acciones de ejecución.</summary>
public partial class PlayerContractPanel : VBoxContainer
{
    public PlayerContractPanel(PlayerContractInfo info,bool compact=false)
    {
        Name="PlayerContractInformation";SizeFlagsHorizontal=SizeFlags.ExpandFill;AddThemeConstantOverride("separation",3);
        AddChild(ClubUi.Text("ReleaseContractHeading",UiText.Get("ReleaseContractHeading"),compact?13:16,"f1b96b"));
        var release=info.Release??ReleaseClauseInfo.Unknown;SetMeta("release_information_state",release.State.ToString());
        var end=info.End?.ToString("dd/MM/yyyy")??UiText.Get("NotAvailable");var value=ReleaseClauseText.Value(release,UiText.Get);
        if(compact)AddChild(ClubUi.Text("ReleaseContractValue",UiText.Get("MarketContractEnd")+": "+end+" · "+UiText.Get("ReleaseClauseLabel")+": "+value,14,"f1b96b"));
        else{
            ClubUi.Field(this,"MarketContractClub",info.Club??UiText.Get("NotAvailable"));ClubUi.Field(this,"MarketContractEnd",end);ClubUi.Field(this,"ReleaseClauseLabel",value);
        }
        AddChild(ClubUi.Text("ReleaseKnowledgeState",UiText.Get("ReleaseSource_"+release.Source)+" · "+UiText.Get("ReleaseState_"+release.State),12,"a6b6ca"));
    }
}
