using UnityEngine;
using UnityEngine.UI;
namespace TemporalContract.UI.Hud
{
    public sealed class HudNoticeBackdrop : MonoBehaviour
    {
        public Text target;
        public CanvasGroup group;
        private void LateUpdate(){group.alpha=target&&target.gameObject.activeSelf?1:0;}
    }
}
