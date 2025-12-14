using System;
using System.Collections.Generic;
using System.Linq;

namespace MazeGame
{
    public class MazeGenerator
    {
        private readonly Random _random;
        private readonly int _width;
        private readonly int _height;

        public MazeCellType[,] Maze { get; private set; }

        public MazeGenerator(int width, int height)
        {
            _width = width;
            _height = height;
            _random = new Random();
            Maze = new MazeCellType[_width, _height];
        }

        public void GenerateMaze()
        {
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    Maze[x, y] = MazeCellType.Wall;

            GenerateWithDFS();
            AddDeadEnds(15);

            Maze[1, 1] = MazeCellType.Start;
            Maze[_width - 2, _height - 2] = MazeCellType.Finish;

            EnsureFinishReachable();
        }

        private void GenerateWithDFS()
        {
            var stack = new Stack<(int x, int y)>();
            var visited = new bool[_width, _height];

            int startX = 1;
            int startY = 1;

            stack.Push((startX, startY));
            visited[startX, startY] = true;
            Maze[startX, startY] = MazeCellType.Path;

            while (stack.Count > 0)
            {
                var current = stack.Peek();
                var neighbors = GetUnvisitedNeighbors(current.x, current.y, visited);

                if (neighbors.Count > 0)
                {
                    var next = neighbors[_random.Next(neighbors.Count)];

                    int wallX = current.x + (next.x - current.x) / 2;
                    int wallY = current.y + (next.y - current.y) / 2;

                    Maze[wallX, wallY] = MazeCellType.Path;
                    Maze[next.x, next.y] = MazeCellType.Path;

                    visited[next.x, next.y] = true;
                    stack.Push(next);
                }
                else
                {
                    stack.Pop();
                }
            }
        }

        private void AddDeadEnds(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var pathCells = GetPathCells();
                if (pathCells.Count == 0) continue;

                var cell = pathCells[_random.Next(pathCells.Count)];
                CreateDeadEndBranch(cell.x, cell.y, _random.Next(2, 5));
            }
        }

        private void CreateDeadEndBranch(int startX, int startY, int length)
        {
            int x = startX;
            int y = startY;
            var directions = new List<(int dx, int dy)> { (0, 2), (0, -2), (2, 0), (-2, 0) };

            for (int i = 0; i < length; i++)
            {
                directions = directions.OrderBy(d => _random.Next()).ToList();

                bool moved = false;
                foreach (var dir in directions)
                {
                    int newX = x + dir.dx;
                    int newY = y + dir.dy;
                    int wallX = x + dir.dx / 2;
                    int wallY = y + dir.dy / 2;

                    if (newX > 0 && newX < _width - 1 && newY > 0 && newY < _height - 1 &&
                        Maze[newX, newY] == MazeCellType.Wall)
                    {
                        Maze[wallX, wallY] = MazeCellType.Path;
                        Maze[newX, newY] = MazeCellType.Path;

                        x = newX;
                        y = newY;
                        moved = true;
                        break;
                    }
                }

                if (!moved) break;
            }
        }

        private List<(int x, int y)> GetPathCells()
        {
            var paths = new List<(int x, int y)>();
            for (int x = 1; x < _width - 1; x++)
            {
                for (int y = 1; y < _height - 1; y++)
                {
                    if (Maze[x, y] == MazeCellType.Path)
                    {
                        paths.Add((x, y));
                    }
                }
            }
            return paths;
        }

        private List<(int x, int y)> GetUnvisitedNeighbors(int x, int y, bool[,] visited)
        {
            var neighbors = new List<(int x, int y)>();

            int[] dx = { 0, 0, 2, -2 };
            int[] dy = { 2, -2, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                if (nx > 0 && nx < _width - 1 && ny > 0 && ny < _height - 1 &&
                    !visited[nx, ny] && Maze[nx, ny] == MazeCellType.Wall)
                {
                    neighbors.Add((nx, ny));
                }
            }

            return neighbors;
        }

        private void EnsureFinishReachable()
        {
            if (!IsFinishReachable())
            {
                CreateGuaranteedPath();
            }
        }

        private bool IsFinishReachable()
        {
            var visited = new bool[_width, _height];
            var queue = new Queue<(int x, int y)>();

            queue.Enqueue((1, 1));
            visited[1, 1] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current.x == _width - 2 && current.y == _height - 2)
                    return true;

                int[] dx = { 0, 0, 1, -1 };
                int[] dy = { 1, -1, 0, 0 };

                for (int i = 0; i < 4; i++)
                {
                    int nx = current.x + dx[i];
                    int ny = current.y + dy[i];

                    if (nx >= 0 && nx < _width && ny >= 0 && ny < _height &&
                        !visited[nx, ny] && Maze[nx, ny] != MazeCellType.Wall)
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            return false;
        }

        private void CreateGuaranteedPath()
        {
            int x = 1, y = 1;

            while (x < _width - 2 || y < _height - 2)
            {
                if (x < _width - 2)
                {
                    x++;
                    Maze[x, y] = MazeCellType.Path;
                }

                if (y < _height - 2)
                {
                    y++;
                    Maze[x, y] = MazeCellType.Path;
                }
            }
        }
    }

    public enum MazeCellType
    {
        Wall,
        Path,
        Start,
        Finish
    }
}