using ModelContextProtocol.Server;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace Utopia.HomeAutomation
{
    [McpServerToolType]
    public static class Refrigerator
    {
        public static readonly string[] PictureFiles = new[] { "ref-empty.jpg", "ref-full.jpg", "ref-half.jpg" };
        public static readonly Random Rng = new();
        public static readonly string ResourceNamespace = "Utopia.Resources";

        [McpServerTool(Name = "refrigerator_get_temp", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets the current temperature of the refrigerator.")]
        public static Task<double> GetTemp()
        {
            return Task.FromResult(4.0);
        }

        [McpServerTool(Name = "refrigerator_get_internal_picture", Destructive = false, OpenWorld = false, ReadOnly = true, Idempotent = true),
            Description("Gets the internal picture of the refrigerator. Can be used to analyze the stock levels inside.")]
        public static Task<byte[]> GetInternalPicture()
        {
            var file = PictureFiles[Rng.Next(PictureFiles.Length)];
            var resourceName = $"{ResourceNamespace}.{file}";
            var assembly = Assembly.GetExecutingAssembly();
            // Try with and without dash normalization
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                // Try fallback: sometimes resource names use underscores or dots
                foreach (var name in assembly.GetManifestResourceNames())
                {
                    if (name.EndsWith(file, StringComparison.OrdinalIgnoreCase))
                    {
                        stream = assembly.GetManifestResourceStream(name);
                        break;
                    }
                }
            }
            if (stream == null)
                throw new FileNotFoundException($"Embedded resource not found: {file}");
            using (stream)
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return Task.FromResult(ms.ToArray());
            }
        }
    }
}
