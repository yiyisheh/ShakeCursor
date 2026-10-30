namespace ShakeCursor
{
    /// <summary>.NET Framework 4.8 has no Math.Clamp.</summary>
    static class MathEx
    {
        public static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

        public static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
    }
}

namespace System.Runtime.CompilerServices
{
    /// <summary>Lets C# records and init accessors compile on .NET Framework.</summary>
    static class IsExternalInit { }
}
