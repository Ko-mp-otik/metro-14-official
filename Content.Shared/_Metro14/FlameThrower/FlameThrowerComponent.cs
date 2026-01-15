using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Metro14.FlameThrower;

/// <summary>
/// Присваивается огнемёту, для управления огнём
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class FlameThrowerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? ShotWithoutFire = new SoundPathSpecifier("/Audio/_Metro14/Weapons/Guns/no-fire-shoot.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? FailShotSound = new SoundPathSpecifier("/Audio/_Metro14/Weapons/Guns/watershot.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float MaxFireSpeed = 10f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float MinFireSpeed = 5f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float MaxFireDistance = 6f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float MinFireDistance = 3;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float SpeedMod = 1f;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public bool Ignited;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan IgnitionTimeOnStart = TimeSpan.FromSeconds(30);

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan IgnitionTimeOnShot = TimeSpan.FromSeconds(5);

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan MaxIgnitionTime = TimeSpan.FromSeconds(60);

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? IgnitionTime;
}

[Serializable, NetSerializable]
public enum FlameThrower : byte
{
    Base = 0,
}
