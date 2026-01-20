using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Stages/Stage", fileName = "Stage_")]
public sealed class ScriptableStage : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Optional stable identifier (useful for logs/analytics).")]
    [SerializeField] private string stageId;

    [Header("Mode")]
    [SerializeField] private StageMode mode = StageMode.PrefabsOnly;

    [Header("Scene (Build Settings name)")]
    [Tooltip("Required when Mode is SceneOnly or SceneWithPrefabs.")]
    [SerializeField] private string sceneName;

    [Header("Prefabs")]
    [Tooltip("Used when Mode is PrefabsOnly or SceneWithPrefabs.")]
    [SerializeField] private GameObject[] prefabs;

    [Header("Scene behavior")]
    [Tooltip("If true, sets the newly loaded stage scene as the active scene.")]
    [SerializeField] private bool setActiveSceneToLoadedScene = true;

    public string StageId => stageId;
    public StageMode Mode => mode;
    public string SceneName => sceneName;
    public GameObject[] Prefabs => prefabs;
    public bool SetActiveSceneToLoadedScene => setActiveSceneToLoadedScene;

    public bool UsesScene => mode == StageMode.SceneOnly || mode == StageMode.SceneWithPrefabs;
    public bool UsesPrefabs => mode == StageMode.PrefabsOnly || mode == StageMode.SceneWithPrefabs;

    public void ValidateOrThrow()
    {
        if (UsesScene && string.IsNullOrWhiteSpace(sceneName))
            throw new InvalidOperationException(
                $"Stage '{name}' requires a Scene Name because Mode is {mode}.");
    }
}