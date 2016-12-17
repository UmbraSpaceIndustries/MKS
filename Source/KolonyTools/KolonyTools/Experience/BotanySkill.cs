using Experience;

namespace KolonyTools.Experience
{
    public class BotanySkill : ExperienceEffect
    {
        public BotanySkill(ExperienceTrait parent) : base(parent)
        {
        }

        public BotanySkill(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "Experience managing basic greenhouses";
        }
    }
}