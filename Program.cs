using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

class Robot
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string Direction { get; set; }

    public Robot(int id, int x, int y, string direction)
    {
        Id = id;
        X = x;
        Y = y;
        Direction = direction;
    }
}

class Node
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Weight { get; set; }

    public Node(int x, int y, int weight)
    {
        X = x;
        Y = y;
        Weight = weight;
    }

    public bool IsWall => Weight == 0;

    public override string ToString()
    {
        return "Y=" + Y + " X=" + X;
    }
}

class Graph
{
    public Node[][] Grid { get; set; }
}


class Player
{
    static IList<string> DIRECTIONS = new List<string>(){"R","L","D","U"};
    
    private static IDictionary<Tuple<int, int>, IList<string>> GetSavedDirectionsCopy(
        IDictionary<Tuple<int, int>, IList<string>> savedDirections)
    {
        var res = new Dictionary<Tuple<int, int>, IList<string>>();
        foreach (var sdKey in savedDirections.Keys)
        {
            res.Add(sdKey, new List<string>());
            foreach (var str in savedDirections[sdKey])
            {
                res[sdKey].Add(str);
            }
        }

        return res;
    }
    
    private static Dictionary<Tuple<int, int>, string> GetMarkersCopy(
        Dictionary<Tuple<int, int>, string> markers)
    {
        var res = new Dictionary<Tuple<int, int>, string>();
        foreach (var mKey in markers.Keys)
        {
            res.Add(mKey, markers[mKey]);
        } 

        return res;
    }
    
    static Node GetNextNode(
        int y, 
        int x, 
        string direction, 
        Graph graph,  
        IDictionary<Tuple<int, int>, IList<string>> savedDirections,
        Dictionary<Tuple<int, int>, string> markers,
        out bool isCycle)
    {
        isCycle = false;
        var sdKey = new Tuple<int, int>(y, x);
        if (markers.ContainsKey(sdKey)) direction = markers[sdKey];
        
        switch (direction) {
            case "R":
                x++;
                break;
            case "L":
                x--;
                break;
            case "U":
                y--;
                break;
            case "D":
                y++;
                break;
        }

        if (y < 0 || x < 0 || y >= graph.Grid.Length || x >= graph.Grid[y].Length || (graph.Grid[y][x]).IsWall)
            return null;
        
        var nextSdKey = new Tuple<int, int>(y, x);
        if (savedDirections.ContainsKey(nextSdKey) && savedDirections[nextSdKey].Any(sd => sd == direction))
        {
            isCycle = true;
            return null;
        }
        return graph.Grid[y][x];
    }

    static IList<Node> GetBestTurnPath(
        int y, 
        int x, 
        Graph graph, 
        IDictionary<Tuple<int, int>, IList<string>> savedDirections, 
        Dictionary<Tuple<int, int>, string> markers,
        string currDirection,
        bool isStart)
    {
        IList<Node> nextPath = null;
        var markerKey = new Tuple<int, int>(y, x);
        
        if (markers.ContainsKey(markerKey))
        {
            nextPath = MakeMaxPath(y, x, markers[markerKey], graph, GetSavedDirectionsCopy(savedDirections), markers);
        }
        //если мы уже были в этой позиции, то здесь нельзя ставить стрелку
        else if (savedDirections.ContainsKey(markerKey) && savedDirections[markerKey].Any())
        {
            nextPath = MakeMaxPath(y, x, currDirection, graph,
                GetSavedDirectionsCopy(savedDirections), markers);
        }
        else
        {
            Dictionary<Tuple<int, int>, string> bestMarkers = null;
           
            foreach (var d in DIRECTIONS)
            {
                var mc = GetMarkersCopy(markers);
                if (!isStart || currDirection != d) mc[markerKey] = d;
                var sdc = GetSavedDirectionsCopy(savedDirections);
                if (!sdc.ContainsKey(markerKey)) sdc.Add(markerKey, new List<string>());
                sdc[markerKey].Add(d);
                
                var directionPath =
                    MakeMaxPath(y, x, d, graph, sdc, mc);
                if (directionPath != null && (nextPath == null || directionPath.Count > nextPath.Count))
                {
                    nextPath = directionPath;
                    bestMarkers = mc;
                }
            }
            if (bestMarkers != null)
            {
                foreach (var key in bestMarkers.Keys)
                {
                    if (!markers.ContainsKey(key)) markers.Add(key, bestMarkers[key]);
                }
            }
        }

        return nextPath;
    }

    static IList<Node> MakeMaxPath(
        int y, 
        int x, 
        string direction, 
        Graph graph, 
        IDictionary<Tuple<int, int>, IList<string>> savedDirections, 
        Dictionary<Tuple<int, int>, string> markers)
    {
        bool isCycled;
        var nextNode = GetNextNode(y, x, direction, graph, savedDirections, markers, out isCycled);
        var currPath = new List<Node>();

        while (nextNode != null){
            currPath.Add(nextNode);
            var sdKey = new Tuple<int, int>(nextNode.Y, nextNode.X);
            if (!savedDirections.ContainsKey(sdKey)) savedDirections.Add(sdKey, new List<string>());
            savedDirections[sdKey].Add(direction);
            nextNode = GetNextNode(nextNode.Y, nextNode.X, direction, graph, savedDirections, markers, out isCycled);
        }
        if (currPath.Count == 0) return null;
        if (isCycled) return currPath;

//        IList<Node> bestNextPath = null;
//        var bestI = -1;
//        Dictionary<Tuple<int, int>, string> bestMarkers = null;
//        for (var i = 0; i < currPath.Count; ++i)
//        {
//            var step = currPath[i];
//            var sdCopy = GetSavedDirectionsCopy(savedDirections);
//            sdCopy[new Tuple<int, int>(step.Y, step.X)].Remove(direction);//т.к. это последний объект в ряду, его направление изменится 
//            var markersCopy = GetMarkersCopy(markers);
//            IList<Node> nextPath = GetBestTurnPath(step.Y, step.X, graph, sdCopy, markersCopy, direction, false);
//            if (nextPath != null && (bestNextPath == null || nextPath.Count + i > bestNextPath.Count + bestI))
//            {
//                bestNextPath = nextPath;
//                bestMarkers = markersCopy;
//                bestI = i;
//            }
//        }

        var lastY = currPath[currPath.Count - 1].Y;
        var lastX = currPath[currPath.Count - 1].X;
        savedDirections[new Tuple<int, int>(lastY, lastX)].Remove(direction);//т.к. это последний объект в ряду, его направление изменится 
        IList<Node> nextPath = GetBestTurnPath(lastY, lastX, graph, savedDirections, markers, direction, false);
        if (nextPath != null) foreach (var step in nextPath) currPath.Add(step);
//        if (bestMarkers != null)
//        {
//            foreach (var key in bestMarkers.Keys)
//            {
//                if (!markers.ContainsKey(key)) markers.Add(key, bestMarkers[key]);
//            }
//        }
//        
//        if (bestNextPath != null) foreach (var step in bestNextPath) currPath.Add(step);
        return currPath;
    }
    
    static void Main(string[] args)
    {
        var grid = new Node[10][];
        var existingMarkers = new Dictionary<Tuple<int, int>, string>();
       
        for (int i = 0; i < 10; i++)
        {
            string line = Console.ReadLine();
            Console.Error.WriteLine(line);
            var row = new Node[line.Length];
            var symbols = line.ToCharArray();
            for (var j = 0; j < symbols.Length; ++j)
            {
                var weight = 1;
                if (symbols[j] == '#')
                {
                    weight = 0;
                }
                else if (symbols[j] != '.')
                {
                    existingMarkers.Add(new Tuple<int, int>(i, j), symbols[j].ToString());
                }
                row[j] = new Node(j, i, weight);
            }

            grid[i] = row;
        }

        var graph = new Graph() {Grid = grid};

        var robots = new List<Robot>();
        
        int robotCount = int.Parse(Console.ReadLine());
        Console.Error.WriteLine(robotCount);
        for (int i = 0; i < robotCount; i++)
        {
            string[] inputs = Console.ReadLine().Split(' ');
            int x = int.Parse(inputs[0]);
            int y = int.Parse(inputs[1]);
            string direction = inputs[2];
            robots.Add(new Robot(i, x, y, direction));
            Console.Error.WriteLine(x + " " + y + " " + direction);
        }

        var res = "";

        for (var r = 0; r < robotCount; ++r)
        {
            var robot = robots[r];
            var savedDirections = new Dictionary<Tuple<int, int>, IList<string>>();
            var markers = new Dictionary<Tuple<int,int>,string>();
            foreach (var em in existingMarkers)
                markers[em.Key] = em.Value;
            
            var btPath = GetBestTurnPath(robot.Y, robot.X, graph, savedDirections, markers, robot.Direction, true);

            foreach (var item in markers.Where(i => !existingMarkers.ContainsKey(i.Key)))
            {
                res += item.Key.Item2 + " " + item.Key.Item1 + " " + item.Value + " ";
            }
        }

        

        Console.WriteLine(res.Remove(res.Length - 1));
    }
}