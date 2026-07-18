namespace InsuranceData;

internal sealed class DeterministicRandom
{
    private ulong _state;

    public DeterministicRandom(int seed, long stream = 0)
    {
        _state = Mix((ulong)(uint)seed ^ ((ulong)stream * 0x9E3779B97F4A7C15UL));
    }

    public int NextInt(int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
        return (int)(NextUInt64() % (uint)maxExclusive);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        }

        return minInclusive + NextInt(maxExclusive - minInclusive);
    }

    public long NextLong(long maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
        return (long)(NextUInt64() % (ulong)maxExclusive);
    }

    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    public bool NextBool(double trueProbability = 0.5d)
    {
        if (trueProbability <= 0d)
        {
            return false;
        }

        if (trueProbability >= 1d)
        {
            return true;
        }

        return NextDouble() < trueProbability;
    }

    public T NextItem<T>(IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            throw new ArgumentException("The item list cannot be empty.", nameof(items));
        }

        return items[NextInt(items.Count)];
    }

    private ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        return Mix(_state);
    }

    private static ulong Mix(ulong value)
    {
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
