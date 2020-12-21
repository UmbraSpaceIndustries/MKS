using System.Collections.Generic;

namespace WOLF.Tests.Unit.Mocks
{
    public class TestDepot : Depot
    {
        public TestDepot() : base("Mun", "East Crater")
        {
        }

        /// <summary>
        /// Exposes the internal resource stream cache for test classes
        /// </summary>
        public Dictionary<string, IResourceStream> Resources
        {
            get { return _resourceStreams; }
        }

        public static string DEPOT_NODE_NAME
        {
            get { return _depotNodeName; }
        }

        public static string STREAM_NODE_NAME
        {
            get { return _streamNodeName; }
        }
    }
}
