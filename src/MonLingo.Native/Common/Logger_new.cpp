#include "../pch.h"
#include "Logger.h"
#include <iostream>
#include <fstream>
#include <sstream>
#include <ctime>

namespace MonLingo {
    namespace Native {

        static bool g_loggerInitialized = false;
        static std::mutex g_logMutex;
        static std::ofstream g_logFile;

        void Logger::Initialize() {
            std::lock_guard<std::mutex> lock(g_logMutex);
            
            if (g_loggerInitialized) {
                return;
            }

            try {
                // 創建日誌目錄
                CreateDirectoryA("logs", nullptr);
                
                // 打開日誌文件
                std::string logFileName = "logs/MonLingo_Native_" + GetCurrentTimeString() + ".log";
                g_logFile.open(logFileName, std::ios::out | std::ios::app);
                
                g_loggerInitialized = true;
                
                // 寫入初始化消息
                WriteLog(LogLevel::Info, "Logger initialized successfully");
            }
            catch (const std::exception& e) {
                // 如果文件日誌失敗，至少控制台還能工作
                g_loggerInitialized = true;
            }
        }

        void Logger::Cleanup() {
            std::lock_guard<std::mutex> lock(g_logMutex);
            
            if (g_loggerInitialized && g_logFile.is_open()) {
                WriteLog(LogLevel::Info, "Logger shutting down");
                g_logFile.close();
            }
            
            g_loggerInitialized = false;
        }

        void Logger::Debug(const std::string& message) {
            WriteLog(LogLevel::Debug, message);
        }

        void Logger::Info(const std::string& message) {
            WriteLog(LogLevel::Info, message);
        }

        void Logger::Warning(const std::string& message) {
            WriteLog(LogLevel::Warning, message);
        }

        void Logger::Error(const std::string& message) {
            WriteLog(LogLevel::Error, message);
        }

        void Logger::WriteLog(LogLevel level, const std::string& message) {
            if (!g_loggerInitialized) {
                return;
            }

            std::lock_guard<std::mutex> lock(g_logMutex);

            std::string levelString;
            switch (level) {
                case LogLevel::Debug:   levelString = "DEBUG"; break;
                case LogLevel::Info:    levelString = "INFO"; break;
                case LogLevel::Warning: levelString = "WARN"; break;
                case LogLevel::Error:   levelString = "ERROR"; break;
            }

            std::string timestamp = GetCurrentTimeString();
            std::string logLine = "[" + timestamp + "] [" + levelString + "] " + message;

            // 輸出到控制台
#ifdef _DEBUG
            std::cout << logLine << std::endl;
#endif

            // 輸出到文件
            if (g_logFile.is_open()) {
                g_logFile << logLine << std::endl;
                g_logFile.flush();
            }

            // 輸出到 Windows 調試器
            OutputDebugStringA((logLine + "\n").c_str());
        }

        std::string Logger::GetCurrentTimeString() {
            auto now = std::chrono::system_clock::now();
            auto time_t = std::chrono::system_clock::to_time_t(now);
            auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(
                now.time_since_epoch()) % 1000;

            std::tm* tm_info = std::localtime(&time_t);
            
            std::ostringstream oss;
            oss << std::put_time(tm_info, "%Y-%m-%d %H:%M:%S");
            oss << '.' << std::setfill('0') << std::setw(3) << ms.count();
            
            return oss.str();
        }

    } // namespace Native
} // namespace MonLingo
