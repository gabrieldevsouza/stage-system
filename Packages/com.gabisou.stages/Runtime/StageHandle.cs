using UnityEngine;

/// <summary>
/// Stage-scoped handle used by external systems to safely register spawned objects to the current stage.
/// Prevents late registration into a newer stage via a loadId check.
/// </summary>
public readonly struct StageHandle
{
    private readonly StageManager _manager;
    private readonly int _loadId;

    public int LoadId => _loadId;

    internal StageHandle(StageManager manager, int loadId)
    {
        _manager = manager;
        _loadId = loadId;
    }

    /// <summary>
    /// Registers an object to be auto-destroyed when this stage unloads.
    /// If the handle is stale, the object is destroyed immediately by the StageManager.
    /// </summary>
    public void Register(GameObject obj)
    {
        _manager?.RegisterAdditionalObject(obj, _loadId);
    }
}