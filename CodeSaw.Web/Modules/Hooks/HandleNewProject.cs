using CodeSaw.Web.Cqrs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeSaw.Web.Modules.Hooks
{
    public class HandleNewProject : ICommand
    {
        public int ProjectId { get; }
        public string ProjectPath { get; }
       
        public HandleNewProject(int projectId, string projectPath)
        {
            ProjectId = projectId;
            ProjectPath = projectPath;
        }

        public class Handler : CommandHandler<HandleNewProject>
        {
            private readonly IEventBus _eventBus;

            public Handler(IEventBus eventBus)
            {
                _eventBus = eventBus;
            }

            public override Task Handle(HandleNewProject command)
            {
                _eventBus.Publish(new NewProjectEvent(command.ProjectId, command.ProjectPath));
                return Task.CompletedTask;
            }
        }
    }
}
