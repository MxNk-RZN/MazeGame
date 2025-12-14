using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MazeGame
{
    public static class PathFinder
    {
        public static List<Point> FindOptimalPath(MazeCellType[,] maze, Point start, Point end)
        {
            int width = maze.GetLength(0);
            int height = maze.GetLength(1);

            if (!IsValidCell((int)start.X, (int)start.Y, maze) || !IsValidCell((int)end.X, (int)end.Y, maze))
                return new List<Point>();

            var openSet = new List<PathNode>();
            var closedSet = new HashSet<Point>();
            var startNode = new PathNode(start, null, 0, CalculateHeuristic(start, end));

            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                var currentNode = openSet.OrderBy(n => n.FCost).First();

                if (currentNode.Position == end)
                {
                    return ReconstructPath(currentNode);
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode.Position);

                foreach (var neighbor in GetNeighbors(currentNode.Position, maze))
                {
                    if (closedSet.Contains(neighbor))
                        continue;

                    float gCost = currentNode.GCost + 1;
                    var neighborNode = openSet.FirstOrDefault(n => n.Position == neighbor);

                    if (neighborNode == null)
                    {
                        neighborNode = new PathNode(neighbor, currentNode, gCost, CalculateHeuristic(neighbor, end));
                        openSet.Add(neighborNode);
                    }
                    else if (gCost < neighborNode.GCost)
                    {
                        neighborNode.Parent = currentNode;
                        neighborNode.GCost = gCost;
                        // FCost вычисляется автоматически через свойство, поэтому убираем эту строку
                    }
                }
            }

            return new List<Point>();
        }

        private static List<Point> ReconstructPath(PathNode endNode)
        {
            var path = new List<Point>();
            var currentNode = endNode;

            while (currentNode != null)
            {
                path.Add(currentNode.Position);
                currentNode = currentNode.Parent;
            }

            path.Reverse();
            return path;
        }

        private static List<Point> GetNeighbors(Point position, MazeCellType[,] maze)
        {
            var neighbors = new List<Point>();
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int newX = (int)position.X + dx[i];
                int newY = (int)position.Y + dy[i];

                if (IsValidCell(newX, newY, maze))
                {
                    neighbors.Add(new Point(newX, newY));
                }
            }

            return neighbors;
        }

        private static bool IsValidCell(int x, int y, MazeCellType[,] maze)
        {
            return x >= 0 && x < maze.GetLength(0) &&
                   y >= 0 && y < maze.GetLength(1) &&
                   maze[x, y] != MazeCellType.Wall;
        }

        private static float CalculateHeuristic(Point a, Point b)
        {
            // Явное преобразование double в float
            return (float)(Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y));
        }

        private class PathNode
        {
            public Point Position { get; }
            public PathNode Parent { get; set; }
            public float GCost { get; set; }
            public float HCost { get; }
            public float FCost => GCost + HCost; // Только get, вычисляется автоматически

            public PathNode(Point position, PathNode parent, float gCost, float hCost)
            {
                Position = position;
                Parent = parent;
                GCost = gCost;
                HCost = hCost;
            }
        }
    }
}