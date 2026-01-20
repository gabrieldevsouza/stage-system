using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Deterministic stage loader supporting scenes, prefabs, and hybrid stages.
/// - Runtime state lives here (MonoBehaviour), not in ScriptableObjects.
/// - Loads are cancellable and supersede previous loads.
/// - Scene load/unload is awaited.
/// - All stage objects are tracked and reliably cleaned up.
/// </summary>
public sealed class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Roots (optional overrides)")]
    [SerializeField] private Transform stagesRoot;
    [SerializeField] private Transform curtainRoot;

    public ScriptableStage CurrentStage { get; private set; }
    public int CurrentLoadId { get; private set; }

    private readonly List<GameObject> _loadedInstances = new();
    private readonly List<GameObject> _additionalStageObjects = new();

    private CancellationTokenSource _loadCts;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureRoots();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = null;
            Instance = null;
        }
    }

    private void EnsureRoots()
    {
        if (stagesRoot == null)
        {
            var go = new GameObject("[StagesRoot]");
            DontDestroyOnLoad(go);
            stagesRoot = go.transform;
        }

        if (curtainRoot == null)
        {
            var go = new GameObject("[CurtainRoot]");
            DontDestroyOnLoad(go);
            curtainRoot = go.transform;
        }
    }

    /// <summary>
    /// Loads a stage. Any in-flight load is cancelled and superseded.
    /// Deterministic: always unloads current stage first.
    /// Returns a StageHandle for stage-scoped object registration.
    /// </summary>
    public async UniTask<StageHandle> LoadStageAsync(ScriptableStage stage, CancellationToken externalToken = default)
    {
        if (stage == null) throw new ArgumentNullException(nameof(stage));
        stage.ValidateOrThrow();

        // Cancel any in-flight load.
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

        var token = _loadCts.Token;

        // Bump load id so other systems can validate stage-scoped actions.
        CurrentLoadId++;
        var loadId = CurrentLoadId;

        // Unload current stage first.
        await UnloadCurrentStageAsync(token);
        token.ThrowIfCancellationRequested();

        CurrentStage = stage;

        // 1) Scene (optional)
        if (stage.UsesScene)
        {
            var loadOp = SceneManager.LoadSceneAsync(stage.SceneName, LoadSceneMode.Additive);
            if (loadOp == null)
                throw new InvalidOperationException($"Failed to start loading scene '{stage.SceneName}'.");

            await loadOp.ToUniTask(cancellationToken: token);

            var loadedScene = SceneManager.GetSceneByName(stage.SceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                throw new InvalidOperationException($"Scene '{stage.SceneName}' did not load correctly.");

            if (stage.SetActiveSceneToLoadedScene)
                SceneManager.SetActiveScene(loadedScene);
        }

        token.ThrowIfCancellationRequested();

        // 2) Prefabs (optional)
        if (stage.UsesPrefabs)
        {
            var prefabs = stage.Prefabs;
            if (prefabs != null)
            {
                for (int i = 0; i < prefabs.Length; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var prefab = prefabs[i];
                    if (prefab == null) continue;

                    var instance = Instantiate(prefab, stagesRoot);
                    _loadedInstances.Add(instance);
                }
            }
        }

        token.ThrowIfCancellationRequested();
        return new StageHandle(this, loadId);
    }

    /// <summary>
    /// Unloads the current stage (tracked instances + registered additional objects + stage scene if any).
    /// Awaitable and deterministic.
    /// </summary>
    public async UniTask UnloadCurrentStageAsync(CancellationToken token = default)
    {
        // Destroy tracked objects first (they may reference things in the stage scene).
        DestroyTrackedObjects(_additionalStageObjects);
        DestroyTrackedObjects(_loadedInstances);

        // Unload scene if current stage uses a scene.
        if (CurrentStage != null && CurrentStage.UsesScene)
        {
            var scene = SceneManager.GetSceneByName(CurrentStage.SceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                var unloadOp = SceneManager.UnloadSceneAsync(scene);
                if (unloadOp != null)
                    await unloadOp.ToUniTask(cancellationToken: token);
            }
        }

        _additionalStageObjects.Clear();
        _loadedInstances.Clear();
        CurrentStage = null;
    }

    private static void DestroyTrackedObjects(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var go = list[i];
            if (go != null)
                UnityEngine.Object.Destroy(go);
        }
    }

    /// <summary>
    /// Registers an object to be auto-destroyed when the stage unloads.
    /// Stale handles are rejected; the object is destroyed immediately.
    /// </summary>
    internal void RegisterAdditionalObject(GameObject obj, int loadId)
    {
        if (obj == null) return;

        // Reject late registration into a newer stage.
        if (loadId != CurrentLoadId)
        {
            Destroy(obj);
            return;
        }

        _additionalStageObjects.Add(obj);
    }
}