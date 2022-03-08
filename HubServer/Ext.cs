using System.Text;

namespace Hub;

public static class Ext
{

    public static string ToSafePath(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            throw new ArgumentNullException(nameof(str));

        var builder = new StringBuilder();
        foreach (var ch in str)
        {
            if (!Path.GetInvalidFileNameChars().Contains(ch))
                builder.Append(ch);
        }

        if (builder.Length == 0)
            throw new InvalidOperationException($"No characters remain sanitising the str '{str}'");

        return builder.ToString();
    }

}