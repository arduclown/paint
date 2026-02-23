using Avalonia;

namespace GraphicEditor.TeamTools.Shapes
{
    public interface IShape
    {
        // Данные для сериализации в формат для Avalonia
        string SerializedData { get; }
        
        // Перемещение фигуры
        void Move(Point offset);

        // Изменение масштаба
        void Scale(double ratio);

        // Поворот на угол
        void Rotate(double angle);

        // Отразить по горизонтали
        void MirrorX();

        // Отразить по вертикали
        void MirrorY();
    }
}