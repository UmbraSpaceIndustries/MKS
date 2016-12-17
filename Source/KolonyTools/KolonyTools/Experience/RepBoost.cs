using Experience;

namespace KolonyTools.Experience
{
    public class RepBoost : ExperienceEffect
    {
        public RepBoost(ExperienceTrait parent) : base(parent)
        {
        }

        public RepBoost(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "A Kolonist that increases reputation";
        }
    }
}