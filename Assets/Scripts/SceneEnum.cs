using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SceneEnum
{
    // --- 확정된 씬 구조 ---
    LoginScene,      // 로그인 및 타이틀 씬 (기존 Title)
    LobbyScene,      // 매치메이킹 로비 씬 (기존 Home)
    LoadingScene,    // 로딩 전용 씬 (추가)
    BattleScene,     // 전투 씬 (기존 Battle)
    ResultScene      // 전투 결과 씬 (추가)
}
