using System;

namespace Textie.Core.Configuration
{
    public class ConfigurationManager
    {
        private SpamConfiguration _currentConfiguration;

        public ConfigurationManager()
        {
            _currentConfiguration = new SpamConfiguration();
        }

        public SpamConfiguration GetConfiguration()
        {
            return _currentConfiguration.Clone();
        }

        public void UpdateConfiguration(SpamConfiguration configuration)
        {
            if (configuration?.IsValid() == true)
            {
                _currentConfiguration = configuration.Clone();
            }
            else
            {
                throw new ArgumentException("Configuration is invalid", nameof(configuration));
            }
        }

        public void ResetToDefaults()
        {
            _currentConfiguration = new SpamConfiguration();
        }
    }
}
