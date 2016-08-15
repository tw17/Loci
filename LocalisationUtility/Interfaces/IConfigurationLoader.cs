using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loci.Models;

namespace Loci.Interfaces
{
    public interface IConfigurationLoader
    {
        void SaveConfiguration(Configuration configuration, string configFile);

        Configuration LoadConfiguration(string configFile);
    }
}
