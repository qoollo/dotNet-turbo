using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// String distance calculation
    /// </summary>
    public static class StringDistance
    {
        /// <summary>
        /// Calculates the Hamming distance between two strings.
        /// Strings should have equal length
        /// </summary>
        /// <param name="strA">First string</param>
        /// <param name="strB">Second string</param>
        /// <returns>Hamming distance</returns>
        public static int GetHammingDistance(string strA, string strB)
        {
            Contract.Requires<ArgumentNullException>(strA != null);
            Contract.Requires<ArgumentNullException>(strB != null);
            Contract.Requires<ArgumentException>(strA.Length == strB.Length);

            int distance = 0;
            for (int i = 0; i < strA.Length; i++)
            {
                if (strA[i] != strB[i])
                    distance++;
            }

            return distance;
        }


        /// <summary>
        /// Calculates the Levenshtein distance between two strings
        /// </summary>
        /// <param name="strA">First string</param>
        /// <param name="strB">Second string</param>
        /// <returns>Levenshtein distance</returns>
        public static int GetLevenshteinDistance(string strA, string strB)
        {
            Contract.Requires<ArgumentNullException>(strA != null);
            Contract.Requires<ArgumentNullException>(strB != null);

            int boundHeight = strA.Length + 1;
            int boundWidth = strB.Length + 1;

            int[,] matrix = new int[boundHeight, boundWidth];

            for (int height = 0; height < boundHeight; height++) 
                matrix[height, 0] = height; 
            for (int width = 0; width < boundWidth; width++)
                matrix[0, width] = width; 

            for (int height = 1; height < boundHeight; height++)
            {
                for (int width = 1; width < boundWidth; width++)
                {
                    int cost = (strA[height - 1] == strB[width - 1]) ? 0 : 1;
                    int insertion = matrix[height, width - 1] + 1;
                    int deletion = matrix[height - 1, width] + 1;
                    int substitution = matrix[height - 1, width - 1] + cost;

                    int distance = Math.Min(insertion, Math.Min(deletion, substitution));

                    matrix[height, width] = distance;
                }
            }

            return matrix[boundHeight - 1, boundWidth - 1];
        }


        /// <summary>
        /// Calculates the Damerau-Levenshtein distance between two strings
        /// </summary>
        /// <param name="strA">First string</param>
        /// <param name="strB">Second string</param>
        /// <returns>Damerau-Levenshtein distance</returns>
        public static int GetDamerauLevenshteinDistance(string strA, string strB)
        {
            Contract.Requires<ArgumentNullException>(strA != null);
            Contract.Requires<ArgumentNullException>(strB != null);

            int boundHeight = strA.Length + 1;
            int boundWidth = strB.Length + 1;

            int[,] matrix = new int[boundHeight, boundWidth];

            for (int height = 0; height < boundHeight; height++)
                matrix[height, 0] = height;
            for (int width = 0; width < boundWidth; width++)
                matrix[0, width] = width;

            for (int height = 1; height < boundHeight; height++)
            {
                for (int width = 1; width < boundWidth; width++)
                {
                    int cost = (strA[height - 1] == strB[width - 1]) ? 0 : 1;
                    int insertion = matrix[height, width - 1] + 1;
                    int deletion = matrix[height - 1, width] + 1;
                    int substitution = matrix[height - 1, width - 1] + cost;

                    int distance = Math.Min(insertion, Math.Min(deletion, substitution));

                    if (height > 1 && width > 1 && strA[height - 1] == strB[width - 2] && strA[height - 2] == strB[width - 1])
                    {
                        distance = Math.Min(distance, matrix[height - 2, width - 2] + cost);
                    }

                    matrix[height, width] = distance;
                }
            }

            return matrix[boundHeight - 1, boundWidth - 1];
        }
    }
}
