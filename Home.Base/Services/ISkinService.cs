using System.Collections.Generic;
using System.Threading.Tasks;

namespace Home.Base.Services
{
    public interface ISkinService
    {
        IEnumerable<SkinInfo> AvailableSkins { get; }
        SkinInfo? CurrentSkin { get; }
        void ApplySkin(string skinName);
        
        // Editor Support
        Task<string> GetSkinContentAsync(string skinName);
        Task SaveSkinContentAsync(string skinName, string content);
        void EditSkin(string skinName);
    }
}
