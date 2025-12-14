using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Effects;

namespace MazeGame
{
    public static class MazeRenderer
    {
        private const double WallThickness = 2.5;
        private const int LightRadius = 2;

        private static readonly Brush _wallBrush = new LinearGradientBrush(
            Color.FromRgb(80, 80, 100),
            Color.FromRgb(50, 50, 70), 45);

        private static readonly Brush _pathBrush = new SolidColorBrush(Color.FromRgb(8, 8, 8));
        private static readonly Brush _startBrush = new SolidColorBrush(Color.FromRgb(15, 15, 25));
        private static readonly Brush _finishBrush = new RadialGradientBrush(
            Color.FromRgb(0, 200, 80), Color.FromRgb(0, 120, 50));

        private static readonly Brush _playerPathBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0));
        private static readonly Brush _optimalPathBrush = new SolidColorBrush(Color.FromArgb(150, 0, 255, 255));
        private static readonly Brush _editHighlightBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));

        private static readonly DropShadowEffect _finishEffect = new DropShadowEffect
        {
            Color = Color.FromRgb(0, 255, 100),
            BlurRadius = 20,
            Opacity = 0.6,
            ShadowDepth = 0
        };

        private static readonly DropShadowEffect _playerEffect = new DropShadowEffect
        {
            Color = Color.FromRgb(0, 150, 255),
            BlurRadius = 15,
            Opacity = 0.8,
            ShadowDepth = 0
        };

        private static Rectangle _lightMask;
        private static Rectangle _fogOfWar;
        private static Point _lastEditPosition = new Point(-1, -1);

        public static void RenderFullMaze(Canvas canvas, MazeCellType[,] maze, int cellSize,
            Point playerPosition, List<Point> playerPath, List<Point> optimalPath, bool showOptimalPath)
        {
            if (canvas == null || maze == null) return;

            canvas.Children.Clear();

            int width = maze.GetLength(0);
            int height = maze.GetLength(1);

            var background = new Rectangle
            {
                Width = width * cellSize,
                Height = height * cellSize,
                Fill = new SolidColorBrush(Color.FromRgb(5, 5, 5)),
                RadiusX = 6,
                RadiusY = 6
            };
            canvas.Children.Add(background);

            if (showOptimalPath && optimalPath != null)
            {
                DrawPath(canvas, optimalPath, cellSize, _optimalPathBrush, 0.6);
            }

            if (playerPath != null && playerPath.Count > 1)
            {
                DrawPath(canvas, playerPath, cellSize, _playerPathBrush, 0.4);
            }

            // Рисуем клетки с подсветкой типа
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (maze[x, y] != MazeCellType.Wall)
                    {
                        DrawPathCell(canvas, x, y, cellSize, maze[x, y]);
                    }
                    else
                    {
                        // Для стен тоже добавляем подсветку
                        DrawWallCell(canvas, x, y, cellSize, maze[x, y]);
                    }
                }
            }

            // Подсветка последней отредактированной клетки
            if (_lastEditPosition.X >= 0 && _lastEditPosition.Y >= 0)
            {
                DrawEditHighlight(canvas, _lastEditPosition, cellSize);
            }

            DrawWallsAsLines(canvas, maze, cellSize);

            _lightMask = CreateLightMask(width, height, cellSize);
            canvas.Children.Add(_lightMask);

            _fogOfWar = CreateFogOfWar(width, height, cellSize);
            canvas.Children.Add(_fogOfWar);

            UpdateLightPosition(playerPosition, cellSize);
            DrawPlayer(canvas, playerPosition, cellSize);
        }

        // Новый метод для подсветки редактирования
        public static void SetLastEditPosition(Point position)
        {
            _lastEditPosition = position;
        }

        private static void DrawEditHighlight(Canvas canvas, Point position, int cellSize)
        {
            var highlight = new Rectangle
            {
                Width = cellSize - 6,
                Height = cellSize - 6,
                Fill = _editHighlightBrush,
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1,
                RadiusX = 2,
                RadiusY = 2
            };

            Canvas.SetLeft(highlight, position.X * cellSize + 3);
            Canvas.SetTop(highlight, position.Y * cellSize + 3);
            canvas.Children.Add(highlight);
        }

        private static void DrawWallCell(Canvas canvas, int x, int y, int cellSize, MazeCellType cellType)
        {
            // Для стен можно добавить специальную отрисовку
            var rect = new Rectangle
            {
                Width = cellSize - 2,
                Height = cellSize - 2,
                Fill = _wallBrush,
                RadiusX = 2,
                RadiusY = 2
            };

            Canvas.SetLeft(rect, x * cellSize + 1);
            Canvas.SetTop(rect, y * cellSize + 1);
            canvas.Children.Add(rect);
        }

        private static void DrawPath(Canvas canvas, List<Point> path, int cellSize, Brush brush, double opacity)
        {
            foreach (var point in path)
            {
                var rect = new Rectangle
                {
                    Width = cellSize - 4,
                    Height = cellSize - 4,
                    Fill = brush,
                    Opacity = opacity,
                    RadiusX = 2,
                    RadiusY = 2
                };

                Canvas.SetLeft(rect, point.X * cellSize + 2);
                Canvas.SetTop(rect, point.Y * cellSize + 2);
                canvas.Children.Add(rect);
            }
        }

        public static void UpdatePlayerOnly(Canvas canvas, Point playerPosition, int cellSize, List<Point> playerPath)
        {
            if (canvas == null) return;

            RemovePlayer(canvas);
            UpdateLightPosition(playerPosition, cellSize);

            if (playerPath != null && playerPath.Count > 0)
            {
                var lastPoint = playerPath[playerPath.Count - 1];
                DrawPath(canvas, new List<Point> { lastPoint }, cellSize, _playerPathBrush, 0.4);
            }

            DrawPlayer(canvas, playerPosition, cellSize);
        }

        // Остальные методы без изменений...
        private static Rectangle CreateLightMask(int width, int height, int cellSize)
        {
            var lightBrush = new RadialGradientBrush
            {
                GradientOrigin = new Point(0.5, 0.5),
                Center = new Point(0.5, 0.5),
                RadiusX = LightRadius,
                RadiusY = LightRadius,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(200, 255, 255, 255), 0.0),
                    new GradientStop(Color.FromArgb(100, 255, 255, 255), 0.3),
                    new GradientStop(Color.FromArgb(50, 255, 255, 255), 0.6),
                    new GradientStop(Colors.Transparent, 1.0)
                }
            };

            var lightMask = new Rectangle
            {
                Width = width * cellSize,
                Height = height * cellSize,
                Fill = Brushes.Black,
                OpacityMask = lightBrush,
                IsHitTestVisible = false
            };

            return lightMask;
        }

        private static Rectangle CreateFogOfWar(int width, int height, int cellSize)
        {
            var fogBrush = new RadialGradientBrush
            {
                GradientOrigin = new Point(0.5, 0.5),
                Center = new Point(0.5, 0.5),
                RadiusX = LightRadius * 1.2,
                RadiusY = LightRadius * 1.2,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Colors.Transparent, 0.0),
                    new GradientStop(Colors.Transparent, 0.7),
                    new GradientStop(Color.FromArgb(180, 0, 0, 0), 1.0)
                }
            };

            var fog = new Rectangle
            {
                Width = width * cellSize,
                Height = height * cellSize,
                Fill = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                OpacityMask = fogBrush,
                IsHitTestVisible = false
            };

            return fog;
        }

        private static void UpdateLightPosition(Point playerPosition, int cellSize)
        {
            if (_lightMask != null && _fogOfWar != null)
            {
                var lightBrush = _lightMask.OpacityMask as RadialGradientBrush;
                var fogBrush = _fogOfWar.OpacityMask as RadialGradientBrush;

                if (lightBrush != null && fogBrush != null)
                {
                    double relativeX = playerPosition.X / (_lightMask.Width);
                    double relativeY = playerPosition.Y / (_lightMask.Height);

                    lightBrush.GradientOrigin = new Point(relativeX, relativeY);
                    lightBrush.Center = new Point(relativeX, relativeY);

                    fogBrush.GradientOrigin = new Point(relativeX, relativeY);
                    fogBrush.Center = new Point(relativeX, relativeY);
                }
            }
        }

        private static void RemovePlayer(Canvas canvas)
        {
            for (int i = canvas.Children.Count - 1; i >= 0; i--)
            {
                var child = canvas.Children[i];
                if (child is Ellipse ellipse && ellipse.Effect == _playerEffect)
                {
                    canvas.Children.RemoveAt(i);
                    break;
                }
            }
        }

        private static void DrawPathCell(Canvas canvas, int x, int y, int cellSize, MazeCellType cellType)
        {
            double size = cellSize - 2;

            var brush = cellType switch
            {
                MazeCellType.Start => _startBrush,
                MazeCellType.Finish => _finishBrush,
                _ => _pathBrush
            };

            var rect = new Rectangle
            {
                Width = size,
                Height = size,
                Fill = brush,
                RadiusX = 3,
                RadiusY = 3
            };

            if (cellType == MazeCellType.Finish)
            {
                rect.Effect = _finishEffect;
            }

            Canvas.SetLeft(rect, x * cellSize + 1);
            Canvas.SetTop(rect, y * cellSize + 1);
            canvas.Children.Add(rect);
        }

        private static void DrawWallsAsLines(Canvas canvas, MazeCellType[,] maze, int cellSize)
        {
            int width = maze.GetLength(0);
            int height = maze.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (maze[x, y] == MazeCellType.Wall)
                    {
                        DrawWallBorders(canvas, x, y, maze, cellSize);
                    }
                }
            }
        }

        private static void DrawWallBorders(Canvas canvas, int x, int y, MazeCellType[,] maze, int cellSize)
        {
            int width = maze.GetLength(0);
            int height = maze.GetLength(1);

            if (y == 0 || maze[x, y - 1] != MazeCellType.Wall)
            {
                DrawLine(canvas, x * cellSize, y * cellSize, (x + 1) * cellSize, y * cellSize);
            }
            if (x == width - 1 || maze[x + 1, y] != MazeCellType.Wall)
            {
                DrawLine(canvas, (x + 1) * cellSize, y * cellSize, (x + 1) * cellSize, (y + 1) * cellSize);
            }
            if (y == height - 1 || maze[x, y + 1] != MazeCellType.Wall)
            {
                DrawLine(canvas, x * cellSize, (y + 1) * cellSize, (x + 1) * cellSize, (y + 1) * cellSize);
            }
            if (x == 0 || maze[x - 1, y] != MazeCellType.Wall)
            {
                DrawLine(canvas, x * cellSize, y * cellSize, x * cellSize, (y + 1) * cellSize);
            }
        }

        private static void DrawLine(Canvas canvas, double x1, double y1, double x2, double y2)
        {
            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = _wallBrush,
                StrokeThickness = WallThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            canvas.Children.Add(line);
        }

        private static void DrawPlayer(Canvas canvas, Point position, int cellSize)
        {
            double centerX = position.X;
            double centerY = position.Y;
            double size = cellSize * 0.35;

            var player = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new RadialGradientBrush(
                    Color.FromRgb(0, 200, 255),
                    Color.FromRgb(0, 100, 180)),
                Stroke = new SolidColorBrush(Color.FromRgb(0, 150, 220)),
                StrokeThickness = 1.2,
                Effect = _playerEffect
            };

            Canvas.SetLeft(player, centerX - size / 2);
            Canvas.SetTop(player, centerY - size / 2);
            canvas.Children.Add(player);
        }
    }
}