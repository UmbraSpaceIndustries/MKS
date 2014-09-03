using System;

namespace KolonyTools
{
    public class MKSLresource
    {
        public string resourceName = "";
        public double amount = 0;

        public double costPerMass = 0;

        public double mass()
        {
            PartResourceDefinition prd = PartResourceLibrary.Instance.GetDefinition(resourceName);
            return (amount * prd.density);
        }

        public string savestring()
        {
            return (resourceName + ":" + amount.ToString());
        }

        public void loadstring(string load)
        {
            string[] SplitArray = load.Split(':');
            resourceName = SplitArray[0];
            amount = Convert.ToDouble(SplitArray[1]);
        }
    }
}