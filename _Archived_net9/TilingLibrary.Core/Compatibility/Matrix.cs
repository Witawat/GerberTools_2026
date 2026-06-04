using System.Numerics;
using SixLabors.ImageSharp;

namespace TilingLibrary.Compatibility
{
    /// <summary>
    /// Matrix wrapper to provide System.Drawing.Drawing2D.Matrix compatibility using System.Numerics.Matrix3x2
    /// </summary>
    public class Matrix
    {
        private Matrix3x2 _matrix = Matrix3x2.Identity;

        public Matrix()
        {
            _matrix = Matrix3x2.Identity;
        }

        public Matrix(float m11, float m12, float m21, float m22, float dx, float dy)
        {
            _matrix = new Matrix3x2(m11, m12, m21, m22, dx, dy);
        }

        public Matrix Clone()
        {
            var m = new Matrix();
            m._matrix = this._matrix;
            return m;
        }

        public Matrix3x2 InnerMatrix => _matrix;

        public void Reset()
        {
            _matrix = Matrix3x2.Identity;
        }

        public void RotateAt(float angle, PointF center)
        {
            var radians = angle * (float)(Math.PI / 180.0);
            var translation1 = Matrix3x2.CreateTranslation(-center.X, -center.Y);
            var rotation = Matrix3x2.CreateRotation(radians);
            var translation2 = Matrix3x2.CreateTranslation(center.X, center.Y);
            _matrix = translation1 * rotation * translation2 * _matrix;
        }

        public void Rotate(float angle)
        {
            var radians = angle * (float)(Math.PI / 180.0);
            _matrix = Matrix3x2.CreateRotation(radians) * _matrix;
        }

        public void Translate(float dx, float dy)
        {
            _matrix = Matrix3x2.CreateTranslation(dx, dy) * _matrix;
        }

        public void Scale(float sx, float sy)
        {
            _matrix = Matrix3x2.CreateScale(sx, sy) * _matrix;
        }

        public void TransformPoints(PointF[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                var p = new Vector2(points[i].X, points[i].Y);
                p = Vector2.Transform(p, _matrix);
                points[i] = new PointF(p.X, p.Y);
            }
        }

        public void Multiply(Matrix matrix)
        {
            _matrix = _matrix * matrix._matrix;
        }
    }
}
