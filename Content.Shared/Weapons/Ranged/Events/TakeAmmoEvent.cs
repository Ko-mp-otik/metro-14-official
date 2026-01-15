using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on a gun when it would like to take the specified amount of ammo.
/// </summary>
public sealed class TakeAmmoEvent(
    int shots,
    List<(EntityUid? Entity, IShootable Shootable)> ammo,
    EntityCoordinates coordinates,
    EntityUid? user)
    : EntityEventArgs
{
    public readonly EntityUid? User = user;
    public readonly int Shots = shots;
    public List<(EntityUid? Entity, IShootable Shootable)> Ammo = ammo;

    /// <summary>
    /// If no ammo returned what is the reason for it?
    /// </summary>
    public string? Reason;

    public SoundSpecifier? FailShotSound;
    /// <summary>
    /// Coordinates to spawn the ammo at.
    /// </summary>
    public EntityCoordinates Coordinates = coordinates;
}
