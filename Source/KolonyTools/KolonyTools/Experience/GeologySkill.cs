using Experience;

namespace KolonyTools.Experience
{
    public class GeologySkill : ExperienceEffect
    {
        public GeologySkill(ExperienceTrait parent) : base(parent)
        {
        }

        public GeologySkill(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "Experience sifting valuable resources out of planetary regolit";
        }
    }
}