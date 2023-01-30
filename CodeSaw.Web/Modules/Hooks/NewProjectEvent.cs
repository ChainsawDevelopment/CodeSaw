using CodeSaw.Web.Cqrs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeSaw.Web.Modules.Hooks
{
    public class NewProjectEvent : Event
    {
        public int ProjectId { get; }
        public string ProjectPath { get; }

        public NewProjectEvent(int projectId, string projectPath)
        {
            ProjectId = projectId;
            ProjectPath = projectPath;
        }
    }
}
