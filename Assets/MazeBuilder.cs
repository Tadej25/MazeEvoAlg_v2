using Assets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System.IO;

public class MazeBuilder : MonoBehaviour
{
    public string FolderPath = "C:/Users/Tadej/Desktop/FERI/MAGISTERIJ/Magisterjska naloga/MazeOutput";
    public GameObject black;
    public GameObject white;

    public Camera camera;
    //Št generacij
    public int numOfGenerations = 50;
    //Št osebkov na generacijo
    public int numOfIterations = 50;
    public int width = 50;

    public Slider G_Slider;
    public Slider I_Slider;

    public Text G_Text;
    public Text I_Text;
    public Text SeedText;
    
    public Text TimeText;

    public Text OpenSpacesFitnessText;
    public Text ClosedSpacesFitnessText;
    public Text DeadEndFitnessText;
    public Text OuterWallFitnessText;
    public Text WalledSpacesFitnessText;
    public Text CorridorFitnessText;
    public Text SolvableFitnessText;
    
    public Text OpenSpacesFitnessWeightText;
    public Text ClosedSpacesFitnessWeightText;
    public Text DeadEndFitnessWeightText;
    public Text OuterWallFitnessWeightText;
    public Text WalledSpacesFitnessWeightText;
    public Text CorridorFitnessWeightText;
    public Text SolvableFitnessWeightText;
    
    //Dolžina seed-a
    [Min(9)]
    public int SeedLenght = 9;

    //Uteži
    [Range(0,1)]
    public float OpenSpacesWeight = 1;
    [Range(0,1)]
    public float ClosedSpacesWeight = 1;
    [Range(0,1)]
    public float DeadEndWeight = 1;
    [Range(0,1)]
    public float OuterWallWeight = 1;
    [Range(0,1)]
    public float WalledSpacesWeight = 1;
    [Range(0,1)]
    public float CorridorWeight = 1;
    [Range(0,1)]
    public int HasToBeSolvable = 1;

    public GameObject loading;
    public Text GeneratedGenerationsText;
    int currentGenerated = 0;

    List<GameObject> instantiatedGO;

    Dictionary<int, List<Fitness>> generations;
    List<int[,]> mazes;
    List<Fitness> result;

    DateTime startTime;

    //Ročno ubijemo thread generacije
    public bool killThread = false;
    //Ali želimo uporabiti navedeno dolžino seed-a ali se naj uporabi dolžina seed-a iz širine labirinta
    public bool SetSeed = false;
    Thread t;

    // An object used to LOCK for thread safe accesses
    private readonly object _lock = new object();
    // Here we will add actions from the background thread
    // that will be "delayed" until the next Update call => Unity main thread
    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    // Start is called before the first frame update
    void Start()
    {
        startTime = DateTime.Now;
        camera.orthographicSize = width / 2 + 1;
        transform.position = new Vector3(this.transform.position.x - (width/2), this.transform.position.y - (width / 2), 0);

        if (SetSeed == false)
        {
            SeedLenght = (int)Math.Pow(width, 2)/8;
        }
        instantiatedGO = new List<GameObject>();
        generations = new Dictionary<int, List<Fitness>>();
        mazes = new List<int[,]>();

        OpenSpacesFitnessWeightText.text = "OSW: " + OpenSpacesWeight.ToString();
        ClosedSpacesFitnessWeightText.text = "CSW: " + ClosedSpacesWeight.ToString();
        DeadEndFitnessWeightText.text = "DEW: " + DeadEndWeight.ToString();
        OuterWallFitnessWeightText.text = "OWW: " + OuterWallWeight.ToString();
        WalledSpacesFitnessWeightText.text = "WSW: " + WalledSpacesWeight.ToString();
        CorridorFitnessWeightText.text = "CW: " + CorridorWeight.ToString();
        SolvableFitnessWeightText.text = "SW: " + HasToBeSolvable.ToString();

        //Generacijo damo v svoj thread, da ne ustavi programa med generiranjem
        t = new Thread(delegate ()
        {
            GenerateMaze();
        });
        t.Start();
        //ScreenCapture.CaptureScreenshot(Application.dataPath + "/screenshots/" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".png");
    }

    void GenerateMaze()
    {
        System.Random r = new System.Random();

        for (int gen = 1; gen <= numOfGenerations; gen++)
        {
            //Debug.Log("CURRENT GENERATION: " + gen);
            if (result == null)
                result = GenerateInduviduals(gen, width, r);
            else
            {
                //Reprodukcija
                var breedingInduviduals = result.OrderByDescending(x => x.Score).ToList().GetRange(0, result.Count / 2);
                var allStringSeed = breedingInduviduals.Select(x => x.builder.StringSeed).ToList();
                Queue<string> newGenerationSeeds = new Queue<string>();

                while (newGenerationSeeds.Count < numOfIterations)
                {
                    //Izberemo prvega straša tako da izberemo prvega v razverščenem seznamu od najmanj otrok do največ (da svak dobi vsaj enega)
                    int MinNumOfChildren = 0;
                    var temp = breedingInduviduals.OrderBy(x => x.builder.ChildrenSeeds.Count).ToList();
                    MinNumOfChildren = temp[0].builder.ChildrenSeeds.Count;
                    Builder parent1 = breedingInduviduals.Where(x => x.builder.ChildrenSeeds.Count <= MinNumOfChildren).First().builder;
                    Builder parent2 = null;
                    int attempts = 0;
                    int similarity = parent1.StringSeed.Length/2;
                    //Preveri morda če ta drugi partner že ni na kapaciteti z otroci
                    while (parent2 == null && parent1 != parent2 && attempts < 10)
                    {
                        try
                        {
                            //seznam partnerjev naključno zmešamo
                            int n = breedingInduviduals.Count;
                            while (n > 1)
                            {
                                n--;
                                int k = r.Next(n + 1);
                                var value = breedingInduviduals[k];
                                breedingInduviduals[k] = breedingInduviduals[n];
                                breedingInduviduals[n] = value;
                            }

                            //Poiščemo prvega partnerja ki je trenutnemu strašu najmanj podoben
                            Fitness foundParent = null;
                            foreach (var induvidual in breedingInduviduals)
                            {
                                if (induvidual.builder != parent1)
                                {
                                    int parentSimilarity = Builder.ComputeSimilarity(parent1.StringSeed, induvidual.builder.StringSeed);
                                    if (parentSimilarity >= similarity)
                                    {
                                        foundParent = induvidual;
                                        break;
                                    }
                                }
                            }
                            if (foundParent != null)
                            { 
                                parent2 = foundParent.builder;
                            }
                            else
                            {
                                //Če partnerja ne najdemo zmanjšamo potrebno razliko za iskanje partnerja
                                similarity--;
                            }
                            //attempts++;
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
                G_Slider.maxValue = generations.Count;

                I_Slider.minValue = 1;
                I_Slider.maxValue = generations[1].Count - 1;

                G_Text.text = ((int)G_Slider.value).ToString();
                I_Text.text = ((int)I_Slider.value).ToString();

                G_Slider.onValueChanged.AddListener(delegate { SliderValueChanged(); });
                I_Slider.onValueChanged.AddListener(delegate { SliderValueChanged(); });

                loading.SetActive(false);
                SliderValueChanged();

                TimeSpan duration = DateTime.Now.Subtract(startTime);
                TimeText.text = duration.ToString("mm':'ss");
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
                fit.WalledSpacesFitnessWeight = WalledSpacesWeight;
                fit.CorridorFitnessWeight = CorridorWeight;
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
            char letter = 'a';
            switch (r.Next(0, 3))
            {
                case 0: letter = (char)r.Next(48, 58); break;
                case 1: letter = (char)r.Next(65, 91); break;
                case 2: letter = (char)r.Next(97, 123); break;
            }

            res += letter;
        }
        return res;
        //return "ý";
    }

    void SliderValueChanged()
    {
        int generation = (int)G_Slider.value;
        int iteration = (int)I_Slider.value;

        G_Text.text = ((int)G_Slider.value).ToString();
        I_Text.text = ((int)I_Slider.value).ToString();

        DrawMaze(generations[generation][iteration].builder.Maze);
        SeedText.text = generations[generation][iteration].builder.StringSeed;

        OpenSpacesFitnessText.text = "OS: " + generations[generation][iteration].OpenSpacesFitness.ToString();
        ClosedSpacesFitnessText.text = "CS: " + generations[generation][iteration].ClosedSpacesFitness.ToString();
        WalledSpacesFitnessText.text = "WS: " + generations[generation][iteration].WalledSpacesFitness.ToString();
        DeadEndFitnessText.text = "DE: " + generations[generation][iteration].DeadEndsFitness.ToString();
        OuterWallFitnessText.text = "OW: " + generations[generation][iteration].OuterWallFitness.ToString();
        CorridorFitnessText.text = "CR: " + generations[generation][iteration].CorridorFitness.ToString();
        SolvableFitnessText.text = "S: " + generations[generation][iteration].SolvableFitness.ToString();
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

        int screenX = (int)this.transform.position.x;
        int screenY = (int)this.transform.position.y;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (maze[y,x] == 1)
                {
                    instantiatedGO.Add(Instantiate(black, new Vector3(screenX + x, screenY + y, 0), Quaternion.identity));
                }
                else
                {
                    instantiatedGO.Add(Instantiate(white, new Vector3(screenX + x, screenY + y, 0), Quaternion.identity));
                }
            }
        }
    }

    private void Update()
    {
        //Uporabljeno za izbris loading ekrana iz drugega threda
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
        if (currentGenerated < generations.Count)
        {
            GeneratedGenerationsText.text = string.Format("{0}/{1}", generations.Count + 1, numOfGenerations);
            currentGenerated = generations.Count;
        }
    }

    public void ButtonSaveClick()
    {
        string folderPath = FolderPath;
        DateTime now = DateTime.Now;
        string folderName = "/output/" + now.ToString("yyyyMMddhhmmss");
        Directory.CreateDirectory(folderPath + folderName);
        t = new Thread(delegate ()
        {
            SaveAll(folderPath, folderName);
        });
        t.Start();
    }
    public void SaveAll(string folderPath, string folderName)
    {
        List<string> csvLines = new List<string>();
        csvLines.Add("TIME;"+TimeText.text);
        /*fit.OpenSpacesFitness = openSpacesFitness;
            fit.OuterWallFitness = outerWallFitness;
            fit.ClosedSpacesFitness = closedSpacesFitness;
            fit.DeadEndsFitness = deadEndsFitness;
            fit.WalledSpacesFitness = walledSpacesFitness;
            fit.CorridorFitness = corridorFitness;
            fit.SolvableFitness = solvable;*/
        csvLines.Add("OPEN SPACES FITNESS WEIGHT;OUTER WALL FITNESS WEIGHT;CLOSED SPACES FITNESS WEIGHT;DEAD END FITNESS WEIGHT;WALLED SPACES FITNESS WEIGHT;CORRIDOR FITNESS WEIGHT;SOLVABLE WEIGHT");
        csvLines.Add(string.Format("{0};{1};{2};{3};{4};{5};{6}",OpenSpacesWeight, OuterWallWeight, ClosedSpacesWeight, DeadEndWeight,WalledSpacesWeight, CorridorWeight, HasToBeSolvable));
        csvLines.Add("GENERATION;OPEN SPACES FITNESS;OUTER WALL FITNESS;CLOSED SPACES FITNESS;DEAD END FITNESS;WALLED SPACES FITNESS;CORRIDOR FITNESS;SOLVABLE;SCORE;SEED;IMAGE NAME");
        /*
        GENERATION;
        OPEN SPACES FITNESS;
        OUTER WALL FITNESS;
        CLOSED SPACES FITNESS;
        DEAD END FITNESS;
        WALLED SPACES FITNESS;
        CORRIDOR FITNESS;
        SOLVABLE;
        SCORE;
        SEED;
        IMAGE NAME
         */
        
        
        foreach (var generation in generations)
        {
            int index = 1;
            foreach (var iteration in generation.Value)
            {
                Debug.Log(string.Format("Saving GEN: {0}, ITE: {1}", generation.Key, index));
                string imgName = string.Format("GEN{0}_{1}.png",generation.Key, index++);
                csvLines.Add(string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10}", 
                    generation.Key, 
                    iteration.OpenSpacesFitness,
                    iteration.OuterWallFitness,
                    iteration.ClosedSpacesFitness,
                    iteration.DeadEndsFitness,
                    iteration.WalledSpacesFitness,
                    iteration.CorridorFitness,
                    iteration.SolvableFitness,
                    iteration.Score,
                    iteration.builder.StringSeed,
                    imgName));
                ///TODO: change slider value

                lock (_lock)
                {
                    // Add an action that requires the main thread
                    _mainThreadActions.Enqueue(() =>
                    {
                        G_Slider.value = generation.Key;
                        I_Slider.value = index;
                        Directory.CreateDirectory(folderPath + folderName + "/screenshots");
                        string path = folderPath + folderName + "/screenshots/" + imgName;
                        ScreenCapture.CaptureScreenshot(path);
                    });
                }
                while (G_Slider.value != generation.Key || I_Slider.value != index)
                {
                    if (I_Slider.value == 99 && G_Slider.value == generation.Key)
                    {
                        break;
                    }
                }
            }
        }
        File.WriteAllLines(folderPath + folderName + "/Data.csv", csvLines);
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
