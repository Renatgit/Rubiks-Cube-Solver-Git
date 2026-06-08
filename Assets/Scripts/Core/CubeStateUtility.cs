using System.Collections.Generic;
using System.Text;

public static class CubeStateUtility
{
    public static bool IsSolved(CubeStateData state)
    {
        return AreEqual(state, CubeState.CreateSolvedState());
    }

    public static bool AreEqual(CubeStateData a, CubeStateData b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        return ListsMatch(a.cornerPermutation, b.cornerPermutation)
            && ListsMatch(a.cornerOrientation, b.cornerOrientation)
            && ListsMatch(a.fullEdgePermutation, b.fullEdgePermutation)
            && ListsMatch(a.fullEdgeOrientation, b.fullEdgeOrientation);
    }

    public static string GetStateKey(CubeStateData state)
    {
        if (state == null)
        {
            return "";
        }

        StringBuilder key = new StringBuilder();

        AppendList(key, state.cornerPermutation);
        AppendList(key, state.cornerOrientation);
        AppendList(key, state.fullEdgePermutation);
        AppendList(key, state.fullEdgeOrientation);

        return key.ToString();
    }

    public static string GetComparisonSummary(CubeStateData a, CubeStateData b)
    {
        if (a == null || b == null)
        {
            return "One or both states are null.";
        }

        return "cornerPermutation=" + ListsMatch(a.cornerPermutation, b.cornerPermutation)
            + ", cornerOrientation=" + ListsMatch(a.cornerOrientation, b.cornerOrientation)
            + ", fullEdgePermutation=" + ListsMatch(a.fullEdgePermutation, b.fullEdgePermutation)
            + ", fullEdgeOrientation=" + ListsMatch(a.fullEdgeOrientation, b.fullEdgeOrientation)
            + ", firstEightEdgesAlignment=" + DictionariesMatch(a.firstEightEdgesAlignment, b.firstEightEdgesAlignment)
            + ", lastFourEdgeAlignment=" + DictionariesMatch(a.lastFourEdgeAlignment, b.lastFourEdgeAlignment);
    }

    private static bool ListsMatch(List<int> a, List<int> b)
    {
        if (a == null || b == null || a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool DictionariesMatch(Dictionary<int, string> a, Dictionary<int, string> b)
    {
        if (a == null || b == null || a.Count != b.Count)
        {
            return false;
        }

        foreach (KeyValuePair<int, string> pair in a)
        {
            if (!b.ContainsKey(pair.Key) || b[pair.Key] != pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static void AppendList(StringBuilder key, List<int> values)
    {
        if (values != null)
        {
            key.Append(string.Join(",", values));
        }

        key.Append("|");
    }
}
