using System.Reflection;
using System.Text.Json;

namespace EndNoteStyleToolbox;

internal static class EmbeddedAssets
{
    private const string Prefix = "EndNoteStyleToolbox.Assets.";
    private static readonly Assembly Assembly = typeof(EmbeddedAssets).Assembly;

    public static IReadOnlyList<StyleInfo> LoadManifest()
    {
        using var stream = OpenRequired(Prefix + "style-manifest.json");
        var styles = JsonSerializer.Deserialize<List<StyleInfo>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException("内置样式清单为空。");

        if (styles.Count == 0)
        {
            throw new InvalidDataException("内置样式清单不应为空。");
        }

        if (!styles.Select(style => style.Number).SequenceEqual(Enumerable.Range(1, styles.Count)))
        {
            throw new InvalidDataException("内置样式序号必须从1开始连续排列。");
        }

        if (styles.Select(style => style.Filename).Distinct(StringComparer.OrdinalIgnoreCase).Count() != styles.Count)
        {
            throw new InvalidDataException("内置样式清单含有重复文件名。");
        }

        foreach (var style in styles)
        {
            ValidateFilename(style.Filename);
            if (string.IsNullOrWhiteSpace(style.Title) || string.IsNullOrWhiteSpace(style.Sha256))
            {
                throw new InvalidDataException("样式清单含有不完整记录。");
            }
        }

        return styles.OrderBy(style => style.Number).ToArray();
    }

    public static byte[] ReadStyle(string filename)
    {
        ValidateFilename(filename);
        using var stream = OpenRequired(Prefix + "Styles." + filename);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    public static string ExportSupportFiles(string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);
        Export(Prefix + "TestMatrix.xlsx", Path.Combine(targetDirectory, "格式库测试矩阵.xlsx"));
        Export(Prefix + "TestReferences.ris", Path.Combine(targetDirectory, "64条标准测试文献.ris"));
        Export(Prefix + "ReferenceTypes.xml", Path.Combine(targetDirectory, "统一多语种文献类型.xml"));
        Export(Prefix + "ThirdPartyNotices.md", Path.Combine(targetDirectory, "THIRD_PARTY_NOTICES.md"));
        return targetDirectory;
    }

    public static string[] ResourceNames() => Assembly.GetManifestResourceNames();

    private static Stream OpenRequired(string name) =>
        Assembly.GetManifestResourceStream(name)
        ?? throw new FileNotFoundException($"缺少内置资源：{name}");

    private static void Export(string resourceName, string path)
    {
        using var input = OpenRequired(resourceName);
        using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        input.CopyTo(output);
    }

    private static void ValidateFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename) ||
            !filename.Equals(Path.GetFileName(filename), StringComparison.Ordinal) ||
            !filename.EndsWith(".ens", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"非法样式文件名：{filename}");
        }
    }
}
