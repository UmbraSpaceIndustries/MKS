using Experience;

namespace KolonyTools.Experience
{
    public class FundsBoost : ExperienceEffect
    {
        public FundsBoost(ExperienceTrait parent) : base(parent)
        {
        }

        public FundsBoost(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "A Kolonist that increases funds";
        }
    }
}