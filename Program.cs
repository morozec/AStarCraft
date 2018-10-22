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
    public Tuple<int, int> Pos { get; set; }
    public char Direction { get; set; }
    public override string ToString()
    {
        return Pos.ToString();
    }
}

class PathMapContainer
{
    public IDictionary<Tuple<int, int>, List<char>> Path { get; set; }
    public char[][] Map { get; set; }

    public PathMapContainer(IDictionary<Tuple<int, int>, List<char>> path, char[][] map)
    {
        Path = path;
        Map = map;
    }

    public override string ToString()
    {
        return Path.Count.ToString();
    }
}

class Player
{
    private static Random _rnd = new Random();
    static IList<char> DIRECTIONS = new List<char>() { 'R', 'L', 'D', 'U' };
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
        BuildSplitedGrid(GetNormedCoord(y - 1, grid.Length), x, grid, robots, splitedGrid);
        BuildSplitedGrid(GetNormedCoord(y + 1, grid.Length), x, grid, robots, splitedGrid);
        BuildSplitedGrid(y, GetNormedCoord(x - 1, grid[y].Length), grid, robots, splitedGrid);
        BuildSplitedGrid(y, GetNormedCoord(x + 1, grid[y].Length), grid, robots, splitedGrid);
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
        return graph[i][GetNormedCoord(j + 1, graph[i].Length)] == '#';
    }

    static void InitTuples(char[][] grid)
    {
        _tuples = new Tuple<int, int>[grid.Length][];
        for (var i = 0; i < grid.Length; ++i)
        {
            _tuples[i] = new Tuple<int, int>[grid[i].Length];
            for (var j = 0; j < grid[i].Length; ++j)
                _tuples[i][j] = new Tuple<int, int>(i, j);
        }
    }

    static IList<PathMapContainer> GetOneDirectionContainer(Tuple<int, int> pos, char[][] grid, IDictionary<Tuple<int, int>, List<char>> currPath,
        char currDirection, char newDirection, IDictionary<Tuple<int, int>, List<char>> prevRobotsSteps)
    {
        if (currPath.ContainsKey(pos) && currPath[pos].Any(s => s == newDirection)) return null;
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

        var currPathCopy = currPath.ToDictionary(etry => etry.Key, entry => entry.Value.ToList());
        if (!currPathCopy.ContainsKey(pos)) currPathCopy.Add(pos, new List<char>());
        currPathCopy[pos].Add(newDirection);

        var isGridChanged = false;
        if (grid[pos.Item1][pos.Item2] == '.' && currDirection != newDirection)
        {
            grid[pos.Item1][pos.Item2] = newDirection;
            isGridChanged = true;
        }
        var pathes = GetAllPossiblePathes(_tuples[y][x], newDirection, grid, currPathCopy, prevRobotsSteps);
        if (isGridChanged) grid[pos.Item1][pos.Item2] = '.';
        return pathes;
    }

    static bool IsNextCellTheSame(Tuple<int, int> pos, char direction, char[][] grid)
    {
        int y = -1;
        int x = -1;

        switch (direction)
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

        var isSame = !(IsUpWall(grid, pos.Item1, pos.Item2) ^ IsUpWall(grid, y, x)) &&
                     !(IsDownWall(grid, pos.Item1, pos.Item2) ^ IsDownWall(grid, y, x)) &&
                     !(IsRightWall(grid, pos.Item1, pos.Item2) ^ IsRightWall(grid, y, x)) &&
                     !(IsLeftWall(grid, pos.Item1, pos.Item2) ^ IsLeftWall(grid, y, x));

        return isSame;
    }

    private static int _counter = 0;
    static IList<PathMapContainer> GetAllPossiblePathes(
        Tuple<int, int> pos, char direction, char[][] grid, IDictionary<Tuple<int, int>, List<char>> currPath,
        IDictionary<Tuple<int, int>, List<char>> prevRobotsSteps)
    {
        if (grid[pos.Item1][pos.Item2] == '#') return null;

        _counter++;

        var res = new List<PathMapContainer>();


        if (grid[pos.Item1][pos.Item2] != '.')//стрелка на карте
        {
            var odc = GetOneDirectionContainer(pos, grid, currPath, direction, grid[pos.Item1][pos.Item2], prevRobotsSteps);
            if (odc != null) res.AddRange(odc);
        }
        else if (currPath.ContainsKey(pos) || IsNextCellTheSame(pos, direction, grid))//уже были в этой точке. нельзя менять направление (или след. точка такая же)
        {
            var odc = GetOneDirectionContainer(pos, grid, currPath, direction, direction, prevRobotsSteps);
            if (odc != null) res.AddRange(odc);
        }
        else if (prevRobotsSteps.ContainsKey(pos))
        {
            if (prevRobotsSteps[pos].Count >= 2)
            {
                var odc = GetOneDirectionContainer(pos, grid, currPath, direction, direction, prevRobotsSteps);
                if (odc != null) res.AddRange(odc);
            }
            else
            {
                var odc = GetOneDirectionContainer(pos, grid, currPath, direction, prevRobotsSteps[pos][0], prevRobotsSteps);
                if (odc != null) res.AddRange(odc);
            }
        }
        else
        {
            if (_counter > 1300)
            {
                var d = DIRECTIONS[_rnd.Next(DIRECTIONS.Count)];
                var odc = GetOneDirectionContainer(pos, grid, currPath, direction, d, prevRobotsSteps);
                if (odc != null) res.AddRange(odc);
            }
            else
            {
                foreach (var d in DIRECTIONS)
                {
                    var odc = GetOneDirectionContainer(pos, grid, currPath, direction, d, prevRobotsSteps);
                    if (odc != null) res.AddRange(odc);
                }
            }
        }


        //grid[pos.Item1][pos.Item2] = '.';
        if (res.Count == 0) res.Add(new PathMapContainer(currPath, grid.Select(a => a.ToArray()).ToArray()));

        return res;
    }

    static IDictionary<char[][], int> BuildBestMap(
        IList<Robot> robots,
        int robotIndex,
        IDictionary<char[][], int> mapsDictionary,
        IDictionary<char[][], IDictionary<Tuple<int, int>, List<char>>> prevRobotsSteps)
    {
        var robot = robots[robotIndex];
        var maps = mapsDictionary.Keys.OrderByDescending(k => mapsDictionary[k]);
        var newMapsDictionary = new Dictionary<char[][], int>();
        var newStepsDictionary = new Dictionary<char[][], IDictionary<Tuple<int, int>, List<char>>>();
        foreach (var map in maps)
        {
            var apps = GetAllPossiblePathes(
                _tuples[robot.Y][robot.X], robot.Direction, map, new Dictionary<Tuple<int, int>, List<char>>(), prevRobotsSteps[map]);
            foreach (var app in apps)
            {
                var appPath = app.Path;
                var appMap = app.Map;

                var sumPathLength = appPath.Count + mapsDictionary[map];
                //for (var j = 0; j < robotIndex; ++j)
                //{
                //    var prevRobot = robots[j];
                //    IDictionary<Tuple<int, int>, IList<char>> prevRobotPath = new Dictionary<Tuple<int, int>, IList<char>>();
                //    BuildPath(prevRobot.Y, prevRobot.X, prevRobot.Direction, appMap, prevRobotPath);
                //    sumPathLength += GetPathCount(prevRobotPath);
                //}
                newMapsDictionary.Add(appMap, sumPathLength);
                var steps = new Dictionary<Tuple<int, int>, List<char>>(prevRobotsSteps[map]);
                foreach (var s in appPath.Keys)
                {
                    if (!steps.ContainsKey(s)) steps.Add(s, new List<char>());
                    foreach (var dir in appPath[s])
                    {
                        if (!steps[s].Any(ss => ss == dir)) steps[s].Add(dir);
                    }
                }
                newStepsDictionary.Add(appMap, steps);
            }
        }

        if (robotIndex == 0)
            return newMapsDictionary;

        return BuildBestMap(robots, robotIndex - 1, newMapsDictionary, newStepsDictionary);
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
            //var startRobot = splitedGris[sg][0];
            //var apps = GetAllPossiblePathes(_tuples[startRobot.Y][startRobot.X], startRobot.Direction, sg, new List<Step>());
            var mapsDictionary = new Dictionary<char[][], int>() { { sg, 0 } };
            var prevRobotsSteps =
                new Dictionary<char[][], IDictionary<Tuple<int, int>, List<char>>>()
                {
                    {sg, new Dictionary<Tuple<int, int>, List<char>>()}
                };
            var bmd = BuildBestMap(splitedGris[sg], splitedGris[sg].Count - 1, mapsDictionary, prevRobotsSteps);

            var bestMap = bmd.Keys.OrderByDescending(k => bmd[k]).FirstOrDefault();

            if (bestMap == null) continue;
            for (int i = 0; i < bestMap.Length; i++)
            {
                for (int j = 0; j < bestMap[i].Length; j++)
                {
                    if (bestMap[i][j] != sg[i][j])
                    {
                        res += j + " " + i + " " + bestMap[i][j] + " ";
                    }
                }
            }
        }

        if (res.Length > 0) res.Remove(res.Length - 1);
        Console.WriteLine(res);
    }
}

