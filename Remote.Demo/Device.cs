using System;
using System.Security.Cryptography;
using System.Windows.Threading;
using Neeo.Sdk.Devices;

namespace Remote.Demo;

internal class Device : NotifierBase
{
    private static readonly string[] _imageUris = new[]
    {
        "https://neeo-sdk.neeo.io/kitten.jpg",
        "https://neeo-sdk.neeo.io/puppy.jpg",
        "https://neeo-sdk.neeo.io/folder.jpg",
        "https://neeo-sdk.neeo.io/file.jpg",
    };

    private string? _imageUri;
    private bool _isMuted = false;
    private bool _isPoweredOn;
    private double _volume = 50;

    

    public string ImageUri => this._imageUri ??= "https://neeo-sdk.neeo.io/puppy.jpg";

    public bool IsMuted
    {
        get => this._isMuted;
        set
        {
            if (this.SetValue(ref this._isMuted, value))
            {
                this.OnPropertyChanged(nameof(this.VolumeLabel));
            }
        }
    }

    public bool IsPoweredOn
    {
        get => this._isPoweredOn;
        set => this.SetValue(ref this._isPoweredOn, value);
    }

    public double Volume
    {
        get => this._volume;
        set
        {
            if (this.SetValue(ref this._volume, value))
            {
                this.OnPropertyChanged(nameof(this.VolumeLabel));
            }
        }
    }

    public string VolumeLabel => this.IsMuted ? "Muted" : this.Volume.ToString();

    public void ProcessButton(string buttonName)
    {
        if (KnownButton.GetKnownButton(buttonName) is not { } button)
        {
            return;
        }
        switch (button)
        {
            case KnownButtons.PowerOff:
                this.IsPoweredOn = false;
                return;
            case KnownButtons.PowerOn:
                this.IsPoweredOn = true;
                return;
            case KnownButtons.VolumeDown when !this.IsMuted: // Volume down does nothing if muted.
                this.Volume = Math.Max(0d, this.Volume - 1d);
                return;
            case KnownButtons.VolumeUp:
                this.IsMuted = false; // Unmute on volume up.
                this.Volume = Math.Min(100d, this.Volume + 1d);
                return;
            case KnownButtons.MuteToggle:
                this.IsMuted = !this.IsMuted;
                return;
        }
    }

    public void ProcessFavorite(string favorite)
    {
    }
}