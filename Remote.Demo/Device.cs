using System;
using System.Security.Cryptography;
using System.Windows.Threading;
using Neeo.Api.Devices;

namespace Remote.Demo;

internal class Device : NotifierBase
{
    private readonly DispatcherTimer _dispatcherTimer;
    private bool _isMuted = false;
    private bool _isPoweredOn;
    private double _volume = 50;

    public Device()
    {
        this._dispatcherTimer = new();
        this._dispatcherTimer.Tick += this.DispatcherTimer_Tick;
        this._dispatcherTimer.Interval = TimeSpan.FromSeconds(10d);
        this._dispatcherTimer.IsEnabled = false;
    }

    private void DispatcherTimer_Tick(object? sender, EventArgs e)
    {
        this.OnPropertyChanged(nameof(this.ImageUri));
    }

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
        set
        {
            if (!this.SetValue(ref this._isPoweredOn, value))
            {
                return;
            }
            if (value)
            {
                this._dispatcherTimer.Start();
            }
            else
            {
                this._dispatcherTimer.Stop();
            }
        }
    }

    public string ImageUri => "http://192.168.253.3:13579/snapshot.jpg?" + RandomNumberGenerator.GetInt32(100000000);

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