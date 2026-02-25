namespace GraphicEditor.Common.Interfaces;

public interface IEditorCommand
{
    void Execute();
    void Undo();
}
