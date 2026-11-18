using Furvo.Core.Careers;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;

namespace Furvo.Presentation;

public partial class Main
{
    private void PersonnelNavigation(Control parent, string active, Action<Action>? guard = null)
    {
        var row = new HFlowContainer { Name = "PersonnelNavigation" };
        parent.AddChild(row);
        row.AddChild(Tab("StaffRoster", () => ShowStaffDirectory(false)));
        row.AddChild(Tab("StaffDevelopment", ShowStaffDevelopment));
        row.AddChild(Tab("StaffDelegations", () => ShowClubDelegations()));
        Button Tab(string key, Action action)
        {
            var button = WindowUi.Button(key, () => { if (guard is null) action(); else guard(action); });
            button.Disabled = active == key;
            return button;
        }
    }

    private void ShowStaffDevelopment()
    {
        var body = Frame("CareerSection", UiText.Get("StaffDevelopment"), "");
        PersonnelNavigation(body, "StaffDevelopment");
        body.AddChild(ClubUi.Text("StaffDevelopmentNotice", UiText.Get("StaffDevelopmentNotice")));
    }

    private void ShowStaffDirectory(bool market)
    {
        var career = _careers!.Load(_workspace!.Career.Id);
        var people = SqliteStaffDirectory.Read(SportsPath, career)
            .Where(p => market ? p.Club != career.ClubId : p.Club == career.ClubId).ToArray();
        var title = market ? "NavPage_MarketStaff" : "StaffRoster";
        var body = Frame("CareerSection", UiText.Get(title), UiText.Get(market ? "StaffMarketNotice" : "StaffRosterNotice"));
        body.Name = "StaffDirectoryPage";
        if (!market) PersonnelNavigation(body, title);
        var filters = new HFlowContainer();
        body.AddChild(filters);
        var name = new LineEdit { Name = "StaffSearchName", PlaceholderText = UiText.Get("Name"), CustomMinimumSize = new(220, 36) };
        name.SizeFlagsVertical = SizeFlags.ShrinkEnd;
        filters.AddChild(name);
        string Department(StaffProfile person) => person.Role == "AcademyCoach" ? "ClubDelegationsDepartment_Academy" : "ClubDelegationsFirstTeam";
        string Team(StaffProfile person) => person.Team ?? (market && person.Club is { } club
            ? _queries!.GetClubDetails(club).Club.Name : UiText.Get("ClubDelegationsFirstTeam"));

        OptionButton? contract = null, expiry = null, department = null, team = null, reputation = null;
        var ratings = people.Where(p => p.Reputation.HasValue).Select(p => p.Reputation!.Value).Distinct().Order().ToArray();
        var departmentKeys = people.Select(Department).Distinct().Order().ToArray();
        var teamNames = people.Select(Team).Distinct().Order().ToArray();
        if (market)
        {
            contract = Picker("StaffSearchStatus", "StaffAll", ["StaffFree", "StaffContracted"], UiText.Get);
            var expiryBox = new VBoxContainer { Name = "StaffExpiryFilter" };
            expiryBox.AddChild(ClubUi.Text("StaffExpiryFilterLabel", UiText.Get("StaffExpiryFilterLabel"), 13));
            filters.AddChild(expiryBox);
            expiry = Picker("StaffSearchExpiry", "StaffAnyExpiry", ["StaffThreeMonths", "StaffSixMonths", "StaffTwelveMonths"], UiText.Get, expiryBox);
            expiry.CustomMinimumSize = new Vector2(245, 36);
            if (ratings.Length > 0)
                reputation = Picker("StaffSearchReputation", "StaffAllReputations", ratings.Select(value => value.ToString()).ToArray(),
                    value => UiText.Format("StaffReputationFilter", value));
        }
        else
        {
            department = Picker("StaffSearchDepartment", "StaffAllDepartments", departmentKeys, UiText.Get);
            team = Picker("StaffSearchTeam", "StaffAllTeams", teamNames, value => value);
        }
        var roles = people.Select(p => p.Role).Distinct().Order().ToArray();
        var role = roles.Length > 1
            ? Picker("StaffSearchRole", "StaffAllRoles", roles, key => UiText.Get("StaffRole_" + key))
            : null;

        var status = ClubUi.Text("StaffSearchCount", "");
        body.AddChild(status);
        var empty = ClubUi.Text("StaffSearchEmpty", "");
        body.AddChild(empty);
        var table = new DataTable(market
            ? [
                Column("Name", 25), Column("StaffRole", 20, center: true), Column("StaffTeam", 26),
                Column("StaffContractStatus", 18, center: true), Column("StaffExpiry", 11, center: true, sortable: true)
            ]
            : [
                Column("Name", 23), Column("StaffRole", 19, center: true), Column("StaffDepartment", 13),
                Column("StaffTeam", 23), Column("StaffResponsibilities", 22)
            ], rowHeight: 34, rowSpacing: 4, constrainColumns: true, proportionalColumns: true)
        { Name = "StaffDirectoryTable" };
        body.AddChild(table);
        var byId = people.ToDictionary(p => p.Id.ToString("D"));
        var responsibilities = new Dictionary<Guid, List<string>>();
        var open = WindowUi.Button("StaffOpenProfile", () => { if (table.SelectedId is { } id) OpenProfile(id); });
        body.AddChild(open);
        if (!market)
        {
            var sport = Sports();
            var eligiblePeople = people.Select(p => p.Id).ToHashSet();
            foreach (var squad in SportsTeams())
            {
                var plan = sport.Plan(squad.Id);
                if (plan.Delegations.Count == 0) continue;
                var eligible = sport.Staff(squad.Id).ToDictionary(p => p.Id);
                foreach (var assignment in plan.Delegations)
                    if (assignment.Responsible is { } id && eligiblePeople.Contains(id)
                        && eligible.TryGetValue(id, out var staff) && staff.Responsibilities.Contains(assignment.Area))
                        AddResponsibility(id, UiText.Get("TrainingResponsibility_" + assignment.Area) + " · " + squad.Name +
                            " · " + UiText.Get(assignment.Mode switch
                            {
                                Furvo.Core.Development.TrainingDelegationMode.Delegated => "ClubDelegationFull",
                                Furvo.Core.Development.TrainingDelegationMode.Mixed => "ClubDelegationSupervised",
                                _ => "ClubDelegationPersonal"
                            }));
            }
            foreach (var state in Academy().List())
                if (state.EffectiveManagement == Furvo.Core.Academy.AcademyManagement.Delegated && eligiblePeople.Contains(state.Coach.Id))
                    AddResponsibility(state.Coach.Id, UiText.Get("AcademyDelegationLineups") + " · " + state.Team.Name +
                        " · " + UiText.Get("AcademyManagement_Delegated"));
        }

        bool? descendingExpiry = null;
        void Refresh()
        {
            var selected = table.SelectedId;
            if (expiry is not null)
            {
                expiry.Disabled = contract?.Selected == 1;
                if (expiry.Disabled) expiry.Select(0);
            }
            var matches = StaffSearch.Find(people, career.CurrentDate, name.Text, contract?.Selected switch
            {
                1 => StaffContractStatus.Free,
                2 => StaffContractStatus.Contracted,
                _ => null
            }, expiry?.Selected switch { 1 => 3, 2 => 6, 3 => 12, _ => null },
                role is { Selected: > 0 } ? roles[role.Selected - 1] : null,
                reputation is { Selected: > 0 } ? ratings[reputation.Selected - 1] : null)
                .Where(p => (department is not { Selected: > 0 } || Department(p) == departmentKeys[department.Selected - 1])
                    && (team is not { Selected: > 0 } || Team(p) == teamNames[team.Selected - 1])).ToArray();
            IEnumerable<StaffProfile> rows = matches;
            if (descendingExpiry is { } descending)
            {
                var known = matches.Where(p => p.Contract?.EndDate is not null);
                rows = (descending ? known.OrderByDescending(p => p.Contract!.EndDate) : known.OrderBy(p => p.Contract!.EndDate))
                    .Concat(matches.Where(p => p.Contract?.EndDate is null));
            }
            table.SetRows(rows.Select(person => new TableRow(person.Id.ToString("D"), market
                ? [person.Name, UiText.Get("StaffRole_" + person.Role), Team(person),
                    UiText.Get("Staff" + person.ContractStatus(career.CurrentDate)), FormatDate(person.Contract?.EndDate)]
                : [person.Name, UiText.Get("StaffRole_" + person.Role), UiText.Get(Department(person)), Team(person),
                    responsibilities.TryGetValue(person.Id, out var tasks) ? string.Join(" / ", tasks) : UiText.Get("StaffNoAssignments")])), selected);
            status.Text = UiText.Format("StaffResultCount", matches.Length);
            empty.Visible = matches.Length == 0;
            if (empty.Visible)
                empty.Text = market && contract?.Selected == 2 &&
                    !people.Any(p => p.ContractStatus(career.CurrentDate) == StaffContractStatus.Contracted)
                    ? UiText.Get("StaffNoVerifiedContracts") : UiText.Get("StaffNoMatches");
            open.Disabled = table.SelectedId is null;
        }
        void AddResponsibility(Guid id, string task)
        {
            if (!responsibilities.TryGetValue(id, out var tasks)) responsibilities[id] = tasks = [];
            tasks.Add(task);
        }
        void OpenProfile(string id)
        {
            if (!byId.TryGetValue(id, out var person)) return;
            ShowStaffProfile(person, career.CurrentDate, () => ShowStaffDirectory(market), responsibilities.GetValueOrDefault(person.Id), market);
        }

        name.TextChanged += _ => Refresh();
        foreach (var option in new[] { contract, expiry, role, department, team, reputation })
            if (option is not null) option.ItemSelected += _ => Refresh();
        table.SortRequested += column =>
        {
            if (!market || column != 4) return;
            descendingExpiry = descendingExpiry is false;
            table.MarkSort(column, descendingExpiry.Value);
            Refresh();
        };
        table.RowSelected += _ => open.Disabled = false;
        table.RowActivated += OpenProfile;
        Refresh();

        OptionButton Picker(string id, string all, string[] values, Func<string, string> label, Control? parent = null)
        {
            var option = new OptionButton { Name = id, FitToLongestItem = false, ClipText = true,
                CustomMinimumSize = new Vector2(205, 36), TooltipText = UiText.Get(all), SizeFlagsVertical = SizeFlags.ShrinkEnd };
            option.AddItem(UiText.Get(all));
            foreach (var value in values) option.AddItem(label(value));
            option.ItemSelected += _ => option.TooltipText = option.GetItemText(option.Selected);
            (parent ?? filters).AddChild(option);
            return option;
        }

        static TableColumn Column(string key, int ratio, bool center = false, bool sortable = false)
        {
            var alignment = center ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            return new(UiText.Get(key), 1, ratio, sortable, alignment, alignment);
        }
    }

    private static string FormatDate(DateOnly? date) => date?.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? UiText.Get("Unknown");

    private void ShowStaffProfile(StaffProfile person, DateOnly date, Action back, IReadOnlyList<string>? responsibilities = null, bool market = false)
    {
        var body = Frame("CareerSection", person.Name, UiText.Get("StaffRole_" + person.Role));
        body.Name = "StaffProfilePage";
        var content = ClubUi.ScrollBody(body, "StaffProfileScroll");
        var overview = ClubUi.ResponsiveRow(content, "StaffProfileOverview");
        var identity = ClubUi.Card(overview, "StaffProfileIdentity");
        ClubUi.Field(identity, "StaffRole", UiText.Get("StaffRole_" + person.Role));
        ClubUi.Field(identity, "StaffDepartment", UiText.Get(person.Role == "AcademyCoach" ? "ClubDelegationsDepartment_Academy" : "ClubDelegationsFirstTeam"));
        ClubUi.Field(identity, "StaffTeam", person.Team ?? (person.Club is { } club ? _queries!.GetClubDetails(club).Club.Name : null));
        if (person.Nationality is { } nationality) ClubUi.Field(identity, "Nationality", UiText.Get("Nation_" + nationality));
        if (person.BirthDate is { } birth && birth <= date)
        {
            ClubUi.Field(identity, "CoachBirthDate", FormatDate(birth));
            ClubUi.Field(identity, "Age", date.Year - birth.Year - (birth.AddYears(date.Year - birth.Year) > date ? 1 : 0));
        }
        if (person.Reputation is { } reputation) ClubUi.Field(identity, "StaffReputation", reputation);
        if (person.Profession is { } professional)
        {
            ClubUi.Field(identity, "StaffSpecialty", UiText.Get("StaffRole_" + professional.Specialty));
            ClubUi.Field(identity, "StaffExperienceYears", professional.ExperienceYears);
        }
        var employment = ClubUi.Card(overview, "StaffContractStatus");
        ClubUi.Field(employment, "StaffContractStatus", UiText.Get("Staff" + person.ContractStatus(date)));
        if (person.Contract is { } contract)
        {
            ClubUi.Field(employment, "StaffStartDate", FormatDate(contract.StartDate));
            ClubUi.Field(employment, "StaffExpiry", FormatDate(contract.EndDate));
            ClubUi.Field(employment, "StaffGrossSalary", ManagerOfferDocument.Money(contract.GrossAnnualSalary, contract.Currency));
            ClubUi.Field(employment, "StaffDurationMonths", contract.CalculateDurationMonths());
            if (person.Employment is { Clauses.Count: 0 })
                employment.AddChild(ClubUi.Text("StaffNoAdditionalClauses", UiText.Get("StaffNoAdditionalClauses"), 13));
        }
        if (person.Profession is { } profile)
        {
            var panels = ClubUi.ResponsiveRow(content, "StaffProfessionalPanels");
            var attributes = ClubUi.Card(panels, "StaffProfessionalAttributes");
            foreach (var key in StaffProfessionalCatalog.For(profile.Specialty))
                AddStaffAttribute(attributes, key, profile.Attributes[key]);
            var highlights = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            panels.AddChild(highlights);
            AddHighlights(highlights, "StaffStrengths", profile.Strengths());
            AddHighlights(highlights, "StaffWeaknesses", profile.Weaknesses());
            var languages = ClubUi.Card(highlights, "StaffLanguages");
            foreach (var language in profile.Languages)
                ClubUi.Field(languages, "StaffLanguage_" + language.Code, UiText.Get("StaffLanguageLevel_" + language.Level));
            ClubUi.Card(content, "StaffProfessionalHistory").AddChild(
                ClubUi.Text("StaffHistoryEmpty", UiText.Get("StaffHistoryExperienceNotice"), 13));
        }
        else
            content.AddChild(ClubUi.Text("StaffLegacyProfile", UiText.Get("StaffLegacyProfile"), 14));
        if (responsibilities is { Count: > 0 })
        {
            var duties = ClubUi.Card(content, "StaffResponsibilities");
            foreach (var duty in responsibilities) duties.AddChild(ClubUi.Text("StaffResponsibility", duty));
        }
        var actions = new HFlowContainer();
        body.AddChild(actions);
        actions.AddChild(WindowUi.Button("StaffBackToDirectory", back));
        if (market)
        {
            var offer = WindowUi.Button("StaffMakeOffer", () => ShowStaffOfferForm(person.Id));
            offer.Disabled = person.ContractStatus(date) == StaffContractStatus.Unknown;
            if (offer.Disabled) offer.TooltipText = UiText.Get("StaffOfferUnavailable");
            actions.AddChild(offer);
        }
    }

    private static void AddHighlights(Control parent, string title, IReadOnlyList<KeyValuePair<string, int>> values)
    {
        var section = ClubUi.Card(parent, title);
        if (values.Count == 0)
            section.AddChild(ClubUi.Text(title + "Empty", UiText.Get("StaffNoHighlight"), 13));
        foreach (var pair in values) AddStaffAttribute(section, pair.Key, pair.Value);
    }

    private static void AddStaffAttribute(Control parent, string key, int value)
    {
        var row = new HBoxContainer { Name = "StaffAttributeRow", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.SetMeta("staff_attribute", key);
        var label = ClubUi.Text("AttributeName", UiText.Get("StaffAttribute_" + key), 13);
        label.SizeFlagsStretchRatio = 3;
        row.AddChild(label);
        row.AddChild(AnalysisAttributeRow.Meter(value, "StaffAttributeMeter"));
        parent.AddChild(row);
    }
}
