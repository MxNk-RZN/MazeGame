using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace MazeGame
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private const int MazeWidth = 19;
        private const int MazeHeight = 19;
        private const int CellSize = 35;

        private MazeGenerator _mazeGenerator;
        private Point _playerPosition;
        private string _gameStatus;
        private bool _isGameCompleted;
        private bool _isMoving;
        private MazeCellType[,] _currentMaze;
        private int _moveCount;
        private bool _isEditMode;
        private bool _showOptimalPath;
        private List<Point> _playerPath;
        private List<Point> _optimalPath;

        public int LightRadius { get; } = 2;

        public MazeCellType[,] Maze
        {
            get => _currentMaze;
            private set
            {
                _currentMaze = value;
                OnPropertyChanged();
            }
        }

        public int CellSizeValue => CellSize;
        public int MazeWidthPixels => MazeWidth * CellSize;
        public int MazeHeightPixels => MazeHeight * CellSize;

        public Point PlayerPosition
        {
            get => _playerPosition;
            set
            {
                _playerPosition = value;
                OnPropertyChanged();
                UpdatePlayerPath();
                CheckGameCompletion();
            }
        }

        public string GameStatus
        {
            get => _gameStatus;
            set
            {
                _gameStatus = value;
                OnPropertyChanged();
            }
        }

        public int MoveCount
        {
            get => _moveCount;
            set
            {
                _moveCount = value;
                OnPropertyChanged();
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged();
                UpdateGameStatus();
                // При переключении режима перерисовываем лабиринт
                RequestFullRender?.Invoke();
            }
        }

        public bool ShowOptimalPath
        {
            get => _showOptimalPath;
            set
            {
                _showOptimalPath = value;
                OnPropertyChanged();
                RequestFullRender?.Invoke();
            }
        }

        public List<Point> PlayerPath => _playerPath;
        public List<Point> OptimalPath => _optimalPath;

        public ICommand NewGameCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand MoveCommand { get; }
        public ICommand ToggleEditModeCommand { get; }
        public ICommand FindOptimalPathCommand { get; }
        public ICommand ToggleOptimalPathCommand { get; }
        public ICommand SaveMazeCommand { get; }
        public ICommand LoadMazeCommand { get; }

        public MainViewModel()
        {
            _mazeGenerator = new MazeGenerator(MazeWidth, MazeHeight);
            _playerPath = new List<Point>();
            _optimalPath = new List<Point>();

            NewGameCommand = new RelayCommand(StartNewGame);
            ExitCommand = new RelayCommand(ExitGame);
            MoveCommand = new RelayCommand<Key>(MovePlayer);
            ToggleEditModeCommand = new RelayCommand(ToggleEditMode);
            FindOptimalPathCommand = new RelayCommand(FindOptimalPath);
            ToggleOptimalPathCommand = new RelayCommand(ToggleOptimalPath);
            SaveMazeCommand = new RelayCommand(SaveMaze);
            LoadMazeCommand = new RelayCommand(LoadMaze);

            StartNewGame();
        }

        private void StartNewGame()
        {
            _mazeGenerator.GenerateMaze();
            Maze = _mazeGenerator.Maze;
            PlayerPosition = new Point(1 * CellSize + CellSize / 2, 1 * CellSize + CellSize / 2);
            MoveCount = 0;
            _playerPath.Clear();
            _optimalPath.Clear();
            IsEditMode = false;
            ShowOptimalPath = false;
            UpdateGameStatus();
            _isGameCompleted = false;
            _isMoving = false;

            OnPropertyChanged(nameof(Maze));
            RequestFullRender?.Invoke();
        }

        private void UpdateGameStatus()
        {
            if (IsEditMode)
            {
                GameStatus = "📝 Режим редактирования: ЛКМ - стена, ПКМ - путь, СКМ - старт/финиш";
            }
            else
            {
                GameStatus = $"🎮 Ходы: {MoveCount} | Лабиринт-головоломка! Много путей, но только один верный...";
            }
        }

        private void ExitGame()
        {
            Application.Current.Shutdown();
        }

        private void MovePlayer(Key key)
        {
            if (_isGameCompleted || _isMoving || IsEditMode) return;

            int newX = (int)PlayerPosition.X;
            int newY = (int)PlayerPosition.Y;

            switch (key)
            {
                case Key.Up:
                case Key.W:
                    newY -= CellSize;
                    break;
                case Key.Down:
                case Key.S:
                    newY += CellSize;
                    break;
                case Key.Left:
                case Key.A:
                    newX -= CellSize;
                    break;
                case Key.Right:
                case Key.D:
                    newX += CellSize;
                    break;
                default:
                    return;
            }

            int cellX = newX / CellSize;
            int cellY = newY / CellSize;

            if (cellX >= 0 && cellX < MazeWidth && cellY >= 0 && cellY < MazeHeight &&
                Maze[cellX, cellY] != MazeCellType.Wall)
            {
                PlayerPosition = new Point(newX, newY);
                MoveCount++;
            }
        }

        private void UpdatePlayerPath()
        {
            var cellPos = new Point((int)PlayerPosition.X / CellSize, (int)PlayerPosition.Y / CellSize);
            if (_playerPath.Count == 0 || _playerPath.Last() != cellPos)
            {
                _playerPath.Add(cellPos);
            }
        }

        private void CheckGameCompletion()
        {
            int playerCellX = (int)PlayerPosition.X / CellSize;
            int playerCellY = (int)PlayerPosition.Y / CellSize;

            if (playerCellX == MazeWidth - 2 && playerCellY == MazeHeight - 2)
            {
                _isGameCompleted = true;
                GameStatus = $"🎉 Победа! Вы прошли лабиринт за {MoveCount} ходов!";

                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(100);
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    ShowVictoryMessage();
                };
                timer.Start();
            }
        }

        private void ShowVictoryMessage()
        {
            var result = MessageBox.Show(
                $"🏆 Вы прошли лабиринт за {MoveCount} ходов!\n\n" +
                "Хотите испытать себя еще раз?",
                "Победа!",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                StartNewGame();
            }
        }

        private void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
        }

        private void FindOptimalPath()
        {
            _optimalPath = PathFinder.FindOptimalPath(Maze, new Point(1, 1), new Point(MazeWidth - 2, MazeHeight - 2));
            if (_optimalPath.Any())
            {
                GameStatus = $"🤖 Найден оптимальный путь длиной {_optimalPath.Count} клеток";
                ShowOptimalPath = true;
            }
            else
            {
                GameStatus = "❌ Оптимальный путь не найден";
            }
        }

        private void ToggleOptimalPath()
        {
            ShowOptimalPath = !ShowOptimalPath;
        }

        private void SaveMaze()
        {
            try
            {
                MazeSerializer.SaveMaze(Maze, "custom_maze.txt");
                GameStatus = "💾 Лабиринт сохранен в custom_maze.txt";
            }
            catch (Exception ex)
            {
                GameStatus = "❌ Ошибка сохранения лабиринта";
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMaze()
        {
            try
            {
                var loadedMaze = MazeSerializer.LoadMaze("custom_maze.txt");
                if (loadedMaze != null)
                {
                    Maze = loadedMaze;
                    PlayerPosition = new Point(1 * CellSize + CellSize / 2, 1 * CellSize + CellSize / 2);
                    MoveCount = 0;
                    _playerPath.Clear();
                    _optimalPath.Clear();
                    GameStatus = "💾 Лабиринт загружен из custom_maze.txt";
                    RequestFullRender?.Invoke();
                }
            }
            catch (Exception ex)
            {
                GameStatus = "❌ Ошибка загрузки лабиринта";
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void HandleCanvasClick(Point position, MouseButton button)
        {
            if (!IsEditMode) return;

            int cellX = (int)(position.X / CellSize);
            int cellY = (int)(position.Y / CellSize);

            if (cellX >= 0 && cellX < MazeWidth && cellY >= 0 && cellY < MazeHeight)
            {
                switch (button)
                {
                    case MouseButton.Left:
                        // Левая кнопка - стена
                        Maze[cellX, cellY] = MazeCellType.Wall;
                        break;
                    case MouseButton.Right:
                        // Правая кнопка - путь
                        Maze[cellX, cellY] = MazeCellType.Path;
                        break;
                    case MouseButton.Middle:
                        // Средняя кнопка - переключение старт/финиш
                        if (cellX == 1 && cellY == 1)
                            Maze[cellX, cellY] = MazeCellType.Start;
                        else if (cellX == MazeWidth - 2 && cellY == MazeHeight - 2)
                            Maze[cellX, cellY] = MazeCellType.Finish;
                        else if (Maze[cellX, cellY] == MazeCellType.Start)
                            Maze[cellX, cellY] = MazeCellType.Finish;
                        else if (Maze[cellX, cellY] == MazeCellType.Finish)
                            Maze[cellX, cellY] = MazeCellType.Path;
                        else
                            Maze[cellX, cellY] = MazeCellType.Start;
                        break;
                }

                // Принудительно обновляем отображение после изменения
                OnPropertyChanged(nameof(Maze));
                RequestFullRender?.Invoke();
            }
        }

        public void RenderMaze(Canvas canvas, bool forceFullRender = false)
        {
            if (forceFullRender)
            {
                MazeRenderer.RenderFullMaze(canvas, Maze, CellSizeValue, PlayerPosition, PlayerPath, OptimalPath, ShowOptimalPath);
            }
            else
            {
                MazeRenderer.UpdatePlayerOnly(canvas, PlayerPosition, CellSizeValue, PlayerPath);
            }
        }

        public event Action RequestFullRender;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}