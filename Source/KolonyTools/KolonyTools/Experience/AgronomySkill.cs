using Experience;

namespace KolonyTools.Experience
{
    public class AgronomySkill : ExperienceEffect
    {
        public AgronomySkill(ExperienceTrait parent) : base(parent)
        {
        }

        public AgronomySkill(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "Experience in advanced farming and crop diversity";
        }
    }
}