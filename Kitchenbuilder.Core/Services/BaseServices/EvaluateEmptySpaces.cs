using Kitchenbuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kitchenbuilder.Core
{
    public static class EvaluateEmptySpaces
    {
        /// <summary>
        /// מערך הצעות לפריסת מטבח על פי המקטעים הריקים (לכל הקירות).
        /// </summary>
        /// <param name="filteredEmptySpaces">
        /// מילון: 
        /// מפתח = אינדקס הקיר, 
        /// ערך = רשימת מקטעים ריקים עם Tuple של 6 ערכים:
        /// (double start, double end, bool hasDoor, bool hasWindow, bool foo, bool bar)
        /// </param>
        /// <param name="kitchen">אובייקט המטבח עם פרטי קירות ורצפה</param>
        public static List<Dictionary<int, (string appliance, double start, double end)>>
            Evaluate(
                Dictionary<int, List<(double start, double end, bool hasDoor, bool hasWindow, bool foo, bool bar)>> filteredEmptySpaces,
                Kitchen kitchen)
        {
            // ממירים פנימית לפורמט ש־HandleOneWall מצפה לו:
            var simpleEmptySpaces = filteredEmptySpaces
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                              .Select(t => (t.start, t.end))
                              .ToList()
                );

            var layoutSuggestions = new List<Dictionary<int, (string appliance, double start, double end)>>();

            int wallCount = kitchen.Walls.Count;
            if (wallCount == 1)
            {
                // קריאה ל־HandleOneWall עם המקטעים הכחולים בלבד
                var suggestion = HandleOneWall.Evaluate(kitchen, simpleEmptySpaces);
                layoutSuggestions.Add(suggestion);
            }
            else
            {
                Console.WriteLine("הטיפול בקירות מרובים יתבצע בהמשך.");
                // TODO: לוגיקה ל־2 קירות ו־U-Base
            }

            return layoutSuggestions;
        }
    }
}
