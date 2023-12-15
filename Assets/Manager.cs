using Assets;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

    public bool colorExtitEntrance = true;

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
    public Slider loadingSlider;

    public string PathToSaveData = "C:/Users/Tadej/Desktop/FERI/MAGISTERIJ/Magisterjska naloga/MazeOutput";

    //public AudioSource ding;

    DateTime startTime;
    DateTime endTime;

    Thread t;
    List<Thread> allThreads = new List<Thread>();

    Queue<Individual> IndividualsToDraw = new Queue<Individual>();
    List<Generation> Generations = new List<Generation>();
    List<GameObject> InstatiatedItems = new List<GameObject>();

    //System.Random r;
    
    public bool KillThread = false;

    // An object used to LOCK for thread safe accesses
    private readonly object _lock = new object();
    // Here we will add actions from the background thread
    // that will be "delayed" until the next Update call => Unity main thread
    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    // Start is called before the first frame update
    void Start()
    {
        //if (r == null)
        //{
        //    r = new System.Random();
        //}

        camera.orthographicSize = MazeSize / 2 + 1;
        transform.position = new Vector3(this.transform.position.x - (MazeSize / 2), this.transform.position.y - (MazeSize / 2), 0);
        
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
                    SetupSliders();
                    loading.SetActive(false);
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
        if (t.IsAlive)
        {
            TimeSpan duration = DateTime.Now.Subtract(startTime);
            TimeText.text = duration.ToString("mm':'ss");
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
        GenerationSlider.maxValue = Generations.Count;

        IndividualSlider.value = 1;
        IndividualSlider.minValue = 1;
        IndividualSlider.maxValue = NumberOfIndividualsPerGeneration;
    }

    private void GenerateMaze()
    {
        int similarityCount = 0;
        DateTime tempStart = DateTime.Now;
        DateTime tempEnd;
        for (int generationIndex = 1; generationIndex <= NumberOfGeneration; generationIndex++)
        {
            Generation g = new Generation("Generation " + generationIndex);
            List<Tuple<string, string>> pairs = new List<Tuple<string, string>>();
            List<Tuple<string, string>> pairs2 = new List<Tuple<string, string>>();

            if (Generations.Count == 0)
            {
                GenerateFirstGeneration(g, new System.Random());
                tempStart = DateTime.Now;
            }
            else
            {
                if (pureRoulleteWheel)
                    GeneratedGenerationsByRoullete(g, new System.Random());
                else
                    GenerateByElitist(g, new System.Random());
                //GenerateByElitistInThreads(g, 10);
            }

            Generations.Add(g);
            if (generationIndex > 2)
            {
                if (g.Equals(Generations[Generations.Count-2]) || g.Equals(Generations[Generations.Count - 3]))
                {
                    similarityCount++;
                }
            }
            if (similarityCount > 3)
            {
                break;
            }
        }
        tempEnd = DateTime.Now;
        TimeSpan tempDuration = tempEnd.Subtract(tempStart);
        Debug.Log("Actual generation time (without generating solvable): " + tempDuration.ToString("mm':'ss"));
    }

    private void GenerateByElitist(Generation g, System.Random r)
    {
        Generation previousGeneration = Generations[Generations.Count - 1];

        List<Individual> topFiftyIndividuals = new List<Individual>(previousGeneration.Individuals.OrderByDescending(x => x.GetScore()).ToList());
        topFiftyIndividuals = new List<Individual>(topFiftyIndividuals.Take(50));

        List<Individual> solvablseIndividuals = previousGeneration.Individuals.Where(x => x._Fitness.SolvableScore > 1).ToList();

        Generation solvableG = new Generation("temporarySolvable");
        Generation topFiftyG = new Generation("temporaryTopFifty");

        topFiftyG.Individuals = topFiftyIndividuals;
        solvableG.Individuals = solvablseIndividuals;

        decimal[] ComulativeFitnessSolvable = GetCumulativeFitness(solvableG);
        decimal[] ComulativeFitnessTopFifty = GetCumulativeFitness(topFiftyG);
        int individualIndex = 1;
        string generationIndex = (Generations.Count + 1).ToString();

        while (g.Individuals.Count < NumberOfIndividualsPerGeneration)
        {
            Individual mama;
            if (solvablseIndividuals.Count > 1)
                mama = RoulleteWheelSelection(solvablseIndividuals, ComulativeFitnessSolvable, r);
            else if (solvablseIndividuals.Count > 0)
                mama = solvablseIndividuals[0];
            else
                mama = RoulleteWheelSelection(topFiftyIndividuals, ComulativeFitnessTopFifty, r);
            Individual papa = null;
            int tries = 1;
            int similarity = 0;
            do
            {
                papa = RoulleteWheelSelection(topFiftyIndividuals, ComulativeFitnessTopFifty, r);
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

            try
            {
                g.Individuals.Add(baby);
                IndividualsToDraw.Enqueue(baby);
            }
            catch (Exception e)
            {
                Debug.LogError("GenerateByElitist, adding to individuals and queue" + e.Message);
            }
        }
    }
    private void GeneratedGenerationsByRoullete(Generation g, System.Random r, Generation previousGeneration = null)
    {
        if (previousGeneration == null || previousGeneration.Individuals.Count == 0)
            previousGeneration = Generations[Generations.Count - 1];
        decimal[] ComulativeFitness = GetCumulativeFitness(previousGeneration);
        int individualIndex = 1;
        string generationIndex = (Generations.Count + 1).ToString();
        while (g.Individuals.Count < NumberOfIndividualsPerGeneration)
        {
            Individual mama = RoulleteWheelSelection(previousGeneration.Individuals, ComulativeFitness, r);
            Individual papa = null;
            int tries = 1;
            int similarity = 0;
            do
            {
                papa = RoulleteWheelSelection(previousGeneration.Individuals, ComulativeFitness, r);
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

    private void GenerateFirstGeneration(Generation g, System.Random r)
    {
        int numSolvableIndividuals = 0;
        Individual individual;
        int individualIndex = 1;
        string generationIndex = (Generations.Count + 1).ToString();
        string name = String.Format("G{0}I{1}", generationIndex, individualIndex);
        List<Thread> threadsForSolvableMaze = new List<Thread>();
        for (int i = 0; i < 10; i++)
        {
            Thread th = new Thread(delegate ()
            {
                System.Random r = new System.Random();
                do
                {
                    individual = new Individual(MazeSize, name, r);
                    individual.Grade((decimal)BorderWeight, (decimal)QualityWeight, (decimal)ConnectivityWeight, (decimal)ShortnessWeight, (decimal)DeadendWeight, (decimal)LoopWeight);
                    if (individual._Fitness.SolvableScore > 0)
                    {
                        g.Individuals.Add(individual);
                        IndividualsToDraw.Enqueue(individual);
                        numSolvableIndividuals++;
                        Debug.Log(string.Format("solvable generated ({0})", numSolvableIndividuals));
                    }
                } while (numSolvableIndividuals < NumberOfIndividualsPerGeneration / 10 && KillThread == false);
            });
            threadsForSolvableMaze.Add(th);
            th.Start();
        }
        allThreads.Concat(threadsForSolvableMaze);

        DateTime startOfWait = DateTime.Now;
        while (threadsForSolvableMaze.Any(x => x.IsAlive) && DateTime.Now.Subtract(startOfWait).TotalSeconds < 1 && KillThread == false)
        {

        }

        foreach (var thread in threadsForSolvableMaze)
        {
            if (thread.IsAlive)
                thread.Abort();
        }

        while (g.Individuals.Count < NumberOfIndividualsPerGeneration && KillThread == false)
        {
            name = String.Format("G{0}I{1}", generationIndex, individualIndex++);
            individual = new Individual(MazeSize, name, r);
            individual.Grade((decimal)BorderWeight, (decimal)QualityWeight, (decimal)ConnectivityWeight, (decimal)ShortnessWeight, (decimal)DeadendWeight, (decimal)LoopWeight);
            g.Individuals.Add(individual);
            IndividualsToDraw.Enqueue(individual);
        }

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

    private Individual RoulleteWheelSelection(List<Individual> individuals, decimal[] comulativeFitness, System.Random r)
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
                if (colorExtitEntrance)
                {
                    if (
                        x == MazeSize - 1 &&
                        y == MazeSize - 2)
                    {
                        GameObject temp = InstatiatedItems[InstatiatedItems.Count - 1];
                        temp.GetComponent<Renderer>().material.color = new Color(255, 255, 0); // yellow, top right
                    }
                    if (
                        x == 0 &&
                        y == 1)
                    {
                        GameObject temp = InstatiatedItems[InstatiatedItems.Count - 1];
                        temp.GetComponent<Renderer>().material.color = new Color(0, 255, 255); //teal bottom left
                    }
                }
            }
        }
    }

    public void SaveDataToFile()
    {
        
        string folderName = "/output/" + DateTime.Now.ToString("yyyyMMddhhmmss");
        Directory.CreateDirectory(PathToSaveData + folderName);
        Thread t1 = new Thread(delegate ()
        {
            MaindThreadSliderActive(true);
            SaveAll(PathToSaveData, folderName);
            Thread.Sleep(500);
            MaindThreadSliderActive(false);
        });
        t1.Start();
    }

    public void SaveAll(string folderPath, string folderName)
    {
        List<string> csvLines = new List<string>();
        csvLines.Add("TIME;" + TimeText.text);
        csvLines.Add("SIZE;" + MazeSize);
        if (pureRoulleteWheel)
            csvLines.Add("SELECTION;ROULLETE WHEEL");
        else if (pureElite)
            csvLines.Add("SELECTION;ELITIST");
        csvLines.Add("NUMBER_OF_GENERATIONS;" + Generations.Count);
        csvLines.Add("NUMBER_OF_INDIVIDUALS;" + Generations[0].Individuals.Count);
        csvLines.Add("BORDER WEIGHT;QUALITY WEIGHT;CONNECTIVITY WEIGHT;SHORTNESS WEIGHT;DEADEND WEIGHT;LOOP WEIGHT");
        csvLines.Add(string.Format("{0};{1};{2};{3};{4};{5}", BorderWeight, QualityWeight, ConnectivityWeight, ShortnessWeight, DeadendWeight, LoopWeight));
        csvLines.Add("NAME;BORDER FITNESS;QUALITY FITNESS;CONNECTIVITY FITNESS;SHORTNESS FITNESS;DEADEND FITNESS;LOOP FITNESS;SCORE");
        /*
            NAME;
            BORDER FITNESS;
            QUALITY FITNESS;
            CONNECTIVITY FITNESS;
            SHORTNESS FITNESS;
            DEADEND FITNESS;
            LOOP FITNESS;
            SCORE;
            SEED
         */

        MaindThreadSliderMaxValue(Generations.Count * Generations[0].Individuals.Count);

        //GenerateStringMazesAsnyc();
        
        foreach (var generation in Generations)
        {
            if (KillThread)
            {
                break;
            }

            int index = 1;
            foreach (var individual in generation.Individuals)
            {
                MaindThreadSliderAddValue(1);
                Debug.Log(string.Format("Saving GEN: {0}, ITE: {1}", generation.Name, individual.Name));
                csvLines.Add(string.Format("{0};{1};{2};{3};{4};{5};{6};{7}",
                    individual.Name,
                    individual._Fitness.BorderScore,
                    individual._Fitness.QualityScore,
                    individual._Fitness.ConnectivityScore,
                    individual._Fitness.ShortnessScore,
                    individual._Fitness.DeadendScore,
                    individual._Fitness.LoopScore,
                    individual._Fitness.Score));
            }
        }
        csvLines.Add("");
        csvLines.Add("GENERATION;SUM;AVERAGE;MEDIAN");
        foreach (var generation in Generations)
        {
            if (KillThread == false)
            {
                break;
            }

            decimal sum = generation.Individuals.Sum(x => x._Fitness.Score);
            decimal average = generation.Individuals.Average(x => x._Fitness.Score);
            decimal median = generation.Individuals.OrderBy(x => x._Fitness.Score).ToList()[(generation.Individuals.Count - 1) / 2]._Fitness.Score;
            csvLines.Add(string.Format("{0};{1};{2};{3}", generation.Name, sum, average, median));
        }
        File.WriteAllLines(folderPath + folderName + "/Data.csv", csvLines);
    }

    private void GenerateStringMazesAsnyc()
    {
        ConcurrentQueue<Individual> individualsToStringify = new ConcurrentQueue<Individual>();
        foreach (Generation generation in Generations)
        {
            generation.Individuals.ForEach(x => individualsToStringify.Enqueue(x));
        }
        int sliderMaxcalue = individualsToStringify.Count;
        MaindThreadSliderMaxValue(sliderMaxcalue);
        int threads = 10;
        List<bool> done = new List<bool>(); 
        for (int i = 0; i < threads; i++)
        {
            Thread t1 = new Thread(delegate ()
            {
                try
                {
                    while (individualsToStringify.Count > 0 && KillThread == false)
                    {
                        Individual i;
                        if (individualsToStringify.TryDequeue(out i))
                        {
                            string temp = i.StringMaze;
                        }
                    }
                }
                finally
                {
                    done.Add(true);
                }
            });
            t1.Start();
        }
        while (KillThread == false && done.Count < threads)
        {
            MaindThreadSliderSetValue(sliderMaxcalue - individualsToStringify.Count);
        }
    }

    void MaindThreadSliderActive(bool state)
    {
        _mainThreadActions.Enqueue(() =>
        {
            loadingSlider.gameObject.SetActive(state);
        });
    }
    void MaindThreadSliderAddValue(decimal value)
    {
        _mainThreadActions.Enqueue(() =>
        {
            loadingSlider.value += (float)value; ;
        });
    }
    void MaindThreadSliderSetValue(decimal value)
    {
        _mainThreadActions.Enqueue(() =>
        {
            loadingSlider.value = (float)value; ;
        });
    }
    void MaindThreadSliderMaxValue(int maxValue)
    {
        _mainThreadActions.Enqueue(() =>
        {
            loadingSlider.maxValue = maxValue;
            loadingSlider.value = 0;
        });
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

            List<string> SeedToDisplay = new List<string>();
            string tempSeed = "";
            for (int y = 0; y < individual.Maze.GetLength(0); y++)
            {
                for (int x = 0; x < individual.Maze.GetLength(0); x++)
                {
                    tempSeed += individual.Maze[y, x];
                }
                SeedToDisplay.Insert(0, tempSeed);
                tempSeed = "";
            }

            foreach (var line in SeedToDisplay)
            {
                tempSeed += line + "\n";
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
        KillThread = true;
        if (t != null && t.IsAlive)
        {
            t.Abort();
        }
        foreach (var thread in allThreads)
        {
            if (thread.IsAlive)
            {
                thread.Abort();
            }
        }
    }
}
