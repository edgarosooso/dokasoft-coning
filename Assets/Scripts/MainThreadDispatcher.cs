using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour {
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    public void Update() {
        lock (_executionQueue) {
            while (_executionQueue.Count > 0) {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public static void Enqueue(Action action) {
        lock (_executionQueue) {
            _executionQueue.Enqueue(action);
        }
    }

    private static MainThreadDispatcher _instance = null;
    public static MainThreadDispatcher Instance() => _instance;

    void Awake() {
        if (_instance == null) _instance = this;
    }
}