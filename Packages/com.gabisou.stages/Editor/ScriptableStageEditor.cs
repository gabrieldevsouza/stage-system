#if UNITY_EDITOR
using UnityEditor;

namespace Editor
{
    [CustomEditor(typeof(ScriptableStage))]
    public sealed class ScriptableStageEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var stage = (ScriptableStage)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(GetHelp(stage), MessageType.Info);
        }

        private static string GetHelp(ScriptableStage stage)
        {
            return stage.Mode switch
            {
                StageMode.PrefabsOnly => "PrefabsOnly: Scene Name is ignored.",
                StageMode.SceneOnly => "SceneOnly: Prefabs are ignored.",
                StageMode.SceneWithPrefabs => "SceneWithPrefabs: both Scene Name and Prefabs are used.",
                _ => "Unknown mode."
            };
        }
    }
}
#endif