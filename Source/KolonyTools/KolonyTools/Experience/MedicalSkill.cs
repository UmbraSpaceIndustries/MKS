using Experience;

namespace KolonyTools.Experience
{
    public class MedicalSkill : ExperienceEffect
    {
        public MedicalSkill(ExperienceTrait parent) : base(parent)
        {
        }

        public MedicalSkill(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "Experience in keeping Kerbals healthy and happy";
        }
    }
}