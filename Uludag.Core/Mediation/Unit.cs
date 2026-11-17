namespace Uludag.Core.Mediation
{
    /// <summary>
    /// "Geriye değer döndürmeyen" istekler için kullanılan boş dönüş tipi (void'in tip karşılığı).
    /// Generic yapılarda <c>void</c> kullanılamadığı için <c>Task&lt;Unit&gt;</c> yazılır.
    /// </summary>
    public readonly struct Unit : IEquatable<Unit>
    {
        public static readonly Unit Value = new();

        public static Task<Unit> Task { get; } = System.Threading.Tasks.Task.FromResult(Value);

        public bool Equals(Unit other) => true;
        public override bool Equals(object? obj) => obj is Unit;
        public override int GetHashCode() => 0;
        public override string ToString() => "()";

        public static bool operator ==(Unit left, Unit right) => true;
        public static bool operator !=(Unit left, Unit right) => false;
    }
}
