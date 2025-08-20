using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MCGame.Blocks;
using MCGame.Chunks;
using MCGame.Player;
using MCGame.Rendering;
using MCGame.Utils;
// using MCGame.ECS;
// using MCGame.ECS.Managers;
// using MCGame.ECS.Systems;
// using Friflo.Engine.ECS;
// using Friflo.Engine.ECS.Systems;
// using MCGame.ECS.Components;
// using Pos = MCGame.ECS.Components.Position;
// using Rot = MCGame.ECS.Components.Rotation;
// using PlayerComp = MCGame.ECS.Components.Player;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MCGame.Core
{
    /// <summary>
    /// MCGame主游戏类
    /// 基于MonoGame框架的类Minecraft游戏主程序
    /// 简化实现：基础的游戏循环和系统管理
    /// </summary>
    public class MCGame : Game
    {
        // MonoGame组件
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _debugFont;

        // 核心系统
        private BlockRegistry _blockRegistry;
        private ChunkManager _chunkManager;
        private PlayerController _playerController;
        private RenderManager _renderManager;

        // ECS系统 - 暂时禁用
        // private ECSWorld _ecsWorld;
        // private ECSBlockManager _ecsBlockManager;
        // private ECSChunkManager _ecsChunkManager;
        // private SystemRoot _systemRoot;
        // private ECSRenderManager _ecsRenderManager;
        // private PerformanceBenchmarkSystem _benchmarkSystem;

        // 游戏状态
        private WorldSettings _worldSettings;
        private bool _isInitialized;
        private bool _debugMode;
        private bool _ecsEnabled;

        // 性能监控
        private Stopwatch _frameStopwatch;
        private float _frameTime;
        private int _fps;
        private int _frameCount;
        private float _fpsTimer;

        // 调试信息
        private RenderStatistics _renderStats;
        private ChunkManagerStats _chunkStats;

        // 相机参数
        private float _renderDistance = 150f;

        public MCGame()
        {
            try
            {
                Logger.Info("Initializing MCGame...");
                
                _graphics = new GraphicsDeviceManager(this);
                Content.RootDirectory = "Content";
                IsMouseVisible = true;
                
                // 设置窗口标题
                Window.Title = "MCGame - MonoGame Minecraft Clone";
                
                // 初始化性能监控
                _frameStopwatch = new Stopwatch();
                _renderStats = new RenderStatistics();
                _debugMode = true;
                
                Logger.Info("MCGame constructor completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Failed to initialize MCGame: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 初始化游戏
        /// </summary>
        protected override void Initialize()
        {
            try
            {
                Logger.Info("Starting game initialization...");
                
                // 设置图形设备参数
                Logger.Debug("Configuring graphics...");
                ConfigureGraphics();

                // 初始化对象池管理器
                Logger.Debug("Initializing object pool manager...");
                ObjectPoolManager.Initialize();

                // 初始化世界设置
                Logger.Debug("Setting up world settings...");
                _worldSettings = WorldSettings.Default;
                _worldSettings.RenderDistance = 10;

                // 初始化核心系统
                Logger.Debug("Initializing core systems...");
                InitializeCoreSystems();

                // 初始化输入处理
                Logger.Debug("Initializing input handling...");
                InitializeInput();

                Logger.Info("Game initialization completed successfully");
                
                base.Initialize();
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Failed to initialize game: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 配置图形设备
        /// </summary>
        private void ConfigureGraphics()
        {
            try
            {
                Logger.Debug("Configuring graphics device...");
                
                // 设置窗口大小
                _graphics.PreferredBackBufferWidth = 1280;
                _graphics.PreferredBackBufferHeight = 720;
                _graphics.IsFullScreen = false;
                _graphics.SynchronizeWithVerticalRetrace = true;
                _graphics.GraphicsProfile = GraphicsProfile.HiDef;
                Logger.Debug($"Set resolution to 1280x720, HiDef profile");
                
                _graphics.ApplyChanges();
                Logger.Debug("Applied graphics settings");

                // 设置视口
                GraphicsDevice.Viewport = new Viewport(0, 0, 1280, 720);
                Logger.Debug("Set viewport");
                
                Logger.Info("Graphics device configured successfully");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to configure graphics: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 初始化核心系统
        /// </summary>
        private void InitializeCoreSystems()
        {
            try
            {
                Logger.Debug("Initializing core systems...");
                
                // 初始化方块注册表
                Logger.Debug("Creating block registry...");
                _blockRegistry = new BlockRegistry(GraphicsDevice);
                Logger.Info("Block registry initialized successfully");

                // 初始化区块管理器
                Logger.Debug("Creating chunk manager...");
                _chunkManager = new ChunkManager(GraphicsDevice, _blockRegistry, _worldSettings);
                Logger.Info("Chunk manager initialized successfully");

                // 初始化玩家控制器
                Logger.Debug("Creating player controller...");
                var initialPosition = new Vector3(0, 65, 0); // 在地面上方
                _playerController = new PlayerController(GraphicsDevice, initialPosition);
                Logger.Info($"Player controller initialized at position {initialPosition}");

                // 初始化渲染管理器
                Logger.Debug("Creating render manager...");
                _renderManager = new RenderManager(GraphicsDevice);

                // 配置渲染选项
                Logger.Debug("Configuring rendering options...");
                ConfigureRendering();

                // 初始化ECS系统 - 暂时禁用
                // InitializeECS();
                // InitializeECSRendering();

                _isInitialized = true;
                Logger.Info("Core systems initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Failed to initialize core systems: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 配置渲染选项
        /// </summary>
        private void ConfigureRendering()
        {
            // 设置渲染距离
            _renderManager.FrustumCulling.SetMaxRenderDistance(_renderDistance);

            // 设置雾效
            _renderManager.SetRenderOptions(
                fogEnabled: true,
                fogStart: 50f,
                fogEnd: _renderDistance,
                fogColor: new Color(135, 206, 235) // 天空蓝
            );

            // 设置光照
            _renderManager.SetLighting(
                ambient: new Vector3(0.3f, 0.3f, 0.3f),
                lightDirection: new Vector3(0.5f, -1f, 0.5f),
                lightColor: new Vector3(1f, 1f, 0.9f)
            );
        }

     
    
        /// <summary>
        /// 初始化输入处理
        /// </summary>
        private void InitializeInput()
        {
            // 注册键盘事件
            Window.TextInput += HandleTextInput;
        }

        /// <summary>
        /// 处理文本输入
        /// </summary>
        private void HandleTextInput(object? sender, TextInputEventArgs e)
        {
            // 处理控制台命令或其他文本输入
            // 简化实现：预留接口
        }

        /// <summary>
        /// 加载内容
        /// </summary>
        protected override void LoadContent()
        {
            try
            {
                Logger.Info("Loading game content...");
                
                // 创建SpriteBatch
                Logger.Debug("Creating SpriteBatch...");
                _spriteBatch = new SpriteBatch(GraphicsDevice);

                // 加载调试字体
                Logger.Debug("Loading debug font...");
                try
                {
                    _debugFont = Content.Load<SpriteFont>("DebugFont");
                    Logger.Info("Debug font loaded successfully");
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to load debug font: {ex.Message}");
                    Logger.Info("Will use default rendering for debug text");
                    _debugFont = null;
                }
                
                Logger.Info("Game content loading completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Failed to load game content: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 初始化内容
        /// </summary>
        private void InitializeContent()
        {
            try
            {
                Logger.Debug("Initializing additional content...");
                
                // 加载纹理和着色器
                // 简化实现：预留内容加载接口
                
                Logger.Debug("Additional content initialization completed");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to initialize additional content: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新游戏状态
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            try
            {
                // 开始帧计时
                _frameStopwatch.Restart();

                // 处理退出
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
                    Keyboard.GetState().IsKeyDown(Keys.Escape))
                {
                    Logger.Info("Exit requested by user");
                    Exit();
                }

                // 处理调试模式切换
                HandleDebugMode();

            if (_isInitialized)
            {
                // 更新玩家
                _playerController.Update(gameTime);

                // 更新区块管理器
                _chunkManager.Update(_playerController.Player.Position);

                // 更新相机
                UpdateCamera();
                
  
                // 更新渲染统计
                UpdateRenderStats();

              }

            // 处理特殊输入
            HandleSpecialInput();

            base.Update(gameTime);

            // 结束帧计时
            _frameStopwatch.Stop();
            _frameTime = _frameStopwatch.ElapsedMilliseconds;
            UpdateFPS(gameTime);
            
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in game update: {ex.Message}", ex);
                // 不要抛出异常，让游戏继续运行
            }
        }

        /// <summary>
        /// 更新相机
        /// </summary>
        private void UpdateCamera()
        {
            var player = _playerController.Player;
            _renderManager.UpdateCamera(
                player.ViewMatrix,
                player.ProjectionMatrix,
                player.Position
            );
        }

        /// <summary>
        /// 更新渲染统计
        /// </summary>
        private void UpdateRenderStats()
        {
            var stats = _renderManager.GetStats();
            _renderStats.DrawCalls = stats.DrawCalls;
            _renderStats.Triangles = stats.Triangles;
            _renderStats.VisibleChunks = stats.VisibleChunks;
            _renderStats.FrameTime = _frameTime;
            _renderStats.FPS = _fps;

            _chunkStats = _chunkManager.GetStats();
        }

  
        /// <summary>
        /// 更新FPS
        /// </summary>
        private void UpdateFPS(GameTime gameTime)
        {
            _frameCount++;
            _fpsTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_fpsTimer >= 1.0f)
            {
                _fps = _frameCount;
                _frameCount = 0;
                _fpsTimer = 0f;
            }
        }

        /// <summary>
        /// 处理调试模式
        /// </summary>
        private void HandleDebugMode()
        {
            var keyboard = Keyboard.GetState();
            
            // F3切换调试模式
            if (keyboard.IsKeyDown(Keys.F3) && !Keyboard.GetState().IsKeyDown(Keys.F3))
            {
                _debugMode = !_debugMode;
            }
        }

        /// <summary>
        /// 处理特殊输入
        /// </summary>
        private void HandleSpecialInput()
        {
            var keyboard = Keyboard.GetState();
            var player = _playerController.Player;

            // F11切换全屏
            if (keyboard.IsKeyDown(Keys.F11) && !Keyboard.GetState().IsKeyDown(Keys.F11))
            {
                _graphics.IsFullScreen = !_graphics.IsFullScreen;
                _graphics.ApplyChanges();
            }

            // 调整渲染距离
            if (keyboard.IsKeyDown(Keys.OemPlus))
            {
                _renderDistance = Math.Min(300f, _renderDistance + 5f);
                _renderManager.FrustumCulling.SetMaxRenderDistance(_renderDistance);
            }
            if (keyboard.IsKeyDown(Keys.OemMinus))
            {
                _renderDistance = Math.Max(50f, _renderDistance - 5f);
                _renderManager.FrustumCulling.SetMaxRenderDistance(_renderDistance);
            }

            // 切换飞行模式
            if (keyboard.IsKeyDown(Keys.F) && !Keyboard.GetState().IsKeyDown(Keys.F))
            {
                _playerController.EnableFlying = !_playerController.EnableFlying;
            }

            // 重新生成世界
            if (keyboard.IsKeyDown(Keys.R) && !Keyboard.GetState().IsKeyDown(Keys.R))
            {
                RegenerateWorld();
            }

                }

        /// <summary>
        /// 重新生成世界
        /// </summary>
        private void RegenerateWorld()
        {
            // 保存玩家位置
            var playerPos = _playerController.Player.Position;

            // 重新创建区块管理器
            _chunkManager.Dispose();
            _chunkManager = new ChunkManager(GraphicsDevice, _blockRegistry, _worldSettings);

            // 恢复玩家位置
            _playerController.Player.SetPosition(playerPos);
        }

        /// <summary>
        /// 绘制游戏
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            try
            {
                // 清空屏幕
                GraphicsDevice.Clear(new Color(135, 206, 235)); // 天空蓝

                if (_isInitialized)
                {
                    // 渲染3D场景
                    Render3DScene();

                    // 渲染UI
                    RenderUI();
                }

                base.Draw(gameTime);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in game draw: {ex.Message}", ex);
                // 尝试继续渲染，即使出错
                try
                {
                    GraphicsDevice.Clear(Color.Red);
                    base.Draw(gameTime);
                }
                catch
                {
                    // 如果连基本渲染都失败，就忽略
                }
            }
        }

        /// <summary>
        /// 渲染3D场景
        /// </summary>
        private void Render3DScene()
        {
            // 获取可见区块
            var visibleChunks = _chunkManager.GetVisibleChunks(_renderManager.FrustumCulling);

            // 渲染区块
            _renderManager.RenderChunks(visibleChunks);
        }

        /// <summary>
        /// 渲染UI
        /// </summary>
        private void RenderUI()
        {
            if (_debugMode && _debugFont != null)
            {
                _spriteBatch.Begin();

                // 绘制调试信息
                RenderDebugInfo();

                _spriteBatch.End();
            }

            // 渲染十字准心
            RenderCrosshair();
        }

        /// <summary>
        /// 渲染调试信息
        /// </summary>
        private void RenderDebugInfo()
        {
                  
            var debugLines = new List<string>
            {
                $"MCGame Debug Mode",
                $"FPS: {_fps}",
                $"Frame Time: {_frameTime:F2}ms",
                $"",
                $"Player Position: {_playerController.Player.Position}",
                $"Player Yaw: {_playerController.Player.Yaw:F2}",
                $"Player Pitch: {_playerController.Player.Pitch:F2}",
                $"",
                $"Render Distance: {_renderDistance:F0}",
                $"Draw Calls: {_renderStats.DrawCalls}",
                $"Triangles: {_renderStats.Triangles}",
                $"Visible Chunks: {_renderStats.VisibleChunks}",
                $"",
                $"Loaded Chunks: {_chunkStats.LoadedChunks}",
                $"Queued Generation: {_chunkStats.QueuedGeneration}",
                $"Queued Meshing: {_chunkStats.QueuedMeshing}",
                $"Chunks Generated: {_chunkStats.ChunksGeneratedThisFrame}",
                $"Chunks Meshed: {_chunkStats.ChunksMeshedThisFrame}",
                $"Pool Size: {_chunkStats.PoolSize}",
                $"",
                $"Flying Mode: {_playerController.EnableFlying}",
                $"Mouse Locked: {_playerController.Player.IsMouseButtonDown(MouseButton.Right)}"
            };

            var y = 10;
            foreach (var line in debugLines)
            {
                _spriteBatch.DrawString(_debugFont, line, new Vector2(10, y), Color.White);
                y += 20;
            }
        }

        /// <summary>
        /// 渲染十字准心
        /// </summary>
        private void RenderCrosshair()
        {
            if (_spriteBatch == null) return;

            var centerX = GraphicsDevice.Viewport.Width / 2;
            var centerY = GraphicsDevice.Viewport.Height / 2;
            const int crosshairSize = 10;

            _spriteBatch.Begin();

            // 绘制十字准心
            _spriteBatch.DrawString(_debugFont ?? Content.Load<SpriteFont>("DebugFont"), "+", 
                new Vector2(centerX - 5, centerY - 10), Color.White);

            _spriteBatch.End();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void UnloadContent()
        {
            // 释放资源
            _chunkManager?.Dispose();
            _renderManager?.Dispose();
            _spriteBatch?.Dispose();
            
                    
            base.UnloadContent();
        }

        /// <summary>
        /// 获取游戏统计信息
        /// </summary>
        public GameStats GetGameStats()
        {
            return new GameStats
            {
                FPS = _fps,
                FrameTime = _frameTime,
                IsInitialized = _isInitialized,
                LoadedChunks = _chunkStats.LoadedChunks,
                VisibleChunks = _renderStats.VisibleChunks,
                DrawCalls = _renderStats.DrawCalls,
                Triangles = _renderStats.Triangles,
                RenderDistance = _renderDistance,
                DebugMode = _debugMode
            };
        }
    }

    /// <summary>
    /// 游戏统计信息
    /// </summary>
    public struct GameStats
    {
        public int FPS { get; set; }
        public float FrameTime { get; set; }
        public bool IsInitialized { get; set; }
        public int LoadedChunks { get; set; }
        public int VisibleChunks { get; set; }
        public int DrawCalls { get; set; }
        public int Triangles { get; set; }
        public float RenderDistance { get; set; }
        public bool DebugMode { get; set; }
    }

    /// <summary>
    /// 游戏入口点
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// 应用程序入口点
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // 初始化日志系统
                Logger.Initialize();
                Logger.Info("=== MCGame Starting ===");
                Logger.Info($"Platform: {Environment.OSVersion}");
                Logger.Info($"Working Directory: {Environment.CurrentDirectory}");
                Logger.Info($"Base Directory: {AppContext.BaseDirectory}");
                
                // 记录系统信息
                LogSystemInfo();

                using (var game = new MCGame())
                {
                    Logger.Info("Starting game loop...");
                    game.Run();
                }
                
                Logger.Info("=== MCGame Exiting Normally ===");
            }
            catch (Exception ex)
            {
                // 确保在异常情况下也能记录日志
                try
                {
                    Logger.Fatal($"Application crashed: {ex.Message}", ex);
                    
                    // 如果日志初始化失败，使用Console输出
                    Console.WriteLine($"=== FATAL ERROR ===");
                    Console.WriteLine($"Message: {ex.Message}");
                    Console.WriteLine($"Type: {ex.GetType().Name}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    
                    Console.WriteLine("=== END ERROR ===");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
                catch
                {
                    // 如果连Console都无法使用，至少显示一个错误对话框
                    System.Windows.Forms.MessageBox.Show(
                        $"Application crashed:\n\n{ex.Message}\n\nCheck logs directory for more details.",
                        "MCGame - Fatal Error",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error
                    );
                }
                
                Environment.Exit(1);
            }
        }

        private static void LogSystemInfo()
        {
            try
            {
                Logger.Info($"System Information:");
                Logger.Info($"  OS Version: {Environment.OSVersion}");
                Logger.Info($"  Processor Count: {Environment.ProcessorCount}");
                Logger.Info($"  Working Set: {Environment.WorkingSet / 1024 / 1024} MB");
                Logger.Info($"  Is 64-bit Process: {Environment.Is64BitProcess}");
                
                // 检查MonoGame相关
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var monogameAssemblies = assemblies.Where(a => a.FullName?.Contains("MonoGame") ?? false);
                foreach (var asm in monogameAssemblies)
                {
                    Logger.Info($"  MonoGame Assembly: {asm.FullName}");
                }
                
                // 检查ECS相关
                var ecsAssemblies = assemblies.Where(a => a.FullName?.Contains("Friflo") ?? false);
                foreach (var asm in ecsAssemblies)
                {
                    Logger.Info($"  ECS Assembly: {asm.FullName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to log system info: {ex.Message}");
            }
        }
    }
}