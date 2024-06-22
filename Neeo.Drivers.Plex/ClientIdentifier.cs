using System;

namespace Neeo.Drivers.Plex;

public interface IClientIdentifier
{
    string Value { get; }
}

internal sealed class ClientIdentifier(IFileStore fileStore) : IClientIdentifier
{
    private static readonly string _fileName = StringMethods.TitleCaseToSnakeCase(nameof(ClientIdentifier));

    private string? _value;

    public string Value => this._value ??= this.GetValue();

    private string GetValue()
    {
        if (this.TryReadIdentifier() is { } existing)
        {
            return existing;
        }
        string identifier = Guid.NewGuid().ToString();
        this.TryWriteIdentifier(identifier);
        return identifier;
    }

    private string? TryReadIdentifier()
    {
        try
        {
            if (fileStore.HasFile(ClientIdentifier._fileName))
            {
                return fileStore.ReadText(ClientIdentifier._fileName);
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void TryWriteIdentifier(string identifier)
    {
        try
        {
            fileStore.WriteText(ClientIdentifier._fileName, identifier);
        }
        catch (Exception)
        {
            // Ignore.
        }
    }
}
