// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Veil.Genitals;
using Content.Shared.Body;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Inventory.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._Veil.Genitals;

public sealed partial class GenitalVisualSystem : SharedGenitalCoverageSystem
{
    [Dependency] private GenitalSystem _genitals = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DidEquipEvent>(OnClothingChanged);
        SubscribeLocalEvent<DidUnequipEvent>(OnClothingChanged);
    }

    private void OnClothingChanged(DidEquipEvent args)
    {
        RefreshBody(args.EquipTarget);
    }

    private void OnClothingChanged(DidUnequipEvent args)
    {
        RefreshBody(args.EquipTarget);
    }

    public void RefreshBody(EntityUid body)
    {
        var layers = new List<GenitalLayerData>();

        foreach (var (organ, genital) in CollectOrgans(body))
        {
            if (!TryBuildLayer(body, organ, genital, out var layer))
                continue;

            layers.Add(layer);
        }

        layers.Sort((a, b) => a.Layer.CompareTo(b.Layer));

        if (layers.Count == 0 && !HasComp<GenitalVisualStateComponent>(body))
            return;

        var state = EnsureComp<GenitalVisualStateComponent>(body);

        var changed = state.Layers.Count != layers.Count;
        if (!changed)
        {
            for (var i = 0; i < layers.Count; i++)
            {
                if (!state.Layers[i].Equals(layers[i]))
                {
                    changed = true;
                    break;
                }
            }
        }

        if (!changed)
            return;

        state.Layers = layers;
        Dirty(body, state);
    }

    private IEnumerable<(EntityUid Organ, GenitalComponent Genital)> CollectOrgans(EntityUid body)
    {
        return _genitals.GetGenitals(body);
    }

    private bool TryBuildLayer(EntityUid body, EntityUid organ, GenitalComponent genital, out GenitalLayerData layer)
    {
        layer = default!;
        if (!GenitalVisualBuilder.TryFindBest(_prototypes, genital.Category, genital.Shape, genital.Size, out var visual) ||
            visual.Internal)
            return false;

        var aroused = TryComp(organ, out GenitalArousalComponent? arousal) && arousal.Aroused;

        layer = new GenitalLayerData
        {
            Layer = visual.Layer,
            State = GenitalVisualBuilder.BuildState(visual, genital.Size, genital.UseSkinColor, aroused),
            Rsi = visual.Rsi,
            Color = genital.Color,
            Aroused = aroused,
            Part = visual.Part,
            Visibility = genital.Visibility,
        };
        return true;
    }
}
