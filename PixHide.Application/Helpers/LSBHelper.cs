namespace PixHide.Application.Helpers;

internal class LSBHelper
{
    public static int[] GetImageIndexes(int n, int seed)
    {
        int[] indexes = new int[n];
        for (int i = 0; i < n; i++) indexes[i] = i;

        var rng = new Random(seed);

        // Fisher-Yates shuffle
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (indexes[i], indexes[j]) = (indexes[j], indexes[i]);
        }

        return indexes;
    }
}
