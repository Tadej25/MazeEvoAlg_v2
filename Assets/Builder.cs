using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets
{
    public class Builder
    {
        public int Size { get; set; }
        public int NumOfWalls { get; set; }
        public int Seed { get; set; }
        public string StringSeed { get; set; }
        public int[,] Maze { get; set; }
        public List<string> ChildrenSeeds { get; set; }
        public System.Random r { get; set; }

        public Builder(int size, int numOfWalls, string seed, System.Random ra)
        {
            Size = size;
            NumOfWalls = numOfWalls;
            StringSeed = seed;
            Seed = seed.Length > 9 ? seed.GetHashCode() : GenerateSeedFromString(StringSeed, ra);
            //Debug.Log(string.Format("Seed sent in: {0}, seed generated: {1}", StringSeed, Seed));
            Maze = new int[Size, Size];
            r = new System.Random(Seed);
            ChildrenSeeds = new List<string>();
        }

        public Builder(Builder mama, Builder papa)
        {

        }

        public int GenerateSeedFromString(string seed, System.Random r)
        {
            string newSeed = "";
            foreach (char letter in seed)
            {
                int intChar = ReduceIntSize(letter);
                newSeed += intChar;
            }
            int nSeed = int.Parse(newSeed);
            return r.Next(0, 2) == 0 ? nSeed : nSeed * (-1);
        }

        public int ReduceIntSize(int number)
        {
            if (number.ToString().Length > 1)
            {
                int newNumber = 0;
                foreach (char num in number.ToString())
                {
                    newNumber += int.Parse(num.ToString());
                }
                if (newNumber.ToString().Length > 1)
                {
                    newNumber = ReduceIntSize(newNumber);
                }
                return newNumber;
            }
            return number;
        }

        public void BuildMaze()
        {
            long sumStringSeed = 0;
            int stringSeedCurrentIndex = 0;

            foreach (char letter in StringSeed)
            {
                sumStringSeed += letter;
            }
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    long currentStringSeedLetterValue = StringSeed[stringSeedCurrentIndex++];
                    
                    if (stringSeedCurrentIndex >= StringSeed.Length)
                        stringSeedCurrentIndex = 0;

                    if ((currentStringSeedLetterValue + sumStringSeed) % 2 == 0)
                    {
                        Maze[i, j] = 1;
                        NumOfWalls--;
                    }
                }
            }
        }

        public static string GenerateChildFromParents(Builder parent1, Builder parent2)
        {
            string childStringSeed = "";
            for (int i = 0; i < parent1.StringSeed.Length; i++)
            {

                //Vzamemo trenutno črko od obeh strašev
                //Njihove char vrednosti seštejemo
                //Vzamemo zadnjo številko od seštevka
                int parentOneCurrentLetter = parent1.StringSeed[i];
                int parentTwoCurrentLetter = parent2.StringSeed[i];
                string combinedParentLettersString = (parentOneCurrentLetter + parentTwoCurrentLetter).ToString();
                int lastNumberOfCombinedParentString = combinedParentLettersString[combinedParentLettersString.Length - 1];

                if ((parentOneCurrentLetter % 2 == 0 && parentTwoCurrentLetter % 2 != 0) ||
                    (parentOneCurrentLetter % 2 != 0 && parentTwoCurrentLetter % 2 == 0))
                {
                    childStringSeed += parent1.StringSeed[i];
                }
                //Če so obe številki trenutne črke strašev sodi potev vzami lrko straša 2 
                else if(parentOneCurrentLetter % 2 == 0 && parentTwoCurrentLetter % 2 == 0)
                {
                    childStringSeed += parent2.StringSeed[i];
                }
                //Drugače vzami naključno črko
                else
                {
                    char letter = (char)parent1.r.Next(65, 91);
                    //Zagotovimo da generiramo različno črko od staršev
                    while (letter == parent1.StringSeed[i] || letter == parent2.StringSeed[i])
                    {
                        letter = (char)parent1.r.Next(65, 91);
                        switch (parent1.r.Next(0, 3))
                        {
                            case 0: letter = (char)parent1.r.Next(48, 58); break;
                            case 1: letter = (char)parent1.r.Next(97, 123); break;
                        }
                    }
                    childStringSeed += letter;
                }
            }
            return childStringSeed;
        }
        public static int ComputeSimilarity(string s, string t)
        {
            try
            {
                int n = s.Length;
                int m = t.Length;
                int[,] d = new int[n + 1, m + 1];

                // Verify arguments.
                if (n == 0)
                {
                    return m;
                }

                if (m == 0)
                {
                    return n;
                }

                // Initialize arrays.
                for (int i = 0; i <= n; d[i, 0] = i++)
                {
                }

                for (int j = 0; j <= m; d[0, j] = j++)
                {
                }

                // Begin looping.
                for (int i = 1; i <= n; i++)
                {
                    for (int j = 1; j <= m; j++)
                    {
                        // Compute cost.
                        int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                        d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                    }
                }
                // Return cost.
                return d[n, m];
            }
            catch (Exception e)
            {
                Debug.LogError("StringCompareError");
            }
            return -1;
        }
    }
}
