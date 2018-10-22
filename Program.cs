using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

class Robot
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public char Direction { get; set; }

    public Robot(int id, int x, int y, char direction)
    {
        Id = id;
        X = x;
        Y = y;
        Direction = direction;
    }
}

class Step
{
    public Tuple<int,int> Pos { get; set; }
    public char Direction { get; set; }
}

class PathMapContainer
{
    public IList<Step> Path { get; set; }
    public char[][] Map { get; set; }

    public PathMapContainer(IList<Step> path, char[][] map)
    {
        Path = path;
        Map = map;
    }
}

class Player
{
    private static Random _rnd = new Random();
    static IList<char> DIRECTIONS = new List<char>(){'R','L','D','U'};
    private static Tuple<int, int>[][] _tuples;

    static int GetNormedCoord(int coord, int coordsCount)
    {
        if (coord < 0) coord = coordsCount - 1;
        else if (coord > coordsCount - 1) coord = 0;
        return coord;
    }

    static IDictionary<char[][], IList<Robot>> GetSplitedGrids(char[][] grid, IList<Robot> robots)
    {
        var res = new Dictionary<char[][], IList<Robot>>();
        var gotRobots = new List<Robot>();
        foreach (var robor in robots.Where(r => !gotRobots.Contains(r)))
        {
            var sg = new Dictionary<Tuple<int, int>, IList<Robot>>();
            BuildSplitedGrid(robor.Y, robor.X, grid, robots, sg);
            var splitedGrid = new char[grid.Length][];
            for (int i = 0; i < grid.Length; ++i)
            {
                splitedGrid[i] = Enumerable.Repeat('#', grid[i].Length).ToArray();
            }

            var newRobots = new List<Robot>();
            foreach (var pos in sg.Keys)
            {
                splitedGrid[pos.Item1][pos.Item2] = grid[pos.Item1][pos.Item2];
                newRobots.AddRange(sg[pos]);
            }
            res.Add(splitedGrid, newRobots);
            gotRobots.AddRange(newRobots);
        }

        return res;
    }

    static void BuildSplitedGrid(int y, int x, char[][] grid, IList<Robot> robots, Dictionary<Tuple<int, int>, IList<Robot>> splitedGrid)
    {
        //if (y < 0 || x < 0 || y > grid.Length - 1 || x > grid[y].Length - 1) return;
        if (grid[y][x] == '#') return;

        var pos = _tuples[y][x];
        if (splitedGrid.ContainsKey(pos)) return;
        splitedGrid.Add(pos, new List<Robot>());
        var robot = robots.SingleOrDefault(r => r.Y == y && r.X == x);
        if (robot != null) splitedGrid[pos].Add(robot);
        BuildSplitedGrid(GetNormedCoord(y-1, grid.Length),x, grid, robots, splitedGrid);
        BuildSplitedGrid(GetNormedCoord(y+1, grid.Length),x, grid, robots, splitedGrid);
        BuildSplitedGrid(y,GetNormedCoord(x-1, grid[y].Length), grid, robots, splitedGrid);
        BuildSplitedGrid(y,GetNormedCoord(x+1, grid[y].Length), grid, robots, splitedGrid);
    }
    
    static void BuildPath(int y, int x, char direction, char[][] graph, IDictionary<Tuple<int, int>, IList<char>> path)
    {
        //var isOutsideGraph = y < 0 || x < 0 || y > graph.Length - 1 || x > graph[y].Length - 1;
        //if (isOutsideGraph) return; //вышли за пределы карты
        if (graph[y][x] == '#') return;//упали в пропасть

        if (graph[y][x] != '.')
        {
            direction = graph[y][x];
        }

        var key = _tuples[y][x]; 
        if (!path.ContainsKey(key)) path.Add(key, new List<char>());
        if (path[key].Any(s => s == direction))//мы зациклились
        {
            return;
        }
        
        path[key].Add(direction);

        switch (direction)
        {
            case 'R':
                x++;
                break;
            case 'L':
                x--;
                break;
            case 'U':
                y--;
                break;
            case 'D':
                y++;
                break;
        }

        y = GetNormedCoord(y, graph.Length);
        x = GetNormedCoord(x, graph[y].Length);

        BuildPath(y, x, direction, graph, path);
    }

    static int GetPathCount(IDictionary<Tuple<int, int>, IList<char>> path)
    {
        var count = 0;
        foreach (var key in path.Keys)
        {
            count += path[key].Count;
        }

        return count;
    }

    static bool IsUpWall(char[][] graph, int i, int j)
    {
        return graph[GetNormedCoord(i - 1, graph.Length)][j] == '#';
    }
    
    static bool IsDownWall(char[][] graph, int i, int j)
    {
        return graph[GetNormedCoord(i + 1, graph.Length)][j] == '#';
    }
    
    static bool IsLeftWall(char[][] graph, int i, int j)
    {
        return graph[i][GetNormedCoord(j - 1, graph[i].Length)] == '#';
    }
    
    static bool IsRightWall(char[][] graph, int i, int j)
    {
        return  graph[i][GetNormedCoord(j + 1, graph[i].Length)] == '#';
    }

    static void InitTuples(char[][]grid)
    {
        _tuples = new Tuple<int, int>[grid.Length][];
        for (var i = 0; i < grid.Length; ++i)
        {
            _tuples[i] = new Tuple<int, int>[grid[i].Length];
            for (var j = 0; j < grid[i].Length; ++j)
                _tuples[i][j] = new Tuple<int, int>(i, j);
        }
    }

    static IList<PathMapContainer> GetOneDirectionContainer(Tuple<int, int> pos, char[][] grid, IList<Step> currPath,
        char currDirection, char newDirection)
    {
        if (currPath.Any(s => s.Pos.Equals(pos) && s.Direction == newDirection)) return null;
        int y = -1;
        int x = -1;

        switch (newDirection)
        {
            case 'D':
                y = GetNormedCoord(pos.Item1 + 1, grid.Length);
                x = pos.Item2;
                break;
            case 'U':
                y = GetNormedCoord(pos.Item1 - 1, grid.Length);
                x = pos.Item2;
                break;
            case 'L':
                y = pos.Item1;
                x = GetNormedCoord(pos.Item2 - 1, grid[pos.Item1].Length);
                break;
            case 'R':
                y = pos.Item1;
                x = GetNormedCoord(pos.Item2 + 1, grid[pos.Item1].Length);
                break;
        }

        var currPathCopy = currPath.ToList();
        currPathCopy.Add(new Step() {Pos = pos, Direction = newDirection});
        var isGridChanged = false;
        if (grid[pos.Item1][pos.Item2] == '.' && currDirection != newDirection)
        {
            grid[pos.Item1][pos.Item2] = newDirection;
            isGridChanged = true;
        }
        var pathes = GetAllPossiblePathes(_tuples[y][x], newDirection, grid, currPathCopy);
        if (isGridChanged) grid[pos.Item1][pos.Item2] = '.';
        return pathes;
    }


    private static int _counter = 0;
    static IList<PathMapContainer> GetAllPossiblePathes(Tuple<int, int> pos, char direction, char[][] grid, IList<Step> currPath)
    {
        if (grid[pos.Item1][pos.Item2] == '#') return null;
        
        _counter++;
        
        var isCorridorCell = false;
        if (IsLeftWall(grid, pos.Item1, pos.Item2) && IsRightWall(grid, pos.Item1, pos.Item2) &&
            !IsUpWall(grid, pos.Item1, pos.Item2) && !IsDownWall(grid, pos.Item1, pos.Item2))
        {
            isCorridorCell = true;
        }
        else if (!IsLeftWall(grid, pos.Item1, pos.Item2) && !IsRightWall(grid, pos.Item1, pos.Item2) &&
                 IsUpWall(grid, pos.Item1, pos.Item2) && IsDownWall(grid, pos.Item1, pos.Item2))
        {
            isCorridorCell = true;
        } 
      
        var res = new List<PathMapContainer>();

        if (grid[pos.Item1][pos.Item2] != '.')//стрелка на карте
        {
            var odc = GetOneDirectionContainer(pos, grid, currPath, direction, grid[pos.Item1][pos.Item2]);
            if (odc != null) res.AddRange(odc);
        }
        else if (currPath.Any(s => s.Pos.Equals(pos)) || isCorridorCell)//уже были в этой точке. нельзя менять направление
        {
            var odc = GetOneDirectionContainer(pos, grid, currPath, direction, direction);
            if (odc != null) res.AddRange(odc);
        }
        else
        {
            if (_counter > 10275)
            {
                var d = DIRECTIONS[_rnd.Next(DIRECTIONS.Count)];
                var odc = GetOneDirectionContainer(pos, grid, currPath, direction, d);
                if (odc != null) res.AddRange(odc);
            }
            else
            {
                foreach (var d in DIRECTIONS)
                {
                    var odc = GetOneDirectionContainer(pos, grid, currPath, direction, d);
                    if (odc != null) res.AddRange(odc);
                }
            }
        }
        
        //grid[pos.Item1][pos.Item2] = '.';
        if (res.Count == 0) res.Add(new PathMapContainer(currPath, grid.Select(a => a.ToArray()).ToArray()));
        
        return res;
    }

    static void Main(string[] args)
    {
        var grid = new char[10][];
       
        for (int i = 0; i < 10; i++)
        {
            string line = Console.ReadLine();
            Console.Error.WriteLine(line);
            var row = new char[line.Length];
            for (var j = 0; j < line.Length; ++j)
                row[j] = line[j];
            grid[i] = row;
        }
        InitTuples(grid);

        var robots = new List<Robot>();
        
        int robotCount = int.Parse(Console.ReadLine());
        Console.Error.WriteLine(robotCount);
        for (int i = 0; i < robotCount; i++)
        {
            string[] inputs = Console.ReadLine().Split(' ');
            int x = int.Parse(inputs[0]);
            int y = int.Parse(inputs[1]);
            var direction = inputs[2][0];
            robots.Add(new Robot(i, x, y, direction));
            Console.Error.WriteLine(x + " " + y + " " + direction);
        }
        var res = "";
        var splitedGris = GetSplitedGrids(grid, robots);
        foreach (var sg in splitedGris.Keys)
        {
            _counter = 0;
            PathMapContainer maxLengthApp = null;
            var maxLength = -1;
            for (var r = splitedGris[sg].Count - 1; r >= 0; --r)
            {
                var startRobot = splitedGris[sg][r];
                var apps = GetAllPossiblePathes(_tuples[startRobot.Y][startRobot.X], startRobot.Direction, sg, new List<Step>());
                
                foreach (var app in apps)
                {
                    var summPathLength = app.Path.Count;
                    for (var i = 0; i < splitedGris[sg].Count; ++i)
                    {
                        if (i==r) continue;
                        var currRobot = splitedGris[sg][i];
                        var path = new Dictionary<Tuple<int, int>, IList<char>>();
                        BuildPath(currRobot.Y, currRobot.X, currRobot.Direction, sg, path);
                        var pathLength = GetPathCount(path);
                        summPathLength += pathLength;
                    }

                    if (summPathLength > maxLength)
                    {
                        maxLength = summPathLength;
                        maxLengthApp = app;
                    }
                }
            }
            
            
            if (maxLengthApp == null) continue;
            
            var map = maxLengthApp.Map;
            for (int i = 0; i < map.Length; i++)
            {
                for (int j = 0; j < map[i].Length; j++)
                {
                    if (map[i][j] != sg[i][j])
                    {
                        res += j + " " + i + " " + map[i][j] + " ";
                    }
                }
            }
        }

        if (res.Length > 0) res.Remove(res.Length - 1);
        Console.WriteLine(res);

    }
}

