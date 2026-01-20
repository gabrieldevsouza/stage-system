using Cysharp.Threading.Tasks;
using UnityEngine;
using Gabisou.Stagesystem;

namespace Gabisou.Stagesystem.Integrations.UI
{
    public sealed class InitializeUI : MonoBehaviour
    {
        [Header("Root Provider (must implement ICanvasProvider)")]
        [SerializeField] private MonoBehaviour canvasProviderBehaviour;

        [Header("UI Prefab")]
        [SerializeField] private GameObject uiPrefab;

        private ICanvasProvider _canvasProvider;
        private StageHandle _stageHandle;

        public void Bind(StageHandle handle) => _stageHandle = handle;

        private void Awake()
        {
            _canvasProvider = canvasProviderBehaviour as ICanvasProvider;
        }

        private async void Start()
        {
            if (_canvasProvider == null || uiPrefab == null)
                return;

            var token = this.GetCancellationTokenOnDestroy();

            await UniTask.WaitUntil(
                () => _canvasProvider.Root != null,
                PlayerLoopTiming.Update,
                token);

            var ui = Instantiate(uiPrefab, _canvasProvider.Root);
            _stageHandle.Register(ui);
        }
    }
}