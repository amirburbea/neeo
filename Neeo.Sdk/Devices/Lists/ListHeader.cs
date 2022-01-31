namespace Neeo.Sdk.Devices.Lists;

public sealed class ListHeader : ListItemBase
{
    public ListHeader(string title)
        : base(ListItemType.Header)
    {
        this.Title = Validator.ValidateString(title, maxLength: 255);
    }

    /// <summary>
    /// Tells the NEEO Brain that this is a Header.
    /// </summary>
    public bool IsHeader { get; } = true;

    public string Title { get; }
}