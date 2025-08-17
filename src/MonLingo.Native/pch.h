#pragma once

// Windows Header Files
#include <windows.h>
#include <unknwn.h>
#include <restrictederrorinfo.h>
#include <hstring.h>

// WinRT Headers
#include <winrt/base.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.Graphics.Capture.h>
#include <winrt/Windows.Graphics.DirectX.h>
#include <winrt/Windows.Graphics.DirectX.Direct3D11.h>

// DirectX Headers
#include <d3d11.h>
#include <dxgi1_2.h>

// Standard Library
#include <memory>
#include <vector>
#include <string>
#include <thread>
#include <mutex>
#include <atomic>
#include <chrono>
#include <functional>

// API Export/Import Macros
#ifdef MONLINGO_NATIVE_EXPORTS
#define MONLINGO_NATIVE_API __declspec(dllexport)
#define MONLINGO_API extern "C" __declspec(dllexport)
#else
#define MONLINGO_NATIVE_API __declspec(dllimport)
#define MONLINGO_API extern "C" __declspec(dllimport)
#endif
