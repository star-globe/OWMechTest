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
        Customize = 6,   // 機体カスタマイズ画面（任意アクセス）
    }

    public class StateManager : SingletonMonoBehaviour<StateManager>
    {
        readonly Dictionary<GameState, Subject<Unit>> subjectDic = new Dictionary<GameState, Subject<Unit>>();
        public GameState State { get; private set; } = GameState.None;

        readonly MissionResultHolder missionResult = new MissionResultHolder();

        /// <summary>直前の戦闘の任務の結果（Result 画面で表示する）</summary>
        public MissionResult MissionResult => missionResult.Result;

        public void SetStart()
        {
            State = GameState.Start;
        }

        /// <summary>
        /// 戦闘中に任務の結果を決める。最初に決まった結果だけが残り、すでに決まっていれば false を返す。
        /// Result への遷移は FinishBattle で行う（撃破の演出を待ってから遷移できるように分けている）
        /// </summary>
        public bool TryDecideMissionResult(MissionResult result)
        {
            if (State != GameState.Battle)
                return false;

            return missionResult.TryDecide(result);
        }

        /// <summary>戦闘を終えて Result へ遷移する。戦闘中でなければ何もしない（二重に遷移しない）</summary>
        public void FinishBattle()
        {
            if (State != GameState.Battle)
                return;

            NextState();
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

            if (nextState == GameState.Battle)
                missionResult.Reset();

            if (subjectDic.ContainsKey(nextState))
            {
                subjectDic[nextState].OnNext(Unit.Default);
            }

            State = nextState;
        }

        public void GoToState(GameState target)
        {
            if (target == GameState.Battle)
                missionResult.Reset();

            if (subjectDic.ContainsKey(target))
            {
                subjectDic[target].OnNext(Unit.Default);
            }

            State = target;
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
