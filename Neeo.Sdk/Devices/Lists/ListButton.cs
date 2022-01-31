﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Lists;

[JsonConverter(typeof(TextJsonConverter<ListButtonIcon>))]
public enum ListButtonIcon
{
    [Text("shuffle")]
    Shuffle,

    [Text("repeat")]
    Repeat
}

public sealed class ListButton
{
    public ListButton(string title, string actionIdentifier, bool? inverse = default, ListButtonIcon? icon = default)
    {
        this.Title = Validator.ValidateString(title, maxLength: 255);
        this.Icon = icon;
        this.Inverse = inverse;
        this.ActionIdentifier = actionIdentifier;
    }

    public string ActionIdentifier { get; }

    [JsonPropertyName("iconName")]
    public ListButtonIcon? Icon { get; }

    public bool? Inverse { get; }

    /// <summary>
    /// Tells the NEEO Brain that this is a Button.
    /// </summary>
    public bool IsButton { get; } = true;

    public string Title { get; }
}

public sealed class ListButtonRow : ListItemBase
{
    public ListButtonRow(IReadOnlyCollection<ListButton> buttons)
        : base(ListItemType.ButtonRow) => this.Buttons = buttons is { Count: >= 1 and <= Constants.MaxButtonsPerRow }
            ? buttons.ToArray() // Copy to prevent mutations.
            : throw new ArgumentException($"Buttons must have between 1 and {Constants.MaxButtonsPerRow} elements.", nameof(buttons));

    public ListButtonRow(params ListButton[] buttons)
        : this((IReadOnlyCollection<ListButton>)buttons)
    {
    }

    public IReadOnlyCollection<ListButton> Buttons { get; }
}