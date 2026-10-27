using System.Globalization;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;

namespace Furvo.Presentation;

/// <summary>Navegación de consulta; compone servicios existentes y nunca expone entidades con atributos ocultos a los controles.</summary>
public partial class Main : Control
{
    private IWorldQueries? _queries;
    private ClubSummary? _club;
    private SquadSort _sort = SquadSort.Name;
    private bool _descending;
    private string? _selectedPlayer;
    private int _loadRequest;
    private int _clubTab;
    private readonly Color _ink = new("e5ecf5");
    private readonly Color _muted = new("a6b6ca");
    private readonly Color _accent = new("f1b96b");

    public override void _Ready()
    {
        Theme = BuildTheme();
        ShowHome();
    }

    public override void _ExitTree() => _loadRequest++;

    private Theme BuildTheme()
    {
        var theme = new Theme { DefaultFontSize = 17 };
        theme.SetColor("font_color", "Label", _ink);
        theme.SetColor("font_color", "Button", _ink);
        theme.SetColor("font_color", "Tree", _ink);
        theme.SetConstant("v_separation", "VBoxContainer", 12);
        theme.SetConstant("h_separation", "HBoxContainer", 14);
        theme.SetConstant("v_separation", "Tree", 10);
        foreach (var state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
        {
            var panel = new StyleBoxFlat { BgColor = new Color(state == "hover" ? "30445f" : "20324a"),
                BorderColor = new Color(state == "focus" ? "f1b96b" : "40536b"),
                ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 10, ContentMarginBottom = 10 };
            panel.SetCornerRadiusAll(5);
            panel.SetBorderWidthAll(state == "focus" ? 2 : 1);
            theme.SetStylebox(state, "Button", panel);
        }
        var table = new StyleBoxFlat { BgColor = new Color("14243a"), BorderColor = new Color("34465e"),
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 6, ContentMarginBottom = 6 };
        table.SetBorderWidthAll(1);
        table.SetCornerRadiusAll(5);
        theme.SetStylebox("panel", "Tree", table);
        return theme;
    }

    /// <summary>Las pantallas se reconstruyen con contenedores adaptables; retirar nodos evita interacciones diferidas con la pantalla anterior.</summary>
    private VBoxContainer Frame(string section, string title, string description, bool showHeading = true)
    {
        foreach (var child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        var background = new ColorRect { Color = new Color("0d192b"), MouseFilter = MouseFilterEnum.Ignore };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (var side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, showHeading ? 28 : 20);
        AddChild(margin);
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", showHeading ? 12 : 6);
        margin.AddChild(body);
        var header = new HBoxContainer();
        body.AddChild(header);
        var brand = Label(UiText.Get("Brand"), showHeading ? 26 : 22, _accent);
        brand.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(brand);
        header.AddChild(Label(UiText.Get(section), 16, _muted));
        body.AddChild(new HSeparator());
        if (showHeading)
        {
            body.AddChild(Label(title, 28, _ink));
            var subtitle = Label(description, 16, _muted);
            subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            body.AddChild(subtitle);
        }
        return body;
    }

    private static Label Label(string text, int size, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Button Button(string key, Action action)
    {
        var button = new Button { Name = key, Text = UiText.Get(key), CustomMinimumSize = new Vector2(0, 44) };
        button.Pressed += action;
        return button;
    }

    private void ShowHome()
    {
        _loadRequest++;
        var body = Frame("Version", UiText.Get("HomeTitle"), UiText.Get("HomeDescription"));
        var center = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
        body.AddChild(center);
        center.AddChild(Label(UiText.Get("SeasonTitle"), 24, _accent));
        var help = Label(UiText.Get("HomeHelp"), 18, _muted);
        help.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        center.AddChild(help);
        var start = Button("BrowseClubs", () => { if (_queries is null) LoadWorld(); else ShowClubs(); });
        center.AddChild(start);
        body.AddChild(Label(UiText.Get("ReadOnlyNotice"), 15, _muted));
        start.GrabFocus();
    }

    /// <summary>Lee en segundo plano; la caché contiene los bytes originales del SQLite embebido y no es una partida.</summary>
    private async void LoadWorld()
    {
        var request = ++_loadRequest;
        var body = Frame("Version", UiText.Get("LoadingTitle"), UiText.Get("LoadingDescription"));
        body.AddChild(Button("Home", ShowHome));
        try
        {
            var cacheDirectory = ProjectSettings.GlobalizePath("user://catalogs");
            var queries = await Task.Run(() =>
            {
                using var source = typeof(Main).Assembly.GetManifestResourceStream("Furvo.World.Initial2026")
                    ?? throw new InvalidDataException("Falta el recurso de la publicación inicial.");
                var database = PublishedWorldQueries.Materialize(source, cacheDirectory);
                return PublishedWorldQueries.Open(database);
            });
            if (request != _loadRequest || !IsInsideTree()) return;
            _queries = queries;
            ShowClubs();
        }
        catch (Exception error)
        {
            if (request != _loadRequest || !IsInsideTree()) return;
            // La interfaz no imprime excepciones ni datos internos; el diagnóstico queda en la consola de desarrollo.
            GD.PushError(error.ToString());
            ShowError();
        }
    }

    private void ShowError(string description = "LoadErrorDescription", Action? retry = null, Action? back = null)
    {
        var body = Frame("Version", UiText.Get(description == "LoadErrorDescription" ? "LoadErrorTitle" : "QueryErrorTitle"), UiText.Get(description));
        body.AddChild(Button("Retry", retry ?? LoadWorld));
        body.AddChild(Button(back is null ? "Home" : "Back", back ?? ShowHome));
    }

    private void ShowClubs()
    {
        try
        {
            var clubs = _queries!.GetClubs();
            var body = Frame("ClubsSection", UiText.Get("ClubsTitle"), UiText.Format("ClubsDescription", clubs.Count));
            var table = new DataTable([
                new(UiText.Get("Club"), 360, 45, HeaderAlignment: HorizontalAlignment.Left, Wrap: true),
                new(UiText.Get("City"), 240, 30, CellAlignment: HorizontalAlignment.Center),
                new(UiText.Get("Reputation"), 200, 25, CellAlignment: HorizontalAlignment.Center)]);
            body.AddChild(table);
            var bar = new HBoxContainer();
            body.AddChild(bar);
            bar.AddChild(Button("Home", ShowHome));
            string? selected = null;
            void OpenClub(string id)
            {
                _club = clubs.Single(x => x.Id.ToString() == id);
                _sort = SquadSort.Name; _descending = false; _selectedPlayer = null;
                _clubTab = 0;
                ShowClub();
            }
            var open = Button("ViewClub", () => { if (selected is not null) OpenClub(selected); });
            open.Disabled = true;
            bar.AddChild(open);
            table.RowSelected += id => { selected = id; open.Disabled = false; };
            table.RowActivated += OpenClub;
            table.SetRows(clubs.Select(x => new TableRow(x.Id.ToString(), [x.Name, x.City ?? UiText.Get("Unknown"),
                x.VisibleReputation?.ToString(CultureInfo.InvariantCulture) ?? UiText.Get("Unknown")])));
            table.FocusTable();
        }
        catch (Exception error) { GD.PushError(error.ToString()); ShowError("ClubsErrorDescription", ShowClubs, ShowHome); }
    }

    /// <summary>La ficha individual vuelve a la misma pestaña Plantilla sin perder orden o selección.</summary>
    private void ShowSquad()
    {
        _clubTab = 1;
        ShowClub();
    }

    /// <summary>Composición de cuatro pestañas con identidad común. La plantilla utiliza el mismo servicio y DataTable anteriores.</summary>
    private void ShowClub()
    {
        try
        {
            var details = _queries!.GetClubDetails(_club!.Id);
            var history = _queries.GetClubHistory(_club.Id);
            var body = Frame("ClubSection", "", "", showHeading: false);
            body.AddChild(new ClubHeader(details));
            var tabs = new TabContainer { Name = "ClubTabs", SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
            tabs.SetMeta("club_id", _club.Id.ToString());
            tabs.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("0d192b"), ContentMarginTop = 8 });
            body.AddChild(tabs);
            tabs.AddChild(new ClubInformationPage(details, history));
            var squadPage = new VBoxContainer { Name = "ClubSquadPage" }; squadPage.SetMeta("club_id", _club.Id.ToString()); tabs.AddChild(squadPage);
            tabs.AddChild(new ClubHistoryPage(history)); tabs.AddChild(new ClubEconomyPage(_club.Id.ToString()));
            var keys = new[] { "ClubInformationTab", "ClubSquadTab", "ClubHistoryTab", "ClubEconomyTab" };
            for (var i = 0; i < keys.Length; i++) tabs.SetTabTitle(i, UiText.Get(keys[i]));
            var bar = new HBoxContainer(); body.AddChild(bar); bar.AddChild(Button("BackClubs", ShowClubs));
            var squad = _queries.GetSquad(_club.Id, _sort, _descending);
            squadPage.AddChild(Label(UiText.Format("SquadDescription", squad.Count, _queries.ReferenceDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)), 15, _muted));
            var table = new DataTable([
                new(UiText.Get("Name"), 280, 4, true), new(UiText.Get("Position"), 190, 2, true),
                new(UiText.Get("Age"), 70, 1, true), new(UiText.Get("Nationality"), 130, 2)]);
            squadPage.AddChild(table);
            var open = Button("ViewPlayer", () => { if (_selectedPlayer is not null) ShowPlayer(new PlayerId(Guid.Parse(_selectedPlayer))); });
            open.Disabled = _selectedPlayer is null; bar.AddChild(open);
            table.RowSelected += id => { _selectedPlayer = id; open.Disabled = false; };
            table.RowActivated += id => ShowPlayer(new PlayerId(Guid.Parse(id)));
            table.SortRequested += column =>
            {
                var next = column switch { 1 => SquadSort.Position, 2 => SquadSort.Age, _ => SquadSort.Name };
                _descending = next == _sort && !_descending; _sort = next; ShowSquad();
            };
            table.SetRows(squad.Select(x => new TableRow(x.Id.ToString(), [x.Name, UiText.Position(x.Position, x.Group),
                x.Age?.ToString(CultureInfo.InvariantCulture) ?? UiText.Get("Unknown"), x.Nationality ?? UiText.Get("Unknown")])), _selectedPlayer);
            table.MarkSort(_sort switch { SquadSort.Position => 1, SquadSort.Age => 2, _ => 0 }, _descending);
            tabs.CurrentTab = _clubTab; open.Visible = _clubTab == 1;
            tabs.TabChanged += tab => { _clubTab = (int)tab; open.Visible = _clubTab == 1; if (_clubTab == 1) table.FocusTable(); };
            if (_clubTab == 1) table.FocusTable();
        }
        catch (Exception error) { GD.PushError(error.ToString()); ShowError("ClubDetailsError", ShowClub, ShowClubs); }
    }

    /// <summary>Solo recibe la proyección pública: cuatro categorías, sin diccionarios internos ni categorías ocultas.</summary>
    private void ShowPlayer(PlayerId playerId)
    {
        try
        {
            _selectedPlayer = playerId.ToString();
            var details = _queries!.GetPlayer(_club!.Id, playerId);
            var body = Frame("PlayerSection", "", "", showHeading: false);
            body.AddChild(new PlayerSheet(details));
            var back = Button("BackSquad", ShowSquad);
            body.AddChild(back);
            back.GrabFocus();
        }
        catch (Exception error) { GD.PushError(error.ToString()); ShowError("PlayerErrorDescription", () => ShowPlayer(playerId), ShowSquad); }
    }
}
