namespace KolonyTools
{
    /// <summary>
    /// A resource that is being transferred via an <see cref="OrbitalLogisticsTransferRequest"/>.
    /// </summary
    public class OrbitalLogisticsTransferRequestResource : IConfigNode
    {
        #region Local instance variables
        protected PartResourceDefinition _resourceDefinition;

        [Persistent(name = "ResourceDefinitionId")]
        protected int _resourceId;
        #endregion

        #region Public instance properties
        // Public properties are basic C# types to facilitate serialization for save files
        [Persistent]
        public double TransferAmount = 0;

        public PartResourceDefinition ResourceDefinition
        {
            get
            {
                if (_resourceDefinition == null)
                    _resourceDefinition = PartResourceLibrary.Instance.GetDefinition(_resourceId);

                return _resourceDefinition;
            }
            set
            {
                _resourceDefinition = value;
                _resourceId = value.id;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Don't use this constructor. It's here to support instantiation via <see cref="ConfigNode"/>.CreateObjectFromConfig.
        /// </summary>
        public OrbitalLogisticsTransferRequestResource() { }

        /// <summary>
        /// Use this constructor.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="transferAmount"></param>
        public OrbitalLogisticsTransferRequestResource(PartResourceDefinition resource, double transferAmount = 0)
        {
            ResourceDefinition = resource;
            TransferAmount = transferAmount;
        }
        #endregion

        #region Public instance methods
        /// <summary>
        /// Calculates the mass of the resource transfer.
        /// </summary>
        /// <returns></returns>
        public double Mass()
        {
             return ResourceDefinition.density * TransferAmount;
        }

        /// <summary>
        /// Implementation of <see cref="IConfigNode.Load(ConfigNode)"/>.
        /// </summary>
        /// <param name="node"></param>
        public void Load(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);
        }

        /// <summary>
        /// Implementation of <see cref="IConfigNode.Save(ConfigNode)"/>.
        /// </summary>
        /// <param name="node"></param>
        public void Save(ConfigNode node)
        {
            ConfigNode.CreateConfigFromObject(this, node);
        }
        #endregion
    }
}