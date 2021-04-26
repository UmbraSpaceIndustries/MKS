using System;
using System.Linq;
using KSP.UI;
using KSP.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace KolonyTools.AC
{
    class EditACPrefab : MonoBehaviour
    {

        private void Awake()
        {
            try
            {
                var prefab = GetAstronautComplexScreenPrefab();

                // There isn't really any specific component we can look for to make it easier
                // without also making things less clear so a long transform path it is
                var testVL = prefab.transform.Find("RootCanvas/AstronautComplex anchor/GameObject/bg and aspectFitter/CrewAssignmentDialog/VL");

                if (testVL == null) throw new Exception("Couldn't find testVL Transform");


                // we can't just delete it (or set it inactive apparently) or AstronautComplex will throw an exception
                // we CAN, however, delete all the UI bits from it
                var panel = testVL.transform.Find("ListAndScrollbar");
                panel.SetParent(null);

                panel.GetComponentsInChildren<ScrollRect>(true).ToList().ForEach(Destroy);
                panel.GetComponentsInChildren<Image>(true).ToList().ForEach(Destroy);
                panel.GetComponentsInChildren<CanvasRenderer>(true).ToList().ForEach(Destroy);
                panel.GetComponentsInChildren<Mask>(true).ToList().ForEach(Destroy);
                panel.GetComponentsInChildren<VerticalLayoutGroup>(true).ToList().ForEach(Destroy);
                panel.GetComponentsInChildren<ContentSizeFitter>(true).ToList().ForEach(Destroy);

                foreach (Transform ch in testVL.transform)
                    ch.gameObject.SetActive(false);

                // reattach panel - AstronautComplex needs something inside here for something but now it won't be
                // rendering anything
                panel.SetParent(testVL);

                // we won't know what the dimensions our replacement will take until the UI is actually spawned,
                // so we'll slip a little trojan right alongside the component (RectTransform) that will contain
                // that info. This helper will create the GUI
                testVL.gameObject.AddComponent<CustomAstronautComplexUISpawner>();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to edit Astronaut Complex: " + e);
            }

            Destroy(gameObject); // once the spawner is attached to the prefab, this object no longer needed
        }


        private static UICanvasPrefab GetAstronautComplexScreenPrefab()
        {
            // the scene has just been entered and the AC shouldn't have spawned yet
            var spawner = FindObjectOfType<ACSceneSpawner>();

            if (spawner == null)
                throw new Exception("Did not find AC spawner");
            if (spawner.ACScreenPrefab == null)
                throw new Exception("AC spawner prefab is null");

            // this might look bizarre, but Unity seems to crash if you mess with this prefab like we're
            // about to do. Luckily ACScreenPrefab is public so we'll quickly make a copy and use that
            // as the "prefab" instead
            var prefab = Instantiate(spawner.ACScreenPrefab);
            prefab.gameObject.SetActive(false);

            spawner.ACScreenPrefab = prefab;

            return prefab;
        }
    }
}