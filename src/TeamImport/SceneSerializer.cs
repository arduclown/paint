using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GraphicEditor.ViewModels;

namespace GraphicEditor.TeamImport;

public static class SceneSerializer
{
    // Настройки сериализации — создаём один раз
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void ExportJson(IEnumerable<ShapeViewModel> shapes, string path)
    {
        var dtos = shapes.Select(ShapeDto.FromViewModel).ToList();
        var json = JsonSerializer.Serialize(dtos, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static List<ShapeViewModel> ImportJson(string path)
    {
        var json = File.ReadAllText(path);
        var dtos = JsonSerializer.Deserialize<List<ShapeDto>>(json);
        if (dtos is null) return [];

        return dtos
            .Select(dto => dto.ToViewModel())
            .Where(vm => vm is not null)
            .Cast<ShapeViewModel>()
            .ToList();
    }
}
