﻿using System.Collections.Generic;
using System.Linq;

namespace WOLF.Tests.Unit.Mocks
{
    public class TestPersister : ScenarioPersister
    {
        /// <summary>
        /// Exposes the depot list for testing.
        /// </summary>
        public List<IDepot> Depots
        {
            get { return base.Depots; }
        }

        /// <summary>
        /// Exposes the route list for testing.
        /// </summary>
        public List<TestRoute> Routes => Routes.Select(r => new TestRoute(
                r.OriginBody,
                r.OriginBiome,
                r.DestinationBody,
                r.DestinationBiome,
                r.Payload,
                this,
                r.GetResources()))
            .ToList();
    }
}
