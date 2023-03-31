using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets
{
    public class Cell
    {
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int Value { get; set; }
        public List<Cell> Neighbours { get; set; }
        public bool Visited { get; set; }
        public Cell(int x, int y, int value, bool visited = false)
        {
            PosX = x;
            PosY = y;
            Value = value;
            Neighbours = new List<Cell>();
            Visited = visited;
        }
    }
}
