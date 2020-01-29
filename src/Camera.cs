using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Apos.Engine
{
    public class Camera
    {
        public float X
        {
            get => _position.X;
            set
            {
                _position.X = value;
                UpdateRotationX();
                UpdatePosition();
            }
        }
        public float Y
        {
            get => _position.Y;
            set
            {
                _position.Y = value;
                UpdateRotationY();
                UpdatePosition();
            }
        }
        public Vector2 Pos
        {
            get => _position;
            set
            {
                _position.X = value.X;
                _position.Y = value.Y;
                UpdateRotationX();
                UpdateRotationY();
                UpdatePosition();
            }
        }
        public float Angle
        {
            get => _angle;
            set
            {
                _rotM11 = (float)Math.Cos(-(_angle = value));
                _rotM12 = (float)Math.Sin(-_angle);
                UpdateScale();
                UpdateRotationX();
                UpdateRotationY();
                UpdatePosition();
            }
        }
        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                UpdateScale();
                UpdateRotationX();
                UpdateRotationY();
                UpdatePosition();
            }
        }
        public Vector2 ViewportRes
        {
            get => _viewportSize;
            set
            {
                if (_viewportSize != value)
                {
                    _viewportSize = value;
                    _viewportSizeOver2.X = _viewportSize.X / 2;
                    _viewportSizeOver2.Y = _viewportSize.Y / 2;
                    _virtualScale = new Vector2(Math.Min(_viewportSize.X / _virtualScreenSize.X, _viewportSize.Y / _virtualScreenSize.Y));
                    UpdateScale();
                    _projection.M11 = (float)(2d / _viewportSize.X);
                    _projection.M22 = (float)(2d / -_viewportSize.Y);
                }
            }
        }
        public Vector2 VirtualRes
        {
            get => _virtualScreenSize;
            set
            {
                _virtualScreenSize = value;
                _virtualScale = new Vector2(Math.Min(_viewportSize.X / _virtualScreenSize.X, _viewportSize.Y / _virtualScreenSize.Y));
                UpdateScale();
            }
        }

        public Matrix View => _view;
        public Vector2 MousePosition => _mousePosition;
        public Matrix Projection => _projection;

        Vector2 _position,
            _scale,
            _virtualScale,
            _actualScale,
            _viewportSize,
            _viewportSizeOver2,
            _virtualScreenSize,
            _mousePosition;
        float _angle,
            _rotM11,
            _rotM12,
            _rotX1,
            _rotY1,
            _rotX2,
            _rotY2,
            _invertM11,
            _invertM12,
            _invertM21,
            _invertM22,
            _invertM41,
            _invertM42;
        double _n27;
        Matrix _view,
            _projection;

        public Camera(Vector2 position, float angle, Vector2 scale, Vector2 virtualScreenSize)
        {
            _position.X = position.X;
            _position.Y = position.Y;
            _rotM11 = (float)Math.Cos(-(_angle = angle));
            _rotM12 = (float)Math.Sin(-_angle);
            _scale = scale;
            _viewportSize = new Vector2(MGame.Viewport.Width, MGame.Viewport.Height);
            _viewportSizeOver2.X = _viewportSize.X / 2;
            _viewportSizeOver2.Y = _viewportSize.Y / 2;
            _view = new Matrix
            {
                M33 = 1,
                M44 = 1
            };
            VirtualRes = virtualScreenSize;
            _projection = new Matrix
            {
                M11 = (float)(2d / _viewportSize.X),
                M22 = (float)(2d / -_viewportSize.Y),
                M33 = -1,
                M41 = -1,
                M42 = 1,
                M44 = 1
            };
        }

        public void UpdateMousePosition(MouseState? mouseState = null)
        {
            if (!mouseState.HasValue)
                mouseState = Mouse.GetState();
            var mouseX = mouseState.Value.Position.X - MGame.Viewport.X;
            var mouseY = mouseState.Value.Position.Y - MGame.Viewport.Y;
            _mousePosition.X = (mouseX * _invertM11) + (mouseY * _invertM21) + _invertM41;
            _mousePosition.Y = (mouseX * _invertM12) + (mouseY * _invertM22) + _invertM42;
        }

        void UpdateRotationX()
        {
            var m41 = -_position.X * _actualScale.X;
            _rotX1 = m41 * _rotM11;
            _rotX2 = m41 * _rotM12;
        }

        void UpdateRotationY()
        {
            var m42 = -_position.Y * _actualScale.Y;
            _rotY1 = m42 * -_rotM12;
            _rotY2 = m42 * _rotM11;
        }

        void UpdatePosition()
        {
            _view.M41 = _rotX1 + _rotY1 + _viewportSizeOver2.X;
            _view.M42 = _rotX2 + _rotY2 + _viewportSizeOver2.Y;
            _invertM41 = (float)(-((double)_view.M21 * -_view.M42 - (double)_view.M22 * -_view.M41) * _n27);
            _invertM42 = (float)(((double)_view.M11 * -_view.M42 - (double)_view.M12 * -_view.M41) * _n27);
        }

        void UpdateScale()
        {
            _actualScale = new Vector2(_scale.X * _virtualScale.X, _scale.Y * _virtualScale.Y);
            UpdateRotationX();
            UpdateRotationY();
            _view.M11 = _actualScale.X * _rotM11;
            _view.M21 = _actualScale.X * -_rotM12;
            _view.M12 = _actualScale.Y * _rotM12;
            _view.M22 = _actualScale.Y * _rotM11;
            _n27 = 1d / ((double)_view.M11 * _view.M22 + (double)_view.M12 * -_view.M21);
            UpdatePosition();
            _invertM11 = (float)(_view.M22 * _n27);
            _invertM21 = (float)(-_view.M21 * _n27);
            _invertM12 = (float)-(_view.M12 * _n27);
            _invertM22 = (float)(_view.M11 * _n27);
        }
    }
}