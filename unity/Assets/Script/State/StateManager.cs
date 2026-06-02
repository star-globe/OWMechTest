using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniRx;

namespace AdvancedGears
{
    public enum GameState
    {
        None      = 0,
        Start     = 1,
        Select    = 2,
        Briefing  = 3,   // ミッション選択後、出撃前の確認画面
        Battle    = 4,
        Result    = 5,
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
                case GameState.Start:    nextState = GameState.Select;    break;
                case GameState.Select:   nextState = GameState.Briefing;  break;
                case GameState.Briefing: nextState = GameState.Battle;    break;
                case GameState.Battle:   nextState = GameState.Result;    break;
                case GameState.Result:   nextState = GameState.Select;    break;
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
