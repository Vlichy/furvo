using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private string RoutineCaption(SetPieceKind kind,string routine)=>
        kind is SetPieceKind.DirectFreeKick or SetPieceKind.IndirectFreeKick&&routine=="direct"&&_freeKickLocation==FreeKickLocation.Distant?UiText.Get("FreeKickLongShot"):kind==SetPieceKind.ThrowIn?UiText.Get("ThrowRoutine_"+routine):
        kind is SetPieceKind.DirectFreeKick or SetPieceKind.IndirectFreeKick&&routine=="short"?UiText.Get("FreeKickShort"):UiText.Get("Routine_"+routine);
    /// <summary>Asignaciones explícitas, sin evaluador automático. Se conservan las preferencias inactivas y legadas.</summary>
    private void RenderSetPieceEditor(VBoxContainer controls,Func<TacticalSettings> get,Func<TacticalSettings,bool> save,
        Action explain,CareerLineup? lineup,IReadOnlyList<LineupPlayer> players)
    {
        var kind=_setPieceKind;var starters=lineup?.Members.Where(m=>m.Role==LineupRole.Starter).Select(m=>m.PlayerId).ToHashSet()??new HashSet<PlayerId>();
        SetPiecePlan Plan()=>get().SetPieces[kind];
        bool UpdatePlan(SetPiecePlan plan,bool rebuild=false)
        {
            var plans=get().SetPieces.ToDictionary();plans[kind]=plan;
            if(!save(get() with{SetPieces=plans}))return false;
            if(rebuild)ShowTactics();else explain();return true;
        }
        void Choice(string name,string title,IReadOnlyList<string> values,Func<string?> current,Func<string?,bool> apply)
        {
            controls.AddChild(ClubUi.Text(name+"Label",UiText.Get(title),15));
            var box=new OptionButton{Name=name,CustomMinimumSize=new Vector2(0,34),SizeFlagsHorizontal=SizeFlags.ExpandFill};controls.AddChild(box);
            box.AddItem(UiText.Get("TacticUndefined"));foreach(var v in values)box.AddItem(name=="SetPieceRoutine"?RoutineCaption(kind,v):UiText.Get("Routine_"+v));
            var value=current();box.Selected=value is null?0:values.ToList().IndexOf(value)+1;
            box.ItemSelected+=i=>{if(!apply(i==0?null:values[(int)i-1])){var old=current();box.Selected=old is null?0:values.ToList().IndexOf(old)+1;}};
        }
        GridContainer? assignmentsGrid=null;
        void Person(string name,string title,Func<PlayerId?> current,Func<PlayerId?,bool> apply,bool shootout=false,IReadOnlySet<PlayerId>? excluded=null)
        {
            var target=(Container?)assignmentsGrid??controls;var cell=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};target.AddChild(cell);
            cell.AddChild(ClubUi.Text(name+"Label",title,14,"a6b6ca"));
            var old=current();
            var options=players.Where(p=>(shootout||starters.Contains(p.Id)||p.Id==old)&&(excluded is null||!excluded.Contains(p.Id)||p.Id==old)).OrderBy(p=>p.Name,StringComparer.Ordinal).ToArray();
            var box=new OptionButton{Name=name,CustomMinimumSize=new Vector2(0,34),SizeFlagsHorizontal=SizeFlags.ExpandFill,FitToLongestItem=false,ClipText=true};cell.AddChild(box);
            box.AddItem(UiText.Get("TacticUnassigned"));
            foreach(var p in options)box.AddItem(p.Name+(!shootout&&!starters.Contains(p.Id)?" · "+UiText.Get("OutsideEleven"):""));
            box.Selected=old is null?0:Array.FindIndex(options,p=>p.Id==old)+1;
            box.ItemSelected+=i=>{if(!apply(i==0?null:options[(int)i-1].Id)){var value=current();box.Selected=value is null?0:Array.FindIndex(options,p=>p.Id==value)+1;}};
        }
        if(kind==SetPieceKind.Penalty)
        {
            controls.AddChild(ClubUi.Text("ShootoutNotice",UiText.Get("ShootoutOnly"),14,"a6b6ca"));
            for(var i=0;i<5;i++)
            {
                var ordinal=i;
                var others=get().ShootoutOrder.Where((p,index)=>index!=ordinal&&p is not null).Select(p=>p!.Value).ToHashSet();
                Person("PenaltyTaker_"+(i+1),UiText.Get("PenaltyOrder_"+(i+1)),()=>get().ShootoutOrder[ordinal],
                    player=>{var order=get().ShootoutOrder.ToArray();order[ordinal]=player;if(!save(get() with{PenaltyTakers=Array.AsReadOnly(order)}))return false;ShowTactics();return true;},true,others);
            }
            return;
        }
        Choice("SetPieceRoutine","TacticRoutine",(kind is SetPieceKind.DirectFreeKick or SetPieceKind.IndirectFreeKick or SetPieceKind.DefendingFreeKick?TacticalCatalog.FreeKickRoutines(kind,_freeKickLocation):TacticalCatalog.Routines(kind)),()=>Plan().Routine,routine=>UpdatePlan(Plan() with{Routine=routine},true));
        if(kind is SetPieceKind.DirectFreeKick or SetPieceKind.IndirectFreeKick&&Plan().Routine is "cross" or "area" or "mixed")
        {
            Choice("SetPieceCrossTarget","CrossTarget",new[]{"near_post","far_post","centre","mixed"},()=>Plan().Configuration.CrossTarget,
                v=>UpdatePlan(Plan() with{Details=Plan().Configuration with{CrossTarget=v}}));
            Choice("SetPieceMovement","CrossMovement",new[]{"run","late","combined"},()=>Plan().Configuration.Movement,
                v=>UpdatePlan(Plan() with{Details=Plan().Configuration with{Movement=v}},true));
        }
        if(Plan().Routine=="rehearsed")
            Choice("SetPieceVariant","RehearsedVariant",new[]{"one_two"},()=>Plan().Configuration.Variant,v=>UpdatePlan(Plan() with{Details=Plan().Configuration with{Variant=v}}));
        if(kind==SetPieceKind.DefendingFreeKick)
        {
            controls.AddChild(ClubUi.Text("WallSizeLabel",UiText.Get("WallSize"),15));
            var wall=new OptionButton{Name="SetPieceWall",CustomMinimumSize=new Vector2(0,34)};controls.AddChild(wall);wall.AddItem(UiText.Get("TacticUndefined"));
            var sizes=new[]{0,1,2,3,4};foreach(var size in sizes)wall.AddItem(size==0?UiText.Get("NoWall"):UiText.Format("WallPlayers",size));
            wall.Selected=Plan().Configuration.WallSize is {} count?Array.IndexOf(sizes,count)+1:0;
            wall.ItemSelected+=i=>{if(!UpdatePlan(Plan() with{Details=Plan().Configuration with{WallSize=i==0?null:sizes[(int)i-1]}},true)){var old=Plan().Configuration.WallSize;wall.Selected=old is {} n?Array.IndexOf(sizes,n)+1:0;}};
            if(Plan().Configuration.WallSize is >0&&_freeKickLocation is FreeKickLocation.Edge or FreeKickLocation.Distant)
                Choice("SetPieceLying","LyingBehindWall",new[]{"yes","no"},()=>Plan().Configuration.Lying is {} lying?(lying?"yes":"no"):null,
                    v=>UpdatePlan(Plan() with{Details=Plan().Configuration with{Lying=v is null?null:v=="yes"}},true));
        }
        var roles=TacticalCatalog.ActiveRoles(kind,Plan());
        assignmentsGrid=new GridContainer{Name="SetPieceAssignments",Columns=2,SizeFlagsHorizontal=SizeFlags.ExpandFill};assignmentsGrid.AddThemeConstantOverride("h_separation",12);assignmentsGrid.AddThemeConstantOverride("v_separation",9);controls.AddChild(assignmentsGrid);
        var assigned=roles.Select(r=>Plan().Configuration.Assignments[r]).Where(p=>p is not null).Select(p=>p!.Value).ToHashSet();
        if(TacticalCatalog.HasTaker(kind))
            Person("SetPieceTaker",UiText.Get("TacticTaker"),()=>Plan().Taker,v=>UpdatePlan(Plan() with{Taker=v},true),excluded:assigned);
        foreach(var role in roles)
        {
            var r=role;var excluded=roles.Where(x=>x!=r).Select(x=>Plan().Configuration.Assignments[x]).Where(p=>p is not null).Select(p=>p!.Value).ToHashSet();
            if(Plan().Taker is {} taker)excluded.Add(taker);
            Person("SetPieceRole_"+role,UiText.Get("SetRole_"+role),()=>Plan().Configuration.Assignments[r],
                v=>{var details=Plan().Configuration;var assignments=details.Assignments.ToDictionary();assignments[r]=v;return UpdatePlan(Plan() with{Details=details with{Assignments=assignments}},true);},excluded:excluded);
        }
        var pending=roles.Count(r=>Plan().Configuration.Assignments[r] is not {} id||!starters.Contains(id));
        if(TacticalCatalog.HasTaker(kind)&&(Plan().Taker is not {} launcher||!starters.Contains(launcher)))pending++;
        controls.AddChild(ClubUi.Text("SetPiecePending",UiText.Format("SetPiecePending",pending),13,"f1b96b"));
        controls.AddChild(ClubUi.Text("SetPieceReferenceNotice",UiText.Get("SetPieceReferences"),12,"a6b6ca"));
    }
}
