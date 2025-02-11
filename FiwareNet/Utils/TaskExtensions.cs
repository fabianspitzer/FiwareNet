using System;
using System.Threading.Tasks;

namespace FiwareNet.Utils;

internal static class TaskExtensions
{
    public static async void OnError(this Task task, Action<Exception> action = null)
    {
        if (task.IsCompleted)
        {
            if (task.IsFaulted) action?.Invoke(task.Exception);
            return;
        }

        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            action?.Invoke(ex);
        }
    }
}