// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Body.Part;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared._Veil.Genitals;

public abstract partial class SharedGenitalCoverageSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private const SlotFlags GroinClothesFlags = SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING | SlotFlags.LEGS;
    private const SlotFlags GroinUnderwearFlags = SlotFlags.UNDERWEARB;
    private const SlotFlags ChestClothesFlags = SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING;
    private const SlotFlags ChestUnderwearFlags = SlotFlags.UNDERWEART;

    public bool IsCoveredByClothes(EntityUid body, BodyPartType partType)
    {
        return IsCovered(body, partType == BodyPartType.Chest ? ChestClothesFlags : GroinClothesFlags);
    }

    public bool IsCoveredByUnderwear(EntityUid body, BodyPartType partType)
    {
        return IsCovered(body, partType == BodyPartType.Chest ? ChestUnderwearFlags : GroinUnderwearFlags);
    }

    public bool IsVisibleTo(EntityUid body, BodyPartType partType, GenitalVisibility visibility)
    {
        return visibility switch
        {
            GenitalVisibility.AlwaysHidden => false,
            GenitalVisibility.HiddenByClothes => !IsCoveredByClothes(body, partType),
            GenitalVisibility.HiddenByUnderwear => !IsCoveredByClothes(body, partType) && !IsCoveredByUnderwear(body, partType),
            _ => false,
        };
    }

    public BodyPartType GetGenitalPart(string categoryId, string shape, float size)
    {
        var category = new ProtoId<GenitalCategoryPrototype>(categoryId);
        if (GenitalVisualBuilder.TryFindBest(_prototypes, category, shape, size, out var visual) && !visual.Internal)
            return visual.Part;

        return categoryId == "Breasts" ? BodyPartType.Chest : BodyPartType.Groin;
    }

    public bool IsGenitalAccessible(EntityUid body, string categoryId, string shape, float size, GenitalVisibility visibility)
    {
        return IsVisibleTo(body, GetGenitalPart(categoryId, shape, size), visibility);
    }

    private bool IsCovered(EntityUid body, SlotFlags flags)
    {
        if (!_inventory.TryGetSlots(body, out var slots))
            return false;

        foreach (var slot in slots)
        {
            if ((slot.SlotFlags & flags) == 0)
                continue;

            if (_inventory.TryGetSlotEntity(body, slot.Name, out _))
                return true;
        }

        return false;
    }
}
