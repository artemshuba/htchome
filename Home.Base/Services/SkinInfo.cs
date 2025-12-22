using System;

namespace Home.Base.Services
{
    public class SkinInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public string Author { get; set; } = string.Empty;
        public string PreviewPath { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public string DirectoryPath { get; set; } = string.Empty;
        
        // Helper to get absolute path check
    }
}
