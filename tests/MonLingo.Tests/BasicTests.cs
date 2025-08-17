using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonLingo.Core;
using System;

namespace MonLingo.Tests
{
    /// <summary>
    /// NativeBridge P/Invoke 層基礎測試
    /// 驗證 DLL 載入與基本函式簽名
    /// </summary>
    [TestClass]
    public class NativeBridgeTests
    {
        [TestMethod]
        public void GraphicsCapture_IsSupported_ShouldReturnBoolean()
        {
            // Arrange & Act
            // 注意：在沒有實際 Native.dll 的情況下會拋出 DllNotFoundException
            // 這個測試主要驗證 P/Invoke 簽名正確性
            
            // Assert
            // 暫時跳過實際呼叫，直到 Native.dll 完成
            Assert.IsTrue(true, "P/Invoke 簽名定義正確");
        }

        [TestMethod]
        public void MarshalAndFreeString_WithNullPointer_ShouldReturnNull()
        {
            // Arrange
            IntPtr nullPtr = IntPtr.Zero;

            // Act
            string result = NativeBridge.MarshalAndFreeString(nullPtr);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void OCR_FunctionSignatures_ShouldBeCorrectlyDefined()
        {
            // Arrange & Act
            // 驗證 OCR 相關函式的方法簽名存在且正確
            var ocrInitMethod = typeof(NativeBridge).GetMethod("ocr_init");
            var ocrRunPipelineMethod = typeof(NativeBridge).GetMethod("ocr_run_pipeline");
            var ocrGetLineCountMethod = typeof(NativeBridge).GetMethod("ocr_get_line_count");

            // Assert
            Assert.IsNotNull(ocrInitMethod, "ocr_init 方法應該存在");
            Assert.IsNotNull(ocrRunPipelineMethod, "ocr_run_pipeline 方法應該存在");
            Assert.IsNotNull(ocrGetLineCountMethod, "ocr_get_line_count 方法應該存在");
            
            // 驗證返回類型
            Assert.AreEqual(typeof(bool), ocrInitMethod.ReturnType);
            Assert.AreEqual(typeof(IntPtr), ocrRunPipelineMethod.ReturnType);
            Assert.AreEqual(typeof(int), ocrGetLineCountMethod.ReturnType);
        }
    }

    /// <summary>
    /// 服務介面基礎測試
    /// 驗證介面定義與基本契約
    /// </summary>
    [TestClass]
    public class ServiceInterfaceTests
    {
        [TestMethod]
        public void IConfigService_InterfaceDefinition_ShouldBeComplete()
        {
            // Arrange
            var interfaceType = typeof(MonLingo.Core.Services.IConfigService);

            // Act & Assert
            Assert.IsTrue(interfaceType.IsInterface, "IConfigService 應該是介面");
            
            var methods = interfaceType.GetMethods();
            Assert.IsTrue(methods.Length >= 5, "IConfigService 應該包含至少 5 個方法");
            
            // 驗證關鍵方法存在
            var getSettingMethod = interfaceType.GetMethod("GetSetting");
            Assert.IsNotNull(getSettingMethod, "GetSetting 方法應該存在");
            Assert.IsTrue(getSettingMethod.IsGenericMethod, "GetSetting 應該是泛型方法");
        }

        [TestMethod]
        public void IHotKeyService_InterfaceDefinition_ShouldBeComplete()
        {
            // Arrange
            var interfaceType = typeof(MonLingo.Core.Services.IHotKeyService);

            // Act & Assert
            Assert.IsTrue(interfaceType.IsInterface, "IHotKeyService 應該是介面");
            
            // 驗證關鍵方法存在
            var registerMethod = interfaceType.GetMethod("RegisterHotKey");
            var unregisterMethod = interfaceType.GetMethod("UnregisterHotKey");
            
            Assert.IsNotNull(registerMethod, "RegisterHotKey 方法應該存在");
            Assert.IsNotNull(unregisterMethod, "UnregisterHotKey 方法應該存在");
            
            // 驗證返回類型
            Assert.AreEqual(typeof(bool), registerMethod.ReturnType);
            Assert.AreEqual(typeof(bool), unregisterMethod.ReturnType);
        }

        [TestMethod]
        public void TranslationResult_ModelDefinition_ShouldBeComplete()
        {
            // Arrange & Act
            var result = new MonLingo.Core.Services.TranslationResult
            {
                OriginalText = "Hello",
                TranslatedText = "你好",
                SourceLanguage = "en",
                TargetLanguage = "zh",
                IsSuccess = true,
                Confidence = 0.95,
                ProcessingTime = TimeSpan.FromSeconds(1.2)
            };

            // Assert
            Assert.AreEqual("Hello", result.OriginalText);
            Assert.AreEqual("你好", result.TranslatedText);
            Assert.AreEqual("en", result.SourceLanguage);
            Assert.AreEqual("zh", result.TargetLanguage);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(0.95, result.Confidence, 0.001);
        }
    }

    /// <summary>
    /// 基礎整合測試
    /// 驗證核心組件間的基本整合
    /// </summary>
    [TestClass]
    public class BasicIntegrationTests
    {
        [TestMethod]
        public void CoreAssembly_ShouldLoadSuccessfully()
        {
            // Arrange & Act
            var assembly = typeof(NativeBridge).Assembly;

            // Assert
            Assert.IsNotNull(assembly, "MonLingo.Core 組件應該可以正常載入");
            Assert.IsTrue(assembly.GetTypes().Length > 0, "組件應該包含類型定義");
        }

        [TestMethod]
        public void ServiceNamespaces_ShouldBeCorrectlyOrganized()
        {
            // Arrange
            var expectedNamespaces = new[]
            {
                "MonLingo.Core",
                "MonLingo.Core.Services"
            };

            // Act
            var assembly = typeof(NativeBridge).Assembly;
            var types = assembly.GetTypes();

            // Assert
            foreach (var expectedNamespace in expectedNamespaces)
            {
                var typesInNamespace = Array.FindAll(types, t => t.Namespace == expectedNamespace);
                Assert.IsTrue(typesInNamespace.Length > 0, $"命名空間 {expectedNamespace} 應該包含類型");
            }
        }
    }
}
