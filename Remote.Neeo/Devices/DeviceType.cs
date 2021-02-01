using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public enum DeviceType
    {
        [Text("ACCESSOIRE")]
        Accessory = 0,
        [Text("AUDIO")]
        Audio,
        [Text("AVRECEIVER")]
        AVReceiver,
        [Text("DVD")]
        DVDisc,
        [Text("GAMECONSOLE")]
        GameConsole,
        [Text("LIGHT")]
        Light,
        [Text("MEDIAPLAYER")]
        MediaPlayer,
        [Text("MUSICPLAYER")]
        MusicPlayer,
        [Text("PROJECTOR")]
        Projector,
        [Text("TV")]
        TV,
        [Text("VOD")]
        VideoOnDemand,
        [Text("HDMISWITCH")]
        HdmiSwitch,
        /// <summary>
        /// CableTV/Satellite/etc...
        /// </summary>
        [Text("DVB")]
        SetTopBox,
        [Text("SOUNDBAR")]
        SoundBar,
        [Text("TUNER")]
        Tuner,
    }
}
