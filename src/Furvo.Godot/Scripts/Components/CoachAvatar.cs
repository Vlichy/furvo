using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Avatar genérico reutilizable, independiente de PlayerId; nunca carga rutas suministradas por el perfil.</summary>
public partial class CoachAvatar:TextureRect
{
    public CoachAvatar(string avatarKey="generic-v1")
    {
        Name="CoachAvatar";CustomMinimumSize=new Vector2(64,80);ExpandMode=ExpandModeEnum.IgnoreSize;StretchMode=StretchModeEnum.KeepAspectCentered;
        Texture=ResourceLoader.Load<Texture2D>("res://Assets/Portraits/avatar.svg");TooltipText=UiText.Get("CoachAvatarPlaceholder");MouseFilter=MouseFilterEnum.Ignore;
    }
}
