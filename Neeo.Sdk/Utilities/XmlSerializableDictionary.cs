using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Xml serializable dictionary.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
[XmlRoot(nameof(Dictionary<TKey, TValue>))]
public class XmlSerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    where TKey : notnull
{
    XmlSchema? IXmlSerializable.GetSchema() => null;

    void IXmlSerializable.ReadXml(XmlReader reader)
    {
        bool isEmpty = reader.IsEmptyElement;
        reader.Read();
        if (isEmpty)
        {
            return;
        }
        XmlSerializer keySerializer = new(typeof(TKey));
        XmlSerializer valueSerializer = new(typeof(TValue));
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            reader.ReadStartElement(nameof(DictionaryEntry));
            reader.ReadStartElement(nameof(DictionaryEntry.Key));
            TKey key = (TKey)keySerializer.Deserialize(reader)!;
            reader.ReadEndElement();
            reader.ReadStartElement(nameof(DictionaryEntry.Value));
            TValue value = (TValue)valueSerializer.Deserialize(reader)!;
            reader.ReadEndElement();
            reader.ReadEndElement();
            this.Add(key, value);
            reader.MoveToContent();
        }
        reader.ReadEndElement();
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
        if (this.Count == 0)
        {
            return;
        }
        XmlSerializer keySerializer = new(typeof(TKey));
        XmlSerializer valueSerializer = new(typeof(TValue));
        foreach ((TKey key, TValue value) in this)
        {
            writer.WriteStartElement(nameof(DictionaryEntry));
            writer.WriteStartElement(nameof(DictionaryEntry.Key));
            keySerializer.Serialize(writer, key);
            writer.WriteEndElement();
            writer.WriteStartElement(nameof(DictionaryEntry.Value));
            valueSerializer.Serialize(writer, value);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
    }
}
