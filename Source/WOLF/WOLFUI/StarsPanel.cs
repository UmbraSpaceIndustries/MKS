using UnityEngine;
using UnityEngine.UI;

namespace WOLFUI
{
    public class StarsPanel : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 1f)]
        private float OffAlpha = 0.2f;

        [SerializeField]
        [Range(0f, 1f)]
        private float OnAlpha = 1f;

        [SerializeField]
        private Image Star1;

        [SerializeField]
        private Image Star2;

        [SerializeField]
        private Image Star3;

        [SerializeField]
        private Image Star4;

        [SerializeField]
        private Image Star5;

        private void Awake()
        {
            if (OnAlpha > 1f)
            {
                OnAlpha = 1f;
            }
            else if (OnAlpha < 0f)
            {
                OnAlpha = 0f;
            }
            if (OffAlpha > 1f)
            {
                OffAlpha = 1f;
            }
            else if (OffAlpha < 0f)
            {
                OffAlpha = 0f;
            }
        }

        public void SetStars(int level)
        {
            ToggleStar(Star1, level >= 1);
            ToggleStar(Star2, level >= 2);
            ToggleStar(Star3, level >= 3);
            ToggleStar(Star4, level >= 4);
            ToggleStar(Star5, level >= 5);
        }

        private void ToggleStar(Image star, bool isOn)
        {
            var color = star.color;
            color.a = isOn ? OnAlpha : OffAlpha;
            star.color = color;
        }
    }
}
