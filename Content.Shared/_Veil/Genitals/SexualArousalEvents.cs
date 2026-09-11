// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Veil.Genitals;

[ByRefEvent]
public readonly record struct GenitalArousalChangedEvent(EntityUid Organ, bool Aroused);

public readonly record struct GenitalPopupShownEvent(EntityUid Body, float SuppressSeconds);
