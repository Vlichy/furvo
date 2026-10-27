using System.Globalization;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Panel compacto de atributos públicos; nombres a la izquierda y números alineados, sin encabezados de tabla repetidos.</summary>
public partial class AttributeSection : PanelContainer
{
    public AttributeSection(VisibleAttributeGroup group, int columns = 1)
    {
        if (group.Category == AttributeCategory.Hidden || !Enum.IsDefined(group.Category))
            throw new ArgumentException("La ficha solo admite categorías visibles.", nameof(group));
        if (columns is < 1 or > 2) throw new ArgumentOutOfRangeException(nameof(columns));
        if (group.Values.Select(v => v.Code).Distinct(StringComparer.Ordinal).Count() != group.Values.Count)
            throw new ArgumentException("Atributos visibles repetidos.", nameof(group));
        Name = "Category_" + group.Category;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ShrinkBegin;
        var panel = new StyleBoxFlat { BgColor = new Color("14243a"), BorderColor = new Color("2c4058"),
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6 };
        panel.SetBorderWidthAll(1);
        panel.SetCornerRadiusAll(5);
        AddThemeStyleboxOverride("panel", panel);
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 6);
        AddChild(body);
        var title = new Label { Text = UiText.Get("Category_" + group.Category) };
        title.AddThemeFontSizeOverride("font_size", 16);
        title.AddThemeColorOverride("font_color", new Color("f1b96b"));
        body.AddChild(title);
        var grid = new GridContainer { Columns = columns, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 24);
        body.AddChild(grid);
        var perColumn = (group.Values.Count + columns - 1) / columns;
        for (var column = 0; column < columns; column++)
        {
            var rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            rows.AddThemeConstantOverride("separation", 0);
            grid.AddChild(rows);
            foreach (var value in group.Values.Skip(column * perColumn).Take(perColumn))
            {
                // Defensa adicional de presentación: un DTO erróneo tampoco puede renderizar códigos ocultos o de otra categoría.
                if (!PlayerAttributeCatalog.All.Any(d => d.Code == value.Code && !d.IsHidden && d.Category == group.Category))
                    throw new ArgumentException("El atributo no pertenece al catálogo público de la categoría.", nameof(group));
                var row = new HBoxContainer { Name = "Attribute_" + value.Code, SizeFlagsHorizontal = SizeFlags.ExpandFill };
                row.SetMeta("attribute_code", value.Code);
                row.AddThemeConstantOverride("separation", 8);
                var label = new Label { Text = UiText.Get("Attribute_" + value.Code),
                    SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
                label.AddThemeFontSizeOverride("font_size", 15);
                var number = new Label { Name = "Value", Text = value.Value.ToString(CultureInfo.InvariantCulture),
                    HorizontalAlignment = HorizontalAlignment.Right, CustomMinimumSize = new Vector2(28, 0) };
                number.AddThemeFontSizeOverride("font_size", 15);
                number.AddThemeColorOverride("font_color", new Color(AttributeValueStyle.ColorHex(value.Value)));
                row.AddChild(label);
                row.AddChild(number);
                rows.AddChild(row);
            }
        }
    }
}
