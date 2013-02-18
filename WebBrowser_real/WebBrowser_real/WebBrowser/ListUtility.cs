using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebBrowser
{
    public class ListUtility
    {
        public static List<List<T>> SplitList<T>(List<T> items, int groupCount)
        {
            List<List<T>> allGroups = new List<List<T>>();

            //split the list into equal groups
            int startIndex = 0;
            int groupLength = (int)Math.Round((double)items.Count / (double)groupCount, 0);
            while (startIndex < items.Count)
            {
                List<T> group = new List<T>();
                group.AddRange(items.GetRange(startIndex, groupLength));
                startIndex += groupLength;

                //adjust group-length for last group
                if (startIndex + groupLength > items.Count)
                {
                    groupLength = items.Count - startIndex;
                }

                allGroups.Add(group);
            }

            //merge last two groups, if more than required groups are formed
            if (allGroups.Count > groupCount && allGroups.Count > 2)
            {
                allGroups[allGroups.Count - 2].AddRange(allGroups.Last());
                allGroups.RemoveAt(allGroups.Count - 1);
            }

            return (allGroups);
        }
    }
}
