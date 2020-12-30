using System.Collections.Generic;
using System.Linq;

namespace Algorithm
{
    public class Utils
    {
        /// <summary>
        /// Возвращает в solutions 2^t сообщений из t пар
        /// </summary>
        /// <param name="list"></param>
        /// <param name="solutions"></param>
        /// <param name="solution"></param>
        public static void Solve(List<List<ulong>> list, List<ulong[]> solutions, ulong[] solution)
        {
            if (solution.All(i => i != 0) && !solutions.Any(s => s.SequenceEqual(solution)))
                solutions.Add(solution);
            for (int i = 0; i < list.Count; i++)
            {
                if (solution[i] != 0)
                    continue; // a caller up the hierarchy set this index to be a number
                for (int j = 0; j < list[i].Count; j++)
                {
                    if (solution.Contains(list[i][j]))
                        continue;
                    var solutionCopy = solution.ToArray();
                    solutionCopy[i] = list[i][j];
                    Solve(list, solutions, solutionCopy);
                }
            }
        }
    }
}