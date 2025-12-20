using System.Collections.Generic;

namespace Home.Base.Services
{
    public interface ISkinService
    {
        IEnumerable<string> AvailableSkins { get; }
        string CurrentSkin { get; }
        void ApplySkin(string skinName);
    }
}
