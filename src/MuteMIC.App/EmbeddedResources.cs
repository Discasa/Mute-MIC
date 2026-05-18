using System.Reflection;

namespace MuteMIC.App;

internal static class EmbeddedResources
{
    public static Icon LoadIcon(string name)
    {
        using Stream stream = Open($"Icons.{name}");
        using Icon icon = new(stream);
        return (Icon)icon.Clone();
    }

    public static byte[] LoadBytes(string logicalName)
    {
        using Stream stream = Open(logicalName);
        using MemoryStream memory = new();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static Stream Open(string logicalName)
    {
        Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(logicalName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Embedded resource not found: {logicalName}");
        }

        return stream;
    }
}
