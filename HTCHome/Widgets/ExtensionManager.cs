using Home.Base.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HTCHome.Widgets
{
    public class ExtensionManager : IExtensionManager
    {
        private readonly List<IExtension> _extensions = new();

        public ExtensionManager()
        {
            // TODO: Load extensions from DLLs
        }

        public void RegisterExtension(IExtension extension)
        {
            _extensions.Add(extension);
        }

        public IEnumerable<T> GetExtensions<T>() where T : IExtension
        {
            return _extensions.OfType<T>();
        }
    }
}
