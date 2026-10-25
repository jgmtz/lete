namespace Lete.Core.Combat;

/// <summary>Retângulo em frações da tela (0 a 1), independente da resolução.</summary>
public readonly record struct ScreenRect(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;
    public float Bottom => Y + Height;

    public bool Intersects(ScreenRect other) =>
        X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;

    public bool IsInsideScreen => X >= 0 && Y >= 0 && Right <= 1 && Bottom <= 1 && Width > 0 && Height > 0;
}

/// <summary>Um layout do HUD de combate: onde fica a caixa de ações e a descrição da opção escolhida.</summary>
public sealed record HudLayout(string Id, ScreenRect Menu, ScreenRect Description);

/// <summary>Layouts do HUD e a área central reservada aos gestos (o quadro da Esquiva).</summary>
public sealed record HudLayoutSet(ScreenRect GestureArea, IReadOnlyList<HudLayout> Layouts)
{
    /// <summary>Problemas (lista vazia = válido): painéis fora da tela ou invadindo a área dos gestos.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (!GestureArea.IsInsideScreen)
        {
            errors.Add("A área dos gestos precisa estar dentro da tela.");
        }

        if (Layouts.Count == 0)
        {
            errors.Add("É preciso pelo menos um layout.");
        }

        foreach (var layout in Layouts)
        {
            foreach (var (name, rect) in new[] { ("menu", layout.Menu), ("descrição", layout.Description) })
            {
                if (!rect.IsInsideScreen)
                {
                    errors.Add($"Layout '{layout.Id}': {name} fora da tela.");
                }

                if (rect.Intersects(GestureArea))
                {
                    errors.Add($"Layout '{layout.Id}': {name} invade a área dos gestos.");
                }
            }
        }

        return errors;
    }
}
