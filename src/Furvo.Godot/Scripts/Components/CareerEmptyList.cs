using Godot;
namespace Furvo.Presentation.Components;
public sealed record CareerListEntry(string Id,string Title,string Description);
/// <summary>Lista reutilizable de comunicaciones/ofertas/vacantes; vacío explícito sin fabricar registros.</summary>
public partial class CareerEmptyList:VBoxContainer
{
    public CareerEmptyList(string name,string emptyKey,IReadOnlyList<CareerListEntry> entries)
    {
        Name=name;SetMeta("entry_count",entries.Count);SizeFlagsHorizontal=SizeFlags.ExpandFill;
        if(entries.Count==0)AddChild(ClubUi.Text("EmptyState",UiText.Get(emptyKey),15,"a6b6ca"));
        foreach(var entry in entries){var row=new VBoxContainer();AddChild(row);row.SetMeta("entry_id",entry.Id);row.AddChild(ClubUi.Text("EntryTitle",entry.Title,17,"f1b96b"));row.AddChild(ClubUi.Text("EntryDescription",entry.Description));}
    }
}
