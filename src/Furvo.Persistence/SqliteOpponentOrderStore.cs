using Furvo.Core.Domain;
using Furvo.Core.Tactics;
using Microsoft.Data.Sqlite;
namespace Furvo.Persistence;
/// <summary>Repositorio preparado: exige un contexto de encuentro verificado que el juego todavía no proporciona.</summary>
public sealed class SqliteOpponentOrderStore(string databasePath,IMatchPreparationContext context)
{
    private readonly string _path=WorldDatabase.PathFor(databasePath);
    public void Save(OpponentOrder order)
    {
        using var c=WorldDatabase.Open(_path,SqliteOpenMode.ReadWrite);using var t=c.BeginTransaction();var owner=SqliteLineupStore.Owner(c,t);var club=owner.Club??throw new InvalidOperationException("Sin equipo dirigido.");
        if(order.Opponent==club)throw new InvalidDataException("El rival no puede ser el propio club.");order.Validate(order.MatchId,order.Opponent,context.OpponentSquad(order.MatchId,club,order.Opponent));SqliteTacticsStore.EnsureSchema(c,t);
        using var q=WorldDatabase.Command(c,t,"INSERT INTO career_opponent_orders VALUES($team,$match,$player,$json) ON CONFLICT(team_id,match_id,player_id) DO UPDATE SET order_json=excluded.order_json;",("$team",club.ToString()),("$match",order.MatchId.ToString("D")),("$player",order.Player.ToString()),("$json",CareerJson.Write(order)));q.ExecuteNonQuery();t.Commit();
    }
    public IReadOnlyList<OpponentOrder> Read(Guid match,ClubId opponent)
    {
        using var c=WorldDatabase.Open(_path);using var t=c.BeginTransaction(deferred:true);var club=SqliteLineupStore.Owner(c,t).Club??throw new InvalidOperationException("Sin equipo dirigido.");var squad=context.OpponentSquad(match,club,opponent);
        using var exists=WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE name='career_opponent_orders';");if(Convert.ToInt32(exists.ExecuteScalar())==0)return Array.Empty<OpponentOrder>();
        using var q=WorldDatabase.Command(c,t,"SELECT order_json FROM career_opponent_orders WHERE team_id=$team AND match_id=$match ORDER BY player_id;",("$team",club.ToString()),("$match",match.ToString("D")));using var r=q.ExecuteReader();var result=new List<OpponentOrder>();while(r.Read()){var order=CareerJson.Read<OpponentOrder>(r.GetString(0));order.Validate(match,opponent,squad);result.Add(order);}return result.AsReadOnly();
    }
}
