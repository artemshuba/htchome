using System.Collections.Generic;

namespace Home.Base.Widgets
{
    public interface IExtensionManager
    {
        IEnumerable<T> GetExtensions<T>() where T : IExtension;
    }
}
