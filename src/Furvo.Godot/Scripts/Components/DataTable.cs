using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Columna configurable; la tabla delega la ordenación para respetar tipos y reglas del servicio de consulta.</summary>
public sealed record TableColumn(string Title, int MinimumWidth, int ExpandRatio = 1, bool Sortable = false,
    HorizontalAlignment HeaderAlignment = HorizontalAlignment.Center,
    HorizontalAlignment CellAlignment = HorizontalAlignment.Left, bool Wrap = false);
/// <summary>Fila con identidad independiente de su texto y de su posición en pantalla.</summary>
public sealed record TableCellVisual(string Kind,int Elements,Action<Rid,Rect2> Paint,string Description);
public sealed record TableRow(string Id, IReadOnlyList<string> Cells,IReadOnlyDictionary<int,string>? CellColors=null,IReadOnlyDictionary<int,TableCellVisual>? Visuals=null);

/// <summary>Tabla reutilizable para clubes, plantillas y atributos; soporta selección, activación y petición de orden.</summary>
public partial class DataTable : VBoxContainer
{
    private readonly Tree _tree;
    private readonly IReadOnlyList<TableColumn> _columns;
    public event Action<string>? RowSelected;
    public event Action<string>? RowActivated;
    public event Action<int>? SortRequested;
    public string? SelectedId => _tree.GetSelected()?.GetMetadata(0).AsString();

    public DataTable(IReadOnlyList<TableColumn> columns, bool selectable = true, bool fitContent = false,int rowHeight=0,int rowSpacing=-1)
    {
        _columns = columns;
        _fitContent = fitContent;
        _rowHeight=rowHeight;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        _tree = new Tree { Columns = columns.Count, HideRoot = true, ColumnTitlesVisible = true,
            SelectMode = Tree.SelectModeEnum.Row, SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 180),
            AllowReselect = true, FocusMode = selectable ? FocusModeEnum.All : FocusModeEnum.None };
        if(rowSpacing>=0)_tree.AddThemeConstantOverride("v_separation",rowSpacing);
        AddChild(_tree);
        for (var i = 0; i < columns.Count; i++)
        {
            _tree.SetColumnTitle(i, columns[i].Title);
            _tree.SetColumnTitleAlignment(i, columns[i].HeaderAlignment);
            // Permite texto largo con salto de línea manteniendo las proporciones configuradas.
            _tree.SetColumnClipContent(i, columns[i].Wrap);
            _tree.SetColumnCustomMinimumWidth(i, columns[i].MinimumWidth);
            _tree.SetColumnExpand(i, true);
            _tree.SetColumnExpandRatio(i, columns[i].ExpandRatio);
        }
        _tree.ItemSelected += () => { if (SelectedId is { } id) RowSelected?.Invoke(id); };
        _tree.ItemActivated += () => { if (SelectedId is { } id) RowActivated?.Invoke(id); };
        _tree.ColumnTitleClicked += (column, button) =>
        {
            if (button == (long)MouseButton.Left && _columns[(int)column].Sortable) SortRequested?.Invoke((int)column);
        };
        _selectable = selectable;
    }

    private readonly int _rowHeight;
    private readonly bool _selectable;
    private readonly bool _fitContent;

    /// <summary>Reconstruye filas conservando selección por ID cuando sigue presente, nunca por índice visual.</summary>
    public void SetRows(IEnumerable<TableRow> rows, string? selectedId = null)
    {
        var rowCount = 0;
        _tree.Clear();
        var root = _tree.CreateItem();
        foreach (var row in rows)
        {
            if (row.Cells.Count != _columns.Count) throw new ArgumentException("La fila no coincide con las columnas.", nameof(rows));
            rowCount++;
            var item = _tree.CreateItem(root);
            item.SetMetadata(0, row.Id);
            if(_rowHeight>0)item.CustomMinimumHeight=_rowHeight;
            for (var i = 0; i < _columns.Count; i++)
            {
                item.SetText(i, row.Cells[i]);
                if(row.CellColors?.TryGetValue(i,out var color)==true)item.SetCustomColor(i,new Color(color));
                item.SetTextAlignment(i, _columns[i].CellAlignment);
                if (_columns[i].Wrap) item.SetAutowrapMode(i, TextServer.AutowrapMode.WordSmart);
                item.SetSelectable(i, _selectable);
                item.SetTooltipText(i, row.Cells[i]);
                if(row.Visuals?.TryGetValue(i,out var visual)==true)
                {
                    item.SetCellMode(i,TreeItem.TreeCellMode.Custom);item.SetText(i,"");item.SetDescription(i,visual.Description);item.SetMeta("cell_text_"+i,row.Cells[i]);item.SetEditable(i,false);
                    item.SetTooltipText(i,visual.Description);item.SetMeta("visual_"+i,visual.Kind);item.SetMeta("visual_elements_"+i,visual.Elements);
                    item.SetCustomDrawCallback(i,Callable.From<TreeItem,Rect2>((_,rect)=>visual.Paint(_tree.GetCustomDrawingCanvasItem(),rect)));
                }
            }
            if (_selectable && row.Id == selectedId) item.Select(0);
        }
        if (_fitContent)
        {
            // Las categorías caben completas en altura: solo el contenedor exterior de ficha desplaza el contenido.
            var rowHeight = _tree.GetThemeFont("font").GetHeight(_tree.GetThemeFontSize("font_size"))
                + 2 * _tree.GetThemeConstant("v_separation");
            _tree.CustomMinimumSize = new Vector2(0, Mathf.Ceil((rowCount + 1) * rowHeight) + 32);
            _tree.ScrollVerticalEnabled = false;
        }
        if (_tree.GetSelected() is not null) _tree.ScrollToItem(_tree.GetSelected());
    }

    public void MarkSort(int column, bool descending)
    {
        for (var i = 0; i < _columns.Count; i++)
            _tree.SetColumnTitle(i, _columns[i].Title + (i == column ? UiText.Get(descending ? "SortDescending" : "SortAscending") : ""));
    }

    public void FocusTable() => _tree.GrabFocus();
}
