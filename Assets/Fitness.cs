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

        float _walledSpacesFitness;
        public float WalledSpacesFitness
        {
            get
            {
                return _walledSpacesFitness * WalledSpacesFitnessWeight;
            }
            set
            {
                _walledSpacesFitness = value;
            }
        }
        public float WalledSpacesFitnessWeight { get; set; } = 1;

        float _corridorFitness;
        public float CorridorFitness
        {
            get
            {
                return _corridorFitness * CorridorFitnessWeight;
            }
            set
            {
                _corridorFitness = value;
            }
        }
        public float CorridorFitnessWeight { get; set; } = 1;
        
        float _solvableFitness;
        public float SolvableFitness
        {
            get
            {
                return _solvableFitness;
            }
            set
            {
                _solvableFitness = value;
            }
        }
        public float SolvableFitnessWeight { get; set; } = 1;

        public float Score { 
            get 
            {
                return OpenSpacesFitness + ClosedSpacesFitness + DeadEndsFitness + OuterWallFitness + WalledSpacesFitness + CorridorFitness + (SolvableFitness * SolvableFitnessWeight * 100);
            } 
        }
        public Builder builder { get; set; }
        public string MazeFileName { get; set; }

        public static Fitness CheckFitness(int[,] maze, bool randomIO)
        {
            Fitness fit = new Fitness();
            int height = maze.GetLength(0);
            int width = maze.GetLength(1);

            float openSpacesFitness = 0;

            float outerWallFitness = 0;

            float closedSpacesFitness = 0;

            float deadEndsFitness = 0;

            float walledSpacesFitness = 0;

            float corridorFitness = 0;

            int solvable = 0;

            
            outerWallFitness = CalculateOuterWallFitness(maze, width, height);

            openSpacesFitness = CalculateOpenSpacesFitness(maze, width, height);

            closedSpacesFitness = CalculateClosedSpacesFitness(maze, width, height);

            deadEndsFitness = CalculateDeadEndFitness(maze, width, height);

            walledSpacesFitness = CalculateWalledSpacesFitness(maze, width, height);

            corridorFitness = CalculateCorridorFitness(maze, width, height);

            solvable = CalculateSolvableFitness(maze, width, height, randomIO);

            ///TODO: Dodaj še fitness kak dolga je pot od vhoda do cilja, daljša je boljša je

            fit.OpenSpacesFitness = openSpacesFitness;
            fit.OuterWallFitness = outerWallFitness;
            fit.ClosedSpacesFitness = closedSpacesFitness;
            fit.DeadEndsFitness = deadEndsFitness;
            fit.WalledSpacesFitness = walledSpacesFitness;
            fit.CorridorFitness = corridorFitness;
            fit.SolvableFitness = solvable;
            return fit;
        }

        private static float CalculateOuterWallFitness(int[,] maze, int width, int height)
        {
            float ret = 0;
            int numOfOuterWalls = 0;
            int maxNumOfOuterWalls = (width * 2 + (height - 2) * 2) - 2;

            #region OUTER_WALL_FITNESS
            //Preverjamo zgornji in spodnji rob
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
            ret = (float)numOfOuterWalls / (float)maxNumOfOuterWalls;
            if (ret > 1)
            {
                ret = 0;
            }
            return ret;
            #endregion
        }

        private static float CalculateOpenSpacesFitness(int[,] maze, int width, int height)
        {
            int numOpenSpaces = 0;
            int maxNumOfOpenSpaces = (width - 2) * (height - 2);
            float ret = 0;

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
                    if (maze[i, j] == 0 &&          //Sredina
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
            ret = (float)temp / (float)maxNumOfOpenSpaces;
            return ret;
            #endregion
        }

        private static float CalculateClosedSpacesFitness(int[,] maze, int width, int height)
        {
            //Izračunam max število zaprtih prostorov (načeloma če bi bilo polje šahovnica je najslabši primer)
            /*  ⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛
                ⬛⬜⬛⬜⬛⬜⬛⬜⬛⬜⬛
                ⬛⬛⬜⬛⬜⬛⬜⬛⬜⬛⬛
                ⬛⬜⬛⬜⬛⬜⬛⬜⬛⬜⬛
                ⬛⬛⬜⬛⬜⬛⬜⬛⬜⬛⬛
                ⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛⬛
             */
            float maxNumOfClosedSpaceCells = (float)((float)(height - 2) * (float)(width - 2));
            int numOfClosedSpaces = 0;

            float ret = 0;

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
                            numOfClosedSpaces += allNeighbours.Count;
                        }
                    }
                }
            }

            //Ker so lahko zaprti prostori veliki in s tem zmanjšajo število zaprtih prostorov sem se odločil, da število zaprtih prostorov kvadriram
            //in če je kvadrat večji od max števila zpartih prostorov smatram da je št. zaprtih prostorov enako max številu
            float numOfClosedSpacesPow = (float)Math.Pow(numOfClosedSpaces, 2);
            float tempNumClosedSpaces = numOfClosedSpacesPow > maxNumOfClosedSpaceCells ? maxNumOfClosedSpaceCells : numOfClosedSpacesPow;

            //Za izračun fitnessa, izračunam kako daleč stran je število zaprtih prostorov od 0 zaprtih prostorov in bližje kot
            //je NULI boljše je
            ret = 1 - (numOfClosedSpaces / maxNumOfClosedSpaceCells);//((float)(Math.Abs(maxNumOfClosedSpaceCells - Math.Abs(0 - tempNumClosedSpaces)) / maxNumOfClosedSpaceCells));
            #endregion
            return ret;
        }

        private static float CalculateDeadEndFitness(int[,] maze, int width, int height)
        {
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
            float ret = 0;

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

            ret = (float)numOfDeadEnds / maxNumOfDeadEnds;
            return ret;
            #endregion
        }

        private static float CalculateWalledSpacesFitness(int[,] maze, int width, int height)
        {
            /**/
            int numWalledSpaces = 0;
            int maxNumOfWalledSpaces = (width - 2) * (height - 2);
            float ret = 0;
            #region WALLED_SPACES_FITNESS
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
                        maze[i + 1, j + 1] == 1 &&  //I
                        maze[i + 1, j - 1] == 1 &&  //G
                        maze[i + 1, j] == 1 &&      //H 
                        maze[i - 1, j + 1] == 1 &&  //C
                        maze[i - 1, j] == 1 &&      //B
                        maze[i - 1, j - 1] == 1 &&  //A
                        maze[i, j + 1] == 1 &&      //F
                        maze[i, j - 1] == 1         //D
                        )
                    {
                        numWalledSpaces++;
                    }
                }
            }
            int tempWalled = maxNumOfWalledSpaces - numWalledSpaces;
            ret = (float)tempWalled / (float)maxNumOfWalledSpaces;
            return ret;
            #endregion
        }

        private static float CalculateCorridorFitness(int[,] maze, int width, int height)
        {
            /**/
            int numOfCorridors = 0;
            int maxNumOfCorridors = (((width - 2)) * ((height - 2) / 2) + ((height - 2) / 2 - 1));
            float ret = 0;

            #region CORRIDOR_FITNESS

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //Preskočimo rob labirinta ker tam ne more biti slepa ulica
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                        continue;

                    if (maze[y, x] == 0)
                    {
                        //Seštejemo vse vrednosti okoli trenutne celice (zid = 1, pot = 0) in če je seštevek enak 2 pomeni da so okoli
                        //trenutne celice dve poti in dva zida
                        int test = maze[y - 1, x] + maze[y + 1, x] + maze[y, x - 1] + maze[y, x + 1];
                        if (test == 2)
                        {
                            numOfCorridors++;
                        }
                    }
                }
            }

            ret = (float)numOfCorridors / maxNumOfCorridors;
            return ret;

            #endregion
        }

        private static int CalculateSolvableFitness(int[,] maze, int width, int height, bool randomIO)
        {
            Cell[,] newMazeToSolve = Crawler.GenerateMapToCrawl(maze);
            int ret = 0;
            if (randomIO)
            {
                #region RAND_SOLVABLE
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Cell currentCell = newMazeToSolve[y, x];
                    if (currentCell.Value == 0 && currentCell.Visited == false)
                    {
                        //Pridobimo vse celice ki so skupaj povezane (neprekinjena veriga sosedov) in če ni noben sosed na robu 
                        //labirinta (smatram kot izhod) potem je ta postor zaprt
                        currentCell.Visited = true;
                        List<Cell> allNeighbours = Crawler.CheckNeighbours(ref currentCell, ref newMazeToSolve);
                        if (allNeighbours.Where(x => x.PosX == 0 || x.PosX == width - 1 || x.PosY == 0 || x.PosY == height - 1).Any() && (currentCell.PosX == 0 || currentCell.PosX == width - 1 || currentCell.PosY == 0 || currentCell.PosY == height - 1))
                        {
                            var actualNeighbours = allNeighbours.Where(x => x.PosX == 0 || x.PosX == width - 1 || x.PosY == 0 || x.PosY == height - 1).ToList();
                            List<Cell> exits = GetNeighbours(currentCell, actualNeighbours, width, height);
                            if (exits.Any())
                            {
                                ret = 1;
                            }
                        }
                    }
                }
            }
            #endregion
            }
            else
            {
                #region STATIC_SOLVABLE
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Cell currentCell = newMazeToSolve[y, x];
                        if (currentCell.Value == 0 && currentCell.Visited == false)
                        {
                            //Pridobimo vse celice ki so skupaj povezane (neprekinjena veriga sosedov) in če ni noben sosed na robu 
                            //labirinta (smatram kot izhod) potem je ta postor zaprt
                            currentCell.Visited = true;
                            List<Cell> allNeighbours = Crawler.CheckNeighbours(ref currentCell, ref newMazeToSolve);
                            //Preverimo ali sta vhod in izhod obdana z zidom
                            /*
                            ⬛
                            ⬜ - Vhod/Izhod
                            ⬛
                             */
                            bool VhodExist = allNeighbours.Any(x => x.PosX == 0 && x.PosY == 1);
                            if (VhodExist)
                            {
                                bool above = allNeighbours.Any(x => x.PosX == 0 && x.PosY == 2);
                                bool below = allNeighbours.Any(x => x.PosX == 0 && x.PosY == 0);
                                VhodExist = above == false && below == false;
                            }

                            bool ExitExist = allNeighbours.Any(x => x.PosX == width - 1 && x.PosY == width - 2);
                            if (VhodExist)
                            {
                                bool above = allNeighbours.Any(x => x.PosX == width - 1 && x.PosY == width - 1);
                                bool below = allNeighbours.Any(x => x.PosX == width - 1 && x.PosY == width - 3);
                                VhodExist = above == false && below == false;
                            }
                            bool NoOtherExits = true;// (allNeighbours.Where(x => x.PosX == 0 || x.PosX == width - 1 || x.PosY == 0 || x.PosY == width - 1).ToList().Count - 2) < 1;
                            if (VhodExist && ExitExist && NoOtherExits)
                            {
                                ret = 1;
                            }
                        }
                    }
                }
                #endregion
            }

            return ret;
        }

        private static List<Cell> GetNeighbours(Cell borderCell, List<Cell> allBorderNeighbours, int width, int height)
        {
            //Pobrišem sosede podane celice, ki so tik zram nje oz neprekinjena veriga sosedov na robu in jih pobrišem ven iz seznama
            //Če na koncu še ostane kakšna celica na robu, potem ta labirint je režljiv (ima vhod in izhod)
            if (borderCell.PosX == 0 || borderCell.PosX == width - 1)
            {
                int tempYminus = borderCell.PosY - 1;
                int tempYplus = borderCell.PosY + 1;
                while (tempYminus > 0)
                {
                    Cell neighbour = allBorderNeighbours.Where(x => x.PosY == tempYminus-- && x.PosX == borderCell.PosX).FirstOrDefault();
                    if (neighbour != null)
                    {
                        allBorderNeighbours.Remove(neighbour);
                    }
                    else
                    {
                        break;
                    }
                }
                while (tempYplus < width - 1)
                {
                    Cell neighbour = allBorderNeighbours.Where(x => x.PosY == tempYplus++ && x.PosX == borderCell.PosX).FirstOrDefault();
                    if (neighbour != null)
                    {
                        allBorderNeighbours.Remove(neighbour);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            if (borderCell.PosY == 0 || borderCell.PosY == width - 1)
            {
                int tempXminus = borderCell.PosX - 1;
                int tempXplus = borderCell.PosX + 1;
                while (tempXminus > 0)
                {
                    Cell neighbour = allBorderNeighbours.Where(x => x.PosX == tempXminus-- && x.PosY == borderCell.PosY).FirstOrDefault();
                    if (neighbour != null)
                    {
                        allBorderNeighbours.Remove(neighbour);
                    }
                    else
                    {
                        break;
                    }
                }
                while (tempXplus < width - 1)
                {
                    Cell neighbour = allBorderNeighbours.Where(x => x.PosX == tempXplus++ && x.PosY == borderCell.PosY).FirstOrDefault();
                    if (neighbour != null)
                    {
                        allBorderNeighbours.Remove(neighbour);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return allBorderNeighbours;
        }
    }
}
