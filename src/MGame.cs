using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Apos.Engine
{
    public enum UpdateState { Cancelled, Finished }
    public enum DrawState { Cancelled, Finished }
    public enum WindowState { Windowed, Fullscreen, Borderless }

    public delegate void SizeChangedEvent(int oldWidth, int oldHeight);
    public delegate UpdateState UpdateEvent();
    public delegate DrawState DrawEvent(SpriteBatch spriteBatch);

    public class Room
    {
        public static Camera Camera;

        public static void AddUpdate(UpdateEvent updateEvent) => MGame.Room._updateEvents.Add(updateEvent);
        public static void RemoveUpdate(UpdateEvent updateEvent) => MGame.Room._updateEvents.Remove(updateEvent);
        public static void RemoveUpdateAt(int i) => MGame.Room._updateEvents.RemoveAt(i);
        public static void ClearUpdates() => MGame.Room._updateEvents.Clear();
        public static void AddDraw(DrawEvent drawEvent) => MGame.Room._drawEvents.Add(drawEvent);
        public static void RemoveDraw(DrawEvent drawEvent) => MGame.Room._drawEvents.Remove(drawEvent);
        public static void RemoveDrawAt(int i) => MGame.Room._drawEvents.RemoveAt(i);
        public static void ClearDraws() => MGame.Room._drawEvents.Clear();

        readonly IList<UpdateEvent> _updateEvents = new List<UpdateEvent>();
        readonly IList<DrawEvent> _drawEvents = new List<DrawEvent>();

        /// <summary>Called when this room becomes the active room</summary>
        public virtual void OnOpen()
        {
            Camera ??= new Camera(Vector2.Zero, 0, Vector2.One, new Vector2(MGame.VirtualRes.Width, MGame.VirtualRes.Height));
            MGame.OnViewportSizeChanged += OnViewportSizeChanged;
            MGame.OnVirtualScreenSizeChanged += OnVirtualScreenSizeChanged;
        }
        /// <summary>Called when the room changes or if the game is exited - while this room is open</summary>
        public virtual void OnClose()
        {
            MGame.OnViewportSizeChanged -= OnViewportSizeChanged;
            MGame.OnVirtualScreenSizeChanged -= OnVirtualScreenSizeChanged;
        }

        public virtual void Update()
        {
            for (var i = 0; i < _updateEvents.Count; i++)
                _updateEvents[i]();
        }
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            for (var i = 0; i < _drawEvents.Count; i++)
                _drawEvents[i](spriteBatch);
        }

        void OnViewportSizeChanged(int oldWidth, int oldHeight) => Camera.ViewportRes = new Vector2(MGame.Viewport.Width, MGame.Viewport.Height);
        void OnVirtualScreenSizeChanged(int oldWidth, int oldHeight) => Camera.VirtualRes = new Vector2(MGame.VirtualRes.Width, MGame.VirtualRes.Height);
    }

    public static class Time
    {
        public static long DeltaTicks { get; internal set; }
        public static double DeltaTimeFull { get; internal set; }
        public static float DeltaTime { get; internal set; }
        public static double TotalTimeFull { get; internal set; }
        public static float TotalTime { get; internal set; }
    }

    public class MGame : Game
    {
        public const long WINDOW_ACTIVE_UPDATE_TICKS = TimeSpan.TicksPerSecond / 60,
            WINDOW_INACTIVE_UPDATE_TICKS = TimeSpan.TicksPerSecond / 30;

        public static event SizeChangedEvent OnViewportSizeChanged,
            OnVirtualScreenSizeChanged;

        public static Room Room
        {
            get => _room;
            set
            {
                if (value != null && !ReferenceEquals(value, _room))
                {
                    _room.OnClose();
                    _room = value;
                    _room.OnOpen();
                }
            }
        }

        public static new GraphicsDevice GraphicsDevice { get; private set; }
        public static GraphicsDeviceManager Graphics { get; private set; }
        public static new ContentManager Content { get; private set; }
        public static SpriteBatch SpriteBatch { get; private set; }
        public static new GameWindow Window { get; private set; }
        public static new bool IsActive { get; private set; }
        public static Viewport Viewport
        {
            get => GraphicsDevice.Viewport;
            private set => GraphicsDevice.Viewport = value;
        }
        public static (int Width, int Height, float HalfWidth, float HalfHeight) VirtualRes { get; private set; }
        public static long TicksPerUpdate { get; private set; }

        static (int Width, int Height) _oldViewportRes,
            _oldBackBufferSize;
        static Room _room;
        static bool _hasAsignedGfxDeviceReset;

        /// <summary>Sets the resolution the game is rendered at</summary>
        public static void SetVirtualRes(int width, int height)
        {
            if ((width != VirtualRes.Width) || (height != VirtualRes.Height))
            {
                int oldWidth = VirtualRes.Width,
                    oldHeight = VirtualRes.Height;
                VirtualRes = (width, height, width / 2f, height / 2f);
                OnVirtualScreenSizeChanged?.Invoke(oldWidth, oldHeight);
                ForceVirtualResUpdate();
                if (!_hasAsignedGfxDeviceReset)
                {
                    Graphics.DeviceReset += Graphics_DeviceReset;
                    _hasAsignedGfxDeviceReset = true;
                }
            }
        }
        /// <summary>Sets the resolution of the game window</summary>
        /// <param name="width">0 will retain the current window width</param>
        /// <param name="height">0 will retain the current window height</param>
        /// <param name="windowState">null will retain the current window state</param>
        public static void SetRes(int width = 0, int height = 0, WindowState? windowState = null)
        {
            if (width > 0)
                Graphics.PreferredBackBufferWidth = width;
            if (height > 0)
                Graphics.PreferredBackBufferHeight = height;
            switch (windowState)
            {
                case WindowState.Windowed:
                    Graphics.IsFullScreen = false;
                    break;
                case WindowState.Fullscreen:
                    Graphics.HardwareModeSwitch = true;
                    Graphics.IsFullScreen = true;
                    break;
                case WindowState.Borderless:
                    Graphics.HardwareModeSwitch = false;
                    Graphics.IsFullScreen = true;
                    break;
            }
            Graphics.ApplyChanges();
        }

        static void ForceVirtualResUpdate()
        {
            var targetAspectRatio = VirtualRes.Width / (float)VirtualRes.Height;
            var width2 = GraphicsDevice.PresentationParameters.BackBufferWidth;
            var height2 = (int)(width2 / targetAspectRatio + .5f);
            if (height2 > GraphicsDevice.PresentationParameters.BackBufferHeight)
            {
                height2 = GraphicsDevice.PresentationParameters.BackBufferHeight;
                width2 = (int)(height2 * targetAspectRatio + .5f);
            }
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Viewport = new Viewport()
            {
                X = (GraphicsDevice.PresentationParameters.BackBufferWidth / 2) - (width2 / 2),
                Y = (GraphicsDevice.PresentationParameters.BackBufferHeight / 2) - (height2 / 2),
                Width = width2,
                Height = height2
            };
            CheckViewportSizeChanged();
        }

        static void Graphics_DeviceReset(object sender, EventArgs e)
        {
            if (GraphicsDevice.PresentationParameters.BackBufferWidth == _oldBackBufferSize.Width && GraphicsDevice.PresentationParameters.BackBufferHeight == _oldBackBufferSize.Height)
                return;
            ForceVirtualResUpdate();
            _oldBackBufferSize = (GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
        }

        static void CheckViewportSizeChanged()
        {
            if (Viewport.Width == _oldViewportRes.Width && Viewport.Height == _oldViewportRes.Height)
                return;
            OnViewportSizeChanged?.Invoke(_oldViewportRes.Width, _oldViewportRes.Height);
            _oldViewportRes = (Viewport.Width, Viewport.Height);
        }

        public MGame(Room room)
        {
            Graphics = new GraphicsDeviceManager(this)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
                SynchronizeWithVerticalRetrace = false
            };
            _oldBackBufferSize = (GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
            Content = base.Content;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            IsFixedTimeStep = true;
            Window = base.Window;
            if (base.IsActive)
                OnActivated(this, EventArgs.Empty);
            else
                OnDeactivated(this, EventArgs.Empty);
            _room = room;
        }

        protected override void Initialize()
        {
            SpriteBatchExtensions.Initialize(GraphicsDevice = base.GraphicsDevice);
            _oldViewportRes = (Viewport.Width, Viewport.Height);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            _room.OnOpen();
        }

        protected override void Update(GameTime gameTime)
        {
            CheckViewportSizeChanged();
            Time.DeltaTicks = gameTime.ElapsedGameTime.Ticks;
            Time.DeltaTime = (float)(Time.DeltaTimeFull = gameTime.ElapsedGameTime.TotalSeconds);
            Time.TotalTime = (float)(Time.TotalTimeFull = gameTime.TotalGameTime.TotalSeconds);
            _room.Update();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            SpriteBatch.Begin();
            SpriteBatch.FillRectangle(new Rectangle(0, 0, MGame.Graphics.PreferredBackBufferWidth, MGame.Graphics.PreferredBackBufferHeight), Color.CornflowerBlue);
            SpriteBatch.End();
            _room.Draw(SpriteBatch);
            base.Draw(gameTime);
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            TargetElapsedTime = new TimeSpan(TicksPerUpdate = WINDOW_ACTIVE_UPDATE_TICKS);
            base.OnActivated(sender, args);
        }
        protected override void OnDeactivated(object sender, EventArgs args)
        {
            TargetElapsedTime = new TimeSpan(TicksPerUpdate = WINDOW_INACTIVE_UPDATE_TICKS);
            base.OnDeactivated(sender, args);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            Room.OnClose();
            base.OnExiting(sender, args);
        }
    }
}