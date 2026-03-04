using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawProject.Services
{
    class UndoRedoService
    {

        private readonly Stack<IUndoRedoCommand> _undoStack = new();
        private readonly Stack<IUndoRedoCommand> _redoStack = new();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void ExecuteCommand(IUndoRedoCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
            // Здесь можно вызвать событие изменения состояния, чтобы обновить UI (кнопки Undo/Redo)
        }

        public void Undo()
        {
            if (!CanUndo) return;
            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
            // Событие
        }

        public void Redo()
        {
            if (!CanRedo) return;
            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
            // Событие
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}
