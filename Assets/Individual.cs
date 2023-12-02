using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets
{
    public class Individual
    {
        public int[,] Maze { get; set; }
        public Fitness2 _Fitness { get; set; }
        public string Name { get; set; }
        public string StringMaze { get; set; }

        public Random r;

        public Individual(int Size, string name, Random r)
        {
            this.Name = name;
            this.Maze = new int[Size, Size];
            this.r = r;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    this.Maze[y, x] = r.Next(0, 2);
                }
            }

            this.Maze[0, 0] = 1;
            this.Maze[0, 1] = 0;
            this.Maze[0, 2] = 1;

            this.Maze[Size - 1, Size - 1] = 1;
            this.Maze[Size - 1, Size - 2] = 0;
            this.Maze[Size - 1, Size - 3] = 1;
        }

        public Individual(Individual mama, Individual papa, string name)
        {
            this.Name = name;
            int Size = mama.Maze.GetLength(0);
            this.Maze = new int[Size, Size];

            r = mama.r;
            if(mama.r.Next(0, 2) == 0)
                r = papa.r;


            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int chance = r.Next(0, 101);
                    int valueToInsert = 0;

                    if (chance < 45)
                        valueToInsert = mama.Maze[x, y];
                    else if(valueToInsert < 95)
                        valueToInsert = papa.Maze[x, y];
                    else
                        valueToInsert = r.Next(0, 2);

                    this.Maze[y, x] = valueToInsert;
                }
            }
        }

        public void Grade(decimal borderWeight, decimal qualityWeight, decimal connectivityWeight, decimal shortnessWeight, decimal deadendWeight, decimal loopWeight)
        {
            //_Fitness = new Fitness2(this) { 
            //    BorderWeight = borderWeight, 
            //    QualityWeight = qualityWeight, 
            //    ConnectivityWeight = connectivityWeight,
            //    ShortnessWeight = shortnessWeight,
            //    DeadendWeight = deadendWeight,
            //    LoopWeight = loopWeight
            //};
            _Fitness = new Fitness2(borderWeight, connectivityWeight, deadendWeight, loopWeight, qualityWeight, shortnessWeight);
            _Fitness.CaluclateScores(this);
        }

        public decimal GetScore()
        {
            return _Fitness.Score;
        }

        public static string GetStringMaze(int[,] maze)
        {
            string rez = "";
            int size = maze.GetLength(0);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    rez += maze[y, x];
                }
            }
            return rez;
        }

    }
}