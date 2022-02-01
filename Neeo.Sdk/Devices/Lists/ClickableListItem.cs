using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Lists;

public abstract class ClickableListItem
{
    protected ClickableListItem(string? actionIdentifier = default, ListUIAction? uiAction = default)
    {
        this.ActionIdentifier = actionIdentifier;
        this.UIAction = uiAction;
    }

    public string? ActionIdentifier { get; }

    [JsonPropertyName("uiAction")]
    public ListUIAction? UIAction { get; }
}