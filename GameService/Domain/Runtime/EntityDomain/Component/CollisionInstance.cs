using Contract.Common;
using Contract.Enum.EntityDomain;
using Domain.Abstraction;

namespace Domain.Runtime.EntityDomain.Component
{
    public class CollisionInstance : ComponentInstance
    {
        #region Attributes
        #endregion

        #region Properties
        public ICollisionShape CollisionShape { get; }
        public CollisionShapeType ShapeType { get; }
        public Vector2 CollisionOffset { get; }
        public CollisionLayer Layer { get; private set; }
        public CollisionLayer Mask { get; private set; }
        public float Width { get; }
        public float Height { get; }
        public float Radius { get; }
        public bool IsBlocking { get; }
        #endregion

        public CollisionInstance(
            Guid definitionId,
            ICollisionShape collisionShape,
            Vector2 collisionOffset,
            CollisionLayer layer,
            CollisionLayer mask) : base(definitionId)
        {
            CollisionShape = collisionShape;
            CollisionOffset = collisionOffset;
            Layer = layer;
            Mask = mask;

            IsBlocking = collisionShape.IsBlocking;

            switch (collisionShape)
            {
                case PointShape:
                    ShapeType = CollisionShapeType.Point;
                    break;

                case BoxShape box:
                    ShapeType = CollisionShapeType.Box;
                    Width = box.Width;
                    Height = box.Height;
                    break;

                case CircleShape circle:
                    ShapeType = CollisionShapeType.Circle;
                    Radius = circle.Radius;
                    break;
            }
        }

        #region Methods
        #endregion
    }

    public class PointShape : ICollisionShape
    {
        #region Attributes
        public bool IsBlocking { get; }
        #endregion

        #region Properties
        #endregion

        public PointShape(
            bool isBlocking)
        {
            IsBlocking = isBlocking;
        }

        #region Methods
        public int ComputeCells(
            Vector2 position,
            Span<(int x, int y)> output)
        {
            if (output.Length == 0)
                return 0;

            output[0] = ((int)position.X, (int)position.Y);
            return 1;
        }

        public bool Intersects(
            Vector2 selfPosition,
            ICollisionShape other,
            Vector2 otherPosition)
        {
            switch (other)
            {
                case PointShape:
                    return
                        selfPosition.X == otherPosition.X &&
                        selfPosition.Y == otherPosition.Y;

                case BoxShape box:
                    {
                        float halfW = box.Width / 2f;
                        float halfH = box.Height / 2f;

                        return
                            selfPosition.X >= otherPosition.X - halfW &&
                            selfPosition.X <= otherPosition.X + halfW &&
                            selfPosition.Y >= otherPosition.Y - halfH &&
                            selfPosition.Y <= otherPosition.Y + halfH;
                    }

                case CircleShape circle:
                    {
                        float dx = selfPosition.X - otherPosition.X;
                        float dy = selfPosition.Y - otherPosition.Y;

                        return dx * dx + dy * dy <=
                               circle.Radius * circle.Radius;
                    }
            }

            return false;
        }
        #endregion
    }

    public class BoxShape : ICollisionShape
    {
        #region Attributes
        #endregion

        #region Properties
        public float Width { get; }
        public float Height { get; }
        public bool IsBlocking { get; }
        #endregion

        public BoxShape(
            float width,
            float height, bool
            isBlocking)
        {
            Width = width;
            Height = height;
            IsBlocking = isBlocking;
        }

        #region Methods
        public int ComputeCells(
            Vector2 position,
            Span<(int x, int y)> output)
        {
            int count = 0;

            int minX = (int)MathF.Floor(position.X - Width / 2);
            int maxX = (int)MathF.Floor(position.X + Width / 2);
            int minY = (int)MathF.Floor(position.Y - Height / 2);
            int maxY = (int)MathF.Floor(position.Y + Height / 2);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (count >= output.Length)
                        return count;

                    output[count++] = (x, y);
                }
            }

            return count;
        }

        public bool Intersects(
            Vector2 selfPosition,
            ICollisionShape other,
            Vector2 otherPosition)
        {
            switch (other)
            {
                case PointShape point:
                    return point.Intersects(
                        otherPosition,
                        this,
                        selfPosition);

                case BoxShape otherBox:
                    {
                        float aMinX = selfPosition.X - Width / 2f;
                        float aMaxX = selfPosition.X + Width / 2f;

                        float aMinY = selfPosition.Y - Height / 2f;
                        float aMaxY = selfPosition.Y + Height / 2f;

                        float bMinX = otherPosition.X - otherBox.Width / 2f;
                        float bMaxX = otherPosition.X + otherBox.Width / 2f;

                        float bMinY = otherPosition.Y - otherBox.Height / 2f;
                        float bMaxY = otherPosition.Y + otherBox.Height / 2f;

                        return
                            aMinX <= bMaxX &&
                            aMaxX >= bMinX &&
                            aMinY <= bMaxY &&
                            aMaxY >= bMinY;
                    }

                case CircleShape circle:
                    {
                        float halfW = Width / 2f;
                        float halfH = Height / 2f;

                        float minX = selfPosition.X - halfW;
                        float maxX = selfPosition.X + halfW;

                        float minY = selfPosition.Y - halfH;
                        float maxY = selfPosition.Y + halfH;

                        float closestX =
                            MathF.Max(minX,
                            MathF.Min(otherPosition.X, maxX));

                        float closestY =
                            MathF.Max(minY,
                            MathF.Min(otherPosition.Y, maxY));

                        float dx = otherPosition.X - closestX;
                        float dy = otherPosition.Y - closestY;

                        return dx * dx + dy * dy <=
                               circle.Radius * circle.Radius;
                    }
            }

            return false;
        }
        #endregion
    }

    public class CircleShape : ICollisionShape
    {
        #region Attributes
        #endregion

        #region Properties
        public float Radius { get; }
        public bool IsBlocking { get; }
        #endregion

        public CircleShape(
            float radius,
            bool isBlocking)
        {
            Radius = radius;
            IsBlocking = isBlocking;
        }

        #region Methods
        public int ComputeCells(
            Vector2 position,
            Span<(int x, int y)> output)
        {
            int count = 0;

            int r = (int)MathF.Ceiling(Radius);
            float rSq = Radius * Radius;

            int centerX = (int)position.X;
            int centerY = (int)position.Y;

            for (int x = -r; x <= r; x++)
            {
                for (int y = -r; y <= r; y++)
                {
                    if (x * x + y * y > rSq)
                        continue;

                    if (count >= output.Length)
                        return count;

                    output[count++] = (centerX + x, centerY + y);
                }
            }

            return count;
        }

        public bool Intersects(
            Vector2 selfPosition,
            ICollisionShape other,
            Vector2 otherPosition)
        {
            switch (other)
            {
                case PointShape point:
                    return point.Intersects(
                        otherPosition,
                        this,
                        selfPosition);

                case BoxShape box:
                    return box.Intersects(
                        otherPosition,
                        this,
                        selfPosition);

                case CircleShape otherCircle:
                    {
                        float dx =
                            selfPosition.X - otherPosition.X;

                        float dy =
                            selfPosition.Y - otherPosition.Y;

                        float radius =
                            Radius + otherCircle.Radius;

                        return dx * dx + dy * dy <=
                               radius * radius;
                    }
            }

            return false;
        }
        #endregion
    }
}