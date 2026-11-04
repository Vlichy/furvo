using Furvo.Core.Development;
using Godot;
namespace Furvo.Presentation.Components;
/// <summary>Fila de una actividad guardada; sin atributos de jugadores ni una segunda fuente de sesiones.</summary>
public partial class TrainingDailyExerciseRow : PanelContainer
{
    public TrainingDailyExerciseRow(TrainingSession block,string participants,string responsible,Action edit,Action delete)
    {
        Name="TrainingDailyBlock_"+block.Id;SetMeta("training_area",TrainingCatalog.Get(block.Exercise).Area.ToString());SetMeta("daily_block_id",block.Id.ToString());SizeFlagsHorizontal=SizeFlags.ExpandFill;
        var style=new StyleBoxFlat{BgColor=new Color("182b43"),BorderColor=new Color("2c4058"),ContentMarginLeft=8,ContentMarginRight=8,ContentMarginTop=6,ContentMarginBottom=6};style.SetBorderWidthAll(1);style.SetCornerRadiusAll(4);AddThemeStyleboxOverride("panel",style);
        var row=new HBoxContainer();row.AddThemeConstantOverride("separation",10);AddChild(row);row.AddChild(new TrainingActivityIcon(TrainingCatalog.Get(block.Exercise).Area,block.Exercise=="rest"){SizeFlagsVertical=SizeFlags.ShrinkCenter});
        var text=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};text.AddThemeConstantOverride("separation",2);row.AddChild(text);
        text.AddChild(ClubUi.Text("TrainingDailyExercise",block.Start.ToString("HH:mm")+"–"+block.Ends.ToString("HH:mm")+" | "+UiText.Get("Exercise_"+block.Exercise),15,"f1b96b"));
        text.AddChild(ClubUi.Text("TrainingDailyParticipants",UiText.Get("TrainingArea_"+TrainingCatalog.Get(block.Exercise).Area)+" · "+participants+" · "+block.Minutes+" min · "+UiText.Get("TrainingLoad_"+block.Load),13));
        text.AddChild(ClubUi.Text("TrainingDailyResponsible",responsible,13,"a6b6ca"));
        var actions=new VBoxContainer();actions.AddThemeConstantOverride("separation",4);row.AddChild(actions);
        var editButton=WindowUi.Button("TrainingDailyEdit",edit);editButton.Name="TrainingDailyEdit_"+block.Id;var deleteButton=WindowUi.Button("TrainingDailyDelete",delete);deleteButton.Name="TrainingDailyDelete_"+block.Id;
        foreach(var button in new[]{editButton,deleteButton}){button.CustomMinimumSize=new Vector2(100,28);button.AddThemeFontSizeOverride("font_size",13);var box=new StyleBoxFlat{BgColor=new Color("22364d"),ContentMarginLeft=8,ContentMarginRight=8,ContentMarginTop=3,ContentMarginBottom=3};box.SetCornerRadiusAll(3);button.AddThemeStyleboxOverride("normal",box);actions.AddChild(button);}
        TooltipText=participants+"\n"+responsible;
    }
}
