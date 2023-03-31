using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets
{
    public class Crawler
    {
        public int CurrentX { get; set; }
        public int CurrentY { get; set; }

        public static Cell[,] GenerateMapToCrawl(int[,] maze)
        {
            //I je GOR DOL, J je LEVO DESNO
            int height = maze.GetLength(0);
            int width = maze.GetLength(1);
            Cell[,] newMaze = new Cell[height, width];

            //Preslikam 2d array 1 in 0 v array celic
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Cell newC = new Cell(x, y, maze[y, x]);
                    newMaze[y, x] = newC;
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Cell temp = newMaze[y, x];
                    if (temp.Value == 0 && temp.Visited == false)
                    {
                        //Prazno polje oz ni zid
                        GetNeighbours(ref temp, ref newMaze);
                    }
                }
            }
            return newMaze;
        }

        private static void GetNeighbours(ref Cell newC, ref Cell[,] newMaze)
        {
            ///TODO: naredi crawlerja ki gre čez vse možne pozicije (ki niso 1) in preverja ali je zaprt prostor ali ne
            if (newC.PosX > 0)
            {
                if (newMaze[newC.PosY, newC.PosX - 1].Value == 0)
                {
                    newC.Neighbours.Add(newMaze[newC.PosY, newC.PosX - 1]);
                }
            }
            if (newC.PosX < newMaze.GetLength(1) - 1)
            {
                if (newMaze[newC.PosY, newC.PosX + 1].Value == 0)
                {
                    newC.Neighbours.Add(newMaze[newC.PosY, newC.PosX + 1]);
                }
            }
            if (newC.PosY > 0)
            {
                if (newMaze[newC.PosY - 1, newC.PosX].Value == 0)
                {
                    newC.Neighbours.Add(newMaze[newC.PosY - 1, newC.PosX]);
                }
            }
            if (newC.PosY < newMaze.GetLength(0) - 1)
            {
                if (newMaze[newC.PosY + 1, newC.PosX].Value == 0)
                {
                    newC.Neighbours.Add(newMaze[newC.PosY + 1, newC.PosX]);
                }
            }
        }

        public static List<Cell> CheckNeighbours(ref Cell c, ref Cell[,] maze)
        {
            List<Cell> listToReturn = new List<Cell>();
            c.Visited = true;
            //Če ima ta celica neobiskane sosede
            List<Cell> nonVisitedNeighbours = c.Neighbours.Where(x => x.Visited == false).ToList();
            //Grem čez vse neobiskane sosede in se rekurzivno sprehajam do vseh sosedov dokler niso vsi obiskani
            for (int i = 0; i < nonVisitedNeighbours.Count; i++)
            {
                Cell temp = nonVisitedNeighbours[i];
                if (temp.Visited == false)
                {
                    listToReturn.Add(temp);
                    listToReturn.AddRange(CheckNeighbours(ref temp, ref maze));
                }
            }
            return listToReturn;
        }
    }
}
