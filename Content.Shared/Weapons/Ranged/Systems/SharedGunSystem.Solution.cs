using Content.Shared._Metro14.FlameThrower;
using Content.Shared.Chemistry.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    private readonly IRobustRandom _random = IoCManager.Resolve<IRobustRandom>();
    protected virtual void InitializeSolution()
    {
        SubscribeLocalEvent<SolutionAmmoProviderComponent, TakeAmmoEvent>(OnSolutionTakeAmmo);
        SubscribeLocalEvent<SolutionAmmoProviderComponent, GetAmmoCountEvent>(OnSolutionAmmoCount);
        SubscribeLocalEvent<SolutionAmmoProviderComponent, EntInsertedIntoContainerMessage>(OnSolutionSlotChange);
        SubscribeLocalEvent<SolutionAmmoProviderComponent, EntRemovedFromContainerMessage>(OnSolutionSlotChange);
    }

    // Metro-14
    /// <summary>
    /// Трекер события изменение контейнера с балоном
    /// </summary>
    protected virtual void OnSolutionSlotChange(EntityUid uid,
        SolutionAmmoProviderComponent provider,
        ContainerModifiedMessage args)
    {
        UpdateSolutionShots((uid, provider)); // Check solution shots count
        if (MagazineSlot != args.Container.ID)
            return;

        SolutionSlotChanged((uid, provider));
    }

    /// <summary>
    /// Метод нужен, чтобы менять спрайт в момент когда, балон или любой жидкостный контейнер
    /// был заряжен в оружие.
    /// </summary>
    private void SolutionSlotChanged(Entity<SolutionAmmoProviderComponent> ent)
    {
        UpdateAmmoCount(ent);
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        var magEnt = GetMagazineEntity(ent);
        Appearance.SetData(ent, AmmoVisuals.MagLoaded, magEnt != null, appearance);
    }
    // Metro-14

    private void OnSolutionTakeAmmo(Entity<SolutionAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        var shots = Math.Min(args.Shots, ent.Comp.Shots);

        // Don't dirty if it's an empty fire.
        if (shots == 0)
            return;

        // Metro-14
        if (!ent.Comp.CanShoot) // Shoot Fail.
        {
            if (TryComp<FlameThrowerComponent>(ent, out var flame))
                args.FailShotSound = flame.FailShotSound; // Пшик саунд

            args.Reason = Loc.GetString("flame-fire-flap");
            return;
        }
        // Metro-14

        for (var i = 0; i < shots; i++)
        {
            args.Ammo.Add(GetSolutionShot(ent, args.Coordinates));
            ent.Comp.Shots--;
        }

        UpdateSolutionShots(ent);
        UpdateSolutionAppearance(ent);
    }

    private void OnSolutionAmmoCount(Entity<SolutionAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        args.Count = ent.Comp.Shots;
        args.Capacity = ent.Comp.MaxShots;
    }

    protected virtual void UpdateSolutionShots(Entity<SolutionAmmoProviderComponent> ent, Solution? solution = null) { }

    protected virtual (EntityUid Entity, IShootable) GetSolutionShot(Entity<SolutionAmmoProviderComponent> ent, EntityCoordinates position)
    {
        var shot = Spawn(ent.Comp.Prototype, position);
        return (shot, EnsureShootable(shot));
    }

    protected void UpdateSolutionAppearance(Entity<SolutionAmmoProviderComponent> ent)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        Appearance.SetData(ent, AmmoVisuals.HasAmmo, ent.Comp.Shots != 0, appearance);
        Appearance.SetData(ent, AmmoVisuals.AmmoCount, ent.Comp.Shots, appearance);
        Appearance.SetData(ent, AmmoVisuals.AmmoMax, ent.Comp.MaxShots, appearance);
    }
}
