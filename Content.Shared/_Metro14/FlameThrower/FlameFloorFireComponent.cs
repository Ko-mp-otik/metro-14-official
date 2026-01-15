namespace Content.Shared._Metro14.FlameThrower;

/// <summary>
/// Используется для отслеживания пола, который горит
/// </summary>
[RegisterComponent]
public sealed partial class FlameFloorFireComponent : Component
{
    [DataField]
    public TimeSpan DecalSpawnPeriod = TimeSpan.FromSeconds(5);

    public TimeSpan? NextDecalSpawn { get; set; }
    public bool SpawnedDecal = false;
}
