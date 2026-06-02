using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniRx;

namespace AdvancedGears
{
    public enum GameState
    {
        None = 0,
        Start,
        Select,
        Battle,
        Result
    }

    public class StateManager : SingletonMonoBehaviour<StateManager>
    {
        readonly Dictionary<GameState, Subject<Unit>> subjectDic = new Dictionary<GameState, Subject<Unit>>();
        public GameState State { get; private set; } = GameState.None;

        public void SetStart()
        {
            State = GameState.Start;
        }

        public void NextState()
        {
            GameState nextState = State;

            switch (State)
            {
                case GameState.Start:   nextState = GameState.Select;   break;
                case GameState.Select:  nextState = GameState.Battle;   break;
                case GameState.Battle:  nextState = GameState.Result;   break;
                case GameState.Result:  nextState = GameState.Select;   break;
            }

            if (subjectDic.ContainsKey(nextState))
            {
                subjectDic[nextState].OnNext(Unit.Default);
            }

            State = nextState;
        }

        public void RegisterStateEvent(GameState state, Action<Unit> action, GameObject gameObject)
        {
            if (subjectDic.TryGetValue(state, out var subject) == false)
            {
                subject = new Subject<Unit>();
                subjectDic.Add(state, subject);
            }

            subject.Subscribe(action).AddTo(gameObject);
        }
    }
}
