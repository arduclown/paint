using Avalonia;
using GraphicEditor.TeamCore;
using GraphicEditor.TeamTools.Shapes;

namespace GraphicEditor.Tests.Core;

[TestFixture]
public class ShapeFactoryTests
{
    private const double MinSize = 5.0;
    private const double Delta = 1e-10;

    // CreateCircle

    [Test]
    public void CreateCircle_RadiusEqualsDistanceCenterToEdge()
    {
        // 3-4-5 triangle -> radius = 5
        var circle = ShapeFactory.CreateCircle(new Point(0, 0), new Point(3, 4));

        Assert.That(circle.RadiusX, Is.EqualTo(5.0).Within(Delta));
    }

    [Test]
    public void CreateCircle_CenterIsPreserved()
    {
        var center = new Point(10, 20);

        var circle = ShapeFactory.CreateCircle(center, new Point(13, 24));

        Assert.That(circle.Center.X, Is.EqualTo(10.0).Within(Delta));
        Assert.That(circle.Center.Y, Is.EqualTo(20.0).Within(Delta));
    }

    [Test]
    public void CreateCircle_EnforcesMinimumRadius_WhenDistanceTooSmall()
    {
        // distance 1.41 < MinSize
        var circle = ShapeFactory.CreateCircle(new Point(0, 0), new Point(1, 1));

        Assert.That(circle.RadiusX, Is.EqualTo(MinSize).Within(Delta));
    }

    [Test]
    public void CreateCircle_EnforcesMinimumRadius_WhenEdgeEqualsCenter()
    {
        var circle = ShapeFactory.CreateCircle(new Point(5, 5), new Point(5, 5));

        Assert.That(circle.RadiusX, Is.EqualTo(MinSize).Within(Delta));
    }

    [Test]
    public void CreateCircle_ReturnsCircleInstance()
    {
        var result = ShapeFactory.CreateCircle(new Point(0, 0), new Point(10, 0));

        Assert.That(result, Is.InstanceOf<Circle>());
    }

    // CreateRectangle

    [Test]
    public void CreateRectangle_NormalPoints_ReturnsCorrectSize()
    {
        // Rectangle from (0,0) to (10,20): width=10, height=20 -> normalized points
        var rect = ShapeFactory.CreateRectangle(new Point(0, 0), new Point(10, 20));

        // Points[0] = top-left (min,min), Points[2] = bottom-right (max,max)
        Assert.That(rect.Points[0].X, Is.EqualTo(0).Within(Delta));
        Assert.That(rect.Points[0].Y, Is.EqualTo(0).Within(Delta));
        Assert.That(rect.Points[2].X, Is.EqualTo(10).Within(Delta));
        Assert.That(rect.Points[2].Y, Is.EqualTo(20).Within(Delta));
    }

    [Test]
    public void CreateRectangle_EnforcesMinimumWidth_WhenDxTooSmall()
    {
        // dx = 2 < MinSize -> x2 = p1.X + MinSize = 5
        var rect = ShapeFactory.CreateRectangle(new Point(0, 0), new Point(2, 20));

        double width = rect.Points[2].X - rect.Points[0].X;
        Assert.That(width, Is.EqualTo(MinSize).Within(Delta));
    }

    [Test]
    public void CreateRectangle_EnforcesMinimumHeight_WhenDyTooSmall()
    {
        // dy = 2 < MinSize -> y2 = p1.Y + MinSize = 5
        var rect = ShapeFactory.CreateRectangle(new Point(0, 0), new Point(10, 2));

        double height = rect.Points[2].Y - rect.Points[0].Y;
        Assert.That(height, Is.EqualTo(MinSize).Within(Delta));
    }

    [Test]
    public void CreateRectangle_EnforcesBothMinima_WhenBothDimensionsTooSmall()
    {
        var rect = ShapeFactory.CreateRectangle(new Point(0, 0), new Point(1, 1));

        double width = rect.Points[2].X - rect.Points[0].X;
        double height = rect.Points[2].Y - rect.Points[0].Y;
        Assert.That(width, Is.EqualTo(MinSize).Within(Delta));
        Assert.That(height, Is.EqualTo(MinSize).Within(Delta));
    }

    [Test]
    public void CreateRectangle_NormalizesPointOrder_WhenP2LessThanP1()
    {
        // p2 is top-left, p1 is bottom-right
        var rect = ShapeFactory.CreateRectangle(new Point(10, 20), new Point(0, 0));

        Assert.That(rect.Points[0].X, Is.EqualTo(0).Within(Delta));
        Assert.That(rect.Points[0].Y, Is.EqualTo(0).Within(Delta));
        Assert.That(rect.Points[2].X, Is.EqualTo(10).Within(Delta));
        Assert.That(rect.Points[2].Y, Is.EqualTo(20).Within(Delta));
    }

    [Test]
    public void CreateRectangle_ReturnsRectangleInstance()
    {
        var result = ShapeFactory.CreateRectangle(new Point(0, 0), new Point(10, 10));

        Assert.That(result, Is.InstanceOf<Rectangle>());
    }

    // CreateTriangle

    [Test]
    public void CreateTriangle_ApexIsAtTopCenter()
    {
        // p1=(0,0), p2=(10,20) -> x1=0, x2=10, y1=0, y2=20 -> apex=(5, 0)
        var tri = ShapeFactory.CreateTriangle(new Point(0, 0), new Point(10, 20));

        Assert.That(tri.Points[0].X, Is.EqualTo(5.0).Within(Delta));
        Assert.That(tri.Points[0].Y, Is.EqualTo(0.0).Within(Delta));
    }

    [Test]
    public void CreateTriangle_BasePointsAreCorrect()
    {
        // p1=(0,0), p2=(10,20) -> left=(0,20), right=(10,20)
        var tri = ShapeFactory.CreateTriangle(new Point(0, 0), new Point(10, 20));

        Assert.That(tri.Points[1].X, Is.EqualTo(0.0).Within(Delta));
        Assert.That(tri.Points[1].Y, Is.EqualTo(20.0).Within(Delta));
        Assert.That(tri.Points[2].X, Is.EqualTo(10.0).Within(Delta));
        Assert.That(tri.Points[2].Y, Is.EqualTo(20.0).Within(Delta));
    }

    [Test]
    public void CreateTriangle_EnforcesMinimumWidth()
    {
        // dx=2 < MinSize -> x2=x1+5; p1=(0,0), p2=(2,20)
        var tri = ShapeFactory.CreateTriangle(new Point(0, 0), new Point(2, 20));

        double width = tri.Points[2].X - tri.Points[1].X;
        Assert.That(width, Is.EqualTo(MinSize).Within(Delta));
    }

    [Test]
    public void CreateTriangle_EnforcesMinimumHeight()
    {
        // dy=2 < MinSize -> y2=y1+5; p1=(0,0), p2=(10,2)
        var tri = ShapeFactory.CreateTriangle(new Point(0, 0), new Point(10, 2));

        double height = tri.Points[1].Y - tri.Points[0].Y;
        Assert.That(height, Is.EqualTo(MinSize).Within(Delta));
    }

    [Test]
    public void CreateTriangle_NormalizesPointOrder_WhenP2LessThanP1()
    {
        // Same bounding box regardless of which corner is p1 or p2
        var tri1 = ShapeFactory.CreateTriangle(new Point(0, 0), new Point(10, 20));
        var tri2 = ShapeFactory.CreateTriangle(new Point(10, 20), new Point(0, 0));

        Assert.That(tri1.Points[0].X, Is.EqualTo(tri2.Points[0].X).Within(Delta));
        Assert.That(tri1.Points[0].Y, Is.EqualTo(tri2.Points[0].Y).Within(Delta));
    }

    [Test]
    public void CreateTriangle_ReturnsTriangleInstance()
    {
        var result = ShapeFactory.CreateTriangle(new Point(0, 0), new Point(10, 10));

        Assert.That(result, Is.InstanceOf<Triangle>());
    }

    // CreateLine

    [Test]
    public void CreateLine_NormalPoints_PreservesEndpoints()
    {
        var line = ShapeFactory.CreateLine(new Point(0, 0), new Point(10, 0));

        Assert.That(line.Points[0].X, Is.EqualTo(0).Within(Delta));
        Assert.That(line.Points[1].X, Is.EqualTo(10).Within(Delta));
    }

    [Test]
    public void CreateLine_EnforcesMinimumLength_WhenManhattanDistanceTooSmall()
    {
        // |dx|+|dy| = 1+1 = 2 < 5 → p2 = (p1.X + MinSize, p1.Y)
        var line = ShapeFactory.CreateLine(new Point(0, 0), new Point(1, 1));

        Assert.That(line.Points[1].X, Is.EqualTo(MinSize).Within(Delta));
        Assert.That(line.Points[1].Y, Is.EqualTo(0).Within(Delta));
    }

    [Test]
    public void CreateLine_EnforcesMinimumLength_WhenPointsAreIdentical()
    {
        var line = ShapeFactory.CreateLine(new Point(3, 7), new Point(3, 7));

        double dx = line.Points[1].X - line.Points[0].X;
        double dy = line.Points[1].Y - line.Points[0].Y;
        Assert.That(Math.Abs(dx) + Math.Abs(dy), Is.GreaterThanOrEqualTo(MinSize));
    }

    [Test]
    public void CreateLine_DoesNotAlterP2_WhenDistanceSufficient()
    {
        // Manhattan distance = 10 >= MinSize
        var line = ShapeFactory.CreateLine(new Point(0, 0), new Point(10, 0));

        Assert.That(line.Points[1].X, Is.EqualTo(10).Within(Delta));
        Assert.That(line.Points[1].Y, Is.EqualTo(0).Within(Delta));
    }

    [Test]
    public void CreateLine_StartsAtP1()
    {
        var line = ShapeFactory.CreateLine(new Point(5, 3), new Point(15, 3));

        Assert.That(line.Points[0].X, Is.EqualTo(5).Within(Delta));
        Assert.That(line.Points[0].Y, Is.EqualTo(3).Within(Delta));
    }

    [Test]
    public void CreateLine_ReturnsLineInstance()
    {
        var result = ShapeFactory.CreateLine(new Point(0, 0), new Point(10, 0));

        Assert.That(result, Is.InstanceOf<Line>());
    }
}
