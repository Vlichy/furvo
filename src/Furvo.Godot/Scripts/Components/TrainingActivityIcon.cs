using Furvo.Core.Development;
using Godot;
namespace Furvo.Presentation.Components;
/// <summary>Iconos vectoriales originales dibujados en Godot; no dependen de fuentes emoji ni recursos externos.</summary>
public partial class TrainingActivityIcon(TrainingArea area,bool rest=false) : Control
{
    public override void _Ready(){CustomMinimumSize=new Vector2(28,28);MouseFilter=MouseFilterEnum.Ignore;}
    public override void _Draw()
    {
        var c=new Color("f1b96b");var white=new Color("e5ecf5");
        void Line(float x,float y,float a,float b)=>DrawLine(new(x,y),new(a,b),white,2,true);
        if(rest){DrawArc(new(14,14),10,.6f,5.7f,32,c,3,true);Line(17,6,11,14);Line(11,14,19,20);return;}
        switch(area){
            case TrainingArea.Field:DrawCircle(new(14,14),11,white,false,2,true);DrawCircle(new(14,14),4,c);Line(4,8,10,11);Line(20,7,17,11);Line(14,18,14,25);break;
            case TrainingArea.Gym:Line(5,14,23,14);Line(6,7,6,21);Line(10,9,10,19);Line(22,7,22,21);Line(18,9,18,19);break;
            case TrainingArea.Goalkeepers:Line(3,24,3,5);Line(3,5,25,5);Line(25,5,25,24);Line(9,6,9,24);Line(19,6,19,24);Line(3,12,25,12);Line(3,19,25,19);break;
            case TrainingArea.SetPieces:DrawRect(new Rect2(3,4,22,19),white,false,2);DrawCircle(new(9,17),2,c);Line(9,17,18,9);Line(18,9,14,9);Line(18,9,18,13);break;
            case TrainingArea.Recovery:DrawArc(new(14,14),10,.3f,5.5f,30,c,2,true);Line(21,5,24,10);Line(24,10,18,9);Line(14,9,14,19);Line(9,14,19,14);break;
            case TrainingArea.Technology:DrawRect(new Rect2(7,7,14,14),c,false,2);for(var i=10;i<22;i+=5){Line(i,3,i,7);Line(i,21,i,25);Line(3,i,7,i);Line(21,i,25,i);}break;
        }
    }
}
