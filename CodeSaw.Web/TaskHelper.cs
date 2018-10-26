using System.Threading.Tasks;
using Antlr.Runtime.Misc;

namespace CodeSaw.Web
{
    public static class TaskHelper
    {
        public static async Task<(T1, T2)> WhenAll<T1, T2>(Task<T1> task1, Task<T2> task2)
        {
            await Task.WhenAll(task1, task2);

            return (
                task1.Result,
                task2.Result
            );
        }

        public static async Task<TResult> Then<T, TResult>(this Task<T> task, Func<T, TResult> project)
        {
            var v = await task;
            return project(v);
        }
    }
}