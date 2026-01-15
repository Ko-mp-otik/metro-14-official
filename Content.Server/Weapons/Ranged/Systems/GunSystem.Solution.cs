using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Item;
using Content.Shared.Vapor;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

    protected override void InitializeSolution()
    {
        base.InitializeSolution();

        SubscribeLocalEvent<SolutionAmmoProviderComponent, MapInitEvent>(OnSolutionMapInit);
        SubscribeLocalEvent<SolutionAmmoProviderComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionMapInit(Entity<SolutionAmmoProviderComponent> entity, ref MapInitEvent args)
    {
        UpdateSolutionShots(entity);
    }

    private void OnSolutionChanged(Entity<SolutionAmmoProviderComponent> entity, ref SolutionContainerChangedEvent args)
    {
        if (args.Solution.Name == entity.Comp.SolutionId)
            UpdateSolutionShots(entity, args.Solution);
    }

    protected override void UpdateSolutionShots(Entity<SolutionAmmoProviderComponent> ent, Solution? solution = null)
    {
        var shots = 0;
        var maxShots = 0;
        // Metro-14
        if (solution == null && GetSolution(ent, out _, out solution))
        {
            ent.Comp.Shots = shots;
            DirtyField(ent.AsNullable(), nameof(SolutionAmmoProviderComponent.Shots));
            ent.Comp.MaxShots = maxShots;
            DirtyField(ent.AsNullable(), nameof(SolutionAmmoProviderComponent.MaxShots));
            return;
        }
        ent.Comp.CanShoot = !ShootAvailable(ent.Comp, solution);
        // Metro-14

        shots = (int)(solution.Volume / ent.Comp.FireCost);
        maxShots = (int)(solution.MaxVolume / ent.Comp.FireCost);

        ent.Comp.Shots = shots;
        DirtyField(ent.AsNullable(), nameof(SolutionAmmoProviderComponent.Shots));

        ent.Comp.MaxShots = maxShots;
        DirtyField(ent.AsNullable(), nameof(SolutionAmmoProviderComponent.MaxShots));

        UpdateSolutionAppearance(ent);
    }

    // Metro-14
    private static bool ShootAvailable(SolutionAmmoProviderComponent component, Solution solution)
    {
        if (component.ShootableReagentsId.Count == 0 || solution.Volume == FixedPoint2.Zero || solution.Contents.Count == 0)
            return true;

        FixedPoint2 unshootableReagentsQuantity = new();
        var dictionary = solution.Contents.Select(reagent => (reagent.Reagent.Prototype, reagent.Quantity)).ToDictionary();
        foreach (var (reagentId, quantity) in dictionary)
        {
            if (!component.ShootableReagentsId.Contains(reagentId))
                unshootableReagentsQuantity += quantity;
        }

        return (unshootableReagentsQuantity / solution.Volume).Float() >= component.MaxImpurities;
    }

    private bool GetSolution(Entity<SolutionAmmoProviderComponent> ent, [NotNullWhen(false)] out Entity<SolutionComponent>? solutionEntity, [NotNullWhen(false)] out Solution? checkSolution)
    {
        if (ent.Comp.ContainerId is not null && _itemSlotsSystem.TryGetSlot(ent, ent.Comp.ContainerId, out var slot) &&
            slot.Item is not null && _solutionContainer.TryGetSolution(slot.Item.Value, ent.Comp.SolutionId, out solutionEntity, out checkSolution))
            return false;

        return !_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out solutionEntity, out checkSolution);
    }
    // Metro-14

    protected override (EntityUid Entity, IShootable) GetSolutionShot(Entity<SolutionAmmoProviderComponent> ent, EntityCoordinates position)
    {
        var (shot, shootable) = base.GetSolutionShot(ent, position);

        if (GetSolution(ent, out var solution, out _))
            return (shot, shootable);

        var newSolution = _solutionContainer.SplitSolution(solution.Value, ent.Comp.FireCost);

        if (newSolution.Volume <= FixedPoint2.Zero)
            return (shot, shootable);

        if (TryComp<AppearanceComponent>(shot, out var appearance))
        {
            Appearance.SetData(shot, VaporVisuals.Color, newSolution.GetColor(ProtoManager).WithAlpha(1f), appearance);
            Appearance.SetData(shot, VaporVisuals.State, true, appearance);
        }

        // Add the solution to the vapor and actually send the thing
        if (_solutionContainer.TryGetSolution(shot, VaporComponent.SolutionName, out var vaporSolution, out _))
        {
            _solutionContainer.TryAddSolution(vaporSolution.Value, newSolution);
        }
        return (shot, shootable);
    }
}
