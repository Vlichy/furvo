using System.Globalization;
using System.Diagnostics;
using Furvo.Core.Careers;
using Furvo.Core.Worlds;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private MarketSearchContext? _marketContext;
    private MarketSnapshot? _marketSnapshot;
    private MarketColumnPreferences? _marketColumns;
    private string? _marketPreferencesWarning;
    private string MarketPreferencesPath=>Path.Combine(_careerDataRoot??ProjectSettings.GlobalizePath("user://"),"ui","market-columns.v1.json");
    private void EnsureMarketPreferences()
    {
        if(_marketColumns is not null)return;
        try{_marketColumns=MarketColumnPreferences.Load(MarketPreferencesPath);}
        catch(Exception error){GD.PushWarning(error.ToString());_marketColumns=new();_marketPreferencesWarning=UiText.Get("MarketPreferencesLoadError");}
    }
#if DEBUG
    /// <summary>QA exclusivamente aislada: permite comprobar preferencias en un proceso nuevo, sin carreras personales.</summary>
    public void OpenIsolatedMarketForValidation(string career)
    {
        if(_careerDataRoot is null)throw new InvalidOperationException("Se requiere almacenamiento aislado.");
        LoadWorldThen(()=>{EnsureCareerServices();EnterCareer(_careers!.Load(new SaveId(Guid.Parse(career))));NavigateCareer(CareerPage.MarketSearch);});
    }
#endif
    private void ShowMarket()
    {
        _workspace!.Open(CareerPage.MarketSearch);
        var career=_workspace.Career.Id.Value;
        if(_marketContext?.Career!=career)_marketContext=new(career);
        var context=_marketContext;
        EnsureMarketPreferences();
        var body=Frame("CareerSection",UiText.Get("MarketTitle"),UiText.Get("MarketReadOnly"));
        body.Name="MarketPage";body.SetMeta("market_career",career.ToString());
        try
        {
            var watch=Stopwatch.StartNew();_marketSnapshot=new SqliteMarketQueries(SportsPath).Read();watch.Stop();
            if(_marketSnapshot.Career!=career)throw new InvalidDataException(UiText.Get("MarketWrongCareer"));
            var snapshot=_marketSnapshot;
            // Un cambio de empleo invalida resultados, selección y filtros anteriores, nunca las preferencias visuales.
            if(!context.MatchesScope(career,snapshot.ManagedClub)){_marketContext=new(career,snapshot.ManagedClub);context=_marketContext;}
            var availableClubs=snapshot.Clubs.Where(c=>c.Id!=snapshot.ManagedClub).ToArray();
            GD.Print("FURVO_MARKET_LOAD: players="+snapshot.Players.Count+" ms="+watch.ElapsedMilliseconds);
            body.SetMeta("market_universe",snapshot.Players.Count);
            var first=new HBoxContainer{Name="MarketFilters"};first.AddThemeConstantOverride("separation",10);body.AddChild(first);
            void Field(HBoxContainer row,string key,Control input,int minimum=0)
            {
                var box=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};box.AddChild(ClubUi.Text(key+"Label",UiText.Get(key),13));
                input.SizeFlagsHorizontal=SizeFlags.ExpandFill;if(minimum>0)input.CustomMinimumSize=new Vector2(minimum,0);box.AddChild(input);row.AddChild(box);
            }
            var name=new LineEdit{Name="MarketName",Text=context.Name,PlaceholderText=UiText.Get("MarketNamePlaceholder")};Field(first,"MarketNameLabel",name,180);
            var club=new OptionButton{Name="MarketClub",FitToLongestItem=false,ClipText=true};club.AddItem(UiText.Get("MarketAllClubs"));
            foreach(var item in availableClubs)club.AddItem(item.Name);
            club.Selected=context.Club is {} chosen?Math.Max(0,availableClubs.ToList().FindIndex(p=>p.Id==chosen)+1):0;TrainingUi.LimitPopup(club);Field(first,"MarketClubLabel",club,200);
            var positions=Enum.GetValues<PlayerRole>();var position=new OptionButton{Name="MarketPosition",FitToLongestItem=false,ClipText=true};position.AddItem(UiText.Get("MarketAllPositions"));
            foreach(var item in positions)position.AddItem(UiText.Get("Position_"+item));
            position.Selected=context.Position is {} role?Array.IndexOf(positions,role)+1:0;TrainingUi.LimitPopup(position);Field(first,"MarketPositionLabel",position,150);
            var minimumAge=new LineEdit{Name="MarketMinimumAge",Text=context.MinimumAge,PlaceholderText="—"};var maximumAge=new LineEdit{Name="MarketMaximumAge",Text=context.MaximumAge,PlaceholderText="—"};
            Field(first,"MarketMinimumAgeLabel",minimumAge,64);Field(first,"MarketMaximumAgeLabel",maximumAge,64);
            var second=new HBoxContainer{Name="MarketAttributeFilters"};second.AddThemeConstantOverride("separation",10);body.AddChild(second);
            var definitions=PlayerAttributeCatalog.All.Where(a=>!a.IsHidden).ToArray();
            var attribute=new OptionButton{Name="MarketAttribute",FitToLongestItem=false,ClipText=true};attribute.AddItem(UiText.Get("MarketAnyAttribute"));
            foreach(var item in definitions){attribute.AddItem(UiText.Get("Attribute_"+item.Code));attribute.SetItemMetadata(attribute.ItemCount-1,item.Code);}
            attribute.Selected=context.Attribute is {} code?Math.Max(0,Array.FindIndex(definitions,a=>a.Code==code)+1):0;TrainingUi.LimitPopup(attribute);Field(second,"MarketAttributeLabel",attribute,220);
            // LineEdit conserva exactamente el borrador tecleado: no redondea decimales ni recorta valores fuera de escala.
            var minimumValue=new LineEdit{Name="MarketMinimumValue",Text=context.MinimumValueText,PlaceholderText="1"};
            var maximumValue=new LineEdit{Name="MarketMaximumValue",Text=context.MaximumValueText,PlaceholderText="20"};
            Field(second,"MarketMinimumValueLabel",minimumValue,70);Field(second,"MarketMaximumValueLabel",maximumValue,70);
            var apply=WindowUi.Button("MarketSearch",()=>{});second.AddChild(apply);
            second.AddChild(WindowUi.Button("MarketClear",()=>{_marketContext=new(career);ShowMarket();}));
            var status=ClubUi.Text("MarketStatus",context.FilterError??_marketPreferencesWarning??"",13,"f1b96b");body.AddChild(status);
            var tools=new HBoxContainer{Name="MarketTableTools"};body.AddChild(tools);
            var count=ClubUi.Text("MarketCount","",14);count.SizeFlagsHorizontal=SizeFlags.ExpandFill;tools.AddChild(count);
            var host=new VBoxContainer{SizeFlagsVertical=SizeFlags.ExpandFill};body.AddChild(host);
            var selection=ClubUi.Text("MarketSelection",UiText.Get("MarketNoSelection"),13);body.AddChild(selection);
            DataTable? table=null;
            var open=WindowUi.Button("MarketOpenPlayer",()=>{if(context.Selected is {} id){context.Scroll=table!.VerticalScroll;context.HorizontalScroll=table.HorizontalScroll;ShowMarketPlayer(id);}});body.AddChild(open);
            void Refresh()
            {
                var columns=_marketColumns!.Columns(context.Position);context.ReconcileSort(columns);
                var rows=MarketQueries.Search(snapshot,context.Applied,context.Sort,context.Descending,context.SortAttribute);context.Reconcile(rows);
                var previous=table;
                if(previous is not null){host.RemoveChild(previous);previous.QueueFree();}
                var headings=new List<TableColumn>{
                    new(UiText.Get("Name"),235,Sortable:true,HeaderAlignment:HorizontalAlignment.Left,Wrap:true),
                    new(UiText.Get("Age"),65,Sortable:true),
                    new(UiText.Get("Club"),230,Sortable:true,Wrap:true),
                    new(UiText.Get("Position"),210,Sortable:true,Wrap:true)};
                foreach(var attributeCode in columns){
                    var title=UiText.Get("Attribute_"+attributeCode);
                    headings.Add(new(title,Math.Max(130,title.Length*10+30),Sortable:true,CellAlignment:HorizontalAlignment.Center));
                }
                headings.Add(new(UiText.Get("Nationality"),130));headings.Add(new(UiText.Get("MarketContractEnd"),165,Sortable:true));
                var current=new DataTable(headings,rowHeight:34,rowSpacing:2,fixedWidths:true){Name="MarketResults"};table=current;host.AddChild(current);
                current.SetMeta("market_columns",string.Join(",",columns));
                current.SetRows(rows.Select(row=>{
                    var cells=new List<string>{row.Details.Player.Name,row.Details.Player.Age?.ToString()??UiText.Get("NotAvailable"),row.Details.Club?.Name??UiText.Get("NotAvailable"),UiText.Position(row.Details.Player.Position,row.Details.Player.Group)};
                    foreach(var attributeCode in columns)cells.Add(MarketQueries.AttributeValue(row,attributeCode)?.ToString()??UiText.Get("NotAvailable"));
                    cells.Add(row.Details.Player.Nationality??UiText.Get("NotAvailable"));cells.Add(row.Contract?.End?.ToString("dd/MM/yyyy")??UiText.Get("NotAvailable"));
                    return new TableRow(row.Details.Player.Id.ToString(),cells);
                }),context.Selected?.ToString());
                current.MarkSort(context.Sort switch{MarketSort.Age=>1,MarketSort.Club=>2,MarketSort.Position=>3,MarketSort.ContractEnd=>5+columns.Count,MarketSort.Attribute=>4+columns.ToList().IndexOf(context.SortAttribute!),_=>0},context.Descending);
                count.Text=snapshot.Players.Count==0?UiText.Get("MarketNoData"):rows.Count==0?UiText.Get("MarketNoResults"):UiText.Format("MarketResultsCount",rows.Count,snapshot.Players.Count);
                count.SetMeta("market_count",rows.Count);open.Disabled=context.Selected is null;
                void Selection(){selection.Text=context.Selected is {} selected?UiText.Format("MarketSelected",MarketQueries.Player(snapshot,selected).Details.Player.Name,MarketQueries.Player(snapshot,selected).Details.Club?.Name??UiText.Get("NotAvailable")):UiText.Get("MarketNoSelection");}
                Selection();
                current.RowSelected+=id=>{context.Selected=new PlayerId(Guid.Parse(id));open.Disabled=false;Selection();};
                current.RowActivated+=id=>{context.Selected=new PlayerId(Guid.Parse(id));context.Scroll=current.VerticalScroll;context.HorizontalScroll=current.HorizontalScroll;ShowMarketPlayer(context.Selected.Value);};
                current.SortRequested+=column=>{
                    var isAttribute=column>=4&&column<4+columns.Count;
                    var next=isAttribute?MarketSort.Attribute:column==5+columns.Count?MarketSort.ContractEnd:column switch{1=>MarketSort.Age,2=>MarketSort.Club,3=>MarketSort.Position,_=>MarketSort.Name};
                    var nextAttribute=isAttribute?columns[column-4]:null;
                    context.Descending=next==context.Sort&&nextAttribute==context.SortAttribute&&!context.Descending;context.Sort=next;context.SortAttribute=nextAttribute;Refresh();
                };
                async void Restore(){
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    if(IsInstanceValid(current)&&current==table){current.VerticalScroll=context.Scroll;current.HorizontalScroll=context.HorizontalScroll;current.ScrollChanged+=v=>context.Scroll=v;current.HorizontalScrollChanged+=v=>context.HorizontalScroll=v;}
                }
                Restore();
            }
            tools.AddChild(WindowUi.Button("MarketConfigureColumns",()=>{
                var dialog=new MarketColumnsWindow(Theme,_marketColumns!,context.Position,codes=>{
                    _marketColumns!.Set(context.Position,codes);
                    try{_marketColumns.Save(MarketPreferencesPath);_marketPreferencesWarning=null;status.Text=context.FilterError??"";}
                    catch(Exception error){GD.PushWarning(error.ToString());_marketPreferencesWarning=UiText.Get("MarketPreferencesSaveError");status.Text=_marketPreferencesWarning;}
                    Refresh();
                });
                AddChild(dialog);dialog.Open();
            }));
            void Draft(){
                context.Name=name.Text;context.Club=club.Selected>0?availableClubs[club.Selected-1].Id:null;context.Position=position.Selected>0?positions[position.Selected-1]:null;
                context.MinimumAge=minimumAge.Text;context.MaximumAge=maximumAge.Text;context.Attribute=attribute.Selected>0?definitions[attribute.Selected-1].Code:null;
                context.MinimumValueText=minimumValue.Text;context.MaximumValueText=maximumValue.Text;minimumValue.Editable=maximumValue.Editable=attribute.Selected>0;
            }
            int? Age(string text){if(string.IsNullOrWhiteSpace(text))return null;if(!int.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var result))throw new InvalidDataException(UiText.Get("MarketAgeInvalid"));return result;}
            void Search(){
                try{
                    Draft();var range=context.Attribute is null?(Minimum:1,Maximum:20):MarketQueries.ParseAttributeRange(minimumValue.Text,maximumValue.Text);
                    var filter=new MarketFilter(context.Name,context.Club,context.Position,Age(context.MinimumAge),Age(context.MaximumAge),context.Attribute,range.Minimum,range.Maximum);filter.Validate();
                    context.Applied=filter;context.Scroll=0;context.FilterError=null;status.Text=_marketPreferencesWarning??"";Refresh();
                }catch(Exception error){context.FilterError=UiText.Get("MarketFiltersNotApplied")+" "+error.Message;status.Text=context.FilterError;}
            }
            apply.Pressed+=Search;name.TextSubmitted+=_=>Search();minimumValue.TextSubmitted+=_=>Search();maximumValue.TextSubmitted+=_=>Search();
            name.TextChanged+=_=>Draft();minimumAge.TextChanged+=_=>Draft();maximumAge.TextChanged+=_=>Draft();club.ItemSelected+=_=>Draft();
            position.ItemSelected+=_=>{Draft();context.HorizontalScroll=0;Refresh();};
            attribute.ItemSelected+=_=>Draft();minimumValue.TextChanged+=_=>Draft();maximumValue.TextChanged+=_=>Draft();Draft();Refresh();
        }
        catch(Exception error){_marketSnapshot=null;GD.PushWarning(error.ToString());body.AddChild(ClubUi.Text("MarketLoadError",UiText.Get("MarketLoadError"),16,"f1b96b"));body.AddChild(WindowUi.Button("MarketRetry",ShowMarket));}
    }
    private void ShowMarketPlayer(PlayerId id)
    {
        try{
            // Revalidar la pertenencia vigente al abrir ficha evita reutilizar resultados de otro empleo.
            var snapshot=new SqliteMarketQueries(SportsPath).Read();
            if(_marketContext is null||!_marketContext.MatchesScope(snapshot.Career,snapshot.ManagedClub)){ShowMarket();return;}
            _marketSnapshot=snapshot;
            if(snapshot.Career!=_workspace!.Career.Id.Value)throw new InvalidOperationException(UiText.Get("MarketWrongCareer"));var row=MarketQueries.Player(snapshot,id);_marketContext!.Selected=id;_workspace.Open(CareerPage.MarketSearch);
            var body=Frame("PlayerSection","","",false);body.Name="MarketPlayerPage";body.SetMeta("market_player",id.ToString());
            var supplementary=new VBoxContainer{Name="MarketContractPanel"};supplementary.AddChild(ClubUi.Text("MarketSportingTeam",UiText.Get("MarketSportingTeam")+": "+(row.TeamName??UiText.Get("NotAvailable"))+" · "+UiText.Get("MarketCategory_"+(row.Category??"Unknown")),13));
            var fields=new VBoxContainer{Name="MarketContractFields",Visible=false};var toggle=WindowUi.Button("MarketContractToggle",()=>fields.Visible=!fields.Visible);supplementary.AddChild(toggle);supplementary.AddChild(fields);
            ClubUi.Field(fields,"MarketContractId",row.Contract?.Id.ToString()??UiText.Get("NotAvailable"));ClubUi.Field(fields,"MarketContractClub",row.Contract?.Club.Name??UiText.Get("NotAvailable"));ClubUi.Field(fields,"MarketContractStart",row.Contract?.Start?.ToString("dd/MM/yyyy")??UiText.Get("NotAvailable"));ClubUi.Field(fields,"MarketContractEnd",row.Contract?.End?.ToString("dd/MM/yyyy")??UiText.Get("NotAvailable"));
            fields.AddChild(ClubUi.Text("MarketContractState",UiText.Get(row.Contract?.Active is true?"MarketContractActive":row.Contract?.Active is false?"MarketContractPastOrFuture":"MarketContractUnknown"),13,"f1b96b"));fields.AddChild(ClubUi.Text("MarketContractUnavailable",UiText.Get("MarketContractUnavailable"),13));
            supplementary.AddChild(WindowUi.Button("TransferMakeOffer",()=>ShowTransferForm(id)));
            body.AddChild(new PlayerSheet(row.Details,supplementary,new(row.Contract?.Id,row.Contract?.Club.Name??row.Details.Club?.Name,row.Contract?.End,row.Contract?.Release??ReleaseClauseInfo.Unknown)));body.AddChild(WindowUi.Button("MarketBackSearch",ShowMarket));
        }catch(Exception error){GD.PushWarning(error.ToString());ShowError("MarketPlayerError",ShowMarket,()=>NavigateCareer(CareerPage.Desktop));}
    }
}
