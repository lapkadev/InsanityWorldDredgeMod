using System;
using System.IO;
using System.Runtime.InteropServices;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.P2P;
using Epic.OnlineServices.Platform;
using UnityEngine;
using static InsanityWorldMod.Core.Constants;

namespace InsanityWorldMod.Core
{
    public static partial class Constants
    {
        public const string EOS_NATIVE_DLL   = "EOSSDK-Win32-Shipping.dll";
        public const string EOS_NATIVE_SUBDIR = "lib";
        public const string EOS_DEVICE_MODEL = "PC Windows 32bit";
    }

    public class EosRuntime : MonoBehaviour
    {
        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpLibFileName);

        private static bool _bootstrapped;

        private EosCredentials _creds;
        private PlatformInterface _platform;
        private bool _connecting;
        private ulong _authExpirationHandle;

        public void Boot(EosCredentials creds)
        {
            _creds = creds;

            if (!LoadNativeLibrary())
                return;
            if (!InitializePlatform())
                return;

            BeginDeviceIdLogin();
        }

        private bool LoadNativeLibrary()
        {
            if (_bootstrapped)
                return true;

            var libPath = Path.Combine(DredgeHooks.GetModBasePath(), EOS_NATIVE_SUBDIR, EOS_NATIVE_DLL);
            if (!File.Exists(libPath))
            {
                G.Log.Error($"EosRuntime: native SDK not found at {libPath}");
                return false;
            }

            var handle = LoadLibrary(libPath);
            if (handle == IntPtr.Zero)
            {
                G.Log.Error($"EosRuntime: LoadLibrary failed for {libPath} (win32 error {Marshal.GetLastWin32Error()})");
                return false;
            }

            _bootstrapped = true;
            G.Log.Info("EosRuntime: native EOS SDK loaded");
            return true;
        }

        private bool InitializePlatform()
        {
            var initOptions = new InitializeOptions
            {
                ProductName    = _creds.ProductName,
                ProductVersion = _creds.ProductVersion,
            };
            var initResult = PlatformInterface.Initialize(ref initOptions);
            if (initResult != Result.Success && initResult != Result.AlreadyConfigured)
            {
                G.Log.Error($"EosRuntime: PlatformInterface.Initialize failed: {initResult}");
                return false;
            }

            LoggingInterface.SetLogLevel(LogCategory.AllCategories, LogLevel.Warning);
            LoggingInterface.SetCallback(OnEosLog);

            var createOptions = new Options
            {
                ProductId    = _creds.ProductId,
                SandboxId    = _creds.SandboxId,
                DeploymentId = _creds.DeploymentId,
                ClientCredentials = new ClientCredentials
                {
                    ClientId     = _creds.ClientId,
                    ClientSecret = _creds.ClientSecret,
                },
            };
            _platform = PlatformInterface.Create(ref createOptions);
            if (_platform == null)
            {
                G.Log.Error("EosRuntime: PlatformInterface.Create returned null");
                return false;
            }

            G.Net.P2P = _platform.GetP2PInterface();

            G.Log.Info("EosRuntime: EOS platform created");
            return true;
        }

        private void OnEosLog(ref LogMessage msg)
        {
            G.Log.Info($"[EOS] {msg.Category}: {msg.Message}");
        }

        private void BeginDeviceIdLogin()
        {
            _connecting = true;
            var options = new CreateDeviceIdOptions { DeviceModel = EOS_DEVICE_MODEL };
            _platform.GetConnectInterface().CreateDeviceId(ref options, null, OnCreateDeviceId);
        }

        private void OnCreateDeviceId(ref CreateDeviceIdCallbackInfo info)
        {
            if (info.ResultCode == Result.Success || info.ResultCode == Result.DuplicateNotAllowed)
            {
                ConnectLogin();
            }
            else if (Common.IsOperationComplete(info.ResultCode))
            {
                _connecting = false;
                G.Log.Error($"EosRuntime: CreateDeviceId failed: {info.ResultCode}");
            }
        }

        private void ConnectLogin()
        {
            var options = new LoginOptions
            {
                UserLoginInfo = new UserLoginInfo { DisplayName = _creds.ProductName },
                Credentials = new Credentials
                {
                    Type  = ExternalCredentialType.DeviceidAccessToken,
                    Token = null,
                },
            };
            _platform.GetConnectInterface().Login(ref options, null, OnConnectLogin);
        }

        private void OnConnectLogin(ref LoginCallbackInfo info)
        {
            if (info.ResultCode == Result.Success)
            {
                _connecting = false;
                G.Net.LocalUserId = info.LocalUserId;
                G.Net.IsInited = true;
                G.Log.Info("EosRuntime: Connect login succeeded");

                var authExpOptions = new AddNotifyAuthExpirationOptions();
                _authExpirationHandle = _platform.GetConnectInterface().AddNotifyAuthExpiration(ref authExpOptions, null, OnAuthExpiration);
            }
            else if (Common.IsOperationComplete(info.ResultCode))
            {
                var options = new CreateUserOptions { ContinuanceToken = info.ContinuanceToken };
                _platform.GetConnectInterface().CreateUser(ref options, null, OnCreateUser);
            }
        }

        private void OnAuthExpiration(ref AuthExpirationCallbackInfo info)
        {
            _platform.GetConnectInterface().RemoveNotifyAuthExpiration(_authExpirationHandle);
            ConnectLogin();
        }

        private void OnCreateUser(ref CreateUserCallbackInfo info)
        {
            if (info.ResultCode != Result.Success)
            {
                _connecting = false;
                G.Log.Error($"EosRuntime: CreateUser failed: {info.ResultCode}");
                return;
            }
            ConnectLogin();
        }

        private void LateUpdate()
        {
            _platform?.Tick();
        }

        private void OnApplicationQuit()
        {
            if (_platform != null)
            {
                _platform.Release();
                _platform = null;
                PlatformInterface.Shutdown();
            }
        }
    }
}
