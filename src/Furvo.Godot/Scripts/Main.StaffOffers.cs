using System.Globalization;
using Furvo.Core.Careers;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;

namespace Furvo.Presentation;

public partial class Main
{
    private SqliteStaffOffers StaffOffers() => new(SportsPath, _careers!.Load(_workspace!.Career.Id));
    private IReadOnlyList<StaffProfile> StaffCandidates() => SqliteStaffDirectory.Read(SportsPath,_workspace!.Career);

    private void RenderStaffOffers(VBoxContainer body, CareerPage page)
    {
        var people = StaffCandidates().ToDictionary(p => p.Id);
        var records = StaffOffers().History().GroupBy(o => o.Id)
            .Where(g => page != CareerPage.MarketNegotiations || g.Any(o => o.State == StaffOfferState.Sent))
            .Select(g => g.Last()).OrderByDescending(o => o.Date).ThenBy(o => o.Id).ToArray();
        if (records.Length == 0)
        {
            var key = page == CareerPage.MarketOffers ? "StaffOffersPending" : "StaffNegotiationsPending";
            body.AddChild(ClubUi.Text(key,UiText.Get(key)));
            return;
        }
        var table = new DataTable([new(UiText.Get("Name"),1,28),new(UiText.Get("StaffRole"),1,19),
            new(UiText.Get("Club"),1,22),new(UiText.Get("StaffOfferPhase"),1,14),new(UiText.Get("StaffProposalDate"),1,14),
            new(UiText.Get("StaffGrossSalary"),1,17),new(UiText.Get("TransferState"),1,20)],
            constrainColumns:true,proportionalColumns:true) { Name = "StaffOffersTable" };
        table.SetRows(records.Select(o => new TableRow(o.Id.ToString(),[people.GetValueOrDefault(o.Employee)?.Name ?? UiText.Get("Unknown"),
            UiText.Get("StaffRole_"+o.Terms.Role),o.Origin is { } origin ? _queries!.GetClubDetails(origin).Club.Name : UiText.Get("StaffFree"),
            UiText.Get("StaffPhase_"+o.Phase),FormatDate(o.Date),
            TransferMoney(o.Terms.Salary,o.Terms.Currency),UiText.Get("StaffOfferState_"+o.State)])));
        body.AddChild(table);
        var open = WindowUi.Button("TransferOpen", () => { if (table.SelectedId is { } id) ShowStaffOfferDetail(Guid.Parse(id),page); });
        open.Disabled = true;
        table.RowSelected += _ => open.Disabled = false;
        table.RowActivated += id => ShowStaffOfferDetail(Guid.Parse(id),page);
        body.AddChild(open);
    }

    private void ShowStaffOfferForm(Guid employee, StaffOffer? counter = null)
    {
        var person = StaffCandidates().Single(p => p.Id == employee);
        var body = Frame("CareerSection",UiText.Get("StaffMakeOffer"),person.Name);
        body.Name = "StaffOfferForm";
        var content = ClubUi.ScrollBody(body,"StaffOfferScroll");
        var columns = ClubUi.ResponsiveRow(content,"StaffOfferColumns");
        var basic = ClubUi.Card(columns,"StaffOfferBasicTerms");
        var clausesPanel = ClubUi.Card(columns,"StaffOfferClauses");
        var teams = SportsTeams().Where(t => t.Category != "First").ToArray();
        var status = ClubUi.Text("StaffOfferFeedback","");
        var selected = new OptionButton { Name = "StaffOfferTeam" };
        foreach (var t in teams) selected.AddItem(t.Name);
        if (counter is not null) selected.Select(Array.FindIndex(teams,t=>t.Id==counter.Terms.Team));
        basic.AddChild(ClubUi.Text("StaffOfferTeamLabel",UiText.Get("StaffTeam")));
        basic.AddChild(selected);
        ClubUi.Field(basic,"StaffRole",UiText.Get("StaffRole_AcademyCoach"));
        var prior = counter?.Terms;
        var today = _workspace!.Career.CurrentDate;
        var errors = new Dictionary<LineEdit,Label>();
        var start = Field("StaffStartDate",FormatDate(prior?.Start ?? today));
        var end = Field("StaffExpiry",FormatDate(prior?.End ?? today.AddYears(1).AddDays(-1)));
        var salary = Field("StaffGrossSalary",Amount(prior?.Salary));
        var bonus = Choice(basic,"StaffSigningBonus","Sin prima",[0,5000,10000,25000,50000,100000],prior?.SigningBonus ?? 0," EUR",true);
        var compensation = Field("StaffCompensation",Amount(prior?.Compensation ?? StaffOfferRules.EstimateCompensation(person, today)));
        compensation.Editable = counter?.Phase != StaffOfferPhase.Personal &&
            person.ContractStatus(today) == StaffContractStatus.Contracted;
        if (compensation.Editable && prior is null)
            basic.AddChild(ClubUi.Text("StaffCompensationEstimate",UiText.Get("StaffCompensationEstimate"),13));
        var reduction = Choice(clausesPanel,"StaffRelegation","Sin reducción",[0,10,20,25,30,40,50],PriorClause(StaffClauseKind.RelegationReduction)?.Percentage ?? 0," %");
        var increase = Field("StaffPromotion",ClauseValue(StaffClauseKind.PromotionIncrease),clausesPanel);
        var previousRestoration = PriorClause(StaffClauseKind.PromotionRestoration);
        var restore = Choice(clausesPanel,"StaffRestoration","Sin recuperación",[0,25,50,75,100],
            previousRestoration is null ? 0 : previousRestoration.Percentage ?? 100," % de la reducción");
        var release = Field("StaffRelease",ClauseValue(StaffClauseKind.Release),clausesPanel);
        var severance = Field("StaffSeverance",ClauseValue(StaffClauseKind.Severance),clausesPanel);
        var promotionBonus = Field("StaffPromotionBonus",ClauseValue(StaffClauseKind.ObjectiveBonus),clausesPanel);
        body.AddChild(status);
        var actions = new HFlowContainer();
        body.AddChild(actions);
        var save = WindowUi.Button("StaffSaveOffer",() =>
        {
            try
            {
                var from = ParseDate(start);
                var until = ParseDate(end);
                if (from < today) throw Invalid(start,"El inicio no puede ser anterior a la fecha de la carrera.");
                if (until <= from || until.DayNumber - from.DayNumber > 1096) throw Invalid(end,"El vencimiento debe ser posterior al inicio. El plazo máximo es de tres años.");
                var clauses = new List<StaffClause>();
                if (reduction() is > 0 and var cut) clauses.Add(new(StaffClauseKind.RelegationReduction,cut,null,null));
                AddClause(increase,StaffClauseKind.PromotionIncrease,true);
                AddClause(release,StaffClauseKind.Release);
                AddClause(severance,StaffClauseKind.Severance);
                AddClause(promotionBonus,StaffClauseKind.ObjectiveBonus,objective:"promotion");
                if (restore() is > 0 and var recovery) clauses.Add(new(StaffClauseKind.PromotionRestoration,recovery,null,null));
                var annual = TransferRulesMoney(salary);
                if (annual <= 0) throw Invalid(salary,"Introduce un salario positivo.");
                var terms = new StaffOfferTerms(teams[selected.Selected].Id,"AcademyCoach",from,until,
                    annual,"EUR",bonus(),TransferRulesMoney(compensation),clauses);
                var store = StaffOffers();
                var draft = counter is null ? store.Draft(employee,terms) : store.Move(counter,"Revise",terms);
                ShowStaffOfferDetail(draft.Id,CareerPage.MarketOffers);
                void AddClause(LineEdit input, StaffClauseKind kind, bool percent = false, string? objective = null)
                {
                    if (string.IsNullOrWhiteSpace(input.Text)) return;
                    var value = TransferRulesMoney(input);
                    if (value <= 0 || percent && value > 100) throw Invalid(input,percent ? "Introduce un porcentaje mayor que 0 y hasta 100." : "Introduce un importe positivo.");
                    clauses.Add(new(kind,percent ? value : null,percent ? null : value,objective));
                }
            }
            catch (Exception error) { status.Text = error.Message; }
        });
        save.Disabled = teams.Length == 0;
        actions.AddChild(save);
        actions.AddChild(WindowUi.Button("Back",()=> { if(counter is null) ShowStaffDirectory(true); else ShowStaffOfferDetail(counter.Id,CareerPage.MarketOffers); }));

        StaffClause? PriorClause(StaffClauseKind kind) => prior?.Clauses.FirstOrDefault(v=>v.Kind==kind);
        string ClauseValue(StaffClauseKind kind)
        {
            var clause = prior?.Clauses.FirstOrDefault(v=>v.Kind==kind);
            return Amount(clause?.Percentage ?? clause?.Amount);
        }
        LineEdit Field(string key,string value,Control? column = null)
        {
            var row = new HBoxContainer();
            var parent = column ?? basic;
            parent.AddChild(row);
            row.AddChild(ClubUi.Text(key+"Label",UiText.Get(key),13));
            var edit = new LineEdit { Name = key, Text = value, SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                CustomMinimumSize = new Vector2(210,34) };
            row.AddChild(edit);
            var error = ClubUi.Text(key+"Error","",12,"ec8e8e");
            error.Hide();
            parent.AddChild(error);
            errors.Add(edit,error);
            var last = value;
            edit.TextChanged += text =>
            {
                if (key is not ("StaffStartDate" or "StaffExpiry") && text.Any(c => !char.IsAsciiDigit(c) && c is not (',' or '.')))
                {
                    edit.Text = last;
                    edit.CaretColumn = last.Length;
                    error.Text = "Introduce solo cifras y un separador decimal.";
                    error.Show();
                    return;
                }
                last = text;
                error.Hide();
            };
            return edit;
        }
        static string Amount(decimal? number) => number?.ToString("0.##",CultureInfo.GetCultureInfo("es-ES")) ?? "";
        decimal TransferRulesMoney(LineEdit edit)
        {
            try { return Furvo.Core.Market.TransferRules.ParseMoney(edit.Text); }
            catch (InvalidDataException e) { throw Invalid(edit,e.Message); }
        }
        DateOnly ParseDate(LineEdit edit) => DateOnly.TryParseExact(edit.Text,"dd/MM/yyyy",CultureInfo.InvariantCulture,
            DateTimeStyles.None,out var date) ? date : throw Invalid(edit,"Introduce una fecha con formato DD/MM/AAAA.");
        InvalidDataException Invalid(LineEdit edit,string message)
        {
            errors[edit].Text = message;
            errors[edit].Show();
            return new InvalidDataException(message);
        }
        Func<decimal> Choice(Control parent,string key,string zero,decimal[] values,decimal initial,string unit,bool custom = false)
        {
            var row = new HBoxContainer();
            parent.AddChild(row);
            row.AddChild(ClubUi.Text(key+"Label",UiText.Get(key),13));
            var options = new OptionButton { Name = key, CustomMinimumSize = new Vector2(210,34) };
            row.AddChild(options);
            var choices = values.Append(initial).Distinct().Order().ToArray();
            foreach (var value in choices) options.AddItem(value == 0 ? zero : Amount(value)+unit);
            options.Select(Array.IndexOf(choices,initial));
            if (!custom) return () => choices[options.Selected];
            options.AddItem("Otro importe");
            var other = Field("StaffCustomBonus","",parent);
            void RefreshCustom()
            {
                other.GetParent<Control>().Visible = options.Selected == choices.Length;
                errors[other].Hide();
            }
            options.ItemSelected += _ => RefreshCustom();
            RefreshCustom();
            return () => options.Selected == choices.Length ? TransferRulesMoney(other) : choices[options.Selected];
        }
    }

    private void ShowStaffOfferDetail(Guid id, CareerPage page)
    {
        var service = StaffOffers();
        var history = service.History(id);
        var latest = history.Last();
        var person = StaffCandidates().Single(p => p.Id == latest.Employee);
        var body = Frame("CareerSection",person.Name,UiText.Get("StaffOfferDetail"));
        body.Name = "StaffOfferDetailPage";
        var content = ClubUi.ScrollBody(body,"StaffOfferHistory");
        foreach (var entry in history.Reverse())
        {
            var card = ClubUi.Card(content,"StaffOfferDetail");
            ClubUi.Field(card,"StaffProposalDate",FormatDate(entry.Date));
            ClubUi.Field(card,"TransferState",UiText.Get("StaffOfferState_"+entry.State));
            ClubUi.Field(card,"StaffOfferPhase",UiText.Get("StaffPhase_"+entry.Phase));
            ClubUi.Field(card,"StaffGrossSalary",TransferMoney(entry.Terms.Salary,entry.Terms.Currency));
            ClubUi.Field(card,"StaffSigningBonus",TransferMoney(entry.Terms.SigningBonus,entry.Terms.Currency));
            ClubUi.Field(card,"StaffCompensation",TransferMoney(entry.Terms.Compensation,entry.Terms.Currency));
            ClubUi.Field(card,"StaffStartDate",FormatDate(entry.Terms.Start));
            ClubUi.Field(card,"StaffExpiry",FormatDate(entry.Terms.End));
            foreach (var clause in entry.Terms.Clauses)
                card.AddChild(ClubUi.Text("StaffClause",UiText.Get("StaffClause_"+clause.Kind) + " " +
                    (clause.Kind == StaffClauseKind.PromotionRestoration ? (clause.Percentage ?? 100) + "% de la reducción"
                    : clause.Percentage is { } percent ? percent + "%" : clause.Amount is { } amount ? TransferMoney(amount,entry.Terms.Currency) : "")));
            if (!string.IsNullOrEmpty(entry.Reason)) card.AddChild(ClubUi.Text("StaffOfferReason",entry.Reason));
        }
        if (latest.State == StaffOfferState.Conditional)
            content.AddChild(ClubUi.Text("StaffFormalizationBlocked",UiText.Get("StaffFormalizationBlocked")));
        var feedback = ClubUi.Text("StaffOfferFeedback","");
        body.AddChild(feedback);
        var actions = new HFlowContainer();
        body.AddChild(actions);
        if (latest.State == StaffOfferState.Draft) ActionButton("StaffSendOffer","Send");
        if (latest.State == StaffOfferState.Draft)
            actions.AddChild(WindowUi.Button("StaffEditDraft",()=>ShowStaffOfferForm(latest.Employee,latest)));
        if (latest.State == StaffOfferState.Sent) ActionButton("StaffEvaluateOffer","Evaluate");
        if (latest.State == StaffOfferState.ClubAgreed) ActionButton("StaffStartPersonal","Personal");
        if (latest.State == StaffOfferState.Counter) actions.AddChild(WindowUi.Button("StaffReviseOffer",()=>ShowStaffOfferForm(latest.Employee,latest)));
        if (latest.State is StaffOfferState.Draft or StaffOfferState.Sent or StaffOfferState.Counter or StaffOfferState.ClubAgreed or StaffOfferState.Conditional)
            ActionButton("StaffWithdrawOffer","Withdraw");
        actions.AddChild(WindowUi.Button("Back",()=>ShowTransferList(page,true)));
        void ActionButton(string key,string action) => actions.AddChild(WindowUi.Button(key,() =>
        {
            try { service.Move(latest,action); ShowStaffOfferDetail(id,page); }
            catch(Exception error) { feedback.Text = error.Message; }
        }));
    }
}
