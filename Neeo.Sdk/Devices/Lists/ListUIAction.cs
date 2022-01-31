using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices.Lists;

[JsonConverter(typeof(TextJsonConverter<ListUIAction>))]
public enum ListUIAction
{
    [Text("close")]
    Close,

    [Text("goToRoot")]
    GoToRoot,

    [Text("goBack")]
    GoBack,

    [Text("reload")]
    Reload
}