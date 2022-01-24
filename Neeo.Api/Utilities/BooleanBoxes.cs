using System.Runtime.CompilerServices;

namespace Neeo.Api.Utilities;

public static class BooleanBoxes
{
    public static readonly object False = false;

    public static readonly object True = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object GetBox(bool value) => value ? BooleanBoxes.True : BooleanBoxes.False;
}