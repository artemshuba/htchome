using Home.Base.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Windows;

namespace HTCHome.Widgets
{
    public partial class WidgetContext
    {
        public ResourceDictionary SkinResources => ((WidgetSkinService)SkinService).SkinResources;

        private class WidgetSkinService : ISkinService
        {
            private readonly string _widgetDir;
            private readonly ILogger _logger;
            private readonly IConfigurationService _configuration;

            public ResourceDictionary SkinResources { get; } = new ResourceDictionary();

            public WidgetSkinService(string widgetDir, ILogger logger, IConfigurationService configuration)
            {
                _widgetDir = widgetDir;
                _logger = logger;
                _configuration = configuration;
                LoadSkins();
            }

            public IEnumerable<SkinInfo> AvailableSkins { get; private set; } = new List<SkinInfo>();
            public SkinInfo? CurrentSkin { get; private set; }

            private void LoadSkins()
            {
                var skinsDir = Path.Combine(_widgetDir, "Skins");
                var skinList = new List<SkinInfo>();

                if (Directory.Exists(skinsDir))
                {
                    foreach (var dir in Directory.GetDirectories(skinsDir))
                    {
                        if (File.Exists(Path.Combine(dir, "Skin.xaml")))
                        {
                            var info = ParseSkinMetadata(dir);
                            skinList.Add(info);
                        }
                    }
                }
                
                AvailableSkins = skinList;

                // Initial Load Logic
                var savedSkin = _configuration.GetValue<string>("Skin");
                if (string.IsNullOrEmpty(savedSkin))
                {
                     // Try Default
                     var defaultSkin = AvailableSkins.FirstOrDefault(s => s.IsDefault) ?? AvailableSkins.FirstOrDefault();
                     if (defaultSkin != null) 
                     {
                         ApplySkin(defaultSkin.Name);
                     }
                }
                else
                {
                    ApplySkin(savedSkin);
                }
            }

            private SkinInfo ParseSkinMetadata(string skinDir)
            {
                var dirName = Path.GetFileName(skinDir);
                var info = new SkinInfo 
                { 
                    Name = dirName, 
                    DirectoryPath = skinDir 
                };

                // 1. Try JSON
                var jsonPath = Path.Combine(skinDir, "skin.json");
                if (File.Exists(jsonPath))
                {
                    try
                    {
                        var json = File.ReadAllText(jsonPath);
                         using (var doc = JsonDocument.Parse(json))
                         {
                             var root = doc.RootElement;
                             if (root.TryGetProperty("name", out var nameProp)) info.Name = nameProp.GetString() ?? dirName;
                             if (root.TryGetProperty("version", out var verProp)) info.Version = verProp.GetString() ?? "1.0";
                             if (root.TryGetProperty("author", out var authProp)) info.Author = authProp.GetString() ?? string.Empty;
                             
                             if (root.TryGetProperty("isDefault", out var defProp)) 
                             {
                                 if (defProp.ValueKind == JsonValueKind.True || defProp.ValueKind == JsonValueKind.False)
                                     info.IsDefault = defProp.GetBoolean();
                             }

                             if (root.TryGetProperty("preview", out var prevProp))
                             {
                                  var preview = prevProp.GetString();
                                  if (!string.IsNullOrEmpty(preview))
                                      info.PreviewPath = Path.Combine(skinDir, preview);
                             }
                         }
                         return info; // Prioritize JSON
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to parse skin.json for {dirName}", ex);
                    }
                }

                var xmlPath = Path.Combine(skinDir, "Skin.xml");
                if (File.Exists(xmlPath))
                {
                    try
                    {
                        var doc = System.Xml.Linq.XDocument.Load(xmlPath);
                        var root = doc.Element("Skin");
                        if (root != null)
                        {
                            info.Name = root.Element("Name")?.Value ?? dirName;
                            info.Version = root.Element("Version")?.Value ?? "1.0";
                            info.Author = root.Element("Author")?.Value ?? string.Empty;
                            var isDefault = root.Element("IsDefault")?.Value;
                            if (bool.TryParse(isDefault, out bool def)) info.IsDefault = def;
                            
                             var preview = root.Element("Preview")?.Value;
                             if (!string.IsNullOrEmpty(preview))
                             {
                                 info.PreviewPath = Path.Combine(skinDir, preview);
                             }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to parse Skin.xml for {dirName}", ex);
                    }
                }
                return info;
            }

            public void ApplySkin(string skinName)
            {
                // Find skin by name OR directory name fallback
                var skin = AvailableSkins.FirstOrDefault(s => s.Name == skinName) 
                           ?? AvailableSkins.FirstOrDefault(s => Path.GetFileName(s.DirectoryPath) == skinName);

                if (skin == null)
                {
                    _logger.Warning($"Skin '{skinName}' not found. Trying fallback.");
                    
                    // 1. Try Default
                    skin = AvailableSkins.FirstOrDefault(s => s.IsDefault);
                    
                    // 2. Try First
                    if (skin == null) skin = AvailableSkins.FirstOrDefault();
                    
                    if (skin == null)
                    {
                        _logger.Error("No skins available! Unloading widget.");
                        return; 
                    }
                    
                    _logger.Info($"Fallback to skin '{skin.Name}'");
                }

                var skinPath = Path.Combine(skin.DirectoryPath, "Skin.xaml");
                if (File.Exists(skinPath))
                {
                    try
                    {
                        var skinUri = new Uri(skinPath, UriKind.Absolute);
                        var skinDict = new ResourceDictionary { Source = skinUri };
                        
                        SkinResources.Clear();
                        skinDict["IsWidgetSkin"] = true; 
                        SkinResources.MergedDictionaries.Add(skinDict);
                        
                        CurrentSkin = skin;
                        
                        // Persist
                        _configuration.SetValue("Skin", skin.Name);
                        _configuration.SaveAsync(); // Fire and forget
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to load skin {skin.Name}", ex);
                        throw; 
                    }
                }
            }
            
             public async Task<string> GetSkinContentAsync(string skinName)
             {
                  // logic needs to find directory based on skinName
                  var skin = AvailableSkins.FirstOrDefault(s => s.Name == skinName);
                   if (skin == null) return string.Empty;

                  var skinPath = Path.Combine(skin.DirectoryPath, "Skin.xaml");
                  if (File.Exists(skinPath))
                  {
                      return await File.ReadAllTextAsync(skinPath);
                  }
                  return string.Empty;
             }
 
             public async Task SaveSkinContentAsync(string skinName, string content)
             {
                  var skin = AvailableSkins.FirstOrDefault(s => s.Name == skinName);
                  if (skin == null) return;

                  var skinPath = Path.Combine(skin.DirectoryPath, "Skin.xaml");
                  await File.WriteAllTextAsync(skinPath, content);
             }
 
             public void EditSkin(string skinName)
             {
                 Application.Current.Dispatcher.Invoke(() =>
                 {
                     var editor = new SkinEditorWindow(this, skinName);
                     editor.Show();
                 });
             }
        }
    }


}
