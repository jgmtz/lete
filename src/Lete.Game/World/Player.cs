using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Lete.Game.World;

/// <summary>
/// Protagonista no mapa: anda como qualquer <see cref="WalkingCharacter"/> e examina objetos próximos.
/// </summary>
public partial class Player : WalkingCharacter
{
    private readonly List<Interactable> _nearby = new();

    public event Action? NearbyChanged;

    public Interactable? CurrentInteractable =>
        _nearby.OrderBy(i => i.GlobalPosition.DistanceSquaredTo(GlobalPosition)).FirstOrDefault();

    public void AddNearby(Interactable interactable)
    {
        if (!_nearby.Contains(interactable))
        {
            _nearby.Add(interactable);
            NearbyChanged?.Invoke();
        }
    }

    public void RemoveNearby(Interactable interactable)
    {
        if (_nearby.Remove(interactable))
        {
            NearbyChanged?.Invoke();
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Input global: o jogador vive dentro do SubViewport, mas a ação vale para o jogo todo.
        if (InputEnabled && Input.IsActionJustPressed("interact") && CurrentInteractable is { } target)
        {
            target.Examine();
        }
    }
}
