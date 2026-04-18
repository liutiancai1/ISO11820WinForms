using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Serilog;

namespace ISO11820WinForms.Utilities
{
    /// <summary>
    /// 资源文件辅助类
    /// 用于统一管理应用程序的图标、图片等资源文件的加载
    /// </summary>
    public static class ResourceHelper
    {
        private static readonly string ResourcesPath = Path.Combine(Application.StartupPath, "Resources");

        /// <summary>
        /// 加载应用程序图标
        /// </summary>
        /// <returns>图标对象，如果加载失败返回 null</returns>
        public static Icon? LoadApplicationIcon()
        {
            try
            {
                // 首先尝试加载 ICO 文件
                var iconPath = Path.Combine(ResourcesPath, "app.ico");
                
                if (File.Exists(iconPath))
                {
                    Log.Information("成功加载应用程序图标: {IconPath}", iconPath);
                    return new Icon(iconPath);
                }
                
                // 如果 ICO 不存在，尝试从 furnace.png 创建图标
                var pngPath = Path.Combine(ResourcesPath, "furnace.png");
                if (File.Exists(pngPath))
                {
                    Log.Information("从 PNG 创建应用程序图标: {PngPath}", pngPath);
                    return CreateIconFromImage(pngPath);
                }
                
                Log.Warning("应用程序图标文件不存在，将使用默认图标");
                return null;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "加载应用程序图标失败");
                return null;
            }
        }

        /// <summary>
        /// 从图片文件创建图标
        /// </summary>
        /// <param name="imagePath">图片文件路径</param>
        /// <returns>图标对象</returns>
        private static Icon? CreateIconFromImage(string imagePath)
        {
            try
            {
                using (var bitmap = new Bitmap(imagePath))
                {
                    // 调整大小为 32x32（标准图标尺寸）
                    using (var resized = new Bitmap(bitmap, new Size(32, 32)))
                    {
                        // 从 Bitmap 创建 Icon
                        IntPtr hIcon = resized.GetHicon();
                        Icon icon = Icon.FromHandle(hIcon);
                        
                        // 注意：这里创建的 Icon 需要调用者负责释放
                        return icon;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "从图片创建图标失败: {ImagePath}", imagePath);
                return null;
            }
        }

        /// <summary>
        /// 加载炉子图片
        /// </summary>
        /// <returns>图片对象，如果加载失败返回 null</returns>
        public static Image? LoadFurnaceImage()
        {
            try
            {
                var imagePath = Path.Combine(ResourcesPath, "furnace.png");
                
                if (File.Exists(imagePath))
                {
                    Log.Debug("成功加载炉子图片: {ImagePath}", imagePath);
                    return Image.FromFile(imagePath);
                }
                else
                {
                    Log.Debug("炉子图片文件不存在: {ImagePath}，将使用占位符", imagePath);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "加载炉子图片失败");
                return null;
            }
        }

        /// <summary>
        /// 检查资源文件是否存在
        /// </summary>
        /// <param name="fileName">资源文件名</param>
        /// <returns>如果文件存在返回 true，否则返回 false</returns>
        public static bool ResourceExists(string fileName)
        {
            var filePath = Path.Combine(ResourcesPath, fileName);
            return File.Exists(filePath);
        }

        /// <summary>
        /// 获取资源文件的完整路径
        /// </summary>
        /// <param name="fileName">资源文件名</param>
        /// <returns>资源文件的完整路径</returns>
        public static string GetResourcePath(string fileName)
        {
            return Path.Combine(ResourcesPath, fileName);
        }

        /// <summary>
        /// 确保 Resources 文件夹存在
        /// </summary>
        public static void EnsureResourcesFolderExists()
        {
            try
            {
                if (!Directory.Exists(ResourcesPath))
                {
                    Directory.CreateDirectory(ResourcesPath);
                    Log.Information("创建 Resources 文件夹: {ResourcesPath}", ResourcesPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "创建 Resources 文件夹失败");
            }
        }
    }
}
