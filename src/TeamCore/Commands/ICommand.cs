namespace GraphicEditor.TeamCore.Commands
{
    // Интерфейс отменяемой команды (паттерн Command + Undo)
    public interface IEditorCommand
    {
        void Execute();
        void Undo();
    }
}
