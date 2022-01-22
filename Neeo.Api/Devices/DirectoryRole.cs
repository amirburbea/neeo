﻿using System.Text.Json.Serialization;
using Neeo.Api.Utilities;

namespace Neeo.Api.Devices;

[JsonConverter(typeof(TextAttribute.EnumJsonConverter<DirectoryRole>))]
public enum DirectoryRole
{
    [Text("ROOT")]
    Root = 0,

    [Text("QUEUE")]
    Queue
}