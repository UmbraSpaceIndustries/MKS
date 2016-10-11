namespace KolonyTools
{
    public class MKSAutoRepair : PartModule
    {
        //This is a simple module that automatically converts recyclables into machinery.
        //It's primary use is for MKS Lite.
        public override void OnStart(StartState state)
        {
            if (!part.Resources.Contains("Machinery"))
                return;
            if (!part.Resources.Contains("Recyclables"))
                return;
            if (part.FindModulesImplementing<BaseConverter>().Count == 0)
                return;

            _convertResources = true;
            _inRes = part.Resources["Recyclables"];
            _outRes = part.Resources["Machinery"];

        }

        private PartResource _inRes;
        private PartResource _outRes;
        private bool _convertResources;

        public void FixedUpdate()
        {
            if (_convertResources && _inRes.amount > 0)
            {
                var amt = _inRes.amount;
                _inRes.amount -= amt;
                _outRes.amount += amt;
                if (_inRes.amount < 0)
                    _inRes.amount = 0;
                if (_outRes.amount > _outRes.maxAmount)
                    _outRes.amount = _outRes.maxAmount;
            }
        }
    }

    public class MksAutoRepair : PartModule
    {
        //This is a simple module that automatically converts recyclables into machinery.
        //It's primary use is for MKS Lite.
        public override void OnStart(StartState state)
        {
            if (!part.Resources.Contains("Machinery"))
                return;
            if (!part.Resources.Contains("Recyclables"))
                return;
            if (part.FindModulesImplementing<BaseConverter>().Count == 0)
                return;

            _convertResources = true;
            _inRes = part.Resources["Recyclables"];
            _outRes = part.Resources["Machinery"];

        }

        private PartResource _inRes;
        private PartResource _outRes;
        private bool _convertResources;

        public void FixedUpdate()
        {
            if (_convertResources && _inRes.amount > 0)
            {
                var amt = _inRes.amount;
                _inRes.amount -= amt;
                _outRes.amount += amt;
                if (_inRes.amount < 0)
                    _inRes.amount = 0;
                if (_outRes.amount > _outRes.maxAmount)
                    _outRes.amount = _outRes.maxAmount;
            }
        }
    }
}