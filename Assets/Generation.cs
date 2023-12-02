using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets
{
    public class Generation
    {
        public List<Individual> Individuals;
        public string Name { get; set; }

        int[,] AverageMaze;

        public Generation(string name)
        {
            Individuals = new List<Individual>();
            this.Name = name;
        }

        public void GenerateAverageMaze()
        {
            int Size = Individuals[0].Maze.GetLength(0);
            AverageMaze = new int[Size, Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int temp = 0;
                    int countOfIndividualsWithPathAtPosition = Individuals.Where(individual => individual.Maze[y, x] == 1).ToList().Count;
                    if (countOfIndividualsWithPathAtPosition > Individuals.Count / 2)
                    {
                        AverageMaze[y, x] = 1;
                    }
                }
            }
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            // TODO: write your implementation of Equals() here
            throw new NotImplementedException();
            return base.Equals(obj);
        }
    }
}
