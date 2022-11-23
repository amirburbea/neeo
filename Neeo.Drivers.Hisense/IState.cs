namespace Neeo.Drivers.Hisense;

public interface IState
{
    StateType Type { get; }

    string ToString();
}
