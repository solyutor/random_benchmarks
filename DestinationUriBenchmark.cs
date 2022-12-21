using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace StringComparisonBenchmarks;

[MemoryDiagnoser]
public class DestinationUriBenchmark
{
    private static readonly uint[] ValidPathChars = {
        0b_0000_0000__0000_0000__0000_0000__0000_0000, // 0x00 - 0x1F
        0b_0010_1111__1111_1111__1111_1111__1101_0010, // 0x20 - 0x3F
        0b_1000_0111__1111_1111__1111_1111__1111_1111, // 0x40 - 0x5F
        0b_0100_0111__1111_1111__1111_1111__1111_1110, // 0x60 - 0x7F
    };

    [Benchmark(Baseline = true)]
    public Uri MakeDestinationAddress()
    {
        return MakeDestinationAddress("http://localhost:8100", "/api/v1/blogs/", new QueryString("?id=1"));
    }
    
    [Benchmark()]
    public Uri MakeDestinationAddressNoAllocation()
    {
        return MakeDestinationAddressNoAllocation("http://localhost:8100", "/api/v1/blogs/", new QueryString("?id=1"));
    }
    
    
    public static Uri MakeDestinationAddress(string destinationPrefix, PathString path, QueryString query)
    {
        ReadOnlySpan<char> prefixSpan = destinationPrefix;

        if (path.HasValue && destinationPrefix.EndsWith('/'))
        {
            // When PathString has a value it always starts with a '/'. Avoid double slashes when concatenating.
            prefixSpan = prefixSpan[0..^1];
        }

        var targetAddress = string.Concat(prefixSpan, EncodePath(path), query.ToUriComponent());

        return new Uri(targetAddress, UriKind.Absolute);
    }
    
    public static Uri MakeDestinationAddressNoAllocation(string destinationPrefix, PathString path, QueryString query)
    {
        string prefix = destinationPrefix;

        if (path.HasValue && destinationPrefix.EndsWith('/'))
        {
            // When PathString has a value it always starts with a '/'. Avoid double slashes when concatenating.
            prefix = prefix[0..^1];
        }

        var targetAddress = string.Concat(prefix, EncodePath(path), query.ToUriComponent());

        return new Uri(targetAddress, UriKind.Absolute);
    }
    
    
    private static string EncodePath(PathString path)
    {
        if (!path.HasValue)
        {
            return string.Empty;
        }

        // Check if any escaping is required.
        var value = path.Value!;
        for (var i = 0; i < value.Length; i++)
        {
            if (!IsValidPathChar(value[i]))
            {
                return EncodePath(value, i);
            }
        }

        return value;
    }
    
    private static string EncodePath(string value, int i)
    {
        StringBuilder? buffer = null;

        var start = 0;
        var count = i;
        var requiresEscaping = false;

        while (i < value.Length)
        {
            if (IsValidPathChar(value[i]))
            {
                if (requiresEscaping)
                {
                    // the current segment requires escape
                    buffer ??= new StringBuilder(value.Length * 3);
                    buffer.Append(Uri.EscapeDataString(value.Substring(start, count)));

                    requiresEscaping = false;
                    start = i;
                    count = 0;
                }

                count++;
                i++;
            }
            else
            {
                if (!requiresEscaping)
                {
                    // the current segment doesn't require escape
                    buffer ??= new StringBuilder(value.Length * 3);
                    buffer.Append(value, start, count);

                    requiresEscaping = true;
                    start = i;
                    count = 0;
                }

                count++;
                i++;
            }
        }

        if (count == value.Length && !requiresEscaping)
        {
            return value;
        }
        else
        {
            if (count > 0)
            {
                buffer ??= new StringBuilder(value.Length * 3);

                if (requiresEscaping)
                {
                    buffer.Append(Uri.EscapeDataString(value.Substring(start, count)));
                }
                else
                {
                    buffer.Append(value, start, count);
                }
            }

            return buffer?.ToString() ?? string.Empty;
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsValidPathChar(char c)
    {
        // Use local array and uint .Length compare to elide the bounds check on array access
        var validChars = ValidPathChars;
        var i = (int)c;

        // Array is in chunks of 32 bits, so get offset by dividing by 32
        var offset = i >> 5; // i / 32;
        // Significant bit position is the remainder of the above calc; i % 32 => i & 31
        var significantBit = 1u << (i & 31);

        // Check offset in bounds and check if significant bit set
        return (uint)offset < (uint)validChars.Length &&
               ((validChars[offset] & significantBit) != 0);
    }
}