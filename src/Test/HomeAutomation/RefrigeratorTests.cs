using Utopia.HomeAutomation;
using System.Reflection;

namespace UtopiaTest.HomeAutomation;

public class RefrigeratorTests
{
    [Fact]
    public async Task GetTemp_AlwaysReturns4()
    {
        var temp = await Refrigerator.GetTemp();
        Assert.Equal(4.0, temp);
    }

    [Fact]
    public async Task GetInternalPicture_ReturnsNonEmptyPngBytes()
    {
        // Run multiple times to check randomness and all files
        var seen = new HashSet<string>();
        var assembly = typeof(Refrigerator).Assembly;
        for (int i = 0; i < 10; i++)
        {
            var bytes = await Refrigerator.GetInternalPicture();
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
            // Check JPG header
            Assert.True(bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[bytes.Length - 2] == 0xFF && bytes[bytes.Length - 1] == 0xD9);
            // Try to identify which file was returned (by size)
            foreach (var file in Refrigerator.PictureFiles)
            {
                var resourceName = $"Utopia.Resources.{file}";
                var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using (stream)
                    {
                        var fileBytes = new byte[stream.Length];
                        stream.Read(fileBytes, 0, fileBytes.Length);
                        if (fileBytes.Length == bytes.Length)
                        {
                            seen.Add(file);
                        }
                    }
                }
            }
        }
        // Should have seen at least 2 different images in 10 tries
        Assert.True(seen.Count >= 2);
    }
}
