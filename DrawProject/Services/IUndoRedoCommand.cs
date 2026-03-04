using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrawProject.Services
{
    internal interface IUndoRedoCommand
    {
        public void Execute();
        public void Undo();
    }
}
