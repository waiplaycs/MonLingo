#pragma once
#include "../pch.h"

namespace MonLingo {
    namespace Native {

        /// <summary>
        /// 日誌級別列舉
        /// </summary>
        enum class LogLevel {
            Debug,
            Info,
            Warning,
            Error
        };

        /// <summary>
        /// 日誌記錄器類
        /// 提供統一的日誌記錄功能
        /// </summary>
        class Logger {
        public:
            /// <summary>
            /// 初始化日誌系統
            /// </summary>
            static void Initialize();

            /// <summary>
            /// 清理日誌系統
            /// </summary>
            static void Cleanup();

            /// <summary>
            /// 記錄調試信息
            /// </summary>
            /// <param name="message">日誌消息</param>
            static void Debug(const std::string& message);

            /// <summary>
            /// 記錄一般信息
            /// </summary>
            /// <param name="message">日誌消息</param>
            static void Info(const std::string& message);

            /// <summary>
            /// 記錄警告信息
            /// </summary>
            /// <param name="message">日誌消息</param>
            static void Warning(const std::string& message);

            /// <summary>
            /// 記錄錯誤信息
            /// </summary>
            /// <param name="message">日誌消息</param>
            static void Error(const std::string& message);

        private:
            /// <summary>
            /// 寫入日誌到各個輸出目標
            /// </summary>
            /// <param name="level">日誌級別</param>
            /// <param name="message">日誌消息</param>
            static void WriteLog(LogLevel level, const std::string& message);

            /// <summary>
            /// 獲取當前時間字符串
            /// </summary>
            /// <returns>格式化的時間字符串</returns>
            static std::string GetCurrentTimeString();
        };

    } // namespace Native
} // namespace MonLingo
