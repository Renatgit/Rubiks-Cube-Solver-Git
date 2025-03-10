using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RubiksCubeSim
{
    public class MoveNode
    {
        public string Move { get; set; }
        public MoveNode Parent { get; set; }

        public MoveNode(string move, MoveNode parent = null)
        {
            Move = move;
            Parent = parent;
        }
    }
}
