using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using RubiksCubeSim;
using System.IO;
using Newtonsoft.Json;

namespace RubiksCubeSim
{
    // Stores permutation and orientation changes maps for each move.
    public class MoveData 
    {
        public int[] CornerPermutation { get; set; }
        public int[] CornerOrientationToIndex { get; set; }
        public int[] CornerOrientationFromIndex { get; set; }
        public int[] FullEdgePermutation { get; set; }
        public int[] EdgeOrientationToIndex { get; set; }
        public int[] EdgeOrientationFromIndex { get; set; }

        public MoveData(int[] cornerPerm, int[] cornerOrientationToIndex = null, int[] cornerOrientationFromIndex = null, int[] fullEdgePermutation = null, int[] edgeOrientationToIndex = null, int[] edgeOrientationFromIndex = null)
        {
            CornerPermutation = cornerPerm;
            CornerOrientationToIndex = cornerOrientationToIndex;
            CornerOrientationFromIndex = cornerOrientationFromIndex;
            FullEdgePermutation = fullEdgePermutation;
            EdgeOrientationToIndex = edgeOrientationToIndex;
            EdgeOrientationFromIndex = edgeOrientationFromIndex;
        }
    }
}
