using KSP.UI;
using UnityEngine;

namespace KolonyTools.AC
{
    class CustomAstronautComplexUISpawner : MonoBehaviour
    {
        private void Start()
        {
#if DEBUG
            DebugDimensions();
#endif
            var vectors = new Vector3[4];
            var uiCam = UIMasterController.Instance.GetComponentInChildren<UIMainCamera>();

            if (uiCam == null)
            {
                Debug.LogError("UIMainCamera not found");
                return;
            }

            var camera = uiCam.GetComponent<Camera>();

            if (camera == null)
            {
                Debug.LogError("Camera attached to UIMainCamera not found");
                return;
            }

            GetComponent<RectTransform>().GetWorldCorners(vectors);

            for (int i = 0; i < 4; ++i)
                vectors[i] = camera.WorldToScreenPoint(vectors[i]);


            // note: these are in screen space
            var rect = new Rect(vectors[1].x, Screen.height - vectors[1].y, vectors[2].x - vectors[1].x,
                vectors[2].y - vectors[3].y);

            gameObject.AddComponent<CustomAstronautComplexUI>().Initialize(rect);

            Destroy(this);
        }


        private void DebugDimensions()
        {
            print("Debugging dimensions");

            var vectors = new Vector3[4];
            var camera = UIMasterController.Instance.GetComponentInChildren<UIMainCamera>().GetComponent<Camera>();

            GetComponent<RectTransform>().GetWorldCorners(vectors);

            foreach (var item in vectors)
                print("Corner: " + item);

            for (int i = 0; i < 4; ++i)
            {
                vectors[i] = camera.GetComponent<Camera>().WorldToScreenPoint(vectors[i]);
                print("Transformed corner: " + vectors[i]);
            }
        }
    }
}