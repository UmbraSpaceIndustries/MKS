using System.Collections.Generic;

namespace WOLFUI
{
    public class PassengerMetadataComparer : Comparer<PassengerMetadata>
    {
        public override int Compare(PassengerMetadata x, PassengerMetadata y)
        {
            return x.DisplayName.CompareTo(y.DisplayName);
        }
    }

    public class PassengerMetadata
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool IsTourist { get; set; }
        public string Occupation { get; set; }
        public int Stars { get; set; }
    }
}
