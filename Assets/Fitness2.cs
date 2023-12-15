using System;
using System.Collections.Generic;

namespace Assets
{
    public class Fitness2
    {
        public decimal BorderWeight { get; set; }
        public decimal QualityWeight{ get; set; }
        public decimal ConnectivityWeight { get; set; }
        public decimal ShortnessWeight { get; set; }
        public decimal DeadendWeight { get; set; }
        public decimal LoopWeight { get; set; }
        public decimal SolvableWeight{ get { return BorderWeight * QualityWeight * ConnectivityWeight * ShortnessWeight * DeadendWeight * LoopWeight; } }
        public decimal SolvableWeight2 { get; set; }

        private decimal borderScore;
        public decimal BorderScore{ get { return borderScore * BorderWeight; } set { borderScore = value; } }

        private decimal qualityScore;
        public decimal QualityScore{ get { return qualityScore * QualityWeight; } set { qualityScore = value; } }

        private decimal connectivityScore;
        public decimal ConnectivityScore{ get { return connectivityScore * ConnectivityWeight; } set { connectivityScore = value; } }

        private decimal shortnessScore;
        public decimal ShortnessScore { get { return shortnessScore * ShortnessWeight; } set { shortnessScore = value; } }
        
        private decimal deadendScore;
        public decimal DeadendScore { get { return deadendScore * DeadendWeight; } set { deadendScore = value; } }
        
        private decimal loopScore;
        public decimal LoopScore { get { return loopScore * LoopWeight; } set { loopScore = value; } }

        private decimal solvableScore = 1;
        public decimal SolvableScoreOLD { get { return solvableScore * SolvableWeight; } set { solvableScore = value; } }
        public decimal SolvableScore { get { return (BorderScore + ConnectivityScore + DeadendScore + LoopScore + QualityScore + ShortnessScore) * SolvableWeight2; } set { solvableScore = value; } }

        public decimal Score{ 
            get 
            {
                decimal rez = BorderScore
                    + SolvableScore
                    + ConnectivityScore
                    + DeadendScore
                    + LoopScore
                    + QualityScore
                    + ShortnessScore;
                if (rez < 0)
                    rez = 1;
                return rez;
            } 
        }

        public string GetSeperateScores
        {
            get
            {
                return string.Format("BorderScore: {0}; SolvableScore: {1}; ConnectivityScore: {2}; DeadendScore: {3}; LoopScore: {4}; QualityScore: {5}; ShortnessScore: {6}; Score: {7}",
                        BorderScore
                        , SolvableScore
                        , ConnectivityScore
                        , DeadendScore
                        , LoopScore
                        , QualityScore
                        , ShortnessScore
                        , Score);
            }
        }

        public Fitness2(
            decimal borderWeight,
            decimal connectivityWeight,
            decimal deadendWeight,
            decimal loopWeight,
            decimal qualityWeight,
            decimal shortnessWeight)
        {
            this.BorderWeight = borderWeight;
            this.ConnectivityWeight = connectivityWeight;
            this.DeadendWeight = deadendWeight;
            this.LoopWeight = loopWeight;
            this.QualityWeight = qualityWeight;
            this.ShortnessWeight = shortnessWeight;
        }
        public void CaluclateScores(Individual individual)
        {
            BorderScore = CheckMazeBorder(individual.Maze);
            int Size = individual.Maze.GetLength(0);
            if (individual.Maze[1,0] == 1 ||individual.Maze[Size - 2, Size - 1] == 1)
            {
                BorderScore = 0;
            }
            QualityScore = CheckMazeQuality(individual.Maze);
            //SolvableScore = CheckMazeSolvability(individual.Maze);
            SolvableWeight2 = CheckMazeSolvability(individual.Maze);
            ConnectivityScore = -1 * CheckMazeConnectivity(individual.Maze);
            ShortnessScore = 0;
            if (SolvableWeight2 > 0)
                ShortnessScore = CheckMazeShortness(individual.Maze);
            decimal[] simplicityRez = ChekMazeSimplicity(individual.Maze);
            DeadendScore = simplicityRez[0];
            LoopScore = simplicityRez[1];
        }

        private decimal CheckMazeSolvability(int[,] maze)
        {
            int Size = maze.GetLength(0);
            int[,] tempMaze = maze.Clone() as int[,];
            if (DepthFirstSerachInner(1, 1, tempMaze) && maze[1, 0] == 0 && maze[Size - 2, Size - 1] == 0)
            {
                return 1;
            }
            return 0;
        }

        private decimal CheckMazeConnectivity(int[,] maze)
        {
            int Size = maze.GetLength(0);
            int[,] tempMaze = maze.Clone() as int[,];

            int componentCount = 0;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (tempMaze[y,x] == 0 && DepthFirstSearch(x, y, tempMaze))
                    {
                        componentCount++;
                    }
                }
            }

            return componentCount;
        }

        private decimal CheckMazeQuality(int[,] maze)
        {
            decimal result = 0;
            int size = maze.GetLength(0);
            for (int y = 1; y < size-1; y++)
			{
                for (int x = 1; x < size-1; x++)
                {
                    int numOfWalls = 0;
                    if (maze[y,x] == 0)
                    {
                        if (maze[y + 1, x] == 1)
                            numOfWalls++;
                        if (maze[y - 1, x] == 1)
                            numOfWalls++;
                        if (maze[y, x + 1] == 1)
                            numOfWalls++;
                        if (maze[y, x + 1] == 1)
                            numOfWalls++;

                        if (numOfWalls == 2 || numOfWalls == 3) 
                        {
                            result++;
                        }
                    }
                }
			}
            return result;
        }

        private decimal CheckMazeBorder(int[,] maze)
        {
            decimal numOfWalls = 0;
            int size = maze.GetLength(0);
            for (int i = 0; i < size; i++)
            {
                if (maze[0,i] == 1)
                {
                    numOfWalls++;
                }
                if (maze[i, 0] == 1 && i != 0)
                {
                    numOfWalls++;
                }
                if (maze[i, size - 1] == 1)
                {
                    numOfWalls++;
                }
                if (maze[size - 1, i] == 1)
                {
                    numOfWalls++;
                }
            }
            return numOfWalls;
        }

        private decimal CheckMazeShortness(int[,] maze)
        {
            //Dijkstra's algorithm
            int Size = maze.GetLength(0);
            int[,] directions = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } };

            // Priority queue for Dijkstra's algorithm
            var pq = new PriorityQueue<PriorityElement>();  // (cost, row, col)
            pq.Enqueue(new PriorityElement(0, 1, 0));

            HashSet<Tuple<int, int>> visited = new HashSet<Tuple<int, int>>();

            while (pq.Count > 0)
            {
                PriorityElement pe = pq.Dequeue();

                int row = pe.row;
                int col = pe.col;
                int cost = pe.cost;

                if (visited.Contains(Tuple.Create(row, col)))
                    continue;

                visited.Add(Tuple.Create(row, col));

                // Check if the exit is reached
                if (row == Size - 2 && col == Size - 1)
                    return cost;

                // Explore neighbors
                for (int i = 0; i < 4; i++)
                {
                    int nr = row + directions[i, 0];
                    int nc = col + directions[i, 1];

                    // Check if the neighbor is within bounds and is a valid path
                    if (nr >= 0 && nr < Size && nc >= 0 && nc < Size && maze[nr, nc] == 0)
                        pq.Enqueue(new PriorityElement(cost + 1, nr, nc));
                }
            }
            return 0;

        }
        
        private decimal[] ChekMazeSimplicity(int[,] maze)
        {
            int Size = maze.GetLength(0);
            int deadEnds = 0;
            int loops = 0;

            int[,] directions = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } };

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    //Če je potka
                    if (maze[y, x] == 0)
                    {
                        int neighborsCount = 0;

                        for (int d = 0; d < 4; d++)
                        {
                            int ni = y + directions[d, 0];
                            int nj = x + directions[d, 1];

                            if (ni >= 0 && ni < Size && nj >= 0 && nj < Size && maze[ni, nj] == 0)
                                neighborsCount++;
                        }

                        if (neighborsCount == 1)
                            deadEnds++;
                        else if (neighborsCount >= 3)
                            loops++;
                    }
                }
            }
            return new decimal[] { deadEnds, loops };
        }

        private bool DepthFirstSearch(int x, int y, int[,] maze, int mark = -1)
        {
            int Size = maze.GetLength(0);
            if (x < 0 || y < 0 || x >= Size || y >= Size || maze[y, x] == -1 || maze[y, x] == 1)
            {
                return false;
            }

            maze[y, x] = mark;

            if (x == Size - 1 && y == Size - 2)
            {
                return true;
            }

            if (DepthFirstSearch(x + 1, y, maze, mark) ||
                DepthFirstSearch(x - 1, y, maze, mark) ||
                DepthFirstSearch(x, y + 1, maze, mark) ||
                DepthFirstSearch(x, y - 1, maze, mark))
            {
                return true;
            }
            return false;
        }

        private bool DepthFirstSerachInner(int x, int y, int[,] maze, int mark = -1)
        {
            int Size = maze.GetLength(0);
            if (x <= 0 || y <= 0 || x >= Size - 1 || y >= Size - 1 || maze[y, x] == -1 || maze[y, x] == 1) 
            {
                return false;
            }

            maze[y, x] = mark;

            if (x == Size - 2 && y == Size - 2)
            {
                return true;
            }

            if (DepthFirstSerachInner(x + 1, y, maze, mark) ||
                DepthFirstSerachInner(x - 1, y, maze, mark) ||
                DepthFirstSerachInner(x, y + 1, maze, mark) ||
                DepthFirstSerachInner(x, y - 1, maze, mark))
            {
                return true;
            }
            return false;
        }

        private int[,] cloneMazeWithoutBorders(int[,] maze)
        {
            int Size = maze.GetLength(0);
            int[,] retMaze = new int[Size - 2, Size - 2];
            for (int y = 1; y < Size - 2; y++)
            {
                for (int x = 1; x < Size - 1; x++)
                {
                    retMaze[y-1, x-1] = maze[y, x];
                }
            }
            return retMaze;
        }

    }

    class PriorityElement : IComparable<PriorityElement>
    {
        public int cost { get; set; }
        public int row { get; set; }
        public int col { get; set; }

        public PriorityElement(int cost, int row, int col)
        {
            this.cost = cost;
            this.row = row;
            this.col = col;
        }

        public int CompareTo(PriorityElement obj)
        {
            if (this.cost > obj.cost)
                return 1;
            if (this.cost < obj.cost)
                return -1;
            return 0;
        }
    }

    class PriorityQueue<T> where T : IComparable<T>
    {
        private List<T> heap = new List<T>();

        public int Count => heap.Count;

        public void Enqueue(T item)
        {
            heap.Add(item);
            int i = heap.Count - 1;

            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (heap[i].CompareTo(heap[parent]) >= 0)
                    break;

                Swap(i, parent);
                i = parent;
            }
        }

        public T Dequeue()
        {
            T item = heap[0];
            int lastIndex = heap.Count - 1;
            heap[0] = heap[lastIndex];
            heap.RemoveAt(lastIndex);

            int i = 0;
            while (true)
            {
                int leftChild = 2 * i + 1;
                int rightChild = 2 * i + 2;
                int smallest = i;

                if (leftChild < heap.Count && heap[leftChild].CompareTo(heap[smallest]) < 0)
                    smallest = leftChild;

                if (rightChild < heap.Count && heap[rightChild].CompareTo(heap[smallest]) < 0)
                    smallest = rightChild;

                if (smallest == i)
                    break;

                Swap(i, smallest);
                i = smallest;
            }

            return item;
        }

        private void Swap(int i, int j)
        {
            T temp = heap[i];
            heap[i] = heap[j];
            heap[j] = temp;
        }
    }
}