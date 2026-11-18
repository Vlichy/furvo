using Furvo.Core.Academy;
using Furvo.Core.Careers;
using Furvo.Core.Development;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;

namespace Furvo.Presentation;

public partial class Main
{
    private enum DelegationDepartment { Training, Academy }
    private enum AcademyDelegationGroup { Sport, Recruitment, Development, Coordination }

    private void ShowClubDelegations(Guid? selectedTeam = null, DelegationDepartment department = DelegationDepartment.Training,
        AcademyDelegationGroup academyGroup = AcademyDelegationGroup.Sport)
    {
        var club = _workspace!.Career.ClubId!.Value;
        var body = Frame("CareerSection", UiText.Get("ClubDelegationsTitle"), _queries!.GetClubDetails(club).Club.Name);
        body.Name = "ClubDelegationsPage";
        var edits = new List<Func<bool>>();
        void ChangeView(Action change)
        {
            if (edits.Any(isDirty => isDirty())) TrainingConfirm(UiText.Get("TrainingDiscardChanges"), change);
            else change();
        }
        PersonnelNavigation(body, "StaffDelegations", ChangeView);
        var departments = new HFlowContainer { Name = "ClubDelegationDepartments" };
        body.AddChild(departments);
        foreach (var section in Enum.GetValues<DelegationDepartment>())
        {
            var button = WindowUi.Button("ClubDelegationsDepartment_" + section,
                () => ChangeView(() => ShowClubDelegations(selectedTeam, section)));
            button.Disabled = section == department;
            departments.AddChild(button);
        }
        var content = ClubUi.ScrollBody(body, "ClubDelegationsScroll");
        if (department == DelegationDepartment.Academy)
        {
            var groups = new HFlowContainer { Name = "AcademyDelegationGroups" };
            body.AddChild(groups);
            body.MoveChild(groups, content.GetParent().GetIndex());
            foreach (var group in Enum.GetValues<AcademyDelegationGroup>())
            {
                var button = WindowUi.Button("AcademyDelegationGroup_" + group,
                    () => ChangeView(() => ShowClubDelegations(selectedTeam, department, group)));
                button.Disabled = group == academyGroup;
                groups.AddChild(button);
            }
            if (academyGroup != AcademyDelegationGroup.Sport)
            {
                content.AddChild(ClubUi.Text("AcademyDelegationGroupStatus",
                    UiText.Get("AcademyDelegationPending_" + academyGroup), 15));
                return;
            }
        }
        var sport = Sports();
        var teams = SportsTeams().Where(t => department == DelegationDepartment.Training || t.Category != "First").ToArray();
        if (teams.Length == 0)
        {
            content.AddChild(ClubUi.Text("ClubDelegationsNoAcademy", UiText.Get("ClubDelegationsNoAcademy"), 14));
            return;
        }
        var team = teams.FirstOrDefault(t => t.Id == selectedTeam) ?? teams.First();
        var picker = new OptionButton { Name = "ClubDelegationsTeam" };
        foreach (var candidate in teams) picker.AddItem(candidate.Name);
        picker.Selected = teams.ToList().FindIndex(t => t.Id == team.Id);
        content.AddChild(picker);
        picker.ItemSelected += index =>
        {
            picker.Selected = Array.FindIndex(teams, candidate => candidate.Id == team.Id);
            ChangeView(() => ShowClubDelegations(teams[(int)index].Id, department));
        };
        var status = ClubUi.Text("ClubDelegationsStatus", UiText.Get("ClubDelegationsNotice"), 14);
        content.AddChild(status);
        content.AddChild(ClubUi.Text("ClubDelegationsDepartment", UiText.Get(
            team.Category == "First" ? "ClubDelegationsFirstTeam" : "NavPage_Academy"), 18, "f1b96b"));
        var grid = new GridContainer { Name = "ClubDelegationsGrid", Columns = 4, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 6);
        content.AddChild(grid);
        foreach (var key in new[] { "ClubDelegationTaskHeader", "TrainingResponsibleLabel", "TrainingDelegationModeLabel", "ClubDelegationActionHeader" })
        {
            var heading = ClubUi.Text(key, UiText.Get(key), 14, "f1b96b");
            heading.SizeFlagsHorizontal = SizeFlags.Fill;
            grid.AddChild(heading);
        }

        var academy = Academy();
        var state = department == DelegationDepartment.Academy ? academy.Get(team.Id) : null;
        if (state is not null)
        {
            var coach = state.Coach;
            var eligible = coach.LinkState == "Institutional";
            AddRow(UiText.Get("AcademyDelegationLineups"), eligible
                    ? [(coach.Id, coach.Name + " — " + UiText.Get("AnalysisStaff_AcademyCoach"))] : [],
                eligible && state.Management == AcademyManagement.Delegated
                    ? new(coach.Id, ManagementAutonomy.Full) : ResponsibilityAssignment.Personal,
                false, true, changed =>
                {
                    var latest = academy.Get(team.Id);
                    if (latest.Revision != state.Revision || changed.EmployeeId is { } id &&
                        (latest.Coach.Id != id || latest.Coach.LinkState != "Institutional"))
                        throw new InvalidOperationException(UiText.Get("TrainingDelegationConflict"));
                    state = academy.SetManagement(team.Id, changed.EmployeeId is null ? AcademyManagement.Direct : AcademyManagement.Delegated, latest.Revision);
                });
        }

        {
            var plan = sport.Plan(team.Id);
            var staff = sport.Staff(team.Id).Where(p => p.Id != _workspace.Career.Coach.Id.Value).ToArray();
            foreach (var area in Enum.GetValues<TrainingResponsibility>())
            {
                var stored = plan.Delegations.SingleOrDefault(d => d.Area == area);
                var eligible = staff.Where(p => p.Responsibilities.Contains(area)).ToArray();
                var responsibility = new ClubResponsibility(area.ToString(), team.Category);
                bool IsEligible(Furvo.Core.Domain.ClubId owner, string code, Guid employee) =>
                    owner == club && code == responsibility.Code && sport.Staff(team.Id).Any(p =>
                        p.Id == employee && p.Id != _workspace.Career.Coach.Id.Value && p.Responsibilities.Contains(area));
                var assignment = new ResponsibilityAssignment(stored?.Responsible, stored?.Mode switch
                {
                    TrainingDelegationMode.Delegated => ManagementAutonomy.Full,
                    TrainingDelegationMode.Mixed => ManagementAutonomy.Supervised,
                    _ => ManagementAutonomy.Personal
                });
                var effective = assignment.EffectiveFor(club, responsibility, IsEligible);
                var label = UiText.Get("TrainingResponsibility_" + area);
                if (effective != assignment) label += " · " + UiText.Get("TrainingStaffIneligible");
                AddRow(label, eligible.Select(p => (p.Id, p.Name + " — " + UiText.Get("AnalysisStaff_" + p.Role))).ToArray(),
                    effective, true, false, changed =>
                    {
                        changed.Validate(club, responsibility, IsEligible);
                        var latest = sport.Plan(team.Id);
                        if (latest.Delegations.SingleOrDefault(d => d.Area == area) != stored)
                            throw new InvalidOperationException(UiText.Get("TrainingDelegationConflict"));
                        TrainingDelegation? next = changed.EmployeeId is { } id
                            ? new(area, id, changed.Autonomy switch
                            {
                                ManagementAutonomy.Full => TrainingDelegationMode.Delegated,
                                ManagementAutonomy.Supervised => TrainingDelegationMode.Mixed,
                                _ => TrainingDelegationMode.PersonalSupport
                            }) : null;
                        sport.SaveDelegation(team.Id, latest.Revision, area, next);
                        stored = next;
                    });
            }
        }

        if (state is not null)
            content.AddChild(ClubUi.Text("AcademyDelegationCompetitionPending", UiText.Get("AcademyDelegationCompetitionPending"), 14));
        content.AddChild(ClubUi.Text("ClubDelegationsPending", UiText.Get("ClubDelegationsPending"), 14));

        void AddRow(string label, (Guid Id, string Name)[] candidates, ResponsibilityAssignment current,
            bool supervised, bool fullOnly, Action<ResponsibilityAssignment> save)
        {
            var task = ClubUi.Text("ClubDelegationTask", label, 14);
            task.SizeFlagsHorizontal = SizeFlags.Fill;
            task.CustomMinimumSize = new Vector2(220, 0);
            grid.AddChild(task);
            var owner = new OptionButton
            {
                FitToLongestItem = false, ClipText = true, CustomMinimumSize = new Vector2(240, 34),
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            owner.AddItem(UiText.Get("ClubDelegationManager"));
            foreach (var person in candidates) owner.AddItem(person.Name);
            owner.Selected = Array.FindIndex(candidates, p => p.Id == current.EmployeeId) + 1;
            grid.AddChild(owner);
            var mode = new OptionButton
            {
                CustomMinimumSize = new Vector2(148, 34), SizeFlagsHorizontal = SizeFlags.Fill
            };
            if (fullOnly)
            {
                mode.AddItem(UiText.Get("AcademyManagement_Direct"));
                mode.AddItem(UiText.Get("AcademyManagement_Delegated"));
                mode.Selected = current.EmployeeId is null ? 0 : 1;
            }
            else
            {
                foreach (var key in new[] { "Personal", "Supervised", "Full" }) mode.AddItem(UiText.Get("ClubDelegation" + key));
                mode.Selected = (int)current.Autonomy;
            }
            grid.AddChild(mode);
            TrainingUi.LimitPopup(owner);
            Button? apply = null;
            ResponsibilityAssignment Pending() => new(owner.Selected == 0 ? null : candidates[owner.Selected - 1].Id,
                fullOnly && mode.Selected == 1 ? ManagementAutonomy.Full : (ManagementAutonomy)mode.Selected);
            void Refresh()
            {
                owner.TooltipText = owner.GetItemText(owner.Selected);
                bool manager = owner.Selected == 0;
                if (!fullOnly)
                {
                    mode.SetItemDisabled(1, manager || !supervised);
                    mode.SetItemDisabled(2, manager);
                }
                if (manager) mode.Selected = 0;
                else if (fullOnly) mode.Selected = 1;
                mode.Disabled = manager || fullOnly;
                if (apply is not null) apply.Disabled = Pending() == current;
            }
            owner.ItemSelected += _ => Refresh();
            mode.ItemSelected += _ => Refresh();
            apply = WindowUi.Button("ClubDelegationSave", () =>
            {
                try { save(Pending()); }
                catch (Exception error) { status.Text = error.Message; return; }
                current = Pending();
                Refresh();
                status.Text = UiText.Get("TrainingDelegationSaved");
            });
            grid.AddChild(apply);
            edits.Add(() => Pending() != current);
            Refresh();
        }
    }
}
