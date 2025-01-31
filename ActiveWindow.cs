using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;

public class ActiveWindow
{
    private const string CoreGraphicsLib = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
 
    private const string AppServicesLib = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";

    [DllImport(CoreGraphicsLib)]
    private static extern IntPtr CFStringCreateWithCString(IntPtr alloc, string str, uint encoding);

    [DllImport(AppServicesLib)]
    private static extern IntPtr AXUIElementCreateSystemWide();

    [DllImport(AppServicesLib)]
    private static extern int AXUIElementCopyAttributeValue(IntPtr element, IntPtr attribute, out IntPtr value);

    [DllImport(AppServicesLib)]
    private static extern int AXUIElementGetPid(IntPtr element, out int pid);

    private static readonly IntPtr kAXFocusedApplication = CFStringCreateWithCString(IntPtr.Zero, "AXFocusedApplication", 0);
    private static readonly IntPtr kAXFocusedWindow = CFStringCreateWithCString(IntPtr.Zero, "AXFocusedWindow", 0);


    private readonly struct CFIndex(int value)
    {
        private readonly IntPtr value = new IntPtr(value);

        public static implicit operator int(CFIndex index)
        {
            return index.value.ToInt32();
        }

        public static implicit operator CFIndex(int value)
        {
            return new CFIndex(value);
        }
    }


    public static bool IsProcessMainWindowFocused(int processId)
    {
        IntPtr systemWideElement = AXUIElementCreateSystemWide();

        int result = AXUIElementCopyAttributeValue(systemWideElement, kAXFocusedApplication, out nint focusedApp);

        if (result != 0 || focusedApp == IntPtr.Zero)
        {
            return false;
        }

        result = AXUIElementGetPid(focusedApp, out int focusedAppPid);
        if (result != 0 || focusedAppPid != processId)
        {
            return false;
        }

        result = AXUIElementCopyAttributeValue(focusedApp, kAXFocusedWindow, out nint focusedWindow);
        if (result != 0 || focusedWindow == IntPtr.Zero)
        {
            return false;
        }
        return true;
    }
}