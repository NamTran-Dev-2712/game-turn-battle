namespace GameTeam.Application.Tests.TestSupport;

/// <summary>
/// Thread-safe ordered log of pipeline steps, appended by recording collaborators (validator, unit
/// of work, cache, handler, logger). Used to prove the actual behavior execution order.
/// </summary>
public sealed class ExecutionRecorder
{
    private readonly List<string> _steps = [];
    private readonly Lock _gate = new();

    public void Add(string step)
    {
        lock (_gate)
        {
            _steps.Add(step);
        }
    }

    public IReadOnlyList<string> Steps
    {
        get
        {
            lock (_gate)
            {
                return _steps.ToArray();
            }
        }
    }
}
