using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
namespace Furvo.Presentation.Components;
public enum DiagramSymbol { Player,Opponent,Target }
public sealed record DiagramPoint(string Id,double Advance,double Width,string Caption,PlayerId? Player=null,
    DiagramSymbol Symbol=DiagramSymbol.Player,bool Pending=false,bool Marked=false,bool Lying=false);
public sealed record DiagramArrow(string From,string To,bool Movement=false,bool? Short=null,bool Highlight=false);
public sealed record DiagramZone(double Advance,double Width,double Length,double Breadth);
public sealed record TacticDiagram(IReadOnlyList<DiagramPoint> Points,IReadOnlyList<DiagramArrow> Arrows,IReadOnlyList<DiagramZone> Zones)
{
    public void Validate()
    {
        if(Points.Select(p=>p.Id).Distinct().Count()!=Points.Count||Points.Any(p=>p.Advance is <0 or >1||p.Width is <0 or >1)||
            Arrows.Any(a=>a.From==a.To||!Points.Any(p=>p.Id==a.From)||!Points.Any(p=>p.Id==a.To)))
            throw new InvalidDataException("Diagrama táctico sin referencias válidas.");
    }
}
/// <summary>Escenas declarativas. Toda trayectoria referencia dos símbolos; nunca coordenadas sueltas en el pintor.</summary>
public static class TacticDiagramBuilder
{
    public static TacticDiagram Build(CareerLineup? lineup,FormationDefinition formation,TacticalSettings settings,int tab,string focus,SetPieceKind kind,bool exactDisposition=false,FreeKickLocation location=FreeKickLocation.Edge)
    {
        var points=new List<DiagramPoint>();var arrows=new List<DiagramArrow>();var zones=new List<DiagramZone>();
        string? Value(string code)=>settings.Instructions.GetValueOrDefault(code);
        void Add(DiagramPoint p){if(!points.Any(x=>x.Id==p.Id))points.Add(p);}
        void Link(string a,string b,bool movement=false,bool? shortPass=null,bool highlight=false){if(a!=b)arrows.Add(new(a,b,movement,shortPass,highlight));}
        string Target(string id,double x,double y,string caption="DiagramTarget"){Add(new(id,x,y,caption,null,DiagramSymbol.Target));return id;}
        if(tab!=3)
        {
            foreach(var slot in formation.Slots)
            {
                var x=slot.X;var y=slot.Y;var defensive=x<.36;
                if(tab==1&&!exactDisposition)
                {
                    var width=Value(defensive?"defend_width":"attack_width");y=.5+(y-.5)*(width=="wide"?1.15:width=="narrow"?.6:1);
                    if(defensive&&slot.Role!=PlayerRole.Goalkeeper)x+=Value("defensive_line")=="high"?.12:Value("defensive_line")=="low"?-.08:0;
                }
                var member=lineup?.Members.SingleOrDefault(m=>m.SlotId==slot.Id);
                Add(new(slot.Id,Math.Clamp(x,0,1),Math.Clamp(y,0,1),slot.TacticalCode is null?"PositionCode_"+slot.Role:"TacticalRole_"+slot.TacticalCode,member?.PlayerId,Pending:member is null));
            }
            DiagramPoint Nearest(double x,double y,string? except=null)=>points.Where(p=>p.Symbol==DiagramSymbol.Player&&p.Id!=except).OrderBy(p=>Math.Pow(p.Advance-x,2)+Math.Pow(p.Width-y,2)).ThenBy(p=>p.Id,StringComparer.Ordinal).First();
            void Run(DiagramPoint p,double x,double y,string id){var t=Target(id,Math.Clamp(x,0,1),Math.Clamp(y,0,1));Link(p.Id,t,true);}
            if(tab==1)
            {
                if(focus=="press_direction"&&Value(focus) is {} direction&&direction!="none")
                {
                    foreach(var p in points.Where(p=>p.Symbol==DiagramSymbol.Player&&p.Advance>.3).OrderByDescending(p=>p.Advance).Take(3).ToArray())
                    {var lane=direction=="left"?.12:direction=="right"?.88:direction=="centre"?.5:p.Width<.5?.12:.88;Run(p,Math.Min(.9,p.Advance+.1),lane,"press_direction_"+p.Id);}
                    if(direction=="flanks"){zones.Add(new(.5,.02,.4,.18));zones.Add(new(.5,.8,.4,.18));}else zones.Add(new(.5,direction=="left"?.02:direction=="right"?.8:.4,.4,.18));
                }
                if(focus=="opponent_keeper"&&Value(focus) is {} keeper&&keeper!="allow")
                {var rival=Target("opponent_keeper_target",.96,.5,"DiagramRival1");foreach(var p in points.Where(p=>p.Symbol==DiagramSymbol.Player&&p.Advance>.3).OrderByDescending(p=>p.Advance).ThenBy(p=>p.Id,StringComparer.Ordinal).Take(keeper=="press"?3:1).ToArray())Link(p.Id,rival,true);}
                if(focus=="offside_trap"&&Value(focus)=="on")
                {var defenders=points.Where(p=>p.Symbol==DiagramSymbol.Player&&formation.Slots.Single(s=>s.Id==p.Id).Role!=PlayerRole.Goalkeeper).OrderBy(p=>p.Advance).Take(4).ToArray();var trapLine=Math.Min(.8,defenders.Max(p=>p.Advance)+.08);zones.Add(new(trapLine,.03,.025,.94));foreach(var p in defenders)Run(p,trapLine,p.Width,"offside_"+p.Id);}
                if(focus=="press_beaten"&&Value(focus) is {} beaten)
                {foreach(var p in points.Where(p=>p.Symbol==DiagramSymbol.Player&&p.Advance>.25).OrderByDescending(p=>p.Advance).Take(4).ToArray()){if(beaten=="structure")zones.Add(new(Math.Max(0,p.Advance-.02),Math.Clamp(p.Width-.04,0,.92),.04,.08));else Run(p,p.Advance+(beaten=="repress"?.08:-.15),p.Width,"beaten_"+p.Id);}}
                if(focus=="passing"&&Value(focus) is {} pass){var a=Nearest(.25,.4);var b=Nearest(pass=="long"?.84:.45,.6,a.Id);Link(a.Id,b.Id,shortPass:pass!="long");if(pass=="mixed"){var c=Nearest(.84,.5,a.Id);if(c.Id!=b.Id)Link(a.Id,c.Id,shortPass:false);}}
                if(focus=="movement"&&Value(focus) is {} movement){var candidates=points.Where(p=>p.Symbol==DiagramSymbol.Player&&p.Advance>.35).OrderByDescending(p=>p.Advance).ThenBy(p=>p.Id,StringComparer.Ordinal).ToArray();var a=candidates.FirstOrDefault();var b=a is null?null:candidates.Skip(1).OrderBy(p=>Math.Abs(p.Advance-a.Advance)*2+Math.Abs(p.Width-a.Width)).ThenBy(p=>p.Id,StringComparer.Ordinal).FirstOrDefault();
                    if(movement is "exchange" or "mixed"&&a is not null&&b is not null){Link(a.Id,b.Id,true);Link(b.Id,a.Id,true);}
                    if(movement is "hold" or "mixed")foreach(var p in points.Where(p=>p.Symbol==DiagramSymbol.Player&&(movement=="hold"||p.Id!=a?.Id&&p.Id!=b?.Id)).ToArray())zones.Add(new(Math.Max(0,p.Advance-.025),Math.Max(0,p.Width-.035),.05,.07));}
                if(focus is "attack_width" or "defend_width"&&Value(focus) is {} width){zones.Add(new(.15,width=="narrow"?.3:width=="wide"?.05:.2,.7,width=="narrow"?.4:width=="wide"?.9:.6));}
                if(focus=="defensive_line"&&Value(focus) is {} line)zones.Add(new(line=="high"?.43:line=="low"?.1:.27,.05,.04,.9));
                if(focus=="organization"&&Value(focus) is {} organization){foreach(var p in points.Where(p=>p.Symbol==DiagramSymbol.Player&&p.Advance<.5&&p.Advance>.1).Take(3).ToArray()){if(organization=="follow")Run(p,p.Advance+.08,p.Width,"follow_"+p.Id);else zones.Add(new(p.Advance-.02,Math.Clamp(p.Width-.04,0,.92),.04,.08));}}
                if(focus=="freedom"&&Value(focus) is {} freedom){foreach(var p in points.Where(p=>p.Symbol==DiagramSymbol.Player&&p.Advance>.5).Take(3).ToArray()){if(freedom=="disciplined")zones.Add(new(p.Advance-.03,Math.Clamp(p.Width-.04,0,.92),.06,.08));else Run(p,p.Advance+(freedom=="free"?.12:.06),p.Width+(p.Width<.5?1:-1)*(freedom=="free"?.12:.06),"freedom_"+p.Id);}}
                if(focus=="progression"&&Value(focus) is {} progress)
                {if(progress!="centre"){zones.Add(new(.4,.02,.5,.2));zones.Add(new(.4,.78,.5,.2));}if(progress!="flanks")zones.Add(new(.4,.37,.5,.26));}
                if(focus=="pressing"&&Value(focus) is {} press)zones.Add(new(press=="high"?.7:press=="low"?.15:.4,.05,.2,.9));
                if(focus=="marking"&&Value(focus) is {} mark)
                {
                    var defenders=points.Where(p=>p.Symbol==DiagramSymbol.Player&&formation.Slots.Single(s=>s.Id==p.Id).Role!=PlayerRole.Goalkeeper).OrderBy(p=>p.Advance).ThenBy(p=>p.Id,StringComparer.Ordinal).Take(4).ToArray();
                    for(var i=0;i<defenders.Length;i++){var p=defenders[i];if(mark=="zonal"||mark=="mixed"&&i<2)zones.Add(new(Math.Max(0,p.Advance-.06),Math.Clamp(p.Width-.09,0,.82),.12,.18));else{var id="mark_rival_"+i;Add(new(id,Math.Min(.97,p.Advance+.07),Math.Clamp(p.Width+.045,0,1),"DiagramRival"+(i+1),null,DiagramSymbol.Opponent));Link(p.Id,id);}}

                }
            }
            else
            {
                var value=Value(focus);
                if(focus is "recovery" or "counter_route"&&Value("recovery")=="counter"){
                    var route=Value("counter_route");if(route is not null){var origin=Nearest(.38,.5);var receivers=points.Where(p=>p.Symbol==DiagramSymbol.Player&&p.Advance>origin.Advance+.07).ToArray();
                    var lanes=route=="both"?new[]{.12,.88}:new[]{route=="left"?.12:route=="right"?.88:.5};foreach(var lane in lanes){var receiver=receivers.OrderBy(p=>route=="long"?-p.Advance:Math.Abs(p.Width-lane)+Math.Abs(p.Advance-.75)).ThenBy(p=>p.Id,StringComparer.Ordinal).FirstOrDefault();if(receiver is not null)Link(origin.Id,receiver.Id,shortPass:route=="long"?false:null);else Link(origin.Id,Target("counter_"+lane,.85,lane),shortPass:route=="long"?false:null);}}}
                else if(focus=="recovery"&&value=="keep"){var a=Nearest(.5,.5);foreach(var b in points.Where(p=>p.Symbol==DiagramSymbol.Player&&p.Id!=a.Id).OrderBy(p=>Math.Pow(p.Advance-a.Advance,2)+Math.Pow(p.Width-a.Width,2)).ThenBy(p=>p.Id,StringComparer.Ordinal).Take(3).ToArray())Link(a.Id,b.Id,shortPass:true);}
                else if(focus=="loss"&&value=="counterpress"){var target=Target("loss",.62,.5,"DiagramLoss");foreach(var p in points.Where(p=>p.Symbol==DiagramSymbol.Player).OrderBy(p=>Math.Abs(p.Advance-.62)+Math.Abs(p.Width-.5)).Take(3).ToArray())Link(p.Id,target,true);}
                else if(focus=="loss"&&value=="regroup")foreach(var p in points.Where(p=>p.Advance>.5).Take(3).ToArray())Run(p,p.Advance-.18,p.Width,"regroup_"+p.Id);
                else if(focus is "recovery" or "loss"&&value=="balance")zones.Add(new(.35,.2,.3,.6));
                else if(focus is "keeper" or "keeper_target"&&(Value("keeper") is not null||Value("keeper_target") is not null))
                {
                    var gk=points.Single(p=>formation.Slots.Single(s=>s.Id==p.Id).Role==PlayerRole.Goalkeeper);
                    var target=Value("keeper_target");var distribution=Value("keeper");
                    var candidates=points.Where(p=>p.Symbol==DiagramSymbol.Player&&p.Id!=gk.Id&&p.Player is not null).Where(p=>target switch{"defenders"=>p.Advance<.45,"forwards"=>p.Advance>.6,"flanks"=>p.Width<.3||p.Width>.7,_=>true}).ToArray();
                    foreach(var shortPass in new[]{true,false}){if(distribution=="short"&&!shortPass||distribution=="long"&&shortPass)continue;foreach(var p in candidates.Where(p=>shortPass?p.Advance-gk.Advance<=.4:p.Advance-gk.Advance>.4).OrderBy(p=>shortPass?p.Advance:-p.Advance).ThenBy(p=>p.Id,StringComparer.Ordinal).Take(2))Link(gk.Id,p.Id,shortPass:shortPass,highlight:target is not null);}
                }
            }
        }
        else
        {
            var plan=settings.SetPieces[kind];var config=plan.Configuration;
            var starters=lineup?.Members.Where(m=>m.Role==LineupRole.Starter).Select(m=>m.PlayerId).ToHashSet()??new HashSet<PlayerId>();
            bool Pending(PlayerId? id)=>id is null||!starters.Contains(id.Value);
            void Role(SetPieceRole role,double x,double y,bool marked=false,bool lying=false)
            {var player=config.Assignments[role];Add(new(role.ToString(),x,y,"SetRole_"+role,player,Pending:Pending(player),Marked:marked&&!Pending(player),Lying:lying));}
            var specialist=kind switch{SetPieceKind.Penalty=>SpecialistKind.Penalty,SetPieceKind.DirectFreeKick=>SpecialistKind.DirectFreeKick,SetPieceKind.IndirectFreeKick=>SpecialistKind.IndirectFreeKick,_=>SpecialistKind.CornerLeft};
            var launcher=(settings.Personnel?.Order(specialist).Count??0)>0?SpecialistSelection.Resolve(settings,specialist,starters):plan.Taker;
            void Launcher(double x,double y)=>Add(new("taker",x,y,"TacticTaker",launcher,Pending:Pending(launcher)));
            var active=TacticalCatalog.ActiveRoles(kind,plan);
            if(kind==SetPieceKind.Penalty)
            {
                for(var i=0;i<5;i++){var id=settings.ShootoutOrder[i];Add(new("penalty"+i,.8-i*.14,.5,"PenaltyOrder_"+(i+1),id,Pending:id is null));}
            }
            else if(kind is SetPieceKind.DefendingCorner or SetPieceKind.DefendingFreeKick)
            {
                var isFree=kind==SetPieceKind.DefendingFreeKick;
                if(plan.Routine is "zonal" or "mixed")zones.AddRange(new[]{new DiagramZone(.04,.14,.2,.24),new(.04,plan.Routine=="mixed"?.62:.38,.2,.24)}.Concat(plan.Routine=="zonal"?new[]{new DiagramZone(.04,.62,.2,.24)}:Array.Empty<DiagramZone>()));
                for(var i=0;i<3;i++)
                {
                    var zone=(SetPieceRole)((int)SetPieceRole.Zone1+i);if(active.Contains(zone)){var mixed=plan.Routine=="mixed";Role(zone,!mixed&&i==1?.24:.12,mixed?(i==0?.2:.8):.2+i*.3);}
                    var marker=(SetPieceRole)((int)SetPieceRole.Marker1+i);
                    if(active.Contains(marker)){Role(marker,.25,.25+i*.25,true);Add(new("rival"+i,.32,.28+i*.25,"DiagramRival"+(i+1),null,DiagramSymbol.Opponent));}
                }
                if(isFree&&config.WallSize is >0)
                {
                    for(var i=0;i<config.WallSize;i++)Role((SetPieceRole)((int)SetPieceRole.Wall1+i),.12,.39+i*.075);
                    if(config.Lying==true)Role(SetPieceRole.Prone,.075,.5,lying:true);
                }
                Add(new("rival_taker",isFree?.4:.01,isFree?.5:.01,"DiagramRivalTaker",null,DiagramSymbol.Opponent));
            }
            else if(plan.Routine is not null)
            {
                if(active.Contains(SetPieceRole.StayBack))Role(SetPieceRole.StayBack,.35,.65);
                var isThrow=kind==SetPieceKind.ThrowIn;var corner=kind==SetPieceKind.AttackingCorner;
                Launcher(isThrow?.68:corner?.99:.7,isThrow||corner?.015:.4);
                var routine=plan.Routine;
                if(isThrow)
                {
                    if(routine=="flick"){Role(SetPieceRole.Header,.75,.27);Role(SetPieceRole.Receiver,.86,.52);Link("taker","Header");Link("Header","Receiver");}
                    else if(routine=="space"){Role(SetPieceRole.Receiver,.61,.25);var t=Target("space",.84,.3);Link("Receiver",t,true);Link("taker",t);}
                    else if(routine=="back"){Role(SetPieceRole.Receiver,.47,.2);Link("taker","Receiver");}
                    else if(routine=="rehearsed")
                    {Role(SetPieceRole.Receiver,.6,.18);Role(SetPieceRole.Finisher,.63,.43);var near=Target("near",.66,.12);var far=Target("far",.85,.36);Link("Receiver",near,true);Link("Finisher",far,true);Link("taker","Receiver");}
                    else if(routine=="short"){Role(SetPieceRole.Receiver,.72,.23);Link("taker","Receiver");}
                    else{var t=Target("throw_area",.89,.5);zones.Add(new(.82,.3,.14,.4));Role(SetPieceRole.Finisher,.77,.48);Link("taker",t);Link("Finisher",t,true);}
                }
                else if(routine=="direct"){var goal=Target("goal",.997,.5,"DiagramGoal");Link("taker",goal);}
                else if(routine=="short"){Role(SetPieceRole.Receiver,corner?.92:.76,.22);Link("taker","Receiver");}
                else if(routine=="rehearsed")
                {Role(SetPieceRole.Receiver,.75,.6);Role(SetPieceRole.Finisher,.68,.35);if(config.Variant=="one_two"){Link("taker","Receiver");var target=Target("rehearsed",.89,.5);Link("Receiver",target);Link("Finisher",target,true);}}
                else
                {
                    var target=config.CrossTarget??(routine=="near_post"?"near_post":routine=="far_post"?"far_post":routine=="mixed"?"mixed":"centre");
                    var width=target=="near_post"?.36:target=="far_post"?.64:.5;
                    var t=Target("cross",.9,width);
                    zones.Add(new(.84,target=="mixed"?.25:width-.07,.12,target=="mixed"?.5:.14));
                    Role(SetPieceRole.Finisher,config.Movement=="late"?.55:.74,.45);if(routine!="cross"||config.CrossTarget is not null)Link("taker",t);if(config.Movement is not null||corner)Link("Finisher",t,true);
                    if(config.Movement=="combined"){Role(SetPieceRole.Receiver,.65,.7);var other=Target("support",.85,.65);Link("Receiver",other,true);}
                }
            }
            // Las funciones pendientes también conservan símbolo y etiqueta; nunca reciben una identidad automática.
            foreach(var role in active.Where(r=>!points.Any(p=>p.Id==r.ToString())))Role(role,.45,.3+((int)role%4)*.13);
        }
        // Las jugadas usan ubicaciones explícitas; el grafo conserva extremos identificados al recolocar símbolos.
        if(tab==3){
            foreach(var p in points.Where(p=>p.Id.StartsWith("Marker",StringComparison.Ordinal)).ToArray()){var index=int.Parse(p.Id[6..])-1;var rival="rival"+index;if(points.Any(p=>p.Id==rival))Link(p.Id,rival);}
            if(kind is SetPieceKind.DefendingCorner or SetPieceKind.DefendingFreeKick){
                foreach(var (role,x,y) in new[]{(SetPieceRole.NearPost,.025,.36),(SetPieceRole.FarPost,.025,.64),(SetPieceRole.Rebound,.31,.52)}){
                    var i=points.FindIndex(p=>p.Id==role.ToString());if(i>=0)points[i]=points[i] with{Advance=x,Width=y};}
            }
            if(kind is SetPieceKind.DirectFreeKick or SetPieceKind.IndirectFreeKick or SetPieceKind.DefendingFreeKick){
                var defending=kind==SetPieceKind.DefendingFreeKick;var advance=location==FreeKickLocation.Distant?.56:location==FreeKickLocation.Edge?.75:.76;var width=location==FreeKickLocation.Left?.07:location==FreeKickLocation.Right?.93:.5;
                if(defending)advance=1-advance;
                var launcher=defending?"rival_taker":"taker";var i=points.FindIndex(p=>p.Id==launcher);if(i>=0)points[i]=points[i] with{Advance=advance,Width=width};
                Add(new("ball",advance,Math.Clamp(width+.028,0,1),"DiagramBall",null,DiagramSymbol.Target));
                if(!defending){
                    if(location==FreeKickLocation.Right){var cross=points.FindIndex(p=>p.Id=="cross");if(cross>=0)points[cross]=points[cross] with{Width=1-points[cross].Width};for(var z=0;z<zones.Count;z++)zones[z]=zones[z] with{Width=1-zones[z].Width-zones[z].Breadth};}
                    var receiver=points.FindIndex(p=>p.Id=="Receiver");if(receiver>=0)points[receiver]=points[receiver] with{Advance=Math.Min(.93,advance+.055),Width=width<.5?width+.14:width-.14};
                    if(settings.SetPieces[kind].Routine=="direct"&&location==FreeKickLocation.Edge)for(var w=0;w<3;w++)Add(new("rival_wall_"+w,.84,.44+w*.06,"DiagramRivalWall",null,DiagramSymbol.Opponent));
                    // La convención corta/larga solo se aplica a pases, no al significado de una carrera.
                    for(var a=0;a<arrows.Count;a++)if(!arrows[a].Movement&&arrows[a].From=="taker")arrows[a]=arrows[a] with{Short=settings.SetPieces[kind].Routine=="short"?true:false};
                }else{
                    var wallX=advance*.55;var wallY=.5+(width-.5)*.55;var length=Math.Sqrt(advance*advance+(width-.5)*(width-.5));var perpendicularX=-(width-.5)/length;var perpendicularY=advance/length;
                    var walls=points.Where(p=>p.Id.StartsWith("Wall",StringComparison.Ordinal)).OrderBy(p=>p.Id,StringComparer.Ordinal).ToArray();
                    for(var w=0;w<walls.Length;w++){var at=points.FindIndex(p=>p.Id==walls[w].Id);points[at]=points[at] with{Advance=Math.Clamp(wallX+perpendicularX*(w-(walls.Length-1)/2d)*.055,.02,.98),Width=Math.Clamp(wallY+perpendicularY*(w-(walls.Length-1)/2d)*.055,.05,.95)};}
                    var prone=points.FindIndex(p=>p.Lying);if(prone>=0)points[prone]=points[prone] with{Advance=Math.Max(.02,wallX-.04),Width=wallY};
                }
            }
        }
        var result=new TacticDiagram(points.AsReadOnly(),arrows.AsReadOnly(),zones.AsReadOnly());result.Validate();return result;
    }
}
