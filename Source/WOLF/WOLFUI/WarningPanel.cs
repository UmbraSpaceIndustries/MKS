using UnityEngine;
using UnityEngine.UI;

namespace WOLFUI
{
    public class WarningPanel : MonoBehaviour
    {
        [SerializeField]
        private Text Label;

        public void Initialize(WarningMetadata warning)
        {
            if (Label != null)
            {
                Label.text = warning.Message;
            }
        }
    }
}
