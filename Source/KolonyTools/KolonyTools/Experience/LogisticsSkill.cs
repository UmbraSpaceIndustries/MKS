using Experience;

namespace KolonyTools.Experience
{
    public class LogisticsSkill : ExperienceEffect
    {
        public LogisticsSkill(ExperienceTrait parent) : base(parent)
        {
        }

        public LogisticsSkill(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
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