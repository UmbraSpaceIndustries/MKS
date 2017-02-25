using UnityEngine;

namespace KolonyTools.AC
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    class EditACPrefabsSpaceCentre : MonoBehaviour
    {
        private void Awake()
        {
            gameObject.AddComponent<EditACPrefab>();
        }
    }
}