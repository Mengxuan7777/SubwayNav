using System;
using System.Collections.Generic;
using UnityEngine;

// Helper class to push tasks back to the main thread
namespace Camera {
    public class MainThreadDispatcher : MonoBehaviour {
        private static readonly Queue<Action> actions = new Queue<Action>();

        public static void Enqueue(Action action) {
            lock (actions) {
                actions.Enqueue(action);
            }
        }

        void Update() {
            while (true) {
                Action action = null;
                lock (actions) {
                    if (actions.Count > 0)
                        action = actions.Dequeue();
                }
                if (action == null) break;
                action();
            }
        }
    }
}