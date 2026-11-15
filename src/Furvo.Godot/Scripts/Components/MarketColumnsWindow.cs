using Furvo.Core.Domain;
using Godot;
namespace Furvo.Presentation.Components;

/// <summary>Editor de preferencias visuales con borrador independiente; Cancelar nunca guarda cambios.</summary>
public partial class MarketColumnsWindow : Window
{
    private readonly VBoxContainer _available;
    private readonly VBoxContainer _selected;
    private readonly PlayerRole? _position;
    private List<string> _draft;
    private bool _closing;
    public MarketColumnsWindow(Theme theme,MarketColumnPreferences preferences,PlayerRole? position,Action<IReadOnlyList<string>> apply)
    {
        Name="MarketColumnsDialog";Title=UiText.Get("MarketConfigureColumns");_position=position;_draft=preferences.Columns(position).ToList();
        var body=WindowUi.Body(this,theme);
        body.AddChild(ClubUi.Text("MarketColumnsHeading",Title+" · "+UiText.Get(position is {} role?"Position_"+role:"MarketAllPositions"),20,"f1b96b"));
        body.AddChild(ClubUi.Text("MarketColumnsScope",UiText.Get("MarketColumnsScope"),13));
        var panes=new HBoxContainer{SizeFlagsVertical=Control.SizeFlags.ExpandFill};panes.AddThemeConstantOverride("separation",16);body.AddChild(panes);
        VBoxContainer Pane(string key){
            var panel=new VBoxContainer{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};panes.AddChild(panel);panel.AddChild(ClubUi.Text(key,UiText.Get(key),15,"f1b96b"));
            var scroll=new ScrollContainer{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill,SizeFlagsVertical=Control.SizeFlags.ExpandFill,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};panel.AddChild(scroll);
            var list=new VBoxContainer{SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};scroll.AddChild(list);return list;
        }
        _available=Pane("MarketColumnsAvailable");_selected=Pane("MarketColumnsSelected");
        FillAvailable();FillSelected();
        var buttons=new HBoxContainer();buttons.AddThemeConstantOverride("separation",8);body.AddChild(buttons);
        buttons.AddChild(WindowUi.Button("MarketColumnsReset",()=>{_draft=MarketColumnPreferences.Defaults(_position).ToList();FillAvailable();FillSelected();}));
        buttons.AddChild(WindowUi.Button("MarketColumnsApply",()=>{apply(_draft.ToArray());Close();}));
        buttons.AddChild(WindowUi.Button("MarketColumnsCancel",Close));
        CloseRequested+=Close;WindowInput+=input=>{if(WindowUi.IsEscape(input)){SetInputAsHandled();Close();}};
    }
    private static void Clear(Node node){foreach(var child in node.GetChildren()){node.RemoveChild(child);child.QueueFree();}}
    private void FillAvailable()
    {
        Clear(_available);
        foreach(var group in PlayerAttributeCatalog.All.Where(a=>!a.IsHidden).GroupBy(a=>a.Category)){
            _available.AddChild(ClubUi.Text("Category_"+group.Key,UiText.Get("Category_"+group.Key),14,"f1b96b"));
            foreach(var definition in group){
                var code=definition.Code;var check=new CheckBox{Name="MarketColumn_"+code,Text=UiText.Get("Attribute_"+code),ButtonPressed=_draft.Contains(code)};
                check.SetMeta("market_column_code",code);_available.AddChild(check);
                check.Toggled+=enabled=>{if(enabled){if(!_draft.Contains(code))_draft.Add(code);}else _draft.Remove(code);FillSelected();};
            }
        }
    }
    private void FillSelected()
    {
        Clear(_selected);
        if(_draft.Count==0)_selected.AddChild(ClubUi.Text("MarketColumnsNone",UiText.Get("MarketColumnsNone"),14));
        for(var i=0;i<_draft.Count;i++){
            var index=i;var code=_draft[i];var row=new HBoxContainer{Name="MarketSelected_"+code};_selected.AddChild(row);row.SetMeta("market_selected_column",code);
            var label=ClubUi.Text("ColumnLabel",UiText.Get("Attribute_"+code),14);label.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;row.AddChild(label);
            var up=WindowUi.Button("MarketColumnUp",()=>{_draft=MarketColumnPreferences.Move(_draft,index,-1).ToList();FillSelected();});up.Name="MarketColumnUp_"+code;up.Disabled=i==0;row.AddChild(up);
            var down=WindowUi.Button("MarketColumnDown",()=>{_draft=MarketColumnPreferences.Move(_draft,index,1).ToList();FillSelected();});down.Name="MarketColumnDown_"+code;down.Disabled=i==_draft.Count-1;row.AddChild(down);
        }
    }
    private void Close(){if(_closing)return;_closing=true;Hide();QueueFree();}
    public void Open()
    {
        var size=GetTree().Root.Size;PopupCentered(new Vector2I(Math.Min(1040,size.X-40),Math.Min(610,size.Y-48)));
    }
}
