using System;
using System.IO;
using System.Windows;

namespace MazeGame
{
    public static class MazeSerializer
    {
        public static void SaveMaze(MazeCellType[,] maze, string filename)
        {
            int width = maze.GetLength(0);
            int height = maze.GetLength(1);

            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine($"{width},{height}");

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        char symbol = maze[x, y] switch
                        {
                            MazeCellType.Wall => '#',
                            MazeCellType.Path => ' ',
                            MazeCellType.Start => 'S',
                            MazeCellType.Finish => 'F',
                            _ => '?'
                        };
                        writer.Write(symbol);
                    }
                    writer.WriteLine();
                }
            }
        }

        public static MazeCellType[,] LoadMaze(string filename)
        {
            if (!File.Exists(filename))
                return null;

            var lines = File.ReadAllLines(filename);
            if (lines.Length < 2)
                return null;

            var sizeParts = lines[0].Split(',');
            if (sizeParts.Length != 2 ||
                !int.TryParse(sizeParts[0], out int width) ||
                !int.TryParse(sizeParts[1], out int height))
                return null;

            var maze = new MazeCellType[width, height];

            for (int y = 0; y < Math.Min(height, lines.Length - 1); y++)
            {
                string line = lines[y + 1];
                for (int x = 0; x < Math.Min(width, line.Length); x++)
                {
                    maze[x, y] = line[x] switch
                    {
                        '#' => MazeCellType.Wall,
                        'S' => MazeCellType.Start,
                        'F' => MazeCellType.Finish,
                        _ => MazeCellType.Path
                    };
                }
            }

            return maze;
        }
    }
}