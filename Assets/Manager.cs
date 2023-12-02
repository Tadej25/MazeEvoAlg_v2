using Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    [Min(1)]
    public int MazeSize = 10;

    [Min(1)]
    public int NumberOfGeneration = 100;
    [Min(1)]
    public int NumberOfIndividualsPerGeneration = 100;

    [Range(0.01f, 100)]
    public float BorderWeight = 0;
    [Range(0.01f, 100)]
    public float QualityWeight = 0;
    [Range(0.01f, 100)]
    public float ConnectivityWeight = 0;
    [Range(0.01f, 100)]
    public float ShortnessWeight = 0;
    [Range(0.01f, 100)]
    public float DeadendWeight = 0;
    [Range(0.01f, 100)]
    public float LoopWeight = 0;

    public bool pureRoulleteWheel = false;
    public bool pureElite = true;

    public GameObject wall;
    public GameObject path;

    public Camera camera;

    public Slider GenerationSlider;
    public Slider IndividualSlider;

    public Text CurrentGenerationText;
    public Text CurentIndividualText;

    public Text TimeText;
    public Text SeedText;

    public GameObject loading;
    public Text GeneratedGenerationsText;

    public AudioSource ding;

    DateTime startTime;
    DateTime endTime;

    Thread t;

    Queue<Individual> IndividualsToDraw = new Queue<Individual>();
    List<Generation> Generations = new List<Generation>();
    List<GameObject> InstatiatedItems = new List<GameObject>();

    System.Random r = new System.Random();

    public bool KillThread = false;

    // An object used to LOCK for thread safe accesses
    private readonly object _lock = new object();
    // Here we will add actions from the background thread
    // that will be "delayed" until the next Update call => Unity main thread
    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    // Start is called before the first frame update
    void Start()
    {
        camera.orthographicSize = MazeSize / 2 + 1;
        transform.position = new Vector3(this.transform.position.x - (MazeSize / 2), this.transform.position.y - (MazeSize / 2), 0);
        SetupSliders();
        GenerationSlider.onValueChanged.AddListener(delegate { SliderValueChange(); });
        IndividualSlider.onValueChanged.AddListener(delegate { SliderValueChange(); });
        t = new Thread(delegate ()
        {
            startTime = DateTime.Now;
            GenerateMaze();
            IndividualsToDraw.Clear();
            endTime = DateTime.Now;
            lock (_lock)
            {
                // Add an action that requires the main thread
                _mainThreadActions.Enqueue(() =>
                {
                    loading.SetActive(false);
                    ding.Play();
                    TimeSpan duration = DateTime.Now.Subtract(startTime);
                    TimeText.text = duration.ToString("mm':'ss");
                });
            }
        });
        t.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (t != null && t.IsAlive && KillThread)
        {
            t.Abort();
        }
        GeneratedGenerationsText.text = string.Format("{0}/{1}", Generations.Count + 1, NumberOfGeneration);
        
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            if ((Input.GetKey(KeyCode.LeftShift)) || (Input.GetKey(KeyCode.RightShift)))
            {
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    GenerationSlider.value += 1;
                }
                else if (Input.GetKey(KeyCode.UpArrow))
                {
                    GenerationSlider.value -= 1;
                }
            }
            else
            {
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    IndividualSlider.value += 1;
                }
                else if (Input.GetKey(KeyCode.UpArrow))
                {
                    IndividualSlider.value -= 1;
                }
            }
        }
        else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                GenerationSlider.value += 1;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                GenerationSlider.value -= 1;
            }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            IndividualSlider.value += 1;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            IndividualSlider.value -= 1;
        }

        lock (_lock)
        {
            // Run all queued actions in order and remove them from the queue
            while (_mainThreadActions.Count > 0)
            {
                var action = _mainThreadActions.Dequeue();

                action?.Invoke();
            }
        }
        if (IndividualsToDraw.Count > 0)
        {
            DeleteInstantiatedItems();
            Individual i = IndividualsToDraw.Dequeue();
            DrawMaze(i);
        }
    }

    private void SetupSliders()
    {
        GenerationSlider.value = 1;
        GenerationSlider.minValue = 1;
        GenerationSlider.maxValue = NumberOfGeneration;

        IndividualSlider.value = 1;
        IndividualSlider.minValue = 1;
        IndividualSlider.maxValue = NumberOfIndividualsPerGeneration;
    }

    private void GenerateMaze()
    {
        for (int generationIndex = 1; generationIndex <= NumberOfGeneration; generationIndex++)
        {
            Generation g = new Generation("Generation " + generationIndex);
            List<Tuple<string, string>> pairs = new List<Tuple<string, string>>();
            List<Tuple<string, string>> pairs2 = new List<Tuple<string, string>>();

            if (Generations.Count == 0)
            {
                GenerateFirstGeneration(g);
            }
            else
            {
                if (pureRoulleteWheel)
                    GeneratedGenerationsByRoullete(g);
                else
                    GenerateByElitist(g);
                //GenerateByElitistInThreads(g, 10);
            }
            
            Generations.Add(g);
        }
    }

    private void GenerateByElitist(Generation g)
    {
        Generation previousGeneration = Generations[Generations.Count - 1];

        List<Individual> topFiftyIndividuals = new List<Individual>(previousGeneration.Individuals.OrderByDescending(x => x.GetScore()).ToList());
        topFiftyIndividuals = new List<Individual>(topFiftyIndividuals.Take(50));

        List<Individual> solvablseIndividuals = previousGeneration.Individuals.Where(x => x._Fitness.SolvableScore2 > 1).ToList();

        Generation solvableG = new Generation("temporarySolvable");
        Generation topFiftyG = new Generation("temporaryTopFifty");

        topFiftyG.Individuals = topFiftyIndividuals;
        solvableG.Individuals = solvablseIndividuals;

        decimal[] ComulativeFitnessSolvable = GetCumulativeFitness(solvableG);
        decimal[] ComulativeFitnessTopFifty = GetCumulativeFitness(topFiftyG);
        int individualIndex = 1;
        string generationIndex = (Generations.Count + 1).ToString();

        while (g.Individuals.Count < 100)
        {
            Individual mama;
            if (solvablseIndividuals.Count > 1)
                mama = RoulleteWheelSelection(solvablseIndividuals, ComulativeFitnessSolvable);
            else if (solvablseIndividuals.Count > 0)
                mama = solvablseIndividuals[0];
            else
                mama = RoulleteWheelSelection(topFiftyIndividuals, ComulativeFitnessTopFifty);
            Individual papa = null;
            int tries = 1;
            int similarity = 0;
            do
            {
                papa = RoulleteWheelSelection(topFiftyIndividuals, ComulativeFitnessTopFifty);
                if (tries++ > 100)
                {
                    similarity++;
                }
                if (similarity > 99)
                {
                    break;
                }
            }
            //while (papa != null && ComputeSimilarity(mama, papa) > similarity);
            while (papa != null && mama.Name.Equals(papa.Name));

            string name = String.Format("G{0}I{1}", generationIndex, individualIndex++);

            Individual baby = new Individual(mama, papa, name);
            baby.Grade((decimal)BorderWeight, (decimal)QualityWeight, (decimal)ConnectivityWeight, (decimal)ShortnessWeight, (decimal)DeadendWeight, (decimal)LoopWeight);

            g.Individuals.Add(baby);
            IndividualsToDraw.Enqueue(baby);
        }
    }
    private void GenerateByElitistInThreads(Generation g, int numOfThreads = 3)
    {
        Generation previousGeneration = Generations[Generations.Count - 1];

        List<Individual> topFiftyIndividuals = new List<Individual>(previousGeneration.Individuals.OrderByDescending(x => x.GetScore()).ToList());
        topFiftyIndividuals = new List<Individual>(topFiftyIndividuals.Take(50));

        List<Individual> solvablseIndividuals = previousGeneration.Individuals.Where(x => x._Fitness.SolvableScore2 > 1).ToList();

        Generation solvableG = new Generation("temporarySolvable");
        Generation topFiftyG = new Generation("temporaryTopFifty");

        topFiftyG.Individuals = topFiftyIndividuals;
        solvableG.Individuals = solvablseIndividuals;

        decimal[] ComulativeFitnessSolvable = GetCumulativeFitness(solvableG);
        decimal[] ComulativeFitnessTopFifty = GetCumulativeFitness(topFiftyG);
        int individualIndex = 1;
        string generationIndex = (Generations.Count + 1).ToString();

        List<Thread> threadsForBabies = new List<Thread>();
        for (int i = 0; i < numOfThreads; i++)
        {
            Thread th = new Thread(delegate ()
            {
                while (g.Individuals.Count < 100)
                {
                    Individual mama;
                    if (solvablseIndividuals.Count > 1)
                        mama = RoulleteWheelSelection(solvablseIndividuals, ComulativeFitnessSolvable);
                    else if (solvablseIndividuals.Count > 0)
                        mama = solvablseIndividuals[0];
                    else
                        mama = RoulleteWheelSelection(topFiftyIndividuals, ComulativeFitnessTopFifty);
                    Individual papa = null;
                    int tries = 1;
                    int similarity = 0;
                    do
                    {
                        papa = RoulleteWheelSelection(topFiftyIndividuals, ComulativeFitnessTopFifty);
                        if (tries++ > 100)
                        {
                            similarity++;
                        }
                        if (similarity > 99)
                        {
                            break;
                        }
                    }
                    //while (papa != null && ComputeSimilarity(mama, papa) > similarity);
                    while (papa != null && mama.Name.Equals(papa.Name));

                    string name = String.Format("G{0}I{1}", generationIndex, individualIndex++);

                    Individual baby = new Individual(mama, papa, name);
                    baby.Grade((decimal)BorderWeight, (decimal)QualityWeight, (decimal)ConnectivityWeight, (decimal)ShortnessWeight, (decimal)DeadendWeight, (decimal)LoopWeight);

                    if (g.Individuals.Count < 100)
                    {
                        g.Individuals.Add(baby);
                        IndividualsToDraw.Enqueue(baby);
                    }
                }
            });
            threadsForBabies.Add(th);
            th.Start();
        }

        while (threadsForBabies.Any(x => x.IsAlive))
        {

        }
        
    }

    private void GeneratedGenerationsByRoullete(Generation g, Generation previousGeneration = null)
    {
        if (previousGeneration == null || previousGeneration.Individuals.Count == 0)
            previousGeneration = Generations[Generations.Count - 1];
        decimal[] ComulativeFitness = GetCumulativeFitness(previousGeneration);
        int individualIndex = 1;
        string generationIndex = (Generations.Count + 1).ToString();
        while (g.Individuals.Count < 100)
        {
            Individual mama = RoulleteWheelSelection(previousGeneration.Individuals, ComulativeFitness);
            Individual papa = null;
            int tries = 1;
            int similarity = 0;
            do
            {
                papa = RoulleteWheelSelection(previousGeneration.Individuals, ComulativeFitness);
                if (tries++ > 100)
                {
                    similarity++;
                }
                if (similarity > 99)
                {
                    break;
                }
            }
            //while (papa != null && ComputeSimilarity(mama, papa) > similarity);
            while (papa != null && mama.Name.Equals(papa.Name));

            string name = String.Format("G{0}I{1}", generationIndex, individualIndex);
            Individual baby = new Individual(mama, papa, name);
            baby.Grade((decimal)BorderWeight, (decimal)QualityWeight, (decimal)ConnectivityWeight, (decimal)ShortnessWeight, (decimal)DeadendWeight, (decimal)LoopWeight);
            g.Individuals.Add(baby);
            IndividualsToDraw.Enqueue(baby);
        }
    }

    private void GenerateFirstGeneration(Generation g)
    {
        bool noSolvableIndividuals = true;
        int numSolvableIndividuals = 0;
        Individual individual;
        int individualIndex = 1;
        string generationIndex = (Generations.Count + 1).ToString();
        string name = String.Format("G{0}I{1}", generationIndex, individualIndex);
        List<Thread> threadsForSolvableMaze = new List<Thread>();
        for (int i = 0; i < 3; i++)
        {
            Thread th = new Thread(delegate ()
            {
                do
                {
                    individual = new Individual(MazeSize, name, r);
                    individual.Grade((decimal)BorderWeight, (decimal)QualityWeight, (decimal)ConnectivityWeight, (decimal)ShortnessWeight, (decimal)DeadendWeight, (decimal)LoopWeight);
                    if (individual._Fitness.SolvableScore > 0)
                    {
                        noSolvableIndividuals = false;
                        g.Individuals.Add(individual);
                        IndividualsToDraw.Enqueue(individual);
                        numSolvableIndividuals++;
                    }
                } while (numSolvableIndividuals < NumberOfIndividualsPerGeneration / 10);
            });
            threadsForSolvableMaze.Add(th);
            th.Start();
        }

        DateTime startOfWait = DateTime.Now;
        while (threadsForSolvableMaze.Any(x=>x.IsAlive) && DateTime.Now.Subtract(startOfWait).TotalSeconds < 61)
        {

        }
        

        while (g.Individuals.Count < 100)
        {
            name = String.Format("G{0}I{1}", generationIndex, individualIndex++);
            individual = new Individual(MazeSize, name, r);
            individual.Grade((decimal)BorderWeight, (decimal)QualityWeight, (decimal)ConnectivityWeight, (decimal)ShortnessWeight, (decimal)DeadendWeight, (decimal)LoopWeight);
            g.Individuals.Add(individual);
            IndividualsToDraw.Enqueue(individual);
        }

    }

    private void TestThreadingGeneration(ref Queue<Individual> babies, List<Individual> orderdIndividualsByScore, int generationIndex)
    {
        int attempt = 1000;
        while (babies.Count < NumberOfIndividualsPerGeneration && attempt > 0)
        {
            attempt--;
            string name = String.Format("G{0}I{1}", generationIndex, babies.Count + 1);
            decimal[] ComulativeFitness = GetCumulativeFitness(Generations[Generations.Count - 1]);
            Individual mama = RoulleteWheelSelection(orderdIndividualsByScore, ComulativeFitness);
            Individual papa = null;
            int tries = 1;
            int similarity = 60;
            do
            {
                papa = RoulleteWheelSelection(orderdIndividualsByScore, ComulativeFitness);
                if (tries++ > 100)
                {
                    similarity++;
                }
                if (similarity > 99)
                {
                    break;
                }
            }
            while (papa != null && mama.Name.Equals(papa.Name));

            Individual baby = new Individual(mama, papa, name);
            baby.Grade((decimal)BorderWeight, (decimal)QualityWeight, (decimal)ConnectivityWeight, (decimal)ShortnessWeight, (decimal)DeadendWeight, (decimal)LoopWeight);
            try
            {
                if (baby != null)
                    babies.Enqueue(baby);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }

    private int ComputeSimilarity(Individual m, Individual p)
    {
        if (m.Name.Equals(p.Name))
        {
            return 100;
        }
        int Size = m.Maze.GetLength(0);
        int totalElements = Size * Size;
        int matchingElements = 0;

        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                if (m.Maze[i, j] == p.Maze[i, j])
                {
                    matchingElements++;
                }
            }
        }

        double similarityPercentage = (double)matchingElements / totalElements * 100;
        return (int)similarityPercentage;
    }

    private decimal[] GetCumulativeFitness(Generation generation)
    {
        decimal score = 0;
        foreach (Individual individual in generation.Individuals)
        {
            score += individual.GetScore();
        }
        decimal[] rez = new decimal[generation.Individuals.Count + 1];
        rez[0] = 0;

        int index = 1;
        decimal comulative = 0;
        foreach (Individual individual in generation.Individuals)
        {
            comulative += individual.GetScore() / score;
            rez[index] = Math.Round(comulative, 5);
            index++;
        }

        rez[rez.Length - 1] = 1;
        return rez;
    }

    private Individual RoulleteWheelSelection(List<Individual> individuals, decimal[] comulativeFitness)
    {
        Individual ret = null;
        decimal chance = (decimal)r.NextDouble();
        for (int i = 0; i < comulativeFitness.Length; i++)
        {
            if (comulativeFitness[i + 1] > chance)
            {
                ret = individuals[i];
                break;
            }
        }
        return ret;
    }

    
    private void FixedUpdate()
    {
        
    }

    private void DrawMaze(Individual individual)
    {
        foreach (var go in InstatiatedItems)
        {
            Destroy(go);
        }
        InstatiatedItems.Clear();

        int screenX = (int)this.transform.position.x;
        int screenY = (int)this.transform.position.y;

        for (int y = 0; y < MazeSize; y++)
        {
            for (int x = 0; x < MazeSize; x++)
            {
                if (individual.Maze[y, x] == 1)
                {
                    GameObject go = Instantiate(wall, new Vector3(screenX + x, screenY + y, 0), Quaternion.identity);
                    go.name = String.Format("x{0}_y{1}_wall", x, y);
                    InstatiatedItems.Add(go);
                }
                else
                {
                    GameObject go = Instantiate(path, new Vector3(screenX + x, screenY + y, 0), Quaternion.identity);
                    go.name = String.Format("x{0}_y{1}_path", x, y);
                    InstatiatedItems.Add(go);
                }
                //if (
                //    x == MazeSize - 1 &&
                //    y == MazeSize - 2)
                //{
                //    GameObject temp = InstatiatedItems[InstatiatedItems.Count - 1];
                //    temp.GetComponent<Renderer>().material.color = new Color(255, 255, 0); // yellow, top right
                //}
                //if (
                //    x == 0 &&
                //    y == 1)
                //{
                //    GameObject temp = InstatiatedItems[InstatiatedItems.Count - 1];
                //    temp.GetComponent<Renderer>().material.color = new Color(0, 255, 255); //teal bottom left
                //}
            }
        }
    }

    private void DeleteInstantiatedItems()
    {
        foreach (GameObject item in InstatiatedItems)
        {
            Destroy(item);
        }
        InstatiatedItems.Clear();
    }

    public void SliderValueChange()
    {
        try
        {
            int g = (int)GenerationSlider.value;
            int i = (int)IndividualSlider.value;
            Individual individual = Generations[g - 1].Individuals[i - 1];
            DrawMaze(individual);
            CurrentGenerationText.text = g.ToString();
            CurentIndividualText.text = i.ToString();

            //Debug.Log(String.Format("Score {5}"
            //    , individual._Fitness.BorderScore
            //    , individual._Fitness.ConnectivityScore
            //    , individual._Fitness.QualityScore
            //    , individual._Fitness.ShortnessScore
            //    , individual._Fitness.SolvableScore
            //    , individual._Fitness.Score
            //    , i
            //    , g));

            string tempSeed = "";
            for (int y = 0; y < individual.Maze.GetLength(0); y++)
            {
                for (int x = 0; x < individual.Maze.GetLength(0); x++)
                {
                    tempSeed += individual.Maze[y, x];
                }
                tempSeed += "\n";
            }

            string Scores = individual._Fitness.GetSeperateScores.Replace("; ", "\n");

            tempSeed += "\n";
            tempSeed += "\n";

            tempSeed += Scores;

            SeedText.text = tempSeed;
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    void OnApplicationQuit()
    {
        if (t != null && t.IsAlive)
        {
            t.Abort();
        }
    }
}
