using System.Collections;
using System.Threading.Tasks;

namespace Camera {
    public static class CoroutineUtil {
        public static IEnumerator AsCoroutine(this Task task) {
            while (!task.IsCompleted) yield return null;
            if (task.Exception != null) throw task.Exception;
        }
    }

}