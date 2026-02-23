using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GraphicEditor.ViewModels;

namespace GraphicEditor.TeamImport;

public static class SvgExporter
{
    public static void Export(IEnumerable<ShapeViewModel> shapes, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1600\" height=\"1200\" viewBox=\"0 0 1600 1200\">");
        sb.AppendLine("  <rect width=\"1600\" height=\"1200\" fill=\"white\"/>");

        foreach (var shape in shapes)
        {
            if (!shape.IsVisible) continue;

            var fc = shape.FillColor;
            var sc = shape.StrokeColor;

            var fill = fc.A == 0
                ? "none"
                : FormattableString.Invariant($"#{fc.R:X2}{fc.G:X2}{fc.B:X2}");

            var fillOp = fc.A == 0
                ? "0"
                : (fc.A / 255.0 * shape.Opacity).ToString("F3", CultureInfo.InvariantCulture);

            var stroke  = FormattableString.Invariant($"#{sc.R:X2}{sc.G:X2}{sc.B:X2}");
            var strokeW = shape.StrokeThickness.ToString("F1", CultureInfo.InvariantCulture);

            sb.AppendLine(
                $"  <path d=\"{shape.Geometry}\" " +
                $"fill=\"{fill}\" fill-opacity=\"{fillOp}\" " +
                $"stroke=\"{stroke}\" stroke-width=\"{strokeW}\" />");
        }

        sb.AppendLine("</svg>");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }
}
