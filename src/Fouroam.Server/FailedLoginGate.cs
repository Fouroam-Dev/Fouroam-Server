namespace Fouroam.Server;

/// <summary>Per-remote-address sliding 60-second window for failed authentication.</summary>
public sealed class FailedLoginGate
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Queue<DateTimeOffset>> _attempts = [];
    private readonly TimeSpan _window = TimeSpan.FromSeconds(60);
    private const int MaxFailures = 5;

    public bool IsBlocked(string address)
    {
        lock (_gate)
        {
            PruneUnsafe(address);
            return _attempts.TryGetValue(address, out var attempts) &&
                   attempts.Count >= MaxFailures;
        }
    }

    public int RecordFailure(string address)
    {
        lock (_gate)
        {
            PruneUnsafe(address);
            if (!_attempts.TryGetValue(address, out var attempts))
                _attempts[address] = attempts = new Queue<DateTimeOffset>();
            attempts.Enqueue(DateTimeOffset.UtcNow);
            return attempts.Count;
        }
    }

    private void PruneUnsafe(string address)
    {
        if (!_attempts.TryGetValue(address, out var attempts)) return;
        var cutoff = DateTimeOffset.UtcNow - _window;
        while (attempts.Count > 0 && attempts.Peek() < cutoff)
            attempts.Dequeue();
        if (attempts.Count == 0) _attempts.Remove(address);
    }
}
