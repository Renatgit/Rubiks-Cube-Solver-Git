using UnityEngine;
using System.Collections;
using NUnit.Framework;
using Assets.Scripts.Core;
using System.Collections.Generic;

namespace Assets.Scripts.Solver
{
	public class BFSSolver: MonoBehaviour
	{
		public static List<string> Solve(CubeStateData startState, int maxDepth)
		{
			Queue<CubeStateData> queue = new Queue<CubeStateData>();
			HashSet<string> visitedStates = new HashSet<string>();

            CubeStateData start = CubeState.CloneState(startState);
			start.depth = 0;
			start.solution = new List<string>();

			queue.Enqueue(start);
			visitedStates.Add(CubeStateUtility.GetStateKey(start));

			while (queue.Count > 0)
			{
				CubeStateData currentState = queue.Dequeue();
				if (CubeStateUtility.IsSolved(currentState))
                {
                    return currentState.solution;
                }

				if (currentState.depth >= maxDepth)
                {
                    continue;
                }
				
				List<CubeStateData> children = MoveGenerator.GenerateChildren(currentState);
                foreach (CubeStateData child in children)
				{
                    string childKey = CubeStateUtility.GetStateKey(child);
                    if (!visitedStates.Contains(childKey))
                    {
                        visitedStates.Add(childKey);
                        queue.Enqueue(child);
                    }
                }
            }
			return null;
        }
	}
}