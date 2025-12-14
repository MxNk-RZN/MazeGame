using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.ComponentModel;
using System.Diagnostics;


namespace MazeGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Stopwatch _renderStopwatch = new Stopwatch();
        private long _lastRenderTime;
        private bool _forceFullRender = true;
        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new MainViewModel();
            DataContext = viewModel;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.RequestFullRender += () => _forceFullRender = true;

            Loaded += MainWindow_Loaded;
            _renderStopwatch.Start();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RenderMaze(true);
            MainGrid.Focus();
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.Maze))
            {
                _forceFullRender = true;
                RenderMaze(true);
            }
            else if (e.PropertyName == nameof(MainViewModel.PlayerPosition))
            {
                var currentTime = _renderStopwatch.ElapsedMilliseconds;
                if (currentTime - _lastRenderTime > 16)
                {
                    RenderMaze(false);
                    _lastRenderTime = currentTime;
                }
            }
        }

        private void RenderMaze(bool forceFullRender = false)
        {
            if (DataContext is MainViewModel viewModel)
            {
                if (_forceFullRender || forceFullRender)
                {
                    MazeRenderer.RenderFullMaze(MazeCanvas, viewModel.Maze, viewModel.CellSizeValue,
                        viewModel.PlayerPosition, viewModel.PlayerPath, viewModel.OptimalPath, viewModel.ShowOptimalPath);
                    _forceFullRender = false;
                }
                else
                {
                    MazeRenderer.UpdatePlayerOnly(MazeCanvas, viewModel.PlayerPosition,
                        viewModel.CellSizeValue, viewModel.PlayerPath);
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (DataContext is MainViewModel viewModel)
            {
                var command = viewModel.MoveCommand as RelayCommand<Key>;
                command?.Execute(e.Key);
            }

            MainGrid.Focus();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MazeCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                var position = e.GetPosition(MazeCanvas);
                viewModel.HandleCanvasClick(position, e.ChangedButton);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}