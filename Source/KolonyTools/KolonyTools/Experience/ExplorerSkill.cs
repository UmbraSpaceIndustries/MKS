using Experience;

namespace KolonyTools.Experience
{
    public class ExplorerSkill : ExperienceEffect
    {
        public ExplorerSkill(ExperienceTrait parent) : base(parent)
        {
        }

        public ExplorerSkill(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "Ability to function even when far from home in adverse conditions";
        }
    }
}