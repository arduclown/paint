using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GraphicEditor.ViewModels;

namespace GraphicEditor.TeamImport;

public static class SceneSerializer
{
    public static void ExportJson(IEnumerable<ShapeViewModel> shapes, string path)
    {
        var dtos = new List<ShapeDto>();
        foreach (var shape in shapes)
            dtos.Add(ShapeDto.FromViewModel(shape));

        var json = JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public static List<ShapeViewModel> ImportJson(string path)
    {
        var json = File.ReadAllText(path);
        var dtos = JsonSerializer.Deserialize<List<ShapeDto>>(json);
        if (dtos == null) return new List<ShapeViewModel>();

        var result = new List<ShapeViewModel>();
        foreach (var dto in dtos)
        {
            var vm = dto.ToViewModel();
            if (vm != null) result.Add(vm);
        }
        return result;
    }
}
