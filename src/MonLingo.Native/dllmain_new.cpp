#include "pch.h"

// 前向聲明
extern "C" void native_cleanup();

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        // DLL被加載到進程時
        break;
    case DLL_THREAD_ATTACH:
        // 新線程創建時
        break;
    case DLL_THREAD_DETACH:
        // 線程結束時
        break;
    case DLL_PROCESS_DETACH:
        // DLL從進程卸載時
        // 調用清理函數
        native_cleanup();
        break;
    }
    return TRUE;
}
