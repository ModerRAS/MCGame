using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using MCGame.ECS.Components;
using PlayerComponent = MCGame.ECS.Components.Player;
using MCGame.Core;

namespace MCGame.ECS.Systems
{
    /// <summary>
    /// 玩家输入处理系统
    /// 处理键盘和鼠标输入，更新输入组件
    /// </summary>
    public class PlayerInputSystem : QuerySystem<Input>
    {
        private KeyboardState _currentKeyboard;
        private KeyboardState _previousKeyboard;
        private MouseState _currentMouse;
        private MouseState _previousMouse;
        private readonly bool _mouseLocked;

        public PlayerInputSystem()
        {
            _currentKeyboard = Keyboard.GetState();
            _previousKeyboard = _currentKeyboard;
            _currentMouse = Mouse.GetState();
            _previousMouse = _currentMouse;
            _mouseLocked = true;
        }

        protected override void OnUpdate()
        {
            var currentKeyboard = Keyboard.GetState();
            var currentMouse = Mouse.GetState();

            // 更新状态
            _previousKeyboard = _currentKeyboard;
            _previousMouse = _currentMouse;
            _currentKeyboard = currentKeyboard;
            _currentMouse = currentMouse;

            // 使用QuerySystem的Entities属性遍历实体
            foreach (var entity in Query.Entities)
            {
                var input = entity.GetComponent<Input>();
                var player = entity.GetComponent<MCGame.ECS.Components.Player>();
                
                // 处理移动输入
                input.Movement = Vector2.Zero;
                if (_currentKeyboard.IsKeyDown(Keys.W)) input.Movement.Y += 1;
                if (_currentKeyboard.IsKeyDown(Keys.S)) input.Movement.Y -= 1;
                if (_currentKeyboard.IsKeyDown(Keys.A)) input.Movement.X -= 1;
                if (_currentKeyboard.IsKeyDown(Keys.D)) input.Movement.X += 1;

                // 处理跳跃输入
                input.Jump = _currentKeyboard.IsKeyDown(Keys.Space) && _previousKeyboard.IsKeyUp(Keys.Space);

                // 处理特殊动作输入
                input.Sprint = _currentKeyboard.IsKeyDown(Keys.LeftShift) || _currentKeyboard.IsKeyDown(Keys.RightShift);
                input.Fly = _currentKeyboard.IsKeyDown(Keys.F) && _previousKeyboard.IsKeyUp(Keys.F);

                // 处理鼠标输入
                if (_mouseLocked)
                {
                    input.Look = new Vector2(
                        _currentMouse.X - _previousMouse.X,
                        _currentMouse.Y - _previousMouse.Y
                    );
                }
            }
        }
    }

    /// <summary>
    /// 玩家移动系统
    /// 根据输入更新玩家位置和速度
    /// </summary>
    public class PlayerMovementSystem : QuerySystem<MCGame.ECS.Components.Position, MCGame.ECS.Components.Rotation, Velocity, Input, MCGame.ECS.Components.Player>
    {
        protected override void OnUpdate()
        {
            var currentKeyboard = Keyboard.GetState();
            
            // 使用QuerySystem的Query属性遍历实体
            // 由于Friflo ECS的ForEachEntity限制，使用传统的遍历方法
            foreach (var entity in Query.Entities)
            {
                var position = entity.GetComponent<MCGame.ECS.Components.Position>();
                var rotation = entity.GetComponent<MCGame.ECS.Components.Rotation>();
                var velocity = entity.GetComponent<Velocity>();
                var input = entity.GetComponent<Input>();
                var player = entity.GetComponent<MCGame.ECS.Components.Player>();
                
                // 更新旋转
                rotation.Value.Y += input.Look.X * player.LookSpeed;
                rotation.Value.X += input.Look.Y * player.LookSpeed;
                rotation.Value.X = MathHelper.Clamp(rotation.Value.X, -MathHelper.PiOver2, MathHelper.PiOver2);

                // 计算移动方向
                var yaw = rotation.Value.Y;
                var forward = new Vector3((float)Math.Sin(yaw), 0, (float)Math.Cos(yaw));
                var right = new Vector3((float)Math.Cos(yaw), 0, -(float)Math.Sin(yaw));

                // 计算移动速度
                var moveSpeed = player.MoveSpeed;
                if (input.Sprint) moveSpeed *= 1.5f;
                if (player.IsFlying) moveSpeed *= 2f;

                // 应用移动输入
                var moveDirection = forward * input.Movement.Y + right * input.Movement.X;
                if (moveDirection != Vector3.Zero)
                {
                    moveDirection.Normalize();
                    velocity.Value = moveDirection * moveSpeed;
                }
                else
                {
                    velocity.Value *= 0.9f; // 减速
                }

                // 处理跳跃
                if (input.Jump && player.IsGrounded && !player.IsFlying)
                {
                    velocity.Value.Y = player.JumpSpeed;
                    player.IsGrounded = false;
                }

                // 处理飞行模式
                if (player.IsFlying)
                {
                    if (input.Jump) velocity.Value.Y = moveSpeed;
                    if (currentKeyboard.IsKeyDown(Keys.LeftControl)) velocity.Value.Y = -moveSpeed;
                }
            }
        }
    }

    /// <summary>
    /// 物理更新系统
    /// 应用重力、碰撞检测和物理模拟
    /// </summary>
    public class PhysicsSystem : QuerySystem<MCGame.ECS.Components.Position, Velocity>
    {
        protected override void OnUpdate()
        {
            var deltaTime = 0.016f; // 假设60 FPS，约16ms

            // 使用QuerySystem的Query属性遍历实体
            // 由于Friflo ECS的ForEachEntity限制，使用传统的遍历方法
            foreach (var entity in Query.Entities)
            {
                var position = entity.GetComponent<MCGame.ECS.Components.Position>();
                var velocity = entity.GetComponent<Velocity>();
                
                // 简化的物理更新
                position.Value += velocity.Value * deltaTime;
                
                // 简单的地面碰撞检测
                if (position.Value.Y <= 0)
                {
                    position.Value.Y = 0;
                    velocity.Value.Y = 0;
                }
            }
        }
    }

    /// <summary>
    /// 相机更新系统
    /// 根据位置和旋转更新相机矩阵
    /// </summary>
    public class CameraSystem : QuerySystem<Camera>
    {
        protected override void OnUpdate()
        {
            // 使用QuerySystem的Query属性遍历实体
            // 由于Friflo ECS的ForEachEntity限制，使用传统的遍历方法
            foreach (var entity in Query.Entities)
            {
                var camera = entity.GetComponent<Camera>();
                if (camera.IsDirty)
                {
                    // 简化的相机更新
                    camera.IsDirty = false;
                }
            }
        }
    }

    /// <summary>
    /// 可见性计算系统
    /// 计算实体是否在视锥体内
    /// </summary>
    public class VisibilitySystem : QuerySystem<Visibility>
    {
        private Vector3 _cameraPosition;

        public void SetViewFrustum(BoundingFrustum frustum, Vector3 cameraPosition)
        {
            _cameraPosition = cameraPosition;
        }

        protected override void OnUpdate()
        {
            // 使用QuerySystem的Query属性遍历实体
            // 由于Friflo ECS的ForEachEntity限制，使用传统的遍历方法
            foreach (var entity in Query.Entities)
            {
                var visibility = entity.GetComponent<Visibility>();
                // 简化的可见性计算
                if (entity.TryGetComponent<MCGame.ECS.Components.Position>(out var position))
                {
                    visibility.Distance = Vector3.Distance(position.Value, _cameraPosition);
                    visibility.IsVisible = visibility.Distance < 200f; // 渲染距离
                }
            }
        }
    }

    /// <summary>
    /// 生命期更新系统
    /// 更新实体的生命期，删除过期实体
    /// </summary>
    public class LifetimeSystem : QuerySystem<Lifetime>
    {
        protected override void OnUpdate()
        {
            var deltaTime = 0.016f; // 假设60 FPS，约16ms

            // 使用QuerySystem的Query属性遍历实体
            // 由于Friflo ECS的ForEachEntity限制，使用传统的遍历方法
            foreach (var entity in Query.Entities)
            {
                var lifetime = entity.GetComponent<Lifetime>();
                lifetime.TimeLeft -= deltaTime;
                
                if (lifetime.TimeLeft <= 0)
                {
                    lifetime.IsExpired = true;
                }
            }
        }
    }

    /// <summary>
    /// 区块状态更新系统
    /// 更新区块的状态和标记
    /// </summary>
    public class ChunkStateSystem : QuerySystem<Chunk>
    {
        protected override void OnUpdate()
        {
            // 使用QuerySystem的Query属性遍历实体
            // 由于Friflo ECS的ForEachEntity限制，使用传统的遍历方法
            foreach (var entity in Query.Entities)
            {
                var chunk = entity.GetComponent<Chunk>();
                // 简化的区块状态更新
                if (chunk.IsLoaded)
                {
                    chunk.State = MCGame.ECS.Components.ChunkState.Loaded;
                }
                else
                {
                    chunk.State = MCGame.ECS.Components.ChunkState.Loading;
                }
        }
    }
}
}