
namespace Shared.Interfaces.Model;

public interface IStatRepo
{
    IStatTracker? CurrentTracker { get; set; }
    bool SyncPending { get; }
    string SyncStatusMessage { get; }

    Task Shutdown();
    void Update();
    Task Save();
}