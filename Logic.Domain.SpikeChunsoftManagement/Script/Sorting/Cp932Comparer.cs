using System.Text;

namespace Logic.Domain.SpikeChunsoftManagement.Script.Sorting;

public sealed class Cp932Comparer : IComparer<string>
{
    private static readonly Encoding Cp932 = Encoding.GetEncoding(932);

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) 
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return 1;

        byte[] xb = Cp932.GetBytes(x);
        byte[] yb = Cp932.GetBytes(y);

        int len = Math.Min(xb.Length, yb.Length);

        for (var i = 0; i < len; i++)
        {
            int diff = xb[i] - yb[i];
            
            if (diff != 0)
                return diff;
        }

        return xb.Length - yb.Length;
    }
}