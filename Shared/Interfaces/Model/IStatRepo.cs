
namespace Shared.Interfaces.Model;

/// <summary>
/// Properties and methods for a local statistics repository.
/// </summary>
/// <remarks>
/// Maintains metadata for current and persisted <see cref="IStatTracker"/>s so that <see cref="ViewModel.IMainVM.Sync_Command"/> can upload pending statistics updates."/>
/// </remarks>
public interface IStatRepo
{
    /// <summary>
    /// Gets or sets the current stat tracker.
    /// </summary>
    IStatTracker? CurrentTracker { get; set; }
    /// <summary>
    /// Gets a value indicating whether any syncs are pending.
    /// </summary>
    bool SyncPending { get; }
    /// <summary>
    /// Gets the current status message for the sync process.
    /// </summary>
    string SyncStatusMessage { get; }
    /// <summary>
    /// Updates the repository given a recent save path and save result.
    /// </summary>
    /// <param name="path">Path of the saved game that may update the repository.</param>
    /// <param name="objNamesAndPositions">Save result. See <see cref="Services.Serializer.BinarySerializer.Save(IBinarySerializable[], string, bool)"/>.</param>
    /// <returns></returns>
    Task<string?> Update(string path, (string, long)[] objNamesAndPositions);
    /// <summary>
    /// Synchronizes the local statistics repository with the Azure database.
    /// </summary>
    /// <remarks>
    /// On this side, synchronization means uploading any pending statistics updates.
    /// </remarks>
    /// <returns>A task whose result is <see langword="true"/> if the synchronization completes successfully, whether full or partial success;
    /// otherwise, <see langword="false"/>.</returns>
    Task<bool> SyncToAzureDB();
}