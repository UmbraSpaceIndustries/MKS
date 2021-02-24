using System.Collections.Generic;

namespace WOLF
{
    public class PassengerComparer : Comparer<IPassenger>
    {
        public override int Compare(IPassenger x, IPassenger y)
        {
            var touristComparison = x.IsTourist.CompareTo(y.IsTourist);
            if (touristComparison != 0)
            {
                return touristComparison;
            }
            return x.Name.CompareTo(y.Name);
        }
    }

    public class Passenger : IPassenger
    {
        private const string PASSENGER_NODE_NAME = "PASSENGER";

        public string DisplayName { get; private set; }
        public string Name { get; private set; }
        public bool IsTourist { get; private set; }
        public string Occupation { get; private set; }
        public int Stars { get; private set; }

        public Passenger(
            string name,
            string displayName,
            string occupation,
            bool isTourist,
            int stars)
        {
            Name = name;
            DisplayName = displayName;
            Occupation = occupation;
            IsTourist = isTourist;
            Stars = stars;
        }

        public Passenger(ProtoCrewMember kerbal)
        {
            Name = kerbal.name;
            DisplayName = kerbal.displayName;
            Occupation = kerbal.experienceTrait.Title;
            IsTourist = kerbal.type == ProtoCrewMember.KerbalType.Tourist;
            Stars = kerbal.experienceLevel;
        }

        /// <summary>
        /// This constructor is used by the persistence layer.
        /// Use one of the other constructors elsewhere in code.
        /// </summary>
        public Passenger()
        {
        }

        public void OnLoad(ConfigNode node)
        {
            Name = node.GetValue(nameof(Name));
            DisplayName = node.GetValue(nameof(DisplayName));
            IsTourist = bool.Parse(node.GetValue(nameof(IsTourist)));
            Occupation = node.GetValue(nameof(Occupation));
            Stars = int.Parse(node.GetValue(nameof(Stars)));
        }

        public void OnSave(ConfigNode node)
        {
            var passengerNode = node.AddNode(PASSENGER_NODE_NAME);
            passengerNode.AddValue(nameof(Name), Name);
            passengerNode.AddValue(nameof(DisplayName), DisplayName);
            passengerNode.AddValue(nameof(IsTourist), IsTourist);
            passengerNode.AddValue(nameof(Occupation), Occupation);
            passengerNode.AddValue(nameof(Stars), Stars);
        }
    }
}
