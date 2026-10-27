using System.Collections.Generic;
using System.Linq;

public static class heartext {
    public static List<List<T>> Chunk<T>(this List<T> list, int chunkSize)
    {
        List<List<T>> ans = new List<List<T>>();
        int currentIndex = 0;
        while (currentIndex < list.Count)
        {
            ans.Add(list.Skip(currentIndex).Take(chunkSize).ToList());
            currentIndex += chunkSize;
        }
        return ans;
    }
    public static List<T> Append<T>(this List<T> list, T t)
    {
        list.Add(t);
        return list;
    }
    public static List<T> Append<T>(this List<T> list, List<T> addList)
    {
        foreach(T t in addList) list.Add(t);
        return list;
    }
    public static List<T> Remove<T>(this List<T> list, List<T> elementsToRemove){
        return list.Where(x => !elementsToRemove.Contains(x)).ToList();
    }
    public static List<T> Replace<T>(this List<T> list, T query, T replacement){
        int index = list.IndexOf(query);
        if (index == -1) return list;
        List<T> listCopy = list.ToList();
        listCopy[index] = replacement;
        return listCopy;
    }
}
