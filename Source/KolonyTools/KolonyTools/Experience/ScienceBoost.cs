using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Experience;

namespace KolonyTools.Experience
{
    public class ScienceBoost : ExperienceEffect
    {
        public ScienceBoost(ExperienceTrait parent) : base(parent)
        {
        }

        public ScienceBoost(ExperienceTrait parent, float[] modifiers) : base(parent, modifiers)
        {
        }

        protected override float GetDefaultValue()
        {
            return 0f;
        }

        protected override string GetDescription()
        {
            return "A Kolonist that increases science";
        }
    }
}
