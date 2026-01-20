using UnityEngine;

namespace Gabisou.Stagesystem.Integrations.UI
{
    public sealed class CanvasManager : MonoBehaviour, ICanvasProvider
    {
        [SerializeField] private Canvas targetCanvas;

        public Transform Root => targetCanvas != null ? targetCanvas.transform : null;
    }
}