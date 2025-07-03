using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BspFileScanner;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App: Application {
    protected override void OnStartup(StartupEventArgs e) {
        // 全局异常处理
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        base.OnStartup(e);
    }

    private static void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e) {
        LogException((Exception?) e.ExceptionObject);

        // 判断是否是致命异常，需要退出应用程序
        var isTerminating = e.IsTerminating;
        if (isTerminating && Current != null) {
            // 使用单独的线程执行关闭操作，避免阻塞当前线程
            Current.Dispatcher.BeginInvoke(new Action(() => {
                Current.Shutdown(1); // 1 表示异常退出代码
            }));
        }
    }

    private static void App_DispatcherUnhandledException(object? sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
        LogException(e.Exception);

        // 对于UI线程异常，可以选择处理后继续运行或退出
        // true 表示已处理; false 会导致应用程序崩溃退出
        e.Handled = false;
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e) {
        LogException(e.Exception);
        e.SetObserved();

        // 对于任务异常，通常不需要退出应用，但可以根据异常类型决定
        var shouldExit = ShouldExitOnException(e.Exception);
        if (shouldExit && Current != null) {
            Current.Dispatcher.BeginInvoke(new Action(() => { Current.Shutdown(1); }));
        }
    }

    private static void LogException(Exception? ex) {
        if (ex == null) return;
        try {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BspFileScanner.error.log");
            File.AppendAllText(logPath, $"[{DateTime.Now}] {ex}{Environment.NewLine}");
        } catch {
            // ignored 避免日志写入失败导致二次异常
        }
    }

    private static bool ShouldExitOnException(Exception ex) {
        // 根据具体需求定义哪些异常需要退出应用
        return ex is OutOfMemoryException or StackOverflowException or AccessViolationException;
    }
}
