using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets
{
    public class Fitness
    {
        float _outerWallFitness;
        public float OuterWallFitness 
        {
            get 
            {
                return _outerWallFitness * OuterWallFitnessWeight;
            }
            set 
            {
                _outerWallFitness = value;
            } 
        }
        public float OuterWallFitnessWeight { get; set; } = 1;

        float _openSpacesFitness;
        public float OpenSpacesFitness
        {
            get
            {
                return _openSpacesFitness * OpenSpacesFitnessWeight;
            }
            set
            {
                _openSpacesFitness = value;
            }
        }
        public float OpenSpacesFitnessWeight { get; set; } = 1;

        float _closedSpacesFitness;
        public float ClosedSpacesFitness
        {
            get
            {
                return _closedSpacesFitness * ClosedSpacesFitnessWeight;
            }
            set
            {
                _closedSpacesFitness = value;
            }
        }
        public float ClosedSpacesFitnessWeight { get; set; } = 1;

        float _deadEndsFitness;
        public float DeadEndsFitness
        {
            get
            {
                return _deadEndsFitness * DeadEndsFitnessWeight;
            }
            set
            {
                _deadEndsFitness = value;
            }
        }
        public float DeadEndsFitnessWeight { get; set; } = 1;

        public float Score { 
            get 
            {
                return OpenSpacesFitness + ClosedSpacesFitness + DeadEndsFitness + OuterWallFitness;
            } 
        }
        public Builder builder { get; set; }
        public string MazeFileName { get; set; }

        public static Fitness CheckFitness(int[,] maze)
        {
            Fitness fit = new Fitness();
            int height = maze.GetLength(0);
            int width = maze.GetLength(1);

            int numOpenSpaces = 0;
            int maxNumOfOpenSpaces = (width - 2) * (height - 2);
            float openSpacesFitness = 0;

            int numOfOuterWalls = 0;
            int maxNumOfOuterWalls = width * 2 + (height - 2) * 2;
            float outerWallFitness = 0;

            //Izračunam max število zaprtih prostorov (načeloma če bi bilo polje šahovnica je najslabši primer)
            /*  ⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛
                ⬛⬜⬛⬜⬛⬜⬛⬜⬛⬜⬛
                ⬛⬛⬜⬛⬜⬛⬜⬛⬜⬛⬛
                ⬛⬜⬛⬜⬛⬜⬛⬜⬛⬜⬛
                ⬛⬛⬜⬛⬜⬛⬜⬛⬜⬛⬛
                ⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛
             */
            float maxNumOfClosedSpaces = (float)((float)(height - 2) * (float)(width - 2) / 2f);
            int numOfClosedSpaces = 0;
            float closedSpacesFitness = 0;

            //Dead end se smatram, da je celica ki je na treh od štirih mestih obdana z zidom in jih je isto veliko kot šahovnica
            //le namesto da je vsaka vrstica zamaknjena za ena je vsaka druga zamaknjena za ena
            /*  ⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛
                ⬛⬜⬛⬜⬛⬜⬛⬜⬛⬜⬛
                ⬛⬜⬛⬜⬛⬜⬛⬜⬛⬜⬛
                ⬛⬛⬜⬛⬜⬛⬜⬛⬜⬛⬛
                ⬛⬛⬜⬛⬜⬛⬜⬛⬜⬛⬛
                ⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛
             */
            float maxNumOfDeadEnds = (float)((float)(height - 2) * (float)(width - 2) / 2f);
            int numOfDeadEnds = 0;
            float deadEndsFitness = 0;

            //Preverjamo zgornji in spodnji rob
            #region OUTER_WALL_FITNESS
            for (int i = 0; i < width; i++)
            {
                if (maze[0, i] == 1)
                {
                    numOfOuterWalls++;
                }
                if (maze[height - 1, i] == 1)
                {
                    numOfOuterWalls++;
                }
            }
            for (int i = 1; i < height - 1; i++)
            {
                if (maze[i, 0] == 1)
                {
                    numOfOuterWalls++;
                }
                if (maze[i, width - 1] == 1)
                {
                    numOfOuterWalls++;
                }
            }
            outerWallFitness = (float)numOfOuterWalls / (float)maxNumOfOuterWalls;
            if (outerWallFitness == 1)
            {
                outerWallFitness = 0;
            }
            #endregion

            #region OPEN_SPACES_FITNESS
            for (int i = 1; i < height - 1; i++)
            {
                for (int j = 1; j < width - 1; j++)
                {
                    /*
                     ABC
                     D F
                     GHI
                     */
                    //Gremo čez vsa polja ki niso na robu, ter preverimo ali ima celica zid okoli sebe in če ga nima je odprt prostor
                    if (
                        maze[i + 1, j + 1] == 0 &&  //I
                        maze[i + 1, j - 1] == 0 &&  //G
                        maze[i + 1, j] == 0 &&      //H 
                        maze[i - 1, j + 1] == 0 &&  //C
                        maze[i - 1, j] == 0 &&      //B
                        maze[i - 1, j - 1] == 0 &&  //A
                        maze[i, j + 1] == 0 &&      //F
                        maze[i, j - 1] == 0         //D
                        )
                    {
                        numOpenSpaces++;
                    }
                }
            }
            int temp = maxNumOfOpenSpaces - numOpenSpaces;
            openSpacesFitness = (float)temp / (float)maxNumOfOpenSpaces;
            #endregion

            #region CLOSED_SPACES_FITNESS

            Cell[,] newMaze = Crawler.GenerateMapToCrawl(maze);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Cell currentCell = newMaze[y, x];
                    if (currentCell.Value == 0 && currentCell.Visited == false)
                    {
                        //Pridobimo vse celice ki so skupaj povezane (neprekinjena veriga sosedov) in če ni noben sosed na robu 
                        //labirinta (smatram kot izhod) potem je ta postor zaprt
                        currentCell.Visited = true;
                        List<Cell> allNeighbours = Crawler.CheckNeighbours(ref currentCell, ref newMaze);
                        allNeighbours.Add(currentCell);
                        if (!allNeighbours.Where(x => x.PosX == 0 || x.PosX == width - 1 || x.PosY == 0 || x.PosY == height - 1).Any())
                        {
                            numOfClosedSpaces++;
                        }
                    }
                }
            }

            //Ker so lahko zaprti prostori veliki in s tem zmanjšajo število zaprtih prostorov sem se odločil, da število zaprtih prostorov kvadriram
            //in če je kvadrat večji od max števila zpartih prostorov smatram da je št. zaprtih prostorov enako max številu
            float numOfClosedSpacesPow = (float)Math.Pow(numOfClosedSpaces, 2);
            float tempNumClosedSpaces = numOfClosedSpacesPow > maxNumOfClosedSpaces ? maxNumOfClosedSpaces : numOfClosedSpacesPow;

            //Za izračun fitnessa, izračunam kako daleč stran je število zaprtih prostorov od 0 zaprtih prostorov in bližje kot
            //je NULI boljše je
            closedSpacesFitness = ((float)(Math.Abs(maxNumOfClosedSpaces - Math.Abs(0 - tempNumClosedSpaces)) / maxNumOfClosedSpaces));
            #endregion

            #region DEAD_END_FITNESS
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //Preskočimo rob labirinta ker tam ne more biti slepa ulica
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                        continue;

                    if (maze[y, x] == 0)
                    {
                        //Seštejemo vse vrednosti okoli trenutne celice (zid = 1, pot = 0) in če je seđtevek enak 3 pomeni da je okoli
                        //trenutne celice ena pot in 3 zidi
                        int test = maze[y - 1, x] + maze[y + 1, x] + maze[y, x - 1] + maze[y, x + 1];
                        if (test == 3)
                        {
                            numOfDeadEnds++;
                        }
                    }
                }
            }

            deadEndsFitness = (float)numOfDeadEnds / maxNumOfDeadEnds;
            #endregion

            fit.OpenSpacesFitness = openSpacesFitness;
            fit.OuterWallFitness = outerWallFitness;
            fit.ClosedSpacesFitness = closedSpacesFitness;
            fit.DeadEndsFitness = deadEndsFitness;
            return fit;
        }
    }
}
