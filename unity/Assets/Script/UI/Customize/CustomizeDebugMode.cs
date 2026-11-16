using System;
using System.Collections.Generic;

namespace AdvancedGears
{
    /// <summary>
    /// カスタマイズ画面のデバッグモード（モデルの結合を確かめるためのモード）の決まりごと。
    /// MainScene の初期化を通らずに Customize シーンを Play するとデバッグモードになり、
    /// マスタにある全パーツから機体を組める。保存データ（PlayerPrefs）は読みも書きもしない。
    /// Unity に依存しないのでテストできる
    /// </summary>
    public static class CustomizeDebugMode
    {
        public const string Notice = "デバッグモード: 保存と「戻る」は使えません（保存データは読み書きしません）";

        /// <summary>デバッグモードにするか。マスタが読み込まれていなければ MainScene を通っていない</summary>
        public static bool IsDebugMode(bool isMasterLoaded) => !isMasterLoaded;

        /// <summary>保存できるか。デバッグモードでは組める組み合わせでも保存しない</summary>
        public static bool CanSave(bool isDebugMode, bool isAssemblable) => !isDebugMode && isAssemblable;

        /// <summary>「戻る」を使えるか。デバッグモードでは戻り先の状態（StateManager）が初期化されていない</summary>
        public static bool CanGoBack(bool isDebugMode) => !isDebugMode;

        /// <summary>
        /// 編集を始めるときの組み合わせ。デバッグモードではアセットの初期値、通常は装備中のパーツ
        /// </summary>
        /// <param name="getEquippedPartId">保存データを反映した装備中のパーツ ID</param>
        /// <param name="getDefaultPartId">アセットに保存された初期値のパーツ ID（無ければ負の値）</param>
        public static Dictionary<PartsAttachType, int> BuildInitialAssembly(
            IEnumerable<PartsAttachType> slots,
            bool isDebugMode,
            Func<PartsAttachType, int> getEquippedPartId,
            Func<PartsAttachType, int> getDefaultPartId)
        {
            var dic = new Dictionary<PartsAttachType, int>();
            foreach (var slot in slots)
                dic[slot] = isDebugMode ? getDefaultPartId(slot) : getEquippedPartId(slot);
            return dic;
        }
    }
}
