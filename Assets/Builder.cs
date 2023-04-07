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
            //Vsako črko pretvorimo v binarno vrednost in uporabimo to vrednost za izris labirinta
            string seedInBinary = "";
            foreach (char c in StringSeed)
            {
                seedInBinary += Convert.ToString(c, 2);
            }
            int start = 0;
            int index = start;
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    Maze[i, j] = int.Parse(seedInBinary[index++].ToString());
                    if (index >= seedInBinary.Length)
                    {
                        start = r.Next(0, seedInBinary.Length);
                        index = start;
                    }
                }
            }
        }

        public static string GenerateChildFromParents(Builder parent1, Builder parent2)
        {
            string childStringSeed = "";
            //45% da vzame gen od straša1
            //45% da vzame gen od starša2
            //5% da generira novi gen MUTACIJA
            for (int i = 0; i < parent1.StringSeed.Length; i++)
            {
                int chance = parent1.r.Next(1, 101);
                if (chance < 46)
                {
                    childStringSeed += parent1.StringSeed[i];
                }
                else if (chance < 96)
                {
                    childStringSeed += parent2.StringSeed[i];
                }
                else
                {
                    char letter = 'a';
                    switch (parent1.r.Next(0, 3))
                    {
                        case 0: letter = (char)parent1.r.Next(48, 58); break;
                        case 1: letter = (char)parent1.r.Next(65, 91); break;
                        case 2: letter = (char)parent1.r.Next(97, 123); break;
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
