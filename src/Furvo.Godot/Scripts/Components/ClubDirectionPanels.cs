using System.Globalization;
using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Dos paneles de consulta editorial: no hay botones, evaluación, confianza ni consecuencias.</summary>
public partial class ClubDirectionPanels : BoxContainer
{
    public ClubDirectionPanels(string clubId)
    {
        Name = "ClubDirectionPanels"; SetMeta("club_id", clubId); SetMeta("proposal_status", "draft");
        SizeFlagsHorizontal = SizeFlags.ExpandFill; AddThemeConstantOverride("separation", 10);
        Resized += () => { var vertical = Size.X < 1000; if (Vertical != vertical) Vertical = vertical; };
        var philosophy = ClubUi.Card(this, "ClubPhilosophy");
        var objectives = ClubUi.Card(this, "ClubBoardExpectations");
        foreach (var panel in new[] { philosophy, objectives })
            panel.AddChild(ClubUi.Text("DirectionDraftNotice", UiText.Get("DirectionDraftNotice"), 12, "f1b96b"));
        ClubDirectionProposal? proposal;
        try { proposal = ClubDirectionProposalCatalog.Find(clubId); }
        catch (Exception error) when (error is InvalidDataException or System.Text.Json.JsonException or ArgumentException)
        {
            // Un borrador defectuoso no debe impedir consultar el club ni los futbolistas.
            GD.PushError(error.ToString());
            foreach (var panel in new[] { philosophy, objectives }) panel.AddChild(ClubUi.Text("DirectionUnavailable", UiText.Get("DirectionUnavailable"), 13, "a6b6ca"));
            return;
        }
        if (proposal is null)
        {
            foreach (var panel in new[] { philosophy, objectives }) panel.AddChild(ClubUi.Text("DirectionPending", UiText.Get("PendingDefinition"), 13));
            return;
        }
        foreach (var principle in proposal.Principles)
        {
            var row = new VBoxContainer { Name = "Principle_" + principle.Code }; row.AddThemeConstantOverride("separation", 2);
            row.SetMeta("principle_code", principle.Code); philosophy.AddChild(row);
            row.AddChild(ClubUi.Text("PrincipleTitle", principle.Title, 14));
            row.AddChild(ClubUi.Text("PrincipleDescription", principle.Description, 12, "a6b6ca"));
        }
        foreach (var objective in proposal.Objectives.OrderBy(o => o.Horizon).ThenBy(o => o.Code, StringComparer.Ordinal))
        {
            var row = new VBoxContainer { Name = "Objective_" + objective.Code }; row.AddThemeConstantOverride("separation", 2);
            row.SetMeta("objective_code", objective.Code); row.SetMeta("objective_priority", objective.Priority.ToString());
            row.SetMeta("objective_horizon", objective.Horizon.ToString()); objectives.AddChild(row);
            row.AddChild(ClubUi.Text("ObjectiveTitle", UiText.Format("DirectionObjectiveTitle", UiText.Get("DirectionHorizon_" + objective.Horizon), objective.Title), 14));
            row.AddChild(ClubUi.Text("ObjectiveDescription", objective.Description, 12, "a6b6ca"));
            row.AddChild(ClubUi.Text("ObjectiveDeadline", UiText.Format("DirectionObjectiveDeadline", UiText.Get("DirectionPriority_" + objective.Priority),
                objective.DueSeason.ToString(CultureInfo.InvariantCulture), ((objective.DueSeason + 1) % 100).ToString("D2", CultureInfo.InvariantCulture),
                objective.Deadline.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)), 12, "a6b6ca"));
        }
    }
}
