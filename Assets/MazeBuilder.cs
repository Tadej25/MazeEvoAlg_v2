using Assets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;

public class MazeBuilder : MonoBehaviour
{
    public GameObject black;
    public GameObject white;

    public int numOfGenerations = 50;
    public int numOfIterations = 50;
    public int width = 50;

    public Slider G_Slider;
    public Slider I_Slider;

    public Text G_Text;
    public Text I_Text;

    [Min(9)]
    public int SeedLenght = 9;

    [Range(0,1)]
    public float OpenSpacesWeight = 1;
    [Range(0,1)]
    public float ClosedSpacesWeight = 1;
    [Range(0,1)]
    public float DeadEndWeight = 1;
    [Range(0,1)]
    public float OuterWallWeight = 1;

    public GameObject loading;

    List<GameObject> instantiatedGO;

    Dictionary<int, List<Fitness>> generations;
    List<int[,]> mazes;
    List<Fitness> result;

    public bool killThread = false;
    Thread t;

    // An object used to LOCK for thread safe accesses
    private readonly object _lock = new object();
    // Here we will add actions from the background thread
    // that will be "delayed" until the next Update call => Unity main thread
    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    // Start is called before the first frame update
    void Start()
    {
        instantiatedGO = new List<GameObject>();
        generations = new Dictionary<int, List<Fitness>>();
        mazes = new List<int[,]>();

        t = new Thread(delegate ()
        {
            GenerateMaze();
        });
        t.Start();

    }

    void GenerateMaze()
    {
        System.Random r = new System.Random();

        for (int gen = 1; gen <= numOfGenerations; gen++)
        {
            Debug.Log("CURRENT GENERATION: " + gen);
            if (result == null)
                result = GenerateInduviduals(gen, width, r);
            else
            {
                //Reprodukcija
                ///TODO: Poštimaj reprodukcijo, da ne pride do tega, da postane en seed dominanten
                ///magari pogruntaj nov način kako se generira novi seed ali pa kako se določi novi partner
                var breedingInduviduals = result.OrderByDescending(x => x.Score).ToList().GetRange(0, result.Count / 2);
                var allStringSeed = breedingInduviduals.Select(x => x.builder.StringSeed).ToList();
                Queue<string> newGenerationSeeds = new Queue<string>();
                int numberOfChildren = 4;

                while (newGenerationSeeds.Count < numOfIterations)
                {
                    int MinNumOfChildren = 0;
                    var temp = breedingInduviduals.OrderBy(x => x.builder.ChildrenSeeds.Count).ToList();
                    MinNumOfChildren = temp[0].builder.ChildrenSeeds.Count;
                    Builder parent1 = breedingInduviduals.Where(x => x.builder.ChildrenSeeds.Count <= MinNumOfChildren).First().builder;
                    Builder parent2 = null;
                    int attempts = 0;
                    int similarity = parent1.StringSeed.Length;
                    //Preveri morda če ta drugi partner že ni na kapaciteti z otroci
                    while (parent2 == null && parent1 != parent2 && attempts < 10)
                    {
                        try
                        {
                            var foundParent = breedingInduviduals.Where(x => x.builder.ChildrenSeeds.Count <= MinNumOfChildren && x.builder != parent1 && Builder.ComputeSimilarity(parent1.StringSeed, x.builder.StringSeed) >= (similarity)).FirstOrDefault();
                            var nonEqualPartnerSeeds = breedingInduviduals.Where(x => x.builder.StringSeed != parent1.StringSeed).ToList();
                            var nonEqualPartnerSeeds1 = breedingInduviduals.Where(x => x.builder.StringSeed != parent1.StringSeed).Select(x => x.builder.StringSeed).ToList();
                            var differentPartnerSeed = nonEqualPartnerSeeds.Where(x => Builder.ComputeSimilarity(parent1.StringSeed, x.builder.StringSeed) >= (similarity - attempts)).ToList();
                            var differentPartnerSeed1 = nonEqualPartnerSeeds.Where(x => Builder.ComputeSimilarity(parent1.StringSeed, x.builder.StringSeed) >= (similarity - attempts)).Select(x => x.builder.StringSeed).ToList();
                            if (differentPartnerSeed.Count > 0)
                            {
                                foundParent = differentPartnerSeed.First();
                            }
                            if (foundParent != null)
                            {
                                parent2 = foundParent.builder;
                            }
                            else
                            {
                                MinNumOfChildren++;
                            }
                            attempts++;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message);
                        }
                    }
                    if (attempts >= 9)
                    {
                        Debug.LogError("Nismo našli partnerja, gen: " + gen);
                        if (killThread)
                        {
                            t.Abort();
                        }
                    }
                    else
                    {
                        string childSeed = Builder.GenerateChildFromParents(parent1, parent2);
                        newGenerationSeeds.Enqueue(childSeed);
                        parent1.ChildrenSeeds.Add(childSeed);
                        parent2.ChildrenSeeds.Add(childSeed);
                    }
                }
                result = GenerateInduviduals(gen, width, r, newGenerationSeeds);
            }
            generations.Add(gen, result);
        }

        lock (_lock)
        {
            // Add an action that requires the main thread
            _mainThreadActions.Enqueue(() =>
            {
                G_Slider.minValue = 1;
                G_Slider.maxValue = generations.Count - 1;

                I_Slider.minValue = 1;
                I_Slider.maxValue = generations[1].Count - 1;

                G_Text.text = ((int)G_Slider.value).ToString();
                I_Text.text = ((int)I_Slider.value).ToString();

                G_Slider.onValueChanged.AddListener(delegate { SliderValueChanged(); });
                I_Slider.onValueChanged.AddListener(delegate { SliderValueChanged(); });

                loading.SetActive(false);
                SliderValueChanged();
            });
        }
    }

    
    List<Fitness> GenerateInduviduals(int generation, int width, System.Random r, Queue<string> newGenSeeds = null)
    {
        List<Fitness> fits = new List<Fitness>();
        try
        {
            for (int iteration = 1; iteration <= numOfIterations; iteration++)
            {
                string tempSeed = (newGenSeeds == null) ? GenerateRandomCharArray(r) : newGenSeeds.Dequeue();
                ///TODO: Spremeni št zidov
                Builder builer = new Builder(width, (int)System.Math.Pow(width, 2), tempSeed, r);
                builer.BuildMaze();
                Fitness fit = Fitness.CheckFitness(builer.Maze);

                fit.builder = builer;
                fit.OpenSpacesFitnessWeight = OpenSpacesWeight;
                fit.ClosedSpacesFitnessWeight = ClosedSpacesWeight;
                fit.DeadEndsFitnessWeight = DeadEndWeight;
                fit.OuterWallFitnessWeight = OuterWallWeight;
                fits.Add(fit);

                //Thread.Sleep(1);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            t.Abort();
        }

        return fits;
    }

    string GenerateRandomCharArray(System.Random r)
    {
        string res = "";
        for (int i = 0; i < SeedLenght; i++)
        {
            char letter = (char)r.Next(65, 91);
            switch (r.Next(0, 3))
            {
                case 0: letter = (char)r.Next(48, 58); break;
                case 1: letter = (char)r.Next(97, 123); break;
            }

            res += letter;
        }
        return res;
    }

    void SliderValueChanged()
    {
        int generation = (int)G_Slider.value;
        int iteration = (int)I_Slider.value;

        G_Text.text = ((int)G_Slider.value).ToString();
        I_Text.text = ((int)I_Slider.value).ToString();

        DrawMaze(generations[generation][iteration].builder.Maze);
    }

    void DrawMaze(int[,] maze)
    {
        int height = maze.GetLength(0);
        int width = maze.GetLength(1);

        foreach (var go in instantiatedGO)
        {
            Destroy(go);
        }
        instantiatedGO.Clear();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (maze[y,x] == 1)
                {
                    instantiatedGO.Add(Instantiate(black, new Vector3(x, y, 0), Quaternion.identity));
                }
                else
                {
                    instantiatedGO.Add(Instantiate(white, new Vector3(x, y, 0), Quaternion.identity));
                }
            }
        }
    }

    private void Update()
    {
        // Lock for thread safe access 
        lock (_lock)
        {
            // Run all queued actions in order and remove them from the queue
            while (_mainThreadActions.Count > 0)
            {
                var action = _mainThreadActions.Dequeue();

                action?.Invoke();
            }
        }
        if (killThread)
        {
            t.Abort();
        }
    }

    void OnApplicationQuit()
    {
        try
        {
            t.Abort();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
}
