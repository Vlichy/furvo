using Furvo.Core.Domain;

namespace Furvo.Core.Queries;

/// <summary>Prioridad descriptiva de un borrador: no determina sanciones ni ponderaciones.</summary>
public enum BoardObjectivePriority { Critical, High, Medium, Low }
public enum BoardObjectiveHorizon { Season, MediumTerm, LongTerm }

/// <summary>Código editorial estable para una posible relación futura con decisiones, sin reglas implementadas.</summary>
public sealed record ClubPrincipleProposal(string Code, string Title, string Description);
public sealed record BoardObjectiveProposal(string Code, string Title, string Description,
    BoardObjectivePriority Priority, BoardObjectiveHorizon Horizon, int StartSeason, int DueSeason)
{
    public DateOnly Deadline => new(DueSeason + 1, 6, 30);
}

/// <summary>Vista de borrador. No es política vigente del club, estado de una partida ni dato persistido.</summary>
public sealed record ClubDirectionProposal(ClubId ClubId, string Version,
    IReadOnlyList<ClubPrincipleProposal> Principles, IReadOnlyList<BoardObjectiveProposal> Objectives);
