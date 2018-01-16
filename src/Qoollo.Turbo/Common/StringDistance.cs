using System;
using System.Collections.Generic;
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
            if (strA == null)
                throw new ArgumentNullException(nameof(strA));
            if (strB == null)
                throw new ArgumentNullException(nameof(strB));
            if (strA.Length != strB.Length)
                throw new ArgumentException("Strings should have equal length");

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
        /// <param name="withTranspositions">Take symbol transposition into account (Damerau-Levenshtein distance)</param>
        /// <returns>Levenshtein distance</returns>
        public static int GetLevenshteinDistance(string strA, string strB, bool withTranspositions)
        {
            if (strA == null)
                throw new ArgumentNullException(nameof(strA));
            if (strB == null)
                throw new ArgumentNullException(nameof(strB));

            int boundHeight = strA.Length + 1;
            int boundWidth = strB.Length + 1;

            int[] prevPrevLine = withTranspositions ? new int[boundWidth] : null;
            int[] previousLine = new int[boundWidth];
            int[] currentLine = new int[boundWidth];
            int[] tmpLineSwap = null;

            for (int width = 0; width < boundWidth; width++)
                currentLine[width] = width;

            for (int height = 1; height < boundHeight; height++)
            {
                if (withTranspositions)
                {
                    tmpLineSwap = prevPrevLine;
                    prevPrevLine = previousLine;
                    previousLine = currentLine;
                    currentLine = tmpLineSwap;
                }
                else
                {
                    tmpLineSwap = previousLine;
                    previousLine = currentLine;
                    currentLine = tmpLineSwap;
                }

                currentLine[0] = height;

                for (int width = 1; width < boundWidth; width++)
                {
                    int cost = (strA[height - 1] == strB[width - 1]) ? 0 : 1;
                    int insertion = currentLine[width - 1] + 1;
                    int deletion = previousLine[width] + 1;
                    int substitution = previousLine[width - 1] + cost;

                    int distance = Math.Min(insertion, Math.Min(deletion, substitution));

                    if (withTranspositions && height > 1 && width > 1 && strA[height - 1] == strB[width - 2] && strA[height - 2] == strB[width - 1])
                    {
                        distance = Math.Min(distance, prevPrevLine[width - 2] + cost);
                    }

                    currentLine[width] = distance;
                }
            }

            return currentLine[boundWidth - 1];
        }


        /// <summary>
        /// Calculates the Levenshtein distance between two strings
        /// </summary>
        /// <param name="strA">First string</param>
        /// <param name="strB">Second string</param>
        /// <returns>Levenshtein distance</returns>
        public static int GetLevenshteinDistance(string strA, string strB)
        {
            return GetLevenshteinDistance(strA, strB, false);
        }
    }
}
