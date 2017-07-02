using BMAPI.v1.HitObjects;
using ReplayAPI;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace osuDodgyMomentsFinder
{
    static class Utils
    {
        /// <summary>
        /// Partitions the given list around a pivot element such that all elements on left of pivot are <= pivot
        /// and the ones at thr right are > pivot. This method can be used for sorting, N-order statistics such as
        /// as median finding algorithms.
        /// Pivot is selected ranodmly if random number generator is supplied else its selected as last element in the list.
        /// Reference: Introduction to Algorithms 3rd Edition, Corman et al, pp 171
        /// </summary>
        private static int Partition<T>(this IList<T> list, int start, int end, Random rnd = null) where T : IComparable<T>
        {
            if (rnd != null)
                list.Swap(end, rnd.Next(start, end));

            var pivot = list[end];
            var lastLow = start - 1;
            for (var i = start; i < end; i++)
            {
                if (list[i].CompareTo(pivot) <= 0)
                    list.Swap(i, ++lastLow);
            }
            list.Swap(end, ++lastLow);
            return lastLow;
        }

        /// <summary>
        /// Returns Nth smallest element from the list. Here n starts from 0 so that n=0 returns minimum, n=1 returns 2nd smallest element etc.
        /// Note: specified list would be mutated in the process.
        /// Reference: Introduction to Algorithms 3rd Edition, Corman et al, pp 216
        /// </summary>
        public static T NthOrderStatistic<T>(this IList<T> list, int n, Random rnd = null) where T : IComparable<T>
        {
            return NthOrderStatistic(list, n, 0, list.Count - 1, rnd);
        }
        private static T NthOrderStatistic<T>(this IList<T> list, int n, int start, int end, Random rnd) where T : IComparable<T>
        {
            while (true)
            {
                var pivotIndex = list.Partition(start, end, rnd);
                if (pivotIndex == n)
                    return list[pivotIndex];

                if (n < pivotIndex)
                    end = pivotIndex - 1;
                else
                    start = pivotIndex + 1;
            }
        }

        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            if (i == j)   //This check is not required but Partition function may make many calls so its for perf reason
                return;
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        /// <summary>
        /// Note: specified list would be mutated in the process.
        /// </summary>
        public static T Median<T>(this IList<T> list) where T : IComparable<T>
        {
            return list.NthOrderStatistic((list.Count - 1) / 2);
        }

        public static double Median<T>(this IEnumerable<T> sequence, Func<T, double> getValue)
        {
            var list = sequence.Select(getValue).ToList();
            var mid = (list.Count - 1) / 2;
            return list.NthOrderStatistic(mid);
        }

        public static double derivative(double a, double b, double c, double h)
        {
            return (-a + 2 * b - c) / h / h;
        }

        public static void outputValues<T>(List<T> values, string fileName)
        {
            string res = "";
            foreach (var value in values)
            {
                res += value + ",";
            }
            if (res.Length > 0)
                res.Remove(res.Length - 1);

            File.WriteAllText(fileName, res);
        }

        //Assumes sorted
        public static double median(List<double> values)
        {
            if(values.Count % 2 == 0)
                return (values[values.Count / 2 - 1] + values[values.Count / 2]) / 2;
            else
                return values[values.Count / 2];
        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static T[] SubArray<T>(this T[] data, int index)
        {
            int length = data.Length - index;
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static int sign(int value)
        {
            return value > 0 ? 1 : 0;
        }


        public static double sqr(double x)
        {
            return x * x;
        }

        public static double dist(ReplayFrame frame1, ReplayFrame frame2)
        {
            return dist(frame1.X, frame1.Y, frame2.X, frame2.Y);
        }

        public static double dist(float x1, float y1, float x2, float y2)
        {
            return Math.Sqrt(sqr(x1 - x2) + sqr(y1 - y2));
        }

        public static string printInfo(ReplayFrame frame, CircleObject obj)
        {
            return "Hit " + obj.Type.ToString() + " (starting at " + obj.StartTime + ")" + " at " + frame.Time;
        }

        public static double pixelPerfectHitFactor(ReplayFrame frame, CircleObject obj)
        {
            return dist(frame.X, frame.Y, obj.Location.X, obj.Location.Y) / obj.Radius;
        }

        public static double variance(List<double> values)
        {
            double avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }
    }

    static class UIUtils
    {
        public static string[] getArgsFromUser()
        {
            DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);


            // get beatmap
            Console.WriteLine("SONG LIST: ");

            FileInfo[] files = directory.GetFiles();
            List<string> beatmaps = new List<string>();
            int counter = 1;
            foreach(FileInfo file in files)
            {
                if(file.Extension == ".osu")
                {
                    beatmaps.Add(file.Name);
                    Console.WriteLine(counter.ToString() + "\t" + file.Name);
                    ++counter;
                }
            }

            Console.Write("Pick number: ");
            int songID = int.Parse(Console.ReadLine());


            // get replay
            Console.Clear();
            Console.WriteLine("PLAY LIST: ");
            List<string> replays = new List<string>();
            counter = 1;
            foreach(FileInfo file in files)
            {
                if(file.Extension == ".osr")
                {
                    replays.Add(file.Name);
                    Console.WriteLine(counter.ToString() + "\t" + file.Name);
                    ++counter;
                }
            }

            Console.Write("Pick number: ");
            int playID = int.Parse(Console.ReadLine());

            Console.Clear();
            return new string[] { beatmaps[songID - 1], replays[playID - 1] };
        }
    }
}
