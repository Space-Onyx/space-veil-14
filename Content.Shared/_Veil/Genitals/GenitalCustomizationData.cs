// SPDX-FileCopyrightText: 2026 Space Veil Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Veil.Genitals;

public static class GenitalCustomizationData
{
    public static readonly string[] Shapes = ["human", "knotted", "plain", "flared"];

    public static readonly Color[] Colors = [Color.HotPink, Color.MediumPurple, Color.Crimson, Color.DodgerBlue, Color.White];

    public static readonly string[] ColorLocKeys = ["pink", "purple", "crimson", "blue", "white"];

    public const int MinSize = 1;
    public const int MaxSize = 3;

    public const int MinVibration = 1;
    public const int MaxVibration = 3;

    public const string PreviewRsi = "_Veil/Objects/Specific/SexToys/dildo.rsi";

    public static string PreviewState(string shape, int size) => $"dildo_{shape}_{size}";
}
