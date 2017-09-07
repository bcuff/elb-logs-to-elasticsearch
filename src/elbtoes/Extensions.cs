using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace elbtoes
{
    internal static class Extensions
    {
        public static void PropagateCompletion(this IDataflowBlock source, IDataflowBlock dest)
        {
            source.Completion.PropagateCompletion(dest);
        }

        public static void PropagateCompletion(this Task source, IDataflowBlock dest)
        {
            source.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    dest.Fault(t.Exception);
                }
                else if (t.IsCanceled)
                {
                    dest.Fault(new TaskCanceledException());
                }
                else
                {
                    dest.Complete();
                }
            });
        }
    }
}
