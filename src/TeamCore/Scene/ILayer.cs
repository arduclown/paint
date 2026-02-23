namespace GraphicEditor.TeamCore.Scene
{
    // Абстракция над слоем сцены — не зависит от UI
    public interface ILayer
    {
        string Name { get; set; }
        bool IsVisible { get; set; }
        bool IsLocked { get; set; }
        bool IsActive { get; set; }
    }
}
