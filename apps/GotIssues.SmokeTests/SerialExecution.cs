namespace GotIssues.SmokeTests;

/// <summary>
/// Every smoke test class joins this collection so they run one at a time.
///
/// Not a style choice: these tests start real containers, and parallel classes would
/// compete for Docker, for image builds, and for the machine. Serialising them also
/// keeps failures readable — a timeout means the stack was slow, not that four stacks
/// were fighting.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SerialExecution
{
    public const string Name = "compose";
}
