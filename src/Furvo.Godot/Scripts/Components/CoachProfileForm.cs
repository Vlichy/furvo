using System.Globalization;
using Furvo.Core.Careers;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Formulario validado y de alta única. El ID se conserva durante reintentos; cancelar no guarda.</summary>
public partial class CoachProfileForm:VBoxContainer
{
    public CoachProfileForm(DateOnly referenceDate,Func<CoachProfile,Task<CoachProfile>> save,Action created,Action back)
    {
        Name="CoachProfileForm";SizeFlagsVertical=SizeFlags.ExpandFill;
        var id=CoachProfileId.New();var body=ClubUi.ScrollBody(this,"CoachFormScroll");body.AddChild(new CoachAvatar());
        body.AddChild(ClubUi.Text("CoachFormHelp",UiText.Format("CoachFormHelp",referenceDate.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture)),14,"a6b6ca"));
        var fields=new GridContainer{Columns=2,SizeFlagsHorizontal=SizeFlags.ExpandFill};fields.AddThemeConstantOverride("h_separation",16);fields.AddThemeConstantOverride("v_separation",10);body.AddChild(fields);
        LineEdit Input(string key,int max,string placeholder="")
        {
            fields.AddChild(ClubUi.Text(key+"Label",UiText.Get(key),15,"a6b6ca"));
            var input=new LineEdit{Name=key,MaxLength=max,PlaceholderText=placeholder,SizeFlagsHorizontal=SizeFlags.ExpandFill,CustomMinimumSize=new Vector2(0,38)};fields.AddChild(input);return input;
        }
        var first=Input("CoachFirstName",60);var last=Input("CoachLastName",100);var birth=Input("CoachBirthDate",10,UiText.Get("DatePlaceholder"));
        fields.AddChild(ClubUi.Text("CoachNationalityLabel",UiText.Get("CoachNationality"),15,"a6b6ca"));
        var nation=new OptionButton{Name="CoachNationality",SizeFlagsHorizontal=SizeFlags.ExpandFill,CustomMinimumSize=new Vector2(0,38)};nation.AddItem(UiText.Get("ChooseNationality"));
        foreach(var code in CoachNationalities.Codes){nation.AddItem(UiText.Get("Nation_"+code));nation.SetItemMetadata(nation.ItemCount-1,code);}fields.AddChild(nation);
        var city=Input("CoachBirthCity",100);
        fields.AddChild(ClubUi.Text("CoachExperienceLabel",UiText.Get("CoachExperience"),15,"a6b6ca"));
        var experience=new OptionButton{Name="CoachExperience",SizeFlagsHorizontal=SizeFlags.ExpandFill,CustomMinimumSize=new Vector2(0,38)};
        foreach(var value in NewCoachBackgrounds.Options)experience.AddItem(UiText.Get("CoachExperience_"+value),(int)value);fields.AddChild(experience);
        body.AddChild(ClubUi.Text("CoachExperienceNotice",UiText.Get("CoachExperienceNotice"),13,"a6b6ca"));
        var error=ClubUi.Text("CoachFormError","",14,"e0a0a0");body.AddChild(error);
        var buttons=new HFlowContainer();buttons.AddThemeConstantOverride("h_separation",12);AddChild(buttons);
        var cancel=WindowUi.Button("BackCoachProfiles",back);buttons.AddChild(cancel);
        var create=WindowUi.Button("SaveCoachProfile",()=>{});buttons.AddChild(create);var busy=false;
        create.Pressed+=async()=>
        {
            if(busy)return;busy=true;create.Disabled=true;cancel.Disabled=true;error.Text="";
            try
            {
                if(!DateOnly.TryParseExact(birth.Text,"dd/MM/yyyy",CultureInfo.InvariantCulture,DateTimeStyles.None,out var birthday) || nation.Selected<=0)throw new ArgumentException("Formulario no válido.");
                var profile=new CoachProfile(id,first.Text,last.Text,birthday,nation.GetItemMetadata(nation.Selected).AsString(),city.Text,(CoachExperience)experience.GetSelectedId(),"generic-v1",referenceDate);
                await save(profile);if(GodotObject.IsInstanceValid(this) && IsInsideTree())created();
            }
            catch(ArgumentException){if(GodotObject.IsInstanceValid(this) && IsInsideTree())error.Text=UiText.Get("CoachFormInvalid");}
            catch(Exception e){GD.PushError(e.ToString());if(GodotObject.IsInstanceValid(this) && IsInsideTree())error.Text=UiText.Get("CoachProfileSaveError");}
            finally{busy=false;if(GodotObject.IsInstanceValid(this) && IsInsideTree()){create.Disabled=false;cancel.Disabled=false;}}
        };
        Ready += () => first.GrabFocus();
    }
}
