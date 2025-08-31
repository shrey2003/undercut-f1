namespace UndercutF1.Data;

public interface IJsonTimingClient
{
    /// <summary>
    /// Fetches the directories which contain suitable files for simulation.
    /// Suitable directories contain a file named <c>live.jsonl</c> and <c>subscribe.json</c>.
    /// The directory name must be formed as <c>/[location]_[session_type]/</c> to support grouping of the directories.
    /// </summary>
    /// <returns>A dictionary of suitable directory paths, grouped by the location name to all sessions in that location.</returns>
    Task<
        Dictionary<(string Location, DateOnly Date), List<(string Session, string Directory)>>
    > GetDirectoryNamesAsync();

    /// <summary>
    /// Starts a simulation using the files inside the provided directory.
    /// The directory provided in <paramref name="directory"/> must contain a
    /// file named <c>live.jsonl</c> and <c>subscribe.json</c>
    /// </summary>
    /// <param name="directory">The directory to load the simulation files from.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> indicating that execution should be stopped.</param>
    /// <returns>A Task indicating when all simulation data has been sent to the <see cref="ITimingService"/>.</returns>
    Task LoadSimulationDataAsync(string directory, CancellationToken cancellationToken = default);
}
