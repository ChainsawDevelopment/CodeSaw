using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;

namespace CodeSaw.Web
{
    [LayoutRenderer("ctx")]
    public class ContextLayoutRenderer : LayoutRenderer
    {
        [DefaultParameter]
        [RequiredParameter]
        public string Item { get; set; }

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var context = MappedDiagnosticsLogicalContext.GetObject("context") as Dictionary<string, object>;

            if (context == null)
            {
                return;
            }

            builder.Append(context[Item]);
        }
    }
}