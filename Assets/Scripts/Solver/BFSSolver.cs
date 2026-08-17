using Assets.Scripts.Core;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


namespace Assets.Scripts.Solver
{
	public static class BFSSolver
	{
        private class SearchNode
        {
            public CubeStateData State { get; private set; }
            public List<string> SolutionPath { get; private set; }
            public string PreviousMove { get; private set; }
            public int Depth { get; private set; }

            public SearchNode(CubeStateData state, List<string> solutionPath, string previousMove, int depth)
            {
                State = state;
                SolutionPath = solutionPath;
                PreviousMove = previousMove;
                Depth = depth;
            }
        }

		public static List<string> Solve(CubeStateData startState, int maxDepth)
		{
			Queue<SearchNode> queue = new Queue<SearchNode>();
			HashSet<string> visitedStates = new HashSet<string>();

            CubeStateData start = CubeState.CloneState(startState);
            SearchNode startNode = new SearchNode(start, new List<string>(), null, 0);

			queue.Enqueue(startNode);
			visitedStates.Add(CubeStateUtility.GetStateKey(start));

			while (queue.Count > 0)
			{
				SearchNode currentNode = queue.Dequeue();
				if (CubeStateUtility.IsSolved(currentNode.State))
                {
                    return currentNode.SolutionPath;
                }

				if (currentNode.Depth >= maxDepth)
                {
                    continue;
                }

                foreach (string move in MoveGenerator.GetValidMoves(currentNode.PreviousMove))
                {
                    CubeStateData child = CubeState.CloneState(currentNode.State);
                    MoveProcessor.ApplyMove(child, move, false);

                    string childKey = CubeStateUtility.GetStateKey(child);
                    if (!visitedStates.Contains(childKey))
                    {
                        List<string> childSolutionPath = new List<string>(currentNode.SolutionPath);
                        childSolutionPath.Add(move);

                        visitedStates.Add(childKey);
                        queue.Enqueue(new SearchNode(child, childSolutionPath, move, currentNode.Depth + 1));
                    }
                }
            }
			return null;
        }
	}
}
