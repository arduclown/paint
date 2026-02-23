using System;
using System.Collections.Generic;
using GraphicEditor.ViewModels;
using SkiaSharp;

namespace GraphicEditor.TeamImport;

public static class PdfExporter
{
    public static void Export(IEnumerable<ShapeViewModel> shapes, string path)
    {
        using var doc = SKDocument.CreatePdf(path)
            ?? throw new InvalidOperationException("Не удалось инициализировать PDF-документ");

        var canvas = doc.BeginPage(1600, 1200);
        canvas.Clear(SKColors.White);

        foreach (var shape in shapes)
        {
            if (!shape.IsVisible) continue;

            var skPath = SKPath.ParseSvgPathData(shape.Geometry);
            if (skPath == null) continue;

            var fc = shape.FillColor;
            if (fc.A > 0)
            {
                using var fillPaint = new SKPaint
                {
                    Color = new SKColor(fc.R, fc.G, fc.B, (byte)(fc.A * shape.Opacity)),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true,
                };
                canvas.DrawPath(skPath, fillPaint);
            }

            var sc = shape.StrokeColor;
            using var strokePaint = new SKPaint
            {
                Color = new SKColor(sc.R, sc.G, sc.B),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (float)shape.StrokeThickness,
                IsAntialias = true,
            };
            canvas.DrawPath(skPath, strokePaint);
            skPath.Dispose();
        }

        doc.EndPage();
        doc.Close();
    }
}
