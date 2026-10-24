using System.Collections.Generic;

namespace CustomWorkers;

internal static class WorkerTaskBoardRosterRules
{
    internal static IReadOnlyList<int> GetMissingHiredWorkerIndexes(
        IEnumerable<int> candidateWorkerIndexes,
        IEnumerable<int> hiredWorkerIndexes,
        IEnumerable<int> existingWorkerIndexes)
    {
        var hired = new HashSet<int>(hiredWorkerIndexes);
        var existing = new HashSet<int>(existingWorkerIndexes);
        var missing = new List<int>();
        var seen = new HashSet<int>();

        foreach (int workerIndex in candidateWorkerIndexes)
        {
            if (hired.Contains(workerIndex) && existing.Add(workerIndex) && seen.Add(workerIndex))
            {
                missing.Add(workerIndex);
            }
        }

        return missing;
    }
}
