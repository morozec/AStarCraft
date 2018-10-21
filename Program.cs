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

class Player
{
    static IList<char> DIRECTIONS = new List<char>(){'.','R','L','D','U'};

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
        
        var pos = new Tuple<int, int>(y, x);
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

        var key = new Tuple<int, int>(y, x);
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

    static IDictionary<Tuple<int, int>, IList<char>> GetCrossPoints(char[][] graph, IList<Robot> robots)
    {
        var res = new Dictionary<Tuple<int, int>, IList<char>>();
        for (var i = 0; i < graph.Length; ++i)
        {
            for (var j = 0; j < graph[i].Length; ++j)
            {
                if (graph[i][j] != '.') continue;
                var wallsCount = 0;
                var isLeftWall = IsLeftWall(graph, i, j);
                var isRigthWall = IsRightWall(graph, i, j);
                var isUpWall = IsUpWall(graph, i, j);
                var isDownWall = IsDownWall(graph, i, j);
                
                if (isLeftWall) wallsCount++;
                if (isRigthWall) wallsCount++;
                if (isUpWall) wallsCount++;
                if (isDownWall) wallsCount++;

                var possDirections = new List<char>();
                
                if (wallsCount == 3)
                {
                    if (robots.Any(r => r.Y == i && r.X == j)) possDirections.Add('.');
                    if (!isLeftWall) possDirections.Add('L');
                    if (!isRigthWall) possDirections.Add('R');
                    if (!isDownWall) possDirections.Add('D');
                    if (!isUpWall) possDirections.Add('U');
                }
                else if (wallsCount == 2)
                {
                    if (!isLeftWall)
                    {
                        if (!isRigthWall) continue;
                        if (robots.Any(r => r.Y == i && r.X == j)) possDirections.Add('.');
                        possDirections.Add('L');
                        if (!isUpWall)
                            possDirections.Add('U');
                        else if (!isDownWall)
                            possDirections.Add('D');
                    }
                    else if (!isRigthWall)
                    {
                        if (!isLeftWall) continue;
                        if (robots.Any(r => r.Y == i && r.X == j)) possDirections.Add('.');
                        possDirections.Add('R');
                        if (!isUpWall)
                            possDirections.Add('U');
                        else if (!isDownWall)
                            possDirections.Add('D');
                    }
                }
                else if (wallsCount == 1)
                {
                    if (robots.Any(r => r.Y == i && r.X == j)) possDirections.Add('.');
                    if (isLeftWall)
                    {
                        possDirections.Add('R');
                        possDirections.Add('D');
                        possDirections.Add('U');
                    }
                    else if (isRigthWall)
                    {
                        possDirections.Add('L');
                        possDirections.Add('D');
                        possDirections.Add('U');
                    }
                    else if (isUpWall)
                    {
                        possDirections.Add('R');
                        possDirections.Add('L');
                        possDirections.Add('D');
                    }
                    else if (isDownWall)
                    {
                        possDirections.Add('R');
                        possDirections.Add('L');
                        possDirections.Add('U');
                    }
                }
                if (possDirections.Any())
                    res.Add(new Tuple<int, int>(i, j), possDirections);
            }
        }

        return res;
    }
   
    static IDictionary<Tuple<int, int>, char> GetBestArrowsPositions(char[][] grid, IList<Robot> robots)
    {
        var res = new Dictionary<Tuple<int, int>, char>();
        var crossPoints = GetCrossPoints(grid, robots);
        if (!crossPoints.Any()) return res;
        var cpList = crossPoints.Keys.ToList();
        var arrowVariants = GetArrowsVariants(0, cpList, crossPoints);

        var maxSummPathLength = 0;
        IList<char> bestArrowVariant = null;

        foreach (var av in arrowVariants)
        {
            var gridCopy = grid.Select(r => r.ToArray()).ToArray();
            for (int j = 0; j < av.Count; ++j)
            {
                var arrow = av[j];
                var cp = cpList[j];
                gridCopy[cp.Item1][cp.Item2] = arrow;
            }

            var summPathLength = 0;
            foreach (var robot in robots)
            {
                IDictionary<Tuple<int, int>, IList<char>> path = new Dictionary<Tuple<int, int>, IList<char>>();
                BuildPath(robot.Y, robot.X, robot.Direction, gridCopy, path);
                summPathLength += GetPathCount(path);
            }

            if (bestArrowVariant == null || summPathLength > maxSummPathLength)
            {
                bestArrowVariant = av;
                maxSummPathLength = summPathLength;
            }
        }
        
        
        if (bestArrowVariant != null)
        {
            for (var j = 0; j < bestArrowVariant.Count; ++j)
                res.Add(cpList[j], bestArrowVariant[j]);
        }

        return res;

    }

    static IList<IList<char>> GetArrowsVariants(int index,
        IList<Tuple<int, int>> allPositions, IDictionary<Tuple<int, int>, IList<char>> possibleDirections)
    {
        var position = allPositions[index];
        if (index == allPositions.Count - 1)
        {
            var oneElemList = new List<IList<char>>();
            foreach (var d in possibleDirections[position])
                oneElemList.Add(new List<char>() {d});
            return oneElemList;
        }

        var res = new List<IList<char>>();
        var nextVariants = GetArrowsVariants(index + 1, allPositions, possibleDirections);
        foreach (var list in nextVariants)
        {
            foreach (var d in possibleDirections[position])
            {
                var listCopy = list.ToList();
                listCopy.Insert(0, d);
                res.Add(listCopy);
            }
        }


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
            var bestArrowsPositions = GetBestArrowsPositions(sg, splitedGris[sg]);
            foreach (var pos in bestArrowsPositions.Keys)
            {
                if (bestArrowsPositions[pos] == '.') continue;
                res += pos.Item2 + " " + pos.Item1 + " " + bestArrowsPositions[pos] + " ";
            }
        }
        
        

        if (res.Length > 0) res.Remove(res.Length - 1);
        Console.WriteLine(res);

        //Console.WriteLine(res.Remove(res.Length - 1));
    }
}

