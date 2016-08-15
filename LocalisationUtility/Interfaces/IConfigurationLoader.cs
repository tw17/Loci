using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LocalisationUtility.Models;

namespace LocalisationUtility.Interfaces
{
    public interface IConfigurationLoader
    {
        void SaveConfiguration(Configuration configuration, string configFile);

        Configuration LoadConfiguration(string configFile);
    }
}
